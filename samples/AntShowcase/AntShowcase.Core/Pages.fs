/// The 13 family pages composing all 97 catalog controls under the Ant theme
/// (contracts/page-registry.md, FR-001/FR-004). Legacy builders come from
/// `FS.GG.UI.Controls` (opened); the net-new Ant primitives live in the feature-132
/// `Display2`/`Navigation2`/`Feedback2`/`DataEntry2`/`Interactive2` modules and the
/// feature-133 chart modules (all opened); the typed widgets live in
/// `FS.GG.UI.Controls.Typed` and are fully-qualified to avoid the module-name clashes
/// (Button/Stack/Grid/…) between surfaces, then converted via `Widget.toControl`.
module AntShowcase.Core.Pages

open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Display2
open FS.GG.UI.Controls.Navigation2
open FS.GG.UI.Controls.Feedback2
open FS.GG.UI.Controls.DataEntry2
open FS.GG.UI.Controls.Interactive2
open AntShowcase.Core
open AntShowcase.Core.Model
open AntShowcase.Core.DemoState

/// Title + body grouping. Theme-independent, so the control-tree shape is identical
/// across antLight/antDark (FR-008/SC-003).
let private section (title: string) (body: Control<AntShowcaseMsg>): Control<AntShowcaseMsg> =
    Border.create
        [ Attr.width 520.0
          Attr.height 150.0
          Attr.padding 8.0
          Border.child
              (Stack.create
                  [ Stack.children [ Label.create [ Label.text title ]; body ] ]) ]

let private group (bodies: Control<AntShowcaseMsg> list): Control<AntShowcaseMsg> =
    Wrap.create [ Wrap.children bodies ]

let private largeSection (title: string) (body: Control<AntShowcaseMsg>): Control<AntShowcaseMsg> =
    Border.create
        [ Attr.width 520.0
          Attr.height 220.0
          Attr.padding 10.0
          Border.child
              (Stack.create
                  [ Stack.children [ Label.create [ Label.text title ]; body ] ]) ]

let private toControl w = Widget.toControl w

// Ant-ish swatch colours for the color-picker demo (consumer literals — the sample never
// alters theme tokens, FR-016; these only seed a control's value).
let private antBlue: Color = Colors.rgb 22uy 119uy 255uy
let private antGreen: Color = Colors.rgb 82uy 196uy 26uy

// Shared seeded chart data (literal, deterministic — R5/R7).
let private series: ChartSeries list =
    [ { Name = "Sessions"
        Points =
          [ { X = 0.0; Y = 2.0; Label = None }
            { X = 1.0; Y = 4.0; Label = None }
            { X = 2.0; Y = 3.0; Label = None }
            { X = 3.0; Y = 6.0; Label = None } ] } ]

let private categoricalValues: ChartPoint list =
    [ { X = 0.0; Y = 40.0; Label = Some "Design" }
      { X = 1.0; Y = 35.0; Label = Some "Engineering" }
      { X = 2.0; Y = 25.0; Label = Some "Product" } ]

// ---------------------------------------------------------------------------------
// Page 1 — Display & Typography
// ---------------------------------------------------------------------------------
let private displayPage (s: DemoState): Control<AntShowcaseMsg> =
    let richBlock =
        RichText.block [ RichText.run "Rich text with measured runs." (RichText.defaultStyle AntTheme.defaultTheme) ]
    group
        [ section "text-block" (TextBlock.create [ TextBlock.text s.TextValue ])
          section "rich-text" (RichText.create richBlock [])
          section "label" (Label.create [ Label.text "A short form label" ])
          section "icon" (Icon.create [ Icon.name "sparkles" ])
          section "separator" (Separator.create [])
          section "badge" (Badge.create [ Badge.text "New" ])
          section "tag" (Tag.create [ Tag.text "Stable" ])
          section "avatar" (Avatar.create [ Avatar.text avatarInitials ]) ]

// ---------------------------------------------------------------------------------
// Page 2 — Cards, Stats & Media
// ---------------------------------------------------------------------------------
let private cardsPage (_s: DemoState): Control<AntShowcaseMsg> =
    group
        [ section "image" (Image.create [ Image.source "assets/sample.png" ])
          section "card" (Card.create [ Card.title "Active users" ])
          section "descriptions" (Descriptions.create [ Attr.items descriptionsItems ])
          section "statistic" (Statistic.create [ Statistic.value "1,284" ])
          section "qr-code" (QrCode.create [ QrCode.value "https://fs.gg/ant" ])
          section "watermark" (Watermark.create [ Watermark.text "FS.GG" ])
          largeSection "calendar" (Calendar.create [ Calendar.onChange (fun d -> PageMsg(TextChanged d)) ])
          section "collapse" (Collapse.create [ Attr.items collapsePanels; Collapse.onChange (fun k -> PageMsg(CollapseToggled k)) ])
          section "carousel" (Carousel.create [ Attr.items carouselSlides ])
          section "timeline" (Timeline.create [ Attr.items timelineItems ]) ]

