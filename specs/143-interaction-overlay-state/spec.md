# Feature Specification: Interaction Overlay State

**Feature Branch**: `143-interaction-overlay-state`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report says P0, P1, P2, P3, and P4 are implemented through Feature
142. The next unstarted roadmap item is P5: Interaction, also described as R4 real interaction and
overlay state. This feature starts that phase.

P5 should turn transient surfaces from static or product-preopened drawings into predictable
interactive UI behavior. Menus, context menus, combo-style dropdowns, split-button menus,
auto-complete suggestions, date-picker calendars, color-pickers, and dialog-like overlays should
open, close, receive focus, dismiss, and layer consistently. End users should be able to operate
those surfaces by pointer and keyboard; product authors should receive deterministic selection and
open-state messages; maintainers should get evidence that overlays still paint and hit-test above
ordinary content through the existing portal and layer foundation.

This is a Tier 1 interaction and runtime feature because it changes observable control behavior,
runtime state ownership, focus routing, keyboard routing, pointer routing, diagnostics, and likely
public package contracts. It must preserve closed-state rendering compatibility unless an explicit
compatibility decision documents otherwise. The feature must stay bounded to interaction overlay
state and must not take on portable scene serialization, compositor promotion, damage-scissored
presentation, intrinsic layout, new text shaping behavior, or a complete widget redesign.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Open and Dismiss Anchored Transient Surfaces (Priority: P1)

An application user activates a menu, split button, combo-style dropdown, auto-complete field,
date-picker calendar, or color picker. The transient surface opens near its trigger, appears above
normal content, receives pointer hits before covered content, and closes through the expected
dismissal action.

**Why this priority**: This is the core P5 value. The report identifies current transient surfaces
as rendered schematics whose behavioral half is missing. A surface that cannot reliably open,
dismiss, and receive hits is not usable.

**Independent Test**: Run scripted pointer and keyboard interactions against representative
transient controls. Verify open state, anchored placement, layer order, hit-test priority,
dismissal behavior, and closed-state compatibility for each control.

**Acceptance Scenarios**:

1. **Given** a closed date-picker trigger, **When** the user activates it, **Then** its calendar
   opens near the trigger, appears above in-flow content, and receives pointer hits before covered
   content.
2. **Given** an open menu or dropdown, **When** the user clicks outside it, **Then** it closes if
   its dismissal policy allows outside dismissal and focus returns to a valid target.
3. **Given** an open transient surface, **When** the user presses Escape, **Then** only the topmost
   dismissible surface closes and lower content does not also handle the same key.
4. **Given** a closed transient control, **When** it renders without user interaction, **Then** the
   closed output remains compatible with the pre-feature closed output.

---

### User Story 2 - Operate Open Surfaces With Keyboard and Focus (Priority: P1)

An application user opens a transient surface and uses keyboard navigation to move within it,
select an item or date, cancel it, and return to the trigger without losing focus or dispatching
duplicate messages.

**Why this priority**: Pointer-only overlays exclude keyboard users and undermine the existing
focus-routing work. P5 must make open surfaces genuinely operable rather than just visible.

**Independent Test**: Exercise keyboard scripts for opening, traversing, selecting, cancelling,
and re-opening each supported interactive surface. Verify focus scope, selection, dispatch count,
and recovery after dismissal.

**Acceptance Scenarios**:

1. **Given** a closed split-button menu, **When** the user opens it with the keyboard and moves
   through entries, **Then** focus stays within the menu until selection or dismissal.
2. **Given** an open date-picker calendar, **When** the user navigates days and confirms a date,
   **Then** exactly one date selection is emitted and the calendar closes.
3. **Given** an open auto-complete suggestion list, **When** the user moves selection and confirms,
   **Then** the selected suggestion is emitted exactly once and focus returns to the text field.
4. **Given** focus points to an element that disappears while a surface is open, **When** the next
   interaction occurs, **Then** focus recovers to a valid target or clears with actionable evidence.

---

### User Story 3 - Enforce Modal Focus and Dismissal Rules (Priority: P1)

An application user opens a dialog-like overlay or other modal transient surface. While it is open,
keyboard traversal and pointer interaction are constrained to the modal scope unless the user
dismisses or completes the modal workflow.

