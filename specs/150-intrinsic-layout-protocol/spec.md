# Feature Specification: Intrinsic Layout Protocol

**Feature Branch**: `150-intrinsic-layout-protocol`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers the next unimplemented roadmap item from the referenced radical rendering architecture report: P8 Radical layout. The feature introduces a constraints-down, sizes-up layout contract with intrinsic-size queries so scrollable and custom containers can determine content extents through the layout system instead of inspecting rendered descendants.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Measure Layout Through a Clear Contract (Priority: P1)

Framework maintainers and control authors need every layout participant to receive explicit size constraints and return a deterministic measured size plus child placement information, so layout behavior is predictable across full and incremental runs.

**Why this priority**: The intrinsic protocol is the foundation for P8. Scroll behavior, caching, and future specialized containers cannot be trusted until the basic measurement contract is clear and testable.

**Independent Test**: Can be tested by running a representative layout corpus through full layout and checking that every participant reports bounded, deterministic sizes and placements for the same inputs.

**Acceptance Scenarios**:

1. **Given** a container with finite width and height bounds, **When** layout is evaluated, **Then** each participant reports a measured size within those bounds and stable child placements.
2. **Given** the same layout inputs evaluated repeatedly, **When** measurement and placement results are compared, **Then** the results are identical.
3. **Given** invalid or contradictory constraints, **When** layout is evaluated, **Then** the result is rejected or degraded with a clear diagnostic rather than producing misleading bounds.

---

### User Story 2 - Size Scrollable Content From Intrinsics (Priority: P1)

Product authors need scrollable content to report its natural extent even when the viewport is smaller than the content, so scroll ranges, clipping, and fixed surrounding chrome behave predictably.

**Why this priority**: The report identifies the current scroll content-height discovery as the specific smell P8 should remove. ScrollViewer is the reference consumer that proves the intrinsic protocol is useful.

**Independent Test**: Can be tested by placing small, exact-fit, oversized, and dynamically changing content in a fixed viewport and verifying that the reported scroll range matches the content's natural extent.

**Acceptance Scenarios**:

1. **Given** content smaller than the viewport, **When** scroll layout is evaluated, **Then** the scrollable extent equals the viewport extent and no unnecessary overflow is reported.
2. **Given** content larger than the viewport, **When** scroll layout is evaluated, **Then** the viewport remains fixed and the scrollable extent reflects the full content size.
3. **Given** content whose intrinsic size changes, **When** layout is reevaluated, **Then** the scroll range updates without changing unrelated surrounding layout.

---

### User Story 3 - Preserve Incremental and Full Layout Parity (Priority: P2)

Maintainers need the new layout cache and intrinsic queries to preserve the existing guarantee that incremental layout produces the same bounds and placements as a full layout evaluation.

**Why this priority**: P8 changes layout semantics. Any mismatch between cached incremental results and full layout results would create difficult rendering and interaction bugs.

**Independent Test**: Can be tested by running the same corpus through cold full layout, warm incremental layout, and changed-input incremental layout, then comparing all reported bounds, placements, scroll extents, and diagnostics.

**Acceptance Scenarios**:

1. **Given** no layout-affecting input changes, **When** warm incremental layout runs after a full layout, **Then** all reported bounds, placements, and scroll extents match the full layout result.
2. **Given** a layout-affecting input changes in one subtree, **When** incremental layout runs, **Then** affected results update and unaffected results remain equivalent to a full layout result.
3. **Given** intrinsic queries are used to determine a container size, **When** incremental layout reuses cached information, **Then** reuse is accepted only when the intrinsic inputs still match.

---

### User Story 4 - Publish Layout Readiness and Compatibility Evidence (Priority: P3)

Package consumers and release reviewers need clear evidence that the new layout contract is ready, compatible with existing general-purpose layout behavior, and bounded in scope.

**Why this priority**: P8 is a Tier 1 layout behavior change. Consumers need documentation, compatibility notes, and validation artifacts before relying on it.

**Independent Test**: Can be tested by reviewing the readiness package and package contract validation to confirm that public behavior, intentional compatibility changes, and deferred layout work are explicit.

**Acceptance Scenarios**:

