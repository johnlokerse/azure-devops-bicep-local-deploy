using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace DevOpsExtension.Models;

public class AzureDevOpsArtifactFeedIdentifiers
{
    [TypeProperty("The Azure DevOps artifact feed name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }

    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Azure DevOps project name (optional - if not provided, feed will be organization-scoped)")]
    public string? ProjectName { get; set; }
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
    public AzureDevOpsArtifactFeedProjectReference? Project { get; set; }
}