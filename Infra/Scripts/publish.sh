#!/bin/bash
set -e

usage="Usage: ./publish.sh <target>"
target=${1:?"Missing target. ${usage}"}

root="$(dirname ${BASH_SOURCE[0]})/.."
ext_name="azure-devops-extension"

# build various flavors
dotnet publish --configuration Release $root -r osx-arm64
dotnet publish --configuration Release $root -r linux-x64
dotnet publish --configuration Release $root -r win-x64

# publish to the registry
~/.azure/bin/bicep publish-extension \
  --bin-osx-arm64 "$root/src/bin/Release/osx-arm64/publish/$ext_name" \
  --bin-linux-x64 "$root/src/bin/Release/linux-x64/publish/$ext_name" \
  --bin-win-x64 "$root/src/bin/Release/win-x64/publish/$ext_name.exe" \
  --target "$target" \
  --force