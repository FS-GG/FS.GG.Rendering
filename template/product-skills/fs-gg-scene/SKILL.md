---
name: fs-gg-scene
description: Build pure scene descriptions in a generated FS.GG.UI product.
---

# Scene Capability

## Scope

Use this skill for product code that builds pure `Scene` / `SceneNode`
descriptions: HUD regions, gameplay geometry, markers, and text. Scene values are
plain data — they perform no window, render, or screenshot I/O themselves.

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/Scene/Scene.fsi`. Read them to confirm any union case's exact
field order locally — no DLL reflection needed. Prefer the self-describing
constructors (`Scene.filledRectangle`, `Scene.textAt`, `Scene.circle`) over the
positional tuple cases to avoid an arity slip.

## Usage

```fsharp
open FS.GG.UI.Scene

let panel = { Red = 40uy; Green = 90uy; Blue = 200uy; Alpha = 255uy }
let ink = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

// A pure scene: a HUD bar plus a label. No I/O happens here.
let hud : Scene =
    Scene.group
        [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 320.0; Height = 48.0 } panel
          Scene.textAt { X = 12.0; Y = 30.0 } "tally: 0" ink ]
```

### Self-positioning HUD text — measure, don't guess

`Scene.measureText : string -> FontSpec -> TextMetrics` is a **pure, host-independent**
metric (distinct from the render-edge glyph shaping), so you can size and align HUD /
overlay strings at authoring time instead of hard-coding coordinates. `TextMetrics` carries
`Width` / `Height` / `Baseline`, and the heuristic is deliberately conservative — a box sized
by it is never narrower than the renderer draws, so text never clips. Right-align a score
label inside a HUD band without a literal x:

```fsharp
open FS.GG.UI.Scene

// hudWidth comes from the layout region (see [[fs-gg-layout]]); no magic numbers.
let placeScore (hudWidth: float) (font: FontSpec) (ink: Color) (scoreText: string) : Scene =
    let m = Scene.measureText scoreText font
    Scene.textAt { X = hudWidth - m.Width; Y = m.Baseline } scoreText ink
```

### Ask the scene what is wrong with it — `Scene.diagnostics`

A scene is a pure value, so you can interrogate it before anything renders — no host, no window, no GL:

```fsharp
open FS.GG.UI.Scene

Scene.describe hud       // SceneElementKind list — what this scene is MADE of
Scene.diagnostics hud    // RenderDiagnostic list — what is WRONG with it
```

`describe` answers *what did I build*; `diagnostics` answers *what will bite me*. Reach for the second
in a test over a scene your `view` produced: a scene that renders to a blank frame is otherwise
indistinguishable from one that renders correctly to a frame you have not looked at, and the diagnostic
list is the cheapest thing that can tell them apart.

### Catch clipping and background bleed before the screenshot

`SceneInspection.inspect` walks the authored hierarchy without a renderer and returns one stable
path row per node. Each row carries its parent/children, effective bounds after translation,
3×3 transforms and clips, and its relation to the viewport. Text, sized text, text runs and shaped
glyph runs use their explicit deterministic metrics. Geometry that cannot be bounded is
`SceneDrawableBounds.Unknown reason`, never a false-safe empty rectangle.

```fsharp
let viewport : Rect = { X = 0.0; Y = 0.0; Width = 320.0; Height = 180.0 }
let rows = SceneInspection.inspect viewport hud

// Product policy: no authored drawable may leave the logical canvas.
let overflow = SceneInspection.outsideViewport rows
Expect.isEmpty overflow "HUD content stays inside the logical viewport"

