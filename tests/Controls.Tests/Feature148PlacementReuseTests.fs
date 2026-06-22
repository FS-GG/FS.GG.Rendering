module Feature148PlacementReuseTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature148 placement reuse policy" [
        test "stable beneficial parity-clean boundary promotes to replay tier" {
            let decision = CompositorPolicy.promotionDecision { BoundaryId = "reuse/stable-boundary"; ObservedStabilityFrames = 5; ObservationWindow = 3; ExpectedSavedWork = 150; MeasuredOverhead = 20; ParityPassed = true }
            Expect.equal decision.Decision Promote "promote"
            Expect.equal decision.Tier ReplayTier "replay tier"
            Expect.stringContains decision.Reason "stable" "reason"
        }

        test "content-change, churn, no-benefit, and parity failure reject or demote" {
            Expect.equal (CompositorPolicy.promotionDecision { BoundaryId = "reuse/failed-parity"; ObservedStabilityFrames = 5; ObservationWindow = 3; ExpectedSavedWork = 150; MeasuredOverhead = 20; ParityPassed = false }).Decision Reject "parity"
            Expect.equal (CompositorPolicy.promotionDecision { BoundaryId = "reuse/churning"; ObservedStabilityFrames = 1; ObservationWindow = 3; ExpectedSavedWork = 150; MeasuredOverhead = 20; ParityPassed = true }).Decision Observe "churn observation"
            Expect.equal (CompositorPolicy.promotionDecision { BoundaryId = "reuse/no-benefit"; ObservedStabilityFrames = 5; ObservationWindow = 3; ExpectedSavedWork = 0; MeasuredOverhead = 0; ParityPassed = true }).Decision Reject "no benefit"
            Expect.equal (CompositorPolicy.promotionDecision { BoundaryId = "reuse/content-changing"; ObservedStabilityFrames = 5; ObservationWindow = 3; ExpectedSavedWork = 20; MeasuredOverhead = 25; ParityPassed = true }).Decision Demote "overhead demotion"
        }

        test "placement-only movement union is deterministic for same seed" {
            let a = CompositorPolicy.placementDamage 120 100 (rect 5.0 5.0 25.0 20.0) (rect 35.0 5.0 25.0 20.0)
            let b = CompositorPolicy.placementDamage 120 100 (rect 5.0 5.0 25.0 20.0) (rect 35.0 5.0 25.0 20.0)
            Expect.equal a b "same-seed deterministic movement damage"
        }
    ]
