module ControlsInteractionTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed

type Msg =
    | Save of int
    | Changed of string

let click id =
    { Kind = "click"; ControlId = Some id; Origin = ControlEventOrigin.Pointer; Nav = None }

[<Tests>]
let interactionTests =
    testList "Controls interaction dispatch" [
        test "pointer activation dispatches exactly one current-view message" {
            let view value =
                Button.create [
                    Button.text "Save"
                    Button.onClick (Save value)
                ]
                |> Control.withKey "save-button"

            Expect.equal (Control.dispatch (click "save-button") (view 1)) [ Save 1 ] "first view dispatches first model value"
            Expect.equal (Control.dispatch (click "save-button") (view 2)) [ Save 2 ] "re-rendered view dispatches current model value"
        }

        test "disabled and read-only controls suppress disallowed dispatch" {
            let disabled =
                Button.create [
                    Button.text "Save"
                    Button.enabled false
                    Button.onClick (Save 1)
                ]
                |> Control.withKey "save-button"

            let readOnly =
                TextBox.create [
                    TextBox.value "Ada"
                    TextBox.readOnly true
                    TextBox.onChanged Changed
                ]
                |> Control.withKey "name"

            Expect.equal (Control.dispatch (click "save-button") disabled) [] "disabled button suppresses click"

            let changed =
                { Kind = "changed"; ControlId = Some "name"; Origin = ControlEventOrigin.Text; Nav = Some(EditedText "Grace") }

            Expect.equal (Control.dispatch changed readOnly) [] "read-only text box suppresses change"
        }

        test "keyboard activation uses the same message-oriented event path" {
            let button =
                Button.create [ Button.text "Save"; Button.onClick (Save 7) ]
                |> Control.withKey "save-button"

            let key =
                { Kind = "click"; ControlId = Some "save-button"; Origin = ControlEventOrigin.Keyboard; Nav = None }

            Expect.equal (Control.dispatch key button) [ Save 7 ] "keyboard activation dispatches through current event binding"
        }
    ]

[<Tests>]
let typedInteractionTests =
    testList "Typed controls interaction dispatch" [
        test "typed Button OnClick dispatches the same message as legacy onClick" {
            let typed =
                FS.GG.UI.Controls.Typed.Button.view
                    { FS.GG.UI.Controls.Typed.Button.defaults with
                        Id = Some "save-button"
                        Text = "Save"
                        OnClick = Some(Save 1) }
                |> Widget.toControl

            Expect.equal (Control.dispatch (click "save-button") typed) [ Save 1 ] "typed Button binds identically to Button.onClick"
        }

        test "typed Button with disabled state suppresses dispatch" {
            let disabled =
                FS.GG.UI.Controls.Typed.Button.view
                    { FS.GG.UI.Controls.Typed.Button.defaults with
                        Id = Some "save-button"
                        Enabled = false
                        OnClick = Some(Save 1) }
                |> Widget.toControl

            Expect.equal (Control.dispatch (click "save-button") disabled) [] "typed disabled button suppresses click"
        }

        test "typed Button without OnClick dispatches nothing" {
            let noHandler =
                FS.GG.UI.Controls.Typed.Button.view
                    { FS.GG.UI.Controls.Typed.Button.defaults with
                        Id = Some "save-button"
                        Text = "Save" }
                |> Widget.toControl

            Expect.equal (Control.dispatch (click "save-button") noHandler) [] "unset OnClick yields no dispatch"
        }

        test "typed CheckBox OnChanged maps payload identically to legacy onChanged" {
            let typed =
                FS.GG.UI.Controls.Typed.CheckBox.view
                    { FS.GG.UI.Controls.Typed.CheckBox.defaults with
                        Id = Some "agree"
                        OnChanged = Some(fun isChecked -> Changed(if isChecked then "on" else "off")) }
                |> Widget.toControl

            let changed =
                { Kind = "changed"; ControlId = Some "agree"; Origin = ControlEventOrigin.Text; Nav = Some(SteppedValue 1.0) }

            Expect.equal (Control.dispatch changed typed) [ Changed "on" ] "typed CheckBox maps the boolean payload identically"
        }
    ]

// ---------------------------------------------------------------------------
// Feature 072 — the new controls are stateless from the framework's view
// (values are product-owned in Props), so the MVU evidence obligation is met by
// these per-control callback-dispatch assertions (T013 DatePicker, T025 others).
// ---------------------------------------------------------------------------
type ExpansionMsg =
    | Clicked
    | KeyPicked of string
    | ToggledTo of bool
    | DatePicked of DateOnly
    | TimePicked of TimeOnly
    | ColorPicked of ColorSwatch

let private clickAt id =
    { Kind = "click"; ControlId = Some id; Origin = ControlEventOrigin.Pointer; Nav = None }

let private selectedWith payload =
    { Kind = "selected"; ControlId = None; Origin = ControlEventOrigin.Selection; Nav = Some(MovedSelection(0, Some payload)) }

