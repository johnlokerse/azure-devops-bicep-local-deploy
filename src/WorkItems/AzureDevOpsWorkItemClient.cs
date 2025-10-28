using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace DevOpsExtension.WorkItems;

public class AzureDevOpsWorkItemClient(HttpClient client)
{
    private const string ApiVersion = "7.1";
    private const string InternalIdKey = "InternalId";

    public static AzureDevOpsWorkItemClient Create(HttpClient client, string baseUrl, string organization,
        string project)
    {
        client.BaseAddress = new Uri($"{baseUrl}/{organization}/{project}/_apis/wit/");
        return new AzureDevOpsWorkItemClient(client);
    }

    // Reference: https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/wiql/query-by-wiql?view=azure-devops-rest-7.1&tabs=HTTP
    public async Task<int?> FindByAsync(int internalId, CancellationToken cancellationToken)
    {
        var wiql = new
        {
            query = $"SELECT [System.Id] FROM WorkItems WHERE [System.Tags] CONTAINS '{InternalIdKey}={internalId}'"
        };

        var response = await client.PostAsync(
            $"wiql?api-version={ApiVersion}",
            new StringContent(
                JsonSerializer.Serialize(wiql),
                Encoding.UTF8,
                "application/json"),
            cancellationToken);

        if (!response.IsSuccessStatusCode) return null;

        var workItems = await response.Content.ReadFromJsonAsync<AzureDevOpsWorkItemsSearchResponse>(cancellationToken);

        if (workItems?.WorkItems.Length > 0) return workItems.WorkItems[0].Id;

        return null;
    }

    // Reference: https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/create?view=azure-devops-rest-7.1&tabs=HTTP
    public async Task CreateAsync(int internalId, string title, string type, CancellationToken cancellationToken)
    {
        AzureWorkItemModificationRequest[] patchDocument =
        [
            new("add", "/fields/System.Title", title),
            new("add", "/fields/System.Tags", internalId > 0 ? $"{InternalIdKey}={internalId}" : "")
        ];

        var serialize = JsonSerializer.Serialize(patchDocument);
        var content = new StringContent(
            serialize,
            Encoding.UTF8,
            "application/json-patch+json");

        var requestUri = $"workitems/${type}?api-version={ApiVersion}";

        var createResponse = await client.PostAsync(
            requestUri,
            content,
            cancellationToken);

        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to create workitem with title '{title}' and id {internalId}. Status: {createResponse.StatusCode}, Response: {errorContent}, Complete Uri {createResponse.RequestMessage?.RequestUri}");
        }
    }

    // Reference: https://learn.microsoft.com/en-us/rest/api/azure/devops/wit/work-items/update?view=azure-devops-rest-7.1&tabs=HTTP
    public async Task UpdateAsync(int id, string title, CancellationToken cancellationToken)
    {
        AzureWorkItemModificationRequest[] patchDocument =
        [
            new("replace", "/fields/System.Title", title)
        ];

        var serialize = JsonSerializer.Serialize(patchDocument);
        var content = new StringContent(
            serialize,
            Encoding.UTF8,
            "application/json-patch+json");

        var requestUri = $"workitems/{id}?api-version={ApiVersion}";

        var createResponse = await client.PatchAsync(
            requestUri,
            content,
            cancellationToken);

        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Failed to update workitem with title '{title}' and id {id}. Status: {createResponse.StatusCode}, Response: {errorContent}, Complete Uri {createResponse.RequestMessage?.RequestUri}");
        }
    }

    private record AzureWorkItemModificationRequest(string Op, string Path, string Value, string? From = null);

    private record AzureDevOpsWorkItemsSearchResponse(AzureDevOpsWorkItemResponse[] WorkItems);

    private record AzureDevOpsWorkItemResponse(int Id);
}