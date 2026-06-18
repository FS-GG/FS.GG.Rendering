module Feature152ReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

let private evidence name status =
    { EvidenceName = name
      EvidencePath = Some($"specs/152-compositor-live-proof/readiness/{name}")
      EvidenceStatus = status
      EvidenceRequired = true
      EvidenceDiagnostics = [] }

[<Tests>]
let tests =
    testList "Feature152 compositor readiness helper" [
        test "status tokens are stable" {
            [ CompositorReadinessAccepted, "accepted"
              CompositorReadinessFallbackGated, "fallback-gated"
              CompositorReadinessFailed, "failed"
              CompositorReadinessEnvironmentLimited, "environment-limited"
              CompositorReadinessMissingEvidence, "missing-evidence"
              CompositorReadinessCompatibilityBlocked, "compatibility-blocked" ]
            |> List.iter (fun (status, token) -> Expect.equal (CompositorReadiness.statusText status) token token)
        }

        test "accepted proof, parity, compatibility, and regression accept correctness even when timing is not a performance claim" {
            let report =
                { Feature = "152-compositor-live-proof"
                  ProofStatus = CompositorReadinessAccepted
                  ParityStatus = CompositorReadinessAccepted
                  TimingStatus = CompositorReadinessEnvironmentLimited
                  CompatibilityStatus = CompositorReadinessAccepted
                  RegressionStatus = CompositorReadinessAccepted
                  Evidence =
                    [ evidence "validation-summary.md" CompositorReadinessAccepted
                      evidence "compatibility-ledger.md" CompositorReadinessAccepted ]
                  Limitations = [] }

            let result = CompositorReadiness.validate report

            Expect.isTrue result.Accepted "correctness readiness accepted"
            Expect.exists result.Diagnostics (fun item -> item.Contains("timing claim status")) "timing no-claim is visible"
        }

        test "missing proof evidence blocks readiness" {
            let report =
                { Feature = "152-compositor-live-proof"
                  ProofStatus = CompositorReadinessAccepted
                  ParityStatus = CompositorReadinessAccepted
                  TimingStatus = CompositorReadinessEnvironmentLimited
                  CompatibilityStatus = CompositorReadinessAccepted
                  RegressionStatus = CompositorReadinessAccepted
                  Evidence = [ { evidence "validation-summary.md" CompositorReadinessAccepted with EvidencePath = None; EvidenceStatus = CompositorReadinessMissingEvidence } ]
                  Limitations = [] }

            let result = CompositorReadiness.validate report

            Expect.isFalse result.Accepted "missing evidence blocks"
            Expect.equal result.Status CompositorReadinessMissingEvidence "missing evidence wins"
        }
    ]
