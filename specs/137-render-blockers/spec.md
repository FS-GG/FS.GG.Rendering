# Feature Specification: Render Blockers — Clipping, Overlay & Scroll

**Feature Branch**: `137-render-blockers`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "fix the blockers"

## Context

Feature 136 (`136-showcase-render-fixes`) shipped the framework text and composite-control fixes but
**deferred** three rendering defect classes because the fix for all of them — clipping a container's
**children** to the container's bounds — wraps cached data-grid-row picture boundaries
(`SceneNode.CachedSubtree`, features 116/120) inside a clip group, which breaks the retained renderer's
**picture-cache parity** invariants (`cache-on ≡ cache-off`, present-but-dead, effectiveness). Three parity
tests failed and the change was reverted rather than shipped broken (see
`specs/136-showcase-render-fixes/contracts/rebaseline-ledger.md` → "Realized outcome", and memory
`feature-136-status`).

This feature unblocks and lands those deferred classes:

- **control-overlap / region-overlap / right-edge spill** — a child paints past its container's edge.
- **overlay-overprint** — open transient surfaces (menus, combo/auto-complete/date-picker dropdowns) are
  overprinted by in-flow neighbours instead of floating above them.
- **unbounded-content / no-scroll** — a `ScrollViewer` is visual chrome only; content taller than the
  viewport spills instead of being clipped and scrollable.

The crux is that the enabling change (clip container children) MUST be applied **identically** in both the
full `Control.renderTree` paint and the incremental retained path (`RetainedRender`), and the picture-cache
fingerprint / effectiveness logic MUST become **clip-aware** so the cache invariants still hold. Without
that, the visible fixes cannot land.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Children never paint past their container, with the retained path still correct (Priority: P1)

A consumer renders a page whose layout places a control (or a long label, or a nav-rail item) at or beyond
its parent container's edge. Every child's drawn area is confined to its container's bounds — no right-edge
or bottom-edge spill, no nav-label bleed — and this holds whether the frame was produced by a full render or
by the retained incremental path reusing cached subtrees (including cached data-grid rows).

**Why this priority**: This is the blocker itself. Container clipping is the shared enabler for overlay and
scroll, and it is the change that broke picture-cache parity in 136. Landing it correctly (parity intact) is
the prerequisite for everything else and fixes the control/region-overlap and spill classes directly.

**Independent Test**: Enable container-child clipping; render the 19 showcase pages (both themes) via the
full path and via the retained path and confirm (a) no child's drawn rect exceeds its container's bounds and
(b) the two paths produce byte-identical output, with the feature-116/120 picture-cache parity suite
(`cache-on ≡ cache-off`, present-but-dead, effectiveness) green.

**Acceptance Scenarios**:

1. **Given** a container whose child is laid out wider/taller than the container, **When** the page renders,
   **Then** the child's painted area is clipped to the container's bounds and nothing paints past the edge.
2. **Given** a container that hosts cached data-grid-row picture boundaries, **When** the retained path
   re-renders it with clipping enabled, **Then** the output is byte-identical to a full render and the
   replay-cache `cache-on ≡ cache-off` parity holds.
3. **Given** the picture cache is enabled, **When** clipping wraps cached subtrees, **Then** the cache still
   reports the same hits/effectiveness and no falsely "present-but-dead" boundary as without clipping.

---

### User Story 2 - Transient surfaces float above neighbours at true z-order (Priority: P1)

A consumer opens a menu, context-menu, combo-box, auto-complete, or date-/time-picker. The open surface
paints **above** every in-flow sibling at its true coordinates (it is not clipped away by an ancestor
container and is not overprinted by later in-flow nodes), its items are distinct (no shared baseline), and a
pointer hit on the overlapping region resolves to the topmost overlay.

**Why this priority**: Overlay-overprint is a P1 defect class from 136. It composes with US1: an overlay must
escape ancestor clipping (it floats above the flow), so it depends on the clipping model being in place.

**Independent Test**: Render a page with an open transient surface over an in-flow sibling; confirm the
surface's drawn area is painted last (z-top), is not clipped by its ancestor container, items occupy distinct
y-bands, and `nearestAuthored`/hit-test at an overlapping point returns the overlay. A page with no open
transient renders identically to a pure in-flow pass.

**Acceptance Scenarios**:

1. **Given** an open dropdown over an in-flow sibling, **When** the page renders, **Then** the dropdown paints
   above the sibling and the sibling does not overprint it.
