---
name: fs-gg-skiaviewer
description: Work on viewer host contracts and generated product viewer usage.
---

# SkiaViewer Capability

## Scope

Owns `src/SkiaViewer/`, viewer tests, `template/fragments/skiaviewer/`, and generated product viewer startup guidance.

## Public Contract

The supported API lives in `src/SkiaViewer/SkiaViewer.fsi`. Surface changes require `readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt`.

## Build Commands

Run `dotnet build FS.GG.Rendering.slnx`. The capability-catalog, public-surface and local-pack
checks are Expecto tests, not build targets — `dotnet test tests/Package.Tests/Package.Tests.fsproj`
covers all three. After an `.fsi` change, regenerate the baseline with
`dotnet fsi scripts/refresh-surface-baselines.fsx` and commit it: the gate re-runs the generator
and fails on any diff.

## Test Commands

Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` and
`dotnet test tests/Package.Tests/Package.Tests.fsproj` (generated-product and package validation).

## Evidence

Capture real viewer command or package evidence under the active feature
readiness package-surface reports. Stable public surface baselines live under
`readiness/surface-baselines/`. Disclose synthetic native evidence if a
platform window system is unavailable.

## Feature 168 Viewer Evidence Rules

- Package-consuming viewer samples must compare current `FS.GG.UI.` package
  pins and use `scripts/refresh-local-feed-and-samples.fsx` or `package-feed`
  proof so stale package pins and local feed assumptions stay visible.
- Prefer real screenshot evidence; disclose degraded capture, require reviewer
  accepted readiness, and preserve manual caveats outside generated summary or
  managed section rewrites.
- Responsiveness evidence is separate from screenshot readiness: validate
  pointer and keyboard activation, then distinguish input routing from update,
  render, and present latency.
- Canceled, timed-out, skipped, synthetic, substitute, degraded,
  pending-review, or environment-limited checks must keep a visible caveat.

## Package Boundary

Keep native window and render effects at the interpreter edge. Scene descriptions stay in Scene; Elmish adapter behavior stays in Elmish.

## Generated Product

Products that select SkiaViewer receive viewer package references, this skill, and product commands that avoid framework gallery checks.

## Runnable example

Open the package namespace and drive a bounded, headless-friendly run:

```fsharp
open System
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let options = { Title = "demo"; InitialSize = { Width = 320; Height = 240 } }
let scene = Rectangle((0.0, 0.0, 320.0, 240.0), Colors.rgb 16uy 16uy 24uy)

match Viewer.runUntilFirstFrame options scene with
| Ok evidence -> printfn "frames=%d renderer=%s" evidence.FramesRendered evidence.RendererMode
| Error failure -> printfn "blocked: %A" failure.BlockedStage
```

## Pointer-aware host: the `MapPointer` seam (mouse / gamepad)

The default generated game host is `GeneratedAppHost`, launched through `Viewer.runApp` /
`Viewer.runAppWithAudio`. It carries **only** `MapKey` and `Tick` — there is **no pointer field**, so a
`game`-profile product that needs the cursor (the twin-stick archetype: WASD move + mouse aim) cannot
reach it from the default host. The `.fsi` marks the trap directly on `runInteractiveViewerWithAudio`:
*"`runAppWithAudio` … `GeneratedAppHost` has no pointer: a product that needed both got silence."*

The pointer-aware, size-aware variant is `InteractiveViewerHost<'model,'msg>` — a field-for-field mirror
of `GeneratedAppHost` **plus** a model-aware pointer seam, with two signatures widened:

```fsharp
type InteractiveViewerHost<'model,'msg> =
    { Init: unit -> 'model * ViewerEffect list        // same as GeneratedAppHost
      Update: 'msg -> 'model -> 'model * ViewerEffect list
      View: Size -> 'model -> SceneNode               // now Size-aware (was 'model -> SceneNode)
      MapKey: ViewerKey -> bool -> 'msg list          // widened from 'msg option (Some m -> [m], None -> [])
      MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list   // the new seam
      Tick: TimeSpan -> 'msg option
      Diagnostics: ViewerDiagnosticsOptions }
```

**`MapPointer` is model-aware where `MapKey` is not**, and that is why it is a separate seam: a pointer
sample is meaningless without the viewport `Size` and the `'model` (an aim vector is the cursor minus the
player's screen position, which lives in the model). Keep the `MapKey` discipline anyway — wrap the raw
`ViewerPointerInput` into a product message and decide what it means in `update`. Its `'model'` argument is
the only reason the interactive host's `View` had to become `Size`-aware too. The raw sample:

```fsharp
type ViewerPointerInput =
    { Phase: ViewerPointerPhaseKind    // Moved | Pressed | Released | Wheel | Exited (RequireQualifiedAccess)
      X: float                          // scene/swapchain coordinate space — no manual DPI/origin math
      Y: float
      Button: ViewerPointerButtonKind option   // Primary | Secondary | Middle (RequireQualifiedAccess)
      DeltaX: float
      DeltaY: float }
```

### Launchers, and the audio-capable path (#429 / #463)

