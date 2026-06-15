/// The page registry — the single source of truth for the 52→10 coverage map
/// (contracts/page-registry.md) — together with each page's real control composition
/// (US1, FR-004). Legacy builders come from `FS.GG.UI.Controls` (opened); the typed
/// widgets live in `FS.GG.UI.Controls.Typed` and are fully-qualified to avoid the
/// module-name clashes (Button/Stack/Grid/…) between the two surfaces, then converted
/// to `Control` via `Widget.toControl`.
module ControlsGallery.Core.Pages

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open ControlsGallery.Core.Model
open ControlsGallery.Core.DemoState

/// Title + body grouping. The grouping is theme-independent, so the control-tree shape
/// is identical across every Light/Dark × accent variant (FR-006/SC-003).
let private section (title: string) (body: Control<GalleryMsg>): Control<GalleryMsg> =
    Stack.create [ Stack.children [ Label.create [ Label.text title ]; body ] ]

let private group (bodies: Control<GalleryMsg> list): Control<GalleryMsg> =
    Stack.create [ Stack.children bodies ]

let private toControl w = Widget.toControl w

// ---------------------------------------------------------------------------------
// Page 1 — Display & Typography
// ---------------------------------------------------------------------------------
let private displayPage (s: DemoState): Control<GalleryMsg> =
    let richBlock =
        RichText.block [ RichText.run "Rich text with measured runs." (RichText.defaultStyle GalleryTheme.defaultTheme) ]
    group
        [ section "text-block" (TextBlock.create [ TextBlock.text s.TextValue ])
          section "rich-text" (RichText.create richBlock [])
          section "label" (Label.create [ Label.text "A short form label" ])
          section "image" (Image.create [ Image.source "assets/sample.png" ])
          section "icon" (Icon.create [ Icon.name "sparkles" ])
          section "separator" (Separator.create [])
          section "badge" (Badge.create [ Badge.text "New" ]) ]

// ---------------------------------------------------------------------------------
// Page 2 — Buttons
// ---------------------------------------------------------------------------------
let private buttonsPage (s: DemoState): Control<GalleryMsg> =
    let toggleProps =
        { FS.GG.UI.Controls.Typed.ToggleButton.defaults with
            Text = "Toggle"
            IsOn = s.ToggleOn
            OnToggle = Some(fun b -> PageMsg(ToggleChanged b)) }
    let splitItems: FS.GG.UI.Controls.Typed.SplitButtonItem list =
        [ { Key = "save-as"; Label = "Save As" }
          { Key = "export"; Label = "Export" } ]
    let splitProps =
        { FS.GG.UI.Controls.Typed.SplitButton.defaults with
            Text = "Save"
            Items = splitItems
            OnClick = Some(PageMsg ButtonClicked)
            OnSelected = Some(fun k -> PageMsg(MenuSelectedMsg k)) }
    group
        [ section "button" (Button.create [ Button.text (sprintf "Clicked %d" s.ButtonClicks); Button.onClick (PageMsg ButtonClicked) ])
          section "icon-button" (IconButton.create [ IconButton.icon "play"; IconButton.onClick (PageMsg ButtonClicked) ])
          section "toggle-button" (toControl (FS.GG.UI.Controls.Typed.ToggleButton.view toggleProps))
          section "split-button" (toControl (FS.GG.UI.Controls.Typed.SplitButton.view splitProps)) ]

// ---------------------------------------------------------------------------------
// Page 3 — Text & Numeric Input
// ---------------------------------------------------------------------------------
let private inputPage (s: DemoState): Control<GalleryMsg> =
    let dateProps =
        { FS.GG.UI.Controls.Typed.DatePicker.defaults with
            Value = Some(System.DateOnly(2026, 6, 15))
            OnChange = Some(fun _ -> PageMsg ButtonClicked) }
    let timeProps =
        { FS.GG.UI.Controls.Typed.TimePicker.defaults with
            Value = Some(System.TimeOnly(9, 30))
            OnChange = Some(fun _ -> PageMsg ButtonClicked) }
    group
        [ section "text-box" (TextBox.create [ TextBox.value s.TextValue; TextBox.onChanged (fun v -> PageMsg(TextChanged v)) ])
          section "text-area" (TextArea.create [ TextArea.value s.AreaValue; TextArea.onChanged (fun v -> PageMsg(AreaChanged v)) ])
          section "numeric-input" (NumericInput.create [ NumericInput.value s.NumericValue; NumericInput.onChanged (fun v -> PageMsg(NumericChanged v)) ])
          section "slider" (Slider.create [ Slider.value s.SliderValue; Slider.onChanged (fun v -> PageMsg(SliderChanged v)) ])
          section "date-picker" (toControl (FS.GG.UI.Controls.Typed.DatePicker.view dateProps))
          section "time-picker" (toControl (FS.GG.UI.Controls.Typed.TimePicker.view timeProps)) ]

