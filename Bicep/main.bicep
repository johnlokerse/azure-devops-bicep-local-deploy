targetScope = 'local'
extension azuredevopsextension

@description('Azure DevOps organization name (short org slug, not full URL).')
param organization string
@description('Project name')
param projectName string
@description('Optional project description')
param projectDescription string = ''
@allowed([
  'Private'
  'Public'
])
param visibility string = 'Private'
@description('Process name (Agile, Scrum, Basic, CMMI)')
param processName string = 'Agile'
@allowed([
  'Git'
  'Tfvc'
])
param sourceControl string = 'Git'
@secure()
@description('Azure DevOps PAT (leave empty to use AZDO_PAT environment variable).')
param pat string = ''

resource project 'AzureDevOpsProject' = {
  name: projectName
  organization: organization
  description: empty(projectDescription) ? null : projectDescription
  visibility: visibility
  processName: processName
  sourceControlType: sourceControl
  pat: empty(pat) ? null : pat
}

output projectId string = project.projectId
output projectState string = project.state
output projectUrl string = project.url