1. **Given** the P8 readiness package, **When** a reviewer opens the summary, **Then** they can identify the accepted layout contract, ScrollViewer proof, parity status, compatibility impact, and limitations.
2. **Given** existing flex-style layouts with default behavior, **When** compatibility validation runs, **Then** bounds and placements remain unchanged unless an intentional difference is documented.
3. **Given** a consumer-facing layout diagnostic, **When** constraints or intrinsic sizing cannot be accepted, **Then** the diagnostic explains the failure or fallback state without relying on private implementation knowledge.

### Edge Cases

- Empty containers, single-child containers, and deeply nested containers.
- Zero-sized, very small, very large, and unbounded constraints.
- Content that fits exactly, barely overflows, or changes size after initial layout.
- Nested scrollable containers and scrollable content inside clipped or layered areas.
- Text, image, and composed controls whose natural size depends on content.
- Layout-affecting attributes that change after a cached layout result exists.
- Intrinsic queries that are unavailable, contradictory, or too expensive to accept.
- Cached intrinsic results that become stale after child content, constraints, or layout-affecting attributes change.
- Existing overlay, render-anywhere, compositor-readiness, and text-shaping evidence must remain valid outside intentional layout deltas.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: As an umbrella scope requirement, the feature MUST implement the P8 Radical layout scope from the referenced report: constraints-down measurement, sizes-up results, intrinsic-size queries, deterministic layout caching, ScrollViewer content extent through the layout contract, and preserved incremental/full layout parity. Acceptance for this umbrella is evaluated through the concrete requirements below.
- **FR-002**: The feature MUST be treated as a Tier 1 contracted change because it changes observable layout behavior and may expand public layout contracts.
- **FR-003**: The system MUST define a layout participant contract that accepts explicit constraints and returns measured size, child placement, and diagnostics in a deterministic form.
- **FR-004**: Constraints MUST represent bounded and unbounded axes, minimum and maximum dimensions, and invalid or contradictory states.
- **FR-005**: Every accepted measured size MUST satisfy the applicable constraints or record an explicit degradation decision.
- **FR-006**: Layout participants MUST expose intrinsic-size queries sufficient for containers to determine natural width and height needs without inspecting rendered descendants.
- **FR-007**: Intrinsic-size results MUST be tied to the same content, constraints, and layout-affecting inputs used by normal measurement.
- **FR-008**: A normal layout pass MUST avoid repeated measurement of the same participant for the same inputs; additional size discovery MUST occur through explicit intrinsic queries.
- **FR-009**: The default general-purpose layout behavior MUST remain compatible with existing layouts unless a difference is intentionally documented.
- **FR-010**: ScrollViewer MUST determine scrollable content extent through the official layout contract and intrinsic results.
- **FR-011**: ScrollViewer MUST preserve fixed viewport bounds while reporting correct scroll ranges for smaller, exact-fit, and overflowing content.
- **FR-012**: Layout caching MUST reuse results only when all layout-affecting inputs, constraints, and intrinsic dependencies still match.
- **FR-013**: Layout caching MUST invalidate stale results when content, constraints, or layout-affecting attributes change.
- **FR-014**: Incremental layout and full layout MUST produce equivalent bounds, placements, scroll extents, and diagnostics for the representative layout corpus defined in this specification.
- **FR-015**: Diagnostics MUST explain invalid constraints, rejected intrinsic results, unsupported layout cases, and fallback behavior in a reviewable form.
- **FR-016**: Readiness evidence MUST include compatibility validation, intrinsic sizing validation, ScrollViewer validation, cache/invalidation validation, and incremental/full parity validation.
- **FR-017**: Public documentation and compatibility notes MUST explain the new layout contract, expected consumer impact, and out-of-scope layout work.
- **FR-018**: The feature MUST NOT include a new general-purpose constraint solver, compositor partial-redraw acceptance, browser rendering, overlay behavior changes, or text shaping changes unless explicitly recorded as separate follow-up work.

### Key Entities

