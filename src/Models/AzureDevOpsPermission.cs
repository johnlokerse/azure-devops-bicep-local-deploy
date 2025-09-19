using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace DevOpsExtension.Models;

public class AzureDevOpsPermissionIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("The Azure DevOps project name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }
}

[ResourceType("AzureDevOpsPermission")]
public class AzureDevOpsPermission : AzureDevOpsPermissionIdentifiers
{
    [TypeProperty("Azure Entra ID (AAD) group object id (GUID)", ObjectTypePropertyFlags.Required)]
    public required string GroupObjectId { get; set; }

    [TypeProperty("Project role to assign the group to (Readers or Contributors)", ObjectTypePropertyFlags.Required)]
    public required string Role { get; set; }

    // Outputs
    [TypeProperty("[OUTPUT] Whether the AAD group is currently assigned to the target role in the project", ObjectTypePropertyFlags.ReadOnly)]
    public bool Assigned { get; set; }

    [TypeProperty("[OUTPUT] Descriptor of the AAD group in Azure DevOps Graph", ObjectTypePropertyFlags.ReadOnly)]
    public string? GroupDescriptor { get; set; }

    [TypeProperty("[OUTPUT] Descriptor of the target project security group", ObjectTypePropertyFlags.ReadOnly)]
    public string? ProjectGroupDescriptor { get; set; }
}
