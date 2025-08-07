using 'main.bicep'

param organization = 'john-lokerse'
param projectName = 'SampleProjectFromBicep'
param projectDescription = 'Created via Bicep local extension quickstart'
param visibility = 'Private'
param processName = 'Agile'
param sourceControl = 'Git'
// NOTE: Prefer setting AZDO_PAT env var instead of inlining this value.
param pat = ''

param repositoryName = 'FirstRepo'
