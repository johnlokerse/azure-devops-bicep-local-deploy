using Azure.Bicep.Types.Concrete;

namespace DevOpsExtension.PipelineRun;

public class AzureDevOpsPipelineRunIdentifiers
{
    [TypeProperty("Azure DevOps organization name (e.g. 'myorg') or full https://dev.azure.com/{org} URL", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Organization { get; set; }

    [TypeProperty("Azure DevOps project name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string Project { get; set; }

    [TypeProperty("Pipeline ID or name", ObjectTypePropertyFlags.Identifier | ObjectTypePropertyFlags.Required)]
    public required string PipelineId { get; set; }
}

[BicepDocHeading("AzureDevOpsPipelineRun", "Represents an Azure DevOps pipeline run.")]
[BicepDocExample(
    "Triggering a pipeline on a specific branch",
    "This example shows how to trigger a pipeline on a specific branch.",
    @"resource pipelineRun 'AzureDevOpsPipelineRun' = {
  organization: 'myorg'
  project: 'myproject'
  pipelineId: '42'
  branch: 'refs/heads/main'
}
")]
[BicepDocExample(
    "Triggering a pipeline with variables and parameters",
    "This example demonstrates triggering a pipeline with custom variables and template parameters passed as JSON strings.",
    @"resource pipelineRun 'AzureDevOpsPipelineRun' = {
  organization: 'myorg'
  project: 'myproject'
  pipelineId: '42'
  branch: 'refs/heads/feature/my-feature'
  variables: '{""MyVar1"": ""value1"", ""MyVar2"": ""value2""}'
  templateParameters: '{""Param1"": ""value1"", ""Param2"": ""value2""}'
}
")]
[BicepDocExample(
    "Triggering a pipeline on a tag",
    "This example shows how to trigger a pipeline on a specific tag.",
    @"resource pipelineRun 'AzureDevOpsPipelineRun' = {
  organization: 'myorg'
  project: 'myproject'
  pipelineId: '42'
  tag: 'refs/tags/v1.0.0'
}
")]
[ResourceType("AzureDevOpsPipelineRun")]
public class AzureDevOpsPipelineRun : AzureDevOpsPipelineRunIdentifiers
{
    [TypeProperty("Branch to trigger the pipeline on (e.g., 'refs/heads/main' or 'main'). Either branch or tag must be specified.")]
    public string? Branch { get; set; }

    [TypeProperty("Tag to trigger the pipeline on (e.g., 'refs/tags/v1.0.0' or 'v1.0.0'). Either branch or tag must be specified.")]
    public string? Tag { get; set; }

    [TypeProperty("Template parameters to pass to the pipeline as JSON string")]
    public string? TemplateParameters { get; set; }

    [TypeProperty("Variables to pass to the pipeline as JSON string")]
    public string? Variables { get; set; }

    [TypeProperty("[OUTPUT] Run ID of the triggered pipeline", ObjectTypePropertyFlags.ReadOnly)]
    public int RunId { get; set; }

    [TypeProperty("[OUTPUT] State of the pipeline run (e.g., 'completed', 'inProgress')", ObjectTypePropertyFlags.ReadOnly)]
    public string? State { get; set; }

    [TypeProperty("[OUTPUT] Result of the pipeline run (e.g., 'succeeded', 'failed')", ObjectTypePropertyFlags.ReadOnly)]
    public string? Result { get; set; }

    [TypeProperty("[OUTPUT] URL to the pipeline run in Azure DevOps", ObjectTypePropertyFlags.ReadOnly)]
    public string? Url { get; set; }
}
