module Feature151ReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

[<Tests>]
let tests =
    testList "Feature151Readiness" [
        test "status vocabulary covers every P8 readiness token" {
            [ LayoutReadinessAccepted, "accepted"
              LayoutReadinessIncomplete, "incomplete"
              LayoutReadinessFailed, "failed"
              LayoutReadinessSkipped, "skipped"
              LayoutReadinessEnvironmentLimited, "environment-limited"
              LayoutReadinessSyntheticOnly, "synthetic-only"
              LayoutReadinessCompatibilityBlocked, "compatibility-blocked"
              LayoutReadinessMissingEvidence, "missing-evidence" ]
            |> List.iter (fun (status, token) -> Expect.equal (LayoutReadiness.statusText status) token token)
        }

        test "accepted Feature151 readiness report aggregates without blockers" {
            let result = LayoutReadiness.validate Feature151ReadinessFixtures.acceptedReport

            Expect.isTrue result.Accepted "accepted"
            Expect.equal result.Status LayoutReadinessAccepted "status"
            Expect.isEmpty result.MissingEvidence "missing evidence"
            Expect.isEmpty result.BlockingLimitations "blocking limitations"
        }

        test "missing evidence, blocked compatibility, and blocking limitations stay visible" {
            let report =
                { Feature151ReadinessFixtures.acceptedReport with
                    CompatibilityStatus = LayoutReadinessCompatibilityBlocked
                    Evidence = [ { Feature151ReadinessFixtures.evidence "validation-summary.md" with Path = None; Status = LayoutReadinessMissingEvidence } ]
                    CompatibilityDeltas =
                        [ { Surface = "FS.GG.UI.Controls"
                            Change = "undocumented behavior"
                            Migration = None
                            Intentional = false } ]
                    Limitations = [ "blocking: package validation missing" ] }

            let result = LayoutReadiness.validate report

            Expect.isFalse result.Accepted "not accepted"
            Expect.equal result.Status LayoutReadinessMissingEvidence "missing evidence wins"
            Expect.contains result.MissingEvidence "validation-summary.md" "missing evidence named"
            Expect.contains result.BlockingLimitations "blocking: package validation missing" "blocking limitation named"
            Expect.exists result.Diagnostics (fun item -> item.Contains("unintentional layout compatibility delta")) "compatibility diagnostic"
        }

        test "readiness file discovery validates the Feature151 package shape" {
            let result =
                ReadinessFileDiscovery.validate
                    { ReadinessDirectory = Feature151ReadinessFixtures.readinessDirectory
                      RequiredFiles = Feature151ReadinessFixtures.requiredFiles
                      ExistingFiles = Feature151ReadinessFixtures.existingReadinessFiles () }

            Expect.isTrue result.Complete (String.concat "; " result.Diagnostics)
            Expect.isEmpty result.MissingFiles "no missing readiness files"
        }
    ]
