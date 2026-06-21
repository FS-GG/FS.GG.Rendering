module ControlsTypedLoweringTests

open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// File-private abbreviations for the legacy builders. Several control names
// (Button, TextBox, CheckBox, …) are also `AccessibilityRole` union cases, so a
// bare `LCheckBox.create` path can resolve to the case rather
// than the module; a module abbreviation forces module resolution.
module LTextBlock = FS.GG.UI.Controls.TextBlock
module LButton = FS.GG.UI.Controls.Button
module LCheckBox = FS.GG.UI.Controls.CheckBox
module LStack = FS.GG.UI.Controls.Stack
module LTextBox = FS.GG.UI.Controls.TextBox
module LTextInput = FS.GG.UI.Controls.TextInput
module LDataGrid = FS.GG.UI.Controls.DataGrid
module LControl = FS.GG.UI.Controls.Control
module LBadge = FS.GG.UI.Controls.Badge
// Feature 106 (T008): the additional starter-demonstrated typed controls.
module LRichText = FS.GG.UI.Controls.RichText
module LLineChart = FS.GG.UI.Controls.LineChart
module LGraphView = FS.GG.UI.Controls.GraphView

// Msg used to bind the typed authoring surface; needs equality for parity.
type Msg =
    | Save
    | Toggled of bool
    | SelectionChanged of string list

// Canonical event used to normalize `EventValue` closures into a comparable
// `MessageValue`. F# functions have no structural equality, so the keystone
// parity comparison replaces every `EventValue f` with the message `f` produces
// for this representative event — proving event-binding parity (FR-008) by
// behavior rather than by closure identity.
let sampleEvent: ControlEvent =
    { Kind = "sample"
      ControlId = None
      Origin = ControlEventOrigin.Pointer
      Nav = Some(EditedText "true") }

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

