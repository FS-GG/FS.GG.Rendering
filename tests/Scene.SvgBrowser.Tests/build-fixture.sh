#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work="${1:?Usage: build-fixture.sh ABSOLUTE_EMPTY_OUTPUT_DIRECTORY}"
[[ "$work" = /* && ! -e "$work" ]] || { echo "Output must be an absolute, absent directory" >&2; exit 1; }
# Original-candidate mode is closed and read-only: all four inputs or none.
original_mode=false
if [[ -n "${SVG_SCENE_ORIGINAL_CANDIDATE_DIR:-}${SVG_SCENE_ORIGINAL_CUSTODY:-}${SVG_SCENE_ORIGINAL_CUSTODY_SHA256:-}${SVG_SCENE_ORIGINAL_SOURCE_SHA:-}" ]]; then
  original_mode=true
  : "${SVG_SCENE_ORIGINAL_CANDIDATE_DIR:?complete original candidate inputs required}"
  : "${SVG_SCENE_ORIGINAL_CUSTODY:?complete original candidate inputs required}"
  : "${SVG_SCENE_ORIGINAL_CUSTODY_SHA256:?complete original candidate inputs required}"
  : "${SVG_SCENE_ORIGINAL_SOURCE_SHA:?complete original candidate inputs required}"
fi
verify_original() {
  python3 - "$repo" "$SVG_SCENE_ORIGINAL_CANDIDATE_DIR" "$SVG_SCENE_ORIGINAL_CUSTODY" "$SVG_SCENE_ORIGINAL_CUSTODY_SHA256" "$SVG_SCENE_ORIGINAL_SOURCE_SHA" <<'PYORIGINAL'
import argparse, hashlib, importlib.util, json, pathlib, re, subprocess, sys
repo, archives, manifest = map(pathlib.Path, sys.argv[1:4])
expected, source = sys.argv[4:6]
if not re.fullmatch('[0-9a-f]{64}',expected) or not re.fullmatch('[0-9a-f]{40}',source):
    raise SystemExit('original candidate exact checksum/source inputs required')
if not archives.is_absolute() or archives.is_symlink() or archives.resolve()!=archives or not archives.is_dir():
    raise SystemExit('original archive directory must be absolute, unlinked and present')
if not manifest.is_absolute() or manifest.parent!=archives or manifest.is_symlink() or not manifest.is_file():
    raise SystemExit('original custody must be the regular manifest beside original archives')
if hashlib.sha256(manifest.read_bytes()).hexdigest()!=expected:
    raise SystemExit('original custody checksum changed')
if any(p.is_symlink() or not p.is_file() for p in archives.glob('*.nupkg')):
    raise SystemExit('original archive link/type refused')
if subprocess.check_output(['git','-C',str(repo),'rev-parse','HEAD'],text=True).strip()!=source:
    raise SystemExit('original candidate source differs from actual checkout')
script=repo/'scripts/release-custody.py'
spec=importlib.util.spec_from_file_location('original_candidate_custody',script)
custody=importlib.util.module_from_spec(spec);spec.loader.exec_module(custody)
plan=repo/'eng/release/font-disposal-0.32.3.json'
payload, record=custody.verified_manifest(argparse.Namespace(plan=plan,archives=archives,manifest=manifest,source_sha=source))
if payload['version']!='0.32.3' or record['version']!='0.32.3':
    raise SystemExit('only original0.32.3 candidate is selected')
selected=[]
for name in ('FS.GG.UI.Scene','FS.GG.UI.KeyboardInput','FS.GG.UI.Scene.SvgBrowser'):
    row=next(r for r in record['archives'] if r['id']==name)
    if row['file']!=name+'.0.32.3.nupkg':raise SystemExit('noncanonical original package filename')
    selected.append({key:row[key] for key in ('id','file','size','sha256')})
print(json.dumps({'version':'0.32.3','sourceSha':source,'custodySha256':expected,
                  'planSha256':record['releasePlanSha256'],'originalRosterCount':len(record['archives']),
                  'archives':selected,'mode':'verified-original-artifacts-no-repack'},sort_keys=True))
PYORIGINAL
}
if [[ "$original_mode" == true ]]; then
  # No CLR/npm/tool can start until full19 custody and current source are joined.
  original_receipt="$(verify_original)"
fi
mkdir -p "$work"
chmod 700 "$work"
feed="$work/feed"
packages="$work/packages"
tools="$work/tools"
mkdir -p "$feed" "$packages" "$tools" "$work/logs"
# The independent focus comparison selects its SDK before package/Fable work.
# Other callers retain the existing SDK selection and working directory.
if [[ -n "${SVG_SCENE_FOCUS_SDK:-}" ]]; then
  [[ "$SVG_SCENE_FOCUS_SDK" == 10.0.401 ]] || { echo 'Unsupported focus SDK input' >&2; exit 1; }
  printf '%s\n' '{"sdk":{"version":"10.0.401","rollForward":"disable","allowPrerelease":false}}' > "$work/global.json"
  cd "$work"
  dotnet --version > "$work/logs/sdk-version.log"
  [[ "$(cat "$work/logs/sdk-version.log")" == 10.0.401 ]] || { echo 'Actual focus SDK differs' >&2; exit 1; }
fi
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
if [[ "$original_mode" == true ]]; then
  printf '%s\n' "$original_receipt" > "$work/original-candidate.json"
  python3 - "$SVG_SCENE_ORIGINAL_CANDIDATE_DIR" "$feed" "$work/original-candidate.json" <<'PYSTAGE'
import hashlib, json, pathlib, shutil, sys
original, feed, receipt=map(pathlib.Path,sys.argv[1:])
for row in json.loads(receipt.read_text())['archives']:
    destination=feed/row['file'];shutil.copyfile(original/row['file'],destination)
    if destination.stat().st_size!=row['size'] or hashlib.sha256(destination.read_bytes()).hexdigest()!=row['sha256']:
        raise SystemExit('original package copy changed')
PYSTAGE
else
  run_stage scene-pack dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$feed" -p:Version=0.32.0 -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
  run_stage keyboard-pack dotnet pack "$repo/src/KeyboardInput/KeyboardInput.fsproj" -c Release -o "$feed" -p:Version=0.32.0 -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
  run_stage browser-pack dotnet pack "$repo/src/Scene.SvgBrowser/Scene.SvgBrowser.fsproj" -c Release -o "$feed" -p:Version=0.32.0 -m:1 -nr:false -p:UseSharedCompilation=false --disable-build-servers
fi
cp -R "$repo/tests/Scene.SvgBrowser.Tests" "$work/Browser"
cp "$repo/tests/Scene.PortableConsumers/DocumentRoundTrip.fs" "$work/Browser/DocumentRoundTrip.fs"
rm -rf "$work/Browser/node_modules" "$work/Browser/dist" "$work/Browser/generated"
if [[ "$original_mode" == true ]]; then
  python3 - "$work/Browser/BrowserFixture.fsproj" <<'PYREFERENCES'
import pathlib,sys,xml.etree.ElementTree as ET
path=pathlib.Path(sys.argv[1]);tree=ET.parse(path);root=tree.getroot()
if root.findall('.//ProjectReference') or root.findall('.//Link'):raise SystemExit('fixture sibling source edge refused')
expected={'FS.GG.UI.Scene','FS.GG.UI.KeyboardInput','FS.GG.UI.Scene.SvgBrowser'}
refs=[r for r in root.findall('.//PackageReference') if r.get('Include','').startswith('FS.GG.UI.')]
if len(refs)!=2 or {r.get('Include') for r in refs}!=expected-{'FS.GG.UI.KeyboardInput'} or any(r.get('Version')!='0.32.0' for r in refs):
    raise SystemExit('fixture must declare exactly the canonical ordinary consumer references')
for row in refs:row.set('Version','[0.32.3]')
ET.SubElement(root.find('ItemGroup'),'PackageReference',Include='FS.GG.UI.KeyboardInput',Version='[0.32.3]')
tree.write(path,encoding='unicode')
PYREFERENCES
fi
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
if [[ "$original_mode" == true ]]; then
  python3 - "$work" <<'PYCACHE'
import hashlib,json,pathlib,sys
root=pathlib.Path(sys.argv[1]);record=json.loads((root/'original-candidate.json').read_text())
assets=json.loads((root/'Browser/obj/project.assets.json').read_text())
if {str(pathlib.Path(p).resolve()) for p in assets['packageFolders']}!={str((root/'packages').resolve())}:
    raise SystemExit('original consumer escaped its empty private package cache')
libraries={key.lower() for key in assets['libraries']}
for row in record['archives']:
    name=row['id'].lower()
    if name+'/0.32.3' not in libraries:raise SystemExit('original consumer resolved wrong package version')
    archive=root/'packages'/name/'0.32.3'/(name+'.0.32.3.nupkg')
    if hashlib.sha256(archive.read_bytes()).hexdigest()!=row['sha256']:
        raise SystemExit('resolved private cache differs from original archive')
PYCACHE
fi
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
if [[ "$original_mode" == true ]]; then
  [[ "$(verify_original)" == "$original_receipt" ]] || { echo 'original candidate custody changed during fixture build' >&2; exit 1; }
fi
echo "Static fixture prepared: $work/Browser/dist/suite.html"
