/// The shared MVU host bridge + the closure-erased `SampleEntry` + the one evidence
/// runner reused by all six samples (research R2/R3/R5, contracts/sample-registry.md).
///
/// `SampleEntry` is non-generic: each sample's `Model`/`Msg` are erased behind closures
/// (`RunEvidence`/`Interactive`), so `Registry.all` can hold six heterogeneous MVU apps in
/// one list. `host` generalizes G1's `Host.create` with a **non-None-capable `Tick`** (the
/// game-loop seam, R3). `evidenceFor` replays the seeded script via `Perf.runScript` for the
/// golden state, folds the host over the script to derive the achieved `Outcome`, attempts an
/// offscreen screenshot (degrade-and-disclose on any failure), and writes the record.
module SampleApps.Core.Harness

open System
open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Layout
open FS.GG.UI.Themes.Default.Theming
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.KeyboardInput
open FS.GG.UI.DesignSystem
open SampleApps.Core.Evidence

/// The offscreen evidence render size (as G1).
let size: Size = { Width = 1024; Height = 768 }

/// A fixed-size drawing surface: wraps an author-supplied `Scene` via the framework's public
/// `CustomControl` canvas seam, so games paint real colored shapes (`SceneNode.Rectangle` etc.)
/// rather than ASCII text — rendered identically in the live window and the offscreen capture.
/// No new framework control is added; this is a consumer use of the existing public surface.
let canvas (id: string) (widthPx: float) (heightPx: float) (draw: unit -> Scene): Control<'msg> =
    let definition: CustomControlDefinition<'msg> =
        { Id = id
          Measure = fun () -> widthPx, heightPx
          Render = draw
          Draw = draw
          Layout =
            fun () ->
                { Defaults.layoutNode id with
                    Intent = { Defaults.layoutIntent with Size = { Width = Some widthPx; Height = Some heightPx } }
                    Content = Some(draw ()) }
          Clip = Some(0.0, 0.0, widthPx, heightPx)
          Effects = []
          HitTest = fun _ _ -> true
          Event = fun _ -> None
          Accessibility = None
          Diagnostics = [] }
    CustomControl.create definition []

/// The closure-erased registry element (research R2). Its sample-specific `Model`/`Msg`
/// live inside the `RunEvidence`/`Interactive` closures, so the type carries no parameter.
type SampleEntry =
    { Id: string
      Family: string
      Title: string
      Controls: string list
      Inputs: string list
      RunEvidence: int -> string -> SampleEvidenceRecord
      Interactive: ThemeMode -> int
      Outcome: ExpectedOutcome }

