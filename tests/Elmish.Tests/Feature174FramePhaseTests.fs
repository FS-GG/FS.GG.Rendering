module Feature174FramePhaseTests

open System
open Expecto
open FS.GG.UI.Controls.Elmish

let private metric (frameMs: float) (paintMs: float) (composeMs: float) productChanged layoutRan =
    { ProductModelChanged = productChanged
      ViewCalled = productChanged
      FullRenderCount = if productChanged then 1 else 0
      RemeasuredNodeCount = if layoutRan then 3 else 0
      MemoHitCount = 0
      MemoMissCount = 0
      VirtualItemsMaterialized = 0
      VirtualItemsTotal = 0
      RepaintedNodeCount = 1
      DirtyRectCount = 1
      DirtyArea = 64
      PictureCacheHitCount = 0
      PictureCacheMissCount = 0
      PictureCacheEntryCount = 0
      TextMeasureCacheHitCount = 0
      TextMeasureCacheMissCount = 0
      LayoutInvalidatedNodeCount = if layoutRan then 1 else 0
      PointerSamplesReceived = 1
      PointerMovesProcessed = 1
      FullRenderFallbackCount = 0
      FrameCause = FrameCause.PointerDiscrete
      DiffRan = productChanged
      LayoutRan = layoutRan
      PaintRan = true
      FrameDuration = TimeSpan.FromMilliseconds frameMs
      PaintDuration = TimeSpan.FromMilliseconds paintMs
      ComposeDuration = TimeSpan.FromMilliseconds composeMs
      ReplayHitCount = 0
      ReplayMissCount = 0
      ReplayRecordCount = 0
      ReplaySkippedNodeCount = 0
      ReplayCacheNativeBytes = 0 }

[<Tests>]
let tests =
    testList "Feature174 frame phase attribution" [
        test "Synthetic timing contribution separates frame preparation from paint and presentation" {
            // SYNTHETIC: literal frame timing isolates the projection math; live evidence is produced by the sample runner.
            let contribution = ControlsElmish.responsivenessTimingContribution (metric 100.0 15.0 5.0 true true)

            Expect.equal contribution.RetainedStepDuration (TimeSpan.FromMilliseconds 80.0) "retained step is pre-paint preparation"
            Expect.equal contribution.UpdateDuration (TimeSpan.FromMilliseconds 80.0) "model update contribution uses the same pre-paint budget"
            Expect.equal contribution.LayoutDuration (TimeSpan.FromMilliseconds 80.0) "layout contribution is phase-attributed when layout ran"
            Expect.equal contribution.ProductMessageCount 1 "product message count is preserved"
        }

        test "Synthetic timing contribution clamps impossible paint/presentation overrun to zero" {
            // SYNTHETIC: defensive literal covers live timing jitter where paint+compose can exceed frame stopwatch granularity.
            let contribution = ControlsElmish.responsivenessTimingContribution (metric 10.0 8.0 8.0 true true)

            Expect.equal contribution.RetainedStepDuration TimeSpan.Zero "negative frame preparation is clamped"
            Expect.equal contribution.LayoutDuration TimeSpan.Zero "layout attribution follows the clamped preparation value"
        }
    ]
