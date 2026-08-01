module FS.GG.UI.SkiaViewer.Tests.Issue1160GamepadPresentationTests

open System
open Expecto
open FS.GG.UI.SkiaViewer

type private Msg =
    | Snapshot of float * float * float * float * float * float
    | Tick

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
            Expect.equal
                (frames |> List.last)
                [ Snapshot(119.0 / 120.0, -119.0 / 120.0, 1.0 / 120.0, 119.0 / 240.0, 119.0 / 360.0, 119.0 / 180.0); Tick ]
                "both sticks and both triggers remain independent through the final frame"
        }
    ]
