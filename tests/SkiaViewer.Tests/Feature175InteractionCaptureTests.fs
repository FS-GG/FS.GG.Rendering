module Feature175InteractionCaptureTests

// Feature 175 S1 — close the "drive interaction → capture the resulting frame" loop, deterministically
// and headlessly. The enabling primitive is `ControlsElmish.Perf.runScriptToModel`: it folds a
// `FrameInput` script (clicks/keys/ticks) through the REAL retained route and returns the FINAL model,
// so we can render the POST-interaction frame and capture it offscreen — instead of capturing only a
// static initial page. The state half needs no GL (pure fold); the PNG readback degrades-and-discloses
// on a no-GL host (Feature-168 evidence honesty: a non-proven capture is disclosed, never a silent pass).

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = Inc

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 160 }

// A button whose label reflects a counter — clicking it is a VISIBLE post-interaction state change.
let private view (_: Size) (n: int) : Control<Msg> =
    Button.create [ Button.text (sprintf "count: %d" n); Button.onClick Inc ]
    |> Control.withKey "btn"
    |> fun b -> Stack.create [ Stack.children [ b ] ]

let private host: InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun Inc n -> n + 1, []
      View = view
      Theme = theme
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

/// Offscreen-capture a rendered scene to `outPath` (mirrors the sample evidence capture path).
let private capture (outPath: string) (scene: SceneNode) : ScreenshotEvidenceResult =
    let request: ScreenshotEvidenceRequest =
        { Command = "interaction-capture"
          AppOrSample = "feature175-s1"
          OutputPath = outPath
          Width = size.Width
          Height = size.Height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 10.0 }
    let options: ViewerOptions =
        { Title = "feature175-s1"
          InitialSize = size
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }
    Viewer.captureScreenshotEvidence request options scene

// Sequenced (feature 203, US4/T024): captures real frames through the shared, single-threaded SceneRenderer.
[<Tests>]
let tests =
    testSequenced
    <| testList "Feature175InteractionCapture" [
        test "driving a click script changes the model, and the resulting frame is captured (or disclosed)" {
            // Locate the button in the initial frame so the script's click lands on it.
            let initial = Control.renderTree theme size (view size 0)
            let _, rect = initial.Bounds |> List.find (fun (id, _) -> id = "btn")
            let cx, cy = rect.X + rect.Width / 2.0, rect.Y + rect.Height / 2.0

            let script: FrameInput<Msg> list =
                [ FrameInput.Pointer(HoverEnter("btn", cx, cy))
                  FrameInput.Pointer(Click("btn", PointerButton.Primary, cx, cy))
                  FrameInput.Idle ]

            // DRIVE: fold the script to the final model via the new primitive.
            let finalModel, metrics = ControlsElmish.Perf.runScriptToModel host size script

            // The deterministic, GL-free proof that the interaction actually drove state.
            Expect.equal finalModel 1 "the scripted click incremented the counter (post-interaction model)"
            Expect.isNonEmpty metrics "the script produced per-frame metrics"

            // CAPTURE: render the POST-interaction frame and read it back offscreen.
            let dir = Path.Combine(Path.GetTempPath(), "feature175-s1-capture")
            Directory.CreateDirectory dir |> ignore
            let outPath = Path.Combine(dir, "after-click.png")
            if File.Exists outPath then File.Delete outPath

            let postScene = SceneNode.Group [ (Control.renderTree theme size (view size finalModel)).Scene ]

            let shot =
                try Result.Ok(capture outPath postScene)
                with ex -> Result.Error ex.Message

            // Feature-168 honesty: a real capture proves a non-empty PNG; a no-GL host discloses an
            // unproven result. Either is acceptable; a CRASH or a silent empty file is not.
            match shot with
            | Result.Ok result when result.ProvesScreenshot ->
                Expect.isTrue (File.Exists outPath) "a proven screenshot wrote its PNG"
                Expect.isGreaterThan (FileInfo(outPath).Length) 0L "the captured PNG is non-empty"
            | Result.Ok result ->
                // Environment-limited: disclosed, not a silent green.
                Expect.isFalse result.ProvesScreenshot "capture unavailable on this host is disclosed, not claimed"
            | Result.Error message ->
                failtestf "capture raised instead of degrading-and-disclosing: %s" message
        }
    ]
