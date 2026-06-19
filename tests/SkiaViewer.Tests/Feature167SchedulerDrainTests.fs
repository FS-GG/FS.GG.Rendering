module Feature167SchedulerDrainTests

open Expecto
open FS.GG.UI.SkiaViewer

[<Tests>]
let tests =
    testList "Feature167 scheduler drain" [
        test "drain clears pending input and reports before/after depth" {
            let _, q1 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerMove "move" Viewer.emptyInputQueue
            let _, q2 = Feature167SchedulerFixtures.enqueue ViewerResponsivenessInputKind.PointerDiscrete "click" q1
            let drain, q3 = Viewer.drainInputQueue 7L "explicit-wake" q2

            Expect.equal drain.BatchId 7L "batch id is carried"
            Expect.equal drain.QueueDepthBeforeDrain 2 "move + discrete were pending"
            Expect.equal drain.QueueDepthAfterDrain 0 "drain is empty after the frame"
            Expect.equal (Viewer.inputQueueDepth q3) 0 "queue was cleared"
        }

        test "dirty state recomposes once when any visible-affecting fact changed" {
            let dirty = Viewer.dirtyState true false false false None [ "input:1" ]
            let clean = Viewer.dirtyState false false false false None []

            Expect.isTrue (Viewer.dirtyStateRequiresRecompose dirty) "product changes require recomposition"
            Expect.isFalse (Viewer.dirtyStateRequiresRecompose clean) "no state change is no-visible-response eligible"
        }
    ]
