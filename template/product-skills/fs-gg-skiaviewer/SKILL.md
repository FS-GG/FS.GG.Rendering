---
name: fs-gg-skiaviewer
description: Wire a generated FS.GG.UI product to the desktop viewer host.
---

# SkiaViewer Capability

## Scope

Use this skill for the host boundary of a generated product: opening the native
window, rendering scenes, routing keyboard input, advancing time, and
interpreting `ViewerEffect` values returned by your pure `update`.

## Public Contract

The signatures you consume are bundled with this product at
`docs/api-surface/SkiaViewer/SkiaViewer.fsi`. `Viewer.runApp` is the canonical
entry point and the only place that performs host-boundary I/O. See
`docs/effects-boundary.md` for the full effect-category description.

## Usage

```fsharp
open FS.GG.UI.SkiaViewer

// Bundle your pure pieces into the host record.
let generatedHost =
    { Init = fun () -> initialModel, []   // initial model + startup effects
      Update = update                     // pure Msg -> Model -> Model * ViewerEffect list
      View = view                         // Model -> SceneNode
      MapKey = mapKey                     // ViewerKey -> bool -> Msg option
      Tick = tick                         // TimeSpan -> Msg option
      Diagnostics = Viewer.defaultDiagnostics }

match Viewer.runApp viewerOptions generatedHost with
| Ok _ -> 0          // window opened, scenes rendered, effects interpreted
| Error _ -> 1       // classified host/launch/verification failure
```

## Build Commands

Run `./fake.sh build -t Dev` then `./fake.sh build -t Verify` in this product.

## Test Commands

Run `./fake.sh build -t Test` for product host-wiring coverage.

## Evidence

Record window-visibility and screenshot evidence under this product's
`readiness/` paths. Do not copy framework readiness reports into the product.

## Package Boundary

Keep window, render, and screenshot I/O inside the `Viewer.runApp` interpreter.
Your `update` and `View` stay pure; never perform host I/O inside them.

## Generated Product

The app profile wires `Viewer.runApp viewerOptions generatedHost` as the default
launch path. Use `Viewer.runAppEvidence` with the **evidence** options for bounded
evidence runs.

## Present mode: live vs evidence — never reuse the evidence options

`ViewerOptions.PresentMode` picks the present mechanism; choose it by launch context:

| Launch context | `PresentMode` | Why |
|----------------|---------------|-----|
| Persistent interactive window | `DirectToSwapchain` | zero-readback live present; unchanged frames skip paint |
| Evidence / screenshot capture | `OffscreenReadback` | small readback surface for deterministic pixel capture |

This product ships **two** option records (`EvidenceCommands.fs`): `viewerOptions`
(`DirectToSwapchain`, the persistent launch) and `evidenceViewerOptions`
(`OffscreenReadback`, the bounded evidence commands). **Do NOT** launch the
persistent window from the evidence options — `OffscreenReadback` renders off-screen
and shows a **blank** window. Keep the live launch on `viewerOptions`
(`DirectToSwapchain`) and the readback evidence on `evidenceViewerOptions`.

A consumer without a blocking compositor/vsync can bound the live loop with
`ViewerOptions.FrameRateCap = Some n` (default `None` = 60); a headless host with no
compositor free-runs toward the cap — an environment limitation, not a defect. To
exit gracefully, return `[ ViewerEffect.CloseWindow ]` from your `update` (no extra
host effect is needed).

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] — build the pure `SceneNode` values this host renders.
- [[fs-gg-keyboard-input]] — feed normalized `ViewerKey` events into `MapKey`.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven render library): https://github.com/mono/SkiaSharp
