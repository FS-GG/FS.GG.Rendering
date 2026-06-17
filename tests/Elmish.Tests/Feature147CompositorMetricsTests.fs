module Feature147CompositorMetricsTests

open System
open Expecto
open FS.GG.UI.Controls.Elmish

let private metrics =
    { ProductModelChanged = true
      ViewCalled = true
      FullRenderCount = 1
      RemeasuredNodeCount = 2
      MemoHitCount = 0
      MemoMissCount = 0
      VirtualItemsMaterialized = 0
      VirtualItemsTotal = 0
      RepaintedNodeCount = 2
      DirtyRectCount = 1
      DirtyArea = 1200
      PictureCacheHitCount = 3
      PictureCacheMissCount = 1
      PictureCacheEntryCount = 4
      TextMeasureCacheHitCount = 0
      TextMeasureCacheMissCount = 0
      LayoutInvalidatedNodeCount = 1
      PointerSamplesReceived = 0
      PointerMovesProcessed = 0
      FullRenderFallbackCount = 0
      FrameCause = FrameCause.Key
      DiffRan = true
      LayoutRan = true
      PaintRan = true
      FrameDuration = TimeSpan.Zero
      PaintDuration = TimeSpan.Zero
      ComposeDuration = TimeSpan.Zero
      ReplayHitCount = 2
      ReplayMissCount = 1
      ReplayRecordCount = 1
      ReplaySkippedNodeCount = 8
      ReplayCacheNativeBytes = 4096 }

[<Tests>]
let tests =
    testList "Feature147 derived compositor metrics" [
        test "passed proof exposes scissor candidate area and reuse counters" {
            let diagnostics = ControlsElmish.compositorDiagnostics true None metrics

            Expect.equal diagnostics.ProofStatus "passed" "proof token"
            Expect.equal diagnostics.DamageUnionArea metrics.DirtyArea "damage union mirrors retained dirty area"
            Expect.equal diagnostics.ScissorCandidateArea metrics.DirtyArea "eligible scissor area"
            Expect.equal diagnostics.ReuseHitCount 5 "picture + replay hits"
            Expect.equal diagnostics.ReuseMissCount 2 "picture + replay misses"
            Expect.equal diagnostics.SnapshotResourceBytes 4096 "resource bytes"
        }

        test "not-ready proof reports fallback and zero scissor area" {
            let diagnostics = ControlsElmish.compositorDiagnostics false None metrics

            Expect.equal diagnostics.ProofStatus "not-ready" "proof token"
            Expect.equal diagnostics.ScissorCandidateArea 0 "cannot scissor"
            Expect.equal diagnostics.FallbackReason (Some "present proof is not ready") "fallback reason"
        }
    ]
