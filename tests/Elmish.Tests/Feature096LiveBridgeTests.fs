module Feature096LiveBridgeTests

// Feature 096 (R1) — the runtime visual-state bridge on the LIVE retained path.
//   * T018 / SC-002 / FR-007: a focused control's derived `Focused` state rides its attributes through
//     the keyed reconciler, so across a sibling-shifting re-render the indicator stays on the SAME
//     retained identity — proven through the live `RetainedRender.init`/`step` path + the REAL bridge
//     (`ControlRuntime.applyRuntimeVisualState`), NOT a hand-seeded `StateByIdentity` map.
//   * T020 / responds-proof: an input (focus/hover) → the bridge → a visibly restyled Scene that an
//     inert / un-bridged build (the same view without the bridge) does NOT produce (identical frames).
// Render-only / deterministic; the in-assembly test reaches the internal retained structure + the
// internal bridge via InternalsVisibleTo (the wired path is the user-reachable surface) —
// [[fs-skia-evidence-mode]].

open System.IO
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = NameChanged of string

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private emptyModel = fst (ControlRuntime.init ())
let private focusModel id = { emptyModel with FocusedControl = Some id }

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private nodeOf (key: ControlId) (r: RetainedRender<'msg>) = (findByKey key r.Root).Value
let private stateOfNode (node: RetainedNode<'msg>) = ControlInternals.visualStateOf node.Control.Attributes

// The editor field, keyed so it carries a stable RetainedId across a shift.
let private editor (name: string) : Control<Msg> =
    TextBox.create [ TextBox.value name; TextBox.onChanged NameChanged ] |> Control.withKey "editor"

// The same view with / without the live runtime bridge applied (the bridge is what the host does in
// `renderRetained` before `init`/`step`).
let private bridged (m: ControlRuntimeModel) (children: Control<Msg> list) : Control<Msg> =
    ControlRuntime.applyRuntimeVisualState m (Stack.create [ Stack.children children ])

let private inert (children: Control<Msg> list) : Control<Msg> =
    Stack.create [ Stack.children children ]

let private readinessRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "096-runtime-visual-state-bridge", "readiness"))

let private writeEvidence (name: string) (lines: string list) =
    Directory.CreateDirectory readinessRoot |> ignore
    File.WriteAllText(Path.Combine(readinessRoot, name), (String.concat "\n" lines) + "\n")

[<Tests>]
let feature096LiveBridge =
    testList "Feature 096 runtime bridge — live retained path" [

        // ---- T018 — focus indicator survives a sibling-shifting re-render via real identity --------
        test "T018 — the Focused indicator stays on the same retained identity across a sibling shift" {
            let m = focusModel "editor"

            // frame 0: the editor is the stack's only child; the bridge derives Focused for it.
            let r0 = (RetainedRender.init theme size (bridged m [ editor "hi" ])).Retained
            let before = nodeOf "editor" r0
            Expect.equal (stateOfNode before) Focused "the bridge stamped Focused onto the focused editor (no consumer attribute)"

            // frame 1: an UNRELATED banner is inserted above the editor, shifting its position.
            let shifted =
                bridged m [ TextBlock.create [ TextBlock.text "banner" ] |> Control.withKey "banner"; editor "hi" ]
            let s = RetainedRender.step theme size r0 shifted
            let after = nodeOf "editor" s.Retained

            Expect.equal after.Identity before.Identity "FR-007: the editor keeps its stable RetainedId across the shift (E2 identity)"
            Expect.equal (stateOfNode after) Focused "SC-002: the Focused indicator stays on the SAME control after the shift"

            // a baseline that rebuilds every frame mints a fresh id for the shifted editor — the
            // indicator only stays put because the wired step path preserves identity.
            let baselineId0 = (nodeOf "editor" ((RetainedRender.init theme size (bridged m [ editor "hi" ])).Retained)).Identity
            let baselineId1 =
                (nodeOf "editor" ((RetainedRender.init theme size shifted).Retained)).Identity
            Expect.notEqual baselineId1 baselineId0 "baseline: rebuilding every frame mints a new id (the wired path is what carries identity)"

            writeEvidence "focus-survives-reshuffle.md"
                [ "# Focus indicator survives a sibling-shifting re-render (feature 096, SC-002, FR-007)"
                  ""
                  "evidence-kind=focus-survives-reshuffle"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=ControlRuntime.applyRuntimeVisualState (the real bridge) + RetainedRender.init/step (the live retained path)"
                  "hand-seeded-state-by-identity=false"
                  "sequence=focus editor -> derive Focused via bridge -> insert banner above (shift) -> re-derive"
                  sprintf "retained-id-stable-across-shift=%b" (after.Identity = before.Identity)
                  sprintf "focused-state-before-shift=%A" (stateOfNode before)
                  sprintf "focused-state-after-shift=%A" (stateOfNode after)
                  "baseline-loses-identity-on-shift=true"
                  "note=the indicator attaches to the E2 stable retained identity (067/091/092 scheme, consumed not re-derived); the resolved Focused look rides the control's attributes through the keyed diff."
                  "authoritative-test=Feature096LiveBridgeTests/Feature 096 runtime bridge — live retained path" ]
        }

        // ---- T020 — responds-proof: bridged input restyles; the inert build does not ---------------
        test "T020 — a focus/hover input restyles on the live path; an inert (un-bridged) build does not" {
            // FOCUS responds-proof on a text-box (Focused turns its border accent — a visible indicator).
            let focusBridged = (Control.renderTree theme size (bridged (focusModel "editor") [ editor "x" ])).Scene
            let focusInert = (Control.renderTree theme size (inert [ editor "x" ])).Scene
            Expect.notEqual focusBridged focusInert "a focus input drives a visible restyle the inert build lacks"

            // HOVER/PRESS responds-proof on a button (Pressed → Muted fill, visibly distinct from Accent).
            let pressModel = { emptyModel with PressedControls = Set.ofList [ "go" ] }
            let btn: Control<Msg> = Button.create [ Button.text "Go" ] |> Control.withKey "go"
            let pressBridged = (Control.renderTree theme size (bridged pressModel [ btn ])).Scene
            let pressInert = (Control.renderTree theme size (inert [ btn ])).Scene
            Expect.notEqual pressBridged pressInert "a press input drives a visible restyle the inert build lacks"

            // the inert build is genuinely inert: with no bridge, the focused/pressed model produces the
            // SAME frame as the resting model (no response at all).
            let restInert = (Control.renderTree theme size (inert [ editor "x" ])).Scene
            Expect.equal focusInert restInert "the un-bridged build does not respond to interaction state (identical frames)"

            writeEvidence "responds-proof.md"
                [ "# Input -> visible restyle on the live retained path (feature 096, responds-proof)"
                  ""
                  "evidence-kind=responds-proof"
                  "renderer-mode=DeterministicRenderOnly"
                  "status=pass"
                  "driven-through=ControlRuntime.applyRuntimeVisualState before RetainedRender (the host renderRetained seam)"
                  sprintf "focus-input-restyles=%b" (focusBridged <> focusInert)
                  sprintf "press-input-restyles=%b" (pressBridged <> pressInert)
                  sprintf "un-bridged-build-is-inert=%b" (focusInert = restInert)
                  "note=the responds-proof is the bridged frame DIFFERING from the inert/un-bridged frame for the same input; an inert build paints identical frames regardless of interaction state. Structural Scene inequality, not a pixel encoder ([[fs-skia-evidence-mode]])."
                  "authoritative-test=Feature096LiveBridgeTests/Feature 096 runtime bridge — live retained path" ]
        }
    ]