/// Build a host from a sample's pure pieces. Effects are always empty — all I/O happens at
/// the App edge (Principle IV). `tick` is non-None for games (the gravity/advance/step seam).
let host
    (init: unit -> 'model)
    (update: 'msg -> 'model -> 'model)
    (view: Size -> 'model -> Control<'msg>)
    (mapKey: ViewerKey -> bool -> 'msg option)
    (mapPointer: PointerInteraction -> 'msg option)
    (tick: TimeSpan -> 'msg option)
    (theme: Theme)
    : InteractiveAppHost<'model, 'msg> =
    { Init = fun () -> init (), []
      Update = fun msg model -> update msg model, []
      View = view
      Theme = theme
      MapKey = mapKey
      MapPointer = mapPointer
      Tick = tick
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// Replay a seeded `FrameInput` script through a host's pure `Update`, returning the final
/// model. Mirrors the live repaint loop's input routing (`MapKeyChord` before `MapKey`; a
/// `Key` frame is a press; `Tick` advances time) so the derived outcome matches what the
/// running app reaches. Pure — no GL, no wall-clock.
let replay (host: InteractiveAppHost<'model, 'msg>) (script: FrameInput<'msg> list): 'model =
    let mutable model = fst (host.Init())
    for input in script do
        let msgOpt =
            match input with
            | FrameInput.Key(k, mods) ->
                match host.MapKeyChord k mods with
                | Some m -> Some m
                | None -> host.MapKey k true
            | FrameInput.Pointer p -> host.MapPointer p
            | FrameInput.Tick dt -> host.Tick dt
            | FrameInput.Idle -> None
        match msgOpt with
        | Some msg -> model <- fst (host.Update msg model)
        | None -> ()
    model

/// The golden state outcome from the deterministic `Perf.runScript` driver (no GL).
let goldenStateFor (host: InteractiveAppHost<'model, 'msg>) (script: FrameInput<'msg> list): string =
    Evidence.goldenState (ControlsElmish.Perf.runScript host size script)

/// Build a record WITHOUT touching the filesystem or GL — the pure path the Expecto suites
/// bind to (build-outcome, determinism, degrade). The screenshot is the disclosed-degrade
/// summary, so the record is byte-stable and the CI signal never depends on a display.
let recordFor
    (sampleId: string)
    (host: InteractiveAppHost<'model, 'msg>)
    (script: FrameInput<'msg> list)
    (deriveOutcome: 'model -> ExpectedOutcome)
    (seed: int)
    : SampleEvidenceRecord =
    let metrics = ControlsElmish.Perf.runScript host size script
    let outcome = deriveOutcome (replay host script)
    Evidence.build sampleId seed outcome metrics (Evidence.degraded "pure suite: capture not attempted")

/// Attempt an offscreen screenshot of the final model. Degrade-and-disclose on any GL
/// failure — never a fabricated frame (FR-008/R-E4). A sample may supply `sceneOf` to paint a
/// bespoke `Scene` directly (games draw a colored board via the public `Scene` primitives);
/// otherwise the control tree is rendered via `Control.renderTree`.
let private capture (sceneOf: (Size -> 'model -> Scene) option) (host: InteractiveAppHost<'model, 'msg>) (model: 'model) (sampleId: string) (outPath: string): ScreenshotEvidenceResult =
    let scene =
        match sceneOf with
        | Some paint -> SceneNode.Group [ paint size model ]
        | None -> SceneNode.Group [ (Control.renderTree host.Theme size (host.View size model)).Scene ]
    let request: ScreenshotEvidenceRequest =
        { Command = "evidence"
          AppOrSample = sampleId
          OutputPath = outPath
          Width = size.Width
          Height = size.Height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 10.0 }
    let options: ViewerOptions =
        { Title = sprintf "sample-apps-evidence-%s" sampleId
          InitialSize = size
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }
    Viewer.captureScreenshotEvidence request options scene

/// The production evidence runner reused by every `SampleEntry.RunEvidence`: writes
/// `state.txt`, derives + records the `Outcome`, attempts an offscreen screenshot
/// (degrade-and-disclose), and writes `run.json` / `summary.md` (+ `frame.png` iff proven)
/// under `<outDir>/<seed>/<sampleId>/`. Always succeeds (exit 0) including disclosed
/// degraded runs (FR-008/FR-014).
let evidenceForWith
    (sceneOf: (Size -> 'model -> Scene) option)
    (sampleId: string)
    (host: InteractiveAppHost<'model, 'msg>)
    (script: FrameInput<'msg> list)
    (deriveOutcome: 'model -> ExpectedOutcome)
    (seed: int)
    (outDir: string)
    : SampleEvidenceRecord =
    let dir = Path.Combine(outDir, string seed, sampleId)
    Directory.CreateDirectory(dir) |> ignore

    // 1. deterministic golden state (no GL needed).
    let metrics = ControlsElmish.Perf.runScript host size script
    File.WriteAllText(Path.Combine(dir, "state.txt"), Evidence.goldenState metrics)

    // 2. achieved outcome from the final model.
    let finalModel = replay host script
    let outcome = deriveOutcome finalModel

    // 3. screenshot — degrade-and-disclose on any GL/capture failure.
    let framePath = Path.Combine(dir, "frame.png")
    let summary =
        try
            let shot = capture sceneOf host finalModel sampleId framePath
            let path = if shot.ProvesScreenshot && File.Exists framePath then Some "frame.png" else None
            Evidence.ofScreenshotResult shot path
        with ex ->
            Evidence.degraded (sprintf "screenshot capture raised: %s" ex.Message)

    // never leave an unproven/stale frame.png on disk.
    if summary.Path.IsNone && File.Exists framePath then
        File.Delete framePath

    // 4. write the record.
    let record = Evidence.build sampleId seed outcome metrics summary
    File.WriteAllText(Path.Combine(dir, "run.json"), Evidence.toRunJson record)
    File.WriteAllText(Path.Combine(dir, "summary.md"), Evidence.toSummaryMd record)
    record

/// The control-rendered evidence runner (productivity samples — their text/form control tree
/// renders directly).
let evidenceFor sampleId host script deriveOutcome seed outDir =
    evidenceForWith None sampleId host script deriveOutcome seed outDir

/// The scene-painted evidence runner (games — `paint` draws a bespoke colored `Scene`).
let evidenceForScene (paint: Size -> 'model -> Scene) sampleId host script deriveOutcome seed outDir =
    evidenceForWith (Some paint) sampleId host script deriveOutcome seed outDir

/// Common GL-gating disclosure used by both interactive paths.
let private discloseNoWindow (capability: ViewerRuntimeCapability): int =
    printfn "sample-apps: interactive mode skipped — no live window/GL host."
    let reasons = capability.UnsupportedHostReasons
    if not (List.isEmpty reasons) then
        printfn "  reason: %s" (String.concat "; " reasons)
    else
        printfn "  reason: renderer mode '%s' reports no persistent window." capability.RendererMode
    0

/// GL-gated **scene-based** interactive launch: paints a raw `SceneNode` each frame via
/// `Viewer.runInteractiveViewer`, so a game's real colored board (not a control placeholder)
/// shows in the live window and advances on the real frame tick. Same degrade-and-disclose
/// posture as the control path (FR-008).
let runInteractiveScene
    (title: string)
    (windowSize: Size)
    (init: unit -> 'model)
    (update: 'msg -> 'model -> 'model)
    (renderScene: Size -> 'model -> Scene)
    (mapKey: ViewerKey -> bool -> 'msg option)
    (tick: TimeSpan -> 'msg option)
    : int =
    let capability = Viewer.runtimeCapability ()
    if not capability.PersistentWindow then
        discloseNoWindow capability
    else
        let host: InteractiveViewerHost<'model, 'msg> =
            { Init = fun () -> init (), []
              Update = fun msg model -> update msg model, []
              View = fun sz model -> SceneNode.Group [ renderScene sz model ]
              MapKey = fun k pressed -> match mapKey k pressed with | Some m -> [ m ] | None -> []
              MapPointer = fun _ _ _ -> []
              Tick = tick
              Diagnostics = Viewer.defaultDiagnostics }
        let options: ViewerOptions =
            { Title = title
              InitialSize = windowSize
              PresentMode = ViewerPresentMode.DirectToSwapchain
              FrameRateCap = Some 60 }
        match Viewer.runInteractiveViewer options host with
        | Result.Ok outcome ->
            printfn "sample-apps: interactive session ended (status=%s)." outcome.Status
            0
        | Result.Error failure ->
            eprintfn "sample-apps: interactive launch failed: %s" failure.Message
            1

/// GL-gated interactive launch (mirror of G1's `Interactive.run`). On a no-window/no-GL
/// host it discloses the reason and exits 0 without launching — it never hangs and never
/// fakes a successful render (FR-008).
let runInteractive (title: string) (host: InteractiveAppHost<'model, 'msg>): int =
    let capability = Viewer.runtimeCapability ()
    if not capability.PersistentWindow then
        printfn "sample-apps: interactive mode skipped — no live window/GL host."
        let reasons = capability.UnsupportedHostReasons
        if not (List.isEmpty reasons) then
            printfn "  reason: %s" (String.concat "; " reasons)
        else
            printfn "  reason: renderer mode '%s' reports no persistent window." capability.RendererMode
        0
    else
        let options: ViewerOptions =
            { Title = title
              InitialSize = { Width = 1280; Height = 800 }
              PresentMode = ViewerPresentMode.DirectToSwapchain
              FrameRateCap = Some 60 }
        // `Result.Ok`/`Result.Error` are qualified: a viewer namespace also defines an `Ok`
        // union case which would otherwise shadow the F# Result constructors.
        match ControlsElmish.runInteractiveApp options host with
        | Result.Ok outcome ->
            printfn "sample-apps: interactive session ended (status=%s)." outcome.Status
            0
        | Result.Error failure ->
            eprintfn "sample-apps: interactive launch failed: %s" failure.Message
            1
