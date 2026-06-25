module CanvasDemo.Program

open System
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default
open CanvasDemo.Game

// A scripted, deterministic input/tick sequence for the evidence run (no wall clock).
let private demoScript: Msg list =
    [ for i in 0 .. 119 do
          if i % 20 = 0 then yield Point { Phase = PointerPhase.Moved; X = float (i % 320); Y = 180.0; Button = None; DeltaX = 0.0; DeltaY = 0.0 }
          yield Tick dt ]

let private host: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> init 1, []
      Update = fun msg model -> update msg model, []
      View = view
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      // The canvas animates continuously; emit a fixed-interval tick every frame.
      Tick = fun _ -> Some(Tick dt)
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private runEvidence () =
    // Reproducibility: two independent runs from the same seed + script yield the same fingerprint.
    let a = evidence 1 demoScript
    let b = evidence 1 demoScript
    printfn "canvas-demo: seeded fingerprint = %s" a
    if a = b then
        printfn "canvas-demo: reproducible (two runs byte-identical)."
        0
    else
        eprintfn "canvas-demo: NON-REPRODUCIBLE — %s <> %s" a b
        1

let private runInteractive () =
    let capability = Viewer.runtimeCapability ()
    if not capability.PersistentWindow then
        printfn "canvas-demo: interactive mode skipped — no live window/GL host."
        0
    else
        let options: ViewerOptions =
            { Title = "Canvas Demo — bouncing ball"
              InitialSize = { Width = 360; Height = 260 }
              PresentMode = ViewerPresentMode.DirectToSwapchain
              FrameRateCap = Some 60 }

        match ControlsElmish.runInteractiveApp options host with
        | Result.Ok outcome ->
            printfn "canvas-demo: interactive session ended (status=%s)." outcome.Status
            0
        | Result.Error failure ->
            eprintfn "canvas-demo: interactive launch failed: %s" failure.Message
            1

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "interactive" :: _ -> runInteractive ()
    | "evidence" :: _
    | [] -> runEvidence ()
    | other :: _ ->
        eprintfn "canvas-demo: unknown subcommand '%s' (use 'evidence' or 'interactive')." other
        1
