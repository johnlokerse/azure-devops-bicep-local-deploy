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
    "This example shows how to create a work item.",
    @"resource workItem 'AzureDevOpsWorkItem' = {
  organization: 'myorg'
  project: 'myproject'
  id: 1
  title: 'mytitle'
  type: 'task'
}
"
)]
[ResourceType("AzureDevOpsWorkItem")] // exposed to Bicep as resource type name
public class AzureDevOpsWorkItem : AzureDevOpsWorkItemIdentifiers
{
    [TypeProperty("Internal Work item Id")]
    public int Id { get; set; }
    
    [TypeProperty("Work item title")]
    public required string Title { get; set; }
    
    [TypeProperty("Work item type")]
    public required string Type { get; set; }
    
    [TypeProperty("Work item description")]
    public string? Description { get; set; }
}