module Feature149DamageParityTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature149 damage parity evidence" [
        test "parity report lists required damage, fallback, and failure cases" {
            let rendered = Compositor.renderFeature149ParityReport ()

            [ "damage/localized-update"
              "damage/overlap"
              "damage/frame-edge"
              "damage/movement-old-new"
              "damage/resize"
              "damage/theme-global"
              "damage/zero-damage"
              "damage/resource-failure"
              "damage/internal-error"
              "damage/parity-failure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)

            Expect.stringContains rendered "Full-frame oracle parity" "oracle disclosed"
            Expect.stringContains rendered "live pixel parity remains limited" "live limitation disclosed"
        }

        test "Feature149 readiness paths stay inside the feature readiness tree" {
            let path = Compositor.feature149ArtifactPath "parity" "parity.md"
            Expect.isTrue (TestAssertions.feature149ReadinessPath path) "readiness path"
        }
    ]
