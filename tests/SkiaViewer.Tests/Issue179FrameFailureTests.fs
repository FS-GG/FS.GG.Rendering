module Issue179FrameFailureTests

open Expecto
open FS.GG.UI.SkiaViewer.Host

// Issue #179. The windowed dispatch loop discarded the effect interpreter's `Result`
// (`interpretEffect effect |> ignore`), so a failed frame changed nothing: `runEventLoop` called
// `DoRender` again on the next tick, `renderFrame` threw again, and the same diagnostic was
// re-emitted at frame rate, forever, behind a window still showing the last good frame. There was
// no device-lost detection, no bound on the retries, and no teardown.
//
// The fix is a bounded, classified failure policy: `classifyFrameFailure` says what a failed frame
// *means* (from driver facts, not from an exception's prose), and `decideFrameFailure` says what
// the run does about it. The persistent window itself is not drivable headless (the same limitation
// recorded for feature 121's pacing and feature 119's startup coverage), so the loop below is the
// host's own accumulation — reset the streak on a presented frame, increment and decide on a failed
// one — driven over a scripted frame stream. What it proves is termination: a permanently-failing
// frame cannot spin.

// The GL reset codes `glGetGraphicsResetStatus` can return (GL 4.5 / GL_KHR_robustness). Spelled out
// here rather than taken from `GLEnum`, because the facts record deliberately does not depend on Silk.
let private guiltyContextReset = 0x8253u
let private innocentContextReset = 0x8254u
let private unknownContextReset = 0x8255u

let private healthy: GlHost.FrameFailureFacts =
    { GraphicsResetStatus = GlHost.glNoError
      ContextAbandoned = false
      GlContextCurrent = true
      WindowSystemPresent = true }

/// Drive a scripted stream of frame outcomes (`true` = the frame presented) through the host's own
/// streak accumulation — `observeFramePresented` / `observeFrameFailed` are exactly what the live
/// `handleFrameFailure` calls, so this cannot pass while the host spins. Returns the teardown reason
/// and the frame index it stopped on, or `None` if the stream ran to completion still rendering.
let private driveFrameLoop facts (frames: bool list) =
    let tracker = GlHost.newFrameFailureTracker ()
    let mutable stopped = None

    frames
    |> List.iteri (fun index presented ->
        if stopped.IsNone then
            if presented then
                GlHost.observeFramePresented tracker
            else
                match GlHost.observeFrameFailed tracker facts GlHost.transientFrameRetryBudget with
                | GlHost.FrameFailureAction.RetryFrame _ -> ()
                | GlHost.FrameFailureAction.TeardownRun reason -> stopped <- Some(reason, index))

    stopped

let private alwaysFailing count = List.replicate count false

