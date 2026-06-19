# Contract: Retained Frame Preparation

## Scope

This is an internal framework contract for `src/Controls/RetainedRender.*` and the `Controls.Elmish` retained render path. It is not a public package API and must not introduce a new consumer-visible surface for Feature 174.

## Required Invariants

- The optimized retained path renders the same `Scene` as the existing full render oracle for covered scenarios.
- `Bounds`, `EventBindings`, `BoundIds`, diagnostics, node count, hit testing, and accessibility-facing metadata remain equivalent to the oracle.
- Retained state reuse is guarded by existing invalidation inputs: visual inputs, layout inputs, modifier layers, text proof inputs, explicit identity, child ordering, child insertion/removal, theme, cache boundaries, and unsafe-reuse rules.
- Localized interactions keep metadata/frame-preparation work proportional to changed or required work rather than repeatedly scanning the entire tree at every unchanged parent.
- Any full fallback remains correct, counted, and diagnosable.

## Work-Scaling Rules

For localized model-changing input after initial render:

- `MetadataVisitedNodeCount < BaselineNodeCount`.
- `RecomputedNodeCount`, `RemeasuredNodeCount`, and `RepaintedNodeCount` remain bounded by changed/required work unless the scenario genuinely invalidates the whole tree.
- Unchanged stable chrome and unchanged page regions must not dominate the frame preparation contribution.
- Pages without replay-cache hits still improve when the bottleneck is retained metadata/frame preparation.

For page navigation:

- Destination content may require meaningful work, but unchanged shell chrome and stable retained regions must not be prepared as if every parent had changed.
- The evidence must distinguish frame preparation, paint, and presentation.

## Parity Checks

Implementation tests must compare the optimized retained result with an oracle for:

- `buttons` page button activation.
- Navigation to `text-numeric-input`.
- Dense nested content.
- Theme changes.
- Animations and overlays.
- Scroll viewers and data-rich controls.
- Pages with no replayable/cacheable boundaries.

Parity failure rejects the optimization even when timing improves.

## Reclassification Trigger

If the implementation requires adding, removing, or changing public `.fsi` surface, package contracts, external dependencies, or intentional behavior, stop Tier 2 implementation and reclassify the feature as Tier 1 before proceeding.