// ---------------------------------------------------------------------------------
// Page 4 — Selection & Toggles
// ---------------------------------------------------------------------------------
let private selectionPage (s: DemoState): Control<GalleryMsg> =
    let listProps =
        { FS.GG.UI.Controls.Typed.ListBox.defaults "demo-list-box" with
            Items = listItems
            OnSelected = Some(fun v -> PageMsg(ListSelectedMsg v)) }
    let listModel, _ = FS.GG.UI.Controls.Typed.ListBox.init listProps
    let multiProps =
        { FS.GG.UI.Controls.Typed.MultiSelectList.defaults "demo-multi" with
            Items = multiItems
            OnChanged = Some(fun v -> PageMsg(MultiChanged v)) }
    let multiModel, _ = FS.GG.UI.Controls.Typed.MultiSelectList.init multiProps
    let comboProps =
        { FS.GG.UI.Controls.Typed.ComboBox.defaults "demo-combo" with
            Items = comboItems
            OnChanged = Some(fun v -> PageMsg(ComboChanged v)) }
    let comboModel, _ = FS.GG.UI.Controls.Typed.ComboBox.init comboProps
    let swatches: FS.GG.UI.Controls.Typed.ColorSwatch list =
        [ { Name = "Indigo"; Color = GalleryTheme.indigo }
          { Name = "Teal"; Color = GalleryTheme.teal } ]
    let colorProps =
        { FS.GG.UI.Controls.Typed.ColorPicker.defaults with
            Swatches = swatches
            Selected = List.tryHead swatches
            OnSelected = Some(fun sw -> PageMsg(ColorChanged sw.Name)) }
    group
        [ section "check-box" (CheckBox.create [ CheckBox.text "Enable telemetry"; CheckBox.checked' s.Checked; CheckBox.onChanged (fun v -> PageMsg(CheckChanged v)) ])
          section "radio-group" (RadioGroup.create [ RadioGroup.items radioOptions; RadioGroup.selected s.RadioSelected; RadioGroup.onChanged (fun v -> PageMsg(RadioChanged v)) ])
          section "switch" (Switch.create [ Switch.checked' s.SwitchOn; Switch.onChanged (fun v -> PageMsg(SwitchChanged v)) ])
          section "list-box" (toControl (FS.GG.UI.Controls.Typed.ListBox.view listProps listModel))
          section "multi-select-list" (toControl (FS.GG.UI.Controls.Typed.MultiSelectList.view multiProps multiModel))
          section "combo-box" (toControl (FS.GG.UI.Controls.Typed.ComboBox.view comboProps comboModel))
          section "color-picker" (toControl (FS.GG.UI.Controls.Typed.ColorPicker.view colorProps)) ]

// ---------------------------------------------------------------------------------
// Page 5 — Data & Collections
// ---------------------------------------------------------------------------------
let private dataPage (s: DemoState): Control<GalleryMsg> =
    let lvProps =
        { FS.GG.UI.Controls.Typed.ListView.defaults "demo-list-view" with
            Items = listItems
            OnSelected = Some(fun v -> PageMsg(ListSelectedMsg v)) }
    let lvModel, _ = FS.GG.UI.Controls.Typed.ListView.init lvProps
    let tvProps =
        { FS.GG.UI.Controls.Typed.TreeView.defaults "demo-tree" with
            Items = treeItems
            OnSelected = Some(fun v -> PageMsg(TreeSelectedMsg v)) }
    let tvModel, _ = FS.GG.UI.Controls.Typed.TreeView.init tvProps
    let cols: DataGridColumn list =
        [ { Key = "name"; Header = "Name"; Width = 160.0; ColumnType = TextColumn }
          { Key = "count"; Header = "Count"; Width = 80.0; ColumnType = NumericColumn } ]
    let rows: DataGridRow list =
        [ { Key = "r1"; Cells = [ { RowKey = "r1"; ColumnKey = "name"; Value = "Indigo" }; { RowKey = "r1"; ColumnKey = "count"; Value = "3" } ] }
          { Key = "r2"; Cells = [ { RowKey = "r2"; ColumnKey = "name"; Value = "Teal" }; { RowKey = "r2"; ColumnKey = "count"; Value = "5" } ] } ]
    group
        [ section "list-view" (toControl (FS.GG.UI.Controls.Typed.ListView.view lvProps lvModel))
          section "tree-view" (toControl (FS.GG.UI.Controls.Typed.TreeView.view tvProps tvModel))
          section "data-grid" (DataGrid.create cols [ DataGrid.rows rows ]) ]

// ---------------------------------------------------------------------------------
// Page 6 — Layout & Containers
// ---------------------------------------------------------------------------------
let private layoutPage (_s: DemoState): Control<GalleryMsg> =
    let sample t = TextBlock.create [ TextBlock.text t ]
    let scrollChild = group [ sample "Row 1"; sample "Row 2"; sample "Row 3" ]
    let scrollProps = FS.GG.UI.Controls.Typed.ScrollViewer.defaults "demo-scroll" (Widget.ofControl scrollChild)
    let splitProps =
        { FS.GG.UI.Controls.Typed.SplitView.defaults with
            Children = [ Widget.ofControl (sample "Left"); Widget.ofControl (sample "Right") ] }
    group
        [ section "stack" (Stack.create [ Stack.children [ sample "A"; sample "B" ] ])
          section "grid" (Grid.create [ Grid.children [ sample "G1"; sample "G2" ] ])
          section "dock" (Dock.create [ Dock.children [ sample "Top"; sample "Fill" ] ])
          section "wrap" (Wrap.create [ Wrap.children [ sample "W1"; sample "W2"; sample "W3" ] ])
          section "border" (Border.create [ Border.child (sample "Bordered") ])
          section "panel" (Panel.create [ Panel.children [ sample "Panel content" ] ])
          section "scroll-viewer" (toControl (FS.GG.UI.Controls.Typed.ScrollViewer.view scrollProps))
          section "split-view" (toControl (FS.GG.UI.Controls.Typed.SplitView.view splitProps)) ]

// ---------------------------------------------------------------------------------
// Page 7 — Navigation & Menus
// ---------------------------------------------------------------------------------
let private navPage (s: DemoState): Control<GalleryMsg> =
    let ctxProps =
        { FS.GG.UI.Controls.Typed.ContextMenu.defaults with
            Items = menuItems
            OnSelected = Some(fun v -> PageMsg(MenuSelectedMsg v)) }
    group
        [ section "tabs" (Tabs.create [ Tabs.items tabItems; Tabs.selected s.Tab; Tabs.onChanged (fun v -> PageMsg(TabChanged v)) ])
          section "menu" (Menu.create [ Menu.items menuItems; Menu.onSelected (fun v -> PageMsg(MenuSelectedMsg v)) ])
          section "context-menu" (toControl (FS.GG.UI.Controls.Typed.ContextMenu.view ctxProps))
          section "toolbar" (Toolbar.create [ Toolbar.children [ TextBlock.create [ TextBlock.text "Undo" ]; TextBlock.create [ TextBlock.text "Redo" ] ] ]) ]

// ---------------------------------------------------------------------------------
// Page 8 — Overlays & Feedback
// ---------------------------------------------------------------------------------
let private overlaysPage (s: DemoState): Control<GalleryMsg> =
    group
        [ section "tooltip" (Tooltip.create [ Tooltip.text "Helpful hint" ])
          section "dialog" (Dialog.create [ Dialog.children [ TextBlock.create [ TextBlock.text "Are you sure?" ] ] ])
          section "overlay" (Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "Overlay layer" ]) ])
          section "toast" (Toast.create [ Toast.text "Saved" ])
          section "progress-bar" (ProgressBar.create [ ProgressBar.value s.ProgressValue ])
          section "spinner" (Spinner.create [])
          section "validation-message" (ValidationMessage.create [ ValidationMessage.text "This field is required" ]) ]

// ---------------------------------------------------------------------------------
// Page 9 — Charts
// ---------------------------------------------------------------------------------
let private chartsPage (_s: DemoState): Control<GalleryMsg> =
    let pts: ChartPoint list =
        [ { X = 0.0; Y = 2.0; Label = None }
          { X = 1.0; Y = 4.0; Label = None }
          { X = 2.0; Y = 3.0; Label = None }
          { X = 3.0; Y = 6.0; Label = None } ]
    let series: ChartSeries list = [ { Name = "Sessions"; Points = pts } ]
    let pieVals: ChartPoint list =
        [ { X = 0.0; Y = 40.0; Label = Some "Indigo" }
          { X = 1.0; Y = 35.0; Label = Some "Teal" }
          { X = 2.0; Y = 25.0; Label = Some "Slate" } ]
    group
        [ section "line-chart" (LineChart.create [ LineChart.series series ])
          section "bar-chart" (BarChart.create [ BarChart.series series ])
          section "pie-chart" (PieChart.create [ PieChart.values pieVals ])
          section "scatter-plot" (ScatterPlot.create [ ScatterPlot.series series ]) ]

// ---------------------------------------------------------------------------------
// Page 10 — Pointer Playground / Custom
// ---------------------------------------------------------------------------------
let private pointerPage (_s: DemoState): Control<GalleryMsg> =
    // Render/Draw/Layout are phantom — never invoked by `create`/`renderTree` (they are
    // host-driven), matching the framework's own custom-control authoring pattern.
    let customDef: CustomControlDefinition<GalleryMsg> =
        { Id = "gallery-custom"
          Measure = fun () -> (160.0, 48.0)
          Render = fun () -> ({ Nodes = [] }: Scene)
          Draw = fun () -> ({ Nodes = [] }: Scene)
          Layout = fun () -> failwith "custom-control layout is host-driven"
          Clip = None
          Effects = []
          HitTest = fun _ _ -> false
          Event = fun _ -> None
          Accessibility = None
          Diagnostics = [] }
    group
        [ section "graph-view" (GraphView.create [ GraphView.nodes [ "Alpha"; "Beta"; "Gamma" ] ])
          section "custom-control" (CustomControl.create customDef []) ]

/// `Core.Pages.all` — exactly 10 entries, in nav order. The control→page assignment
/// is authoritative for the coverage check (FR-003).
let all: GalleryPage list =
    [ { Id = "display-typography"; Index = 1; Title = "Display & Typography"; Family = "Display & Typography"
        ControlIds = [ "text-block"; "rich-text"; "label"; "image"; "icon"; "separator"; "badge" ]
        Build = displayPage }
      { Id = "buttons"; Index = 2; Title = "Buttons"; Family = "Buttons"
        ControlIds = [ "button"; "icon-button"; "toggle-button"; "split-button" ]
        Build = buttonsPage }
      { Id = "text-numeric-input"; Index = 3; Title = "Text & Numeric Input"; Family = "Text & Numeric Input"
        ControlIds = [ "text-box"; "text-area"; "numeric-input"; "slider"; "date-picker"; "time-picker" ]
        Build = inputPage }
      { Id = "selection-toggles"; Index = 4; Title = "Selection & Toggles"; Family = "Selection & Toggles"
        ControlIds = [ "check-box"; "radio-group"; "switch"; "list-box"; "multi-select-list"; "combo-box"; "color-picker" ]
        Build = selectionPage }
      { Id = "data-collections"; Index = 5; Title = "Data & Collections"; Family = "Data & Collections"
        ControlIds = [ "list-view"; "tree-view"; "data-grid" ]
        Build = dataPage }
      { Id = "layout-containers"; Index = 6; Title = "Layout & Containers"; Family = "Layout & Containers"
        ControlIds = [ "stack"; "grid"; "dock"; "wrap"; "border"; "panel"; "scroll-viewer"; "split-view" ]
        Build = layoutPage }
      { Id = "navigation-menus"; Index = 7; Title = "Navigation & Menus"; Family = "Navigation & Menus"
        ControlIds = [ "tabs"; "menu"; "context-menu"; "toolbar" ]
        Build = navPage }
      { Id = "overlays-feedback"; Index = 8; Title = "Overlays & Feedback"; Family = "Overlays & Feedback"
        ControlIds = [ "tooltip"; "dialog"; "overlay"; "toast"; "progress-bar"; "spinner"; "validation-message" ]
        Build = overlaysPage }
      { Id = "charts"; Index = 9; Title = "Charts"; Family = "Charts"
        ControlIds = [ "line-chart"; "bar-chart"; "pie-chart"; "scatter-plot" ]
        Build = chartsPage }
      { Id = "pointer-custom"; Index = 10; Title = "Pointer Playground / Custom"; Family = "Pointer Playground / Custom"
        ControlIds = [ "graph-view"; "custom-control" ]
        Build = pointerPage } ]

/// Page lookup by id; falls back to the first page for an unknown id.
let byId (id: string): GalleryPage =
    all |> List.tryFind (fun p -> p.Id = id) |> Option.defaultValue (List.head all)