// Page policy: this stable authored slot must contribute nothing behind a deep screen.
let background = SceneInspection.contributingDescendants "/nodes/0/group/0/nodes/0" rows
Expect.isEmpty background "the background page slot is excluded"
```

Keep page slots stable (use `Scene.empty` for an excluded slot) when a test treats a path as a
semantic boundary. These structural probes are an earlier red signal for clipping and bleed;
they **complement, not replace, final raster inspection**, which remains the authority for pixels,
font realization, antialiasing and visual quality.

## Common pitfalls

- **Consumer geometry records colliding with framework `Point`/`Rect`.** Scene exposes
  `Point = { X: float; Y: float }` and `Rect = { X: float; Y: float; Width: float;
  Height: float }`. If your product also defines a geometry record with the same field
  names (a common `type Vec2 = { X: float; Y: float }`), F# label resolution binds a
  bare `{ X = ...; Y = ... }` to whichever record type is in scope **last**, which
  produces a misleading error cascade at unrelated call sites. Disambiguate explicitly
  at the boundary — annotate the type or qualify the fields — and convert your record
  into the framework type when you call Scene:
  ```fsharp
  type Vec2 = { X: float; Y: float }                     // product geometry
  let toPoint (v: Vec2) : Point = { X = v.X; Y = v.Y }   // explicit conversion
  let p : Point = { Point.X = 0.0; Point.Y = 0.0 }       // or qualify fields inline
  ```
- **The same collision also happens consumer-vs-consumer, not just against the
  framework.** Two of *your own* records sharing a field — a `Creep` and a `Tower`
  both carrying `.Pos` (or `.Id`, `.Hp`) — make a bare accessor like
  `let posOf x = x.Pos` infer the **last-declared** record for `x`, so it silently
  type-checks against the wrong type. Annotate the parameter at each shared access
  (`let posOf (c: Creep) = c.Pos`). The [[fs-gg-game:fs-gg-game-core]] grid-sim recipe walks
  this `.Pos` case in full.

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

### Fixed resolution — scale at the host, not in the scene

If your product has a fixed logical playfield (the dominant 2D game shape — "coordinates are
1280x720; the host scales to the window"), **do not scale the scene**. Build every node in
logical coordinates and set `ViewerOptions.LogicalSize = Some { Width = 1280; Height = 720 }`
at the host boundary. The host then scales that canvas uniformly to whatever surface it has,
centers it, clips to it, and letterboxes the surplus axis — in the live window, across resize,
and on the offscreen evidence surface alike. Pointer input arrives already mapped back into
logical coordinates.

SkiaViewer is the sole owner of that fit and its inverse pointer mapping. For a runtime resolution
change emit `ViewerEffect.ApplyLogicalCanvas nextSize`; do not add a scene transform or pointer
division alongside it. Interactive Controls layout and hit tests receive the new logical size
directly, while the viewer alone handles framebuffer scaling and letterboxing.

This is why `GeneratedAppHost.View : 'model -> SceneNode` is handed no `Size`: with a
`LogicalSize` there is nothing to derive from it, and without one your product should be
resolution-independent anyway. Window-size arithmetic scattered through `view` is the thing
the signature exists to prevent. See `fs-gg-skiaviewer`.

There is deliberately **no `Scale` scene node**. A uniform fit is a host concern, and a scene
that scales itself has to know the window size to do it — reintroducing exactly the coupling
`LogicalSize` removes.

#### `PerspectiveNode` — the low-level escape hatch

`PerspectiveNode of transform: PerspectiveTransform * scene: Scene` concatenates a raw 3x3
affine/perspective matrix onto the canvas (`M11`/`M22` scale, `M13`/`M23` translate). It is the
primitive `LogicalSize` is built from, and it is the escape hatch for transforms this skill's
other nodes do not express — rotation, shear, non-uniform scale, true perspective.

Reach for it only when you need such a transform **within** your logical canvas, via
`Scene.withPerspective : PerspectiveTransform -> Scene -> Scene`. Do not reach for it to fit a
fixed canvas to a window: that needs the window size, which `view` does not get, and
`LogicalSize` already does the job. For a plain offset, prefer `Scene.translate` — it says what
it means.

## Declarative motion — animation as data

Motion is **data**, not a mutable timeline. Against an existing `Scene` you declare that
opacity, an affine `Transform`, and/or a `Color` should travel from a start value to a target
over a `Tween`'s duration and easing. Sampling is a **pure** function of an explicit
`TimeSpan`, so identical inputs and identical time samples produce byte-identical output — and
a settled animation lowers to the exact static render of the same widget (the identity-at-rest
rule). The signatures are bundled at `docs/api-surface/Scene/Animation.fsi`; nothing here
performs I/O.

### The transform vocabulary

A `Transform` carries motion-specific labels (`TranslateX/Y`, `ScaleX/Y`, `RotationDegrees`) —
deliberately *not* Scene's `X`/`Y`, to avoid exactly the bare-literal record collisions the
pitfalls section warns about. Start from `Transform.identity` (no translate, unit scale, no
rotation) and lower a transform into the 3×3 matrix a `PerspectiveNode` wants with
`Transform.toPerspectiveTransform`. `Transform.isIdentity` is the at-rest test the sampler uses
to decide whether a node needs a `PerspectiveNode` wrapper at all — a settled transform stays
byte-identical to the static node.

```fsharp
open FS.GG.UI.Scene

let slideIn : Transform = { Transform.identity with TranslateX = -64.0 }
let atRest : bool = Transform.isIdentity slideIn                 // false while sliding
let matrix : PerspectiveTransform = Transform.toPerspectiveTransform slideIn
```

### Declare the animation, sample it as deterministic frames

An `Animation` is three optional tweens (opacity / transform / colour); an absent property is
its identity. `Tween.progress` gives the normalized, eased, clamped position in `[0,1]` for an
elapsed time (`Duration ≤ 0 ⇒ 1.0`, so no divide-by-zero). Interpolate leaf values with the
supplied interpolants — `Animation.lerpFloat` for opacity, `Animation.lerpColor` for colour.
`Animation.applyAt` composes opacity + transform onto the target scene at one time sample (and,
by the identity-at-rest rule, returns the target unwrapped once settled); `Animation.sampleColor`
surfaces the sampled colour *separately*, because the frozen wire format has no scene-wide tint
node — drive your own recolouring from it rather than expecting `applyAt` to tint. `Animation.isSettled`
gates redraw (true once every present tween has run its `Duration`), and `Animation.sampleFrames`
produces a deterministic `Scene list` at explicit times — the evidence a test asserts over.

