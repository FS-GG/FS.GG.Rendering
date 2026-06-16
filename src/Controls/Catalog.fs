namespace FS.GG.UI.Controls

open System

type CatalogAccessibility =
    { Role: string
      NameSource: string
      StateMetadata: string list
      FocusBehavior: string
      KeyboardOperation: string
      ContrastEvidence: string }

type ControlDefinition =
    { Id: string
      DisplayName: string
      Category: string
      Module: string
      Purpose: string
      RequiredAttributes: string list
      CommonAttributes: string list
      Events: string list
      VisualStates: string list
      Accessibility: CatalogAccessibility
      Examples: string list
      Tests: string list
      Evidence: string list
      SupportStatus: string
      Owner: string }

module Catalog =
    let accessibility role =
        { Role = role
          NameSource = "text/value/accessibility attribute"
          StateMetadata = [ "enabled"; "visible"; "selected"; "validation"; "loading" ]
          FocusBehavior = "catalog focus order"
          KeyboardOperation = "Tab navigation and Enter/Space activation where interactive"
          ContrastEvidence = "readiness/layout-rendering.md" }

    let definition id displayName category moduleName purpose required common events states role =
        { Id = id
          DisplayName = displayName
          Category = category
          Module = moduleName
          Purpose = purpose
          RequiredAttributes = required
          CommonAttributes = common
          Events = events
          VisualStates = states
          Accessibility = accessibility role
          Examples = [ "samples/ControlsGallery/Program.fs" ]
          Tests =
            [ "tests/Controls.Tests/CatalogTests.fs"
              "tests/Controls.Tests/SemanticTests.fs"
              "tests/Controls.Tests/InteractionTests.fs"
              "tests/Controls.Tests/AccessibilityTests.fs"
              "tests/Controls.Tests/RenderingTests.fs" ]
          Evidence =
            [ "specs/010-skia-controls-library/readiness/control-catalog.md"
              "specs/010-skia-controls-library/readiness/layout-rendering.md"
              "specs/011-controls-boundary-refactor/readiness/control-catalog.md" ]
          SupportStatus = "supported"
          Owner = "controls" }

    let common = [ "enabled"; "visible"; "width"; "height"; "padding"; "style"; "theme"; "accessibility" ]
    let states = [ "normal"; "disabled"; "hover"; "pressed"; "focused"; "selected"; "validation"; "loading" ]
    let chartDataGridEvidence = [ "specs/011-controls-boundary-refactor/readiness/chart-datagrid-controls.md" ]

    let withChartDataGridEvidence row =
        { row with Evidence = row.Evidence @ chartDataGridEvidence }

    let supportedControls =
        [
          // BEGIN GENERATED: typed-catalog/text-block
          definition "text-block" "Text Block" "display" "TextBlock" "Static model-owned text display." [ "text" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/text-block
          // BEGIN GENERATED: typed-catalog/rich-text
          definition "rich-text" "Rich Text" "display" "RichText" "Skia-specific rich text display with measurement, clipping, effects, diagnostics, and accessibility metadata." [ "runs" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/rich-text
          // BEGIN GENERATED: typed-catalog/label
          definition "label" "Label" "display" "Label" "Short form label text." [ "text" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/label
          // BEGIN GENERATED: typed-catalog/image
          definition "image" "Image" "display" "Image" "Image placeholder or drawing-surface reference." [ "value" ] common [] states "Image"
          // END GENERATED: typed-catalog/image
          // BEGIN GENERATED: typed-catalog/icon
          definition "icon" "Icon" "display" "Icon" "Named icon glyph or product symbol." [ "text" ] common [] states "Image"
          // END GENERATED: typed-catalog/icon
          // BEGIN GENERATED: typed-catalog/separator
          definition "separator" "Separator" "display" "Separator" "Visual divider between regions." [] common [] states "StaticText"
          // END GENERATED: typed-catalog/separator
          // BEGIN GENERATED: typed-catalog/badge
          definition "badge" "Badge" "display" "Badge" "Compact status label." [ "text" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/badge
          // BEGIN GENERATED: typed-catalog/button
          definition "button" "Button" "input" "Button" "Pointer and keyboard activatable command." [ "text" ] common [ "onClick" ] states "Button"
          // END GENERATED: typed-catalog/button
          // BEGIN GENERATED: typed-catalog/icon-button
          definition "icon-button" "Icon Button" "input" "IconButton" "Icon-only activatable command." [ "text" ] common [ "onClick" ] states "Button"
          // END GENERATED: typed-catalog/icon-button
          // BEGIN GENERATED: typed-catalog/text-box
          definition "text-box" "Text Box" "input" "TextBox" "Plain single-line text entry." [ "value" ] common [ "onChanged" ] states "TextBox"
          // END GENERATED: typed-catalog/text-box
          // BEGIN GENERATED: typed-catalog/text-area
          definition "text-area" "Text Area" "input" "TextArea" "Plain multi-line text entry." [ "value" ] common [ "onChanged" ] states "TextBox"
          // END GENERATED: typed-catalog/text-area
          // BEGIN GENERATED: typed-catalog/numeric-input
          definition "numeric-input" "Numeric Input" "input" "NumericInput" "Model-owned numeric value editor." [ "value" ] common [ "onChanged" ] states "TextBox"
          // END GENERATED: typed-catalog/numeric-input
          // BEGIN GENERATED: typed-catalog/check-box
          definition "check-box" "Check Box" "selection" "CheckBox" "Boolean choice with checked state." [ "text" ] common [ "onChanged" ] states "CheckBox"
          // END GENERATED: typed-catalog/check-box
          // BEGIN GENERATED: typed-catalog/radio-group
          definition "radio-group" "Radio Group" "selection" "RadioGroup" "Single selection from a visible option set." [ "items" ] common [ "onChanged" ] states "RadioGroup"
          // END GENERATED: typed-catalog/radio-group
          // BEGIN GENERATED: typed-catalog/switch
          definition "switch" "Switch" "selection" "Switch" "Compact Boolean setting." [] common [ "onChanged" ] states "CheckBox"
          // END GENERATED: typed-catalog/switch
          // BEGIN GENERATED: typed-catalog/slider
          definition "slider" "Slider" "input" "Slider" "Continuous numeric value selection." [ "value" ] common [ "onChanged" ] states "Slider"
          // END GENERATED: typed-catalog/slider
          // BEGIN GENERATED: typed-catalog/list-view
          definition "list-view" "List View" "data" "Collections" "Bounded visible-range list display." [ "items" ] common [ "onSelected" ] states "List"
          // END GENERATED: typed-catalog/list-view
          // BEGIN GENERATED: typed-catalog/list-box
          definition "list-box" "List Box" "selection" "Collections" "Single-selection list box." [ "items" ] common [ "onSelected" ] states "List"
          // END GENERATED: typed-catalog/list-box
          // BEGIN GENERATED: typed-catalog/multi-select-list
          definition "multi-select-list" "Multi Select List" "selection" "Collections" "Multiple-selection list with model-owned selected keys." [ "items" ] common [ "onChanged" ] states "List"
          // END GENERATED: typed-catalog/multi-select-list
          // BEGIN GENERATED: typed-catalog/combo-box
          definition "combo-box" "Combo Box" "selection" "Collections" "Compact selection list." [ "items" ] common [ "onChanged" ] states "List"
          // END GENERATED: typed-catalog/combo-box
          // BEGIN GENERATED: typed-catalog/tree-view
          definition "tree-view" "Tree View" "data" "Collections" "Hierarchical item display." [ "items" ] common [ "onSelected" ] states "List"
          // END GENERATED: typed-catalog/tree-view
          // BEGIN GENERATED: typed-catalog/data-grid
          definition "data-grid" "Data Grid" "data" "DataGrid" "Table-like bounded visible-range data control with product-owned rows, selection, focus, sort, and filter metadata." [ "columns"; "rows" ] common [ "onSelected"; "onFocusChanged"; "onSortChanged" ] states "Grid"
          |> withChartDataGridEvidence
          // END GENERATED: typed-catalog/data-grid
          // BEGIN GENERATED: typed-catalog/stack
          definition "stack" "Stack" "layout" "Stack" "Ordered vertical or horizontal child composition." [ "children" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/stack
          // BEGIN GENERATED: typed-catalog/grid
          definition "grid" "Grid" "layout" "Grid" "Structured child composition." [ "children" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/grid
          // BEGIN GENERATED: typed-catalog/dock
          definition "dock" "Dock" "layout" "Dock" "Docked region composition." [ "children" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/dock
          // BEGIN GENERATED: typed-catalog/wrap
          definition "wrap" "Wrap" "layout" "Wrap" "Wrapping child layout." [ "children" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/wrap
          // BEGIN GENERATED: typed-catalog/border
          definition "border" "Border" "layout" "Border" "Single child with border and padding." [ "child" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/border
          // BEGIN GENERATED: typed-catalog/panel
          definition "panel" "Panel" "layout" "Panel" "General-purpose child surface." [ "children" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/panel
          // BEGIN GENERATED: typed-catalog/scroll-viewer
          definition "scroll-viewer" "Scroll Viewer" "layout" "Collections" "Scrollable child viewport." [ "child" ] common [ "onChanged" ] states "List"
          // END GENERATED: typed-catalog/scroll-viewer
          // BEGIN GENERATED: typed-catalog/split-view
          definition "split-view" "Split View" "layout" "Collections" "Resizable two-region layout." [ "children" ] common [ "onChanged" ] states "StaticText"
          // END GENERATED: typed-catalog/split-view
          // BEGIN GENERATED: typed-catalog/tabs
          definition "tabs" "Tabs" "navigation" "Tabs" "Model-owned active page selection." [ "items" ] common [ "onChanged" ] states "Tab"
          // END GENERATED: typed-catalog/tabs
          // BEGIN GENERATED: typed-catalog/menu
          definition "menu" "Menu" "navigation" "Menu" "Command menu selection." [ "items" ] common [ "onSelected" ] states "Menu"
          // END GENERATED: typed-catalog/menu
          // BEGIN GENERATED: typed-catalog/context-menu
          definition "context-menu" "Context Menu" "navigation" "Menu" "Contextual command menu." [ "items" ] common [ "onSelected" ] states "Menu"
          // END GENERATED: typed-catalog/context-menu
          // BEGIN GENERATED: typed-catalog/toolbar
          definition "toolbar" "Toolbar" "navigation" "Toolbar" "Compact command group." [ "children" ] common [ "onClick" ] states "Menu"
          // END GENERATED: typed-catalog/toolbar
          // BEGIN GENERATED: typed-catalog/tooltip
          definition "tooltip" "Tooltip" "overlay" "Tooltip" "Auxiliary hover/focus explanation." [ "text" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/tooltip
          // BEGIN GENERATED: typed-catalog/dialog
          definition "dialog" "Dialog" "overlay" "Dialog" "Modal content region." [ "children" ] common [ "onSelected" ] states "Dialog"
          // END GENERATED: typed-catalog/dialog
          // BEGIN GENERATED: typed-catalog/toast
          definition "toast" "Toast" "feedback" "Toast" "Transient status message." [ "text" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/toast
          // BEGIN GENERATED: typed-catalog/overlay
          definition "overlay" "Overlay" "overlay" "Overlay" "Layered child content." [ "child" ] common [] states "Dialog"
          // END GENERATED: typed-catalog/overlay
          // BEGIN GENERATED: typed-catalog/progress-bar
          definition "progress-bar" "Progress Bar" "feedback" "ProgressBar" "Determinate progress indicator." [ "value" ] common [] states "Progress"
          // END GENERATED: typed-catalog/progress-bar
          // BEGIN GENERATED: typed-catalog/spinner
          definition "spinner" "Spinner" "feedback" "Spinner" "Indeterminate progress indicator." [] common [] states "Progress"
          // END GENERATED: typed-catalog/spinner
          // BEGIN GENERATED: typed-catalog/validation-message
          definition "validation-message" "Validation Message" "feedback" "ValidationMessage" "Validation text tied to model state." [ "text" ] common [] states "StaticText"
          // END GENERATED: typed-catalog/validation-message
          // BEGIN GENERATED: typed-catalog/line-chart
          definition "line-chart" "Line Chart" "chart" "LineChart" "Controls-owned line data visualization." [ "series" ] common [ "onSelected" ] states "Chart"
          |> withChartDataGridEvidence
          // END GENERATED: typed-catalog/line-chart
          // BEGIN GENERATED: typed-catalog/bar-chart
          definition "bar-chart" "Bar Chart" "chart" "BarChart" "Controls-owned bar data visualization." [ "series" ] common [ "onSelected" ] states "Chart"
          |> withChartDataGridEvidence
          // END GENERATED: typed-catalog/bar-chart
          // BEGIN GENERATED: typed-catalog/pie-chart
          definition "pie-chart" "Pie Chart" "chart" "PieChart" "Controls-owned part-to-whole visualization." [ "values" ] common [ "onSelected" ] states "Chart"
          |> withChartDataGridEvidence
          // END GENERATED: typed-catalog/pie-chart
          // BEGIN GENERATED: typed-catalog/scatter-plot
          definition "scatter-plot" "Scatter Plot" "chart" "ScatterPlot" "Controls-owned point cloud visualization." [ "series" ] common [ "onSelected" ] states "Chart"
          |> withChartDataGridEvidence
          // END GENERATED: typed-catalog/scatter-plot
          // BEGIN GENERATED: typed-catalog/graph-view
          definition "graph-view" "Graph View" "graph" "GraphView" "Controls-owned node and edge visualization." [ "nodes" ] common [ "onSelected" ] states "Graph"
          |> withChartDataGridEvidence
          // END GENERATED: typed-catalog/graph-view
          // BEGIN GENERATED: typed-catalog/custom-control
          definition "custom-control" "Custom Control" "custom" "CustomControl" "Product-owned wrapper; renderTree paints a labeled placeholder, not the custom Render/Draw content — build must-show geometry from primitive controls (Border/TextBlock/Stack)." [] common [ "onCustom" ] states "Custom"
          // END GENERATED: typed-catalog/custom-control
          // BEGIN GENERATED: typed-catalog/toggle-button
          definition "toggle-button" "Toggle Button" "input" "ToggleButton" "On/off command with product-owned pressed state." [ "text" ] common [ "onToggle" ] states "Button"
          // END GENERATED: typed-catalog/toggle-button
          // BEGIN GENERATED: typed-catalog/split-button
          definition "split-button" "Split Button" "input" "SplitButton" "Primary action plus a popup menu of secondary commands." [ "text" ] common [ "onClick"; "onSelected" ] states "Menu"
          // END GENERATED: typed-catalog/split-button
          // BEGIN GENERATED: typed-catalog/date-picker
          definition "date-picker" "Date Picker" "input" "DatePicker" "Typed date entry with a popup calendar." [] common [ "onChange" ] states "TextBox"
          // END GENERATED: typed-catalog/date-picker
          // BEGIN GENERATED: typed-catalog/time-picker
          definition "time-picker" "Time Picker" "input" "TimePicker" "Typed time entry with hour and minute segments." [] common [ "onChange" ] states "TextBox"
          // END GENERATED: typed-catalog/time-picker
          // BEGIN GENERATED: typed-catalog/color-picker
          definition "color-picker" "Color Picker" "selection" "ColorPicker" "Palette swatch color selection." [ "swatches" ] common [ "onSelected" ] states "List"
          // END GENERATED: typed-catalog/color-picker
          // Feature 132 (D2.1): net-new generic Ant-overview controls (Display2/Feedback2/Navigation2/
          // Interactive2/DataEntry2). Hand-maintained GENERATED rows (the source-repo typed-catalog
          // generator is governance material, excluded at import — see PROVENANCE.md). Mirrored in catalog.yml.
          // BEGIN GENERATED: typed-catalog/tag
          definition "tag" "Tag" "display" "Tag" "Compact coloured status chip." [] common [ "onClose" ] states "StaticText"
          // END GENERATED: typed-catalog/tag
          // BEGIN GENERATED: typed-catalog/avatar
          definition "avatar" "Avatar" "display" "Avatar" "Round monogram or image stand-in." [] common [] states "Image"
          // END GENERATED: typed-catalog/avatar
          // BEGIN GENERATED: typed-catalog/card
          definition "card" "Card" "display" "Card" "Framed content surface with a header band." [] common [] states "Group"
          // END GENERATED: typed-catalog/card
          // BEGIN GENERATED: typed-catalog/descriptions
          definition "descriptions" "Descriptions" "display" "Descriptions" "Label/value term list." [] common [] states "Group"
          // END GENERATED: typed-catalog/descriptions
          // BEGIN GENERATED: typed-catalog/statistic
          definition "statistic" "Statistic" "display" "Statistic" "Emphasised metric over a caption." [] common [] states "StaticText"
          // END GENERATED: typed-catalog/statistic
          // BEGIN GENERATED: typed-catalog/timeline
          definition "timeline" "Timeline" "display" "Timeline" "Vertical dotted event rail." [] common [] states "Group"
          // END GENERATED: typed-catalog/timeline
          // BEGIN GENERATED: typed-catalog/empty
          definition "empty" "Empty" "feedback" "Empty" "Muted no-data placeholder." [] common [] states "StaticText"
          // END GENERATED: typed-catalog/empty
          // BEGIN GENERATED: typed-catalog/skeleton
          definition "skeleton" "Skeleton" "feedback" "Skeleton" "Grey loading placeholder bars." [] common [] states "StaticText"
          // END GENERATED: typed-catalog/skeleton
          // BEGIN GENERATED: typed-catalog/qr-code
          definition "qr-code" "QR Code" "display" "QrCode" "Deterministic QR module grid." [] common [] states "Image"
          // END GENERATED: typed-catalog/qr-code
          // BEGIN GENERATED: typed-catalog/watermark
          definition "watermark" "Watermark" "display" "Watermark" "Faint repeated brand text overlay." [] common [] states "StaticText"
          // END GENERATED: typed-catalog/watermark
          // BEGIN GENERATED: typed-catalog/alert
          definition "alert" "Alert" "feedback" "Alert" "Coloured information banner." [] common [ "onClose" ] states "Alert"
          // END GENERATED: typed-catalog/alert
          // BEGIN GENERATED: typed-catalog/result
          definition "result" "Result" "feedback" "Result" "Centred operation-outcome panel." [] common [] states "StaticText"
          // END GENERATED: typed-catalog/result
          // BEGIN GENERATED: typed-catalog/drawer
          definition "drawer" "Drawer" "feedback" "Drawer" "Edge-anchored sliding panel." [] common [ "onClose" ] states "Dialog"
          // END GENERATED: typed-catalog/drawer
          // BEGIN GENERATED: typed-catalog/popover
          definition "popover" "Popover" "feedback" "Popover" "Floating callout anchored to a trigger." [] common [] states "Tooltip"
          // END GENERATED: typed-catalog/popover
          // BEGIN GENERATED: typed-catalog/popconfirm
          definition "popconfirm" "Popconfirm" "feedback" "Popconfirm" "Confirm callout with accept/cancel." [] common [ "onConfirm"; "onCancel" ] states "Dialog"
          // END GENERATED: typed-catalog/popconfirm
          // BEGIN GENERATED: typed-catalog/tour
          definition "tour" "Tour" "feedback" "Tour" "Guided multi-step highlight callout." [] common [] states "Dialog"
          // END GENERATED: typed-catalog/tour
          // BEGIN GENERATED: typed-catalog/float-button
          definition "float-button" "Float Button" "navigation" "FloatButton" "Circular floating action button." [] common [ "onClick" ] states "Button"
          // END GENERATED: typed-catalog/float-button
          // BEGIN GENERATED: typed-catalog/breadcrumb
          definition "breadcrumb" "Breadcrumb" "navigation" "Breadcrumb" "Separated path trail." [] common [] states "Navigation"
          // END GENERATED: typed-catalog/breadcrumb
          // BEGIN GENERATED: typed-catalog/steps
          definition "steps" "Steps" "navigation" "Steps" "Numbered horizontal progress steps." [] common [] states "Group"
          // END GENERATED: typed-catalog/steps
          // BEGIN GENERATED: typed-catalog/pagination
          definition "pagination" "Pagination" "navigation" "Pagination" "Page-number chip row." [] common [ "onChange" ] states "Navigation"
          // END GENERATED: typed-catalog/pagination
          // BEGIN GENERATED: typed-catalog/segmented
          definition "segmented" "Segmented" "navigation" "Segmented" "Connected single-select segment row." [] common [ "onChange" ] states "TabList"
          // END GENERATED: typed-catalog/segmented
          // BEGIN GENERATED: typed-catalog/anchor
          definition "anchor" "Anchor" "navigation" "Anchor" "Vertical in-page link list." [] common [] states "Navigation"
          // END GENERATED: typed-catalog/anchor
          // BEGIN GENERATED: typed-catalog/affix
          definition "affix" "Affix" "navigation" "Affix" "Pinned-to-edge bar." [] common [] states "Group"
          // END GENERATED: typed-catalog/affix
          // BEGIN GENERATED: typed-catalog/collapse
          definition "collapse" "Collapse" "display" "Collapse" "Stacked expandable section headers." [] common [ "onChange" ] states "Group"
          // END GENERATED: typed-catalog/collapse
          // BEGIN GENERATED: typed-catalog/rate
          definition "rate" "Rate" "input" "Rate" "Star rating row." [] common [ "onChange" ] states "Slider"
          // END GENERATED: typed-catalog/rate
          // BEGIN GENERATED: typed-catalog/carousel
          definition "carousel" "Carousel" "display" "Carousel" "Rotating slide deck." [] common [] states "Group"
          // END GENERATED: typed-catalog/carousel
          // BEGIN GENERATED: typed-catalog/calendar
          definition "calendar" "Calendar" "display" "Calendar" "Month day-cell grid." [] common [ "onChange" ] states "Grid"
          // END GENERATED: typed-catalog/calendar
          // BEGIN GENERATED: typed-catalog/cascader
          definition "cascader" "Cascader" "selection" "Cascader" "Cascading selection columns." [] common [ "onChange" ] states "ComboBox"
          // END GENERATED: typed-catalog/cascader
          // BEGIN GENERATED: typed-catalog/auto-complete
          definition "auto-complete" "Auto Complete" "input" "AutoComplete" "Text field with a suggestion dropdown." [] common [ "onChange" ] states "ComboBox"
          // END GENERATED: typed-catalog/auto-complete
          // BEGIN GENERATED: typed-catalog/upload
          definition "upload" "Upload" "input" "Upload" "File drop zone with an upload action." [] common [ "onChange" ] states "Button"
          // END GENERATED: typed-catalog/upload
        ]

    let standardSchema =
        [ { Kind = StandardControlKind.TextBlock
            RequiredAttributes = [ StandardAttributeName.Text ]
            SupportedAttributes = [ StandardAttributeName.Text; StandardAttributeName.Custom "accessibility" ]
            SupportedEvents = []
            CustomAllowed = false }
          { Kind = StandardControlKind.Button
            RequiredAttributes = [ StandardAttributeName.Text ]
            SupportedAttributes = [ StandardAttributeName.Text ]
            SupportedEvents = [ StandardEventKind.Click ]
            CustomAllowed = false }
          { Kind = StandardControlKind.TextBox
            RequiredAttributes = [ StandardAttributeName.Value ]
            SupportedAttributes = [ StandardAttributeName.Value ]
            SupportedEvents = [ StandardEventKind.Changed ]
            CustomAllowed = false }
          { Kind = StandardControlKind.LineChart
            RequiredAttributes = [ StandardAttributeName.Series ]
            SupportedAttributes = [ StandardAttributeName.Series ]
            SupportedEvents = [ StandardEventKind.Selected ]
            CustomAllowed = false }
          { Kind = StandardControlKind.BarChart
            RequiredAttributes = [ StandardAttributeName.Series ]
            SupportedAttributes = [ StandardAttributeName.Series ]
            SupportedEvents = [ StandardEventKind.Selected ]
            CustomAllowed = false }
          { Kind = StandardControlKind.PieChart
            RequiredAttributes = [ StandardAttributeName.Values ]
            SupportedAttributes = [ StandardAttributeName.Values ]
            SupportedEvents = [ StandardEventKind.Selected ]
            CustomAllowed = false }
          { Kind = StandardControlKind.ScatterPlot
            RequiredAttributes = [ StandardAttributeName.Series ]
            SupportedAttributes = [ StandardAttributeName.Series ]
            SupportedEvents = [ StandardEventKind.Selected ]
            CustomAllowed = false }
          { Kind = StandardControlKind.GraphView
            RequiredAttributes = [ StandardAttributeName.Nodes ]
            SupportedAttributes = [ StandardAttributeName.Nodes ]
            SupportedEvents = [ StandardEventKind.Selected ]
            CustomAllowed = false }
          { Kind = StandardControlKind.DataGrid
            RequiredAttributes = [ StandardAttributeName.Columns; StandardAttributeName.Rows ]
            SupportedAttributes = [ StandardAttributeName.Columns; StandardAttributeName.Rows; StandardAttributeName.VisibleRange; StandardAttributeName.SelectedRows; StandardAttributeName.FocusedCell ]
            SupportedEvents = [ StandardEventKind.Selected; StandardEventKind.FocusChanged; StandardEventKind.SortChanged ]
            CustomAllowed = false } ]

    let knownControlKinds () =
        standardSchema |> List.map _.Kind

    let requiredAttributes kind =
        standardSchema
        |> List.tryFind (fun schema -> schema.Kind = kind)
        |> Option.map _.RequiredAttributes
        |> Option.defaultValue []

    let supportedAttributes kind =
        standardSchema
        |> List.tryFind (fun schema -> schema.Kind = kind)
        |> Option.map _.SupportedAttributes
        |> Option.defaultValue []

    let supportedEvents kind =
        standardSchema
        |> List.tryFind (fun schema -> schema.Kind = kind)
        |> Option.map _.SupportedEvents
        |> Option.defaultValue []

    let validateStandardControl (control: Control<'msg>) =
        let standardAttributeName name =
            match name with
            | StandardAttributeName.Text -> "text"
            | StandardAttributeName.Value -> "value"
            | StandardAttributeName.Children -> "children"
            | StandardAttributeName.Series -> "series"
            | StandardAttributeName.Values -> "values"
            | StandardAttributeName.Columns -> "columns"
            | StandardAttributeName.Rows -> "rows"
            | StandardAttributeName.Items -> "items"
            | StandardAttributeName.Nodes -> "nodes"
            | StandardAttributeName.VisibleRange -> "visibleRange"
            | StandardAttributeName.SelectedRows -> "selectedRows"
            | StandardAttributeName.FocusedCell -> "focusedCell"
            | StandardAttributeName.Custom value -> value

        let standardAttributeFromName name =
            match name with
            | "text" -> Some StandardAttributeName.Text
            | "value" -> Some StandardAttributeName.Value
            | "children" -> Some StandardAttributeName.Children
            | "series" -> Some StandardAttributeName.Series
            | "values" -> Some StandardAttributeName.Values
            | "columns" -> Some StandardAttributeName.Columns
            | "rows" -> Some StandardAttributeName.Rows
            | "items" -> Some StandardAttributeName.Items
            | "nodes" -> Some StandardAttributeName.Nodes
            | "visibleRange" -> Some StandardAttributeName.VisibleRange
            | "selectedRows" -> Some StandardAttributeName.SelectedRows
            | "focusedCell" -> Some StandardAttributeName.FocusedCell
            | _ -> None

        let standardEventFromName name =
            match name with
            | "onClick" -> Some StandardEventKind.Click
            | "onChanged" -> Some StandardEventKind.Changed
            | "onSelected" -> Some StandardEventKind.Selected
            | "onFocusChanged" -> Some StandardEventKind.FocusChanged
            | "onSortChanged" -> Some StandardEventKind.SortChanged
            | _ -> None

        let schema =
            standardSchema
            |> List.tryFind (fun schema ->
                match schema.Kind, control.Kind with
                | StandardControlKind.TextBlock, "text-block"
                | StandardControlKind.Button, "button"
                | StandardControlKind.TextBox, "text-box"
                | StandardControlKind.LineChart, "line-chart"
                | StandardControlKind.BarChart, "bar-chart"
                | StandardControlKind.PieChart, "pie-chart"
                | StandardControlKind.ScatterPlot, "scatter-plot"
                | StandardControlKind.GraphView, "graph-view"
                | StandardControlKind.DataGrid, "data-grid" -> true
                | _ -> false)

        match schema with
        | None -> [ Diagnostics.customExtension control.Kind "custom-control" ]
        | Some schema ->
            let missing =
                schema.RequiredAttributes
                |> List.choose (fun required ->
                    let name = standardAttributeName required
                    if control.Attributes |> List.exists (fun attr -> attr.Name = name) then
                        None
                    else
                        Some(Diagnostics.missingStandardAttribute schema.Kind required))

            let unsupportedAttributes =
                control.Attributes
                |> List.choose (fun attr ->
                    match standardAttributeFromName attr.Name with
                    | Some standard when attr.Category <> Event && not (schema.SupportedAttributes |> List.contains standard) ->
                        Some(Diagnostics.unsupportedStandardAttribute schema.Kind standard)
                    | _ -> None)

            let unsupportedEvents =
                control.Attributes
                |> List.choose (fun attr ->
                    match standardEventFromName attr.Name with
                    | Some standard when attr.Category = Event && not (schema.SupportedEvents |> List.contains standard) ->
                        Some(Diagnostics.unsupportedStandardEvent schema.Kind standard)
                    | _ -> None)

            missing @ unsupportedAttributes @ unsupportedEvents

    let supportedCount () =
        supportedControls
        |> List.filter (fun row -> row.SupportStatus = "supported")
        |> List.length

    let categories () =
        supportedControls
        |> List.map _.Category
        |> List.distinct
        |> List.sort

    let validate () =
        [ if supportedCount () < 30 then
              yield Diagnostics.create None "catalog" MissingRequiredAttribute Error "Catalog has fewer than 30 supported controls."

          for row in supportedControls do
              if row.Owner <> "controls" then
                  yield Diagnostics.create (Some row.Id) row.Id StaleGeneratedReference Error "Catalog row is not Controls-owned."
              if String.IsNullOrWhiteSpace row.Purpose then
                  yield Diagnostics.create (Some row.Id) row.Id MissingRequiredAttribute Error "Catalog row is missing purpose."
              if row.VisualStates.IsEmpty then
                  yield Diagnostics.create (Some row.Id) row.Id MissingRequiredAttribute Error "Catalog row is missing visual states."
              if row.Examples.IsEmpty || row.Tests.IsEmpty || row.Evidence.IsEmpty then
                  yield Diagnostics.create (Some row.Id) row.Id MissingRequiredAttribute Error "Catalog row is missing examples, tests, or evidence."
              if row.Accessibility.Role.Trim() = "" then
                  yield Diagnostics.create (Some row.Id) row.Id MissingAccessibilityMetadata Error "Catalog row is missing accessibility role."
          for row in supportedControls do
              if row.Id = "data-grid" && row.Category <> "data" && row.Category <> "collection" then
                  yield Diagnostics.create (Some row.Id) row.Id UnsupportedStateCombination Error "DataGrid must be categorized as data or collection."
              if row.Id = "data-grid" && row.Module <> "DataGrid" then
                  yield Diagnostics.create (Some row.Id) row.Id StaleGeneratedReference Error "DataGrid catalog row must be owned by the Controls DataGrid module."
              if row.Id = "rich-text" && row.Module <> "RichText" then
                  yield Diagnostics.create (Some row.Id) row.Id MissingRequiredAttribute Error "Rich text catalog row must reference the RichText module." ]

    let markdownSummary () =
        [ "# Control Catalog"
          ""
          $"Supported controls: {supportedCount ()}"
          ""
          "| Id | Category | Module | Events |"
          "|----|----------|--------|--------|"
          yield!
              supportedControls
              |> List.map (fun row ->
                  let events = String.concat ", " row.Events
                  $"| {row.Id} | {row.Category} | {row.Module} | {events} |") ]
        |> String.concat Environment.NewLine
