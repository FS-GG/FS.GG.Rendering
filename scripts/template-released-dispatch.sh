#!/usr/bin/env bash
#
# template-released-dispatch.sh — Feature 214 (Release → Templates Dispatch Sender)
#
# Sends the `fs-gg-ui-template-released` repository_dispatch to FS-GG/FS.GG.Templates so its
# upstream-bump.yml opens a pin-bump PR for the just-released template coherent-set version.
#
# It derives the version from the triggering tag ref (refs/tags/fs-gg-ui-template/v<version>),
# validates it, builds the JSON payload, and dispatches via the `gh` REST call. Every failure mode
# (undeterminable version, missing credential, gh/network/receiver error) is fail-loud (FR-006).
#
# Contract (source of truth = the receiver): specs/214-release-dispatch-sender/contracts/
#   event_type           = fs-gg-ui-template-released                         (FR-001)
#   client_payload.version matches ^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$ (FR-002)
#   target               = FS-GG/FS.GG.Templates
#
# Env:
#   GITHUB_REF  triggering ref, e.g. refs/tags/fs-gg-ui-template/v0.1.50-preview.1  (required)
#   GH_TOKEN    cross-repo credential from secrets.TEMPLATES_DISPATCH_TOKEN         (required)
#   DRY_RUN=1   print the payload it WOULD send and skip the network call           (optional)
#
set -euo pipefail

readonly TARGET_REPO='FS-GG/FS.GG.Templates'
readonly EVENT_TYPE='fs-gg-ui-template-released'
readonly TAG_PREFIX='refs/tags/fs-gg-ui-template/v'
readonly VERSION_RE='^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$'

fail() { printf 'template-released-dispatch: ERROR: %s\n' "$1" >&2; exit 1; }

ref="${GITHUB_REF:-}"
[ -n "$ref" ] || fail "GITHUB_REF is unset/empty; cannot determine the released version."

# Derive version: strip the canonical template tag prefix (FR-002/FR-003).
case "$ref" in
  "${TAG_PREFIX}"*) version="${ref#"$TAG_PREFIX"}" ;;
  *) fail "ref '$ref' is not a template tag (expected ${TAG_PREFIX}<version>); refusing to send." ;;
esac

# Undeterminable-version guard (FR-006, edge "version cannot be determined"): non-empty AND valid.
# No payload is printed or sent on failure.
[ -n "$version" ] || fail "version is empty after stripping prefix from ref '$ref'; refusing to send."
if ! printf '%s' "$version" | grep -Eq "$VERSION_RE"; then
  fail "derived version '$version' (from ref '$ref') is malformed; expected ${VERSION_RE}; refusing to send."
fi

# Credential-present guard (FR-004/FR-006): fail loud if the cross-repo token is missing, even in
# DRY_RUN — never attempt or simulate an unauthenticated send.
[ -n "${GH_TOKEN:-}" ] || fail "GH_TOKEN (from secrets.TEMPLATES_DISPATCH_TOKEN) is empty/unset; cannot dispatch to ${TARGET_REPO}."

# Attributable record BEFORE sending (US2/FR-006): target, event, and derived version are visible
# in the run log whether the send then succeeds or fails.
printf 'template-released-dispatch: target=%s event_type=%s client_payload.version=%s\n' \
  "$TARGET_REPO" "$EVENT_TYPE" "$version"

if [ "${DRY_RUN:-}" = "1" ]; then
  printf 'DRY_RUN=1 — skipping network dispatch. Would POST /repos/%s/dispatches:\n' "$TARGET_REPO"
  printf 'event_type=%s\n' "$EVENT_TYPE"
  printf 'client_payload.version=%s\n' "$version"
  exit 0
fi

# Real send. `gh api` exits non-zero on any HTTP/receiver error; `set -e` propagates it (FR-006).
gh api -X POST "/repos/${TARGET_REPO}/dispatches" \
  -f "event_type=${EVENT_TYPE}" \
  -F "client_payload[version]=${version}"

printf 'template-released-dispatch: dispatched %s (version %s) to %s\n' \
  "$EVENT_TYPE" "$version" "$TARGET_REPO"
