module Feature150ReadinessHelperTests

open Expecto
open FS.GG.UI.Testing

let private acceptedEvidence name =
    { Name = name
      Path = Some($"specs/150-intrinsic-layout-protocol/readiness/{name}.md")
      Status = LayoutReadinessAccepted
      Required = true
      Diagnostics = [] }

let private acceptedReport =
    { Feature = "150-intrinsic-layout-protocol"
      ContractStatus = LayoutReadinessAccepted
      ScrollViewerStatus = LayoutReadinessAccepted
      IntrinsicStatus = LayoutReadinessAccepted
      ParityStatus = LayoutReadinessAccepted
      CompatibilityStatus = LayoutReadinessAccepted
      DiagnosticsStatus = LayoutReadinessAccepted
      Evidence =
        [ acceptedEvidence "validation-summary"
          acceptedEvidence "compatibility-ledger"
          acceptedEvidence "scrollviewer-validation"
          acceptedEvidence "intrinsic-cache-validation"
          acceptedEvidence "full-incremental-parity" ]
      CompatibilityDeltas =
        [ { Surface = "FS.GG.UI.Layout"
            Change = "Feature150 intrinsic protocol records"
            Migration = Some "Existing evaluate/evaluateIncremental signatures remain source-compatible."
            Intentional = true } ]
      Limitations = [] }

[<Tests>]
let tests =
    testList "Feature150ReadinessHelper" [
        test "accepted report validates when required evidence is present" {
            let result = LayoutReadiness.validate acceptedReport

            Expect.isTrue result.Accepted "accepted readiness"
            Expect.equal result.Status LayoutReadinessAccepted "status"
            Expect.isEmpty result.Diagnostics "no diagnostics"
        }

        test "missing evidence and compatibility blocks are explicit" {
            let report =
                { acceptedReport with
                    CompatibilityStatus = LayoutReadinessCompatibilityBlocked
                    Evidence = [ { acceptedEvidence "validation-summary" with Path = None; Status = LayoutReadinessMissingEvidence } ]
                    CompatibilityDeltas = [ { Surface = "FS.GG.UI.Controls"; Change = "undocumented"; Migration = None; Intentional = false } ] }

            let result = LayoutReadiness.validate report

            Expect.isFalse result.Accepted "not accepted"
            Expect.equal result.Status LayoutReadinessMissingEvidence "missing evidence wins"
            Expect.contains result.MissingEvidence "validation-summary" "missing evidence named"
            Expect.exists result.Diagnostics (fun item -> item.Contains("unintentional layout compatibility delta")) "compatibility diagnostic"
        }
    ]

