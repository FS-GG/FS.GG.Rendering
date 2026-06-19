module Feature167InputQueueTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature167 input queue" [
        test "discrete inputs preserve receipt order and sequence ids" {
            let a, q1 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.KeyDown "Enter" Viewer.emptyInputQueue
            let b, q2 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerDiscrete "click" q1
            let drain, _ = Viewer.drainInputQueue 1L "input" q2

            Expect.equal a.SequenceId 1L "first sequence id"
            Expect.equal b.SequenceId 2L "second sequence id"
            Expect.equal (drain.DiscreteInputs |> List.map _.Payload) [ "Enter"; "click" ] "discrete order is stable"
        }

        test "continuous pointer moves coalesce without removing discrete input" {
            let _, q1 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerMove "move-1" Viewer.emptyInputQueue
            let latest, q2 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerMove "move-2" q1
            let _, q3 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.KeyDown "Enter" q2
            let drain, _ = Viewer.drainInputQueue 1L "input" q3

            Expect.equal drain.CoalescedPointer.Value.SequenceId latest.SequenceId "latest move wins"
            Expect.equal drain.CoalescedMovementCount 1 "one earlier move was coalesced"
            Expect.equal (drain.DiscreteInputs |> List.map _.Payload) [ "Enter" ] "discrete key remains queued"
        }
    ]
