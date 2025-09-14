# AzureDevOpsArtifactFeed

Represents an Azure DevOps artifact feed for package management and distribution.

## Example usage

### Creating a basic organization-scoped feed

This example shows how to create a basic artifact feed at the organization level.

```bicep
resource feed 'AzureDevOpsArtifactFeed' = {
  name: 'my-packages'
  organization: 'myorg'
  description: 'Main package feed for the organization'
  hideDeletedPackageVersions: true
  upstreamEnabled: true
}
```

### Creating a project-scoped feed with permissions

This example shows how to create a project-scoped feed with specific permissions.

```bicep
resource projectFeed 'AzureDevOpsArtifactFeed' = {
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
```

### Creating a feed with upstream sources

This example shows how to create a feed with upstream package sources configured.

```bicep
resource feedWithUpstream 'AzureDevOpsArtifactFeed' = {
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
```

## Argument reference

The following arguments are available:

- `name` - (Required) The Azure DevOps artifact feed name
- `organization` - (Required) Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL
- `description` - (Optional) Feed description
- `hideDeletedPackageVersions` - (Optional) Whether to hide deleted package versions
- `permissions` - (Optional) Feed permissions:
  - `identityDescriptor` - (Optional) Identity descriptor for the permission
  - `identityId` - (Optional) Identity ID for the permission
  - `role` - (Optional) Role level for the permission (1=Reader, 2=Contributor, 3=Collaborator, 4=Owner, 5=Administrator)
- `project` - (Optional) Azure DevOps project name (optional - if not provided, feed will be organization-scoped)
- `upstreamEnabled` - (Optional) Whether upstream sources are enabled
- `upstreamSources` - (Optional) Upstream sources configuration:
  - `id` - (Optional) Upstream source ID
  - `location` - (Optional) Upstream source location
  - `name` - (Optional) Upstream source name
  - `protocol` - (Optional) Upstream source protocol

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `feedId` - Feed ID
- `projectReference` - Project reference (when project-scoped):
- `url` - Feed URL

## Notes

When working with the 'AzureDevOpsArtifactFeed' resource, ensure you have the extension imported in your Bicep file:

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
- Upstream sources allow your feed to proxy packages from external sources like npmjs.org or nuget.org

## Additional reference

For more information, see the following links:

- [Azure DevOps Artifacts documentation][00]
- [Manage feed permissions][01]
- [Configure upstream sources][02]

<!-- Link reference definitions -->
[00]: https://docs.microsoft.com/en-us/azure/devops/artifacts/
[01]: https://docs.microsoft.com/en-us/azure/devops/artifacts/feeds/feed-permissions
[02]: https://docs.microsoft.com/en-us/azure/devops/artifacts/how-to/set-up-upstream-sources

