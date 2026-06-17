# Feature Specification: Layout Attributes and Metrics Green

**Feature Branch**: `138-layout-attrs-metrics-fix`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item" from the active report
`docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md`.

## Context

The active radical rendering report names P0 as the next independently shippable step before the renderer
refactors begin: make the existing flex layout model authorable through the public control authoring surface
and fix the pre-existing text-cache metrics failure so the solution is green.

This feature is intentionally a quick win. Framework consumers should be able to request common flex layout
behavior directly, including padding, margin, gap, alignment, flex growth/shrink, flex basis, and min/max
size constraints. Existing screens with no authored layout values should keep their current geometry, while
screens that opt in should see those authored values honored by layout and by incremental layout invalidation.

The same phase also closes the known metrics defect recorded in the report: a cold text-heavy frame is
currently counted as having cache hits when it should be a cold measurement window. Maintainers need that
metric to be truthful before using it as evidence during larger retained-renderer and compositor work.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Author flex layout values directly (Priority: P1)

A framework consumer authors a control tree with explicit padding, margin, gap, alignment, flex growth,
flex shrink, flex basis, and min/max size constraints. The rendered layout honors those values consistently,
so fixed chrome regions can stay fixed, flexible content can take the remaining space, and container spacing
can be described without hidden layout workarounds.

**Why this priority**: This is the main P0 user value. The layout model already supports these concepts, but
consumers cannot rely on them until the public authoring surface drives the actual layout result.

**Independent Test**: Build focused layout examples that author each supported value one at a time and in
representative combinations. Confirm the measured bounds reflect the authored values and that equivalent
screens with no authored values keep their prior geometry.

**Acceptance Scenarios**:

1. **Given** a container with authored padding and gap, **When** it is laid out, **Then** child bounds include
   the requested inset and spacing.
2. **Given** siblings with authored flex growth, flex shrink, and flex basis, **When** their parent has extra
   or constrained space, **Then** the siblings divide space according to the authored flex values.
3. **Given** a child with authored min/max size constraints, **When** parent space is smaller or larger than
   the child's preferred size, **Then** the child respects the authored bounds.
4. **Given** a screen that does not author these layout values, **When** it is laid out, **Then** its bounds
   remain unchanged from the current default behavior.

---

### User Story 2 - Incremental layout knows which authored values affect geometry (Priority: P1)

A maintainer changes a geometry-affecting layout value between frames. The incremental layout path treats
that frame as a geometry change and re-measures the necessary layout scope. A visual-only or content-only
change does not become a false geometry invalidation.

**Why this priority**: Authoring support is only safe if retained and incremental rendering stay correct.
New layout values must participate in the same invalidation rules as existing size and orientation changes.

**Independent Test**: Change each newly supported geometry-affecting value across frames and verify that the
layout invalidation count is greater than zero and no greater than the rendered node count for that frame.
Change a non-layout value and verify that layout invalidation remains zero while the rendered output still
updates correctly.

**Acceptance Scenarios**:

1. **Given** a warm retained frame, **When** an authored padding, margin, gap, alignment, flex, or min/max
   size value changes, **Then** the layout invalidation metric reports a geometry change and the final bounds
   match a full layout of the same frame.
2. **Given** a warm retained frame, **When** only a visual style value changes, **Then** layout invalidation
   remains zero.
3. **Given** a generated corpus of candidate authoring values, **When** the layout-affecting set is checked,
   **Then** every geometry-driving value is included and every non-geometry value is excluded.

---

### User Story 3 - Shell chrome can be pinned without special-case layout fixes (Priority: P2)

A generated product or sample app uses top, side, and bottom chrome around a scrollable content region. The
chrome can be pinned to its intended size with authored layout values while the content region takes the
remaining space and remains usable.

**Why this priority**: The report calls out the shell-chrome failure as an immediate blocker that this P0
work should unblock. It is a visible proof that the authoring surface solves real application layout
problems instead of only isolated unit cases.

**Independent Test**: Render a shell-style screen with fixed header, footer, navigation, and flexible content
at the default `640x480` viewport and the compact `400x300` viewport. Confirm that fixed chrome regions keep
their requested dimensions and the content region receives the remaining space at both sizes.

**Acceptance Scenarios**:

1. **Given** a shell screen with authored non-shrinking header, footer, and side navigation, **When** it is
   rendered at `640x480` and `400x300`, **Then** those chrome regions keep their requested dimensions.
2. **Given** the same shell screen, **When** content is taller than the content region, **Then** the content
   region remains bounded and the chrome is still visible.

---

### User Story 4 - Text-cache metrics are truthful before refactors (Priority: P2)

A maintainer captures frame metrics for a text-heavy sequence. The first cold frame reports cold text
measurement work, the next equivalent warm frame reports reuse, and style-only or idle frames report no
false text or layout work.

**Why this priority**: The report records a pre-existing cold-frame metrics failure. Larger rendering work
will depend on metrics as evidence, so this defect must be corrected before those refactors start.

**Independent Test**: Run a deterministic text-heavy frame sequence through the public host metrics path.
Confirm cold, warm, style-only, and idle frames report the expected hit, miss, and invalidation regimes.

