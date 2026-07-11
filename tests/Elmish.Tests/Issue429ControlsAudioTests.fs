module Issue429ControlsAudioTests

// Issue #429 — the Controls host family can now request sound. Before this, `runInteractiveApp` (the
// only entry point whose `View` returns a `Control` and whose host has a pointer) had no audio sink,
// and `runAppWithAudio` (the only one with a sink) takes a `GeneratedAppHost` whose `View` returns a
// bare `SceneNode` — so `Button`/`Slider` could not even be AUTHORED on it. A start screen, a volume
// slider, click-to-target: all needed both, and got silence.
//
// These drive the REAL adapter path (`routeInteractivePointer` — the same routing `runInteractiveApp`
// wires) with a real control tree, so what is asserted is what a menu button actually does: a click
// resolves an authored `onClick`, `update` answers with a `PlayAudio`, and the sink the host installs
// receives it. The live launch itself is GL-bound and not drivable headless (the limitation #365/#396
// record), so `runInteractiveAppWithAudio` is asserted on the unsupported-host path.

open Expecto
open FS.GG.Audio.Core
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg =
    | StartPressed
    | VolumeChanged of float

type private Model = { Started: bool; Volume: float }

let private size = { Width = 320; Height = 200 }

let private click = SoundId "ui-click"
let private music = TrackId "menu-theme"

/// A start screen: a Button that plays a click when pressed. Authored the documented way — an
/// `onClick` binding, no `MapPointer` clauses.
let private menuView (_: Size) (_: Model) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "Start"; Button.onClick StartPressed ] |> Control.withKey "start" ] ]

let private update (msg: Msg) (model: Model) : Model * ViewerEffect list =
    match msg with
    | StartPressed ->
        // The whole point of #429: a pointer-driven update asking for sound.
        { model with Started = true }, [ PlayAudio [ Audio.playSfx click 1.0 ] ]
    | VolumeChanged v -> { model with Volume = v }, [ PlayAudio [ Audio.setBusVolume Bus.Music v ] ]

let private menuHost: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> { Started = false; Volume = 0.5 }, [ PlayAudio [ Audio.playMusic music true ] ]
      Update = update
      View = menuView
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private pointer phase x y : ViewerPointerInput =
    { Phase = phase
      X = x
      Y = y
      Button = Some ViewerPointerButtonKind.Primary
      DeltaX = 0.0
      DeltaY = 0.0 }

/// Centre of a control's computed bounds at `size` — the point a user clicks.
let private centreOf (model: Model) (nodeId: ControlId) =
    let rendered = Control.renderTree menuHost.Theme size (menuHost.View size model)

    let available: FS.GG.UI.Layout.AvailableSpace =
        { Width = float size.Width
          WidthMode = FS.GG.UI.Layout.Exactly
          Height = float size.Height
          HeightMode = FS.GG.UI.Layout.Exactly }

    let result = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
    let b = result.Bounds |> List.find (fun b -> b.NodeId = nodeId)
    b.Bounds.X + b.Bounds.Width / 2.0, b.Bounds.Y + b.Bounds.Height / 2.0

/// Press+release at (x, y) through the real adapter path; return the routed msgs.
let private clickAt (model: Model) (x: float) (y: float) =
    let state1, down = ControlsElmish.routeInteractivePointer menuHost (Pointer.init ()) size model (pointer ViewerPointerPhaseKind.Pressed x y)
    let _state2, up = ControlsElmish.routeInteractivePointer menuHost state1 size model (pointer ViewerPointerPhaseKind.Released x y)
    down @ up

/// Fold routed messages through `update`, exactly as the host loop does, and collect the effects.
let private effectsOf (model: Model) (msgs: Msg list) =
    msgs
    |> List.fold
        (fun (current, acc) msg ->
            let next, produced = menuHost.Update msg current
            next, acc @ produced)
        (model, [])
    |> snd

[<Tests>]
let tests =
    testList
        "issue-429 controls audio seam"
        [
          // The scenario the issue names: "every game with a menu". A real click on a real Button,
          // routed through the real adapter, asks for a sound.
          test "clicking an authored Button requests the sound its update emits" {
            let model = { Started = false; Volume = 0.5 }
            let x, y = centreOf model (ControlId "start")

            let msgs = clickAt model x y
            Expect.equal msgs [ StartPressed ] "the click resolved the authored onClick"

            let audio = effectsOf model msgs |> GeneratedAppHost.audioRequests

            Expect.equal audio [ Audio.playSfx click 1.0 ] "the pointer-driven update requested exactly one click sfx"
          }

          // The sink is the host's; prove the batch a click produces actually lands in it.
          test "the audio sink a host installs receives what a click requested" {
            let played = ResizeArray<AudioEffect>()
            let sink: AudioEffect list -> unit = played.AddRange

            let model = { Started = false; Volume = 0.5 }
            let x, y = centreOf model (ControlId "start")

            // The loop hands each frame's batch to the sink, in dispatch order — init, then the click.
            menuHost.Init() |> snd |> GeneratedAppHost.audioRequests |> sink
            clickAt model x y |> effectsOf model |> GeneratedAppHost.audioRequests |> sink

            Expect.equal
                (List.ofSeq played)
                [ Audio.playMusic music true; Audio.playSfx click 1.0 ]
                "the sink received the menu music, then the click"
          }

          // A settings screen's volume slider. This asserts the request REACHES the sink, which is all
          // this repo owns; whether a backend then mixes it is FS.GG.Audio's business (`SetBusVolume` is
          // a documented no-op on the raw-backend path — filed separately, see #429).
          test "a volume change is a pure audio request like any other" {
            let model = { Started = false; Volume = 0.5 }
            let audio = effectsOf model [ VolumeChanged 0.25 ] |> GeneratedAppHost.audioRequests

            Expect.equal audio [ Audio.setBusVolume Bus.Music 0.25 ] "a settings slider requests a bus-volume change"
          }

          test "runInteractiveAppWithAudio never reaches the sink on a host that cannot open a window" {
            let played = ResizeArray<AudioEffect>()

            if Viewer.runtimeCapability().PersistentWindow then
                skiptestf "host can open a persistent window; the unsupported-host path is not exercised here"
            else
                let options =
                    { Title = "Menu"
                      InitialSize = { Width = 640; Height = 480 }
                      PresentMode = ViewerPresentMode.OffscreenReadback
                      FrameRateCap = None
                      LogicalSize = None }

                match ControlsElmish.runInteractiveAppWithAudio options played.AddRange menuHost with
                | Result.Ok _ -> failtest "an unsupported host cannot report a successful launch"
                | Result.Error failure ->
                    Expect.equal
                        failure.Classification
                        UnsupportedEnvironment
                        "runInteractiveAppWithAudio classifies an unsupported host exactly as runInteractiveApp does"

                Expect.isEmpty played "no sound is played when no window ever opened"
          } ]
