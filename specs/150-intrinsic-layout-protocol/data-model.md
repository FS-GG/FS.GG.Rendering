# Data Model: Intrinsic Layout Protocol

## Layout Participant

**Purpose**: A control, container, or custom layout-capable item that can be measured, queried for
intrinsics, and placed under explicit constraints.

**Fields**:

- `ParticipantId`: stable layout node/control id.
- `Kind`: control/container/layout kind.
- `Children`: ordered child participants.
- `Visibility`: visible, hidden, or collapsed.
- `LayoutIntent`: flex and authored layout inputs already represented by `LayoutIntent`.
- `MeasureCapability`: normal content measurement callback or built-in flex/container evaluator.
- `IntrinsicCapability`: supported intrinsic axes and query behavior.
- `ContentIdentity`: render/layout-affecting content fingerprint or version.
- `Diagnostics`: participant-level limitations or fallback explanations.

**Validation rules**:

- `ParticipantId` is unique within an evaluated tree.
- Child order is part of layout identity.
- Layout-affecting changes update the participant dependency key.
- Missing intrinsic capability is diagnostic when a container requires it.

## Layout Constraints

**Purpose**: Normalized constraints sent from parent to child during measurement.

**Fields**:

- `MinWidth`, `MaxWidth`: lower and upper horizontal bounds; max may be unbounded.
- `MinHeight`, `MaxHeight`: lower and upper vertical bounds; max may be unbounded.
- `WidthMode`, `HeightMode`: undefined, exactly, or at-most compatibility modes when needed.
- `Source`: viewport, parent, intrinsic query, fallback, or compatibility path.
- `NormalizedIdentity`: deterministic identity used in measure and cache keys.

**Validation rules**:

- Minimum values are finite and non-negative.
- Finite maximum values are greater than or equal to minimum values.
- Contradictory, NaN, infinite minimum, or negative constraints produce diagnostics and are rejected
  or degraded before output is accepted.
- Equivalent constraints normalize to the same identity.

## Measure Request

**Purpose**: One normal measurement of a participant under constraints for a layout pass.

**Fields**:

- `ParticipantId`: target participant.
- `Constraints`: normalized layout constraints.
- `ParentPath`: stable path for diagnostics and deterministic cache keys.
- `PassId`: layout pass identity.
- `LayoutInputKey`: content, layout intent, visibility, child order, and measurement dependency key.
- `CacheSnapshot`: read-only cache entries available to the measurement.

**Validation rules**:

- A participant is measured at most once per pass for the same constraints and input key.
- Measurement cannot inspect rendered descendant bounds to discover natural size.
- Additional natural-size discovery must use explicit intrinsic queries.

## Measured Layout Result

**Purpose**: Accepted measured size, child placements, intrinsic dependencies, and diagnostics for a
participant under constraints.

**Fields**:

- `ParticipantId`: measured participant.
- `Constraints`: normalized constraints used for measurement.
- `MeasuredSize`: accepted width and height.
- `ChildPlacements`: child placement records relative to the participant.
- `IntrinsicDependencies`: intrinsic query keys consumed during measurement.
- `CacheWrite`: cache entry candidate for deterministic reuse.
- `Diagnostics`: invalid, degraded, fallback, or unsupported layout messages.

**Validation rules**:

- Accepted size satisfies constraints unless a diagnostic records the explicit degradation.
- Child placements are finite, deterministic, and tied to child ids.
- Diagnostics are stable for repeated equivalent inputs.
- Cache writes include every dependency that affects reuse safety.

## Child Placement

**Purpose**: Placement result for one child inside a measured participant.

**Fields**:

- `ChildId`: placed child participant id.
- `X`, `Y`: placement origin relative to parent.
- `Width`, `Height`: placed child size.
- `Visibility`: resulting child visibility.
- `PlacementIdentity`: deterministic identity over bounds, visibility, z/layer inputs where layout
  depends on them, and pixel-snap policy when applicable.

**Validation rules**:

- Bounds are finite.
- Collapsed nodes do not occupy layout space.
- Hidden nodes retain layout space when current behavior requires it.
- Placement identities match between full and incremental layout for equivalent inputs.

## Intrinsic Query

**Purpose**: Explicit natural-size request used by containers that need size information outside the
normal measure result.

**Fields**:

- `ParticipantId`: queried participant.
- `Axis`: min width, max width, min height, or max height.
- `CrossAxisConstraint`: optional cross-axis bound relevant to text/wrapping/container behavior.
- `LayoutInputKey`: content/layout dependency key.
- `QuerySource`: ScrollViewer, custom container, compatibility check, or diagnostic probe.
- `QueryIdentity`: deterministic cache key identity.