2. **Given** an overlay nested inside a clipped container, **When** the page renders, **Then** the overlay is
   NOT clipped to that container (it floats above the flow at true coordinates).
3. **Given** a point inside an open overlay that overlaps an in-flow control, **When** hit-tested, **Then**
   the topmost overlay wins.
4. **Given** a page with no open transient surface, **When** rendered, **Then** the output is byte-identical
   to the pre-overlay-pass in-flow render (the overlay group is empty).

---

### User Story 3 - Long pages stay within the window and scroll (Priority: P2)

A consumer views a page taller than the content region inside a `ScrollViewer`. The content is clipped to the
viewport's box (nothing spills outside it), a scroll offset and a scroll affordance exist, and content beyond
the viewport is reachable by scrolling rather than painted outside the window.

**Why this priority**: Unbounded-content/no-scroll is the P3 class from 136; it depends on the container
clipping model (a `ScrollViewer` is a clipping viewport) and integrates the sample Shell's region sizing.

**Independent Test**: Render a `ScrollViewer` whose content is taller than its box; confirm the content is
clipped to the box, a scroll offset + affordance are exposed, and a control beyond the fold is clipped
(scrollable) not spilled; the status strip / feedback text render fully within the window.

**Acceptance Scenarios**:

1. **Given** content taller than the `ScrollViewer` box, **When** rendered, **Then** the content is clipped to
   the box and a scroll affordance is shown.
2. **Given** a page taller than the content region, **When** rendered at the default window size, **Then**
   nothing paints outside the content region.

---

### User Story 4 - The corrected output is re-baselined and the 19 pages re-verified (Priority: P3)

The maintainer re-establishes the golden/drift baselines as intended correctness changes and re-captures all
19 showcase pages (both themes), confirming zero instances of the seven defect classes — the verification
deferred in 136 because it is the vehicle for exactly these layout/overlay/scroll classes.

**Why this priority**: Closes the disclosure/verification loop (FR-012/FR-013 from 136) once the framework
fixes land; it is meaningless before US1–US3 ship.

**Independent Test**: Run the feature-135 19-page evidence capture (both themes) after US1–US3; confirm the
rebaseline ledger has one disclosed row per changed baseline and no page exhibits any of the seven defect
classes. Where no display is available, record a disclosed no-GL degrade — never a fabricated pass.

**Acceptance Scenarios**:

1. **Given** US1–US3 are landed, **When** the 19 pages are re-captured, **Then** none exhibits wrong-glyph,
   truncation, region/control overlap, overlay overprint, mis-structured composites, or unbounded content.
2. **Given** a baseline changed, **When** it is re-established, **Then** the change is recorded in the
   rebaseline ledger as intended (not silently overwritten).

### Edge Cases

- A container that both clips its children AND hosts cached data-grid-row picture boundaries (the exact 136
  failure): clipping must not change cache hits/effectiveness or the `cache-on ≡ cache-off` result.
- An overlay nested several containers deep: it must escape ALL ancestor clips, not just its immediate parent.
- A `ScrollViewer` whose content is itself a cached subtree: clipping the viewport must preserve cache parity.
- A container with zero children, or children that produce no drawn scenes: composition must be byte-identical
  to the pre-clip `own @ children` (no spurious empty clip node that changes a fingerprint).
- Incremental re-render where only a deep child changed: the clip wrapper must be reproduced identically so
  the retained frame stays byte-identical to a full render.
- Theme invariance: every fix holds identically under antLight and antDark.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The renderer MUST clip every container's children to the container's bounds so no child paints
  past its parent's edges (right/bottom spill, nav-label bleed).
- **FR-002**: Container-child clipping MUST be applied identically by the full render (`Control.renderTree`)
  and the incremental retained path (`RetainedRender`), so the two produce byte-identical output (the
  existing full ≡ retained parity is preserved).
- **FR-003**: The retained renderer's picture-cache fingerprint and effectiveness/present-but-dead logic MUST
  become clip-aware so that, with cached subtrees wrapped in a clip, the `cache-on ≡ cache-off` byte-identical
  parity and the cache-hit/effectiveness invariants (features 116/120) still hold.
- **FR-004**: The renderer MUST paint transient/overlay surfaces last (z-top) at their true coordinates so an
  open menu/combo/auto-complete/date-picker surface floats above in-flow siblings and is NOT clipped by an
  ancestor container.