**Acceptance Scenarios**:

1. **Given** a cold text-heavy frame, **When** metrics are captured, **Then** text-cache hits are zero and
   text-cache misses represent the measured text work.
2. **Given** an equivalent warm text-heavy frame after the cold frame, **When** metrics are captured, **Then**
   text-cache hits are nonzero and misses are zero.
3. **Given** a style-only or idle frame over warm text, **When** metrics are captured, **Then** text-cache
   misses, layout invalidation, and re-measure counts are all zero.

### Edge Cases

- Authored layout values equal to their defaults should not introduce extra bounds changes.
- Zero padding, zero margin, zero gap, zero flex growth, and zero flex shrink should be accepted and applied
  according to their normal layout meaning.
- Min size greater than preferred size and max size smaller than preferred size should clamp geometry
  predictably.
- Removed layout values should invalidate layout just like changed layout values.
- Margin and padding remain uniform values for this feature; edge-specific values are outside this P0 scope.
- A cold text-heavy frame with repeated identical text should not report hits until the cache has a prior
  resident measurement from an earlier frame.
- A metrics capture repeated with the same frame sequence should produce byte-identical metric values.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The public control authoring surface MUST allow consumers to request padding, margin, gap,
  alignment, flex growth, flex shrink, flex basis, and min/max size constraints.
- **FR-002**: Authored layout values MUST affect measured bounds in the same frame they are authored.
- **FR-003**: Screens that do not author the newly supported layout values MUST preserve their current
  default bounds.
- **FR-004**: Authored values MUST override defaults consistently, including explicit zero values.
- **FR-005**: Every newly supported geometry-affecting authoring value MUST be treated as layout-affecting by
  incremental layout invalidation.
- **FR-006**: Visual-only and content-only changes MUST NOT be treated as layout-affecting unless they also
  change a geometry-driving value.
- **FR-007**: The layout-affecting authoring-value guard MUST detect drift between supported layout values
  and the invalidation set.
- **FR-008**: Shell-style screens MUST be able to pin fixed chrome regions and allocate remaining space to the
  content region using authored layout values.
- **FR-009**: The text-cache metric window MUST report zero hits on a cold text-heavy frame with no prior
  resident measurements from an earlier frame.
- **FR-010**: The text-cache metric window MUST report hits and zero misses on an equivalent warm text-heavy
  frame after the cold frame.
- **FR-011**: Style-only and idle frames over warm text MUST report zero text-cache misses, zero layout
  invalidations, and zero re-measured nodes.
- **FR-012**: The layout and metrics behavior MUST be deterministic for repeated same-sequence captures.
- **FR-013**: This feature MUST leave larger renderer-architecture refactors out of scope; it prepares the
  codebase for them by making P0 green.

### Key Entities

- **Layout authoring value**: A consumer-supplied geometry value such as padding, margin, gap, alignment,
  flex sizing, or min/max size constraints.
- **Default layout behavior**: The geometry produced when a control does not supply the new authoring values.
  This behavior is the compatibility baseline for existing screens.
- **Layout invalidation set**: The set of changed authoring values that require layout to be re-measured on an
  incremental frame.
- **Shell chrome layout**: A representative application frame with fixed header, footer, navigation, and a
  flexible content region.
- **Text-cache metric window**: The frame-level accounting boundary that distinguishes cold measurement work,
  warm reuse, style-only work, and idle work.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of supported layout authoring values have focused tests showing they affect measured
  bounds when authored.
- **SC-002**: 100% of existing no-authored-value layout compatibility cases keep their prior bounds.
- **SC-003**: 100% of newly supported geometry-affecting values are included in the layout invalidation guard,
  and 100% of visual-only values in the guard corpus remain excluded.
- **SC-004**: The shell-chrome proof renders fixed chrome regions at their requested dimensions and keeps the
  content region bounded at both validated viewports: `640x480` and `400x300`.
- **SC-005**: The cold text-heavy metrics proof reports exactly zero text-cache hits and at least one
  text-cache miss.
- **SC-006**: The warm text-heavy metrics proof reports at least one text-cache hit and exactly zero
  text-cache misses.
- **SC-007**: Style-only and idle metrics proofs report exactly zero text-cache misses, zero layout
  invalidations, and zero re-measured nodes.
- **SC-008**: Repeated same-sequence metrics captures produce byte-identical metric values.
- **SC-009**: The layout, controls, and host metrics verification suites complete with zero failing tests
  before P1 begins.

## Assumptions

- The active report's "next item" means P0: layout attributes plus the pre-existing text-cache metrics fix.
- The public authoring surface should expose the existing flex layout concepts; this feature does not replace
  the layout engine or introduce the later intrinsic-size protocol.
- Padding and margin remain uniform values for P0. Edge-specific variants can be specified separately if they
  become necessary.
- Existing default geometry remains the compatibility baseline unless a screen explicitly authors one of the
  newly supported values.
- The text-cache metrics defect is a reporting-window or accounting defect, not a request to redesign the
  cache itself.
- This is a Tier 1 change because it expands public authoring behavior and changes observable layout and
  metric behavior.
