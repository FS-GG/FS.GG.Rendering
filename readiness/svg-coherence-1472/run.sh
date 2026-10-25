#!/usr/bin/env bash
# Usage: run.sh <baseline Scene.dll dir> <candidate Scene.dll dir> <work dir> [rounds]
# Builds the two harnesses once per Scene.dll, then runs ABBA (baseline, candidate, candidate, baseline) pinned to 3 CPUs.
# The create harness (this directory) measures construction; the #1469 query harness confirms query cost is unchanged.
set -euo pipefail
BASE="$1"; CAND="$2"; WORK="$3"; ROUNDS="${4:-9}"; CPUS="${CPUS:-9-11}"
HERE="$(cd "$(dirname "$0")" && pwd)"
Q1469="$HERE/../svg-coherence-1469"
mkdir -p "$WORK/raw"
sha256sum "$BASE/FS.GG.UI.Scene.dll" "$CAND/FS.GG.UI.Scene.dll" > "$WORK/raw/scene-dll-sha256.txt"
export TMPDIR="$WORK/tmp"; mkdir -p "$TMPDIR"
export DOTNET_NOLOGO=1 DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_TieredPGO=0
for arm in baseline candidate; do
  dll="$BASE"; [ "$arm" = candidate ] && dll="$CAND"
  dotnet build "$HERE/CreateBench.fsproj" -c Release -o "$WORK/build-create-$arm" -p:SceneDll="$dll/FS.GG.UI.Scene.dll" -p:UseSharedCompilation=false >/dev/null
  dotnet build "$Q1469/Bench.fsproj" -c Release -o "$WORK/build-query-$arm" -p:SceneDll="$dll/FS.GG.UI.Scene.dll" -p:UseSharedCompilation=false >/dev/null
done
i=0
for arm in baseline candidate candidate baseline; do
  i=$((i+1))
  taskset -c "$CPUS" dotnet "$WORK/build-create-$arm/CreateBench.dll" "$arm" "$WORK/raw/create-run$i-$arm.json" "$ROUNDS"
  taskset -c "$CPUS" dotnet "$WORK/build-query-$arm/Bench.dll" "$arm" "$WORK/raw/query-run$i-$arm.json" 12
done
