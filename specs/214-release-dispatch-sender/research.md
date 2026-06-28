# Phase 0 Research: Release → Templates Dispatch Sender

All NEEDS CLARIFICATION from the Technical Context are resolved below. Each decision is grounded in
the existing repo state (workflows, tags, scripts) and the spec's fixed contract.

## R1 — Trigger: what event fires the sender?

- **Decision**: A **new** workflow `template-dispatch.yml` triggered on
  `on: push: tags: ['fs-gg-ui-template/v*']` (plus `workflow_dispatch` for manual re-fire).
- **Rationale**: The template release is published as the template-scoped tag
  `fs-gg-ui-template/v<version>` (Feature 206; the live tag `fs-gg-ui-template/v0.1.50-preview.1`
  exists in this repo, confirmed via `git tag -l`). The existing `release.yml` triggers on
  `push: tags: ['v*']` / `release: published` — and `fs-gg-ui-template/v*` does **not** match the
  `v*` glob (it starts with `f`), so `release.yml` does not see template tags today. Filtering the
  new trigger to the template tag pattern makes the trigger itself satisfy FR-007 (fires only for
  genuine template releases) and FR-001/FR-003 (version is derivable from the tag).
- **Alternatives considered**:
  - *Add a sender job to `release.yml`*: rejected — would broaden `release.yml`'s trigger to template
    tags and risk perturbing its existing gating; FR-008 wants existing release behavior untouched.
  - *Trigger on `release: published`*: rejected — coherent-set template releases are published as
    tags (Feature 206), not GitHub Releases; a tag-pattern trigger is the precise, drift-free signal.

## R2 — Version derivation

- **Decision**: Derive the version by stripping the prefix `refs/tags/fs-gg-ui-template/v` from
  `github.ref`. e.g. `refs/tags/fs-gg-ui-template/v0.1.50-preview.1` → `0.1.50-preview.1`. Validate
  the result is non-empty and matches `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$` (the existing
  registry version form). On failure, abort non-zero (do **not** send).
- **Rationale**: FR-002/FR-003 require the exact, non-hard-coded version in the receiver's form. The
  prefix-strip is the single source of the version (the tag), matching the version-coherence guard's
  treatment of `fs-gg-ui/v<V>` tags (`scripts/validate-version-coherence.fsx`). The shape check
  enforces the "version cannot be determined → fail, never send empty/placeholder" edge case.
- **Alternatives considered**: reading `<FsGgUiVersion>` from props — rejected; the *release identity*
  is the tag, and coupling to a file invites send/tag drift (FR-003 forbids hand-entered/decoupled).

## R3 — Dispatch mechanism

- **Decision**: Send via the `gh` CLI REST call
  `gh api -X POST /repos/FS-GG/FS.GG.Templates/dispatches -f event_type=fs-gg-ui-template-released
  -F client_payload[version]=<version>` (built inside `scripts/template-released-dispatch.sh`).
- **Rationale**: `gh` is preinstalled on `ubuntu-latest`; this adds **no new third-party Action**
  (constitution: dependencies minimized). The endpoint is the standard `repository_dispatch` API the
  receiver listens on. Extracting it into a script makes the logic locally runnable (`DRY_RUN=1`).
- **Alternatives considered**:
  - *`peter-evans/repository-dispatch`*: works and is widely used, but adds a pinned third-party
    dependency for a one-line REST call — rejected on the minimize-dependencies constraint.
  - *The planned reusable org dispatch-sender (FS-GG/.github#22)*: not yet available; the script is
    written so it can later delegate to that action without changing the contract.

## R4 — Credential

- **Decision**: Authenticate the `gh` call with a cross-repo token exposed as
  `secrets.TEMPLATES_DISPATCH_TOKEN` (an org/repo secret backed by the org credential of
  FS-GG/.github#21). The script fails loudly if the token env is empty/unset.
- **Rationale**: FR-004 — the default per-repo `GITHUB_TOKEN` cannot dispatch into another repo;
  cross-repo `repository_dispatch` requires a credential with `contents:write` (or equivalent
  fine-grained dispatch permission) on FS.GG.Templates. This is the **BLOCKING** dependency for the
  live path (spec Dependencies; board item #10 "Blocked by H4 · .github dispatch-sender"). The sender
  logic is authorable and testable now; live delivery waits on the credential.
- **Alternatives considered**: `GITHUB_TOKEN` — rejected (FR-004, cannot cross repos). Hard-coding a
  PAT — rejected (secret hygiene; fork exposure).

## R5 — Fork safety

- **Decision**: Gate the job with `if: github.repository == 'FS-GG/FS.GG.Rendering'`, mirroring the
  existing `release.yml` jobs.
- **Rationale**: FR-005 / US3 — forks must never send and must never see the secret. This is the
  repo's established posture (both `release.yml` jobs already use this exact guard). Forks also do not
  hold the secret, so even absent the guard a fork send would fail — the guard makes it explicit and
  silent (no spurious failure noise on forks).

## R6 — Observability & failure surfacing

- **Decision**: Run the send step under `set -euo pipefail`; echo the target repo, event type, and
  derived version before sending; let any non-zero (`gh` error, empty version, empty token) fail the
  job. `concurrency` is not shared with `release.yml`.
- **Rationale**: FR-006 / US2 — a failed or undeterminable send must be a visible job failure, never
  silently swallowed (constitution VI). The echo gives an attributable, auditable record (US2 AS1).
- **Alternatives considered**: `continue-on-error` / `|| true` — rejected (would hide failures,
  violating FR-006 and constitution VI's no-silent-failure rule).

## R7 — Idempotency / duplicate & concurrent sends

- **Decision**: No sender-side dedup; rely on the receiver's idempotency (a repeat version → no-op /
  no-change pin-bump). Each tag push sends exactly its own version.
- **Rationale**: Spec edge cases — duplicate notification is safe (receiver no-ops); concurrent
  releases each send their own version and the latest pin wins. Adding sender-side state would add
  complexity for no benefit (constitution III).

## R8 — Test strategy (constitution V) under a blocked live path

- **Decision**: Two real, runnable layers + one disclosed deferral:
  1. `actionlint` over `template-dispatch.yml` (structure, expression, `if`-guard, trigger).
  2. `DRY_RUN=1 scripts/template-released-dispatch.sh` driven with sample refs: asserts
     `refs/tags/fs-gg-ui-template/v0.1.50-preview.1` → payload `{"version":"0.1.50-preview.1"}`; and
     asserts the fail-loud branches (bad/empty version ref → non-zero & no send; empty token → non-zero).
  3. **Deferred (disclosed)**: the actual cross-repo `repository_dispatch` end-to-end (sender →
     receiver PR) is blocked by the org credential (R4); documented in quickstart as the live check to
     run once FS-GG/.github#21 lands. No synthetic stand-in pretends the network send happened.
- **Rationale**: Constitution V permits a deferred real-evidence path when disclosed; the dry-run
  proves everything controllable locally (derivation, payload, guards) without a credential.
