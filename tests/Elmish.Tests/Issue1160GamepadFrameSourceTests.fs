module FS.GG.UI.Elmish.Tests.Issue1160GamepadFrameSourceTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg =
    | Snapshot of float * float * float * float * float * float
    | Tick

let private options: ViewerOptions =
    { Title = "Gamepad test"
      InitialSize = { Width = 320; Height = 200 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None
      LogicalSize = None }

// Regression for Rendering#1160: the gamepad source is the host-owned native boundary;
// product code receives only deterministic snapshots/messages. This drives 120 frame polls so
// both sticks and triggers must survive independently without a product-local host wrapper.
[<Tests>]
let tests =
    testList "Issue 1160 gamepad frame source" [
        test "polls once per scripted frame and preserves independent twin-stick values" {
            let mutable polls = 0
            let source =
                { Poll =
                    fun () ->
                        let i = polls
                        polls <- polls + 1
                        Some
                            { LeftStickX = float i / 120.0
                              LeftStickY = -float i / 120.0
                              RightStickX = 1.0 - float i / 120.0
                              RightStickY = float i / 240.0
                              LeftTrigger = float i / 360.0
                              RightTrigger = float i / 180.0 }
                  Map = fun snapshot -> [ snapshot.LeftStickX, snapshot.RightStickX, snapshot.RightTrigger ] }

            let observed = [ for _ in 1 .. 120 -> GamepadFrameSource.poll source ]

            Expect.equal polls 120 "one poll is made for each scripted presentation frame"
            Expect.equal observed.Head [ 0.0, 1.0, 0.0 ] "the first snapshot preserves both stick axes and trigger"
            let finalLeft, finalRight, finalTrigger = observed |> List.last |> List.exactlyOne
            Expect.floatClose Accuracy.high finalLeft (119.0 / 120.0) "the later left stick remains independent"
            Expect.floatClose Accuracy.high finalRight (1.0 / 120.0) "the later right stick remains independent"
            Expect.floatClose Accuracy.high finalTrigger (119.0 / 180.0) "the later trigger remains independent"
        }

        test "the Controls gamepad launcher folds 120 complete snapshots before their ticks" {
            let mutable polls = 0
            let source =
                { Poll =
                    fun () ->
                        let i = float polls
                        polls <- polls + 1
                        Some
                            { LeftStickX = i / 120.0
                              LeftStickY = -i / 120.0
                              RightStickX = 1.0 - i / 120.0
                              RightStickY = i / 240.0
                              LeftTrigger = i / 360.0
                              RightTrigger = i / 180.0 }
                  Map = fun s -> [ Snapshot(s.LeftStickX, s.LeftStickY, s.RightStickX, s.RightStickY, s.LeftTrigger, s.RightTrigger) ] }

            let host: InteractiveAppHost<Msg list, Msg> =
                { Init = fun () -> [], []
                  Update = fun msg model -> model @ [ msg ], []
                  View = fun _ _ -> Button.create [ Button.text "Gamepad" ]
                  Theme = Theme.light
                  MapKey = fun _ _ -> None
                  MapPointer = fun _ -> None
                  Tick = fun _ -> Some Tick
                  MapKeyChord = fun _ _ -> None
                  OnFrameMetrics = ignore
                  Diagnostics = Viewer.defaultDiagnostics }

            let observed =
                ControlsElmish.runInteractiveAppWithGamepadLauncher
                    (fun _ adapted ->
                        let initial, _ = adapted.Host.Init()

                        [ 1 .. 120 ]
                        |> List.fold (fun model _ ->
                            let messages =
                                GamepadFrameSource.poll adapted.Gamepad
                                @ (adapted.Host.Tick System.TimeSpan.Zero |> Option.toList)

                            messages
                            |> List.fold (fun state msg -> adapted.Host.Update msg state |> fst) model) initial)
                    options
                    { Host = host; Gamepad = source }

            Expect.equal polls 120 "the Controls launcher exposes exactly one native poll per presentation frame"
            Expect.equal observed.Length 240 "each of 120 frames preserves one snapshot and one ordinary tick"
            Expect.equal observed.[0] (Snapshot(0.0, -0.0, 1.0, 0.0, 0.0, 0.0)) "the first complete snapshot reaches Controls"
            Expect.equal observed.[1] Tick "the frame's tick follows its snapshot"
            match observed.[238] with
            | Snapshot(lx, ly, rx, ry, lt, rt) ->
                Expect.floatClose Accuracy.high lx (119.0 / 120.0) "the final left X reaches Controls"
                Expect.floatClose Accuracy.high ly (-119.0 / 120.0) "the final left Y reaches Controls"
                Expect.floatClose Accuracy.high rx (1.0 / 120.0) "the final right X reaches Controls"
                Expect.floatClose Accuracy.high ry (119.0 / 240.0) "the final right Y reaches Controls"
                Expect.floatClose Accuracy.high lt (119.0 / 360.0) "the final left trigger reaches Controls"
                Expect.floatClose Accuracy.high rt (119.0 / 180.0) "the final right trigger reaches Controls"
            | Tick -> failtest "the final snapshot slot contained a tick"
            Expect.equal observed.[239] Tick "the final frame retains its ordinary tick"
        }
    ]
