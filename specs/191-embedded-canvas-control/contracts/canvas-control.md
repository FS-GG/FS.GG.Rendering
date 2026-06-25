# Phase 1 Contracts: Embedded Canvas Control

**Feature**: 191-embedded-canvas-control | **Date**: 2026-06-25

These are the **public `.fsi` surfaces** introduced by this feature. Per Constitution I & II they are
authored and exercised in FSI **before** any `.fs` body, and per Tier-1 classification the surface-area
baselines are updated to match. Signatures are the design contract; exact spellings may be refined when
exercised in FSI, but the shape and visibility are fixed here.

## C1 — `SceneValue` attribute case (`src/Controls`, attribute value DU)

Added to the existing attribute value DU (declared in the relevant `.fsi`). Not a new `Control` field.

```fsharp
// in the AttrValue DU
| SceneValue of FS.GG.UI.Scene.Scene
```

Internal accessor consumed by `paintLeaf` (internal, reached by tests via `InternalsVisibleTo`):

```fsharp
// ControlInternals
val internal sceneAttr : Control<'msg> -> FS.GG.UI.Scene.Scene option
```

## C2 — `Canvas` constructor module (`src/Controls/Canvas.fsi`)

Mirrors the existing `Display`/`Buttons` constructor-module pattern. One semantic control kind; no
per-theme fork.

```fsharp
module Canvas =

    /// Author-supplied immutable scene, painted into the control's box.
    /// Authored in canvas-local coordinates: origin (0,0) top-left, y-down, logical units.
    val scene : FS.GG.UI.Scene.Scene -> Attr<'msg>

    /// Optional internal viewport transform (pan/zoom) applied to the content, not to layout size.
    val viewport : FS.GG.UI.Scene.PerspectiveTransform -> Attr<'msg>

    /// Mark this canvas volatile: bypass picture caching and wall it behind a repaint boundary so its
    /// per-frame change cannot dirty surrounding cached chrome.
    val volatile' : Attr<'msg>

    /// Forward raw pointer samples (position, button, wheel) in canvas-local space to the model.
    val onPointer : (FS.GG.UI.Controls.PointerSample -> 'msg) -> Attr<'msg>

    /// Forward raw key events to a focused canvas.
    val onKey : (FS.GG.UI.KeyboardInput.ViewerKey -> FS.GG.UI.KeyboardInput.KeyModifiers -> 'msg) -> Attr<'msg>

    /// Construct the canvas control from its attributes.
    val create : Attr<'msg> list -> Control<'msg>
```

**Contract rules**

- `create` with no `scene` attr ⇒ a control that paints a design-time placeholder.
- `scene` content is translated to the laid-out box origin and clipped to the box.
- `volatile'` present ⇒ the canvas subtree is always-dirty and not wrapped as a cache boundary; absent
  ⇒ the canvas caches like any leaf (fingerprint-gated).
- Explicit `width`/`height` attrs set the box; otherwise a default box is used.

## C3 — Element library (`src/Canvas/Elements.fsi`, package `FS.GG.UI.Canvas`)

Pure, position-independent drawables. All return `Scene`; none mutate.

```fsharp
type Element<'props> = 'props -> FS.GG.UI.Scene.Scene

module Elements =
    val rect     : w:float -> h:float -> Paint -> FS.GG.UI.Scene.Scene
    val sprite   : image:string -> w:float -> h:float -> FS.GG.UI.Scene.Scene
    val circle   : r:float -> Color -> FS.GG.UI.Scene.Scene
    val polyline : points:Point list -> Paint -> FS.GG.UI.Scene.Scene
    val at       : x:float -> y:float -> FS.GG.UI.Scene.Scene -> FS.GG.UI.Scene.Scene  // translate
    val layer    : FS.GG.UI.Scene.Scene list -> FS.GG.UI.Scene.Scene                   // group
    /// Wrap an expensive fragment so the picture cache keys on identity.
    val cached   : key:string -> FS.GG.UI.Scene.Scene -> FS.GG.UI.Scene.Scene
```

## C4 — Fixed-timestep loop (`src/Canvas/Loop.fsi`, package `FS.GG.UI.Canvas`)

```fsharp
type StepState<'world> =
    { Current : 'world
      Previous : 'world
      Accumulator : float }

module Loop =
    /// Seed a StepState from an initial world (Previous = Current, Accumulator = 0).
    val init : 'world -> StepState<'world>

    /// Advance by a fixed timestep accumulator.
    /// dt        — fixed step seconds (e.g. 1.0/60.0)
    /// integrate — pure 'world -> dt -> 'world simulation step
    /// frameTime — elapsed seconds since last advance; clamped to <= 0.25 (spiral-of-death guard)
    /// Deterministic: output depends only on its arguments (no wall-clock read).
    val advance :
        dt: float ->
        integrate: ('world -> float -> 'world) ->
        frameTime: float ->
        StepState<'world> ->
            StepState<'world>

    /// Interpolation factor in [0,1) for rendering between Previous and Current.
    val alpha : dt: float -> StepState<'world> -> float
```

## C5 — Optional `DrawScope` builder (`src/Canvas/DrawScope.fsi`) — may defer to Phase 3

A thin ergonomic appender that *emits* an immutable `Scene` (does not mutate rendered state). Justified
under Constitution III as the cross-framework-expected immediate-feeling authoring surface.

```fsharp
type DrawScope =
    member Rect : Rect -> Paint -> unit
    member Path : PathSpec -> Paint -> unit
    member WithTransform : FS.GG.UI.Scene.PerspectiveTransform -> (DrawScope -> unit) -> unit

module Canvas =
    /// Collect appended nodes into an immutable Scene.
    val draw : (DrawScope -> unit) -> FS.GG.UI.Scene.Scene
```

## C6 — Input routing (internal, `src/Controls.Elmish`)

No new *public* surface required if forwarding rides the existing event-binding channel; the internal
routing additions are exercised through the public `Canvas.onPointer`/`Canvas.onKey` constructors.

- `routeInteractivePointer`: a pointer interaction hitting a `canvas` node bound to `onPointer`
  dispatches the **raw `PointerSample`** (not just an interpreted `Click`), via a raw-sample channel
  added for canvas kinds (existing `PointerInteraction` routing left intact).
- `routeFocusedKey`: a focused `canvas` bound to `onKey` receives `ViewerKey` + `KeyModifiers` before
  default navigation.

## Surface-baseline impact (Tier 1)

| Baseline | Change |
|----------|--------|
| `readiness/surface-baselines/FS.GG.UI.Controls.txt` | + `Canvas` module (C2), + `canvas` catalog kind, + `SceneValue` attribute case (C1) |
| `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` | + any public input-forwarding surface (C6), if exposed |
| `readiness/surface-baselines/FS.GG.UI.Canvas.txt` | NEW — `Elements` (C3), `Loop`/`StepState` (C4), optional `DrawScope` (C5) |

All additions are deliberate and reviewed; the surface-drift test (`tests/Package.Tests/
SurfaceAreaTests.fs`) is updated in lockstep, never silenced.
