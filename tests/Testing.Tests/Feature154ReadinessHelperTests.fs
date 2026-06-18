module Feature154ReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

let private evidence name status =
    { EvidenceName = name
      EvidencePath = Some($"specs/154-compositor-proof-acceptance/readiness/{name}")
      EvidenceStatus = status
      EvidenceRequired = true
      EvidenceDiagnostics = [] }

[<Tests>]
let tests =
    testList "Feature154 compositor readiness helper" [
        test "accepted proof and parity accept safety while rejected timing stays a visible non-claim" {
            let report =
                { Feature = "154-compositor-proof-acceptance"
                  ProofStatus = CompositorReadinessAccepted
                  ParityStatus = CompositorReadinessAccepted
                  TimingStatus = CompositorReadinessFallbackGated
                  CompatibilityStatus = CompositorReadinessAccepted
                  RegressionStatus = CompositorReadinessAccepted
                  Evidence =
                    [ evidence "validation-summary.md" CompositorReadinessAccepted
                      evidence "proof-set.md" CompositorReadinessAccepted
                      evidence "parity/README.md" CompositorReadinessAccepted ]
                  Limitations = [] }

            let result = CompositorReadiness.validate report

            Expect.isTrue result.Accepted "safety readiness accepts without a performance claim"
            Expect.exists result.Diagnostics (fun item -> item.Contains("timing claim status")) "timing no-claim is visible"
        }

        test "environment-limited proof and fallback parity remain non-accepting" {
            let report =
                { Feature = "154-compositor-proof-acceptance"
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

        test "failed package or missing evidence blocks final readiness" {
            let missing =
                { Feature = "154-compositor-proof-acceptance"
                  ProofStatus = CompositorReadinessAccepted
                  ParityStatus = CompositorReadinessAccepted
                  TimingStatus = CompositorReadinessAccepted
                  CompatibilityStatus = CompositorReadinessAccepted
                  RegressionStatus = CompositorReadinessAccepted
                  Evidence = [ { evidence "validation-summary.md" CompositorReadinessMissingEvidence with EvidencePath = None } ]
                  Limitations = [] }

            let failed =
                { missing with
                    Evidence = [ evidence "regression-validation.md" CompositorReadinessFailed ]
                    RegressionStatus = CompositorReadinessFailed }

            Expect.equal (CompositorReadiness.validate missing).Status CompositorReadinessMissingEvidence "missing evidence"
            Expect.equal (CompositorReadiness.validate failed).Status CompositorReadinessFailed "failed regression"
        }
    ]
