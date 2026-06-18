module Feature153ReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

let private evidence name status =
    { EvidenceName = name
      EvidencePath = Some($"specs/153-compositor-proof-interpreter/readiness/{name}")
      EvidenceStatus = status
      EvidenceRequired = true
      EvidenceDiagnostics = [] }

[<Tests>]
let tests =
    testList "Feature153 compositor readiness helper" [
        test "accepted proof and parity accept correctness while timing remains a non-claim" {
            let report =
                { Feature = "153-compositor-proof-interpreter"
                  ProofStatus = CompositorReadinessAccepted
                  ParityStatus = CompositorReadinessAccepted
                  TimingStatus = CompositorReadinessEnvironmentLimited
                  CompatibilityStatus = CompositorReadinessAccepted
                  RegressionStatus = CompositorReadinessAccepted
                  Evidence =
                    [ evidence "validation-summary.md" CompositorReadinessAccepted
                      evidence "proof-set.md" CompositorReadinessAccepted
                      evidence "compatibility-ledger.md" CompositorReadinessAccepted ]
                  Limitations = [] }

            let result = CompositorReadiness.validate report

            Expect.isTrue result.Accepted "correctness readiness is accepted"
            Expect.exists result.Diagnostics (fun item -> item.Contains("timing claim status")) "timing no-claim is visible"
        }

        test "environment-limited proof status is non-accepting even when files exist" {
            let report =
                { Feature = "153-compositor-proof-interpreter"
                  ProofStatus = CompositorReadinessEnvironmentLimited
                  ParityStatus = CompositorReadinessFallbackGated
                  TimingStatus = CompositorReadinessEnvironmentLimited
                  CompatibilityStatus = CompositorReadinessAccepted
                  RegressionStatus = CompositorReadinessAccepted
                  Evidence =
                    [ evidence "validation-summary.md" CompositorReadinessEnvironmentLimited
                      evidence "proof-set.md" CompositorReadinessEnvironmentLimited
                      evidence "live-proof/unsupported/README.md" CompositorReadinessEnvironmentLimited ]
                  Limitations = [ "zero accepted partial-redraw artifacts" ] }

            let result = CompositorReadiness.validate report

            Expect.isFalse result.Accepted "environment-limited proof cannot accept readiness"
            Expect.equal result.Status CompositorReadinessEnvironmentLimited "environment-limited proof wins"
        }
    ]
