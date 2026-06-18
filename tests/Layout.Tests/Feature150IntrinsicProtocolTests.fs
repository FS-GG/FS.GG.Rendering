module Feature150IntrinsicProtocolTests

open Expecto
open FS.GG.UI.Layout

[<Tests>]
let tests =
    testList "Feature150IntrinsicProtocol" [
        test "constraints normalize invalid minimums and expose deterministic identity" {
            let c = Layout.constraints Viewport -1.0 (Some 120.0) -5.0 (Some 180.0)

            Expect.equal c.MinWidth 0.0 "negative min width normalizes"
            Expect.equal c.MinHeight 0.0 "negative min height normalizes"
            Expect.equal c.WidthMode AtMost "bounded max is at-most"
            Expect.stringContains c.NormalizedIdentity "Viewport" "identity names source"
        }

        test "measureProtocol reports measured size and child placement evidence" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let c = Layout.constraintsFromAvailable Viewport Feature150Fixtures.available
            let measured = Layout.measureProtocol c root

            Expect.equal measured.ParticipantId "root" "root measured"
            Expect.isTrue (measured.MeasuredSize.MeasuredWidth >= 0.0 && measured.MeasuredSize.MeasuredHeight >= 0.0) "measured size is bounded"
            Expect.equal measured.ChildPlacements.Length 2 "direct children have placement records"
            Expect.isNonEmpty measured.CacheEntryId "cache candidate id is recorded"
        }

        test "explicit intrinsic query returns accepted natural height evidence" {
            let root = Feature150Fixtures.intrinsicColumn ()
            let inputKey = Layout.layoutInputKey root
            let query = Layout.intrinsicQuery root.Id IntrinsicMaxHeight (Some 120.0) inputKey DiagnosticProbe
            let result = Layout.evaluateIntrinsic query root

            Expect.isTrue result.Accepted "intrinsic result accepted"
            Expect.isTrue (result.Size > 0.0) "natural size is positive"
            Expect.equal result.QueryIdentity query.QueryIdentity "query identity round-trips"
        }
    ]
