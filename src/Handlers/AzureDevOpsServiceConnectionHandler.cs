using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

namespace DevOpsExtension.Handlers;

public class AzureDevOpsServiceConnectionHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsServiceConnection, AzureDevOpsServiceConnectionIdentifiers>
{
    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetServiceConnectionAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            PopulateOutputs(request.Properties, existing);
            await SetFederatedOutputsAsync(request.Config, request.Properties, cancellationToken);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var existing = await GetServiceConnectionAsync(request.Config, props, cancellationToken);
        if (existing is null)
        {
            await CreateServiceConnectionAsync(request.Config, props, cancellationToken);
            existing = await GetServiceConnectionAsync(request.Config, props, cancellationToken) ?? throw new InvalidOperationException("Service connection creation did not return service connection.");
        }
        PopulateOutputs(props, existing);
        await SetFederatedOutputsAsync(request.Config, props, cancellationToken);
        return GetResponse(request);
    }

    protected override AzureDevOpsServiceConnectionIdentifiers GetIdentifiers(AzureDevOpsServiceConnection properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
        Name = properties.Name,
    };

    private void PopulateOutputs(AzureDevOpsServiceConnection props, dynamic sc)
    {
        props.ServiceConnectionId = sc.id;
        props.Url = sc.url;
        props.AuthorizationScheme = sc.scheme;
    }

    private async Task<dynamic?> GetServiceConnectionAsync(Configuration configuration, AzureDevOpsServiceConnection props, CancellationToken ct)
    {
        try
        {
            var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
            using var client = CreateClient(configuration);
            // list filtered by name as API doesn't provide direct by-name endpoint
            var resp = await client.GetAsync($"{baseUrl}/{org}/{Uri.EscapeDataString(props.Project)}/_apis/serviceendpoint/endpoints?endpointNames={Uri.EscapeDataString(props.Name)}&api-version=7.1-preview.4", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            if (json.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in arr.EnumerateArray())
                {
                    if (string.Equals(item.GetProperty("name").GetString(), props.Name, StringComparison.OrdinalIgnoreCase))
                    {
                        return new
                        {
                            id = item.GetProperty("id").GetString(),
                            name = item.GetProperty("name").GetString(),
                            type = item.TryGetProperty("type", out var t) ? t.GetString() : null,
                            url = item.TryGetProperty("url", out var u) ? u.GetString() : null,
                            scheme = item.TryGetProperty("authorization", out var auth) && auth.TryGetProperty("scheme", out var sch) ? sch.GetString() : null
                        };
                    }
                }
            }
        }
        catch { }
        return null;
    }

    private async Task CreateServiceConnectionAsync(Configuration configuration, AzureDevOpsServiceConnection props, CancellationToken ct)
    {
        ValidateProps(props);
        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using var client = CreateClient(configuration);
        var projectId = await ResolveProjectIdAsync(client, org, baseUrl, props.Project, ct);
        object body = BuildCreationBody(props, projectId, baseUrl, org);
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync($"{baseUrl}/{org}/{Uri.EscapeDataString(props.Project)}/_apis/serviceendpoint/endpoints?api-version=7.1-preview.4", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to create service connection: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }

        // Delay to allow backend consistency before permission patch
        await Task.Delay(TimeSpan.FromSeconds(3), ct);

        if (props.GrantAllPipelines)
        {
            try
            {
                // Need the created id; quick lookup
                var created = await GetServiceConnectionAsync(configuration, props, ct);
                if (created != null)
                {
                    var permissionBody = new
                    {
                        allPipelines = new { authorized = true, authorizedBy = (string?)null, authorizedOn = (string?)null },
                        pipelines = Array.Empty<object>(),
                        resource = new { type = "endpoint", id = (string)created.id }
                    };
                    var permContent = new StringContent(JsonSerializer.Serialize(permissionBody, JsonOptions), Encoding.UTF8, "application/json");
                    var permResp = await PatchPermissionsAsync(client, $"{baseUrl}/{org}/{Uri.EscapeDataString(props.Project)}/_apis/pipelines/pipelinePermissions/endpoint/{created.id}?api-version=7.1-preview.1", permContent, ct);
                    permResp.Dispose();
                }
            }
            catch { }
        }
    }

    private static void ValidateProps(AzureDevOpsServiceConnection config)
    {
        // Mutually exclusive scope sets: either Subscription (subscriptionId + subscriptionName) OR Management Group (managementGroupName/Id)
        var hasSubscriptionSet = !string.IsNullOrWhiteSpace(config.SubscriptionId) && !string.IsNullOrWhiteSpace(config.SubscriptionName);
        var hasManagementGroupSet = !string.IsNullOrWhiteSpace(config.ManagementGroupName) || !string.IsNullOrWhiteSpace(config.ManagementGroupId);
        if (hasSubscriptionSet && hasManagementGroupSet)
        {
            throw new InvalidOperationException("Provide either subscriptionId/subscriptionName OR managementGroupName/managementGroupId, not both.");
        }

        if (string.IsNullOrWhiteSpace(config.TenantId))
        {
            throw new InvalidOperationException("TenantId is required.");
        }
        if (string.IsNullOrWhiteSpace(config.ClientId))
        {
            throw new InvalidOperationException("ClientId is required for workload identity federation.");
        }
        var scope = config.ScopeLevel ?? AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription;
        if (scope == AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription)
        {
            if (string.IsNullOrWhiteSpace(config.SubscriptionId) || string.IsNullOrWhiteSpace(config.SubscriptionName))
            {
                throw new InvalidOperationException("SubscriptionId and SubscriptionName are required for Subscription scope.");
            }
        }
        else if (scope == AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup)
        {
            if (string.IsNullOrWhiteSpace(config.ManagementGroupName))
            {
                throw new InvalidOperationException("ManagementGroupName is required for ManagementGroup scope.");
            }
        }
    }

    private static object BuildCreationBody(AzureDevOpsServiceConnection props, string projectId, string baseUrl, string org)
    {
        var scope = props.ScopeLevel ?? AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription;
        object data = scope switch
        {
            AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup => new
            {
                environment = "AzureCloud",
                scopeLevel = "ManagementGroup",
                creationMode = "Manual",
                ManagementGroupName = props.ManagementGroupName,
                ManagementGroupId = props.ManagementGroupId
            },
            _ => new
            {
                environment = "AzureCloud",
                scopeLevel = "Subscription",
                creationMode = "Manual",
                subscriptionId = props.SubscriptionId,
                subscriptionName = props.SubscriptionName
            }
        };

        return new
        {
            authorization = new
            {
                parameters = new
                {
                    serviceprincipalid = props.ClientId,
                    tenantid = props.TenantId
                },
                scheme = "WorkloadIdentityFederation"
            },
            createdBy = new { },
            data,
            isShared = false,
            isOutdated = false,
            isReady = false,
            name = props.Name,
            owner = "library",
            type = "AzureRM",
            url = "https://management.azure.com/",
            description = props.Description ?? string.Empty,
            serviceEndpointProjectReferences = new[]
            {
                new
                {
                    description = props.Description ?? string.Empty,
                    name = props.Name,
                    projectReference = new { id = projectId, name = props.Project }
                }
            }
        };
    }

    private static async Task<string> ResolveProjectIdAsync(HttpClient client, string org, string baseUrl, string projectName, CancellationToken ct)
    {
        var resp = await client.GetAsync($"{baseUrl}/{org}/_apis/projects/{Uri.EscapeDataString(projectName)}?api-version=7.1-preview.4", ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Project '{projectName}' not found or inaccessible: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return json.GetProperty("id").GetString()!;
    }

    private static Task<HttpResponseMessage> PatchPermissionsAsync(HttpClient client, string uri, HttpContent content, CancellationToken ct)
    {
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), uri) { Content = content };
        return client.SendAsync(req, ct);
    }

    private async Task SetFederatedOutputsAsync(Configuration configuration, AzureDevOpsServiceConnection props, CancellationToken ct)
    {
        try
        {
            var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
            using var client = CreateClient(configuration);
            // connection data endpoint to obtain org GUID (instanceId)
            var resp = await client.GetAsync($"{baseUrl}/{org}/_apis/connectiondata?api-version=7.1-preview.1", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return; // silently ignore; outputs remain unset
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            string? orgGuid = null;
            if (json.TryGetProperty("instanceId", out var inst) && inst.ValueKind == JsonValueKind.String)
            {
                orgGuid = inst.GetString();
            }
            if (!string.IsNullOrWhiteSpace(orgGuid))
            {
                props.Issuer = $"https://vstoken.dev.azure.com/{orgGuid}";
            }
            // Always compute subject identifier (even if issuer failed) using org slug (not GUID)
            props.SubjectIdentifier = $"sc://{org}/{props.Project}/{props.Name}";
        }
        catch
        {
            // ignore – optional outputs
        }
    }
}
