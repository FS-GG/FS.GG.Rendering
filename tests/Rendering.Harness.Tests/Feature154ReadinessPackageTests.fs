module Feature154ReadinessPackageTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature154 readiness package" [
        test "validation summary links proof parity timing fallback compatibility package and regression evidence" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render2.emitFeature154ValidationSummary model

            [ "Status: `environment-limited`"
              "Proof set: `environment-limited`"
              "Parity status: `fallback-gated`"
              "Timing status: `inconclusive`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "Selected attempts: `0/3`"
              "proof-set.md"
              "parity/README.md"
              "timing/timing-damage.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "compatibility ledger records no new public surface and migration guidance" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render2.emitFeature154CompatibilityLedger model

            [ "CompositorProof.AcceptedProofSet"
              "CompositorReadiness"
              "No new public `.fsi` surface is required"
              "Controls and Controls.Elmish compositor diagnostics"
              "Migration Guidance"
              "Synthetic Disclosure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "MVU publication contract records proof parity timing and artifact effects" {
            let model0, effects0 = Compositor.FeatureState.initFeature154 ()
            Expect.exists effects0 (function Compositor.Types.WriteFeature154Artifact path -> path.EndsWith("validation-summary.md")) "initial summary effect"

            let model1, _ = Compositor.FeatureState.updateFeature154 (Compositor.Types.ProofEvidenceRecorded "environment-limited") model0
            let model2, _ = Compositor.FeatureState.updateFeature154 (Compositor.Types.ParityEvidenceRecorded "fallback-gated") model1
            let model3, _ = Compositor.FeatureState.updateFeature154 (Compositor.Types.TimingEvidenceRecorded "inconclusive") model2
            let model4, _ = Compositor.FeatureState.updateFeature154 (Compositor.Types.ArtifactPublished "proof-set.md") model3

            Expect.equal model4.ProofStatus "environment-limited" "proof"
            Expect.equal model4.ParityStatus "fallback-gated" "parity"
            Expect.equal model4.TimingStatus "inconclusive" "timing"
            Expect.contains model4.PublishedArtifacts "proof-set.md" "artifact"
        }
    ]
