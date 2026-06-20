---
description: "Task list for Fix Non-Functional Controls in the Second Ant Showcase"
---

# Tasks: Fix Non-Functional Controls in the Second Ant Showcase

**Input**: Design documents from `/specs/175-fix-showcase-controls/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: INCLUDED. The constitution gate *Spec → FSI → semantic tests → implementation* and the
contracts' "failing-first" acceptance evidence make tests mandatory for this feature. Each shared
surface change drafts its `.fsi` delta first, then a failing semantic test, then the `.fs`
implementation.

**Organization**: Tasks are grouped by user story (US1–US4) so each can be implemented and verified
independently. This is a **Tier 1** corrective feature: every public-surface touch updates `.fsi`
+ the matching surface baseline + tests in the same change.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1, US2, US3, US4 (Setup / Foundational / Polish carry no story label)
- Each task names exact file paths.

## Path Conventions

Multi-package F# framework + package-consuming sample. Shared controls under `src/Controls`,
`src/Controls.Elmish`, `src/SkiaViewer`; deterministic tests under `tests/`; sample under
`samples/SecondAntShowcase`; evidence under `specs/175-fix-showcase-controls/readiness/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Evidence scaffolding and a known-green starting baseline for the no-regression bar.

- [X] T001 [P] Create the readiness evidence scaffold directories and seed files: `specs/175-fix-showcase-controls/readiness/finding-log.md`, `specs/175-fix-showcase-controls/readiness/coverage-classification.md`, `specs/175-fix-showcase-controls/readiness/responsiveness/`, `specs/175-fix-showcase-controls/readiness/visual-parity/`, and `specs/175-fix-showcase-controls/readiness/validation-summary.md`, each with the schema headers from `data-model.md` (Finding) and `contracts/control-pass-coverage.md`.
- [X] T002 [P] Capture the current-state baseline (FR-014 no-removal proof): run `dotnet test tests/Controls.Tests tests/Elmish.Tests tests/SkiaViewer.Tests samples/SecondAntShowcase/SecondAntShowcase.Tests` and record the passing set into `specs/175-fix-showcase-controls/readiness/validation-summary.md` under a "baseline (pre-change)" heading so no existing passing behavior is lost.
- [X] T003 [P] Snapshot the current public-surface baselines for the three touched packages (`FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`) so each later `.fsi` delta can be diffed against a known baseline; note the baseline location/command in `specs/175-fix-showcase-controls/readiness/validation-summary.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The classification instrument and root-cause map that every user story depends on. No
user-story fix begins until the catalog is fully classified and each defect's tier is known.

**⚠️ CRITICAL**: Phase 3+ work cannot proceed without the root-cause map (it is what guarantees no
control is missed and that each fix is tier-correct).

- [X] T004 Build the per-control coverage classification baseline in `specs/175-fix-showcase-controls/readiness/coverage-classification.md`: enumerate every catalog control from `samples/SecondAntShowcase/SecondAntShowcase.Core/InteractionContracts.fs` and `CoverageMap.fs`, mark each `Interactive` (has contract) or `DisplayOnly` (has recorded reason), with zero unclassified (FR-012, SC-007).
- [X] T005 Produce the per-control root-cause map across the 13 interaction families in `specs/175-fix-showcase-controls/readiness/finding-log.md`: for each control failing under real input, record Symptom, RootCause, FixTier (Tier1 shared-surface vs Tier2 sample-local), Status=`Open`, per the `Finding` entity in `data-model.md` and research R6.
- [X] T006 [P] Draft the `.fsi` surface deltas first (no `.fs` yet) for the scroll seam: `src/Controls/Widgets/Containers.fsi` (`ScrollViewerProps`/`ScrollViewer` offset-state seam, `OnChanged` retained as optional report), `src/Controls/Control.fsi` (`scrollAffordance`/`scrollViewerGeom` offset-aware signatures), and `src/Controls/Pointer.fsi` (thumb-drag + scroll-key paths) per `contracts/scroll-interaction.md`.
- [X] T007 [P] Draft the `.fsi` surface deltas first for the interaction-state seam: `src/Controls.Elmish/ControlsElmish.fsi` (hover/focus retained-repaint trigger) and `src/Controls/ControlRuntime.fsi` (`applyRuntimeVisualState` coverage seam) per `contracts/interaction-state.md`.

**Checkpoint**: Catalog fully classified, every defect logged with a tier, and the `.fsi` seams
drafted — user-story implementation can begin.

---

## Phase 3: User Story 1 - Scroll the content region with real input (Priority: P1) 🎯 MVP

**Goal**: The content-region `scroll-viewer` scrolls under drag, wheel, and keyboard; the thumb
tracks the offset; no draggable thumb when content fits; hit-testing inside the region is
offset-aware.

**Independent Test**: Open a page taller than the content region; drag the thumb, wheel, and use
scroll keys; confirm content offset and thumb position change together, bottom content becomes
reachable, and a fitting page shows no draggable thumb (SC-001).

### Tests for User Story 1 (write first, ensure they FAIL) ⚠️

- [X] T008 [P] [US1] Failing test: `applyScrollDelta` offset clamps to `[0, max(0, ContentHeight - ViewportHeight)]` at both bounds with no overscroll, in `tests/Controls.Tests`.
- [X] T009 [P] [US1] Failing test: thumb height derives from the viewport/content ratio and thumb position from `Offset / (ContentHeight - ViewportHeight)`; no draggable thumb when `ContentHeight <= ViewportHeight` (incl. one-pixel-overflow dead-zone), in `tests/Controls.Tests`.
- [X] T010 [P] [US1] Failing test: offset-aware hit-testing resolves the correct control inside a scrolled `scroll-viewer` (subtract offset before resolve), in `tests/Controls.Tests`.
- [X] T011 [P] [US1] Failing test: `Scroll(...)` interaction routing updates scroll offset and triggers a damage-local retained repaint (no full-tree prepare), in `tests/Elmish.Tests`.
- [X] T012 [P] [US1] Regression-guard test (expected GREEN, not failing-first): viewer wheel/`PointerScrolled` delivery emits a `Scroll(control, dx, dy, x, y)` interaction over the region, in `tests/SkiaViewer.Tests`. **Exception to the failing-first rule**: viewer scroll delivery already exists (research R2: `SkiaViewer.fs:2325` → `Pointer.fs:242`), so this task locks in the existing behavior rather than driving new code; the new-code failing-first coverage for consuming that interaction into a scroll offset lives in T011 (`tests/Elmish.Tests`).

### Implementation for User Story 1

- [X] T013 [US1] Implement scroll-offset runtime state keyed by the `scroll-viewer` `ControlId` and the single `applyScrollDelta` transition (clamped) in `src/Controls/Widgets/Containers.fs` (matching the `Containers.fsi` seam from T006).
- [X] T014 [US1] Translate content by `-Offset` and clip to the viewport, and paint the thumb (height from ratio, position from offset, no thumb when not scrollable) in `src/Controls/Control.fs` (`scrollViewerGeom`/`scrollAffordance`).
- [X] T015 [US1] Implement offset-aware hit-testing at the `hitTestComputed` seam in `src/Controls/Control.fs` (and `src/Controls/Pointer.fs` if the routing call site needs it), subtracting the region offset for `scroll-viewer` descendants (FR-009).
- [X] T016 [US1] Add thumb-drag and the enumerated scroll-key paths reducing to `applyScrollDelta` in `src/Controls/Pointer.fs` (matching the `Pointer.fsi` seam from T006). Scroll keys per `contracts/scroll-interaction.md`: `ArrowUp`/`ArrowDown` (line step), `PageUp`/`PageDown` (viewport-height step), `Home`/`End` (top/bottom), `Space`/`Shift+Space` (page down/up).
- [X] T017 [US1] Consume the `Scroll` interaction into the scroll offset with a damage-local retained repaint in `src/Controls.Elmish/ControlsElmish.fs` (re-encoded `Scroll` path).
- [X] T018 [US1] Bind the content-region `ScrollViewer`'s scroll affordance (and optional `OnChanged` report) in `samples/SecondAntShowcase/SecondAntShowcase.Core/Shell.fs` so the showcase content region scrolls.
- [X] T019 [US1] Update the public-surface baselines for `Containers.fsi`, `Control.fsi`, and `Pointer.fsi` deltas and run the surface-drift check; record the deltas in `specs/175-fix-showcase-controls/readiness/validation-summary.md`.
- [ ] T020 [US1] Capture US1 evidence: live desktop scroll run (drag/wheel/keyboard, thumb tracks offset, bottom content reachable) into `specs/175-fix-showcase-controls/readiness/responsiveness/`, or `environment-limited` result with disclosed substitute; mark the related findings `Fixed`/`ReVerified` in `finding-log.md`.

**Checkpoint**: The content region scrolls under real input with a tracking thumb — US1 is
independently demonstrable (MVP).

---

## Phase 4: User Story 2 - See hover and focus feedback on interactive controls (Priority: P1)

**Goal**: Every interactive control shows an Ant hover state on pointer-over and a distinct focus
affordance on keyboard focus; combined hover+focus shows both; display-only controls stay static.

**Independent Test**: Move the pointer across each interactive control and tab focus through a page;
confirm visible hover on over (clears on leave), distinct focus that moves with focus, combined
state shows both, and display-only controls present no interactive affordance (SC-002).

### Tests for User Story 2 (write first, ensure they FAIL) ⚠️

- [X] T021 [P] [US2] Failing test: `applyRuntimeVisualState` stamps the correct `VisualState` (`Hovered`/`Focused`/combined) for every interactive kind used by the showcase, including the `ghost` nav buttons, in `tests/Controls.Tests`.
- [X] T022 [P] [US2] Failing test: combined hover+focus keeps both affordances (neither suppresses the other) and focus persists when the pointer leaves while focus remains; hover state clears when the pointer leaves the window and when the page changes while a control is hovered (spec Edge Cases); display-only kinds stay `Normal` under input, in `tests/Controls.Tests`.
- [X] T023 [P] [US2] Failing test: `HoverChanged`/`FocusChanged` routing triggers a model-unchanged, damage-local retained repaint (does not rebuild the view tree per pointer move), in `tests/Elmish.Tests`.

### Implementation for User Story 2

- [X] T024 [US2] Extend `applyRuntimeVisualState` to cover every interactive kind used by the showcase, including the `ghost` nav-button style path, in `src/Controls/ControlRuntime.fs` (matching the `ControlRuntime.fsi` seam from T007).
- [X] T025 [US2] Ensure `HoverChanged`/`FocusChanged` (`HoverControl`/`FocusControl`) trigger the retained repaint on hover-enter/leave and focus change (not only on model change) in `src/Controls.Elmish/ControlsElmish.fs` (matching the `ControlsElmish.fsi` seam from T007).
- [X] T026 [US2] Paint `Hovered`/`Focused`/combined per-kind via resolved Ant palette roles in the `*Geom` writers in `src/Controls/Control.fs`, ensuring neither state suppresses the other.
- [X] T027 [US2] Update the public-surface baselines for `ControlRuntime.fsi` and `ControlsElmish.fsi` deltas and run the surface-drift check; record the deltas in `specs/175-fix-showcase-controls/readiness/validation-summary.md`.
- [ ] T028 [US2] Capture US2 evidence: live desktop run showing hover/focus appear and clear across interactive controls within the live-responsiveness target into `specs/175-fix-showcase-controls/readiness/responsiveness/`, or `environment-limited` with disclosed substitute; mark related findings `Fixed`/`ReVerified` in `finding-log.md`.

**Checkpoint**: Hover and focus feedback are visible on interactive controls and absent on
display-only controls — US2 is independently demonstrable.

---

## Phase 5: User Story 3 - Every interactive control responds to genuine input (Priority: P1)

**Goal**: Every control with an interaction contract produces its promised visible state change
under real input (matching scripted coverage), overlays open/dismiss with focus return, and every
display-only control is confirmed static — zero unresolved findings, none unclassified.

**Independent Test**: For each interaction family perform the contract's primary interaction with
real input and confirm the promised evidence; confirm live equals scripted; confirm display-only
controls stay static; run the coverage check — every control classified, none failing (SC-003,
SC-004, SC-005, SC-007).

### Tests for User Story 3 (write first, ensure they FAIL) ⚠️

- [X] T029 [P] [US3] Failing per-family live-vs-scripted parity tests: for each of the 13 interaction families, assert the live-input evidence equals the scripted `Model.update` evidence, in `samples/SecondAntShowcase/SecondAntShowcase.Tests`.
- [X] T030 [P] [US3] Failing test: overlay-bearing controls (drawer, popover, popconfirm, tooltip, dialog, tour, context menu) open and dismiss under real input and, on close, return focus to the opening trigger control (or the nearest focusable ancestor if the trigger is gone/unfocusable, per `contracts/interaction-state.md` and FR-013), in `samples/SecondAntShowcase/SecondAntShowcase.Tests` (and/or `tests/Elmish.Tests` for `interpretOverlayEffect`).
- [X] T031 [P] [US3] Failing coverage check: every catalog control resolves to exactly one classification and no interactive control fails under the verification path, in `samples/SecondAntShowcase/SecondAntShowcase.Tests`.

### Implementation for User Story 3

- [X] T032 [US3] Apply the shared (Tier 1) per-family fixes identified in the root-cause map (retained pointer routing, focus traversal, offset-aware activation) across `src/Controls/Pointer.fs`, `src/Controls.Elmish/ControlsElmish.fs`, and `src/Controls/Control.fs` as each finding dictates.
- [X] T033 [US3] Implement overlay open/dismiss + focus-return under real input through `interpretOverlayEffect` in `src/Controls.Elmish/ControlsElmish.fs` (FR-013).
- [X] T034 [P] [US3] Apply the sample-local (Tier 2) wiring fixes (e.g. unbound `OnChanged`, page-level bindings) in `samples/SecondAntShowcase/SecondAntShowcase.Core/` (`Pages.fs`, `Templates.fs`, `Model.fs` as the map indicates), recording each as Tier 2 in `finding-log.md`.
- [X] T035 [US3] Confirm the ~30 display-only controls remain static with no interactive affordance, consistent with their recorded reasons; record confirmation in `specs/175-fix-showcase-controls/readiness/coverage-classification.md` (FR-008, SC-004).
- [X] T036 [US3] Update any public-surface baselines touched by Tier 1 fixes in T032/T033 and run the surface-drift check; record deltas in `specs/175-fix-showcase-controls/readiness/validation-summary.md`.
- [X] T037 [US3] Drive every finding to `ReVerified` in `specs/175-fix-showcase-controls/readiness/finding-log.md` (Open → Fixed → ReVerified with verification = live path / test name / environment-limited); confirm zero unresolved (SC-005).

**Checkpoint**: The whole catalog responds to real input matching scripted coverage; every control
classified; zero unresolved findings — US3 complete.

---

## Phase 6: User Story 4 - Each fixed control stays correct in light and dark Ant appearances (Priority: P2)

**Goal**: Hover/focus/active/scroll affordances use correct Ant palette roles in antLight and
antDark with no new spacing/alignment/clipping/contrast regression.

**Independent Test**: Produce the visual review set for affected pages in both appearances and
confirm correct palette roles and no new visual regression (SC-006).

### Tests for User Story 4 (write first, ensure they FAIL) ⚠️

- [ ] T038 [P] [US4] Failing visual-parity test: hover/focus/active/scroll affordances resolve correct Ant palette roles in both antLight and antDark for the affected pages, in `samples/SecondAntShowcase/SecondAntShowcase.Tests`.

### Implementation for User Story 4

- [X] T039 [US4] Produce the light/dark visual review sets for the affected pages (including the accepted minimum size) into `specs/175-fix-showcase-controls/readiness/visual-parity/` via the existing visual-readiness path (`VisualReadinessWorkflow.fs`).
- [X] T040 [US4] Resolve any palette-role/spacing/clipping/contrast regression surfaced by T038/T039 in `src/Controls/Control.fs` paint paths (palette roles only — no hardcoded colors, no per-theme fork); re-run the visual-parity test to green.

**Checkpoint**: Affected pages are visually correct in both appearances with no new regression — US4
complete.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final no-regression proof, budgets, and validation roll-up.

- [X] T041 Re-run the Feature 174 responsiveness budgets via the existing runner (`ResponsivenessWorkflow.fs` / `SecondAntShowcase.App` Responsiveness CLI) and confirm no regression (button follow-up median ≤ 150 ms / p95 ≤ 250 ms; navigation median ≤ 250 ms / p95 ≤ 500 ms). Additionally capture scroll/hover/focus follow-up-frame latency and confirm each appears within the same live-responsiveness target as button activation (median ≤ 150 ms / p95 ≤ 250 ms), satisfying the plan Performance Goal; record all results (including an `environment-limited` note where the headless lane cannot measure live latency) in `specs/175-fix-showcase-controls/readiness/responsiveness/`.
- [X] T042 [P] Re-run the full deterministic suite (`tests/Controls.Tests`, `tests/Elmish.Tests`, `tests/SkiaViewer.Tests`, `samples/SecondAntShowcase/SecondAntShowcase.Tests`) and confirm all green plus the FR-014 no-removal baseline from T002 is preserved.
- [X] T043 Run the full public-surface drift check across all three touched packages and confirm every `.fsi` delta has a matching baseline update (Tier 1 obligation).
- [ ] T044 Complete `specs/175-fix-showcase-controls/readiness/validation-summary.md`: overall pass/limitation summary, links to finding-log (all `ReVerified`), coverage-classification (all classified), responsiveness, and visual-parity evidence; disclose any synthetic substitute (`// SYNTHETIC:` / `Synthetic` token / PR note).
- [ ] T045 [P] Execute the `quickstart.md` validation walkthrough end to end (build → deterministic tests → scenarios 1–4) and confirm the documented expected results.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories (the root-cause map and
  classification baseline are the instrument every story relies on).
