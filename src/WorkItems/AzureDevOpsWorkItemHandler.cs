namespace DevOpsExtension.WorkItems;

public class AzureDevOpsWorkItemHandler
    : AzureDevOpsResourceHandlerBase<AzureDevOpsWorkItem, AzureDevOpsWorkItemIdentifiers>
{
    protected override async Task<ResourceResponse> Preview(
        ResourceRequest request,
        CancellationToken cancellationToken)
    {
        var props = request.Properties;

        var client = CreateClient(request, props);

        var existing = await client.FindByAsync(props.Id, cancellationToken);

        if (existing is not null) props.Id = existing.Value;

        return GetResponse(request);
    }

    protected override async Task<ResourceResponse> CreateOrUpdate(
        ResourceRequest request,
        CancellationToken cancellationToken)
    {
        var props = request.Properties;

        var client = CreateClient(request, props);

        var existing = await client.FindByAsync(props.Id, cancellationToken);

        if (existing is null)
            await client.CreateAsync(props.Id, props.Title, props.Type, cancellationToken);
        else
            await client.UpdateAsync(existing.Value, props.Title, cancellationToken);

        return GetResponse(request);
    }

    protected override AzureDevOpsWorkItemIdentifiers GetIdentifiers(
        AzureDevOpsWorkItem properties)
    {
        return new AzureDevOpsWorkItemIdentifiers
        {
            Organization = properties.Organization,
            Project = properties.Project
        };
    }

    private static AzureDevOpsWorkItemClient CreateClient(ResourceRequest request, AzureDevOpsWorkItem props)
    {
        var (organization, baseUrl) = GetOrgAndBaseUrl(props.Organization);
        return AzureDevOpsWorkItemClient.Create(
            CreateClient(request.Config), 
            baseUrl, 
            organization, 
            props.Project);
    }
}
