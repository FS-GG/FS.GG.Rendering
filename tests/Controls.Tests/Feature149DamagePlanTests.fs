module Feature149DamagePlanTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature149 damage plan policy" [
        test "overlapping damage uses true union area and stays inside the frame" {
            let damage =
                RetainedRender.damageRegionSet
                    100
                    80
                    false
                    "damage/overlap"
                    [ rect 0.0 0.0 40.0 40.0
                      rect 20.0 20.0 40.0 40.0 ]

            Expect.equal damage.Cause "damage/overlap" "cause"
            Expect.equal damage.Regions.Length 2 "visible regions"
            Expect.isLessThan damage.UnionArea 3200 "overlap counted once"
            Expect.isTrue (damage.UnionArea <= 8000) "union never exceeds frame"
        }

        test "resource and internal failures remain explicit fallback decisions" {
            let localized = RetainedRender.damageRegionSet 100 80 false "damage/resource-failure" [ rect 10.0 10.0 20.0 20.0 ]

            Expect.equal
                (RetainedRender.classifyDamageFallback false (Some "resource failure") localized)
                (Some(FailedProof "resource failure"))
                "resource fallback"

            Expect.equal
                (RetainedRender.classifyDamageFallback false (Some "internal error") localized)
                (Some(FailedProof "internal error"))
                "internal fallback"
        }
    ]
