module Audit_AnimationClock

// AUDIT (feature 006, T004 sanity + T023) — the per-identity animation clock
// (`RetainedRender.advance`/`sampleOnPaint`/`clockActive`).
//   * DETERMINISM + CLAMP (FR-007): replaying an identical injected-delta sequence reproduces identical
//     state; a very-large delta clamps Elapsed to the duration with NO overshoot past the endpoint;
//     `sampleOnPaint` is a pure deterministic function of (clock, ownScene).
//   * `clockActive` gates redraw: TRUE while in flight, settles to FALSE — both directions, proving it
//     actually gates (DISCRIMINATING).

open System
open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private ms (n: float) = TimeSpan.FromMilliseconds n
let private ownScene: Scene list = [ Scene.rectangle (0.0, 0.0, 10.0, 10.0) Colors.black ]

let private freshClock (target: VisualState) : AnimationClock =
    { Anim =
        { Animation.empty with
            Opacity = Some { Start = 0.0; End = 1.0; Duration = RetainedRender.defaultTransitionDuration; Easing = Easing.EaseOut } }
      Elapsed = TimeSpan.Zero
      Target = target
      From = [] }

let private sampledOpacity (clock: AnimationClock) : float =
    match clock.Anim.Opacity with
    | Some tween -> Tween.sample Animation.lerpFloat clock.Elapsed tween
    | None -> 1.0

[<Tests>]
let tests =
    testList "Audit: Animation clock determinism + clamp + redraw gating (FR-007)" [

        // ---- T004 scaffold sanity ----
        test "Audit: AnimationClock scaffold reachability — advance/clockActive + counters (T004)" {
            let c = freshClock Hover
            let _ = RetainedRender.advance (ms 16.0) c
            Expect.isTrue (RetainedRender.clockActive c) "advance/clockActive seams reachable"
            let theme = Theme.light
            let size: Size = { Width = 320; Height = 240 }
            let view: Control<int> = { Kind = "stack"; Key = None; Attributes = []; Children = []; Content = None; Accessibility = None }
            let s = RetainedRender.step theme size (RetainedRender.init theme size view).Retained view
            Expect.isTrue (s.WorkReduction.RepaintedNodeCount >= 0) "WorkReductionRecord counter reachable"
        }

        // ---- DETERMINISM over an injected-delta sequence ----
        test "Audit: replaying an identical injected-delta sequence reproduces identical state (>=1000 cases) (FR-007)" {
            let deltaGen = Gen.choose (0, 40) |> Gen.map (fun n -> ms (float n))
            let deterministic (deltas: TimeSpan list) =
                let c0 = freshClock Hover
                let runA = deltas |> List.fold (fun c d -> RetainedRender.advance d c) c0
                let runB = deltas |> List.fold (fun c d -> RetainedRender.advance d c) c0
                runA = runB && sampledOpacity runA = sampledOpacity runB
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen (Gen.listOf deltaGen)) deterministic)
        }

        test "Audit: a non-positive delta is a no-op (never rewinds)" {
            let c = { freshClock Hover with Elapsed = ms 60.0 }
            Expect.equal (RetainedRender.advance (ms 0.0) c) c "a zero delta leaves the clock unchanged"
            Expect.equal (RetainedRender.advance (ms -25.0) c) c "a negative delta is a no-op (no rewind)"
        }

        // ---- CLAMP: no overshoot past the endpoint ----
        test "Audit: a very-large delta clamps Elapsed to the duration and settles at the endpoint (NO overshoot)" {
            let c = freshClock Hover
            let advanced = RetainedRender.advance (TimeSpan.FromSeconds 10.0) c
            Expect.equal advanced.Elapsed RetainedRender.defaultTransitionDuration "Elapsed clamps to the tween duration"
            Expect.equal (sampledOpacity advanced) 1.0 "the sampled opacity is exactly the End value — no overshoot past the endpoint"
            Expect.isFalse (sampledOpacity advanced > 1.0) "the sample never exceeds the End value"
        }

        // ---- clockActive gates redraw — DISCRIMINATING both directions ----
        test "Audit: clockActive gates redraw — TRUE while in flight, FALSE once settled (DISCRIMINATING)" {
            let started = freshClock Hover
            Expect.isTrue (RetainedRender.clockActive started) "a freshly-started clock is active (requests redraw)"
            let mid = RetainedRender.advance (ms 50.0) started
            Expect.isTrue (RetainedRender.clockActive mid) "a mid-flight clock is still active"
            let settled = RetainedRender.advance (TimeSpan.FromSeconds 10.0) started
            Expect.isFalse (RetainedRender.clockActive settled) "a settled (clamped) clock is inactive — gating stops the redraw"
        }

        // ---- sampleOnPaint determinism ----
        test "Audit: sampleOnPaint is a pure deterministic function of (clock, ownScene)" {
            let active = RetainedRender.advance (ms 50.0) (freshClock Hover)
            let a = RetainedRender.sampleOnPaint active ownScene
            let b = RetainedRender.sampleOnPaint active ownScene
            Expect.equal a b "sampleOnPaint reproduces an identical composite for an identical (clock, ownScene)"
            // discriminating: a clock at a different Elapsed samples a different opacity, so the composite differs.
            let earlier = RetainedRender.advance (ms 10.0) (freshClock Hover)
            Expect.notEqual (sampledOpacity earlier) (sampledOpacity active) "distinct Elapsed ⇒ distinct sampled opacity (the sampler is sensitive, not constant)"
        }
    ]
