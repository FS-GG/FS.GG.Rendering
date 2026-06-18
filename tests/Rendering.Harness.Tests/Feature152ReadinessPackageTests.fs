module Feature152ReadinessPackageTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature152 readiness package" [
        test "validation summary links proof, parity, timing, compatibility, package, and regression evidence" {
            let model0, _ = Compositor.initReadiness ()
            let model1, _ = Compositor.updateReadiness (Compositor.TierEvaluated(Compositor.DamageScissorTier, Compositor.Limited "missing proof")) model0
            let rendered = Compositor.renderFeature152ValidationSummary model1

            [ "Status: `environment-limited`"
              "Performance claim: `environment-limited`"
              "live-proof/README.md"
              "live-proof/unsupported/README.md"
              "parity/README.md"
              "timing/README.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "compatibility ledger documents public helper surface and fallback behavior" {
            let model, _ = Compositor.initReadiness ()
            let rendered = Compositor.renderFeature152CompatibilityLedger model

            [ "CompositorProof"
              "CompositorReadiness"
              "fallback"
              "Migration Guidance"
              "Limitations" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }
    ]
