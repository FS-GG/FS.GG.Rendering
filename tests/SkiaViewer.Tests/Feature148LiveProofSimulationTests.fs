module Feature148LiveProofSimulationTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature148 Synthetic live proof simulations" [
        test "Feature148 Synthetic non-preserving host fails instead of passing proof" {
            // SYNTHETIC: unmatched untouched pixels model a non-preserving compositor without native readback.
            let verdict =
                CompositorProof.classifyObservations
                    [ { RegionId = "u1"
                        Kind = CompositorProof.ObservedRegionKind.Untouched
                        ExpectedIdentity = "sentinel"
                        ActualIdentity = "cleared"
                        Matched = false }
                      { RegionId = "d1"
                        Kind = CompositorProof.ObservedRegionKind.Damaged
                        ExpectedIdentity = "sentinel"
                        ActualIdentity = "sentinel"
                        Matched = true } ]

            Expect.equal verdict (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.ClearedPixels) "cleared pixels fail proof"
        }

        test "Feature148 Synthetic unsupported readback is environment-limited" {
            // SYNTHETIC: empty observations model readback unavailable; no accepted host proof is claimed.
            match CompositorProof.classifyObservations [] with
            | CompositorProof.PresentProofEnvironmentLimited reason ->
                Expect.stringContains reason "missing" "missing samples"
            | other -> failtestf "expected environment-limited, got %A" other
        }

        test "Feature148 Synthetic missing display timeout permission and host error remain disclosed failure categories" {
            // SYNTHETIC: symbolic failure causes exercise formatting only; live-host errors need real artifacts.
            [ CompositorProof.PresentProofFailureCause.MissingDisplay
              CompositorProof.PresentProofFailureCause.Timeout
              CompositorProof.PresentProofFailureCause.HostError "permission denied"
              CompositorProof.PresentProofFailureCause.SyntheticEvidence ]
            |> List.iter (fun cause ->
                let text = CompositorProof.failureCauseText cause
                Expect.isFalse (System.String.IsNullOrWhiteSpace text) $"cause {cause} has text")
        }
    ]
