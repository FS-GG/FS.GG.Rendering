# Readiness — No-regression baseline & validation ledger

Standing evidence for Feature 216. Layers 1–2 + the `release.yml` byte-diff guard run locally;
Layer 3 (live cross-repo send) is **disclosed-deferred** on the org App secrets (FR-008/FR-009).

> **Adapted baseline (STANDING).** This feature has no F# test projects, so `baseline-tests.fsx`
> does not apply. The honest CI analogue: `actionlint` lints both workflows clean today and
> `release.yml` already matches `origin/main` — so any later diff/lint failure is unambiguously
> caused by this migration, not pre-existing drift.

## T002 — No-regression baseline (BEFORE any edit)

```text
$ actionlint .github/workflows/template-dispatch.yml .github/workflows/release.yml
(exit 0 — both workflows lint clean)

$ git fetch origin main && git diff --stat origin/main -- .github/workflows/release.yml
(exit 0 — no diff; release.yml starts byte-identical to origin/main)
```

Clean starting state confirmed: both lint at exit 0, `release.yml` has no diff vs `origin/main`.

## T005 — Early behavior smoke (existing Feature 214 sender, DRY_RUN) — behavior to PRESERVE

Drove the existing `scripts/template-released-dispatch.sh` (`DRY_RUN=1`, `GH_TOKEN=dummy`) through the
boundary refs to record exactly what the new derive-only helper must preserve:

| `GITHUB_REF` | exit | observed |
|--------------|------|----------|
| `refs/tags/fs-gg-ui-template/v0.1.52-preview.1` | 0 | derives `version=0.1.52-preview.1` |
| `refs/heads/main` | 1 | fail-loud "not a template tag"; no version |
| `refs/tags/fs-gg-ui-template/v` | 1 | fail-loud "empty after stripping"; no version |
| `refs/tags/fs-gg-ui-template/vNOPE` | 1 | fail-loud "malformed"; no version |
| `` (empty) | 1 | fail-loud "GITHUB_REF unset/empty"; no version |

**Behavior to preserve in `derive-template-version.sh`**: strip `refs/tags/fs-gg-ui-template/v`,
assert `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`, emit the version on the happy path, and
fail-loud (non-zero, no version) on non-tag / empty / malformed. The Feature 214
credential-missing + DRY_RUN-payload behavior is intentionally dropped — the credential + POST move to
the reusable workflow (research R3). The true live POST is Layer 3 (deferred, FR-008).

## T011 — Local validation (fail-before / pass-after) — US1

**Fail-before** (`scripts/derive-template-version.sh` absent):
```text
$ bash scripts/test-derive-template-version.sh   → exit 1 (5 scenarios fail, exit 127 "No such file")
```
**Pass-after** (harness + helper landed):
```text
$ actionlint .github/workflows/template-dispatch.yml      → exit 0   (Layer 1)
$ bash scripts/test-derive-template-version.sh            → exit 0   (Layer 2, ALL PASS)
```
Harness scenarios: happy-path (`v0.1.52-preview.1` → `version=…` on stdout AND `$GITHUB_OUTPUT`,
exit 0) + non-tag / empty-after-strip / malformed / unset-ref (all exit 1, no version emitted).

## T014 — Stored-secret absence check (US2)

```text
$ grep -rn 'TEMPLATES_DISPATCH_TOKEN\|GH_TOKEN' .github scripts   → no matches
```
No long-lived cross-repo secret remains anywhere; the `dispatch` job authenticates solely via the
reusable workflow's run-time-minted App token (FR-002/SC-003). The retired Feature 214 send artifacts
(`scripts/template-released-dispatch.sh` + its harness) were deleted (T013).

## T015 — Binding-mechanism check (US2)

`.github/workflows/template-dispatch.yml` maps the hyphenated callee ports explicitly:
`app-id: ${{ secrets.APP_ID }}` / `app-private-key: ${{ secrets.APP_PRIVATE_KEY }}`. **No
`secrets: inherit` directive** exists (the only `inherit` text is the comment documenting *why*
inherit cannot bind hyphenated ports — research R1).

## T016 / T017 / T018 — Fork-safety, trigger disjointness & release.yml byte-diff (US3)

- **T016 fork safety**: guard `if: github.repository == 'FS-GG/FS.GG.Rendering'` is on the `derive`
  job; `dispatch` declares `needs: derive`. On a fork `derive` is skipped, so `dispatch` never starts
  — no send, no credential exposure (FR-004/US3).
- **T017 trigger disjointness**: `template-dispatch.yml` triggers only on `fs-gg-ui-template/v*`
  (+ inspection-only `workflow_dispatch`); `release.yml` triggers on bare `v*`. The slash means the
  two globs cannot match the same tag (FR-006, research R6).
- **T018 release.yml byte-diff**:
  ```text
  $ git fetch origin main && git diff --exit-code origin/main -- .github/workflows/release.yml
  → exit 0, no diff — release.yml byte-identical (SC-004/FR-006)
  ```
