# Feature Specification: Fix Non-Functional Controls in the Second Ant Showcase

**Feature Branch**: `175-fix-showcase-controls`

**Created**: 2026-06-20

**Status**: Draft

**Change Classification**: Tier 1 (alters observable interactive behavior of shared controls and the second showcase sample; any hover/focus/scroll state changes that reach `FS.GG.UI.*` control surface must update `.fsi` and surface baselines. If the fix is confined to sample wiring with no control-surface change, it downgrades to Tier 2 at planning time.)

**Input**: User description: "in the second antd showcase many controls are not functional like scrollbar. there is no focus on hover... go through all controls and fix problems you encounter"

## Overview

The second Ant showcase (the `SecondAntShowcase` sample) presents the full control
catalog as a live Ant Design experience. While the showcase renders correctly and
its scripted/headless interaction coverage passes, a person actually running the
sample finds that many controls do not respond to real pointer and keyboard input:
the content-region scrollbar does not scroll, controls give no hover or focus
feedback when the pointer moves over them or focus lands on them, and several
interactive controls do not visibly change when clicked, dragged, typed into, or
keyboard-activated.

This feature is a systematic pass over **every** control in the second showcase to
make the live, hand-driven experience match what the showcase claims: each control
that is supposed to be interactive must respond to genuine user input with visible
feedback, the scrollable content region must scroll, and hover/focus affordances
must appear. Controls that are intentionally display-only must remain clearly
non-interactive. Every problem encountered during the pass is recorded, fixed, and
re-verified.

The scope is corrective: it does not add new controls or new pages. It closes the
gap between "passes scripted coverage" and "works when a person uses it."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Scroll the content region with real input (Priority: P1)

A person runs the second showcase, opens a page whose content is taller than the
visible content region, and scrolls — by dragging the scrollbar, by wheel, and by
keyboard. The content moves and the scrollbar thumb tracks the position. When a page
fits within the region, no misleading scroll affordance is shown.

**Why this priority**: The user named the scrollbar as the first broken control, and
an unscrollable content region makes the lower part of dense pages unreachable. A
showcase you cannot scroll cannot demonstrate the controls below the fold.

**Independent Test**: Open a page taller than the content region, drag the scrollbar
thumb, use the wheel, and use keyboard scrolling; confirm the content offset and
thumb position change together and that the previously hidden content becomes
reachable.

**Acceptance Scenarios**:

1. **Given** a page whose content exceeds the visible content-region height, **When**
   the user drags the scrollbar thumb, **Then** the content scrolls and the thumb
   position reflects the new scroll offset.
2. **Given** the pointer is over the scrollable content region, **When** the user
   scrolls with the wheel, **Then** the content scrolls by a consistent increment and
   stops at the top and bottom bounds.
3. **Given** focus is in the scrollable content region, **When** the user presses
   standard scroll keys, **Then** the content scrolls accordingly.
4. **Given** a page whose content fits within the content region, **When** the page is
   shown, **Then** no active scrollbar thumb is presented as draggable beyond the
   bounds.

---

### User Story 2 - See hover and focus feedback on interactive controls (Priority: P1)

A person moves the pointer over interactive controls and tabs focus through them.
Each interactive control shows the Ant hover affordance when the pointer is over it
and a distinct focus affordance when it holds keyboard focus. Moving the pointer away
or focus elsewhere removes the affordance.

**Why this priority**: The user explicitly reported "there is no focus on hover."
Hover and focus feedback are core Ant Design states; without them the showcase fails
to demonstrate the design language and the controls feel dead under real input.

**Independent Test**: Move the pointer across each interactive control and tab focus
through the page; confirm a visible hover state on pointer-over, a visible and
distinct focus state on focus, and that both clear when the pointer or focus leaves.

**Acceptance Scenarios**:

1. **Given** an interactive control, **When** the pointer moves over it, **Then** the
   control shows its Ant hover state and reverts when the pointer leaves.
2. **Given** an interactive control, **When** it receives keyboard focus, **Then** it
   shows a focus affordance distinct from hover, and the affordance moves with focus.
3. **Given** a control is both hovered and focused, **When** both states apply, **Then**
   the combined Ant state is shown without one affordance hiding the other.
4. **Given** a display-only control, **When** the pointer moves over it, **Then** it
   does not present an interactive hover or focus affordance that implies it is
   actionable.

---

### User Story 3 - Every interactive control responds to genuine input (Priority: P1)

