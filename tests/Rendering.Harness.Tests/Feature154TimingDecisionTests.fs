module Feature154TimingDecisionTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature154 timing decision" [
        test "timing report declares threshold noise policy scenario count and repetitions" {
            let rendered = Compositor.Render2.emitFeature154TimingReport "damage" 5 5

            [ "Decision: `inconclusive`"
              "Performance claim: `not-accepted`"
              "Policy: `same-profile-live-threshold-v1`"
              "Scenario count: `5`"
              "Repetitions per scenario: `5`"
              "Context-only evidence" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "timing tier inventory keeps damage claim separate from safety readiness" {
            Expect.contains Compositor.Config.feature154TimingTiers "damage" "damage timing tier"
        }
    ]
