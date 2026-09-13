#!/usr/bin/env bash
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
: "${QUINT_BIN:?set QUINT_BIN to exact Quint 0.32.0}"
expected_quint_sha=939b64095b706017f2f202c6f99c860c40be7c31bddc2b98557316e50f42cd7f
[[ "$(sha256sum "$QUINT_BIN" | cut -d' ' -f1)" == "$expected_quint_sha" ]]
[[ "$($QUINT_BIN --version)" == "0.32.0" ]]

model="$root/readiness/svg-input-01-2/quint/inputCommand.qnt"
scratch="$(mktemp -d "${TMPDIR:-/tmp}/input-command-model.XXXXXX")"
trap 'rm -rf -- "$scratch"' EXIT

"$QUINT_BIN" typecheck "$model" > "$scratch/typecheck.log"
for witness in terminalPrefixWitness unavailableDeadlineDoesNotDispatch continuationCancelsTerminal \
  nonmatchRetriesAtRootOnce lostReleaseWitness cancellationNeutralizesBeforeLostRelease \
  oneSourceReleasePreservesOtherOwner; do
  "$QUINT_BIN" run "$model" --main inputCommandTest --step "$witness" \
    --max-steps 1 --max-samples 1 --n-traces 1 --backend=typescript --verbosity 0 \
    >> "$scratch/tests.log"
done

for run in a b; do
  mkdir -p "$scratch/traces-$run"
  "$QUINT_BIN" run "$model" --main inputCommand --invariant inputCommandSafe \
    --max-steps 16 --max-samples 64 --n-traces 32 --seed=0x02468ace \
    --backend=typescript --out-itf "$scratch/traces-$run/trace_{seq}.itf.json" --verbosity 0 \
    > "$scratch/run-$run.log"
  mkdir -p "$scratch/normalized-$run"
  for trace in "$scratch/traces-$run"/*.itf.json; do
    jq -S 'del(."#meta")' "$trace" > "$scratch/normalized-$run/$(basename "$trace")"
  done
done
diff -ru "$scratch/normalized-a" "$scratch/normalized-b" >/dev/null

python3 - "$model" "$scratch" <<'PY'
from pathlib import Path
import sys
source = Path(sys.argv[1]).read_text()
scratch = Path(sys.argv[2])
deadline = source.replace(
    "pending: 0,\n      deadlineRequested: false,\n      invocations: if (available)",
    "pending: 1,\n      deadlineRequested: true,\n      invocations: if (available)",
    1,
)
if deadline == source:
    raise SystemExit("deadline mutant anchor drifted")
(scratch / "deadline-mutant.qnt").write_text(deadline)
cancel = source.replace(
    "heldA: false,\n    heldB: false,\n    heldAggregate: false,\n  }\n\n  action step",
    "heldA: state.heldA,\n    heldB: state.heldB,\n    heldAggregate: state.heldAggregate,\n  }\n\n  action step",
    1,
)
if cancel == source:
    raise SystemExit("lost-release mutant anchor drifted")
(scratch / "lost-release-mutant.qnt").write_text(cancel)
PY

if "$QUINT_BIN" run "$scratch/deadline-mutant.qnt" --main inputCommandTest --step terminalPrefixWitness \
  --max-steps 1 --max-samples 1 --n-traces 1 --backend=typescript --verbosity 0 >/dev/null 2>&1; then
  echo 'input-command-model: terminal-prefix mutant survived' >&2
  exit 1
fi
if "$QUINT_BIN" run "$scratch/lost-release-mutant.qnt" --main inputCommandTest --step cancellationNeutralizesBeforeLostRelease \
  --max-steps 1 --max-samples 1 --n-traces 1 --backend=typescript --verbosity 0 >/dev/null 2>&1; then
  echo 'input-command-model: lost-release mutant survived' >&2
  exit 1
fi

echo 'input-command-model: typecheck=passed tests=passed seeded-traces=deterministic mutants=2-killed'
