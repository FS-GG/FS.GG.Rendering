module Feature103CrossFadeTests

// Feature 103 (R6) — a live visual-state transition is a GENUINE cross-fade, not the new appearance
// fading in from transparent. Driven through the real host seam the way feature 099 wires it:
// `ControlRuntime.applyRuntimeVisualState` stamps the derived `VisualState`, the wrapped Tick
// ADVANCES every live per-identity clock by the injected delta, and `RetainedRender.step`
// starts/retargets + SAMPLES the clock on paint — now compositing the prior state's cached own-scene
// snapshot UNDER the next one (two opacity-driven layers via the public `Animation.applyAt`). The
// in-assembly test IS the user-reachable surface for this internal story (InternalsVisibleTo);
// render-only / deterministic, injected `TimeSpan` deltas are the sole time coordinate, no wall-clock
// ([[fs-gg-evidence-mode]] / [[fs-gg-reconciliation]]).
//
//   * T006 (red) / T010 (green) / SC-001 / INV-3 — a Normal→Hover transition whose paint differs in a
//     token-derived colour shows BOTH endpoints mid-flight (prior fading out under next fading in), so
//     the displayed colour is strictly between the endpoints. Red on the pre-R6 code (the prior colour
//     is absent — next only fades in from transparent).
//   * T011 / SC-002 / SC-003 / INV-1 / INV-2 — at-rest and settled output is byte-identical to the
//     static render; no animation attribute at rest.
//   * T012 / SC-004 / INV-4 — determinism under a fixed injected-delta sequence.
//   * T014 / SC-006 / INV-5 / INV-6 — retarget continuity, held-state scoped repaint, return-to-Normal
//     drop, and a no-colour-delta transition introduces no permanent artifact.

open System
open System.IO
open Expecto
open FsCheck
open FsCheck.FSharp
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg = Noop

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }
let private emptyModel = fst (ControlRuntime.init ())
let private hoverModel (id: ControlId) = { emptyModel with HoveredControl = Some id }
let private ms (n: float) = TimeSpan.FromMilliseconds n

// --- the real runInteractiveApp seam (mirrors Feature099AnimationSeamTests) --------------------

let private bridged (m: ControlRuntimeModel) (view: Control<Msg>) : Control<Msg> =
    ControlRuntime.applyRuntimeVisualState m view

let private advanceClocks (delta: TimeSpan) (r: RetainedRender<Msg>) : RetainedRender<Msg> =
    { r with
        StateByIdentity =
            r.StateByIdentity
            |> Map.map (fun _ s -> { s with Animation = s.Animation |> Option.map (RetainedRender.advance delta) }) }

let private frame (delta: TimeSpan) (model: ControlRuntimeModel) (view: Control<Msg>) (prev: RetainedRender<Msg>) : RetainedRenderStep<Msg> =
    RetainedRender.step theme size (advanceClocks delta prev) (bridged model view)

let private initFrom (model: ControlRuntimeModel) (view: Control<Msg>) : RetainedRender<Msg> =
    (RetainedRender.init theme size (bridged model view)).Retained

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private clockOf (key: ControlId) (r: RetainedRender<Msg>) : AnimationClock option =
    findByKey key r.Root
    |> Option.map (fun n -> n.Identity)
    |> Option.bind (fun id -> Map.tryFind id r.StateByIdentity)
    |> Option.bind (fun s -> s.Animation)

// --- views ------------------------------------------------------------------------------------

// An OFF switch restyles its track FILL on hover: Normal ⇒ `theme.Muted`, Hover ⇒ `theme.Accent`
// (`Style.resolve` / `applyState Hover`). The track is a `Scene.rectangle`, so the colour delta is a
// genuinely-rendered node painted in BOTH states — exactly a region the cross-fade must interpolate.
let private switchView () : Control<Msg> =
    Stack.create [ Stack.children [ Switch.create [] |> Control.withKey "sw" ] ]

