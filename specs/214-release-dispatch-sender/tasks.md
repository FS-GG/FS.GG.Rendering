---
description: "Task list for Release → Templates Dispatch Sender"
---

# Tasks: Release → Templates Dispatch Sender

**Input**: Design documents from `/specs/214-release-dispatch-sender/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/template-released-dispatch.md ✓, quickstart.md ✓

**Tests**: No F# test projects are added (this feature adds no F# code). The mandated test evidence is
`actionlint` + a `DRY_RUN=1` harness for the dispatch script — these are tracked as first-class tasks,
not optional extras, per plan.md "Standing assumption" and research R8.

**Organization**: Tasks are grouped by user story. NOTE: all three stories are realized inside the
**same two files** (`.github/workflows/template-dispatch.yml` and `scripts/template-released-dispatch.sh`).
They are still independently *testable* (each maps to a distinct, separately-verifiable behavior), but
tasks touching the same file are **not** parallelizable — `[P]` is used sparingly and only across
genuinely distinct files.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: US1 / US2 / US3 (Setup / Foundational / Polish carry no story label)
- Exact file paths are included in each task.

## Path Conventions

This is a CI / release-automation feature in a single repository. Artifacts live at the repo root:

- `.github/workflows/template-dispatch.yml` — NEW sender workflow
- `scripts/template-released-dispatch.sh` — NEW dispatch script (`DRY_RUN=1` testable)
- `specs/214-release-dispatch-sender/readiness/` — evidence/baseline output

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Tooling and a recorded pre-change baseline so FR-008 (no release regression) is provable.

> **⚠️ Baseline scope (adapted, honest).** This feature adds **no F# code** and touches no module
> surface, so the .NET `*.Tests.fsproj` suite is *not* the regression surface here — `release.yml`
> byte-equality is (FR-008). The baseline below therefore (a) records the current `release.yml`
> content hash as the regression anchor and (b) confirms the workflow-lint toolchain. We do **not**
> claim a full red/green .NET baseline that this change cannot affect.

- [X] T001 Create the readiness output dir `specs/214-release-dispatch-sender/readiness/` and record the FR-008 regression anchor: `git rev-parse HEAD:.github/workflows/release.yml` (or `git hash-object .github/workflows/release.yml`) into `specs/214-release-dispatch-sender/readiness/release-yml-baseline.txt`
- [X] T002 [P] Confirm `actionlint` is available (`actionlint -version`); if absent, install per quickstart.md Prerequisites pinned to a fixed version (`go install github.com/rhysd/actionlint/cmd/actionlint@v1.7.7`, not `@latest`, for reproducible lint evidence) and record the exact version in `specs/214-release-dispatch-sender/readiness/baseline.md`
- [X] T003 [P] Confirm `gh` CLI is available (`gh --version`) and POSIX `bash` (`bash --version`) for local script execution; record in `specs/214-release-dispatch-sender/readiness/baseline.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Pin the exact contract the sender must realize, and establish the local proof harness,
BEFORE writing the workflow/script — so US1–US3 build on a verified target, not a hypothesis.

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

