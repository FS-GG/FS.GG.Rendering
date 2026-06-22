module Feature147CompositorReadinessTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature147 compositor readiness package" [
        test "readiness update records proof, parity, tier verdict, and diagnostics" {
            let model0, effects = Compositor.FeatureState.initReadiness ()
            Expect.isTrue (effects |> List.exists (function Compositor.Types.WriteValidationSummary _ -> true | _ -> false)) "summary effect"

            let model1, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.DiagnosticRecorded "loaded") model0
            let model2, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.ParityRecorded("damage/localized-update", Compositor.Types.ParityPassed)) model1
            let model3, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.DamageScissorTier, Compositor.Types.Ready)) model2

            let summary = Compositor.Render.renderValidationSummary model3
            Expect.stringContains summary "Damage scissor | ready" "ready damage tier"
            Expect.stringContains summary "damage/localized-update" "parity row"
            Expect.stringContains summary "loaded" "diagnostic"
        }

        test "compatibility ledger states fallback and environment-limited limitations" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render.renderCompatibilityLedger model
            Expect.stringContains rendered "Damage-scissored redraw is proof-gated" "release note"
            Expect.stringContains rendered "Environment-limited host observations" "limitation"
        }
    ]
