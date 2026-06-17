# Feature Specification: Overlay Host Widget Integration

**Feature Branch**: `144-overlay-host-widget-integration`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report says P0 through P4 are implemented and P5 interaction work has
started through Feature 143. Feature 143 delivered the pure overlay coordinator, diagnostics,
replay evidence, focused tests, and partial readiness records. The report names the next action as
completing the remaining P5 host and widget integration before moving to P6 render-anywhere work.

This feature turns the Feature 143 coordinator slice into live transient-surface behavior across
supported controls and host routing paths. Menus, context menus, split-button menus, combo
dropdowns, auto-complete lists, date-picker calendars, color palettes, and modal overlays should
publish the metadata needed for runtime routing; pointer, keyboard, and focus routing should
interpret overlay decisions deterministically; product-owned visibility should remain explicit; and
the AntShowcase date-picker should become the reference live flow with evidence.

This is a Tier 1 interaction and package-contract feature because it changes observable control
behavior, input routing, focus routing, product message dispatch, compatibility guidance, validation
evidence, and possibly public contracts. It must stay bounded to P5 integration. It must not start
P6 portable scene serialization, browser rendering, compositor promotion, damage-scissored
presentation, intrinsic layout, new text shaping behavior, text editing, selection editing, or a
widget-catalog redesign.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Expose Transient Widget Behavior (Priority: P1)

A product author uses supported transient controls and expects each control to disclose enough
behavioral metadata for open, placement, dismissal, focus, and selection handling. The author keeps
ownership of product state while the framework provides deterministic interaction requests and
evidence.

**Why this priority**: The Feature 143 coordinator cannot deliver user value until controls identify
their transient surface, trigger, anchor, dismissal policy, focus scope, and selection behavior.
This is the minimum viable integration slice.

**Independent Test**: Inspect each supported transient control through public authoring paths and
verify that enabled controls expose valid surface metadata, disabled controls do not open, and
closed-state behavior remains compatible.

**Acceptance Scenarios**:

1. **Given** an enabled menu, split button, combo dropdown, auto-complete field, date-picker, color
   picker, or modal trigger, **When** the control is authored with product-owned visibility, **Then**
   it exposes a trigger, surface identity, anchor relationship, dismissal policy, and focus target
   that the runtime can route.
2. **Given** a disabled transient trigger, **When** activation is requested, **Then** no surface opens
   and the ignored activation is observable only through requested diagnostics.
3. **Given** a transient control is closed, **When** it is rendered before any interaction, **Then**
   its visible output and hit-test behavior remain compatible with the pre-integration closed state.
4. **Given** a supported control lacks required transient metadata, **When** validation runs, **Then**
   the missing metadata is reported as a readiness failure rather than silently falling back to stale
   behavior.

---

### User Story 2 - Route Pointer, Keyboard, and Focus Through Overlay State (Priority: P1)

An application user opens a transient surface and interacts by pointer or keyboard. Topmost surfaces
receive eligible input first, modal surfaces block covered content, focus enters and exits the
correct scope, and the product receives each selection or state-change request exactly once.

**Why this priority**: Visible overlays are not complete without input routing. This story connects
the coordinator to the user workflows that make transient surfaces operable.

**Independent Test**: Run scripted pointer and keyboard interactions against representative
surfaces and compare open state, focus state, dismissal reason, topmost hit target, and product
messages across repeated runs.

**Acceptance Scenarios**:

1. **Given** an open dropdown above covered content, **When** the user clicks inside the dropdown,
   **Then** the dropdown receives the hit before covered content.
2. **Given** an open menu, **When** the user presses Escape, **Then** only the topmost eligible
   surface dismisses and lower content does not also process the same key.
3. **Given** a modal overlay is open, **When** the user clicks or tabs toward covered content,
   **Then** covered content is blocked unless the modal policy dismisses first.
4. **Given** a surface emits a selection or command, **When** the interaction completes, **Then** the
   product receives exactly one corresponding message and focus recovers to the trigger, parent
   surface, or documented fallback target.

---

### User Story 3 - Preserve Product-Owned Visibility and Compatibility (Priority: P1)

A product author migrates existing controls that already carry explicit open or selected state. The
framework adds routing requests, diagnostics, and evidence without silently taking state ownership
away from the product or changing closed-state output.

**Why this priority**: Existing product code must remain predictable. The integration should make
interactive behavior observable, not hide new state mutations behind the runtime.

**Independent Test**: Run compatibility fixtures for existing explicit open-state controls and verify
that state-change requests are emitted as product-visible messages, closed-state output remains
compatible, and documented migration guidance covers any intentional contract changes.

**Acceptance Scenarios**:

1. **Given** a product owns a date-picker's open state, **When** the user activates the trigger,
   **Then** the runtime requests an open-state change through a product-visible message rather than
   mutating product state silently.
2. **Given** a product handles a selection message, **When** a transient item, date, or color is
   selected, **Then** the product receives exactly one selection message and one compatible close
   request when policy requires closing.
