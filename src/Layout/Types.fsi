namespace FS.GG.UI.Layout

open FS.GG.UI.Scene

/// Public contract type exposed by this FS.GG.UI package.
type LayoutBounds =
    { X: float
      Y: float
      Width: float
      Height: float }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutNodeId = string

/// Public contract type exposed by this FS.GG.UI package.
type HorizontalAlignment =
    | Left
    | Center
    | Right
    | Stretch

/// Public contract type exposed by this FS.GG.UI package.
type VerticalAlignment =
    | Top
    | Middle
    | Bottom
    | Stretch

/// Public contract type exposed by this FS.GG.UI package.
type DockPosition =
    | Top
    | Bottom
    | Left
    | Right
    | Fill

/// Public contract type exposed by this FS.GG.UI package.
type LayoutPadding =
    { Left: float
      Top: float
      Right: float
      Bottom: float }

/// Public contract type exposed by this FS.GG.UI package.
type MeasureMode =
    | Undefined
    | Exactly
    | AtMost

/// Public contract type exposed by this FS.GG.UI package.
type LayoutDirection =
    | Row
    | Column

/// Public contract type exposed by this FS.GG.UI package.
type LayoutWrap =
    | NoWrap
    | Wrap

/// Public contract type exposed by this FS.GG.UI package.
type LayoutAlign =
    | Auto
    | Start
    | Center
    | End
    | Stretch
    | SpaceBetween
    | SpaceAround
    | SpaceEvenly

/// Public contract type exposed by this FS.GG.UI package.
type LayoutVisibility =
    | Visible
    | Hidden
    | Collapsed

/// Public contract type exposed by this FS.GG.UI package.
type LayoutSize =
    { Width: float option
      Height: float option }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutGap =
    { Row: float
      Column: float }

/// Public contract type exposed by this FS.GG.UI package.
type DiagnosticSeverity =
    | Info
    | Warning
    | Error

/// Public contract type exposed by this FS.GG.UI package.
type LayoutDiagnosticCode =
    | InvalidAvailableSpace
    | InvalidLayoutValue
    | DuplicateLayoutNodeId
    | UnsatisfiedConstraint
    | UnmeasurableContent
    | FallbackBoundsApplied
    | UnsupportedLayoutIntent
    | UnsupportedIntrinsicQuery
    | RejectedIntrinsicResult
    | StaleLayoutCacheEntry
    | DuplicateMeasurement
    | InsufficientDependencyEvidence
    | ContradictoryIntrinsicExtent

/// Public contract type exposed by this FS.GG.UI package.
type LayoutDiagnostic =
    { NodeId: LayoutNodeId option
      Code: LayoutDiagnosticCode
      Severity: DiagnosticSeverity
      Message: string
      Constraint: string option
      FallbackApplied: bool }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutIntent =
    { Direction: LayoutDirection
      Wrap: LayoutWrap
      AlignItems: LayoutAlign
      AlignSelf: LayoutAlign option
      JustifyContent: LayoutAlign
      Padding: LayoutPadding
      Margin: LayoutPadding
      Gap: LayoutGap
      Size: LayoutSize
      MinSize: LayoutSize
      MaxSize: LayoutSize
      FlexGrow: float
      FlexShrink: float
      FlexBasis: float option }

/// Public contract type exposed by this FS.GG.UI package.
type MeasureRequest =
    { AvailableWidth: float
      WidthMode: MeasureMode
      AvailableHeight: float
      HeightMode: MeasureMode }

/// Public contract type exposed by this FS.GG.UI package.
type MeasureResponse =
    { Width: float
      Height: float
      Diagnostics: LayoutDiagnostic list }

/// Public contract type exposed by this FS.GG.UI package.
type ContentMeasure = MeasureRequest -> MeasureResponse

/// Public contract type exposed by this FS.GG.UI package.
type LayoutNode =
    { Id: LayoutNodeId
      Intent: LayoutIntent
      Visibility: LayoutVisibility
      Measure: ContentMeasure option
      Content: Scene option
      Children: LayoutNode list }

/// Public contract type exposed by this FS.GG.UI package.
type AvailableSpace =
    { Width: float
      WidthMode: MeasureMode
      Height: float
      HeightMode: MeasureMode }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutConstraintBound =
    | Bounded of float
    | Unbounded