**Why this priority**: Some overlay surfaces are not merely dropdowns; they must prevent accidental
interaction with covered content. The interaction model needs explicit modal semantics before
application authors can rely on it.

**Independent Test**: Open modal and non-modal overlays side by side in scripted scenarios. Verify
that modal overlays trap focus and block lower-layer interaction, while non-modal overlays use their
configured dismissal policy.

**Acceptance Scenarios**:

1. **Given** a modal overlay is open, **When** the user presses Tab or Shift+Tab, **Then** focus
   cycles within the modal scope and never moves to covered content.
2. **Given** a modal overlay is open, **When** the user clicks covered content, **Then** the covered
   content does not receive the interaction unless the modal dismisses first by policy.
3. **Given** nested transient surfaces, **When** the user dismisses the topmost surface, **Then**
   focus returns to the surface or trigger that opened it without closing unrelated surfaces.
4. **Given** a non-modal tooltip or toast-like surface, **When** it is present, **Then** it does not
   trap focus unless it has explicitly become an interactive surface.

---

### User Story 4 - Keep Overlay State Deterministic and Auditable (Priority: P1)

A framework maintainer validates overlay interactions across direct rendering, retained rendering,
keyboard input, pointer input, cache-enabled mode, and cache-disabled mode. The same scripted
interaction produces the same open-state transitions, focus transitions, dispatched product
messages, diagnostics, paint order, and hit-test order across runs.

**Why this priority**: Interaction state is easy to make nondeterministic. The previous roadmap
phases made parity and deterministic evidence core contracts; P5 must join those contracts rather
than becoming a new source of hidden drift.

**Independent Test**: Replay deterministic interaction scripts for representative surfaces across
direct, cold retained, warm retained, cache-enabled, and cache-disabled paths. Compare state logs,
product messages, focus evidence, diagnostics, hit-test evidence, and visible output.

**Acceptance Scenarios**:

1. **Given** a scripted open-select-dismiss sequence, **When** it is replayed three times, **Then**
   the state log, product messages, focus evidence, and diagnostics are identical.
2. **Given** an open overlay above retained content, **When** direct rendering and retained
   rendering are compared, **Then** paint order, hit-test order, and visible output are equivalent.
3. **Given** cache-enabled and cache-disabled modes, **When** the same overlay script runs, **Then**
   user-visible behavior and state evidence remain equivalent.
4. **Given** a validation limitation or pre-existing failure is encountered, **When** readiness is
   recorded, **Then** it is distinguished from new P5 behavior.

---

### User Story 5 - Demonstrate the Reference Date Picker Flow (Priority: P2)

A product author runs the AntShowcase date-picker reference scenario and sees a complete
open-navigate-select-dismiss flow that can be used as the example for other generated products and
future widget work.

**Why this priority**: The report names the AntShowcase date-picker as the reference consumer. It
is a concrete end-to-end demonstration that the overlay manager, focus behavior, layer behavior,
and product message dispatch work together.

**Independent Test**: Run the reference date-picker script in showcase-level verification. Confirm
initial closed state, open state, calendar navigation, date selection, dismissal, focus recovery,
and evidence output.

**Acceptance Scenarios**:

1. **Given** the reference date-picker starts closed, **When** the scripted trigger action runs,
   **Then** the calendar opens and is visibly associated with the trigger.
2. **Given** the calendar is open, **When** the scripted user chooses a date, **Then** the product
   state receives that date once, the calendar closes, and focus returns to the trigger or field.
3. **Given** the showcase is rendered in closed state after the interaction, **When** it is compared
   with normal closed rendering, **Then** no stale overlay content remains visible or hit-testable.
4. **Given** the reference flow is complete, **When** maintainers review evidence, **Then** it names
   any public contract, visual baseline, or diagnostic changes required by P5.

### Edge Cases

- Disabled triggers should not open transient surfaces, and should disclose the ignored activation
  only through existing diagnostic channels when diagnostics are requested.
- Empty menus, empty suggestion lists, empty color palettes, and date pickers with no selected value
  should open or remain closed according to their documented control behavior without errors.
- Multiple open surfaces should obey topmost-first key and pointer handling; Escape and outside
  clicks should affect only the topmost eligible surface.