3. **Given** an intentional public contract or baseline change is needed, **When** readiness is
   reviewed, **Then** migration guidance, compatibility impact, and versioning rationale identify
   the change before the feature is considered complete.

---

### User Story 4 - Demonstrate the Live Reference Date Picker Flow (Priority: P2)

A generated-product maintainer runs the AntShowcase date-picker flow and sees a complete live
open, navigate, select, dismiss, focus-recover, and no-stale-overlay interaction with recorded
evidence.

**Why this priority**: The report identifies the date picker as the reference consumer. A live
showcase flow proves that widget metadata, host routing, product messages, focus recovery, and
evidence collection work together.

**Independent Test**: Run the reference date-picker scenario end to end and verify closed initial
state, open state, navigation, selection, dismissal, focus recovery, no stale overlay content, and
evidence output.

**Acceptance Scenarios**:

1. **Given** the reference date picker starts closed, **When** the scripted trigger action runs,
   **Then** the calendar opens in association with the trigger and is recorded in interaction
   evidence.
2. **Given** the calendar is open, **When** the scripted user navigates and selects a date, **Then**
   exactly one date selection reaches product state, the calendar closes, and focus returns to the
   trigger or field.
3. **Given** the flow has dismissed the calendar, **When** the final closed state is rendered and
   hit-tested, **Then** no stale calendar content remains visible or active.

---

### User Story 5 - Prove Integrated Overlay Rendering and Auditability (Priority: P2)

A framework maintainer validates that integrated overlays produce deterministic interaction logs,
compatible closed-state output, equivalent behavior across rendering and cache modes, and concrete
visual proof when the host environment supports it.

**Why this priority**: Feature 143 recorded partial evidence and an unsupported-host limitation.
Completing P5 requires replacing the remaining integration limitation with proof or a clearly owned
environment limitation.

**Independent Test**: Replay an overlay fixture corpus across direct rendering, retained rendering,
cache-enabled mode, cache-disabled mode, and the available offscreen visual path. Compare interaction
logs, focus evidence, product messages, diagnostics, visible output, and hit order.

**Acceptance Scenarios**:

1. **Given** a representative overlay script, **When** it is replayed three times, **Then** the
   interaction log, product messages, focus evidence, diagnostics, and hit decisions are byte
   identical.
2. **Given** equivalent overlay state across rendering modes, **When** validation compares visible
   output and hit order, **Then** no user-visible differences are reported.
3. **Given** the offscreen visual host is available, **When** overlay visual proof runs, **Then** it
   records a real artifact showing overlay order, hit order, and final closed state.
4. **Given** the offscreen visual host is unavailable, **When** readiness is recorded, **Then** the
   limitation names the environment, owner, next proof path, and why behavioral evidence is still
   trustworthy.

### Edge Cases

- Disabled transient triggers must never open a surface or emit product state-change requests.
- Empty menus, empty suggestions, empty palettes, and date pickers without a selected date must
  remain operable or safely closed according to their documented behavior.
- Multiple open surfaces must obey topmost-first handling for pointer, keyboard, dismissal, and
  selection events.
- A trigger or anchor that disappears while its surface is open must close or invalidate the surface
  safely without leaving stale visible or hit-testable content.
- A trigger that moves after layout changes must update placement evidence before the next eligible
  interaction.
- Missing focus targets must recover to a valid trigger, parent surface, documented fallback, or
  explicit no-focus state with diagnostics.
- Modal overlays must block covered content; non-interactive tooltip and toast-like surfaces must
  not trap focus or steal key input.
- Repeated equivalent scripts must produce stable logs, diagnostics, product messages, focus
  evidence, and visible output.
- Intentional public contract, diagnostic, baseline, or evidence changes must be documented before
  readiness.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Supported transient controls MUST disclose behavioral metadata for surface kind,
  trigger identity, anchor relationship, layer priority, dismissal policy, focus scope, and
  selection or command behavior.
- **FR-002**: Supported transient controls MUST include menu, context menu, split-button menu,
  combo dropdown, auto-complete suggestion list, date-picker calendar, color-picker palette, and
  dialog-like modal overlay.
- **FR-003**: Enabled transient controls MUST emit deterministic open, close, selection, and
  focus-routing requests through product-visible messages or evidence channels.
- **FR-004**: Disabled transient triggers MUST NOT open surfaces, change product state, or emit
  selection messages.
- **FR-005**: Pointer routing MUST send eligible events to the topmost surface before covered
  content, including outside-click dismissal and modal blocking rules.
- **FR-006**: Keyboard routing MUST support documented activation, traversal, selection,
  cancellation, Escape dismissal, and focus recovery for supported interactive surfaces.
- **FR-007**: Modal surfaces MUST trap traversal within their focus scope and block lower-layer
  pointer or keyboard interaction unless their dismissal policy permits dismissal first.
- **FR-008**: Product-owned visibility MUST remain explicit: runtime interaction MUST request
  product state changes rather than silently mutating product-owned open or selected state.
