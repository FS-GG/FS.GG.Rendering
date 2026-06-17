# Feature Specification: Retained Renderer Unification

**Feature Branch**: `141-retained-renderer-unification`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report says P0, P1, and P2 are shipped through Feature 140.
The next planned item is P3: retained renderer unification, also described as R1b. This
feature starts that phase.

P3 should finish the one-builder architecture promised by the report. Maintainers need one
authoritative scene assembly producer, with retained rendering acting as a reuse and
reconciliation layer over that producer rather than a second hand-written composition path.
Framework consumers should see the same rendered output, diagnostics, cache parity, and
authoring contracts they see today, while maintainers gain a structural guard against future
full-vs-retained drift.

This is a Tier 1 architecture feature because it changes renderer ownership boundaries,
package-level verification evidence, and retained rendering behavior risk. It must preserve
public authoring and scene contracts unless an explicit compatibility decision says otherwise.
The feature must stay bounded to retained renderer unification and must not take on later text,
interaction, portable protocol, compositor, or intrinsic layout phases.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Render Through One Authoritative Assembly Producer (Priority: P1)

A rendering maintainer changes how control visuals, child visuals, modifiers, layers, portals,
legacy scene forms, cache boundaries, and diagnostics assemble into renderable output. The
maintainer makes that change in one authoritative assembly producer and can prove direct,
first-frame retained, and warm retained rendering all use the same semantic result.

**Why this priority**: This is the core P3 value. The report identifies the second retained
builder as the source of recent drift and makes eliminating it the keystone for later phases.

**Independent Test**: Use focused scenes covering current composition semantics and compare direct
rendering, a cold retained frame, and a warm retained frame. Confirm they produce equivalent
visible output, diagnostics, fingerprints, and cache behavior from the same assembly ownership.

**Acceptance Scenarios**:

1. **Given** a scene with nested controls, modifiers, layers, and cache boundaries, **When** direct
   and retained rendering produce a frame, **Then** both modes use the same authoritative assembly
   semantics and produce equivalent output.
2. **Given** a retained frame is rendered for the first time, **When** it is compared with direct
   rendering, **Then** cold retained output, diagnostics, and fingerprints match the direct result.
3. **Given** a retained frame is rendered after prior state exists, **When** no relevant inputs
   changed, **Then** the warm retained result remains equivalent while reporting reuse evidence.
4. **Given** a composition rule changes in a future feature, **When** maintainers review the
   architecture, **Then** there is no separate retained composition rule set to update in parallel.

---

### User Story 2 - Preserve Existing Consumer Behavior (Priority: P1)

A framework consumer upgrades through the retained renderer unification and sees no unexpected
changes in layout, visuals, overlay layering, text proof behavior, diagnostics, cache parity,
or public authoring contracts.

**Why this priority**: P3 is an architecture unification. Consumer-visible changes would make it
hard to distinguish unification risk from new feature semantics.

**Independent Test**: Run existing parity, retained rendering, cache, scene, controls, surface, and
golden verification suites, plus focused compatibility examples from P1 and P2. Confirm any
observable change is explicitly documented and approved before readiness.

**Acceptance Scenarios**:

1. **Given** an existing screen with no retained reuse opportunity, **When** it renders before and
   after this feature, **Then** the visible output and diagnostics remain compatible.
2. **Given** an existing screen using modifiers, local z-order, portals, legacy lowering, cached
   subtrees, or glyph-run proof data, **When** it renders through retained mode, **Then** it remains
   equivalent to direct rendering.
3. **Given** public surface verification, **When** the feature is validated, **Then** public
   authoring and scene contracts remain unchanged or every intentional change has migration
   guidance and versioning rationale.
4. **Given** golden or pixel baseline verification, **When** a baseline differs, **Then** the
   difference is recorded with a rationale before the feature is considered ready.

---

### User Story 3 - Reuse Retained State Without Owning Scene Semantics (Priority: P1)

A performance maintainer inspects retained rendering behavior and can see that retained state
stores prior assembly results, identities, and reuse evidence, but does not fabricate independent
scene composition. Retained reuse remains deterministic and invalidates when identities or inputs
change.

**Why this priority**: Retained rendering is still valuable only if it reduces work without becoming
a second source of truth. This story protects both correctness and the retained renderer's purpose.

