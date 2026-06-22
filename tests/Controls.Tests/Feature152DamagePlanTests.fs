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
                CompositorPolicy.damageRegionSet
                    { FrameWidth = 100
                      FrameHeight = 80
                      FullFrameInvalidation = false
                      Cause = "damage/edge-clipped"
                      Boxes =
                        [ rect -20.0 -20.0 40.0 40.0
                          rect 10.0 10.0 30.0 30.0 ] }

            Expect.equal damage.Cause "damage/edge-clipped" "cause"
            Expect.isTrue (damage.UnionArea > 0) "non-zero"
            Expect.isTrue (damage.UnionArea <= 8000) "bounded to frame"
        }

        test "resource failure and full invalidation are explicit fallbacks" {
            let fullFrame = CompositorPolicy.damageRegionSet { FrameWidth = 100; FrameHeight = 80; FullFrameInvalidation = true; Cause = "damage/full-frame-invalidation"; Boxes = [ rect 0.0 0.0 100.0 80.0 ] }
            let localized = CompositorPolicy.damageRegionSet { FrameWidth = 100; FrameHeight = 80; FullFrameInvalidation = false; Cause = "damage/resource-failure"; Boxes = [ rect 10.0 10.0 20.0 20.0 ] }

            Expect.equal
                (CompositorPolicy.classifyDamageFallback true None fullFrame)
                (Some FullFrameInvalidation)
                "full-frame fallback"

            Expect.equal
                (CompositorPolicy.classifyDamageFallback false (Some "resource failure") localized)
                (Some(FailedProof "resource failure"))
                "resource fallback"
        }
    ]
