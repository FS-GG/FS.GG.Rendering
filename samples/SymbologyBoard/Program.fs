module SymbologyBoard.Program

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default
open SymbologyBoard.Board

// The fixed seed + scripted tick sequence for the headless evidence run (no wall clock): advance the board
// through 120 whole fixed steps so every unit has moved and bounced at least once before fingerprinting.
[<Literal>]
let private Seed = 1

let private boardScript: Msg list = [ for _ in 1..120 -> Tick dt ]

// Presentation only (kept out of the pure Board core): paint the interpolated board into a volatile canvas
// sized to the board extent. The host `Tick` drives the simulation; the model carries all state.
let private view (_: Size) (model: Model) : Control<Msg> =
    Canvas.create
        [ Attr.width BoardWidth
          Attr.height BoardHeight
          Canvas.volatile'
          Canvas.scene (renderScene model) ]
    |> Control.withKey "board"

let private host: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> init Seed, []
      Update = fun msg model -> update msg model, []
      View = view
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      // The board animates continuously; emit a fixed-interval tick every frame.
      Tick = fun _ -> Some(Tick dt)
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

// The `evidence` subcommand is the repro-CHECK wrapper around the pure `Board.evidence` fingerprint: call
// it twice from the SAME seed + script and compare. Never report divergence as success (FR-005/Constitution
// VI). Canonical strings live in contracts/cli-contract.md.
let private runEvidence () =
    let a = evidence Seed boardScript
    let b = evidence Seed boardScript
    printfn "symbology-board: seeded fingerprint = %s" a

    if a = b then
        printfn "symbology-board: reproducible (two runs byte-identical)."
        0
    else
        eprintfn "symbology-board: NON-REPRODUCIBLE — %s <> %s" a b
        1

let private runInteractive () =
    let capability = Viewer.runtimeCapability ()

    if not capability.PersistentWindow then
        printfn "symbology-board: interactive mode skipped — no live window/GL host."
        0
    else
        let options: ViewerOptions =
            { Title = "Symbology Board — live roster"
              InitialSize = { Width = int BoardWidth; Height = int BoardHeight }
              PresentMode = ViewerPresentMode.DirectToSwapchain
              FrameRateCap = Some 60 }

        match ControlsElmish.runInteractiveApp options host with
        | Result.Ok outcome ->
            printfn "symbology-board: interactive session ended (status=%s)." outcome.Status
            0
        | Result.Error failure ->
            eprintfn "symbology-board: interactive launch failed: %s" failure.Message
            1

[<EntryPoint>]
let main argv =
    match Array.toList argv with
    | "interactive" :: _ -> runInteractive ()
    | "evidence" :: _
    | [] -> runEvidence ()
    | other :: _ ->
        eprintfn "symbology-board: unknown subcommand '%s' (use 'evidence' or 'interactive')." other
        1
