#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output="${1:-$repo/readiness/svg-scene-02-6/preview-a-performance.json}"
mode="${SVG_SCENE_PERFORMANCE_MODE:-diagnostic}"
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-svg-performance.XXXXXX")"
trap 'rm -rf "$work"' EXIT
feed="$work/feed"
packages="$work/packages"
tools="$work/tools"
mkdir -p "$feed" "$packages" "$tools" "$(dirname "$output")"

dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$feed" -p:Version=0.29.0-preview.1
dotnet pack "$repo/src/KeyboardInput/KeyboardInput.fsproj" -c Release -o "$feed" -p:Version=0.29.0-preview.1
dotnet pack "$repo/src/Scene.SvgBrowser/Scene.SvgBrowser.fsproj" -c Release -o "$feed" -p:Version=0.29.0-preview.1
cp -R "$repo/tests/Scene.SvgBrowser.Tests" "$work/Browser"
cp "$repo/tests/Scene.PortableConsumers/DocumentRoundTrip.fs" "$work/Browser/DocumentRoundTrip.fs"
rm -rf "$work/Browser/node_modules" "$work/Browser/dist" "$work/Browser/generated"
cat > "$work/NuGet.Config" <<CONFIG
<configuration>
  <packageSources>
    <clear />
    <add key="candidate" value="$feed" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="candidate"><package pattern="FS.GG.UI.Scene*" /><package pattern="FS.GG.UI.KeyboardInput" /></packageSource>
    <packageSource key="nuget"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
CONFIG
export NUGET_PACKAGES="$packages"
dotnet restore "$work/Browser/BrowserFixture.fsproj" --configfile "$work/NuGet.Config"
dotnet tool install fable --version 5.17.0 --tool-path "$tools" --configfile "$work/NuGet.Config"
"$tools/fable" "$work/Browser/BrowserFixture.fsproj" --outDir "$work/Browser/generated" --lang JavaScript --noCache
npm ci --prefix "$work/Browser"
npm run --prefix "$work/Browser" build
source_manifest="$work/source-manifest.tsv"
source_manifest_after="$work/source-manifest-after.tsv"
write_source_manifest() {
  local destination="$1"
  git -C "$repo" ls-files -z --cached --others --exclude-standard -- \
    src/KeyboardInput src/Scene src/Scene.SvgBrowser tests/Scene.SvgBrowser.Tests \
    | sort -z \
    | while IFS= read -r -d '' relative; do
        case "$relative" in
          */bin/*|*/obj/*|*/node_modules/*|*/dist/*|*/packages.lock.json) continue ;;
        esac
        printf '%s\t%s\n' "$(sha256sum "$repo/$relative" | cut -d' ' -f1)" "$relative"
      done > "$destination"
}
write_source_manifest "$source_manifest"
source_digest="$(sha256sum "$source_manifest" | cut -d' ' -f1)"
package_digest="$(find "$feed" -name 'FS.GG.UI.*.nupkg' -type f -print0 | sort -z | while IFS= read -r -d '' package; do sha256sum "$package" | cut -d' ' -f1; done | sha256sum | cut -d' ' -f1)"

if [[ "${SVG_SCENE_PERFORMANCE_SMOKE:-}" == "gallery" ]]; then
  node "$work/Browser/performance-test.mjs" --mode diagnostic --gallery-smoke --out "$output" \
    --source-digest "sha256:$source_digest" --source-manifest "$source_manifest" --package-digest "sha256:$package_digest"
  exit 0
fi

for mutant in unnecessary-rebuild listener-leak excessive-document-acceptance; do
  log="$work/$mutant.log"
  if node "$work/Browser/performance-test.mjs" --mode diagnostic --mutant "$mutant" --out "$work/$mutant.json" \
      --source-digest "sha256:$source_digest" --source-manifest "$source_manifest" --package-digest "sha256:$package_digest" >"$log" 2>&1; then
    echo "performance mutant survived: $mutant" >&2
    exit 1
  fi
  case "$mutant" in
    unnecessary-rebuild) expected="unnecessary-rebuild gate" ;;
    listener-leak) expected="listener/resource-leak gate" ;;
    excessive-document-acceptance) expected="excessive-document gate" ;;
  esac
  if ! grep -F "$expected" "$log" >/dev/null; then
    cat "$log" >&2
    echo "performance mutant failed for an unexpected reason: $mutant" >&2
    exit 1
  fi
done

if [[ "$mode" == "reference" ]]; then
  reference_ready=false
  for _ in $(seq 1 120); do
    one_minute_load="$(cut -d' ' -f1 /proc/loadavg)"
    # The receipt still enforces the declared <=1.0 start/end predicates. Begin
    # substantially below that ceiling so observation work does not consume it.
    if awk -v observed="$one_minute_load" 'BEGIN { exit !(observed <= 0.35) }'; then
      reference_ready=true
      break
    fi
    sleep 5
  done
  if [[ "$reference_ready" != true ]]; then
    echo "reference host did not settle below load1 0.35" >&2
    exit 1
  fi
fi

node "$work/Browser/performance-test.mjs" --mode "$mode" --out "$output" \
  --source-digest "sha256:$source_digest" --source-manifest "$source_manifest" --package-digest "sha256:$package_digest"
write_source_manifest "$source_manifest_after"
if ! cmp "$source_manifest" "$source_manifest_after"; then
  echo "authoritative source manifest changed during performance observation" >&2
  exit 1
fi
echo "svg-preview-a-performance: result=passed mode=$mode evidence=$output"
