namespace FS.GG.UI.Controls

open System
open FS.GG.UI.Scene
open FS.GG.UI.DesignSystem

/// Feature 189 (US2, FR-004/T018): `faithfulContent` (rich-family preview geometry dispatch) + the
/// `dataGridCells` projection, relocated **byte-identically** from `ControlInternals` (inline kind
/// `match` preserved; US3 transforms it into the registry painter table). `module internal`; opens the
/// geometry/prelude siblings. Compiled before `Control.fs` so the residual render/assembly resolve it.
module internal ContentRender =
    open ControlPrimitives
    open ChartGeometry
    open WidgetGeometry
    /// Dispatch a rich-family control to its faithful geometry (within `box`, below the title).
    let faithfulContent (theme: Theme) (box: Rect) (control: Control<'msg>) : Scene list =
        let label = control.Content |> Option.defaultValue ""
        let items = stringListOf "items" control
        // Feature 093 (E3): attached style classes + current VisualState for the migrated kinds.
        // `state` is `Normal` unless a `visualState` attribute is present, so the no-class default
        // case stays byte-identical to the prior procedural output (FR-005). The state rides the
        // control's attributes, so it travels through the keyed reconciler and a state-driven look
        // survives a sibling-shifting re-render under the retained identity (FR-006, SC-005).
        let classes = styleClassesOf control.Attributes
        let state = visualStateOf control.Attributes
        // Feature 129 (F4): the semantic intent — lowered to the `style` attribute by `Button.view`
        // (`Primitives.fs:99`) and, until now, never read by the renderer (so `Danger` ≡ `Primary`).
        // It is now extracted and threaded into the central resolver. A missing attribute defaults
        // to the neutral `"primary"`. The default (neutral) policy still ignores it, so this is
        // byte-identical under the default theme — but the value now reaches resolution (FR-002, R3).
        let intent = textValueOf "style" control |> Option.defaultValue "primary"
        match control.Kind with
        | "line-chart" -> lineGeom theme box (chartValues control)
        | "bar-chart" -> barGeom theme box (chartValues control)
        | "pie-chart" -> pieGeom theme box (chartValues control)
        | "scatter-plot" -> scatterGeom theme box (chartValues control)
        | "graph-view" -> graphGeom theme box (chartValues control)
        // Feature 133 (D2C.1) — net-new generic charts (theme-role-driven, no theme-identity branch).
        | "area-chart" -> areaGeom theme box (chartValues control)
        | "column-chart" -> columnGeom theme box (chartValues control)
        | "histogram" -> histogramGeom theme box (chartValues control)
        | "box-plot" -> boxPlotGeom theme box (chartValues control)
        | "heatmap" -> heatmapGeom theme box (chartValues control)
        | "radar-chart" -> radarGeom theme box (chartValues control)
        | "rose-chart" -> roseGeom theme box (chartValues control)
        | "waterfall-chart" -> waterfallGeom theme box (chartValues control)
        | "funnel-chart" -> funnelGeom theme box (chartValues control)
        | "gauge-chart" -> gaugeGeom theme box (floatValue "value" 0.5 control.Attributes)
        | "sankey-diagram" -> sankeyGeom theme box (chartValues control)
        | "chord-diagram" -> chordGeom theme box (chartValues control)
        | "treemap" -> treemapGeom theme box (chartValues control)
        | "sunburst" -> sunburstGeom theme box (chartValues control)
        | "list-view"
        | "list-box"
        | "multi-select-list"
        | "combo-box"
        | "tree-view"
        | "menu"
        | "context-menu" ->
            rowsGeom theme box (stringListOf "items" control) (stringListOf "selectedKeys" control |> Set.ofList)
        | "data-grid" -> gridGeom theme box (itemsOr [ "Name"; "Qty"; "Widget"; "12"; "Gadget"; "7" ] items)
        | "radio-group" -> radioGeom theme box classes state (stringListOf "items" control) (textValueOf "value" control)
        | "tabs" -> tabsGeom theme box (stringListOf "items" control) (textValueOf "value" control)
        | "slider" -> sliderGeom theme box classes state (floatValue "value" 0.5 control.Attributes)
        | "progress-bar" -> progressGeom theme box (floatValue "value" 0.0 control.Attributes)
        | "numeric-input" -> numericGeom theme box (floatValue "value" 0.0 control.Attributes)
        | "switch" -> switchGeom theme box classes state (boolValue "selected" false control.Attributes)
        | "check-box" -> checkboxGeom theme box classes state (boolValue "selected" false control.Attributes) label
        // command / button family
        | "button" -> buttonGeom theme box classes state "button" intent label
        | "icon-button" -> buttonGeom theme box classes state "icon-button" intent label
        | "badge" -> badgeGeom theme box label
        | "toggle-button" -> toggleGeom theme box (boolValue "selected" true control.Attributes) label
        | "split-button" -> splitGeom theme box label
        // layout / container family
        | "stack" -> stackGeom theme box items
        | "grid" -> gridLayoutGeom theme box items
        | "dock" -> dockGeom theme box items
        | "wrap" -> wrapGeom theme box items
        | "split-view" -> splitViewGeom theme box items
        | "toolbar" -> toolbarGeom theme box items
        | "panel" -> panelGeom theme box (if label = "" then "Panel content" else label)
        | "border" -> borderGeom theme box (if label = "" then "Bordered" else label)
        | "scroll-viewer" -> scrollViewerGeom theme box (if label = "" then "Scrollable content" else label)
        | "overlay" -> overlayGeom theme box (if label = "" then "Overlaid content" else label)
        | "date-picker"
        | "time-picker" -> pickerGeom theme box (control.Content |> Option.defaultValue control.Kind)
        | "color-picker" -> swatchGeom theme box
        | "spinner" -> spinnerGeom theme box
        | "image" -> imageGeom theme box (textValueOf "value" control |> Option.defaultValue "image")
        // text-input / rich-text / divider family (feature 082)
        | "text-box" -> textFieldGeom theme box classes state (textValueOf "value" control |> Option.defaultValue "")
        | "text-area" -> textAreaFieldGeom theme box (textValueOf "value" control |> Option.defaultValue "")
        | "rich-text" -> richTextGeom theme box (richTextRuns control)
        | "separator" -> separatorGeom theme box
        // Feature 132 (D2.1) — net-new Ant-overview controls. All paint flows from `theme` roles
        // (and, where intent matters, the resolver) — no branch on theme identity (FR-007, R4).
        | "tag" -> tagGeom theme box label
        | "avatar" -> avatarGeom theme box label
        | "card" -> cardGeom theme box label
        | "descriptions" -> descriptionsGeom theme box (stringListOf "items" control)
        | "statistic" -> statisticGeom theme box (textValueOf "value" control |> Option.orElse (control.Content) |> Option.defaultValue "")
        | "timeline" -> timelineGeom theme box (stringListOf "items" control)
        | "empty" -> emptyGeom theme box label
        | "skeleton" -> skeletonGeom theme box
        | "qr-code" -> qrCodeGeom theme box
        | "watermark" -> watermarkGeom theme box label
        | "alert" -> alertGeom theme box label
        | "result" -> resultGeom theme box label
        | "drawer" -> drawerGeom theme box label
        | "popover" -> popoverGeom theme box label Plain
        | "popconfirm" -> popoverGeom theme box (if label = "" then "Confirm?" else label) WithActions
        | "tour" -> popoverGeom theme box (if label = "" then "Step 1 of 3" else label) WithActions
        | "float-button" -> floatButtonGeom theme box label
        | "breadcrumb" -> breadcrumbGeom theme box (stringListOf "items" control)
        | "steps" -> stepsGeom theme box (stringListOf "items" control)
        | "pagination" -> paginationGeom theme box (int (floatValue "value" 4.0 control.Attributes))
        | "segmented" -> segmentedGeom theme box (stringListOf "items" control)
        | "anchor" -> anchorGeom theme box (stringListOf "items" control)
        | "affix" -> affixGeom theme box label
        | "collapse" -> collapseGeom theme box (stringListOf "items" control)
        | "rate" -> rateGeom theme box (floatValue "value" 0.0 control.Attributes)
        | "carousel" -> carouselGeom theme box (stringListOf "items" control)
        | "calendar" -> calendarGeom theme box
        | "cascader" -> cascaderGeom theme box (stringListOf "items" control)
        | "auto-complete" -> autoCompleteGeom theme box (textValueOf "value" control |> Option.defaultValue "")
        | "upload" -> uploadGeom theme box label
        | "icon" ->
            let name =
                control.Content
                |> Option.orElseWith (fun () -> textValueOf "text" control)
                |> Option.defaultValue "icon"
            iconGeom theme box name
        | other -> emptyState theme box other

    /// Feature 113 (Phase 5) — the resolved cell/header data the `data-grid` row/column projection
    /// (`gridGeom`) consumes: the control's `items` attribute, or the same sample fallback
    /// `faithfulContent` substitutes when none is authored. This is the projection's sole control-borne
    /// input; the memoization seam (`RetainedRender.memoize`) folds it with the theme + evaluated box
    /// into the deterministic dependency value (an equal value ⇒ a byte-identical projection, FR-006).
    let dataGridCells (control: Control<'msg>) : string list =
        itemsOr [ "Name"; "Qty"; "Widget"; "12"; "Gadget"; "7" ] (stringListOf "items" control)
