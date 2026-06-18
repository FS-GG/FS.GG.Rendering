module Feature159IdentitySplitTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Scene

let private box x y =
    Some { X = x; Y = y; Width = 64.0; Height = 32.0 }

// SYNTHETIC: These tests exercise pure identity classification with minimal retained-boundary fixtures.
[<Tests>]
let tests =
    testList "Feature159 identity split" [
        test "Synthetic stable content with changed placement reuses content and records placement-only reuse" {
            let priorContent = RetainedRender.feature159ContentIdentity "row-1" "run-1" 42UL None
            let currentContent = RetainedRender.feature159ContentIdentity "row-1" "run-1" 42UL None
            let priorPlacement = RetainedRender.feature159PlacementIdentity "row-1" (box 0.0 0.0) 0.0 0.0 1.0 []
            let currentPlacement = RetainedRender.feature159PlacementIdentity "row-1" (box 16.0 0.0) 16.0 0.0 1.0 []

            let decision =
                RetainedRender.feature159ClassifyReuse
                    (Some priorContent)
                    currentContent
                    (Some priorPlacement)
                    currentPlacement
                    true
                    true
                    true
                    false

            Expect.equal (RetainedRender.feature159ReuseStatusToken decision.Status) "content-reused-placement-updated" "reuse decision"
            Expect.equal decision.CounterDelta.PlacementOnlyReuseCount 1 "placement-only reuse count"
            Expect.isGreaterThan decision.CounterDelta.NetSavedWork 0 "net saved work"
        }

        test "Synthetic content change re-records content rather than reusing stale placement" {
            let priorContent = RetainedRender.feature159ContentIdentity "row-1" "run-1" 42UL None
            let currentContent = RetainedRender.feature159ContentIdentity "row-1" "run-1" 99UL None
            let placement = RetainedRender.feature159PlacementIdentity "row-1" (box 0.0 0.0) 0.0 0.0 1.0 []

            let decision =
                RetainedRender.feature159ClassifyReuse
                    (Some priorContent)
                    currentContent
                    (Some placement)
                    placement
                    true
                    true
                    true
                    false

            Expect.equal (RetainedRender.feature159ReuseStatusToken decision.Status) "content-re-recorded" "content changed"
            Expect.equal decision.CounterDelta.ContentRerecordCount 1 "re-record count"
            Expect.equal decision.CounterDelta.PlacementOnlyReuseCount 0 "no stale placement-only reuse"
        }
    ]
