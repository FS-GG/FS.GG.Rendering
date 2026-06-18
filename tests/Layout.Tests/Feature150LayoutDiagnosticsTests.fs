module Feature150LayoutDiagnosticsTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature150Diagnostics" [
        test "duplicate participant ids produce measurement diagnostics" {
            let child = Feature150Fixtures.measuredLeaf "dup" 20.0 20.0
            let root = Feature150Fixtures.container "root" 100.0 100.0 [ child; child ]
            let constraints = Layout.constraintsFromAvailable Parent (Defaults.availableSpace 100.0 100.0)
            let result = Layout.measureProtocol constraints root

            Expect.exists result.Diagnostics (fun d -> d.Code = DuplicateMeasurement) "duplicate measurement diagnostic"
        }

        test "participant mismatch rejects intrinsic query" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let query = Layout.intrinsicQuery "other" IntrinsicMaxHeight (Some 120.0) "key" DiagnosticProbe
            let result = Layout.evaluateIntrinsic query root

            Expect.isFalse result.Accepted "mismatched query rejected"
            Expect.exists result.Diagnostics (fun d -> d.Code = UnsupportedIntrinsicQuery) "unsupported query diagnostic"
        }
    ]

