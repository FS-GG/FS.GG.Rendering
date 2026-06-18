module Feature149ReuseDecisionTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature149 reuse decisions" [
        test "stable beneficial boundaries promote while incomplete windows observe" {
            let observing = RetainedRender.promotionDecision "boundary" 1 3 120 10 true
            Expect.equal observing.Decision Observe "stability window incomplete"

            let promoted = RetainedRender.promotionDecision "boundary" 3 3 120 10 true
            Expect.equal promoted.Decision Promote "stable beneficial boundary promotes"
            Expect.equal promoted.Tier ReplayTier "promotion targets replay tier"
        }

        test "no-benefit and parity-failed boundaries demote or reject" {
            let noBenefit = RetainedRender.promotionDecision "simple" 3 3 10 20 true
            Expect.equal noBenefit.Decision Demote "overhead demotes"

            let failedParity = RetainedRender.promotionDecision "bad" 3 3 120 10 false
            Expect.equal failedParity.Decision Reject "parity failure rejects"
            Expect.equal failedParity.Tier NoCompositorTier "rejected tier"
        }
    ]
