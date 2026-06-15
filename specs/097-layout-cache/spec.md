# Feature Specification: Layout Cache — Incremental Re-Measure (Feature 097)

**Feature Branch**: `097-layout-cache`

**Created**: 2026-06-15

**Status**: Draft

**Input**: User description: "next item in the implementation plan"

## Context

This is a **conformance-backfill** specification — task **C2** in the 2026-06-15 missing-features plan,
continuing the Workstream C pattern that feature 091 established and features 092 / 093 / 095 / 096 / 099
have followed.

Feature 091 wired the keyed reconciler onto the render path so that a `RetainedRender.step` recomputes
only the **changed paint subtree** instead of rebuilding the whole scene every frame. But 091's
work-reduction story covered **paint**, not **measure**: every `step` still ran a *full* layout pass over
the whole tree (`Layout.evaluate`), so a one-pixel width change to a single leaf re-measured every node on
screen. Layout — text measurement, flex distribution, bounds assignment — is the other half of per-frame
cost, and on a large tree it dominates.

Feature 097 (the "R2" accretion) **adds the measure-side cache**: it carries the previous frame's full
`LayoutResult` frame to frame as a per-frame **measure/bounds cache**, derives a **layout-dirty set** from
the reconcile patch (`layoutDirtySet`), and feeds both into an **incremental layout evaluator**
(`Layout.evaluateIncremental`) that re-measures only the dirty set — conservatively propagated up to each
dirty node's first **fixed-size ancestor** (a content-independent boundary that absorbs the change) — and
**reuses the cached bounds for everything else**. The decisive correctness guarantee is **equivalence**:
the incremental result is **byte-identical** to a full `Layout.evaluate` of the same frame (INV-1), so the
cache is invisible to the rendered output — it only changes *how much work* was done, never *what was
produced*. The work actually saved is surfaced honestly through two new frame metrics:
`RemeasuredNodeCount` (the post-propagation set actually re-measured) and `LayoutInvalidatedNodeCount`
(the pre-propagation patch-derived dirty-set size).

The implementation (`layoutDirtySet` + the `step` wiring in `src/Controls/RetainedRender.fs`, the
`Layout.evaluateIncremental` evaluator and its `Invalidated` report in `FS.GG.UI.Layout`), the accreted
`RetainedRender.fsi` surface (the `RetainedRender.Layout` cache field, and the `RemeasuredNodeCount` /
`LayoutInvalidatedNodeCount` fields on `WorkReductionRecord`), and the executable suites —
`Feature097IncrementalTests` and `Audit_IncrementalLayout` in `Layout.Tests` (the pure incremental
evaluator + its equivalence invariant) and `Feature097WiringTests` in `Controls.Tests` (the live wired
`step` path) — **already exist** in the imported, rebranded source. **No Spec Kit spec/plan/tasks have
ever described this work** (and, unlike 092/099, it had **no `readiness/` evidence** either). This
document backfills the contract so the capability is governed by `Spec → .fsi → semantic tests →
implementation` like any other feature.

The surface splits two ways. The **incremental evaluator itself** (`Layout.evaluate`,
`Layout.evaluateIncremental`, and the `LayoutResult` shape with its `Invalidated` / `Revision` fields) is
**public** in the `FS.GG.UI.Layout` package — but it was **already part of the committed surface baseline**
when this code was imported, so this backfill adds **zero *new* public-surface-baseline delta** (the
surface baseline is type-granular; `LayoutResult` / `ComputedBounds` already appear, and the public
`Layout` module functions hang off already-baselined types). The **wiring** surface — `layoutDirtySet`,
the `RetainedRender.Layout` cache field, and the `RemeasuredNodeCount` / `LayoutInvalidatedNodeCount`
metrics on `WorkReductionRecord` — is **assembly-internal**, reached by the `Controls.Elmish` adapter and
the harness via `InternalsVisibleTo`. Per the constitution's vertical-slice rule, the in-assembly
Expecto/FsCheck tests **are** the user-reachable surface for the internal wiring user stories, and the
public-package `Layout.Tests` exercise the public evaluator directly.

**Scope boundary.** Feature 097 owns the **measure/bounds cache and the incremental measure pass**. It
does **not** own:

- the **paint-side** work reduction (the reconciler-driven partial repaint) — that is feature **091**;
- the **picture cache** (cross-frame cached paint) — feature **116** — and the **text-measure cache** —
  feature **117** — which are *separate* LRU caches layered on top of the same `step` (097 is the
  per-frame bounds reuse, not a bounded cross-frame measurement store);
