---
description: "Task list for Concrete Ant Design theme with widened component coverage (D2.1)"
---

# Tasks: Concrete Ant Design theme with widened component coverage (D2.1)

**Input**: Design documents from `/specs/132-ant-concrete-theme/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: INCLUDED — the feature spec explicitly requires a parity suite, per-control contract suites (five families), and a coverage-matrix honesty check (FR-008/FR-011/FR-013, SC-001/SC-003/SC-004).

**Organization**: Tasks are grouped by user story (US1–US5) and follow the internal phasing P-A…P-E from plan.md so the tree stays green at each step.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1–US5)
- Exact file paths are included in every task

## Path Conventions

Multi-project F# solution (`FS.GG.Rendering.slnx`). Source under `src/`, tests under `tests/Controls.Tests/`, docs under `docs/product/`, surface baselines under `tests/surface-baselines/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Scaffold the new `FS.GG.UI.Themes.AntDesign` package and wire it into the solution and surface-baseline tooling.

- [X] T001 Create `src/Themes.AntDesign/Themes.AntDesign.fsproj` mirroring `src/Themes.Default/Themes.Default.fsproj` (target `net10.0`, `PackageId`/`AssemblyName` = `FS.GG.UI.Themes.AntDesign`, single `ProjectReference` to `src/DesignSystem` only — NO reference to `Controls`)
- [X] T002 Register the new project in `FS.GG.Rendering.slnx`
- [X] T003 [P] Add the row `("FS.GG.UI.Themes.AntDesign", "Themes.AntDesign")` to `scripts/refresh-surface-baselines.fsx`
- [X] T004 [P] Create placeholder baseline file `tests/surface-baselines/FS.GG.UI.Themes.AntDesign.txt` (empty; regenerated in T013)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Test-project wiring and the pre-feature byte-identical reference that every user story's verification depends on.

**⚠️ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T005 Add a `ProjectReference` to `src/Themes.AntDesign/Themes.AntDesign.fsproj` in `tests/Controls.Tests/Controls.Tests.fsproj` (compile order after `Themes.Default`) so parity and contract suites can resolve the Ant theme
- [X] T006 Establish the SC-005 byte-identical reference: run `dotnet build -c Debug`, run the existing `DesignTokenParity` suite green, and record the current **resolved-style/contract baselines** as the pre-feature reference (the deterministic SC-005 gate), plus any GL-gated rendering goldens as advisory evidence (no production code change)

**Checkpoint**: Package scaffold builds, test project references it, pre-feature baseline captured — user stories can begin.

---

## Phase 3: User Story 1 - App author opts into a visibly Ant-styled UI without forking controls (Priority: P1) 🎯 MVP

**Goal**: A concrete `AntTheme` value + `AntIntentPolicy` that render the *existing* controls in Ant's visual language through the shared resolver/token seams — opt-in, no control forks, default theme byte-identical. This is the standalone MVP (plan P-A).

**Independent Test**: Render a control tree built only from existing controls under Default vs AntDesign; confirm (a) AntDesign differs visibly (color/spacing/radius/intent), (b) behavior + accessibility contract identical, (c) no control type is Ant-specific, (d) Default output unchanged.

### Tests for User Story 1 ⚠️ (write first, ensure they FAIL before implementation)

- [X] T007 [P] [US1] Create `tests/Controls.Tests/Feature132ThemeParityTests.fs` asserting, over a tree of EXISTING controls only: contract-identical (roles/names/states/focus order/event bindings) and ≥1 resolved visual property divergent under Default vs AntDesign, and failing if any control branches on theme identity (follow `Feature093ParityTests.fs` / `Feature105ParityTests.fs`)

### Implementation for User Story 1

- [X] T008 [P] [US1] Author `src/Themes.AntDesign/AntTheme.fsi` declaring `antLight: Theme` and `antDark: Theme` (curated signature, visibility lives here)
- [X] T009 [US1] Implement `src/Themes.AntDesign/AntTheme.fs` populating every `Theme` field (`Foreground`/`Background`/`Muted`/`Accent`/`Danger`/`Success`/`Warning`/`FontFamily`/`FontSize`/`Density`/`CornerRadius`/`ContrastRequiredRatio`) from existing Ant-derived `DesignTokensExt` entries — no hardcoded hex/literals at use sites (data-model §1)
- [X] T010 [P] [US1] Author `src/Themes.AntDesign/AntIntentPolicy.fsi` declaring `policy: StyleResolver.IntentPolicy`
- [X] T011 [US1] Implement `src/Themes.AntDesign/AntIntentPolicy.fs`: `ApplyIntent` maps `primary`/`default`/`dashed`/`text`/`link`/`danger` to distinct resolved styles over the structural base; `""`/unknown → identity (total, never raises) (data-model §2)
- [X] T012 [US1] Set compile order in `Themes.AntDesign.fsproj` (`AntTheme` before `AntIntentPolicy`)
- [X] T013 [US1] Regenerate and commit `tests/surface-baselines/FS.GG.UI.Themes.AntDesign.txt` via `dotnet fsi scripts/refresh-surface-baselines.fsx` (contains `AntTheme` + `AntIntentPolicy`)
- [X] T014 [US1] Verify: T007 parity green over existing controls; Default theme **resolved-style/contract baseline** byte-identical to T006 reference (SC-005 — the deterministic gate; any pixel golden comparison is advisory/GL-gated, not the gate); `Danger` intent observably distinct under AntDesign (SC-007)

