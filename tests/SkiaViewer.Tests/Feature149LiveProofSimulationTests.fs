module Feature149LiveProofSimulationTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature149 Synthetic live proof simulations" [
        test "Feature149 Synthetic non-preserving host fails instead of passing proof" {
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
                        ExpectedIdentity = "damage"
                        ActualIdentity = "damage"
                        Matched = true } ]

            Expect.equal verdict (CompositorProof.PresentProofFailed CompositorProof.PresentProofFailureCause.ClearedPixels) "cleared pixels fail proof"
        }

        test "Feature149 Synthetic unsupported readback is environment-limited" {
            // SYNTHETIC: no observations models a host that cannot produce readback samples.
            match CompositorProof.classifyObservations [] with
            | CompositorProof.PresentProofEnvironmentLimited reason -> Expect.stringContains reason "missing" "reason"
            | other -> failtestf "expected environment-limited, got %A" other
        }

        test "Feature149 Synthetic failure categories remain disclosed" {
            // SYNTHETIC: failure-case vocabulary check; real host classification happens at the interpreter edge.
            [ CompositorProof.PresentProofFailureCause.MissingDisplay
              CompositorProof.PresentProofFailureCause.Timeout
              CompositorProof.PresentProofFailureCause.HostError "permission denied"
              CompositorProof.PresentProofFailureCause.SyntheticEvidence ]
            |> List.iter (fun cause ->
                let text = CompositorProof.failureCauseText cause
                Expect.isFalse (System.String.IsNullOrWhiteSpace text) "failure cause has text")
        }
    ]
