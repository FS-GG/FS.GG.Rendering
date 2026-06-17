module Feature143OverlayModalHitTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 modal hit routing" [
        test "modal hit decision blocks lower-layer target and records evidence" {
            let opened, _ = openOne (modalSurface "dialog")

            let decision =
                { Input = "pointer:covered-content"
                  CandidateLayers = [ "content"; "dialog" ]
                  ChosenTarget = Some "content-button"
                  BlockedByModal = Some "dialog"
                  OutsideOfSurface = None }

            let blocked, effects = OverlayState.update (PointerRouted decision) opened

            Expect.equal blocked.ActiveSurface (Some "dialog") "modal remains active"
            Expect.exists blocked.Diagnostics (fun d -> d.Code = LowerLayerBlocked) "lower-layer block diagnostic"
            Expect.equal (List.head effects) (RecordTopmostHit decision) "hit evidence preserved"
        }
    ]
