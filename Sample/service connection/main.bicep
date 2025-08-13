targetScope = 'local'
extension azuredevops

@description('Azure DevOps organization name (short org slug, not full URL).')
param organization string

@description('Project name')
param projectName string


resource serviceConnection 'AzureDevOpsServiceConnection' = {
  name: 'mysvc'
  organization: organization
  project: projectName
  grantAllPipelines: true
  description: 'Service connection for Azure resources'
  clientId: ''
  subscriptionId: ''
  subscriptionName: ''
  tenantId: ''
}
