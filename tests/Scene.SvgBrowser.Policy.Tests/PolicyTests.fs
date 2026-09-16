module SceneSvgBrowserPolicyTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Scene.SvgBrowser

let config = { SuspensionMilliseconds = 1000.0 }

let step observation state =
    SvgSessionPolicy.update config observation state

let private ms (value: float) = TimeSpan.FromMilliseconds value

let private animationClip () =
    {
        Id = "pulse"
        Duration = ms 100.0
        Tracks =
            [
                Opacity,
                [
                    {
                        Time = TimeSpan.Zero
                        Value = ScalarValue 0.0
                        EasingToNext = Linear
                    }
                    {
                        Time = ms 100.0
                        Value = ScalarValue 1.0
                        EasingToNext = Linear
                    }
                ]
            ]
        Cues =
            [
                {
                    Id = "impact"
                    Time = ms 50.0
                    Payload = "sfx.impact"
                }
            ]
        Loop = Once
    }
    |> AnimationClip.validate
    |> Result.defaultWith (fun issues -> failwithf "%A" issues)

let private request id importance revision =
    {
        Id = id
        AuthorityRevision = revision
        Clip = animationClip ()
        Importance = importance
    }

[<Tests>]
let tests =
    testList
        "SVG session browser policy"
        [
            test "variable presentation cadence converts elapsed once and coalesces projection demand" {
                let initial = SvgSessionPolicy.initialize config
                let first, firstEffects = step (SvgSessionPolicyObservation.Frame 10.0) initial
                Expect.isEmpty firstEffects "the first timestamp establishes the browser boundary"
                let second, secondEffects = step (SvgSessionPolicyObservation.Frame 26.667) first

                Expect.equal
                    secondEffects
                    [
                        SvgSessionPolicyEffect.AdvanceElapsed 16667UL
                        SvgSessionPolicyEffect.RequestProjection 0UL
                    ]
                    "elapsed converts to integer microseconds and demands one projection"

                let third, thirdEffects = step (SvgSessionPolicyObservation.Frame 43.333) second

                Expect.equal
                    thirdEffects
                    [
                        SvgSessionPolicyEffect.AdvanceElapsed 16666UL
                        SvgSessionPolicyEffect.ProjectionDemandCoalesced 0UL
                    ]
                    "a slow projection consumer cannot grow ownership"

                Expect.isTrue third.ProjectionQueued "one replacement projection is retained"
            }

            test "a completed projection is monotonic and drains one coalesced replacement" {
                let initial = SvgSessionPolicy.initialize config
                let pending, _ = initial |> step SvgSessionPolicyObservation.DemandProjection
                let queued, _ = pending |> step SvgSessionPolicyObservation.DemandProjection

                let accepted, effects =
                    queued |> step (SvgSessionPolicyObservation.CompleteProjection(0UL, 7UL))

                Expect.equal
                    effects
                    [
                        SvgSessionPolicyEffect.ApplyProjection 7UL
                        SvgSessionPolicyEffect.RequestProjection 0UL
                    ]
                    "the accepted projection is applied before its replacement is requested"

                let unchanged, stale =
                    accepted |> step (SvgSessionPolicyObservation.CompleteProjection(0UL, 6UL))

                Expect.equal
                    stale
                    [ SvgSessionPolicyEffect.ProjectionRevisionRejected(7UL, 6UL) ]
                    "older retained state is explicit"

                Expect.equal unchanged accepted "a stale revision changes no state"
            }

            test "suspension invalidates requests and asks authority to recover" {
                let initial = SvgSessionPolicy.initialize config
                let timed, _ = initial |> step (SvgSessionPolicyObservation.Frame 1.0)
                let suspended, effects = timed |> step (SvgSessionPolicyObservation.Frame 5001.0)
                Expect.equal suspended.Status SvgSessionStatus.Recovering "the host stops ordinary advancement"
                Expect.equal suspended.Generation 1UL "late replies belong to the old generation"

                Expect.equal
                    effects
                    [
                        SvgSessionPolicyEffect.CancelGeneration 0UL
                        SvgSessionPolicyEffect.PauseAuthority
                        SvgSessionPolicyEffect.RequestRecovery 1UL
                    ]
                    "recovery is explicit"

                Expect.equal
                    (suspended |> step SvgSessionPolicyObservation.Suspend)
                    (suspended, [])
                    "duplicate lifecycle signals do not replace recovery twice"

                let unchanged, stale =
                    suspended |> step (SvgSessionPolicyObservation.CompleteProjection(0UL, 99UL))

                Expect.equal
                    stale
                    [ SvgSessionPolicyEffect.ProjectionRejected(1UL, 0UL, 99UL) ]
                    "the stale worker reply is refused"

                Expect.equal unchanged suspended "stale completion is inert"
            }

            test "pause, step, reset, resume, replacement and disposal keep bounded ownership" {
                let initial = SvgSessionPolicy.initialize config
                let paused, pause = initial |> step SvgSessionPolicyObservation.Pause

                Expect.equal
                    pause
                    [
                        SvgSessionPolicyEffect.CancelGeneration 0UL
                        SvgSessionPolicyEffect.PauseAuthority
                    ]
                    "pause invalidates current work"

                let stepped, once = paused |> step SvgSessionPolicyObservation.StepOnce

                Expect.equal
                    once
                    [
                        SvgSessionPolicyEffect.StepAuthority
                        SvgSessionPolicyEffect.RequestProjection 1UL
                    ]
                    "single-step presents while paused"

                let reset, resetEffects = stepped |> step SvgSessionPolicyObservation.Reset

                Expect.equal
                    resetEffects
                    [
                        SvgSessionPolicyEffect.CancelGeneration 1UL
                        SvgSessionPolicyEffect.ResetAuthority
                        SvgSessionPolicyEffect.RequestProjection 2UL
                    ]
                    "reset replaces requests"

                let running, resumed = reset |> step SvgSessionPolicyObservation.Resume
                Expect.equal resumed [ SvgSessionPolicyEffect.ResumeAuthority ] "resume restarts the clock boundary"
                let replaced, replacement = running |> step SvgSessionPolicyObservation.Replace

                Expect.equal
                    replacement
                    [
                        SvgSessionPolicyEffect.CancelGeneration 2UL
                        SvgSessionPolicyEffect.GenerationReplaced 3UL
                    ]
                    "replacement owns a fresh generation"

                let disposed, disposal = replaced |> step SvgSessionPolicyObservation.Dispose

                Expect.equal
                    disposal
                    [ SvgSessionPolicyEffect.CancelGeneration 3UL; SvgSessionPolicyEffect.Disposed ]
                    "disposal releases the last generation"

                Expect.equal disposed.Status SvgSessionStatus.Disposed "disposed is terminal"
                Expect.isFalse disposed.ProjectionPending "no request remains"

                Expect.equal
                    (disposed |> step SvgSessionPolicyObservation.DemandProjection)
                    (disposed, [])
                    "post-disposal observations are inert"
            }

            test "animation frame sampling advances presentation revision and emits only crossed live cues" {
                let animationConfig = { MaxDecorativeEffects = 2 }

                let initial =
                    SvgAnimationPolicy.initialize animationConfig 7UL SvgMotionPreference.Full

                let started, startEffects =
                    initial
                    |> SvgAnimationPolicy.update
                        animationConfig
                        (SvgAnimationObservation.Start(request "player" Essential 7UL))

                Expect.equal
                    (SvgAnimationPolicy.observe started)
                    (7UL, 1UL, SvgAnimationStatus.Running, SvgMotionPreference.Full, 1)
                    "start owns one effect outside authority state"

                Expect.isTrue
                    (startEffects
                     |> List.exists (function
                         | SvgAnimationEffect.ScheduleFrame -> true
                         | _ -> false))
                    "an active effect schedules a frame"

                let advanced, effects =
                    started
                    |> SvgAnimationPolicy.update animationConfig (SvgAnimationObservation.Frame(ms 50.0))

                Expect.equal
                    (SvgAnimationPolicy.observe advanced)
                    (7UL, 2UL, SvgAnimationStatus.Running, SvgMotionPreference.Full, 1)
                    "one sample advances only presentation revision"

                Expect.isTrue
                    (effects
                     |> List.exists (function
                         | SvgAnimationEffect.CueBatch("player", 7UL, [ cue ]) when cue.Cue.Id = "impact" -> true
                         | _ -> false))
                    "the crossed cue is delivered once"

                let sought, seekEffects =
                    advanced
                    |> SvgAnimationPolicy.update animationConfig (SvgAnimationObservation.Seek("player", ms 25.0))

                Expect.isFalse
                    (seekEffects
                     |> List.exists (function
                         | SvgAnimationEffect.CueBatch _ -> true
                         | _ -> false))
                    "seek is visually reproducible and silent"

                Expect.equal
                    (SvgAnimationPolicy.observe sought)
                    (7UL, 3UL, SvgAnimationStatus.Running, SvgMotionPreference.Full, 1)
                    "seek has a monotonic presentation revision"
            }

            test "reduced motion settles declared decoration without replaying cues" {
                let animationConfig = { MaxDecorativeEffects = 1 }

                let initial =
                    SvgAnimationPolicy.initialize animationConfig 3UL SvgMotionPreference.Reduced

                let settled, effects =
                    initial
                    |> SvgAnimationPolicy.update
                        animationConfig
                        (SvgAnimationObservation.Start(request "spark" (Decorative SvgReducedMotionBehavior.Settle) 3UL))

                Expect.equal
                    (SvgAnimationPolicy.observe settled)
                    (3UL, 1UL, SvgAnimationStatus.Running, SvgMotionPreference.Reduced, 0)
                    "settled decoration owns no frame"

                Expect.isTrue
                    (effects
                     |> List.exists (function
                         | SvgAnimationEffect.ApplySample(_, _, _, sample) when sample.IsComplete -> true
                         | _ -> false))
                    "the accessible settled value is applied"

                Expect.isFalse
                    (effects
                     |> List.exists (function
                         | SvgAnimationEffect.CueBatch _ -> true
                         | _ -> false))
                    "reduction never replays historical cues"

                Expect.isTrue
                    (effects
                     |> List.exists (function
                         | SvgAnimationEffect.CancelFrame -> true
                         | _ -> false))
                    "no decorative frame remains"
            }

            test "decorative limit, stale authority and lifecycle refusal preserve accepted state" {
                let animationConfig = { MaxDecorativeEffects = 1 }

                let initial =
                    SvgAnimationPolicy.initialize animationConfig 9UL SvgMotionPreference.Full

                let one, _ =
                    initial
                    |> SvgAnimationPolicy.update
                        animationConfig
                        (SvgAnimationObservation.Start(
                            request "one" (Decorative SvgReducedMotionBehavior.Substitute) 9UL
                        ))

                let unchanged, limit =
                    one
                    |> SvgAnimationPolicy.update
                        animationConfig
                        (SvgAnimationObservation.Start(request "two" (Decorative SvgReducedMotionBehavior.Settle) 9UL))

                Expect.equal unchanged one "excess decoration cannot mutate accepted ownership"

                Expect.equal
                    limit
                    [
                        SvgAnimationEffect.EffectRefused(SvgAnimationRefusal.DecorativeLimitReached 1)
                    ]
                    "degradation is deterministic"

                let same, stale =
                    one
                    |> SvgAnimationPolicy.update
                        animationConfig
                        (SvgAnimationObservation.Start(request "old" Essential 8UL))

                Expect.equal same one "stale authority cannot start presentation"

                Expect.equal
                    stale
                    [
                        SvgAnimationEffect.EffectRefused(SvgAnimationRefusal.StaleAuthorityRevision(9UL, 8UL))
                    ]
                    "the revision mismatch is explicit"

                let replaced, replaceEffects =
                    one
                    |> SvgAnimationPolicy.update animationConfig (SvgAnimationObservation.ReplaceAuthority 10UL)

                Expect.equal
                    (SvgAnimationPolicy.observe replaced)
                    (10UL, 1UL, SvgAnimationStatus.Running, SvgMotionPreference.Full, 0)
                    "replacement drops obsolete presentation only"

                Expect.equal replaceEffects [ SvgAnimationEffect.CancelFrame ] "the old frame is cancelled"

                let disposed, _ =
                    replaced
                    |> SvgAnimationPolicy.update animationConfig SvgAnimationObservation.Dispose

                let after, refusal =
                    disposed
                    |> SvgAnimationPolicy.update
                        animationConfig
                        (SvgAnimationObservation.Start(request "late" Essential 10UL))

                Expect.equal after disposed "disposed is terminal"

                Expect.equal
                    refusal
                    [ SvgAnimationEffect.EffectRefused SvgAnimationRefusal.Disposed ]
                    "post-disposal work is refused"
            }
        ]
