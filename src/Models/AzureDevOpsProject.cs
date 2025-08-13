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

    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }
}

[ResourceType("AzureDevOpsProject")] // exposed to Bicep as resource type name
public class AzureDevOpsProject : AzureDevOpsProjectIdentifiers
{
    [TypeProperty("Project description")]
    public string? Description { get; set; }

    [TypeProperty("Project visibility (Private/Public)")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ProjectVisibility? Visibility { get; set; } = ProjectVisibility.Private;

    [TypeProperty("Process name to use (e.g. Agile, Scrum, Basic, CMMI)")]
    public string? ProcessName { get; set; } = "Agile";

    [TypeProperty("Source control type (Git or Tfvc)")]
    public string? SourceControlType { get; set; } = "Git";

    // Outputs
    [TypeProperty("[OUTPUT] Project id (GUID)")]
    public string? ProjectId { get; set; }

    [TypeProperty("[OUTPUT] Project state")]
    public string? State { get; set; }

    [TypeProperty("[OUTPUT] Project web URL")]
    public string? Url { get; set; }
}