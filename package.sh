#!/usr/bin/env bash
# Pack BlazorWasmDevTools. The package version is major.minor.<commit count on main>,
# where major.minor is the <Version> in the instrumentation project.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$repo_root"

branch="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$branch" != "main" ]]; then
  echo "warning: building a package on branch '${branch}', not main" >&2
fi

revision="$(git rev-list --count main)"

csproj="instrumentation/BlazorWasmDevTools/BlazorWasmDevTools.csproj"
base_version="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$csproj")"
if [[ -z "$base_version" ]]; then
  echo "error: could not read <Version> from ${csproj}" >&2
  exit 1
fi

IFS='.' read -r major minor _ <<<"$base_version"
if [[ -z "$major" || -z "$minor" ]]; then
  echo "error: expected a major.minor version in ${csproj}, found '${base_version}'" >&2
  exit 1
fi

package_version="${major}.${minor}.${revision}"
echo "Packing BlazorWasmDevTools ${package_version}"

cd instrumentation
dotnet test
# GeneratePackageOnBuild skips the build during pack, so force a Release build here.
dotnet pack BlazorWasmDevTools/BlazorWasmDevTools.csproj -c Release -p:Version="${package_version}" -p:GeneratePackageOnBuild=false
