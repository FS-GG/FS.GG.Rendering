# Tasks: Virtualization Counts & Overscan (Feature 114)

**Input**: Design documents from `/specs/114-virtualization-counts/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/virtualization-counts.md, quickstart.md

**Tests**: Conformance pass — `countVirtual`, the public `VirtualItems*` metrics, overscan, offscreen
addressability, a11y position, and the five suites already exist. 114 imported with **no `readiness/`** —
authoring it is the genuine deliverable. No new behaviour is built.

## Format: `[ID] [P?] [Story] Description`

## Phase 1: Setup

- [X] T001 Build clean: `dotnet build FS.GG.Rendering.slnx -c Release` — 0/0
- [X] T002 [P] Confirm `countVirtual` + `WorkReductionRecord.VirtualMaterialized`/`VirtualTotal` (internal) in `src/Controls/RetainedRender.fsi`; `FrameMetrics.VirtualItemsMaterialized`/`VirtualItemsTotal` in `src/Controls.Elmish/ControlsElmish.fsi`; `CollectionModel.Overscan` + `visibleRange` overscan param in `src/Controls/Collections.fsi`; `CollectionPosition` + `AccessibilityMetadata.Collection` in `src/Controls/Types.fsi`
- [X] T003 [P] Confirm the five suites: `tests/Controls.Tests/Feature114{Overscan,OverscanParity,Offscreen,Accessibility}Tests.fs`, `tests/Elmish.Tests/Feature114VirtualMetricsTests.fs`

## Phase 2: Foundational (gates)

- [X] T004 Verify signatures vs `contracts/virtualization-counts.md` (C1–C5): `visibleRange ... -> overscan:int -> VisibleRange`; `CollectionPosition { TotalItems; FocusedIndex }`; the public `VirtualItems*` fields; internal `countVirtual` carrier (FR-007/012/013/017)
- [X] T005 Confirm zero new public-surface delta: `git status -s tests/surface-baselines/` empty — carrier internal, public touches field/param-level on already-baselined types (FR-017)

## Phase 3: User Story 1 — bounded, non-scaling materialization (P1) 🎯 MVP

- [X] T006 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "114"`; confirm `Feature114OverscanTests`: identical Count across 100/1000/10000 (SC-001/FR-003); `V + 2N` mid-list (FR-007); top/bottom edge-clamp (FR-002); fitting grid realizes all (FR-004); negative overscan clamps to 0
- [X] T007 [P] [US1] Author `readiness/us1-bounded-materialization.md` against SC-001

## Phase 4: User Story 2 — overscan parity + opt-in correctness (P1)

- [X] T008 [US2] Confirm `Feature114OverscanParityTests`: overscan 0 byte-identical slice + same keys (SC-002/FR-006); opt-in real contiguous edge-clamped rows, visible region unchanged (SC-003/FR-007); keyed rows stable across a scroll (FR-008)
- [X] T009 [P] [US2] Author `readiness/us2-overscan-parity.md` against SC-002/SC-003

## Phase 5: User Story 3 — offscreen addressability + accessibility (P2)

- [X] T010 [US3] Confirm `Feature114OffscreenTests`: select/toggle/focus/relocate an offscreen row without materializing (SC-004/FR-009/010); boundary-crossing relocation lands correctly + advances window (SC-005/FR-011); visible-row dispatch byte-identical (FR-016)
- [X] T011 [US3] Confirm `Feature114AccessibilityTests`: logical `TotalItems`/`FocusedIndex` (FR-012); `FocusedIndex = None` when nothing focused; `Collection = None` for a non-collection control
- [X] T012 [P] [US3] Author `readiness/us3-offscreen-a11y.md` against SC-004/SC-005

## Phase 6: User Story 4 — virtual metrics (P2)

- [X] T013 [US4] Run `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter "114"`; confirm `Feature114VirtualMetricsTests`: `materialized ≤ visible` + `total = RowCount` (FR-013); non-scaling across 100/1000/10000 (SC-006/FR-014); 0/0 with no control; idle 0/0; multiple grids aggregate
- [X] T014 [P] [US4] Author `readiness/us4-virtual-metrics.md` against SC-006

## Phase 7: Polish

- [X] T015 Full suite `dotnet test FS.GG.Rendering.slnx -c Release` — 0 failures (standing skips unrelated to 114)
- [X] T016 Re-confirm zero new public-surface delta (FR-017)
- [X] T017 [P] Verify readiness → SC mapping; each file discloses deterministic / counts-and-byte-identity scope (no pixel claim; offscreen = logical, not GL)
- [X] T018 Record DF-1 (redundant access modifiers in `.fs`) as out-of-scope (Complexity Tracking) — not edited here
- [X] T019 Run `/speckit-analyze` for cross-artifact consistency

## Dependencies & Parallel

- Setup → Foundational (gates) → US1–US4 (parallel; distinct suites/assertions) → Polish.
- [P] readiness authoring (T007/T009/T012/T014) writes distinct files.

## Notes

- No source edits in this backfill; a red test is a finding, not a redesign license. DF-1 deferred (T018).
- The genuine deliverable is the readiness evidence (114 imported without it; tests do not self-write).
- Surface gate (T005/T016) is the direct check of FR-017.
</content>
