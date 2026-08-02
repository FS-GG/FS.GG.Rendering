module Issue1159RawPointerPacingTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Themes.Default

type private Msg =
    | BoundClick
    | Aim of float * float

let private size = { Width = 320; Height = 200 }

let private host: InteractiveAppHost<Msg list, Msg> =
    { Init = fun () -> [], []
      Update = fun msg model -> model @ [ msg ], []
      View = fun _ _ -> Button.create [ Button.text "Fire"; Button.onClick BoundClick ] |> Control.withKey "fire"
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private pointer phase x y =
    { Phase = phase
      X = x
      Y = y
      Button = Some ViewerPointerButtonKind.Primary
      DeltaX = 0.0
      DeltaY = 0.0 }

let private raw input _ _ = [ Aim(input.X, input.Y) ]

[<Tests>]
let tests =
    testList "issue-1159 raw pointer composition" [
        test "raw fallback follows Controls binding without bypassing it" {
            let state, pressed =
                ControlsElmish.routeInteractivePointerWithRawFallback
                    host raw (Pointer.init ()) size [] (pointer ViewerPointerPhaseKind.Pressed 10.0 10.0)

            let _, released =
                ControlsElmish.routeInteractivePointerWithRawFallback
                    host raw state size [] (pointer ViewerPointerPhaseKind.Released 10.0 10.0)

            Expect.equal pressed [ Aim(10.0, 10.0) ] "the press reaches the raw fallback"
            Expect.equal released [ BoundClick; Aim(10.0, 10.0) ] "the authored click is preserved before raw aim"
        }

        test "deterministic Controls paced composition applies 1000 moves, preserves click order, and retains counters" {
            let frames =
                [ for frame in 0 .. 59 ->
                      [ if frame = 0 then
                            yield pointer ViewerPointerPhaseKind.Pressed 10.0 10.0
                            yield pointer ViewerPointerPhaseKind.Released 10.0 10.0
                        for sample in 1 .. (if frame < 40 then 17 else 16) do
                            yield pointer ViewerPointerPhaseKind.Moved (float sample) (float frame) ] ]
            let receipt =
                ControlsElmish.Live.runDeterministicPointerPacingThroughControls
                    ViewerContinuousPointerPolicy.CoalesceLatestPerFrame
                    System.DateTimeOffset.UnixEpoch
                    size
                    raw
                    host
                    frames
            Expect.equal receipt.Drains.Length 60 "one receipt per presentation boundary"
            Expect.equal (receipt.Metrics |> List.sumBy _.RawSamplesReceived) 1002 "every raw move and discrete sample reaches the Viewer queue"
            Expect.equal (receipt.Metrics |> List.sumBy _.FoldedSamplesApplied) 62 "one aim plus the lossless press/release pair applies per frame"
            Expect.equal (receipt.Metrics |> List.sumBy _.CoalescedSamples) 940 "the receipt records dropped raw moves"
            Expect.equal (receipt.Metrics |> List.sumBy _.ModelUpdates) 63 "the click binding and raw mapper are folded through the host update path"
            Expect.equal receipt.Metrics.[59].PresentedFrames 60L "present counter reaches all 60 frame boundaries"
            Expect.equal receipt.Model.[0..4] [ Aim(10.0, 10.0); BoundClick; Aim(10.0, 10.0); Aim(17.0, 0.0); Aim(17.0, 1.0) ] "discrete press/release and its binding stay ordered ahead of coalesced aims"
        }
    ]
