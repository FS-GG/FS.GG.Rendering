# Research: Intrinsic Layout Protocol

## Decision: Treat Feature 150 as P8/R3b intrinsic layout

**Rationale**: The radical rendering roadmap records P8 as the remaining unimplemented item after
P0-P7. Its explicit exit criteria are a constraints/intrinsics protocol, ScrollViewer
reimplementation, and preserved incremental/full parity. The feature spec scopes exactly that work
and excludes new compositor, overlay, browser, text-shaping, and solver behavior.

**Alternatives considered**:

- Revisit earlier R3a flex-attribute work: rejected because Feature 138 already shipped it.
- Fold in a relational constraint solver: rejected because the roadmap keeps Yoga/flex as the
  default predictable model and treats solver containers as a separate specialized escape hatch.
- Re-open P7 compositor acceptance: rejected because P8 must not claim new compositor behavior.

## Decision: Extend the existing Layout package contract

**Rationale**: `src/Layout` already owns public `LayoutNode`, `LayoutIntent`, `MeasureRequest`,
`MeasureResponse`, `AvailableSpace`, `LayoutResult`, diagnostics, rendering of computed bounds, and
incremental workflow hooks. Extending this package keeps public layout concepts in one place,
preserves the current package dependency direction, and lets Controls consume an official contract
instead of inspecting rendered descendants.

**Alternatives considered**:

- Add a separate intrinsic package: rejected because it would split one layout contract into two
  surfaces and complicate cache invalidation.
- Put intrinsic queries in Controls only: rejected because ScrollViewer would still be the only
  special case and custom containers would not receive a reusable layout capability.
- Replace the current evaluator wholesale: rejected because compatibility evidence needs additive
  evolution of existing Yoga-backed behavior.

## Decision: Use record/function contracts, not an object-first layout interface

**Rationale**: The roadmap describes an `ILayout`-style protocol conceptually, but the repository
constitution prefers plain F# records and functions unless a hierarchy is clearly needed. Records for
constraints, measure input, child placement, intrinsic query, cache key, cache entry, and
diagnostics give FSI users an inspectable contract and keep participant callbacks easy to test.

**Alternatives considered**:

- A public OO `ILayout` interface: rejected because it would introduce hierarchy and dispatch before
  the implementation proves it is necessary.
- Reuse only the existing `ContentMeasure` callback: rejected because it cannot express child
  placement, intrinsic-axis queries, or cache dependency evidence.
- Keep all new types internal: rejected because this is a Tier 1 public layout contract change.

## Decision: Keep Yoga as the default flex implementation behind the protocol

**Rationale**: Existing `LayoutIntent` already maps flex direction, wrap, align, justify, padding,
margin, gap, fixed/min/max sizes, grow, shrink, and basis into Yoga. The intrinsic protocol should
wrap the evaluator, not replace it. This preserves existing behavior for default layouts and keeps
the low-level Layout package dependency boundary limited to Scene and Yoga.Net.

**Alternatives considered**:

- Implement a new general solver now: rejected by FR-018 and the roadmap's predictability/O(n)
  rationale.
- Hand-roll flex layout in Controls: rejected because Layout already owns the Yoga-backed
  implementation and public tests.
- Move Yoga usage into Controls: rejected because it inverts the package boundary.

## Decision: Make intrinsic queries explicit and cache-keyed

**Rationale**: P8 exists because ScrollViewer currently derives content height by walking computed
descendant bounds after Yoga clamps the content to the viewport. Intrinsics are the sanctioned
second size-discovery path: min/max natural width/height under explicit constraints, tied to the
same content identity, layout-affecting inputs, constraint normalization, and participant version
used by normal measurement.

**Alternatives considered**:

- Measure children repeatedly with different constraints during normal layout: rejected because it
  violates the single-measure-per-pass invariant and hides work.
- Infer natural extent from final descendant bounds: rejected because this is the smell P8 removes.
- Cache intrinsic results only by node id: rejected because content, constraints, and layout inputs
  can change without changing identity.

## Decision: Rework ScrollViewer around layout-provided content extent

**Rationale**: ScrollViewer is the reference consumer that proves the protocol works. It must keep
viewport bounds fixed while reporting extent from child natural size. Smaller and exact-fit content
normalizes extent to the viewport; overflowing content reports full natural size; changed child
intrinsics update scroll range without changing unrelated surrounding layout.

**Alternatives considered**:

- Keep `Control.scrollViewport` descendant walking as the source of truth: rejected because it is the
  known P8 defect.
- Use widget-specific row-count heuristics: rejected because custom content, text, images, and nested
  containers need the same official path.
- Clamp all content to the viewport and report no overflow: rejected because it breaks scroll ranges.

## Decision: Preserve full/incremental parity through dependency-keyed cache reuse

**Rationale**: Existing Feature097 and Feature138 tests pin incremental layout parity. P8 adds new
cacheable data, so reuse must depend on normalized constraints, layout-affecting attributes,
content/measure identity, intrinsic query inputs, child order, visibility, and participant version.
Unmatched keys force recomputation; stale entries produce diagnostics or are ignored before output.

**Alternatives considered**:

- Reuse measured bounds when only node id matches: rejected because it accepts stale layout.
- Disable incremental caching for all intrinsic layouts: rejected because the feature explicitly
  includes deterministic layout caching and invalidation evidence.
- Treat intrinsic cache misses as errors: rejected because misses are expected after valid input
  changes; only unsupported or contradictory intrinsic results are diagnostic failures/degradations.

## Decision: Publish bounded readiness evidence

**Rationale**: This is a Tier 1 layout change. Reviewers and package consumers need one summary that
links to compatibility, intrinsic sizing, ScrollViewer, cache/invalidation, diagnostics, and
full/incremental parity evidence. Surface baselines and compatibility notes are required for any
public `.fsi` change.

**Alternatives considered**:

- Rely only on unit tests: rejected because readiness must explain public impact and limitations.
- Publish readiness after merge only: rejected because tasks and review need concrete artifacts to
  target.
- Count synthetic-only evidence as accepted: rejected by the constitution's real-evidence bias.
