# AzureDevOpsRepository

Represents a Git repository in an Azure DevOps project for source code management.

## Example usage

### Creating a basic repository

This example shows how to create a basic Git repository in an Azure DevOps project.

```bicep
resource repository 'AzureDevOpsRepository' = {
  name: 'my-app'
  organization: 'myorg'
  project: 'my-project'
}
```

### Creating a repository with default branch

This example shows how to create a repository and set a specific default branch.

```bicep
resource repoWithBranch 'AzureDevOpsRepository' = {
  name: 'web-application'
  organization: 'myorg'
  project: 'frontend-team'
  defaultBranch: 'refs/heads/main'
}
```

### Creating multiple repositories for microservices

This example shows how to create multiple repositories for a microservices architecture.

```bicep
var services = [
  'user-service'
  'order-service'
  'payment-service'
  'notification-service'
]

resource microserviceRepos 'AzureDevOpsRepository' = [for service in services: {
  name: service
  organization: 'myorg'
  project: 'microservices-platform'
  defaultBranch: 'refs/heads/main'
}]
```

## Argument reference

The following arguments are available:

- `name` - (Required) The Azure DevOps repository name
- `organization` - (Required) Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL
- `project` - (Required) Project name that will contain the repository
- `defaultBranch` - (Optional) Default branch name to set after creation (e.g. 'refs/heads/main'). If omitted, Azure DevOps sets one after first push.

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `remoteUrl` - [OUTPUT] HTTPS clone URL
- `repositoryId` - [OUTPUT] Repository id (GUID)
- `sshUrl` - [OUTPUT] SSH clone URL
- `webUrl` - [OUTPUT] Repository web URL

## Notes

When working with the 'AzureDevOpsRepository' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
extension azureDevOpsExtension with {
  // extension configuration
}
```

Please note the following important considerations when using the `AzureDevOpsRepository` resource:

- Repository names must be unique within the project
- The parent project must exist before creating the repository
- Default branch format should be 'refs/heads/branch-name' (e.g., 'refs/heads/main')
- If no default branch is specified, Azure DevOps will set one after the first push
- Repository URLs and IDs are generated after creation and available as output properties
- Both HTTPS and SSH clone URLs are provided in the outputs

## Additional reference

For more information, see the following links:

- [Azure DevOps Repos documentation][00]
- [Git repository management][01]
- [Repository permissions and security][02]

<!-- Link reference definitions -->
[00]: https://docs.microsoft.com/en-us/azure/devops/repos/
[01]: https://docs.microsoft.com/en-us/azure/devops/repos/git/
[02]: https://docs.microsoft.com/en-us/azure/devops/repos/git/set-git-repository-permissions

