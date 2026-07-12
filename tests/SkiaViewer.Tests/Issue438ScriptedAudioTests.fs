module Issue438ScriptedAudioTests

open Expecto
open FS.GG.Audio.Core
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Issue #438. #429 gave the interactive host family an audio sink, but only on its NON-scripted entry
// points: `runInteractiveViewerScript`/`…WithWindowBehavior` still handed the core `ignore`, so a
// scripted product emitted `ViewerEffect.PlayAudio` and the batch was dropped with no error and no
// diagnostic. That is the same silent discard #429 was filed about, surviving in the one path the
// evidence and responsiveness tooling actually drives — so "audio was requested during a scripted run"
// was the single thing about sound that could not be observed.
//
// WHAT THESE TESTS CAN AND CANNOT PIN, stated plainly. The persistent runners gate on
// `runtimeCapability.PersistentWindow` (false headless) and are not drivable here — the #365/#396
// limitation their loops already record. So a headless test CANNOT distinguish a scripted runner that
// passes `audioSink` from one that passes `ignore`: with no window, the loop never runs and neither
// sink is ever called. What is asserted instead:
//
//   1. the shared fold — which the scripted path now routes through — hands PlayAudio to the sink;
//   2. the scripted entry points hold the same contract as their #429 siblings on an unsupported host
//      (Error, and nothing played), so a scripted run cannot silently *claim* to have played;
//   3. the sinkless scripted runners still exist and still play nothing (the additive guarantee).
//
// The wiring itself (scripted runner -> core -> fold) is carried by the type system: the core's
// `audioSink` parameter is not optional, so the sinkless runners must name `ignore` explicitly. A
// revert to `ignore` in an audio-capable runner would compile — that gap is the #365/#396 limitation,
// not something this file can close, and it is why the Live runners share ONE body (`runScriptCore`)
// rather than a sinkless copy and an audio copy that could drift.

let private white = { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }

let private blip = SoundId "blip"
let private theme = TrackId "theme"

type private Model = { Ticks: int }

type private Msg = Advance

/// A host driven by a SCRIPT rather than a live pointer — the shape the evidence tooling runs — that
/// requests sound as pure `AudioEffect` values on a `ViewerEffect.PlayAudio`.
let private scriptedAudioHost: InteractiveViewerHost<Model, Msg> =
    { Init = fun () -> { Ticks = 0 }, [ PlayAudio [ Audio.playMusic theme true ] ]
      Update =
        fun Advance model ->
            let next = { Ticks = model.Ticks + 1 }
            next, [ PlayAudio [ Audio.playSfx blip 0.75 ]; RenderScene(Text((0.0, 0.0), "tick", white)) ]
      View = fun _ model -> Text((0.0, 0.0), $"ticks {model.Ticks}", white)
      MapKey = fun _ _ -> [ Advance ]
      MapPointer = fun _ _ _ -> []
      Tick = fun _ -> None
      Diagnostics = Viewer.defaultDiagnostics }

/// Drive the shared fold the way a scripted loop does, collecting what reached the sink.
let private playedBy (effects: ViewerEffect list) =
    let played = ResizeArray<AudioEffect>()

    Viewer.interpretViewerEffects
        (fun batch -> played.AddRange batch)
        ignore // #535 persistence sink: this script emits no Persist effect
        ignore
        ignore
        ignore
        ignore // #444 evidence sink: this script emits no evidence effect
        effects
    |> ignore

    List.ofSeq played

let private options: ViewerOptions =
    { Title = "Product"
      InitialSize = { Width = 640; Height = 480 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None
      LogicalSize = None }

[<Tests>]
let tests =
    testList
        "issue-438 scripted audio seam"
        [
          // THE regression. A scripted run's effects now route through the same fold the live loops
          // use, so the notes a scripted product asks for reach the sink instead of the floor.
          test "a scripted run's PlayAudio batches reach the sink, in dispatch order" {
            let _, initEffects = scriptedAudioHost.Init()
            let _, updateEffects = scriptedAudioHost.Update Advance { Ticks = 0 }

            Expect.equal
                (playedBy (initEffects @ updateEffects))
                [ Audio.playMusic theme true; Audio.playSfx blip 0.75 ]
                "the opening music and the scripted tick's sfx both reach the sink — not discarded"
          }

          test "a scripted frame that requests no sound never touches the sink" {
            Expect.isEmpty
                (playedBy [ RenderScene(Text((0.0, 0.0), "silent", white)) ])
                "a silent scripted frame plays nothing"
          }

          // Parity with #429's entry-point test: an unsupported host must not report success, and must
          // certainly not claim to have played anything. A scripted run that cannot open a window is a
          // failure, not a silent success with no audio.
          test "runInteractiveViewerScriptWithAudio never reaches the sink on a host that cannot open a window" {
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                match
                    Viewer.runInteractiveViewerScriptWithAudio
                        options
                        []
                        (fun batch -> played.AddRange batch)
                        scriptedAudioHost
                with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful scripted launch"
                | Result.Error failure ->
                    Expect.equal
                        failure.Classification
                        UnsupportedEnvironment
                        "the scripted audio runner classifies an unsupported host exactly as its sinkless twin does"

                Expect.isEmpty played "no sound is played when no window ever opened"
          }

          test "runInteractiveViewerScriptWithWindowBehaviorAndAudio holds the same contract" {
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                match
                    Viewer.runInteractiveViewerScriptWithWindowBehaviorAndAudio
                        options
                        Viewer.defaultWindowBehavior
                        []
                        (fun batch -> played.AddRange batch)
                        scriptedAudioHost
                with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful scripted launch"
                | Result.Error failure ->
                    Expect.equal failure.Classification UnsupportedEnvironment "same classification as the sinkless twin"

                Expect.isEmpty played "no sound is played when no window ever opened"
          }

          // The additive guarantee: #438 must not change what the sinkless scripted runners do.
          test "the sinkless scripted runner still refuses an unsupported host, exactly as before" {
            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                match Viewer.runInteractiveViewerScript options [] scriptedAudioHost with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful scripted launch"
                | Result.Error failure ->
                    Expect.equal failure.Classification UnsupportedEnvironment "unchanged by #438"
          }
        ]
