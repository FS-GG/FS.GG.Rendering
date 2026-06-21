module Feature147PromotionReuseTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature147 promotion and placement reuse policy" [
        test "stable beneficial parity-clean boundary promotes" {
            let decision = RetainedRender.promotionDecision { BoundaryId = "panel"; ObservedStabilityFrames = 4; ObservationWindow = 3; ExpectedSavedWork = 120; MeasuredOverhead = 12; ParityPassed = true }
            Expect.equal decision.Decision Promote "promoted"
            Expect.equal decision.Tier ReplayTier "replay tier"
        }

        test "incomplete stability observes, parity failure rejects, overhead demotes" {
            Expect.equal (RetainedRender.promotionDecision { BoundaryId = "a"; ObservedStabilityFrames = 1; ObservationWindow = 3; ExpectedSavedWork = 100; MeasuredOverhead = 10; ParityPassed = true }).Decision Observe "wait for stability"
            Expect.equal (RetainedRender.promotionDecision { BoundaryId = "b"; ObservedStabilityFrames = 3; ObservationWindow = 3; ExpectedSavedWork = 100; MeasuredOverhead = 10; ParityPassed = false }).Decision Reject "parity failure rejects"
            Expect.equal (RetainedRender.promotionDecision { BoundaryId = "c"; ObservedStabilityFrames = 3; ObservationWindow = 3; ExpectedSavedWork = 20; MeasuredOverhead = 25; ParityPassed = true }).Decision Demote "overhead demotes"
        }

        test "placement-only movement damages old and new regions" {
            let damage = RetainedRender.placementDamage 100 100 (rect 0.0 0.0 20.0 20.0) (rect 10.0 0.0 20.0 20.0)
            Expect.equal damage.Regions.Length 2 "old and new placements are damaged"
            Expect.equal damage.UnionArea 600 "overlap counted once"
        }
    ]
