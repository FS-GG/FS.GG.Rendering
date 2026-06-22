---
description: "Task list — Harness Data-Table Refactor (185)"
---

# Tasks: Harness Data-Table Refactor

**Input**: Design documents from `/specs/185-harness-data-table-refactor/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/harness-internal-contracts.md, quickstart.md

**Tests**: No *new* test suites are requested — this is a behavior-preserving refactor. The evidence
is (a) a pre-refactor artifact baseline + per-story **semantic diff** (FR-008) and (b) **retargeting**
existing harness tests off removed `renderFeature*`/directory vals onto the descriptor-driven surface
(FR-010). Retargeting tasks are included per story; no test is weakened or deleted to go green.

**Organization**: Tasks are grouped by user story (US1–US4) so each can be implemented, validated, and
shipped independently. Per spec/plan, the dependency edges are US1 → US2 → US3, with US4 independent.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 / US4 (Setup, Foundational, Polish carry no story label)
- Every task names exact file path(s)

## Path Conventions

All production code lives under `tools/Rendering.Harness/`; the primary test gate is
`tests/Rendering.Harness.Tests/`. Repo root: `/home/developer/projects/FS.GG.Rendering`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the harness builds at HEAD and establish the comprehensive no-regression baseline
before any production edit.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test
> project and record the full red/green set, so pre-existing reds (e.g. stale-feed `Package.Tests` /
> `ControlsGallery` carried from 182/183) are known up front and not mistaken for regressions at
> merge. Do NOT hand-pick a subset: `dotnet test FS.GG.Rendering.slnx` deliberately omits
> `tests/Package.Tests` (release-only public-surface gate) and the `samples/**/*.Tests` (feed
> consumers) — exactly where Feature 175's surprises hid. Use the discovery-based runner so nothing
> silently drops out.

- [X] T001 Confirm HEAD builds: `dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release` succeeds, and record the file/line counts the refactor must shrink (`Compositor.fs` 5,512, `Cli.fs` 3,928, `ValidationLanes.fs` 1,376) into `specs/185-harness-data-table-refactor/readiness/head-metrics.md`
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/185-harness-data-table-refactor/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge — SC-004 reference)
- [X] T003 [P] Record the SC-003 starting counts so the drop-to-zero is provable: `grep -cE 'ReadinessDirectory' tools/Rendering.Harness/Compositor.fs` and `grep -cE '^\s*let\s+renderFeature' tools/Rendering.Harness/Compositor.fs`, written into `specs/185-harness-data-table-refactor/readiness/head-metrics.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-refactor artifact baseline corpus and the byte-identical-literal
inventory that every user story diffs against, and draft the fail-loud SSOT seams. No user story may
begin until this phase is complete.

**⚠️ CRITICAL**: No US1–US4 work can start until the baseline corpus + literal inventory exist.

> **⚠️ Early live run (STANDING, adapted — do not omit).** This is a *refactor with no behavior
> change*, so there is no root-cause hypothesis to confirm. The plan (§Summary callout) makes the
> **equivalent** obligation explicit: **drive the real running harness** for every catalog feature (148,149,152–161; 150/151 absent) and
> snapshot every emitted readiness/evidence/parity/timing artifact into a baseline corpus *before
> touching code*. This live run is the ground truth every per-story semantic diff is measured against;
> the plan's equivalence claims are unverified until the harness has actually been run and its output
> captured. Pull this forward — do not defer artifact capture to the per-story checkpoints.

