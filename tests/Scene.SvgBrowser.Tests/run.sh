#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output="${1:-$repo/readiness/svg-scene-02-5/browser-observations.json}"
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-svg-browser.XXXXXX")"
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
source_digest="$(cat "$repo/src/KeyboardInput/KeyboardInput.fsi" "$repo/src/KeyboardInput/KeyboardInput.fs" "$repo/src/Scene.SvgBrowser/SvgBrowser.fsi" "$repo/src/Scene.SvgBrowser/SvgBrowser.fs" | sha256sum | cut -d' ' -f1)"
package_digest="$(find "$feed" -name 'FS.GG.UI.*.nupkg' -type f -print0 | sort -z | while IFS= read -r -d '' package; do sha256sum "$package" | cut -d' ' -f1; done | sha256sum | cut -d' ' -f1)"
if [[ -n "${SVG_SCENE_ORCA_OBSERVATION:-}" ]]; then
  SVG_SCENE_AT_SOURCE_DIGEST="sha256:$source_digest" SVG_SCENE_AT_PACKAGE_DIGEST="sha256:$package_digest" \
    bash "$work/Browser/run-orca.sh" "$work/Browser" "$SVG_SCENE_ORCA_OBSERVATION"
fi

python3 - "$work" <<'PY'
import json, pathlib, sys, zipfile
root=pathlib.Path(sys.argv[1])
project=(root/'Browser'/'BrowserFixture.fsproj').read_text()
if 'ProjectReference' in project or '<Link>' in project:
    raise SystemExit('browser fixture contains a sibling source edge')
assets=json.loads((root/'Browser'/'obj'/'project.assets.json').read_text())
libraries={name.lower() for name in assets['libraries']}
for required in ('fs.gg.ui.scene/', 'fs.gg.ui.keyboardinput/', 'fs.gg.ui.scene.svgbrowser/', 'fable.browser.dom/'):
    if not any(name.startswith(required) for name in libraries):
        raise SystemExit(f'browser closure is missing {required}')
for forbidden in ('skiasharp/', 'fs.gg.ui.skiaviewer/', 'fs.gg.ui.controls.elmish/'):
    if any(name.startswith(forbidden) for name in libraries):
        raise SystemExit(f'browser closure contains forbidden dependency {forbidden}')
scene=next((root/'feed').glob('FS.GG.UI.Scene.0.29.0-preview.1.nupkg'))
adapter=next((root/'feed').glob('FS.GG.UI.Scene.SvgBrowser.*.nupkg'))
keyboard=next((root/'feed').glob('FS.GG.UI.KeyboardInput.*.nupkg'))
with zipfile.ZipFile(scene) as archive:
    nuspec=archive.read('FS.GG.UI.Scene.nuspec').decode('utf-8-sig')
    if 'Fable.Browser.Dom' in nuspec:
        raise SystemExit('portable Scene package acquired a browser dependency')
with zipfile.ZipFile(adapter) as archive:
    fable={name for name in archive.namelist() if name.startswith('fable/')}
    expected={'fable/FS.GG.UI.Scene.SvgBrowser.fsproj','fable/SvgBrowser.fsi','fable/SvgBrowser.fs'}
    if fable != expected:
        raise SystemExit(f'unexpected adapter Fable view: {sorted(fable)}')
with zipfile.ZipFile(keyboard) as archive:
    fable={name for name in archive.namelist() if name.startswith('fable/')}
    expected={'fable/FS.GG.UI.KeyboardInput.fsproj','fable/KeyboardInput.fsi','fable/KeyboardInput.fs'}
    if fable != expected:
        raise SystemExit(f'unexpected keyboard Fable view: {sorted(fable)}')
print('browser-package-closure: isolated=passed scene-browser-free=passed adapter-browser-explicit=passed')
PY
base="${output%.json}"
for family in chromium firefox webkit; do
  node "$work/Browser/browser-test.mjs" --browser "$family" --out "$base.$family.json" \
    --source-digest "sha256:$source_digest" --package-digest "sha256:$package_digest"
done

cmp "$base.chromium.exported.svg" "$base.firefox.exported.svg"
cmp "$base.chromium.exported.svg" "$base.webkit.exported.svg"
cp "$base.chromium.exported.svg" "$base.exported.svg"

if [[ -n "${SVG_SCENE_ORCA_OBSERVATION:-}" && -f "$SVG_SCENE_ORCA_OBSERVATION" ]]; then
  at_evidence="$(jq -c '{result,assistiveTechnology,environment,announcements,negativeControl,claims}' "$SVG_SCENE_ORCA_OBSERVATION")"
else
  at_evidence='{"result":"unavailable","reason":"No actual desktop assistive-technology process was exercised in this invocation. DOM/ARIA automation is retained only as non-AT evidence."}'
fi
jq -s --argjson at "$at_evidence" '
  {
    schema: "fsgg.svg-scene.browser-matrix-observation/v1",
    result: (if $at.result == "pass" then "pass" else "incomplete" end),
    candidate: .[0].candidate,
    automatedDimensions: {
      functional: {result:"pass", browsers:map(.environment.browserFamily)},
      visual: {result:"pass", browsers:map({family:.environment.browserFamily, reference:.document.visualReference})},
      resizeDprTouchReflow: {result:"pass", browsers:map({family:.environment.browserFamily, cases:.browserMatrix})},
      exportReload: {result:"pass", browsers:map(.environment.browserFamily)},
      nativeGeometryFontTiming: {result:"pass", browsers:map(.environment.browserFamily)}
    },
    externalDimensions: {
      assistiveTechnology: $at
    },
    browsers: map(.),
    milestoneReady: ($at.result == "pass"),
    claims: {allAutomatedBrowserFamiliesPassed:true, actualAssistiveTechnologyObserved:($at.result == "pass"), touchIsEmulated:true, packagePublished:false}
  }
' "$base.chromium.json" "$base.firefox.json" "$base.webkit.json" > "$output"
echo "svg-browser-matrix: automated=passed browsers=chromium,firefox,webkit assistive-technology=$(jq -r '.externalDimensions.assistiveTechnology.result' "$output") evidence=$output"