- **FR-009**: Completed selections or commands MUST dispatch exactly once per user action, including
  when the same event also closes a transient surface.
- **FR-010**: Anchor removal, missing anchors, stale focus targets, blocked dismissals, no-fit
  placement, disabled triggers, and duplicate dispatch attempts MUST fail safely with actionable
  diagnostics or readiness failures.
- **FR-011**: Direct rendering, first-frame retained rendering, warm retained rendering,
  cache-enabled mode, and cache-disabled mode MUST produce equivalent visible output, hit order,
  focus transitions, product messages, and diagnostics for equivalent overlay scripts.
- **FR-012**: The AntShowcase date-picker MUST provide a live reference flow covering open,
  navigation, selection, dismissal, focus recovery, final closed-state verification, and evidence
  output.
- **FR-013**: Offscreen visual validation MUST provide real overlay-order and closed-state proof
  when the host environment supports it; unsupported-host limitations MUST be recorded with owner,
  cause, and next proof path.
- **FR-014**: Closed transient controls MUST preserve existing visible output, hit-test behavior,
  diagnostics, and public authoring behavior unless an intentional compatibility change is
  documented.
- **FR-015**: Public contract changes MUST include compatibility impact, migration guidance,
  surface-baseline evidence, and versioning rationale.
- **FR-016**: The feature MUST NOT implement portable scene serialization, browser rendering,
  compositor promotion, damage-scissored presentation, intrinsic layout, new text shaping behavior,
  text editing, selection editing, or a full widget-catalog redesign.

### Key Entities

- **Transient control**: A control that can create or reveal a temporary interactive surface, such
  as a menu, dropdown, calendar, palette, suggestion list, or modal overlay.
- **Transient surface metadata**: The behavior description that identifies a surface kind, trigger,
  anchor, dismissal policy, focus scope, layer priority, and selection behavior.
- **Product-owned visibility**: Open or selected state that remains represented in application state
  and changes only through product-visible messages.
- **Routing decision**: The deterministic result of applying pointer, keyboard, focus, layer, and
  modal policy to an input event.
- **Focus recovery target**: The trigger, parent surface, fallback target, or explicit no-focus
  state used after dismissal or stale target removal.
- **Reference date-picker flow**: The end-to-end showcase scenario that proves live overlay
  integration for generated products.
- **Visual proof artifact**: Evidence from the available rendering host showing overlay order, hit
  order, and final closed-state behavior.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the eight supported transient surface categories expose complete behavioral
  metadata in validation fixtures.
- **SC-002**: 100% of enabled transient-control fixtures open through pointer activation and their
  documented keyboard activation path.
- **SC-003**: 100% of disabled trigger fixtures remain closed and emit zero product state-change or
  selection messages.
- **SC-004**: 100% of scripted topmost, outside-dismiss, Escape-dismiss, modal-blocking, and
  nested-surface scenarios route input to the expected target.
- **SC-005**: 100% of focus recovery fixtures return focus to the trigger, parent surface,
  documented fallback, or explicit no-focus state with diagnostics.
- **SC-006**: At least 50 keyboard interaction scripts complete with zero duplicate product
  selections or commands.
- **SC-007**: The reference date-picker flow passes open, navigate, select, dismiss, focus-recover,
  no-stale-overlay, and evidence-output checks in one deterministic script.
- **SC-008**: At least 100 overlay fixture or generated scenes report equivalent visible output,
  hit order, focus transitions, product messages, and diagnostics across direct, retained, and
  cache comparison modes.
- **SC-009**: Replaying each representative overlay script three consecutive times produces
  byte-identical interaction logs, diagnostics, product messages, focus evidence, and hit decisions.
- **SC-010**: Offscreen visual validation records at least one real proof artifact for overlay
  ordering and final closed state, or records 100% of unsupported-host limitations with owner,
  cause, and next proof path.
- **SC-011**: Closed-state compatibility validation reports zero unexpected visual, hit-test, or
  diagnostic changes.
- **SC-012**: Public surface validation reports either zero public contract changes or 100% documented
  contract changes with migration guidance and versioning rationale.
- **SC-013**: Scope review confirms zero implementation tasks for portable serialization, browser
  rendering, compositor promotion, damage-scissored presentation, intrinsic layout, new text shaping
  behavior, text editing, selection editing, or widget-catalog redesign.

## Assumptions

- Feature 143's pure overlay coordinator, diagnostics, replay evidence, and initial readiness
  records are available and remain the behavioral source for this integration.
- Feature 140's layer and portal ordering behavior is available for overlay paint and hit priority.
- Existing product-authored open state remains the source of truth for controls that already expose
  explicit visibility.
- This feature targets one active application window and one active focus chain at a time; multi-window
  overlay coordination is out of scope.
- Tooltip and toast-like surfaces remain non-interactive unless authored as interactive transient
  surfaces.
- Host environments may differ in visual proof capability; unsupported-host limitations must be
  documented rather than counted as passing visual proof.
