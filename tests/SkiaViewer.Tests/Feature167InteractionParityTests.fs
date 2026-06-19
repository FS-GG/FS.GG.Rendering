module Feature167InteractionParityTests

open System
open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature167 interaction parity" [
        test "ordered discrete input replay reaches the same final state as immediate dispatch" {
            let receivedAt = DateTimeOffset(2026, 6, 19, 8, 0, 0, TimeSpan.Zero)

            let _, q1 = Viewer.enqueueInput receivedAt ViewerResponsivenessInputKind.KeyDown "A" Viewer.emptyInputQueue
            let _, q2 = Viewer.enqueueInput (receivedAt.AddMilliseconds 1.0) ViewerResponsivenessInputKind.PointerDiscrete "B" q1
            let _, q3 = Viewer.enqueueInput (receivedAt.AddMilliseconds 2.0) ViewerResponsivenessInputKind.KeyUp "C" q2
            let drain, _ = Viewer.drainInputQueue 1L "parity" q3

            let queuedFinal =
                drain.DiscreteInputs
                |> List.fold (fun state envelope -> state + envelope.Payload) ""

            Expect.equal queuedFinal "ABC" "discrete queue replay preserves product-visible final-state order"
        }
    ]