**Validation rules**:

- Queries are pure and deterministic for the same participant, axis, cross-axis constraint, and input
  key.
- Unsupported or contradictory queries produce diagnostics rather than fabricated extents.
- Query identity changes when content, constraints, visibility, child order, or layout-affecting
  attributes change.

## Intrinsic Size Result

**Purpose**: Natural-size answer for an intrinsic query.

**Fields**:

- `QueryIdentity`: matching query identity.
- `Size`: finite natural width or height.
- `Dependencies`: child query/result identities consumed to answer the query.
- `Accepted`: whether the value can drive layout/scroll extent.
- `Diagnostics`: fallback, unsupported, expensive, unavailable, or contradiction messages.

**Validation rules**:

- Accepted size is finite and non-negative.
- A rejected intrinsic result cannot drive ScrollViewer extent or container size.
- Dependency identities are complete enough to invalidate stale cached answers.

## Layout Cache Entry

**Purpose**: Reusable measured or intrinsic result tied to all inputs that make reuse safe.

**Fields**:

- `EntryId`: deterministic id.
- `EntryKind`: measured result or intrinsic result.
- `ParticipantId`: owning participant.
- `ConstraintIdentity`: normalized constraints or query identity.
- `LayoutInputKey`: content/layout dependency key.
- `ChildDependencyKeys`: ordered child measurement or intrinsic keys.
- `ResultIdentity`: measured/result payload identity.
- `Revision`: evaluator/cache algorithm revision.

**Validation rules**:

- Reuse requires exact match of participant id, kind, constraints/query, layout input key, child
  dependency keys, and revision.
- Stale entries are ignored and may produce diagnostics.
- Cache reuse must not change the full/incremental result.

## Scroll Content Extent

**Purpose**: Natural content size used to compute scroll range inside a fixed viewport.

**Fields**:

- `ScrollViewerId`: owning ScrollViewer control id.
- `ViewportWidth`, `ViewportHeight`: fixed viewport size from layout.
- `ContentWidth`, `ContentHeight`: natural content extent from layout/intrinsic results.
- `OffsetX`, `OffsetY`: current scroll offsets.
- `MaxOffsetX`, `MaxOffsetY`: accepted scroll ranges.
- `ExtentSource`: intrinsic result, measured fallback, empty content, or diagnostic fallback.
- `Diagnostics`: extent-related messages.

**Validation rules**:

- Content extent is at least the viewport extent for non-error states.
- Overflow is reported when natural extent exceeds the viewport.
- Smaller and exact-fit content produce zero unnecessary overflow.
- Changed intrinsic content updates scroll range without changing unrelated surrounding layout.

## Layout Diagnostic

**Purpose**: Reviewable explanation of accepted, degraded, rejected, fallback, or limited layout
outcomes.

**Fields**:

- `NodeId`: optional participant/control id.
- `Code`: invalid constraints, unsatisfied constraints, unsupported intrinsic query, stale cache,
  fallback bounds, unmeasurable content, compatibility delta, or readiness limitation.
- `Severity`: info, warning, or error.
- `Message`: user-readable explanation.
- `Constraint`: normalized constraint/query context where relevant.
- `FallbackApplied`: whether layout proceeded with an explicit fallback.
- `DependencyKey`: optional cache/query key context.

**Validation rules**:

- Diagnostics are deterministic for equivalent inputs.
- Error diagnostics prevent accepted misleading results.
- Warnings document safe degradation or compatibility limitations.

## Layout Readiness Report

**Purpose**: Single review surface for P8 acceptance.

**Fields**:

- `Feature`: `150-intrinsic-layout-protocol`.
- `ContractStatus`: public surface and compatibility verdict.
- `ScrollViewerStatus`: content extent validation verdict and artifact links.
- `IntrinsicStatus`: query/caching validation verdict and artifact links.
- `ParityStatus`: full/incremental parity verdict and corpus link.
- `CompatibilityStatus`: default-behavior compatibility and documented intentional deltas.
- `DiagnosticsStatus`: invalid/unsupported case coverage.
- `Limitations`: deferred or environment-limited work.

**Validation rules**:

- Accepted readiness links to evidence for every required status.
- Incomplete, failed, skipped, or synthetic-only evidence cannot count as accepted.
- A reviewer can identify P8 status and supporting paths from this report.