- the **lock-step name set** `ControlInternals.layoutAffectingAttrNames` that `layoutDirtySet` reads to
  decide geometry-dirtiness — that set's drift guard is feature **101**, proven separately by
  `Feature101LayoutDriftGuardTests`. 097 *consumes* the name set; it does not own it.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - A localized geometry change re-measures only its boundary subtree (Priority: P1)

When a single control's geometry changes — e.g. one leaf's width — and that leaf sits under a
**fixed-size ancestor** (a container with an explicit, content-independent size on both axes), the
incremental layout pass re-measures **only** that boundary subtree and **reuses the cached bounds** for
the root and every unrelated sibling. The re-measured set is a **strict subset** of the whole tree, yet
the produced bounds are **byte-identical** to a full re-measure of the frame.

**Why this priority**: This is the headline payoff of 097 over 091 — 091 reduced *paint* but still
re-measured the whole tree every frame. Re-measuring only the changed boundary subtree is the MVP slice
and the whole point of the feature: the measure-side analog of 091's partial repaint.

**Independent Test**: Build a tree `root(stack) → [ panel(fixed 200×100) → [leafA, leafB] ; sibling ]`,
seed the cache with frame 0, then change only `leafA`'s width. Confirm `evaluateIncremental` (carrying the
prior result) re-measures `leafA` and the `panel` boundary, does **not** re-measure the root or the
sibling (`Invalidated` excludes them), and produces a bounds map **byte-identical** to a full
`Layout.evaluate` of the new frame. On the wired `RetainedRender.step` path, `RemeasuredNodeCount` is
strictly less than `BaselineNodeCount` and strictly greater than 0.

**Acceptance Scenarios**:

1. **Given** a frame with a fixed-size boundary `panel` containing `leafA`/`leafB` and an unrelated
   `sibling`, **When** only `leafA`'s size changes and the next frame is evaluated incrementally carrying
   the prior result, **Then** the `Invalidated` set contains `leafA` and `panel` but **not** the root or
   `sibling`, and the bounds map equals a full `Layout.evaluate` byte-for-byte.
2. **Given** the same change on the wired `RetainedRender.step` path, **When** the frame is stepped,
   **Then** `RemeasuredNodeCount` is strictly less than `BaselineNodeCount` and strictly greater than 0,
   and the rendered `Scene` equals a full `Control.renderTree` rebuild byte-for-byte.

---

### User Story 2 - The incremental result is always byte-identical to a full re-measure (Priority: P1)

Across **any** tree shape and **any** sequence of cumulative geometry edits, the incremental evaluator
carrying its own previous result forward produces bounds **byte-identical** to a full `Layout.evaluate` of
the same frame — at **every** step of the sequence. The cache never produces a stale, drifted, or
divergent layout; it is invisible to the output by construction.

**Why this priority**: Equivalence (INV-1) is the load-bearing correctness guarantee of the whole feature.
A measure cache that can diverge from a full re-measure — even rarely — is worse than no cache, because it
silently corrupts layout. Proving byte-identity over generated trees and edit sequences is co-critical
with US1: the partial re-measure (US1) is only safe *because* it is provably equivalent (US2).

**Independent Test**: Over ≥1000 FsCheck-generated `(tree, edit-sequence)` cases, seed the cache with a
full evaluate of frame 0, then apply each cumulative size edit through **both** the incremental evaluator
(carrying its result forward as the cache) and a full `evaluate`, and assert the two bounds maps are equal
at every step. The first divergence (if any) fails the property with a diagnostic.

**Acceptance Scenarios**:

1. **Given** a randomly generated tree and a cumulative sequence of size edits, **When** each edit is
   applied through both the incremental evaluator (carrying its cache) and a full evaluate, **Then** the
   two bounds maps are byte-identical at every step, across ≥1000 generated cases.
2. **Given** the wired `step` path, **When** a localized geometry edit, a child insert, and a
   content-only change are each applied, **Then** the rendered `Scene` equals a full `Control.renderTree`
   rebuild byte-for-byte in every case.

---

### User Story 3 - The re-measure metric is honest — never under- or over-claims work saved (Priority: P2)

The frame's `RemeasuredNodeCount` reports the **actual** set re-measured, neither inflating nor deflating
the saving. For a localized edit under a fixed-size ancestor it is a **strict subset** of the tree; for a
genuine whole-tree relayout (a geometry change with a **content-sized chain to the root**, where no
fixed-size ancestor can absorb it) it **equals** the baseline; for an empty patch it is **0**. The
companion `LayoutInvalidatedNodeCount` reports the **pre-propagation** patch-derived dirty-set size, and
is always `≤ RemeasuredNodeCount` (propagation only ever expands the set).

