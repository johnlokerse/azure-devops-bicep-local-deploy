targetScope = 'local'
extension azuredevops

@description('Azure DevOps organization name (short org slug, not full URL).')
param organization string

@description('Project name')
param projectName string

@description('Client ID of the service principal')
param clientId string

@description('Subscription ID')
param subscriptionId string

@description('Subscription name')
param subscriptionName string

@description('Tenant ID')
param tenantId string

// Make sure the principal has permissions to the subscription or management group
resource serviceConnection 'AzureDevOpsServiceConnection' = {
  name: 'mysvc'
  organization: organization
  project: projectName
  grantAllPipelines: true
  description: 'Service connection for Azure resources'
  clientId: clientId
  subscriptionId: subscriptionId
  subscriptionName: subscriptionName
  tenantId: tenantId
}
