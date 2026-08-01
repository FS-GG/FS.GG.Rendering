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

let private host: InteractiveAppHost<int, Msg> =
    { Init = fun () -> 0, []
      Update = fun _ model -> model, []
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
                    host raw (Pointer.init ()) size 0 (pointer ViewerPointerPhaseKind.Pressed 10.0 10.0)

            let _, released =
                ControlsElmish.routeInteractivePointerWithRawFallback
                    host raw state size 0 (pointer ViewerPointerPhaseKind.Released 10.0 10.0)

            Expect.equal pressed [ Aim(10.0, 10.0) ] "the press reaches the raw fallback"
            Expect.equal released [ BoundClick; Aim(10.0, 10.0) ] "the authored click is preserved before raw aim"
        }

        test "public paced launcher folds 1000 moves and retains its receipt" {
            if not (Viewer.runtimeCapability().PersistentWindow) then
                skiptest "the production viewer script requires a persistent-window host"
            else
                let aims = ResizeArray<Msg>()
                let receipts = ResizeArray<ViewerPointerPacingMetrics>()
                let pacing = { Viewer.defaultPointerPacingOptions with ContinuousPolicy = ViewerContinuousPointerPolicy.CoalesceLatestPerFrame; OnMetrics = receipts.Add }
                let script =
                    [ for frame in 0 .. 59 do
                          for i in 1 .. (if frame < 40 then 17 else 16) do
                              yield ViewerScriptInput.Pointer(pointer ViewerPointerPhaseKind.Moved (float i) (float frame))
                          yield ViewerScriptInput.WaitFrame ]

                match ControlsElmish.Live.runPointerPacingScript { Title = "paced"; InitialSize = size; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None; LogicalSize = None } pacing (fun input _ _ -> [ Aim(input.X, input.Y) ]) host script with
                | Result.Error failure -> failtestf "paced public launcher failed: %A" failure
                | Result.Ok _ ->
                    let folded = receipts |> Seq.sumBy _.FoldedSamplesApplied
                    Expect.isLessThanOrEqual folded 60 "at most one raw aim sample survives each presentation boundary"
                    Expect.equal (receipts |> Seq.sumBy _.RawSamplesReceived) 1000 "the lower receipt observes every raw move"
        }
    ]
