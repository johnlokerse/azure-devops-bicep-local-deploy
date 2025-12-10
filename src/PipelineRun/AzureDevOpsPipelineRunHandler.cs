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
        var properties = request.Properties;
        ValidateInputs(properties);

        var (organization, baseUrl) = GetOrgAndBaseUrl(properties.Organization);
        using var client = CreateClient(request.Config);

        // Resolve pipeline ID if a name was provided
        var pipelineId = await ResolvePipelineIdAsync(client, organization, baseUrl, properties.Project, properties.PipelineId, cancellationToken);

        // Trigger the pipeline run
        var runId = await TriggerPipelineAsync(client, organization, baseUrl, properties.Project, pipelineId, properties, cancellationToken);

        // Get the pipeline run details
        var runDetails = await GetPipelineRunAsync(client, organization, baseUrl, properties.Project, pipelineId, runId, cancellationToken);

        // Populate output properties
        properties.RunId = runDetails.id;
        properties.State = runDetails.state;
        properties.Result = runDetails.result;
        properties.Url = runDetails.url;

        return GetResponse(request);
    }

    protected override AzureDevOpsPipelineRunIdentifiers GetIdentifiers(AzureDevOpsPipelineRun properties) => new()
    {
        Organization = properties.Organization,
        Project = properties.Project,
        PipelineId = properties.PipelineId
    };

    private static void ValidateInputs(AzureDevOpsPipelineRun properties)
    {
        if (string.IsNullOrWhiteSpace(properties.Branch) && string.IsNullOrWhiteSpace(properties.Tag))
        {
            throw new InvalidOperationException("Either 'branch' or 'tag' must be specified.");
        }

        if (!string.IsNullOrWhiteSpace(properties.Branch) && !string.IsNullOrWhiteSpace(properties.Tag))
        {
            throw new InvalidOperationException("Cannot specify both 'branch' and 'tag'. Choose one.");
        }
    }

    private static async Task<string> ResolvePipelineIdAsync(
        HttpClient client,
        string organization,
        string baseUrl,
        string project,
        string pipelineIdOrName,
        CancellationToken cancellationToken)
    {
        // If it's already a numeric ID, return it
        if (int.TryParse(pipelineIdOrName, out _))
        {
            return pipelineIdOrName;
        }

        // Otherwise, lookup by name
        var uri = $"{baseUrl}/{organization}/{Uri.EscapeDataString(project)}/_apis/pipelines?api-version=7.1";
        var response = await client.GetAsync(uri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to list pipelines: {(int)response.StatusCode} {response.ReasonPhrase} {errorMessage}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
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
        string organization,
        string baseUrl,
        string project,
        string pipelineId,
        AzureDevOpsPipelineRun properties,
        CancellationToken cancellationToken)
    {
        string refName;
        if (!string.IsNullOrWhiteSpace(properties.Branch))
        {
            var branch = properties.Branch.Replace("refs/heads/", "");
            refName = $"refs/heads/{branch}";
        }
        else
        {
            var tag = properties.Tag!.Replace("refs/tags/", "");
            refName = $"refs/tags/{tag}";
        }

        object? templateParameters = null;
        if (!string.IsNullOrWhiteSpace(properties.TemplateParameters))
        {
            try
            {
                templateParameters = JsonSerializer.Deserialize<Dictionary<string, object>>(properties.TemplateParameters);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"Invalid JSON in templateParameters: {exception.Message}", exception);
            }
        }

        object? variables = null;
        if (!string.IsNullOrWhiteSpace(properties.Variables))
        {
            try
            {
                variables = JsonSerializer.Deserialize<Dictionary<string, object>>(properties.Variables);
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException($"Invalid JSON in variables: {exception.Message}", exception);
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
            templateParameters = templateParameters,
            variables = variables
        };

        var content = new StringContent(JsonSerializer.Serialize(body, JsonOptions), Encoding.UTF8, "application/json");
        var uri = $"{baseUrl}/{organization}/{Uri.EscapeDataString(project)}/_apis/pipelines/{pipelineId}/runs?api-version=7.1";

        var response = await client.PostAsync(uri, content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to trigger pipeline: {(int)response.StatusCode} {response.ReasonPhrase} {errorMessage}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        if (json.TryGetProperty("id", out var idProperty))
        {
            return idProperty.GetInt32();
        }

        throw new InvalidOperationException("Pipeline run response did not contain an ID.");
    }

    private static async Task<dynamic> GetPipelineRunAsync(
        HttpClient client,
        string organization,
        string baseUrl,
        string project,
        string pipelineId,
        int runId,
        CancellationToken cancellationToken)
    {
        var uri = $"{baseUrl}/{organization}/{Uri.EscapeDataString(project)}/_apis/pipelines/{pipelineId}/runs/{runId}?api-version=7.1";
        var response = await client.GetAsync(uri, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorMessage = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to get pipeline run: {(int)response.StatusCode} {response.ReasonPhrase} {errorMessage}");
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
        return new
        {
            id = json.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
            state = json.TryGetProperty("state", out var state) ? state.GetString() : null,
            result = json.TryGetProperty("result", out var result) ? result.GetString() : null,
            url = json.TryGetProperty("url", out var url) ? url.GetString() : null
        };
    }
}