**Why this priority**: The work-reduction metrics are how 097's value is observed and how regressions are
caught. A metric that claims a reduction that did not happen (or hides one that did) makes the cache
unauditable. P2 because it protects and reports the core journeys (US1/US2) rather than being a separate
user-facing capability, and it is proven by the same suites.

**Independent Test**: On the wired `step` path, assert: a localized geometry edit yields
`0 < RemeasuredNodeCount < BaselineNodeCount`; a root-orientation change (content-sized chain) yields
`RemeasuredNodeCount = BaselineNodeCount`; an at-rest (identical) frame yields `RemeasuredNodeCount = 0`.
Over generated trees, whenever the partial path is taken, the re-measured set is a strict subset of all
node ids and always contains the requested node. `LayoutInvalidatedNodeCount` is `≤ RemeasuredNodeCount`.

**Acceptance Scenarios**:

1. **Given** a localized geometry edit under a fixed-size ancestor, **When** the frame is stepped,
   **Then** `0 < RemeasuredNodeCount < BaselineNodeCount`.
2. **Given** a root-level geometry change with a content-sized chain to the root, **When** the frame is
   stepped, **Then** `RemeasuredNodeCount = BaselineNodeCount` (the metric never under-reports a genuine
   whole-tree relayout).
3. **Given** an at-rest frame (identical tree), **When** it is stepped, **Then** `RemeasuredNodeCount = 0`
   and `LayoutInvalidatedNodeCount = 0`.
4. **Given** any frame, **When** it is stepped, **Then** `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount`
   (the pre-propagation dirty set is a subset of the re-measured boundary subtrees).

---

### User Story 4 - A non-geometry change does not dirty measure (Priority: P2)

A change that does not affect geometry — a content/text change, a style change, a visual-state change —
**does not** enter the layout-dirty set, so the frame re-measures **nothing** (`RemeasuredNodeCount = 0`)
while still repainting as needed and staying byte-identical to a full rebuild. Only a change to a
geometry-driving attribute (a name in `ControlInternals.layoutAffectingAttrNames` or an
`AttrCategory.Layout`-tagged attribute) or a non-`Keep` child op (insert / remove / move) dirties measure.

**Why this priority**: This is the precision guard on the dirty-set derivation — without it the cache would
conservatively re-measure on every paint-only change and the measure saving would evaporate on common
interactions (hover, focus, text edit). P2 because it sharpens the core journeys rather than adding a new
one, and is proven by the same wired suite.

**Independent Test**: On the wired `step` path, change only a leaf's `Content` (no geometry attr, no child
op) and assert `RemeasuredNodeCount = 0` while the rendered `Scene` still equals a full rebuild. Separately,
a child **insert** under the boundary yields `RemeasuredNodeCount > 0` (a child op *does* dirty its
container) and stays byte-identical.

**Acceptance Scenarios**:

1. **Given** a frame whose only change is a leaf's text content, **When** it is stepped, **Then**
   `RemeasuredNodeCount = 0` and the rendered `Scene` equals a full `Control.renderTree` rebuild.
2. **Given** a frame that inserts a child under the fixed-size boundary, **When** it is stepped, **Then**
   `RemeasuredNodeCount > 0` (the child op dirties its container) and the rendered `Scene` is
   byte-identical to a full rebuild.

---

### Edge Cases

- **Empty dirty set (at rest)**: `evaluateIncremental` with an empty dirty list re-measures **nothing**,
  reuses every cached bound, and returns bounds byte-identical to a full evaluate; the wired
  `RemeasuredNodeCount` is 0.
- **Content-sized chain to the root** (no fixed-size ancestor between the changed node and the root):
  propagation reaches the root, so the incremental pass **falls back to a full re-measure** — still
  byte-identical, with `RemeasuredNodeCount = BaselineNodeCount`. Degenerate-correct, never wrong.
- **`Replace` patch** (a `Kind`/`Key` change): the replaced subtree is re-measured **fresh** under its
  boundary (it is unconditionally added to the dirty set).
- **Attribute removal**: removing a geometry-driving attribute dirties measure, with the category
  recovered from the **previous** node's attribute (so a removed `AttrCategory.Layout` attr still dirties).
- **Honest `Invalidated`**: the evaluator's reported `Invalidated` set is the **actual** re-measured set —
  a proper superset of the requested dirty nodes (it includes the fixed-size-ancestor boundary it climbed
  to), never a copy of the request and never the whole tree unless propagation genuinely reached the root.
