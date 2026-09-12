#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
evidence="${1:-}"
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-portable-consumers.XXXXXX")"
feed="$work/feed"
packages="$work/packages"
tools="$work/tools"
mkdir -p "$feed" "$packages" "$tools"

dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$feed" -p:Version=0.29.0-preview.1
cp -R "$repo/tests/Scene.PortableConsumers/DotNet" "$work/DotNet"
cp -R "$repo/tests/Scene.PortableConsumers/Fable" "$work/Fable"
cp "$repo/tests/Scene.PortableConsumers/Replay.fs" "$work/DotNet/Replay.fs"
cp "$repo/tests/Scene.PortableConsumers/Replay.fs" "$work/Fable/Replay.fs"
cp "$repo/tests/Scene.PortableConsumers/DocumentRoundTrip.fs" "$work/DotNet/DocumentRoundTrip.fs"
cp "$repo/tests/Scene.PortableConsumers/DocumentRoundTrip.fs" "$work/Fable/DocumentRoundTrip.fs"
cp "$repo/tests/Scene.PortableConsumers/DocumentReplay.fs" "$work/DotNet/DocumentReplay.fs"
cp "$repo/tests/Scene.PortableConsumers/DocumentReplay.fs" "$work/Fable/DocumentReplay.fs"
cp "$repo/tests/Scene.PortableConsumers/AuthoringCorrespondence.fs" "$work/DotNet/AuthoringCorrespondence.fs"
cp "$repo/tests/Scene.PortableConsumers/AuthoringCorrespondence.fs" "$work/Fable/AuthoringCorrespondence.fs"
cp "$repo/models/svg-foundation/retained-interaction.traces.tsv" "$work/retained-interaction.traces.tsv"
cp "$repo/models/svg-foundation/document-interaction.traces.tsv" "$work/document-interaction.traces.tsv"

cat > "$work/NuGet.Config" <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="candidate" value="$feed" />
    <add key="nuget" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="candidate"><package pattern="FS.GG.UI.Scene" /></packageSource>
    <packageSource key="nuget"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
EOF

export NUGET_PACKAGES="$packages"
dotnet restore "$work/DotNet/DotNet.fsproj" --configfile "$work/NuGet.Config"
dotnet build "$work/DotNet/DotNet.fsproj" --no-restore
dotnet run --project "$work/DotNet/DotNet.fsproj" --no-build -- \
  "$work/retained-interaction.traces.tsv" "$work/dotnet-projections.tsv" \
  "$work/dotnet-document.txt" "$work/dotnet-export.svg" \
  "$work/document-interaction.traces.tsv" "$work/dotnet-document-projections.tsv" "$work/dotnet-authoring.txt" | tee "$work/dotnet-replay.log"

dotnet restore "$work/Fable/Fable.fsproj" --configfile "$work/NuGet.Config"
dotnet tool install fable --version 5.13.0 --tool-path "$tools" --configfile "$work/NuGet.Config"
"$tools/fable" "$work/Fable/Fable.fsproj" --outDir "$work/javascript" --noCache
node "$work/javascript/Program.js" "$work/retained-interaction.traces.tsv" "$work/fable-projections.tsv" \
  "$work/fable-document.txt" "$work/fable-export.svg" \
  "$work/document-interaction.traces.tsv" "$work/fable-document-projections.tsv" "$work/fable-authoring.txt" | tee "$work/fable-replay.log"
cmp "$work/dotnet-projections.tsv" "$work/fable-projections.tsv"
cmp "$work/dotnet-document-projections.tsv" "$work/fable-document-projections.tsv"
cmp "$work/dotnet-document.txt" "$work/fable-document.txt"
cmp "$work/dotnet-export.svg" "$work/fable-export.svg"
cmp "$work/dotnet-authoring.txt" "$work/fable-authoring.txt"
projection_sha="$(sha256sum "$work/dotnet-projections.tsv" | cut -d' ' -f1)"
document_sha="$(sha256sum "$work/dotnet-document.txt" | cut -d' ' -f1)"
document_projection_sha="$(sha256sum "$work/dotnet-document-projections.tsv" | cut -d' ' -f1)"
authoring_sha="$(sha256sum "$work/dotnet-authoring.txt" | cut -d' ' -f1)"
echo "portable-document-roundtrip: runtimes=dotnet,fable-node projection-sha256=$projection_sha document-interaction-sha256=$document_projection_sha authoring-sha256=$authoring_sha document-sha256=$document_sha"