- [X] T004 **Early live run (baseline corpus)**: build the harness and drive it for every catalog feature (148,149,152–161 — 150/151 are absent), snapshotting every emitted artifact into a baseline corpus — `dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release` then run each feature's readiness and `cp -r` the emitted `specs/<slug>/readiness/…` trees into `/tmp/185-baseline/` (per quickstart Step 0). Record in `specs/185-harness-data-table-refactor/readiness/baseline-corpus.md` which artifacts were captured per feature
- [X] T005 Build the **byte-identical-literal inventory** (FR-008, edge "Directory-path drift"): enumerate the fixed CI-grepped path strings (the `readiness/…` directory literals behind the 110 `*ReadinessDirectory` constants) and the required report-header literals that downstream CI greps, into `specs/185-harness-data-table-refactor/readiness/frozen-literals.md` — these must stay byte-identical through every story
- [X] T006 Build the **classification / re-homing map**: catalog the 12 descriptor rows (148,149,152–161), each feature's current variant set + required headers (read from the inline literals in `tools/Rendering.Harness/Compositor.fs`), and the ~20% feature/variant bodies that genuinely diverge (candidate `FeatureRenderHooks`), into `specs/185-harness-data-table-refactor/readiness/rehoming-map.md` (drives US1 `RequiredHeaders` population and US2 hook discovery)
- [X] T007 [P] Write a reusable **semantic-diff harness** (parse status/counts/required-headers/meaningful-ordering, compare a re-emitted artifact tree to `/tmp/185-baseline/`) as `scripts/semantic-diff-artifacts.fsx`, plus the byte-identity check for the `frozen-literals.md` set — invoked at every per-story checkpoint (FR-008 verification)
- [X] T008 [P] Draft the SSOT **fail-loud validation seam** in `tools/Rendering.Harness/FeatureCatalog.fsi`/`.fs`: signatures for a duplicate-`CliAlias` check and an exhaustive descriptor-lookup-by-`Id` that throws on a missing row (FR-011, C-1) — `.fsi`-first, no implementation body yet
- [X] T009 [P] Draft the curated `.fsi` seams for the four target split modules (`Compositor.Types`, `Compositor.Config`, `Compositor.FeatureState`, `Compositor.Render`) as empty/stub `.fsi` files plus their compile-order slot in `tools/Rendering.Harness/Rendering.Harness.fsproj` (after `FeatureCatalog.fs`, before `PackageFeed.fs`/`Cli.fs`) — Constitution II, C-5; bodies land in US1/US2

**Checkpoint**: Baseline corpus captured from a live harness run, frozen-literal inventory recorded,
semantic-diff tooling ready, fail-loud + `.fsi` seams drafted — US1 implementation can begin.

---

## Phase 3: User Story 1 - Single source of truth for feature metadata (Priority: P1) 🎯 MVP

**Goal**: Every per-feature constant (slug, aliases, directories, required headers, accepted-profile)
is reachable from **one** `FeatureCatalog` descriptor row; the 110 `*ReadinessDirectory` constants and
duplicated header/profile literals in `Compositor.fs` become descriptor-derived lookups.

**Independent Test**: Build the harness and run the full Release `*.Tests.fsproj` sweep — readiness
artifacts land at the **same paths** with the **same headers**; `grep -cE 'ReadinessDirectory'
tools/Rendering.Harness/Compositor.fs` → 0; a duplicate alias / unknown id fails loud (FR-011).

- [X] T010 [US1] Populate `RequiredHeaders` for all 12 descriptor rows in `tools/Rendering.Harness/FeatureCatalog.fs` from the header literals recorded in `rehoming-map.md` (currently `[]` for every row); update `FeatureCatalog.fsi` to expose the field as populated data (FR-001, data-model FeatureDescriptor)
- [X] T011 [US1] Add descriptor-derived directory/path helpers to `FeatureCatalog` (`readinessDirectory`/`variantDirectory`/`*Path` keyed off `Slug`) in `tools/Rendering.Harness/FeatureCatalog.fsi`/`.fs`, reproducing the **exact** prior path byte-strings from `frozen-literals.md` (FR-002, C-1, edge "Directory-path drift")
- [X] T012 [US1] Implement the fail-loud SSOT checks seamed in T008 in `tools/Rendering.Harness/FeatureCatalog.fs`: duplicate-`CliAlias` detection and exhaustive `descriptorById` that throws a clear error on a missing row (FR-011, edge "Feature not in the catalog" / "CLI alias collision")
- [X] T013 [US1] Repoint the 110 `*ReadinessDirectory` constants in `tools/Rendering.Harness/Compositor.fs` at the descriptor lookups from T011, then **delete** the standalone `let` bindings; update `Compositor.fsi` to drop the removed directory vals (FR-002, SC-003)
- [X] T014 [US1] Replace the duplicated required-header / accepted-profile literals inline in `tools/Rendering.Harness/Compositor.fs` with `descriptor.RequiredHeaders` / `descriptor.Config.AcceptedProfileId` lookups (FR-002); leave per-feature one-off values inline only where not shared (data-model FeatureConfig rule)
- [X] T015 [US1] Retarget any `tests/Rendering.Harness.Tests/` (and secondary harness-surface) call-sites that referenced a now-removed `*ReadinessDirectory` val onto the descriptor lookup (FR-010) — coverage equivalent, not weakened
- [X] T016 [US1] **Checkpoint validation**: `dotnet build tools/Rendering.Harness/Rendering.Harness.fsproj -c Release`; `dotnet test tests/Rendering.Harness.Tests -c Release`; run `scripts/semantic-diff-artifacts.fsx` against `/tmp/185-baseline` (artifacts semantically equivalent, frozen literals byte-identical); assert `grep -cE 'ReadinessDirectory' tools/Rendering.Harness/Compositor.fs` → 0 (SC-003); demonstrate the duplicate-alias check fires (FR-011)

