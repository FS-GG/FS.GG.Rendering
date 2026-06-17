# Embedded Canvas Control — Analysis & Implementation Plan

**Date:** 2026-06-17
**Status:** Proposal (research complete; not yet scheduled)
**Scope:** A first-class, model-driven `Canvas` control for games and arbitrary rendering, embedded inside the existing Ant-themed UI, plus a reusable "element" library authored as pure `state -> Scene` functions.
**Decision input:** User selected *"Embedded in the app"* — a render/game viewport that lives inside the control tree alongside themed chrome (not a full-window takeover).

---

## 1. Executive summary

The framework already has the two hard prerequisites for an arbitrary-rendering canvas: a **pure, immutable `Scene` display-list IR** (paths/béziers, gradients, blend modes, transforms, vertex meshes, clipping, text, images — rasterized by SkiaSharp) and a **continuous ~60 fps host loop** with an `isAnimating`-gated animation-tick subscription. What is *missing* is a control that lets a developer's own `Scene` reach the painter: the existing `CustomControl` declares the right shape (`Render: unit -> Scene`) but its scene is **never stored or painted** — `renderTree` falls through to a labeled placeholder.

This plan recommends **not** repairing `CustomControl` but adding a sibling **`canvas` control kind** whose drawn content is carried as an immutable `Scene` (produced by the application from its model). This choice is dictated by the framework's own grain and confirmed by how mature frameworks expose custom drawing:

