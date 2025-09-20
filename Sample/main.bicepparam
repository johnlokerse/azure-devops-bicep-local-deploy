using 'main.bicep'

param organization = '<add-your-organization-here>'
param projectName = 'SampleProjectFromBicep'
param projectDescription = 'Created via Bicep local extension quickstart'
param visibility = 'Private'
param processName = 'Agile'
param sourceControl = 'Git'

param repositoryName = 'FirstRepo'

param artifactName = 'FirstFeed'

param entraGroupObjectId = '<add-your-group-object-id-here>' // optional, remove if not needed
param azureDevOpsRole = 'Readers' // optional, remove if not needed
