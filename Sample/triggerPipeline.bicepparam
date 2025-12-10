using 'triggerPipeline.bicep'

param parOrganization = '<string>'
param parProject = '<string>'
param parPipelineId = '<string>'
param parPipelineBranch = '<string>'
param parTemplateParameters = string({
  Key: 'value'
})
