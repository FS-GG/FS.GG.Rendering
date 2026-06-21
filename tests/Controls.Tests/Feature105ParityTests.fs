module ControlsFeature105ParityTests

// Feature 105 (US1/US4, SC-006): the parity guard for the behaviour-preserving
// consolidation of the lowering helpers. It pins the shared `WidgetLowering`
// helpers and the collapsed `onChanged` adapters to their pre-change behaviour, so
// it goes RED only if a consolidation perturbs key application, an event-kind
// string, a payload parse, or the accessibility metadata. Green by construction
// for this refactor (the helper bodies are the verbatim originals, single-sourced).

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed

// Several legacy builder modules (Button, CheckBox, Switch, Slider, TextBox, …) share
// a name with an `AccessibilityRole` union case, so a bare `FS.GG.UI.Controls.X.member`
// path can resolve to the case. Module abbreviations force module resolution.
module LControl = FS.GG.UI.Controls.Control
module LButton = FS.GG.UI.Controls.Button
module LCheckBox = FS.GG.UI.Controls.CheckBox
module LSwitch = FS.GG.UI.Controls.Switch
module LSlider = FS.GG.UI.Controls.Slider
module LNumericInput = FS.GG.UI.Controls.NumericInput
module LTextBox = FS.GG.UI.Controls.TextBox
module LTextArea = FS.GG.UI.Controls.TextArea
module LRadioGroup = FS.GG.UI.Controls.RadioGroup
module LTabs = FS.GG.UI.Controls.Tabs

type Msg =
    | Bool of bool
    | Num of float
    | Str of string
    | Strs of string list

// Feature 184 (US3): the adapters now read the typed `Nav` outcome, so the parity driver feeds a
// `NavPayload option` instead of the retired stringly `Payload`.
let private eventWith (nav: NavPayload option) : ControlEvent =
    { Kind = "sample"
      ControlId = None
      Origin = ControlEventOrigin.Pointer
      Nav = nav }

// Pull the bound event function out of an event attribute, then run it for a typed outcome.
let private runEvent (nav: NavPayload option) (attr: Attr<'msg>) : 'msg =
    match attr.Value with
    | EventValue f -> f (eventWith nav)
    | _ -> failwithf "expected an event attribute, got %A" attr.Value

let private repr (x: 'a) = sprintf "%A" x

[<Tests>]
let feature105ParityTests =
    testList "Feature 105 lowering-helper parity (SC-006)" [
        // --- WidgetLowering.withKeyOpt (FR-001) -----------------------------
        test "withKeyOpt Some applies a stable key identically to Control.withKey" {
            let c = LButton.create [ LButton.text "x" ]

            Expect.equal
                (repr (WidgetLowering.withKeyOpt (Some "k") c))
                (repr (LControl.withKey "k" c))
                "withKeyOpt (Some k) == Control.withKey k"
        }

        test "withKeyOpt None passes the control through unchanged" {
            let c = LButton.create [ LButton.text "x" ]
            Expect.equal (repr (WidgetLowering.withKeyOpt None c)) (repr c) "withKeyOpt None == identity"
        }

        // --- WidgetLowering.onString / onStringList (FR-002) ----------------
        test "onString binds the event kind and defaults an absent outcome to empty" {
            let attr: Attr<Msg> = WidgetLowering.onString "onSelected" Str
            Expect.equal attr.Name "onSelected" "binds the requested event kind"
            Expect.equal attr.Category Event "is an event attribute"
            Expect.equal (runEvent (Some(EditedText "hi")) attr) (Str "hi") "edited-text outcome passes through"
            Expect.equal (runEvent (Some(MovedSelection(0, Some "hi"))) attr) (Str "hi") "selection item passes through"
            Expect.equal (runEvent None attr) (Str "") "absent outcome defaults to empty string"
        }

        test "onStringList lifts a single typed outcome to a one-element list" {
            let attr: Attr<Msg> = WidgetLowering.onStringList "onChanged" Strs
            Expect.equal (runEvent (Some(EditedText "a")) attr) (Strs [ "a" ]) "single outcome => one-element list"
            Expect.equal (runEvent None attr) (Strs []) "absent outcome => empty list"
        }

        // --- WidgetLowering.a11y (FR-004/FR-009) ----------------------------
        test "a11y builds the documented role + keyboard accessibility metadata" {
            let typed: Attr<Msg> = WidgetLowering.a11y AccessibilityRole.Button "Save" [ "ArrowDown" ]

            let inline' : Attr<Msg> =
                Attr.accessibility (
                    Accessibility.metadata
                        AccessibilityRole.Button
                        "Save"
                        [ "normal" ]
                        None
                        (Accessibility.keyboard true [ "Enter"; "Space" ] [ "ArrowDown" ])
                        None
                        None)

            Expect.equal (repr typed) (repr inline') "a11y == the inline accessibility metadata"
        }

        // --- onChanged adapters in Control.fs (FR-003) ----------------------
        // Feature 184 (US3): a boolean toggle reports its new state as `SteppedValue 1.0/0.0`.
        test "bool onChanged reads the typed stepped value (CheckBox, Switch)" {
            let attr: Attr<Msg> = LCheckBox.onChanged Bool
            Expect.equal (runEvent (Some(SteppedValue 1.0)) attr) (Bool true) "1.0 => true"
            Expect.equal (runEvent (Some(SteppedValue 0.0)) attr) (Bool false) "0.0 => false"
            Expect.equal (runEvent None attr) (Bool false) "absent => false"

            let switchAttr: Attr<Msg> = LSwitch.onChanged Bool
            Expect.equal (runEvent (Some(SteppedValue 1.0)) switchAttr) (Bool true) "Switch 1.0 => true"
        }

        test "float onChanged reads the typed stepped value and falls back to 0.0 (Slider, NumericInput)" {
            let attr: Attr<Msg> = LSlider.onChanged Num
            Expect.equal (runEvent (Some(SteppedValue 1.5)) attr) (Num 1.5) "stepped value passes"
            Expect.equal (runEvent None attr) (Num 0.0) "absent => 0.0"

            let numericAttr: Attr<Msg> = LNumericInput.onChanged Num
            Expect.equal (runEvent (Some(SteppedValue 2.25)) numericAttr) (Num 2.25) "NumericInput reads stepped value"
        }

        test "string onChanged reads the typed text outcome (TextBox, TextArea, RadioGroup, Tabs)" {
            for attr in
                [ LTextBox.onChanged Str
                  LTextArea.onChanged Str
                  LRadioGroup.onChanged Str
                  LTabs.onChanged Str ] do
                Expect.equal (runEvent (Some(EditedText "hi")) attr) (Str "hi") "edited text passes through"
                Expect.equal (runEvent None attr) (Str "") "absent => empty string"
        }
    ]
