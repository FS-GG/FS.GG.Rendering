module ControlsTypedExpansionTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// ---------------------------------------------------------------------------
// Feature 072 — catalog breadth expansion (five genuinely new controls spanning
// buttons / pickers / date-time). This file carries the keystone evidence:
//   * contract: every new typed module exists and exposes defaults/view
//   * lowering parity (SC-002): typed `view |> Widget.toControl` ≡ the explicit
//     hand-written composition of existing legacy builders (T012 DatePicker,
//     T024 ToggleButton/SplitButton/TimePicker/ColorPicker).
// Each new control is a COMPOSITION of existing legacy builders — no new
// StandardControlKind variant, no renderer change (FR-004, SC-007).
// ---------------------------------------------------------------------------

// File-private abbreviations for the legacy builders. Several control names
// (Button, Menu, Grid, Border, Overlay, List) are also `AccessibilityRole` union
// cases or typed modules, so a bare path can resolve to the wrong symbol; a module
// abbreviation forces module resolution (the TypedMigrationTests technique).
module LButton = FS.GG.UI.Controls.Button
module LMenu = FS.GG.UI.Controls.Menu
module LToolbar = FS.GG.UI.Controls.Toolbar
module LOverlay = FS.GG.UI.Controls.Overlay
module LGrid = FS.GG.UI.Controls.Grid
module LWrap = FS.GG.UI.Controls.Wrap
module LStack = FS.GG.UI.Controls.Stack
module LTextBox = FS.GG.UI.Controls.TextBox
module LLabel = FS.GG.UI.Controls.Label
module LControl = FS.GG.UI.Controls.Control

type Msg =
    | Save
    | Picked of string
    | Toggled of bool
    | DateChosen of DateOnly
    | TimeChosen of TimeOnly
    | ColorChosen of ColorSwatch

// Order-normalized, event-canonicalized structural projection (the TypedLoweringTests
// technique): F# functions have no structural equality, so every `EventValue f` is
// replaced by the message `f` produces for one sample event.
let sampleEvent: ControlEvent =
    { Kind = "sample"
      ControlId = None
      Origin = ControlEventOrigin.Pointer
      Payload = Some "alpha"
      Nav = None }

