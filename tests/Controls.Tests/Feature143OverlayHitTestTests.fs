module Feature143OverlayHitTestTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

[<Tests>]
let tests =
    testList "Feature143 topmost hit routing" [
        test "stack order is stable and topmost-only dismissal blocks lower surfaces" {
            let state =
                run [ OpenRequested(surface TransientSurfaceKind.Menu "bottom" 1)
                      OpenRequested(surface TransientSurfaceKind.ContextMenu "top" 20) ]

            let blocked, effects = OverlayState.update (DismissRequested(Some "bottom", DismissalReason.Escape)) state

            Expect.equal (blocked.OpenSurfaces |> List.map _.Id.SurfaceId) [ "bottom"; "top" ] "lower surface remains open"
            Expect.exists blocked.Diagnostics (fun d -> d.Code = BlockedOverlayDismissal) "blocked lower dismissal diagnostic"
            Expect.contains effects ConsumeInput "blocked dismissal consumes input"
        }

        test "outside pointer routes through the target surface dismissal policy" {
            let state, _ = openOne (surface TransientSurfaceKind.ComboDropdown "combo" 10)

            let decision =
                { Input = "pointer:outside"
                  CandidateLayers = [ "content"; "combo" ]
                  ChosenTarget = Some "content"
                  BlockedByModal = None
                  OutsideOfSurface = Some "combo" }

            let closed, effects = OverlayState.update (PointerRouted decision) state

            Expect.isEmpty closed.OpenSurfaces "outside pointer dismissed top surface"
            Expect.equal (List.head effects) (RecordTopmostHit decision) "hit evidence is first"
            Expect.isTrue ((OverlayState.replayLog closed).HitDecisions.Length = 1) "hit decision recorded"
        }
    ]
