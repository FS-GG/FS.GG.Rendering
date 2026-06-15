# Phase 1 — Data Model: Layout Cache — Incremental Re-Measure (Feature 097)

The 097-in-scope entities. The **evaluator + result types** (`LayoutResult`, `ComputedBounds`,
`AvailableSpace`) are **public** in `FS.GG.UI.Layout` (already in the committed surface baseline); the
**wiring** entities (the `RetainedRender.Layout` cache field, `layoutDirtySet`, and the two re-measure
metrics) are **assembly-internal**. Equality is F# **structural** equality throughout; the evaluator is
pure and total (no wall-clock).

## LayoutResult (the per-frame measure/bounds cache)

The previous frame's full layout, carried frame-to-frame as `RetainedRender.Layout` and reused by
`evaluateIncremental`. Public type (`FS.GG.UI.Layout.Types`).

| Field | Type | Meaning |
|---|---|---|
| `Bounds` | `ComputedBounds list` | Per-`NodeId` computed bounds. The reuse payload — `evaluateIncremental` keeps these for every non-re-measured node. |
| `Diagnostics` | `LayoutDiagnostic list` | Layout diagnostics for the frame. |
| `Invalidated` | `LayoutNodeId list` | The **actual** set re-measured to produce this result (FR-001). A proper **superset** of the requested dirty nodes (includes the fixed-size-ancestor boundary); empty for an at-rest frame; the whole tree only when propagation reached the root. |
| `Revision` | `int64` | Advances by exactly **1** per `evaluateIncremental` (FR-009 determinism marker). |

**Invariants**:
- `boundsMap(evaluateIncremental prev dirty avail frame) = boundsMap(evaluate avail frame)` — **byte-identical**
  for any tree and any dirty set (INV-1 / FR-007).
- `Invalidated` ⊇ the requested dirty nodes, and `|Invalidated| ≤ |all nodes|` (FR-001 / SC-008).
- Empty dirty set ⇒ `Invalidated = []` and `Bounds` reused wholesale (FR-008 / SC-006).

## ComputedBounds (the reuse unit)

| Field | Type | Meaning |
|---|---|---|
| `NodeId` | `LayoutNodeId` | The node's layout id (`Key |> defaultValue path`). |
| `Bounds` | `LayoutBounds` | The measured box. Reused verbatim across frames for an unchanged node. |
| `Visibility` | `LayoutVisibility` | The node's layout visibility. |

## layoutDirtySet (the patch-derived dirty set — internal)

The input to `evaluateIncremental`: a `Set<LayoutNodeId>` of **self-dirty** nodes (pre-propagation), computed
by a pure walk over `(prev, patch, next)`. Internal to `Controls` (`val internal`).

**Dirty predicate** (a node is self-dirty iff any holds):
- its `Update` **sets** an attr whose `Category = AttrCategory.Layout` **or** whose `Name` is in
  `ControlInternals.layoutAffectingAttrNames`;
- its `Update` **removes** an attr that was geometry-driving (name in the set, or `Layout` category recovered
  from the **prev** node's attribute);
- its `Update` carries a **non-`Keep` child op** (`ChildInsert` / `ChildRemove` / `ChildMove`);
- it is a `Replace` (re-measured fresh under its boundary).

A `Keep`, and an `Update` that changes only content/style/state/visual-state, is **not** self-dirty (FR-003 /
SC-004).

## evaluateIncremental (the incremental evaluator — public)

`previous: LayoutResult -> changedNodeIds: LayoutNodeId list -> available: AvailableSpace -> root: LayoutNode -> LayoutResult`.
Public (`FS.GG.UI.Layout.Layout`).

**Behavior**:
- **Propagate** each changed node up to its first **fixed-size ancestor** (content-independent size on both
  axes) and along its flex line (FR-004); a content-sized chain to the root ⇒ full re-measure fallback.
- **Re-measure** only the propagated set; **reuse** `previous.Bounds` for everything else (FR-005).
- **Report** the actual re-measured set as `Invalidated` (FR-001) and advance `Revision` by 1 (FR-009).
- **Total + deterministic**: same inputs ⇒ same `Bounds` and `Invalidated`; no wall-clock, no randomness.

## RetainedRender.Layout (the carried cache field — internal)

The `LayoutResult` field on the retained render record, seeded by `init` (full `evaluate`) and advanced by
each `step` to the incremental result. The frame-to-frame carrier of the measure cache (FR-002).

## WorkReductionRecord — the 097 metrics (internal)

The per-frame work record (091/092) gains two measure-side fields:

| Field | Type | Meaning |
|---|---|---|
| `RemeasuredNodeCount` | `int` | **Post-propagation** set actually re-measured (`Set.count` / `Invalidated`). Strict subset of `BaselineNodeCount` for a localized edit; **equals** `BaselineNodeCount` for a whole-tree relayout (never under-reports — FR-010); `0` for an at-rest frame (FR-006 / SC-003). |
| `LayoutInvalidatedNodeCount` | `int` | **Pre-propagation** patch-derived dirty-set size (`Set.count` of `layoutDirtySet`). Always `≤ RemeasuredNodeCount` (propagation only expands the set); `0` on an idle / style-only / visual-state-only frame. |

`BaselineNodeCount` (existing) is what a full rebuild re-measures (== node count) — the comparison anchor.

## layoutAffectingAttrNames (consumed, owned by feature 101)

The geometry-driving attribute-name `Set` that the dirty predicate reads. Owned and drift-guarded by feature
101 (`Feature101LayoutDriftGuardTests`); 097 reads it and must stay in lock-step, but does not modify it.

## Relationships

```text
Reconcile.diff(prev, next) ──patch──▶ layoutDirtySet(prev, patch, next) ──Set<LayoutNodeId>──┐
                                              │ (reads layoutAffectingAttrNames, feature 101)  │
RetainedRender.Layout (prev LayoutResult) ────┼───────────────────────────────────────────────┤
                                              ▼                                                ▼
                                  Layout.evaluateIncremental(prev, dirty, avail, next)
                                              │  propagate→fixed-size ancestor; re-measure dirty; reuse cached Bounds
                                              ▼
                                  LayoutResult { Bounds; Invalidated; Revision }  ≡ full evaluate (INV-1)
                                              │
                  ┌───────────────────────────┼──────────────────────────────┐
                  ▼                            ▼                              ▼
   RemeasuredNodeCount = |Invalidated|   LayoutInvalidatedNodeCount = |dirty|   paint walk reads Bounds
   (post-propagation, ≤ baseline)        (pre-propagation, ≤ Remeasured)        ⇒ Scene ≡ Control.renderTree
```
</content>