**Checkpoint**: SSOT proven — directories/headers/profile sourced from descriptors, 110 directory
constants gone, fail-loud in place. US1 is independently shippable.

---

## Phase 4: User Story 2 - Parametric renderer replaces the 85-function grid (Priority: P2)

**Goal**: Replace the 85 `renderFeature<N><Variant>` functions (all 85 deleted across T022+T024) + 6 per-feature state machines with one
parametric renderer + state driver over the descriptor's `Variants`, with explicit `FeatureRenderHooks`
for the divergent ~20%; convert the two feature-number-`match` renderers to `Id` lookups; split
`Compositor.fs` into the four `Compositor.*` modules (Pattern E).

**Independent Test**: Re-emit artifacts and semantic-diff against `/tmp/185-baseline`; a descriptor
with `{ValidationSummary; Timing; Parity}` emits exactly those three reports, semantically identical;
`grep -cE '^\s*let\s+renderFeature' tools/Rendering.Harness/Compositor*.fs` → 0; no `Compositor*.fs`
file > ~1,500 lines.

**Depends on**: US1 (descriptor is now authoritative for directories/headers/variants).

- [X] T017 [US2] Extract the ~60 type/DU defs (`ArtifactPublished`, `HostProfile`, `TierEvaluated`, `ParityPassed`, `PresentProof`, `Ready`, `Rejected`, `Limited`, …) from `tools/Rendering.Harness/Compositor.fs` into a new `tools/Rendering.Harness/Compositor.Types.fs` + curated `.fsi`; keep them reachable for retargeted tests (data-model re-homing, FR-010, C-5)
- [X] T018 [US2] Move the descriptor-derived directory/header/profile lookups (from US1) into `tools/Rendering.Harness/Compositor.Config.fs` + `.fsi` (absorbs the former 110-constant surface), consuming `FeatureCatalog` (data-model re-homing)
- [X] T019 [US2] Add the `FeatureRenderHooks` record (option-typed override fields, one per divergent variant body discovered in `rehoming-map.md`) and a `Renderers` field on `FeatureDescriptor` in `tools/Rendering.Harness/FeatureCatalog.fsi`/`.fs`; default rows to all-`None` (data-model FeatureRenderHooks, FR-003)
- [X] T020 [US2] Implement the parametric state driver `init`/`update`/`status` over a descriptor in `tools/Rendering.Harness/Compositor.FeatureState.fs` + `.fsi`, replacing the 6 per-feature state machines; keep transitions pure (Constitution IV)
- [X] T021 [US2] Implement the generic `renderFeature : FeatureDescriptor -> ReportVariant -> <report>` in `tools/Rendering.Harness/Compositor.Render.fs` + `.fsi`, dispatching over `d.Variants`; a `Variant` with no template **and** no hook must fail loud (FR-003, C-2, edge "Variant declared but no template path"). Use tag-indexed dispatch over `ReportVariant` — no SRTP/inline/reflection (Constitution III)
- [X] T022 [US2] Move each genuinely divergent feature/variant body (the ~20% from `rehoming-map.md`, e.g. 159 promotion / 160 throughput / 161 lane-ledger) into a `Some f` `FeatureRenderHooks` override on its descriptor, deleting the corresponding top-level `renderFeatureNNN…` function (FR-003, US2-AS2)
- [X] T023 [US2] Convert `renderPackageValidation` and `renderRegressionValidation` from `match featureNum` dispatch to `descriptorById`-keyed lookups in `tools/Rendering.Harness/Compositor.Render.fs` (FR-004, US2-AS3)
- [X] T024 [US2] Delete the remaining non-divergent `renderFeature*` top-level functions from `tools/Rendering.Harness/Compositor.fs` (the ~80% not already moved to `FeatureRenderHooks` in T022; T022 + T024 together remove all 85); rewrite `Compositor.fsi` to remove those vals and expose only the parametric/descriptor surface, re-exporting `Compositor.Types` where tests need it (SC-003, C-5)
- [X] T025 [P] [US2] Retarget `tests/Rendering.Harness.Tests/` call-sites that invoked a removed `renderFeature<N><Variant>` or `feature###Id` val onto `Compositor.Render.renderFeature descriptor variant` / `descriptorById` (FR-010); apply the same retargeting to secondary consumers (`tests/Elmish.Tests`, `tests/Package.Tests`, `tests/Controls.Tests`, `tests/SkiaViewer.Tests`, `tests/Scene.Tests`, `tests/Layout.Tests`) where they reference removed vals
- [X] T026 [US2] **Checkpoint validation**: build; re-emit artifacts and run `scripts/semantic-diff-artifacts.fsx` vs `/tmp/185-baseline` (status/counts/headers/ordering equivalent — FR-008); assert `grep -cE '^\s*let\s+renderFeature' tools/Rendering.Harness/Compositor*.fs` → 0 (SC-003) and no `Compositor*.fs` > ~1,500 lines (SC-001); `dotnet test tests/Rendering.Harness.Tests -c Release`

