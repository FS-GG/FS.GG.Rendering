module Feature099AnimationSeamTests

// Feature 099 (R4) — the host animation seam on the LIVE retained path. The seam is the exact
// sequence `runInteractiveApp` wires: each frame the wrapped `Tick` ADVANCES every live per-identity
// clock by the injected delta, then `renderRetained` stamps the derived `VisualState`
// (`ControlRuntime.applyRuntimeVisualState`, R1) and `RetainedRender.step` starts/retargets +
// SAMPLES the clock on paint. These tests drive that same sequence directly (the in-assembly test IS
// the user-reachable surface for the internal wired path) — no live Vulkan window, render-only /
// deterministic ([[fs-skia-evidence-mode]]).
//   * T008 / SC-001 / FR-002/FR-003: a hover transition ANIMATES — ≥1 intermediate sampled
//     appearance before the target, converging to exactly the snapped target; a no-seam build snaps.
//   * T011 / SC-002 / FR-004: an in-flight tween SURVIVES a sibling-shifting unrelated re-render and
//     completes from its prior Elapsed (replaces the hand-seeded Feature092 PRECONDITION).
//   * T017 / SC-005 / FR-007: a removed identity's clock is GC'd via the existing liveIds filter.
//   * T019 / SC-006 / FR-010: advancing a clock keeps repaint scoped (no whole-tree repaint/measure).

open System
open System.IO
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private emptyModel = fst (ControlRuntime.init ())
let private hoverModel id = { emptyModel with HoveredControl = Some id }

let private ms (n: float) = TimeSpan.FromMilliseconds n
let private frameDelta = ms 16.0

// The button is keyed so it carries a stable RetainedId across a sibling shift.
let private buttonView () : Control<Msg> =
    Stack.create [ Stack.children [ Button.create [ Button.text "Go" ] |> Control.withKey "go" ] ]

let private bannerThenButton () : Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "banner" ] |> Control.withKey "banner"
                Button.create [ Button.text "Go" ] |> Control.withKey "go" ] ]

let private bridged (m: ControlRuntimeModel) (view: Control<Msg>) : Control<Msg> =
    ControlRuntime.applyRuntimeVisualState m view

// The host Tick wrapper: advance every live per-identity clock by the injected delta.
let private advanceClocks (delta: TimeSpan) (r: RetainedRender<Msg>) : RetainedRender<Msg> =
    { r with
        StateByIdentity =
            r.StateByIdentity
            |> Map.map (fun _ s -> { s with Animation = s.Animation |> Option.map (RetainedRender.advance delta) }) }

// One full host frame: Tick advance, then renderRetained (bridge + step). Mirrors runInteractiveApp.
let private frame (delta: TimeSpan) (model: ControlRuntimeModel) (view: Control<Msg>) (prev: RetainedRender<Msg>) : RetainedRenderStep<Msg> =
    RetainedRender.step theme size (advanceClocks delta prev) (bridged model view)

let private initFrom (model: ControlRuntimeModel) (view: Control<Msg>) : RetainedRender<Msg> =
    (RetainedRender.init theme size (bridged model view)).Retained

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private idOfKey (key: ControlId) (r: RetainedRender<Msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private clockOf (key: ControlId) (r: RetainedRender<Msg>) : AnimationClock option =
    idOfKey key r |> Option.bind (fun id -> Map.tryFind id r.StateByIdentity) |> Option.bind (fun s -> s.Animation)

module private Evidence =
    let readinessRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "099-live-animation-clock", "readiness"))

    let write (name: string) (lines: string list) =
        Directory.CreateDirectory readinessRoot |> ignore
        File.WriteAllText(Path.Combine(readinessRoot, name), (String.concat "\n" lines) + "\n")

// =============================================================================================
// T008 / SC-001 — a hover transition ANIMATES on the live seam (≥1 intermediate before the target).
// =============================================================================================

