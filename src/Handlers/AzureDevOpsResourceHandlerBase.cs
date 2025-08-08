using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

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
