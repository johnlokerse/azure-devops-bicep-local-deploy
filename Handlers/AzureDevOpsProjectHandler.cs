using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

namespace DevOpsExtension.Handlers;

public class AzureDevOpsProjectHandler : TypedResourceHandler<AzureDevOpsProject, AzureDevOpsProjectIdentifiers, Configuration>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetProjectAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            request.Properties.ProjectId = existing.id;
            request.Properties.State = existing.state;
            request.Properties.Url = existing.url;
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var existing = await GetProjectAsync(request.Config, props, cancellationToken);
        if (existing is null)
        {
            await CreateProjectAsync(request.Config, props, cancellationToken);
            existing = await GetProjectAsync(request.Config, props, cancellationToken) ?? throw new InvalidOperationException("Project creation did not return project.");
        }
        else
        {
            if (!string.IsNullOrWhiteSpace(props.Description) && props.Description != existing.description)
            {
                await UpdateProjectDescriptionAsync(request.Config, existing.id, props, cancellationToken);
                existing = await GetProjectAsync(request.Config, props, cancellationToken) ?? existing;
            }
        }

        props.ProjectId = existing.id;
        props.State = existing.state;
        props.Url = existing.url;
        return GetResponse(request);
    }

    protected override AzureDevOpsProjectIdentifiers GetIdentifiers(AzureDevOpsProject properties) => new()
    {
        Organization = properties.Organization,
        Name = properties.Name,
    };

    private async Task<dynamic?> GetProjectAsync(Configuration configuration, AzureDevOpsProject props, CancellationToken ct)
    {
        try
        {
            var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
            using var client = CreateClient(configuration);
            var resp = await client.GetAsync($"{baseUrl}/{org}/_apis/projects/{Uri.EscapeDataString(props.Name)}?api-version=7.1-preview.4", ct);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }
            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
            return new
            {
                id = json.GetProperty("id").GetString()!,
                name = json.GetProperty("name").GetString()!,
                state = json.TryGetProperty("state", out var s) ? s.GetString() : null,
                description = json.TryGetProperty("description", out var d) ? d.GetString() : null,
                url = json.TryGetProperty("url", out var u) ? u.GetString() : null,
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task CreateProjectAsync(Configuration configuration, AzureDevOpsProject props, CancellationToken ct)
    {
        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using var client = CreateClient(configuration);
        var processId = await ResolveProcessTemplateIdAsync(client, org, baseUrl, props.ProcessName ?? "Agile", ct);

        var body = new
        {
            name = props.Name,
            description = props.Description,
            visibility = (props.Visibility ?? ProjectVisibility.Private).ToString().ToLowerInvariant(),
            capabilities = new
            {
                versioncontrol = new { sourceControlType = props.SourceControlType ?? "Git" },
                processTemplate = new { templateTypeId = processId }
            }
        };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var resp = await client.PostAsync($"{baseUrl}/{org}/_apis/projects?api-version=7.1-preview.4", content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to create project: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
    }

    private async Task UpdateProjectDescriptionAsync(Configuration configuration, string projectId, AzureDevOpsProject props, CancellationToken ct)
    {
        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using var client = CreateClient(configuration);
        var body = new { description = props.Description };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var resp = await client.PatchAsync($"{baseUrl}/{org}/_apis/projects/{projectId}?api-version=7.1-preview.4", content, ct);
    }

    private async Task<string> ResolveProcessTemplateIdAsync(HttpClient client, string org, string baseUrl, string processName, CancellationToken ct)
    {
        try
        {
            var resp = await client.GetAsync($"{baseUrl}/{org}/_apis/process/processes?api-version=7.1-preview.2", ct);
            if (resp.IsSuccessStatusCode)
            {
                var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
                if (json.TryGetProperty("value", out var arr) && arr.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in arr.EnumerateArray())
                    {
                        var name = item.GetProperty("name").GetString();
                        if (string.Equals(name, processName, StringComparison.OrdinalIgnoreCase))
                        {
                            return item.GetProperty("id").GetString()!;
                        }
                    }
                }
            }
        }
        catch
        {
        }
        return "adcc42ab-9882-485e-a3ed-7678f01f66bc";
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

internal static class HttpClientExtensions
{
    public static Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content, CancellationToken ct)
    {
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
        return client.SendAsync(req, ct);
    }
}
