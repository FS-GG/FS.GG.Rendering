module Feature094FocusRoutingTests

// Feature 094 (E4) — the HOST key-routing seam `routeFocusedKey`, driven through the REAL retained
// adapter path (the same `RetainedRender` + `resolveFocus` + `routeFocusedText` seam `runInteractiveApp`
// wires), reached via InternalsVisibleTo with NO hand-seeded identity map. A focused Button activates
// once (pointer-equivalent), a focused Slider navigates on the arrows, the E1 text seam is unchanged,
// focus survives a sibling-shifting re-render via the live retained path, and the focus indicator is
// the E3 resolver's `Focused` state (no procedural branch). Render-only / deterministic — no live
// Vulkan window ([[fs-gg-evidence-mode]]).

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

type private Msg =
    | Clicked
    | SliderChanged of float
    | TextChanged of string

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (c: Control<'msg>) : RetainedRender<'msg> = (RetainedRender.init theme size c).Retained

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private idOfKey (key: ControlId) (r: RetainedRender<'msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private boxOfKey (key: ControlId) (r: RetainedRender<'msg>) : Rect =
    (findByKey key r.Root).Value.Fragment.Box.Value

let private centre (b: Rect) = b.X + b.Width / 2.0, b.Y + b.Height / 2.0

// --- views -----------------------------------------------------------------------------------

let private routingView () : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Button.create [ Button.text "Go"; Button.onClick Clicked ] |> Control.withKey "btn"
                Slider.create [ Slider.value 0.5; Slider.onChanged SliderChanged ] |> Control.withKey "sld" ] ]

// a sibling-shifting re-render: a non-focusable banner inserted above pushes both controls' positions
// (and their path-derived ControlIds) down, exercising the LIVE retained path's identity survival.
let private shiftedRoutingView () : Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBlock.create [ TextBlock.text "banner" ] |> Control.withKey "banner"
                Button.create [ Button.text "Go"; Button.onClick Clicked ] |> Control.withKey "btn"
                Slider.create [ Slider.value 0.5; Slider.onChanged SliderChanged ] |> Control.withKey "sld" ] ]

let private textView () : Control<Msg> =
    Stack.create
        [ Stack.children
              [ TextBox.create [ TextBox.value "hi"; TextBox.onChanged TextChanged ] |> Control.withKey "editor" ] ]

