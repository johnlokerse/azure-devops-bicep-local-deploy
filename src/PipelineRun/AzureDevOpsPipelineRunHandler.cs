using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DevOpsExtension.PipelineRun;

public class AzureDevOpsPipelineRunHandler : AzureDevOpsResourceHandlerBase<AzureDevOpsPipelineRun, AzureDevOpsPipelineRunIdentifiers>
{
    protected override Task<ResourceResponse> Preview(ResourceRequest request, CancellationToken cancellationToken)
    {
        // For preview, we don't trigger the pipeline, just validate inputs
        ValidateInputs(request.Properties);
        return Task.FromResult(GetResponse(request));
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(ResourceRequest request, CancellationToken cancellationToken)
    {
        var props = request.Properties;
        ValidateInputs(props);

        var (org, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        using var client = CreateClient(request.Config);

        // Resolve pipeline ID if a name was provided
        var pipelineId = await ResolvePipelineIdAsync(client, org, baseUrl, props.Project, props.PipelineId, cancellationToken);

        // Trigger the pipeline run
        var runId = await TriggerPipelineAsync(client, org, baseUrl, props.Project, pipelineId, props, cancellationToken);

        // Get the pipeline run details
        var runDetails = await GetPipelineRunAsync(client, org, baseUrl, props.Project, pipelineId, runId, cancellationToken);

        // Populate output properties
        props.RunId = runDetails.id;
        props.State = runDetails.state;
        props.Result = runDetails.result;
        props.Url = runDetails.url;

        return GetResponse(request);
    }

    protected override AzureDevOpsPipelineRunIdentifiers GetIdentifiers(AzureDevOpsPipelineRun properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
        PipelineId = properties.PipelineId
    };

    private static void ValidateInputs(AzureDevOpsPipelineRun props)
    {
        if (string.IsNullOrWhiteSpace(props.Branch) && string.IsNullOrWhiteSpace(props.Tag))
        {
            throw new InvalidOperationException("Either 'branch' or 'tag' must be specified.");
        }

        if (!string.IsNullOrWhiteSpace(props.Branch) && !string.IsNullOrWhiteSpace(props.Tag))
        {
            throw new InvalidOperationException("Cannot specify both 'branch' and 'tag'. Choose one.");
        }
    }

    private static async Task<string> ResolvePipelineIdAsync(
        HttpClient client,
        string org,
        string baseUrl,
        string project,
        string pipelineIdOrName,
        CancellationToken ct)
    {
        // If it's already a numeric ID, return it
        if (int.TryParse(pipelineIdOrName, out _))
        {
            return pipelineIdOrName;
        }

        // Otherwise, lookup by name
        var uri = $"{baseUrl}/{org}/{Uri.EscapeDataString(project)}/_apis/pipelines?api-version=7.1";
        var resp = await client.GetAsync(uri, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to list pipelines: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (json.TryGetProperty("value", out var pipelines) && pipelines.ValueKind == JsonValueKind.Array)
        {
            foreach (var pipeline in pipelines.EnumerateArray())
            {
                if (pipeline.TryGetProperty("name", out var name) &&
                    string.Equals(name.GetString(), pipelineIdOrName, StringComparison.OrdinalIgnoreCase))
                {
                    if (pipeline.TryGetProperty("id", out var id))
                    {
                        return id.GetInt32().ToString();
                    }
                }
            }
        }

        throw new InvalidOperationException($"Pipeline '{pipelineIdOrName}' not found in project '{project}'.");
    }

    private static async Task<int> TriggerPipelineAsync(
        HttpClient client,
        string org,
        string baseUrl,
        string project,
        string pipelineId,
        AzureDevOpsPipelineRun props,
        CancellationToken ct)
    {
        string refName;
        if (!string.IsNullOrWhiteSpace(props.Branch))
        {
            var branch = props.Branch.Replace("refs/heads/", "");
            refName = $"refs/heads/{branch}";
        }
        else
        {
            var tag = props.Tag!.Replace("refs/tags/", "");
            refName = $"refs/tags/{tag}";
        }

        object? templateParams = null;
        if (!string.IsNullOrWhiteSpace(props.TemplateParameters))
        {
            try
            {
                templateParams = JsonSerializer.Deserialize<Dictionary<string, object>>(props.TemplateParameters);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON in templateParameters: {ex.Message}", ex);
            }
        }

        object? variables = null;
        if (!string.IsNullOrWhiteSpace(props.Variables))
        {
            try
            {
                variables = JsonSerializer.Deserialize<Dictionary<string, object>>(props.Variables);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException($"Invalid JSON in variables: {ex.Message}", ex);
            }
        }

        var body = new
        {
            resources = new
            {
                repositories = new
                {
                    self = new
                    {
                        refName = refName
                    }
                }
            },
            templateParameters = templateParams,
            variables = variables
        };

        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var uri = $"{baseUrl}/{org}/{Uri.EscapeDataString(project)}/_apis/pipelines/{pipelineId}/runs?api-version=7.1";

        var resp = await client.PostAsync(uri, content, ct);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to trigger pipeline: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        if (json.TryGetProperty("id", out var idProp))
        {
            return idProp.GetInt32();
        }

        throw new InvalidOperationException("Pipeline run response did not contain an ID.");
    }

    private static async Task<dynamic> GetPipelineRunAsync(
        HttpClient client,
        string org,
        string baseUrl,
        string project,
        string pipelineId,
        int runId,
        CancellationToken ct)
    {
        var uri = $"{baseUrl}/{org}/{Uri.EscapeDataString(project)}/_apis/pipelines/{pipelineId}/runs/{runId}?api-version=7.1";
        var resp = await client.GetAsync(uri, ct);

        if (!resp.IsSuccessStatusCode)
        {
            var err = await resp.Content.ReadAsStringAsync(ct);
            throw new InvalidOperationException($"Failed to get pipeline run: {(int)resp.StatusCode} {resp.ReasonPhrase} {err}");
        }

        var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
        return new
        {
            id = json.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            state = json.TryGetProperty("state", out var state) ? state.GetString() : null,
            result = json.TryGetProperty("result", out var result) ? result.GetString() : null,
            url = json.TryGetProperty("url", out var url) ? url.GetString() : null
        };
    }
}
