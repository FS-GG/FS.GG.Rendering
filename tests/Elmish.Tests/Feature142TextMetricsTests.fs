module Feature142TextMetricsTests

open Expecto
open FS.GG.UI.Scene

let private font: FontSpec = { Family = None; Size = 18.0; Weight = None }

[<Tests>]
let tests =
    testList "Feature142 Elmish text metrics contract" [
        test "shaped metric evidence is stable enough for retained frame metrics" {
            let direct = Scene.buildFallbackShapedText "retained text" font
            let cold = Scene.buildFallbackShapedText "retained text" font
            let warm = Scene.buildFallbackShapedText "retained text" font

            Expect.equal direct.Fingerprint cold.Fingerprint "direct and cold retained evidence match for unchanged text"
            Expect.equal cold.Fingerprint warm.Fingerprint "warm retained evidence remains stable"
            Expect.equal (Scene.measureShapedText direct) (Scene.measureShapedText warm) "metrics are equivalent across modes"
        }
    ]