**Checkpoint**: Largest line-count win landed — one renderer + state driver + hooks, `Compositor.fs`
split into four modules, zero `renderFeature*` functions. US1 + US2 work independently.

---

## Phase 5: User Story 3 - One readiness workflow replaces per-feature CLI commands (Priority: P3)

**Goal**: Collapse the per-feature `Cli.fs` command handlers into one descriptor-driven `runReadiness`
workflow (probe → mkdirs → build reports → write N variant files → render) + an alias→descriptor
command table, preserving the observable CLI contract (artifacts, paths, exit codes, run-id shape).

**Independent Test**: Invoke each alias form (`156`, `feature156`, `156-same-profile-timing`) — same
artifacts + exit code; an unknown alias (`feature999`) errors the same way (non-zero, same message
shape); no per-feature handler function remains.

**Depends on**: US1 (alias/variant table) + US2 (the parametric renderer it calls).

- [X] T027 [US3] Implement `runReadiness : FeatureDescriptor -> exitCode` in `tools/Rendering.Harness/Cli.fs`: probe → make the descriptor's directories → build reports → write the `d.Variants` files → call `Compositor.Render.renderFeature` (data-model runReadiness, C-3, FR-005)
- [X] T028 [US3] Build the alias→descriptor command table keyed on `CliAliases` in `tools/Rendering.Harness/Cli.fs`, dispatching every feature through `runReadiness`; route id / `feature<N>` / slug forms to the same descriptor (US3-AS1)
- [X] T029 [US3] Delete the per-feature `runFeature*Cmd` handlers from `tools/Rendering.Harness/Cli.fs`; preserve argument parsing, exit codes, and run-id formatting (run-id may differ only in an already-embedded timestamp — FR-007)
- [X] T030 [US3] Preserve unknown-alias error behavior: route an unresolved alias through the fail-loud descriptor lookup (T012) so it reports the same error + non-zero exit code as before — no silent success, no last-wins (FR-007, FR-011, US3-AS2, edge "CLI alias collision")
- [X] T031 [P] [US3] Retarget any `tests/Rendering.Harness.Tests/` test that exercised a removed per-feature CLI handler onto the `runReadiness`/command-table path (FR-010)
- [X] T032 [US3] **Checkpoint validation** (quickstart Step 3): for `156` / `feature156` / `156-same-profile-timing` confirm identical artifacts + exit code; `dotnet run --project tools/Rendering.Harness -c Release -- feature999; echo "exit=$?"` errors as before; assert no `runFeature*Cmd` handlers remain (SC-002, SC-006); semantic-diff re-emitted artifacts vs baseline

