# Feature Specification: Modifier Layer IR Foundation

**Feature Branch**: `140-modifier-layer-ir`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report lists P2 as the next item after P0 layout/metrics cleanup
and P1 shared assembly extraction. Feature 138 covers P0 and feature 139 covers P1, so this
feature starts P2: an internal modifier/layer foundation for scene composition, plus the small
glyph-run representation spike needed to keep future text and portable-rendering work aligned.

P2 should prove cleaner composition semantics before the project commits to a broad public scene
contract change. Maintainers need a single internal model for ordered visual modifiers, local
z-order, out-of-tree portal layers, legacy-node compatibility, paint/hit-test ordering, and stable
text glyph data. Framework consumers should either see unchanged behavior or receive an explicit
compatibility and migration plan for any public change that proves necessary.

This is a Tier 1 architecture feature because it may affect scene contracts, rendering output,
surface baselines, and golden evidence. The feature must keep the scope bounded: it establishes
and validates the P2 foundation but does not implement full retained-renderer unification, full
text shaping, overlay interaction state, a portable scene protocol, compositor promotion, or the
later intrinsic layout protocol.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Compose Visual Semantics Consistently (Priority: P1)

A rendering maintainer describes a scene using ordered visual effects such as clipping, opacity,
offsets, transforms, backgrounds, overlays, cache boundaries, and local z-order. The system applies
those effects in a documented order, classifies their invalidation impact consistently, and can
normalize equivalent effect sequences without changing the rendered result.

**Why this priority**: This is the core P2 value. Later renderer work depends on composition rules
that are explicit, testable, and not scattered across special cases.

**Independent Test**: Build focused composition examples for each supported modifier category and
for representative modifier chains. Compare pre-normalized and normalized output, invalidation
classification, scene diagnostics, and cache fingerprints for equivalent inputs.

**Acceptance Scenarios**:

1. **Given** a scene with multiple ordered visual effects, **When** it is assembled and rendered,
   **Then** the effects apply in the documented order and the visible output matches that order.
2. **Given** a sequence containing identity or combinable effects, **When** the sequence is
   normalized, **Then** the rendered output and diagnostics remain equivalent to the original.
3. **Given** a layout-affecting effect changes, **When** the next frame is measured and rendered,
   **Then** layout and paint invalidation are both reported for the affected scope.
4. **Given** a paint-only or order-only effect changes, **When** the next frame is measured and
   rendered, **Then** the system avoids false layout invalidation while still updating the output.

---

### User Story 2 - Replace Ad Hoc Overlays with Portals and Layers (Priority: P1)

A framework maintainer authors or migrates overlay-like content that must escape ancestor clipping
and paint above normal content. The system represents that content as a portal into an ordered layer
host, while local z-order remains scoped to the parent container.

**Why this priority**: The report identifies the current overlay pass as a special-case mechanism.
P2 must generalize that behavior before interaction and retained-renderer work build on it.

**Independent Test**: Render scenes with in-flow content, clipped ancestors, local z-order changes,
and portal content targeting multiple layer classes. Verify paint order and hit-test order from the
same ordering evidence.

**Acceptance Scenarios**:

1. **Given** a child with local z-order inside a parent, **When** the parent renders multiple
   children, **Then** only that parent's child order is affected.
2. **Given** portal content anchored from inside a clipped subtree, **When** the scene renders,
   **Then** the portal content appears in its target layer and is not clipped by the ancestor that
   contained the anchor.
3. **Given** multiple portal layers, **When** they are rendered and hit-tested, **Then** higher
   layers paint above and receive hits before lower layers.
4. **Given** a scene that previously used overlay behavior, **When** it is migrated to portals,
   **Then** the visible output remains equivalent or any intentional difference is documented in
   the rebaseline ledger.

---

### User Story 3 - Preserve Legacy Scene Compatibility (Priority: P1)

A downstream consumer or existing test still uses the current scene forms for clipping,
translation, perspective, cached subtrees, text, and overlays. The project can prove those existing
forms continue to work while internally lowering them into the new composition foundation.

**Why this priority**: The report calls out public scene compatibility as a major risk. P2 should
reduce that risk with compatibility evidence before requiring consumers to migrate.

**Independent Test**: Run compatibility scenes that use legacy composition and text forms. Confirm
their rendered output, diagnostics, descriptions, cache fingerprints, and public surface evidence
remain stable unless an intentional change is disclosed.

**Acceptance Scenarios**:

1. **Given** an existing scene using legacy clipping or translation, **When** it is rendered through
   the new foundation, **Then** its output remains equivalent to the compatibility baseline.
