module Feature153ReadinessPackageTests

open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature153 readiness package" [
        test "validation summary links attempts proof set fallback compatibility package and regression evidence" {
            let model0, _ = Compositor.FeatureState.initReadiness ()
            let model1, _ = Compositor.FeatureState.updateReadiness (Compositor.Types.TierEvaluated(Compositor.Types.DamageScissorTier, Compositor.Types.Limited "missing proof")) model0
            let rendered = Compositor.Render.emitFeature153ValidationSummary model1

            [ "Status: `environment-limited`"
              "Proof set: `environment-limited`"
              "Fallback status: `fallback-gated`"
              "Performance claim: `not-accepted`"
              "live-proof/attempts/README.md"
              "live-proof/unsupported/README.md"
              "proof-set.md"
              "compatibility-ledger.md"
              "package-validation.md"
              "regression-validation.md" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "proof-set renderer keeps selected attempts explicit and non-accepting when evidence is missing" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render.emitFeature153ProofSet model

            [ "Status: `environment-limited`"
              "Selected attempts: `0/3`"
              "no accepted three-run capable-host proof set"
              "Partial redraw remains fallback-gated" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }

        test "compatibility ledger documents public proof and fallback behavior" {
            let model, _ = Compositor.FeatureState.initReadiness ()
            let rendered = Compositor.Render.emitFeature153CompatibilityLedger model

            [ "CompositorProof.AcceptedProofSet"
              "GlHost.LiveProofHostFacts"
              "Viewer.liveProofInterpreterSupported"
              "CompositorReadiness"
              "Migration Guidance"
              "Synthetic Disclosure" ]
            |> List.iter (fun required -> Expect.stringContains rendered required required)
        }
    ]
