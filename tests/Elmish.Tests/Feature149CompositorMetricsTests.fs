module Feature149CompositorMetricsTests

open System
open Expecto
open FS.GG.UI.Controls.Elmish

let private metrics replaySkipped replayMisses =
    { ProductModelChanged = true
      ViewCalled = true
      FullRenderCount = 1
      RemeasuredNodeCount = 2
      MemoHitCount = 0
      MemoMissCount = 0
      VirtualItemsMaterialized = 0
      VirtualItemsTotal = 0
      RepaintedNodeCount = 3
      DirtyRectCount = 2
      DirtyArea = 1600
      PictureCacheHitCount = 4
      PictureCacheMissCount = 1
      PictureCacheEntryCount = 5
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
      ReplayHitCount = 3
      ReplayMissCount = replayMisses
      ReplayRecordCount = replayMisses
      ReplaySkippedNodeCount = replaySkipped
      ReplayCacheNativeBytes = 32 * 1024 }

[<Tests>]
let tests =
    testList "Feature149 compositor metrics" [
        test "diagnostics expose proof, damage, reuse, snapshot bytes, and demotion state" {
            let diagnostics = ControlsElmish.compositorDiagnostics true None (metrics 12 1)

            Expect.equal diagnostics.ProofStatus "passed" "proof"
            Expect.equal diagnostics.DamageUnionArea 1600 "damage union"
            Expect.equal diagnostics.ReuseHitCount 7 "picture + replay hits"
            Expect.equal diagnostics.ReuseMissCount 2 "picture + replay misses"
            Expect.equal diagnostics.SnapshotResourceBytes (32 * 1024) "snapshot bytes"
            Expect.equal diagnostics.DemotionCount 0 "skipped replay work avoids demotion"
        }

        test "fallback and no-benefit demotion are visible when proof or replay evidence is weak" {
            let diagnostics = ControlsElmish.compositorDiagnostics false (Some "environment-limited proof") (metrics 0 2)

            Expect.equal diagnostics.ProofStatus "not-ready" "proof"
            Expect.equal diagnostics.ScissorCandidateArea 0 "no scissor without proof"
            Expect.equal diagnostics.FallbackReason (Some "environment-limited proof") "fallback"
            Expect.equal diagnostics.DemotionCount 1 "no replay skipped work with misses demotes"
        }
    ]
