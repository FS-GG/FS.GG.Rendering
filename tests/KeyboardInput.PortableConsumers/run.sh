#!/usr/bin/env bash
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work="$(mktemp -d "${TMPDIR:-/tmp}/keyboard-input-consumers.XXXXXX")"
trap 'rm -rf "$work"' EXIT
version="0.30.0-svg-input.1.local"
mkdir -p "$work/feed" "$work/packages" "$work/tools"

dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$work/feed" -p:Version="$version" >/dev/null
dotnet pack "$repo/src/KeyboardInput/KeyboardInput.fsproj" -c Release -o "$work/feed" -p:Version="$version" >/dev/null
cp -R "$repo/tests/KeyboardInput.PortableConsumers/DotNet" "$work/DotNet"
cp -R "$repo/tests/KeyboardInput.PortableConsumers/Fable" "$work/Fable"
cp "$repo/tests/KeyboardInput.PortableConsumers/Common.fs" "$work/DotNet/Common.fs"
cp "$repo/tests/KeyboardInput.PortableConsumers/Common.fs" "$work/Fable/Common.fs"

cat > "$work/Directory.Packages.props" <<EOF
<Project><PropertyGroup><ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally></PropertyGroup><ItemGroup>
<PackageVersion Include="FS.GG.UI.Scene" Version="$version" />
<PackageVersion Include="FS.GG.UI.KeyboardInput" Version="$version" />
</ItemGroup></Project>
EOF
cat > "$work/NuGet.Config" <<EOF
<configuration><packageSources><clear/><add key="candidate" value="$work/feed"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="candidate"><package pattern="FS.GG.UI.*"/></packageSource><packageSource key="nuget"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
EOF

export NUGET_PACKAGES="$work/packages"
dotnet restore "$work/DotNet/DotNet.fsproj" --configfile "$work/NuGet.Config" >/dev/null
dotnet run --project "$work/DotNet/DotNet.fsproj" --no-restore > "$work/dotnet.txt"
dotnet restore "$work/Fable/Fable.fsproj" --configfile "$work/NuGet.Config" >/dev/null
dotnet tool install fable --version 5.17.1 --tool-path "$work/tools" --configfile "$work/NuGet.Config" >/dev/null
"$work/tools/fable" "$work/Fable/Fable.fsproj" --outDir "$work/js" --noCache >/dev/null
node "$work/js/Program.js" > "$work/fable.txt"
cmp "$work/dotnet.txt" "$work/fable.txt"

python3 - "$work" "$version" <<'PY'
import json,pathlib,sys,zipfile
root=pathlib.Path(sys.argv[1]); version=sys.argv[2]
assets=json.loads((root/'Fable/obj/project.assets.json').read_text())
libraries={name.lower() for name in assets['libraries']}
for forbidden in ('skiasharp/','fs.gg.game.','fable.browser.dom/','fs.gg.ui.controls','fs.gg.ui.layout'):
    if any(name.startswith(forbidden) for name in libraries): raise SystemExit(f'forbidden Fable dependency: {forbidden}')
package=root/'feed'/f'FS.GG.UI.KeyboardInput.{version}.nupkg'
with zipfile.ZipFile(package) as archive:
    fable={name for name in archive.namelist() if name.startswith('fable/')}
expected={'fable/FS.GG.UI.KeyboardInput.fsproj','fable/KeyboardInput.fsi','fable/KeyboardInput.fs','fable/CommandInput.fsi','fable/CommandInput.fs'}
if fable != expected: raise SystemExit(f'unexpected curated Fable view: {sorted(fable)}')
print('keyboard-input-portable: dotnet=passed fable-node=passed closure=scene-only')
PY
