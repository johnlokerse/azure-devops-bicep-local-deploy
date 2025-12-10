# AGENTS.md

This file provides context and instructions for AI coding agents working on the Azure DevOps Bicep Local-Deploy extension.

## Project Overview

This is an **experimental** Azure Bicep local-deploy extension that configures Azure DevOps via its REST API. The extension allows users to define Azure DevOps resources (projects, repositories, artifact feeds, service connections, permissions, extensions, work items, and pipeline runs) in Bicep templates and deploy them locally.

**Primary languages:**
- **C# (.NET 9)** for the extension implementation
- **Bicep** for usage samples and templates

**Architecture:** Screaming architecture with feature-based folders (e.g., `src/Project/`, `src/Repository/`, `src/ArtifactFeed/`)

**Key documentation:**
- Azure Bicep local-deploy framework: https://techcommunity.microsoft.com/blog/azuregovernanceandmanagementblog/create-your-own-bicep-local-extension-using-net/4439967
- C# coding conventions: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Azure DevOps REST API: https://learn.microsoft.com/en-us/rest/api/azure/devops/

## Setup Commands

**Prerequisites:**
- .NET 9 SDK
- Bicep CLI v0.37.4+ (with `localDeploy` experimental feature)

**Build and publish extension locally:**
```powershell
./Infra/Scripts/Publish-Extension.ps1 -Target ./src/bin/azure-devops-extension
```

**Configure Bicep to use the local extension:**
Edit `bicepconfig.json`:
```json
{
  "experimentalFeaturesEnabled": {
    "localDeploy": true
  },
  "extensions": {
    "azuredevops": "<path-to-binary>/azure-devops-extension"
  }
}
```

## Build & Test Commands

**Build the extension:**
```bash
dotnet build src/azure-devops-bicep-local.sln
```

**Publish for local testing:**
```powershell
./Infra/Scripts/Publish-Extension.ps1 -Target ./src/bin/azure-devops-extension
```
*Note: Run this from the same directory as the `.csproj` file*

**Test with Bicep:**
```bash
bicep local-deploy Sample/main.bicepparam
```

**Generate documentation:**
```bash
bicep-local-docgen generate --force
```

## Code Style Guidelines

### C# Conventions
- Follow **Microsoft C# coding conventions**
- Use **production-quality** code standards
- Prefer `async/await` for I/O operations
- Use meaningful variable names (no single-letter variables except loop counters)
- Keep methods focused and single-purpose
- Use dependency injection where appropriate

### Bicep Conventions
- Keep examples **minimal, correct, and secure**
- Prefer **Workload Identity Federation** over Personal Access Tokens (PATs)
- Never echo tokens or secrets in outputs
- Use `@secure()` decorator for sensitive parameters
- Document all parameters with descriptions

### Naming Conventions
- Resource types: `AzureDevOps<ResourceName>` (e.g., `AzureDevOpsProject`)
- Handlers: `AzureDevOps<ResourceName>Handler` (e.g., `AzureDevOpsProjectHandler`)
- Folders: Match the resource name (e.g., `src/Project/`, `src/Repository/`)

## Handler Development Pattern

When adding a new Azure DevOps resource type, follow this pattern:

### 1. Create Feature Directory
```
src/
└── <FeatureName>/
    ├── AzureDevOps<Thing>.cs          # Resource model
    └── AzureDevOps<Thing>Handler.cs   # Handler implementation
```

### 2. Implement Required Methods

Each handler must implement these methods:

| Method           | Request Type       | Purpose                                                           | Returns                            |
| ---------------- | ------------------ | ----------------------------------------------------------------- | ---------------------------------- |
| `Preview`        | `ResourceRequest`  | Look up existing resource, populate outputs for read-only ops     | `GetResponse(request)`             |
| `CreateOrUpdate` | `ResourceRequest`  | Idempotent create/update; populate outputs                        | `GetResponse(request)`             |
| `Get`            | `ReferenceRequest` | (OPTIONAL) Check existence for `@onlyIfNotExists()` decorator     | `GetResponse(request, properties)` |
| `GetIdentifiers` | N/A                | Extract identifier object from properties                         | `TIdentifiers` instance            |

### 3. Idempotent Operations
- **Always** GET first to check if resource exists
- Use POST for creation, PATCH/PUT for updates (based on API support)
- Handle 404 gracefully for non-existent resources
- Ensure operations can be safely retried

### 4. Handler Examples

**Project Handler:**
- Creates project, polls `operations/{id}` until `succeeded`
- Polls project until `state == wellFormed`
- Updates description via PATCH when changed

**Artifact Feed Handler:**
- Uses `feeds.dev.azure.com` endpoints
- Supports org-scoped or project-scoped feeds
- Implements `Get()` for `@onlyIfNotExists()` support
- Returns properties with identifiers but null outputs when feed doesn't exist

**Permission Handler:**
- Imports Entra ID groups into Azure DevOps Graph
- Resolves project role groups by name
- Uses pagination for Graph API list operations with continuation tokens

**Extension Handler:**
- Installs/updates marketplace extensions at organization level
- Uses `extmgmt.dev.azure.com` endpoints
- Idempotent: if version matches, no action taken

