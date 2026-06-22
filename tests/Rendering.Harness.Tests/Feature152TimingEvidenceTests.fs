module Feature152TimingEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature152 timing evidence" [
        test "timing report requires five scenarios, five repetitions, and no overclaim" {
            let rendered = Compositor.Render.emitFeature152TimingReport "damage"

            [ "at least 5 representative live scenarios"
              "at least 5 comparable repetitions"
              "Verdict: `environment-limited`"
              "No compositor performance claim is accepted" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }
    ]
