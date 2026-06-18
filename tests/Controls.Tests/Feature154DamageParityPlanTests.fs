module Feature154DamageParityPlanTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature154 damage parity plan" [
        test "required parity scenarios keep retained damage bounded or fallback-explicit" {
            let localized = RetainedRender.damageRegionSet 120 100 false "damage/localized-update" [ rect 10.0 10.0 24.0 16.0 ]
            let movement = RetainedRender.placementDamage 120 100 (rect 10.0 10.0 24.0 16.0) (rect 40.0 10.0 24.0 16.0)
            let fullFrame = RetainedRender.damageRegionSet 120 100 true "damage/full-invalidation" [ rect 0.0 0.0 120.0 100.0 ]

            Expect.equal (RetainedRender.classifyDamageFallback true None localized) None "localized can be scoped when proof is ready"
            Expect.isTrue (movement.UnionArea > localized.UnionArea) "movement covers old and new placement"
            Expect.equal (RetainedRender.classifyDamageFallback true None fullFrame) (Some FullFrameInvalidation) "full invalidation falls back"
        }

        test "unsafe proof and resource failure suppress damage-scoped parity acceptance" {
            let invalidDamage = RetainedRender.damageRegionSet 120 100 false "damage/invalid-damage" [ rect -20.0 -20.0 200.0 200.0 ]
            let resourceFailure = RetainedRender.damageRegionSet 120 100 false "damage/resource-failure" [ rect 10.0 10.0 24.0 16.0 ]

            Expect.equal
                (RetainedRender.classifyDamageFallback false (Some "missing accepted proof set") invalidDamage)
                (Some(FailedProof "missing accepted proof set"))
                "missing proof suppresses scissor candidate"

            Expect.equal
                (RetainedRender.classifyDamageFallback false (Some "resource failure") resourceFailure)
                (Some(FailedProof "resource failure"))
                "resource failure records fallback"
        }
    ]
