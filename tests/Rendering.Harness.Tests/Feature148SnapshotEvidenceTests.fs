module Feature148SnapshotEvidenceTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature148 snapshot evidence" [
        test "snapshot report records lifecycle and resource-budget readiness gates" {
            let rendered = Compositor.Render.emitFeature148SnapshotReport ()

            [ "snapshot/expensive-stable"
              "snapshot/simple-scene"
              "snapshot/churning"
              "snapshot/over-budget"
              "snapshot/invalid-resource"
              "snapshot/unsupported-host"
              "snapshot/parity-failure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)

            Expect.stringContains rendered (string Compositor.Config.snapshotBudget.MaxEntries) "entry budget"
            Expect.stringContains rendered (string Compositor.Config.snapshotBudget.MaxBytes) "byte budget"
            Expect.stringContains rendered "20%" "benefit threshold"
        }
    ]