**Checkpoint**: US1 is a shippable MVP — existing controls render Ant-styled, default theme unchanged.

---

## Phase 4: User Story 2 - Maximal, honest Ant component coverage via a coverage matrix (Priority: P1)

**Goal**: A coverage matrix dispositioning every Ant overview component, guarded by an automated honesty check (plan P-B). Drives the net-new control scope in US3.

**Independent Test**: Open the matrix; confirm exactly one row per Ant overview entry with a valid disposition; the honesty check fails on any missing row, dangling control/token reference, or blank disposition.

### Tests for User Story 2 ⚠️ (write first, ensure it FAILS before the matrix exists)

- [X] T015 [P] [US2] Create `tests/Controls.Tests/Feature132CoverageMatrixTests.fs` that parses the matrix doc and fails if (a) any Ant overview component (pinned snapshot list) lacks a row, (b) any row lacks a disposition, (c) any `existing`/`net-new`/`composition` row names a control id absent from `Catalog` or a token entry absent from the `DesignSystem` public surface (follow F6/131 parse-then-assert pattern; research §5)

### Implementation for User Story 2

- [X] T016 [US2] Author `docs/product/ant-design/coverage/ant-component-coverage.md` with one row per Ant overview component (`antComponent`, `antCategory`, `disposition`, `repoControls`, `tokenEntries`, `rationale`); header records the Ant source hub and the hub's snapshot retrieval date (`2026-06-16`) — the hub owns that date; no fabricated version label (FR-010/FR-012, data-model §4)
- [X] T017 [US2] Finalize the `net-new` vs `composition` split for borderline items (Card, Steps, Breadcrumb, Result, Descriptions, Segmented) per the "add a primitive only when composition can't express it cleanly" rule (research §4); ensure zero un-dispositioned rows (SC-002)
- [X] T018 [US2] Run T015 green: every component dispositioned, every covered row references live controls/tokens (SC-002, SC-003)

**Checkpoint**: Coverage is enumerable and machine-checked; the net-new control list for US3 is now finalized.

---

## Phase 5: User Story 3 - Net-new generic controls fill the Ant-overview gaps (Priority: P1)

**Goal**: Add the generic, theme-agnostic controls the Ant overview needs but the library lacks, each registered in the catalog and passing the same five test families as existing controls; styled by both themes, theme-aware in neither (plan P-C presentational + P-D interactive).

**Independent Test**: For each net-new control, confirm catalog registration (category, attributes, 8 visual states, accessibility), coherent render under BOTH themes, and passing Catalog/Semantic/Interaction/Accessibility/Rendering suites; no Ant-specific branching in the control.

> Final net-new vs composition membership comes from the T017 matrix. Items dispositioned `composition` get a documented composition in the matrix instead of a module here.

### Catalog + shared test scaffolding (sequential — shared files)

- [X] T019 [US3] Add net-new control rows to `src/Controls/catalog.yml` (source of truth) for the finalized net-new ids, each with `id`, `category`, `module`/`typedModule`, required/common attributes, the standard 8 `visualStates`, `accessibility`, `events`, `supportStatus: supported` (contract R1; data-model §3)
- [X] T020 [US3] Regenerate the GENERATED rows in `src/Controls/Catalog.fs` and `src/Controls/Catalog.fsi` from `catalog.yml` (depends on T019)
- [X] T021 [P] [US3] Create `tests/Controls.Tests/Feature132NewControlContractTests.fs` parameterized over the net-new control ids, exercising all five families (Catalog/Semantic/Interaction/Accessibility/Rendering) and dual-theme render; write to FAIL before the modules exist (FR-008, SC-004, contract R6)

### Presentational batch (plan P-C) — `[P]` across modules (distinct files)

