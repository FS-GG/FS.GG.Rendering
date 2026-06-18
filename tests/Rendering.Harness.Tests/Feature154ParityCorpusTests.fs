module Feature154ParityCorpusTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature154 parity corpus" [
        test "parity scenario inventory contains the ten required scenario ids" {
            [ "damage/localized-update"
              "damage/no-change"
              "damage/movement"
              "damage/overlap"
              "damage/edge-clipping"
              "damage/resize"
              "damage/full-invalidation"
              "damage/invalid-damage"
              "damage/unsupported-host"
              "damage/resource-failure" ]
            |> List.iter (fun scenario -> Expect.contains Compositor.feature154ScenarioIds scenario scenario)
        }

        test "parity report records same-profile proof gate and fallback reasons" {
            let rendered = Compositor.renderFeature154ParityReport ()

            [ "Status: `fallback-gated`"
              "Host profile binding: `same-profile-required`"
              "`damage/localized-update`"
              "`damage/full-invalidation`"
              "`damage/unsupported-host`"
              "Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw." ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }
    ]
