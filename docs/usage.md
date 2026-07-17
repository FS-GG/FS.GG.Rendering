# Using FS.GG.Rendering

A practical guide to consuming FS.GG.Rendering from an F# application: how to get
the packages, the three ways to put pixels on screen, theming, headless/offscreen
rendering, and the current runtime requirements and limits.

For the one-paragraph version, see the [README](../README.md). For the architecture
and layer model, see [`docs/product/layering.md`](product/layering.md) and
[`docs/product/module-map.md`](product/module-map.md).

---

## Mental model

FS.GG.Rendering is an F# desktop UI framework. You describe **what** to draw — either
a retained **scene** of primitives, or a tree of semantic **controls** — and the
framework measures, lays out, and paints it with **SkiaSharp over OpenGL**. For
interactive apps it runs a Model-View-Update (MVU) loop: state in, a view tree out,
input routed back as messages.

Three things are worth internalizing up front:

- **The render core is Elmish-free.** `Scene`, `Layout`, `Controls`, and `KeyboardInput`
  carry no dependency on the Elmish programming model.
- **Interactivity is MVU, but Elmish is optional.** The viewer exposes its own host
  record (`Init`/`Update`/`View`) generic over *your* `'model`/`'msg`. You can drive
  that directly, or opt into idiomatic [Elmish](https://elmish.github.io/elmish/)
  (`Cmd`, subscriptions) via the adapter packages.
- **Live rendering needs a GPU/display; offscreen rendering does not.** The same view
  code can be rendered headlessly to a buffer for tests and CI.

---

## Getting the packages

The libraries are published as `FS.GG.UI.*` packages targeting **`net10.0`** (current
framework version `0.12.0`). Every release **dual-publishes** the byte-identical
coherent set to public [nuget.org](https://www.nuget.org/packages?q=FS.GG.UI) (via GitHub
OIDC Trusted Publishing) and the org [GitHub Packages](https://github.com/orgs/FS-GG/packages)
feed (`nuget.pkg.github.com/FS-GG`). Consume them one of these ways:

1. **Public feed** — restore straight from nuget.org (no source configuration needed):
   ```sh
   dotnet add package FS.GG.UI.SkiaViewer --version 0.12.0
   dotnet add package FS.GG.UI.Controls   --version 0.12.0
   ```

2. **Project reference** — clone this repo and reference the `src/*/*.fsproj` you need
   directly. Most direct for framework development.

3. **Local pack** — produce packages and add a local feed:
   ```sh
   dotnet pack FS.GG.Rendering.slnx -c Release -o ./nupkgs
   dotnet nuget add source "$(pwd)/nupkgs" --name fs-gg-local
   # then in your app:
   dotnet add package FS.GG.UI.SkiaViewer
   dotnet add package FS.GG.UI.Controls
   ```

4. **Project template** — scaffold a ready-wired app:
   ```sh
   dotnet new install .          # from the repo root (installs FS.GG.UI.Template)
   dotnet new fs-gg-ui -n MyApp  # short name: fs-gg-ui
   ```

### Package map

All 16 libraries plus the `FS.GG.UI` BOM metapackage (see [module map](product/module-map.md)
for the owning source module of each):

| Package | What it gives you |
|---|---|
| `FS.GG.UI` | BOM / metapackage — a single version-coherent reference to the whole set |
| `FS.GG.UI.Scene` | Retained scene graph, drawing primitives, animation |
| `FS.GG.UI.Layout` | Layout engine and layout graph |
| `FS.GG.UI.KeyboardInput` | Pointer + keyboard models and dispatch |
| `FS.GG.UI.SkiaViewer` | The SkiaSharp-over-OpenGL viewer/host and render loop |
| `FS.GG.UI.Controls` | Semantic control set (Button, TextBox, ComboBox, DataGrid, Dialog…) |
| `FS.GG.UI.DesignSystem` | Token model, `Theme` record, `ResolvedStyle`, and the pure `Style.resolve` resolver |
| `FS.GG.UI.Themes.Default` | The default **Light**/**Dark** theme and mode+accent derivation |
| `FS.GG.UI.Themes.AntDesign` | Opt-in **Ant Design** theme (`AntTheme.antLight`/`antDark`) + intent policy |
| `FS.GG.UI.Controls.Elmish` | **Optional** Elmish adapter for control-set products — `runInteractiveApp`/`program`, Cmd/subscriptions (see [Which Elmish adapter?](#which-elmish-adapter)) |
| `FS.GG.UI.Elmish` | **Optional** pure scene Elmish adapter (`SceneNode` view, no control tree) |
| `FS.GG.UI.Testing` | Test helpers — capture, screenshot, responds/perf proof seams |
| `FS.GG.UI.Diagnostics` | Runtime diagnostic taxonomy, aggregation, readiness, and artifact contracts |
| `FS.GG.UI.Canvas` | Dependency-light element library + deterministic fixed-timestep game loop |
| `FS.GG.UI.Symbology` | Pure unit-symbology vocabulary (stat→channel Token → legible vector symbols) |
| `FS.GG.UI.Symbology.Render` | Headless Scene→PNG bridge for the symbology design loop |
| `FS.GG.UI.Build` | In-process governance engine (evidence gates) for generated workspaces |

A windowed controls app typically references `FS.GG.UI.Controls` + `FS.GG.UI.SkiaViewer`
(+ a theme package, and `FS.GG.UI.Controls.Elmish` if you want Elmish) — or just the
`FS.GG.UI` BOM to pull the coherent set at one version.

> **Note:** referencing `FS.GG.UI.SkiaViewer` brings `Fable.Elmish.dll` onto your
> dependency graph transitively — the viewer's window lifecycle is implemented on top
> of Elmish internally. You never have to *write* Elmish, but the assembly ships with
> your app regardless.

---

## Three ways to render

### 1. A static scene

The simplest path takes a `SceneNode` and presents it. No model, no messages.

```fsharp
open FS.GG.UI.SkiaViewer

let options : ViewerOptions =
    { Title = "Hello"
      InitialSize = (* a Scene.Size *) sceneSize
      PresentMode = ViewerPresentMode.DirectToSwapchain   // live default
      FrameRateCap = None                                 // None = 60 FPS
      LogicalSize = None }                                // None = draw in surface coordinates

match Viewer.run options scene with
| Ok outcome  -> ()                  // window ran and closed cleanly
| Error fail  -> eprintfn "%A" fail
```

### A fixed-resolution game

Set `LogicalSize` and your product draws in exactly that coordinate space, whatever the window
does. The host scales the canvas uniformly to the surface, centers it, clips to it, and leaves
letterbox bars on the surplus axis — in the live window, on resize, and on the offscreen evidence
surface alike. Pointer input is mapped back into logical coordinates before your product sees it.

```fsharp
let options : ViewerOptions =
    { Title = "Breakout"
      InitialSize = { Width = 1920; Height = 1080 }       // the window we open with
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FrameRateCap = None
      LogicalSize = Some { Width = 1280; Height = 720 } } // the space `view` draws in
```

`GeneratedAppHost.View` is handed no `Size` on purpose: with a `LogicalSize` there is nothing to
derive from it, and without one your product should be resolution-independent anyway. Reach for
`PerspectiveNode` only when you need a transform this seam does not express.

Use this for splash content, fixed visuals, or to sanity-check your GL setup.

### 2. Interactive MVU — without Elmish

`Viewer.runInteractiveViewer` drives a full input→update→repaint loop using the
framework's own host record. Your `'model`/`'msg` are your own; the only framework
types are `ViewerEffect`, `SceneNode`, and the input event types.

```fsharp
open FS.GG.UI.SkiaViewer

let host : InteractiveViewerHost<Model, Msg> =
    { Init       = fun () -> initialModel, []
      Update     = fun msg model -> update msg model, []     // returns model * ViewerEffect list
      View       = fun size model -> renderScene size model  // -> SceneNode
      MapKey     = fun key isDown -> keysToMsgs key isDown    // -> Msg list
      MapPointer = fun pointer size model -> pointerToMsgs …  // -> Msg list
      Tick       = fun dt -> Some (TickMsg dt)
      Diagnostics = Viewer.defaultDiagnostics }

Viewer.runInteractiveViewer options host |> ignore
```

This is the seam to use when you have your own state-management style and just want a
rendered, interactive window. No `Cmd`, no `Program`, no Elmish in your code.

### 3. Interactive MVU — with controls and Elmish

When you want the **semantic control set** (Button, TextBox, DataGrid…) and idiomatic
Elmish, use the `Controls.Elmish` adapter. The `View` returns a `Control<'msg>` tree
(reconciled frame-to-frame), and a `Theme` styles it.

```fsharp
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish

let host : InteractiveAppHost<Model, Msg> =
    { Init        = fun () -> initialModel, []
      Update      = fun msg model -> update msg model, []
      View        = fun size model -> view size model        // -> Control<'msg>
      Theme       = Theme.light
      MapKey      = fun key isDown -> None
      MapPointer  = fun interaction -> None
      Tick        = fun dt -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

ControlsElmish.runInteractiveApp options host |> ignore
```

`ControlsElmish` also exposes `program` / `programOfWidget` for wiring into an existing
`Elmish.Program`, plus command/subscription interpreters
(`interpretKeyboardEffect`, `interpretControlEffect`, `interpretPointerEffect`) so
control and keyboard runtime effects flow through normal Elmish `Cmd`s.

> The control builders that populate a `Control<'msg>`/`Widget<'msg>` tree live in
> `FS.GG.UI.Controls` (see the controls catalog in the generated template docs and
> `src/Controls/Widgets/`). The examples above focus on the *host wiring*, which is the
> part most likely to trip you up.

### Which Elmish adapter?

FS.GG.UI ships **two** Elmish adapter packages for **two different product shapes** — pick by
what your view produces. Both are supported (each has its own product skill); they are not
interchangeable.

- **`FS.GG.UI.Controls.Elmish`** — for products built on the **semantic control set** (way 3
  above). Your `View` returns a `Control<'msg>` tree, and the adapter runs the interactive
  host (`runInteractiveApp`, or `program`/`programOfWidget` to wire into an existing
  `Elmish.Program`) with keyboard/pointer/control runtime effects flowing through Elmish
  `Cmd`/subscriptions. Every sample in this repo uses it.
- **`FS.GG.UI.Elmish`** — the lower-level **pure scene adapter** (`ElmishAdapter`). Your
  `render` returns a `SceneNode` (no control tree); `init`/`update` stay pure, returning the
  next model plus effect *values* interpreted at the host boundary, bridging viewer messages
  and effects into Elmish envelopes over `Viewer.runInteractiveViewer` (way 2).

If your app uses buttons, text boxes, grids — anything from the control set — reach for
`FS.GG.UI.Controls.Elmish`.

---

## Theming

Controls own **behavior**; themes own **appearance**. A theme is a record of color
roles, typography, density, and radius applied at render time — the *same* control
tree renders under any theme. The framework ships three themes: **Light** and **Dark** from
`FS.GG.UI.Themes.Default`, plus an opt-in **Ant Design** theme from
`FS.GG.UI.Themes.AntDesign`:

```fsharp
open FS.GG.UI.Controls

let theme = Theme.dark
let custom = Theme.light |> Theme.withAccent myAccent |> Theme.withDensity 1.25
```

```fsharp
open FS.GG.UI.Themes.AntDesign

let ant = AntTheme.antLight   // or AntTheme.antDark — Ant's visual language, same controls
```

The Ant theme is a concrete `Theme` value plus an `AntIntentPolicy`, behaviour-neutral over
the existing semantic controls (no control forks; [ADR-0006](product/decisions/0006-antdesign-theme-and-new-controls.md)).
Dynamic composition (mode + accent → palette → theme) is available in the
`FS.GG.UI.Controls.Theming` module. Further named design languages (Fluent, Material) and
design-specific kits are **not yet implemented** — see [Current limits](#current-limits).

---

## Headless & offscreen rendering (tests, CI, screenshots)

The same view code renders without a window, which is how the project tests itself and
how you can snapshot output deterministically:

```fsharp
// Render a bounded number of frames offscreen and get evidence back:
Viewer.runForFrames 1 options scene
Viewer.runBounded request options scene
Viewer.captureScreenshotEvidence screenshotRequest options scene
```

`FS.GG.UI.Testing` adds capture and "responds-proof" helpers (did a real input produce
a visible change?). The offscreen path uses no GPU display and is deterministic, so it
runs in headless CI. Set `PresentMode = ViewerPresentMode.OffscreenReadback` when you
need a CPU-readable buffer for capture.

### Headless deterministic PNG evidence — no GPU, no GL, no display (feature 221)

`SceneEvidence.renderPng size scene : Result<byte[], SceneEvidenceFailure>` renders a scene
description to a **real, decodable PNG** in a bare container — **no GPU, no OpenGL context,
no X server, no virtual display**. The bytes decode to exactly the requested `Size` and show
the scene's geometry, colour, and bundled-font text; the same `(scene, size)` renders
**byte-for-byte identical** output across runs and machines, so the PNG can be committed or
diffed as CI evidence.

```fsharp
// Once at host/test startup, inject the CPU rasterizer into the dependency-light Scene surface
// (this is also wired automatically by Text.installMeasurer / Text.installShapingProvider):
FS.GG.UI.SkiaViewer.Text.installPngRasterizer ()

match SceneEvidence.renderPng { Width = 800; Height = 600 } scene with
| Ok pngBytes -> System.IO.File.WriteAllBytes ("evidence.png", pngBytes)   // real, decodable PNG
| Error failure -> eprintfn "no image evidence: %s (%A)" failure.Message failure.Classification
```

The pixels come from a SkiaSharp **CPU raster** surface (`SKSurface.Create(SKImageInfo)`, no
`GRContext`) sharing the same exhaustive painter as the live viewer. When no rasterizer is
injected, or a render genuinely cannot complete, `renderPng` returns a **typed
`SceneEvidenceFailure`** naming the blocked stage and classifying it
(`UnsupportedEnvironment` vs `ProductDefect`) — it never returns a success-shaped non-image.
This is the supported headless image-evidence path: portability over speed (a representative
scene renders in well under the 5 s CI budget).

### Pixel proof of the live game window — GL/virtual-display required (feature 221, US2)

The live viewer presents **direct-to-swapchain** (GPU present, no GPU→CPU readback), so an
external X11 grab of the window region reads solid black. To obtain an image of the live
frame, render the same scene through the offscreen-readback route, which **requires a GL
context / virtual display** (e.g. `Xvfb` + EGL) — this is distinct from the no-GL headless
PNG path above:

1. Run the viewer with `PresentMode = ViewerPresentMode.OffscreenReadback`.
2. The host's `renderSceneToPixels` (`src/SkiaViewer/Host/OpenGl.fs`) renders to an offscreen
   GL surface over the `GRContext` and reads the pixels back to CPU.
3. `Viewer.captureScreenshotEvidence` writes the decodable PNG of the current frame.

No step requires inspecting compiled binaries or trial-and-error. On a bare no-GL runner this
route is `environment-limited` (it needs the GL/virtual display); the no-GL deterministic
`renderPng` path above is the portable alternative.

---

## Runtime requirements

- **.NET**: `net10.0`.
- **Native**: SkiaSharp native assets (pulled in transitively:
  `SkiaSharp.NativeAssets.Linux` / `.Win32`) and a working **OpenGL** stack via
  Silk.NET.
- **Live window**: a real desktop/GL session (X11 + GL on Linux). The viewer reports
  capability via `Viewer.runtimeCapability` / `Viewer.desktopSessionDiagnostic` and
  **fails-classified rather than crashing** when GL/display is unavailable.
- **Headless/offscreen**: no display required (T0/T1 deterministic and offscreen-readback
  paths). `SceneEvidence.renderPng` produces a real, decodable, deterministic PNG with **no
  GPU/GL/display** via the injected CPU rasterizer (feature 221). Pixel proof of the **live
  window** still needs a GL/virtual-display host (the offscreen-readback route above).

---

## Current limits

This is a `0.1.x-preview`; consume accordingly.

- **Preview cadence** — published on nuget.org and GitHub Packages, but the public surface
  may move between previews (see the API-preview note below).
- **Themes**: Light, Dark, and Ant Design ship; Fluent/Material and design-kit compositions
  are planned, not present.
- **API is preview** — public surface is drift-gated (stable within a build) but may
  move between previews.
- **Live present timing / faithful-vsync perf and kernel-level input injection** are
  capability tiers that require a GL/uinput-capable host; headless environments degrade
  and disclose rather than fake a result.

See the implementation plan under
[`docs/reports/`](reports/) for the roadmap on themes, the layer split, and the
remaining harness tiers.

---

## Where to look next

- [`README.md`](../README.md) — the short overview and build/test commands.
- [`docs/product/layering.md`](product/layering.md) — the four-layer model (controls /
  design system / themes / kits) and the one-control-set rule.
- [`docs/product/module-map.md`](product/module-map.md) — what each module owns.
- [`docs/harness/capability-baseline.md`](harness/capability-baseline.md) — what the
  test/perf harness proves and what it explicitly does not.
- `tests/surface-baselines/*.txt` — the committed public API surface of each package.