- [X] T022 [P] [US3] `src/Controls/Display2.fsi` + `Display2.fs` — presentational display controls (e.g. `tag`, `avatar`, `card`, `descriptions`, `statistic`, `timeline`, `empty`, `skeleton`, `qr-code`, `watermark`): pure render + attributes + events, shape follows `Badge` (contract R2/R3/R5)
- [X] T023 [P] [US3] `src/Controls/Feedback2.fsi` + `Feedback2.fs` — feedback/overlay controls (e.g. `alert`, `result`, `drawer`, `popover`, `popconfirm`, `tour`, `float-button`): pure render + attributes + events
- [X] T024 [P] [US3] `src/Controls/Navigation2.fsi` + `Navigation2.fs` — navigation controls (e.g. `breadcrumb`, `steps`, `pagination`, `segmented`, `anchor`, `affix`): pure render + attributes + events

### Interactive batch (plan P-D)

- [X] T025 [P] [US3] `src/Controls/Interactive2.fsi` + `Interactive2.fs` — parent-state attribute+event interactive controls (e.g. `collapse` expand/collapse, `rate`, `carousel`, `calendar`); Ant-specific states expressed via the standard 8 visual states or documented (spec edge cases)
- [X] T026 [US3] `src/Controls/DataEntry2.fsi` + `DataEntry2.fs` — genuinely workflow-bearing controls (e.g. `cascader`, `transfer`, `upload`, `auto-complete`, `mentions`) exposing `Model`/`Msg`/`Effect` like `DataGrid`/`Collections`; no ad-hoc internal mutable state (contract R5, Constitution IV). Items where MVU is too heavy for the value are dispositioned composition/deferred in the matrix instead

### Render wiring, surface, verification

- [X] T027 [US3] Wire the new control kinds' render/geometry in `src/Controls/Control.fs` through `StyleResolver` only — no branch on theme identity (FR-007, contract R4)
- [X] T028 [US3] Register all new modules in `src/Controls/Controls.fsproj` in correct compile order
- [X] T029 [US3] Regenerate `tests/surface-baselines/FS.GG.UI.Controls.txt`; review the diff so the only new rows are the net-new control modules (and their `Model`/`Msg`/`Effect`/attribute-helper types) — no incidental surface leaks (contract surface-baseline expectation, FR-015)
- [X] T030 [US3] Run T021 green for every net-new control (all five families) and confirm each renders coherently under Default (neutral) and AntDesign (Ant-styled) (SC-004)

**Checkpoint**: All net-new controls exist, are cataloged, dual-theme-render, and pass the same contracts as existing controls.

---

## Phase 6: User Story 4 - "One control set, many themes" parity is proven (Priority: P2)

**Goal**: Harden the parity test to span every control category including each net-new family, and land the Tier-1 provenance (decision record, module map, final baselines) (plan P-E).

**Independent Test**: Run the parity test over a representative tree covering every category (display/input/selection/layout/navigation/feedback/data/overlay + each net-new family); confirm it asserts contract-identity + visual-divergence and passes, and fails on any theme-identity branch.

- [X] T031 [US4] Extend `tests/Controls.Tests/Feature132ThemeParityTests.fs` so its sample tree spans every catalog category INCLUDING each net-new family (coverage not silently narrowed to easy controls) (FR-013, SC-001)
- [X] T032 [US4] Confirm the parity assertions: behavior/accessibility contract identical, ≥1 resolved visual property divergent, and a failing case if any control reads theme identity to branch behavior (FR-014)
- [X] T033 [P] [US4] Write the Tier-1 decision record `docs/product/decisions/0006-antdesign-theme-and-new-controls.md` covering the new public package, the net-new public controls (surface delta), the chosen Ant snapshot, and the no-token-value-change / opt-in / no-fork guarantees (FR-017)
- [X] T034 [P] [US4] Update `docs/product/module-map.md` moving the AntDesign theme row from "planned" to "owned assembly" (FR-016)
- [X] T035 [US4] Final baseline regeneration for both packages via `scripts/refresh-surface-baselines.fsx`; review `git diff --stat tests/surface-baselines/` for only intended churn (FR-015)
- [X] T036 [US4] Run the full `tests/Controls.Tests` suite plus the surface-drift and `DesignTokenParity` gates green — zero existing token-value changes, committed baselines for both packages (SC-005, SC-006)

**Checkpoint**: The layering invariant is machine-proven; Tier-1 provenance complete.

---

## Phase 7: User Story 5 - Ant Design Charts queued as a follow-up in the plan (Priority: P3)

