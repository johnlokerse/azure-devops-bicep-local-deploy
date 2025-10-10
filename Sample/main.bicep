targetScope = 'local'
extension azuredevops

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

@description('Repository name')
param repositoryName string

@description('Artifact feed name')
param artifactName string

@description('Entra ID group objectId (GUID) to assign to the project role')
param entraGroupObjectId string?

@description('Project role to grant to the Entra group')
param azureDevOpsRole string?

@description('Client ID of the service principal')
param clientId string

@description('Subscription ID')
param subscriptionId string

@description('Subscription name')
param subscriptionName string

@description('Tenant ID')
param tenantId string

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

resource artifactFeed 'AzureDevOpsArtifactFeed' = {
  name: artifactName
  organization: organization
  project: project.name
}

resource readerPermission 'AzureDevOpsPermission' = if (!empty(entraGroupObjectId) && !empty(azureDevOpsRole)) {
  groupObjectId: entraGroupObjectId!
  organization: organization
  project: project.name
  role: azureDevOpsRole!
}

resource serviceConnection 'AzureDevOpsServiceConnection' = {
  name: 'my-first-service-connection'
  organization: organization
  project: project.name
  grantAllPipelines: true
  description: 'Service connection for Azure resources created by Bicep'
  clientId: clientId
  subscriptionId: subscriptionId
  subscriptionName: subscriptionName
  tenantId: tenantId
}


// Outputs
output projectId string = project.projectId
output projectState string = project.state
output projectUrl string = project.url

output repositoryId string = repository.repositoryId
output repositoryWebUrl string = repository.webUrl
output repositoryRemoteUrl string = repository.remoteUrl
output repositorySshUrl string = repository.sshUrl

output artifactFeedId string = artifactFeed.feedId
output artifactFeedUrl string = artifactFeed.url

output serviceConnectionIdentifier string = serviceConnection.subjectIdentifier
output serviceConnectionIssuer string = serviceConnection.issuer
