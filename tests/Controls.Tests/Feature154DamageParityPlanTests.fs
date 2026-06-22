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
            let localized = CompositorPolicy.damageRegionSet { FrameWidth = 120; FrameHeight = 100; FullFrameInvalidation = false; Cause = "damage/localized-update"; Boxes = [ rect 10.0 10.0 24.0 16.0 ] }
            let movement = CompositorPolicy.placementDamage 120 100 (rect 10.0 10.0 24.0 16.0) (rect 40.0 10.0 24.0 16.0)
            let fullFrame = CompositorPolicy.damageRegionSet { FrameWidth = 120; FrameHeight = 100; FullFrameInvalidation = true; Cause = "damage/full-invalidation"; Boxes = [ rect 0.0 0.0 120.0 100.0 ] }

            Expect.equal (CompositorPolicy.classifyDamageFallback true None localized) None "localized can be scoped when proof is ready"
            Expect.isTrue (movement.UnionArea > localized.UnionArea) "movement covers old and new placement"
            Expect.equal (CompositorPolicy.classifyDamageFallback true None fullFrame) (Some FullFrameInvalidation) "full invalidation falls back"
        }

        test "unsafe proof and resource failure suppress damage-scoped parity acceptance" {
            let invalidDamage = CompositorPolicy.damageRegionSet { FrameWidth = 120; FrameHeight = 100; FullFrameInvalidation = false; Cause = "damage/invalid-damage"; Boxes = [ rect -20.0 -20.0 200.0 200.0 ] }
            let resourceFailure = CompositorPolicy.damageRegionSet { FrameWidth = 120; FrameHeight = 100; FullFrameInvalidation = false; Cause = "damage/resource-failure"; Boxes = [ rect 10.0 10.0 24.0 16.0 ] }

            Expect.equal
                (CompositorPolicy.classifyDamageFallback false (Some "missing accepted proof set") invalidDamage)
                (Some(FailedProof "missing accepted proof set"))
                "missing proof suppresses scissor candidate"

            Expect.equal
                (CompositorPolicy.classifyDamageFallback false (Some "resource failure") resourceFailure)
                (Some(FailedProof "resource failure"))
                "resource failure records fallback"
        }
    ]