A person works through every page and exercises each interactive control by hand —
clicking buttons, toggling switches, typing into fields, dragging sliders, picking
dates, selecting options, opening overlays, paging data, submitting forms. Each
control produces the visible state change its contract promises. Each display-only
control is confirmed to be intentionally static.

**Why this priority**: The user asked to "go through all controls and fix problems."
This is the core deliverable: the live behavior of the whole catalog must match the
showcase's interaction contracts, not just its scripted replay.

**Independent Test**: For each control with an interaction contract, perform the
documented primary interaction with real input and confirm the promised visible
evidence appears; for each display-only control, confirm no interactive response is
expected or shown.

**Acceptance Scenarios**:

1. **Given** any control with an interaction contract, **When** the user performs its
   documented primary interaction with real pointer or keyboard input, **Then** the
   contract's expected state change and visible evidence occur.
2. **Given** a control that previously failed to respond under real input, **When** the
   fix is applied, **Then** it responds identically whether driven by hand or by the
   scripted coverage path.
3. **Given** a display-only control, **When** the user attempts to interact, **Then**
   it remains static and is identifiable as display-only, consistent with its recorded
   reason.
4. **Given** the full catalog, **When** the coverage check runs, **Then** every
   interactive control is accounted for as responsive and every display-only control
   is accounted for as static, with no control unclassified.

---

### User Story 4 - Each fixed control stays correct in light and dark Ant appearances (Priority: P2)

A reviewer confirms that the corrected interactions and the hover/focus affordances
look right in both Ant light and Ant dark appearances and do not introduce visual
regressions to spacing, color roles, or typography.

**Why this priority**: The second showcase's standing value is Ant fidelity in both
appearances. Interaction fixes must not trade working behavior for a visual
regression, and hover/focus colors are appearance-sensitive.

**Independent Test**: Produce the visual review set for the affected pages in both
appearances and confirm hover, focus, scroll, and active states use the correct Ant
palette roles with no clipping, overlap, or contrast loss.

**Acceptance Scenarios**:

1. **Given** a corrected control, **When** it is shown in Ant light and Ant dark,
   **Then** its hover, focus, and active states use the correct Ant palette roles in
   each appearance.
2. **Given** the affected pages, **When** the visual review set is inspected, **Then**
   no new spacing, alignment, clipping, or contrast regression is introduced by the
   fixes.

---

### Edge Cases

- What happens when content is exactly the height of the content region (no overflow,
  or a one-pixel overflow)? The scroll affordance must not flicker or present a
  draggable thumb when there is nothing to scroll.
- How does a control behave when hovered and focused at the same time, then the
  pointer leaves while focus remains? The focus affordance must persist.
- What happens at the scroll bounds — does wheel/drag/keyboard stop cleanly at top and
  bottom without overscroll artifacts?
- How are overlay surfaces (drawer, popover, dialog, tooltip, tour) dismissed under
  real input, and does focus return to a sensible place when they close?
- What happens to hover/focus state when the pointer leaves the window or the page
  changes while a control is hovered or focused?
- How do controls inside the scrollable region report hover/focus hit-testing after
  the region has been scrolled (the pointer-to-control mapping must account for scroll
  offset)?
- What happens at the accepted minimum window size where the content region is
  shortest and overflow is most common?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The second showcase's scrollable content region MUST scroll in response
  to real pointer drag on the scrollbar, pointer wheel over the region, and standard
  keyboard scroll input, with the scrollbar thumb position reflecting the current
  scroll offset.
- **FR-002**: The content region MUST clamp scrolling at its top and bottom bounds and
  MUST NOT present a draggable scroll affordance when content fits within the region.
- **FR-003**: Every interactive control MUST present an Ant hover state when the
  pointer is over it and clear that state when the pointer leaves.
- **FR-004**: Every interactive control MUST present an Ant focus affordance, distinct
  from hover, when it holds keyboard focus, and MUST move that affordance as focus
  moves.
- **FR-005**: When a control is simultaneously hovered and focused, the system MUST
  present the combined Ant state without either affordance suppressing the other.
- **FR-006**: Every control that has an interaction contract MUST produce that
  contract's expected state change and visible evidence when its documented primary
  interaction is performed with genuine pointer or keyboard input.
- **FR-007**: A control's live (hand-driven) behavior MUST match its scripted coverage
  behavior; a control MUST NOT pass scripted coverage while failing under real input.
