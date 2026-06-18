module Feature152DamagePlanTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature152 damage plan policy" [
        test "edge-clipped and overlapping damage stays bounded" {
            let damage =
                RetainedRender.damageRegionSet
                    100
                    80
                    false
                    "damage/edge-clipped"
                    [ rect -20.0 -20.0 40.0 40.0
                      rect 10.0 10.0 30.0 30.0 ]

            Expect.equal damage.Cause "damage/edge-clipped" "cause"
            Expect.isTrue (damage.UnionArea > 0) "non-zero"
            Expect.isTrue (damage.UnionArea <= 8000) "bounded to frame"
        }

        test "resource failure and full invalidation are explicit fallbacks" {
            let fullFrame = RetainedRender.damageRegionSet 100 80 true "damage/full-frame-invalidation" [ rect 0.0 0.0 100.0 80.0 ]
            let localized = RetainedRender.damageRegionSet 100 80 false "damage/resource-failure" [ rect 10.0 10.0 20.0 20.0 ]

            Expect.equal
                (RetainedRender.classifyDamageFallback true None fullFrame)
                (Some FullFrameInvalidation)
                "full-frame fallback"

            Expect.equal
                (RetainedRender.classifyDamageFallback false (Some "resource failure") localized)
                (Some(FailedProof "resource failure"))
                "resource fallback"
        }
    ]
