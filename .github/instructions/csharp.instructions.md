---
applyTo: "**/*.cs"
description: "C# guidelines for the Azure DevOps Bicep local-deploy extension"
---

# C# Guidelines (Authoritative Style)
- **Adopt Microsoft’s Common C# Code Conventions** as the canonical style reference for this project.
- Prefer modern C# features and clarity over cleverness; keep methods small and testable.
- Nullability: enabled; avoid `!` unless justified in a comment.
- Logging: never log secrets.
- Avoid abbreviations, for example in this context use 'request' instead of 'req' and 'cancellationToken' instead of 'ct':

```csharp
    protected override async Task<ResourceResponse> Preview(ResourceRequest req, CancellationToken ct)
    {
        // Implementation here
    }
```

# Documentation
- Keep names consistent with existing resources (e.g., `AzureDevOpsProject`, `AzureDevOpsRepository`).
- Document the ResourceTypes with `BicepDocHeading`, `BicepDocExample` and `BicepDocCustom`.