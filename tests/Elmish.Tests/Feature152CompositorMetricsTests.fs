module Feature152CompositorMetricsTests

open System
open Expecto
open FS.GG.UI.Controls.Elmish

let private metrics dirtyArea replaySkipped replayMisses =
    { ProductModelChanged = true
      ViewCalled = true
      FullRenderCount = 1
      RemeasuredNodeCount = 2
      MemoHitCount = 0
      MemoMissCount = 0
      VirtualItemsMaterialized = 0
      VirtualItemsTotal = 0
      RepaintedNodeCount = 1
      DirtyRectCount = 1
      DirtyArea = dirtyArea
      PictureCacheHitCount = 1
      PictureCacheMissCount = 0
      PictureCacheEntryCount = 1
      TextMeasureCacheHitCount = 0
      TextMeasureCacheMissCount = 0
      LayoutInvalidatedNodeCount = 0
      PointerSamplesReceived = 0
      PointerMovesProcessed = 0
      FullRenderFallbackCount = 0
      FrameCause = FrameCause.Tick
      DiffRan = true
      LayoutRan = true
      PaintRan = true
      FrameDuration = TimeSpan.Zero
      PaintDuration = TimeSpan.Zero
      ComposeDuration = TimeSpan.Zero
      ReplayHitCount = 0
      ReplayMissCount = replayMisses
      ReplayRecordCount = replayMisses
      ReplaySkippedNodeCount = replaySkipped
      ReplayCacheNativeBytes = 1024 }

[<Tests>]
let tests =
    testList "Feature152 compositor metrics" [
        test "proof, damage, fallback, and resource diagnostics remain reviewer-visible" {
            let diagnostics = ControlsElmish.compositorDiagnostics true None (metrics 96 4 0)

            Expect.equal diagnostics.ProofStatus "passed" "proof"
            Expect.equal diagnostics.DamageUnionArea 96 "damage"
            Expect.equal diagnostics.ScissorCandidateArea 96 "scissor candidate"
            Expect.equal diagnostics.FallbackReason None "no fallback"
            Expect.equal diagnostics.SnapshotResourceBytes 1024 "resource bytes"
        }

        test "environment-limited proof suppresses scissor candidate area" {
            let diagnostics = ControlsElmish.compositorDiagnostics false (Some "environment-limited proof") (metrics 96 0 1)

            Expect.equal diagnostics.ProofStatus "not-ready" "proof"
            Expect.equal diagnostics.ScissorCandidateArea 0 "no scissor without proof"
            Expect.equal diagnostics.FallbackReason (Some "environment-limited proof") "fallback"
            Expect.equal diagnostics.DemotionCount 1 "weak replay demotes"
        }
    ]