- **User Stories (Phase 3–6)**: All depend on Foundational. US1, US2 are independent of each other.
  US3 builds on the root-cause map and, for offset-aware activation inside the scroll region,
  benefits from US1's hit-test seam (T015). US4 depends on the affordances added by US1–US3 being in
  place for the affected pages.
- **Polish (Phase 7)**: Depends on all targeted user stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. Self-contained (scroll seam + sample binding).
- **US2 (P1)**: After Foundational. Independent of US1 (different `.fsi`/`.fs` seams).
- **US3 (P1)**: After Foundational. Tier 1 per-family fixes; offset-aware activation cases assume
  US1's T015. Independently testable via per-family parity + coverage check.
- **US4 (P2)**: After the controls it reviews exist (US1–US3 affordances). Independently testable as
  a visual-parity pass.

### Within Each User Story

- `.fsi` seam (drafted in Foundational) → failing semantic test → `.fs` implementation → surface
  baseline update → evidence capture.

### Parallel Opportunities

- Setup T001/T002/T003 run in parallel.
- Foundational T006/T007 (`.fsi` drafts) run in parallel after T004/T005.
- US1 tests T008–T012 run in parallel (different test projects/files).
- US2 tests T021–T023 run in parallel.
- US3 tests T029–T031 run in parallel.
- After Foundational, **US1 and US2 can be developed in parallel by different developers** (disjoint
  seams); US3's Tier 2 wiring (T034) parallels its Tier 1 fixes.

