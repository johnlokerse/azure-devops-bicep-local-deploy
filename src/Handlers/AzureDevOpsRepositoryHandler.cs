using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

namespace DevOpsExtension.Handlers;

public class AzureDevOpsRepositoryHandler : TypedResourceHandler<AzureDevOpsRepository, AzureDevOpsRepositoryIdentifiers, Configuration>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetRepositoryAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            PopulateOutputs(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var existing = await GetRepositoryAsync(request.Config, props, cancellationToken);
        if (existing is null)
        {
            await CreateRepositoryAsync(request.Config, props, cancellationToken);
            existing = await GetRepositoryAsync(request.Config, props, cancellationToken) ?? throw new InvalidOperationException("Repository creation did not return repository.");
        }

        PopulateOutputs(props, existing);
        return GetResponse(request);
    }

    protected override AzureDevOpsRepositoryIdentifiers GetIdentifiers(AzureDevOpsRepository properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
        Name = properties.Name,
    };

    private void PopulateOutputs(AzureDevOpsRepository props, dynamic repo)
    {
        props.RepositoryId = repo.id;
        props.WebUrl = repo.webUrl;
        props.RemoteUrl = repo.remoteUrl;
        props.SshUrl = repo.sshUrl;
    }

    private async Task<dynamic?> GetRepositoryAsync(Configuration configuration, AzureDevOpsRepository props, CancellationToken ct)
    {
        try
        {
            var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
            using var client = CreateClient(configuration);
            var resp = await client.GetAsync($"{baseUrl}/{org}/{Uri.EscapeDataString(props.Project)}/_apis/git/repositories/{Uri.EscapeDataString(props.Name)}?api-version=7.1-preview.1", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return new
            {
                id = json.GetProperty("id").GetString()!,
                name = json.GetProperty("name").GetString()!,
                webUrl = json.TryGetProperty("webUrl", out var w) ? w.GetString() : null,
                remoteUrl = json.TryGetProperty("remoteUrl", out var r) ? r.GetString() : null,
                sshUrl = json.TryGetProperty("sshUrl", out var s) ? s.GetString() : null,
                defaultBranch = json.TryGetProperty("defaultBranch", out var d) ? d.GetString() : null,
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task CreateRepositoryAsync(Configuration configuration, AzureDevOpsRepository props, CancellationToken ct)
    {
        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using var client = CreateClient(configuration);

        // Resolve project id
        var projectResp = await client.GetAsync($"{baseUrl}/{org}/_apis/projects/{Uri.EscapeDataString(props.Project)}?api-version=7.1-preview.4", ct);
        if (!projectResp.IsSuccessStatusCode)
        {
            var errText = await projectResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Project '{props.Project}' not found or inaccessible: {(int)projectResp.StatusCode} {projectResp.ReasonPhrase} {errText}");
        }
        var projectJson = await projectResp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        var projectId = projectJson.GetProperty("id").GetString();
        if (string.IsNullOrWhiteSpace(projectId))
        {
            throw new InvalidOperationException("Failed to resolve project id.");
        }

        var body = new
        {
            name = props.Name,
            project = new { id = projectId }
        };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync($"{baseUrl}/{org}/_apis/git/repositories?api-version=7.1-preview.1", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to create repository: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
    }

    private static (string org, string baseUrl) GetOrgAndBaseUrl(string organization)
    {
        if (organization.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(organization.TrimEnd('/'));
            var org = uri.Segments.Last().Trim('/');
            return (org, $"{uri.Scheme}://{uri.Host}");
        }
        return (organization, "https://dev.azure.com");
    }

    private static HttpClient CreateClient(Configuration configuration)
    {
        var pat = configuration.AccessToken ?? Environment.GetEnvironmentVariable("AZDO_PAT");
        if (string.IsNullOrWhiteSpace(pat))
        {
            throw new InvalidOperationException("A PAT must be supplied via property 'pat' or AZDO_PAT environment variable.");
        }
        var client = new HttpClient();
        var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }
}
