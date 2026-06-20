---
description: "Task list for Automated Control Pass for the Second AntShowcase"
---

# Tasks: Automated Control Pass for the Second AntShowcase

**Input**: Design documents from `/specs/176-test-antshowcase-controls/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (all present)

**Tests**: INCLUDED. The spec, plan (Constitution Check "Spec → FSI → semantic tests →
implementation"), and the runner contract (`contracts/control-pass-runner.md` "Test obligations")
all require failing-first tests, so test tasks are first-class here.

**Organization**: Tasks are grouped by user story. US1 (functional) and US2 (visual) are both P1 and
are delivered by the **same** sample-local runner — they share the pure pass plan built in the
Foundational phase, then split into the functional dimension (US1) and the visual/interaction-state
dimension (US2) of each verdict record.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 / US4 (Setup, Foundational, Polish carry no story label)
- Every task names exact file paths.

## Path Conventions

- Sample-local (Tier 2): `samples/SecondAntShowcase/{SecondAntShowcase.Core,SecondAntShowcase.App,SecondAntShowcase.Tests}/`
- Shared-surface fixes (Tier 1, only where the pass proves a framework defect): `src/{Controls,Controls.Elmish,SkiaViewer,Testing}/` with mirrored `.fsi`, surface baseline, and failing-first tests under `tests/`.
- Evidence: `specs/176-test-antshowcase-controls/readiness/`; report: `docs/reports/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project scaffolding and the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T002 MUST run **every** test project via
> the discovery-based runner so pre-existing reds (stale surface baselines, stale sample pins,
> missing-report failures — exactly Feature 175's surprises) are known up front and not mistaken for
> regressions at merge. Do NOT hand-pick a subset.

- [X] T001 Register the new sample-local files in the project files so they compile before any code is written: add `ControlPass.fsi`/`ControlPass.fs` to `samples/SecondAntShowcase/SecondAntShowcase.Core/SecondAntShowcase.Core.fsproj` (the pure pass plan is **type-only / catalog-only** — it references `CoverageMap` + `InteractionContracts` but NOT `Model`/`Msg`, so place it after `InteractionContracts.fs` and before `Model.fs`; if it turns out to need `Model` types, move it after `Model.fs` instead — F# compiles top-down); add `ControlPassRunner.fs` to `samples/SecondAntShowcase/SecondAntShowcase.App/SecondAntShowcase.App.fsproj` (after `Interactive.fs`/`Responsiveness.fs`); add `ControlPassCoverageTests.fs` and `ControlPassRunnerTests.fs` to `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` (before `Main.fs`).
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/176-test-antshowcase-controls/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**/*.Tests` — and records the full red/green set; pre-existing reds are flagged here, not at merge).
- [X] T003 [P] Create the evidence directory skeleton: `specs/176-test-antshowcase-controls/readiness/{verdict-records,visual-evidence}/` and placeholder `finding-log.md` + `validation-summary.md` headers per `plan.md` Project Structure.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The pure pass plan, the contract seams, and the live verification gate that every user
story depends on.

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** T006 drives and observes the **real running
> app** before any US3 fix. Per `plan.md` (standing assumption) and `research.md` §D8, every
> root-cause hypothesis (H1–H5) is **unverified** — Feature 175 had 15 presses → 15 renders with
> green unit tests while the live app was broken. No US3 fix may be built on an unconfirmed
> hypothesis.

- [X] T004 [P] Confirm the completeness oracle (research §D1) in `samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassCoverageTests.fs` with a failing-first test: the emitted record-id set equals `CoverageMap.catalogIds ()` **exactly** — set equality, no missing/duplicate (C-1/G-2/VR-1). Template pages do NOT introduce new control ids: a template-reachable control is exercised in its template context but classifies against its existing catalog identity and is recorded as an extra `PageContext` on that catalog id (research §D1). If `CoverageMap`/`Templates.fs` lacks a named accessor to enumerate template-reachable controls, add one (e.g. `CoverageMap.templateReachable ()`) rather than hand-listing — and assert it maps onto `catalogIds ()`. Classification source = `InteractionContracts.fs` (≥1 behavior ⇒ Interactive, else DisplayOnly + reason — D2).
- [X] T005 Draft the pure pass-plan seam FIRST (Spec→FSI→tests→impl): write `samples/SecondAntShowcase/SecondAntShowcase.Core/ControlPass.fsi` declaring the verdict-record types (`ControlVerdictRecord`, `BehaviorOutcome`, `InteractionStateOutcome`, `DamageOutcome`, `VisualEvidenceItem`, `Finding`, and the verdict DUs) per `data-model.md` and `contracts/verdict-record.md`, plus the pure `catalog → behaviors → record skeleton` plan functions. Keep this module type-only/catalog-only (no `Model`/`Msg` dependency — see T001 ordering). Stub `ControlPass.fs` so it compiles (functions `failwith "not implemented"`).
- [X] T006 **Early live smoke run** (gates US3): run the not-yet-complete pass / existing live paths against the real app to confirm or replace research §D8 hypotheses BEFORE any fix. Run quickstart scenarios (1)+(2)+(6): `dotnet run --project samples/SecondAntShowcase/SecondAntShowcase.App -c Release -- control-pass --seed 1 --backend x11xtest --require-live --out specs/176-test-antshowcase-controls/readiness` on a visible desktop (or accept `environment-limited` with disclosed substitute on a headless host). Record observed live evidence + which of H1–H5 are confirmed/replaced in `specs/176-test-antshowcase-controls/readiness/finding-log.md`.
- [X] T007 [P] Set up the shared test + evidence scaffolding: in `samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassRunnerTests.fs` add the Expecto test list registration (wire into `Main.fs`) and a `ControlPassFixtures` helper for seeded runner invocation + artifact loading that US1/US2/US3 tests reuse. Any synthetic substitute (e.g. faked window diagnostic for env-limited assertions in CI) carries the `Synthetic` token in the test name and is disclosed at the use site (Principle V).

**Checkpoint**: Catalog oracle + behavior source confirmed, pass-plan `.fsi` seam drafted, root-cause
hypotheses verified against a live run — user-story implementation can begin.

---

## Phase 3: User Story 1 - Every control is exercised automatically and parity is proven (Priority: P1) 🎯 MVP

**Goal**: A single unattended pass drives every cataloged control through its full documented
behavior set and emits exactly one classified functional verdict record per control — no human input,
none left unexercised/unclassified, deterministic, env-limited where no window.

**Independent Test**: Run `control-pass --seed 1` and confirm one record per `CoverageMap.catalogIds ()`,
each terminal (Classified/EnvironmentLimited), interactive controls have every documented behavior
exercised with an asserted state change, display-only controls are `NotApplicable` with a reason.

### Tests for User Story 1 (write FIRST, ensure they FAIL) ⚠️

- [X] T008 [P] [US1] In `samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassCoverageTests.fs`, assert behavior coverage: for every Interactive control, `BehaviorsExercised` covers every documented behavior in its `InteractionContracts.fs` contract (C-4/G-3, FR-002, SC-002) — not one representative action. **Caveat (the oracle is the contract):** this test only proves "exercised == declared"; it does NOT prove the declared set is complete. Before relying on it as the SC-002 oracle, review each control's contract for completeness (T004 fold) and extend thin contracts (research §D2) so SC-002 is not greened by an under-declared contract.
- [X] T009 [P] [US1] In `samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassRunnerTests.fs`, assert classification completeness: every emitted record is terminal (no `Unexercised`/`Unclassified`); `DisplayOnly` ⇒ non-empty `ClassificationReason` and `FunctionalVerdict = NotApplicable`; `Interactive` ⇒ `FunctionalVerdict ∈ {Pass;Fail;NeedsReview;EnvironmentLimited}` (C-2/C-3, VR-2/VR-5).
- [X] T010 [P] [US1] In `ControlPassRunnerTests.fs`, assert determinism (G-4, SC-005): two same-seed/same-build runs yield identical functional verdicts and byte-stable verdict records, wall-clock fields excluded (`GeneratedAtUtc` convention) — extend/mirror existing `DeterminismTests`.
- [X] T011 [P] [US1] In `ControlPassRunnerTests.fs`, assert fail-closed environment-limited degradation (G-6, FR-008): when no live window is presentable, live-only checks emit explicit `EnvironmentLimited` records with a non-zero signal (never silent pass/fail); structural Pure-backend evidence still runs. Reuse `SkiaViewer` window diagnostics + `ValidationLanes` `EnvironmentLimited` (synthetic window diagnostic disclosed, `Synthetic` in name).

### Implementation for User Story 1

- [X] T012 [US1] Implement the verdict-record types and pure plan in `samples/SecondAntShowcase/SecondAntShowcase.Core/ControlPass.fs` (satisfying `ControlPass.fsi` from T005): catalog → per-control behavior list (from `InteractionContracts.fs`) → record skeleton with classification (Interactive/DisplayOnly + reason). Verdict aggregation is a pure fold over records (Elmish/MVU boundary — no IO here). Keep type-only/catalog-only per the T001 fsproj ordering.
- [X] T013 [US1] Implement the input-driven functional exercise in `samples/SecondAntShowcase/SecondAntShowcase.App/ControlPassRunner.fs`: wire `Rendering.Harness.Input` (`InputScript`/`InputStep`, Pure backend default) to drive the showcase MVU (`SecondAntShowcase.Core` `Model`/`Msg`/`update`) through every documented behavior per control; assert each resulting `Model` state change → `BehaviorOutcome` (research §D3). Include the **empty / no-overflow / disabled** edge case (spec Edge Cases): scroll regions with no overflow, empty lists/grids, and disabled controls render their "nothing to do" affordance without error and are recorded as such, never a functional `Fail`. IO stays at the App edge.
- [X] T014 [US1] Implement environment-limited detection + determinism seeding in `ControlPassRunner.fs`: thread `--seed`/pinned clock through all evidence paths; detect no-window via `SkiaViewer` window diagnostics + `ValidationLanes`, mapping live-only outcomes to `EnvironmentLimited` (research §D6, §D7; G-4, G-6).
- [X] T015 [US1] Add the `control-pass` subcommand dispatch + argument parsing in `samples/SecondAntShowcase/SecondAntShowcase.App/Program.fs` per `contracts/control-pass-runner.md` CLI surface (`--seed --themes --sizes --backend --require-live --page/--all --out --json`); document and implement the flag→domain mapping `--themes light,dark → appearances antLight,antDark` and `--sizes preferred,minimum → 1600×1000, 1280×800` (the CLI says "themes" but the domain concept is "appearances"); exit codes 0/1/2 per the contract (1 on completeness/classification failure, non-terminal finding, or `--require-live` unavailable; 2 on bad args).
- [X] T016 [US1] Implement the functional outputs writer in `ControlPassRunner.fs`: one Control Verdict Record per cataloged control under `--out`/`verdict-records/` (catalog order, not discovery order) + `validation-summary.md` (+ JSON when `--json`) with aggregate counts/caveats. Enforce G-2 completeness (record-id set == catalog) → non-zero exit on missing/duplicate.

**Checkpoint**: Unattended pass emits one terminal functional verdict per cataloged control,
deterministic and env-aware. T008–T011 pass. US1 is independently demonstrable (MVP).

---

## Phase 4: User Story 2 - Visual fidelity of every control is captured and reviewable (Priority: P1)

**Goal**: The same pass captures, per control, visual evidence across both appearances × both sizes at
rest, per-interaction-state evidence for interactive controls (each verified to differ from rest),
overlay appear/dismiss, continuous-input feedback, and damage-local repaint — each cell carrying a
fidelity verdict with reasons.

**Independent Test**: Run the pass and confirm each control has light/dark × preferred/minimum evidence
at rest, each interactive control has per-supported-state evidence differing from rest, each transition
is damage-local, continuous-input controls track input without catch-up lag, and every non-`Approved`
cell carries a reason.

### Tests for User Story 2 (write FIRST, ensure they FAIL) ⚠️

- [X] T017 [P] [US2] In `samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassRunnerTests.fs`, assert matrix completeness (M-1/M-2, FR-003/FR-004, SC-003) via the `VisualReadinessReport`: every control has Complete evidence for both appearances × both sizes at `Rest`; every interactive control has evidence for each supported interaction state.
- [X] T018 [P] [US2] In `ControlPassRunnerTests.fs`, assert state-differs-from-rest (M-2, US2 AC2), damage-locality (M-4, FR-005), and **continuous-input feedback** (spec Edge Cases; plan Performance Goals) via the retained diff + responsiveness/render-lag evidence for a representative control of each interactive family: each captured interaction state has a real node delta vs rest; each transition's `DamageOutcome` is `Localized` (or carries `IntentionalDamageException`), never `Broad`/`FullSurface` silently; slider/scroll **drag** shows continuous feedback (offset tracks input, no catch-up lag), not only start/end states.
- [X] T019 [P] [US2] In `ControlPassRunnerTests.fs`, assert overlay appear/dismiss (M-5, FR-015) for each transient family (tooltip, popover, drawer, dialog, tour, toast, popconfirm): driven via trigger, the transient surface appears and dismisses, with focus return where applicable.

### Implementation for User Story 2

- [X] T020 [US2] Implement the appearance × size matrix capture in `samples/SecondAntShowcase/SecondAntShowcase.App/ControlPassRunner.fs` using `FS.GG.UI.Testing.VisualCaptureMatrix.expand` + `VisualCompleteness.validate` + `VisualReadiness.evaluate` → `VisualEvidenceItem` per cell (light/dark × preferred/minimum at rest), applying the T015 `--themes → antLight/antDark` mapping so the captured `Appearance` field matches the domain values (research §D3; M-1).
- [X] T021 [US2] Implement interaction-state driving in `ControlPassRunner.fs`: drive each interactive control into every supported state (hover/focus/active/selected/disabled/error from the interaction contract) and verify each differs from rest via `ControlInspection.inspectRetained`; capture the **disabled** and **empty/no-overflow** states (spec Edge Cases) as correctly-rendered affordances, not as missing-state failures (research §D4; M-2, FR-004).
- [X] T022 [US2] Implement continuous-input feedback capture in `ControlPassRunner.fs` for continuous-input controls (slider, scroll thumb): drive a live drag via `Rendering.Harness.Input` and capture continuous-feedback evidence through the existing `Responsiveness.fs`/`RenderLagProbe.fs` paths (research §D3), asserting the offset tracks input with no catch-up lag within the Feature 174 live-responsiveness target — not only start/end states (spec Edge Cases; plan Performance Goals; satisfies T018's continuous-input assertion).
- [X] T023 [US2] Implement overlay/transient handling in `ControlPassRunner.fs`: drive each transient surface via its trigger, capture the open state, assert appearance + dismissal + focus return (research §D4; M-5, FR-015).
- [X] T024 [US2] Implement damage-locality evidence in `ControlPassRunner.fs`: capture each state transition as a `RetainedInspectionArtifact`, validate with `RetainedInspectionValidation.defaultRules`, store the `DamageRegionInspection` (dirty rects, %, region ids) as `DamageOutcome`; `Broad`/`FullSurface` without `IntentionalDamageException` ⇒ Finding (research §D5; M-4, FR-005). MUST NOT introduce full-tree frame preparation.
- [X] T025 [US2] Implement the visual outputs writer in `ControlPassRunner.fs`: per-cell captures + contact sheets under `--out`/`visual-evidence/`, each carrying a `FidelityVerdict` (`Approved|NeedsReview|Blocked`) with ≥1 reason from the taxonomy when non-approved (M-3, US2 AC4); `CaptureStatus ∈ {Degraded,Blocked}` ⇒ cell `EnvironmentLimited`, never silent `Approved` (M-6). Merge the `VisualVerdict` into each Control Verdict Record from T016.

**Checkpoint**: Each control has the full appearance/size/state visual evidence + continuous-feedback +
damage-local proof with reasoned fidelity verdicts. T017–T019 pass. US1 + US2 together = full
per-control record.

---

## Phase 5: User Story 3 - Problems found are fixed and re-verified (Priority: P2)

**Goal**: Every defect the pass surfaces moves to a terminal state — `FixedAndReVerified` (sample-local
in the showcase, shared in `src/` at Tier 1) with before/after evidence, or `Deferred` with rationale +
follow-up. No interactive control left non-functional, no regression vs the pre-fix baseline.

**Independent Test**: Take a recorded defect, apply its fix, re-run the relevant pass slice, and confirm
the finding log transitions it to `FixedAndReVerified` (before/after evidence) or `Deferred` (rationale
+ follow-up).

> **⚠️ No fix without a confirmed root cause** — T006's live smoke run gates this phase. Each fix
> below is built only on a hypothesis confirmed by running the real app.

### Tests for User Story 3 (write FIRST, ensure they FAIL) ⚠️

- [X] T026 [P] [US3] For each confirmed defect, add a failing-first semantic test at the layer it lives: sample-local in `samples/SecondAntShowcase/SecondAntShowcase.Tests/` (e.g. `InteractionTests.fs`/`VisualEvidenceTests.fs`); shared-surface (Tier 1) in the matching `tests/Controls.Tests/`, `tests/Elmish.Tests/`, `tests/SkiaViewer.Tests/`, or `tests/Testing.Tests/` driven through the packed/prelude FSI surface (VR-10).

### Implementation for User Story 3

- [X] T027 [US3] Implement the finding-log infrastructure: triage each defect surfaced by the pass into `specs/176-test-antshowcase-controls/readiness/finding-log.md` with the `Finding` schema (id, description, affected controls, `SampleLocal|FrameworkShared`, `Tier1|Tier2`, severity, lifecycle, before-evidence) per `data-model.md` Finding entity (FR-009).
- [X] T028 [P] [US3] Fix each **sample-local** defect in `samples/SecondAntShowcase/` (showcase Core/App), e.g. unbound `OnChanged`/template-context wiring gaps (research §D8 H4), without papering over shared defects (FR-011).
- [X] T029 [US3] Fix each **shared-surface (Tier 1)** defect where it lives in `src/{Controls,Controls.Elmish,SkiaViewer,Testing}/`, shipping the `.fsi` delta + matching surface baseline + the T026 failing-first test together in the same change (FR-011, VR-10; plan Tier 1 obligations). One semantic control set — no per-theme/per-sample control fork.
- [X] T030 [US3] For each Tier 1 fix, re-pack the touched `FS.GG.UI.*` package(s) to `~/.local/share/nuget-local/` and bump the sample pins in `samples/SecondAntShowcase/**` so the showcase consumes the fix before re-verification (quickstart Prerequisites; mirrors Feature 175 follow-up).
- [X] T031 [US3] Re-run the relevant pass slice and transition each finding in `finding-log.md` to `FixedAndReVerified` (both before- and after-evidence, VR-11) or `Deferred` (rationale + follow-up ref, VR-9); confirm zero findings remain `Found` (SC-006). The affected control is marked accordingly, never a silent pass.
- [X] T032 [US3] Re-run the full pass and confirm no regression vs the pre-fix baseline (T002): 0 interactive controls non-functional, 0 records regress (FR-012, VR-12, SC-007), and the **Feature 174 responsiveness budgets** are not regressed (button-activation follow-up frame median ≤ 150 ms / p95 ≤ 250 ms; page navigation median ≤ 250 ms / p95 ≤ 500 ms; continuous-input feedback within target — plan Performance Goals).

**Checkpoint**: Every finding terminal; no non-functional control; no regression (functional + visual +
responsiveness). US3 complete.

---

## Phase 6: User Story 4 - Comprehensive framework/library report is delivered (Priority: P2)

**Goal**: A single `docs/reports/` document consolidating every framework/library finding +
improvement opportunity, separated from sample-local fixes, each with severity, classification,
evidence, and recommendation, ordered by priority.

**Independent Test**: Open the report and confirm it lists framework/library items with severity,
sample-vs-framework classification, evidence/reference, and recommendation, separated from sample-local
fixes, covering every category the pass exercised.

### Tests for User Story 4 (write FIRST, ensure they FAIL) ⚠️

- [X] T033 [P] [US4] Extend `samples/SecondAntShowcase/SecondAntShowcase.Tests/DocumentationReviewTests.fs` to assert the report exists under `docs/reports/`, contains the required Part headers (Part 1 Framework/library … Sample-local fixes separated, Prioritisation table, roadmap, appendix), and that every framework prioritisation-table row has a non-empty severity and recommendation (structural check, not prose — `contracts/framework-report.md` Test obligation).

### Implementation for User Story 4

- [X] T034 [US4] Author `docs/reports/2026-06-20-feature-176-second-antshowcase-control-pass-report.md` per `contracts/framework-report.md` structure: lead metadata → executive summary → background → Part 1 Framework/library (each finding: Evidence/Root cause/Impact/Mitigation/Recommendation + Effort/Risk) → Parts 2–4 (tooling/process/skills if surfaced) → separated Sample-local Part. Author it **from** the finding log + verdict records (R-6 evidence-anchored, R-7 classification fidelity). Cover every category the pass exercised — functional, visual, interaction-state, continuous-input, damage-locality, overlays, determinism, environment-limits, testing-helper gaps (R-5, SC-008).
- [X] T035 [US4] Add the prioritisation table (ID, Item, Severity, Effort, Leverage), phased roadmap (Phase A/B/C), and evidence appendix anchored to readiness artifacts + `file:line` code pointers; link back to `specs/176-test-antshowcase-controls/`; ensure framework items are separated from sample-local and ordered by priority/impact (R-1…R-5, FR-014, SC-008).

**Checkpoint**: Report delivered, structurally validated, evidence-anchored. US4 complete.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and capture.

- [X] T036 Run the full quickstart validation (`specs/176-test-antshowcase-controls/quickstart.md` scenarios 1–9), including the determinism re-run (5) and the headless environment-limited check (6); record results in `specs/176-test-antshowcase-controls/readiness/validation-summary.md`.
- [X] T037 [P] Re-run the comprehensive baseline `dotnet fsi scripts/baseline-tests.fsx --out specs/176-test-antshowcase-controls/readiness/baseline-final.md` and diff against T002 to confirm no test project regressed (solution + `Package.Tests` + samples), and confirm the Feature 174 responsiveness-budget tests stay green (plan Performance Goals; see T032).
- [X] T038 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/176-test-antshowcase-controls/feedback/` (process friction, generalizable-code candidates, severity).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. T006 (early live smoke run) gates US3.
- **US1 (Phase 3)** and **US2 (Phase 4)**: Both P1, both depend on Foundational. They share the runner: US1 builds the functional dimension + writer (T012–T016), US2 adds the visual dimension and merges into the same record (T020–T025). US2 implementation depends on the runner skeleton from US1 (T013/T016), so run US1 → US2 (or staff US2 tests T017–T019 in parallel with US1 implementation).
- **US3 (Phase 5)**: Depends on US1+US2 surfacing defects, and on the T006 live confirmation.
- **US4 (Phase 6)**: Depends on the US3 finding log + verdict records.
- **Polish (Phase 7)**: Depends on all desired user stories complete.

### Within Each User Story

- Tests (T008–T011, T017–T019, T026, T033) are written and MUST FAIL before implementation.
- Pure `ControlPass.fsi` (T005) before `ControlPass.fs` (T012) before the runner (T013+).
- Runner skeleton (T013, T016) before visual capture merges into the record (T025).
- Continuous-input capture (T022) satisfies the T018 continuous-feedback assertion.
- Tier 1 fix (T029) ships `.fsi` + baseline + test together; re-pack/re-pin (T030) before re-verify (T031).

### Parallel Opportunities

- T004 and T007 (Foundational) are [P] — different files.
- All US1 tests T008–T011 are [P] (T008 in CoverageTests, T009–T011 in RunnerTests — coordinate edits to RunnerTests or write as distinct test lists).
- All US2 tests T017–T019 are [P]; US4 test T033 is [P].
- T028 (sample-local fixes) is [P] vs T029 (shared fixes) — different trees.
- Polish T037/T038 are [P].

---

## Parallel Example: User Story 1 tests

```bash
# Author the failing-first US1 tests together (distinct concerns):
Task: "T008 behavior coverage in samples/SecondAntShowcase/SecondAntShowcase.Tests/ControlPassCoverageTests.fs"
Task: "T009 classification completeness in .../ControlPassRunnerTests.fs"
Task: "T010 determinism in .../ControlPassRunnerTests.fs"
Task: "T011 environment-limited degradation in .../ControlPassRunnerTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Phase 1 Setup → Phase 2 Foundational (CRITICAL — includes T006 **early live smoke run** validating
   research §D8 hypotheses against the real app before any fix).
2. Phase 3 US1 → unattended pass emits one terminal functional verdict per cataloged control.
3. **STOP and VALIDATE**: independent test of US1 (record count == catalog, behaviors exercised, deterministic, env-aware).

### Incremental Delivery

1. Setup + Foundational → foundation ready (oracle, seam, live-confirmed hypotheses).
2. US1 → functional pass (MVP).
3. US2 → visual fidelity + continuous feedback merged into each record.
4. US3 → fix + re-verify all surfaced defects (sample-local + Tier 1 shared).
5. US4 → consolidated framework/library report.
6. Polish → quickstart + baseline + feedback.

---

## Notes

- [P] = different files, no incomplete-task dependency.
- US1/US2 are both P1 and intentionally share the runner; their *records* are one object with a
  functional dimension (US1) and a visual dimension (US2).
- The runner/CLI/tests are Tier 2 (no public `FS.GG.UI.*` delta); only US3 shared-surface fixes are
  Tier 1 and carry `.fsi` + baseline + test obligations.
- Verify tests fail before implementing; commit after each task or logical group.
- Live-only checks fail-closed to `environment-limited` — never a silent pass (FR-008).
- The behavior-coverage oracle (T008) is the interaction contract; thin contracts must be extended
  (T004/T008 fold) so SC-002 is not greened by an under-declared behavior set.
