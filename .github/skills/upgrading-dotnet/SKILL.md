---
name: upgrading-dotnet
description: Upgrade .NET projects to newer versions. Use when upgrading TargetFramework in csproj/vbproj/fsproj files, migrating between .NET versions, configuring global.json, pinning SDK versions, updating CI/CD pipelines for .NET, or troubleshooting .NET upgrade issues.
---

# .NET Project Upgrade Skill

## When to Use This Skill

- Upgrading project target frameworks (e.g., `net6.0` → `net9.0`)
- Updating .NET SDK versions across projects
- Migrating multiple projects in a solution
- Version pinning and controlled rollouts
- Updating CI/CD pipelines for new .NET versions
- Preparing hosting environments for new runtimes

## Quick Start: Basic Upgrade Process

### 1. Identify Projects to Upgrade

List all project files that need upgrading:

```bash
find . -name "*.csproj" -o -name "*.vbproj" -o -name "*.fsproj"
```

### 2. Update Project Files (Minimum Required Change)

Open each project file (`.csproj`, `.vbproj`, or `.fsproj`) and update the `<TargetFramework>` property:

**Before:**

```xml
<PropertyGroup>
  <TargetFramework>net8.0</TargetFramework>
</PropertyGroup>
```

**After (upgrading to .NET 9):**

```xml
<PropertyGroup>
  <TargetFramework>net9.0</TargetFramework>
</PropertyGroup>
```

For multi-targeted projects, update `<TargetFrameworks>`:

```xml
<PropertyGroup>
  <TargetFrameworks>net8.0;net9.0</TargetFrameworks>
</PropertyGroup>
```

### 3. Restore Workloads

If using specialized workloads (MAUI, ASP.NET, etc.):

```bash
dotnet workload restore
```

### 4. Build and Test

```bash
dotnet build
dotnet test
```

The SDK will provide warnings and errors to guide further changes.

## Advanced: Version Pinning Strategies

### Pin SDK Version with global.json

Create or update `global.json` in your solution root to control which SDK version is used:

```bash
dotnet new globaljson --sdk-version 9.0.100 --roll-forward latestFeature
```

This ensures consistent builds across different machines and CI environments.

**File structure after:**

```text
solution-root/
├── global.json          (pins SDK version)
├── src/
│   ├── Project1/
│   └── Project2/
└── tests/
```

### Control Analyzer Behavior

Lock analyzer rules to a specific .NET version to prevent new warnings on upgrade:

```xml
<PropertyGroup>
  <AnalysisLevel>9.0</AnalysisLevel>
</PropertyGroup>
```

This is useful when upgrading gradually—prevents new analyzer rules from breaking builds before you're ready to address them.

### Manage Package Versions

#### Option A: Central Package Management (Recommended)

Create `Directory.Packages.props` in solution root:

```xml
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
  </PropertyGroup>

  <ItemGroup>
    <PackageVersion Include="Microsoft.Extensions.AI" Version="9.10.1" />
    <PackageVersion Include="Azure.Identity" Version="1.17.0" />
  </ItemGroup>
</Project>
```

In project files, reference without version:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.Extensions.AI" />
  <PackageReference Include="Azure.Identity" />
</ItemGroup>
```

#### Option B: Package Lock Files

Enable lock files to ensure reproducible restores:

```xml
<PropertyGroup>
  <RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
  <RestoreLockedMode>true</RestoreLockedMode>
</PropertyGroup>
```

Generate lock file:

```bash
dotnet restore
```

## CI/CD Pipeline Updates

Update your CI/CD pipeline to use the new SDK version. For GitHub Actions:

**Before:**

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '8.0'
```

**After:**

```yaml
- name: Setup .NET
  uses: actions/setup-dotnet@v4
  with:
    dotnet-version: '9.0'
```

## Multi-Project Upgrade Checklist

Copy this checklist and track progress:

```text
Upgrade Progress:
- [ ] Review breaking changes for target .NET version
- [ ] Update all .csproj files to new target framework
- [ ] Run dotnet workload restore if needed
- [ ] Build entire solution: dotnet build
- [ ] Run full test suite: dotnet test
- [ ] Update global.json if using version pinning
- [ ] Update CI/CD pipeline configuration
- [ ] Test in CI/CD environment
- [ ] Commit changes
```

## Breaking Changes and Migration

Check for breaking changes specific to your target version:

- [**.NET 9**](https://learn.microsoft.com/en-us/dotnet/core/compatibility/9.0)
- [**.NET 10**](https://learn.microsoft.com/en-us/dotnet/core/compatibility/10.0)
- [**ASP.NET Core**](https://learn.microsoft.com/en-us/aspnet/core/migration/)

## Hosting Environment Updates

### Docker

Update Dockerfile `FROM` statements:

```dockerfile
# Old
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# New
FROM mcr.microsoft.com/dotnet/aspnet:9.0
```

### Azure App Service

Configuration changes are required through Azure Portal or IaC (e.g., Bicep, Terraform).

### Linux Servers

Install new .NET runtime via package manager:

```bash
# Ubuntu/Debian
sudo apt-get update
sudo apt-get install -y dotnet-runtime-9.0
```

## Troubleshooting

### Validation Loop

1. Run `dotnet build`
2. If errors: review messages, fix issues, repeat step 1
3. Run `dotnet test`
4. If failures: fix tests, repeat step 3
5. **Only proceed when both pass**

### Build Fails After Update

1. Check error messages—SDK provides guidance
2. Review breaking changes for your target version
3. Update NuGet packages that may have new version requirements
4. Run `dotnet clean` and rebuild

### Tests Fail

- Run tests individually to isolate issues
- Check for API changes in updated packages
- Review test framework compatibility (e.g., xUnit, NUnit versions)

### CI/CD Pipeline Fails

- Verify new SDK version is available in CI environment
- Check `global.json` is in repo root (CI should respect it)
- Update any hardcoded version references in pipeline

## Key Resources

- [**.NET Official Upgrade Guide**](https://learn.microsoft.com/en-us/dotnet/core/install/upgrade)
- [**global.json Reference**](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)
- [**AnalysisLevel Property**](https://learn.microsoft.com/en-us/dotnet/core/project-sdk/msbuild-props#analysislevel)
- [**ASP.NET Core Migration**](https://learn.microsoft.com/en-us/aspnet/core/migration/)

## Examples by Scenario

### Scenario 1: Simple Single-Project Upgrade

For a single `.csproj` project:

1. Update `<TargetFramework>net9.0</TargetFramework>`
2. Run `dotnet build`
3. Run `dotnet test`
4. Commit with message: "Upgrade to .NET 9"

### Scenario 2: Multi-Project Solution with CI/CD

For solutions with multiple projects and GitHub Actions:

1. Update all `.csproj` files in the solution
2. Create/update `global.json` in solution root
3. Update `.github/workflows/*.yml` to use new version
4. Run full test suite locally: `dotnet test`
5. Push and verify CI passes
6. Commit message: "Upgrade solution to .NET 9 (includes CI/CD updates)"

### Scenario 3: Gradual Multi-Version Support

For libraries supporting multiple .NET versions:

1. Update `<TargetFrameworks>net8.0;net9.0</TargetFrameworks>`
2. Test on both versions: `dotnet test --framework net8.0 && dotnet test --framework net9.0`
3. Document supported versions in README
4. Gradually drop old version support in future releases

