# AzureDevOpsProject

Represents an Azure DevOps project for organizing work, code, and resources.

## Example usage

### Creating a basic private project

This example shows how to create a basic private project with default settings.

```bicep
resource project 'AzureDevOpsProject' = {
  name: 'my-project'
  organization: 'myorg'
  description: 'Main development project for our team'
  visibility: 'Private'
  processName: 'Agile'
  sourceControlType: 'Git'
}
```

### Creating a public project with Scrum process

This example shows how to create a public project using the Scrum process template.

```bicep
resource scrumProject 'AzureDevOpsProject' = {
  name: 'open-source-project'
  organization: 'myorg'
  description: 'Open source project for community collaboration'
  visibility: 'Public'
  processName: 'Scrum'
  sourceControlType: 'Git'
}
```

### Creating a project with TFVC source control

This example shows how to create a project using Team Foundation Version Control.

```bicep
resource tfvcProject 'AzureDevOpsProject' = {
  name: 'legacy-project'
  organization: 'myorg'
  description: 'Legacy project using TFVC for source control'
  visibility: 'Private'
  processName: 'CMMI'
  sourceControlType: 'Tfvc'
}
```

## Argument reference

The following arguments are available:

- `name` - (Required) The Azure DevOps project name
- `organization` - (Required) Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL
- `description` - (Optional) Project description
- `processName` - (Optional) Process name to use (e.g. Agile, Scrum, Basic, CMMI)
- `projectId` - (Optional) [OUTPUT] Project id (GUID)
- `sourceControlType` - (Optional) Source control type (Git or Tfvc)
- `state` - (Optional) [OUTPUT] Project state
- `url` - (Optional) [OUTPUT] Project web URL
- `visibility` - (Optional) Project visibility (Private/Public) (Can be `Private`, or `Public`)

## Notes

When working with the 'AzureDevOpsProject' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
extension azureDevOpsExtension with {
  // extension configuration
}
```

Please note the following important considerations when using the `AzureDevOpsProject` resource:

- Project names must be unique within the organization
- Available process templates: Agile, Scrum, Basic, CMMI (case-sensitive)
- Source control types: Git (recommended) or Tfvc
- Public projects are visible to everyone on the internet
- Project deletion is not supported through this resource - must be done manually
- The project URL and ID are generated after creation and available as output properties

## Additional reference

For more information, see the following links:

- [Azure DevOps Projects documentation][00]
- [Choose a process template][01]
- [Project visibility and access][02]

<!-- Link reference definitions -->
[00]: https://docs.microsoft.com/en-us/azure/devops/organizations/projects/
[01]: https://docs.microsoft.com/en-us/azure/devops/boards/work-items/guidance/choose-process
[02]: https://docs.microsoft.com/en-us/azure/devops/organizations/public/about-public-projects