// `Control<'msg>` is not an equality type (its `AttrValue` DU carries a function
// case), so structural parity is compared through a deterministic `%A` projection
// of the order-normalized, event-canonicalized control.
let show (control: Control<'msg>) = sprintf "%A" (normControl control)

let parityEqual (typed: Widget<'msg>) (legacy: Control<'msg>) message =
    Expect.equal (show (Widget.toControl typed)) (show legacy) message

let eventAttrs (control: Control<'msg>) =
    control.Attributes |> List.filter (fun attr -> attr.Category = Event)

[<Tests>]
let typedLoweringTests =
    testList "Typed controls lowering parity (keystone, six controls)" [
        // --- US1 primitives -------------------------------------------------
        test "TextBlock lowers structurally equal to legacy TextBlock.create" {
            let typed = TextBlock.view { TextBlock.defaults with Text = "Sign in" }

            let legacy =
                LTextBlock.create [ LTextBlock.text "Sign in" ]

            parityEqual typed legacy "TextBlock view lowers to legacy IR"
        }

        test "Button lowers equal to legacy and OnClick=Some binds identically" {
            let typed =
                Button.view
                    { Button.defaults with
                        Text = "Submit"
                        OnClick = Some Save }

            let legacy =
                LButton.create
                    [ LButton.text "Submit"
                      LButton.enabled true
                      Attr.style "primary"
                      LButton.onClick Save ]

            parityEqual typed legacy "Button view lowers to legacy IR"
        }

        test "Button OnClick=None lowers to no event binding" {
            let typed = Button.view { Button.defaults with Text = "Submit" }
            Expect.isEmpty (eventAttrs (Widget.toControl typed)) "unset OnClick produces no event binding"
        }

        test "Button Intent lowers to the legacy variant style attribute" {
            let typed = Button.view { Button.defaults with Text = "Delete"; Intent = Danger }

            let legacy =
                LButton.create
                    [ LButton.text "Delete"
                      LButton.enabled true
                      Attr.style "danger" ]

            parityEqual typed legacy "Danger intent lowers to style \"danger\""
        }

        test "CheckBox lowers equal to legacy and OnChanged payload maps identically" {
            let typed =
                CheckBox.view
                    { CheckBox.defaults with
                        Text = "Agree"
                        Checked = true
                        OnChanged = Some Toggled }

            let legacy =
                LCheckBox.create
                    [ LCheckBox.text "Agree"
                      LCheckBox.checked' true
                      LCheckBox.onChanged Toggled ]

            parityEqual typed legacy "CheckBox view lowers to legacy IR"
        }

        test "CheckBox OnChanged=None lowers to no event binding" {
            let typed = CheckBox.view { CheckBox.defaults with Text = "Agree"; Checked = true }
            Expect.isEmpty (eventAttrs (Widget.toControl typed)) "unset OnChanged produces no event binding"
        }

        // --- US2 composition ------------------------------------------------
        test "Stack lowers equal to legacy with children in author order" {
            let childA = TextBlock.view { TextBlock.defaults with Text = "A" }
            let childB = Button.view { Button.defaults with Text = "B"; OnClick = Some Save }

            let typed =
                Stack.view
                    { Stack.defaults with
                        Orientation = Vertical
                        Spacing = 8.0
                        Children = [ childA; childB ] }

            let legacy =
                LStack.create
                    [ Attr.create "orientation" Layout (TextValue "vertical")
                      Attr.create "spacing" Layout (FloatValue 8.0)
                      LStack.children [ Widget.toControl childA; Widget.toControl childB ] ]

            parityEqual typed legacy "Stack view lowers to legacy IR"

            let order = (Widget.toControl typed).Children |> List.map (fun child -> child.Content)
            Expect.equal order [ Some "A"; Some "B" ] "typed children lower in author order"
        }

        test "Widget.ofControl bridges a legacy control and round-trips unchanged" {
            let legacy = LBadge.create [ LBadge.text "beta" ]
            Expect.equal (sprintf "%A" (Widget.toControl (Widget.ofControl legacy))) (sprintf "%A" legacy) "toControl (ofControl c) = c"

            let typed =
                Stack.view
                    { Stack.defaults with
                        Children =
                            [ Widget.ofControl legacy
                              TextBlock.view { TextBlock.defaults with Text = "Welcome" } ] }

            let children = (Widget.toControl typed).Children
            Expect.equal children.Length 2 "both children present"
            Expect.equal (sprintf "%A" children.[0]) (sprintf "%A" legacy) "bridged legacy control is unchanged inside typed children"
        }

        // --- US3 stateful controls (MVU delegation + view parity) -----------
        test "TextBox init/update delegate to TextInput and view lowers to legacy IR" {
            let props: TextBoxProps<Msg> = { TextBox.defaults "email" with Value = "Ada" }

            let model, effects = TextBox.init props
            let legacyModel, legacyEffects = LTextInput.init "email" SingleLine "Ada"
            Expect.equal model legacyModel "TextBox.init model equals TextInput.init"
            Expect.equal effects legacyEffects "TextBox.init effects equal TextInput.init"

            let model', effects' = TextBox.update (InsertText "x") model
            let legacyModel', legacyEffects' = LTextInput.update (InsertText "x") legacyModel
            Expect.equal model' legacyModel' "TextBox.update model equals TextInput.update"
            Expect.equal effects' legacyEffects' "TextBox.update effects equal TextInput.update"

            let typed = TextBox.view props model'

            let legacy =
                LTextBox.create
                    [ LTextBox.value model'.DraftText
                      LTextBox.readOnly false
                      LTextBox.validation model'.Validation ]
                |> LControl.withKey "email"

            parityEqual typed legacy "TextBox view lowers to legacy IR for current model"
        }

        test "DataGrid init/update delegate to DataGrid and view lowers to legacy IR" {
            let columns = [ { Key = "name"; Header = "Name"; Width = 120.0; ColumnType = TextColumn } ]

            let rows =
                [ { Key = "row-1"
                    Cells = [ { RowKey = "row-1"; ColumnKey = "name"; Value = "Ada" } ] } ]

            let props: DataGridProps<Msg> =
                { DataGrid.defaults "grid" with
                    Columns = columns
                    Rows = rows
                    RowHeight = 24.0
                    ViewportHeight = 240.0 }

            let model, effects = DataGrid.init props
            let legacyModel, legacyEffects = LDataGrid.init "grid" columns rows.Length 24.0 240.0
            Expect.equal model legacyModel "DataGrid.init model equals DataGrid.init"
            Expect.equal effects legacyEffects "DataGrid.init effects equal DataGrid.init"

            let model', effects' = DataGrid.update (SelectRow "row-1") model
            let legacyModel', legacyEffects' = LDataGrid.update (SelectRow "row-1") legacyModel
            Expect.equal model' legacyModel' "DataGrid.update model equals DataGrid.update"
            Expect.equal effects' legacyEffects' "DataGrid.update effects equal DataGrid.update"

            let typed = DataGrid.view props model'

            let legacy =
                LDataGrid.create
                    columns
                    [ LDataGrid.rows rows
                      LDataGrid.visibleRange model'.VisibleRange
                      LDataGrid.selectedRows model'.SelectedRows
                      LDataGrid.focusedCell model'.FocusedCell ]
                |> LControl.withKey "grid"

            parityEqual typed legacy "DataGrid view lowers to legacy IR for current model"
        }

        // --- Feature 106 (T008): the remaining starter-demonstrated typed controls ----------
        test "RichText view lowers to legacy RichText.create for its runs" {
            let style = LRichText.defaultStyle Theme.light
            let runs = [ LRichText.run "Hello " style; LRichText.run "world" style ]
            let typed = RichText.view { RichText.defaults with Runs = runs }
            let legacy = LRichText.create (LRichText.block runs) []
            parityEqual typed legacy "RichText view lowers to legacy IR"
        }

        test "LineChart view lowers to legacy LineChart.create for its series" {
            let series = [ { Name = "Revenue"; Points = [ { X = 0.0; Y = 1.0; Label = None } ] } ]
            let typed = LineChart.view { LineChart.defaults with Series = series }
            let legacy = LLineChart.create [ LLineChart.series series ]
            parityEqual typed legacy "LineChart view lowers to legacy IR"
        }

        test "GraphView view lowers to legacy GraphView.create for its nodes" {
            let nodes = [ "form"; "chart"; "grid" ]
            let typed = GraphView.view { GraphView.defaults with Nodes = nodes }
            let legacy = LGraphView.create [ LGraphView.nodes nodes ]
            parityEqual typed legacy "GraphView view lowers to legacy IR"
        }
    ]