- **FR-005**: The overlay pass MUST be reproduced identically in the incremental retained path so full ≡
  retained parity holds with overlays present, and a page with no open transient surface MUST render
  byte-identically to the pre-overlay in-flow pass.
- **FR-006**: Hit-testing (`nearestAuthored`/`hitTest`) MUST consult the overlay group before in-flow so the
  topmost overlay wins a pointer hit in an overlapping region.
- **FR-007**: Items within a transient surface MUST occupy distinct y-bands (no two share a baseline / no
  overprint).
- **FR-008**: `ScrollViewer` MUST be a real clipping viewport: it clips content to its box, exposes a scroll
  offset and a scroll affordance, and clips (makes scrollable) content taller than the viewport instead of
  spilling it.
- **FR-009**: Every fix MUST be theme-invariant (identical behavior under antLight and antDark) and MUST
  preserve the byte-identical same-seed determinism established in 136.
- **FR-010**: All renderer output changes MUST be treated as intended correctness fixes: affected G1/G2 golden
  evidence, the rendered-output drift gate, and any affected surface-area baselines MUST be re-established and
  disclosed in the rebaseline ledger, never silently overwritten.
- **FR-011**: The 19 showcase pages MUST be re-captured (both themes) and confirmed free of all seven defect
  classes; where no display is available a disclosed no-GL degrade MUST be recorded (no fabricated pass).
- **FR-012**: Fixes MUST be at the framework layer where the defect is owned (renderer/control/layout); only
  genuinely compositional chrome-region sizing may live in the sample `Shell.fs`.

### Key Entities *(include if feature involves data)*

- **Container clip composition**: the single shared rule that combines a node's own paint with its children's
  scenes, clipping children to the node's box — used by both the full and retained paint paths so they cannot
  diverge.
- **Overlay group**: the ordered set of transient/overlay nodes painted after (above) the in-flow group, at
  true coordinates, outside ancestor clips; consulted first by hit-testing.
- **Clip-aware picture-cache key**: the cache fingerprint/identity extended so a cached subtree's reuse is
  valid (and its effectiveness measured correctly) when it is nested inside a container clip.
- **ScrollViewer viewport**: a clipping region plus a scroll offset and affordance; content beyond the box is
  clipped, not spilled.
- **Rebaseline ledger row**: one disclosed record per changed baseline (id, defect/fix cause, before/after,
  intended-confirmation).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: With container clipping enabled, the three picture-cache parity tests that failed in 136
  (`cache-on ≡ cache-off` byte-identical, present-but-dead, picture-cache effectiveness) pass, and the full
  Controls/Scene/SkiaViewer/Layout suites remain green (zero regressions).
- **SC-002**: On all 19 showcase pages in both themes, no child control's drawn area exceeds its container's
  bounds (zero spill).
- **SC-003**: Every open transient surface on the showcase paints above its in-flow neighbours with distinct,
  non-overprinting items, and a hit on an overlapping region resolves to the overlay.
- **SC-004**: A page taller than its content region paints nothing outside the region; content beyond the
  viewport is clipped and reachable via the scroll affordance.
- **SC-005**: The full render and the incremental retained render produce byte-identical output on every
  showcase page with clipping and overlays present (full ≡ retained parity preserved).
- **SC-006**: Two same-seed headless re-captures are byte-identical (determinism preserved).
- **SC-007**: Re-capturing the 19 pages shows zero instances of any of the seven defect classes; every changed
  golden/drift/surface baseline has a disclosed rebaseline-ledger row (no silent overwrite).

## Assumptions

- The blocker is exactly as recorded in 136: clipping cached subtrees breaks the feature-116/120 picture-cache
  parity; the fix is to make that cache logic clip-aware, not to abandon clipping.
- The feature-135 19-page evidence harness is reused as the verification vehicle (no new evidence mechanism).
- Transient-surface controls (combo/menu/auto-complete/date-/time-picker) build on the existing `Overlay`
  container; the overlay pass extends the existing render/hit-test paths rather than introducing a new tree.
- GL screenshot capture may be unavailable on the build host; headless deterministic paths (parity, clipping,
  overlay ordering, hit-test, scroll geometry) run anywhere, and a no-GL re-capture degrades with disclosure.
- The pre-existing uncommitted `samples/AntShowcase/Shell.fs` edits are in-progress sample work; this feature
  owns only the framework changes plus, if needed, the compositional chrome-region sizing.
- This is a **Tier 1** change (intended shared-control/renderer output changes); baselines are re-established
  and disclosed, never silently overwritten.
