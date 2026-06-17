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
            let decision = RetainedRender.promotionDecision "panel" 4 3 120 12 true
            Expect.equal decision.Decision Promote "promoted"
            Expect.equal decision.Tier ReplayTier "replay tier"
        }

        test "incomplete stability observes, parity failure rejects, overhead demotes" {
            Expect.equal (RetainedRender.promotionDecision "a" 1 3 100 10 true).Decision Observe "wait for stability"
            Expect.equal (RetainedRender.promotionDecision "b" 3 3 100 10 false).Decision Reject "parity failure rejects"
            Expect.equal (RetainedRender.promotionDecision "c" 3 3 20 25 true).Decision Demote "overhead demotes"
        }

        test "placement-only movement damages old and new regions" {
            let damage = RetainedRender.placementDamage 100 100 (rect 0.0 0.0 20.0 20.0) (rect 10.0 0.0 20.0 20.0)
            Expect.equal damage.Regions.Length 2 "old and new placements are damaged"
            Expect.equal damage.UnionArea 600 "overlap counted once"
        }
    ]
