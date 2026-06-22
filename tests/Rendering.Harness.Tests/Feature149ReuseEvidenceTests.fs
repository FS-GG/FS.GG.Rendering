module Feature149ReuseEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature149 reuse evidence" [
        test "reuse report records placement-only, mixed-change, no-change, churn, and parity cases" {
            let rendered = Compositor.Render.emitFeature149ReuseReport ()

            [ "reuse/stable-boundary"
              "reuse/placement-only"
              "reuse/mixed-change"
              "reuse/no-change"
              "reuse/content-changing"
              "reuse/churning"
              "reuse/no-benefit"
              "reuse/failed-parity"
              "reuse/same-seed" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)

            Expect.stringContains rendered "old/new movement damage" "movement damage disclosed"
            Expect.stringContains rendered "benefit checks" "benefit gate disclosed"
        }
    ]