**Independent Test**: Exercise retained trees with stable inputs, changed visual inputs, changed
layout inputs, changed explicit identity, reordered children, and removed children. Verify reuse,
invalidation, and output equivalence in each case.

**Acceptance Scenarios**:

1. **Given** a subtree with stable identity and unchanged inputs, **When** a warm retained frame is
   produced, **Then** the retained renderer may reuse prior assembly results and still match direct
   rendering.
2. **Given** a subtree whose visual or layout inputs changed, **When** a warm retained frame is
   produced, **Then** the retained renderer invalidates the affected reuse and matches direct
   rendering.
3. **Given** child identity or ordering changes, **When** retained state is reconciled, **Then**
   state is preserved only for compatible children and stale output does not remain visible.
4. **Given** a retained frame cannot safely reuse prior state, **When** rendering continues, **Then**
   the system falls back to fresh assembly for that scope and records actionable evidence.

---

### User Story 4 - Prove the Drift Bug Class Is Closed (Priority: P2)

A maintainer planning future text, interaction, portable rendering, compositor, or intrinsic layout
work can rely on P3 evidence showing that direct and retained rendering are no longer maintained
as parallel composition implementations.

**Why this priority**: P3 is an enabling architecture phase. Later phases become cheaper only if the
evidence clearly proves the second-builder drift risk has been removed.

**Independent Test**: Review the feature evidence package and run deterministic fixture and
randomized tree checks. Confirm the evidence identifies one assembly owner, zero retained-only
composition rules, and the later work that remains out of scope.

**Acceptance Scenarios**:

1. **Given** completed P3 evidence, **When** a maintainer starts a later phase, **Then** the
   assembly owner, retained reuse boundary, parity guarantees, and remaining risks are documented.
2. **Given** randomized control trees and composition chains, **When** direct, cold retained, and
   warm retained outputs are compared, **Then** all generated cases satisfy the same equivalence
   expectations as focused fixtures.
3. **Given** a review of retained renderer responsibilities, **When** maintainers inspect the
   architecture, **Then** retained code contains reuse and reconciliation responsibilities but no
   independent scene semantics.
4. **Given** the feature scope, **When** it is reviewed, **Then** full text shaping, overlay
   interaction state, portable serialization, compositor promotion, and intrinsic layout remain
   absent from implementation scope.

### Edge Cases

- Empty controls, empty child collections, and content with no visible scene should remain
  equivalent in direct, cold retained, and warm retained modes.
- Nested clipping, transforms, offsets, cache boundaries, local z-order, and portal layers should
  preserve the same output and hit-test ordering established by Feature 140.
- Legacy-lowered scene forms should remain compatible when rendered through retained mode.
- Cache-enabled and cache-disabled modes should remain equivalent when retained state is cold,
  warm, invalidated, or partially reused.
- Explicit identity changes, duplicate identity values, reordered children, inserted children, and
  removed children should not leave stale retained output visible.
- A failed or abandoned retained update should not expose a partially updated frame.
- Diagnostics and metrics should distinguish fresh assembly, retained reuse, and invalidated reuse
  without changing the user-visible scene description.
- Repeated equivalent inputs should produce deterministic fingerprints and reuse evidence across
  runs.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The rendering system MUST have exactly one authoritative assembly producer for the
  current scene semantics used by both direct and retained rendering.
- **FR-002**: Retained rendering MUST reuse and reconcile prior assembly results rather than own an
  independent scene composition implementation.
- **FR-003**: Direct rendering, first-frame retained rendering, and warm retained rendering MUST
  produce equivalent visible output, diagnostics, fingerprints, and cache behavior for equivalent
  inputs.
- **FR-004**: Retained reuse MUST be based on stable identity and equivalent inputs, and MUST
  invalidate when relevant identity, visual, layout, modifier, layer, portal, text proof, or cache
  boundary inputs change.
- **FR-005**: Retained reconciliation MUST preserve state only for compatible children and MUST
  discard or rebuild state for incompatible, removed, reordered, or changed children.
- **FR-006**: The system MUST prevent stale or partially updated retained output from becoming the
  visible frame after a failed or abandoned retained update.
- **FR-007**: Existing full-versus-retained, cold-versus-warm, cache-enabled-versus-cache-disabled,
  and deterministic rendering parity checks MUST remain valid.
