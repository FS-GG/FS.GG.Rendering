module Issue429InteractiveAudioTests

open System
open Expecto
open FS.GG.Audio.Core
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Issue #429. #245 gave the GAME family a real audio seam (`ViewerEffect.PlayAudio` +
// `Viewer.runAppWithAudio`). The interactive (pointer/size-aware) family never got one, so the two
// seams were mutually exclusive: `runAppWithAudio` has no pointer, `runInteractiveApp` has no audio.
// A product that needs both — every game with a menu, a volume slider, click-to-target — was stuck,
// and the failure was the bad kind: the interactive loop's effect fold left `PlayAudio` in the
// DISCARD group, so the product got silence with no error, no diagnostic, and nothing in the type
// system objecting. It looked wired.
//
// The two loops carried byte-identical effect folds and drifted. They now share ONE
// (`Viewer.interpretViewerEffects`), so there is no second copy to forget — and that fold is where
// these tests aim, because it is the exact code that discarded the audio.
//
// The live persistent runners gate on `runtimeCapability.PersistentWindow` (false headless) and are
// not drivable here — the limitation #365/#396 already record for their loops. So the sink WIRING is
// asserted on the shared fold directly (the seam is `internal` for precisely this reason, as
// `runtimeStateRepaint` is), and the entry points are asserted on the unsupported-host path.

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

let private blip = SoundId "blip"
let private theme = TrackId "theme"

type private Model = { Hits: int }

type private Msg = Hit

/// A pointer-driven host that requests sound the way a game menu does: pure `AudioEffect` values
/// carried on a `ViewerEffect.PlayAudio`, never a device call.
let private interactiveAudioHost: InteractiveViewerHost<Model, Msg> =
    { Init = fun () -> { Hits = 0 }, [ PlayAudio [ Audio.playMusic theme true ] ]
      Update =
        fun Hit model ->
            let next = { Hits = model.Hits + 1 }
            next, [ PlayAudio [ Audio.playSfx blip 0.5 ]; RenderScene(Text((0.0, 0.0), "hit", white)) ]
      View = fun _ model -> Text((0.0, 0.0), $"hits {model.Hits}", white)
      MapKey = fun _ _ -> []
      MapPointer = fun _ _ _ -> [ Hit ]
      Tick = fun _ -> None
      Diagnostics = Viewer.defaultDiagnostics }

/// Drive the shared fold the way a loop does, collecting everything it routed.
let private interpret (effects: ViewerEffect list) =
    let played = ResizeArray<AudioEffect>()
    let scenes = ResizeArray<SceneNode>()
    let diagnostics = ResizeArray<ViewerDiagnosticEvent>()
    let mutable dispatched = false

    let closeRequested =
        Viewer.interpretViewerEffects
            (fun batch -> played.AddRange batch)
            ignore // #535 persistence sink: these cases emit no Persist effect
            scenes.Add
            (fun () -> dispatched <- true)
            diagnostics.Add
            ignore // #444 evidence sink: these cases emit no evidence effect, so nothing reaches it
            effects

    {| Played = List.ofSeq played
       Scenes = List.ofSeq scenes
       Diagnostics = List.ofSeq diagnostics
       Dispatched = dispatched
       CloseRequested = closeRequested |}

