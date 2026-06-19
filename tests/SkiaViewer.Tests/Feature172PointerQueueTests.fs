module Feature172PointerQueueTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature172 pointer queue responsiveness" [
        test "discrete pointer input drains before a queued non-input tick" {
            let _, q1 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.Tick "tick" Viewer.emptyInputQueue
            let _, q2 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerDiscrete "click" q1
            let drain, _ = Viewer.drainInputQueue 172L "pointer-priority" q2

            let kinds = drain.DiscreteInputs |> List.map _.InputKind

            Expect.equal kinds [ ViewerResponsivenessInputKind.PointerDiscrete; ViewerResponsivenessInputKind.Tick ] "pointer click precedes background tick"
            Expect.equal drain.QueueDepthBeforeDrain 2 "both queued inputs are counted before drain"
            Expect.equal drain.QueueDepthAfterDrain 0 "drain clears pending work"
        }

        test "coalesced pointer move keeps latest sample and records skipped movement" {
            let _, q1 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerMove "move:1" Viewer.emptyInputQueue
            let latest, q2 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerMove "move:2" q1
            let drain, _ = Viewer.drainInputQueue 173L "move-coalescing" q2

            Expect.equal (drain.CoalescedPointer |> Option.map _.SequenceId) (Some latest.SequenceId) "latest pointer move wins"
            Expect.equal drain.CoalescedMovementCount 1 "one earlier move was coalesced"
            Expect.equal drain.QueueDepthBeforeDrain 1 "coalesced moves occupy one queue slot"
        }

        test "first presented-frame latency records carry drain depth and total timing" {
            let record =
                { Feature167SchedulerFixtures.latency 1 ViewerResponsivenessInputKind.PointerDiscrete 18.0 with
                    QueueDepthAtReceipt = 3
                    QueueDepthAtDrain = 1
                    PresentedFrameId = Some 42L }

            let json = Viewer.latencyRecordToJsonLine record

            Expect.stringContains json "\"queueDepthAtReceipt\":3" "receipt depth is serialized"
            Expect.stringContains json "\"queueDepthAtDrain\":1" "drain depth is serialized"
            Expect.stringContains json "\"presentedFrameId\":42" "first presented frame id is serialized"
            Expect.stringContains json "\"totalInputToVisibleMs\":18" "input-to-visible timing is serialized"
        }
    ]
