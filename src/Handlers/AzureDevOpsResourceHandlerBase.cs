using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;
using Azure.Identity;
using Azure.Core;

namespace DevOpsExtension.Handlers;

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
            Console.WriteLine("Warning: A personal access token (PAT) is less secure than Azure Entra access tokens. Consider using Azure Entra tokens instead.");
            var authToken = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authToken);
            return client;
        }

        //    Resource ID for Azure DevOps first-party app: 499b84ac-1321-427f-aa17-267ca6975798
        //    Use ".default" scope syntax for AAD v2 endpoint.
        try
        {
            // Static cache to avoid re-instantiating credentials repeatedly.
            var credential = DefaultAzureDevOpsCredential.Instance;
            var token = credential.GetToken(new TokenRequestContext(new[] { "499b84ac-1321-427f-aa17-267ca6975798/.default" })); // GUID is the well-known resource id of the Azure DevOsp rest api
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
}
