module Feature150LayoutMetricsTests

open System
open Expecto
open FS.GG.UI.Controls.Elmish

let private metrics =
    { ProductModelChanged = true
      ViewCalled = true
      FullRenderCount = 1
      RemeasuredNodeCount = 7
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
      LayoutInvalidatedNodeCount = 3
      PointerSamplesReceived = 0
      PointerMovesProcessed = 0
      FullRenderFallbackCount = 0
      FrameCause = FrameCause.Key
      DiffRan = true
      LayoutRan = true
      PaintRan = false
      FrameDuration = TimeSpan.Zero
      PaintDuration = TimeSpan.Zero
      ComposeDuration = TimeSpan.Zero
      ReplayHitCount = 0
      ReplayMissCount = 0
      ReplayRecordCount = 0
      ReplaySkippedNodeCount = 0
      ReplayCacheNativeBytes = 0 }

[<Tests>]
let tests =
    testList "Feature150LayoutMetrics" [
        test "layoutMetrics projects deterministic layout and invalidation counts" {
            let projected = ControlsElmish.layoutMetrics metrics

            Expect.equal projected.LayoutWorkCount 7 "remeasured work"
            Expect.equal projected.IntrinsicInvalidationCount 3 "invalidated work"
            Expect.equal projected.IntrinsicCacheMissCount 3 "layout-running frame reports invalidation misses"
            Expect.equal projected.IntrinsicCacheHitCount 0 "no intrinsic cache hits are fabricated"
        }
    ]

