targetScope = 'local'
// Note: No accessToken provided -> uses environment (AZ CLI login, managed identity, or federated identity) for Azure DevOps
extension azuredevops

@description('Azure DevOps organization name (short org slug, or full https://dev.azure.com/{org} URL).')
param organization string

@description('Project name')
param projectName string

@description('Azure Entra ID group objectId (GUID) to assign to the project role')
param entraGroupObjectId string

@allowed([
  'Readers'
  'Contributors'
])
@description('Project role to grant to the AAD group')
param role string = 'Readers'

// Assign an Entra group to a built-in project role
resource permission 'AzureDevOpsPermission' = {
  organization: organization
  project: projectName
  groupObjectId: entraGroupObjectId
  role: role
}

output assigned bool = permission.assigned
output groupDescriptor string = permission.groupDescriptor
output projectGroupDescriptor string = permission.projectGroupDescriptor
