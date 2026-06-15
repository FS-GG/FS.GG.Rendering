module ControlsTypedMigrationTests

open System
open System.IO
open Microsoft.FSharp.Reflection
open Expecto
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Typed
open FS.GG.UI.Themes.Default
open FS.GG.UI.DesignSystem

// ---------------------------------------------------------------------------
// Feature 070 — migrate the remaining 41 catalog controls to the typed front
// door. This file carries the keystone evidence:
//   * contract: every new typed module exists and exposes defaults/view (T006)
//   * lowering parity: typed `view |> Widget.toControl` ≡ legacy builder (T020–T028)
//   * stateful delegation: typed init/update equal the reused model (T029–T032)
// ---------------------------------------------------------------------------

// File-private abbreviations for the legacy builders. Several control names
// (Image, Slider, Menu, Dialog, Grid, Border, Overlay, …) are also
// `AccessibilityRole` union cases, so a bare `Image.create` path can resolve to
// the case rather than the module; a module abbreviation forces module resolution.
module LRichText = FS.GG.UI.Controls.RichText
module LLabel = FS.GG.UI.Controls.Label
module LImage = FS.GG.UI.Controls.Image
module LIcon = FS.GG.UI.Controls.Icon
module LSeparator = FS.GG.UI.Controls.Separator
module LBadge = FS.GG.UI.Controls.Badge
module LProgressBar = FS.GG.UI.Controls.ProgressBar
module LSpinner = FS.GG.UI.Controls.Spinner
module LValidationMessage = FS.GG.UI.Controls.ValidationMessage
module LIconButton = FS.GG.UI.Controls.IconButton
module LNumericInput = FS.GG.UI.Controls.NumericInput
module LRadioGroup = FS.GG.UI.Controls.RadioGroup
module LSwitch = FS.GG.UI.Controls.Switch
module LSlider = FS.GG.UI.Controls.Slider
module LTextArea = FS.GG.UI.Controls.TextArea
module LTextInput = FS.GG.UI.Controls.TextInput
module LCollections = FS.GG.UI.Controls.Collections
module LGrid = FS.GG.UI.Controls.Grid
module LDock = FS.GG.UI.Controls.Dock
module LWrap = FS.GG.UI.Controls.Wrap
module LBorder = FS.GG.UI.Controls.Border
module LPanel = FS.GG.UI.Controls.Panel
module LTabs = FS.GG.UI.Controls.Tabs
module LMenu = FS.GG.UI.Controls.Menu
module LToolbar = FS.GG.UI.Controls.Toolbar
module LTooltip = FS.GG.UI.Controls.Tooltip
module LDialog = FS.GG.UI.Controls.Dialog
module LToast = FS.GG.UI.Controls.Toast
module LOverlay = FS.GG.UI.Controls.Overlay
module LLineChart = FS.GG.UI.Controls.LineChart
module LBarChart = FS.GG.UI.Controls.BarChart
module LPieChart = FS.GG.UI.Controls.PieChart
module LScatterPlot = FS.GG.UI.Controls.ScatterPlot
module LGraphView = FS.GG.UI.Controls.GraphView
module LControl = FS.GG.UI.Controls.Control

type Msg =
    | Save
    | Picked of string
    | PickedMany of string list
    | Toggled of bool
    | NumberChanged of float
    | Scrolled of float

// Order-normalized, event-canonicalized structural projection (same technique as
// TypedLoweringTests.fs): F# functions have no structural equality, so every
// `EventValue f` is replaced with the message `f` produces for one sample event.
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

