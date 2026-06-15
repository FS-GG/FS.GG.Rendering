# Contract — Layout Cache / Incremental Re-Measure (Feature 097)

Two surfaces. The **incremental evaluator** is a **public** `FS.GG.UI.Layout` API (already in the committed
surface baseline — zero *new* delta). The **wiring** (dirty set, cache field, metrics) is **internal** to
`Controls`; per the constitution's vertical-slice rule the in-assembly `Controls.Tests` are its
user-reachable surface. Signatures are reproduced from `src/Layout/Layout.fsi` / `src/Layout/Types.fsi` and
`src/Controls/RetainedRender.fsi`; behavior clauses are what the suites assert.

## C1 — `Layout.evaluateIncremental` (the public incremental evaluator)

```fsharp
val evaluateIncremental :
    previous: LayoutResult ->
    changedNodeIds: LayoutNodeId list ->
    available: AvailableSpace ->
    root: LayoutNode ->
        LayoutResult
```

- **Equivalence (INV-1)**: the returned `Bounds` map MUST be **byte-identical** to `Layout.evaluate available
  root` for **any** tree and **any** `changedNodeIds`.
- **Propagation**: each changed node is propagated up to its first **fixed-size ancestor** (content-independent
  size on both axes) and along its flex line; a content-sized chain to the root ⇒ **full re-measure fallback**.
- **Reuse**: only the propagated set is re-measured; `previous.Bounds` is reused for everything else.
- **Honest `Invalidated`**: the returned `Invalidated` is the **actual** re-measured set — a proper **superset**
  of `changedNodeIds`, never a verbatim copy, never the whole tree unless propagation reached the root.
- **Empty input**: `changedNodeIds = []` ⇒ re-measure nothing, `Invalidated = []`, bounds byte-identical to a
  full evaluate.
- **Total + deterministic**: same inputs ⇒ same result; no wall-clock, no randomness; `Revision = previous.Revision + 1`.

*Pins*: FR-001, FR-004, FR-007, FR-008, FR-009. *Used by*: US1, US2, US3 (`Feature097IncrementalTests`,
`Audit_IncrementalLayout`).

## C2 — `LayoutResult` (the cache shape — public)

```fsharp
type LayoutResult =
    { Bounds: ComputedBounds list
      Diagnostics: LayoutDiagnostic list
      Invalidated: LayoutNodeId list
      Revision: int64 }
```

- `Bounds` is the per-`NodeId` reuse payload; `Invalidated` is the honest re-measured set (C1); `Revision`
  advances by 1 per incremental evaluation.

*Pins*: FR-001, FR-002, FR-009.

## C3 — `layoutDirtySet` (the patch-derived dirty set — internal)

```fsharp
val internal layoutDirtySet :
    prev: Control<'msg> -> patch: Reconcile.NodePatch<'msg> -> next: Control<'msg> -> Set<string>
```

- A node is self-dirty iff its `Update` sets/removes an `AttrCategory.Layout` attribute, sets/removes a
  geometry-driving **name** in `ControlInternals.layoutAffectingAttrNames`, or carries a non-`Keep` child op;
  a `Replace` is self-dirty (re-measured fresh).
- A content/style/state/visual-state change is **not** self-dirty.
- Pure walk over `(prev, patch, next)` in the `LayoutNodeId` (`Key |> defaultValue path`) domain; total.

*Pins*: FR-003. *Used by*: US1, US4 (`Feature097WiringTests`).

## C4 — `RetainedRender.Layout` (the carried cache field — internal)

```fsharp
// field on the internal RetainedRender<'msg> record:
Layout: FS.GG.UI.Layout.LayoutResult
```

- Seeded by `init` with a full `Layout.evaluate`; advanced by each `step` to the incremental result (the
  frame-to-frame measure/bounds cache).

*Pins*: FR-002, FR-005.

## C5 — `RemeasuredNodeCount` / `LayoutInvalidatedNodeCount` (the metrics — internal)

```fsharp
// fields on the internal WorkReductionRecord:
RemeasuredNodeCount: int          // post-propagation set actually re-measured (|Invalidated|)
LayoutInvalidatedNodeCount: int   // pre-propagation patch-derived dirty-set size (|layoutDirtySet|)
```

- `0 < RemeasuredNodeCount < BaselineNodeCount` for a localized edit under a fixed-size ancestor.
- `RemeasuredNodeCount = BaselineNodeCount` for a whole-tree relayout (never under-reports — FR-010).
- `RemeasuredNodeCount = 0` and `LayoutInvalidatedNodeCount = 0` for an at-rest frame.
- `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` always.

*Pins*: FR-006, FR-010. *Used by*: US3 (`Feature097WiringTests`).

## C6 — Wired render parity (the vertical-slice proof — internal)

- On `RetainedRender.step`, the rendered `Scene` MUST equal `Control.renderTree theme size next`
  byte-for-byte for localized-edit, geometry-change, child-insert, content-only, and at-rest frames.

*Pins*: FR-005, FR-008. *Used by*: US2, US4 (`Feature097WiringTests`).

## Surface-drift

- **Zero new public-surface-baseline delta** (FR-011): `Layout.evaluateIncremental` / `LayoutResult` were
  already baselined (`tests/surface-baselines/FS.GG.UI.Layout.txt`, type-granular); the wiring (C3/C4/C5)
  is `internal` (absent from `FS.GG.UI.Controls.txt`). The surface-drift check must pass byte-unchanged.
</content>