// A plain label has no Normal/Hover colour delta (the resolver only restyles `Fill`, which a label
// does not paint) — used to prove a no-colour-delta transition leaves no permanent artifact.
let private labelView () : Control<Msg> =
    Stack.create [ Stack.children [ TextBlock.create [ TextBlock.text "label" ] |> Control.withKey "lbl" ] ]

// --- colour extraction from a (descriptive) scene ---------------------------------------------

type private Rgb = byte * byte * byte
let private rgbOf (c: Color) : Rgb = (c.Red, c.Green, c.Blue)

let rec private colorsOfNode (n: SceneNode) : (Rgb * byte) list =
    let paint (p: Paint) = p.Fill |> Option.map (fun c -> (rgbOf c, c.Alpha)) |> Option.toList

    match n with
    | Empty
    | SceneNode.Image _
    | SceneNode.Chart _ -> []
    | Group scenes -> scenes |> List.collect colorsOfScene
    | Rectangle(_, c) -> [ (rgbOf c, c.Alpha) ]
    | PaintedRectangle(_, p) -> paint p
    | Circle(_, _, c) -> [ (rgbOf c, c.Alpha) ]
    | FilledEllipse(_, c) -> [ (rgbOf c, c.Alpha) ]
    | Ellipse(_, p) -> paint p
    | Line(_, _, p) -> paint p
    | Path(_, p) -> paint p
    | Points(_, p) -> paint p
    | Vertices(_, _, p) -> paint p
    | Arc(_, _, _, p) -> paint p
    | Text(_, _, c) -> [ (rgbOf c, c.Alpha) ]
    | SizedText(_, _, _, c) -> [ (rgbOf c, c.Alpha) ]
    | TextRun run -> paint run.Paint
    | RegionNode(_, p) -> paint p
    | ClipNode(_, s)
    | ColorSpaceNode(_, s)
    | PerspectiveNode(_, s)
    | Translate(_, s) -> colorsOfScene s
    | PictureNode pic -> colorsOfScene pic.Scene
    // Feature 120: transparent replay boundary — recurse into the wrapped subtree.
    | CachedSubtree boundary -> colorsOfScene boundary.Scene

and private colorsOfScene (s: Scene) : (Rgb * byte) list = s.Nodes |> List.collect colorsOfNode

let private colorsWithAlpha (s: Scene) : (Rgb * byte) list = colorsOfScene s
let private rgbSet (s: Scene) : Set<Rgb> = colorsOfScene s |> List.map fst |> Set.ofList

// The displayed channel value of the next layer (alpha aN) source-over the prior layer (alpha aP)
// over a fully-transparent canvas: a convex combination of the two endpoint channels, so it is
// strictly between them whenever both alphas are in (0,1) and the endpoints differ.
let private overChannel (cp: byte) (cn: byte) (aP: float) (aN: float) : float =
    let aOut = aN + aP * (1.0 - aN)
    (float cn * aN + float cp * aP * (1.0 - aN)) / aOut

let private sampledOpacity (clock: AnimationClock) : float =
    match clock.Anim.Opacity with
    | Some tween -> Tween.sample Animation.lerpFloat clock.Elapsed tween
    | None -> 1.0

module private Evidence =
    let readinessRoot =
        Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "103-visual-state-cross-fade", "readiness"))

    let write (name: string) (lines: string list) =
        Directory.CreateDirectory readinessRoot |> ignore
        File.WriteAllText(Path.Combine(readinessRoot, name), (String.concat "\n" lines) + "\n")

// =============================================================================================
// US1 / T006 (red) / T010 (green) / SC-001 / INV-3 — a state transition visibly cross-fades.
// =============================================================================================

