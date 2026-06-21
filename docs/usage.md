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

- **The render core is Elmish-free.** `Scene`, `Layout`, `Color`, `Controls`,
  `KeyboardInput`, and `Input` carry no dependency on the Elmish programming model.
- **Interactivity is MVU, but Elmish is optional.** The viewer exposes its own host
  record (`Init`/`Update`/`View`) generic over *your* `'model`/`'msg`. You can drive
  that directly, or opt into idiomatic [Elmish](https://elmish.github.io/elmish/)
  (`Cmd`, subscriptions) via the adapter packages.
- **Live rendering needs a GPU/display; offscreen rendering does not.** The same view
  code can be rendered headlessly to a buffer for tests and CI.

---

## Getting the packages

The libraries are published as `FS.GG.UI.*` packages targeting **`net10.0`**
(current version `0.1.0-preview.1`). They are **not yet on a public NuGet feed**, so
consume them one of these ways:

1. **Project reference** — clone this repo and reference the `src/*/*.fsproj` you need
   directly. Most direct for development.

2. **Local pack** — produce packages and add a local feed:
   ```sh
   dotnet pack FS.GG.Rendering.slnx -c Release -o ./nupkgs
   dotnet nuget add source "$(pwd)/nupkgs" --name fs-gg-local
   # then in your app:
   dotnet add package FS.GG.UI.SkiaViewer
   dotnet add package FS.GG.UI.Controls
   ```

3. **Project template** — scaffold a ready-wired app:
   ```sh
   dotnet new install .          # from the repo root (installs FS.GG.UI.Template)
   dotnet new fs-gg-ui -n MyApp  # short name: fs-gg-ui
   ```

### Package map

| Package | What it gives you |
|---|---|
| `FS.GG.UI.Scene` | Retained scene graph, drawing primitives, animation |
| `FS.GG.UI.Layout` | Layout engine and layout graph |
| `FS.GG.UI.KeyboardInput` | Pointer + keyboard models and dispatch |
| `FS.GG.UI.SkiaViewer` | The SkiaSharp-over-OpenGL viewer/host and render loop |
| `FS.GG.UI.Controls` | Semantic control set (Button, TextBox, ComboBox, DataGrid, Dialog…), theming |
| `FS.GG.UI.Elmish` / `FS.GG.UI.Controls.Elmish` | **Optional** Elmish adapters (Cmd/subscriptions/program) |
| `FS.GG.UI.Testing` | Test helpers — capture, screenshot, responds/perf proof seams |

A windowed controls app typically references `FS.GG.UI.Controls` +
`FS.GG.UI.SkiaViewer` (+ `FS.GG.UI.Controls.Elmish` if you want Elmish).

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
      FrameRateCap = None }                               // None = 60 FPS

match Viewer.run options scene with
| Ok outcome  -> ()                  // window ran and closed cleanly
| Error fail  -> eprintfn "%A" fail
```

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

---

## Theming

Controls own **behavior**; themes own **appearance**. A theme is a record of color
roles, typography, density, and radius applied at render time — the *same* control
tree renders under any theme. Today the framework ships **Light** and **Dark**:

```fsharp
open FS.GG.UI.Controls

let theme = Theme.dark
let custom = Theme.light |> Theme.withAccent myAccent |> Theme.withDensity 1.25
```

Dynamic composition (mode + accent → palette → theme) is available in the
`FS.GG.UI.Controls.Theming` module. Named design languages (Ant/Fluent/Material) and
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
  paths).

---

## Current limits

This is a `0.1.0-preview`; consume accordingly.

- **Not on a public feed yet** — use project reference, local pack, or the template.
- **Themes**: only Light/Dark ship; Ant/Fluent/Material and design-kit compositions are
  planned, not present.
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
