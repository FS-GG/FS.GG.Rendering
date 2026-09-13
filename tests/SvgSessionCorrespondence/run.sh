#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
game_root="${1:?usage: run.sh <exact-game-checkout>}"
expected="$(tr -d '[:space:]' < "$repo/tests/SvgSessionCorrespondence/game-source-revision.txt")"
actual="$(git -C "$game_root" rev-parse HEAD)"
[[ "$actual" == "$expected" ]] || { echo "expected Game $expected, found $actual" >&2; exit 1; }
work="$(mktemp -d "${TMPDIR:-/tmp}/svg-session-correspondence.XXXXXX")"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/feed" "$work/packages" "$work/tools"
dotnet pack "$game_root/src/Game.Core/FS.GG.Game.Core.fsproj" -c Release -o "$work/feed" -p:Version=0.15.0-svg-runtime.3 >/dev/null
dotnet pack "$repo/src/Scene/Scene.fsproj" -c Release -o "$work/feed" -p:Version=0.29.0-svg-runtime.4 >/dev/null
dotnet pack "$repo/src/KeyboardInput/KeyboardInput.fsproj" -c Release -o "$work/feed" -p:Version=0.29.0-svg-runtime.4 >/dev/null
dotnet pack "$repo/src/Scene.SvgBrowser/Scene.SvgBrowser.fsproj" -c Release -o "$work/feed" -p:Version=0.29.0-svg-runtime.4 >/dev/null
cp -R "$repo/tests/SvgSessionCorrespondence" "$work/Correspondence"
cat > "$work/NuGet.Config" <<CONFIG
<configuration><packageSources><clear/><add key="candidate" value="$work/feed"/><add key="nuget" value="https://api.nuget.org/v3/index.json"/></packageSources><packageSourceMapping><packageSource key="candidate"><package pattern="FS.GG.*"/></packageSource><packageSource key="nuget"><package pattern="*"/></packageSource></packageSourceMapping></configuration>
CONFIG
export NUGET_PACKAGES="$work/packages"
dotnet restore "$work/Correspondence/Correspondence.fsproj" --configfile "$work/NuGet.Config" >/dev/null
dotnet build "$work/Correspondence/Correspondence.fsproj" --no-restore >/dev/null
dotnet_output="$(dotnet run --project "$work/Correspondence/Correspondence.fsproj" --no-build | tail -1)"
dotnet tool install fable --version 5.17.0 --tool-path "$work/tools" --configfile "$work/NuGet.Config" >/dev/null
"$work/tools/fable" "$work/Correspondence/Correspondence.fsproj" --outDir "$work/js" --lang JavaScript --noCache >/dev/null
fable_output="$(node "$work/js/Program.js" | tail -1)"
[[ "$dotnet_output" == "$fable_output" ]] || { printf 'dotnet: %s\nfable:  %s\n' "$dotnet_output" "$fable_output" >&2; exit 1; }
digest="$(printf %s "$dotnet_output" | sha256sum | cut -d' ' -f1)"
printf 'svg-session-correspondence: game=%s bytes=%s sha256:%s\n' "$actual" "${#dotnet_output}" "$digest"
