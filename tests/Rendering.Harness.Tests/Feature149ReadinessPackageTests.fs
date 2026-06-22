module Feature149ReadinessPackageTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature149 readiness package" [
        test "readiness summary links proof, parity, reuse, snapshot, timing, and compatibility evidence" {
            let model0, _ = Compositor.FeatureState.initReadiness ()
            let model1, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Ready)) model0
            let model2, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "missing timing")) model1

            let rendered = Compositor.Render.emitFeature149ValidationSummary model2

            [ "Status: `environment-limited`"
              "Live proof"
              "Damage scissor"
              "Placement reuse"
              "Replay"
              "Snapshot"
              "Timing"
              "Public diagnostics"
              "live-proof/proof.md"
              "parity/parity.md"
              "reuse/reuse.md"
              "snapshots/snapshots.md"
              "timing/timing-*.md"
              "compatibility-ledger.md" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "compatibility ledger discloses public metrics, baselines, migration, and synthetic limitations" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render.emitFeature149CompatibilityLedger model

            [ "## Public Metrics and Diagnostics"
              "Feature149 harness routes"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations"
              "Synthetic simulations" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "evidence disclosure forbids live partial-redraw overclaims" {
            let rendered = Evidence.feature149NonOverclaimDisclosure ()

            Expect.stringContains rendered "fresh matching capable-host live proof" "proof gate"
            Expect.stringContains rendered "environment-limited or synthetic evidence cannot enable partial redraw" "non-overclaim rule"
        }
    ]
