using Azure.Bicep.Types.Concrete;

namespace DevOpsExtension.Repository;

public class AzureDevOpsRepositoryIdentifiers
{
    [TypeProperty("The Azure DevOps repository name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Project name that will contain the repository", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }
}

[BicepDocHeading("AzureDevOpsRepository", "Represents a Git repository in an Azure DevOps project for source code management.")]
[BicepDocExample(
    "Creating a basic repository",
    "This example shows how to create a basic Git repository in an Azure DevOps project.",
    @"resource repository 'AzureDevOpsRepository' = {
  name: 'my-app'
  organization: 'myorg'
  project: 'my-project'
}
"
)]
[BicepDocExample(
    "Creating a repository with default branch",
    "This example shows how to create a repository and set a specific default branch.",
    @"resource repoWithBranch 'AzureDevOpsRepository' = {
  name: 'web-application'
  organization: 'myorg'
  project: 'frontend-team'
  defaultBranch: 'refs/heads/main'
}
"
)]
[BicepDocExample(
    "Creating multiple repositories for microservices",
    "This example shows how to create multiple repositories for a microservices architecture.",
    @"var services = [
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
"
)]
[BicepDocCustom("Notes", @"When working with the 'AzureDevOpsRepository' resource, ensure you have the extension imported in your Bicep file:

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
- Both HTTPS and SSH clone URLs are provided in the outputs")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Azure DevOps Repos documentation][00]
- [Git repository management][01]
- [Repository permissions and security][02]

<!-- Link reference definitions -->
[00]: https://docs.microsoft.com/en-us/azure/devops/repos/
[01]: https://docs.microsoft.com/en-us/azure/devops/repos/git/
[02]: https://docs.microsoft.com/en-us/azure/devops/repos/git/set-git-repository-permissions")]
[ResourceType("AzureDevOpsRepository")]
public class AzureDevOpsRepository : AzureDevOpsRepositoryIdentifiers
{
    [TypeProperty("Default branch name to set after creation (e.g. 'refs/heads/main'). If omitted, Azure DevOps sets one after first push.")]
    public string? DefaultBranch { get; set; }

    // Outputs
    [TypeProperty("[OUTPUT] Repository id (GUID)", ObjectTypePropertyFlags.ReadOnly)]
    public string? RepositoryId { get; set; }

    [TypeProperty("[OUTPUT] Repository web URL", ObjectTypePropertyFlags.ReadOnly)]
    public string? WebUrl { get; set; }

    [TypeProperty("[OUTPUT] HTTPS clone URL", ObjectTypePropertyFlags.ReadOnly)]
    public string? RemoteUrl { get; set; }

    [TypeProperty("[OUTPUT] SSH clone URL", ObjectTypePropertyFlags.ReadOnly)]
    public string? SshUrl { get; set; }
}