> **⚠️ Early live proof (adapted per plan.md, lines 24–30 + research R8).** This feature has no
> running "app"; the equivalent live check is the end-to-end cross-repo dispatch, which is **BLOCKED**
> by the org credential (FS-GG/.github#21/#22). The strongest *available* proof — and the standing
> substitute for the live smoke run — is `actionlint` on the workflow + a `DRY_RUN=1` exercise of the
> script that confirms derived version, payload shape, and the fail-loud paths. T006 schedules that
> harness skeleton up front; the real send is a **disclosed deferred verification** (Phase 6 / T021),
> never a silent assumption.

- [X] T004 Re-read the receiver contract source of truth `specs/214-release-dispatch-sender/contracts/template-released-dispatch.md` and `data-model.md`; capture in `specs/214-release-dispatch-sender/readiness/contract-pin.md` the exact non-negotiables the sender must hit: `event_type=fs-gg-ui-template-released`, `client_payload.version` form `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`, target `FS-GG/FS.GG.Templates`, tag-prefix `refs/tags/fs-gg-ui-template/v`
- [X] T005 [P] Verify the trigger signal exists and is distinct from `release.yml`: `git tag -l 'fs-gg-ui-template/*'` shows `fs-gg-ui-template/v0.1.50-preview.1`, and confirm `release.yml`'s full trigger set — `release: types:[published]` + `push: tags:['v*']` + `workflow_dispatch` — produces NO `fs-gg-ui-template/v*` push (`v*` does NOT glob-match `fs-gg-ui-template/v…`, and neither the release nor manual entry creates that tag); note this in `readiness/contract-pin.md` as the FR-007/FR-008 basis
- [X] T006 Create the dry-run proof harness `scripts/test-template-released-dispatch.sh` (a small POSIX driver invoking the not-yet-written `scripts/template-released-dispatch.sh` with the four quickstart scenarios: happy path, empty version, malformed version, missing token) — assertions defined now, run in each user-story phase as that behavior lands

**Checkpoint**: Contract pinned, trigger signal confirmed, dry-run harness ready — sender implementation can begin.

---

## Phase 3: User Story 1 - Downstream pin updates automatically on a template release (Priority: P1) 🎯 MVP

**Goal**: A canonical template tag push derives the released version from the tag and dispatches
`fs-gg-ui-template-released` with `client_payload.version` to `FS-GG/FS.GG.Templates`, with zero
manual steps (FR-001, FR-002, FR-003, SC-001, SC-002).

**Independent Test**: `DRY_RUN=1 GH_TOKEN=dummy GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.50-preview.1 scripts/template-released-dispatch.sh`
prints `event_type=fs-gg-ui-template-released` and `client_payload.version=0.1.50-preview.1`, exit 0
(quickstart Layer 2 happy path). Live round-trip is the deferred check in T021.

- [X] T007 [US1] Create `scripts/template-released-dispatch.sh`: derive version by stripping `refs/tags/fs-gg-ui-template/v` from `GITHUB_REF`, build the JSON payload, and dispatch via `gh api -X POST /repos/FS-GG/FS.GG.Templates/dispatches -f event_type=fs-gg-ui-template-released -F 'client_payload[version]=<version>'`; honor `DRY_RUN=1` to print `event_type=` / `client_payload.version=` and skip the network call (FR-001, FR-002, FR-003, R3)
- [X] T008 [US1] Create `.github/workflows/template-dispatch.yml` with `on: push: tags: ['fs-gg-ui-template/v*']` (plus `workflow_dispatch`), a single job that checks out and runs `scripts/template-released-dispatch.sh`, passing `GITHUB_REF` and `GH_TOKEN: ${{ secrets.TEMPLATES_DISPATCH_TOKEN }}` (FR-001, R1, R4)
- [X] T009 [US1] Run the happy-path proof: `DRY_RUN=1 GH_TOKEN=dummy GITHUB_REF=refs/tags/fs-gg-ui-template/v0.1.50-preview.1 scripts/template-released-dispatch.sh` via `scripts/test-template-released-dispatch.sh`; confirm payload `{"version":"0.1.50-preview.1"}` and exit 0; capture output to `specs/214-release-dispatch-sender/readiness/dry-run-us1.txt` (SC-002 no drift)

**Checkpoint**: The sender derives and (dry-run) emits the correct payload — MVP behavior provable without the credential.

---

## Phase 4: User Story 2 - Release operators can see whether the notification was sent (Priority: P2)

**Goal**: Success and failure of the send are visible/attributable in the run; no failure (missing
credential, network/receiver error, undeterminable version) is silently swallowed (FR-006, US2, SC-004).

**Independent Test**: Force each failure mode in dry-run and confirm a non-zero exit with a clear
message and NO payload sent: empty version ref, malformed version, empty `GH_TOKEN`
(quickstart Layer 2 edge cases).

- [X] T010 [US2] Harden `scripts/template-released-dispatch.sh` for fail-loud: run under `set -euo pipefail`, echo target repo + event type + derived version BEFORE sending (attributable record), and exit non-zero on `gh` error (FR-006, R6, US2 AS1)
- [X] T011 [US2] Add the undeterminable-version guard in `scripts/template-released-dispatch.sh`: if the stripped version is empty OR fails `^[0-9]+\.[0-9]+\.[0-9]+(-preview\.[0-9]+)?$`, print a message naming the bad ref and exit non-zero WITHOUT printing/sending a payload (FR-006, edge "version cannot be determined")
- [X] T012 [US2] Add the credential-present guard in `scripts/template-released-dispatch.sh`: if `GH_TOKEN` (from `TEMPLATES_DISPATCH_TOKEN`) is empty/unset, fail non-zero with a message naming the missing token, even under `DRY_RUN=1` (FR-004, FR-006, edge "credential missing")
- [X] T013 [US2] Run the fail-loud proofs via `scripts/test-template-released-dispatch.sh`: empty-version ref, `vNOPE` malformed, and empty `GH_TOKEN` each yield non-zero + no payload; capture to `specs/214-release-dispatch-sender/readiness/dry-run-us2.txt` (SC-004)

**Checkpoint**: Every controllable failure mode surfaces visibly; happy path (US1) still passes.

---

## Phase 5: User Story 3 - Notifications fire only for genuine canonical template releases (Priority: P3)

**Goal**: The send fires only for real template tags on the canonical repo — never from forks, never
for non-template events (FR-005, FR-007, US3, SC-003).

**Independent Test**: `actionlint` confirms the `on: push: tags: ['fs-gg-ui-template/v*']` trigger and
the `if: github.repository == 'FS-GG/FS.GG.Rendering'` guard parse; reason about the negative cases
(fork → guard skips; non-template tag → trigger doesn't match) per quickstart Layer 3 negative checks.

- [X] T014 [US3] Add the canonical-repo guard to `.github/workflows/template-dispatch.yml`: `if: github.repository == 'FS-GG/FS.GG.Rendering'` on the send job, mirroring `release.yml` (FR-005, R5, US3 AS1)
- [X] T015 [US3] Confirm the trigger pattern in `.github/workflows/template-dispatch.yml` is exactly `fs-gg-ui-template/v*` (not `v*`), so non-template release events never match (FR-007, R1, US3 AS2); note the distinction from `release.yml`'s `v*` in `readiness/contract-pin.md`
- [X] T016 [US3] Run `actionlint .github/workflows/template-dispatch.yml` (quickstart Layer 1); confirm exit 0 and that trigger + guard + step expressions parse; capture to `specs/214-release-dispatch-sender/readiness/actionlint.txt` (SC-003 basis)

**Checkpoint**: All three stories independently provable in dry-run/lint; sender complete pending live credential.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Contract coherence, regression proof (FR-008), and disclosure of the deferred live path.

- [X] T017 [P] Update the `fs-gg-ui-template` cross-repo registry/contract entry to record the sender half as realized (FR-009); use the `cross-repo-coordination` skill — keep the registry the source of truth and coherent with the receiver
- [X] T018 [P] Confirm FR-008 regression guard: `git diff --stat origin/main -- .github/workflows/release.yml` produces NO output, and the content hash matches `specs/214-release-dispatch-sender/readiness/release-yml-baseline.txt` (T001); record in `readiness/baseline.md`
- [X] T019 [P] Run the full quickstart.md Layer 1 + Layer 2 sequence end-to-end one more time and confirm every expected exit code/message; record the consolidated result in `specs/214-release-dispatch-sender/readiness/quickstart-run.md`
- [X] T020 Update the Coordination board item FS-GG/FS.GG.Rendering#10 (and parent FS-GG/.github#16) noting the sender is authored + locally proven, still blocked on the live credential (FS-GG/.github#21/#22), via the `cross-repo-coordination` skill
- [X] T021 Record the **disclosed deferred verification** in `specs/214-release-dispatch-sender/readiness/deferred-live-check.md`: the end-to-end cross-repo send (sender → receiver pin-bump PR, SC-001/SC-002/SC-005) is BLOCKED by the org credential and MUST be run per quickstart Layer 3 once FS-GG/.github#21 lands — explicitly NOT faked green

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories (pins the contract + builds the proof harness).
- **User Stories (Phase 3–5)**: All depend on Foundational. They share two files, so:
  - **US1 (P1)** creates the script + workflow — it is the structural prerequisite for US2/US3 edits to those same files.
  - **US2 (P2)** and **US3 (P3)** harden the files US1 created; they are independently *testable* but, because they edit the same two files, must be sequenced (not run in parallel against each other).
- **Polish (Phase 6)**: Depends on US1–US3 complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on other stories — the MVP.
- **US2 (P2)**: Edits the script US1 created (observability/fail-loud). Independently testable via the edge-case dry-runs.
- **US3 (P3)**: Edits the workflow US1 created (guards/trigger). Independently testable via `actionlint` + negative reasoning.

### Within Each User Story

- Implementation lands, then its dry-run/lint proof runs (T009 for US1, T013 for US2, T016 for US3).

### Parallel Opportunities

- Setup: T002 and T003 in parallel (different checks).
- Foundational: T005 parallel with T004; T006 after the contract is pinned.
- Polish: T017, T018, T019 in parallel (registry / git-diff / quickstart — distinct artifacts).
- **NOT parallel**: any two tasks editing `scripts/template-released-dispatch.sh` (T007, T010, T011, T012) or `.github/workflows/template-dispatch.yml` (T008, T014, T015) — same file, sequence them.

---

## Parallel Example: Setup Phase

```bash
# T002 and T003 touch different tooling — run together:
Task: "Confirm actionlint is available and record version"
Task: "Confirm gh CLI + bash available and record versions"
```

## Parallel Example: Polish Phase

```bash
# Distinct artifacts — run together:
Task: "Update fs-gg-ui-template registry contract entry (FR-009)"
Task: "Confirm release.yml byte-unchanged vs origin/main (FR-008)"
Task: "Re-run quickstart Layer 1+2 and record consolidated result"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (baseline anchor + tooling).
2. Phase 2: Foundational (pin contract, build dry-run harness) — the **early proof** substitute for the live smoke run.
3. Phase 3: User Story 1 — script + workflow emit the correct payload (dry-run).
4. **STOP and VALIDATE**: T009 happy-path dry-run is green; payload shape matches the contract.
5. The sender is functionally complete for the credentialed path; demo via dry-run.

### Incremental Delivery

1. Setup + Foundational → harness ready.
2. US1 → correct payload derivation/emit (MVP, dry-run proven).
3. US2 → fail-loud/observability proven across all edge cases.
4. US3 → fork/non-template guards proven via actionlint.
5. Polish → registry coherence, FR-008 regression proof, deferred-live-check disclosure.

### Deferred (disclosed) — NOT in scope to make green here

- Live end-to-end cross-repo dispatch (SC-001/SC-002/SC-005) is BLOCKED by the org credential
  (FS-GG/.github#21/#22). Tracked in T021; run per quickstart Layer 3 once provisioned.

---

## Notes

- `[P]` = different files, no dependencies. The two product files are shared across all three stories, so most story tasks are intentionally sequential.
- `[Story]` label maps each task to US1/US2/US3 for traceability.
- This feature adds no F# code; "test evidence" = `actionlint` + `DRY_RUN=1` script proofs (real, runnable) plus one disclosed deferred live check.
- Commit after each task or logical group.
