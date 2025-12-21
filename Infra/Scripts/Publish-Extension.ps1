#!/usr/bin/env pwsh
[cmdletbinding()]
param(
  [Parameter(Mandatory = $true)][string]$Target
)

$ErrorActionPreference = "Stop"

function ExecSafe([scriptblock] $ScriptBlock) {
  & $ScriptBlock
  if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
  }
}

$root = "$PSScriptRoot/../.."
$extName = "azure-devops-extension"

# Get the target framework from the project file
$csprojPath = "$root/src/DevOpsExtension.csproj"
[xml]$csproj = Get-Content $csprojPath
$targetFramework = $csproj.Project.PropertyGroup.TargetFramework | Where-Object { $_ } | Select-Object -First 1

Write-Host "Building for target framework: $targetFramework"

# build various flavors
ExecSafe { dotnet publish --configuration Release $root -r osx-arm64 }
ExecSafe { dotnet publish --configuration Release $root -r osx-x64 }
ExecSafe { dotnet publish --configuration Release $root -r linux-x64 }
ExecSafe { dotnet publish --configuration Release $root -r win-x64 }

# publish to the registry
ExecSafe { ~/.azure/bin/bicep publish-extension `
    --bin-osx-arm64 "$root/src/bin/Release/$targetFramework/osx-arm64/publish/$extName" `
    --bin-osx-x64 "$root/src/bin/Release/$targetFramework/osx-x64/publish/$extName" `
    --bin-linux-x64 "$root/src/bin/Release/$targetFramework/linux-x64/publish/$extName" `
    --bin-win-x64 "$root/src/bin/Release/$targetFramework/win-x64/publish/$extName.exe" `
    --target "$Target" `
    --force }