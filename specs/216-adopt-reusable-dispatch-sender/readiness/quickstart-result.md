# Readiness — Full quickstart validation (T020)

Ran 2026-06-28 on this dev box. End-to-end `quickstart.md` re-run: Layers 1–2 + the `release.yml`
byte-diff regression guard all green; Layer 3 (live cross-repo send) is **disclosed-deferred** on the
org App secrets (FR-008/FR-009) — not run, not faked.

| Check | Command | Result |
|-------|---------|--------|
| Layer 1 — workflow lint | `actionlint .github/workflows/template-dispatch.yml` | **exit 0** ✓ |
| Layer 2 — derive harness | `scripts/test-derive-template-version.sh` | **ALL PASS** (exit 0) ✓ |
| Regression guard | `git diff --exit-code origin/main -- .github/workflows/release.yml` | **exit 0, no diff** ✓ |
| (sanity) release.yml lint | `actionlint .github/workflows/release.yml` | exit 0 ✓ |

## Layer 3 — live cross-repo send (DISCLOSED-DEFERRED, NOT run)

**Status: BLOCKED / deferred — not executed, not summarized as green.** Gated on the FR-008
cross-repo request (T006, filed on `FS-GG/.github#22`): confirm the exact `app-id`/`app-private-key`
org secret names (working assumption `APP_ID`/`APP_PRIVATE_KEY`) and the App's install scope on
`FS-GG/FS.GG.Rendering` + `FS-GG/FS.GG.Templates`. Once confirmed, push a real `fs-gg-ui-template/v*`
tag and capture the sender run URL + the FS.GG.Templates pin-bump PR URL (within 10 min of the push,
SC-002), then close `FS-GG/FS.GG.Rendering#10` and move the board item Blocked → Done (T012).

The reusable workflow was already smoke-tested end-to-end by `FS-GG/.github` (App token →
`repository_dispatch` accepted, per `.github#22`'s 2026-06-28 verification note), so the live path is
expected to go green on the first authenticated release; the Rendering-side adoption is complete and
lint/harness-proven now.

## Caveats (visible, per repository evidence rules)

- **Layer 3 not run** — environment-limited (no org App secret access from this checkout). Kept as a
  visible deferred check; **not** counted toward "fully green".
- All Layer 1/2/regression evidence above is reproducible locally with no credential.
