#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work="$(mktemp -d "${TMPDIR:-/tmp}/scene-portable-consumers.XXXXXX")"
feed="$work/feed"
packages="$work/packages"
tools="$work/tools"
mkdir -p "$feed" "$packages" "$tools"

dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$feed"
cp -R "$repo/tests/Scene.PortableConsumers/DotNet" "$work/DotNet"
cp -R "$repo/tests/Scene.PortableConsumers/Fable" "$work/Fable"

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
dotnet run --project "$work/DotNet/DotNet.fsproj" --no-build

dotnet restore "$work/Fable/Fable.fsproj" --configfile "$work/NuGet.Config"
dotnet tool install fable --version 5.17.0 --tool-path "$tools" --configfile "$work/NuGet.Config"
"$tools/fable" "$work/Fable/Fable.fsproj" --outDir "$work/javascript" --noCache

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
}
if fable_files != expected_fable_files:
    raise SystemExit(f"unexpected curated Fable source view: {sorted(fable_files)}")
print(f"isolated-consumers: dotnet=passed fable=passed package={package.name} browser-native-dependencies=absent")
PY
