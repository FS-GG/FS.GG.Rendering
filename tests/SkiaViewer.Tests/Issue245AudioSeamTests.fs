module Issue245AudioSeamTests

open System
open Expecto
open FS.GG.Audio.Core
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

type AudioModel = { Hits: int }

type AudioMsg =
    | Hit
    | Quiet

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

let private blip = SoundId "blip"
let private theme = TrackId "theme"

/// A host whose `update` requests sound the way a game does: pure `AudioEffect` values carried out
/// on a `ViewerEffect.PlayAudio`, never a device call.
let private audioHost =
    { Init = fun () -> { Hits = 0 }, [ PlayAudio [ Audio.playMusic theme true ] ]
      Update =
        fun msg model ->
            match msg with
            | Hit ->
                let next = { Hits = model.Hits + 1 }
                next, [ PlayAudio [ Audio.playSfx blip 0.5 ]; RenderScene(Text((0.0, 0.0), "hit", white)) ]
            | Quiet -> model, [ RenderScene(Text((0.0, 0.0), "quiet", white)) ]
      View = fun model -> Text((0.0, 0.0), $"hits {model.Hits}", white)
      MapKey = fun key isDown -> if isDown && key = Space then Some Hit else None
      Tick = fun _ -> None
      Diagnostics = Viewer.defaultDiagnostics }

[<Tests>]
let tests =
    testList
        "issue-245 audio host seam"
        [ test "audioRequests flattens every PlayAudio batch in dispatch order and drops non-audio effects" {
            let effects =
                [ RenderScene(Text((0.0, 0.0), "a", white))
                  PlayAudio [ Audio.playSfx blip 0.25; Audio.stopMusic ]
                  ReadPixels
                  PlayAudio [ Audio.playMusic theme false ]
                  CloseWindow ]

            Expect.equal
                (GeneratedAppHost.audioRequests effects)
                [ Audio.playSfx blip 0.25; Audio.stopMusic; Audio.playMusic theme false ]
                "batches concatenate in order, and only audio survives"
          }

          test "audioRequests is empty when a frame requested no sound" {
            let effects = [ RenderScene(Text((0.0, 0.0), "a", white)); CloseWindow ]
            Expect.isEmpty (GeneratedAppHost.audioRequests effects) "a silent frame requests nothing"
          }

          test "a product's requested sound survives the viewer boundary as AudioEvidence" {
            // This is the assertion Breakout1 could only make against its own model: what `update`
            // requested is now readable at the host boundary, with no device and no window.
            let _, effects =
                GeneratedAppHost.dispatchKey audioHost { RawKey = "Space"; Direction = ViewerKeyDirection.KeyDown } { Hits = 0 }

            let evidence = GeneratedAppHost.audioRequests effects |> Audio.interpret

            Expect.equal evidence.Requested [ Audio.playSfx blip 0.5 ] "the keypress requested exactly one sfx"
          }

          test "keyboard dispatch still routes the model update alongside the audio request" {
            let model, effects =
                GeneratedAppHost.dispatchKey audioHost { RawKey = "Space"; Direction = ViewerKeyDirection.KeyDown } { Hits = 0 }

            Expect.equal model.Hits 1 "update ran"
            Expect.exists effects (function RenderScene _ -> true | _ -> false) "the frame still renders"
            Expect.exists effects (function PlayAudio _ -> true | _ -> false) "and still requests audio"
          }

          test "init effects carry the opening music request" {
            let _, initEffects = audioHost.Init()

            Expect.equal
                (GeneratedAppHost.audioRequests initEffects)
                [ Audio.playMusic theme true ]
                "a game can request music before the first frame"
          }

          test "runAppWithAudio never reaches the sink on a host that cannot open a window" {
            // The sink is only driven once the viewer owns a window. On an unsupported host
            // `runAppWithAudio` must fail exactly as `runApp` does, and must not play anything.
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                let options =
                    { Title = "Product"
                      InitialSize = { Width = 640; Height = 480 }
                      PresentMode = ViewerPresentMode.OffscreenReadback
                      FrameRateCap = None; LogicalSize = None }

                match Viewer.runAppWithAudio options (fun batch -> played.AddRange batch) audioHost with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful launch"
                | Result.Error failure ->
                    Expect.equal
                        failure.Classification
                        UnsupportedEnvironment
                        "runAppWithAudio classifies an unsupported host exactly as runApp does"

                Expect.isEmpty played "no sound is played when no window ever opened"
          }

          test "the sink the template installs drives a real backend end to end" {
            // Exactly the composition `Program.fs` hands to `runAppWithAudio` — `Audio.play backend` —
            // but over the deterministic record-only backend, so it needs no device and no window.
            // This is the step that did not exist before #245: a request made in `update` reaching
            // FS.GG.Audio.Host.
            use backend = FS.GG.Audio.Host.NullBackend.create ()
            let sink: AudioEffect list -> unit = FS.GG.Audio.Host.Audio.play backend

            let _, initEffects = audioHost.Init()
            let _, keyEffects =
                GeneratedAppHost.dispatchKey audioHost { RawKey = "Space"; Direction = ViewerKeyDirection.KeyDown } { Hits = 0 }

            // Drive the sink the way the viewer's effect interpreter does: one batch per frame.
            sink (GeneratedAppHost.audioRequests initEffects)
            sink (GeneratedAppHost.audioRequests keyEffects)

            Expect.equal
                backend.Evidence.Requested
                [ Audio.playMusic theme true; Audio.playSfx blip 0.5 ]
                "the backend received the opening music then the keypress sfx, in dispatch order"
          } ]
