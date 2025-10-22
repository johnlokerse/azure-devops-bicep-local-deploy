using Azure.Bicep.Types.Concrete;

namespace DevOpsExtension.Permission;

public class AzureDevOpsPermissionIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("The Azure DevOps project name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }
}

[BicepDocHeading("AzureDevOpsPermission", "Assigns a Microsoft Entra ID group to a project role in Azure DevOps.")]
[BicepDocExample(
    "Assigning a group to the Contributors role",
    "This example shows how to assign an Entra ID group to the Contributors role in a project.",
    @"resource permission 'AzureDevOpsPermission' = {
  organization: 'myorg'
  project: 'my-project'
  groupObjectId: '00000000-0000-0000-0000-000000000000' // Replace with your Entra ID group object ID
  role: 'Contributors'
}
")]
[BicepDocCustom("Notes", @"When working with the 'AzureDevOpsPermission' resource, ensure you have the extension imported in your Bicep file:

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
")]
[BicepDocCustom("Additional reference", @"For more information, see the following links:

- [About permissions and security groups][00]
- [Security groups, service accounts, and permissions reference][01]
- [Default permissions quick reference][02]

<!-- Link reference definitions -->
[00]: https://learn.microsoft.com/en-us/azure/devops/organizations/security/about-permissions?view=azure-devops&tabs=preview-page
[01]: https://learn.microsoft.com/en-us/azure/devops/organizations/security/permissions?view=azure-devops&tabs=preview-page
[02]: https://learn.microsoft.com/en-us/azure/devops/organizations/security/permissions-access?view=azure-devops")]

[ResourceType("AzureDevOpsPermission")]
public class AzureDevOpsPermission : AzureDevOpsPermissionIdentifiers
{
    [TypeProperty("Azure Entra ID group object id (GUID)", ObjectTypePropertyFlags.Required)]
    public required string GroupObjectId { get; set; }

    [TypeProperty("Project role to assign the group to (e.g., Readers, Contributors, Project Administrators, or any custom role)", ObjectTypePropertyFlags.Required)]
    public required string Role { get; set; }

    // Outputs
    [TypeProperty("[OUTPUT] Whether the Entra ID group is currently assigned to the target role in the project", ObjectTypePropertyFlags.ReadOnly)]
    public bool Assigned { get; set; }

    [TypeProperty("[OUTPUT] Descriptor of the Entra ID group in Azure DevOps Graph", ObjectTypePropertyFlags.ReadOnly)]
    public string? GroupDescriptor { get; set; }

    [TypeProperty("[OUTPUT] Descriptor of the target project security group", ObjectTypePropertyFlags.ReadOnly)]
    public string? ProjectGroupDescriptor { get; set; }
}
