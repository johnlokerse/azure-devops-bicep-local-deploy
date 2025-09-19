# Copilot instructions for this repo

Purpose: a Bicep Local Extension that provisions Azure DevOps resources via REST APIs from Bicep templates. See `Sample/` for runnable examples.

## Architecture and key pieces

- Extension host: `src/Program.cs`
  - Registers the Bicep local extension and these resource handlers:
    - `AzureDevOpsProjectHandler`, `AzureDevOpsRepositoryHandler`, `AzureDevOpsArtifactFeedHandler`, `AzureDevOpsServiceConnectionHandler`.
  - `Configuration` type exposes `accessToken` to the extension (maps to PAT). Version is taken from the assembly.
- Resource contract
  - Models in `src/Models/*` define Bicep resource shapes using `[ResourceType("...")]` and `[TypeProperty]`.
  - Each handler derives from `AzureDevOpsResourceHandlerBase<TProps, TIdentifiers>` and implements:
    - `Preview(...)` — look up by identifiers and populate outputs on `request.Properties`.
    - `CreateOrUpdate(...)` — idempotent create; update small deltas if applicable; set outputs; `return GetResponse(request)`.
    - `GetIdentifiers(...)` — return the identifier object used by Bicep.
  - Always set outputs by writing to `request.Properties` before calling `GetResponse(...)`.
- HTTP/auth basics (see `AzureDevOpsResourceHandlerBase`):
  - `CreateClient` uses one of: `Configuration.AccessToken` → `AZDO_PAT` env var → Azure Entra token via `DefaultAzureCredential` for DevOps scope (`499b84ac-1321-427f-aa17-267ca6975798/.default`).
  - `GetOrgAndBaseUrl(org)` accepts either the short org slug (e.g., `myorg`) or full `https://dev.azure.com/{org}` and returns `(org, baseUrl)`; packaging uses `https://feeds.dev.azure.com`.
  - JSON via `System.Text.Json` with `JsonSerializerDefaults.Web` (`JsonOptions`). Use the provided `PatchAsync` helper for PATCH.

## Behavior patterns per handler

- Project: `AzureDevOpsProjectHandler`
  - Creates project, polls `operations/{id}` until `succeeded`, then polls project until `state == wellFormed` before proceeding. Updates description via PATCH when changed.
- Repository: `AzureDevOpsRepositoryHandler`
  - Validates project exists, then creates repo under `/{org}/{project}/_apis/git/repositories`. Retries transient provisioning gaps (e.g., `DataspaceNotFoundException`) with backoff.
- Artifact Feed: `AzureDevOpsArtifactFeedHandler`
  - Uses `feeds.dev.azure.com` endpoints; supports org- or project-scoped feeds; optional upstream sources; looks up project id when project is specified.
- Service Connection: `AzureDevOpsServiceConnectionHandler`
  - Creates AzureRM connection using Workload Identity Federation (requires `clientId`, `tenantId`; scope either Subscription or Management Group). Optionally grants "all pipelines" permissions. Computes OIDC `issuer` and `subjectIdentifier` via `_apis/connectiondata`.

## Developer workflows

- Prereqs: .NET 9 SDK, Bicep CLI v0.37.4+.
- Build & publish extension binaries (macOS, Linux, Windows) and publish:
  - PowerShell script: `Infra/Scripts/Publish-Extension.ps1 -Target <local path or br:...>`
  - Example local target: `./Infra/Scripts/Publish-Extension.ps1 -Target ./azure-devops-extension`
  - Example ACR target: `-Target "br:<registry>.azurecr.io/extensions/azuredevops:<version>"`
- Use in Bicep (local deploy):
  - Configure `bicepconfig.json` with the extension path or `br:` reference (see `Sample/bicepconfig.json`).
  - Example extension block: `extension azuredevops with { accessToken: pat }` or rely on `AZDO_PAT`/Entra.
  - Run: `bicep local-deploy Sample/main.bicepparam`.

## Conventions and gotchas

- Identifiers: Model classes mark identifier properties with `ObjectTypePropertyFlags.Identifier | Required` (e.g., `Organization`, `Project`, `Name`).
- Organization input may be either slug or full URL; prefer using `GetOrgAndBaseUrl` (or feed variant) to build endpoints.
- Outputs are exposed as properties on the model with labels like `[OUTPUT]` in comments; set them in handlers.
- Keep Azure DevOps API versions consistent with current handlers (e.g., projects `7.1-preview.4`, ops `7.1-preview.1`, git repos `7.1-preview.1`, feeds `7.2-preview.1`, service endpoint `7.1-preview.4`).
- Error handling: return `null` from "get" helpers on non-success to indicate not found; throw `InvalidOperationException` with response details on create/update failures.

## Extending the extension (new resource type)

- Add a model in `src/Models/` with `[ResourceType("YourType")]` and identifier properties.
- Implement a handler in `src/Handlers/` deriving from the base; implement `Preview`, `CreateOrUpdate`, `GetIdentifiers`.
- Register the handler in `src/Program.cs` with `.WithResourceHandler<YourHandler>()`.
- Follow existing patterns for auth, URL building, polling/retries, and outputs.

References: `Sample/main.bicep`, `Sample/federatedServiceConnection/main.bicep`, `README.md`.
