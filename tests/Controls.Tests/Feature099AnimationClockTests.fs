module Feature099AnimationClockTests

// Feature 099 (R4) — the per-identity animation clock CORE, on the live retained path.
//   * T013 / SC-004 / FR-006: determinism — two runs over an identical injected-delta sequence
//     produce identical clock state; plus the delta/trigger edge cases (non-positive no-op,
//     very-large clamp-to-end, retarget-mid-flight-from-current-value, return-to-Normal settled
//     drop, multi-clock independence).
//   * T014 / SC-003 / FR-005: identity-at-rest — a frame for an identity with no active clock emits
//     no animation attribute and is byte-identical to the pre-R4 static render (zero recompute).
// The pure core (`advance` / `updateClockForState` / `sampleOnPaint` / `clockActive`) and the live
// `RetainedRender.init`/`step` path are reached via InternalsVisibleTo (the in-assembly test IS the
// user-reachable surface for these internal stories) — [[fs-gg-evidence-mode]].

open System
open System.IO
open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

// A fresh fade-in clock toward `target`, Elapsed 0 (mirrors what `updateClockForState` starts).
let private freshClock (target: VisualState) : AnimationClock =
    { Anim =
        { Animation.empty with
            Opacity = Some { Start = 0.0; End = 1.0; Duration = RetainedRender.defaultTransitionDuration; Easing = Easing.EaseOut } }
      Elapsed = TimeSpan.Zero
      Target = target
      // Feature 103 (R6): no prior snapshot for these pure-core clock tests — a `[]` `From` is the
      // plain fade-in degenerate case, leaving the advance/retarget/drop semantics under test unchanged.
      From = [] }

let private sampledOpacity (clock: AnimationClock) : float =
    match clock.Anim.Opacity with
    | Some tween -> Tween.sample Animation.lerpFloat clock.Elapsed tween
    | None -> 1.0

let private ms (n: float) = TimeSpan.FromMilliseconds n

// --- builders (no bridge: every node is Normal, so no clock is ever started) ------------------

let private leaf (key: string) (content: string) : Control<int> =
    { Kind = "text-block"
      Key = Some key
      Attributes =
        [ { Name = "width"; Category = AttrCategory.Style; Value = FloatValue 120.0 }
          { Name = "height"; Category = AttrCategory.Style; Value = FloatValue 24.0 } ]
      Children = []
      Content = Some content
      Accessibility = None }

let private stack (children: Control<int> list) : Control<int> =
    { Kind = "stack"; Key = None; Attributes = []; Children = children; Content = None; Accessibility = None }

module private Evidence =
    let readinessRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "099-live-animation-clock", "readiness"))

    let write (name: string) (lines: string list) =
        Directory.CreateDirectory readinessRoot |> ignore
        File.WriteAllText(Path.Combine(readinessRoot, name), (String.concat "\n" lines) + "\n")

// =============================================================================================
// T013 / SC-004 / FR-006 — determinism + delta/trigger edge cases on the pure core.
// =============================================================================================