[<Tests>]
let tests =
    testList
        "GL frame-failure policy (issue #179)"
        [ testList
              "classification distinguishes the three causes (Constitution VI)"
              [ test "an abandoned Skia context is a lost device" {
                    Expect.equal
                        (GlHost.classifyFrameFailure { healthy with ContextAbandoned = true })
                        GlHost.FrameFailureKind.DeviceLost
                        "Skia abandons the context when the device is gone"
                }

                test "a driver-reported graphics reset is a lost device, whoever was at fault" {
                    for status in [ guiltyContextReset; innocentContextReset; unknownContextReset ] do
                        Expect.equal
                            (GlHost.classifyFrameFailure { healthy with GraphicsResetStatus = status })
                            GlHost.FrameFailureKind.DeviceLost
                            $"reset code 0x%X{status} means the device was lost"
                }

                test "no reported reset is not, by itself, evidence of a healthy device" {
                    // A context without GL_KHR_robustness always reports glNoError. `ContextAbandoned`
                    // is what still catches the loss — a reset code is a positive signal only.
                    Expect.equal
                        (GlHost.classifyFrameFailure
                            { healthy with
                                GraphicsResetStatus = GlHost.glNoError
                                ContextAbandoned = true })
                        GlHost.FrameFailureKind.DeviceLost
                        "a non-robust context reports no reset; Skia's abandonment still detects the loss"
                }

                test "a context that is no longer current is a lost device" {
                    Expect.equal
                        (GlHost.classifyFrameFailure { healthy with GlContextCurrent = false })
                        GlHost.FrameFailureKind.DeviceLost
                        "the context outlived its surface; no reset needs to be reported"
                }

                test "a vanished window system is not an implementation defect" {
                    Expect.equal
                        (GlHost.classifyFrameFailure { healthy with WindowSystemPresent = false })
                        GlHost.FrameFailureKind.WindowSystemUnavailable
                        "Constitution VI: a missing window system stays distinguishable from a defect"
                }

                test "a failed draw on a healthy device is a transient defect" {
                    Expect.equal
                        (GlHost.classifyFrameFailure healthy)
                        GlHost.FrameFailureKind.TransientDrawFailure
                        "nothing about the device is wrong, so the draw is at fault"
                }

                test "a lost device outranks a vanished window system" {
                    // Undocking a laptop takes both away at once; the device is the more specific cause.
                    Expect.equal
                        (GlHost.classifyFrameFailure
                            { healthy with
                                ContextAbandoned = true
                                WindowSystemPresent = false })
                        GlHost.FrameFailureKind.DeviceLost
                        "an abandoned context is reported as such even with no window system left"
                } ]

          testList
              "the policy bounds every failure"
              [ test "a lost device is terminal on its first frame" {
                    match GlHost.decideFrameFailure GlHost.FrameFailureKind.DeviceLost 1 GlHost.transientFrameRetryBudget with
                    | GlHost.FrameFailureAction.TeardownRun _ -> ()
                    | action -> failtestf "device loss must never be retried, got %A" action
                }

                test "a vanished window system is terminal on its first frame" {
                    match
                        GlHost.decideFrameFailure GlHost.FrameFailureKind.WindowSystemUnavailable 1 GlHost.transientFrameRetryBudget
                    with
                    | GlHost.FrameFailureAction.TeardownRun _ -> ()
                    | action -> failtestf "there is nothing to present to, got %A" action
                }

                test "a transient failure is retried up to the budget, then torn down" {
                    let budget = GlHost.transientFrameRetryBudget

                    for attempt in 1..budget do
                        match GlHost.decideFrameFailure GlHost.FrameFailureKind.TransientDrawFailure attempt budget with
                        | GlHost.FrameFailureAction.RetryFrame reported ->
                            Expect.equal reported attempt "the retry reports the attempt it is on"
                        | action -> failtestf "attempt %i is within budget %i, got %A" attempt budget action

                    match GlHost.decideFrameFailure GlHost.FrameFailureKind.TransientDrawFailure (budget + 1) budget with
                    | GlHost.FrameFailureAction.TeardownRun _ -> ()
                    | action -> failtestf "past the budget the failure is not transient, got %A" action
                }

                test "the retry budget is bounded and positive" {
                    Expect.isGreaterThan GlHost.transientFrameRetryBudget 0 "a zero budget would tear down on one hiccup"
                    Expect.isLessThan GlHost.transientFrameRetryBudget 60 "a budget near a frame's worth of retries is a spin"
                } ]

          testList
              "the loop terminates instead of spinning (the regression)"
              [ test "a permanently failing frame tears the run down within the budget" {
                    // The pre-fix behaviour: 10_000 failing frames, 10_000 identical diagnostics, no exit.
                    match driveFrameLoop healthy (alwaysFailing 10_000) with
                    | None -> failtest "the loop never terminated — this is the unbounded spin issue #179 reports"
                    | Some(_, index) ->
                        Expect.equal
                            index
                            GlHost.transientFrameRetryBudget
                            "teardown lands on the frame after the budget is exhausted"
                }

                test "a lost device tears the run down on the first failed frame" {
                    match driveFrameLoop { healthy with ContextAbandoned = true } (alwaysFailing 10_000) with
                    | None -> failtest "a lost device must not spin"
                    | Some(reason, index) ->
                        Expect.equal index 0 "no frame is retried against a device that is gone"
                        Expect.stringContains reason "device was lost" "the reason names the cause"
                }

                test "a recovered frame clears the streak, so a flaky device keeps rendering" {
                    // fail, present, fail, present, ... never accumulates `budget` consecutive failures.
                    let flaky = List.init 10_000 (fun index -> index % 2 = 1)

                    Expect.isNone (driveFrameLoop healthy flaky) "an intermittent failure recovers rather than tearing down"
                }

                test "a streak that resets one frame short of the budget still recovers" {
                    let budget = GlHost.transientFrameRetryBudget
                    let nearMiss = List.replicate budget false @ [ true ] @ List.replicate budget false

                    Expect.isNone
                        (driveFrameLoop healthy nearMiss)
                        "the budget counts consecutive failures, so a presented frame in between clears it"
                }

                test "the streak resumes after recovery — a device that fails, recovers, then dies is torn down" {
                    let budget = GlHost.transientFrameRetryBudget
                    let frames = List.replicate budget false @ [ true ] @ alwaysFailing (budget + 1)

                    match driveFrameLoop healthy frames with
                    | None -> failtest "the second streak must still be bounded"
                    | Some(_, index) ->
                        Expect.equal index (budget + 1 + budget) "teardown lands after the second streak exhausts the budget"
                } ]

          test "the fatal diagnostic that ends a run is Fatal, staged at the frame render" {
              let fatal = Diagnostics.frameLoopAbandoned "The OpenGL device was lost." (Some "GL_GUILTY_CONTEXT_RESET")

              Expect.equal fatal.Severity DiagnosticSeverity.Fatal "an abandoned frame loop is not a warning"
              Expect.equal fatal.Stage DiagnosticStage.FrameRender "the failure is attributed to the frame it died on"
              Expect.equal fatal.Cause (Some "GL_GUILTY_CONTEXT_RESET") "the underlying detail survives into the fatal diagnostic"
          } ]
