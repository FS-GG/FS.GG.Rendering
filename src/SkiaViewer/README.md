# FS.GG.UI.SkiaViewer

Skia viewer host workflow contracts for FS.GG.UI V3 products.

`FS.GG.UI.SkiaViewer` is one of the **FS.GG.UI** distribution packages — an F# / Elmish UI and 2D
scene-graph framework for .NET 10 desktop, rendered through Vulkan + SkiaSharp.

## Install

```bash
dotnet add package FS.GG.UI.SkiaViewer
```

Or scaffold a full governed project that wires the FS.GG.UI packages together:

```bash
dotnet new install FS.GG.UI.Template
dotnet new fs-gg-ui -o MyApp
```

## Usage

`Viewer.run` opens the Vulkan + Skia viewer for a single scene. It returns a `Result`, so a host
that has no usable desktop session (or fails to present) reports a typed `ViewerRunFailure` rather
than throwing — match on it instead of assuming success.

```fsharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let options: ViewerOptions =
    { Title = "My App"
      InitialSize = { Width = 1280; Height = 720 } }

// The rendered content is any SceneNode from FS.GG.UI.Scene.
let scene: SceneNode = Rectangle((0.0, 0.0, 1280.0, 720.0), Colors.white)

match Viewer.run options scene with
| Ok outcome ->
    printfn "Window opened: %b, first frame presented: %b" outcome.WindowOpened outcome.FirstFramePresented
| Error failure ->
    printfn "Viewer blocked at %A (%A): %s" failure.BlockedStage failure.Classification failure.Message
```

For an Elmish-style application, build a `GeneratedAppHost` (with `Init` / `Update` / `View` /
`MapKey` / `Tick` / `Diagnostics`) and run it with `Viewer.runApp options host`, which returns the
same `Result<ViewerLaunchOutcome, ViewerRunFailure>`.

Portable scene packages from `FS.GG.UI.Scene.SceneCodec` can be rendered through the
`ReferenceRendering` oracle to produce a PNG artifact plus protocol, capability, resource, and
renderer metadata. Unsupported host conditions return `ReferenceEnvironmentLimited` rather than a
false pass.

## API at a glance

- **`Viewer.run` / `Viewer.runApp`** — open the viewer for a single `SceneNode`, or for a full
  `GeneratedAppHost<'model,'msg>`; both return `Result<ViewerLaunchOutcome, ViewerRunFailure>`.
- **`Viewer.runBounded` / `runUntilFirstFrame` / `runForFrames`** — headless, bounded evidence runs
  that render to a `ViewerRunFailure`-or-`ViewerRunEvidence` `Result` for CI and screenshots.
- **`Viewer.captureScreenshotEvidence`** — drive a `ScreenshotEvidenceRequest` to a
  `ScreenshotEvidenceResult` recording capture source, pixel-content validation, and proof status.
- **`ReferenceRendering.init` / `update` / `run`** — MVU-style reference rendering for portable
  scene packages, with explicit inspect/render/write effects and Skia-backed PNG evidence.
- **`Viewer.desktopSessionDiagnostic` / `runtimeCapability`** — probe the host for a usable desktop
  session and the renderer/keyboard/window capabilities currently available.
- **`ViewerOptions`, `ViewerRunRequest`, `GeneratedAppHost<'model,'msg>`** — the core input
  contracts: window title/size, bounded-run target plus diagnostics, and the Elmish app surface.
- **Responsiveness diagnostics** — `ViewerLatencyRecord`, `ViewerResponsivenessSummary`,
  `ViewerResponsivenessBudget`, stable token helpers, JSONL/Markdown summary writers, and default
  budgets for input receipt, input-to-visible latency, and long-frame reporting.
- **Input queue helpers** — `Viewer.enqueueInput`, `drainInputQueue`, `dirtyState`, and
  `dirtyStateRequiresRecompose` model the live scheduler boundary: discrete input preserves order,
  continuous pointer movement coalesces, and retained recomposition is requested once per dirty
  frame.
- **`FS.GG.UI.SkiaViewer.Host.Viewer`** — the lower-level Elmish host edge (`create`,
  `withSubscription` / `withEventMapping` / `withEffectMapping`, `run`) over `ViewerProgram<'model,'msg>`.
- **`Host.Diagnostics`** — constructors for structured `RenderDiagnostic` values (`vulkanUnavailable`,
  `unsupportedPlatform`, `frameRenderFailed`, `startupFailed`, and more), keyed by `DiagnosticStage`.
- **`CompositorProof` / `Host.GlHost.ScissorDecision`** — Feature147/148 proof and scissor policy
  contracts. Damage-scissored redraw remains proof-gated: missing, stale, failed, host-mismatched,
  synthetic, or environment-limited proof keeps the viewer on a full-redraw fallback until fresh
  sentinel/damage readback evidence is available for the active host profile. Snapshot and timing
  readiness likewise require parity-clean evidence and resource/timing artifacts before claiming
  performance value.

## Responsiveness Evidence

The viewer exposes additive responsiveness contracts for diagnostic runs and readiness tooling. A
run writes `records.jsonl`, `summary.json`, `summary.md`, and `environment.md`; environment-limited
hosts must report statuses such as `headless-substitute`, `missing-boundary`, or
`no-visible-surface` instead of claiming accepted live latency.

Use `Viewer.summarizeResponsivenessRecords` with `Viewer.defaultResponsivenessBudget` to aggregate
latency records, then `Viewer.writeResponsivenessRun` to persist reviewer-readable and
machine-readable evidence.

## Versioning

All `FS.GG.UI.*` libraries share one version and move together. In a generated project a
single `<FsSkiaUiVersion>` in `Directory.Packages.props` pins every package — upgrading is one
edit; see `docs/UPGRADING.md`. Pre-release versions use a `-preview.N` suffix.

## Links

- Repository & issues: https://github.com/FS-Skia-UI/FS-Skia-UI
- License: MIT