// ---------------------------------------------------------------------------------
// Page 3 — Buttons & Commands
// ---------------------------------------------------------------------------------
let private buttonsPage (s: DemoState): Control<AntShowcaseMsg> =
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
// Page 4 — Text & Numeric Input
// ---------------------------------------------------------------------------------
let private inputPage (s: DemoState): Control<AntShowcaseMsg> =
    let dateProps =
        { FS.GG.UI.Controls.Typed.DatePicker.defaults with
            Id = Some "date-picker"
            Value = s.DatePickerSelected
            IsOpen = s.DatePickerOpen
            OnChange = Some(fun date -> PageMsg(DatePickerChanged date)) }
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
          section "time-picker" (toControl (FS.GG.UI.Controls.Typed.TimePicker.view timeProps))
          section "rate" (Rate.create [ Rate.value s.RateValue; Rate.onChange (fun v -> PageMsg(RateChanged(float v))) ])
          section "auto-complete" (AutoComplete.create [ AutoComplete.value s.AutoCompleteValue; AutoComplete.onChange (fun v -> PageMsg(AutoCompleteChanged v)) ])
          section "upload" (Upload.create [ Upload.text "Upload"; Upload.onChange (fun v -> PageMsg(UploadChanged v)) ]) ]

// ---------------------------------------------------------------------------------
// Page 5 — Selection & Toggles
// ---------------------------------------------------------------------------------
let private selectionPage (s: DemoState): Control<AntShowcaseMsg> =
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
        [ { Name = "ant-blue"; Color = antBlue }
          { Name = "ant-green"; Color = antGreen } ]
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
          section "color-picker" (toControl (FS.GG.UI.Controls.Typed.ColorPicker.view colorProps))
          section "cascader" (Cascader.create [ Attr.items cascaderItems; Cascader.onChange (fun v -> PageMsg(CascaderChanged v)) ]) ]

// ---------------------------------------------------------------------------------
// Page 6 — Layout & Containers
// ---------------------------------------------------------------------------------
let private layoutPage (_s: DemoState): Control<AntShowcaseMsg> =
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
          largeSection "scroll-viewer" (toControl (FS.GG.UI.Controls.Typed.ScrollViewer.view scrollProps))
          largeSection "split-view" (toControl (FS.GG.UI.Controls.Typed.SplitView.view splitProps)) ]

// ---------------------------------------------------------------------------------
// Page 7 — Navigation & Menus
// ---------------------------------------------------------------------------------
let private navPage (s: DemoState): Control<AntShowcaseMsg> =
    let ctxProps =
        { FS.GG.UI.Controls.Typed.ContextMenu.defaults with
            Items = menuItems
            OnSelected = Some(fun v -> PageMsg(MenuSelectedMsg v)) }
    group
        [ section "tabs" (Tabs.create [ Tabs.items tabItems; Tabs.selected s.Tab; Tabs.onChanged (fun v -> PageMsg(TabChanged v)) ])
          section "menu" (Menu.create [ Menu.items menuItems; Menu.onSelected (fun v -> PageMsg(MenuSelectedMsg v)) ])
          section "context-menu" (toControl (FS.GG.UI.Controls.Typed.ContextMenu.view ctxProps))
          section "toolbar" (Toolbar.create [ Toolbar.children [ TextBlock.create [ TextBlock.text "Undo" ]; TextBlock.create [ TextBlock.text "Redo" ] ] ])
          section "float-button" (FloatButton.create [ FloatButton.text "+"; FloatButton.onClick (PageMsg ButtonClicked) ])
          section "breadcrumb" (Breadcrumb.create [ Attr.items breadcrumbTrail ])
          section "steps" (Steps.create [ Attr.items stepsItems ])
          section "pagination" (Pagination.create [ Pagination.total paginationTotal; Pagination.onChange (fun p -> PageMsg(PageChanged(int p))) ])
          section "segmented" (Segmented.create [ Attr.items segmentedOptions; Segmented.onChange (fun v -> PageMsg(SegmentedChanged v)) ])
          section "anchor" (Anchor.create [ Attr.items anchorItems ])
          section "affix" (Affix.create [ Affix.text "Pinned toolbar" ]) ]

// ---------------------------------------------------------------------------------
// Page 8 — Overlays
// ---------------------------------------------------------------------------------
let private overlaysPage (_s: DemoState): Control<AntShowcaseMsg> =
    group
        [ section "tooltip" (Tooltip.create [ Tooltip.text "Helpful hint" ])
          section "dialog" (Dialog.create [ Dialog.children [ TextBlock.create [ TextBlock.text "Are you sure?" ] ] ])
          section "overlay" (Overlay.create [ Overlay.child (TextBlock.create [ TextBlock.text "Overlay layer" ]) ]) ]