**Checkpoint**: CLI contract unchanged, one `runReadiness` workflow + command table. US1+US2+US3 ship.

---

## Phase 6: User Story 4 - runLane decomposed into single-responsibility stages (Priority: P4)

**Goal**: Split `ValidationLanes.runLane` (~154 lines from line 1063) into a `ProcessRunner`,
`TimeoutManager`, `OutputBuffer`, and a thin orchestrator, reusing the file's existing MVU edge, with
`LaneResult` and `runLanes`/caller contract unchanged.

**Independent Test**: Run the lane sweep — a normal lane's `LaneResult` (status, output, timing)
matches baseline; a timed-out lane is reported as a timeout with the logic isolated in
`TimeoutManager`, and `TimedOut` vs `NoProgressTimedOut` is preserved.

**Depends on**: Nothing in US1–US3 (independent; can land any time after Foundational).

- [X] T033 [P] [US4] Extract process spawn + stdout/stderr/exit capture into a `ProcessRunner` unit in `tools/Rendering.Harness/ValidationLanes.fs` (+ `.fsi`), keeping spawn at the MVU interpreter edge (Constitution IV, data-model Lane-execution units)
- [X] T034 [P] [US4] Extract wall-clock + no-progress timeout → terminate into a `TimeoutManager` unit in `tools/Rendering.Harness/ValidationLanes.fs` (+ `.fsi`), preserving `TimedOut` vs `NoProgressTimedOut` (C-4, US4-AS2)
- [X] T035 [P] [US4] Extract captured-output accumulation into an `OutputBuffer` unit in `tools/Rendering.Harness/ValidationLanes.fs` (+ `.fsi`)
- [X] T036 [US4] Rewrite `runLane` as a thin orchestrator composing `ProcessRunner` + `TimeoutManager` + `OutputBuffer` into the unchanged `LaneResult`, leaving `runLanes` and all callers untouched (FR-006, C-4) — depends on T033–T035
- [X] T037 [US4] **Checkpoint validation** (quickstart Step 4): `dotnet test tests/Rendering.Harness.Tests -c Release` — normal-lane `LaneResult` matches baseline (US4-AS1); timed-out lane reported as timeout (US4-AS2). **US4-specific gate** (the SC-001 1,500-line cap is already met at HEAD — `ValidationLanes.fs` is 1,376 lines — so it does *not* prove the decomposition): assert `runLane` is now a thin orchestrator (≤ ~40 lines) that only composes `ProcessRunner` + `TimeoutManager` + `OutputBuffer`, and that those three units exist as separately-callable functions/types with their own `.fsi` signatures (FR-006, C-4); keep `ValidationLanes.fs` ≤ 1,500 lines (SC-001)

**Checkpoint**: `runLane` decomposed; lane results unchanged. All four stories independently functional.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final whole-feature acceptance against the success criteria and evidence capture.

- [X] T038 **Final test sweep** (SC-004): `dotnet fsi scripts/baseline-tests.fsx --out specs/185-harness-data-table-refactor/readiness/after.md`; diff the red/green set against `readiness/baseline.md` — same set, known pre-existing reds unchanged (no new reds, none masked)
- [X] T039 [P] **Final semantic-diff + byte-identity** (SC-005): run `scripts/semantic-diff-artifacts.fsx` over the full 12-feature (148,149,152–161) corpus vs `/tmp/185-baseline`, and the byte-identity check over `frozen-literals.md` — record results in `specs/185-harness-data-table-refactor/readiness/semantic-equivalence.md`
- [X] T040 [P] **SC-001/SC-003 metrics**: assert no `tools/Rendering.Harness/` file > ~1,500 lines, 0 `renderFeature*` top-level functions, 0 `*ReadinessDirectory` constants; append before/after counts to `readiness/head-metrics.md`
- [X] T041 **SC-002 single-site proof**: walk through adding a sample (hypothetical) feature as one `catalog` descriptor row in `tools/Rendering.Harness/FeatureCatalog.fs`, confirm it compiles and gets a CLI command with **no** new handler function, then revert the sample row; record the walkthrough in `readiness/sc-002-single-site.md`
- [X] T042 [P] Update the feedback capture for the feature under `specs/185-harness-data-table-refactor/feedback/` (process friction, generalizable-code candidates) per the `fs-gg-feedback-capture` workflow

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories — the baseline corpus +
  frozen-literal inventory + semantic-diff tooling must exist before any production edit.