<!-- skill-refs: closed-ok FS.GG.Rendering#429 — cited as history: the issue that added the audio-capable pointer path (runInteractiveViewerWithAudio) -->
<!-- skill-refs: closed-ok FS.GG.Rendering#463 — cited as history: the code-path prior art that let pointer and audio compose -->

The interactive host has the same launcher family as `runApp`, so pointer and audio compose:

- `Viewer.runInteractiveViewer` — the live pointer/keyboard host (audio dropped, like `runApp`).
- `Viewer.runInteractiveViewerWithAudio` — **the game path**: pointer *and* an `audioSink`. #429/#463
  added this because audio was previously reachable only through `runAppWithAudio`, whose
  `GeneratedAppHost` has no pointer — so a product that needed pointer **and** sound got silence on one.
  `PlayAudio` batches are handed to the sink exactly as under `runAppWithAudio`.
- `-WithWindowBehavior` and `-Script` (`runInteractiveViewerScript*`, for offscreen/headless folds)
  variants mirror the `runApp` set.

The Controls adapter `ControlsElmish.runInteractiveApp` / `runInteractiveAppWithAudio` sits on top for
**control-tree** products (its `View` returns a `Control<'msg>` and its `MapPointer` takes a resolved
`PointerInteraction` — authored `EventBindings` win, `MapPointer` is the fallback). A game that draws its
own `SceneNode` wants the scene-level `InteractiveViewerHost`, not the Controls host.

Swapping a generated game off `GeneratedAppHost`/`runAppWithAudio` is a governance-scanned `Program.fs`
change (the launch expression and the `let generatedHost` / `MapKey = mapKey` / `Tick = tick` bindings are
literal-string pins in the generated product's `GovernanceTests`, so the launch line and the pins move
together — the same coordinated edit persistence adoption needs). The product-facing worked recipe,
with the field-by-field swap table and a full `Program.fs`, is in the generated product's
`fs-gg-skiaviewer` skill under *Wiring a game onto the pointer-aware host*.

## Common pitfalls

- **`Result.Ok`/`Result.Error` shadowed by `ViewerDiagnosticLevel`.** `open
  FS.GG.UI.SkiaViewer` brings `ViewerDiagnosticLevel.Error` (and its peers
  `Warning`/`Info`/`Debug`/`Trace`) into scope, so a **bare** `Ok`/`Error` in a
  match or constructor can bind to the union case instead of the `Result` case —
  e.g. `Error msg` resolves ambiguously and a bare `| Error -> …` arm silently
  matches the diagnostic level, not a failed `Result`. **Remedy:** qualify as
  `Result.Ok` / `Result.Error` when you mean the result type. This is the same
  co-opened-DU collision documented for `Unknown` in the
  [[fs-gg-keyboard-input]] "Common pitfalls" (where `ViewerKey.Unknown` and
  `ViewerRunBlockedStage.Unknown` collide) — qualify the case when two opened
  modules export the same name.

## Live-loop repaint & trace read-back (Feature 175)

- **One repaint policy across every loop.** A no-product-message input may still change runtime state
  (focus traversal, hover, scroll offsets) with NO model change, so the scene must be re-derived from
  `host.View` or the change renders a frame late (the "focus one click behind" / dead-hover / dead-scroll
  class). Both viewer loops route this through the single internal seam
  `Viewer.runtimeStateRepaint producedMessages current deriveScene` (a no-op when messages already drove
  a dispatch+re-derive). Do NOT re-add an inline `currentScene <- host.View …` in a handler — call the
  one policy, so the key-only and full-interactive loops cannot drift apart.
- **Trace read-back (observe live state without a repack).** `RenderLagTrace` writes stderr when
  `FS_GG_RENDER_LAG_TRACE=1` AND, independently, captures in-memory when capture is on. From a test or
  tool: `Viewer.traceStartCapture ()` → drive the interaction → `Viewer.traceDrainCapture () :
  (event, fields) list`. This replaces the add-eprintfn-and-repack loop for diagnosing focus/hover/
  scroll/dispatch timing. (Internal seams via InternalsVisibleTo; the buffer is process-global, so
  assert on the PRESENCE of uniquely-named events.)

## Persistent problems

When a problem outlasts reasonable in-repo attempts, extensive external research is
**mandatory** — consult **official online docs first** (the F#/.NET docs and the driven
library's own documentation/API reference), then community sources (forums, Reddit, Q&A
sites, issue trackers and changelogs). Record the findings and resolving links in the
feature's `specs/<feature>/feedback/` folder and, for durable lessons, in this skill's
**Sources** line. Offline, the mandate degrades to recording "research blocked — <why>"
rather than hard-failing the phase.

## Related

- [[fs-gg-scene]] supplies the scene descriptions this host renders.
- [[fs-gg-elmish]] wires viewer hosting into an Elmish program.

## Sources / links

- F#/.NET docs: https://learn.microsoft.com/en-us/dotnet/fsharp/
- SkiaSharp (driven native rendering library): https://github.com/mono/SkiaSharp
