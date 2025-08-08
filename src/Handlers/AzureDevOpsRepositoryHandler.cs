using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Bicep.Local.Extension.Host.Handlers;
using DevOpsExtension.Models;
using System.Net;

namespace DevOpsExtension.Handlers;

public class AzureDevOpsRepositoryHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsRepository, AzureDevOpsRepositoryIdentifiers>
{
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

        // Resolve project id (also validates project exists)
        var projectResp = await client.GetAsync($"{baseUrl}/{org}/_apis/projects/{Uri.EscapeDataString(props.Project)}?api-version=7.1-preview.4", ct);
        if (!projectResp.IsSuccessStatusCode)
        {
            var errText = await projectResp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Project '{props.Project}' not found or inaccessible: {(int)projectResp.StatusCode} {projectResp.ReasonPhrase} {errText}");
        }
        // Create repo scoped to the project path; retry to bridge project provisioning lag for git dataspace
        var createUrl = $"{baseUrl}/{org}/{Uri.EscapeDataString(props.Project)}/_apis/git/repositories?api-version=7.1-preview.1";
        var body = new { name = props.Name };
        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");

        const int maxAttempts = 10;
        for (int attempt = 1; attempt <= maxAttempts; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            var resp = await client.PostAsync(createUrl, content, ct);
            if (resp.IsSuccessStatusCode)
            {
                return;
            }

            var err = await resp.Content.ReadAsStringAsync(ct);
            if (attempt < maxAttempts && ShouldRetryForProjectProvisioning(resp.StatusCode, err))
            {
                var delay = TimeSpan.FromSeconds(Math.Min(30, 1 << Math.Min(5, attempt - 1))); // 1,2,4,8,16,30
                await Task.Delay(delay, ct);
                continue;
            }

            throw new InvalidOperationException($"Failed to create repository: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }
    }

    private static bool ShouldRetryForProjectProvisioning(HttpStatusCode status, string error)
    {
        if (status == HttpStatusCode.NotFound)
        {
            return true;
        }
        if (status == HttpStatusCode.InternalServerError &&
            (error.Contains("DataspaceNotFoundException", StringComparison.OrdinalIgnoreCase) ||
             error.Contains("Could not find dataspace with category Git", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        return false;
    }
}