// ---------------------------------------------------------------------------------
// Page 9 — Feedback & Status
// ---------------------------------------------------------------------------------
let private feedbackPage (s: DemoState): Control<AntShowcaseMsg> =
    group
        [ section "toast" (Toast.create [ Toast.text "Saved" ])
          section "progress-bar" (ProgressBar.create [ ProgressBar.value s.ProgressValue ])
          section "spinner" (Spinner.create [])
          section "validation-message" (ValidationMessage.create [ ValidationMessage.text "This field is required" ])
          section "empty" (Empty.create [ Empty.text "No data" ])
          section "skeleton" (Skeleton.create [])
          section "alert" (Alert.create [ Alert.text "Your changes have been saved." ])
          section "result" (Result.create [ Result.title "Submitted successfully" ])
          largeSection "drawer" (Drawer.create [ Drawer.title "Filters"; Drawer.onClose (PageMsg(DrawerToggled false)) ])
          section "popover" (Popover.create [ Popover.text "More information" ])
          section "popconfirm" (Popconfirm.create [ Popconfirm.text "Delete this item?"; Popconfirm.onConfirm (PageMsg ButtonClicked); Popconfirm.onCancel (PageMsg ButtonClicked) ])
          section "tour" (Tour.create [ Tour.text "Step 1 of 3 — welcome!" ]) ]

// ---------------------------------------------------------------------------------
// Page 10 — Data Collections
// ---------------------------------------------------------------------------------
let private dataPage (_s: DemoState): Control<AntShowcaseMsg> =
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
        [ { Key = "r1"; Cells = [ { RowKey = "r1"; ColumnKey = "name"; Value = "Design" }; { RowKey = "r1"; ColumnKey = "count"; Value = "3" } ] }
          { Key = "r2"; Cells = [ { RowKey = "r2"; ColumnKey = "name"; Value = "Engineering" }; { RowKey = "r2"; ColumnKey = "count"; Value = "5" } ] } ]
    group
        [ largeSection "list-view" (toControl (FS.GG.UI.Controls.Typed.ListView.view lvProps lvModel))
          largeSection "tree-view" (toControl (FS.GG.UI.Controls.Typed.TreeView.view tvProps tvModel))
          largeSection "data-grid" (DataGrid.create cols [ DataGrid.rows rows ]) ]

// ---------------------------------------------------------------------------------
// Page 11 — Charts I — Statistical
// ---------------------------------------------------------------------------------
let private chartsStatPage (_s: DemoState): Control<AntShowcaseMsg> =
    group
        [ largeSection "line-chart" (LineChart.create [ LineChart.series series ])
          largeSection "bar-chart" (BarChart.create [ BarChart.series series ])
          largeSection "pie-chart" (PieChart.create [ PieChart.values categoricalValues ])
          largeSection "scatter-plot" (ScatterPlot.create [ ScatterPlot.series series ])
          largeSection "area-chart" (AreaChart.create [ AreaChart.series series ])
          largeSection "column-chart" (ColumnChart.create [ ColumnChart.series series ])
          largeSection "histogram" (Histogram.create [ Histogram.values categoricalValues ])
          largeSection "box-plot" (BoxPlot.create [ BoxPlot.series series ]) ]

// ---------------------------------------------------------------------------------
// Page 12 — Charts II — Advanced
// ---------------------------------------------------------------------------------
let private chartsAdvPage (_s: DemoState): Control<AntShowcaseMsg> =
    group
        [ largeSection "heatmap" (Heatmap.create [ Heatmap.values categoricalValues ])
          largeSection "radar-chart" (RadarChart.create [ RadarChart.values categoricalValues ])
          largeSection "rose-chart" (RoseChart.create [ RoseChart.values categoricalValues ])
          largeSection "waterfall-chart" (WaterfallChart.create [ WaterfallChart.values categoricalValues ])
          largeSection "funnel-chart" (FunnelChart.create [ FunnelChart.values categoricalValues ])
          largeSection "gauge-chart" (GaugeChart.create [ GaugeChart.value 0.72 ])
          largeSection "treemap" (Treemap.create [ Treemap.values categoricalValues ])
          largeSection "sunburst" (Sunburst.create [ Sunburst.values categoricalValues ]) ]

