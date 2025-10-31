---
applyTo: "**/*.bicep,**/*.bicepparam"
description: "Bicep local-deploy usage patterns"
---

# Bicep Local-Deploy Rules
- Always set `targetScope = 'local'` and declare `extension azuredevops`.
- Keep templates minimal; pass values via `param` blocks and `.bicepparam` files.
- **Never** output or log secrets (PATs, tokens). Avoid linter suppressions for secret outputs.
- Avoid using Personal Access Tokens (PATs) in favor of Azure Entra authentication when possible.

## Example skeleton
```bicep
targetScope = 'local'
extension azuredevops

param organization string
param projectName string

resource project 'AzureDevOpsProject' = {
  name: projectName
  organization: organization
}

output projectId string = project.projectId
```