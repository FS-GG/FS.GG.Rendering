module Feature167SchedulerFixtures

open System
open FS.GG.UI.SkiaViewer

let now = DateTimeOffset(2026, 6, 19, 8, 0, 0, TimeSpan.Zero)

let enqueue kind payload queue =
    Viewer.enqueueInput now kind payload queue

let phase (total: float) =
    { ReceiptDuration = Some(TimeSpan.FromMilliseconds 0.2)
      QueueDelay = Some(TimeSpan.FromMilliseconds 1.0)
      RoutingDuration = Some(TimeSpan.FromMilliseconds 2.0)
      UpdateDuration = Some(TimeSpan.FromMilliseconds 3.0)
      ViewDuration = Some(TimeSpan.FromMilliseconds 4.0)
      RetainedStepDuration = Some(TimeSpan.FromMilliseconds 5.0)
      LayoutDuration = Some(TimeSpan.FromMilliseconds 1.0)
      TextDuration = Some(TimeSpan.FromMilliseconds 0.5)
      PaintDuration = Some(TimeSpan.FromMilliseconds 6.0)
      PresentDuration = Some(TimeSpan.FromMilliseconds 7.0)
      TotalInputToVisibleDuration = Some(TimeSpan.FromMilliseconds total) }

let latency (seq: int) (kind: ViewerResponsivenessInputKind) (total: float) =
    { RecordId = sprintf "resp-test-%06i" seq
      RunId = "resp-test"
      InputSequenceId = int64 seq
      InputKind = kind
      InputName = Some "activation"
      Page = Some "buttons"
      ControlGroup = Some "button"
      ReceiptTimestamp = now
      QueueDepthAtReceipt = 0
      QueueDepthAtDrain = 1
      CoalescedMovementCount = 0
      ProductMessageCount = 1
      ProductStateChanged = true
      RuntimeStateChanged = true
      VisibleResponse = ViewerResponsivenessVisibleResponse.PresentedFrame
      PresentedFrameId = Some(int64 seq)
      EnvironmentStatus = ViewerResponsivenessEnvironmentStatus.Measured
      PhaseTiming = phase total
      DirtyRegion =
        Some
            { DirtyRectCount = Some 1
              DirtyArea = Some 64
              RepaintedNodeCount = Some 1
              Status = ViewerResponsivenessEnvironmentStatus.Measured }
      LongFrame = total >= 50.0
      Diagnostics = [] }