// ---------------------------------------------------------------------------------
// Page 13 — Graphs & Custom
// ---------------------------------------------------------------------------------
let private graphsPage (_s: DemoState): Control<AntShowcaseMsg> =
    // Render/Draw/Layout are phantom — never invoked by `create`/`renderTree` (they are
    // host-driven), matching the framework's own custom-control authoring pattern.
    let customDef: CustomControlDefinition<AntShowcaseMsg> =
        { Id = "ant-showcase-custom"
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
    // A small author-supplied scene for the embedded `canvas` control (Feature 191): a filled box
    // with a label, painted in canvas-local coordinates.
    let canvasScene: Scene =
        { Nodes =
            [ Rectangle((0.0, 0.0, 160.0, 90.0), { Red = 22uy; Green = 119uy; Blue = 255uy; Alpha = 255uy })
              SceneNode.Text((12.0, 50.0), "canvas", { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy }) ] }
    group
        [ largeSection "graph-view" (GraphView.create [ GraphView.nodes [ "Alpha"; "Beta"; "Gamma" ] ])
          largeSection "sankey-diagram" (SankeyDiagram.create [ SankeyDiagram.nodes [ "Source"; "Stage"; "Target" ] ])
          largeSection "chord-diagram" (ChordDiagram.create [ ChordDiagram.nodes [ "A"; "B"; "C" ] ])
          largeSection "canvas" (Canvas.create [ Canvas.scene canvasScene ])
          section "custom-control" (CustomControl.create customDef []) ]

/// The 13 family pages, tagged `Catalog`, in nav order. The control→page assignment is
/// authoritative for the coverage check (FR-003).
let familyPages: Page list =
    [ { Id = "display-typography"; Title = "Display & Typography"; Kind = Catalog
        ControlIds = [ "text-block"; "rich-text"; "label"; "icon"; "separator"; "badge"; "tag"; "avatar" ]
        View = displayPage }
      { Id = "cards-stats-media"; Title = "Cards, Stats & Media"; Kind = Catalog
        ControlIds = [ "image"; "card"; "descriptions"; "statistic"; "qr-code"; "watermark"; "calendar"; "collapse"; "carousel"; "timeline" ]
        View = cardsPage }
      { Id = "buttons"; Title = "Buttons & Commands"; Kind = Catalog
        ControlIds = [ "button"; "icon-button"; "toggle-button"; "split-button" ]
        View = buttonsPage }
      { Id = "text-numeric-input"; Title = "Text & Numeric Input"; Kind = Catalog
        ControlIds = [ "text-box"; "text-area"; "numeric-input"; "slider"; "date-picker"; "time-picker"; "rate"; "auto-complete"; "upload" ]
        View = inputPage }
      { Id = "selection-toggles"; Title = "Selection & Toggles"; Kind = Catalog
        ControlIds = [ "check-box"; "radio-group"; "switch"; "list-box"; "multi-select-list"; "combo-box"; "color-picker"; "cascader" ]
        View = selectionPage }
      { Id = "layout-containers"; Title = "Layout & Containers"; Kind = Catalog
        ControlIds = [ "stack"; "grid"; "dock"; "wrap"; "border"; "panel"; "scroll-viewer"; "split-view" ]
        View = layoutPage }
      { Id = "navigation-menus"; Title = "Navigation & Menus"; Kind = Catalog
        ControlIds = [ "tabs"; "menu"; "context-menu"; "toolbar"; "float-button"; "breadcrumb"; "steps"; "pagination"; "segmented"; "anchor"; "affix" ]
        View = navPage }
      { Id = "overlays"; Title = "Overlays"; Kind = Catalog
        ControlIds = [ "tooltip"; "dialog"; "overlay" ]
        View = overlaysPage }
      { Id = "feedback-status"; Title = "Feedback & Status"; Kind = Catalog
        ControlIds = [ "toast"; "progress-bar"; "spinner"; "validation-message"; "empty"; "skeleton"; "alert"; "result"; "drawer"; "popover"; "popconfirm"; "tour" ]
        View = feedbackPage }
      { Id = "data-collections"; Title = "Data Collections"; Kind = Catalog
        ControlIds = [ "list-view"; "tree-view"; "data-grid" ]
        View = dataPage }
      { Id = "charts-statistical"; Title = "Charts I — Statistical"; Kind = Catalog
        ControlIds = [ "line-chart"; "bar-chart"; "pie-chart"; "scatter-plot"; "area-chart"; "column-chart"; "histogram"; "box-plot" ]
        View = chartsStatPage }
      { Id = "charts-advanced"; Title = "Charts II — Advanced"; Kind = Catalog
        ControlIds = [ "heatmap"; "radar-chart"; "rose-chart"; "waterfall-chart"; "funnel-chart"; "gauge-chart"; "treemap"; "sunburst" ]
        View = chartsAdvPage }
      { Id = "graphs-custom"; Title = "Graphs & Custom"; Kind = Catalog
        ControlIds = [ "graph-view"; "sankey-diagram"; "chord-diagram"; "canvas"; "custom-control" ]
        View = graphsPage } ]
