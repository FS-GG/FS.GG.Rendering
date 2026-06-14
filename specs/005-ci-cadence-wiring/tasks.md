---
description: "Task list for: Wire Validation into CI at Chosen Cadences (Stage R6)"
---

# Tasks: Wire Validation into CI at Chosen Cadences (Migration Stage R6)

**Input**: Design documents from `/specs/005-ci-cadence-wiring/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (cadence-matrix, gate-contract, run-summary.schema), quickstart.md

**Tests**: No automated test phase is generated — this is CI/infrastructure (declarative YAML + Markdown), validated by real CI runs and a documented audit (quickstart V1–V7), not unit tests. The ONLY conditional code-test task is for a `scripts/ci/summarize-evidence.*` helper, and only if Decision 6 forces adding one (T024). Per plan.md, that helper's pure logic would get a minimal test in `Rendering.Harness.Tests`.

**Organization**: Tasks are grouped by user story. US1 (the deterministic gate) is the MVP; US2 makes the gate's claims truthful; US3 places every check at exactly its cadence.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (setup, foundational, and polish carry no story label)
- Exact file paths are included in every task

## Conventions / verified facts (from repo inspection)

- Solution: `FS.GG.Rendering.slnx` (repo root).
- Harness CLI: `tests/Rendering.Harness` — subcommands `probe`, `offscreen`, `perf`, `live-x11`, `input`. Exit contract: `0` = ran-and-passed **or** cleanly-skipped (`run.json.status:"skipped"`), `1` = assertion failed, `2` = bad usage (misconfig).
- Deterministic local-tier projects (capability `none`): `tests/{Color,Scene,Layout,Input,KeyboardInput,Elmish,Controls,Testing,Lib}.Tests`.
- GL-needing local projects (degrade-and-disclose): `tests/{SkiaViewer,Smoke}.Tests`.
- Surface drift: `tests/surface-baselines/` + `scripts/refresh-surface-baselines.fsx`.
- Release-only: `tests/Package.Tests` (present); template `Product.Tests` is present as **template source** at `template/base/tests/Product.Tests` — the release check **instantiates the template** and runs the generated product's tests (do not invent a top-level `tests/Product.Tests`, which does not exist).
- Cadence source of truth: `docs/validation/validation-set.md`; audited via new `docs/ci/cadence-map.md`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the in-repo locations the wiring lives in.

- [X] T001 Create the CI workflow directory `.github/workflows/` and the docs directory `docs/ci/` at the repository root (empty, ready for the workflow files and the cadence map).
- [X] T002 [P] Add `docs/ci/cadence-map.md` skeleton header that cites `docs/validation/validation-set.md` and `specs/005-ci-cadence-wiring/contracts/cadence-matrix.md` as its sources (content filled in T018). This anchors FR-012 traceability early.

**Checkpoint**: Target directories exist; nothing executes yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The single shared evidence/disclosure mechanism every workflow reuses. Building it once here keeps the harness exit-code mapping and the proof-scope summary identical across `gate`/`release`/`capability` (Decision 2, FR-005/FR-006/FR-010) and prevents a second drifting source of truth.

**⚠️ CRITICAL**: US1, US2, and US3 all consume this composite action. Complete before starting any story.

- [X] T003 Create a reusable composite action `.github/actions/harness-evidence/action.yml` that: (a) runs a given harness subcommand from `tests/Rendering.Harness`, (b) decides pass/skip/fail **from each emitted `run.json.status`, NOT from the process exit code alone** — multi-tier subcommands emit one `run.json` per tier under `<out>/<tier>/`, and the aggregate process exit code conflates them (notably `offscreen` returns exit `1` whenever T1 is cleanly *skipped*: see `Cli.fs` `runOffscreen`, which requires both T0 and T1 `Passed` for exit `0`). Map **per tier**: `status:"passed"`→pass, `status:"skipped"`→disclosed skip (never fail), `status:"failed"`→fail; reserve process exit `2` for fail-as-misconfig. (c) uploads `run.json`/`metrics.csv`/`summary.md` via `actions/upload-artifact`. Inputs: subcommand + args + a per-tier `required` map (which tier's non-pass/non-skip fails the job).
- [X] T004 In `.github/actions/harness-evidence/action.yml`, add the proof-scope job-summary renderer per `contracts/run-summary.schema.md`: read harness `run.json` (`status`, `proofLevel`, `authoritativeFor`, `notAuthoritativeFor`, `rationale`) and append `proved` / `notProvedHere` / `failed` / `overall` sections to `$GITHUB_STEP_SUMMARY`. A `skipped` status MUST render under `notProvedHere` with its rationale, never under `proved` (FR-005, FR-006, SC-004, SC-005). Prefer folding the harness `summary.md` (Decision 6) — add no new script here.
- [X] T005 [P] Document the SDK-pinning decision inside `docs/ci/cadence-map.md`: whether jobs rely on the preinstalled `net10.0` SDK or add `actions/setup-dotnet` keyed off the repo's `global.json` (if present). Used identically by every workflow's build/test steps.

**Checkpoint**: A single, audited evidence+disclosure action exists. User-story workflows can now reuse it.

---

## Phase 3: User Story 1 - Fast, trustworthy pre-merge gate on every change (Priority: P1) 🎯 MVP

**Goal**: A push/PR to the default branch automatically builds the solution and runs the fast deterministic validation, reporting one clear pass/fail that blocks merge on failure — no manual step, no release-only or capability work.

**Independent Test**: Open a clean PR → gate green; open a PR breaking a deterministic local-tier test (or `.fsi` surface drift) → gate red and merge blocked; confirm no release-only/package check runs (quickstart V1, V2).

### Implementation for User Story 1

- [X] T006 [US1] Create `.github/workflows/gate.yml` with triggers `push` and `pull_request` targeting the default branch, running on `ubuntu-latest`, using **no privileged secrets** so fork PRs run (FR-001, FR-013). Add a `concurrency` group keyed to the ref with `cancel-in-progress: true` so superseded pushes don't leave misleading stale results (Edge Case: concurrent pushes).
- [X] T007 [US1] In `.github/workflows/gate.yml`, add the checkout + build step `dotnet build FS.GG.Rendering.slnx` on `net10.0`; a build failure MUST fail the gate (FR-002). Honor the SDK-pinning decision from T005.
- [X] T008 [US1] In `.github/workflows/gate.yml`, add the default local deterministic tier step: `dotnet test` over `tests/{Color,Scene,Layout,Input,KeyboardInput,Elmish,Controls,Testing,Lib}.Tests` (capability `none`). Any failure MUST fail the gate (FR-002, FR-003).
- [X] T009 [US1] In `.github/workflows/gate.yml`, add the surface-baseline drift step that verifies `tests/surface-baselines/` against current `.fsi` surface (run `scripts/refresh-surface-baselines.fsx` in check mode / diff against committed baselines); any drift MUST fail the gate (FR-003). Resolve the spec's "first run with no baseline" edge case explicitly: a missing baseline fails the gate (never silently passes) — note the chosen behavior in `docs/ci/cadence-map.md`.
- [X] T010 [US1] In `.github/workflows/gate.yml`, add the docs build step running the `fsdocs` site build from current sources (build only, no publish) in **strict mode** so the repo's `FsDocsWarnOnMissingDocs=true` (`Directory.Build.props`) is treated as failure, not a warning; a broken docs build *or* a missing-doc stub MUST fail the gate (FR-003).
- [X] T011 [US1] In `.github/workflows/gate.yml`, add the harness offscreen step invoking the `.github/actions/harness-evidence` action with subcommand `offscreen` — **one invocation emits both `T0/run.json` and `T1/run.json`** (Cli.fs `runOffscreen`). Evaluate the **T0** evidence as `required: true`: `T0/run.json.status:"failed"` MUST fail the gate (FR-004). Do **not** gate on the `offscreen` *process exit code* — it is `1` whenever T1 is skipped, which must not red the gate. Artifacts and the proof-scope summary come from the action.
- [X] T012 [US1] In `.github/workflows/gate.yml`, ensure the job emits the proof-scope job summary (via the action) and uploads harness artifacts for the run, so a clean run shows the deterministic checks under `proved` (gate-contract Outputs).

**Checkpoint**: MVP — the deterministic gate runs automatically, blocks merge on a real break, and stays green on a clean change. (GL degrade-and-disclose + release/capability cadences are added in US2/US3.)

---

## Phase 4: User Story 2 - Capability-blocked checks degrade and disclose, never overclaim (Priority: P2)

**Goal**: On the headless gate runner, GL/display/input-dependent checks are skipped-with-written-rationale (never passed, never omitted), the run summary plainly separates "proved" from "not proved here," capability absence never reddens the gate, and a genuine misconfiguration fails fast with probe facts.

**Independent Test**: Run the gate on the headless hosted runner; confirm `SkiaViewer.Tests`, `Smoke.Tests`, and harness T1 appear under `notProvedHere` with rationale and `run.json.status:"skipped"` (exit `0`), the gate is still green, and a simulated misconfiguration fails fast instead of skipping (quickstart V3, V4, V5).

### Implementation for User Story 2

- [X] T013 [US2] In `.github/workflows/gate.yml`, add a probe step that runs harness `probe` (via the `.github/actions/harness-evidence` action) to classify the runner (display? GL? `/dev/uinput`?) and expose its result for the GL-dependent steps to key off — never guess capability (FR-005, FR-010).
- [X] T014 [US2] In `.github/workflows/gate.yml`, add the GL-dependent local checks `tests/SkiaViewer.Tests` and `tests/Smoke.Tests` so that when the probe reports no hardware GL they are **skipped with a written, machine-readable rationale** ("no hardware GL on hosted runner") and do **not** affect pass/fail; if GL *should* be present but the toolchain is misconfigured, fail fast with probe facts (FR-005, FR-010, FR-011).
- [X] T015 [US2] In `.github/workflows/gate.yml`, evaluate the harness **T1** evidence (`T1/run.json`) produced by the **same `offscreen` invocation as T011** — do **not** re-invoke `offscreen`. Treat T1 as `required: false`: absent GL yields `status:"skipped"` + rationale and never fails the gate; a `failed` T1 is advisory, not gating (FR-005, FR-011). This is precisely why T003/T011 must read per-tier `run.json` rather than the aggregate `offscreen` exit code.
- [X] T016 [US2] Verify/extend the `.github/actions/harness-evidence` summary renderer (T004) against `contracts/run-summary.schema.md` for the headless gate case: `notProvedHere` lists every skipped GL check with its rationale, a reader can answer "was live/visual behavior verified here?" from the summary alone (explicit *no*), and a misconfiguration appears under `failed` with probe facts rather than as a silent skip (FR-006, FR-010, SC-004, SC-005).
- [X] T017 [US2] Confirm the gate's overall pass/fail in `.github/workflows/gate.yml` is unaffected by any capability-absent skip (skips excluded from the red/green decision), matching the gate-contract "What can NEVER fail the gate" list (FR-005, FR-011).

**Checkpoint**: The gate's green now truthfully means "everything reachable here passed," with every unreachable check disclosed — not hidden, not faked.

---

## Phase 5: User Story 3 - Each check runs at exactly its declared cadence (Priority: P2)

**Goal**: Release-only checks run only on a release trigger (never on push/PR), capability tiers (live X11, perf, input) run only on schedule/manual and never gate merge, and every validation-set member maps to exactly one cadence — auditable by inspection.

**Independent Test**: Audit `docs/ci/cadence-map.md` against `docs/validation/validation-set.md` per `contracts/cadence-matrix.md` (every member in exactly one cadence, no release-only in the gate); trigger a release build and a manual capability run and confirm placement (quickstart V6).

### Implementation for User Story 3

- [X] T018 [US3] Fill in `docs/ci/cadence-map.md` (skeleton from T002): enumerate every validation-set member with its R3 frequency label, the CI cadence/trigger it maps to, its capability requirement, and headless-runner behavior — mirroring `contracts/cadence-matrix.md`. State the audit invariants (exactly one cadence per member; no release-only in gate; only gate is required) so FR-009 is verifiable by inspection (FR-009, FR-012).
- [X] T019 [US3] Perform the cadence audit: cross-check `docs/ci/cadence-map.md` rows against `docs/validation/validation-set.md` and against the actual triggers in `gate.yml`/`release.yml`/`capability.yml`; record the pass result (no overlap, no release-only in gate) in `docs/ci/cadence-map.md` (FR-009, SC-003, SC-007).
- [X] T020 [P] [US3] Create `.github/workflows/release.yml` triggered on `release`/tag (+ `workflow_dispatch`), running `tests/Package.Tests` and the template `Product.Tests` (present at `template/base/tests/Product.Tests`) by **instantiating the template and running the generated product's tests** — gate this step on the template-instantiation tooling being wired, not on the project file's existence (the file already exists). Restrict to non-fork context (no fork secrets exposure). This workflow MUST NOT be a required check and MUST NOT run on push/PR (FR-008, FR-013, SC-007).
- [X] T021 [P] [US3] Create `.github/workflows/capability.yml` triggered on `schedule` (+ `workflow_dispatch`), invoking harness `live-x11` (T2), `perf` (T3), and `input --backend uinput` (T-uinput) via the `.github/actions/harness-evidence` action with `required: false`; target a capable runner label but degrade-and-disclose until one exists (provisioning out of scope). Restrict to non-fork context. It MUST NOT be a required check; its failure or absence MUST NOT block merge (FR-007, FR-011, FR-013).

**Checkpoint**: All three cadences exist and are honored; the no-overlap invariant is documented and audited; the fast push gate carries zero release/capability cost (SC-008).

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Maintainer-facing wiring, end-to-end validation, and the conditional helper.

- [X] T022 [P] Document the branch-protection action (one-time maintainer step) in `docs/ci/cadence-map.md`: require **only** the `gate` workflow's checks for merge; release/capability are never required (gate-contract, FR-007). The spec defines which checks are required; enabling protection is the maintainer's action.
- [X] T023 Run the quickstart validations end-to-end (`specs/005-ci-cadence-wiring/quickstart.md` V1–V7): clean PR green (V1), broken PR red/merge-blocked (V2), headless degrade-and-disclose (V3/V4), misconfig fail-fast vs absence (V5), cadence audit + release/capability placement (V6), fork PR real signal without secrets (V7). Record outcomes, **including the measured deterministic-gate wall-clock** in `docs/ci/cadence-map.md` so SC-002 (<10 min) is auditable, not merely observed once.
- [X] T024 [P] CONDITIONAL — only if T004/T016 prove the harness `summary.md` + inline job-summary cannot satisfy FR-006: add `scripts/ci/summarize-evidence.fsx` to fold harness `run.json` artifacts into one proof-scope summary, and add a minimal pure-logic test for it in `tests/Rendering.Harness.Tests` (plan.md Testing). If the inline renderer suffices, SKIP this task and note in `docs/ci/cadence-map.md` that no new glue was needed (Decision 6).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. The `harness-evidence` composite action (T003/T004) **BLOCKS** all stories (gate T0/T1, capability tiers, and every proof-scope summary reuse it).
- **User Story 1 (Phase 3)**: Depends on Foundational. Independently testable MVP.
- **User Story 2 (Phase 4)**: Depends on Foundational + US1's `gate.yml` existing (it adds steps to the same file). Builds on US1 but is independently verifiable (headless degrade behavior).
- **User Story 3 (Phase 5)**: Depends on Foundational. Mostly independent of US1/US2 (separate workflow files), though the cadence audit T019 cross-checks all three workflows once they exist.
- **Polish (Phase 6)**: Depends on the desired stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on other stories.
- **US2 (P2)**: Edits `gate.yml` from US1 → sequence US2 after US1 (same file). Independently testable.
- **US3 (P2)**: After Foundational. `release.yml`/`capability.yml`/`cadence-map.md` are independent files; only the audit (T019) needs the others present.

### Within Each Story

- US1: T006 (workflow skeleton) → T007–T012 add steps to the same `gate.yml` (sequential — one file).
- US2: T013 (probe) → T014/T015 (GL/T1 steps) → T016/T017 (summary + pass-rule). Sequential — same `gate.yml` + shared action.
- US3: T018 (map) → T019 (audit, needs workflows); T020 and T021 are independent files.

### Parallel Opportunities

- T002 ‖ (after T001).
- T005 ‖ T003/T004 within Foundational (different concerns; T005 is doc-only).
- T020 ‖ T021 (different workflow files, no shared edits).
- T022 ‖ T024 in Polish (different files).
- Tasks editing the **same** `gate.yml` (T006–T017) are NOT parallel.

---

## Parallel Example: User Story 3

```bash
# release.yml and capability.yml are separate files with no shared edits:
Task: "Create .github/workflows/release.yml (Package.Tests + conditional template Product.Tests) — T020"
Task: "Create .github/workflows/capability.yml (harness live-x11/perf/input, never required) — T021"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (T001–T002).
2. Phase 2: Foundational — the `harness-evidence` action (T003–T005). CRITICAL: blocks all stories.
3. Phase 3: US1 — `gate.yml` deterministic gate (T006–T012).
4. **STOP and VALIDATE**: quickstart V1/V2 — clean PR green, broken PR red + merge blocked.
5. The deterministic merge gate is the shippable MVP.

### Incremental Delivery

1. Setup + Foundational → shared evidence action ready.
2. US1 → deterministic gate (MVP) → validate V1/V2.
3. US2 → degrade-and-disclose truthfulness → validate V3/V4/V5 on a headless run.
4. US3 → release + capability cadences + audited cadence map → validate V6.
5. Polish → branch protection doc, full quickstart V1–V7, conditional summarize-evidence helper.

---

## Notes

- This stage **wires**; it does not re-decide the validation set (Stage R3) or modify the harness (Stage R5).
- [P] = different files, no incomplete-task dependency. Most `gate.yml` edits are sequential (same file).
- A green check must mean what it says: never mark a skipped/blocked check as passing (Principles V, VI).
- Default-branch name in triggers (`gate.yml`) and non-fork restriction (`release.yml`/`capability.yml`) should match the repo's actual default branch and remote — confirm at T006/T020/T021.
- Template `Product.Tests` exists as template source (`template/base/tests/Product.Tests`); T020 exercises it by **instantiating the template**, not by referencing a top-level `tests/Product.Tests` (which does not exist). Do not invent the latter (cadence-matrix traceability invariant 3).