/// Public contract type exposed by this FS.GG.UI package.
type LayoutConstraintSource =
    | Viewport
    | Parent
    | IntrinsicProbe
    | Fallback
    | Compatibility

/// Public contract type exposed by this FS.GG.UI package.
type LayoutConstraints =
    { MinWidth: float
      MaxWidth: LayoutConstraintBound
      MinHeight: float
      MaxHeight: LayoutConstraintBound
      WidthMode: MeasureMode
      HeightMode: MeasureMode
      Source: LayoutConstraintSource
      NormalizedIdentity: string }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutMeasuredSize =
    { MeasuredWidth: float
      MeasuredHeight: float }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutMeasurementRequest =
    { ParticipantId: LayoutNodeId
      Constraints: LayoutConstraints
      ParentPath: string
      PassId: string
      LayoutInputKey: string }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutChildPlacement =
    { ChildId: LayoutNodeId
      Bounds: LayoutBounds
      Visibility: LayoutVisibility
      PlacementIdentity: string }

/// Public contract type exposed by this FS.GG.UI package.
type IntrinsicAxis =
    | IntrinsicMinWidth
    | IntrinsicMaxWidth
    | IntrinsicMinHeight
    | IntrinsicMaxHeight

/// Public contract type exposed by this FS.GG.UI package.
type IntrinsicQuerySource =
    | ScrollViewer
    | CustomContainer
    | CompatibilityCheck
    | DiagnosticProbe

/// Public contract type exposed by this FS.GG.UI package.
type IntrinsicQuery =
    { ParticipantId: LayoutNodeId
      Axis: IntrinsicAxis
      CrossAxisConstraint: float option
      LayoutInputKey: string
      QuerySource: IntrinsicQuerySource
      QueryIdentity: string
      Revision: int }

/// Public contract type exposed by this FS.GG.UI package.
type IntrinsicDependency =
    { QueryIdentity: string
      ResultIdentity: string }

/// Public contract type exposed by this FS.GG.UI package.
type IntrinsicSizeResult =
    { QueryIdentity: string
      Size: float
      Dependencies: IntrinsicDependency list
      Accepted: bool
      Diagnostics: LayoutDiagnostic list }

/// Public contract type exposed by this FS.GG.UI package.
type MeasuredLayoutResult =
    { ParticipantId: LayoutNodeId
      Constraints: LayoutConstraints
      MeasuredSize: LayoutMeasuredSize
      ChildPlacements: LayoutChildPlacement list
      IntrinsicDependencies: IntrinsicDependency list
      CacheEntryId: string
      Diagnostics: LayoutDiagnostic list }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutCacheEntryKind =
    | MeasuredLayoutEntry
    | IntrinsicLayoutEntry

/// Public contract type exposed by this FS.GG.UI package.
type LayoutCacheEntry =
    { EntryId: string
      EntryKind: LayoutCacheEntryKind
      ParticipantId: LayoutNodeId
      ConstraintIdentity: string
      LayoutInputKey: string
      ChildDependencyKeys: string list
      ResultIdentity: string
      Revision: int }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutContentExtentSource =
    | EmptyContent
    | IntrinsicResult
    | MeasuredFallback
    | DiagnosticFallback

/// Public contract type exposed by this FS.GG.UI package.
type LayoutContentExtent =
    { ContentWidth: float
      ContentHeight: float
      MaxHorizontalOffset: float
      MaxVerticalOffset: float
      ExtentSource: LayoutContentExtentSource
      DependencyKeys: string list
      Diagnostics: LayoutDiagnostic list }

/// Public contract type exposed by this FS.GG.UI package.
type ComputedBounds =
    { NodeId: LayoutNodeId
      Bounds: LayoutBounds
      Visibility: LayoutVisibility }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutResult =
    { Bounds: ComputedBounds list
      Diagnostics: LayoutDiagnostic list
      Invalidated: LayoutNodeId list
      Revision: int64 }

/// Public contract type exposed by this FS.GG.UI package.
type SnapMode =
    | Floor
    | Round
    | Expand

