---
description: "Task list for Release → Templates Dispatch Sender"
---

# Tasks: Release → Templates Dispatch Sender

**Input**: Design documents from `/specs/214-release-dispatch-sender/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/template-released-dispatch.md ✓, quickstart.md ✓

**Tests**: This feature has **no F# / running app**. Per plan.md, the standing "live smoke run" is
substituted by the strongest available local proof — `actionlint` on the new workflow plus a
`DRY_RUN=1` exercise of the dispatch script (derivation, payload shape, fail-loud branches). The real
cross-repo `repository_dispatch` send is **BLOCKED** by the org cross-repo credential
(`secrets.TEMPLATES_DISPATCH_TOKEN`, FS-GG/.github#21/#22) and is recorded as a disclosed deferred
verification — never faked green.

**Organization**: Tasks grouped by user story. US1 (the send) creates the two shared files
(`scripts/template-released-dispatch.sh`, `.github/workflows/template-dispatch.yml`); US2
(observability) and US3 (fork/non-template safety) layer onto those same files, so tasks touching a
shared file are sequential within that file.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3 (maps to spec.md user stories)

## Path Conventions

CI / release automation, single repository. New artifacts live at the repo root:
`.github/workflows/template-dispatch.yml`, `scripts/template-released-dispatch.sh`,
`scripts/test-template-released-dispatch.sh`; evidence under
`specs/214-release-dispatch-sender/readiness/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Evidence scaffolding, baseline, and reproducible tooling.

- [X] T001 Create the evidence directory `specs/214-release-dispatch-sender/readiness/` and confirm the target dirs `.github/workflows/` and `scripts/` exist for the new sender artifacts
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/214-release-dispatch-sender/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; this feature adds no F#, so the set MUST be unchanged at merge) AND capture the FR-008 anchor `git diff --stat origin/main -- .github/workflows/release.yml` (expect empty) into the readiness log
- [X] T003 [P] Pin the workflow linter for reproducible evidence: install `actionlint` at a fixed version (`go install github.com/rhysd/actionlint/cmd/actionlint@v1.7.7`, not `@latest`) and record the resolved version in `specs/214-release-dispatch-sender/readiness/`. **Dependency note (constitution §Engineering Constraints):** dev/test-only linter (not shipped, not a package reference); need = reproducible structural/semantic check of `template-dispatch.yml`; pinning = fixed `@v1.7.7`; owner = this feature's CI evidence (release-automation), removable without affecting the product surface

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the fixed contract, prove derivation/payload locally before fleshing out the sender.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

> **⚠️ Early local proof (STANDING requirement, adapted — the live cross-repo send is BLOCKED).**
> This feature has no running app; the live equivalent (end-to-end dispatch → receiver PR) is blocked
> by the org credential. Per plan.md, T005 substitutes it with the strongest available local
> evidence — `actionlint` + a `DRY_RUN=1` derivation/payload check — run on a minimal scaffold BEFORE
> any guard/observability work. Treat the plan's derivation and trigger hypotheses (R1/R2) as
> unverified assumptions until T005 passes.

- [X] T004 Build the contract / root-cause map every story depends on — record in `specs/214-release-dispatch-sender/readiness/` the verified facts: (a) the live template tag exists (`git tag -l 'fs-gg-ui-template/v*'` → `fs-gg-ui-template/v0.1.50-preview.1`); (b) `release.yml`'s `v*` trigger does NOT match `fs-gg-ui-template/v*` (R1, FR-008); (c) the receiver's event id `fs-gg-ui-template-released` and `client_payload.version` shape from `contracts/template-released-dispatch.md`
- [X] T005 **Early local proof (substitute live smoke run)**: scaffold minimal `scripts/template-released-dispatch.sh` + `.github/workflows/template-dispatch.yml`, then run `actionlint .github/workflows/template-dispatch.yml` (exit 0) and one `DRY_RUN=1 GH_TOKEN=dummy GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.50-preview.1 scripts/template-released-dispatch.sh` confirming derived version `0.1.50-preview.1` and payload shape; capture both logs to `specs/214-release-dispatch-sender/readiness/`
- [X] T006 [P] Create the dry-run test-harness seam `scripts/test-template-released-dispatch.sh` (POSIX) stubbing the four quickstart Layer-2 scenarios (happy / empty-version / malformed-version / missing-token) so each story asserts against it
- [X] T007 Confirm the sender contract seam is coherent with the receiver: reconcile `specs/214-release-dispatch-sender/contracts/template-released-dispatch.md` event id + payload against the `fs-gg-ui-template` cross-repo registry entry (FR-009) before implementation

**Checkpoint**: Contract facts recorded, derivation/payload proven locally on a scaffold — user-story implementation can begin.

---

