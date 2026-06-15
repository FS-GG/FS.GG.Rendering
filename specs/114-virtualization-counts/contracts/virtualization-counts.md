# Contract — Virtualization Counts & Overscan (Feature 114)

The **internal** counting carrier (pinned via `InternalsVisibleTo`) + the additive public surface on
already-baselined types. Behaviour clauses are what the five `Feature114*Tests` suites assert.

## C1 — `countVirtual` → `VirtualMaterialized` / `VirtualTotal` (internal)

- A read-only walk: `VirtualMaterialized` = materialized `data-grid-row` count; `VirtualTotal` = sum of each
  `data-grid`'s logical `Total`. Render output unchanged; `0/0` with no virtualized control; aggregated.

*Pins*: FR-013. *Used by*: US4 (via the public metrics).

## C2 — `Collections.visibleRange` (+ `CollectionModel.Overscan`) (public, additive)

```fsharp
val visibleRange:
    rowHeight: float -> viewportHeight: float -> scrollOffset: float -> totalItems: int -> overscan: int -> VisibleRange
```

- `overscan = 0` ⇒ byte-identical to the historic visible slice (FR-006).
- `overscan = N` ⇒ `Count = V + 2N`, edge-clamped (no index `< 0` or `>= Total`); negative clamps to 0
  (FR-002/FR-007).
- `VisibleRange { FirstIndex; Count; Total }`.

*Pins*: FR-002, FR-003, FR-004, FR-006, FR-007, FR-008. *Used by*: US1, US2.

## C3 — Offscreen logical addressability (via `DataGrid.update`)

- Selecting / toggling / focusing / relocating to an offscreen row MUST update the logical model **without
  materializing** the path; a boundary-crossing relocate lands on the correct next logical row and advances
  the window (relocate, not expand). A visible-row dispatch is byte-identical to pre-feature.

*Pins*: FR-009, FR-010, FR-011, FR-016. *Used by*: US3.

## C4 — `CollectionPosition` / `AccessibilityMetadata.Collection` (public, additive)

```fsharp
type CollectionPosition = { TotalItems: int; FocusedIndex: int option }
// AccessibilityMetadata.Collection: CollectionPosition option
```

- `TotalItems` / `FocusedIndex` are the **logical** values (independent of materialization); `Collection =
  None` for a non-collection control (at-rest a11y byte-identical).

*Pins*: FR-012. *Used by*: US3.

## C5 — `FrameMetrics.VirtualItemsMaterialized` / `VirtualItemsTotal` (public, additive)

- `materialized ≤ visible`, `total = RowCount`, non-scaling across totals; `0/0` with no virtualized control;
  aggregated across multiple grids.

*Pins*: FR-013, FR-014. *Used by*: US4.

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-017): `countVirtual`/`VirtualMaterialized`/`VirtualTotal` are
  `internal`; every public type touched (`FrameMetrics`, `CollectionModel`, `Collections`, `VisibleRange`,
  `CollectionPosition`, `AccessibilityMetadata`, `DataGrid`) is already in the committed baseline; the additions
  are field/parameter-level (type-granular baseline). Baselines stay byte-unchanged.
</content>
