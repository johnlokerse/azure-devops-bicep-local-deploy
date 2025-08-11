# Azure DevOps Bicep Local Extension (Experimental)

This project demonstrates a custom Bicep Local Extension that can create Azure DevOps configuration via the Azure DevOps REST API using Bicep.

> [!NOTE]
> This is an experimental Bicep feature and is subject to change. Do not use it in production.

> [!NOTE]
> Community contributions are welcome!

## Current Capabilities

Experimental / sample only. Limited functionality for now:

- **Create** Azure DevOps Project
- **Create** Azure DevOps Repos

See the [Sample](./Sample/main.bicep) folder for an example Bicep template.

## Prerequisites

- .NET 9 SDK
- Bicep CLI v0.37.4+ (for `local-deploy`)

## How to use it locally or via an Azure Container Registry (ACR)

Here are the steps to run it either locally or using an ACR.

### Local build

Run script `Publish-Extension.ps1` from the folder [Infra/Scripts/](./Infra/Scripts/) to publish the project and to publish the extension locally for Bicep to use:

```powershell
./Infra/Scripts/Publish-Extension.ps1 -Target ./azure-devops-extension
```

This creates the binary that contains the Azure DevOps API calls. Prepare your `bicepconfig.json` to refer to the binary. Set `experimentalFeaturesEnabled` -> `localDeploy` to `true` and refer the extension `azuredevops` to the binary:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true
  },
  "extensions": {
    "azuredevops": "<path-to-binary>/azure-devops-extension" // local
  },
  "implicitExtensions": []
}
```

Run `bicep local-deploy main.bicepparam` to test the extension locally. Also, see the example in the [Sample](./Sample/) folder.

### Azure Container Registry build

If you want to make use of an Azure Container Registry then I would recommend to fork the project, and run the GitHub Actions. Or, run the [Bicep template](./Infra/main.bicep) for the ACR deployment locally and then push it using the same principal

```powershell
[string] $target = "br:<registry-name>.azurecr.io/extensions/azuredevops:<version>"

./Infra/Scripts/Publish-Extension.ps1 -Target $target
```

In the `bicepconfig.json` you refer to the ACR:

```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true
  },
  "extensions": {
    "azuredevops": "br:<registry-name>.azurecr.io/extensions/azuredevops:<version>" // ACR
  },
  "implicitExtensions": []
}
```

## Public ACR

If you want to try it out without effort, then you can use `br:azuredevopsbicep.azurecr.io/extensions/azuredevops:0.1.4` as the ACR reference.

## Bicep Usage Example

```bicep
targetScope = 'local'
extension azuredevops

param organization string
param projectName string
param pat string
param repositoryName string

resource project 'AzureDevOpsProject' = {
  name: projectName
  organization: organization
  pat: pat
}

resource repository 'AzureDevOpsRepository' = {
  name: repositoryName
  organization: organization
  project: project.name
  pat: pat
}

output id string = project.projectId
```

### Separate Repository Deployment

Create a repository in an existing project using a separate deployment:

`Bicep/repository.bicep`:

```bicep
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

resource project 'AzureDevOpsProject' = {
  name: projectName
  organization: organization
  description: empty(projectDescription) ? null : projectDescription
  visibility: visibility
  processName: processName
  sourceControlType: sourceControl
}

output projectId string = project.projectId
output projectState string = project.state
output projectUrl string = project.url
```

Deploy:

```bash
bicep local-deploy Bicep/repository.bicepparam
```

## Security

Prefer environment variable over passing PAT as a property. Secrets in parameters can leak into logs. Never commit real PATs. Exploring ways to use Workload Identity Federation instead of PAT.

Export your PAT to avoid putting secrets in files:

```bash
export AZDO_PAT="<your pat>"
```

## Disclaimer

Sample only – not an official Microsoft supported extension. Use at your own risk.