[<Tests>]
let tests =
    testList
        "issue-429 interactive audio seam"
        [
          // THE regression. Before #429 this batch reached the interactive loop and every note in it
          // was dropped on the floor. The fold must hand each batch to the sink, in dispatch order.
          test "the shared effect fold hands every PlayAudio batch to the sink, in dispatch order" {
            let result =
                interpret
                    [ RenderScene(Text((0.0, 0.0), "a", white))
                      PlayAudio [ Audio.playSfx blip 0.25; Audio.stopMusic ]
                      ReadPixels
                      PlayAudio [ Audio.playMusic theme false ] ]

            Expect.equal
                result.Played
                [ Audio.playSfx blip 0.25; Audio.stopMusic; Audio.playMusic theme false ]
                "batches reach the sink concatenated in dispatch order — not discarded"
          }

          test "a frame that requests no sound never touches the sink" {
            let result = interpret [ RenderScene(Text((0.0, 0.0), "a", white)); ReadPixels ]
            Expect.isEmpty result.Played "a silent frame plays nothing"
          }

          // The fold is shared by BOTH loops now, so it must still do everything the generated-app loop
          // relied on it for — audio cannot have been bolted on at the cost of the other arms.
          test "the shared fold still routes scene, input-dispatch, diagnostics and close" {
            let scene = Text((0.0, 0.0), "b", white)
            let diagnostic = Viewer.productDefectDiagnostic "View" "boom"

            let result =
                interpret
                    [ RenderScene scene
                      DispatchInput(FS.GG.UI.KeyboardInput.Space, true)
                      EmitDiagnostic diagnostic
                      CloseWindow ]

            Expect.equal result.Scenes [ scene ] "the scene is routed"
            Expect.isTrue result.Dispatched "input dispatch is recorded"
            Expect.equal result.Diagnostics [ diagnostic ] "the diagnostic is captured"
            Expect.isTrue result.CloseRequested "CloseWindow still requests a close"
          }

          test "an effect batch with no CloseWindow does not request a close" {
            let result = interpret [ PlayAudio [ Audio.playSfx blip 1.0 ] ]
            Expect.isFalse result.CloseRequested "audio alone never closes the window"
          }

          // The product's own request survives the pointer route — this is the composition TankSim1
          // could not express: a pointer-driven update that asks for sound.
          test "a pointer-driven update requests sound the interactive host can now realize" {
            let click: ViewerPointerInput =
                { Phase = ViewerPointerPhaseKind.Pressed
                  X = 40.0
                  Y = 20.0
                  Button = Some ViewerPointerButtonKind.Primary
                  DeltaX = 0.0
                  DeltaY = 0.0 }

            let msgs = interactiveAudioHost.MapPointer click { Width = 320; Height = 200 } { Hits = 0 }

            let _, effects =
                msgs |> List.fold (fun (model, acc) msg ->
                    let next, produced = interactiveAudioHost.Update msg model
                    next, acc @ produced) ({ Hits = 0 }, [])

            let evidence = GeneratedAppHost.audioRequests effects |> Audio.interpret

            Expect.equal evidence.Requested [ Audio.playSfx blip 0.5 ] "the pointer interaction requested exactly one sfx"
          }

          test "init effects carry the opening music request on the interactive host too" {
            let _, initEffects = interactiveAudioHost.Init()

            Expect.equal
                (GeneratedAppHost.audioRequests initEffects)
                [ Audio.playMusic theme true ]
                "a pointer-driven game can request music before the first frame"
          }

          test "runInteractiveViewerWithAudio never reaches the sink on a host that cannot open a window" {
            // The sink is only driven once the viewer owns a window. On an unsupported host the audio
            // entry point must fail exactly as `runInteractiveViewer` does, and must play nothing.
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                let options =
                    { Title = "Product"
                      InitialSize = { Width = 640; Height = 480 }
                      PresentMode = ViewerPresentMode.OffscreenReadback
                      FrameRateCap = None
                      LogicalSize = None }

                match Viewer.runInteractiveViewerWithAudio options (fun batch -> played.AddRange batch) interactiveAudioHost with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful launch"
                | Result.Error failure ->
                    Expect.equal
                        failure.Classification
                        UnsupportedEnvironment
                        "runInteractiveViewerWithAudio classifies an unsupported host exactly as runInteractiveViewer does"

                Expect.isEmpty played "no sound is played when no window ever opened"
          }

          test "the sink a product installs drives a real backend end to end" {
            // Exactly the composition a generated product hands to `runInteractiveViewerWithAudio` —
            // `FS.GG.Audio.Host.Audio.play backend` — over the deterministic record-only backend, so it
            // needs no device and no window. This is the step that did not exist before #429 for a
            // pointer-driven product.
            use backend = FS.GG.Audio.Host.NullBackend.create ()
            let sink: AudioEffect list -> unit = FS.GG.Audio.Host.Audio.play backend

            let _, initEffects = interactiveAudioHost.Init()
            let _, clickEffects = interactiveAudioHost.Update Hit { Hits = 0 }

            // Drive the sink through the REAL shared fold, the way the live loop does: one batch per frame.
            let run effects =
                Viewer.interpretViewerEffects sink ignore ignore ignore ignore ignore effects |> ignore

            run initEffects
            run clickEffects

            Expect.equal
                backend.Evidence.Requested
                [ Audio.playMusic theme true; Audio.playSfx blip 0.5 ]
                "the backend received the opening music then the click sfx, in dispatch order"
          } ]