[<Tests>]
let animatesVsSnaps =
    testList "099 US1 a visual-state transition animates (not snaps) on the live seam" [

        test "hover a button → the appearance eases in across frames and converges to exactly the snapped target" {
            let view = buttonView ()
            let r0 = initFrom emptyModel view // frame 0: Normal, no clock
            let hover = hoverModel "go"

            // Drive consecutive hover frames through the real seam, capturing each painted frame.
            let mutable cur = r0

            let frames =
                [ for _ in 1..16 do
                      let s = frame frameDelta hover view cur
                      cur <- s.Retained
                      yield s.Render.Scene ]

            // The snapped target = the STATIC render a no-seam build would jump straight to.
            let snapTarget = (Control.renderTree theme size (bridged hover view)).Scene

            // A no-seam build would have produced `snapTarget` on the very first hover frame.
            Expect.notEqual frames.[0] snapTarget "the first hover frame is an intermediate (eased) appearance — it does NOT snap to the target"

            let settledIdx = frames |> List.tryFindIndex ((=) snapTarget)
            Expect.isSome settledIdx "SC-001: the transition converges to EXACTLY the snapped target appearance"

            let idx = settledIdx.Value
            Expect.isGreaterThan idx 0 "at least one intermediate frame precedes the settled target (animates, not snaps)"

            let intermediates = frames |> List.take idx
            Expect.isTrue (intermediates |> List.forall (fun f -> f <> snapTarget)) "every pre-settle frame is a distinct intermediate appearance"
            Expect.isGreaterThan (List.length (List.distinct intermediates)) 1 "the intermediate appearances progress (the fade advances frame to frame)"

            Evidence.write "us1-animates-vs-snaps.md"
                [ "# A visual-state transition animates on the live host (feature 099, SC-001)"
                  ""
                  "evidence-kind=animates-vs-snaps"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=ControlRuntime.applyRuntimeVisualState + RetainedRender.advance (Tick) + RetainedRender.step (the real runInteractiveApp seam)"
                  "representative-kind=Button (R1-migrated; hover on the opacity channel)"
                  "default-transition-duration-ms=150"
                  "easing=EaseOut"
                  "injected-delta-ms=16"
                  sprintf "frames-captured=%d" (List.length frames)
                  sprintf "intermediate-frames-before-target=%d" idx
                  sprintf "converges-to-exact-snap-target=%b" (settledIdx.IsSome)
                  sprintf "first-frame-snaps-to-target=%b" (frames.[0] = snapTarget)
                  "no-seam-counterfactual=a build without the seam paints the snapped target on frame 0 (no intermediate) and fails the intermediate-frame assertion"
                  "note=AUTHORITATIVE proof is the captured sampled frame sequence: ≥1 intermediate appearance (structurally distinct from the target) precedes a frame byte-equal to the static snapped target. Structural Scene equality, no pixel encoder ([[fs-skia-evidence-mode]])."
                  "authoritative-test=Feature099AnimationSeamTests/099 US1 a visual-state transition animates (not snaps) on the live seam" ]
        }
    ]

// =============================================================================================
// T011 / SC-002 — an in-flight tween survives a sibling-shifting re-render and completes (real seam).
// =============================================================================================