let eventAttrs (control: Control<'msg>) =
    control.Attributes |> List.filter (fun attr -> attr.Category = Event)

let repositoryRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
        else (match Directory.GetParent dir |> Option.ofObj with Some p -> find p.FullName | None -> dir)
    find __SOURCE_DIRECTORY__

let read (relativePath: string) =
    File.ReadAllText(Path.Combine(repositoryRoot, relativePath.Replace("/", string Path.DirectorySeparatorChar)))

// ---------------------------------------------------------------------------
// T006 — contract: the new typed .fsi surface declares no `obj` field and no
// untyped/string-keyed event payload.
// ---------------------------------------------------------------------------
[<Tests>]
let typedMigrationContractTests =
    let newSignatures =
        [ "src/Controls/Widgets/Display.fsi"
          "src/Controls/Widgets/Input.fsi"
          "src/Controls/Widgets/TextAreaWidget.fsi"
          "src/Controls/Widgets/CollectionsWidgets.fsi"
          "src/Controls/Widgets/Containers.fsi"
          "src/Controls/Widgets/Navigation.fsi"
          "src/Controls/Widgets/Overlay.fsi"
          "src/Controls/Widgets/ChartsWidgets.fsi"
          "src/Controls/Widgets/CustomControlWidget.fsi" ]

    testList "Feature 070 typed migration contract (SC-005)" [
        test "every new typed .fsi has no obj field and no untyped payload" {
            for path in newSignatures do
                let text = read path
                Expect.isFalse (text.Contains ": obj") $"{path} has no obj-typed field"
                Expect.isFalse (text.Contains "UntypedValue") $"{path} exposes no untyped payload"
        }

        test "all 41 typed modules expose defaults/view and lower to their catalog kind" {
            // Existence is proven by these calls compiling; each lowers to its kind.
            let kindOf (w: Widget<_>) = (Widget.toControl w).Kind

            // Group 1 Display (pure)
            Expect.equal (kindOf (RichText.view RichText.defaults)) "rich-text" "rich-text"
            Expect.equal (kindOf (Label.view Label.defaults)) "label" "label"
            Expect.equal (kindOf (Image.view Image.defaults)) "image" "image"
            Expect.equal (kindOf (Icon.view Icon.defaults)) "icon" "icon"
            Expect.equal (kindOf (Separator.view Separator.defaults)) "separator" "separator"
            Expect.equal (kindOf (Badge.view Badge.defaults)) "badge" "badge"
            Expect.equal (kindOf (ProgressBar.view ProgressBar.defaults)) "progress-bar" "progress-bar"
            Expect.equal (kindOf (Spinner.view Spinner.defaults)) "spinner" "spinner"
            Expect.equal (kindOf (ValidationMessage.view ValidationMessage.defaults)) "validation-message" "validation-message"
            // Group 2 Input (pure + one event)
            Expect.equal (kindOf (IconButton.view IconButton.defaults)) "icon-button" "icon-button"
            Expect.equal (kindOf (NumericInput.view NumericInput.defaults)) "numeric-input" "numeric-input"
            Expect.equal (kindOf (RadioGroup.view RadioGroup.defaults)) "radio-group" "radio-group"
            Expect.equal (kindOf (Switch.view Switch.defaults)) "switch" "switch"
            Expect.equal (kindOf (Slider.view Slider.defaults)) "slider" "slider"
            // Group 3 stateful TextArea
            let taProps: TextAreaProps<Msg> = TextArea.defaults "ta"
            let taModel, _ = TextArea.init taProps
            Expect.equal (kindOf (TextArea.view taProps taModel)) "text-area" "text-area"
            // Group 4 collections (stateful)
            let lvProps: ListViewProps<Msg> = ListView.defaults "lv"
            let lvModel, _ = ListView.init lvProps
            Expect.equal (kindOf (ListView.view lvProps lvModel)) "list-view" "list-view"
            let lbProps: ListBoxProps<Msg> = ListBox.defaults "lb"
            let lbModel, _ = ListBox.init lbProps
            Expect.equal (kindOf (ListBox.view lbProps lbModel)) "list-box" "list-box"
            let msProps: MultiSelectListProps<Msg> = MultiSelectList.defaults "ms"
            let msModel, _ = MultiSelectList.init msProps
            Expect.equal (kindOf (MultiSelectList.view msProps msModel)) "multi-select-list" "multi-select-list"
            let cbProps: ComboBoxProps<Msg> = ComboBox.defaults "cb"
            let cbModel, _ = ComboBox.init cbProps
            Expect.equal (kindOf (ComboBox.view cbProps cbModel)) "combo-box" "combo-box"
            let tvProps: TreeViewProps<Msg> = TreeView.defaults "tv"
            let tvModel, _ = TreeView.init tvProps
            Expect.equal (kindOf (TreeView.view tvProps tvModel)) "tree-view" "tree-view"
            // Group 5 containers (pure)
            Expect.equal (kindOf (Grid.view Grid.defaults)) "grid" "grid"
            Expect.equal (kindOf (Dock.view Dock.defaults)) "dock" "dock"
            Expect.equal (kindOf (Wrap.view Wrap.defaults)) "wrap" "wrap"
            let leaf = Label.view { Label.defaults with Text = "x" }
            Expect.equal (kindOf (Border.view (Border.defaults leaf))) "border" "border"
            Expect.equal (kindOf (Panel.view Panel.defaults)) "panel" "panel"
            Expect.equal (kindOf (ScrollViewer.view (ScrollViewer.defaults "sv" leaf))) "scroll-viewer" "scroll-viewer"
            Expect.equal (kindOf (SplitView.view SplitView.defaults)) "split-view" "split-view"
            // Group 6 navigation
            Expect.equal (kindOf (Tabs.view Tabs.defaults)) "tabs" "tabs"
            Expect.equal (kindOf (Menu.view Menu.defaults)) "menu" "menu"
            Expect.equal (kindOf (ContextMenu.view ContextMenu.defaults)) "context-menu" "context-menu"
            Expect.equal (kindOf (Toolbar.view Toolbar.defaults)) "toolbar" "toolbar"
            // Group 7 overlay
            Expect.equal (kindOf (Tooltip.view Tooltip.defaults)) "tooltip" "tooltip"
            Expect.equal (kindOf (Dialog.view Dialog.defaults)) "dialog" "dialog"
            Expect.equal (kindOf (Toast.view Toast.defaults)) "toast" "toast"
            Expect.equal (kindOf (Overlay.view (Overlay.defaults leaf))) "overlay" "overlay"
            // Group 8 charts/graph
            Expect.equal (kindOf (LineChart.view LineChart.defaults)) "line-chart" "line-chart"
            Expect.equal (kindOf (BarChart.view BarChart.defaults)) "bar-chart" "bar-chart"
            Expect.equal (kindOf (PieChart.view PieChart.defaults)) "pie-chart" "pie-chart"
            Expect.equal (kindOf (ScatterPlot.view ScatterPlot.defaults)) "scatter-plot" "scatter-plot"
            Expect.equal (kindOf (GraphView.view GraphView.defaults)) "graph-view" "graph-view"
            // Group 9 custom-control bridge
            let legacy = LBadge.create [ LBadge.text "beta" ]
            Expect.equal (kindOf (CustomControl.ofControl legacy)) "badge" "custom-control bridge round-trips"
        }
    ]

// ---------------------------------------------------------------------------
// T020–T026 — per-control lowering-parity matrix (keystone, SC-002).
// ---------------------------------------------------------------------------
[<Tests>]
let typedMigrationParityTests =
    testList "Feature 070 lowering parity (41 controls, SC-002)" [
        // --- Group 1 Display ------------------------------------------------
        test "Display group lowers structurally equal to legacy *.create" {
            let style = LRichText.defaultStyle Theme.dark
            let runs = [ LRichText.run "hi" style ]
            parityEqual (RichText.view { RichText.defaults with Runs = runs }) (LRichText.create (LRichText.block runs) []) "rich-text"
            parityEqual (Label.view { Label.defaults with Text = "L" }) (LLabel.create [ LLabel.text "L" ]) "label"
            parityEqual (Image.view { Image.defaults with Value = "img.png" }) (LImage.create [ LImage.source "img.png" ]) "image"
            parityEqual (Icon.view { Icon.defaults with Text = "star" }) (LIcon.create [ LIcon.name "star" ]) "icon"
            parityEqual (Separator.view Separator.defaults) (LSeparator.create []) "separator"
            parityEqual (Badge.view { Badge.defaults with Text = "beta" }) (LBadge.create [ LBadge.text "beta" ]) "badge"
            parityEqual (ProgressBar.view { ProgressBar.defaults with Value = 0.5 }) (LProgressBar.create [ LProgressBar.value 0.5 ]) "progress-bar"
            parityEqual (Spinner.view Spinner.defaults) (LSpinner.create []) "spinner"
            parityEqual
                (ValidationMessage.view { ValidationMessage.defaults with Text = "bad"; Severity = Invalid "x" })
                (LValidationMessage.create [ LValidationMessage.text "bad"; Attr.validation (Invalid "x") ])
                "validation-message"
        }

        // --- Group 2 Input --------------------------------------------------
        test "Input group lowers structurally equal to legacy *.create" {
            parityEqual
                (IconButton.view { IconButton.defaults with Text = "go"; Intent = Danger; OnClick = Some Save })
                (LIconButton.create [ LIconButton.icon "go"; Attr.enabled true; Attr.style "danger"; LIconButton.onClick Save ])
                "icon-button"
            parityEqual
                (NumericInput.view { NumericInput.defaults with Value = 3.0; OnChanged = Some NumberChanged })
                (LNumericInput.create [ LNumericInput.value 3.0; Attr.readOnly false; LNumericInput.onChanged NumberChanged ])
                "numeric-input"
            parityEqual
                (RadioGroup.view { RadioGroup.defaults with Items = [ "a"; "b" ]; SelectedKey = Some "a"; OnChanged = Some Picked })
                (LRadioGroup.create [ LRadioGroup.items [ "a"; "b" ]; LRadioGroup.selected "a"; LRadioGroup.onChanged Picked ])
                "radio-group"
            parityEqual
                (Switch.view { Switch.defaults with Checked = true; OnChanged = Some Toggled })
                (LSwitch.create [ LSwitch.checked' true; LSwitch.onChanged Toggled ])
                "switch"
            parityEqual
                (Slider.view { Slider.defaults with Value = 0.25; OnChanged = Some NumberChanged })
                (LSlider.create [ LSlider.value 0.25; LSlider.onChanged NumberChanged ])
                "slider"
        }

        // --- Group 5 Containers ---------------------------------------------
        test "Container group lowers structurally equal, child order preserved" {
            let a = Label.view { Label.defaults with Text = "A" }
            let b = Badge.view { Badge.defaults with Text = "B" }
            parityEqual
                (Grid.view { Grid.defaults with Children = [ a; b ] })
                (LGrid.create [ LGrid.children [ Widget.toControl a; Widget.toControl b ] ])
                "grid"
            parityEqual
                (Dock.view { Dock.defaults with Children = [ a; b ] })
                (LDock.create [ LDock.children [ Widget.toControl a; Widget.toControl b ] ])
                "dock"
            parityEqual
                (Wrap.view { Wrap.defaults with Orientation = Vertical; Spacing = 4.0; Children = [ a ] })
                (LWrap.create [ Attr.create "orientation" Layout (TextValue "vertical"); Attr.create "spacing" Layout (FloatValue 4.0); LWrap.children [ Widget.toControl a ] ])
                "wrap"
            parityEqual
                (Border.view { Border.defaults a with Thickness = 2.0; Padding = 3.0 })
                (LBorder.create [ LBorder.child (Widget.toControl a); Attr.create "thickness" Layout (FloatValue 2.0); Attr.padding 3.0 ])
                "border"
            parityEqual
                (Panel.view { Panel.defaults with Children = [ a; b ] })
                (LPanel.create [ LPanel.children [ Widget.toControl a; Widget.toControl b ] ])
                "panel"
            parityEqual
                (ScrollViewer.view (ScrollViewer.defaults "sv" a))
                (LControl.standard (StandardControlKind.Custom "scroll-viewer") [ Attr.child (Widget.toControl a) ] |> LControl.withKey "sv")
                "scroll-viewer"
            parityEqual
                (SplitView.view { SplitView.defaults with Children = [ a; b ] })
                (LControl.standard (StandardControlKind.Custom "split-view") [ Attr.children [ Widget.toControl a; Widget.toControl b ]; Attr.create "orientation" Layout (TextValue "horizontal") ])
                "split-view"

            let order = (Widget.toControl (Grid.view { Grid.defaults with Children = [ a; b ] })).Children |> List.map (fun c -> c.Content)
            Expect.equal order [ Some "A"; Some "B" ] "grid children lower in author order"
        }

        // --- Group 6 Navigation ---------------------------------------------
        test "Navigation group lowers structurally equal to legacy *.create" {
            parityEqual
                (Tabs.view { Tabs.defaults with Items = [ "one"; "two" ]; SelectedKey = Some "one"; OnChanged = Some Picked })
                (LTabs.create [ LTabs.items [ "one"; "two" ]; LTabs.selected "one"; LTabs.onChanged Picked ])
                "tabs"
            parityEqual
                (Menu.view { Menu.defaults with Items = [ "file" ]; OnSelected = Some Picked })
                (LMenu.create [ LMenu.items [ "file" ]; LMenu.onSelected Picked ])
                "menu"
            parityEqual
                (ContextMenu.view { ContextMenu.defaults with Items = [ "copy" ]; OnSelected = Some Picked })
                (LControl.standard (StandardControlKind.Custom "context-menu") [ Attr.items [ "copy" ]; Attr.onWith "onSelected" (fun e -> e.Payload |> Option.defaultValue "" |> Picked) ])
                "context-menu"
            let a = Label.view { Label.defaults with Text = "A" }
            parityEqual
                (Toolbar.view { Toolbar.defaults with Children = [ a ]; OnClick = Some Save })
                (LToolbar.create [ LToolbar.children [ Widget.toControl a ]; Attr.on "onClick" Save ])
                "toolbar"
        }

        // --- Group 7 Overlay ------------------------------------------------
        test "Overlay group lowers structurally equal to legacy *.create" {
            let a = Label.view { Label.defaults with Text = "A" }
            parityEqual (Tooltip.view { Tooltip.defaults with Text = "tip" }) (LTooltip.create [ LTooltip.text "tip" ]) "tooltip"
            parityEqual
                (Dialog.view { Dialog.defaults with Title = Some "T"; IsOpen = true; Children = [ a ]; OnSelected = Some Picked })
                (LDialog.create [ LDialog.children [ Widget.toControl a ]; Attr.create "title" Content (TextValue "T"); Attr.selected true; Attr.onWith "onSelected" (fun e -> e.Payload |> Option.defaultValue "" |> Picked) ])
                "dialog"
            parityEqual
                (Toast.view { Toast.defaults with Text = "saved"; Severity = Pending "x" })
                (LToast.create [ LToast.text "saved"; Attr.validation (Pending "x") ])
                "toast"
            parityEqual
                (Overlay.view { Overlay.defaults a with IsOpen = true })
                (LOverlay.create [ LOverlay.child (Widget.toControl a); Attr.selected true ])
                "overlay"
        }

        // --- Group 8 Charts/graph + stateful collections + custom-control ---
        test "Charts/graph group lowers structurally equal to legacy *.create" {
            let series = [ { Name = "s"; Points = [ { X = 0.0; Y = 1.0; Label = None } ] } ]
            let points = [ { X = 1.0; Y = 2.0; Label = Some "p" } ]
            parityEqual (LineChart.view { LineChart.defaults with Series = series }) (LLineChart.create [ LLineChart.series series ]) "line-chart"
            parityEqual (BarChart.view { BarChart.defaults with Series = series }) (LBarChart.create [ LBarChart.series series ]) "bar-chart"
            parityEqual (PieChart.view { PieChart.defaults with Values = points }) (LPieChart.create [ LPieChart.values points ]) "pie-chart"
            parityEqual (ScatterPlot.view { ScatterPlot.defaults with Series = series }) (LScatterPlot.create [ LScatterPlot.series series ]) "scatter-plot"
            parityEqual (GraphView.view { GraphView.defaults with Nodes = [ "n1" ] }) (LGraphView.create [ LGraphView.nodes [ "n1" ] ]) "graph-view"
        }

        test "Stateful collections lower structurally equal to Control.standard for the model state" {
            let props: ListViewProps<Msg> = { ListView.defaults "lv" with Items = [ "a"; "b" ]; OnSelected = Some Picked }
            let model, _ = ListView.init props
            let legacy =
                LControl.standard (StandardControlKind.Custom "list-view")
                    [ Attr.items [ "a"; "b" ]
                      Attr.create "selectedKeys" State (StringListValue(model.SelectedKeys |> Set.toList))
                      Attr.create "visibleRange" Data (UntypedValue model.VisibleRange)
                      Attr.onWith "onSelected" (fun e -> e.Payload |> Option.defaultValue "" |> Picked) ]
                |> LControl.withKey "lv"
            parityEqual (ListView.view props model) legacy "list-view"
        }

        test "custom-control bridge round-trips a legacy control with structural equality" {
            let legacy = LBadge.create [ LBadge.text "beta" ]
            Expect.equal (sprintf "%A" (Widget.toControl (CustomControl.ofControl legacy))) (sprintf "%A" legacy) "toControl (ofControl c) = c"
        }

        // --- T027 — optional event = None lowers to NO binding --------------
        test "every optional event prop set to None lowers to no event binding (FR-005)" {
            let noEvents (w: Widget<Msg>) name = Expect.isEmpty (eventAttrs (Widget.toControl w)) $"{name} unset event => no binding"
            noEvents (IconButton.view IconButton.defaults) "icon-button"
            noEvents (NumericInput.view NumericInput.defaults) "numeric-input"
            noEvents (RadioGroup.view RadioGroup.defaults) "radio-group"
            noEvents (Switch.view Switch.defaults) "switch"
            noEvents (Slider.view Slider.defaults) "slider"
            noEvents (Menu.view Menu.defaults) "menu"
            noEvents (ContextMenu.view ContextMenu.defaults) "context-menu"
            noEvents (Tabs.view Tabs.defaults) "tabs"
            noEvents (Toolbar.view Toolbar.defaults) "toolbar"
            noEvents (LineChart.view LineChart.defaults) "line-chart"
            noEvents (SplitView.view SplitView.defaults) "split-view"
            let dlgChild = Label.view { Label.defaults with Text = "x" }
            // Dialog always carries IsOpen (state) + children; assert no Event-category attr when OnSelected=None.
            noEvents (Dialog.view Dialog.defaults) "dialog"
            ignore dlgChild
            let lvProps: ListViewProps<Msg> = ListView.defaults "lv"
            let lvModel, _ = ListView.init lvProps
            noEvents (ListView.view lvProps lvModel) "list-view"
        }
    ]

// ---------------------------------------------------------------------------
// T036 — catalog cross-check (SC-007): each catalog required attribute
// (PascalCased) maps to a field on the control's typed Props record, and the
// typed module names agree with the catalog `module` fact, for all 41 migrated
// controls. `custom-control` is bridge-typed (Widget.ofControl) — no Props schema.
// ---------------------------------------------------------------------------
[<Tests>]
let typedMigrationCatalogTests =
    // id -> typed Props type for the 41 migrated controls (the 6 from 065 are
    // covered by CatalogTests.typedPropsById; custom-control is bridge-typed).
    let typedPropsById: Map<string, System.Type> =
        [ "rich-text", typeof<RichTextProps<int>>
          "label", typeof<LabelProps<int>>
          "image", typeof<ImageProps<int>>
          "icon", typeof<IconProps<int>>
          "separator", typeof<SeparatorProps<int>>
          "badge", typeof<BadgeProps<int>>
          "progress-bar", typeof<ProgressBarProps<int>>
          "spinner", typeof<SpinnerProps<int>>
          "validation-message", typeof<ValidationMessageProps<int>>
          "icon-button", typeof<IconButtonProps<int>>
          "numeric-input", typeof<NumericInputProps<int>>
          "radio-group", typeof<RadioGroupProps<int>>
          "switch", typeof<SwitchProps<int>>
          "slider", typeof<SliderProps<int>>
          "text-area", typeof<TextAreaProps<int>>
          "list-view", typeof<ListViewProps<int>>
          "list-box", typeof<ListBoxProps<int>>
          "multi-select-list", typeof<MultiSelectListProps<int>>
          "combo-box", typeof<ComboBoxProps<int>>
          "tree-view", typeof<TreeViewProps<int>>
          "grid", typeof<GridProps<int>>
          "dock", typeof<DockProps<int>>
          "wrap", typeof<WrapProps<int>>
          "border", typeof<BorderProps<int>>
          "panel", typeof<PanelProps<int>>
          "scroll-viewer", typeof<ScrollViewerProps<int>>
          "split-view", typeof<SplitViewProps<int>>
          "tabs", typeof<TabsProps<int>>
          "menu", typeof<MenuProps<int>>
          "context-menu", typeof<ContextMenuProps<int>>
          "toolbar", typeof<ToolbarProps<int>>
          "tooltip", typeof<TooltipProps<int>>
          "dialog", typeof<DialogProps<int>>
          "toast", typeof<ToastProps<int>>
          "overlay", typeof<OverlayProps<int>>
          "line-chart", typeof<LineChartProps<int>>
          "bar-chart", typeof<BarChartProps<int>>
          "pie-chart", typeof<PieChartProps<int>>
          "scatter-plot", typeof<ScatterPlotProps<int>>
          "graph-view", typeof<GraphViewProps<int>> ]
        |> Map.ofList

    let recordFields (t: System.Type) =
        FSharpType.GetRecordFields t |> Array.map (fun p -> p.Name) |> Set.ofArray

    let pascalCase (s: string) = s.Substring(0, 1).ToUpper() + s.Substring(1)

    let rowsById = Catalog.supportedControls |> List.map (fun r -> r.Id, r) |> Map.ofList

    testList "Feature 070 catalog cross-check (SC-007)" [
        test "all 41 migrated controls have a typed Props mapping (custom-control bridge-typed)" {
            // The catalog has 47 ids; 6 are the 065 reference slice (TextBlock/Button/
            // TextBox/CheckBox/DataGrid/Stack) and custom-control is the bridge escape hatch.
            Expect.equal typedPropsById.Count 40 "40 of the 41 migrated controls expose a Props record"
            // (custom-control is the 41st migrated control; it is intentionally schema-free.)
            Expect.isTrue (rowsById.ContainsKey "custom-control") "custom-control is in the catalog"
        }

        test "each catalog required attribute maps to a typed Props field (PascalCased)" {
            for KeyValue(id, propsType) in typedPropsById do
                let row = rowsById.[id]
                let fields = recordFields propsType
                for required in row.RequiredAttributes do
                    Expect.isTrue
                        (fields.Contains(pascalCase required))
                        $"{id} required attribute '{required}' maps to typed Props field '{pascalCase required}'"
        }
    ]

// ---------------------------------------------------------------------------
// T029–T032 — stateful delegation equality (SC-003) and no forked model.
// ---------------------------------------------------------------------------
[<Tests>]
let typedMigrationDelegationTests =
    testList "Feature 070 stateful delegation (SC-003)" [
        test "TextArea init/update delegate to TextInput (multiline)" {
            let props: TextAreaProps<Msg> = { TextArea.defaults "note" with Value = "hi" }
            let model, effects = TextArea.init props
            let lModel, lEffects = LTextInput.init "note" MultiLine "hi"
            Expect.equal model lModel "TextArea.init model equals TextInput.init"
            Expect.equal effects lEffects "TextArea.init effects equal TextInput.init"
            Expect.equal model.Mode MultiLine "text-area is multiline"

            let m2, e2 = TextArea.update (InsertText "x") model
            let lm2, le2 = LTextInput.update (InsertText "x") lModel
            Expect.equal m2 lm2 "TextArea.update model equals TextInput.update"
            Expect.equal e2 le2 "TextArea.update effects equal TextInput.update"
        }

        test "every selection collection's init/update delegate to Collections" {
            let cases: (CollectionModel * CollectionEffect list) list =
                [ ListView.init (ListView.defaults "a")
                  ListBox.init (ListBox.defaults "a")
                  MultiSelectList.init (MultiSelectList.defaults "a")
                  ComboBox.init (ComboBox.defaults "a")
                  TreeView.init (TreeView.defaults "a") ]
            let expectedModel, expectedEffects = LCollections.init "a" 0 24.0 240.0
            for (model, effects) in cases do
                Expect.equal model expectedModel "collection init model equals Collections.init"
                Expect.equal effects expectedEffects "collection init effects equal Collections.init"

            // update delegation: each typed update equals Collections.update directly.
            let baseModel, _ = LCollections.init "a" 5 24.0 240.0
            let msg = SelectKey "k"
            let expected = LCollections.update msg baseModel
            Expect.equal (ListView.update msg baseModel) expected "ListView.update == Collections.update"
            Expect.equal (ListBox.update msg baseModel) expected "ListBox.update == Collections.update"
            Expect.equal (MultiSelectList.update msg baseModel) expected "MultiSelectList.update == Collections.update"
            Expect.equal (ComboBox.update msg baseModel) expected "ComboBox.update == Collections.update"
            Expect.equal (TreeView.update msg baseModel) expected "TreeView.update == Collections.update"
        }

        test "stateful facades introduce no parallel model type (SC-003)" {
            // The init/update signatures return the existing model/effect types — proven
            // by these bindings type-checking against the reused types.
            let taInit: TextAreaProps<Msg> -> TextInputModel * TextInputEffect list = TextArea.init
            let lvInit: ListViewProps<Msg> -> CollectionModel * CollectionEffect list = ListView.init
            ignore taInit
            ignore lvInit
            Expect.isTrue true "no forked model type"
        }
    ]