let private eventAttrs (control: Control<'msg>) =
    let rec all (c: Control<'msg>) =
        c :: (c.Children |> List.collect all)
        @ (c.Attributes
           |> List.collect (fun a ->
               match a.Value with
               | ChildValue ch -> all ch
               | ChildrenValue chs -> chs |> List.collect all
               | _ -> []))
    all control |> List.collect (fun c -> c.Attributes |> List.filter (fun a -> a.Category = Event))

[<Tests>]
let typedExpansionInteractionTests =
    testList "Feature 072 typed expansion interaction dispatch" [
        test "DatePicker selecting a day dispatches OnChange carrying the chosen DateOnly (T013)" {
            let picker =
                DatePicker.view
                    { DatePicker.defaults with
                        Value = Some(DateOnly(2026, 6, 15))
                        IsOpen = true
                        OnChange = Some DatePicked }
                |> Widget.toControl

            Expect.equal
                (Control.dispatch (clickAt "day-20") picker)
                [ DatePicked(DateOnly(2026, 6, 20)) ]
                "clicking a day dispatches OnChange with that date"
        }

        test "DatePicker with no value renders an empty calendar and dispatches nothing (T013 edge)" {
            let picker =
                DatePicker.view { DatePicker.defaults with OnChange = Some DatePicked }
                |> Widget.toControl

            Expect.equal (Control.dispatch (clickAt "day-1") picker) [] "no selection => no day buttons => no dispatch"
        }

        test "DatePicker with OnChange None lowers day buttons to no binding (T013)" {
            let picker =
                DatePicker.view { DatePicker.defaults with Value = Some(DateOnly(2026, 6, 15)); IsOpen = true }
                |> Widget.toControl

            Expect.isEmpty (eventAttrs picker) "OnChange None => no event binding anywhere"
        }

        test "ToggleButton dispatches OnToggle with the next state (T025)" {
            let off =
                ToggleButton.view { ToggleButton.defaults with Id = Some "t"; IsOn = false; OnToggle = Some ToggledTo }
                |> Widget.toControl

            Expect.equal (Control.dispatch (clickAt "t") off) [ ToggledTo true ] "toggling from off dispatches true"

            let on =
                ToggleButton.view { ToggleButton.defaults with Id = Some "t"; IsOn = true; OnToggle = Some ToggledTo }
                |> Widget.toControl

            Expect.equal (Control.dispatch (clickAt "t") on) [ ToggledTo false ] "toggling from on dispatches false"

            let noHandler =
                ToggleButton.view { ToggleButton.defaults with Id = Some "t"; IsOn = false }
                |> Widget.toControl

            Expect.equal (Control.dispatch (clickAt "t") noHandler) [] "OnToggle None dispatches nothing"
        }

        test "SplitButton dispatches OnClick for the primary action and OnSelected for a menu item (T025)" {
            let split =
                SplitButton.view
                    { SplitButton.defaults with
                        Id = Some "s"
                        Text = "Save"
                        IsOpen = true
                        Items = [ { Key = "cut"; Label = "Cut" }; { Key = "copy"; Label = "Copy" } ]
                        OnClick = Some Clicked
                        OnSelected = Some KeyPicked }
                |> Widget.toControl

            let clickAnywhere = { Kind = "click"; ControlId = None; Origin = ControlEventOrigin.Pointer; Nav = None }
            Expect.equal (Control.dispatch clickAnywhere split) [ Clicked ] "primary action dispatches OnClick"
            Expect.equal (Control.dispatch (selectedWith "copy") split) [ KeyPicked "copy" ] "menu item dispatches OnSelected with its key"
        }

        test "SplitButton with empty Items lowers to an empty menu without failing (T025 edge)" {
            let split =
                SplitButton.view { SplitButton.defaults with Text = "Save" }
                |> Widget.toControl

            Expect.equal split.Kind "toolbar" "empty split-button still lowers"
            Expect.isEmpty (Control.dispatch (selectedWith "x") split) "empty menu dispatches nothing"
        }

        test "TimePicker segments dispatch OnChange with the adjusted time (T025)" {
            let picker =
                TimePicker.view { TimePicker.defaults with Value = Some(TimeOnly(10, 30)); OnChange = Some TimePicked }
                |> Widget.toControl

            Expect.equal (Control.dispatch (clickAt "hour-segment") picker) [ TimePicked(TimeOnly(11, 30)) ] "hour segment advances the hour"
            Expect.equal (Control.dispatch (clickAt "minute-segment") picker) [ TimePicked(TimeOnly(10, 31)) ] "minute segment advances the minute"
        }

        test "ColorPicker dispatches OnSelected with the chosen swatch (T025)" {
            let red = { Name = "Red"; Color = { Red = 255uy; Green = 0uy; Blue = 0uy; Alpha = 255uy } }
            let blue = { Name = "Blue"; Color = { Red = 0uy; Green = 0uy; Blue = 255uy; Alpha = 255uy } }

            let picker =
                ColorPicker.view
                    { ColorPicker.defaults with Swatches = [ red; blue ]; Selected = Some red; OnSelected = Some ColorPicked }
                |> Widget.toControl

            Expect.equal (Control.dispatch (clickAt "swatch-Blue") picker) [ ColorPicked blue ] "clicking a swatch dispatches OnSelected with it"

            let empty = ColorPicker.view ColorPicker.defaults |> Widget.toControl
            Expect.isEmpty (Control.dispatch (clickAt "swatch-Red") empty) "empty palette dispatches nothing"
        }
    ]
