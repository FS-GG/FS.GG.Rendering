module Feature148ReuseEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature148 reuse evidence" [
        test "reuse report records movement, content-change, churn, parity, and same-seed cases" {
            let rendered = Compositor.renderFeature148ReuseReport ()

            [ "reuse/stable-boundary"
              "reuse/moving-only"
              "reuse/scrolling"
              "reuse/content-changing"
              "reuse/churning"
              "reuse/failed-parity"
              "reuse/same-seed" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)

            Expect.stringContains rendered "old and new placement regions" "movement damage disclosed"
            Expect.stringContains rendered "30%" "threshold disclosed"
        }
    ]