- **FR-008**: Display-only controls MUST remain static under real input and MUST NOT
  present hover/focus affordances that imply they are interactive; each MUST stay
  consistent with its recorded display-only reason.
- **FR-009**: Hit-testing for hover, focus, and activation inside the scrollable region
  MUST account for the current scroll offset so the correct control responds after the
  region is scrolled.
- **FR-010**: The system MUST record each problem found during the control pass, the
  fix applied, and the re-verification result, so the catalog can be confirmed fully
  swept with no unresolved interaction defect.
- **FR-011**: Hover, focus, scroll, and active affordances MUST render with the correct
  Ant palette roles in both Ant light and Ant dark appearances, with no new visual
  regression to spacing, alignment, clipping, or contrast.
- **FR-012**: The coverage check MUST classify every control as either responsive-
  interactive or intentionally display-only, leaving no control unclassified, and MUST
  fail if an interactive control does not respond under the verification path.
- **FR-013**: Overlay-bearing controls (drawer, popover, popconfirm, tooltip, dialog,
  tour, context menu) MUST open and dismiss under real input, and on close focus MUST
  return to the control that opened the overlay (the trigger), or to the nearest
  focusable ancestor still present if the trigger no longer exists or is unfocusable.
- **FR-014**: The fixes MUST NOT remove any control, page, or existing passing
  behavior from the second showcase; the existing catalog and template pages remain
  intact.

### Key Entities

- **Control**: A catalog item in the showcase, classified as interactive (has an
  interaction contract) or display-only (has a recorded display-only reason). Carries a
  stable identity, the page it appears on, and its expected live behavior.
- **Interaction contract**: The recorded promise for an interactive control — its
  primary action, input kind, expected state change, and visible evidence — used as the
  acceptance bar for live behavior.
- **Interaction state**: The transient hover, focus, active, and combined states a
  control exposes in response to pointer and keyboard input, evaluated per Ant
  appearance.
- **Scroll state**: The content region's scroll offset, content extent, and thumb
  position; the source of truth for whether and how far the region can scroll.
- **Finding**: A recorded interaction defect discovered during the pass, with its fix
  and re-verification outcome, contributing to the zero-unresolved-defect bar.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On every page whose content overflows the content region, a person can
  scroll to the previously hidden bottom content using drag, wheel, and keyboard, and
  the thumb position matches the offset in 100% of cases.
- **SC-002**: 100% of interactive controls show a visible hover state on pointer-over
  and a distinct visible focus state on focus, and both clear when pointer/focus leaves.
- **SC-003**: 100% of controls that have an interaction contract produce the contract's
  expected visible evidence when driven by real input, with zero controls that respond
  only under scripted replay.
- **SC-004**: 100% of display-only controls remain static under real input with no
  interactive affordance, consistent with their recorded reasons.
- **SC-005**: The control-pass finding log reaches zero unresolved interaction defects
  before the feature is accepted.
- **SC-006**: The visual review set for affected pages shows correct Ant hover/focus/
  active palette roles in both light and dark appearances with zero new visual
  regressions.
- **SC-007**: The coverage check reports every control classified (interactive or
  display-only) with no unclassified control and no failing interactive control.

## Assumptions

- "The second antd showcase" refers to the `SecondAntShowcase` sample, the second of
  the two Ant showcases in the repository.
- The set of controls and pages is the current catalog; this feature corrects their
  live interactivity rather than adding new controls or pages.
- The existing interaction contracts and display-only reasons are the correct
  specification of intended behavior; controls are made to match them, and a contract is
  changed only if it is found to misstate the intended behavior.
- "Focus on hover" in the user's report means the absence of both pointer-hover
  feedback and keyboard-focus feedback; both are in scope as distinct Ant states.
- Some interaction defects may originate in shared `FS.GG.UI.*` control behavior rather
  than only in sample wiring; where a shared-control change is required it follows Tier 1
  obligations (`.fsi`, surface baselines, tests). The planning phase confirms whether the
  fix is sample-local (Tier 2) or reaches the shared control surface (Tier 1).
- Verification uses the repository's existing live-responsiveness and visual-readiness
  evidence paths; where a real interactive path is unavailable in the headless lane, the
  substitute is disclosed per the constitution's evidence rules.
- Both Ant light and Ant dark appearances and the accepted review sizes (including the
  minimum size) are the inspection baseline, consistent with the second showcase's
  existing review practice.
