module Feature142RunItemizationTests

open Expecto
open FS.GG.UI.Scene

let private font: FontSpec = { Family = None; Size = 18.0; Weight = None }

[<Tests>]
let tests =
    testList "Feature142 run itemization evidence" [
        test "Latin fallback result records left-to-right Latin evidence" {
            let shaped = Scene.buildFallbackShapedText "Hello" font
            let run = shaped.Runs |> List.exactlyOne

            Expect.equal run.Direction LeftToRight "Latin text is LTR"
            Expect.equal run.Script LatinScript "Latin text is bucketed"
            Expect.equal run.TextRange (0, 5) "source range is recorded"
        }

        test "mixed direction text is disclosed as mixed" {
            let shaped = Scene.buildFallbackShapedText "abc \u0633\u0644\u0627\u0645" font
            let run = shaped.Runs |> List.exactlyOne

            Expect.equal run.Direction MixedDirection "mixed LTR/RTL text records mixed direction"
            Expect.equal run.Script MixedScript "mixed script evidence is recorded"
        }
    ]
