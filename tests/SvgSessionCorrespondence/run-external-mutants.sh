#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
work="$(mktemp -d "${TMPDIR:-/tmp}/svg-external-mutants.XXXXXX")"
trap 'rm -rf "$work"' EXIT
mkdir -p "$work/base"
cp -R "$repo/src" "$repo/tests/Scene.SvgBrowser.Policy.Tests" "$work/base/"
python3 - "$work/base/Scene.SvgBrowser.Policy.Tests/Scene.SvgBrowser.Policy.Tests.fsproj" <<'PY'
import pathlib, sys
p=pathlib.Path(sys.argv[1])
p.write_text(p.read_text().replace('../../src/', '../src/'))
PY

kill_mutant() {
  local name="$1" old="$2" new="$3" pattern="$4"
  local lane="$work/$name"
  cp -R "$work/base" "$lane"
  python3 - "$lane/src/Scene.SvgBrowser/SvgExternalSessionHost.fs" "$old" "$new" <<'PY'
import pathlib, sys
p=pathlib.Path(sys.argv[1]); old=sys.argv[2]; new=sys.argv[3]; text=p.read_text()
if text.count(old) < 1: raise SystemExit('mutant target was absent')
p.write_text(text.replace(old,new,1))
PY
  if dotnet test "$lane/Scene.SvgBrowser.Policy.Tests/Scene.SvgBrowser.Policy.Tests.fsproj" -c Release \
      --filter "$pattern" --nologo > "$lane.log" 2>&1; then
    echo "$name mutant survived" >&2
    exit 1
  fi
  printf '%s:killed ' "$name"
}

kill_mutant revision \
  '| Some accepted when revision <= accepted ->' \
  '| Some accepted when false ->' \
  'FullyQualifiedName~rejected_current_completion'
kill_mutant epoch \
  'elif state.Epoch <> Some epoch then' \
  'elif false then' \
  'FullyQualifiedName~stale_mount_epoch'
kill_mutant dispose \
  'if state.Status = SvgExternalSessionStatus.Disposed then' \
  'if false then' \
  'FullyQualifiedName~disconnect_replacement_and_disposal'
printf '\n'
