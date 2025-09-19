using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

namespace DevOpsExtension.Handlers;

public class AzureDevOpsPermissionHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsPermission, AzureDevOpsPermissionIdentifiers>
{
    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var (assigned, groupDesc, projectGroupDesc) = await EnsureGraphEntitiesResolvedAsync(request.Config, props, resolveOnly: true, cancellationToken);
        props.Assigned = assigned;
        props.GroupDescriptor = groupDesc;
        props.ProjectGroupDescriptor = projectGroupDesc;
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var (assigned, groupDesc, projectGroupDesc) = await EnsureGraphEntitiesResolvedAsync(request.Config, props, resolveOnly: false, cancellationToken);
        props.Assigned = assigned;
        props.GroupDescriptor = groupDesc;
        props.ProjectGroupDescriptor = projectGroupDesc;
        return GetResponse(request);
    }

    protected override AzureDevOpsPermissionIdentifiers GetIdentifiers(AzureDevOpsPermission properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
    };

    private async Task<(bool assigned, string? groupDescriptor, string? projectGroupDescriptor)> EnsureGraphEntitiesResolvedAsync(Configuration configuration, AzureDevOpsPermission props, bool resolveOnly, CancellationToken ct)
    {
        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        var graphBase = GetGraphBaseUrl(baseUrl);
        using var client = CreateClient(configuration);
        var projectId = await ResolveProjectIdOrThrowAsync(client, org, baseUrl, props.Project, ct);
        var groupDescriptor = await EnsureGroupImportedAsync(client, org, graphBase, props.GroupObjectId, ct);
        var projectGroupDescriptor = await ResolveProjectGroupDescriptorAsync(client, org, graphBase, projectId, props.Role, ct);

        // Check if membership already exists
        var isMember = await CheckMembershipAsync(client, graphBase, org, groupDescriptor, projectGroupDescriptor, ct);
        if (!isMember && !resolveOnly)
        {
            // Add membership
            await AddMembershipAsync(client, graphBase, org, groupDescriptor, projectGroupDescriptor, ct);

            // Verify
            isMember = await CheckMembershipAsync(client, graphBase, org, groupDescriptor, projectGroupDescriptor, ct);
        }

        return (isMember, groupDescriptor, projectGroupDescriptor);
    }

    private static async Task<string> EnsureGroupImportedAsync(HttpClient client, string org, string graphBase, string entraGroupObjectId, CancellationToken ct)
    {
        // Validate GUID format early to reduce surprises
        if (!Guid.TryParse(entraGroupObjectId, out _))
        {
            throw new InvalidOperationException($"GroupObjectId '{entraGroupObjectId}' is not a valid GUID for an Entra ID group.");
        }

        var baseListUrl = $"{graphBase}/{org}/_apis/graph/groups?subjectTypes=aadgp&api-version=7.1-preview.1";
        string? continuation = null;
        while (true)
        {
            var listUrl = continuation is null ? baseListUrl : baseListUrl + "&continuationToken=" + Uri.EscapeDataString(continuation);
            var listResp = await client.GetAsync(listUrl, ct);
            if (!listResp.IsSuccessStatusCode)
            {
                break;
            }
            var listJson = await listResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (listJson.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var origin = item.TryGetProperty("origin", out var o) ? o.GetString() : null;
                    var subjectKind = item.TryGetProperty("subjectKind", out var sk) ? sk.GetString() : null;
                    var originId = item.TryGetProperty("originId", out var oid) ? oid.GetString() : null;
                    if (string.Equals(origin, "aad", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(subjectKind, "group", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(originId, entraGroupObjectId, StringComparison.OrdinalIgnoreCase))
                    {
                        return item.GetProperty("descriptor").GetString()!;
                    }
                }
            }
            // get continuation header
            if (listResp.Headers.TryGetValues("X-MS-ContinuationToken", out var values))
            {
                continuation = values is null ? null : System.Linq.Enumerable.FirstOrDefault(values);
                if (string.IsNullOrEmpty(continuation))
                {
                    break;
                }
            }
            else
            {
                break;
            }
        }

        // Not present, create/import via Graph groups create
        var body = new { originId = entraGroupObjectId };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var createResp = await client.PostAsync($"{graphBase}/{org}/_apis/graph/groups?api-version=7.1-preview.1", content, ct);
        if (!createResp.IsSuccessStatusCode)
        {
            var err = await createResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to import Entra ID group into Azure DevOps Graph: {(int)createResp.StatusCode} {createResp.ReasonPhrase} {err}");
        }
        var json = await createResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var createdOrigin = json.TryGetProperty("origin", out var co) ? co.GetString() : null;
        var createdSubjectKind = json.TryGetProperty("subjectKind", out var csk) ? csk.GetString() : null;
        if (!string.Equals(createdOrigin, "aad", StringComparison.OrdinalIgnoreCase) || !string.Equals(createdSubjectKind, "group", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Entra group import returned a non-Entra or non-group subject.");
        }
        return json.GetProperty("descriptor").GetString()!;
    }

    private static async Task<string> ResolveProjectGroupDescriptorAsync(HttpClient client, string org, string graphBase, string projectId, string role, CancellationToken ct)
    {
        // First resolve the project's Graph descriptor from its GUID
        var descResp = await client.GetAsync($"{graphBase}/{org}/_apis/graph/descriptors/{projectId}?api-version=7.1-preview.1", ct);
        if (!descResp.IsSuccessStatusCode)
        {
            var err = await descResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to resolve project graph descriptor: {(int)descResp.StatusCode} {descResp.ReasonPhrase} {err}");
        }
        var descJson = await descResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var scopeDescriptor = descJson.TryGetProperty("value", out var val) ? val.GetString() : null;
        if (string.IsNullOrWhiteSpace(scopeDescriptor))
        {
            throw new InvalidOperationException("Project graph descriptor response missing 'value'.");
        }

        // List groups for the project scope and pick the known Readers/Contributors built-in groups
        var resp = await client.GetAsync($"{graphBase}/{org}/_apis/graph/groups?scopeDescriptor={Uri.EscapeDataString(scopeDescriptor)}&api-version=7.1-preview.1", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to list project groups: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (!json.TryGetProperty("value", out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidOperationException("Unexpected response format when listing project groups.");
        }

        var roleNorm = role?.Trim();
        if (string.IsNullOrWhiteSpace(roleNorm))
        {
            throw new InvalidOperationException("Role is required. Allowed values: Readers, Contributors.");
        }
        string targetName = string.Equals(roleNorm, "readers", StringComparison.OrdinalIgnoreCase) ? "Readers"
            : string.Equals(roleNorm, "contributors", StringComparison.OrdinalIgnoreCase) ? "Contributors"
            : throw new InvalidOperationException($"Unsupported role '{role}'. Allowed values: Readers, Contributors.");
        foreach (var item in arr.EnumerateArray())
        {
            var desc = item.TryGetProperty("descriptor", out var d) ? d.GetString() : null;
            var displayName = item.TryGetProperty("displayName", out var dn) ? dn.GetString() : null;
            if (string.Equals(displayName, targetName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(desc))
            {
                return desc!;
            }
            // Fallback: principalName may contain project-qualified name including the role
            var principalName = item.TryGetProperty("principalName", out var pn) ? pn.GetString() : null;
            if (!string.IsNullOrWhiteSpace(principalName) && principalName!.Contains(targetName, StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(desc))
            {
                return desc!;
            }
        }
        // As a fallback try known well-known descriptors might not be stable; better to throw
        throw new InvalidOperationException($"Could not locate the '{targetName}' security group for project.");
    }

    private static async Task<bool> CheckMembershipAsync(HttpClient client, string graphBase, string org, string memberDescriptor, string containerDescriptor, CancellationToken ct)
    {
        var resp = await client.GetAsync($"{graphBase}/{org}/_apis/graph/memberships/{Uri.EscapeDataString(memberDescriptor)}/{Uri.EscapeDataString(containerDescriptor)}?api-version=7.1-preview.1", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.OK)
        {
            return true;
        }
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
        // For other statuses treat as not found but surface on add
        return false;
    }

    private static async Task AddMembershipAsync(HttpClient client, string graphBase, string org, string memberDescriptor, string containerDescriptor, CancellationToken ct)
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await client.PutAsync($"{graphBase}/{org}/_apis/graph/memberships/{Uri.EscapeDataString(memberDescriptor)}/{Uri.EscapeDataString(containerDescriptor)}?api-version=7.1-preview.1", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to add group membership: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
    }
}
