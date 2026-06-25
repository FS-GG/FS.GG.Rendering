namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

/// Feature 183 (US1 / FR-001) — the single internal source of truth for per-`Kind` dispatch.
///
/// Before this module the same `Control.Kind: string` was switched on in ~13 parallel, drifting sites
/// across `Control.fs`/`Inspection.fs`/`Accessibility.fs`/`ControlRuntime.fs`/`RetainedRender.fs`. They
/// now call the functions here, so adding a kind is a single-site change and the catalog↔registry
/// completeness test (SC-001) catches an omission. The module is compiled **before** its consumers
/// (Controls.fsproj `<Compile Include>` order) so there is no back-edge (FR-011); it is `internal`
/// (no public surface, no bump — Tier 2) and reached by the test assembly via `InternalsVisibleTo`.
///
/// `Control.Kind` stays a public `string`. The functions are authored to reproduce the *exact* prior
/// match expressions — including arms for non-catalog runtime kinds (`popup`, `table`, `dialog`,
/// `data-grid-row`, `data-grid-header`) — so behavior is byte-identical (FR-005). The painter dispatch
/// (`faithfulContent`) and the required-attribute validation are **not** here (FR-010 retentions: a
/// back-edge to `Control.fs`' private geoms / the `Catalog.fs` row SSOT respectively — see
/// `readiness/post-change/retentions.md`).
module internal ControlKindRegistry =

    /// Which attribute a chart kind pulls its data points from (`chartValues`, Control.fs:502).
    type ChartDataSource =
        | Series
        | Values
        | GraphNodes

    /// The virtualization role of a kind for `RetainedRender.countVirtual` (RetainedRender.fs:1732).
    type VirtualizationRole =
        | Grid
        | GridRow

    // --- the dispatch functions (verbatim relocations of the prior per-site matches) ---

    /// `richFamilies` (Control.fs:550) — kinds that lower to control-specific faithful geometry and
    /// fill the preview canvas (304×132) rather than the box+label fallback.
    let private richFamilies =
        Set.ofList
            [ "line-chart"; "bar-chart"; "pie-chart"; "scatter-plot"; "graph-view"
              "list-view"; "list-box"; "multi-select-list"; "combo-box"; "tree-view"; "data-grid"
              "menu"; "context-menu"; "radio-group"; "tabs"
              "slider"; "progress-bar"; "numeric-input"; "switch"; "check-box"
              "button"; "icon-button"; "badge"; "toggle-button"; "split-button"
              "date-picker"; "time-picker"; "color-picker"; "spinner"; "image"; "icon"
              "stack"; "grid"; "dock"; "wrap"; "panel"; "border"; "scroll-viewer"
              "split-view"; "toolbar"; "overlay"
              "text-box"; "text-area"; "rich-text"; "separator"
              "tag"; "avatar"; "card"; "descriptions"; "statistic"; "timeline"; "empty"; "skeleton"
              "qr-code"; "watermark"; "alert"; "result"; "drawer"; "popover"; "popconfirm"; "tour"
              "float-button"; "breadcrumb"; "steps"; "pagination"; "segmented"; "anchor"; "affix"
              "collapse"; "rate"; "carousel"; "calendar"; "cascader"; "auto-complete"; "upload"
              "area-chart"; "column-chart"; "histogram"; "box-plot"; "heatmap"; "radar-chart"
              "rose-chart"; "waterfall-chart"; "funnel-chart"; "gauge-chart"; "sankey-diagram"
              "chord-diagram"; "treemap"; "sunburst" ]

    /// `chartFamilies` (Control.fs:579) — rich kinds whose geometry is additionally clipped to the box.
    let private chartFamilies =
        Set.ofList
            [ "line-chart"; "bar-chart"; "pie-chart"; "scatter-plot"; "graph-view"
              "area-chart"; "column-chart"; "histogram"; "box-plot"; "heatmap"; "radar-chart"
              "rose-chart"; "waterfall-chart"; "funnel-chart"; "gauge-chart"; "sankey-diagram"
              "chord-diagram"; "treemap"; "sunburst" ]

    /// Rich-family membership — `nodeWidth`/`nodeHeight`/`paintLeaf` (Control.fs:606/613/2050/2351).
    let isRich (kind: string) = Set.contains kind richFamilies

    /// Chart-family membership — chart-clip in `paintLeaf` (Control.fs:2356).
    let isChart (kind: string) = Set.contains kind chartFamilies

    /// Chart data-source routing — `chartValues` raw extraction (Control.fs:502). `None` ⇒ `[]`.
    let chartSource (kind: string) : ChartDataSource option =
        match kind with
        | "line-chart"
        | "bar-chart"
        | "scatter-plot"
        | "area-chart"
        | "column-chart"
        | "box-plot" -> Some Series
        | "pie-chart"
        | "histogram"
        | "heatmap"
        | "radar-chart"
        | "rose-chart"
        | "waterfall-chart"
        | "funnel-chart"
        | "gauge-chart"
        | "treemap"
        | "sunburst" -> Some Values
        | "graph-view"
        | "sankey-diagram"
        | "chord-diagram" -> Some GraphNodes
        | _ -> None

    /// Whether a kind lays out horizontally — `directionOf` Row set (Control.fs:2157). Includes the
    /// non-catalog internal `data-grid-row`/`data-grid-header` kinds, exactly as the prior match did.
    let layoutRow (kind: string) =
        match kind with
        | "data-grid"
        | "data-grid-row"
        | "data-grid-header"
        | "toolbar"
        | "split-view"
        | "wrap"
        | "grid"
        | "dock" -> true
        | _ -> false

    /// Whether a kind carries the scroll affordance — `applyScrollOffsets` (ControlRuntime.fs:373).
    let hasScrollAffordance (kind: string) = kind = "scroll-viewer"

    /// Virtualization role — `countVirtual` (RetainedRender.fs:1732). `data-grid-row` is a non-catalog
    /// internal kind, preserved here exactly as the prior match.
    let virtualizationOf (kind: string) : VirtualizationRole option =
        match kind with
        | "data-grid-row" -> Some GridRow
        | "data-grid" -> Some Grid
        | _ -> None

    /// Inspection node kind for a non-root node — `kindOf` (Inspection.fs:48). The root case
    /// (`path = "0"`) stays at the call site; the `Custom value` default is preserved.
    let inspectionNodeKind (kind: string) : VisualInspectionNodeKind =
        match kind with
        | "text-block"
        | "label"
        | "button"
        | "validation-message"
        | "tooltip"
        | "toast" -> VisualInspectionNodeKind.Text
        | "image" -> VisualInspectionNodeKind.Image
        | "overlay" -> VisualInspectionNodeKind.Overlay
        | "popup" -> VisualInspectionNodeKind.Popup
        | "stack"
        | "panel"
        | "scroll-viewer"
        | "data-grid" -> VisualInspectionNodeKind.Container
        | value -> VisualInspectionNodeKind.Custom value

    /// Inspection surface role for a non-root node — `surfaceRoleOf` (Inspection.fs:68). The root case
    /// stays at the call site; the `Content` default is preserved.
    let surfaceRole (kind: string) : VisualInspectionSurfaceRole =
        match kind with
        | "overlay" -> VisualInspectionSurfaceRole.Overlay
        | "popup"
        | "tooltip" -> VisualInspectionSurfaceRole.Popup
        | "toast"
        | "validation-message" -> VisualInspectionSurfaceRole.Feedback
        | "menu"
        | "tabs" -> VisualInspectionSurfaceRole.Navigation
        | _ -> VisualInspectionSurfaceRole.Content

    /// Accessibility role for a kind — `roleFor` (Accessibility.fs:28). Includes non-catalog `table`/
    /// `dialog` arms; the `Custom` default is preserved.
    let a11yRole (kind: string) : AccessibilityRole =
        match kind with
        | "text-block"
        | "label"
        | "badge"
        | "validation-message" -> StaticText
        | "button"
        | "icon-button" -> Button
        | "text-box"
        | "text-area"
        | "numeric-input" -> TextBox
        | "check-box"
        | "switch" -> CheckBox
        | "radio-group" -> RadioGroup
        | "slider" -> Slider
        | "list-view"
        | "list-box"
        | "multi-select-list"
        | "combo-box"
        | "tree-view" -> List
        | "data-grid"
        | "table" -> AccessibilityRole.Grid
        | "menu"
        | "context-menu"
        | "toolbar" -> Menu
        | "tabs" -> Tab
        | "dialog"
        | "overlay" -> Dialog
        | "progress-bar"
        | "spinner" -> Progress
        | "image"
        | "icon" -> AccessibilityRole.Image
        | "line-chart"
        | "bar-chart"
        | "pie-chart"
        | "scatter-plot" -> AccessibilityRole.Chart
        | "graph-view" -> Graph
        | _ -> AccessibilityRole.Custom

    // --- the registry table: the catalog↔registry completeness oracle (SC-001) ---

    /// One internal record per control kind — the *data* dispatch (the painter and required-attribute
    /// validation are FR-010 retentions, not here). Built from the functions above so the table can
    /// never drift from the live dispatch.
    type ControlKindEntry =
        { IsRich: bool
          IsChart: bool
          ChartSource: ChartDataSource option
          LayoutRow: bool
          HasScrollAffordance: bool
          Virtualization: VirtualizationRole option
          InspectionNodeKind: VisualInspectionNodeKind
          SurfaceRole: VisualInspectionSurfaceRole
          A11yRole: AccessibilityRole }

    let private entryFor (kind: string) =
        { IsRich = isRich kind
          IsChart = isChart kind
          ChartSource = chartSource kind
          LayoutRow = layoutRow kind
          HasScrollAffordance = hasScrollAffordance kind
          Virtualization = virtualizationOf kind
          InspectionNodeKind = inspectionNodeKind kind
          SurfaceRole = surfaceRole kind
          A11yRole = a11yRole kind }

    /// Every control kind the standard catalog publishes (`Catalog.supportedControls` ids). Hardcoded
    /// here because `Catalog.fs` compiles *after* this module; the catalog↔registry completeness test
    /// (SC-001) asserts this set equals the live catalog both directions, so an omission fails the build.
    let private catalogKinds =
        [ "affix"; "alert"; "anchor"; "area-chart"; "auto-complete"; "avatar"; "badge"; "bar-chart"
          "border"; "box-plot"; "breadcrumb"; "button"; "calendar"; "canvas"; "card"; "carousel"; "cascader"
          "check-box"; "chord-diagram"; "collapse"; "color-picker"; "column-chart"; "combo-box"
          "context-menu"; "custom-control"; "data-grid"; "date-picker"; "descriptions"; "dialog"
          "dock"; "drawer"; "empty"; "float-button"; "funnel-chart"; "gauge-chart"; "graph-view"
          "grid"; "heatmap"; "histogram"; "icon"; "icon-button"; "image"; "label"; "line-chart"
          "list-box"; "list-view"; "menu"; "multi-select-list"; "numeric-input"; "overlay"
          "pagination"; "panel"; "pie-chart"; "popconfirm"; "popover"; "progress-bar"; "qr-code"
          "radar-chart"; "radio-group"; "rate"; "result"; "rich-text"; "rose-chart"; "sankey-diagram"
          "scatter-plot"; "scroll-viewer"; "segmented"; "separator"; "skeleton"; "slider"; "spinner"
          "split-button"; "split-view"; "stack"; "statistic"; "steps"; "sunburst"; "switch"; "tabs"
          "tag"; "text-area"; "text-block"; "text-box"; "time-picker"; "timeline"; "toast"
          "toggle-button"; "toolbar"; "tooltip"; "tour"; "tree-view"; "treemap"; "upload"
          "validation-message"; "waterfall-chart"; "watermark"; "wrap" ]

    /// The per-kind dispatch table — one entry for every catalog kind. Built once at module load and
    /// read by `Map.tryFind` (no per-frame rebuild on the hot paths — contract §4).
    let registry : Map<string, ControlKindEntry> =
        catalogKinds |> List.map (fun k -> k, entryFor k) |> Map.ofList

    let tryEntry (kind: string) : ControlKindEntry option = Map.tryFind kind registry