/// Public contract type exposed by this FS.GG.UI package.
type PixelSnapPolicy =
    { ScaleFactor: float
      Mode: SnapMode }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutWorkflowModel =
    { Root: LayoutNode
      Available: AvailableSpace
      Result: LayoutResult option
      LastChangedNodeIds: LayoutNodeId list
      PixelSnapPolicy: PixelSnapPolicy }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutWorkflowMsg =
    | LayoutHostResized of AvailableSpace
    | LayoutVisibilityChanged of LayoutNodeId * LayoutVisibility
    | LayoutIntentChanged of LayoutNodeId * LayoutIntent
    | LayoutMeasurementChanged of LayoutNodeId
    | LayoutEvaluationCompleted of LayoutResult

/// Public contract type exposed by this FS.GG.UI package.
type LayoutWorkflowEffect =
    | EvaluateLayout
    | EvaluateIncrementalLayout of LayoutNodeId list

/// Public contract type exposed by this FS.GG.UI package.
type LayoutSizing =
    { DesiredWidth: float option
      DesiredHeight: float option
      HorizontalAlignment: HorizontalAlignment
      VerticalAlignment: VerticalAlignment }

/// Public contract type exposed by this FS.GG.UI package.
type LayoutChild =
    { Content: Scene
      Sizing: LayoutSizing
      Dock: DockPosition option }

/// Public contract type exposed by this FS.GG.UI package.
type StackConfig =
    { Bounds: LayoutBounds
      Padding: LayoutPadding
      Spacing: float }

/// Public contract type exposed by this FS.GG.UI package.
type DockConfig =
    { Bounds: LayoutBounds
      Padding: LayoutPadding
      Spacing: float }

/// Public contract type exposed by this FS.GG.UI package.
type GraphKind =
    | Directed
    | Undirected

/// Public contract type exposed by this FS.GG.UI package.
type GraphNode =
    { Id: string
      Label: string
      Style: Color option }

/// Public contract type exposed by this FS.GG.UI package.
type GraphEdge =
    { Source: string
      Target: string
      Weight: float option
      Label: string option }

/// Public contract type exposed by this FS.GG.UI package.
type GraphConfig =
    { Kind: GraphKind
      Bounds: LayoutBounds }

/// Public contract type exposed by this FS.GG.UI package.
type GraphDefinition =
    { Config: GraphConfig
      Nodes: GraphNode list
      Edges: GraphEdge list }

/// Public contract type exposed by this FS.GG.UI package.
type GraphNodeLayout =
    { Node: GraphNode
      Bounds: LayoutBounds }

/// Public contract type exposed by this FS.GG.UI package.
type GraphLayoutResult =
    { Nodes: GraphNodeLayout list
      Edges: GraphEdge list }

/// Public contract module exposed by this FS.GG.UI package.
module Defaults =
    /// Public contract function exposed by this FS.GG.UI package.
    val padding : LayoutPadding
    /// Public contract function exposed by this FS.GG.UI package.
    val layoutGap : LayoutGap
    /// Public contract function exposed by this FS.GG.UI package.
    val layoutSize : LayoutSize
    /// Public contract function exposed by this FS.GG.UI package.
    val layoutIntent : LayoutIntent
    /// Public contract function exposed by this FS.GG.UI package.
    val layoutNode : id: LayoutNodeId -> LayoutNode
    /// Public contract function exposed by this FS.GG.UI package.
    val availableSpace : width: float -> height: float -> AvailableSpace
    /// Public contract function exposed by this FS.GG.UI package.
    val pixelSnapPolicy : scaleFactor: float -> PixelSnapPolicy
    /// Public contract function exposed by this FS.GG.UI package.
    val sizing : LayoutSizing
    /// Public contract function exposed by this FS.GG.UI package.
    val bounds : width: float -> height: float -> LayoutBounds
    /// Public contract function exposed by this FS.GG.UI package.
    val stackConfig : width: float -> height: float -> StackConfig
    /// Public contract function exposed by this FS.GG.UI package.
    val dockConfig : width: float -> height: float -> DockConfig
    /// Public contract function exposed by this FS.GG.UI package.
    val graphConfig : kind: GraphKind -> width: float -> height: float -> GraphConfig
    /// Public contract function exposed by this FS.GG.UI package.
    val child : content: Scene -> LayoutChild
