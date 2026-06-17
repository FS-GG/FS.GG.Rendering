module Feature147DamageUnionTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature147 damage union policy" [
        test "overlapping damage is clipped, deduplicated, and counted once" {
            let damage =
                RetainedRender.damageRegionSet
                    100
                    100
                    false
                    "localized"
                    [ rect 0.0 0.0 20.0 20.0
                      rect 10.0 10.0 20.0 20.0
                      rect 10.0 10.0 20.0 20.0
                      rect -10.0 -10.0 15.0 15.0 ]

            Expect.equal damage.Regions.Length 3 "duplicate rectangles are removed after clipping"
            Expect.equal damage.UnionArea 700 "overlap is counted once"
            Expect.isTrue (damage.UnionArea <= 10000) "damage never exceeds frame area"
        }

        test "full-frame invalidation produces a full-frame damage region" {
            let damage = RetainedRender.damageRegionSet 80 60 true "theme" [ rect 10.0 10.0 5.0 5.0 ]
            Expect.equal damage.Regions.Length 1 "one full-frame region"
            Expect.equal damage.UnionArea (80 * 60) "full frame area"
            Expect.isTrue damage.FullFrameInvalidation "flag preserved"
        }

        test "fallback classification rejects missing proof and full-frame invalidation" {
            let localized = RetainedRender.damageRegionSet 80 60 false "localized" [ rect 1.0 1.0 10.0 10.0 ]
            let full = RetainedRender.damageRegionSet 80 60 true "resize" []

            Expect.equal (RetainedRender.classifyDamageFallback false None localized) (Some MissingProof) "missing proof blocks scissor"
            Expect.equal (RetainedRender.classifyDamageFallback true None full) (Some FullFrameInvalidation) "full-frame invalidation blocks scissor"
            Expect.equal (RetainedRender.classifyDamageFallback true None localized) None "ready proof + localized damage may scissor"
        }
    ]
