using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DevOpsExtension.ArtifactFeed;

public class AzureDevOpsArtifactFeedHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsArtifactFeed, AzureDevOpsArtifactFeedIdentifiers>
{
    private const string FeedsApiVersion = "7.2-preview.1";

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        // Use GetFeedByIdentifiersAsync to get full feed details including description, flags, etc.
        var identifiers = GetIdentifiers(request.Properties);
        var existing = await GetFeedByIdentifiersAsync(request.Config, identifiers, cancellationToken);

        if (existing is not null)
        {
            request.Properties.FeedId = existing.id;
            request.Properties.Url = existing.url;
            if (existing.project is not null)
            {
                request.Properties.ProjectReference = new AzureDevOpsArtifactFeedProjectReference
                {
                    Id = existing.project.id,
                    Name = existing.project.name
                };
            }
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;

        var identifiers = GetIdentifiers(props);
        var existing = await GetFeedByIdentifiersAsync(request.Config, identifiers, cancellationToken);

        if (existing is null)
        {
            Console.WriteLine($"Creating feed '{props.Name}'.");
            await CreateFeedAsync(request.Config, props, cancellationToken);
            existing = await GetFeedByIdentifiersAsync(request.Config, identifiers, cancellationToken) ?? throw new InvalidOperationException("Feed creation did not return feed.");
        }

        // Populate all output properties
        props.FeedId = existing.id;
        props.Url = existing.url;
        if (existing.project is not null)
        {
            props.ProjectReference = new AzureDevOpsArtifactFeedProjectReference
            {
                Id = existing.project.id,
                Name = existing.project.name
            };
        }

        return GetResponse(request);
    }

    protected override AzureDevOpsArtifactFeedIdentifiers GetIdentifiers(AzureDevOpsArtifactFeed properties) => new()
    {
        Organization = properties.Organization,
        Name = properties.Name,
        Project = properties.Project,
    };

    private async Task<dynamic?> GetFeedByIdentifiersAsync(Configuration configuration, AzureDevOpsArtifactFeedIdentifiers identifiers, CancellationToken ct)
    {
        try
        {
            var (org, baseUrl) = GetOrgAndFeedsBaseUrl(identifiers.Organization);
            using var client = CreateClient(configuration);

            string apiUrl;
            if (!string.IsNullOrWhiteSpace(identifiers.Project))
            {
                // Project-scoped feed
                apiUrl = $"{baseUrl}/{org}/{Uri.EscapeDataString(identifiers.Project)}/_apis/packaging/feeds/{Uri.EscapeDataString(identifiers.Name)}?api-version={FeedsApiVersion}";
            }
            else
            {
                // Organization-scoped feed
                apiUrl = $"{baseUrl}/{org}/_apis/packaging/feeds/{Uri.EscapeDataString(identifiers.Name)}?api-version={FeedsApiVersion}";
            }

            var resp = await client.GetAsync(apiUrl, ct);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return new
            {
                id = json.GetProperty("id").GetString()!,
                name = json.GetProperty("name").GetString()!,
                url = json.TryGetProperty("url", out var url) ? url.GetString() : null,
                description = json.TryGetProperty("description", out var description) ? description.GetString() : null,
                hideDeletedPackageVersions = json.TryGetProperty("hideDeletedPackageVersions", out var hideDeletedPackageVersions) ? hideDeletedPackageVersions.GetBoolean() : true,
                upstreamEnabled = json.TryGetProperty("upstreamEnabled", out var upstreamEnabled) ? upstreamEnabled.GetBoolean() : true,
                project = json.TryGetProperty("project", out var project) && project.ValueKind != JsonValueKind.Null ? new
                {
                    id = project.GetProperty("id").GetString()!,
                    name = project.TryGetProperty("name", out var pName) ? pName.GetString() : null,
                } : null,
            };
        }
        catch
        {
            return null;
        }
    }

    private static (string org, string baseUrl) GetOrgAndFeedsBaseUrl(string organization)
    {
        if (organization.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(organization.TrimEnd('/'));
            var org = uri.Segments.Last().Trim('/');
            return (org, $"{uri.Scheme}://feeds.{uri.Host}");
        }
        return (organization, "https://feeds.dev.azure.com");
    }

    private async Task CreateFeedAsync(Configuration configuration, AzureDevOpsArtifactFeed props, CancellationToken ct)
    {
        var (org, baseUrl) = GetOrgAndFeedsBaseUrl(props.Organization);
        using var client = CreateClient(configuration);

        // Build the request body
        var upstreamSources = props.UpstreamSources?.Select(us => new
        {
            id = us.Id,
            name = us.Name,
            location = us.Location,
            protocol = us.Protocol
        }).Cast<object>().ToArray() ?? Array.Empty<object>();

        var body = new
        {
            name = props.Name,
            description = props.Description,
            hideDeletedPackageVersions = props.HideDeletedPackageVersions,
            upstreamEnabled = props.UpstreamEnabled,
            upstreamSources,
            project = await GetProjectReferenceAsync(client, props.Organization, props.Project, ct)
        };

        string apiUrl;
        if (!string.IsNullOrWhiteSpace(props.Project))
        {
            // Project-scoped feed
            apiUrl = $"{baseUrl}/{org}/{Uri.EscapeDataString(props.Project)}/_apis/packaging/feeds?api-version={FeedsApiVersion}";
        }
        else
        {
            // Organization-scoped feed
            apiUrl = $"{baseUrl}/{org}/_apis/packaging/feeds?api-version={FeedsApiVersion}";
        }

        Console.WriteLine($"Creating feed '{props.Name}' in organization '{org}' at {apiUrl}");

        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync(apiUrl, content, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var errorContent = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to create feed '{props.Name}'. Status: {resp.StatusCode}, Response: {errorContent}");
        }
    }

    private async Task<object?> GetProjectReferenceAsync(HttpClient client, string organization, string? projectName, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(projectName))
        {
            return null;
        }

        try
        {
            var (org, baseUrl) = GetOrgAndBaseUrl(organization);
            var resp = await client.GetAsync($"{baseUrl}/{org}/_apis/projects/{Uri.EscapeDataString(projectName)}?api-version=7.1-preview.4", ct);
            if (!resp.IsSuccessStatusCode)
            {
                throw new InvalidOperationException($"Project '{projectName}' not found in organization '{org}'.");
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return new
            {
                id = json.GetProperty("id").GetString()!
            };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to retrieve project '{projectName}': {ex.Message}", ex);
        }
    }
}
