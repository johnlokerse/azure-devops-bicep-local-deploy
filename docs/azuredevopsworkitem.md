# AzureDevOpsWorkItem

Represents an Azure DevOps work item.

## Example usage

### Creating or updating a simple work item by internal id

This example shows how to create a work item. Description is optional. If the work item already exists, it will be updated (title and description).

```bicep
resource workItem 'AzureDevOpsWorkItem' = {
  organization: 'myorg'
  project: 'myproject'
  id: 1
  title: 'mytitle'
  description: 'my description'
  type: 'task'
}
```

## Argument reference

The following arguments are available:

- `id` - (Required) Internal Work item Id
- `organization` - (Required) Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL
- `project` - (Required) Azure DevOps project name
- `title` - (Required) Work item title
- `type` - (Required) Work item type
- `description` - (Optional) Work item description

