# Contract: Interaction Overlay State

## Scope

This contract defines the planned external and cross-module behavior for Feature 143. Exact F# names may be
adjusted during `.fsi`-first implementation, but the semantics below are required unless the plan is amended.

## Public/Package Surface

Any public overlay contract must live under the existing Controls package and be declared in `.fsi` before
implementation. The expected shape is:

```fsharp
type TransientSurfaceKind =
    | Menu
    | ContextMenu
    | SplitButtonMenu
    | ComboDropdown
    | AutoCompleteSuggestions
    | DatePickerCalendar
    | ColorPickerPalette
    | DialogModal

type DismissalReason =
    | Escape
    | OutsidePointer
    | SelectionCompletion
    | ExplicitClose
    | AnchorRemoved
    | ProductClosed

type OverlayMsg
type OverlayState
type OverlayEffect

module OverlayState =
    val init: unit -> OverlayState
    val update: msg: OverlayMsg -> state: OverlayState -> OverlayState * OverlayEffect list
```

If the implementation keeps the coordinator internal, the same semantics still apply to internal `.fsi`
contracts and must be covered by semantic tests through the public runtime and control APIs.

## Authoring Compatibility

- Existing product-owned `IsOpen` and value props remain valid.
- Products receive explicit open/close/selection/focus messages or effects; runtime interaction does not
  silently change product state.
- Closed transient controls preserve their current visible output, hit-test behavior, diagnostics, and public
  authoring behavior unless a compatibility note documents an intentional change.
- Public surface changes require migration guidance, versioning rationale, and refreshed surface baselines.

## Supported Surface Contract

Feature readiness requires representative coverage for:

```text
menu
context menu
split-button menu
combo-style dropdown
auto-complete suggestions
date-picker calendar
color-picker palette
dialog-like modal overlay
```

Each supported surface must declare or derive:

- a stable trigger identity
- surface kind
- anchor identity or anchor bounds source
- dismissal policy
- focus scope behavior
- layer priority
- product dispatch mapping

## Input Routing Contract

For each pointer or keyboard input while a surface is open:

1. Resolve current overlay stack and layer order for the active frame.
2. Select the topmost eligible surface or determine that the input is outside it.
3. Apply modal blocking before lower content dispatch.
4. Apply dismissal policy before lower content dispatch.
5. Route keyboard navigation inside the focused surface scope when applicable.
6. Emit product messages exactly once for completed selection or command actions.
7. Record deterministic hit, focus, overlay, dismissal, dispatch, and diagnostic evidence.

Lower content receives the original input only when the topmost policy allows pass-through.

## Focus Contract

- Opening an interactive surface moves focus to a valid focus target in that surface or records why it cannot.
- Dismissing a surface recovers focus to the trigger, parent surface, or another documented target.
- Modal surfaces trap Tab and Shift+Tab inside their focus scope.
- Non-interactive tooltip/toast-like surfaces do not trap focus or steal key input unless promoted to
  interactive behavior.
- Stale focus targets fail safely and emit diagnostics.

## Dismissal Contract

Dismissal reasons are ordered and topmost-first:

- Escape
- outside pointer action
- selection completion
- explicit close action
- anchor removal
- product-driven close

Only the topmost eligible surface dismisses for a single Escape or outside pointer action. Blocked dismissal
attempts remain observable through diagnostics when diagnostics are requested.

## Anchor And Placement Contract

- Surfaces are anchored to their trigger or declared anchor using resolved current-frame bounds.
- Anchor movement updates placement on the next frame.
- Anchor removal closes the surface or records a safe blocked/diagnostic outcome according to policy.
- Viewport-edge no-fit cases disclose placement limitations without leaving stale hit targets.

## Rendering And Hit-Test Contract

- Open overlays render through the existing layer/portal behavior and appear above in-flow content.
- Hit-test priority follows the same topmost ordering as paint/layer evidence.
- Direct rendering, first-frame retained rendering, warm retained rendering, cache-enabled mode, and
  cache-disabled mode produce equivalent visible output and interaction evidence for equivalent overlay states.

## Replay Evidence Contract

Interaction replay logs must include:

- ordered input events
- overlay state transitions
- focus transitions
- product dispatches
- dismissal reasons and outcomes
- diagnostics
- topmost hit target decisions
- direct/retained/cache parity references

Equivalent scripts replayed three times must produce byte-identical logs, product messages, diagnostics, focus
evidence, and topmost hit decisions.

## Reference Date Picker Contract

The AntShowcase date-picker reference flow must prove:

1. initial closed render contains no overlay content
2. trigger activation opens the calendar near the trigger
3. keyboard or pointer navigation reaches a date
4. selecting a date dispatches exactly one product selection
5. the calendar closes
6. focus recovers to the trigger or field
7. post-dismissal render has no stale visible or hit-testable overlay content
8. readiness evidence names any public surface, baseline, diagnostic, or validation limitation
