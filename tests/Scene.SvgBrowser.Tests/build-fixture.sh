#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work="${1:?Usage: build-fixture.sh ABSOLUTE_EMPTY_OUTPUT_DIRECTORY}"
[[ "$work" = /* && ! -e "$work" ]] || { echo "Output must be an absolute, absent directory" >&2; exit 1; }
mkdir -p "$work"
chmod 700 "$work"
feed="$work/feed"
packages="$work/packages"
tools="$work/tools"
mkdir -p "$feed" "$packages" "$tools" "$work/logs"
export NUGET_PACKAGES="$packages"
export MSBUILDDISABLENODEREUSE=1
export DOTNET_CLI_USE_MSBUILD_SERVER=0
run_stage() {
  local stage="$1"
  shift
  local status=0
  "$@" >"$work/logs/$stage.log" 2>&1 || status=$?
  printf '%s\t%s\n' "$stage" "$status" >>"$work/logs/statuses.tsv"
  if [[ "$status" != 0 ]]; then tail -n 30 "$work/logs/$stage.log" >&2; return "$status"; fi
  echo "fixture-build: $stage passed"
}
source "$repo/tests/Scene.SvgBrowser.Tests/source-manifest.sh"
write_source_manifest "$work/source-manifest.tsv"
run_stage scene-pack dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$feed" -p:Version=0.32.0 -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
run_stage keyboard-pack dotnet pack "$repo/src/KeyboardInput/KeyboardInput.fsproj" -c Release -o "$feed" -p:Version=0.32.0 -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
run_stage browser-pack dotnet pack "$repo/src/Scene.SvgBrowser/Scene.SvgBrowser.fsproj" -c Release -o "$feed" -p:Version=0.32.0 -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
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
run_stage fixture-restore dotnet restore "$work/Browser/BrowserFixture.fsproj" --configfile "$work/NuGet.Config" -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
python3 - "$feed" "$work/Browser/packaged-svg-geometry-worker.js" <<'PY'
import pathlib, sys, zipfile
feed, output = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
package = next(feed.glob('FS.GG.UI.Scene.SvgBrowser.*.nupkg'))
with zipfile.ZipFile(package) as archive:
    output.write_bytes(archive.read('contentFiles/any/any/svg-geometry-worker.js'))
PY
run_stage fable-install dotnet tool install fable --version 5.17.0 --tool-path "$tools" --configfile "$work/NuGet.Config"
run_stage fable-compile "$tools/fable" "$work/Browser/BrowserFixture.fsproj" --outDir "$work/Browser/generated" --lang JavaScript --noCache
run_stage npm-install npm ci --prefix "$work/Browser"
run_stage vite-build npm run --prefix "$work/Browser" build
write_source_manifest "$work/source-manifest-after.tsv"
cmp "$work/source-manifest.tsv" "$work/source-manifest-after.tsv"
run_stage artifact-manifest node "$work/Browser/package-fixture.mjs" --work "$work"
echo "Static fixture prepared: $work/Browser/dist/suite.html"
