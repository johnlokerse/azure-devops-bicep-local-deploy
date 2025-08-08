targetScope = 'subscription'

@description('Name of the Azure Container Registry for the Azure Bicep extension')
param parName string

@description('Resource Group name')
param parResourceGroupName string

@description('SKU for the Azure Container Registry')
@allowed([
  'Standard'
  'Premium'
])
param parSku string

@description('Location for the Azure Container Registry and Resource Group')
param parLocation string = resourceGroup().location

resource resResourceGroup 'Microsoft.Resources/resourceGroups@2025-04-01' = {
  name: parResourceGroupName
  location: parLocation
}

module modAzureContainerRegistry 'br/public:avm/res/container-registry/registry:0.9.1' = {
  scope: resResourceGroup
  params: {
    name: parName
    location: resResourceGroup.location
    acrSku: parSku
    acrAdminUserEnabled: false
    anonymousPullEnabled: true
  }
}
