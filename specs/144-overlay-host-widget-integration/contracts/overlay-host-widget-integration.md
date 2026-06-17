# Contract: Overlay Host Widget Integration

## Scope

This contract defines the required external and cross-module behavior for Feature 144. Exact F# names may be
adjusted during `.fsi`-first implementation, but the semantics below are required unless this plan is amended.

## Existing Coordinator Contract

Feature 144 consumes the existing `FS.GG.UI.Controls.OverlayState` contract from Feature 143:

```fsharp
type OverlayState
type OverlayMsg
type OverlayEffect
type OverlaySurface
type TransientSurfaceKind

module OverlayState =
    val init: unit -> OverlayState
    val update: msg: OverlayMsg -> state: OverlayState -> OverlayState * OverlayEffect list
```

The coordinator remains pure, total, host-independent, and replayable. Feature 144 must not move native viewer
I/O or product-model mutation into `OverlayState.update`.

## Transient Widget Metadata Contract

Each supported transient widget authoring path must expose or derive metadata that can be translated into an
`OverlaySurface` when product-owned state says the surface is open or activation requests opening.

Required metadata:

- stable trigger identity
- stable surface identity
- `TransientSurfaceKind`
- trigger enabled/disabled state
- anchor identity and current-frame anchor evidence
- layer priority
- dismissal policy
- focus scope and recovery target
- modal flag
- product dispatch mapping for open, close, selection, command, or diagnostic requests

Supported categories:

```text
menu
context menu
split-button menu
combo dropdown
auto-complete suggestion list
date-picker calendar
color-picker palette
dialog-like modal overlay
```

Validation must fail when a supported widget lacks required metadata. Disabled triggers must not open surfaces or
emit product state-change/selection messages.

## Product-Owned Visibility Contract

- Product model state remains the source of truth for open/closed and selected/value state.
- Runtime interaction emits explicit product-visible requests such as open, close, selection, command, focus, or
  diagnostic effects.
- The host must not silently mutate product-owned state or rewrite widget attributes.
- Existing closed authoring behavior remains compatible unless a readiness note documents an intentional change.

## Pointer Routing Contract

For every pointer sample or folded pointer interaction while an overlay is active:

1. Resolve the current overlay stack and layer order for the frame.
2. Determine the topmost eligible target, outside-surface condition, or modal blocker.
3. Apply outside-pointer dismissal policy before lower content dispatch.
4. Emit overlay effects, runtime messages, product messages, and diagnostics in deterministic order.
5. Deliver the original pointer interaction to lower content only when policy allows pass-through.

Hit-test order must match paint/layer order. Direct routing and retained routing must produce equivalent product
messages and evidence for equivalent frames.

## Keyboard Routing Contract

Keyboard input is offered in this order:

1. Existing focused text delivery when applicable.
2. Overlay-aware Escape, traversal, activation, navigation, and selection routing for the active/focused surface.
3. Existing focused-control routing.
4. Host `MapKey` fallback when the input is not consumed.

Escape dismisses only the topmost eligible surface. Modal scopes trap Tab and Shift+Tab. Selection or command
dispatch occurs exactly once for each completed user action.

## Focus Contract

- Opening an interactive surface requests initial focus inside the surface when one is available.
- Dismissal recovers focus to the trigger, parent surface, documented fallback, or explicit no-focus state.
- Modal surfaces cycle traversal inside their focus scope and block covered content.
- Non-interactive tooltip/toast-like surfaces do not capture focus unless promoted to an interactive surface.
- Stale focus targets fail safely and emit diagnostics.

## AntShowcase Reference Flow Contract

The AntShowcase date-picker flow must prove:

1. initial closed render contains no calendar overlay content
2. trigger activation requests product-owned open state
3. calendar opens anchored to the trigger or field
4. navigation reaches a valid date
5. selection emits exactly one date selection
6. close request follows selection when policy requires it
7. focus recovers to the trigger or field
8. final closed render has no stale visible or hit-testable calendar content
9. evidence records replay log, focus transitions, product messages, diagnostics, and visual proof or host limitation

## Rendering, Replay, And Evidence Contract

- Direct rendering, first-frame retained rendering, warm retained rendering, cache-enabled mode, and cache-disabled
  mode must produce equivalent visible output, hit order, focus transitions, product messages, and diagnostics for
  equivalent overlay scripts.
- Representative scripts replayed three consecutive times must produce byte-identical logs.
- Offscreen visual validation must record a real proof artifact when the host supports it.
- Unsupported host limitations must name the environment, owner, cause, next proof path, and why behavioral
  evidence remains trustworthy.

## Compatibility Contract

Public contract changes require:

- `.fsi`-first design and semantic tests
- updated surface baselines
- compatibility impact notes
- migration guidance
- versioning rationale
- readiness evidence links

Closed-state compatibility validation must report zero unexpected visual, hit-test, or diagnostic changes.

## Exclusions

This feature must not implement portable scene serialization, browser rendering, compositor promotion,
damage-scissored presentation, intrinsic layout, new text shaping behavior, text editing, selection editing, or a
widget-catalog redesign.
