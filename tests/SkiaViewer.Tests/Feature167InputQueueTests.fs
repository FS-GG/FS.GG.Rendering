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

        test "public frame-paced policy folds a 1000 Hz move stream to 60 updates and keeps press release click lossless" {
            let movesPerFrame = [ for frame in 0 .. 59 -> 1000 / 60 + (if frame < 1000 % 60 then 1 else 0) ]

            let mutable queue = Viewer.emptyInputQueue
            let mutable moveUpdates = 0
            let mutable discrete = []

            for frame, count in List.indexed movesPerFrame do
                for sample in 1 .. count do
                    let _, next =
                        Viewer.enqueueInputWithPointerPolicy
                            ViewerContinuousPointerPolicy.CoalesceLatestPerFrame
                            Feature167SchedulerFixtures.now
                            ViewerResponsivenessInputKind.PointerMove
                            $"move:{frame}:{sample}"
                            queue
                    queue <- next

                if frame = 30 then
                    for payload in [ "press"; "release"; "click" ] do
                        let _, next =
                            Viewer.enqueueInputWithPointerPolicy
                                ViewerContinuousPointerPolicy.CoalesceLatestPerFrame
                                Feature167SchedulerFixtures.now
                                ViewerResponsivenessInputKind.PointerDiscrete
                                payload
                                queue
                        queue <- next

                let drain, next = Viewer.drainInputQueue (int64 frame + 1L) "presented-frame" queue
                queue <- next
                moveUpdates <- moveUpdates + (if drain.CoalescedPointer.IsSome then 1 else 0)
                discrete <- discrete @ (drain.DiscreteInputs |> List.map _.Payload)

            Expect.equal moveUpdates 60 "1000 raw move samples produce at most one product move per presented frame"
            Expect.equal discrete [ "press"; "release"; "click" ] "discrete sequence is delivered once and in order"
        }

        test "immediate policy retains every move sample" {
            let queue =
                [ 1 .. 8 ]
                |> List.fold (fun state sample ->
                    Viewer.enqueueInputWithPointerPolicy
                        ViewerContinuousPointerPolicy.Immediate
                        Feature167SchedulerFixtures.now
                        ViewerResponsivenessInputKind.PointerMove
                        $"move:{sample}"
                        state
                    |> snd) Viewer.emptyInputQueue

            let drain, _ = Viewer.drainInputQueue 1L "immediate" queue
            Expect.equal (drain.DiscreteInputs |> List.map _.Payload) [ for sample in 1 .. 8 -> $"move:{sample}" ] "no move is folded"
            Expect.isNone drain.CoalescedPointer "immediate moves do not occupy the replaceable slot"
        }
    ]
