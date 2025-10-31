using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DevOpsExtension.ServiceConnection;

public class AzureDevOpsServiceConnectionHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsServiceConnection, AzureDevOpsServiceConnectionIdentifiers>
{
    private sealed record ServiceConnectionDetails(
        string? Id,
        string? Name,
        string? Type,
        string? Url,
        string? Scheme,
        string? WorkloadIdentityFederationIssuer,
        string? WorkloadIdentityFederationSubject);

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        ServiceConnectionDetails? existing = await GetServiceConnectionAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            PopulateOutputs(request.Properties, existing);
        }

        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        AzureDevOpsServiceConnection props = request.Properties;
        ServiceConnectionDetails? existing = await GetServiceConnectionAsync(request.Config, props, cancellationToken);
        if (existing is null)
        {
            await CreateServiceConnectionAsync(request.Config, props, cancellationToken);
            existing = await GetServiceConnectionAsync(request.Config, props, cancellationToken) ?? throw new InvalidOperationException("Service connection creation did not return service connection.");
        }

        PopulateOutputs(props, existing);
        return GetResponse(request);
    }

    protected override AzureDevOpsServiceConnectionIdentifiers GetIdentifiers(AzureDevOpsServiceConnection properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
        Name = properties.Name,
    };

    private static void PopulateOutputs(AzureDevOpsServiceConnection props, ServiceConnectionDetails serviceConnection)
    {
        props.ServiceConnectionId = serviceConnection.Id;
        props.Url = serviceConnection.Url;
        props.AuthorizationScheme = serviceConnection.Scheme;
        props.Issuer = serviceConnection.WorkloadIdentityFederationIssuer;
        props.SubjectIdentifier = serviceConnection.WorkloadIdentityFederationSubject;
    }

    private async Task<ServiceConnectionDetails?> GetServiceConnectionAsync(Configuration configuration, AzureDevOpsServiceConnection props, CancellationToken cancellationToken)
    {
        (string organization, string baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using HttpClient client = CreateClient(configuration);
        try
        {
            string requestUri = $"{baseUrl}/{organization}/{Uri.EscapeDataString(props.Project)}/_apis/serviceendpoint/endpoints?endpointNames={Uri.EscapeDataString(props.Name)}&api-version=7.1-preview.4";
            HttpResponseMessage response = await client.GetAsync(requestUri, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null; // treat as not existing / inaccessible
            }
            JsonElement json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            if (!json.TryGetProperty("value", out JsonElement array) || array.ValueKind != JsonValueKind.Array)
            {
                return null;
            }
            foreach (JsonElement item in array.EnumerateArray())
            {
                if (string.Equals(item.GetProperty("name").GetString(), props.Name, StringComparison.OrdinalIgnoreCase))
                {
                    string? scheme = null;
                    string? issuer = null;
                    string? subject = null;

                    if (item.TryGetProperty("authorization", out JsonElement authorizationElement))
                    {
                        if (authorizationElement.TryGetProperty("scheme", out JsonElement schemeElement))
                        {
                            scheme = schemeElement.GetString();
                        }
                        if (authorizationElement.TryGetProperty("parameters", out JsonElement parametersElement))
                        {
                            if (parametersElement.TryGetProperty("workloadIdentityFederationIssuer", out JsonElement issuerElement))
                            {
                                issuer = issuerElement.GetString();
                            }
                            if (parametersElement.TryGetProperty("workloadIdentityFederationSubject", out JsonElement subjectElement))
                            {
                                subject = subjectElement.GetString();
                            }
                        }
                    }

                    string? identifier = item.GetProperty("id").GetString();
                    string? name = item.GetProperty("name").GetString();
                    string? type = item.TryGetProperty("type", out JsonElement typeElement) ? typeElement.GetString() : null;
                    string? url = item.TryGetProperty("url", out JsonElement urlElement) ? urlElement.GetString() : null;

                    return new ServiceConnectionDetails(
                        identifier,
                        name,
                        type,
                        url,
                        scheme,
                        issuer,
                        subject);
                }
            }
        }
        catch (HttpRequestException httpRequestException)
        {
            throw new InvalidOperationException($"Failed to retrieve service connections for project '{props.Project}' in organization '{props.Organization}'.", httpRequestException);
        }
        catch (TaskCanceledException taskCanceledException)
        {
            throw new OperationCanceledException($"Retrieving service connection '{props.Name}' was canceled or timed out (org: '{props.Organization}', project: '{props.Project}').", taskCanceledException, cancellationToken);
        }
        catch (JsonException jsonException)
        {
            throw new InvalidOperationException("Received malformed JSON while parsing service connection list response.", jsonException);
        }
        return null;
    }

    private async Task CreateServiceConnectionAsync(Configuration configuration, AzureDevOpsServiceConnection props, CancellationToken cancellationToken)
    {
        ValidateProps(props);
        (string organization, string baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using HttpClient client = CreateClient(configuration);
        string projectId = await ResolveProjectIdOrThrowAsync(client, organization, baseUrl, props.Project, cancellationToken);
        object body = BuildCreationBody(props, projectId, baseUrl, organization);
        StringContent content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        HttpResponseMessage response = await client.PostAsync($"{baseUrl}/{organization}/{Uri.EscapeDataString(props.Project)}/_apis/serviceendpoint/endpoints?api-version=7.1-preview.4", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            string error = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to create service connection: {(int)response.StatusCode} {response.ReasonPhrase} {error}");
        }

        // Delay to allow backend consistency before permission patch
        await Task.Delay(TimeSpan.FromSeconds(3), cancellationToken);

        if (props.GrantAllPipelines)
        {
            try
            {
                ServiceConnectionDetails? created = await GetServiceConnectionAsync(configuration, props, cancellationToken);
                if (created == null)
                {
                    return;
                }
                object permissionBody = new
                {
                    allPipelines = new { authorized = true, authorizedBy = (string?)null, authorizedOn = (string?)null },
                    pipelines = Array.Empty<object>(),
                    resource = new { type = "endpoint", id = created.Id }
                };
                StringContent permissionContent = new StringContent(JsonSerializer.Serialize(permissionBody, JsonOptions), Encoding.UTF8, "application/json");
                using HttpResponseMessage _ = await PatchPermissionsAsync(client, $"{baseUrl}/{organization}/{Uri.EscapeDataString(props.Project)}/_apis/pipelines/pipelinePermissions/endpoint/{created.Id}?api-version=7.1-preview.1", permissionContent, cancellationToken);
            }
            catch (HttpRequestException httpRequestException)
            {
                throw new InvalidOperationException($"Failed to set pipeline permissions for service connection '{props.Name}'.", httpRequestException);
            }
            catch (TaskCanceledException taskCanceledException)
            {
                throw new OperationCanceledException($"Setting pipeline permissions for service connection '{props.Name}' was canceled or timed out.", taskCanceledException, cancellationToken);
            }
            catch (JsonException jsonException)
            {
                throw new InvalidOperationException($"Malformed JSON encountered while setting pipeline permissions for service connection '{props.Name}'.", jsonException);
            }
        }
    }

    private static void ValidateProps(AzureDevOpsServiceConnection config)
    {
        // Mutually exclusive scope sets: either Subscription (subscriptionId + subscriptionName) OR Management Group (managementGroupName/Id)
        bool hasSubscriptionSet = !string.IsNullOrWhiteSpace(config.SubscriptionId) && !string.IsNullOrWhiteSpace(config.SubscriptionName);
        bool hasManagementGroupSet = !string.IsNullOrWhiteSpace(config.ManagementGroupName) || !string.IsNullOrWhiteSpace(config.ManagementGroupId);
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

        AzureDevOpsServiceConnection.ServiceConnectionScopeLevel? scopeLevel = config.ScopeLevel;
        if (scopeLevel == AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription)
        {
            if (string.IsNullOrWhiteSpace(config.SubscriptionId) || string.IsNullOrWhiteSpace(config.SubscriptionName))
            {
                throw new InvalidOperationException("SubscriptionId and SubscriptionName are required for Subscription scope.");
            }
        }
        else if (scopeLevel == AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.ManagementGroup)
        {
            if (string.IsNullOrWhiteSpace(config.ManagementGroupName))
            {
                throw new InvalidOperationException("ManagementGroupName is required for ManagementGroup scope.");
            }
        }
    }

    private static object BuildCreationBody(AzureDevOpsServiceConnection props, string projectId, string baseUrl, string org)
    {
        AzureDevOpsServiceConnection.ServiceConnectionScopeLevel scopeLevel = props.ScopeLevel ?? AzureDevOpsServiceConnection.ServiceConnectionScopeLevel.Subscription;
        object data = scopeLevel switch
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

    private static Task<HttpResponseMessage> PatchPermissionsAsync(HttpClient client, string uri, HttpContent content, CancellationToken cancellationToken)
    {
        HttpRequestMessage request = new HttpRequestMessage(new HttpMethod("PATCH"), uri) { Content = content };
        return client.SendAsync(request, cancellationToken);
    }
}
