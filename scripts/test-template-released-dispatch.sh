#!/usr/bin/env bash
#
# test-template-released-dispatch.sh — Feature 214 dry-run proof harness.
#
# Drives scripts/template-released-dispatch.sh through the four quickstart Layer-2 scenarios with
# DRY_RUN=1 (no network, no credential). This is the standing local substitute for the live
# cross-repo smoke (blocked by the org credential, FS-GG/.github#21/#22); the real send is the
# disclosed deferred check (quickstart Layer 3 / T021).
#
# Usage: scripts/test-template-released-dispatch.sh
# Exit:  0 iff every scenario behaves as asserted; non-zero on the first failed assertion.
#
set -uo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
SUT="$here/template-released-dispatch.sh"
GOOD_REF='refs/tags/fs-gg-ui-template/v0.1.50-preview.1'
fails=0

# run <name> <expected_exit> <must_contain|-> <must_not_contain|-> -- <env assignments...>
run() {
  local name="$1" want_exit="$2" must="$3" mustnot="$4"; shift 4
  [ "$1" = "--" ] && shift
  local out rc
  out="$(env "$@" DRY_RUN=1 bash "$SUT" 2>&1)"; rc=$?
  local ok=1
  [ "$rc" = "$want_exit" ] || { ok=0; printf '  ✗ exit: got %s want %s\n' "$rc" "$want_exit"; }
  if [ "$must" != "-" ] && ! printf '%s' "$out" | grep -Fq "$must"; then
    ok=0; printf '  ✗ missing expected substring: %s\n' "$must"
  fi
  if [ "$mustnot" != "-" ] && printf '%s' "$out" | grep -Fq "$mustnot"; then
    ok=0; printf '  ✗ unexpected substring present: %s\n' "$mustnot"
  fi
  if [ "$ok" = 1 ]; then printf 'PASS  %s (exit %s)\n' "$name" "$rc"; else
    printf 'FAIL  %s\n' "$name"; printf '%s\n' "$out" | sed 's/^/      | /'; fails=$((fails+1)); fi
}

echo "== Feature 214 dispatch dry-run harness =="

# US1 happy path: derives version, prints payload, exit 0 (FR-002/FR-003, SC-002).
run "happy-path derives version + payload" 0 \
  "client_payload.version=0.1.50-preview.1" "-" -- \
  GH_TOKEN=dummy GITHUB_REF="$GOOD_REF"

# US2 edge: empty version → fail, no payload printed (FR-006).
run "empty version fails, no payload" 1 \
  "is empty after stripping" "client_payload.version=" -- \
  GH_TOKEN=dummy GITHUB_REF='refs/tags/fs-gg-ui-template/v'

# US2 edge: malformed version → fail (FR-006).
run "malformed version fails" 1 \
  "is malformed" "client_payload.version=" -- \
  GH_TOKEN=dummy GITHUB_REF='refs/tags/fs-gg-ui-template/vNOPE'

# US2 edge: missing credential → fail visibly even in dry-run (FR-004/FR-006).
run "missing credential fails (even dry-run)" 1 \
  "TEMPLATES_DISPATCH_TOKEN" "client_payload.version=" -- \
  GH_TOKEN= GITHUB_REF="$GOOD_REF"

echo
if [ "$fails" = 0 ]; then echo "ALL PASS"; exit 0; else echo "$fails scenario(s) FAILED"; exit 1; fi
