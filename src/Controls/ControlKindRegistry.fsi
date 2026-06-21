namespace FS.GG.UI.Controls

open FS.GG.UI.Scene

/// Feature 183 (US1 / FR-001): the single internal per-`Kind` dispatch registry. `module internal`
/// (mirrors `module internal Reconcile`/`RetainedRender`) — reached by the test assembly via
/// `InternalsVisibleTo`, never part of the public package surface (`FS.GG.UI.Controls.txt` unchanged).
/// A paired `.fsi` is required by the maintained-package surface governance even though nothing here is
/// public.
module internal ControlKindRegistry =

    /// Which attribute a chart kind pulls its data points from (`chartValues`).
    type ChartDataSource =
        | Series
        | Values
        | GraphNodes

    /// The virtualization role of a kind for `RetainedRender.countVirtual`.
    type VirtualizationRole =
        | Grid
        | GridRow

    /// Rich-family membership (faithful-geometry kinds).
    val isRich: kind: string -> bool

    /// Chart-family membership (kinds whose geometry is clipped to the box).
    val isChart: kind: string -> bool

    /// Chart data-source routing; `None` ⇒ the kind contributes no chart points.
    val chartSource: kind: string -> ChartDataSource option

    /// Whether a kind lays out horizontally (`directionOf` Row set).
    val layoutRow: kind: string -> bool

    /// Whether a kind carries the scroll affordance (`applyScrollOffsets`).
    val hasScrollAffordance: kind: string -> bool

    /// Virtualization role for `countVirtual` (`data-grid` / `data-grid-row`).
    val virtualizationOf: kind: string -> VirtualizationRole option

    /// Inspection node kind for a non-root node (`Custom kind` default).
    val inspectionNodeKind: kind: string -> VisualInspectionNodeKind

    /// Inspection surface role for a non-root node (`Content` default).
    val surfaceRole: kind: string -> VisualInspectionSurfaceRole

    /// Accessibility role for a kind (`Custom` default).
    val a11yRole: kind: string -> AccessibilityRole

    /// One internal record per control kind — the data dispatch (the painter and required-attribute
    /// validation are FR-010 retentions, kept at their original sites).
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

    /// The per-kind dispatch table — one entry for every catalog kind (the catalog↔registry
    /// completeness oracle, SC-001). Built once at module load.
    val registry: Map<string, ControlKindEntry>

    /// Look up a catalog kind's entry (`None` for a non-catalog runtime kind).
    val tryEntry: kind: string -> ControlKindEntry option