- Nested surfaces should restore focus to the parent surface or trigger after the child closes.
- A trigger or anchor that disappears while its surface is open should close the surface safely and
  prevent stale hit targets from remaining active.
- A trigger that moves because layout changes should move or re-anchor the surface on the next
  frame without producing stale placement evidence.
- Surfaces near the viewport edge should choose a stable placement that remains usable when there
  is enough available space, and should disclose when no fully fitting placement exists.
- Modal overlays should block lower-layer pointer and keyboard interaction until dismissed or
  completed by policy.
- Non-interactive tooltip or toast-like surfaces should not trap focus or steal key input.
- Repeated equivalent interaction scripts should produce stable state logs, product messages,
  diagnostics, fingerprints, and visible output.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a deterministic interaction state model for transient
  surfaces, including open or closed state, active surface identity, trigger identity, anchor
  evidence, layer priority, dismissal policy, and focus scope.
- **FR-002**: Supported interactive transient surfaces MUST include, at minimum, menu, context menu,
  split-button menu, combo-style dropdown, auto-complete suggestions, date-picker calendar,
  color-picker palette, and dialog-like modal overlays.
- **FR-003**: Users MUST be able to open supported transient surfaces through pointer activation
  and through the control's documented keyboard activation path when the control is enabled.
- **FR-004**: Open transient surfaces MUST render through the existing layered overlay behavior so
  they can appear above in-flow content and receive hit-tests before covered content.
- **FR-005**: Open transient surfaces MUST be anchored to their trigger or declared anchor, and the
  anchor relationship MUST update when the trigger's resolved position changes.
- **FR-006**: The topmost eligible transient surface MUST handle dismissal keys, outside pointer
  actions, and selection actions before lower layers or covered content receive the same input.
- **FR-007**: Dismissal behavior MUST support Escape, outside pointer action, selection completion,
  explicit close action, and anchor removal, each governed by the surface's dismissal policy.
- **FR-008**: Interactive surfaces MUST move focus into a valid focus target when opened and MUST
  return focus to the trigger, parent surface, or another valid recovery target when dismissed.
- **FR-009**: Modal surfaces MUST trap keyboard traversal within their focus scope and block covered
  content from receiving pointer or keyboard interactions unless dismissal policy permits dismissal
  first.
- **FR-010**: Keyboard navigation within open surfaces MUST be deterministic and MUST dispatch each
  completed selection or command exactly once.
- **FR-011**: Product-owned visibility and product message flows MUST remain compatible: a product
  can still represent open state explicitly, and runtime interaction must emit clear state-change or
  selection messages rather than silently changing product state.
- **FR-012**: Closed transient controls MUST preserve their existing visible output, hit-test
  behavior, diagnostics, and public authoring behavior unless an intentional compatibility change is
  documented.
- **FR-013**: Direct rendering, first-frame retained rendering, warm retained rendering,
  cache-enabled mode, and cache-disabled mode MUST produce equivalent visible output and interaction
  evidence for equivalent overlay states and scripts.
- **FR-014**: Interaction replay evidence MUST include the ordered input events, overlay state
  transitions, focus transitions, dispatched product messages, dismissal reasons, diagnostics, and
  topmost hit target decisions.
- **FR-015**: Missing anchors, stale focus targets, blocked dismissals, disabled triggers, and
  no-fit placement cases MUST fail safely with actionable diagnostics rather than stale visible or
  hit-testable content.
- **FR-016**: The AntShowcase date-picker MUST serve as a reference end-to-end flow covering open,
  keyboard or pointer navigation, selection, dismissal, focus recovery, and readiness evidence.
- **FR-017**: Public runtime, control, focus, pointer, keyboard, or widget contract changes MUST
  include compatibility impact, migration guidance, surface-baseline evidence, and versioning
  rationale.
- **FR-018**: Any intentional pixel, golden, diagnostic, or interaction-log baseline change MUST be
  recorded with the reason for the change before the feature is considered ready.
- **FR-019**: The feature MUST NOT implement portable scene serialization, browser rendering,
  compositor promotion, damage-scissored presentation, intrinsic layout, new text shaping behavior,
  text editing, selection editing, or a complete redesign of the widget catalog.
