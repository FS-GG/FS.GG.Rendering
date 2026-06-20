module Feature175HoverFocusTests

// Feature 175 (US2, FR-003/FR-004/FR-005) — live hover/focus feedback on interactive controls,
// driven through the REAL bridge (ControlRuntime.applyRuntimeVisualState) + Control.renderTree,
// exactly as the host applies it. Failing-first: a focused BUTTON produced no visible change
// (buttonGeom ignored the stroke), and a hovered+focused control collapsed to Focused (the hover
// fill was suppressed) — both fixed by this feature.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

let private theme = Theme.light
let private size: Size = { Width = 320; Height = 120 }

let private empty = fst (ControlRuntime.init ())
let private hoverOf id = { empty with HoveredControl = Some id }
let private focusOf id = { empty with FocusedControl = Some id }
let private hoverFocusOf id = { empty with HoveredControl = Some id; FocusedControl = Some id }

let private sceneOf (m: ControlRuntimeModel) (c: Control<'msg>) =
    (Control.renderTree theme size (ControlRuntime.applyRuntimeVisualState m c)).Scene

let private ghostButton: Control<int> = Button.create [ Button.text "Nav"; Attr.styleClasses [ StyleClass.Custom "ghost" ] ] |> Control.withKey "g"
let private plainButton: Control<int> = Button.create [ Button.text "Go" ] |> Control.withKey "b"
let private textBox: Control<int> = TextBox.create [ TextBox.value "x" ] |> Control.withKey "t"
let private displayLabel: Control<int> = TextBlock.create [ TextBlock.text "static" ] |> Control.withKey "d"

[<Tests>]
let tests =
    testList "Feature175HoverFocus" [
        // ---- T021: every interactive kind shows a visible hover AND a distinct focus -------------
        test "a default button shows a visible hover state (F-009: lightened fill, was a no-op)" {
            // A default button's resting fill is already Accent; hover lightens it so the state is visible.
            Expect.notEqual (sceneOf (hoverOf "b") plainButton) (sceneOf empty plainButton) "hover restyles a default button"
        }

        test "a button shows a visible FOCUS state (the pre-175 gap: buttonGeom ignored the stroke)" {
            let rest = sceneOf empty plainButton
            let focused = sceneOf (focusOf "b") plainButton
            Expect.notEqual focused rest "focus is now visible on a button (focus ring)"
            Expect.notEqual focused (sceneOf (hoverOf "b") plainButton) "focus is distinct from hover"
        }

        test "the ghost nav button shows both hover and a distinct focus" {
            let rest = sceneOf empty ghostButton
            Expect.notEqual (sceneOf (hoverOf "g") ghostButton) rest "ghost hover restyles"
            Expect.notEqual (sceneOf (focusOf "g") ghostButton) rest "ghost focus restyles (focus ring)"
        }

        test "a text-box shows a visible focus state" {
            Expect.notEqual (sceneOf (focusOf "t") textBox) (sceneOf empty textBox) "text-box focus restyles its border"
        }

        // ---- T022: combined hover+focus shows BOTH; display-only stays static ---------------------
        test "combined hover+focus shows both affordances (neither suppresses the other)" {
            // Ghost button: transparent resting fill, so hover (Accent fill) and focus (Accent ring) are
            // independently visible, and the combined state shows BOTH (fill + ring).
            let hover = sceneOf (hoverOf "g") ghostButton
            let focus = sceneOf (focusOf "g") ghostButton
            let both = sceneOf (hoverFocusOf "g") ghostButton
            Expect.notEqual both hover "combined differs from hover-only (focus ring added)"
            Expect.notEqual both focus "combined differs from focus-only (hover fill retained)"
        }

        test "deriveVisualState yields the combined state when both hovered and focused" {
            Expect.equal (ControlRuntime.deriveVisualState (hoverFocusOf "b") "b") FocusedHover "both → FocusedHover (not just Focused)"
            Expect.equal (ControlRuntime.deriveVisualState (focusOf "b") "b") Focused "focus only → Focused"
            Expect.equal (ControlRuntime.deriveVisualState (hoverOf "b") "b") Hover "hover only → Hover"
        }

        test "a display-only control stays static under a hover+focus model" {
            // A display-only kind named by the interaction model must not restyle (FR-008): the bridge
            // only stamps when the derived state is non-Normal; a text-block resolves Normal regardless.
            Expect.equal (sceneOf (hoverFocusOf "d") displayLabel) (sceneOf empty displayLabel) "text-block is inert under input"
        }

        // ---- T023: a hover/focus change re-stamps damage-locally (no whole-tree rebuild per move) --
        test "a hover change re-stamps damage-locally — not the whole tree" {
            let tree: Control<int> = Stack.create [ Stack.children [ plainButton; ghostButton ] ]
            let stamp0 = ControlRuntime.runtimeStampFor None empty tree
            let stamp1 = ControlRuntime.runtimeStampFor (Some(empty, stamp0.Stamped)) (hoverOf "g") tree
            Expect.isGreaterThan stamp1.RuntimeStateTouchedNodeCount 0 "the hover change re-stamped the hovered control"
            Expect.isLessThan stamp1.RuntimeStateTouchedNodeCount (Control.count tree) "the re-stamp is damage-local (fewer than all nodes touched)"
        }
    ]
