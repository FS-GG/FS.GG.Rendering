module Feature142ShapedTextDeterminismTests

open Expecto
open FS.GG.UI.Scene

let private font: FontSpec = { Family = Some "Inter"; Size = 16.0; Weight = Some 400 }

[<Tests>]
let tests =
    testList "Feature142 shaped text determinism" [
        test "fallback fingerprints are byte-stable across three repeated runs" {
            let fingerprints =
                [ for _ in 1..3 -> (Scene.buildFallbackShapedText "deterministic text" font).Fingerprint ]

            Expect.equal (fingerprints |> List.distinct |> List.length) 1 "same input produces one fingerprint"
        }

        test "render-affecting font changes alter fingerprints" {
            let normal = Scene.buildFallbackShapedText "same text" font
            let bigger = Scene.buildFallbackShapedText "same text" { font with Size = 22.0 }

            Expect.notEqual normal.Fingerprint bigger.Fingerprint "size participates in the fingerprint"
        }
    ]
