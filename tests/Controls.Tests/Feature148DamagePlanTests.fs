module Feature148DamagePlanTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private rect x y w h : Rect =
    { X = x; Y = y; Width = w; Height = h }

[<Tests>]
let tests =
    testList "Feature148 damage plan policy" [
        test "clipped overlapping edge damage has true union area and source cause" {
            let damage =
                RetainedRender.damageRegionSet
                    100
                    80
                    false
                    "damage/frame-edge"
                    [ rect -5.0 -5.0 20.0 20.0
                      rect 10.0 10.0 20.0 20.0
                      rect 90.0 70.0 20.0 20.0 ]

            Expect.equal damage.Cause "damage/frame-edge" "cause/source boundary"
            Expect.equal damage.Regions.Length 3 "visible clipped regions"
            Expect.isTrue (damage.UnionArea <= 8000) "union never exceeds frame"
            Expect.isTrue (damage.Regions |> List.forall (fun r -> r.DamageX >= 0 && r.DamageY >= 0)) "clipped to frame"
        }

        test "movement damage covers old and new placement regions" {
            let damage = RetainedRender.placementDamage 120 100 (rect 0.0 0.0 30.0 20.0) (rect 20.0 5.0 30.0 20.0)
            Expect.equal damage.Cause "placement-only movement" "movement cause"
            Expect.equal damage.Regions.Length 2 "old and new regions"
            Expect.isGreaterThan damage.UnionArea 0 "movement damages non-zero area"
        }

        test "fallback classification covers missing, failed, environment-limited, empty, and full-frame cases" {
            let empty = RetainedRender.damageRegionSet 100 80 false "damage/idle" []
            let localized = RetainedRender.damageRegionSet 100 80 false "damage/localized-update" [ rect 10.0 10.0 20.0 20.0 ]
            let full = RetainedRender.damageRegionSet 100 80 true "damage/theme-global" [ rect 10.0 10.0 20.0 20.0 ]

            Expect.equal (RetainedRender.classifyDamageFallback false None localized) (Some MissingProof) "missing proof"
            Expect.equal (RetainedRender.classifyDamageFallback false (Some "environment-limited readback") localized) (Some(EnvironmentLimited "environment-limited readback")) "environment"
            Expect.equal (RetainedRender.classifyDamageFallback false (Some "stale proof") localized) (Some(FailedProof "stale proof")) "failed proof"
            Expect.equal (RetainedRender.classifyDamageFallback true None empty) (Some EmptyDamage) "empty idle"
            Expect.equal (RetainedRender.classifyDamageFallback true None full) (Some FullFrameInvalidation) "full frame"
        }
    ]
