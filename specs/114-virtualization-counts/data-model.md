# Phase 1 — Data Model: Virtualization Counts & Overscan (Feature 114)

The 114-in-scope entities. The counting carrier (`WorkReductionRecord.VirtualMaterialized`/`VirtualTotal`) is
**assembly-internal**; the public types touched are **already baselined** and 114's additions are
field/parameter-level. All pure/deterministic.

## countVirtual + WorkReductionRecord.VirtualMaterialized / VirtualTotal (internal)

A read-only walk over the lowered tree: `VirtualMaterialized` = number of materialized `data-grid-row` nodes
(the realized window); `VirtualTotal` = sum of the logical `Total` over every `data-grid` node. Render output
unchanged; both 0 with no virtualized control; aggregated across multiple grids.

## FrameMetrics.VirtualItemsMaterialized / VirtualItemsTotal (public, additive)

| Field | Type | Meaning |
|---|---|---|
| `VirtualItemsMaterialized` | `int` | Row items materialized this frame; bounded by `visible + 2 × overscan`; does **not** scale with the total. |
| `VirtualItemsTotal` | `int` | Logical item count the virtualized control(s) represent (sum of each grid's `Total`). |

## CollectionModel.Overscan + Collections.visibleRange (public, additive)

- `CollectionModel.Overscan: int` — extra logical rows realized on **each** side of the visible window;
  default `0` (byte-identical historic slice); negative clamps to 0.
- `visibleRange rowHeight viewportHeight scrollOffset totalItems overscan : VisibleRange` — the additive
  `overscan` trailing parameter; `overscan = 0` is byte-identical.
- `VisibleRange { FirstIndex: int; Count: int; Total: int }` — `Total` is the value `countVirtual` sums.

## CollectionPosition + AccessibilityMetadata.Collection (public, additive)

- `CollectionPosition { TotalItems: int; FocusedIndex: int option }` — the logical size + position, computed
  from the model, never the realized slice.
- `AccessibilityMetadata.Collection: CollectionPosition option` — `Some` for a virtualized DataGrid, `None`
  for every non-collection control (at-rest a11y byte-identical).

## Relationships

```text
DataGrid model (RowCount, scroll, Overscan) ──visibleRange──▶ VisibleRange { FirstIndex; Count=V+2*overscan (clamped); Total }
        │                                                            │
        │ realized window (only these rows materialized)             │
        ▼                                                            ▼
   countVirtual(lowered tree) ─▶ VirtualMaterialized (<= visible) , VirtualTotal (= sum of Total)
        │                                   │
        ▼                                   ▼
   FrameMetrics.VirtualItemsMaterialized / VirtualItemsTotal   (non-scaling across 100/1000/10000)

   offscreen row select/toggle/focus/relocate ──▶ DataGrid.update (logical only; no materialization)
   AccessibilityMetadata.Collection = Some { TotalItems (logical); FocusedIndex (logical) }   (None for non-collections)
```
</content>
