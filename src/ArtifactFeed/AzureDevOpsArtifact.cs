using Azure.Bicep.Types.Concrete;

namespace DevOpsExtension.ArtifactFeed;

[BicepDocHeading("AzureDevOpsArtifactFeed", "Represents an Azure DevOps artifact feed for package management and distribution.")]
[BicepDocExample(
    "Creating a basic organization-scoped feed",
    "This example shows how to create a basic artifact feed at the organization level.",
    @"resource feed 'AzureDevOpsArtifactFeed' = {
  name: 'my-packages'
  organization: 'myorg'
  description: 'Main package feed for the organization'
  hideDeletedPackageVersions: true
  upstreamEnabled: true
}
"
)]
[BicepDocExample(
    "Creating a project-scoped feed with permissions",
    "This example shows how to create a project-scoped feed with specific permissions.",
    @"resource projectFeed 'AzureDevOpsArtifactFeed' = {
  name: 'project-packages'
  organization: 'myorg'
  project: 'my-project'
  description: 'Package feed for the project team'
  hideDeletedPackageVersions: false
  upstreamEnabled: true
  permissions: [
    {
      identityDescriptor: 'Microsoft.TeamFoundation.Identity;S-1-9-1551374245-1204400969-2402986413-2179408616-0-0-0-0-1'
      role: 4 // Owner
    }
    {
      identityDescriptor: 'Microsoft.TeamFoundation.Identity;S-1-9-1551374245-1204400969-2402986413-2179408616-0-0-0-0-2'
      role: 2 // Contributor
    }
  ]
}
"
)]
[BicepDocExample(
    "Creating a feed with upstream sources",
    "This example shows how to create a feed with upstream package sources configured.",
    @"resource feedWithUpstream 'AzureDevOpsArtifactFeed' = {
  name: 'enterprise-packages'
  organization: 'myorg'
  project: 'shared-services'
  description: 'Enterprise package feed with upstream sources'
  upstreamEnabled: true
  upstreamSources: [
    {
      name: 'npmjs'
      location: 'https://registry.npmjs.org/'
      protocol: 'npm'
    }
    {
      name: 'nuget-org'
      location: 'https://api.nuget.org/v3/index.json'
      protocol: 'nuget'
    }
  ]
}
"
)]
[BicepDocCustom("Notes", @"When working with the 'AzureDevOpsArtifactFeed' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
extension azureDevOpsExtension with {
  // extension configuration
}
```

Please note the following important considerations when using the `AzureDevOpsArtifactFeed` resource:

- Feed names must be unique within the organization or project scope
- Organization can be specified as just the name (e.g., 'myorg') or the full URL
- If no project is specified, the feed will be organization-scoped
- Role levels for permissions: 1=Reader, 2=Contributor, 3=Collaborator, 4=Owner, 5=Administrator
- Upstream sources allow your feed to proxy packages from external sources like npmjs.org or nuget.org")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [Azure DevOps Artifacts documentation][00]
- [Manage feed permissions][01]
- [Configure upstream sources][02]

<!-- Link reference definitions -->
[00]: https://docs.microsoft.com/en-us/azure/devops/artifacts/
[01]: https://docs.microsoft.com/en-us/azure/devops/artifacts/feeds/feed-permissions
[02]: https://docs.microsoft.com/en-us/azure/devops/artifacts/how-to/set-up-upstream-sources")]
[ResourceType("AzureDevOpsArtifactFeed")] // exposed to Bicep as resource type name
public class AzureDevOpsArtifactFeed : AzureDevOpsArtifactFeedIdentifiers
{
    [TypeProperty("Feed description")]
    public string? Description { get; set; }

    [TypeProperty("Whether to hide deleted package versions")]
    public bool HideDeletedPackageVersions { get; set; } = true;

    [TypeProperty("Whether upstream sources are enabled")]
    public bool UpstreamEnabled { get; set; } = true;

    [TypeProperty("Feed permissions")]
    public AzureDevOpsArtifactFeedPermission[]? Permissions { get; set; }

    [TypeProperty("Upstream sources configuration")]
    public AzureDevOpsArtifactFeedUpstreamSource[]? UpstreamSources { get; set; }

    // Outputs
    [TypeProperty("Feed ID", ObjectTypePropertyFlags.ReadOnly)]
    public string? FeedId { get; set; }

    [TypeProperty("Feed URL", ObjectTypePropertyFlags.ReadOnly)]
    public string? Url { get; set; }

    [TypeProperty("Project reference (when project-scoped)", ObjectTypePropertyFlags.ReadOnly)]
    public AzureDevOpsArtifactFeedProjectReference? ProjectReference { get; set; }
}
public class AzureDevOpsArtifactFeedIdentifiers
{
    [TypeProperty("The Azure DevOps artifact feed name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Azure DevOps project name (optional - if not provided, feed will be organization-scoped)")]
    public string? Project { get; set; }
}

public class AzureDevOpsArtifactFeedPermission
{
    [TypeProperty("Identity descriptor for the permission")]
    public string? IdentityDescriptor { get; set; }

    [TypeProperty("Identity ID for the permission")]
    public string? IdentityId { get; set; }

    [TypeProperty("Role level for the permission (1=Reader, 2=Contributor, 3=Collaborator, 4=Owner, 5=Administrator)")]
    public int Role { get; set; }
}

public class AzureDevOpsArtifactFeedUpstreamSource
{
    [TypeProperty("Upstream source ID")]
    public string? Id { get; set; }

    [TypeProperty("Upstream source name")]
    public string? Name { get; set; }

    [TypeProperty("Upstream source location")]
    public string? Location { get; set; }

    [TypeProperty("Upstream source protocol")]
    public string? Protocol { get; set; }
}

public class AzureDevOpsArtifactFeedProjectReference
{
    [TypeProperty("Project ID")]
    public string? Id { get; set; }

    [TypeProperty("Project name")]
    public string? Name { get; set; }
}