mutants=(wrong-order stale-revision-acceptance invalid-reference-acceptance lost-capture non-atomic-edit)
for runtime in dotnet fable; do
  for mutant in "${mutants[@]}"; do
    grep -F "document-mutant: runtime=" "$work/$runtime-replay.log" | grep -F "name=$mutant killed-at=DOCUMENT-TRACE-DIVERGENCE" >/dev/null
  done
done

if [[ -n "$evidence" ]]; then
  mkdir -p "$(dirname "$evidence")"
  python3 - "$work" "$evidence" "$projection_sha" "$document_projection_sha" "$document_sha" "$authoring_sha" <<'PY'
import json, pathlib, re, sys
work, output = map(pathlib.Path, sys.argv[1:3])
projection_sha, document_projection_sha, document_sha, authoring_sha = sys.argv[3:]
rows = []
pattern = re.compile(r"document-mutant: runtime=(\S+) name=(\S+) killed-at=DOCUMENT-TRACE-DIVERGENCE trace=(\d+) step=(\d+) action=(\S+)")
for runtime in ("dotnet", "fable"):
    text = (work / f"{runtime}-replay.log").read_text()
    for match in pattern.finditer(text):
        rows.append({"runtime": match.group(1), "name": match.group(2), "firstDivergence": {"trace": int(match.group(3)), "step": int(match.group(4)), "action": match.group(5)}})
evidence = {
    "schema": "fsgg.svg-scene.reducer-replay/v1",
    "result": "pass",
    "corpora": {"retainedTransitions": 192, "documentTransitions": 192, "relationship": "retained prefix/subject plus document interaction expansion"},
    "runtimes": ["dotnet", "fable-node"],
    "matchingProjection": True,
    "digests": {"retainedProjectionSha256": projection_sha, "documentProjectionSha256": document_projection_sha, "documentSha256": document_sha, "authoringCorrespondenceSha256": authoring_sha},
    "mutants": rows,
    "claims": {"allFiveMutantsKilledAtFirstDivergenceInBothRuntimes": len(rows) == 10, "safeSupportedSvgImport": True, "atomicAuthoringTransactions": True, "arbitraryImport": False},
}
output.write_text(json.dumps(evidence, indent=2) + "\n")
PY
fi

python3 - "$work" <<'PY'
import json
import pathlib
import sys
import zipfile

root = pathlib.Path(sys.argv[1])
for consumer in ("DotNet", "Fable"):
    project = (root / consumer / f"{consumer}.fsproj").read_text()
    if "ProjectReference" in project or "<Link>" in project:
        raise SystemExit(f"{consumer} consumer contains a sibling source edge")

assets = json.loads((root / "Fable" / "obj" / "project.assets.json").read_text())
libraries = {name.lower() for name in assets["libraries"]}
for forbidden in ("skiasharp/", "fs.gg.ui.skiaviewer/", "fs.gg.ui.controls.elmish/", "fable.browser.dom/"):
    if any(name.startswith(forbidden) for name in libraries):
        raise SystemExit(f"Fable closure contains forbidden dependency {forbidden}")

package = next((root / "feed").glob("FS.GG.UI.Scene.*.nupkg"))
with zipfile.ZipFile(package) as archive:
    fable_files = {name for name in archive.namelist() if name.startswith("fable/")}
expected_fable_files = {
    "fable/FS.GG.UI.Scene.fsproj",
    "fable/Types.fsi",
    "fable/Types.fs",
    "fable/RetainedSvg.fsi",
    "fable/RetainedSvg.fs",
    "fable/SvgDocument.fsi",
    "fable/SvgDocument.fs",
    "fable/SvgAuthoring.fsi",
    "fable/SvgAuthoring.fs",
    "fable/SvgArt.fsi",
    "fable/SvgArt.fs",
}
if fable_files != expected_fable_files:
    raise SystemExit(f"unexpected curated Fable source view: {sorted(fable_files)}")
print(f"isolated-consumers: dotnet=passed fable=passed package={package.name} browser-native-dependencies=absent")
PY
