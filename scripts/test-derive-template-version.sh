#!/usr/bin/env bash
#
# test-derive-template-version.sh — Feature 216 derivation harness (quickstart Layer 2).
#
# Drives scripts/derive-template-version.sh through the boundary cases (no network, no credential):
# happy-path derive + non-tag / empty-after-strip / malformed edges. This is the local
# fail-before/pass-after proof of the version derivation the migrated sender preserves; the live
# cross-repo send (credential + POST) now lives in the reusable workflow and is the disclosed deferred
# Layer 3 check (research R3). The Feature 214 credential-missing + DRY_RUN-payload cases are gone by
# design — there is no credential or POST in this helper anymore.
#
# Usage: scripts/test-derive-template-version.sh
# Exit:  0 iff every scenario behaves as asserted; non-zero on the first failed assertion.
#
set -uo pipefail

here="$(cd "$(dirname "$0")" && pwd)"
SUT="$here/derive-template-version.sh"
GOOD_REF='refs/tags/fs-gg-ui-template/v0.1.52-preview.1'
fails=0

# run <name> <expected_exit> <stdout_must|-> <stdout_mustnot|-> <ghoutput_must|-> -- <env assignments...>
#
# Each scenario runs the SUT with a fresh $GITHUB_OUTPUT temp file so we can assert both stdout AND
# what (if anything) was appended to $GITHUB_OUTPUT (the CI step output).
run() {
  local name="$1" want_exit="$2" must="$3" mustnot="$4" gh_must="$5"; shift 5
  [ "$1" = "--" ] && shift
  local out rc ghfile ghcontent
  ghfile="$(mktemp)"
  out="$(env "$@" GITHUB_OUTPUT="$ghfile" bash "$SUT" 2>&1)"; rc=$?
  ghcontent="$(cat "$ghfile" 2>/dev/null)"
  rm -f "$ghfile"
  local ok=1
  [ "$rc" = "$want_exit" ] || { ok=0; printf '  ✗ exit: got %s want %s\n' "$rc" "$want_exit"; }
  if [ "$must" != "-" ] && ! printf '%s' "$out" | grep -Fq "$must"; then
    ok=0; printf '  ✗ missing expected stdout substring: %s\n' "$must"
  fi
  if [ "$mustnot" != "-" ] && printf '%s' "$out" | grep -Fq "$mustnot"; then
    ok=0; printf '  ✗ unexpected stdout substring present: %s\n' "$mustnot"
  fi
  if [ "$gh_must" != "-" ] && ! printf '%s' "$ghcontent" | grep -Fq "$gh_must"; then
    ok=0; printf '  ✗ missing expected $GITHUB_OUTPUT line: %s (got: %s)\n' "$gh_must" "$ghcontent"
  fi
  # On a failed derivation, NOTHING must be written to $GITHUB_OUTPUT (no version leaks).
  if [ "$want_exit" != "0" ] && printf '%s' "$ghcontent" | grep -Fq 'version='; then
    ok=0; printf '  ✗ version written to $GITHUB_OUTPUT on a failure path: %s\n' "$ghcontent"
  fi
  if [ "$ok" = 1 ]; then printf 'PASS  %s (exit %s)\n' "$name" "$rc"; else
    printf 'FAIL  %s\n' "$name"; printf '%s\n' "$out" | sed 's/^/      | /'; fails=$((fails+1)); fi
}

echo "== Feature 216 derive-template-version harness =="

# Happy path: derives version → stdout `version=…` AND `$GITHUB_OUTPUT` line, exit 0 (FR-005).
run "happy-path derives version (stdout + GITHUB_OUTPUT)" 0 \
  "version=0.1.52-preview.1" "-" "version=0.1.52-preview.1" -- \
  GITHUB_REF="$GOOD_REF"

# Edge: non-tag ref (e.g. a manual workflow_dispatch on a branch) → fail, no version emitted.
run "non-tag ref fails, no version" 1 \
  "is not a template tag" "version=" "-" -- \
  GITHUB_REF='refs/heads/main'

# Edge: empty version after strip → fail, no version emitted.
run "empty version fails, no version" 1 \
  "is empty after stripping" "version=" "-" -- \
  GITHUB_REF='refs/tags/fs-gg-ui-template/v'

# Edge: malformed version → fail, no version emitted.
run "malformed version fails, no version" 1 \
  "is malformed" "version=" "-" -- \
  GITHUB_REF='refs/tags/fs-gg-ui-template/vNOPE'

# Edge: unset GITHUB_REF → fail loud, no version emitted.
run "unset ref fails, no version" 1 \
  "GITHUB_REF" "version=" "-" -- \
  GITHUB_REF=

echo
if [ "$fails" = 0 ]; then echo "ALL PASS"; exit 0; else echo "$fails scenario(s) FAILED"; exit 1; fi
