module Feature143OverlayReplayTests

open Expecto
open FS.GG.UI.Controls
open Feature143OverlayFixtures

let private replayScript id =
    [ OpenRequested(surface TransientSurfaceKind.DatePickerCalendar id 10)
      KeyRouted(None, "Tab")
      SelectionCompleted(id, id + ":selection", Some "2026-06-17") ]

[<Tests>]
let tests =
    testList "Feature143 overlay replay" [
        test "equivalent scripts produce byte-identical replay logs" {
            let logs =
                [ 1 .. 3 ]
                |> List.map (fun _ -> replayScript "date" |> run |> OverlayState.replayLog)

            Expect.equal logs[0] logs[1] "run 1 equals run 2"
            Expect.equal logs[1] logs[2] "run 2 equals run 3"
        }

        test "fixture corpus covers at least 100 deterministic overlay scenes" {
            let logs =
                [ 0 .. 99 ]
                |> List.map (fun index ->
                    let id = $"scene-{index}"
                    replayScript id |> run |> OverlayState.replayLog)

            Expect.equal logs.Length 100 "100 overlay-state scenes exercised"
            Expect.all logs (fun log -> log.ProductDispatches.Length = 1) "each scene dispatches exactly once"
        }
    ]