let rec normControl (control: Control<'msg>) : Control<'msg> =
    { control with
        Attributes = control.Attributes |> List.map normAttr |> List.sortBy (fun attr -> attr.Name)
        Children = control.Children |> List.map normControl }

and normAttr (attr: Attr<'msg>) : Attr<'msg> =
    match attr.Value with
    | EventValue map -> { attr with Value = MessageValue(map sampleEvent) }
    | ChildValue child -> { attr with Value = ChildValue(normControl child) }
    | ChildrenValue children -> { attr with Value = ChildrenValue(children |> List.map normControl) }
    | _ -> attr

let show (control: Control<'msg>) = sprintf "%A" (normControl control)

let parityEqual (typed: Widget<'msg>) (legacy: Control<'msg>) message =
    Expect.equal (show (Widget.toControl typed)) (show legacy) message

// The exact accessibility-metadata expression the typed views attach to each
// composite root (FR-009). Duplicated here so the parity fixture is explicit.
let a11y (role: AccessibilityRole) (nameSource: string) (navigationKeys: string list) : Attr<'msg> =
    Attr.accessibility (
        Accessibility.metadata
            role
            nameSource
            [ "normal" ]
            None
            (Accessibility.keyboard true [ "Enter"; "Space" ] navigationKeys)
            None
            None)

let private color r g b : Color = { Red = r; Green = g; Blue = b; Alpha = 255uy }

// ---------------------------------------------------------------------------
// Contract — each new module exposes defaults/view and lowers to a composition
// rooted at the expected existing control kind (no new StandardControlKind).
// ---------------------------------------------------------------------------
[<Tests>]
let typedExpansionContractTests =
    testList "Feature 072 typed expansion contract (SC-001, SC-007)" [
        test "the five new typed modules expose defaults/view and lower to existing kinds" {
            let kindOf (w: Widget<_>) = (Widget.toControl w).Kind

            Expect.equal (kindOf (ToggleButton.view ToggleButton.defaults)) "button" "toggle-button lowers to a button"
            Expect.equal (kindOf (SplitButton.view SplitButton.defaults)) "toolbar" "split-button lowers to a toolbar"
            Expect.equal (kindOf (DatePicker.view DatePicker.defaults)) "stack" "date-picker lowers to a stack"
            Expect.equal (kindOf (TimePicker.view TimePicker.defaults)) "stack" "time-picker lowers to a stack"
            Expect.equal (kindOf (ColorPicker.view ColorPicker.defaults)) "wrap" "color-picker lowers to a wrap"
        }

        test "each new control renders without diagnostics for representative props" {
            let widgets : Widget<Msg> list =
                [ ToggleButton.view { ToggleButton.defaults with Text = "Bold"; IsOn = true; OnToggle = Some Toggled }
                  SplitButton.view
                      { SplitButton.defaults with
                          Text = "Save"
                          IsOpen = true
                          Items = [ { Key = "cut"; Label = "Cut" } ]
                          OnClick = Some Save
                          OnSelected = Some Picked }
                  DatePicker.view { DatePicker.defaults with Value = Some(DateOnly(2026, 6, 15)); IsOpen = true; OnChange = Some DateChosen }
                  TimePicker.view { TimePicker.defaults with Value = Some(TimeOnly(10, 30)); OnChange = Some TimeChosen }
                  ColorPicker.view
                      { ColorPicker.defaults with
                          Swatches = [ { Name = "Red"; Color = color 255uy 0uy 0uy } ]
                          OnSelected = Some ColorChosen } ]

            for widget in widgets do
                let rendered = Control.render Theme.light (Widget.toControl widget)
                Expect.isEmpty rendered.Diagnostics "new control renders with no diagnostics"
        }
    ]

// ---------------------------------------------------------------------------
// T012 — DatePicker lowering parity (P1 keystone, SC-002).
// ---------------------------------------------------------------------------
[<Tests>]
let datePickerParityTests =
    testList "Feature 072 DatePicker lowering parity (US1, SC-002)" [
        test "DatePicker lowers structurally equal to its explicit legacy composition" {
            let value = DateOnly(2026, 6, 15)

            let typed =
                DatePicker.view
                    { DatePicker.defaults with
                        Id = Some "d"
                        Value = Some value
                        IsOpen = true
                        OnChange = Some DateChosen }

            let field = LTextBox.create [ LTextBox.value "2026-06-15"; LTextBox.readOnly true ]
            let trigger = LButton.create [ LButton.text "Open calendar"; LButton.enabled true ]

            let dayButtons =
                [ for day in 1..30 ->
                      LButton.create
                          [ LButton.text (string day)
                            LButton.enabled true
                            LButton.onClick (DateChosen(DateOnly(2026, 6, day))) ]
                      |> LControl.withKey (sprintf "day-%d" day) ]

            let calendar = LGrid.create [ LGrid.children dayButtons ]
            let overlay = LOverlay.create [ LOverlay.child calendar; Attr.selected true ]

            let legacy =
                LStack.create
                    [ LStack.children [ field; trigger; overlay ]
                      a11y AccessibilityRole.TextBox "Date picker" [ "ArrowLeft"; "ArrowRight"; "ArrowUp"; "ArrowDown" ] ]
                |> LControl.withKey "d"

            parityEqual typed legacy "date-picker"
        }

        test "DatePicker with no value lowers to an empty field and empty calendar" {
            let typed =
                DatePicker.view { DatePicker.defaults with Id = Some "d"; OnChange = Some DateChosen }

            let field = LTextBox.create [ LTextBox.value ""; LTextBox.readOnly true ]
            let trigger = LButton.create [ LButton.text "Open calendar"; LButton.enabled true ]
            let calendar = LGrid.create [ LGrid.children [] ]
            let overlay = LOverlay.create [ LOverlay.child calendar; Attr.selected false ]

            let legacy =
                LStack.create
                    [ LStack.children [ field; trigger; overlay ]
                      a11y AccessibilityRole.TextBox "Date picker" [ "ArrowLeft"; "ArrowRight"; "ArrowUp"; "ArrowDown" ] ]
                |> LControl.withKey "d"

            parityEqual typed legacy "date-picker empty"
        }
    ]

// ---------------------------------------------------------------------------
// T024 — ToggleButton / SplitButton / TimePicker / ColorPicker parity (US3, SC-002).
// ---------------------------------------------------------------------------
[<Tests>]
let breadthParityTests =
    testList "Feature 072 button/picker lowering parity (US3, SC-002)" [
        test "ToggleButton lowers structurally equal to its explicit legacy composition" {
            let typed =
                ToggleButton.view
                    { ToggleButton.defaults with
                        Id = Some "t"
                        Text = "Bold"
                        IsOn = true
                        OnToggle = Some Toggled }

            let legacy =
                LButton.create
                    [ LButton.text "Bold"
                      LButton.enabled true
                      Attr.selected true
                      LButton.onClick (Toggled false)
                      a11y AccessibilityRole.Button "Toggle button" [ "Tab"; "Shift+Tab" ] ]
                |> LControl.withKey "t"

            parityEqual typed legacy "toggle-button"
        }

        test "SplitButton lowers structurally equal to its explicit legacy composition" {
            let items = [ { Key = "cut"; Label = "Cut" }; { Key = "copy"; Label = "Copy" } ]

            let typed =
                SplitButton.view
                    { SplitButton.defaults with
                        Id = Some "s"
                        Text = "Save"
                        IsOpen = true
                        Items = items
                        OnClick = Some Save
                        OnSelected = Some Picked }

            let primary = LButton.create [ LButton.text "Save"; LButton.enabled true; LButton.onClick Save ]
            let trigger = LButton.create [ LButton.text "More"; LButton.enabled true ]
            let menu = LMenu.create [ LMenu.items [ "Cut"; "Copy" ]; LMenu.onSelected Picked ]
            let overlay = LOverlay.create [ LOverlay.child menu; Attr.selected true ]

            let legacy =
                LToolbar.create
                    [ LToolbar.children [ primary; trigger; overlay ]
                      a11y AccessibilityRole.Menu "Split button" [ "ArrowDown"; "ArrowUp"; "Tab" ] ]
                |> LControl.withKey "s"

            parityEqual typed legacy "split-button"
        }

        test "TimePicker lowers structurally equal to its explicit legacy composition" {
            let value = TimeOnly(10, 30)

            let typed =
                TimePicker.view
                    { TimePicker.defaults with
                        Id = Some "tp"
                        Value = Some value
                        OnChange = Some TimeChosen }

            let hour =
                LButton.create [ LButton.text "10"; LButton.enabled true; LButton.onClick (TimeChosen(value.AddHours 1.0)) ]
                |> LControl.withKey "hour-segment"

            let minute =
                LButton.create [ LButton.text "30"; LButton.enabled true; LButton.onClick (TimeChosen(value.AddMinutes 1.0)) ]
                |> LControl.withKey "minute-segment"

            let separator = LLabel.create [ LLabel.text ":" ]

            let legacy =
                LStack.create
                    [ LStack.children [ hour; separator; minute ]
                      a11y AccessibilityRole.TextBox "Time picker" [ "ArrowUp"; "ArrowDown" ] ]
                |> LControl.withKey "tp"

            parityEqual typed legacy "time-picker"
        }

        test "ColorPicker lowers structurally equal to its explicit legacy composition" {
            let red = { Name = "Red"; Color = color 255uy 0uy 0uy }
            let blue = { Name = "Blue"; Color = color 0uy 0uy 255uy }

            let typed =
                ColorPicker.view
                    { ColorPicker.defaults with
                        Id = Some "c"
                        Swatches = [ red; blue ]
                        Selected = Some red
                        OnSelected = Some ColorChosen }

            let cell (swatch: ColorSwatch) selected =
                LButton.create
                    [ LButton.text swatch.Name
                      Attr.selected selected
                      Attr.create "color" Style (UntypedValue(swatch.Color :> obj))
                      LButton.onClick (ColorChosen swatch) ]
                |> LControl.withKey (sprintf "swatch-%s" swatch.Name)

            let legacy =
                LWrap.create
                    [ LWrap.children [ cell red true; cell blue false ]
                      a11y AccessibilityRole.List "Color picker" [ "ArrowLeft"; "ArrowRight"; "ArrowUp"; "ArrowDown" ] ]
                |> LControl.withKey "c"

            parityEqual typed legacy "color-picker"
        }

        test "empty Items / Swatches lower without failing (edge case)" {
            let split = SplitButton.view { SplitButton.defaults with Text = "Save" } |> Widget.toControl
            Expect.equal split.Kind "toolbar" "empty split-button still lowers to a toolbar"

            let palette = ColorPicker.view ColorPicker.defaults |> Widget.toControl
            Expect.equal palette.Kind "wrap" "empty color-picker still lowers to a wrap"
            Expect.isEmpty palette.Children "empty color-picker has no swatch cells"
        }
    ]
