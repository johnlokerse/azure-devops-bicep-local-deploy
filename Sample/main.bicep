targetScope = 'local'
extension azuredevops with {
  accessToken: pat
}

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
param pat string?

@description('Repository name')
param repositoryName string

resource project 'AzureDevOpsProject' = {
  name: projectName
  organization: organization
  description: empty(projectDescription) ? null : projectDescription
  visibility: visibility
  processName: processName
  sourceControlType: sourceControl
}

resource repository 'AzureDevOpsRepository' = {
  name: repositoryName
  organization: organization
  project: project.name
}

output projectId string = project.projectId
output projectState string = project.state
output projectUrl string = project.url

output repositoryId string = repository.repositoryId
output repositoryWebUrl string = repository.webUrl
output repositoryRemoteUrl string = repository.remoteUrl
output repositorySshUrl string = repository.sshUrl
