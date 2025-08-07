using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Bicep.Local.Extension.Types.Attributes;

namespace DevOpsExtension.Models;

public enum ProjectVisibility
{
    Private,
    Public
}

public class AzureDevOpsProjectIdentifiers
{
    [TypeProperty("The Azure DevOps project name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Name { get; set; }
}

[ResourceType("AzureDevOpsProject")] // exposed to Bicep as resource type name
public class AzureDevOpsProject : AzureDevOpsProjectIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Project description")]
    public string? Description { get; set; }

    [TypeProperty("Project visibility (Private/Public)")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectVisibility? Visibility { get; set; } = ProjectVisibility.Private;

    [TypeProperty("Process name to use (e.g. Agile, Scrum, Basic, CMMI)")]
    public string? ProcessName { get; set; } = "Agile";

    [TypeProperty("Source control type (Git or Tfvc)")]
    public string? SourceControlType { get; set; } = "Git";

    [TypeProperty("Personal Access Token (PAT) for Azure DevOps with appropriate scopes. If omitted, environment variable AZDO_PAT is used.")]
    public string? Pat { get; set; }

    // Outputs
    [TypeProperty("Project id (GUID)")]
    public string? ProjectId { get; set; }

    [TypeProperty("Project state")]
    public string? State { get; set; }

    [TypeProperty("Project web URL")]
    public string? Url { get; set; }
}