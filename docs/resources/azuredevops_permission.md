# AzureDevOpsPermission

Assigns a Microsoft Entra ID group to a project role in Azure DevOps.

## Example usage

### Assigning a group to the Contributors role

This example shows how to assign an Entra ID group to the Contributors role in a project.

```bicep
resource permission 'AzureDevOpsPermission' = {
  organization: 'myorg'
  project: 'my-project'
  groupObjectId: '00000000-0000-0000-0000-000000000000' // Replace with your Entra ID group object ID
  role: 'Contributors'
}
```

## Argument reference

The following arguments are available:

- `groupObjectId` - (Required) Azure Entra ID (AAD) group object id (GUID)
- `organization` - (Required) Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL
- `project` - (Required) The Azure DevOps project name
- `role` - (Required) Project role to assign the group to (Readers or Contributors)

## Attribute reference

In addition to all arguments above, the following attributes are outputted:

- `assigned` - [OUTPUT] Whether the AAD group is currently assigned to the target role in the project
- `groupDescriptor` - [OUTPUT] Descriptor of the AAD group in Azure DevOps Graph
- `projectGroupDescriptor` - [OUTPUT] Descriptor of the target project security group

## Notes

When working with the 'AzureDevOpsPermission' resource, ensure you have the extension imported in your Bicep file:

```bicep
// main.bicep
targetScope = 'local'
extension azureDevOpsExtension with {
  // extension configuration
}
```

Please note the following important considerations when using the `AzureDevOpsPermission` resource:
- The `groupObjectId` must be a valid Entra ID group object ID (GUID). You can find this in the Azure portal under the group's properties or supply it with a Bicep parameter using MicrosoftGraph Bicep module.
- The `role` must match an existing project group name in Azure DevOps. Common built-in roles include 'Readers', 'Contributors', and 'Project Administrators'. Custom roles can also be used if they exist in the project.


## Additional reference

For more information, see the following links:

- [About permissions and security groups][00]
- [Security groups, service accounts, and permissions reference][01]
- [Default permissions quick reference][02]

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/azure/devops/organizations/security/about-permissions?view=azure-devops&tabs=preview-page
[01]: https://learn.microsoft.com/en-us/azure/devops/organizations/security/permissions?view=azure-devops&tabs=preview-page
[02]: https://learn.microsoft.com/en-us/azure/devops/organizations/security/permissions-access?view=azure-devops