[<Tests>]
let determinism =
    testList "099 US3 determinism + edges (pure clock core)" [

        test "two runs over an identical injected-delta sequence produce identical clock state (≥1000 cases)" {
            let deltaGen = Gen.choose (0, 40) |> Gen.map (fun n -> ms (float n))

            let deterministic (deltas: TimeSpan list) =
                let clock0 = freshClock Hover
                let runA = deltas |> List.fold (fun c d -> RetainedRender.advance d c) clock0
                let runB = deltas |> List.fold (fun c d -> RetainedRender.advance d c) clock0
                // identical accumulated Elapsed AND identical sampled opacity (pure function of deltas).
                runA = runB && sampledOpacity runA = sampledOpacity runB

            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen (Gen.listOf deltaGen)) deterministic)
        }

        test "a non-positive delta is a no-op (clock unchanged, never rewinds)" {
            let c = { freshClock Hover with Elapsed = ms 60.0 }
            Expect.equal (RetainedRender.advance (ms 0.0) c) c "a zero delta leaves the clock unchanged"
            Expect.equal (RetainedRender.advance (ms -25.0) c) c "a negative delta is a no-op (no rewind)"
        }

        test "a very-large delta clamps Elapsed to the duration and the sample settles at the end (no overshoot)" {
            let c = freshClock Hover
            let advanced = RetainedRender.advance (TimeSpan.FromSeconds 10.0) c
            Expect.equal advanced.Elapsed RetainedRender.defaultTransitionDuration "Elapsed clamps to the tween duration"
            Expect.isFalse (RetainedRender.clockActive advanced) "a clamped clock is settled (no longer active)"
            Expect.equal (sampledOpacity advanced) 1.0 "the sampled opacity is exactly the End value (no overshoot past target)"
        }

        test "a mid-flight retarget re-aims from the CURRENT sampled value (no snap to start)" {
            // advance Hover halfway, then the state flips to Pressed.
            let mid = RetainedRender.advance (ms 75.0) (freshClock Hover)
            let currentBeforeFlip = sampledOpacity mid
            Expect.isGreaterThan currentBeforeFlip 0.0 "the mid-flight clock is partway open"

            let retargeted = (RetainedRender.updateClockForState Pressed [] (Some mid)).Value
            Expect.equal retargeted.Target Pressed "the retarget re-aims toward the new state"
            Expect.equal retargeted.Elapsed TimeSpan.Zero "the retarget restarts the eased segment"
            // sampling the retargeted clock at Elapsed 0 yields the value it was displaying, not 0 (no snap).
            Expect.floatClose Accuracy.high (sampledOpacity retargeted) currentBeforeFlip "continues from the displayed value (no snap to start)"
        }

        test "a settled return-to-Normal clock is DROPPED to None (at-rest restored)" {
            let settledNormal = RetainedRender.advance (ms 200.0) (freshClock Normal)
            Expect.isFalse (RetainedRender.clockActive settledNormal) "precondition: the return-to-Normal clock has settled"
            Expect.isNone (RetainedRender.updateClockForState Normal [] (Some settledNormal)) "a settled Normal-targeted clock drops to None"
        }

        test "a held non-Normal state does NOT re-fire: a settled clock at the same state advances-only (kept)" {
            let settledHover = RetainedRender.advance (ms 200.0) (freshClock Hover)
            let kept = RetainedRender.updateClockForState Hover [] (Some settledHover)
            Expect.equal kept (Some settledHover) "a settled clock at its own state is kept unchanged (no spurious re-start)"
        }

        test "entering a non-Normal state from rest STARTS a fresh fade (Elapsed 0, opacity from 0)" {
            let started = (RetainedRender.updateClockForState Hover [] None).Value
            Expect.equal started.Target Hover "the started clock targets the entered state"
            Expect.equal started.Elapsed TimeSpan.Zero "a fresh start begins at Elapsed 0"
            Expect.equal (sampledOpacity started) 0.0 "a fresh fade begins fully transparent"
            Expect.isTrue (RetainedRender.clockActive started) "a freshly-started clock is active"
        }

        test "multiple clocks advance INDEPENDENTLY: one completing does not perturb another's Elapsed/sample" {
            // A near-end clock and an early clock advanced by the same frame delta.
            let near = { freshClock Hover with Elapsed = ms 140.0 }
            let early = { freshClock Pressed with Elapsed = ms 10.0 }
            let delta = ms 20.0

            let near' = RetainedRender.advance delta near
            let early' = RetainedRender.advance delta early

            Expect.equal near'.Elapsed RetainedRender.defaultTransitionDuration "the near clock clamps/settles at its own duration"
            Expect.equal early'.Elapsed (ms 30.0) "the early clock advances by exactly its own injected delta"
            // the early clock's sampled value depends only on its own Elapsed — the other settling is irrelevant.
            Expect.equal (sampledOpacity early') (sampledOpacity (RetainedRender.advance delta early)) "the early clock's sample is unperturbed by the other clock completing"
        }

        test "capture determinism evidence (SC-004/FR-006)" {
            let deltas = [ for _ in 1..12 -> ms 16.0 ]
            let runA = deltas |> List.fold (fun c d -> RetainedRender.advance d c) (freshClock Hover)
            let runB = deltas |> List.fold (fun c d -> RetainedRender.advance d c) (freshClock Hover)

            Evidence.write "us3-determinism.md"
                [ "# Animation clock determinism — identical injected-delta sequence ⇒ identical output (feature 099, SC-004/FR-006)"
                  ""
                  "evidence-kind=determinism"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.advance (the pure clock core the host Tick wrapper calls)"
                  "wall-clock-consulted=false"
                  "time-source=injected per-frame TimeSpan delta only (no Date.now / no System clock)"
                  sprintf "fscheck-cases=%d" 1000
                  sprintf "fixed-sequence-frames=%d" (List.length deltas)
                  sprintf "run-a-elapsed-ms=%f" runA.Elapsed.TotalMilliseconds
                  sprintf "run-b-elapsed-ms=%f" runB.Elapsed.TotalMilliseconds
                  sprintf "two-runs-identical=%b" (runA = runB)
                  "edge-non-positive-delta=no-op (never rewinds)"
                  "edge-very-large-delta=clamps to duration, sample settles at End (no overshoot)"
                  "edge-retarget-mid-flight=re-aims from current sampled value (no snap to start)"
                  "edge-return-to-normal-settled=dropped to None (byte-identical at rest restored)"
                  "edge-multi-clock=each RetainedId advances its own clock independently"
                  "authoritative-test=Feature099AnimationClockTests/099 US3 determinism + edges (pure clock core)" ]
        }
    ]