[<Tests>]
let survival =
    testList "099 US2 an in-flight animation survives an unrelated re-render and completes" [

        test "hover → tick a few frames → sibling-shifting re-render → the SAME identity's clock continues from prior Elapsed and completes" {
            let view = buttonView ()
            let hover = hoverModel "go"

            // start the tween and advance a few frames (mid-flight).
            let r0 = initFrom emptyModel view
            let s1 = frame frameDelta hover view r0 // starts the clock (Elapsed 0)
            let s2 = frame frameDelta hover view s1.Retained
            let s3 = frame frameDelta hover view s2.Retained

            let idBeforeShift = idOfKey "go" s3.Retained
            let elapsedBeforeShift = (clockOf "go" s3.Retained).Value.Elapsed
            Expect.isGreaterThan elapsedBeforeShift TimeSpan.Zero "the clock is mid-flight before the shift"

            // the UNRELATED shift: insert a banner above the button (model unchanged) → the button's
            // positional path moves, but its keyed RetainedId is stable.
            let shifted = frame frameDelta hover (bannerThenButton ()) s3.Retained

            Expect.equal (idOfKey "go" shifted.Retained) idBeforeShift "FR-008: the button keeps its stable RetainedId across the positional shift (no parallel identity scheme)"

            let clockAfterShift = (clockOf "go" shifted.Retained).Value
            // the Tick wrapper advanced it by one more frame; it did NOT reset to 0 at the shift.
            Expect.isGreaterThan clockAfterShift.Elapsed elapsedBeforeShift "SC-002: the clock continued from its prior Elapsed across the shift (not reset, not dropped)"

            // keep ticking the shifted tree until it completes.
            let mutable cur = shifted.Retained

            for _ in 1..14 do
                cur <- (frame frameDelta hover (bannerThenButton ()) cur).Retained

            let completed = clockOf "go" cur |> Option.map RetainedRender.clockActive |> Option.defaultValue false
            Expect.isFalse completed "the clock completes (settles) through the real seam after the shift"

            // same final result as an UN-shifted run: drive the same number of frames without the shift
            // and compare the per-frame Elapsed trajectory — the shift does not perturb the clock.
            let elapsedSeq (shiftAt: int option) =
                let mutable r = initFrom emptyModel view
                [ for i in 1..18 do
                      let v = match shiftAt with Some k when i > k -> bannerThenButton () | _ -> view
                      let s = frame frameDelta hover v r
                      r <- s.Retained
                      yield (clockOf "go" s.Retained |> Option.map (fun c -> c.Elapsed)) ]

            Expect.equal (elapsedSeq (Some 3)) (elapsedSeq None) "FR-004: the shifted run's clock trajectory is identical to the un-shifted run (same final result)"

            Evidence.write "us2-survival.md"
                [ "# An in-flight animation survives an unrelated re-render and completes (feature 099, SC-002/FR-004)"
                  ""
                  "evidence-kind=survival"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.advance (Tick) + RetainedRender.step over the existing RetainedId-keyed StateByIdentity carry (the real seam)"
                  "hand-seeded-clock=false"
                  "replaces=Feature092LiveSurvivalTests hand-seeded startedClock() PRECONDITION"
                  "sequence=hover button -> tick 3 frames (clock mid-flight) -> insert banner above (sibling shift) -> continue ticking to completion"
                  sprintf "identity-stable-across-shift=%b" (idOfKey "go" shifted.Retained = idBeforeShift)
                  sprintf "elapsed-before-shift-ms=%f" elapsedBeforeShift.TotalMilliseconds
                  sprintf "elapsed-after-shift-ms=%f" clockAfterShift.Elapsed.TotalMilliseconds
                  sprintf "clock-continued-not-reset=%b" (clockAfterShift.Elapsed > elapsedBeforeShift)
                  sprintf "shifted-trajectory-equals-unshifted=%b" (elapsedSeq (Some 3) = elapsedSeq None)
                  "note=the clock rides the E2 stable RetainedId map; the sibling shift moves the button's position but not its identity, so the carried clock keeps advancing to completion. No parallel identity scheme (FR-008)."
                  "authoritative-test=Feature099AnimationSeamTests/099 US2 an in-flight animation survives an unrelated re-render and completes" ]
        }
    ]

// =============================================================================================
// T017 / SC-005 — a removed identity's clock is GC'd via the existing liveIds filter.
// =============================================================================================

