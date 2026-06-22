#!/usr/bin/env bash
# Re-emit every catalog feature's readiness artifacts into an isolated target dir.
#
# Feature 185 (harness data-table refactor) evidence driver: capture a pre-refactor
# baseline corpus, then re-emit after each story and semantic-diff the two trees.
# Behavior-preserving refactor — the emitted tree must stay semantically equivalent
# (status/counts/required-headers/ordering) and byte-identical for CI-grepped literals.
#
# Usage:  scripts/emit-harness-readiness.sh <target-dir>
# Writes: <target-dir>/<feature-id>/...   (one subtree per feature)
set -u
TARGET="${1:?usage: emit-harness-readiness.sh <target-dir>}"
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
HARNESS="$ROOT/tools/Rendering.Harness"
mkdir -p "$TARGET"

# 12 catalog features (148,149,152,153,154,155,156-161). All route through the
# single `compositor-readiness --feature <N>` subcommand (legacy handler for
# 148-155, dedicated handlers for 156-161).
FEATURES=(148 149 152 153 154 155 156 157 158 159 160 161)

for f in "${FEATURES[@]}"; do
  out="$TARGET/$f"
  mkdir -p "$out"
  dotnet run --project "$HARNESS" -c Release --no-build -- \
    compositor-readiness --feature "$f" --out "$out" \
    > "$out/.stdout.txt" 2> "$out/.stderr.txt"
  echo "feature $f -> exit=$? out=$out"
done