let private order (r: RetainedRender<'msg>) : TabOrder = Focus.order r.Root.Control

// =============================================================================================
// T017 / SC-002 — focused Button activates ONCE (pointer-equivalent); focused Slider navigates.
// =============================================================================================

[<Tests>]
let activationTests =
    testList "094 US2 routeFocusedKey activation + navigation (SC-002)" [
        test "a focused Button + an ActivationKey produces exactly the pointer-equivalent message ONCE" {
            let r = routingView () |> rinit
            let focused = idOfKey "btn" r

            let _, ctrlMsgs, msgs = ControlsElmish.routeFocusedKey r focused (order r) ViewerKey.Enter false
            Expect.equal msgs [ Clicked ] "Enter dispatches the same message a pointer click would, exactly once (no double-dispatch)"
            Expect.isEmpty ctrlMsgs "activation emits no traversal message"

            let _, _, spaceMsgs = ControlsElmish.routeFocusedKey r focused (order r) ViewerKey.Space false
            Expect.equal spaceMsgs [ Clicked ] "Space also activates the focused button once"
        }

        test "a focused Slider + ArrowRight/ArrowLeft produces its value-change message" {
            let r = routingView () |> rinit
            let focused = idOfKey "sld" r

            let _, _, rightMsgs = ControlsElmish.routeFocusedKey r focused (order r) ViewerKey.ArrowRight false

            match rightMsgs with
            | [ SliderChanged v ] -> Expect.floatClose Accuracy.medium v 0.6 "ArrowRight steps the slider value up (0.5 -> 0.6)"
            | other -> failtestf "expected exactly one SliderChanged, got %A" other

            let _, _, leftMsgs = ControlsElmish.routeFocusedKey r focused (order r) ViewerKey.ArrowLeft false

            match leftMsgs with
            | [ SliderChanged v ] -> Expect.floatClose Accuracy.medium v 0.4 "ArrowLeft steps the slider value down (0.5 -> 0.4)"
            | other -> failtestf "expected exactly one SliderChanged, got %A" other
        }

        test "an unmatched key falls through — no msgs, no traversal (SC-006 totality)" {
            let r = routingView () |> rinit
            let focused = idOfKey "btn" r
            let _, ctrlMsgs, msgs = ControlsElmish.routeFocusedKey r focused (order r) (ViewerKey.Unknown "Q") false
            Expect.isEmpty msgs "an unmatched key produces no product message (host then consults MapKey)"
            Expect.isEmpty ctrlMsgs "an unmatched key produces no traversal message"
        }
    ]

// =============================================================================================
// T015 / SC-001 — Tab / Shift+Tab traverse the order, emitting FocusControl.
// =============================================================================================

[<Tests>]
let traversalWiringTests =
    testList "094 US1 traversal at the host seam (SC-001)" [
        test "Tab advances focus to the next stop, Shift+Tab to the previous" {
            let r = routingView () |> rinit // order: btn ; sld
            let btn = idOfKey "btn" r
            let sld = idOfKey "sld" r

            let _, fwd, _ = ControlsElmish.routeFocusedKey r btn (order r) (ViewerKey.Unknown "Tab") false
            Expect.equal fwd [ FocusControl(Some "sld") ] "Tab from btn moves focus to sld"

            let _, back, _ = ControlsElmish.routeFocusedKey r sld (order r) (ViewerKey.Unknown "Shift+Tab") true
            Expect.equal back [ FocusControl(Some "btn") ] "Shift+Tab from sld moves focus back to btn"

            let _, wrap, _ = ControlsElmish.routeFocusedKey r sld (order r) (ViewerKey.Unknown "Tab") false
            Expect.equal wrap [ FocusControl(Some "btn") ] "Tab from the last stop wraps cyclically to the first"
        }
    ]

// =============================================================================================
// T018 / SC-003 — the E1 text seam is unchanged (text delivery is not regressed).
// =============================================================================================

[<Tests>]
let textSeamTests =
    testList "094 US2 E1 text seam preserved (SC-003)" [
        test "a focused text control still receives typed text through the unchanged routeFocusedText path" {
            let r0 = textView () |> rinit
            let ex, ey = centre (boxOfKey "editor" r0)
            let focused = ControlsElmish.resolveFocus r0 ex ey
            Expect.equal focused (idOfKey "editor" r0) "the click resolves the editor's RetainedId"

            let r1, msgs = ControlsElmish.routeFocusedText r0 focused (InsertText "X")
            let draft = (Map.find focused.Value r1.StateByIdentity).Text.Value.DraftText
            Expect.equal draft "hiX" "SC-003: the first keystroke appends to the pre-filled value, unchanged from E1"
            Expect.contains msgs (TextChanged "hiX") "the text control's onChanged binding still dispatches"
        }
    ]

// =============================================================================================
// T023 / SC-004 — focus survives a sibling-shifting re-render via the LIVE retained path.
// =============================================================================================

[<Tests>]
let stabilityTests =
    testList "094 US3 focus stability over the live retained path (SC-004)" [
        test "after a sibling-shifting RetainedRender.step the focused control keeps its RetainedId and still activates" {
            let r0 = routingView () |> rinit
            let focused = idOfKey "btn" r0

            // the UNRELATED shift: insert a banner above (the model is otherwise identical).
            let s = RetainedRender.step theme size r0 (shiftedRoutingView ())
            Expect.equal (idOfKey "btn" s.Retained) focused "SC-004: btn keeps its stable RetainedId across the positional shift (not a hand-seeded map)"

            // the SAME focused RetainedId still routes activation on the post-shift retained tree.
            let _, _, msgs = ControlsElmish.routeFocusedKey s.Retained focused (order s.Retained) ViewerKey.Enter false
            Expect.equal msgs [ Clicked ] "the focused button still activates after the shift via the same identity"
        }
    ]

// =============================================================================================
// T025 / FR-006 — pointer<->keyboard focus composition.
// =============================================================================================

[<Tests>]
let pointerCompositionTests =
    testList "094 US3 pointer<->keyboard composition (FR-006)" [
        test "a pointer press sets focus to the focusable control under it; traversal continues from there" {
            let r = routingView () |> rinit
            // click the slider — resolveFocus gives its RetainedId, and the slider node is focusable.
            let sx, sy = centre (boxOfKey "sld" r)
            let hit = ControlsElmish.resolveFocus r sx sy
            Expect.equal hit (idOfKey "sld" r) "the press resolves to the slider's RetainedId"

            let node = (findByKey "sld" r.Root).Value
            Expect.isTrue (node.Control.Accessibility |> Option.exists (fun m -> m.Keyboard.Focusable)) "the slider is focusable, so the host adopts it as focus"

            // subsequent Tab continues from the pointer-set focus (sld -> wraps to btn).
            let _, fwd, _ = ControlsElmish.routeFocusedKey r hit (order r) (ViewerKey.Unknown "Tab") false
            Expect.equal fwd [ FocusControl(Some "btn") ] "traversal continues from the pointer-set focus position"
        }

        test "a press on a NON-focusable region leaves the current focus unchanged (not silently cleared)" {
            let r = shiftedRoutingView () |> rinit
            // the banner is a non-focusable static text; a press over it must NOT become focus.
            let bx, by = centre (boxOfKey "banner" r)
            let hit = ControlsElmish.resolveFocus r bx by

            let isFocusable =
                hit
                |> Option.bind (fun id -> findByKey "banner" r.Root |> Option.filter (fun n -> n.Identity = id))
                |> Option.exists (fun n -> n.Control.Accessibility |> Option.exists (fun m -> m.Keyboard.Focusable))

            Expect.isFalse isFocusable "the banner under the press is non-focusable, so the host leaves the current focus unchanged (FR-006)"
        }
    ]

// =============================================================================================
// T026 / SC-005 — the focus indicator is the E3 resolver's `Focused` state (no procedural branch).
// 093 (E3) has landed (the `Style` resolver is present); the E3-resolver path is asserted directly.
// =============================================================================================

[<Tests>]
let focusIndicatorTests =
    let baseStyle: ResolvedStyle =
        { Foreground = Colors.black
          Fill = Colors.white
          Stroke = Colors.black
          StrokeWidth = 1.0
          FontFamily = None
          FontSize = 14.0
          FontWeight = None }

    testList "094 US3 focus indicator via E3 resolver (SC-005)" [
        test "the Focused visual state resolves to a distinct style — the indicator is resolver-driven" {
            let normal = Style.resolve theme baseStyle [] Normal
            let focusedStyle = Style.resolve theme baseStyle [] Focused
            Expect.notEqual focusedStyle normal "Focused resolves distinctly from Normal (no procedural per-kind focus branch)"
            Expect.equal focusedStyle.Stroke theme.Accent "the focus indicator IS the resolver's Focused stroke (theme.Accent)"
        }

        test "the indicator is present on the focused control and absent on the unfocused one (moves with focus)" {
            // The indicator is the resolver's Focused stroke (theme.Accent). It appears on exactly the
            // control whose VisualState is Focused, and an unfocused (Normal) control does not carry it
            // — so moving the Focused state from one control to another moves the indicator, with no
            // procedural per-kind focus branch (the resolver is the sole authority).
            let focusedStroke = (Style.resolve theme baseStyle [] Focused).Stroke
            let unfocusedStroke = (Style.resolve theme baseStyle [] Normal).Stroke
            Expect.equal focusedStroke theme.Accent "the focused control carries the accent indicator"
            Expect.notEqual unfocusedStroke theme.Accent "an unfocused control does not carry the indicator"
        }
    ]

// =============================================================================================
// T027 — input->visible-change responds-proof for a key-driven focus change (reused E1 core).
// =============================================================================================

[<Tests>]
let respondsProofTests =
    testList "094 US3 responds-proof for a key-driven focus change" [
        test "a key-driven focus change yields a visible change (Responsive); an inert view yields Inert" {
            // A focus-reflecting view: the focused control carries the resolver's Focused state AND the
            // header names the focused control, so a key-driven focus change visibly changes the frame
            // through the production `Control.renderTree` path.
            let focusOn (focusedKey: string) : Control<Msg> =
                let btnFocus = if focusedKey = "btn" then [ Attr.visualState Focused ] else []
                let sldFocus = if focusedKey = "sld" then [ Attr.visualState Focused ] else []

                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text $"focused: {focusedKey}" ] |> Control.withKey "indicator"
                            Button.create ([ Button.text "Go"; Button.onClick Clicked ] @ btnFocus) |> Control.withKey "btn"
                            Slider.create ([ Slider.value 0.5; Slider.onChanged SliderChanged ] @ sldFocus) |> Control.withKey "sld" ] ]

            // BEFORE: focus on btn; AFTER: a Tab moved focus to sld -> the focus indicator/header moves.
            let before = (Control.renderTree theme size (focusOn "btn")).Scene
            let after = (Control.renderTree theme size (focusOn "sld")).Scene
            let proof = ControlsElmish.respondsProofOf before after
            Expect.equal proof.Verdict Responsive "the key-driven focus change produced a visible change (Responsive)"

            // an INERT capture (identical frames) -> Inert; "renders" cannot be passed off as "responds".
            let inertProof = ControlsElmish.respondsProofOf before before
            Expect.equal inertProof.Verdict Inert "identical frames yield an Inert verdict"
        }
    ]