## Phase 3: User Story 1 - Downstream pin updates automatically on a template release (Priority: P1) 🎯 MVP

**Goal**: A canonical `fs-gg-ui-template/v*` tag push sends exactly one `fs-gg-ui-template-released` dispatch to FS.GG.Templates carrying the released version.

**Independent Test**: `DRY_RUN=1` with `GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.50-preview.1` prints `event_type=fs-gg-ui-template-released` and `client_payload.version=0.1.50-preview.1` and exits 0 (the live end-to-end send is the deferred Layer-3 check, T019).

- [X] T008 [US1] Implement version derivation in `scripts/template-released-dispatch.sh`: strip the literal prefix `refs/tags/fs-gg-ui-template/v` from `GITHUB_REF`, validate non-empty and matching `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$` (FR-002, FR-003, R2)
- [X] T009 [US1] Build payload + send in `scripts/template-released-dispatch.sh`: `gh api -X POST /repos/FS-GG/FS.GG.Templates/dispatches -f event_type=fs-gg-ui-template-released -F 'client_payload[version]=<version>'`, with `DRY_RUN=1` printing the exact payload and skipping the network call (FR-001, FR-002, R3) — depends on T008
- [X] T010 [US1] Implement the sender workflow `.github/workflows/template-dispatch.yml`: `on: push: tags: ['fs-gg-ui-template/v*']` plus a `workflow_dispatch` manual entry; a single job that passes `GITHUB_REF` and `GH_TOKEN` (`secrets.TEMPLATES_DISPATCH_TOKEN`) into `scripts/template-released-dispatch.sh` (FR-001, FR-003, R1) — depends on T009 (script interface)
- [X] T011 [P] [US1] Assert the happy path in `scripts/test-template-released-dispatch.sh`: DRY_RUN happy-path → exit 0 with `event_type=fs-gg-ui-template-released` and `client_payload.version=0.1.50-preview.1` (quickstart Layer 2; SC-001, SC-002)

**Checkpoint**: The send (derivation + payload + dispatch + workflow trigger) is functional and dry-run-provable end of US1 — MVP.

---

## Phase 4: User Story 2 - Release operators can see whether the notification was sent (Priority: P2)

**Goal**: Every send outcome (success or any failure) is a visible, attributable step result — never silently swallowed.

**Independent Test**: Force each failure path under `DRY_RUN=1` (undeterminable version, empty `GH_TOKEN`) and confirm a non-zero exit with a clear message and no payload printed.

- [X] T012 [US2] Harden `scripts/template-released-dispatch.sh`: run under `set -euo pipefail`; echo target repo / event type / derived version before sending; non-zero exit and NO payload on undeterminable version and on empty `GH_TOKEN` — no `|| true` masking (FR-006, FR-004, R6, edge cases) — same file as T008/T009, sequential
- [X] T013 [US2] Surface the outcome in `.github/workflows/template-dispatch.yml`: confirm no `continue-on-error` / `|| true` masks the send step and the step reports success/failure attributably (FR-006, US2 AS1/AS2); **also assert the negative for FR-004** — the send step's `GH_TOKEN` is wired ONLY to `secrets.TEMPLATES_DISPATCH_TOKEN` and never falls back to the default per-repo `GITHUB_TOKEN` — same file as T010, sequential
- [X] T014 [P] [US2] Assert the fail-loud branches in `scripts/test-template-released-dispatch.sh`: empty-version ref → non-zero & no payload; malformed version → non-zero; empty token → non-zero naming the missing `TEMPLATES_DISPATCH_TOKEN`/`GH_TOKEN` (quickstart Layer 2 edges; SC-004)

**Checkpoint**: Happy path AND every failure path are visibly surfaced and asserted; US1 still passes.

---

## Phase 5: User Story 3 - Notifications fire only for genuine canonical template releases (Priority: P3)

**Goal**: No fork and no non-template event ever sends — the trigger pattern and canonical-repo guard are the boundary.

**Independent Test**: Static/deferred reasoning (live fork run needs the credential): confirm the `if` guard skips on a non-canonical `github.repository`, and that a `v*` release tag does not match the `fs-gg-ui-template/v*` trigger.

- [X] T015 [US3] Add the canonical-repo guard to `.github/workflows/template-dispatch.yml`: `if: github.repository == 'FS-GG/FS.GG.Rendering'` on the send job, mirroring `release.yml` (FR-005, US3, R5) — same file as T010/T013, sequential
- [X] T016 [US3] Confirm the trigger IS the genuine-template guard: the `on: push: tags: ['fs-gg-ui-template/v*']` filter means non-template (`v*`) events never match; document in the contract note that a `workflow_dispatch` manual run lands on a non-tag ref → version-derivation fails loud, never a spurious send (FR-007) — `.github/workflows/template-dispatch.yml` + `specs/214-release-dispatch-sender/contracts/template-released-dispatch.md`
- [X] T017 [P] [US3] Record the negative-path reasoning in `specs/214-release-dispatch-sender/readiness/`: fork push skips via the `if` guard (and forks lack the secret); a `v*` tag does not match the trigger (SC-003) — disclosed as static/deferred (not live-runnable without a fork + credential)