---

## Parallel Example: User Story 1

```bash
# Launch the failing-first tests for US1 together (different test projects):
Task: "applyScrollDelta clamp test in tests/Controls.Tests"
Task: "thumb height/position + no-thumb test in tests/Controls.Tests"
Task: "offset-aware hit-test in tests/Controls.Tests"
Task: "Scroll routing + damage-local repaint in tests/Elmish.Tests"
Task: "wheel/PointerScrolled delivery in tests/SkiaViewer.Tests"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1 Setup → Phase 2 Foundational (classification + root-cause map + `.fsi` drafts).
2. Phase 3 US1: failing tests → scroll implementation → sample binding → surface baseline → evidence.
3. **STOP and VALIDATE**: the content region scrolls under drag/wheel/keyboard with a tracking thumb.
4. Demo the MVP.

### Incremental Delivery

1. Setup + Foundational → instrument ready.
2. US1 (scroll) → test → demo (MVP).
3. US2 (hover/focus) → test → demo.
4. US3 (every control + overlays + zero findings) → test → demo.
5. US4 (light/dark fidelity) → test → demo.
6. Polish: budgets, full suite, surface drift, validation summary, quickstart walkthrough.

### Parallel Team Strategy

After Foundational: Developer A → US1, Developer B → US2 (disjoint seams), then converge on US3
(per-family pass guided by the shared root-cause map) and US4 (visual parity).

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- Tier 1 rule: every public-surface change ships `.fsi` + matching surface baseline + tests in the
  same change; per-fix tier is recorded in `finding-log.md`.
- Verify each failing-first test actually fails before implementing.
- Repaints for scroll/hover/focus MUST stay damage-local — no full-tree frame preparation.
- Disclose any synthetic substitute at the use site, in the test name, and in the PR.
- Acceptance bars: SC-001 (scroll), SC-002 (hover/focus), SC-003/SC-004/SC-007 (every control
  classified + responsive), SC-005 (zero unresolved findings), SC-006 (light/dark fidelity).
