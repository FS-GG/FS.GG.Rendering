# Phase 0 — Research: Layout Cache — Incremental Re-Measure (Feature 097)

This is a **conformance backfill**: the implementation already exists in the imported source, so "research"
here is the recovery and recording of the design decisions the code already embodies. There were **no open
`NEEDS CLARIFICATION`** items in the spec; each decision below is reconstructed from `Layout.fsi`/`Layout.fs`,
`RetainedRender.fs` (`layoutDirtySet` + the `step` wiring), and the three existing suites.

## Decision 1 — Cache the previous frame's full `LayoutResult`, reuse cached bounds per frame

- **Decision**: `RetainedRender` carries the previous frame's full `LayoutResult` frame to frame as the
  per-frame measure/bounds cache (the `Layout` field), seeded by `init` with a full `evaluate` and advanced
  by each `step` to the incremental result. `evaluateIncremental` re-measures only the dirty set and reuses
  the cached `Bounds` for every untouched node.
- **Rationale**: 091 reduced *paint* but still re-measured the whole tree every frame; measure (text
  measurement, flex distribution, bounds assignment) is the other half of per-frame cost and dominates on
  large trees. Reusing the prior frame's bounds for the unchanged majority is the measure-side analog of
  091's partial repaint.
- **Alternatives considered**: A bounded cross-frame LRU keyed by node content — rejected: that is the shape
  of the *picture* cache (116) and *text-measure* cache (117), which are separate features; 097 is the
  per-frame full-result reuse, not a bounded store, and it must be byte-identical to a full evaluate.

## Decision 2 — Derive the dirty set from the reconcile patch, not from a dirty-flag protocol

- **Decision**: `layoutDirtySet` walks `(prev, patch, next)` in parallel and marks a node self-dirty iff its
  `Update` sets/removes an `AttrCategory.Layout` attribute, sets/removes a geometry-driving **name** in
  `ControlInternals.layoutAffectingAttrNames`, or carries a non-`Keep` child op (insert/remove/move); a
  `Replace` is re-measured fresh. The walk is in the `LayoutNodeId` (`Key |> defaultValue path`) domain the
  evaluator uses.
- **Rationale**: The reconciler already computes the exact structural delta; deriving dirtiness from the
  patch reuses that work and avoids a parallel dirty-flag protocol that could drift from the actual diff.
  Keying off the geometry-name set means only geometry-affecting changes dirty measure (precision —
  Decision 3).
- **Alternatives considered**: A per-node "dirty" bit threaded through the control tree — rejected:
  duplicates the reconciler's job, adds mutable state to the pure tree, and risks divergence from the diff.

## Decision 3 — A non-geometry change must not dirty measure (precision)

- **Decision**: A content/text change, a style change, or a visual-state change does **not** enter the dirty
  set — only an `AttrCategory.Layout`/geometry-name attribute change or a child op does. So a hover, a focus,
  or a text edit re-measures **nothing** while still repainting.
- **Rationale**: Without this, the cache would conservatively re-measure on every paint-only change and the
  measure saving would evaporate on the most common interactions. The dirty-set predicate is the precision
  guard that makes the saving real (SC-004).
- **Alternatives considered**: Treating any `Update` as measure-dirty — rejected: correct but useless (it
  re-measures on every frame that paints), defeating the feature.

## Decision 4 — Lock-step with the geometry-name set (`layoutAffectingAttrNames`), guarded separately

- **Decision**: `layoutDirtySet` reads `ControlInternals.layoutAffectingAttrNames` to decide geometry
  dirtiness. That name set is a *separate* hot-path `Set` from the names the layout lowering (`toLayout`)
  actually reads; the two are kept in lock-step by the behavioral-probe equality gate in
  `Feature101LayoutDriftGuardTests`, which fails the build the instant they drift in either direction.
- **Rationale**: A dirty-set predicate that read a different name set than the lowering would either
  under-dirty (stale layout — a real bug) or over-dirty (lost saving). Pinning them with a build-failing
  guard makes the lock-step a checked invariant. 097 **consumes** the set; feature 101 **owns** the guard.
- **Alternatives considered**: Auto-deriving the dirty predicate from `toLayout`'s reads — rejected (this is
  feature 101's recorded decision): the two sets serve different hot paths and are deliberately kept
  explicit, with the drift guard as the safety net.

## Decision 5 — Conservative propagation to the first fixed-size ancestor

- **Decision**: Inside `evaluateIncremental`, each self-dirty node's dirtiness is propagated up to its first
  **fixed-size ancestor** (a container with an explicit, content-independent size on both axes) and along its
  flex line as needed; that boundary's enclosing subtree is reused. If no fixed-size ancestor exists between
  the dirty node and the root (a content-sized chain to the root), propagation reaches the root and the pass
  **falls back to a full re-measure**.
- **Rationale**: A descendant's size change can only affect layout up to the first ancestor whose size is
  content-independent — that boundary absorbs the change. Propagating to it (and no further) is the maximal
  safe reuse. The content-sized-chain fallback is what guarantees correctness in the degenerate case
  (Decision 6).
- **Alternatives considered**: Re-measuring only the literal dirty nodes without propagation — rejected:
  unsound, because a content-sized parent's own box depends on its children's measured sizes, so it must be
  re-measured too.

