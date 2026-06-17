# Feature Specification: Shared Assembly Extraction

**Feature Branch**: `139-shared-assembly-extraction`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report lists P0 as layout attributes plus metrics cleanup and P1 as R1a shared assembly extraction. Feature 138 already covers P0, so this feature starts P1.

R1a is a behavior-preserving architecture step. Maintainers need one authoritative current-semantics assembly rule set for scene composition before adding modifier, portal, and retained-renderer changes in later phases. Framework consumers should see the same visuals, public authoring behavior, diagnostics, and cache parity they see today.

This feature is a Tier 1 signature-impacting internal refactor because the shared assembly seam is declared through curated `.fsi` signatures. It must not change public authoring contracts, portable scene contracts, package surface baselines, or golden outputs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Change Composition Rules in One Place (Priority: P1)

A framework maintainer needs to reason about how current rendering semantics compose a control's own visuals, child visuals, container clipping, visual offsets, cache boundaries, and overlay output. The maintainer can find one authoritative assembly rule set and verify that both immediate and retained rendering use it.

**Why this priority**: The report identifies duplicated assembly as the root cause behind recent retained-render and overlay regressions. Reducing that duplication is the main value of P1.

**Independent Test**: Inspect the rendering architecture and run focused composition fixtures that exercise clipping, offsets, overlays, and cache boundaries through immediate, cold retained, and warm retained paths. Confirm those paths use the same assembly rules and produce equivalent output.

**Acceptance Scenarios**:

1. **Given** a tree with nested container clipping, **When** immediate and retained rendering compose it, **Then** both paths apply the same clipping boundary and produce equivalent output.
2. **Given** a tree with visual offsets and cache boundaries, **When** the first retained frame and a warm retained frame are compared with immediate rendering, **Then** all three outputs remain equivalent.
3. **Given** a tree with in-flow content and overlay content, **When** immediate and retained rendering compose it, **Then** in-flow content and overlay content appear in the same order as before this feature.
4. **Given** a future maintainer reviewing assembly ownership, **When** they trace current scene composition, **Then** they find one authoritative rule set rather than separate hand-written full and retained assembly behavior.

---

### User Story 2 - Preserve Existing Rendering Behavior (Priority: P1)

A framework consumer upgrades through this internal refactor and sees no visual, layout, interaction, diagnostic, or metric behavior change in existing screens.

**Why this priority**: R1a exists to reduce architecture risk before semantic changes. Any consumer-visible change belongs in a later feature with its own compatibility plan.

**Independent Test**: Run the existing parity, cache, retained-render, layout, surface, and golden verification suites. Confirm the refactor introduces no public-surface drift and no intentional pixel baseline changes.

**Acceptance Scenarios**:

1. **Given** an existing screen with no overlays, **When** it is rendered before and after this feature, **Then** the visible result and diagnostics remain unchanged.
2. **Given** an existing screen with overlays or clipped containers, **When** it is rendered before and after this feature, **Then** the visible result and diagnostics remain unchanged.
3. **Given** public surface baseline checks, **When** the feature is verified, **Then** no public authoring or scene contract changes are reported.
4. **Given** cache-on and cache-off comparison modes, **When** the feature is verified, **Then** cached output remains equivalent to direct output.

---

### User Story 3 - Prepare Later Radical Rendering Work (Priority: P2)

A maintainer planning modifier algebra, portals, or retained-renderer unification can build on a smaller and safer assembly boundary rather than updating multiple composition paths in parallel.

**Why this priority**: This is enabling work for P2 and P3 in the report. It should leave a clear seam and evidence trail without taking on later public IR or behavior changes.

**Independent Test**: Review the planning notes and verification evidence for this feature. Confirm later phases can target the shared assembly boundary and that modifier, portal, intrinsic-layout, and retained-unification work remains out of scope.

**Acceptance Scenarios**:

1. **Given** the completed feature evidence, **When** a maintainer starts P2 planning, **Then** the assembly boundary and remaining responsibilities are documented clearly enough to plan against.
2. **Given** the completed feature evidence, **When** the scope is reviewed, **Then** it contains no modifier algebra, portal semantics, intrinsic layout protocol, text shaping, compositor, or portable protocol changes.
3. **Given** a later composition-rule change, **When** maintainers estimate impact, **Then** the change no longer requires synchronized edits across separate full and retained assembly implementations.

