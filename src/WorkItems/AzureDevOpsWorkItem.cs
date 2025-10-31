using Azure.Bicep.Types.Concrete;

namespace DevOpsExtension.WorkItems;

public class AzureDevOpsWorkItemIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Azure DevOps project name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }
}

[BicepDocHeading("AzureDevOpsWorkItem", "Represents an Azure DevOps work item.")]
[BicepDocExample(
    "Creating or updating a simple work item by internal id",
    "This example shows how to create a work item. Description is optional. If the work item already exists, it will be updated (title and description).",
    @"resource workItem 'AzureDevOpsWorkItem' = {
  organization: 'myorg'
  project: 'myproject'
  id: 1
  title: 'mytitle'
  description: 'my description'
  type: 'task'
}
"
)]
[ResourceType("AzureDevOpsWorkItem")] // exposed to Bicep as resource type name
public class AzureDevOpsWorkItem : AzureDevOpsWorkItemIdentifiers
{
    [TypeProperty("Internal Work item Id", ObjectTypePropertyFlags.Required)]
    public required int Id { get; set; }
    
    [TypeProperty("Work item title", ObjectTypePropertyFlags.Required)]
    public required string Title { get; set; }
    
    [TypeProperty("Work item type", ObjectTypePropertyFlags.Required)]
    public required string Type { get; set; }
    
    [TypeProperty("Work item description")]
    public string? Description { get; set; }
}