- **First frame**: `init` seeds the cache with a full `Layout.evaluate`; the first `step` measures against
  that seed (there is no partial saving to claim on a cold cache beyond what the patch dirties).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The incremental evaluator MUST report an **honest `Invalidated`** set — the **actual** set of
  nodes it re-measured this frame — which MUST be a proper **superset** of the requested dirty nodes
  (including the fixed-size-ancestor boundary it propagated to) and MUST NOT be a verbatim copy of the
  request nor the whole tree unless propagation genuinely reached the root.
- **FR-002**: `RetainedRender` MUST carry the previous frame's full `LayoutResult` frame to frame as the
  per-frame **measure/bounds cache** (the `Layout` field), seeded by `init` with a full `evaluate` and
  advanced by each `step` to the incremental result.
- **FR-003**: The **layout-dirty set** MUST be derived from the reconcile patch (`layoutDirtySet`), in the
  `LayoutNodeId` domain the evaluator uses. A node MUST be self-dirty iff its `Update` sets/removes an
  `AttrCategory.Layout` attribute, sets/removes a geometry-driving **name** in
  `ControlInternals.layoutAffectingAttrNames`, OR carries a non-`Keep` child op
  (insert/remove/move); a `Replace` MUST be re-measured fresh. A content/style/state/visual-state change
  MUST NOT dirty measure.
- **FR-004**: The dirty set MUST be conservatively **propagated** inside `evaluateIncremental` up to each
  dirty node's first **fixed-size ancestor** (and along its flex line as needed), so a content-independent
  boundary absorbs the change and its enclosing subtree's bounds are reused.
- **FR-005**: `step` MUST run the **incremental** evaluator (`evaluateIncremental`), re-measuring only the
  propagated dirty set and **reusing the previous frame's cached bounds** for everything else — replacing
  the full-tree `evaluate` per frame.
- **FR-006**: `WorkReductionRecord` MUST surface `RemeasuredNodeCount` — the **post-propagation** set
  actually re-measured this frame (`Invalidated`) — and `LayoutInvalidatedNodeCount` — the
  **pre-propagation** patch-derived dirty-set size (`Set.count` of `layoutDirtySet`) — with
  `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` always.
- **FR-007**: The incremental result MUST be **byte-identical** to a full `Layout.evaluate` of the same
  frame (INV-1 equivalence) for **any** tree shape and **any** cumulative edit sequence, at every step.
- **FR-008**: An **empty** dirty set MUST re-measure **nothing** and reuse every cached bound (the at-rest
  frame is byte-identical to a full evaluate with `RemeasuredNodeCount = 0`); the **wired** render output
  MUST stay byte-identical to a full `Control.renderTree` rebuild for localized, geometry-changing,
  child-op, content-only, and at-rest frames alike.
- **FR-009**: The evaluator MUST be **total** and **deterministic** (no wall-clock, no randomness): the
  same `(previous result, dirty set, frame)` MUST always produce the same bounds and the same `Invalidated`
  set; `Revision` advances by exactly 1 per incremental evaluation.
- **FR-010**: A genuine whole-tree relayout (a geometry change whose propagation reaches the root) MUST be
  reported **without under-claiming**: `RemeasuredNodeCount` MUST equal `BaselineNodeCount` in that case.
- **FR-011**: The backfill MUST add **zero new public-surface-baseline delta**. The public
  `Layout.evaluate` / `Layout.evaluateIncremental` evaluator and the `LayoutResult` shape were already in
  the committed baseline; the **wiring** surface (`layoutDirtySet`, the `RetainedRender.Layout` cache
  field, and the `RemeasuredNodeCount` / `LayoutInvalidatedNodeCount` metrics) MUST stay **internal**.
  (Verified directly by the surface-drift check; this requirement has no separate Success Criterion.)

### Key Entities *(include if feature involves data)*

- **LayoutResult (the cache)**: the previous frame's full layout — its `Bounds` (per-`NodeId`
  `ComputedBounds`), `Invalidated` (the set re-measured to produce it), and `Revision`. Carried as
  `RetainedRender.Layout` frame to frame; the per-frame measure/bounds cache that `evaluateIncremental`
  reuses.
- **layoutDirtySet**: the patch-derived set of self-dirty `LayoutNodeId`s (pre-propagation) — the input to
  the incremental evaluator, computed by a pure walk over `(prev, patch, next)`.
- **evaluateIncremental**: the incremental layout evaluator — takes the previous result + the dirty set,
  propagates to fixed-size ancestors, re-measures only that set, reuses cached bounds elsewhere, and
  reports the actual re-measured set as `Invalidated`. Byte-identical to `evaluate` (INV-1).