2. **Given** an existing cached subtree, **When** cache-enabled and cache-disabled modes are
   compared, **Then** both modes remain equivalent.
3. **Given** a downstream consumer that matches existing public scene cases, **When** the feature is
   reviewed, **Then** the compatibility plan states which cases remain supported, which are
   deprecated, and the migration path for each deprecated case.
4. **Given** public surface verification, **When** the feature is complete, **Then** every public
   change is intentional, documented, and covered by migration guidance.

---

### User Story 4 - Establish a Glyph-Run Data Shape for Future Text Work (Priority: P2)

A text and portable-rendering maintainer needs a stable representation of shaped text output that
can be measured, painted, fingerprinted, and eventually serialized without depending on the full
future shaping pipeline. P2 introduces the representation proof and compatibility expectations
without changing text behavior beyond that proof.

**Why this priority**: The report pairs P2 with a glyph-run spike so future text shaping and
render-anywhere work do not create a second incompatible text representation.

**Independent Test**: Exercise representative text scenes through the glyph-run representation proof
and compare measured advances, rendered bounds, fingerprints, and fallback behavior for deterministic
sample text.

**Acceptance Scenarios**:

1. **Given** deterministic sample text, **When** it is represented as glyph-run data, **Then** the
   measured advance used for layout matches the advance used for drawing in the proof cases.
2. **Given** text scenes that do not opt into glyph-run proof behavior, **When** they render,
   **Then** existing deterministic fallback output remains compatible with the baseline.
3. **Given** equivalent glyph-run data across repeated runs, **When** fingerprints are computed,
   **Then** the fingerprints are stable and suitable for cache and future protocol work.
4. **Given** complex text shaping requirements beyond the proof scope, **When** the feature is
   reviewed, **Then** those requirements are explicitly deferred to the later text-shaping feature.

---

### User Story 5 - Produce Compatibility and Evidence for P3 Planning (Priority: P2)

A maintainer preparing the retained-renderer unification can rely on P2 evidence to understand the
composition foundation, compatibility commitments, remaining public-surface risks, and exact
verification status.

**Why this priority**: P2 is an enabling phase. Its value is not only new structure, but also the
evidence that P3 can remove the retained duplication without guessing about modifier and layer
semantics.

**Independent Test**: Review the feature evidence package and run the required verification suites.
Confirm the evidence names behavior that changed, behavior that stayed compatible, and work
intentionally left for later phases.

**Acceptance Scenarios**:

1. **Given** completed P2 evidence, **When** a maintainer starts P3 planning, **Then** modifier,
   layer, portal, legacy-lowering, and glyph-run responsibilities are documented clearly enough to
   plan against.
2. **Given** any changed pixel or surface baseline, **When** the evidence is reviewed, **Then** each
   change has an explicit disclosure entry and rationale.
3. **Given** the scope boundary, **When** the feature is reviewed, **Then** retained-renderer
   unification, full text shaping, overlay interaction state, portable protocol work, compositor
   work, and intrinsic layout are all absent from the implementation scope.

### Edge Cases

- Empty modifier chains and identity effects should not alter output, diagnostics, or fingerprints.
- Nested clips, offsets, transforms, and cache boundaries should preserve the same effective output
  in cache-enabled and cache-disabled modes.
- Equal local z-order values should fall back to declaration order so paint and hit-test behavior
  stays deterministic.
- Portal content with no target layer or no anchor evidence should fail safely with actionable
  diagnostics rather than silently disappearing.
- Empty portal layers should render equivalently to a scene with no portal layers.
- Portal content anchored inside clipped or transformed ancestors should use the resolved anchor
  evidence defined for this feature, not the ancestor's clipping result.
- Legacy overlay-only scenes should remain visible after migration to portal layers.
- Repeated equivalent glyph-run data should produce byte-identical fingerprints across runs.
- Text outside the glyph-run proof scope should keep the existing deterministic fallback behavior.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide one internal composition foundation for ordered visual effects,
  cache boundaries, local z-order, portal layers, and glyph-run proof data.
- **FR-002**: The effect ordering rules MUST be documented and covered by tests that distinguish
  different orders when order changes visible behavior.
- **FR-003**: The system MUST classify supported effects as layout-affecting, paint-affecting, or
  order-affecting, and MUST use those classifications during invalidation evidence.
- **FR-004**: The system MUST normalize identity and equivalent effect sequences without changing
  rendered output, diagnostics, or cache fingerprints.
- **FR-005**: Local z-order MUST affect only siblings within the same parent scope.
- **FR-006**: Portal content MUST render through ordered layer hosts that can escape ancestor clipping
  when the portal target requires it.
