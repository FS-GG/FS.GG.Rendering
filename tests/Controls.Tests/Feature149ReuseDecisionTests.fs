module Feature149ReuseDecisionTests

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList "Feature149 reuse decisions" [
        test "stable beneficial boundaries promote while incomplete windows observe" {
            let observing = RetainedRender.promotionDecision { BoundaryId = "boundary"; ObservedStabilityFrames = 1; ObservationWindow = 3; ExpectedSavedWork = 120; MeasuredOverhead = 10; ParityPassed = true }
            Expect.equal observing.Decision Observe "stability window incomplete"

            let promoted = RetainedRender.promotionDecision { BoundaryId = "boundary"; ObservedStabilityFrames = 3; ObservationWindow = 3; ExpectedSavedWork = 120; MeasuredOverhead = 10; ParityPassed = true }
            Expect.equal promoted.Decision Promote "stable beneficial boundary promotes"
            Expect.equal promoted.Tier ReplayTier "promotion targets replay tier"
        }

        test "no-benefit and parity-failed boundaries demote or reject" {
            let noBenefit = RetainedRender.promotionDecision { BoundaryId = "simple"; ObservedStabilityFrames = 3; ObservationWindow = 3; ExpectedSavedWork = 10; MeasuredOverhead = 20; ParityPassed = true }
            Expect.equal noBenefit.Decision Demote "overhead demotes"

            let failedParity = RetainedRender.promotionDecision { BoundaryId = "bad"; ObservedStabilityFrames = 3; ObservationWindow = 3; ExpectedSavedWork = 120; MeasuredOverhead = 10; ParityPassed = false }
            Expect.equal failedParity.Decision Reject "parity failure rejects"
            Expect.equal failedParity.Tier NoCompositorTier "rejected tier"
        }
    ]
