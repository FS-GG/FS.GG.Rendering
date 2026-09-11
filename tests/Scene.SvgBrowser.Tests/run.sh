#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
output="${1:-$repo/readiness/svg-scene-02-4/browser-observations.json}"
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
source_digest="$(cat "$repo/src/KeyboardInput/KeyboardInput.fsi" "$repo/src/KeyboardInput/KeyboardInput.fs" "$repo/src/Scene.SvgBrowser/SvgBrowser.fsi" "$repo/src/Scene.SvgBrowser/SvgBrowser.fs" | sha256sum | cut -d' ' -f1)"
package_digest="$(find "$feed" -name 'FS.GG.UI.*.nupkg' -type f -print0 | sort -z | xargs -0 sha256sum | sha256sum | cut -d' ' -f1)"
node "$work/Browser/browser-test.mjs" --out "$output" --source-digest "sha256:$source_digest" --package-digest "sha256:$package_digest"