### Edge Cases

- Empty controls, controls with no children, and containers with no visual output should remain equivalent across immediate and retained rendering.
- Nested clipping should preserve the same effective visible region as before this feature.
- Overlay-only content should remain paint-ordered consistently with existing overlay behavior.
- Cache boundaries inside clipped or offset subtrees should remain equivalent with caching enabled and disabled.
- Warm retained frames should not reuse stale assembly when layout or visual inputs change.
- Existing diagnostics should still describe the same user-visible scene structure even if internal ownership changes.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The rendering system MUST have one authoritative current-semantics assembly rule set for composing own visuals, child visuals, clipping, offsets, cache boundaries, and overlays.
- **FR-002**: Immediate rendering, first-frame retained rendering, and warm retained rendering MUST use that authoritative rule set for current scene composition.
- **FR-003**: Existing rendered output MUST remain byte-identical or pixel-identical within the repository's established verification tolerances.
- **FR-004**: Existing public authoring contracts and scene contracts MUST remain unchanged.
- **FR-005**: Existing diagnostics, scene descriptions, and work-reduction metrics MUST remain semantically unchanged for equivalent inputs.
- **FR-006**: Existing full-versus-retained, cold-versus-warm, and cache-enabled-versus-cache-disabled parity checks MUST remain valid.
- **FR-007**: Verification MUST include focused composition coverage for nested clipping, offsets, cache boundaries, overlays, empty content, and warm retained reuse.
- **FR-008**: The feature evidence MUST document the single assembly ownership boundary and the responsibilities intentionally left for later phases.
- **FR-009**: The feature MUST NOT introduce modifier algebra, portal semantics, public IR changes, intrinsic layout protocol changes, text shaping changes, compositor changes, or portable protocol changes.
- **FR-010**: Any verification limitation or pre-existing external failure encountered during validation MUST be recorded with enough detail for a maintainer to distinguish it from this feature's behavior.

### Key Entities

- **Current-semantics assembly rule set**: The single source of truth for how today's rendering semantics combine own visuals, child visuals, clipping, offsets, cache boundaries, and overlays into renderable output.
- **Immediate rendering path**: The direct rendering route used as a compatibility and parity reference.
- **Retained rendering path**: The incremental rendering route that reuses prior work while preserving the same visible result as immediate rendering.
- **Parity oracle**: A verification comparison that proves two rendering modes produce equivalent output for the same input.
- **Assembly ownership evidence**: Documentation or review evidence showing where current assembly rules live and which later radical changes remain out of scope.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of existing full-versus-retained parity checks pass with no new failures.
- **SC-002**: 100% of existing cache-enabled-versus-cache-disabled parity checks pass with no new failures.
- **SC-003**: Focused composition verification covers at least six edge categories: nested clipping, offsets, cache boundaries, overlays, empty content, and warm retained reuse.
- **SC-004**: Public surface verification reports zero intentional public contract changes for this feature.
- **SC-005**: Golden or pixel verification reports zero intentional rendering baseline changes for this feature.
- **SC-006**: Architecture evidence identifies exactly one authoritative current-semantics assembly rule set used by immediate, first-frame retained, and warm retained rendering.
- **SC-007**: Scope review confirms zero later-phase semantics included: modifier algebra, portals, public IR changes, intrinsic layout protocol, text shaping, compositor, and portable protocol are all excluded.
- **SC-008**: The relevant verification suites complete with zero new failures attributable to this feature.

## Assumptions

- Feature 138 covers the report's P0 quick win, so the next report item is P1: R1a shared assembly extraction.
- R1a is intentionally behavior-preserving; any semantic rendering change belongs in P2 or later.
- The immediate rendering path remains the compatibility reference for current output.
- Existing parity and baseline tests are sufficient compatibility oracles when supplemented by focused composition coverage for the duplicated assembly cases named in the report.
- This feature uses Tier 1 verification because it intentionally updates `.fsi` implementation contracts, even though it must preserve public contracts and observable behavior.
