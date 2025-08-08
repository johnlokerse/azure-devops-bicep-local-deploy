#!/usr/bin/env pwsh
[cmdletbinding()]
param(
   [Parameter(Mandatory=$true)][string]$Target
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

# build various flavors
ExecSafe { dotnet publish --configuration Release $root -r osx-arm64 --self-contained true -p:PublishSingleFile=true }
ExecSafe { dotnet publish --configuration Release $root -r linux-x64 --self-contained true -p:PublishSingleFile=true }
ExecSafe { dotnet publish --configuration Release $root -r win-x64 --self-contained true -p:PublishSingleFile=true }

# publish to the registry
ExecSafe { ~/.azure/bin/bicep publish-extension `
  --bin-osx-arm64 "$root/src/bin/Release/osx-arm64/publish/$extName" `
  --bin-linux-x64 "$root/src/bin/Release/linux-x64/publish/$extName" `
  --bin-win-x64 "$root/src/bin/Release/win-x64/publish/$extName.exe" `
  --target "$Target" `
  --force }