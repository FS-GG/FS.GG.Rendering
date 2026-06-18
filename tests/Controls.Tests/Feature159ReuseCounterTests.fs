module Feature159ReuseCounterTests

open Expecto
open FS.GG.UI.Controls

// SYNTHETIC: These fixtures use pure counter and identity records to prove fail-closed accounting.
[<Tests>]
let tests =
    testList "Feature159 reuse counters" [
        test "Synthetic counter helper preserves placement reuse and net saved work" {
            let work =
                { WorkReductionRecord.BaselineNodeCount = 10
                  RecomputedNodeCount = 2
                  ChangedSubtreeBound = 1
                  ShiftedNodeCount = 0
                  RemeasuredNodeCount = 1
                  MemoHits = 0
                  MemoMisses = 0
                  VirtualMaterialized = 0
                  VirtualTotal = 0
                  RepaintedNodeCount = 1
                  DirtyRectCount = 1
                  DirtyArea = 64
                  PictureCacheHits = 2
                  PictureCacheMisses = 1
                  PictureCacheEntryCount = 3
                  TextMeasureCacheHits = 0
                  TextMeasureCacheMisses = 0
                  LayoutInvalidatedNodeCount = 1
                  ReplayHits = 2
                  ReplayMisses = 1
                  ReplayRecords = 1
                  ReplaySkippedNodes = 8
                  ReplayCacheNativeBytes = 512
                  AvoidedContentWork = 8
                  PlacementOnlyReuseCount = 2
                  ContentRecordCount = 1
                  ContentRerecordCount = 1
                  PromotionCount = 1
                  DemotionCount = 0
                  FallbackCount = 0
                  PromotionOverhead = 3
                  NetSavedWork = 5 }

            let counters = RetainedRender.feature159CountersFromWork work
            Expect.equal counters.PlacementOnlyReuseCount 2 "placement reuse"
            Expect.equal counters.ContentRerecordCount 1 "content re-record"
            Expect.equal counters.NetSavedWork 5 "net saved work"
        }

        test "Synthetic cross-profile reuse is rejected with zero accepted counters" {
            let content = RetainedRender.feature159ContentIdentity "row-1" "run-1" 42UL None
            let placement = RetainedRender.feature159PlacementIdentity "row-1" None 0.0 0.0 1.0 []
            let decision = RetainedRender.feature159ClassifyReuse (Some content) content (Some placement) placement true false true false

            Expect.equal (RetainedRender.feature159ReuseStatusToken decision.Status) "reuse-rejected" "rejected"
            Expect.equal (decision.PrimaryReason |> Option.map RetainedRender.feature159ReasonToken) (Some "cross-profile-evidence") "cross-profile"
            Expect.equal decision.CounterDelta.PlacementOnlyReuseCount 0 "no accepted reuse"
        }
    ]