**Pipeline Run Handler:**
- Triggers Azure DevOps pipelines via POST request
- Supports triggering by branch or tag
- Accepts pipeline ID (number) or name (string)
- Passes variables and template parameters as objects
- Retrieves run details immediately after triggering

### 5. Documentation
After creating a new handler:
1. Add `BicepDocHeading`, `BicepDocExample`, and `BicepDocCustom` attributes to the resource class
2. Run: `bicep-local-docgen generate --force`
3. Update `Sample/` folder with usage example

## Testing Instructions

### Manual Testing
1. Build and publish the extension locally
2. Configure `bicepconfig.json` to point to the local binary
3. Create a test `.bicep` file with your resource
4. Run `bicep local-deploy <file>.bicepparam`
5. Verify the resource was created in Azure DevOps

### Testing Checklist
- [ ] Resource creates successfully on first run
- [ ] Idempotent: Running again doesn't fail or duplicate
- [ ] Updates work when properties change
- [ ] Error messages are clear and actionable
- [ ] No secrets logged or exposed
- [ ] Documentation generated correctly

## Security Considerations

### Critical Security Rules
- **NEVER output secrets** (PATs, tokens, API keys) in code, logs, or Bicep outputs
- Always use `@secure()` decorator for sensitive Bicep parameters
- Prefer **Workload Identity Federation** over Personal Access Tokens
- Validate all inputs before making API calls
- Use HTTPS for all API endpoints
- Never log full request/response bodies that might contain secrets

### Authentication Patterns
- **Preferred:** Azure Entra ID tokens via Workload Identity Federation
- **Fallback:** Personal Access Tokens (PATs) with minimal scopes
- When using PATs, remind users they're less secure than Entra tokens

## PR/Commit Guidelines

### Commit Messages
- Use **imperative mood** (e.g., "Add permission handler" not "Added permission handler")
- Keep title concise (50 chars or less)
- Include short body with rationale or links if needed
- Reference issue numbers when applicable

**Example:**
```
Add work item handler for Azure DevOps

Implements create/update for work items via REST API.
Supports custom fields and area/iteration paths.

Fixes #42
```

### Pull Request Description
Every PR should include:
- **Problem:** What issue does this solve?
- **Approach:** How does this PR solve it?
- **Risks:** Any breaking changes or edge cases?
- **Test notes:** How to test this change
- **Sample updates:** Any changes to the Sample/ folder?

### Code Review Focus
- Security: No secrets exposed
- Idempotency: Can operations be safely retried?
- Error handling: Clear error messages
- Documentation: Attributes and examples added
- Structure: Follows screaming architecture pattern

## Development Workflow

### Adding a New Resource Handler
1. Create feature directory: `src/<FeatureName>/`
2. Implement resource model and handler
3. Add documentation attributes
4. Update `Sample/` with usage example
5. Generate docs: `bicep-local-docgen generate --force`
6. Test locally: `bicep local-deploy Sample/main.bicepparam`
7. Create PR with description

### Debugging Tips
- Check Azure DevOps REST API docs for endpoint details
- Use Fiddler/Postman to test API calls directly
- Enable verbose logging in handler for troubleshooting
- Verify API responses match expected schema

### Common Patterns
- **Polling:** Many operations require polling until completion (projects, operations)
- **Pagination:** Graph API requires continuation token handling
- **Scoping:** Some resources are org-scoped, others are project-scoped
- **Prerequisites:** Some resources require others to exist first (e.g., repository needs project)

## Project Structure

```
azure-devops-bicep-local/
├── src/
│   ├── Project/              # Project handler
│   ├── Repository/           # Repository handler
│   ├── ArtifactFeed/        # Artifact feed handler
│   ├── ServiceConnection/   # Service connection handler
│   ├── Permission/          # Permission handler
│   ├── Extension/           # Extension handler
│   ├── WorkItem/            # Work item handler
│   └── PipelineRun/         # Pipeline run handler
├── Sample/                  # Example Bicep templates
│   ├── main.bicep
│   └── main.bicepparam
├── docs/                    # Generated documentation
├── Infra/                   # Infrastructure as code
│   └── Scripts/            # Build and publish scripts
└── AGENTS.md               # This file
```

## Resources & Links

- **Framework quickstart:** https://techcommunity.microsoft.com/blog/azuregovernanceandmanagementblog/create-your-own-bicep-local-extension-using-net/4439967
- **Azure DevOps REST API:** https://learn.microsoft.com/en-us/rest/api/azure/devops/
- **C# Coding Conventions:** https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Custom Bicep extension development guide: https://johnlokerse.dev/2025/10/20/create-your-own-custom-extension-for-azure-bicep/
- **Bicep Documentation:** https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/

## Notes for AI Agents

- **Context gathering:** Always read existing handler implementations before proposing changes
- **Consistency:** Follow patterns from existing handlers (especially `AzureDevOpsProjectHandler`, `AzureDevOpsArtifactFeedHandler`, and `AzureDevOpsPipelineRunHandler`)
- **API documentation:** Fetch latest Azure DevOps REST API docs before implementing new endpoints
- **Minimal edits:** Only modify files when explicitly required
- **Testing:** Always suggest testing steps after code changes
- **Breaking down tasks:** For complex features, create a todo list of discrete actionable tasks before starting
