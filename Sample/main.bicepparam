using 'main.bicep'

// Azure DevOps project
param organization = '<string>'
param projectName = 'SampleProjectCreatedByBicep'
param projectDescription = 'Created via Bicep local extension quickstart'
param visibility = 'Private'
param processName = 'Agile'
param sourceControl = 'Git'

// Azure Repos repository
param repositoryName = 'FirstRepoCreatedByBicep'

// Azure Artifacts feed
param artifactName = 'FirstFeedCreatedByBicep'

// Entra Group role assignment
param entraGroupObjectId = '<guid>' // optional, remove if not needed
param azureDevOpsRole = 'Readers' // optional, remove if not needed

// Service connection
param clientId = '<guid>' // Application (client) ID of the service principal
param subscriptionId = '<guid>' // Subscription ID of the Azure subscription
param subscriptionName = '<string>'
param tenantId = '<guid'>

// Azure DevOps extensions to install
param extensions = [
  {
    publisherName: 'ms'
    extensionName: 'vss-code-search'
    version: '20.263.0.848933653'
  }
]