```fsharp
open System
open FS.GG.UI.Scene

let fade : Animation =
    { Animation.empty with
        Opacity = Some { Start = 0.0; End = 1.0; Duration = TimeSpan.FromMilliseconds 200.0; Easing = EaseInOut } }

let at = TimeSpan.FromMilliseconds 100.0

let t : float = Tween.progress at fade.Opacity.Value          // eased progress in [0,1]
let alpha : float = Animation.lerpFloat 0.0 1.0 t             // opacity leaf interpolant
let tint : Color option = Animation.sampleColor at fade       // None here (no Color tween)
let frame : SceneNode = Animation.applyAt at fade hud         // opacity + transform composed
let finished : bool = Animation.isSettled (TimeSpan.FromMilliseconds 200.0) fade

// One Scene per requested time sample — byte-identical across runs, so a snapshot test is stable.
let frames : Scene list =
    Animation.sampleFrames [ TimeSpan.Zero; at; TimeSpan.FromMilliseconds 200.0 ] fade hud

// A direct colour interpolation (e.g. a damage flash you composite yourself):
let flash : Color =
    Animation.lerpColor
        { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy }
        { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }
        t
```

### Retarget without snapping — `AnimationState`

When the target can **change mid-flight** (a health bar whose value updates while the last
change is still animating), hold an `AnimationState<'a>` in your own model rather than a raw
elapsed clock. `AnimationState.retarget` restarts from the *currently displayed* value
(`Start = Current`, `Elapsed = 0`) so there is no snap-back, and `AnimationState.isActive` is
true while the transition is still in flight (`Elapsed < Duration && Current <> Target`) — the
redraw gate for the stateful case.

```fsharp
open FS.GG.UI.Scene

// state : AnimationState<float> is held in the product model and advanced each tick.
let retargeted = AnimationState.retarget 0.75 state
let stillMoving : bool = AnimationState.isActive retargeted
```

## Test Commands

Run `./fake.sh build -t Test` to exercise product-owned scene examples.

## Evidence

Record scene and bounds evidence under this product's `readiness/` paths. Do not
copy framework readiness reports into the product.

## Package Boundary

## Expected-workload performance

For game performance evidence, define representative state and scene density **before feature
implementation**, and count the production `view` result rather than a synthetic scene. Each required
row begins as `Placeholder`; run `./fake.sh build -t PerformanceEvidence`, review the emitted
`definitionDigest`, and mark it `Authored` only after its product state/messages traverse the real
`update` + `view` route. A changed definition invalidates the acknowledgement. The command records nodes
by layer alongside p50/p95/p99 and fails Placeholder/stale rows and the normal-play node/timing target.
`./fake.sh build -t PerformanceIntent` projects those same executable digests plus the target FPS,
maximum expected scale, timing/catch-up limits, structural scene-cost budget, measurement capability,
and live-compositor posture into the published Contracts 7.x shape used by SDD. Never maintain a second
scene-performance declaration.
A linked blocking performance-debt issue allows a baseline artifact but never makes acceptance green.
A 64x64 world with thousands of repeated fog or minimap nodes should fail before
row-run/static-subtree remediation and pass afterward. The command is bounded headless scene-route
evidence; live compositor and swapchain proof remain host work.

Keep the product-owned `performanceCostDrivers` inventory independent from workload declarations.
Every `GameplayVisualInventory` element must bind to a workload or carry a reasoned non-performance
disposition, and maximum scale must cite production configuration plus observed counters. Run this
machine coverage gate first. Next, run `PerformanceCriticRequest` for a fresh-context critic over the
exact opaque runner receipts, byte-digested evidence artifact, inventory, raw samples, host facts, and
rubric; an unresolved underrepresentative,
synthetic-only, unmeasured, misclassified, or ambiguous result blocks representative readiness.
Record its verdict in an attributable external review system at the exact landing commit. In-repo JSON
or an author-entered identity/mode string cannot establish independence.

Scene must not reference Elmish, the viewer host, layout, or widgets. Keep host
wiring in `fs-gg-skiaviewer` and control authoring in `fs-gg-ui-widgets`.

## Generated Product

Scene is the base capability in every profile; build product geometry from these
primitives and feed the resulting `SceneNode` to your `View`.

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). If your product uses Spec Kit, record the findings
and resolving links under the feature's `specs/<feature>/feedback/` folder; otherwise record
them in this skill's **Sources** / durable-lessons line (and any product-local `docs/`
location). Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-skiaviewer]] — render the `SceneNode` this skill builds at the host boundary.
- [[fs-gg-ui-widgets]] — compose higher-level controls that ultimately emit scenes.
- [[fs-gg-layout]] — compute the HUD + gameplay regions the scene primitives are placed into.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