## Decision 6 — Equivalence (INV-1) is the load-bearing guarantee, proven over generated data

- **Decision**: The incremental result MUST be **byte-identical** to a full `Layout.evaluate` of the same
  frame, for any tree shape and any cumulative edit sequence, at every step — proven by an FsCheck property
  over ≥1000 generated `(tree, edit-sequence)` cases that runs both evaluators and compares bounds maps.
- **Rationale**: A measure cache that can diverge from a full re-measure — even rarely — silently corrupts
  layout and is worse than no cache. Byte-identity over generated data (not canned fixtures) is the only
  honest way to make the partial re-measure safe. The partial path (Decisions 2/5) is *only* sound because
  it is provably equivalent.
- **Alternatives considered**: Spot-checking a few hand-built fixtures — rejected: too weak to trust a cache
  whose whole value proposition is "invisible to the output."

## Decision 7 — Honest re-measure metrics: post-propagation count and pre-propagation count

- **Decision**: `WorkReductionRecord` surfaces `RemeasuredNodeCount` (the post-propagation set actually
  re-measured this frame — `Invalidated`) and `LayoutInvalidatedNodeCount` (the pre-propagation
  patch-derived dirty-set size — `Set.count` of `layoutDirtySet`), with
  `LayoutInvalidatedNodeCount ≤ RemeasuredNodeCount` always. A localized edit yields a strict subset; a
  whole-tree relayout yields exactly the baseline; an idle frame yields 0.
- **Rationale**: The work-reduction metrics are how the cache's value is observed and how regressions are
  caught. Surfacing *both* the pre- and post-propagation counts makes the propagation step itself auditable
  (the gap between them is the propagation cost). The "equals baseline on a whole-tree relayout" clause
  ensures the metric never *under*-claims a genuine full re-measure (FR-010).
- **Alternatives considered**: A single "saved nodes" number — rejected: it hides whether the saving came
  from the dirty-set precision or the propagation boundary, and can't distinguish "nothing changed" from
  "everything changed."

## Decision 8 — Honest `Invalidated`: report the actual re-measured set, a superset of the request

- **Decision**: `evaluateIncremental` reports `Invalidated` as the **actual** set it re-measured — a proper
  **superset** of the requested dirty nodes (it includes the fixed-size-ancestor boundary it climbed to) —
  never a verbatim copy of the request, and never the whole tree unless propagation genuinely reached the
  root.
- **Rationale**: If `Invalidated` just echoed the request, the metric would under-report the real work; if it
  always claimed the whole tree, it would over-report. Reporting the genuine re-measured set is what makes
  `RemeasuredNodeCount` trustworthy (SC-008).
- **Alternatives considered**: Returning the requested ids unchanged — rejected: dishonest (the boundary was
  re-measured but uncounted), and it would make the subset/whole-tree assertions meaningless.

## Decision 9 — Wired byte-identity vs a full `Control.renderTree` rebuild

- **Decision**: On the wired `RetainedRender.step` path, the rendered `Scene` MUST equal a full
  `Control.renderTree` rebuild of the same frame byte-for-byte, for localized-edit, geometry-change,
  child-insert, content-only, and at-rest frames alike — because the reused cached bounds equal a full
  re-measure (Decision 6) and the paint walk reads those bounds.
- **Rationale**: Equivalence at the evaluator level (Decision 6) is necessary but not sufficient — the proof
  must extend to the actual wired render the user sees. `Feature097WiringTests` closes that gap by comparing
  the wired scene to the full-rebuild oracle on every change shape.
- **Alternatives considered**: Trusting evaluator equivalence alone — rejected: the wiring (dirty-set
  derivation, the `step` call, the paint walk reading cached bounds) is its own surface that can regress
  independently of the evaluator.

## Decision 10 — Test split: pure evaluator (Layout.Tests) vs wired path (Controls.Tests)

- **Decision**: Pure-evaluator equivalence, the boundary subset, the at-rest case, and honest `Invalidated`
  live in `Layout.Tests` (`Feature097IncrementalTests` + `Audit_IncrementalLayout`) against the public
  package; the wired metric honesty, dirty-set precision, and byte-identity vs full rebuild live in
  `Controls.Tests/Feature097WiringTests`, reaching the internal wiring via `InternalsVisibleTo`.
- **Rationale**: The evaluator is a public `FS.GG.UI.Layout` API and its properties belong with the package;
  the wiring (`layoutDirtySet`, the `step` call, the metrics) is internal to `Controls` and its properties
  belong there. The split keeps each suite's "user-reachable surface" honest.
- **Alternatives considered**: One combined suite — rejected: it would conflate the public-package contract
  with the internal-wiring contract and blur which assembly owns which proof.

## Renderer-mode / evidence honesty

The wired byte-identity proofs use **structural scene equality** (the authoritative parity proof) and the
evaluator proofs use **bounds-map equality** (`NodeId → ComputedBounds`); both are exercised over **generated
data** (no canned fixtures) for the equivalence invariant. The readiness evidence (authored in
`/speckit-implement`, since 097 imported without it) is captured deterministically and does **not** claim
pixel-level or desktop-visibility proof — consistent with the 091/092/099 backfills.
</content>
