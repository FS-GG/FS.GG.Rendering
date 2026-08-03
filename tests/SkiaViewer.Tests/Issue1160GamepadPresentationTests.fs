module FS.GG.UI.SkiaViewer.Tests.Issue1160GamepadPresentationTests

open System
open Elmish
open Expecto
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

type private Msg =
    | Snapshot of float * float * float * float * float * float
    | Tick
    | SuccessfulPresentation

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

        test "production completion route pairs startup and terminal presentations with one poll" {
            let mutable polls = 0
            let mutable shutdownRequested = false
            let received = ResizeArray<Msg list>()
            let source =
                { Poll =
                    fun () ->
                        polls <- polls + 1
                        Some
                            { LeftStickX = float polls
                              LeftStickY = 0.0
                              RightStickX = 0.0
                              RightStickY = 0.0
                              LeftTrigger = 0.0
                              RightTrigger = 0.0 }
                  Map = fun snapshot -> [ Snapshot(snapshot.LeftStickX, snapshot.LeftStickY, snapshot.RightStickX, snapshot.RightStickY, snapshot.LeftTrigger, snapshot.RightTrigger) ] }

            let configuration = Viewer.defaultConfiguration "gamepad production route" { Width = 64; Height = 64 }
            let program =
                Viewer.create configuration (fun () -> (), Cmd.none) (fun _ model -> model, Cmd.none) (fun () -> FS.GG.UI.Scene.Scene.empty)
                |> Viewer.withEventMapping (function
                    | FramePresented -> Some SuccessfulPresentation
                    | _ -> None)

            let dispatch = function
                | SuccessfulPresentation ->
                    ViewerRuntime.frameMessages (Some source) (fun _ -> Some Tick) TimeSpan.Zero
                    |> received.Add
                    if received.Count = 3 then shutdownRequested <- true
                | _ -> ()

            // The startup RenderFrame succeeds before any RenderTick. It must still own one poll.
            GlHost.completePresentation program dispatch (Ok "startup") |> ignore

            // A failed attempt is not a presentation boundary and therefore cannot poll.
            let failed = Diagnostics.frameRenderFailed "synthetic failed present"
            GlHost.completePresentation program dispatch (Result.Error failed) |> ignore

            // A paced frame and the last successful frame before cancellation each own one poll.
            GlHost.completePresentation program dispatch (Ok "paced") |> ignore
            GlHost.completePresentation program dispatch (Ok "terminal") |> ignore

            // The same guard used by the live loop prevents a canceled terminal iteration from even
            // entering DoRender, so it cannot manufacture a fourth completion callback or poll.
            if GlHost.shouldAttemptPresentation shutdownRequested false then
                GlHost.completePresentation program dispatch (Ok "after-cancel") |> ignore

            Expect.equal polls 3 "exactly the three successful presentations poll"
            Expect.equal received.Count 3 "startup, paced, and terminal successful frames are paired"
            received
            |> Seq.iteri (fun index messages ->
                match messages with
                | [ Snapshot(lx, _, _, _, _, _); Tick ] ->
                    Expect.floatClose Accuracy.high lx (float (index + 1)) "snapshot precedes the ordinary tick on every successful presentation"
                | unexpected -> failtestf "presentation %d had unexpected ordering: %A" index unexpected)
        }
    ]