- **fixed-size ancestor (boundary)**: a container with an explicit, content-independent size on both axes;
  it absorbs a descendant's geometry change so the change does not propagate past it. The unit of reuse.
- **RemeasuredNodeCount**: the post-propagation re-measured count surfaced on `WorkReductionRecord` — the
  honest partial-measure metric (strict subset localized; baseline whole-tree; 0 idle).
- **LayoutInvalidatedNodeCount**: the pre-propagation dirty-set size on `WorkReductionRecord`; always
  `≤ RemeasuredNodeCount`.
- **layoutAffectingAttrNames**: the geometry-driving attribute-name set (owned by feature 101) that
  `layoutDirtySet` reads to decide geometry-dirtiness; consumed here, not owned here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A localized geometry edit under a fixed-size ancestor re-measures a **strict subset** of the
  tree — the requested node and its boundary are in `Invalidated`, the root and unrelated siblings are not
  — while the produced bounds equal a full `Layout.evaluate` byte-for-byte, in 100% of cases.
- **SC-002**: The incremental evaluator (carrying its result forward) produces bounds byte-identical to a
  full `evaluate` at **every** step, across **≥1000** FsCheck-generated `(tree, edit-sequence)` cases.
- **SC-003**: `RemeasuredNodeCount` is honest in 100% of cases: `0 < count < BaselineNodeCount` for a
  localized edit, `count = BaselineNodeCount` for a whole-tree relayout, and `count = 0` for an at-rest
  frame — never under- or over-reporting the actual re-measure.
- **SC-004**: A content/style/state/visual-state change re-measures **nothing** (`RemeasuredNodeCount = 0`)
  while still rendering byte-identical to a full rebuild; a content-sized chain to the root correctly falls
  back to a full re-measure — in 100% of cases.
- **SC-005**: The **wired** `RetainedRender.step` render output is byte-identical to a full
  `Control.renderTree` rebuild for localized-edit, geometry-change, child-insert, content-only, and at-rest
  frames, in 100% of cases.
- **SC-006**: An empty dirty set re-measures nothing and reuses every cached bound (`Invalidated` empty,
  bounds byte-identical to a full evaluate) in 100% of cases.
- **SC-008**: The evaluator's reported `Invalidated` is a proper **superset** of the requested dirty nodes
  (the actual re-measured set, never just the request), in 100% of cases.

## Assumptions

- The keyed reconciler producing the patch (`Reconcile.diff` / `NodePatch`), the retained render structure
  and `RetainedRender.step` (feature 091), the `Layout` module's full `evaluate` and `LayoutNode`/`LayoutResult`
  model, and the geometry-driving name set `ControlInternals.layoutAffectingAttrNames` (feature 101) already
  exist in the imported source. 097 is the **backfilled contract** for *the incremental measure pass and its
  bounds cache* (derive the dirty set, evaluate incrementally, reuse cached bounds, surface the honest
  metrics), not new-from-scratch construction.
- The **wiring** surface stays internal; "users" of those stories are framework internals plus the
  in-assembly tests (per the constitution's vertical-slice rule). The **incremental evaluator** is a
  public `FS.GG.UI.Layout` API exercised directly by the public-package `Layout.Tests`. No *new* public
  API is added by this backfill — the evaluator was already in the committed surface baseline.
- Feature 097 owns the **per-frame measure/bounds cache and incremental re-measure**. The **paint-side**
  partial repaint is feature **091**; the bounded cross-frame **picture cache** is feature **116** and the
  **text-measure cache** is feature **117** (separate LRU stores on the same `step`); the **lock-step name
  set** drift guard is feature **101**. Those share surface in the same `RetainedRender.fsi` / `step` but
  are out of scope for 097 and proven by their own features.
- Render-output equivalence is judged by **structural scene equality** and **bounds-map equality** (the
  authoritative parity proofs); the incremental evaluator's byte-identity to a full `evaluate` (INV-1) is
  the core guarantee, exercised over generated data (no canned fixtures).
- Unlike 092/099, feature 097 had **no `readiness/` evidence** in the imported source — the plan (C2)
  calls for authoring readiness alongside the spec, drawing on the existing `Feature097IncrementalTests` /
  `Audit_IncrementalLayout` (Layout.Tests) and `Feature097WiringTests` (Controls.Tests) suites.
- This is the **C2** conformance backfill in the 2026-06-15 missing-features plan, following the 091
  pattern and the 092 / 093 / 095 / 096 / 099 closes; `/speckit-plan`, `/speckit-tasks`, and
  `/speckit-implement` reduce to a conformance pass (confirm the suites are green, author/regenerate the
  readiness evidence, and the surface delta is zero), not a build.
</content>
</invoke>
