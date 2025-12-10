targetScope = 'local'
extension azuredevops

param parOrganization string
param parProject string
param parPipelineId string
param parPipelineBranch string
param parTemplateParameters string

resource pipelineRun 'AzureDevOpsPipelineRun' = {
  organization: parOrganization
  project: parProject
  pipelineId: parPipelineId
  branch: parPipelineBranch
  templateParameters: parTemplateParameters
}
