module Feature151LayoutRegressionMetricsTests

open System
open Expecto
open FS.GG.UI.Controls.Elmish

let private frameMetrics layoutRan invalidated hits misses =
    { ProductModelChanged = true
      ViewCalled = true
      FullRenderCount = 1
      RemeasuredNodeCount = invalidated + hits + misses
      MemoHitCount = 0
      MemoMissCount = 0
      VirtualItemsMaterialized = 0
      VirtualItemsTotal = 0
      RepaintedNodeCount = 0
      DirtyRectCount = 0
      DirtyArea = 0
      PictureCacheHitCount = 0
      PictureCacheMissCount = 0
      PictureCacheEntryCount = 0
      TextMeasureCacheHitCount = 0
      TextMeasureCacheMissCount = 0
      LayoutInvalidatedNodeCount = invalidated
      PointerSamplesReceived = 0
      PointerMovesProcessed = 0
      FullRenderFallbackCount = 0
      FrameCause = FrameCause.Key
      DiffRan = true
      LayoutRan = layoutRan
      PaintRan = true
      FrameDuration = TimeSpan.Zero
      PaintDuration = TimeSpan.Zero
      ComposeDuration = TimeSpan.Zero
      ReplayHitCount = hits
      ReplayMissCount = misses
      ReplayRecordCount = hits + misses
      ReplaySkippedNodeCount = 0
      ReplayCacheNativeBytes = 0 }

[<Tests>]
let tests =
    testList "Feature151LayoutRegressionMetrics" [
        test "layout work metrics remain deterministic for accepted P8 frames" {
            let metrics = ControlsElmish.layoutMetrics (frameMetrics true 3 2 1)

            Expect.equal metrics.LayoutWorkCount 6 "layout work count"
            Expect.equal metrics.IntrinsicInvalidationCount 3 "invalidation count"
            Expect.equal metrics.IntrinsicCacheMissCount 3 "layout-running invalidations are reported as misses"
            Expect.equal metrics.IntrinsicCacheHitCount 0 "hits are not fabricated from replay cache counters"
        }

        test "non-layout frames do not fabricate intrinsic work" {
            let metrics = ControlsElmish.layoutMetrics (frameMetrics false 5 10 10)

            Expect.equal metrics.IntrinsicInvalidationCount 5 "raw invalidation count remains observable"
            Expect.equal metrics.IntrinsicCacheMissCount 0 "no layout pass means no intrinsic miss claim"
            Expect.equal metrics.IntrinsicCacheHitCount 0 "no intrinsic hit claim"
        }
    ]
