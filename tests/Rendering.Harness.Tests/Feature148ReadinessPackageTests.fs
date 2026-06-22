module Feature148ReadinessPackageTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature148 readiness package" [
        test "readiness summary links proof, parity, reuse, snapshot, and timing evidence" {
            let model0, _ = Compositor.FeatureState.initReadiness ()
            let model1, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.PlacementReuseTier, Compositor.Types.Ready)) model0
            let model2, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.SnapshotTier, Compositor.Types.Limited "missing timing")) model1

            let rendered = Compositor.Render.emitFeature148ValidationSummary model2

            [ "Live proof"
              "Damage scissor"
              "Placement reuse"
              "Replay"
              "Snapshot"
              "live-proof/proof.md"
              "parity/parity.md"
              "reuse/reuse.md"
              "snapshots/snapshots.md"
              "timing/timing-*.md" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "compatibility ledger discloses public metrics, baselines, migration, and synthetic limitations" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render.emitFeature148CompatibilityLedger model

            [ "## Public Metrics and Diagnostics"
              "## Baseline References"
              "## Release Notes Draft"
              "## Migration Guidance"
              "## Limitations"
              "Synthetic simulations" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }
    ]
