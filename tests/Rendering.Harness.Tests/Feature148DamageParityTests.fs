module Feature148DamageParityTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature148 damage parity evidence" [
        test "parity report lists the required damage corpus and fallback cases" {
            let rendered = Compositor.renderFeature148ParityReport ()

            [ "damage/localized-update"
              "damage/overlap"
              "damage/frame-edge"
              "damage/movement-old-new"
              "damage/resize"
              "damage/theme-global"
              "damage/stale-proof"
              "damage/disabled"
              "damage/unsupported"
              "damage/parity-failure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)

            Expect.stringContains rendered "Full-frame oracle parity" "oracle disclosed"
        }

        test "Feature148 readiness paths stay inside the feature readiness tree" {
            let path = Compositor.feature148ArtifactPath "parity" "parity.md"
            Expect.isTrue (TestAssertions.feature148ReadinessPath path) "readiness path"
        }
    ]
