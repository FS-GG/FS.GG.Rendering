#!/usr/bin/env bash
# Shared first-pack path: candidate qualification and publisher custody use the same byte contract.
set -euo pipefail
plan="${1:?release plan required}"
source_sha="${2:?source SHA required}"
version="${3:?coherent version required}"
python3 scripts/release-source-guard.py --plan "$plan" --source-sha "$source_sha" --version "$version"
[[ "$(git rev-parse HEAD)" == "$source_sha" ]] || { echo 'release-pack: checked-out source differs from requested identity' >&2; exit 1; }
[[ ! -d artifacts/packages || -z "$(find artifacts/packages -maxdepth 1 -name '*.nupkg' -print -quit)" ]] || {
  echo 'release-pack: refusing to replace existing package custody' >&2; exit 1;
}
mkdir -p artifacts/packages
dotnet pack FS.GG.Rendering.slnx -c Release -p:Version="$version" -o artifacts/packages
dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -p:Version="$version" -o artifacts/packages
python3 scripts/release-custody.py prepare --plan "$plan" --archives artifacts/packages \
  --manifest artifacts/packages/release-custody.json --source-sha "$source_sha"
python3 scripts/release-custody.py verify --plan "$plan" --archives artifacts/packages \
  --manifest artifacts/packages/release-custody.json --source-sha "$source_sha"
