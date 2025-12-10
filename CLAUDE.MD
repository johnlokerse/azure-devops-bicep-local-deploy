# CLAUDE.MD

This file provides context and instructions specifically for Claude Code when working on the Azure DevOps Bicep Local-Deploy extension.

## Project Overview

This is an **experimental** Azure Bicep local-deploy extension that configures Azure DevOps via its REST API. The extension allows users to define Azure DevOps resources (projects, repositories, artifact feeds, service connections, permissions, extensions, work items, and pipeline runs) in Bicep templates and deploy them locally.

**Primary languages:**
- **C# (.NET 9)** for the extension implementation
- **Bicep** for usage samples and templates

**Architecture:** Screaming architecture with feature-based folders (e.g., `src/Project/`, `src/Repository/`, `src/ArtifactFeed/`)

**Key documentation:**
- Azure Bicep local-deploy framework: https://techcommunity.microsoft.com/blog/azuregovernanceandmanagementblog/create-your-own-bicep-local-extension-using-net/4439967
- Create your own custom extension for Azure Bicep: https://johnlokerse.dev/2025/10/20/create-your-own-custom-extension-for-azure-bicep/
- C# coding conventions: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- Azure DevOps REST API: https://learn.microsoft.com/en-us/rest/api/azure/devops/

## Claude Code Workflow

### Initial Context Gathering
When starting a new task:
1. **Read existing implementations** - Use the Read tool to understand current patterns
2. **Search for similar code** - Use Grep to find related functionality
3. **Explore the architecture** - Use the Task tool with subagent_type=Explore for open-ended questions about codebase structure

### Task Planning
For complex tasks (adding new resource handlers, refactoring, etc.):
1. Use TodoWrite to break down the task into actionable steps
2. Mark tasks as in_progress before starting work
3. Mark tasks as completed immediately after finishing each step
4. Update the todo list if new subtasks are discovered

### Code Changes
1. **Always read before editing** - Never propose changes to files you haven't read
2. **Use specialized tools** - Prefer Read/Edit/Write over bash commands for file operations
3. **Run commands in parallel** - When commands are independent, execute them simultaneously
4. **Test incrementally** - Build and test after each significant change

## Build & Test Commands

### Building the Extension
```bash
dotnet build src/azure-devops-bicep-local.sln
```

### Publishing for Local Testing
```powershell
./Infra/Scripts/Publish-Extension.ps1 -Target ./src/bin/azure-devops-extension
```
Run this from the same directory as the `.csproj` file.

### Testing with Bicep
```bash
bicep local-deploy Sample/main.bicepparam
```

### Generating Documentation
```bash
bicep-local-docgen generate --force
```

Run this after adding or modifying resource documentation attributes.

## Code Style Guidelines

### C# Conventions
- Follow **Microsoft C# coding conventions**
- Use **production-quality** code standards
- Prefer `async/await` for I/O operations
- Use meaningful, descriptive variable names - **avoid abbreviations**:
  - Use `cancellationToken` instead of `ct`
  - Use `properties` instead of `props`
  - Use `organization` instead of `org`
  - Use `response` instead of `resp`
  - Use `errorMessage` instead of `err`
  - Use `exception` instead of `ex`
  - No single-letter variables except loop counters (i, j, k)
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

When adding a new Azure DevOps resource type:

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

### 4. Reference Implementations

**Best examples to reference:**
- `src/Project/AzureDevOpsProjectHandler.cs` - Polling, state management
- `src/ArtifactFeed/AzureDevOpsArtifactFeedHandler.cs` - Get() implementation for @onlyIfNotExists()
- `src/Permission/AzureDevOpsPermissionHandler.cs` - Pagination with continuation tokens
- `src/PipelineRun/AzureDevOpsPipelineRunHandler.cs` - Pipeline triggering, multiple API calls in sequence

### 5. Documentation
After creating a new handler:
1. Add `BicepDocHeading`, `BicepDocExample`, and `BicepDocCustom` attributes to the resource class
2. Run: `bicep-local-docgen generate --force`
3. Update `Sample/` folder with usage example

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

## Git Operations

### Creating Commits
Only create commits when explicitly requested. When asked:

1. Run in parallel:
   - `git status` - Check untracked files
   - `git diff` - Review staged and unstaged changes
   - `git log --oneline -5` - Review recent commit message style

