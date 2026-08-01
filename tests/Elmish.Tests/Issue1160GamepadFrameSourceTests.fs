module FS.GG.UI.Elmish.Tests.Issue1160GamepadFrameSourceTests

open Expecto
open FS.GG.UI.SkiaViewer

// Regression for Rendering#1160: the gamepad source is the host-owned native boundary;
// product code receives only deterministic snapshots/messages. This drives 120 frame polls so
// both sticks and triggers must survive independently without a product-local host wrapper.
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
            Expect.equal (observed.Tail |> List.last) [ 119.0 / 120.0, 1.0 / 120.0, 119.0 / 180.0 ] "later snapshots do not alias the independent controls"
        }
    ]
