module FS.GG.UI.SkiaViewer.Tests.Issue1160GamepadPresentationTests

open System
open Expecto
open FS.GG.UI.SkiaViewer

type private Msg =
    | Snapshot of float * float * float * float * float * float
    | Tick

[<Tests>]
let tests =
    testList "Issue 1160 gamepad presentation boundary" [
        test "120 frames poll once, preserve six fields, and dispatch snapshot before Tick" {
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

            let frames =
                [ for _ in 1 .. 120 ->
                    ViewerRuntime.frameMessages (Some source) (fun _ -> Some Tick) TimeSpan.Zero ]

            Expect.equal polls 120 "the live-launcher frame seam polls once per presentation frame"
            Expect.equal frames.Head [ Snapshot(0.0, -0.0, 1.0, 0.0, 0.0, 0.0); Tick ] "snapshot is delivered before Tick"
            match frames |> List.last with
            | [ Snapshot(lx, ly, rx, ry, lt, rt); Tick ] ->
                Expect.floatClose Accuracy.high lx (119.0 / 120.0) "the final left X remains independent"
                Expect.floatClose Accuracy.high ly (-119.0 / 120.0) "the final left Y remains independent"
                Expect.floatClose Accuracy.high rx (1.0 / 120.0) "the final right X remains independent"
                Expect.floatClose Accuracy.high ry (119.0 / 240.0) "the final right Y remains independent"
                Expect.floatClose Accuracy.high lt (119.0 / 360.0) "the final left trigger remains independent"
                Expect.floatClose Accuracy.high rt (119.0 / 180.0) "the final right trigger remains independent"
            | finalFrame -> failtestf "unexpected final frame ordering: %A" finalFrame
        }
    ]
