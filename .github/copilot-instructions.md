# Copilot — Project Instructions (Azure DevOps Bicep Local-Deploy extension)

You are contributing to an experimental Azure Bicep **local-deploy** extension that configures Azure DevOps via its REST API. Primary languages: C# (.NET) for the extension, Bicep for usage samples.

## Goals

- Generate **production-quality C#** code that follows Microsoft’s C# coding conventions.
- Generate Bicep **local-deploy** examples that are minimal, correct, and secure.
- Prefer secure auth patterns (Workload Identity) over PATs. Never echo tokens or secrets.

## Guardrails

- **Never output secrets** (PATs, tokens) in code, logs, or Bicep outputs.
- Respect repo structure. If adding a new resource handler, follow existing patterns (e.g., `AzureDevOpsProject`, `AzureDevOpsRepository`) and keep names consistent.
- Respect the screaming architecture folder structure (e.g., `src/Project/`, `src/Repository/`).

## Behavior patterns per handler

- Project: `AzureDevOpsProjectHandler`
  - Creates project, polls `operations/{id}` until `succeeded`, then polls project until `state == wellFormed` before proceeding. Updates description via PATCH when changed.
- Repository: `AzureDevOpsRepositoryHandler`
  - Validates project exists, then creates repo under `/{org}/{project}/_apis/git/repositories`.
- Artifact Feed: `AzureDevOpsArtifactFeedHandler`
  - Uses `feeds.dev.azure.com` endpoints; supports org- or project-scoped feeds; optional upstream sources; looks up project id when project is specified.
  - **Implements Get()** for `@onlyIfNotExists()` support — returns properties with identifiers but null outputs when feed doesn't exist.
- Service Connection: `AzureDevOpsServiceConnectionHandler`
  - Creates AzureRM connection using Workload Identity Federation (requires `clientId`, `tenantId`; scope either Subscription or Management Group). Optionally grants "all pipelines" permissions. Computes OIDC `issuer` and `subjectIdentifier` via `_apis/connectiondata`.
- Permission: `AzureDevOpsPermissionHandler`
  - Imports Entra ID groups into Azure DevOps Graph, resolves project role groups by name, ensures membership. Uses pagination for Graph API list operations with continuation tokens.
- Extension: `AzureDevOpsExtensionHandler`
  - Installs/updates marketplace extensions at organization level. Uses `extmgmt.dev.azure.com` endpoints. Idempotent: if version matches, no action taken.

## Handler method details

### Preview vs CreateOrUpdate vs Get

| Method           | Request Type       | Purpose                                                                       | Returns                            | When Called                                 |
| ---------------- | ------------------ | ----------------------------------------------------------------------------- | ---------------------------------- | ------------------------------------------- |
| `Preview`        | `ResourceRequest`  | Look up existing resource, populate outputs for read-only operations          | `GetResponse(request)`             | Bicep `what-if` or preview mode             |
| `CreateOrUpdate` | `ResourceRequest`  | Idempotent create (or update small properties if supported); populate outputs | `GetResponse(request)`             | Bicep deployment/provisioning               |
| `Get`            | `ReferenceRequest` | **(OPTIONAL)** Check resource existence for `@onlyIfNotExists()` decorator    | `GetResponse(request, properties)` | Bicep conditional resource existence checks |
| `GetIdentifiers` | N/A (in-memory)    | Extract identifier object from properties                                     | `TIdentifiers` instance            | Framework calls internally                  |

## New resource types (pattern)

When adding a new Azure DevOps concept (e.g., boards area path, pipeline permissions):
1) Create a new `FeatureX` directory
2) Create `src/FeatureX/AzureDevOps<Thing>.cs` and `src/FeatureX/AzureDevOps<Thing>Handler.cs` to match the existing folder structure (e.g., `src/Project/`, `src/Repository/`).
3) Implement idempotent **Create/Update** (GET to detect; POST/PUT/PATCH accordingly).
4) Map only necessary fields first; keep request/response models tight and well-named.
5) Update docs and the `Sample/` usage if relevant.
  To update docs, add `BicepDocHeading`, `BicepDocExample`, and `BicepDocCustom` attributes to the resource class. And run command: `bicep-local-docgen generate --force`

## PR/commit hygiene
- Commit messages: imperative mood, concise title + short body with rationale or links.
- PR description should list: problem, approach, risks, test notes, sample updates.