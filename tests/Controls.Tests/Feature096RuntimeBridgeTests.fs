module Feature096RuntimeBridgeTests

// Feature 096 (R1) — the runtime visual-state bridge.
//   * T010 / SC-004: `deriveVisualState` runtime-precedence (Pressed > Selected > Focused > Hover >
//     Normal), unknown id ⇒ Normal, determinism.
//   * T011 / SC-001: `applyRuntimeVisualState` stamps the derived state onto a NO-ATTRIBUTE control
//     so a migrated control restyles on hover/press/select; a non-interacted sibling stays Normal.
//   * T012 / FR-005 / SC-003: a Normal-and-unset tree is returned UNCHANGED and is Scene-byte-
//     identical to the un-bridged build; the live retained step recomputes 0 nodes at rest.
//   * T017 / SC-002: a focused control gains the Focused indicator; moving focus moves the indicator.
//   * T021 / FR-003 / SC-004: consumer-set non-Normal state out-ranks any derived interaction state;
//     a consumer-Normal control lets the derived state fill the slot (single carrier channel).
//   * T022 / SC-004: an FsCheck property over ≥1000 generated (model, id, consumer-state) combos —
//     totality + determinism, the closed order holds, consumer non-Normal preserved 100%.
//   * T024 / SC-005: a single hover entering one control surfaces a bounded (< baseline) repaint.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 240 }
let private box: Rect = { X = 10.0; Y = 40.0; Width = 284.0; Height = 92.0 }

let private emptyModel = fst (ControlRuntime.init ())

let private modelWith (f: ControlRuntimeModel -> ControlRuntimeModel) = f emptyModel