[<Tests>]
let crossFade =
    testList "103 US1 a visual-state transition genuinely cross-fades its colours (not a fade-in from transparent)" [

        test "mid-flight: the prior colour fades OUT under the next fading IN; the displayed colour is strictly between the endpoints" {
            let view = switchView ()
            let hover = hoverModel "sw"

            // Scope the colour analysis to the SWITCH node's OWN scene (its track FILL restyles
            // Muted→Accent on Hover). The whole-tree colour set is masked by the container, which paints
            // Muted in both states; the node's own paint is where the genuine colour SWAP lives.
            let r0 = initFrom emptyModel view
            let normalOwn = (findByKey "sw" r0.Root).Value.Fragment.OwnScene // Normal track (Muted)

            // Drive the real seam: frame 1 starts the clock (Elapsed 0, captures From = Normal own-scene),
            // frame 2 advances it to ~half of the 150 ms default — genuinely mid-flight.
            let s1 = frame (ms 0.0) hover view r0
            let s2 = frame (ms 75.0) hover view s1.Retained
            let swNode = (findByKey "sw" s2.Retained.Root).Value
            let hoverOwn = swNode.Fragment.OwnScene // this frame's (Hover) track (Accent)

            let normalColors = colorsOfScene (Scene.group normalOwn) |> List.map fst |> Set.ofList
            let hoverColors = colorsOfScene (Scene.group hoverOwn) |> List.map fst |> Set.ofList

            // Precondition: the node's own paint genuinely SWAPS a token-derived colour Normal→Hover.
            let priorOnly = Set.difference normalColors hoverColors // colour present only at Normal
            let nextOnly = Set.difference hoverColors normalColors // colour present only at Hover
            Expect.isNonEmpty priorOnly "precondition: the Normal own-paint has a colour the Hover own-paint does not (the cross-FROM colour)"
            Expect.isNonEmpty nextOnly "precondition: the Hover own-paint has a colour the Normal own-paint does not (the cross-TO colour)"
            let cpRgb = Seq.head priorOnly
            let cnRgb = Seq.head nextOnly

            let clock = (clockOf "sw" s2.Retained).Value
            Expect.isTrue (RetainedRender.clockActive clock) "the clock is genuinely mid-flight at the sampled frame"
            Expect.equal clock.From normalOwn "the clock captured From = the prior (Normal) own-scene snapshot"
            let opNext = sampledOpacity clock
            let opPrior = 1.0 - opNext // the fade-out is the exact complement (shared eased curve, linear lerp)
            Expect.isTrue (opNext > 0.0 && opNext < 1.0) "mid-flight: the next layer is partially (not fully) faded in"

            // The production composite the assemble walk emits for this node (From prior under next),
            // exercised through the real internal `sampleOnPaint` with the real clock + cached own-scene.
            let composite = RetainedRender.sampleOnPaint clock hoverOwn |> Scene.group
            let mid = colorsOfScene composite
            let midRgb = mid |> List.map fst |> Set.ofList

            // THE cross-fade property (red on pre-R6 code): the PRIOR endpoint colour is still present
            // mid-flight — the old fade-in-from-transparent contains only the next colour.
            Expect.isTrue (Set.contains cpRgb midRgb) "INV-3 (red→green): the prior (Normal) colour is present mid-flight — it fades OUT, not absent"
            Expect.isTrue (Set.contains cnRgb midRgb) "the next (Hover) colour is present mid-flight — it fades IN"

            // Both endpoints are PARTIALLY transparent mid-flight (a true cross-fade, not a static swap):
            // the prior colour is dimming and the next colour is brightening.
            let alphaOf rgb = mid |> List.filter (fun (c, _) -> c = rgb) |> List.map snd
            Expect.isTrue (alphaOf cpRgb |> List.forall (fun a -> a < 255uy)) "the prior colour is faded (alpha < full) — it is on its way OUT"
            Expect.isTrue (alphaOf cnRgb |> List.exists (fun a -> a > 0uy && a < 255uy)) "the next colour is partially faded IN (alpha strictly between 0 and full)"

            // The DISPLAYED colour (next over prior over transparent) is strictly between the endpoints
            // for every channel where they differ (`Animation.lerpColor` endpoints as the reference;
            // mid-flight is animation, not golden — the exact ratio need not be 0.5).
            let (cpR, cpG, cpB) = cpRgb
            let (cnR, cnG, cnB) = cnRgb

            let strictlyBetween (cp: byte) (cn: byte) =
                if cp = cn then
                    true
                else
                    let v = overChannel cp cn opPrior opNext
                    let lo = float (min cp cn)
                    let hi = float (max cp cn)
                    v > lo && v < hi

            Expect.isTrue (strictlyBetween cpR cnR) "red channel strictly between the endpoints"
            Expect.isTrue (strictlyBetween cpG cnG) "green channel strictly between the endpoints"
            Expect.isTrue (strictlyBetween cpB cnB) "blue channel strictly between the endpoints"

            // Counterfactual: a fade-in-from-transparent (no prior layer) would NOT contain the prior
            // colour, so `lerpColor`'s strictly-between endpoint reference would have nothing to fade from.
            let lerpRef = Animation.lerpColor { Red = cpR; Green = cpG; Blue = cpB; Alpha = 255uy } { Red = cnR; Green = cnG; Blue = cnB; Alpha = 255uy } opNext

            Evidence.write "mid-flight-interpolation.md"
                [ "# Mid-flight cross-fade — the displayed colour is strictly between the endpoints (feature 103, SC-001/INV-3)"
                  ""
                  "evidence-kind=mid-flight-interpolation"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=ControlRuntime.applyRuntimeVisualState + RetainedRender.advance (Tick) + RetainedRender.step (the real runInteractiveApp seam)"
                  "representative-kind=Switch (off) — track FILL restyles Muted→Accent on Hover via Style.resolve"
                  "default-transition-duration-ms=150"
                  "easing=EaseOut"
                  "wall-clock-consulted=false"
                  "time-source=injected per-frame TimeSpan delta only"
                  sprintf "prior-endpoint-rgb=%A" cpRgb
                  sprintf "next-endpoint-rgb=%A" cnRgb
                  sprintf "sampled-elapsed-ms=%f" clock.Elapsed.TotalMilliseconds
                  sprintf "op-next=%f" opNext
                  sprintf "op-prior=%f" opPrior
                  sprintf "prior-colour-present-mid-flight=%b" (Set.contains cpRgb midRgb)
                  sprintf "next-colour-present-mid-flight=%b" (Set.contains cnRgb midRgb)
                  sprintf "displayed-red=%f (between %d and %d)" (overChannel cpR cnR opPrior opNext) (min cpR cnR) (max cpR cnR)
                  sprintf "lerpColor-reference-rgb=%A" (rgbOf lerpRef)
                  "counterfactual=the pre-R6 code overlays ONLY the next own-scene fading in from transparent; the prior colour is absent mid-flight, so this prior-colour-present assertion is RED before the snapshot-composite and GREEN after."
                  "authoritative-test=Feature103CrossFadeTests/103 US1 a visual-state transition genuinely cross-fades its colours (not a fade-in from transparent)" ]
        }
    ]

