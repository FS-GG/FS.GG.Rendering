module Issue1046LifecycleSeamTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer

let private pointer phase x =
    { Phase = phase
      X = x
      Y = 10.0
      Button = None
      DeltaX = 0.0
      DeltaY = 0.0 }

[<Tests>]
let tests =
    testList "Issue 1046 frame-loop lifecycle seam" [
        test "creation starts with no retained runtime or pending input state" {
            let state = FrameLoopLifecycle.create<obj, obj> ()

            Expect.isNone state.Focused "no focus exists before the first frame"
            Expect.isNone state.Retained "the retained tree is seeded by the first frame"
            Expect.isNone state.PendingMove "no pointer sample is pending"
            Expect.equal state.PointerSampleCount 0 "the first frame starts with an empty input batch"
            Expect.isEmpty state.SurfacedDiagnostics "no diagnostic has been disclosed"
        }

        test "move and discrete boundaries preserve the existing latest-pending accounting" {
            let state = FrameLoopLifecycle.create<obj, obj> ()
            let first = pointer ViewerPointerPhaseKind.Moved 10.0
            let second = pointer ViewerPointerPhaseKind.Moved 20.0

            FrameLoopLifecycle.recordPointerSample state
            Expect.isNone (FrameLoopLifecycle.takePendingMove state) "the first move has no predecessor to flush"
            FrameLoopLifecycle.deferMove first state
            Expect.equal (FrameLoopLifecycle.completeMoveBoundary state) 0 "the first move carries into the next boundary"

            FrameLoopLifecycle.recordPointerSample state
            Expect.equal (FrameLoopLifecycle.takePendingMove state) (Some first) "the prior latest move is flushed"
            FrameLoopLifecycle.deferMove second state
            Expect.equal (FrameLoopLifecycle.completeMoveBoundary state) 1 "one carried move completed"

            FrameLoopLifecycle.recordPointerSample state
            Expect.equal (FrameLoopLifecycle.takePendingMove state) (Some second) "a discrete boundary flushes the pending move first"
            Expect.equal (FrameLoopLifecycle.completeDiscreteBoundary state) 2 "the carried move and discrete sample share one batch"
            Expect.equal state.PointerSampleCount 0 "a discrete boundary drains the batch"
        }

        test "diagnostic disclosure is a one-time lifecycle transition" {
            let state = FrameLoopLifecycle.create<obj, obj> ()

            let diagnostic =
                { ControlId = Some "button"
                  ControlKind = "button"
                  Code = HitTestFailed
                  Severity = ControlDiagnosticSeverity.Warning
                  Message = "synthetic characterization"
                  EvidencePath = None }

            Expect.isTrue (FrameLoopLifecycle.surfaceDiagnosticOnce diagnostic state) "the first observation is surfaced"
            Expect.isFalse (FrameLoopLifecycle.surfaceDiagnosticOnce diagnostic state) "the same observation is suppressed"
            Expect.hasLength state.SurfacedDiagnostics 1 "the disclosure ledger contains one stable key"
        }
    ]