- **US1 (Phase 3, P1)**: Depends on Foundational. The MVP — SSOT proven.
- **US2 (Phase 4, P2)**: Depends on US1 (descriptor authoritative).
- **US3 (Phase 5, P3)**: Depends on US1 + US2 (calls the parametric renderer via the alias table).
- **US4 (Phase 6, P4)**: Depends only on Foundational — independent of US1–US3, can land any time.
- **Polish (Phase 7)**: Depends on all shipped stories.

### Within Each User Story

- Descriptor/data changes before the constants that read them; module extraction before deletion of
  the originals; production change before its test retargeting; story complete before its checkpoint.
- Each story ends build-green + semantic-diff clean before the next priority begins.

### Parallel Opportunities

- **Phase 1**: T003 ∥ T001/T002 setup work.
- **Phase 2**: T007 (semantic-diff harness), T008 (fail-loud seam), T009 (`.fsi` seams) run in
  parallel once the baseline corpus (T004) + maps (T005/T006) exist.
- **US2**: T025 (test retargeting) ∥ once removed vals are known.
- **US4**: T033, T034, T035 (ProcessRunner / TimeoutManager / OutputBuffer) are independent extractions
  — fully parallel; T036 orchestrator joins them.
- **Cross-story**: US4 (Phase 6) can be worked in parallel with US1–US3 by a second developer.
- **Polish**: T039, T040, T042 run in parallel.

---

## Parallel Example: User Story 4

```bash
# Three independent extractions from ValidationLanes.fs can run together:
Task: "Extract ProcessRunner unit in tools/Rendering.Harness/ValidationLanes.fs (+ .fsi)"
Task: "Extract TimeoutManager unit in tools/Rendering.Harness/ValidationLanes.fs (+ .fsi)"
Task: "Extract OutputBuffer unit in tools/Rendering.Harness/ValidationLanes.fs (+ .fsi)"
# Then the orchestrator composes them:
Task: "Rewrite runLane as thin orchestrator over the three units"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → confirm HEAD green + record metrics.
2. Phase 2 Foundational (CRITICAL) — **capture the live baseline corpus + frozen-literal inventory +
   semantic-diff tooling** before any edit. The refactor's equivalence claims are unverified until the
   harness has been run and its output captured.
3. Phase 3 US1 → SSOT proven.
4. **STOP and VALIDATE**: T016 — build green, artifacts semantically equivalent, 110 directory
   constants gone, fail-loud in place.
5. Ship US1 independently.

### Incremental Delivery

1. Setup + Foundational → baseline + tooling ready.
2. US1 → validate (T016) → ship (MVP — SSOT).
3. US2 → validate (T026) → ship (largest line-count win + module split).
4. US3 → validate (T032) → ship (CLI collapsed).
5. US4 → validate (T037) → ship (lane decomposition) — may land in parallel any time after Foundational.
6. Polish (T038–T042) → final SC-001…SC-006 acceptance.

### Parallel Team Strategy

- Developer A: US1 → US2 → US3 (the descriptor chain, must be sequential).
- Developer B: US4 (independent) in parallel, after Foundational.

---

## Notes

- **[P]** = different files, no dependency on an incomplete task.
- **[Story]** label maps each task to US1–US4 for traceability; Setup/Foundational/Polish carry none.
- This is a **behavior-preserving** refactor: every story's checkpoint re-emits artifacts and
  semantic-diffs against `/tmp/185-baseline` (FR-008); CI-grepped path/header literals stay
  byte-identical (`frozen-literals.md`).
- Test retargeting (FR-010) keeps **equivalent coverage** on the descriptor path — no assertion is
  weakened or deleted to go green.
- All new public modules carry a curated `.fsi` (Constitution II); no access modifiers in `.fs`.
- No new project / dependency / inter-project reference (FR-009); work is confined to
  `tools/Rendering.Harness/`.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