- **Authoring API:** an immediate-*feeling* `DrawScope`-style builder (the ergonomic developers expect from Flutter/Compose/SwiftUI) that **emits the existing immutable `Scene` IR** (the SVG/`Shape` retained model). Reusable elements are **pure `'props -> Scene` functions** — the F# analogue of Compose `Painter`, Skia `SkPicture`, HTML `Path2D`, WPF `Drawing`.
- **Invalidation:** driven by **structural equality / the existing scene fingerprint**, not a hand-written `shouldRepaint`. Every surveyed framework is designing `shouldRepaint` *away* in favor of state-dependency + value equality; the repo already has a collision-resistant `hashScene` fingerprint that does exactly this for free.
- **Cache discipline:** the animated canvas is an **immediate-mode island**. A subtree that changes every frame must **not** be picture-cached (record + immediate-invalidate is pure overhead) and must be **walled behind a boundary** (Flutter `RepaintBoundary` / React `memo`) so its per-frame churn never dirties the static chrome around it. This requires a small, explicit "volatile / no-cache" opt-out at the canvas node.
- **Game loop:** drive simulation on a **fixed timestep accumulator** (Glenn Fiedler) inside Elmish `update`, fed by the `isAnimating`-gated tick; model input as a **`Set` of held keys + pointer state reconstructed from edge events** (no polling exists — and shouldn't).

The work is sized at roughly **four phases**: (0) decide the scene-carrier mechanism, (1) the `canvas` control + paint path + no-cache boundary, (2) the input/event forwarding, (3) the element library + a fixed-timestep game-loop helper. Phase 1 alone yields a paintable embedded canvas; phases 2–3 make it interactive and ergonomic for games.

---

## 2. Goal & non-goals

### Goal
1. A `canvas` control that paints an application-supplied `Scene` into its laid-out box, clipped to that box, integrated with layout/hit-testing/focus.
2. A reusable **element library** — pure functions `'props -> Scene` (sprites, shapes, primitives, helpers) — bundleable and testable in isolation, usable in any of the three placement models.
3. First-class **interactivity**: raw pointer + keyboard events forwarded to the model; a fixed-timestep update helper; smooth ~60 fps animation without cache thrash.
4. Determinism preserved: identical model → identical scene → identical fingerprint → reproducible frames and golden tests.

### Non-goals (this iteration)
- Full-window/no-chrome game host (that was *Option C*; deliberately deselected).
- A GPU shader / compute pipeline beyond what `Scene` + Skia already expose.
- An ECS runtime. (See §6.6 — start functional; revisit only if entity counts make per-frame record-tree updates measurably hot.)
- Replacing/over-extending `CustomControl`'s placeholder behavior beyond what §7 specifies.

---

## 3. Current architecture (offline findings)

All file:line references verified against the working tree on 2026-06-17.

### 3.1 The `Scene` IR — rich and sufficient
`Scene = { Nodes: SceneNode list }` (`src/Scene/Scene.fsi:353`) is an immutable display list. Primitives (`src/Scene/Scene.fsi:198`): `Rectangle`/`PaintedRectangle`, `Circle`/`Ellipse`/`FilledEllipse`, `Line`, `Path` (béziers + arcs, `MoveTo/LineTo/QuadTo/CubicTo/ArcTo/Close`, winding/even-odd), `Points`, `Vertices` (triangle mesh), `Arc`, `Text`/`TextRun`/`SizedText`, `Image`, `ClipNode`, `RegionNode`, `ColorSpaceNode`, `PerspectiveNode` (3×3 affine), `Translate`, `Group`, `PictureNode`, `CachedSubtree`. `Paint` (`Scene.fsi:89`) carries fill/stroke/opacity/antialias/blend-mode + shaders (linear/radial/sweep gradients), color/mask/image filters (blur, drop-shadow), and path effects (dash/discrete/corner). Construction is functional combinators (`Scene.group`, `Scene.translate`, `Scene.rectangleWithPaint`, `Scene.path`, `Paint.fill |> Paint.withShader ...`). Backend: `src/SkiaViewer/SceneRenderer.fs` (save/restore/concat for transforms & clips).

**Conclusion:** arbitrary 2D rendering is fully expressible today. No new primitives are required for v1.

### 3.2 The control pipeline & the `CustomControl` gap
`Control<'msg>` (`src/Controls/Types.fsi:307`) is `{ Kind; Key; Attributes; Children; Content; Accessibility }` — **`Content` is `string option` (a text label), there is no `Scene` field.** A leaf paints via `paintLeaf` (`src/Controls/Control.fs:2161`): if `Kind ∈ richFamilies` (`Control.fs:449`) it calls `faithfulContent` (`Control.fs:1784`) to synthesize preview geometry; otherwise it renders a labeled placeholder box.

`CustomControl` (`src/Controls/CustomControl.fs`) declares `Render: unit -> Scene` / `Draw: unit -> Scene` but `create` only does `Control.create "custom-control" ...` — **the author's `Scene` is discarded.** There is no `"custom-control"` case in `faithfulContent`, so it falls to the `emptyState` placeholder (`Control.fs:1902`). This is the concrete blocker referenced in earlier discussion.

### 3.3 Picture cache & fingerprint — the invalidation engine we want to reuse
`PictureCacheKey = { Box; Fingerprint: uint64 }` (`src/Controls/RetainedRender.fs:65`). `hashScene` (`RetainedRender.fs:289`) is an FNV-1a walk over every `SceneNode` payload — **any render-affecting change to the painted scene changes the fingerprint.** `paintBoundary` (`src/SkiaViewer/PictureReplayCache.fs:62`) replays a cached `SKPicture` on fingerprint match, else re-records. Cap 256, LRU. There is an internal `PictureCacheEnabled` flag (`RetainedRender.fs` ~line 110, Feature 116 FR-007) used as an "always-miss oracle" in tests — proof that a per-node cache bypass is mechanically feasible, though not yet exposed per-kind.

**Key implication:** because the fingerprint is computed from the *painted scene*, a canvas that carries its scene through the normal paint path gets **correct invalidation for free** — no `shouldRepaint` needed. The only problem is *thrash*: a scene that changes every frame is a guaranteed cache miss every frame, so it should bypass the cache rather than churn it.

### 3.4 Input model — message-driven, no polling
Host events: `ViewerPointerInput { Phase; X; Y; Button; DeltaX; DeltaY }` (`src/SkiaViewer/SkiaViewer.fsi:537`) and `ViewerKey`/`ViewerKeyEvent`/`KeyModifiers` (`src/KeyboardInput/KeyboardInput.fsi:9`). Controls-layer pointer types: `PointerSample` + `PointerInteraction` (`src/Controls/Pointer.fsi:10`). Routing: `routeInteractivePointer` (`src/Controls.Elmish/ControlsElmish.fsi:358`) hit-tests against the layout result, recovers the authored `ControlId`, and joins `(ControlId, EventKind)` against `ControlRenderResult.EventBindings`; unmatched interactions fall back to `host.MapPointer`. Keys route via `routeFocusedKey` (`ControlsElmish.fsi:454`) then `host.MapKey`. Event bindings are attributes: `Attr.on "onClick" msg` (`src/Controls/Attributes.fs:91`), carried as `ControlEventBinding { ControlId; EventKind; Dispatch }` (`Types.fsi:362`).

**Implication:** to give the canvas raw input we add canvas-specific event kinds (`onPointer`, `onKey`) whose `Dispatch` forwards the raw `PointerSample`/`ViewerKey` to the model. No polling API exists — and per §5/§6 we don't want one; we reconstruct held-input state in the model.

### 3.5 Frame loop & animation tick
`ViewerOptions.FrameRateCap: int option` (`src/SkiaViewer/SkiaViewer.fsi:7`, default 60) bounds both update and present cadence. `Animation.tickSubscription isAnimating toMsg interval model` (`src/Elmish/AnimationTick.fsi`) emits an immediate first frame then one `toMsg interval` per `interval`, and returns `Sub.none` once `isAnimating model` is false — the idiomatic MVU rAF gate. Wire via `Program.withSubscription`.

### 3.6 Test patterns to mirror
`RetainedRender.step` + assertions on `WorkReduction` (`PictureCacheHits/Misses`, `RepaintedNodeCount`, `DirtyArea`) — see `tests/Controls.Tests/Feature116PictureCacheTests.fs`, `Feature120FingerprintTests.fs`, `RenderingTests.fs`, `PointerInteractionTests.fs`. Golden-scene comparisons flatten the scene and assert byte-identity.

---

## 4. Comparative research (online findings)

How six mature systems expose custom drawing, distilled to the decisions that matter here. (Full citations in §11.)

| System | Drawing exposure | Invalidation signal | Reusable element |
|---|---|---|---|
| **Flutter** `CustomPainter` | immediate `paint(canvas,size)` | `shouldRepaint(old)` **or** `repaint:` Listenable (preferred for anim) | `ui.Picture` (record/replay), `RepaintBoundary` |
| **SwiftUI** `Canvas` | immediate `(GraphicsContext,Size)` closure | state-dependency + value equality (no shouldRepaint) | `resolve(Text/Image)`, `Shape`, `drawLayer` |
| **Compose** `Canvas`/`DrawScope` | immediate `DrawScope` lambda | **phase model**: reading state only in draw lambda invalidates *draw phase only* | `fun DrawScope.draw…`, `Painter`, `drawWithCache` |
| **HTML5 Canvas2D** | immediate, paints pixels | none (developer owns rAF redraw) | `Path2D`, offscreen-canvas sprites |
| **Skia** `SkPicture` | immediate `SkCanvas` | none — immutable picture, app owns staleness | one immutable `SkPicture`, many transforms |
| **WPF/Avalonia** `OnRender` | imperative calls **recorded to a retained instruction list** | explicit dirty: `InvalidateVisual()` / `AffectsRender` | `Drawing`/`Geometry` + `Freezable.Freeze()` |

### 4.1 Design lessons extracted
1. **Expose `model -> Scene`, not a mutable canvas.** The repo is *not* choosing immediate vs retained — it already has an immutable IR + picture cache, exactly like WPF/Avalonia "record imperative calls into a retained list." Keep the immediate-mode *ergonomics* (a `DrawScope`-style builder) but have it **emit immutable `Scene`**. This is the SVG/`Shape` model in F#.
2. **`shouldRepaint` is a footgun; use structural equality / fingerprint.** Flutter's own docs warn `paint` can run when `shouldRepaint` is false and skip it entirely; SwiftUI/Compose removed it; WPF reduced it to `AffectsRender`. The repo's `hashScene` fingerprint is the sound default — when the canvas's scene is unchanged, replay; when changed, re-record. **Do not add a `shouldRepaint` hook.**
3. **Converge on the universal coordinate default:** origin top-left, y-down, logical units; a save/restore stack of (transform, clip) modeled as **structural nesting** (`Translate`/`ClipNode`) rather than a mutable stack — matching Compose `withTransform({…}){…}` and Avalonia `using ctx.PushTransform(m)`. Provide both affine helpers and a raw 3×3 matrix (`PerspectiveNode` already exists). Keep the canvas's *internal* draw transform separate from its *layout* size (WPF `RenderTransform` vs `LayoutTransform`).
4. **Reusable elements = pure `'props -> Scene` + record-once/replay-many for expensive ones.** Value-level composition (one immutable fragment under many `Translate` nodes — Skia's "one picture, many transforms") plus cache-level reuse via the existing `SKPicture` cache keyed by `(identity, box, dpi)`. Adopt SwiftUI's "resolve once, re-tint via shading" so a recolored sprite reuses geometry.

### 4.2 Game-loop lessons (real-time inside MVU)
1. **Fixed-timestep accumulator (Glenn Fiedler, "Fix Your Timestep!").** Never feed wall-clock `dt` into simulation. `accumulator += frameTime; while accumulator >= dt { prev = cur; integrate(cur, dt); accumulator -= dt }`, then render an interpolated blend `alpha = accumulator/dt` of `prev`/`cur`. Clamp `frameTime` to ≤0.25 s to kill the "spiral of death." This is determinism-friendly and slots cleanly into Elmish `update`.
2. **The loop is a subscription, not a poll.** Keep `isAnimating`-gated `tickSubscription`; the tick `Msg` carries `dt`; only the tick advances the world. Input messages mutate only the *input slice* of the model.
3. **Reconstruct input state in the model.** Hold a `Set<Key>` (KeyDown adds, KeyUp removes) + pointer position/buttons. Distinguish **held (level)** from **pressed/released-this-tick (edge)**; consume edge flags exactly once per fixed step and clear them, or one press leaks across substeps. Distribute continuous deltas (÷ substep count) across a multi-step frame.
4. **Avoid cache thrash by isolation, not by caching.** Treat the canvas as an immediate-mode island: rebuild its scene each tick, **don't diff or picture-cache it**, wall it behind a boundary so churn can't dirty static chrome (Flutter `RepaintBoundary`, React canvas-outside-render). Cache the static, never the volatile; diff at the seam.
5. **Functional first, ECS later.** Pure `state -> Scene` matches MVU value semantics and fixed-timestep reproducibility. Reach for data-oriented component maps + `system: dt -> World -> World` only if entity-count cost proves it.

---

## 5. Design decisions

| # | Decision | Rationale |
|---|---|---|
| D1 | **New `canvas` kind**, not a repaired `CustomControl`. | Canvas needs a `model -> Scene` (cache-friendly, fingerprintable) signature; `CustomControl`'s `unit -> Scene` pushes mutable/closure state and can't be fingerprinted. Leave `CustomControl` as-is (or deprecate later). |
| D2 | **Carry the scene as an `Attr` value** (`SceneValue of Scene`), not a new `Control` field. | Minimally invasive — avoids touching every `Control<'msg>` construction site; `paintLeaf` extracts and paints it; the fingerprint already walks the painted scene, so invalidation is automatic. |
| D3 | **No `shouldRepaint` hook.** Invalidation = scene fingerprint. | Matches SwiftUI/Compose/WPF direction; reuses `hashScene`; keeps determinism. |
| D4 | **Explicit "volatile / no-cache" opt-out** on the canvas node + a **repaint boundary** around it. | A per-frame scene is a guaranteed cache miss; skip the record/replay overhead and wall it off so it can't dirty static chrome. Implemented as a per-kind flag feeding `paintBoundary`'s enable check. |
| D5 | **Local coordinate space:** canvas content is authored at origin (0,0) top-left of the canvas box; the control applies the box-origin translation + box clip. Optional viewport transform attr for pan/zoom. | Universal convention; keeps element library position-independent; separates draw transform from layout size. |
| D6 | **Element library = pure `'props -> Scene` functions** in a new `FS.GG.UI.Canvas` module, plus an optional `DrawScope`-style builder that emits `Scene`. | The F# analogue of `Painter`/`Path2D`/`Drawing`; testable in isolation; framework-agnostic (works in embedded, full-window, or headless). |
| D7 | **Raw input via new `onPointer`/`onKey` canvas event kinds**; application reconstructs held-input state. | No polling exists or should; mirrors Elm `keyboard-extra`. |
| D8 | **Fixed-timestep helper** (`Canvas.Loop`) implementing the accumulator + interpolation, fed by the existing `isAnimating`-gated tick. | Determinism + smoothness; reuses the host loop unchanged. |

### Rejected alternatives
- **Repair `CustomControl` (Option B earlier).** `unit -> Scene` can't be fingerprinted without invoking the closure each diff; worse cache story; no model parameter. Rejected per D1.
- **Add `Scene option` to `Control<'msg>`.** Touches every construction/clone/test site and the `Types.fsi` public surface. Attribute-carried scene (D2) is strictly less invasive with identical paint/fingerprint behavior.
- **A `shouldRepaint` callback.** Rejected per D3 — fragile and against the grain of an immutable-IR + fingerprint engine.

---

## 6. Proposed architecture

### 6.1 The scene carrier (D2)
Add an attribute value case (in the `AttrValue` DU, `src/Controls/Attributes.*` / `Types.fsi`):

```fsharp
| SceneValue of FS.GG.UI.Scene.Scene
```

`paintLeaf` (`Control.fs:2161`) gains a branch *before* the `richFamilies` check:

```fsharp
let private paintLeaf theme (box: Rect) (c: Control<'msg>) : Scene list =
    match c.Kind, ControlInternals.sceneAttr c with
    | "canvas", Some scene ->
        // translate author-local content into the box, clip to the box
        [ Scene.clipped (RectClip box)
            (Scene.translate box.X box.Y scene) ]
    | "canvas", None -> [ emptyCanvasGeom theme box ]   // design-time placeholder
    | _ ->
        if Set.contains c.Kind richFamilies then ... else ...
```

Because the painted scene now contains the author's nodes, `hashScene` (`RetainedRender.fs:289`) already produces a content-sensitive fingerprint — **no fingerprint change needed** (closes the gap the explore agent flagged at item 9 without extending the type).

### 6.2 The `canvas` kind registration
- **Catalog** (`Catalog.fs`): add a GENERATED block — id `canvas`, category `display`, events `[ "onPointer"; "onKey" ]`, accessibility role e.g. `Image`/`Group`.
- **`richFamilies`** (`Control.fs:449`): **do not** add `canvas` (its content is author-supplied, not framework-synthesized). Instead give it an explicit paint branch (§6.1). Add `canvas` to a new small `volatileFamilies`/`noCacheFamilies` set consumed by the cache boundary (D4).
- **Default size** (`Control.fs:505/512`): give `canvas` a sensible default box (e.g. 304×132 design-time, but honor explicit `width`/`height` attrs).

### 6.3 Public constructor surface
New module (e.g. `src/Controls/Canvas.fs` + `.fsi`, mirroring `Display`/`Buttons`):

```fsharp
module Canvas =
    /// Author-supplied immutable scene, painted into the control's box (local coords, top-left origin).
    val scene : Scene -> Attr<'msg>
    /// Optional internal viewport transform (pan/zoom) applied to the scene, not to layout.
    val viewport : PerspectiveTransform -> Attr<'msg>
    /// Mark this canvas volatile: skip picture caching + wall behind a repaint boundary.
    val volatile' : Attr<'msg>
    /// Raw input forwarding.
    val onPointer : (PointerSample -> 'msg) -> Attr<'msg>
    val onKey : (ViewerKey -> KeyModifiers -> 'msg) -> Attr<'msg>
    /// Construct the control.
    val create : Attr<'msg> list -> Control<'msg>
```

### 6.4 No-cache boundary (D4)
- `paintBoundary` (`PictureReplayCache.fs:62`) already short-circuits when `cache.Enabled` is false. Thread a per-node "volatile" signal so the canvas subtree is recorded/replayed *bypassed* (painted directly each frame) and is **not** wrapped as a `CachedSubtree`/`CacheBoundary`. Concretely: when assembling fragments in `RetainedRender`, if `Kind ∈ noCacheFamilies` (or the `volatile'` attr is present), emit the scene without a cache boundary and mark the fragment so the diff treats it as always-dirty (no false "Keep").
- This is the `RepaintBoundary` analogue: the canvas's per-frame change is contained; sibling/parent picture-cache entries survive (validate via `WorkReduction.PictureCacheHits` on the static chrome in tests).

### 6.5 Input forwarding (D7)
- Add event kinds `onPointer`/`onKey` to the `StandardEventKind`→string map (`Attributes.fs:24`) or as free-form `Attr.on "onPointer" ...`.
- In `routeInteractivePointer` (`ControlsElmish.fsi:358`): when a pointer interaction hits a `canvas` node bound to `onPointer`, dispatch the raw `PointerSample` (not just `Click`). May require surfacing the raw sample alongside the interpreted `PointerInteraction` (extend the binding's `Dispatch` payload, or add a raw-sample channel for canvas kinds).
- Keyboard: a focused `canvas` with `onKey` receives `ViewerKey` + `KeyModifiers` via `routeFocusedKey` (`ControlsElmish.fsi:454`) before default navigation. Canvas must be focusable (participate in `Focus.order`).
- **Application side** (documented pattern, not framework code): hold `{ PressedKeys: Set<ViewerKey>; Pointer: PointerSample option; EdgeDown: Set<ViewerKey> }` in the model; KeyDown adds, KeyUp removes; clear edge sets each fixed step.

### 6.6 Element library (D6)
New library/namespace `FS.GG.UI.Canvas` (or `FS.GG.UI.Elements`):

```fsharp
/// Pure, position-independent drawable. Compose with Scene.group / Scene.translate.
type Element<'props> = 'props -> Scene

module Elements =
    val rect    : w:float -> h:float -> Paint -> Scene
    val sprite  : image:string -> w:float -> h:float -> Scene
    val circle  : r:float -> Color -> Scene
    val polyline : Point list -> Paint -> Scene
    val at      : x:float -> y:float -> Scene -> Scene     // = Scene.translate
    val layer   : Scene list -> Scene                       // = Scene.group
    // expensive fragments may be wrapped so the picture cache keys on identity:
    val cached  : key:string -> Scene -> Scene             // emits a CachedSubtree boundary
```

Optional ergonomic builder (`DrawScope`-style, emits `Scene` — not mutation):

```fsharp
type DrawScope =
    member Rect : Rect -> Paint -> unit
    member Path : PathSpec -> Paint -> unit
    member WithTransform : PerspectiveTransform -> (DrawScope -> unit) -> unit
module Canvas =
    val draw : (DrawScope -> unit) -> Scene   // collects appended nodes into an immutable Scene
```

ECS is explicitly deferred (§2 non-goals); the library stays pure-functional. If needed later, add `World` + `system: dt -> World -> World` over component maps without importing an ECS runtime.

### 6.7 Fixed-timestep game loop helper (D8)
A reusable helper (in `FS.GG.UI.Canvas` or `FS.GG.UI.Elmish`):

```fsharp
type StepState<'world> = { Current: 'world; Previous: 'world; Accumulator: float }

module Loop =
    /// dt = fixed step (e.g. 1/60). frameTime clamped to <= 0.25s.
    val advance :
        dt: float ->
        integrate: ('world -> float -> 'world) ->
        frameTime: float ->
        StepState<'world> ->
            StepState<'world>
    /// Interpolation factor for rendering between Previous and Current.
    val alpha : dt: float -> StepState<'world> -> float
```

Wired in the app's `update` on the tick message; `view` renders `lerp Previous Current (alpha …)` into `Canvas.scene`. Subscription unchanged: `Animation.tickSubscription (fun m -> m.Running) Tick (TimeSpan.FromMilliseconds 16.0) model`.

### 6.8 Data flow (one frame, animating)
```
host tick (≤16ms) ──> Tick dt ──> update: Loop.advance (fixed-step accumulator) ──> new world
                                         │
view model ──> Canvas.scene (lerp prev/cur) ──> Control "canvas" + SceneValue
                                         │
RetainedRender.step ──> canvas node is volatile ──> NO picture cache, painted directly,
                          walled behind boundary ──> static chrome stays cache-HIT
                                         │
SceneRenderer ──> SkiaSharp raster
```

---

## 7. Implementation plan (phased)

Each phase is independently shippable and testable. File:line targets are the verified edit sites.

### Phase 0 — Decision spike (½ day)
- **T0.1** Confirm D2 (attribute-carried `SceneValue`) vs the `Control` field alternative with a 30-line spike: add `SceneValue`, a hard-coded `canvas` paint branch, render a static scene end-to-end via an existing sample/test host. Verify fingerprint sensitivity (change scene → cache miss; same scene → hit).
- **Exit:** a static red rectangle authored as a `Scene` paints inside a `canvas` control embedded in a stack.

### Phase 1 — The `canvas` control (paintable, embedded) (2–3 days)
- **T1.1** Add `SceneValue of Scene` to the attr value DU; helper `ControlInternals.sceneAttr` (`Attributes.*`, `Types.fsi`).
- **T1.2** `Catalog.fs` GENERATED block for `canvas`.
- **T1.3** `paintLeaf` branch (`Control.fs:2161`) per §6.1 (translate to box + `RectClip`); design-time placeholder when no scene.
- **T1.4** Default sizing (`Control.fs:505/512`) honoring explicit `width`/`height`.
- **T1.5** `src/Controls/Canvas.fs[i]` public surface (`scene`, `viewport`, `create`).
- **T1.6** `StyleResolver` (`src/DesignSystem/StyleResolver.fs`) — add a `canvas` case if any background/border styling is wanted (else inherit container default).
- **Tests:** golden-scene (`RenderingTests.fs` pattern) — authored scene appears, clipped to box, translated to box origin; embedding inside a `stack`/`panel` lays out correctly.
- **Exit:** an embedded canvas paints arbitrary authored scenes; determinism test green.

### Phase 2 — Cache discipline & interactivity (3–4 days)
- **T2.1** `noCacheFamilies`/`volatile'` plumbing into the cache boundary (`RetainedRender.fs` fragment assembly + `PictureReplayCache.fs:62`); canvas subtree bypasses record/replay and is always-dirty.
- **T2.2** Repaint-boundary isolation: prove the static chrome around a volatile canvas stays `PictureCacheHits` while the canvas repaints every frame (Feature116-style `WorkReduction` test).
- **T2.3** `onPointer` forwarding in `routeInteractivePointer` (`ControlsElmish.fsi:358`) — raw `PointerSample` to the bound dispatch; hit-test honors the canvas box.
- **T2.4** `onKey` forwarding + canvas focusability in `routeFocusedKey` (`ControlsElmish.fsi:454`) / `Focus.order`.
- **Tests:** `PointerInteractionTests.fs` pattern — pointer move/press/release/wheel inside the canvas dispatches raw samples; key events reach a focused canvas; cache-isolation test.
- **Exit:** an interactive canvas receiving raw pointer + keyboard, with the surrounding UI cache-stable.

### Phase 3 — Element library + game-loop helper (3–5 days)
- **T3.1** `FS.GG.UI.Canvas` library project: `Elements` (rect/sprite/circle/polyline/at/layer/cached) as pure `Scene` combinators.
- **T3.2** Optional `DrawScope` builder emitting immutable `Scene`.
- **T3.3** `Loop.advance` / `Loop.alpha` fixed-timestep accumulator + interpolation; clamp `frameTime ≤ 0.25`.
- **T3.4** Documented input-state pattern (`Set<ViewerKey>` + edge sets) — a small sample (bouncing sprites / Pong) under `samples/` exercising tick + input + interpolation.
- **Tests:** unit tests for `Loop.advance` (determinism: same seed+inputs → identical world; spiral-of-death clamp); element library golden scenes; the sample renders deterministically headless.
- **Exit:** a runnable embedded mini-game; reusable element kit; deterministic loop.

### Phase 4 — Polish & docs (1–2 days)
- Catalog/gallery entry for `canvas`; pattern doc under `docs/`; deprecate or document `CustomControl` vs `canvas` (see §8); accessibility metadata defaults; performance note (when to mark `volatile'`).

**Rough total:** ~10–15 working days for all four phases; Phase 1 (~3 days) is the minimum viable embedded canvas.

---

## 8. `CustomControl` disposition
`CustomControl` overlaps conceptually but is strictly weaker (`unit -> Scene`, no fingerprint, placeholder paint). Options, in preference order:
1. **Leave as-is, document** that `canvas` supersedes it for new work. (Lowest risk.)
2. **Re-base `CustomControl.create` onto the `canvas` machinery** (call its `Render()` once, store the result as `SceneValue`) — makes the existing placeholder actually paint, for free, as a thin adapter.
3. **Deprecate** with a migration note. Defer until `canvas` ships.

Recommended: ship `canvas` (Phases 0–3), then do option 2 as a small follow-up so existing `CustomControl` callers light up.

---

## 9. Risks & mitigations
| Risk | Likelihood | Mitigation |
|---|---|---|
| Attribute-carried `Scene` bloats the attr list / fingerprint cost for huge scenes | Med | The fingerprint already walks the painted scene regardless of carrier; for volatile canvases the cache is bypassed so no extra hashing. For static canvases, `cached` fragments key on identity. Benchmark in T2.2. |
| Cache-bypass plumbing leaks dirtiness into chrome (boundary not tight) | Med | T2.2 is an explicit isolation test asserting chrome stays `PictureCacheHits`. |
| Raw pointer/key forwarding requires surfacing data the routing layer currently interprets away | Med | Add a raw-sample channel for `canvas` kinds rather than reshaping `PointerInteraction`; keep existing routing intact. |
| Per-frame `view` rebuild cost for the whole tree even when only canvas changes | Low–Med | MVU rebuilds `view`, but the reconciler diffs the static chrome to "unchanged" cheaply (identity-stable); only the volatile canvas repaints. Validate with `WorkReduction.RepaintedNodeCount`. |
| Determinism regressions from wall-clock leaking into the loop | Low | `Loop` takes `frameTime` as a parameter; tick carries the nominal `interval` (fixed), not `Date.now()`. Golden tests assert reproducibility. |
| Coordinate-convention confusion (local vs box vs device) | Low | D5 fixes top-left/y-down local authoring; control applies box translate; document once. |

---

## 10. Open questions for the author
1. **Scene carrier:** approve D2 (attribute `SceneValue`) over adding a `Scene` field to `Control<'msg>`? (Plan assumes yes.)
2. **`volatile'` default:** should `canvas` be volatile (no-cache) *by default*, with an opt-*in* to cache static canvases — or cached by default with opt-out? (Plan leans volatile-by-default for game use; static dashboards may prefer cached.)
3. **`CustomControl`:** option 1 (leave) or option 2 (re-base onto `canvas`)? (Plan recommends ship-then-rebase.)
4. **Library home:** new `FS.GG.UI.Canvas` project, or fold `Elements`/`Loop` into existing `Controls`/`Elmish` libs?
5. **Input granularity:** is raw `PointerSample` + `ViewerKey` forwarding sufficient for v1, or do you also want gesture/drag interpretation (the existing `DragBegin/Move/End` interactions) surfaced to the canvas?

---

## 11. References

**Offline (codebase, verified 2026-06-17):** `src/Scene/Scene.fsi:89,198,353`; `src/SkiaViewer/SceneRenderer.fs`; `src/Controls/Types.fsi:307,362`; `src/Controls/Control.fs:449,505,512,1784,1902,2161`; `src/Controls/CustomControl.fs[i]`; `src/Controls/RetainedRender.fs:65,289,~110`; `src/SkiaViewer/PictureReplayCache.fs:62`; `src/Controls/Pointer.fsi:10`; `src/SkiaViewer/SkiaViewer.fsi:7,537`; `src/KeyboardInput/KeyboardInput.fsi:9`; `src/Controls/Attributes.fs:24,91`; `src/Controls.Elmish/ControlsElmish.fsi:358,454`; `src/Elmish/AnimationTick.fs[i]`; `tests/Controls.Tests/Feature116PictureCacheTests.fs`, `Feature120FingerprintTests.fs`, `RenderingTests.fs`, `PointerInteractionTests.fs`.

**Online (custom-draw APIs):**
- Flutter CustomPainter — https://api.flutter.dev/flutter/rendering/CustomPainter-class.html ; shouldRepaint — https://api.flutter.dev/flutter/rendering/CustomPainter/shouldRepaint.html ; RepaintBoundary — https://api.flutter.dev/flutter/widgets/RepaintBoundary-class.html ; PictureRecorder — https://api.flutter.dev/flutter/dart-ui/PictureRecorder-class.html
- SwiftUI Canvas — https://developer.apple.com/documentation/swiftui/canvas ; GraphicsContext — https://developer.apple.com/documentation/swiftui/graphicscontext ; TimelineView — https://developer.apple.com/documentation/swiftui/timelineview
- Jetpack Compose draw — https://developer.android.com/develop/ui/compose/graphics/draw/overview ; phases — https://developer.android.com/develop/ui/compose/phases ; performance/phases — https://developer.android.com/develop/ui/compose/performance/phases
- HTML5 Canvas2D — https://developer.mozilla.org/en-US/docs/Web/API/CanvasRenderingContext2D ; Path2D — https://developer.mozilla.org/en-US/docs/Web/API/Path2D ; optimizing — https://developer.mozilla.org/en-US/docs/Web/API/Canvas_API/Tutorial/Optimizing_canvas
- Skia — SkPicture https://api.skia.org/classSkPicture.html ; SkCanvas https://api.skia.org/classSkCanvas.html ; SKPicture (SkiaSharp) https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skpicture
- WPF rendering overview — https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/wpf-graphics-rendering-overview ; DrawingContext — https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.drawingcontext ; DrawingVisual — https://learn.microsoft.com/en-us/dotnet/desktop/wpf/graphics-multimedia/using-drawingvisual-objects ; Avalonia custom rendering — https://docs.avaloniaui.net/docs/graphics-animation/custom-rendering

**Online (game loop / reactive):**
- Fix Your Timestep! (Glenn Fiedler) — https://gafferongames.com/post/fix_your_timestep/
- Reliable fixed timestep & inputs (Tomšů) — https://jakubtomsu.github.io/posts/input_in_fixed_timestep/
- Elm subscriptions — https://elmprogramming.com/subscriptions.html ; ohanhi/keyboard-extra — https://github.com/ohanhi/keyboard-extra
- Proving Immediate-Mode GUIs are Performant — https://www.forrestthewoods.com/blog/proving-immediate-mode-guis-are-performant/
- Flutter RepaintBoundary (perf) — https://saropa-contacts.medium.com/why-flutters-repaintboundary-is-your-secret-weapon-against-jank-c610194a1ce4 ; React.memo — https://react.dev/reference/react/memo ; rAF with React hooks — https://css-tricks.com/using-requestanimationframe-with-react-hooks/
- ECS FAQ — https://github.com/SanderMertens/ecs-faq ; Bevy ECS — https://github.com/bevyengine/bevy/blob/main/crates/bevy_ecs/README.md

---

*Prepared via combined offline codebase exploration and online framework/game-loop research. All codebase line references verified against the working tree on 2026-06-17; confirm before editing as the tree evolves.*
