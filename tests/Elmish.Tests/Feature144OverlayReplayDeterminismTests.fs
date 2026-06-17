module Feature144OverlayReplayDeterminismTests

open Expecto
open FS.GG.UI.Controls
open Feature144OverlayDispatchFixtures

let private script () =
    [ OpenRequested(surface TransientSurfaceKind.DatePickerCalendar "replay-date")
      KeyRouted(None, "Tab")
      SelectionCompleted("replay-date", "replay-date:2026-06-17", Some "2026-06-17") ]

let private run () =
    script ()
    |> List.fold
        (fun state msg ->
            let next, _ = OverlayState.update msg state
            next)
        (OverlayState.init ())
    |> OverlayState.replayLog

[<Tests>]
let tests =
    testList "Feature144 overlay replay determinism" [
        test "three equivalent runs produce identical logs" {
            let logs = [ run (); run (); run () ]

            Expect.equal logs.[0] logs.[1] "run 1 and 2 match"
            Expect.equal logs.[1] logs.[2] "run 2 and 3 match"
            Expect.equal logs.[0].ProductDispatches.Length 1 "one product dispatch recorded"
        }
    ]