- **FR-020**: Verification limitations and pre-existing failures encountered during validation MUST
  be recorded so maintainers can distinguish them from P5 behavior.

### Key Entities

- **Transient surface**: A temporary UI surface such as a menu, dropdown, calendar, palette,
  suggestion list, or modal dialog that can appear above normal content.
- **Trigger**: The control or interaction source that opens a transient surface and usually receives
  focus back when the surface closes.
- **Overlay state**: The durable record of which transient surfaces are open, how they were opened,
  their anchors, dismissal policies, focus scopes, and layer priorities.
- **Anchor evidence**: The resolved relationship between a trigger or declared anchor and the
  surface placement used for painting and hit-testing.
- **Dismissal policy**: Rules that decide which events may close a surface and whether lower content
  may receive the event after dismissal.
- **Focus scope**: The set of focusable targets owned by an open surface, including modal trap
  behavior and recovery target.
- **Topmost hit target**: The surface or control that receives an input event after layered
  ordering and modal blocking rules are applied.
- **Interaction replay log**: Deterministic evidence of inputs, overlay-state transitions, focus
  transitions, product messages, diagnostics, and hit-test decisions.
- **Reference date-picker flow**: The showcase scenario used to prove an end-to-end interactive
  overlay workflow for generated products.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Focused overlay verification covers at least eight supported surface categories:
  menu, context menu, split-button menu, combo-style dropdown, auto-complete suggestions,
  date-picker calendar, color-picker palette, and dialog-like modal overlay.
- **SC-002**: 100% of supported enabled transient-surface fixtures open through pointer activation
  and through their documented keyboard activation path.
- **SC-003**: 100% of open surface fixtures render above covered in-flow content and receive
  topmost hit-tests before covered content.
- **SC-004**: 100% of dismissal fixtures close only the topmost eligible surface for Escape,
  outside pointer action, selection completion, explicit close action, and anchor removal.
- **SC-005**: 100% of focus recovery fixtures return focus to the trigger, parent surface, or a
  documented valid recovery target after dismissal.
- **SC-006**: 100% of modal fixtures keep keyboard traversal within the modal focus scope and block
  lower-layer pointer or keyboard interaction while the modal remains open.
- **SC-007**: Keyboard navigation verification covers at least 50 scripted interactions and reports
  zero duplicate product selections or commands.
- **SC-008**: The AntShowcase date-picker reference flow passes open, navigate, select, dismiss,
  focus-recovery, and no-stale-overlay checks in one deterministic script.
- **SC-009**: Direct, first-frame retained, and warm retained rendering match for at least 100
  overlay-state fixture or generated scenes.
- **SC-010**: Cache-enabled and cache-disabled verification reports zero visible-output,
  hit-target, focus-transition, or product-message differences for equivalent overlay scripts.
- **SC-011**: Replaying the same overlay script three consecutive times produces byte-identical
  interaction logs, diagnostics, product messages, and focus evidence.
- **SC-012**: Closed-state compatibility verification reports either zero changed closed-state
  baselines or 100% documented changes with rationale and migration guidance.
- **SC-013**: Public surface verification reports either zero public contract changes or 100%
  documented changes with migration guidance and versioning rationale.
- **SC-014**: Scope review confirms zero implementation tasks for portable serialization, browser
  rendering, compositor promotion, damage-scissored presentation, intrinsic layout, new text
  shaping behavior, text editing, selection editing, or full widget-catalog redesign.

## Assumptions

- Feature 140 provides the internal portal and layer foundation needed for overlay painting and
  hit-test ordering.
- Feature 141 provides retained renderer unification, so overlay interaction state should be
  validated through direct, cold retained, and warm retained paths.
- Feature 142 provides text-shaping behavior that this feature must preserve but not expand.
- The first P5 slice targets one active application window and one active focus chain at a time;
  multi-window coordination is out of scope.
- Existing typed controls with `IsOpen`-style product-owned state remain compatible. This feature
  adds deterministic interaction messages and runtime evidence around that state rather than
  silently taking ownership away from products.
- Tooltip and toast-like surfaces are treated as non-interactive unless authored or promoted as
  interactive surfaces.
- Accessibility semantics already present on controls remain the source of focusability and
  keyboard-role expectations; this feature extends transient-surface behavior around those
  semantics.
