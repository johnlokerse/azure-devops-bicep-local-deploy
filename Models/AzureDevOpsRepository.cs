using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace DevOpsExtension.Models;

public class AzureDevOpsRepositoryIdentifiers
{
    [TypeProperty("The Azure DevOps repository name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }
}

[ResourceType("AzureDevOpsRepository")]
public class AzureDevOpsRepository : AzureDevOpsRepositoryIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Project name that will contain the repository", ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }

    [TypeProperty("Personal Access Token (PAT) for Azure DevOps with appropriate scopes. If omitted, environment variable AZDO_PAT is used.")]
    public string? Pat { get; set; }

    [TypeProperty("Default branch name to set after creation (e.g. 'refs/heads/main'). If omitted, Azure DevOps sets one after first push.")]
    public string? DefaultBranch { get; set; }

    // Outputs
    [TypeProperty("Repository id (GUID)")]
    public string? RepositoryId { get; set; }

    [TypeProperty("Repository web URL")]
    public string? WebUrl { get; set; }

    [TypeProperty("HTTPS clone URL")]
    public string? RemoteUrl { get; set; }

    [TypeProperty("SSH clone URL")]
    public string? SshUrl { get; set; }
}