- **FR-008**: Verification MUST include focused compatibility coverage for the composition areas
  shipped in P1 and P2: shared assembly, modifiers, local z-order, portal layers, legacy lowering,
  cached subtrees, and glyph-run proof data.
- **FR-009**: Verification MUST include randomized tree and composition-chain coverage that compares
  direct, cold retained, and warm retained results.
- **FR-010**: Architecture evidence MUST identify the single assembly owner, the retained reuse
  boundary, and the removed retained-only composition responsibilities.
- **FR-011**: Existing public authoring and scene contracts MUST remain unchanged unless an explicit
  compatibility decision documents the change, migration guidance, and versioning impact.
- **FR-012**: Any intentional pixel, diagnostic, metric, or public surface change MUST be recorded
  with a rationale and reviewed before the feature is considered ready.
- **FR-013**: The feature MUST NOT introduce full text shaping, overlay interaction state, portable
  scene serialization, compositor promotion, damage-scissored presentation, or the intrinsic layout
  protocol.
- **FR-014**: Verification limitations and pre-existing failures encountered during validation MUST
  be recorded so maintainers can distinguish them from P3 behavior.

### Key Entities

- **Authoritative assembly producer**: The single source of truth that turns current control and
  scene semantics into renderable output.
- **Retained renderer**: The rendering mode that keeps prior state to reduce repeated work while
  preserving the same output as direct rendering.
- **Reconciliation identity**: Stable identity evidence used to decide whether prior retained state
  can be preserved for a current child or subtree.
- **Retained assembly result**: Prior renderable output and metadata that retained rendering may
  reuse only when identity and inputs remain compatible.
- **Invalidation evidence**: Recorded reason that retained state was reused, rebuilt, or discarded
  for a frame or subtree.
- **Parity oracle**: A verification comparison proving two rendering modes or cache modes produce
  equivalent output for the same input.
- **Unification evidence**: Documentation and tests showing retained rendering no longer owns a
  separate scene composition implementation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing full-versus-retained parity checks pass with no new failures
  attributable to this feature.
- **SC-002**: 100% of existing cache-enabled-versus-cache-disabled parity checks pass with no new
  failures attributable to this feature.
- **SC-003**: Focused retained compatibility verification covers at least eight categories: empty
  content, nested clipping, transforms or offsets, cache boundaries, local z-order, portal layers,
  legacy lowering, and glyph-run proof data.
- **SC-004**: Randomized verification covers at least 200 generated control trees or composition
  chains and finds zero direct/cold-retained/warm-retained equivalence failures.
- **SC-005**: Architecture evidence identifies exactly one authoritative assembly producer and zero
  retained-only scene composition rule sets.
- **SC-006**: Retained invalidation evidence covers at least six change categories: visual input,
  layout input, modifier or layer input, text proof input, explicit identity, and child ordering.
- **SC-007**: Public surface verification reports either zero public contract changes or 100%
  documented changes with migration guidance and versioning rationale.
- **SC-008**: Golden or pixel verification reports either zero intentional baseline changes or 100%
  documented baseline changes with a disclosure entry.
- **SC-009**: Determinism verification shows repeated equivalent retained frames produce stable
  fingerprints and reuse evidence across at least three consecutive runs.
- **SC-010**: Scope review confirms zero implementation tasks for full text shaping, overlay
  interaction state, portable serialization, compositor promotion, damage-scissored presentation,
  and intrinsic layout.
- **SC-011**: The relevant scene, controls, retained-rendering, cache-parity, hit-test, surface, and
  golden verification suites complete with zero new failures attributable to this feature.

## Assumptions

- Feature 138 covers P0, Feature 139 covers P1, and Feature 140 covers P2, so the next report item
  is P3: retained renderer unification.
- Feature 140 provides the internal modifier/layer, portal, legacy-lowering, and glyph-run proof
  foundation that this feature must preserve.
- Direct rendering remains the compatibility reference for visible output during this unification.
- Retained rendering should continue to provide work-reuse evidence, but correctness and parity take
  priority over preserving any specific reuse count.
- Existing parity and baseline suites are the primary compatibility oracles when supplemented by
  focused and randomized retained unification coverage.
- This feature is Tier 1 because it changes renderer ownership boundaries and verification evidence,
  even though public authoring and scene contracts are expected to remain compatible.