[<Tests>]
let gc =
    testList "099 US4 a removed identity's animation clock is garbage-collected" [

        test "animate a control, then remove it → its identity (incl. its clock) is absent the next frame" {
            let view = buttonView ()
            let hover = hoverModel "go"

            let r0 = initFrom emptyModel view
            let s1 = frame frameDelta hover view r0
            let s2 = frame frameDelta hover view s1.Retained

            let goId = (idOfKey "go" s2.Retained).Value
            Expect.isTrue (Map.containsKey goId s2.Retained.StateByIdentity) "the button has retained state while animating"
            Expect.isTrue ((clockOf "go" s2.Retained).IsSome) "the button has an active animation clock"

            // re-render with the button GONE entirely.
            let removed = Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "only" ] |> Control.withKey "only" ] ]
            let s3 = frame frameDelta hover removed s2.Retained

            Expect.isFalse (Map.containsKey goId s3.Retained.StateByIdentity) "SC-005/FR-007: the removed identity's state (incl. its clock) is absent the next frame (no leak)"

            Evidence.write "us4-gc.md"
                [ "# A removed identity's animation clock is garbage-collected (feature 099, SC-005/FR-007)"
                  ""
                  "evidence-kind=gc"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.step (the existing liveIds filter; no new GC code)"
                  "sequence=hover button (clock active) -> re-render with the button removed -> inspect next frame's StateByIdentity"
                  "clock-present-while-live=true"
                  sprintf "clock-absent-after-removal=%b" (not (Map.containsKey goId s3.Retained.StateByIdentity))
                  "note=the generalized animation slot rides the same RetainedUiState the liveIds filter already drops for removed identities; matching the existing focus/text GC behavior, the clock leaves with its identity (no parallel identity scheme, no dangling animation state)."
                  "authoritative-test=Feature099AnimationSeamTests/099 US4 a removed identity's animation clock is garbage-collected" ]
        }
    ]

// =============================================================================================
// T019 / SC-006 — advancing a clock keeps repaint scoped (no whole-tree repaint/re-measure).
// =============================================================================================

[<Tests>]
let scopedRepaint =
    testList "099 scoped repaint — animation does not force a whole-tree repaint/re-measure" [

        test "a steady-state hover frame advances the clock (the scene changes) yet recomputes/re-measures ZERO nodes" {
            let view = buttonView ()
            let hover = hoverModel "go"

            let r0 = initFrom emptyModel view
            let s1 = frame frameDelta hover view r0 // first hover frame: starts the clock + repaints the button for the Hover stamp
            let s2 = frame frameDelta hover view s1.Retained // steady-state: Hover stamp unchanged, only the clock advances
            let s3 = frame frameDelta hover view s2.Retained

            // the animation IS progressing (the painted frame changes frame to frame)...
            Expect.notEqual s2.Render.Scene s3.Render.Scene "the animation advances — consecutive frames differ (the clock is sampled)"

            // ...yet a steady-state animating frame forces NO measure/paint recompute of the tree.
            Expect.equal s3.WorkReduction.RecomputedNodeCount 0 "SC-006: advancing the clock repaints no nodes (paint-level overlay, not a whole-tree repaint)"
            Expect.equal s3.WorkReduction.RemeasuredNodeCount 0 "FR-010: advancing the clock re-measures no nodes (R2 incremental measure preserved)"
            Expect.isTrue ((clockOf "go" s3.Retained |> Option.map RetainedRender.clockActive).Value) "the clock is genuinely active during these frames"

            Evidence.write "scoped-repaint.md"
                [ "# Animation repaint stays scoped to the active subtree (feature 099, SC-006/FR-010)"
                  ""
                  "evidence-kind=scoped-repaint"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.step WorkReduction metric on a steady-state animating frame"
                  sprintf "steady-state-recompute-count=%d" s3.WorkReduction.RecomputedNodeCount
                  sprintf "steady-state-remeasure-count=%d" s3.WorkReduction.RemeasuredNodeCount
                  sprintf "frame-changes-while-animating=%b" (s2.Render.Scene <> s3.Render.Scene)
                  "note=animation is a paint-level overlay applied to cached STATIC fragments at scene assembly; a structurally-unchanged animating frame takes the Keep fast path (zero re-measure, zero re-paint) while still sampling the clock, so one active animation never invalidates the at-rest fast path for the rest of the tree."
                  "authoritative-test=Feature099AnimationSeamTests/099 scoped repaint — animation does not force a whole-tree repaint/re-measure" ]
        }
    ]
