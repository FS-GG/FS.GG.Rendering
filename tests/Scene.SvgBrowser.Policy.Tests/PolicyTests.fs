module SceneSvgBrowserPolicyTests

open Expecto
open FS.GG.UI.Scene.SvgBrowser

let config = { SuspensionMilliseconds = 1000.0 }
let step observation state = SvgSessionPolicy.update config observation state

[<Tests>]
let tests =
    testList "SVG session browser policy" [
        test "variable presentation cadence converts elapsed once and coalesces projection demand" {
            let initial = SvgSessionPolicy.initialize config
            let first, firstEffects = step (SvgSessionPolicyObservation.Frame 10.0) initial
            Expect.isEmpty firstEffects "the first timestamp establishes the browser boundary"
            let second, secondEffects = step (SvgSessionPolicyObservation.Frame 26.667) first
            Expect.equal secondEffects [ SvgSessionPolicyEffect.AdvanceElapsed 16667UL; SvgSessionPolicyEffect.RequestProjection 0UL ] "elapsed converts to integer microseconds and demands one projection"
            let third, thirdEffects = step (SvgSessionPolicyObservation.Frame 43.333) second
            Expect.equal thirdEffects [ SvgSessionPolicyEffect.AdvanceElapsed 16666UL; SvgSessionPolicyEffect.ProjectionDemandCoalesced 0UL ] "a slow projection consumer cannot grow ownership"
            Expect.isTrue third.ProjectionQueued "one replacement projection is retained"
        }

        test "a completed projection is monotonic and drains one coalesced replacement" {
            let initial = SvgSessionPolicy.initialize config
            let pending, _ = initial |> step SvgSessionPolicyObservation.DemandProjection
            let queued, _ = pending |> step SvgSessionPolicyObservation.DemandProjection
            let accepted, effects = queued |> step (SvgSessionPolicyObservation.CompleteProjection(0UL, 7UL))
            Expect.equal effects [ SvgSessionPolicyEffect.ApplyProjection 7UL; SvgSessionPolicyEffect.RequestProjection 0UL ] "the accepted projection is applied before its replacement is requested"
            let unchanged, stale = accepted |> step (SvgSessionPolicyObservation.CompleteProjection(0UL, 6UL))
            Expect.equal stale [ SvgSessionPolicyEffect.ProjectionRevisionRejected(7UL, 6UL) ] "older retained state is explicit"
            Expect.equal unchanged accepted "a stale revision changes no state"
        }

        test "suspension invalidates requests and asks authority to recover" {
            let initial = SvgSessionPolicy.initialize config
            let timed, _ = initial |> step (SvgSessionPolicyObservation.Frame 1.0)
            let suspended, effects = timed |> step (SvgSessionPolicyObservation.Frame 5001.0)
            Expect.equal suspended.Status SvgSessionStatus.Recovering "the host stops ordinary advancement"
            Expect.equal suspended.Generation 1UL "late replies belong to the old generation"
            Expect.equal effects [ SvgSessionPolicyEffect.CancelGeneration 0UL; SvgSessionPolicyEffect.PauseAuthority; SvgSessionPolicyEffect.RequestRecovery 1UL ] "recovery is explicit"
            Expect.equal (suspended |> step SvgSessionPolicyObservation.Suspend) (suspended, []) "duplicate lifecycle signals do not replace recovery twice"
            let unchanged, stale = suspended |> step (SvgSessionPolicyObservation.CompleteProjection(0UL, 99UL))
            Expect.equal stale [ SvgSessionPolicyEffect.ProjectionRejected(1UL, 0UL, 99UL) ] "the stale worker reply is refused"
            Expect.equal unchanged suspended "stale completion is inert"
        }

        test "pause, step, reset, resume, replacement and disposal keep bounded ownership" {
            let initial = SvgSessionPolicy.initialize config
            let paused, pause = initial |> step SvgSessionPolicyObservation.Pause
            Expect.equal pause [ SvgSessionPolicyEffect.CancelGeneration 0UL; SvgSessionPolicyEffect.PauseAuthority ] "pause invalidates current work"
            let stepped, once = paused |> step SvgSessionPolicyObservation.StepOnce
            Expect.equal once [ SvgSessionPolicyEffect.StepAuthority; SvgSessionPolicyEffect.RequestProjection 1UL ] "single-step presents while paused"
            let reset, resetEffects = stepped |> step SvgSessionPolicyObservation.Reset
            Expect.equal resetEffects [ SvgSessionPolicyEffect.CancelGeneration 1UL; SvgSessionPolicyEffect.ResetAuthority; SvgSessionPolicyEffect.RequestProjection 2UL ] "reset replaces requests"
            let running, resumed = reset |> step SvgSessionPolicyObservation.Resume
            Expect.equal resumed [ SvgSessionPolicyEffect.ResumeAuthority ] "resume restarts the clock boundary"
            let replaced, replacement = running |> step SvgSessionPolicyObservation.Replace
            Expect.equal replacement [ SvgSessionPolicyEffect.CancelGeneration 2UL; SvgSessionPolicyEffect.GenerationReplaced 3UL ] "replacement owns a fresh generation"
            let disposed, disposal = replaced |> step SvgSessionPolicyObservation.Dispose
            Expect.equal disposal [ SvgSessionPolicyEffect.CancelGeneration 3UL; SvgSessionPolicyEffect.Disposed ] "disposal releases the last generation"
            Expect.equal disposed.Status SvgSessionStatus.Disposed "disposed is terminal"
            Expect.isFalse disposed.ProjectionPending "no request remains"
            Expect.equal (disposed |> step SvgSessionPolicyObservation.DemandProjection) (disposed, []) "post-disposal observations are inert"
        }
    ]