**Goal**: Record the Ant Design Charts work as an explicit follow-up entry in the active implementation plan — no chart code here.

**Independent Test**: Open the active plan; confirm a dedicated, correctly-scoped Ant Design Charts follow-up entry exists, cites the charts overview source, scopes it as design-language adoption (no JS/React dependency), and is sequenced after D2.1.

- [X] T037 [US5] Append/confirm the Ant Design Charts follow-up entry (Phase D2-Charts / task D2C.1) in `docs/reports/2026-06-15-11-34-missing-features-implementation-plan.md`, citing the charts overview source, scoped as catalog + token mapping over the existing chart controls (line/bar/pie/scatter), no JS/React charting dependency, sequenced after this feature (FR-019)
- [X] T038 [US5] Verify this feature ships NO chart implementation code beyond the plan entry (FR-020, SC-008)

**Checkpoint**: Charts roadmap captured; this feature's scope stays bounded.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T039 [P] Run the full `quickstart.md` validation (sections 1–6) end-to-end and confirm each expected outcome
- [X] T040 [P] Cross-link the coverage matrix from the Ant reference hub `docs/product/ant-design/reference/ant-llms-sources.md` and tidy matrix rationale wording
- [X] T041 Final review of the combined public-surface diff (`Themes.AntDesign` + `Controls`) for incidental leaks; confirm both drift gates remain green

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Foundational. The standalone MVP (P-A)
- **US2 (Phase 4)**: Depends on Foundational; independent of US1. Its matrix output (T017) finalizes the US3 control list
- **US3 (Phase 5)**: Depends on Foundational; the net-new id list is finalized by US2/T017, so US3 implementation should follow US2
- **US4 (Phase 6)**: Depends on US1 (parity scaffold) and US3 (net-new families to span); P-E hardening
- **US5 (Phase 7)**: Independent docs-only; can run any time after Setup
- **Polish (Phase 8)**: After all desired stories complete

### Within Each User Story

- Tests are written first and must FAIL before implementation (T007, T015, T021)
- `.fsi` before `.fs` (Constitution I/II); catalog row + `Catalog.fs` regen before module bodies that reference generated kinds
- Baseline regeneration after the modules compile
- Story complete and verified before moving to the next priority

### Parallel Opportunities

- T003, T004 (Setup) in parallel
- T008 and T010 (`.fsi` authoring) in parallel; T007 test in parallel with `.fsi` authoring
- US2 and US5 can proceed in parallel with US1
- T022, T023, T024, T025 (distinct net-new module files) in parallel after T020
- T033, T034 (decision record + module map) in parallel during US4

---

## Parallel Example: User Story 1

```bash
# Author signatures + failing parity test together (distinct files):
Task: "Create Feature132ThemeParityTests.fs (existing-controls parity)"   # T007
Task: "Author src/Themes.AntDesign/AntTheme.fsi"                          # T008
Task: "Author src/Themes.AntDesign/AntIntentPolicy.fsi"                   # T010
```

## Parallel Example: User Story 3 (net-new modules)

```bash
# After catalog rows (T019) + Catalog.fs regen (T020) + failing contract test (T021):
Task: "Implement src/Controls/Display2.fsi + Display2.fs"      # T022
Task: "Implement src/Controls/Feedback2.fsi + Feedback2.fs"    # T023
Task: "Implement src/Controls/Navigation2.fsi + Navigation2.fs"# T024
Task: "Implement src/Controls/Interactive2.fsi + Interactive2.fs" # T025
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → Phase 2 Foundational
2. Phase 3 (US1): AntTheme + AntIntentPolicy over existing controls
3. **STOP and VALIDATE**: parity green over existing controls, Default byte-identical, Danger intent visible
4. This is a shippable theme on its own (plan P-A)

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → ship the Ant theme over existing controls (MVP)
3. US2 → coverage matrix + honesty check (finalizes US3 scope)
4. US3 → net-new controls (presentational then interactive)
5. US4 → parity hardening + Tier-1 provenance + final baselines
6. US5 → charts follow-up plan entry (docs-only, can land anytime)

---

## Notes

- `[P]` = different files, no dependencies on incomplete tasks
- Shared files force sequencing: `catalog.yml` (T019), `Catalog.fs/.fsi` (T020), `Control.fs` (T027), `Controls.fsproj` (T028), the single parity file (T007/T031), and the single contract-test file (T021) are NOT `[P]` with each other
- Tier-1 discipline: every new public module has a `.fsi`; baselines regenerated and committed in the same change
- Verify failing tests before implementing; keep the Default theme byte-identical at every step (SC-005)
- Commit after each task or logical group
