using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;

namespace DevOpsExtension.Handlers;

public class AzureDevOpsExtensionHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsExtension, AzureDevOpsExtensionIdentifiers>
{
    private const string ExtensionApiVersion = "7.1-preview.1";

    protected override async Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        var existing = await GetExtensionAsync(request.Config, request.Properties, cancellationToken);
        if (existing is not null)
        {
            PopulateOutputs(request.Properties, existing);
        }
        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        var existing = await GetExtensionAsync(request.Config, props, cancellationToken);

        // If extension doesn't exist or version differs, install/update it
        if (existing is null || !string.Equals(existing.version, props.Version, StringComparison.OrdinalIgnoreCase))
        {
            await InstallOrUpdateExtensionAsync(request.Config, props, cancellationToken);
            existing = await GetExtensionAsync(request.Config, props, cancellationToken) ?? throw new InvalidOperationException("Extension installation did not return extension details.");
        }

        PopulateOutputs(props, existing);
        return GetResponse(request);
    }

    protected override AzureDevOpsExtensionIdentifiers GetIdentifiers(AzureDevOpsExtension properties) => new()
    {
        Organization = properties.Organization,
        PublisherName = properties.PublisherName,
        ExtensionName = properties.ExtensionName,
    };

    private static void PopulateOutputs(AzureDevOpsExtension props, dynamic extension)
    {
        props.ExtensionId = extension.extensionId;
        props.PublisherId = extension.publisherId;
    }

    private static (string org, string baseUrl) GetOrgAndExtMgmtBaseUrl(string organization)
    {
        if (organization.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var uri = new Uri(organization.TrimEnd('/'));
            var org = uri.Segments.Last().Trim('/');
            return (org, $"{uri.Scheme}://extmgmt.{uri.Host}");
        }
        return (organization, "https://extmgmt.dev.azure.com");
    }

    private async Task<dynamic?> GetExtensionAsync(Configuration configuration, AzureDevOpsExtension props, CancellationToken cancellationToken)
    {
        try
        {
            var (org, baseUrl) = GetOrgAndExtMgmtBaseUrl(props.Organization);
            using var client = CreateClient(configuration);

            var apiUrl = $"{baseUrl}/{org}/_apis/extensionmanagement/installedextensionsbyname/{Uri.EscapeDataString(props.PublisherName)}/{Uri.EscapeDataString(props.ExtensionName)}?api-version={ExtensionApiVersion}";

            var resp = await client.GetAsync(apiUrl, cancellationToken);
            if (!resp.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            return new
            {
                extensionId = json.GetProperty("extensionId").GetString()!,
                publisherId = json.GetProperty("publisherId").GetString()!,
                version = json.TryGetProperty("version", out var version) ? version.GetString() : null,
                extensionName = json.TryGetProperty("extensionName", out var extensionName) ? extensionName.GetString() : null,
                publisherName = json.TryGetProperty("publisherName", out var publisherName) ? publisherName.GetString() : null,
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task InstallOrUpdateExtensionAsync(Configuration configuration, AzureDevOpsExtension props, CancellationToken cancellationToken)
    {
        var (org, baseUrl) = GetOrgAndExtMgmtBaseUrl(props.Organization);
        using var client = CreateClient(configuration);

        // Install/update extension using POST with version in the path
        var apiUrl = $"{baseUrl}/{org}/_apis/extensionmanagement/installedextensionsbyname/{Uri.EscapeDataString(props.PublisherName)}/{Uri.EscapeDataString(props.ExtensionName)}/{Uri.EscapeDataString(props.Version)}?api-version={ExtensionApiVersion}";

        // POST request with empty body
        var content = new StringContent(string.Empty, Encoding.UTF8, "application/json");
        var response = await client.PostAsync(apiUrl, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to install extension '{props.PublisherName}.{props.ExtensionName}' version '{props.Version}'. Status: {response.StatusCode}, Response: {errorContent}");
        }
    }
}
