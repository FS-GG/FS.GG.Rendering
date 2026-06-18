module Feature152DamageParityTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature152 damage parity evidence" [
        test "parity report lists accepted, fallback, unsupported, and rejected scenario classes" {
            let rendered = Compositor.renderFeature152ParityReport ()

            [ "damage/localized-update"
              "damage/no-change"
              "damage/resize"
              "damage/full-frame-invalidation"
              "damage/invalid-damage"
              "damage/unsupported"
              "damage/parity-failure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "readiness paths stay inside the Feature152 readiness tree" {
            let path = Compositor.feature152ArtifactPath "parity" "README.md"
            Expect.stringContains path "specs/152-compositor-live-proof/readiness/parity" "readiness path"
        }
    ]
