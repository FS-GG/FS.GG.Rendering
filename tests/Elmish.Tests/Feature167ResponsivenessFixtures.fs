module Feature167ResponsivenessFixtures

open System
open FS.GG.UI.Scene
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type Msg =
    | Inc
    | Noop

let size: Size = { Width = 320; Height = 200 }

let view model =
    Stack.create
        [ Stack.children [ Button.create [ Button.text (string model); Button.onClick Inc ] |> Control.withKey "btn" ] ]

let host =
    { Init = fun () -> 0, []
      Update =
        fun msg model ->
            (match msg with
             | Inc -> model + 1
             | Noop -> model),
            []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey =
        fun key isDown ->
            if isDown && key = Enter then Some Inc
            else None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let run script =
    ControlsElmish.Perf.runScript host size script

let deterministicShape (metrics: FrameMetrics) =
    metrics.ProductModelChanged,
    metrics.ViewCalled,
    metrics.FullRenderCount,
    metrics.RemeasuredNodeCount,
    metrics.PointerSamplesReceived,
    metrics.PointerMovesProcessed,
    metrics.FrameCause,
    metrics.DiffRan,
    metrics.LayoutRan,
    metrics.PaintRan,
    metrics.PaintDuration,
    metrics.ComposeDuration

let latency (total: float) =
    { RecordId = "resp-elmish-000001"
      RunId = "resp-elmish"
      InputSequenceId = 1L
      InputKind = ViewerResponsivenessInputKind.KeyDown
      InputName = Some "Enter"
      Page = Some "fixture"
      ControlGroup = Some "button"
      ReceiptTimestamp = DateTimeOffset(2026, 6, 19, 8, 0, 0, TimeSpan.Zero)
      QueueDepthAtReceipt = 0
      QueueDepthAtDrain = 1
      CoalescedMovementCount = 0
      ProductMessageCount = 1
      ProductStateChanged = true
      RuntimeStateChanged = false
      VisibleResponse = ViewerResponsivenessVisibleResponse.PresentedFrame
      PresentedFrameId = Some 1L
      EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.Measured
      PhaseTiming =
        { ReceiptDuration = Some(TimeSpan.FromMilliseconds 0.2)
          QueueDelay = Some(TimeSpan.Zero)
          RoutingDuration = Some(TimeSpan.Zero)
          UpdateDuration = Some(TimeSpan.Zero)
          ViewDuration = Some(TimeSpan.Zero)
          RetainedStepDuration = Some(TimeSpan.FromMilliseconds total)
          LayoutDuration = Some(TimeSpan.Zero)
          TextDuration = Some(TimeSpan.Zero)
          PaintDuration = Some(TimeSpan.Zero)
          PresentDuration = Some(TimeSpan.Zero)
          TotalInputToVisibleDuration = Some(TimeSpan.FromMilliseconds total) }
      DirtyRegion = None
      LongFrame = false
      Diagnostics = [] }