**Checkpoint**: Fork/non-template boundary in place and reasoned; US1 and US2 still pass.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T018 [P] Run the full quickstart Layers 1+2 and capture evidence into `specs/214-release-dispatch-sender/readiness/`: `actionlint .github/workflows/template-dispatch.yml` clean + all four `scripts/test-template-released-dispatch.sh` scenarios green; include a one-line confirmation that the send step references `secrets.TEMPLATES_DISPATCH_TOKEN` and not `GITHUB_TOKEN` (FR-004 negative check)
- [X] T019 [P] Record the deferred live check (quickstart Layer 3) as a disclosed, BLOCKED verification in `specs/214-release-dispatch-sender/readiness/` AND state it in the PR description: **SC-001 and SC-005 (zero-manual, 100%-of-releases) remain PENDING-CREDENTIAL** — the end-to-end cross-repo send + receiver pin-bump PR awaits `secrets.TEMPLATES_DISPATCH_TOKEN` (FS-GG/.github#21/#22). Local dry-run green ≠ live SC-001/SC-005 proven; do NOT fake it green
- [X] T020 Verify the FR-008 regression guard: `git diff --stat origin/main -- .github/workflows/release.yml` returns no output (release.yml byte-unchanged; Package.Tests / template-instantiation / Feature-212 stock-root gating intact)
- [X] T021 [P] Update the cross-repo `fs-gg-ui-template` registry/contract entry to mark the sender half realized (FR-009) via the `cross-repo-coordination` protocol, and reflect closure on board item FS-GG/FS.GG.Rendering#10

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. T005 (early local proof) must pass before US implementation.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 creates the shared files; US2 and US3 layer onto them (so they are sequenced after US1 on the shared files, not parallel to it).
- **Polish (Phase 6)**: Depends on US1–US3 complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational — authors `template-released-dispatch.sh` + `template-dispatch.yml`. The MVP.
- **US2 (P2)**: After US1 — hardens the same script/workflow for fail-loud observability. Independently testable via the failure scenarios.
- **US3 (P3)**: After US1 — adds the canonical-repo guard and documents the trigger boundary on the same workflow. Independently testable via static guard reasoning.

### Within / Across Stories — shared-file note

`scripts/template-released-dispatch.sh` is touched by T008, T009 (US1) and T012 (US2): sequential.
`.github/workflows/template-dispatch.yml` is touched by T010 (US1), T013 (US2), T015/T016 (US3): sequential.
The test harness `scripts/test-template-released-dispatch.sh` is a different file: its assertion
tasks (T011, T014) are `[P]` relative to the implementation tasks.

### Parallel Opportunities

- T003 (tooling) parallel with T001/T002 setup.
- T006 (test-harness seam) parallel with T004/T005/T007 within Foundational.
- T011, T014, T017 (assertions/evidence in separate files) parallel within their phases.
- Polish: T018, T019, T021 parallel ([P]); T020 is a standalone verification.

---

## Parallel Example: Foundational

```bash
# After T004/T005 land the scaffold, the harness seam is independent:
Task: "T006 Create POSIX dry-run harness scripts/test-template-released-dispatch.sh"
Task: "T007 Reconcile contract doc against the fs-gg-ui-template registry entry"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (incl. the **early local proof** T005 that validates
derivation/payload on a scaffold before guard/observability work) → 3. Phase 3 US1 → **STOP & VALIDATE**
the dry-run happy path → the sender is demonstrably correct locally (live send deferred, T019).

### Incremental Delivery

US1 (the send) → US2 (make every outcome visible) → US3 (lock the fork/non-template boundary) →
Polish (full quickstart evidence + FR-008 regression guard + registry coherence). Each story is an
independently testable increment that does not break the previous.

---

## Notes

- This feature touches **zero F#** and no public module surface — the `.fsi`/surface obligations are N/A; the contract obligation (FR-009) is met via the contract doc + registry update (T007, T021).
- The real cross-repo send is BLOCKED (org credential FS-GG/.github#21/#22) and is a **disclosed deferred** check (T019) — never reported green without the credential. **SC-001 / SC-005 are therefore PENDING-CREDENTIAL at merge** and MUST be flagged as such in the PR/readiness, not implied green by the local dry-run.
- FR-008 is the hard regression guard: `release.yml` MUST stay byte-unchanged (T002 anchor, T020 verify).
- Commit after each task or logical group; stop at any checkpoint to validate the story independently.
