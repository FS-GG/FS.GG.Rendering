module Feature142TextFixtureCorpusTests

open Expecto
open FS.GG.UI.Scene
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature142 text fixture corpus" [
        test "fixture corpus has at least 40 cases and eight categories" {
            let fixtures = TextShapingFixtures.all
            let categories = fixtures |> List.map _.Category |> List.distinct

            Expect.isGreaterThanOrEqual fixtures.Length 40 "at least 40 fixtures are present"
            Expect.isGreaterThanOrEqual categories.Length 8 "at least eight categories are represented"
        }

        test "fallback oracle accepts the declared corpus expectations" {
            for fixture in TextShapingFixtures.all do
                let shaped = Scene.buildFallbackShapedText fixture.Text fixture.Font
                Expect.isTrue (TextShapingOracle.assertFingerprintStable shaped) $"fingerprint stable for {fixture.Id}"
                Expect.isTrue (TextShapingOracle.measureDrawAdvanceDelta shaped <= 1.0) $"measure/draw advance parity for {fixture.Id}"
        }
    ]