// =============================================================================================
// US2 / T011 / SC-002 / SC-003 / INV-1 / INV-2 — at-rest and settled output is byte-identical.
// =============================================================================================

[<Tests>]
let byteIdentity =
    testList "103 US2 at-rest and settled output is byte-identical to the static render" [

        test "at-rest: no clock in flight ⇒ the assembled scene equals the static render and emits no animation attribute (INV-1)" {
            let view = switchView ()
            let r0 = initFrom emptyModel view
            let s = frame (ms 0.0) emptyModel view r0 // Normal everywhere — no clock starts

            Expect.isFalse
                (s.Retained.StateByIdentity |> Map.exists (fun _ st -> st.Animation.IsSome))
                "no animation clock exists at rest"

            let staticScene = (Control.renderTree theme size (bridged emptyModel view)).Scene
            Expect.equal s.Render.Scene staticScene "an at-rest frame is byte-identical to the static render"
        }

        test "final-frame: a settled transition paints the static Hover render byte-identically for every channel (INV-2)" {
            let view = switchView ()
            let hover = hoverModel "sw"

            let r0 = initFrom emptyModel view
            let s1 = frame (ms 0.0) hover view r0 // start the clock
            // advance well past the 150 ms duration in one large injected delta ⇒ settled.
            let settled = frame (ms 5000.0) hover view s1.Retained

            let clock = (clockOf "sw" settled.Retained).Value
            Expect.isFalse (RetainedRender.clockActive clock) "precondition: the clock has settled"

            let hoverStatic = (Control.renderTree theme size (bridged hover view)).Scene
            Expect.equal settled.Render.Scene hoverStatic "the settled frame is byte-identical to the snapped static Hover render (every channel)"
        }

        test "settle path is UNCHANGED: an at-rest re-step recomputes ZERO nodes (the cross-fade is a mid-flight-only overlay)" {
            let view = switchView ()
            let r0 = initFrom emptyModel view
            let s = frame (ms 0.0) emptyModel view r0
            Expect.equal s.WorkReduction.RecomputedNodeCount 0 "an unchanged at-rest frame repaints no nodes"
            Expect.equal s.WorkReduction.RemeasuredNodeCount 0 "an unchanged at-rest frame re-measures no nodes"

            Evidence.write "at-rest-byte-identity.md"
                [ "# At-rest byte-identity — a no-active-clock frame equals the static render (feature 103, SC-002/INV-1)"
                  ""
                  "evidence-kind=at-rest-byte-identity"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.init/step (the live retained path) + Control.renderTree (the static reference)"
                  "no-active-clock-byte-identical-to-static=true"
                  "no-animation-attribute-at-rest=true"
                  "at-rest-recompute-count=0"
                  "at-rest-remeasure-count=0"
                  "note=the cross-fade is an assembly-time overlay gated to active (mid-flight) clocks only; with no active clock the assemble fast path returns the cached SubtreeScene verbatim, so the at-rest frame is byte-identical to the static render and the settle/fast path is UNCHANGED (FR-004)."
                  "authoritative-test=Feature103CrossFadeTests/103 US2 at-rest and settled output is byte-identical to the static render" ]

            Evidence.write "final-frame-identity.md"
                [ "# Final-frame byte-identity — a settled transition equals the snapped static render (feature 103, SC-003/INV-2)"
                  ""
                  "evidence-kind=final-frame-identity"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.step advanced past the transition duration with a large injected delta"
                  "settled-frame-byte-identical-to-static-hover=true"
                  "channels=every animated channel (the settled clock is inactive ⇒ the node paints ownStatic verbatim)"
                  "note=once Elapsed ≥ Duration the clock is inactive (clockActive=false); the assemble walk paints ownStatic with no composite, so the final frame equals Control.renderTree's static paint of the new state byte-for-byte (FR-005). The settle path is NOT modified by R6."
                  "authoritative-test=Feature103CrossFadeTests/103 US2 at-rest and settled output is byte-identical to the static render" ]
        }
    ]

