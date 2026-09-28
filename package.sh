#!/usr/bin/env bash
# Pack BlazorWasmDevTools. The package version is major.minor.<commit count on main>.<minutes since last commit on main>,
# where major.minor is the <Version> in the instrumentation project.
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$repo_root"

branch="$(git rev-parse --abbrev-ref HEAD)"
if [[ "$branch" != "main" ]]; then
  echo "warning: building a package on branch '${branch}', not main" >&2
fi

build="$(git rev-list --count main)"
# main_last_commit_epoch="$(git log -1 --format=%ct main 2>/dev/null || git log -1 --format=%ct)"
# if [[ -n "$main_last_commit_epoch" ]]; then
#   now_epoch="$(date -u +%s)"
#   revision="$(( (now_epoch - main_last_commit_epoch) / 60 ))"
# else
#   revision="0"
# fi

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

# .${revision}"
package_version="${major}.${minor}.${build}"
echo "Packing BlazorWasmDevTools ${package_version}"

cd instrumentation
dotnet test
# GeneratePackageOnBuild is enabled in the project; disable it here to avoid producing a second package during pack's build step.
dotnet pack BlazorWasmDevTools/BlazorWasmDevTools.csproj -c Release -p:Version="${package_version}" -p:GeneratePackageOnBuild=false
