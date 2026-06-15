# Feature Specification: Virtualization Counts & Overscan (Feature 114)

**Feature Branch**: `114-virtualization-counts`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C7** in the 2026-06-15 missing-features plan
(Workstream C pattern: 091 / 092 / 093 / 095 / 096 / 099 / 097 / 103 / 110 / 113).

A virtualized DataGrid realizes only the rows in (and near) the viewport. Feature 114 makes that bounded
materialization **observable, opt-in-tunable, and logically addressable**:

- a read-only `countVirtual` walk tallies the **materialized** `data-grid-row` count and the **logical**
  total per frame, surfaced as `FrameMetrics.VirtualItemsMaterialized` / `VirtualItemsTotal`;
- an opt-in `Overscan` widens the realized window by up to `2 × Overscan` real, edge-clamped adjacent rows
  (default `0` reproduces the historic visible slice byte-identically);
- offscreen rows are **logically addressable** — selecting / toggling / focusing / relocating to an
  offscreen row updates the logical model without materializing the path;
- accessibility reports the **logical** total and focused index (independent of materialization).

The materialized count is **bounded** (`visibleCount + 2 × overscan`) and **does not scale** with the total:
a 100-, 1000-, and 10000-row grid with the same viewport + overscan all report the same materialized count.

The implementation (`countVirtual` + `WorkReductionRecord.VirtualMaterialized`/`VirtualTotal` in
`RetainedRender.fs`/`.fsi`; the public `FrameMetrics.VirtualItems*`; `CollectionModel.Overscan` +
`Collections.visibleRange`'s overscan parameter; `CollectionPosition` + `AccessibilityMetadata.Collection`)
and the five suites (`Feature114OverscanTests`, `Feature114OverscanParityTests`, `Feature114OffscreenTests`,
`Feature114AccessibilityTests` in `Controls.Tests`; `Feature114VirtualMetricsTests` in `Elmish.Tests`)
**already exist** in the imported source. **No Spec Kit spec/plan/tasks describe this work**, and 114
imported with **no `readiness/`**. This document backfills the contract.

The counting carrier is **assembly-internal**; every public type touched (`FrameMetrics`, `CollectionModel`,
`Collections`, `CollectionPosition`, `AccessibilityMetadata`, `DataGrid`) is **already in the committed
baseline**, and 114's additions are field/parameter-level on those types — so the backfill adds **zero new
public-surface-baseline delta** (type-granular baseline). Per the constitution's vertical-slice rule the
in-assembly tests are the user-reachable surface.

**Scope boundary.** 114 owns the virtualization **counts**, **overscan**, **offscreen addressability**, and
**accessibility position**. The neighbouring metrics/caches are owned by their own features: layout cache
097, memo seam 113, picture cache 116, text cache 117, replay cache 120.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Materialization is bounded and does not scale with the total (Priority: P1)

A virtualized grid realizes only `visibleCount + 2 × overscan` rows, edge-clamped — the same count whether
the grid has 100, 1000, or 10000 rows. With overscan `N` a mid-list window realizes exactly `V + 2N` real
rows; a grid that fits the window realizes the whole set.

**Why this priority**: The whole point of virtualization is bounded work; observing and bounding it is the MVP.

**Independent Test**: Assert the realized `Count` is identical across 100/1000/10000 at a fixed overscan
(FR-003/SC-001); a mid-list window with overscan N realizes exactly `V + 2N` (FR-007); edge-clamping at top
and bottom (FR-002); a grid fitting the window realizes all (FR-004); negative overscan clamps to 0.

**Acceptance Scenarios**:

1. **Given** 100/1000/10000-row grids at a fixed overscan, **When** windowed, **Then** the realized `Count`
   is identical (FR-003/SC-001).
2. **Given** overscan N mid-list, **When** windowed, **Then** exactly `V + 2N` real rows are realized,
   edge-clamped (FR-002/FR-007); a fitting grid realizes the whole set (FR-004).

---

### User Story 2 - Overscan default is byte-identical; opt-in adds only real adjacent rows (Priority: P1)

Overscan `0` realizes **exactly** the historic visible slice — same keys, byte-identical scene. Opt-in
overscan `N` adds only **real, contiguous, edge-clamped** adjacent rows; the visible region is unchanged and
keyed rows stay stable across a scroll where the window overlaps.

**Why this priority**: Co-critical with US1. The feature must be a no-op by default (no regression) and add
only genuine rows when opted in.

**Independent Test**: Overscan 0 → byte-identical scene + same keys (FR-006/SC-002); overscan N → only real
contiguous adjacent rows, visible region unchanged (FR-007/SC-003); edge-clamped (no fabricated rows); keyed
rows stable across a scroll (FR-008).

**Acceptance Scenarios**:

1. **Given** overscan 0, **When** rendered, **Then** the scene is byte-identical to the historic visible slice
   with the same keys (FR-006/SC-002).
2. **Given** overscan N, **When** rendered, **Then** only real contiguous adjacent rows are added, the visible
   region is unchanged, and a scroll reuses keys where the window overlaps (FR-007/FR-008/SC-003).

---

### User Story 3 - Offscreen rows are logically addressable; accessibility is logical (Priority: P2)

Selecting, toggling, focusing, or relocating to an **offscreen** row updates the logical model **without
materializing the path**; a boundary-crossing relocation lands on the correct next logical row and advances
the window. Accessibility reports the **logical** `TotalItems` and `FocusedIndex` (independent of
materialization); a non-collection control reports `Collection = None` (at-rest a11y byte-identical).

**Why this priority**: P2 — correctness + accessibility guard. Virtualization must not make offscreen content
unreachable or misreport size/position to assistive tech.

**Independent Test**: Select/toggle/focus an offscreen row without materializing it (FR-009/FR-010/SC-004);
boundary-crossing relocation lands correctly and advances the window (FR-011/SC-005); a11y reports logical
total + focused index (FR-012); a relocate brings the target into the window (relocate, not expand — O4); a
visible-row dispatch is byte-identical to pre-feature (FR-016).

**Acceptance Scenarios**:

1. **Given** an offscreen row, **When** selected/toggled/focused, **Then** the logical model updates without
   materializing the path (FR-009/FR-010/SC-004).
2. **Given** a boundary-crossing relocation, **When** applied, **Then** it lands on the correct next logical
   row and advances the window (FR-011/SC-005); a11y reports the logical total + focused index (FR-012); a
   non-collection control reports `Collection = None`.

---

### User Story 4 - Virtual metrics are observable and non-scaling over a host script (Priority: P2)

Over `Perf.runScript`, a frame building a virtualized grid reports `materialized ≤ visible` and
`total = RowCount`; the materialized count does **not** scale across 100/1000/10000; a frame with no
virtualized control (and an idle frame) reports `0 / 0`; multiple virtualized controls **aggregate**.

**Why this priority**: P2 — the observability guard that the bound (US1) holds in the live metric surface.

**Independent Test**: Assert the five metric regimes (materialized ≤ visible + total = RowCount; non-scaling;
no-control 0/0; idle 0/0; aggregation).

**Acceptance Scenarios**:

1. **Given** a virtualized-grid frame, **When** scripted, **Then** `VirtualItemsMaterialized ≤ visible` and
   `VirtualItemsTotal = RowCount` (FR-013); the materialized count is identical across totals (FR-014/SC-006).
2. **Given** no virtualized control / an idle frame / multiple grids, **When** scripted, **Then** `0/0` /
   `0/0` / aggregated counts respectively.

---

### Edge Cases

- **Total fits the window**: the whole set is realized transparently.
- **Top/bottom edge**: realized window is edge-clamped (no index `< 0` or `>= Total`); no fabricated rows.
- **Negative overscan**: clamped to 0 on the way in.
- **Offscreen target**: addressed/relocated logically without materializing the path.
- **No virtualized control / idle frame**: metrics `0 / 0`.
- **Nothing focused**: `FocusedIndex = None`.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-002**: The realized window MUST be edge-clamped (no index `< 0` or `>= Total`).
- **FR-003 / FR-014**: The realized window / materialized count MUST NOT scale with the total (identical at
  100/1000/10000 for a fixed window + overscan).
- **FR-004**: A grid whose total fits the window MUST realize the whole set transparently.
- **FR-006**: Overscan default (`0`) MUST be byte-identical to the pre-feature visible slice.
- **FR-007**: Opt-in overscan `N` MUST add only real, contiguous, edge-clamped adjacent rows (`V + 2N`).
- **FR-008**: Keyed rows MUST be stable across a scroll (same key reused where the window overlaps).
- **FR-009 / FR-010**: Focusing / selecting / toggling / relocating to an offscreen row MUST update the
  logical model **without materializing** the path.
- **FR-011**: A boundary-crossing relocation MUST land on the correct next logical row and advance the window.
- **FR-012**: Accessibility `TotalItems` / `FocusedIndex` MUST be the **logical** values (independent of
  materialization); a non-collection control reports `Collection = None`.
- **FR-013**: Frame metrics MUST report `materialized ≤ visible` and `total = RowCount`, aggregated across
  multiple virtualized controls; `0/0` when none.
- **FR-016**: Dispatch for an already-materialized (visible) row MUST be byte-identical to pre-feature.
- **FR-017**: The backfill MUST add **zero new public-surface-baseline delta** (the counting carrier is
  internal; every public type touched is already baselined, additions are field/param-level).

### Key Entities *(include if feature involves data)*

- **countVirtual**: the read-only per-frame walk → `VirtualMaterialized` (realized `data-grid-row` count) +
  `VirtualTotal` (sum of each `data-grid`'s logical `Total`). Render output unchanged.
- **VirtualItemsMaterialized / VirtualItemsTotal**: the public `FrameMetrics` fields surfacing the counts.
- **CollectionModel.Overscan**: opt-in extra rows realized on each side (default 0; negative clamps to 0).
- **Collections.visibleRange (+overscan)**: computes the `VisibleRange { FirstIndex; Count; Total }`.
- **CollectionPosition / AccessibilityMetadata.Collection**: the logical total + focused index for a11y.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Bounded materialization — the materialized count is identical across totals at a fixed overscan,
  100% of cases.
- **SC-002**: Overscan 0 realizes exactly the historic visible slice (byte-identical scene), 100% of cases.
- **SC-003**: Opt-in overscan adds only real adjacent rows; the visible region is unchanged, 100% of cases.
- **SC-004**: An offscreen row is addressable (select/toggle/focus) without materializing it, 100% of cases.
- **SC-005**: A11y total + position are logical; boundary navigation lands correctly, 100% of cases.
- **SC-006**: Virtual metrics are non-scaling across 100/1000/10000, 100% of cases.

## Assumptions

- The DataGrid model, the `VisibleRange` windowing, the public `FrameMetrics`, and `AccessibilityMetadata`
  already exist. 114 is the **backfilled contract** for the counts + overscan + offscreen addressability +
  a11y position, not new-from-scratch construction.
- Every public type touched is **already in the committed baseline**; 114's additions are field/parameter-level
  (the baseline is type-granular) ⇒ **zero new** public-surface delta. The `countVirtual` carrier is internal.
- 114 imported with executable suites (Controls.Tests + Elmish.Tests, headless, no FsCheck, no GL) but **no
  `readiness/`** (tests do not self-write); authoring readiness is part of this backfill.
- This is the **C7** conformance backfill; `/speckit-*` reduce to a conformance pass.
</content>