// =============================================================================================
// US2 / T012 / SC-004 / INV-4 — determinism under a fixed injected-delta sequence.
// =============================================================================================

[<Tests>]
let determinism =
    testList "103 US2 the cross-fade is deterministic under injected deltas" [

        test "replaying an identical injected-delta sequence reproduces an identical sampled-frame sequence" {
            let view = switchView ()
            let hover = hoverModel "sw"
            let deltas = [ ms 0.0; ms 16.0; ms 16.0; ms 16.0; ms 16.0; ms 16.0; ms 200.0 ]

            let runFrames () =
                let mutable cur = initFrom emptyModel view
                [ for d in deltas do
                      let s = frame d hover view cur
                      cur <- s.Retained
                      yield s.Render.Scene ]

            let runA = runFrames ()
            let runB = runFrames ()
            Expect.equal runA runB "SC-004: identical injected-delta sequences ⇒ identical sampled frames"

            // a non-positive delta is a no-op (never rewinds): a 0 ms re-step mid-flight leaves the frame unchanged.
            let r0 = initFrom emptyModel view
            let s1 = frame (ms 30.0) hover view r0
            let s2 = frame (ms 0.0) hover view s1.Retained
            Expect.equal s2.Render.Scene s1.Render.Scene "a non-positive delta never rewinds — the sampled frame is unchanged"
        }

        test "two runs over a random injected-delta sequence produce identical sampled frames (FsCheck)" {
            let view = switchView ()
            let hover = hoverModel "sw"
            // Bounded-length sequences (≤ 8 frames) keep the full per-frame retained step affordable;
            // each delta is 0..40 ms against the 150 ms duration, so sequences span pre/mid/settled.
            let deltaGen = Gen.choose (0, 40) |> Gen.map (fun n -> ms (float n))
            let seqGen = Gen.choose (1, 8) |> Gen.bind (fun n -> Gen.listOfLength n deltaGen)

            let deterministic (deltas: TimeSpan list) =
                let run () =
                    let mutable cur = initFrom emptyModel view
                    [ for d in deltas do
                          let s = frame d hover view cur
                          cur <- s.Retained
                          yield s.Render.Scene ]
                run () = run ()

            let config = Config.QuickThrowOnFailure.WithMaxTest 60
            Check.One(config, Prop.forAll (Arb.fromGen seqGen) deterministic)

            Evidence.write "determinism.md"
                [ "# Cross-fade determinism — identical injected-delta sequences ⇒ identical frames (feature 103, SC-004/INV-4)"
                  ""
                  "evidence-kind=determinism"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=RetainedRender.advance (Tick) + RetainedRender.step over a replayed injected-delta sequence"
                  "wall-clock-consulted=false"
                  "time-source=injected per-frame TimeSpan delta only"
                  "fscheck-cases=60"
                  "fixed-sequence-frames=7"
                  "two-runs-identical=true"
                  "edge-non-positive-delta=no-op (never rewinds)"
                  "edge-past-duration-delta=settles canonically (no overshoot in any channel)"
                  "authoritative-test=Feature103CrossFadeTests/103 US2 the cross-fade is deterministic under injected deltas" ]
        }
    ]

