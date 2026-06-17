# Research: Interaction Overlay State

## Decision: Model overlay interaction as pure MVU data in Controls

**Rationale**: Overlay behavior is stateful user workflow: open, close, focus entry, focus recovery, keyboard
navigation, modal trapping, pointer dismissal, selection completion, and diagnostics. The constitution requires
stateful workflows to expose a model/message/effect boundary. Keeping the model pure makes scripted replay,
direct/retained comparison, cache-enabled/cache-disabled comparison, and byte-identical evidence feasible.

**Alternatives considered**:
- Host-private mutable overlay stack. Rejected because it hides state transitions from tests and product
  messages.
- Product-only `IsOpen` state. Rejected because it preserves authoring but does not solve topmost routing,
  focus trapping, outside dismissal, anchor loss, or deterministic evidence.

## Decision: Keep product-owned visibility compatible and emit explicit state-change messages

**Rationale**: Existing typed controls already represent open/value state through product-owned props such as
`IsOpen` and callbacks. The runtime should coordinate interaction and emit state-change or selection messages;
it must not silently rewrite product state. This preserves closed-state compatibility and lets product authors
keep normal Elmish ownership.

**Alternatives considered**:
- Runtime owns all open state directly. Rejected because it breaks existing authoring expectations and obscures
  the product's source of truth.
- No new runtime messages. Rejected because keyboard/pointer dismissal would remain non-deterministic from the
  framework's perspective.

## Decision: Add an overlay coordinator rather than folding all behavior into Pointer or Focus

**Rationale**: Pointer and Focus already solve focused pieces of the problem. Overlay behavior crosses them:
topmost hit target, outside dismissal, modal blocking, focus scopes, selection completion, layer priority, and
anchor validity. A coordinator can call into existing Pointer/Focus helpers while keeping overlay policy in one
auditable place.

**Alternatives considered**:
- Pointer-owned overlay dismissal. Rejected because keyboard and modal focus rules would be split elsewhere.
- Focus-owned overlay stack. Rejected because hit-test priority and outside pointer policy are not focus-order
  concerns.

## Decision: Supported surface kinds are explicit and finite for the first slice

**Rationale**: The spec requires at least menu, context menu, split-button menu, combo-style dropdown,
auto-complete suggestions, date-picker calendar, color-picker palette, and dialog-like modal overlay. A closed
DU or equivalent finite contract makes test coverage measurable and prevents tooltip/toast behavior from
accidentally becoming focus-trapping interaction.

**Alternatives considered**:
- Free-form string surface kinds. Rejected for the first slice because dispatch, diagnostics, and coverage would
  be weaker.
- Complete Ant widget redesign. Rejected as out of scope.

## Decision: Anchor evidence comes from resolved layout bounds and Feature 140 layer/portal behavior

**Rationale**: Feature 140 provides the layer/portal foundation, and existing render results already expose
control bounds and retained hit testing. Anchoring should use resolved trigger or declared-anchor bounds from
the current frame, then produce explicit diagnostics for missing anchors, moved anchors, or no-fit placement.

**Alternatives considered**:
- Add an intrinsic layout protocol first. Rejected because intrinsic layout is explicitly out of scope for this
  feature.
- Hard-code overlay coordinates in individual controls. Rejected because it would create stale placement and
  duplicate hit-test behavior.

## Decision: Route input topmost-first from the same ordered overlay evidence used for painting

**Rationale**: Open overlays must receive hits before covered content, and Escape/outside pointer actions must
affect only the topmost eligible surface. The order used to paint layers and the order used to route hit targets
must be derived from the same frame evidence to preserve deterministic direct/retained parity.

**Alternatives considered**:
- Separate pointer-only z-order logic. Rejected because it can drift from paint order.
- Broadcast Escape or outside clicks to every open surface. Rejected because the spec requires topmost-only
  dismissal.

## Decision: Focus scope is overlay-local for interactive overlays and modal-trapping for modal overlays

**Rationale**: Existing focus ordering is pure and deterministic. Overlay focus should narrow traversal to the
surface scope while open, recover to the trigger or parent when dismissed, and trap traversal inside modal
surfaces. Non-interactive tooltip/toast-like surfaces should not enter the focus scope unless explicitly promoted
to interactive.

**Alternatives considered**:
- Global tab order while overlays are open. Rejected because it allows focus to leak behind modal and menu
  surfaces.
- Always trap focus for any overlay node. Rejected because tooltips/toasts should not steal keyboard control.

## Decision: Dismissal policy is a data contract, not per-control branching

**Rationale**: Escape, outside pointer action, selection completion, explicit close, and anchor removal must be
governed by policy. A policy value lets the runtime report why a surface dismissed or why it stayed open, and
keeps nested/modal behavior auditable.

**Alternatives considered**:
- Per-widget hard-coded dismissal code. Rejected because it duplicates policy and complicates nested surfaces.
- Always dismiss on outside pointer. Rejected because modal or guarded workflows may block outside dismissal.

## Decision: Interaction replay evidence is a first-class validation artifact

**Rationale**: The success criteria require repeated scripts to produce byte-identical state logs, product
messages, diagnostics, focus evidence, and hit-test decisions. The feature should define a stable replay record
that captures input events, overlay transitions, focus transitions, dismissal reasons, product dispatches,
diagnostics, and topmost hit targets.

**Alternatives considered**:
- Rely only on rendered pixel or golden checks. Rejected because interaction bugs often do not show up in
  closed-state pixels.
- Use ad hoc test logs. Rejected because parity and readiness need comparable evidence.

## Decision: No new runtime dependency

**Rationale**: The feature can use existing F# data structures, Expecto tests, retained render evidence, and
current Controls/Elmish/KeyboardInput packages. Adding a dependency would need versioning and maintenance
rationale without solving the core model/routing problem.

**Alternatives considered**:
- Importing a UI focus/overlay library. Rejected because this is a framework-runtime concern tied to existing
  control IDs, retained hit testing, and layer evidence.

## Decision: AntShowcase date picker is the reference flow

**Rationale**: The roadmap names the AntShowcase date-picker as the concrete end-to-end consumer. It exercises
open, anchored placement, calendar focus/navigation, selection dispatch, dismissal, focus recovery, and no stale
overlay content after closing.

**Alternatives considered**:
- Use a generic menu only. Rejected because menus prove less of the date-picker/calendar and product-message
  path.
- Build a new showcase app first. Rejected because the existing generated-product/showcase route is the intended
  consumer evidence.
