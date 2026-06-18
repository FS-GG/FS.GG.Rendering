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
            let decision = RetainedRender.promotionDecision "reuse/stable-boundary" 5 3 150 20 true
            Expect.equal decision.Decision Promote "promote"
            Expect.equal decision.Tier ReplayTier "replay tier"
            Expect.stringContains decision.Reason "stable" "reason"
        }

        test "content-change, churn, no-benefit, and parity failure reject or demote" {
            Expect.equal (RetainedRender.promotionDecision "reuse/failed-parity" 5 3 150 20 false).Decision Reject "parity"
            Expect.equal (RetainedRender.promotionDecision "reuse/churning" 1 3 150 20 true).Decision Observe "churn observation"
            Expect.equal (RetainedRender.promotionDecision "reuse/no-benefit" 5 3 0 0 true).Decision Reject "no benefit"
            Expect.equal (RetainedRender.promotionDecision "reuse/content-changing" 5 3 20 25 true).Decision Demote "overhead demotion"
        }

        test "placement-only movement union is deterministic for same seed" {
            let a = RetainedRender.placementDamage 120 100 (rect 5.0 5.0 25.0 20.0) (rect 35.0 5.0 25.0 20.0)
            let b = RetainedRender.placementDamage 120 100 (rect 5.0 5.0 25.0 20.0) (rect 35.0 5.0 25.0 20.0)
            Expect.equal a b "same-seed deterministic movement damage"
        }
    ]