- **FR-007**: Paint order and hit-test order MUST be derived from one shared ordering rule for
  in-flow content and layered portal content.
- **FR-008**: Existing scene forms for clipping, translation, perspective, cached subtrees, text, and
  overlay behavior MUST remain compatible or receive explicit deprecation and migration guidance.
- **FR-009**: Existing cache-enabled versus cache-disabled parity MUST remain valid for scenes that
  include modifiers, layers, portals, legacy-lowered nodes, and glyph-run proof data.
- **FR-010**: Existing full versus retained rendering parity MUST remain valid for compatibility scenes
  affected by this foundation.
- **FR-011**: The glyph-run representation proof MUST support deterministic measurement, drawing,
  diagnostics, and fingerprinting for representative sample text.
- **FR-012**: Full text shaping, bidi handling, font fallback expansion, and line-breaking behavior
  MUST remain out of scope except where needed to prove the glyph-run data shape.
- **FR-013**: Any public surface change MUST be intentionally documented with compatibility impact,
  migration guidance, surface-baseline evidence, and versioning recommendation.
- **FR-014**: Any pixel baseline change MUST be recorded in a disclosure ledger with the reason for
  the change and the affected scenario.
- **FR-015**: The feature MUST NOT include retained-renderer unification, overlay interaction state,
  portable scene serialization, compositor promotion, damage-scissored presentation, or the
  intrinsic layout protocol.
- **FR-016**: Verification limitations and pre-existing failures encountered during validation MUST
  be recorded so maintainers can distinguish them from P2 behavior.

### Key Entities

- **Modifier effect**: An ordered visual or structural effect applied to content, such as clipping,
  opacity, offset, transform, background, overlay, cache boundary, or local z-order.
- **Effect classification**: The category that determines whether a changed effect should invalidate
  layout, paint, ordering, or a combination of those scopes.
- **Portal**: Content authored at one location but rendered into an ordered layer host so it can escape
  local clipping or stacking limits.
- **Layer host**: An ordered rendering destination for portal content, such as normal content,
  popups, tooltips, modal surfaces, drag feedback, or transient notifications.
- **Legacy lowering**: Compatibility behavior that maps existing scene forms onto the new composition
  foundation while preserving the existing public contract during the migration window.
- **Glyph-run data**: Stable shaped-text evidence containing enough information for deterministic
  measurement, drawing, diagnostics, and fingerprinting.
- **Compatibility plan**: The documented decision record for public surface changes, deprecated forms,
  migration guidance, and versioning impact.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of supported modifier categories have focused ordering and invalidation tests.
- **SC-002**: At least 12 representative modifier-chain cases prove normalization preserves output,
  diagnostics, and fingerprints.
- **SC-003**: Paint order and hit-test order match in 100% of in-flow and portal-layer ordering tests.
- **SC-004**: Legacy compatibility tests cover at least six categories: clipping, translation,
  perspective, cached subtrees, text, and overlay behavior.
- **SC-005**: Cache-enabled versus cache-disabled parity passes for 100% of modifier, layer, portal,
  legacy-lowered, and glyph-run proof scenarios.
- **SC-006**: Full versus retained parity passes for 100% of compatibility scenarios affected by this
  foundation.
- **SC-007**: The glyph-run proof covers at least five deterministic sample text cases and produces
  stable fingerprints across repeated runs.
- **SC-008**: Public surface verification reports either zero public changes or 100% documented public
  changes with migration guidance and versioning recommendation.
- **SC-009**: Every intentional pixel baseline change has a disclosure-ledger entry before the feature
  is considered ready for planning or implementation completion.
- **SC-010**: Scope review confirms zero implementation tasks for retained unification, full text
  shaping, overlay interaction state, portable serialization, compositor promotion, and intrinsic
  layout.
- **SC-011**: The relevant scene, controls, retained-rendering, cache-parity, hit-test, surface, and
  golden verification suites complete with zero new failures attributable to this feature.

## Assumptions

- Feature 138 covers the report's P0 quick win and feature 139 covers P1, so the next report item is
  P2: internal modifier/layer model plus glyph-run representation spike.
- P2 should prove the internal foundation first and expose only the smallest public surface needed
  for compatibility or authoring.
- Existing scene behavior is the compatibility baseline unless the feature explicitly documents an
  intentional change and supplies migration evidence.
- Existing overlay behavior should migrate to portal layers as a compatibility-preserving behavior
  change where possible.
- The glyph-run work in this feature is a representation proof, not the later full text-shaping
  implementation.
- This feature is Tier 1 because it may affect public contracts, baselines, and rendering evidence.
