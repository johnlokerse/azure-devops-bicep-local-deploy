using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using System.Net.Http;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

namespace DevOpsExtension.Handlers;

/// <summary>
/// Assigns a Microsoft Entra ID (Azure AD) group to a project security group in Azure DevOps.
/// The role is free input and can be any built-in or custom project group name (for example:
/// Readers, Contributors, Build Administrators, Endpoint Administrators, Endpoint Creators,
/// Project Administrators, Project Valid Users, or a custom group you created).
/// This resource is idempotent: it imports the AAD group into Azure DevOps Graph if needed,
/// locates the project group by name within the project scope, and ensures membership exists.
/// </summary>
public class AzureDevOpsPermissionHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsPermission, AzureDevOpsPermissionIdentifiers>
{
    private const string GraphApiVersion = "7.1-preview.1";
    private const string ContinuationHeader = "X-MS-ContinuationToken";

    /// <inheritdoc />
    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var (assigned, groupDescriptor, projectGroupDescriptor) = await EnsureGraphEntitiesResolvedAsync(request.Config, props, resolveOnly: true, cancellationToken);
        props.Assigned = assigned;
        props.GroupDescriptor = groupDescriptor;
        props.ProjectGroupDescriptor = projectGroupDescriptor;
        return GetResponse(request);
    }

    /// <inheritdoc />
    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var (assigned, groupDescriptor, projectGroupDescriptor) = await EnsureGraphEntitiesResolvedAsync(request.Config, props, resolveOnly: false, cancellationToken);
        props.Assigned = assigned;
        props.GroupDescriptor = groupDescriptor;
        props.ProjectGroupDescriptor = projectGroupDescriptor;
        return GetResponse(request);
    }

    protected override AzureDevOpsPermissionIdentifiers GetIdentifiers(AzureDevOpsPermission properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
    };

    /// <summary>
    /// Ensures the AAD group is imported into ADO Graph, resolves the target project role group,
    /// and creates the membership if necessary.
    /// </summary>
    private async Task<(bool assigned, string? groupDescriptor, string? projectGroupDescriptor)> EnsureGraphEntitiesResolvedAsync(
        Configuration configuration,
        AzureDevOpsPermission props,
        bool resolveOnly,
        CancellationToken cancellationToken)
    {
        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        var graphBase = GetGraphBaseUrl(baseUrl);
        using var client = CreateClient(configuration);
        var projectId = await ResolveProjectIdOrThrowAsync(client, org, baseUrl, props.Project, cancellationToken);
        var groupDescriptor = await EnsureGroupImportedAsync(client, org, graphBase, props.GroupObjectId, cancellationToken);
        var projectGroupDescriptor = await ResolveProjectGroupDescriptorAsync(client, org, graphBase, projectId, props.Project, props.Role, cancellationToken);

        // Check if membership already exists
        var isMember = await CheckMembershipAsync(client, graphBase, org, groupDescriptor, projectGroupDescriptor, cancellationToken);
        if (!isMember && !resolveOnly)
        {
            // Add membership
            await AddMembershipAsync(client, graphBase, org, groupDescriptor, projectGroupDescriptor, cancellationToken);

            // Verify
            isMember = await CheckMembershipAsync(client, graphBase, org, groupDescriptor, projectGroupDescriptor, cancellationToken);
        }

        return (isMember, groupDescriptor, projectGroupDescriptor);
    }

    /// <summary>
    /// Ensures the specified Entra group (by objectId) exists in Azure DevOps Graph and returns its descriptor.
    /// </summary>
    private static async Task<string> EnsureGroupImportedAsync(HttpClient client, string org, string graphBase, string entraGroupObjectId, CancellationToken cancellationToken)
    {
        // Validate GUID format early to reduce surprises
        if (!Guid.TryParse(entraGroupObjectId, out _))
        {
            throw new InvalidOperationException($"GroupObjectId '{entraGroupObjectId}' is not a valid GUID for an Entra ID group.");
        }

        var baseListUrl = $"{graphBase}/{org}/_apis/graph/groups?subjectTypes=aadgp&api-version={GraphApiVersion}";
        string? continuation = null;
        while (true)
        {
            var listUrl = continuation is null ? baseListUrl : baseListUrl + "&continuationToken=" + Uri.EscapeDataString(continuation);
            var listResp = await client.GetAsync(listUrl, cancellationToken);
            if (!listResp.IsSuccessStatusCode)
            {
                break;
            }
            var listJson = await listResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (listJson.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array && arr.GetArrayLength() > 0)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    var origin = GetString(item, "origin");
                    var subjectKind = GetString(item, "subjectKind");
                    var originId = GetString(item, "originId");
                    if (string.Equals(origin, "aad", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(subjectKind, "group", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(originId, entraGroupObjectId, StringComparison.OrdinalIgnoreCase))
                    {
                        return GetString(item, "descriptor")!;
                    }
                }
            }
            // get continuation header
            if (listResp.Headers.TryGetValues(ContinuationHeader, out var values))
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
        var createResp = await client.PostAsync($"{graphBase}/{org}/_apis/graph/groups?api-version={GraphApiVersion}", content, cancellationToken);
        if (!createResp.IsSuccessStatusCode)
        {
            var err = await createResp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to import Entra ID group into Azure DevOps Graph: {(int)createResp.StatusCode} {createResp.ReasonPhrase} {err}");
        }
        var json = await createResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var createdOrigin = GetString(json, "origin");
        var createdSubjectKind = GetString(json, "subjectKind");
        if (!string.Equals(createdOrigin, "aad", StringComparison.OrdinalIgnoreCase) || !string.Equals(createdSubjectKind, "group", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Entra group import returned a non-Entra or non-group subject.");
        }
        return GetString(json, "descriptor")!;
    }

    /// <summary>
    /// Resolves the project-scoped built-in group descriptor matching the requested role.
    /// </summary>
    private static async Task<string> ResolveProjectGroupDescriptorAsync(HttpClient client, string org, string graphBase, string projectId, string projectName, string role, CancellationToken cancellationToken)
    {
        // First resolve the project's Graph descriptor from its GUID
        var descResp = await client.GetAsync($"{graphBase}/{org}/_apis/graph/descriptors/{projectId}?api-version={GraphApiVersion}", cancellationToken);
        if (!descResp.IsSuccessStatusCode)
        {
            var err = await descResp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to resolve project graph descriptor: {(int)descResp.StatusCode} {descResp.ReasonPhrase} {err}");
        }
        var descJson = await descResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        var scopeDescriptor = GetString(descJson, "value");
        if (string.IsNullOrWhiteSpace(scopeDescriptor))
        {
            throw new InvalidOperationException("Project graph descriptor response missing 'value'.");
        }

        // List groups for the project scope with pagination and perform strict matching
        var targetName = string.IsNullOrWhiteSpace(role) ? throw new InvalidOperationException("Role is required.") : role.Trim();
        var expectedPrincipal = $"[{projectName}]\\{targetName}"; // project-qualified principal name

        var baseListUrl = $"{graphBase}/{org}/_apis/graph/groups?scopeDescriptor={Uri.EscapeDataString(scopeDescriptor)}&api-version={GraphApiVersion}";
        string? continuation = null;
        var existingRolesGroups = new List<string>(capacity: 8);

        while (true)
        {
            var listUrl = continuation is null ? baseListUrl : baseListUrl + "&continuationToken=" + Uri.EscapeDataString(continuation);
            var resp = await client.GetAsync(listUrl, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                var err = await resp.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"Failed to list project groups: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (json.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (!IsProjectScoped(item))
                    {
                        continue;
                    }

                    var displayName = GetString(item, "displayName");
                    if (existingRolesGroups.Count < 8 && !string.IsNullOrWhiteSpace(displayName))
                    {
                        existingRolesGroups.Add(displayName!);
                    }

                    if (IsMatchingGroup(item, targetName, expectedPrincipal))
                    {
                        return GetString(item, "descriptor")!;
                    }
                }
            }

            if (resp.Headers.TryGetValues(ContinuationHeader, out var values))
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

        var existingRolesGroupsList = existingRolesGroups.Count == 0 ? "none" : string.Join(", ", existingRolesGroups);
        throw new InvalidOperationException($"Role '{targetName}' does not exist in project '{projectName}'. Available examples: {existingRolesGroupsList}.");
    }

    /// <summary>
    /// Checks if a membership exists between the member and container descriptors.
    /// </summary>
    private static async Task<bool> CheckMembershipAsync(HttpClient client, string graphBase, string org, string memberDescriptor, string containerDescriptor, CancellationToken cancellationToken)
    {
        var resp = await client.GetAsync($"{graphBase}/{org}/_apis/graph/memberships/{Uri.EscapeDataString(memberDescriptor)}/{Uri.EscapeDataString(containerDescriptor)}?api-version={GraphApiVersion}", cancellationToken);
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

    /// <summary>
    /// Adds the membership between the member and container descriptors.
    /// </summary>
    private static async Task AddMembershipAsync(HttpClient client, string graphBase, string org, string memberDescriptor, string containerDescriptor, CancellationToken cancellationToken)
    {
        var content = new StringContent("{}", Encoding.UTF8, "application/json");
        var resp = await client.PutAsync($"{graphBase}/{org}/_apis/graph/memberships/{Uri.EscapeDataString(memberDescriptor)}/{Uri.EscapeDataString(containerDescriptor)}?api-version={GraphApiVersion}", content, cancellationToken);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to add group membership: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
    }

    private static string? GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.ValueKind == JsonValueKind.Null ? null : value.GetString()
            : null;
    }
    private static bool IsProjectScoped(JsonElement group)
    {
        var domain = GetString(group, "domain");
        return !string.IsNullOrWhiteSpace(domain) &&
               domain.Contains("Classification/TeamProject", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsMatchingGroup(JsonElement group, string targetName, string expectedPrincipal)
    {
        var displayName = GetString(group, "displayName");
        if (!string.IsNullOrWhiteSpace(displayName) &&
            string.Equals(displayName, targetName, StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(GetString(group, "descriptor"));
        }

        var principalName = GetString(group, "principalName");
        if (!string.IsNullOrWhiteSpace(principalName) &&
            string.Equals(principalName, expectedPrincipal, StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(GetString(group, "descriptor"));
        }

        return false;
    }
}
