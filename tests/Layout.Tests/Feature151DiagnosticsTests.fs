module Feature151DiagnosticsTests

open System
open Expecto
open FS.GG.UI.Layout

let private expectDiagnostic code result message =
    let codes = Feature151CorpusFixtures.diagnosticCodes result
    Expect.isTrue (Set.contains code codes) message

[<Tests>]
let tests =
    testList "Feature151Diagnostics" [
        test "invalid available space fails closed with finite fallback bounds" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "invalid-available")
            let result = Feature151CorpusFixtures.resultOf item

            expectDiagnostic InvalidAvailableSpace result "invalid available diagnostic"
            for nodeId in item.RequiredNodeIds do
                Expect.isTrue (Feature151CorpusFixtures.finiteBounds (Feature151CorpusFixtures.boundsOf nodeId result)) $"{nodeId} fallback finite"
        }

        test "contradictory min and max size is diagnostic rather than misleading" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "contradictory-size")
            let result = Feature151CorpusFixtures.resultOf item

            expectDiagnostic UnsatisfiedConstraint result "unsatisfied constraint diagnostic"
            Expect.isTrue (Feature151CorpusFixtures.finiteBounds (Feature151CorpusFixtures.boundsOf "contradictory-size" result)) "fallback bounds"
        }

        test "duplicate participant ids are visible in evaluate and measureProtocol evidence" {
            let item = Feature151CorpusFixtures.allCases |> List.find (fun item -> item.CaseId = "duplicate-node")
            let result = Feature151CorpusFixtures.resultOf item
            let measured = Layout.measureProtocol (Feature151CorpusFixtures.constraintsFor item) item.Root

            expectDiagnostic DuplicateLayoutNodeId result "duplicate node diagnostic"
            Expect.exists measured.Diagnostics (fun diagnostic -> diagnostic.Code = DuplicateMeasurement) "duplicate measurement diagnostic"
        }

        test "unsupported intrinsic query rejects participant mismatch" {
            let root = Feature151CorpusFixtures.dynamicContent 24.0
            let query = Layout.intrinsicQuery "other" IntrinsicMaxHeight (Some 120.0) (Layout.layoutInputKey root) DiagnosticProbe
            let result = Layout.evaluateIntrinsic query root

            Expect.isFalse result.Accepted "participant mismatch is rejected"
            Expect.exists result.Diagnostics (fun diagnostic -> diagnostic.Code = UnsupportedIntrinsicQuery) "unsupported diagnostic"
        }

        test "diagnostic fallback content extent remains finite and explicit" {
            let extent = Layout.contentExtent Double.NaN -1.0 None

            Expect.equal extent.ExtentSource EmptyContent "empty fallback source"
            Expect.equal extent.ContentWidth 0.0 "invalid viewport width normalizes"
            Expect.equal extent.ContentHeight 0.0 "invalid viewport height normalizes"
            Expect.isEmpty extent.Diagnostics "empty content has no intrinsic diagnostics"
        }
    ]
