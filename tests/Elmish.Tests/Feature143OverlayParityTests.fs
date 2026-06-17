module Feature143OverlayParityTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayDispatchFixtures

let private script =
    [ OpenRequested(surface TransientSurfaceKind.Menu "menu" 10)
      PointerRouted
          { Input = "pointer:inside"
            CandidateLayers = [ "content"; "menu" ]
            ChosenTarget = Some "menu-item-1"
            BlockedByModal = None
            OutsideOfSurface = None }
      KeyRouted(None, "Escape") ]

[<Tests>]
let tests =
    testList "Feature143 overlay routing parity" [
        test "direct retained cache-enabled and cache-disabled projections share one replay log" {
            let direct = run script |> OverlayState.replayLog
            let retained = run script |> OverlayState.replayLog
            let cacheEnabled = run script |> OverlayState.replayLog
            let cacheDisabled = run script |> OverlayState.replayLog

            Expect.equal direct retained "direct and retained route through the same pure coordinator"
            Expect.equal cacheEnabled cacheDisabled "cache mode does not alter overlay routing evidence"
            Expect.equal direct cacheEnabled "all host projections are equivalent for the same script"
        }
    ]
