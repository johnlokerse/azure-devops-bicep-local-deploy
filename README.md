# Azure DevOps Bicep Local Extension (Experimental)

This project demonstrates a custom Bicep Local Extension (C# / .NET 9) that can create (and idempotently ensure) an Azure DevOps Project via the Azure DevOps REST API.

## Status
Experimental / sample only. Limited functionality: create project if missing, update description if changed. No delete support, no advanced updates.

## Prerequisites
* .NET 9 SDK
* Bicep CLI v0.37.4+ (for `local-deploy` + local extensions)
* Azure DevOps Personal Access Token (PAT) with at minimum: `Project and Team (Read, Write, & Manage)` scope.

Export your PAT to avoid putting secrets in files:
```bash
export AZDO_PAT="<your pat>"
```

## Build & Publish Locally
Publish self-contained binaries for the three supported runtimes (adjust for your OS/arch as needed):
```bash
dotnet publish --configuration Release -r osx-arm64 .
dotnet publish --configuration Release -r linux-x64 .
dotnet publish --configuration Release -r win-x64 .

bicep publish-extension --bin-osx-arm64 ./bin/Release/osx-arm64/publish/azure-devops-extension --bin-linux-x64 ./bin/Release/linux-x64/publish/azure-devops-extension --bin-win-x64 ./bin/Release/win-x64/publish/azure-devops-extension.exe --target ./bin/azure-devops-extension --force
```

`bicepconfig.json` is already configured to reference `./bin/azure-devops-extension`.

## Deploy (Local Execution)
Populate `main.bicepparam` or override parameters on the command line, then:
```bash
bicep local-deploy main.bicepparam
```

Expected output (example):
```
Output projectId: 00000000-0000-0000-0000-000000000000
Output projectState: wellFormed
Output projectUrl: https://dev.azure.com/myorganization/_apis/projects/00000000-0000-0000-0000-000000000000
Resource project (Create): Succeeded
Result: Succeeded
```

## Bicep Usage Example
```bicep
targetScope = 'local'
extension azuredevopsextension

param organization string
param projectName string
param pat string

resource project 'AzureDevOpsProject' = {
  name: projectName
  organization: organization
  pat: pat
}

output id string = project.projectId
```

## First Implementation Steps
1. Read the Bicep local extension quickstart (done / mirrored here).
2. Scaffold the .NET project (`DevOpsExtension.csproj`, `Program.cs`).
3. Define resource model (`AzureDevOpsProject`) and identifiers.
4. Implement handler (`AzureDevOpsProjectHandler`) with Preview + CreateOrUpdate using Azure DevOps REST API.
5. Create Bicep configuration & sample templates (`bicepconfig.json`, `main.bicep`, `main.bicepparam`).
6. Acquire PAT & export `AZDO_PAT` env var.
7. Publish extension binaries with `dotnet publish` and `bicep publish-extension`.
8. Execute `bicep local-deploy main.bicepparam` to test.
9. Iterate: add advanced properties (teams, repos, policies) as needed.

## Security Notes
Prefer environment variable over passing PAT as a property. Secrets in parameters can leak into logs. Never commit real PATs.

## Next Enhancements (Ideas)
* Add support for creating default team & repo settings.
* Add deletion (requires design choice – currently "Create only").
* Expand output (default repo id, web URLs).
* Parameter validation & richer error messages.
* Unit tests with mocked HTTP handler.

## Disclaimer
Sample only – not an official Microsoft supported extension. Use at your own risk.
# azure-devops-bicep-local