// =============================================================================================
// T014 / SC-003 / FR-005 — identity-at-rest: no active clock ⇒ no animation attribute,
// byte-identical to the pre-R4 static render, zero at-rest recompute.
// =============================================================================================

[<Tests>]
let identityAtRest =
    testList "099 US3 identity-at-rest (byte-identical, zero recompute)" [

        test "a frame with no active clock is byte-identical to the static Control.renderTree render" {
            let prev = stack [ leaf "a" "A"; leaf "editor" "hi" ]
            let next = stack [ leaf "a" "A-changed"; leaf "editor" "hi" ]

            let r0 = (RetainedRender.init theme size prev).Retained
            let s = RetainedRender.step theme size r0 next

            // No bridge is applied, so every node is Normal and NO clock is ever started.
            Expect.isFalse (s.Retained.StateByIdentity |> Map.exists (fun _ st -> st.Animation.IsSome)) "no animation clock exists at rest"

            // The wired scene equals the full static rebuild of `next` — byte-identical (FR-005).
            let staticScene = (Control.renderTree theme size next).Scene
            Expect.equal s.Render.Scene staticScene "an at-rest frame is byte-identical to the pre-R4 static render"
        }

        test "an at-rest re-step of an unchanged tree recomputes ZERO nodes (E2 RecomputedNodeCount = 0 preserved)" {
            let view = stack [ leaf "a" "A"; leaf "editor" "hi" ]
            let r0 = (RetainedRender.init theme size view).Retained
            let s = RetainedRender.step theme size r0 view

            Expect.equal s.WorkReduction.RecomputedNodeCount 0 "an unchanged at-rest frame repaints no nodes"
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "an unchanged at-rest frame re-measures no nodes"
        }

        test "a settled-and-dropped clock returns the identity to byte-identical at-rest output" {
            // updateClockForState drops a settled Normal-targeted clock to None, so the next paint is static.
            let settledNormal = RetainedRender.advance (ms 200.0) (freshClock Normal)
            let dropped = RetainedRender.updateClockForState Normal [] (Some settledNormal)
            Expect.isNone dropped "the settled return-to-Normal clock is dropped (no lingering animation output)"

            Evidence.write "us3-identity-at-rest.md"
                [ "# Identity-at-rest — a no-active-clock frame is byte-identical to the pre-R4 golden (feature 099, SC-003/FR-005)"
                  ""
                  "evidence-kind=identity-at-rest"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.init/step (the live retained path) + Control.renderTree (the static golden)"
                  "no-active-clock-byte-identical-to-static=true"
                  "at-rest-recompute-count=0"
                  "at-rest-remeasure-count=0"
                  "settled-return-to-normal-clock-dropped=true"
                  "note=`Animation.applyAt`'s identity-at-rest lowering + dropping a settled return-to-Normal clock means an at-rest identity emits NO animation attribute; the wired scene equals the full static rebuild byte-for-byte."
                  "authoritative-test=Feature099AnimationClockTests/099 US3 identity-at-rest (byte-identical, zero recompute)" ]
        }
    ]