let private stateOf (control: Control<'msg>) = ControlInternals.visualStateOf control.Attributes

// Control<'msg> carries function-valued attributes, so it has no structural equality; compare the
// lowered tree shape by its `%A` projection (the same technique the parity oracles use).
let private dump (control: Control<'msg>) = sprintf "%A" control

// A keyed migrated button with NO visualState attribute (the zero-consumer-code path).
let private button (key: string) : Control<int> =
    Button.create [ Button.text "Go" ] |> Control.withKey key

[<Tests>]
let feature096RuntimeBridgeTests =
    testList "Feature 096 runtime visual-state bridge" [

        // ---- T010 — deriveVisualState runtime precedence (SC-004) -----------------------------
        test "T010 — runtime precedence Pressed > Selected > Focused > Hover > Normal" {
            // Build one model in which "btn" is simultaneously pressed, selected, focused, hovered;
            // peeling each higher state off must reveal exactly the next-ranked one.
            let pressed =
                modelWith (fun m ->
                    { m with
                        PressedControls = Set.ofList [ "btn" ]
                        Selection = Some { ControlId = "btn"; Start = 0; End = 1 }
                        FocusedControl = Some "btn"
                        HoveredControl = Some "btn" })
            Expect.equal (ControlRuntime.deriveVisualState pressed "btn") Pressed "pressed out-ranks all"

            let selected = { pressed with PressedControls = Set.empty }
            Expect.equal (ControlRuntime.deriveVisualState selected "btn") Selected "selected out-ranks focus/hover"

            let focused = { selected with Selection = None }
            Expect.equal (ControlRuntime.deriveVisualState focused "btn") Focused "focus out-ranks hover"

            let hover = { focused with FocusedControl = None }
            Expect.equal (ControlRuntime.deriveVisualState hover "btn") Hover "hover out-ranks normal"

            let normal = { hover with HoveredControl = None }
            Expect.equal (ControlRuntime.deriveVisualState normal "btn") Normal "no interaction ⇒ Normal"
        }

        test "T010 — an id named by no interaction state resolves to Normal" {
            let m = modelWith (fun m -> { m with HoveredControl = Some "btn"; FocusedControl = Some "other" })
            Expect.equal (ControlRuntime.deriveVisualState m "unnamed") Normal "an unreferenced id is Normal"
        }

        test "T010 — deriveVisualState is deterministic for identical inputs" {
            let m = modelWith (fun m -> { m with PressedControls = Set.ofList [ "a"; "b" ]; HoveredControl = Some "c" })
            Expect.equal (ControlRuntime.deriveVisualState m "a") (ControlRuntime.deriveVisualState m "a") "identical inputs ⇒ identical result"
        }

        // ---- T011 — bridged restyle with a no-attribute view (SC-001) -------------------------
        test "T011 — a migrated control restyles on hover/press/select with a NO-attribute view" {
            let hovered = modelWith (fun m -> { m with HoveredControl = Some "btn" })
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState hovered (button "btn"))) Hover "the bridge stamped Hover onto a control the consumer left unstyled"

            // A pressed primary button resolves Fill = Muted — visibly distinct from its Normal Accent
            // fill — proving the runtime state actually drove the resolved paint (not just the attr).
            let pressed = modelWith (fun m -> { m with PressedControls = Set.ofList [ "btn" ] })
            let bridged = ControlRuntime.applyRuntimeVisualState pressed (button "btn")
            Expect.equal (stateOf bridged) Pressed "press stamps Pressed"
            Expect.notEqual
                (ControlInternals.faithfulContent theme box bridged)
                (ControlInternals.faithfulContent theme box (button "btn"))
                "the pressed control's resolved paint differs from its Normal render (it restyled)"

            let selected = modelWith (fun m -> { m with Selection = Some { ControlId = "btn"; Start = 0; End = 2 } })
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState selected (button "btn"))) Selected "selection stamps Selected"
        }

        test "T011 — a non-interacted sibling resolves Normal and is returned unchanged" {
            let hovered = modelWith (fun m -> { m with HoveredControl = Some "btn" })
            let sibling = button "other"
            let bridged = ControlRuntime.applyRuntimeVisualState hovered sibling
            Expect.equal (stateOf bridged) Normal "the un-hovered sibling stays Normal"
            Expect.equal (dump bridged) (dump sibling) "the untouched sibling is structurally unchanged (no attribute added)"
        }

        // ---- T012 — byte-identity at rest (FR-005 / SC-003) -----------------------------------
        test "T012 — a Normal-and-unset tree is returned UNCHANGED by the bridge" {
            let tree: Control<int> =
                Stack.create [ Stack.children [ button "btn"; Slider.create [] |> Control.withKey "s" ] ]
            let bridged = ControlRuntime.applyRuntimeVisualState emptyModel tree
            Expect.equal (dump bridged) (dump tree) "an empty model adds no attribute anywhere — the tree is unchanged"
        }

        test "T012 — the at-rest bridged tree is Scene-byte-identical to the un-bridged build" {
            let tree: Control<int> =
                Stack.create [ Stack.children [ button "btn"; Slider.create [] |> Control.withKey "s" ] ]
            let bridged = ControlRuntime.applyRuntimeVisualState emptyModel tree
            Expect.equal
                (Control.renderTree theme size bridged).Scene
                (Control.renderTree theme size tree).Scene
                "the bridge emits no attribute at rest ⇒ byte-identical Scene"
        }

        test "T012 — the live retained step recomputes 0 nodes at rest (RecomputedNodeCount unchanged)" {
            let tree: Control<int> =
                Stack.create [ Stack.children [ button "btn"; Slider.create [] |> Control.withKey "s" ] ]
            let init = RetainedRender.init theme size tree
            let rest = RetainedRender.step theme size init.Retained (ControlRuntime.applyRuntimeVisualState emptyModel tree)
            Expect.equal rest.WorkReduction.RecomputedNodeCount 0 "an at-rest bridged frame is identity ⇒ zero recompute"
        }

        // ---- T017 — focus indicator (SC-002) --------------------------------------------------
        test "T017 — a focused control gains the Focused indicator with no consumer focus attribute" {
            let focusA = modelWith (fun m -> { m with FocusedControl = Some "a" })
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState focusA (button "a"))) Focused "focused control gets Focused"
            // Focus on a text-box turns its resolver-driven border accent (a visible focus indicator).
            let tb: Control<int> = TextBox.create [ TextBox.value "hi" ] |> Control.withKey "a"
            Expect.notEqual
                (ControlInternals.faithfulContent theme box (ControlRuntime.applyRuntimeVisualState focusA tb))
                (ControlInternals.faithfulContent theme box tb)
                "a focused text-box's resolved paint differs from its unfocused render (focus indicator visible)"
        }

        test "T017 — moving focus moves the indicator; the previously-focused control returns to Normal" {
            let focusA = modelWith (fun m -> { m with FocusedControl = Some "a" })
            let focusB = modelWith (fun m -> { m with FocusedControl = Some "b" })
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState focusA (button "a"))) Focused "a is focused under focusA"
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState focusB (button "a"))) Normal "a returns to Normal once focus moves to b"
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState focusB (button "b"))) Focused "b now carries the indicator"
        }

        // ---- T021 — consumer-vs-derived arbitration (FR-003 / SC-004) -------------------------
        test "T021 — a consumer-Disabled control the runtime reports hovered/pressed/focused stays Disabled" {
            let busy =
                modelWith (fun m ->
                    { m with
                        HoveredControl = Some "btn"
                        PressedControls = Set.ofList [ "btn" ]
                        FocusedControl = Some "btn" })
            let disabled: Control<int> = Button.create [ Button.text "Go"; Attr.visualState Disabled ] |> Control.withKey "btn"
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState busy disabled)) Disabled "consumer Disabled out-ranks every derived state"
        }

        test "T021 — a consumer-Selected control the runtime reports Pressed stays Selected" {
            let pressedModel = modelWith (fun m -> { m with PressedControls = Set.ofList [ "btn" ] })
            let selected: Control<int> = Button.create [ Button.text "Go"; Attr.visualState Selected ] |> Control.withKey "btn"
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState pressedModel selected)) Selected "consumer Selected out-ranks derived Pressed"
        }

        test "T021 — a consumer-Normal control the runtime reports focused becomes Focused (derived fills the slot)" {
            let focused = modelWith (fun m -> { m with FocusedControl = Some "btn" })
            // The consumer left state at Normal (no attribute); the derived Focused fills the slot.
            Expect.equal (stateOf (ControlRuntime.applyRuntimeVisualState focused (button "btn"))) Focused "derived fills the consumer's Normal slot"
        }

        // ---- T024 — partial repaint: a single hover is a bounded repaint (SC-005) -------------
        test "T024 — a single hover entering one control is a bounded (< baseline) repaint" {
            let tree (m: ControlRuntimeModel) : Control<int> =
                ControlRuntime.applyRuntimeVisualState m
                    (Stack.create [ Stack.children [ button "a"; button "b"; button "c" ] ])
            let init = RetainedRender.init theme size (tree emptyModel)
            let hoverB = modelWith (fun m -> { m with HoveredControl = Some "b" })
            let step = RetainedRender.step theme size init.Retained (tree hoverB)
            let w = step.WorkReduction
            Expect.isLessThan w.RecomputedNodeCount w.BaselineNodeCount "a localized hover repaints fewer than all nodes (SC-005)"
            Expect.isGreaterThan w.ChangedSubtreeBound 0 "the hovered control IS counted as changed work"
        }

        // ---- T015 / T023 — each widened kind restyles under a runtime state; Normal ≡ unset --------
        test "T015/T023 — each widened kind restyles under a runtime state and is byte-identical at rest" {
            // Per-kind the visible channel differs: slider/switch/radio-group restyle their accent FILL
            // (Pressed → Muted); text-box restyles its border STROKE (Focused → Accent). All four are
            // byte-identical at rest (attaching `Normal` ≡ no attribute), proving FR-006 / SC-006.
            let withState (state: VisualState) (c: Control<'msg>) : Control<'msg> =
                { c with Attributes = c.Attributes @ [ Attr.visualState state ] }

            let cases: (string * Control<int> * VisualState) list =
                [ "slider", Slider.create [ Slider.value 0.5 ], Pressed
                  "switch", Switch.create [ Switch.checked' true ], Pressed
                  "radio-group", RadioGroup.create [ RadioGroup.items [ "a"; "b" ]; RadioGroup.selected "a" ], Pressed
                  "text-box", TextBox.create [ TextBox.value "x" ], Focused ]

            for (name, control, visible) in cases do
                let atRest = ControlInternals.faithfulContent theme box control
                let normalAttr = ControlInternals.faithfulContent theme box (withState Normal control)
                Expect.equal normalAttr atRest (sprintf "%s: a Normal attribute is byte-identical to the unset (at-rest) render" name)
                let restyled = ControlInternals.faithfulContent theme box (withState visible control)
                Expect.notEqual restyled atRest (sprintf "%s: a runtime visual state restyles its resolved paint" name)
        }

        // ---- T023 — an unmigrated kind shows NO render delta under a runtime state (SC-006) --------
        test "T023 — an unmigrated kind shows no render delta under a runtime state" {
            let withState (state: VisualState) (c: Control<'msg>) : Control<'msg> =
                { c with Attributes = c.Attributes @ [ Attr.visualState state ] }
            // progress-bar / numeric-input remain unmigrated (096 widened only slider/text-box/
            // radio-group/switch); stamping a runtime state must NOT change their render.
            let unmigrated: Control<int> list =
                [ ProgressBar.create [ ProgressBar.value 0.5 ]; NumericInput.create [ NumericInput.value 3.0 ] ]
            for control in unmigrated do
                Expect.equal
                    (ControlInternals.faithfulContent theme box (withState Pressed control))
                    (ControlInternals.faithfulContent theme box control)
                    "an unmigrated kind ignores the runtime visual state — no render delta"
        }
    ]

// ---- T022 — FsCheck property over the bridge (SC-004) --------------------------------------
module private Gen096 =
    let ids = [ "a"; "b"; "c"; "btn" ]

    let private genIdOpt: Gen<ControlId option> = Gen.elements (None :: List.map Some ids)

    let private genId: Gen<ControlId> = Gen.elements ("unnamed" :: ids)

    let genModel: Gen<ControlRuntimeModel> =
        gen {
            let! focused = genIdOpt
            let! hovered = genIdOpt
            let! pressed = Gen.listOf (Gen.elements ids) |> Gen.map Set.ofList
            let! selId = genIdOpt
            let selection = selId |> Option.map (fun c -> { ControlId = c; Start = 0; End = 1 })
            return
                { fst (ControlRuntime.init ()) with
                    FocusedControl = focused
                    HoveredControl = hovered
                    PressedControls = pressed
                    Selection = selection }
        }

    // Consumer state including the head states (Disabled/Validation/Loading) that are NEVER derived.
    let genConsumer: Gen<VisualState> =
        Gen.elements
            [ Normal
              Disabled
              Loading
              Pressed
              Selected
              Focused
              Hover
              VisualState.Validation Valid
              VisualState.Validation(Invalid "e") ]

    let tuple: Gen<ControlRuntimeModel * ControlId * VisualState> =
        gen {
            let! m = genModel
            let! id = genId
            let! consumer = genConsumer
            return (m, id, consumer)
        }

    // Build a keyed control whose consumer state is `consumer` (Normal ≡ no attribute / unset).
    let control (id: ControlId) (consumer: VisualState) : Control<int> =
        let attrs = if consumer = Normal then [] else [ Attr.visualState consumer ]
        Control.create "button" attrs |> Control.withKey id

[<Tests>]
let feature096BridgePropertyTests =
    testList "Feature 096 bridge properties (FsCheck, SC-004)" [

        testCase "deriveVisualState is total + deterministic over ≥1000 generated combos" (fun () ->
            let prop (m, id, _) = ControlRuntime.deriveVisualState m id = ControlRuntime.deriveVisualState m id
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen096.tuple) prop))

        testCase "the closed order holds: consumer non-Normal preserved, else derived fills the slot (≥1000)" (fun () ->
            // The bridge realizes the fixed order Disabled > Validation > Loading > Pressed > Selected >
            // Focused > Hover > Normal: a consumer-set non-Normal state (the head states are ONLY
            // consumer-set) is preserved 100%; a consumer-Normal control takes the derived runtime tail.
            let prop (m, id, consumer) =
                let result = Gen096.control id consumer |> ControlRuntime.applyRuntimeVisualState m |> stateOf
                let expected = if consumer = Normal then ControlRuntime.deriveVisualState m id else consumer
                result = expected
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen096.tuple) prop))

        testCase "applyRuntimeVisualState is deterministic over ≥1000 generated combos" (fun () ->
            let prop (m, id, consumer) =
                let once () = Gen096.control id consumer |> ControlRuntime.applyRuntimeVisualState m |> dump
                once () = once ()
            let config = Config.QuickThrowOnFailure.WithMaxTest 1000
            Check.One(config, Prop.forAll (Arb.fromGen Gen096.tuple) prop))
    ]
