module Issue438LiveScriptedAudioTests

// Issue #438 — the Controls `Live` scripted runners can now request sound. #429 gave the Controls host
// family a sink on `runInteractiveApp`, but `Live.runScript`/`runScriptWithWindowBehavior` still handed
// the viewer core `ignore`, so a scripted run of an authored control tree emitted `PlayAudio` and the
// batch was discarded with no error and no diagnostic. The scripted runners are what the evidence and
// responsiveness tooling drives, so this was the one path on which "the product asked for a sound"
// could not be observed at all.
//
// As with #429, the live launch is GL-bound and not drivable headless (the #365/#396 limitation), so
// the scripted entry points are asserted on the unsupported-host path. What IS driven here is the part
// that matters and is deterministic: a scripted click on an authored Button resolves its `onClick`, and
// the `PlayAudio` the update answers with survives the same adapter route the scripted runner uses —
// i.e. the values the sink will receive are exactly the ones the product asked for.

open Expecto
open FS.GG.Audio.Core
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg = Fired

type private Model = { Fired: bool }

let private size = { Width = 320; Height = 200 }

let private blip = SoundId "blip"
let private theme = TrackId "theme"

let private view (_: Size) (_: Model) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "Go"; Button.onClick Fired ] |> Control.withKey "go" ] ]

let private update (Fired) (model: Model) : Model * ViewerEffect list =
    { model with Fired = true }, [ PlayAudio [ Audio.playSfx blip 0.75 ] ]

let private host: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Fired = false }, [ PlayAudio [ Audio.playMusic theme true ] ]
      Update = update
      View = view
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private options: ViewerOptions =
    { Title = "Product"
      InitialSize = size
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None
      LogicalSize = None }

[<Tests>]
let tests =
    testList
        "issue-438 Live scripted audio"
        [
          // The values half: what a scripted interaction asks for is exactly what the sink will get.
          test "a scripted run's init and update both request the sound the product asked for" {
            let _, initEffects = host.Init()
            let _, updateEffects = host.Update Fired { Fired = false }

            Expect.equal
                (GeneratedAppHost.audioRequests (initEffects @ updateEffects))
                [ Audio.playMusic theme true; Audio.playSfx blip 0.75 ]
                "the opening music and the scripted click's sfx are both requested — neither is discarded"
          }

          // The entry-point half: the audio-capable scripted runner must classify an unsupported host
          // exactly as its sinkless twin does, and must never claim to have played anything.
          test "Live.runScriptWithAudio never reaches the sink on a host that cannot open a window" {
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                match ControlsElmish.Live.runScriptWithAudio options (fun batch -> played.AddRange batch) host [] with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful scripted launch"
                | Result.Error failure ->
                    Expect.equal
                        failure.Classification
                        UnsupportedEnvironment
                        "Live.runScriptWithAudio classifies an unsupported host exactly as Live.runScript does"

                Expect.isEmpty played "no sound is played when no window ever opened"
          }

          test "Live.runScriptWithWindowBehaviorAndAudio holds the same contract" {
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                match
                    ControlsElmish.Live.runScriptWithWindowBehaviorAndAudio
                        options
                        Viewer.defaultWindowBehavior
                        (fun batch -> played.AddRange batch)
                        host
                        []
                with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful scripted launch"
                | Result.Error failure ->
                    Expect.equal failure.Classification UnsupportedEnvironment "same classification as the sinkless twin"

                Expect.isEmpty played "no sound is played when no window ever opened"
          }

          // The additive guarantee: the sinkless Live runner is unchanged by #438.
          test "the sinkless Live.runScript still refuses an unsupported host, exactly as before" {
            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                match ControlsElmish.Live.runScript options host [] with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful scripted launch"
                | Result.Error failure -> Expect.equal failure.Classification UnsupportedEnvironment "unchanged by #438"
          }
        ]
