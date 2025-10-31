using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Azure.Core;
using Azure.Identity;
using Bicep.Local.Extension.Host.Handlers;

namespace DevOpsExtension;

public abstract class AzureDevOpsResourceHandlerBase<TProps, TIdentifiers>
    : TypedResourceHandler<TProps, TIdentifiers, Configuration>
    where TProps : class
    where TIdentifiers : class
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    protected static (string org, string baseUrl) GetOrgAndBaseUrl(string organization)
    {
        if (organization.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(organization.TrimEnd('/'));
            var org = uri.Segments.Last().Trim('/');
            return (org, $"{uri.Scheme}://{uri.Host}");
        }
        return (organization, "https://dev.azure.com");
    }

    protected static HttpClient CreateClient(Configuration configuration)
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var pat = configuration.AccessToken ?? Environment.GetEnvironmentVariable("AZDO_PAT");
        if (!string.IsNullOrWhiteSpace(pat))
        {
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            return client;
        }

        //    Resource ID for Azure DevOps first-party app: 499b84ac-1321-427f-aa17-267ca6975798 & Use ".default" scope syntax for AAD v2 endpoint.
        try
        {
            var credential = DefaultAzureDevOpsCredential.Instance;
            var token = credential.GetToken(new TokenRequestContext(new[] { "499b84ac-1321-427f-aa17-267ca6975798/.default" })); // GUID is the well-known resource id of the Azure DevOps rest api
            if (string.IsNullOrWhiteSpace(token.Token))
            {
                throw new InvalidOperationException("Empty Azure Entra access token returned for Azure DevOps scope.");
            }
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
            return client;
        }
        catch (Exception ex)
        {
            client.Dispose();
            throw new InvalidOperationException("Failed to acquire Azure DevOps credentials. Provide a PAT (property 'pat' or AZDO_PAT) or ensure a federated / managed identity is configured.", ex);
        }
    }

    // Internal helper to cache DefaultAzureCredential instance (which can be costly to build repeatedly)
    private static class DefaultAzureDevOpsCredential
    {
        internal static readonly DefaultAzureCredential Instance = new(new DefaultAzureCredentialOptions());
    }

    protected static string? TryGetOperationIdFromResponse(HttpResponseMessage response, JsonElement? parsedBody)
    {
        try
        {
            if (parsedBody.HasValue && parsedBody.Value.ValueKind == JsonValueKind.Object &&
                parsedBody.Value.TryGetProperty("id", out var idProp))
            {
                return idProp.GetString();
            }
        }
        catch { }

        var loc = response.Headers.Location;
        if (loc != null)
        {
            var seg = loc.Segments.LastOrDefault();
            if (!string.IsNullOrWhiteSpace(seg))
            {
                return seg.Trim('/');
            }
        }
        return null;
    }

    protected static Task<HttpResponseMessage> PatchAsync(HttpClient client, string requestUri, HttpContent content, CancellationToken ct)
    {
        var req = new HttpRequestMessage(new HttpMethod("PATCH"), requestUri) { Content = content };
        return client.SendAsync(req, ct);
    }

    /// <summary>
    /// Returns the correct base host for Azure DevOps Graph APIs.
    /// For dev.azure.com it uses vssps.dev.azure.com; otherwise returns the provided baseUrl.
    /// </summary>
    protected static string GetGraphBaseUrl(string baseUrl)
    {
        var host = new Uri(baseUrl).Host;
        return host.Contains("dev.azure.com", StringComparison.OrdinalIgnoreCase)
            ? "https://vssps.dev.azure.com"
            : baseUrl;
    }

    /// <summary>
    /// Best-effort lookup of a project's GUID by name. Returns null if not found or on non-success.
    /// </summary>
    protected static async Task<string?> TryResolveProjectIdAsync(HttpClient client, string org, string baseUrl, string projectName, CancellationToken ct)
    {
        var resp = await client.GetAsync($"{baseUrl}/{org}/_apis/projects/{Uri.EscapeDataString(projectName)}?api-version=7.1-preview.4", ct);
        if (!resp.IsSuccessStatusCode)
        {
            return null;
        }
        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (json.TryGetProperty("id", out var idProp) && idProp.ValueKind == JsonValueKind.String)
        {
            return idProp.GetString();
        }
        return null;
    }

    /// <summary>
    /// Strict project id lookup that throws a concise error when the project is not found or inaccessible.
    /// </summary>
    protected static async Task<string> ResolveProjectIdOrThrowAsync(HttpClient client, string org, string baseUrl, string projectName, CancellationToken ct)
    {
        var id = await TryResolveProjectIdAsync(client, org, baseUrl, projectName, ct);
        if (!string.IsNullOrWhiteSpace(id))
        {
            return id!;
        }
        throw new InvalidOperationException($"Project '{projectName}' not found or inaccessible in organization '{org}'.");
    }
}