- **Layout Participant**: A control, container, or layout-capable item that can be measured and placed under explicit constraints.
- **Layout Constraints**: The bounded or unbounded size limits that a participant must respect when reporting its measured size.
- **Measured Layout Result**: The accepted size, child placements, and diagnostics produced for a participant under specific constraints.
- **Intrinsic Size Result**: The natural size information a participant reports so containers can size or scroll content without inspecting rendered descendants.
- **Layout Cache Entry**: A reusable record of measured or intrinsic layout results tied to layout-affecting inputs and constraints.
- **Scroll Content Extent**: The natural content size used to determine scroll ranges inside a fixed viewport.
- **Layout Diagnostic**: A reviewable explanation of accepted, degraded, rejected, fallback, or environment-limited layout outcomes.
- **Layout Readiness Report**: The evidence summary that states whether P8 met its intrinsic protocol, ScrollViewer, compatibility, and parity goals.

### Scope and Classification

- This feature is the P8/R3b intrinsic-layout phase from the radical rendering roadmap.
- In scope: layout participant contract, constraints-down/sizes-up behavior, intrinsic-size queries, deterministic cache and invalidation evidence, ScrollViewer rework, compatibility evidence, documentation, and readiness artifacts.
- Out of scope: making a relational constraint solver the core layout model, changing compositor acceptance, adding new rendering backends, changing text shaping behavior, or changing overlay interaction behavior.
- Expected classification: Tier 1, because the feature affects observable layout behavior and may add or alter consumer-visible layout contracts.

### Representative Layout Corpus

The representative layout corpus used for FR-014 and SC-001 MUST include at least 12 named cases, recorded in readiness evidence, covering:

- Constrained roots with finite width and height.
- Unbounded horizontal and vertical axes.
- Zero-sized, very small, and very large constraints.
- Invalid or contradictory min/max constraints.
- Measured leaves with deterministic child placement identity.
- Intrinsic-capable nodes reporting natural width and height.
- Containers deriving size from intrinsic child results.
- Dynamic content size changes.
- Layout-affecting attribute changes including LayoutIntent and visibility.
- Child insertion, removal, and reorder.
- Fixed-viewport ScrollViewer cases for smaller, exact-fit, and overflowing content.
- Nested scrollable content and scrollable content inside clipped or layered parents.
- Unsupported or unavailable intrinsic queries that must produce diagnostics or explicit fallback.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the representative layout corpus defined in this specification produces equivalent bounds, placements, scroll extents, and diagnostics between full layout and incremental layout.
- **SC-002**: 100% of default-behavior compatibility cases preserve existing bounds and placements, except for intentional differences documented in the compatibility ledger.
- **SC-003**: ScrollViewer validation covers at least 10 representative content cases, including empty, smaller-than-viewport, exact-fit, overflowing, nested, and dynamically changing content, with all expected scroll extents accepted.
- **SC-004**: Cache and invalidation evidence covers at least 5 layout-affecting input-change categories and shows no accepted stale measured or intrinsic result.
- **SC-005**: 100% of invalid, contradictory, or unsupported intrinsic-layout cases produce actionable diagnostics and no accepted misleading layout result.
- **SC-006**: Layout readiness can be reviewed from a single summary in under 10 minutes, including links or paths to compatibility, ScrollViewer, intrinsic, cache, and parity evidence.
- **SC-007**: Public contract and package validation pass with only documented layout-related changes.
- **SC-008**: Focused regression validation for prior retained rendering, overlay, render-anywhere, text-shaping, and compositor-readiness evidence passes with no undocumented behavior changes.

## Assumptions

- "Next item" means P8 Radical layout because the referenced report states that P0 through P7 are implemented or landed and P8 remains unimplemented.
- Feature 150 starts from the branch state after Feature 149, including retained renderer unification, modifier/layer foundations, overlay state, render-anywhere protocol, text shaping evidence, and compositor readiness evidence.
- The first P8 slice focuses on the intrinsic layout protocol and ScrollViewer proof. A specialized relational layout container may be planned later, but it is not required for P8 acceptance.
- Existing general-purpose flex-style layout remains the default model.
- Environment-limited compositor proof from P7 does not block specifying or implementing P8, but P8 must not claim new compositor acceptance.
- The planning phase will decide exact public surface shape, naming, and package placement under the repository constitution.
