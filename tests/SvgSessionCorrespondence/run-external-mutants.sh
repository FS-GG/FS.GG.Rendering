#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work="$(mktemp -d "${TMPDIR:-/tmp}/svg-external-mutants.XXXXXX")"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/base"
cp -R "$repo/src" "$repo/tests/Scene.SvgBrowser.Policy.Tests" "$work/base/"
cp "$repo/Directory.Build.props" "$repo/Directory.Build.local.props" \
  "$repo/Directory.Packages.props" "$repo/Directory.Packages.local.props" \
  "$repo/global.json" "$work/base/"
python3 - "$work/base/Scene.SvgBrowser.Policy.Tests/Scene.SvgBrowser.Policy.Tests.fsproj" <<'PY'
import pathlib, sys
p=pathlib.Path(sys.argv[1])
text=p.read_text().replace('../../src/', '../src/').replace('..\\..\\src\\', '..\\src\\')
p.write_text(text)
PY
project="$work/base/Scene.SvgBrowser.Policy.Tests/Scene.SvgBrowser.Policy.Tests.fsproj"
dotnet build "$project" -c Release --nologo > "$work/baseline-build.log" 2>&1

prove_baseline() {
  local name="$1" pattern="$2"
  local log="$work/baseline-$name.log"
  dotnet test "$project" -c Release --no-build --filter "$pattern" --nologo > "$log" 2>&1
  grep -Eq 'Passed!|Passed:' "$log" || { echo "$name baseline did not report a passing test" >&2; exit 1; }
  grep -Eq 'Total:[[:space:]]+1' "$log" || { echo "$name baseline selection did not discover exactly one test" >&2; exit 1; }
}

kill_mutant() {
  local name="$1" old="$2" new="$3" pattern="$4" marker="$5"
  prove_baseline "$marker" "$pattern"
  local lane="$work/$name"
  cp -R "$work/base" "$lane"
  python3 - "$lane/src/Scene.SvgBrowser/SvgExternalSessionHost.fs" "$old" "$new" <<'PY'
import pathlib, sys
p=pathlib.Path(sys.argv[1]); old=sys.argv[2]; new=sys.argv[3]; text=p.read_text()
if text.count(old) < 1: raise SystemExit('mutant target was absent')
p.write_text(text.replace(old,new,1))
PY
  local mutated="$lane/Scene.SvgBrowser.Policy.Tests/Scene.SvgBrowser.Policy.Tests.fsproj"
  dotnet build "$mutated" -c Release --nologo > "$lane-build.log" 2>&1 || {
    echo "$name mutant did not compile" >&2; exit 1;
  }
  if dotnet test "$mutated" -c Release --no-build --filter "$pattern" --nologo > "$lane-test.log" 2>&1; then
    echo "$name mutant survived" >&2
    exit 1
  fi
  grep -Fq "$marker" "$lane-test.log" || { echo "$name failed outside its selected assertion" >&2; exit 1; }
  grep -Eq 'Failed!|Failed:' "$lane-test.log" || { echo "$name did not report an assertion failure" >&2; exit 1; }
  printf '%s:killed ' "$name"
}

kill_mutant acquisition \
  'elif state.PendingAcquisitionId <> Some acquisitionId then' \
  'elif false then' \
  'FullyQualifiedName~old duplicate cannot drain' \
  'old duplicate cannot drain'
kill_mutant revision \
  '| Some accepted when revision <= accepted ->' \
  '| Some accepted when false ->' \
  'FullyQualifiedName~rejected current completion' \
  'rejected current completion'
kill_mutant generation \
  'if generation <> state.MountGeneration then' \
  'if false then' \
  'FullyQualifiedName~stale generation and epoch' \
  'stale generation and epoch'
kill_mutant dispose \
  'if state.Status = SvgExternalSessionStatus.Disposed then' \
  'if false then' \
  'FullyQualifiedName~invalidation disconnect and disposal' \
  'invalidation disconnect and disposal'
printf '\n'
