#!/usr/bin/env bash
set -euo pipefail
repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
quint="${QUINT_BIN:?set QUINT_BIN to the exact Quint 0.32.0 executable}"
expected_sha=939b64095b706017f2f202c6f99c860c40be7c31bddc2b98557316e50f42cd7f
actual_sha="$(sha256sum "$quint" | cut -d' ' -f1)"
[[ "$actual_sha" == "$expected_sha" ]] || { echo "Quint object mismatch" >&2; exit 1; }
[[ "$($quint --version)" == 0.32.0 ]] || { echo "Quint version mismatch" >&2; exit 1; }
work="$(mktemp -d "${TMPDIR:-/tmp}/svg-external-model.XXXXXX")"
trap 'rm -rf "$work"' EXIT
python3 - "$repo/models/svg-runtime/external-session.md" "$work/externalSession.qnt" <<'PY'
import pathlib, re, sys
source=pathlib.Path(sys.argv[1]).read_text()
m=re.search(r'```quint externalSession\.qnt \+=\n(.*?)\n```', source, re.S)
if not m: raise SystemExit('canonical external-session Quint fence missing')
pathlib.Path(sys.argv[2]).write_text(m.group(1)+'\n')
PY
"$quint" typecheck "$work/externalSession.qnt" > "$work/typecheck.log"
"$quint" test "$work/externalSession.qnt" --main externalSessionTest --backend typescript \
  --match 'normalFlow|sameEpochPreservesBaseline|newEpochRestartsBaseline|staleEpochCannotApply|queuedAfterRevisionRejection|lostThenQueuedReplacement|cancelledThenQueuedReplacement|callbackFailureThenQueuedReplacement|presentationCallbackFailureIsExplicit|invalidateCancels|disposeCancelsAndTerminates|generationExhaustionRefusesWrap' \
  --out-itf "$work/{test}_{seq}.itf.json" --out "$work/test.json"
count="$(find "$work" -name '*.itf.json' -type f | wc -l)"
[[ "$count" == 12 ]] || { echo "expected twelve directed ITFs, found $count" >&2; exit 1; }
python3 - "$work" "$repo/tests/SvgSessionCorrespondence/external-session-production-traces.tsv" <<'PY'
import glob, json, os, pathlib, sys
work, expected_path = pathlib.Path(sys.argv[1]), pathlib.Path(sys.argv[2])
keys = ['generation','epoch','status','accepted','pending','queued','outcome','effect1','effect2']
rows = ['scenario\tindex\t' + '\t'.join(keys)]
for path in sorted(work.glob('*.itf.json')):
    name = path.name.split('_', 1)[0]
    for item in json.loads(path.read_text())['states']:
        state = item['state']
        def value(key):
            raw = state[key]
            return raw['#bigint'] if isinstance(raw, dict) else str(raw).lower()
        rows.append(name + '\t' + str(item['#meta']['index']) + '\t' + '\t'.join(value(key) for key in keys))
actual = '\n'.join(rows) + '\n'
if actual != expected_path.read_text():
    raise SystemExit('all-state ITF corpus differs from the production replay corpus')
PY
printf 'svg-external-model: quint=0.32.0 sha256:%s directed-itfs=%s model-sha256:%s\n' \
  "$actual_sha" "$count" "$(sha256sum "$work/externalSession.qnt" | cut -d' ' -f1)"
