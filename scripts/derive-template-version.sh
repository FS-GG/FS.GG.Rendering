#!/usr/bin/env bash
#
# derive-template-version.sh — Feature 216 (Adopt Reusable App-Token Dispatch-Sender).
#
# Single responsibility: derive + validate the released template version from the triggering tag ref
# and expose it. The cross-repo POST and credential are NOT here anymore — they live in the org
# reusable workflow FS-GG/.github/.github/workflows/dispatch-sender.yml, which the `dispatch` job
# `uses:`. This helper is the `derive` job's body (repurposed from the Feature 214
# template-released-dispatch.sh derivation half; research R3).
#
# Behavior (preserved from Feature 214, send path removed):
#   - strip `refs/tags/fs-gg-ui-template/v` from $GITHUB_REF
#   - assert ^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$
#   - on success: emit `version=<v>` to stdout AND append it to $GITHUB_OUTPUT (the job output)
#   - on non-tag / empty-after-strip / malformed / unset: fail loud (non-zero, NOTHING emitted) (FR-005)
#
# Env:
#   RELEASE_TAG_REF  the ref to derive from, when the caller knows it. Takes precedence.  (optional)
#   GITHUB_REF       triggering ref, e.g. refs/tags/fs-gg-ui-template/v0.1.52-preview.1   (fallback)
#   GITHUB_OUTPUT    CI step-output file; appended on success only (optional locally)
#
# WHY RELEASE_TAG_REF EXISTS. On a `workflow_call` the runner sets GITHUB_REF to the CALLER's ref
# (`refs/heads/main`), not the tag. release-tags.yml therefore hands us the tag ref that its cut
# version implies. It cannot do that through GITHUB_REF: the runner reserves the GITHUB_* namespace
# and re-injects its own value, so a step-level `env: GITHUB_REF:` is declared and then silently
# ignored — the step log shows the override while the process sees `refs/heads/main`. That is exactly
# how the FS.GG.UI 0.4.0 cut lost its Templates dispatch. Read a name the runner does not own.
set -euo pipefail

readonly TAG_PREFIX='refs/tags/fs-gg-ui-template/v'
readonly VERSION_RE='^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$'

fail() { printf 'derive-template-version: ERROR: %s\n' "$1" >&2; exit 1; }

ref="${RELEASE_TAG_REF:-${GITHUB_REF:-}}"
[ -n "$ref" ] || fail "neither RELEASE_TAG_REF nor GITHUB_REF is set; cannot determine the released version."

# Derive version: strip the canonical template tag prefix (FR-005).
case "$ref" in
  "${TAG_PREFIX}"*) version="${ref#"$TAG_PREFIX"}" ;;
  *) fail "ref '$ref' is not a template tag (expected ${TAG_PREFIX}<version>); refusing to derive." ;;
esac

# Undeterminable-version guard (FR-005): non-empty AND valid. Nothing is emitted on failure.
[ -n "$version" ] || fail "version is empty after stripping prefix from ref '$ref'; refusing to derive."
if ! printf '%s' "$version" | grep -Eq "$VERSION_RE"; then
  fail "derived version '$version' (from ref '$ref') is malformed; expected ${VERSION_RE}; refusing to derive."
fi

# Success: emit to stdout (operator-visible) AND to $GITHUB_OUTPUT so the `derive` job can expose it
# as `outputs.version` for the `dispatch` job's `version:` input.
printf 'version=%s\n' "$version"
if [ -n "${GITHUB_OUTPUT:-}" ]; then
  printf 'version=%s\n' "$version" >> "$GITHUB_OUTPUT"
fi