2. Draft commit message:
   - Use imperative mood (e.g., "Add work item handler")
   - Keep title under 50 characters
   - Add body with rationale if needed
   - End with Claude Code signature

3. Stage and commit:
   ```bash
   git add <relevant-files> && git commit -m "$(cat <<'EOF'
   Your commit message here.
   EOF
   )"
   ```

### Creating Pull Requests
When asked to create a PR:

1. Run in parallel:
   - `git status`
   - `git diff main...HEAD` - Review all changes from base branch
   - `git log --oneline main..HEAD` - Review all commits

2. Draft PR description with:
   - **Summary:** Bullet points of changes
   - **Test plan:** How to test the changes

3. Push and create PR:
   ```bash
   gh pr create --title "Your PR title" --body "$(cat <<'EOF'
   ## Summary
   - First change
   - Second change

   ## Test plan
   - [ ] Build succeeds
   - [ ] Tests pass
   - [ ] Manual testing steps

   EOF
   )"
   ```

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
├── AGENTS.md               # General AI agent instructions
└── CLAUDE.MD               # This file (Claude Code specific)
```

## Common Development Patterns

### Polling Pattern
Many Azure DevOps operations require polling:
```csharp
// Check operation status until completion
while (operationStatus != "succeeded")
{
    await Task.Delay(1000);
    operationStatus = await GetOperationStatus(operationId);
}
```

### Pagination Pattern
Graph API requires continuation token handling:
```csharp
string? continuationToken = null;
do
{
    var response = await GetPagedResults(continuationToken);
    continuationToken = response.ContinuationToken;
    // Process results
} while (!string.IsNullOrEmpty(continuationToken));
```

### Scoping Pattern
Resources can be org-scoped or project-scoped:
- **Org-scoped:** Extensions, some artifact feeds
- **Project-scoped:** Repositories, work items, project-scoped feeds

## Testing Workflow

After implementing changes:

1. **Build:**
   ```bash
   dotnet build src/azure-devops-bicep-local.sln
   ```

2. **Publish locally:**
   ```powershell
   ./Infra/Scripts/Publish-Extension.ps1 -Target ./src/bin/azure-devops-extension
   ```

3. **Test with sample:**
   ```bash
   bicep local-deploy Sample/main.bicepparam
   ```

4. **Verify in Azure DevOps:**
   Check that resources were created/updated correctly

### Testing Checklist
- [ ] Resource creates successfully on first run
- [ ] Idempotent: Running again doesn't fail or duplicate
- [ ] Updates work when properties change
- [ ] Error messages are clear and actionable
- [ ] No secrets logged or exposed
- [ ] Documentation generated correctly

## Resources & Links

- **Framework quickstart:** https://techcommunity.microsoft.com/blog/azuregovernanceandmanagementblog/create-your-own-bicep-local-extension-using-net/4439967
- **Azure DevOps REST API:** https://learn.microsoft.com/en-us/rest/api/azure/devops/
- **C# Coding Conventions:** https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- **Custom Bicep extension guide:** https://johnlokerse.dev/2025/10/20/create-your-own-custom-extension-for-azure-bicep/
- **Bicep Documentation:** https://learn.microsoft.com/en-us/azure/azure-resource-manager/bicep/

## Claude Code Best Practices

### Tool Usage
- **Use Task tool for exploration** - Don't use Grep/Glob directly for open-ended questions about codebase structure
- **Read before editing** - Always use Read tool before Edit or Write
- **Parallel execution** - Run independent commands simultaneously for efficiency
- **Specialized tools over bash** - Prefer Read/Edit/Write over cat/sed/echo

### Avoiding Over-Engineering
- Only make changes directly requested or clearly necessary
- Don't add features, refactoring, or "improvements" beyond what was asked
- Don't add error handling for scenarios that can't happen
- Don't create abstractions for one-time operations
- Keep solutions simple and focused

### Communication
- Be concise - responses display in CLI
- Use GitHub-flavored markdown for formatting
- Output text directly - never use bash echo or comments to communicate
- Include file references with line numbers: `file_path:line_number`

### Context Management
- This is a git worktree at: `/Users/john/.claude-worktrees/azure-devops-bicep-local/beautiful-kirch`
- Main repository: `/Users/john/Documents/Repositories/azure-devops-bicep-local`
- Current branch: `beautiful-kirch`
- Main branch for PRs: `main`