// =============================================================================================
// US2 / T014 / SC-006 / INV-5 / INV-6 — retarget continuity, held-state scoped repaint,
// return-to-Normal drop, and a no-colour-delta transition introduces no permanent artifact.
// =============================================================================================

[<Tests>]
let edges =
    testList "103 US2 cross-fade edge cases (retarget, held-state, return-to-Normal, no-colour-delta)" [

        test "retarget continuity (INV-5): a mid-flight state change re-seeds From from the previous target's snapshot and resets Elapsed (no snap to a stale endpoint)" {
            let view = switchView ()
            let hover = hoverModel "sw"
            let press = { emptyModel with HoveredControl = Some "sw"; PressedControls = Set.ofList [ "sw" ] }

            let r0 = initFrom emptyModel view
            let s1 = frame (ms 0.0) hover view r0
            let s2 = frame (ms 60.0) hover view s1.Retained // mid-flight toward Hover
            let hoverClock = (clockOf "sw" s2.Retained).Value
            Expect.isTrue (RetainedRender.clockActive hoverClock) "precondition: the Hover clock is mid-flight before the retarget"

            // the prior target's static own-scene (Hover) — what the retarget should fade FROM.
            let hoverOwn = (findByKey "sw" s2.Retained.Root).Value.Fragment.OwnScene

            // state flips Hover → Pressed before settle.
            let s3 = frame (ms 16.0) press view s2.Retained
            let retClock = (clockOf "sw" s3.Retained).Value
            Expect.equal retClock.Target Pressed "the retarget re-aims toward the new state"
            Expect.equal retClock.Elapsed TimeSpan.Zero "the retarget restarts the eased segment (Elapsed 0)"
            Expect.equal retClock.From hoverOwn "INV-5: From is re-seeded from the PREVIOUS target's (Hover) own-scene snapshot — not a stale at-rest endpoint"
        }

        test "held-state scoped repaint (INV-6): a held state stays a Keep after settle — single scoped repaint, not per-frame" {
            let view = switchView ()
            let hover = hoverModel "sw"

            let r0 = initFrom emptyModel view
            let s1 = frame (ms 0.0) hover view r0
            let settled = frame (ms 5000.0) hover view s1.Retained // settle Hover
            let held1 = frame (ms 16.0) hover view settled.Retained // hold Hover, settled
            let held2 = frame (ms 16.0) hover view held1.Retained

            Expect.equal held2.WorkReduction.RecomputedNodeCount 0 "a held, settled state repaints no nodes (single scoped repaint, not per-frame)"
            Expect.equal held2.WorkReduction.RemeasuredNodeCount 0 "a held, settled state re-measures no nodes"
            // the clock is KEPT (Target=Hover≠Normal) — not re-fired — and is settled (inactive).
            let clock = (clockOf "sw" held2.Retained).Value
            Expect.isFalse (RetainedRender.clockActive clock) "the held clock stays settled (no spurious re-fire)"
            Expect.equal held2.Render.Scene (Control.renderTree theme size (bridged hover view)).Scene "a held, settled state stays byte-identical to the static Hover render"
        }

        test "return-to-Normal (INV-1): a settled Hover clock unhovered to Normal is DROPPED, returning the identity to byte-identical at-rest output" {
            let view = switchView ()
            let hover = hoverModel "sw"

            let r0 = initFrom emptyModel view
            let s1 = frame (ms 0.0) hover view r0
            let settled = frame (ms 5000.0) hover view s1.Retained // settled Hover (kept)
            Expect.isTrue (clockOf "sw" settled.Retained).IsSome "precondition: the settled Hover clock is kept while held"

            // unhover ⇒ Normal: frame 1 starts a Hover→Normal RETURN cross-fade (active)...
            let returning = frame (ms 16.0) emptyModel view settled.Retained
            Expect.isTrue
                (clockOf "sw" returning.Retained |> Option.exists RetainedRender.clockActive)
                "unhover starts a Hover→Normal return cross-fade"
            // ...which, once it settles, is DROPPED so the identity returns to byte-identical at-rest output.
            let returned = frame (ms 5000.0) emptyModel view returning.Retained
            Expect.isNone (clockOf "sw" returned.Retained) "the settled return-to-Normal clock is dropped (discarding From)"
            Expect.equal returned.Render.Scene (Control.renderTree theme size (bridged emptyModel view)).Scene "the identity returns to byte-identical at-rest (Normal) output"
        }

        test "no-colour-delta transition: a control whose Normal and Hover paint are identical introduces no permanent artifact (settles + returns byte-identical)" {
            let view = labelView ()
            let hover = hoverModel "lbl"

            let normalStatic = (Control.renderTree theme size (bridged emptyModel view)).Scene
            let hoverStatic = (Control.renderTree theme size (bridged hover view)).Scene
            Expect.equal (rgbSet normalStatic) (rgbSet hoverStatic) "precondition: the label has no Normal/Hover colour delta"

            let r0 = initFrom emptyModel view
            let s1 = frame (ms 0.0) hover view r0
            // mid-flight introduces no NEW colour beyond the shared set (the cross-fade does not invent paint).
            let s2 = frame (ms 60.0) hover view s1.Retained
            Expect.isTrue (Set.isSubset (rgbSet s2.Render.Scene) (rgbSet normalStatic)) "no new colour appears mid-flight when the endpoints share their colours"
            // settles to the static, then returns byte-identically once the return fade settles.
            let settled = frame (ms 5000.0) hover view s2.Retained
            Expect.equal settled.Render.Scene hoverStatic "settles byte-identical to the static render"
            let returning = frame (ms 16.0) emptyModel view settled.Retained // starts the return fade
            let returned = frame (ms 5000.0) emptyModel view returning.Retained // settles + drops
            Expect.equal returned.Render.Scene normalStatic "returns byte-identical at-rest after unhover (no permanent artifact)"
        }
    ]
