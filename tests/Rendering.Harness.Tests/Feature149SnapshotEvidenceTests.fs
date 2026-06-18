module Feature149SnapshotEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature149 snapshot evidence" [
        test "snapshot report records lifecycle, budget, unsupported, stale, and parity gates" {
            let rendered = Compositor.renderFeature149SnapshotReport ()

            [ "snapshot/expensive-stable"
              "snapshot/create-reuse-refresh"
              "snapshot/replacement-eviction-disposal"
              "snapshot/over-budget"
              "snapshot/stale-resource"
              "snapshot/invalid-resource"
              "snapshot/unsupported-host"
              "snapshot/parity-failure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)

            Expect.stringContains rendered (string Compositor.snapshotBudget.MaxEntries) "entry budget"
            Expect.stringContains rendered (string Compositor.snapshotBudget.MaxBytes) "byte budget"
            Expect.stringContains rendered "20%" "benefit threshold"
        }
    ]
