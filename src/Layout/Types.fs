namespace FS.GG.UI.Layout

open FS.GG.UI.Scene
open System

type LayoutBounds =
    { X: float
      Y: float
      Width: float
      Height: float }

type LayoutNodeId = string

type HorizontalAlignment =
    | Left
    | Center
    | Right
    | Stretch

type VerticalAlignment =
    | Top
    | Middle
    | Bottom
    | Stretch

type DockPosition =
    | Top
    | Bottom
    | Left
    | Right
    | Fill

type LayoutPadding =
    { Left: float
      Top: float
      Right: float
      Bottom: float }

type MeasureMode =
    | Undefined
    | Exactly
    | AtMost

type LayoutDirection =
    | Row
    | Column

type LayoutWrap =
    | NoWrap
    | Wrap

type LayoutAlign =
    | Auto
    | Start
    | Center
    | End
    | Stretch
    | SpaceBetween
    | SpaceAround
    | SpaceEvenly

type LayoutVisibility =
    | Visible
    | Hidden
    | Collapsed

type LayoutSize =
    { Width: float option
      Height: float option }

type LayoutGap =
    { Row: float
      Column: float }

type DiagnosticSeverity =
    | Info
    | Warning
    | Error

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

type LayoutDiagnostic =
    { NodeId: LayoutNodeId option
      Code: LayoutDiagnosticCode
      Severity: DiagnosticSeverity
      Message: string
      Constraint: string option
      FallbackApplied: bool }

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

type MeasureRequest =
    { AvailableWidth: float
      WidthMode: MeasureMode
      AvailableHeight: float
      HeightMode: MeasureMode }

type MeasureResponse =
    { Width: float
      Height: float
      Diagnostics: LayoutDiagnostic list }

type ContentMeasure = MeasureRequest -> MeasureResponse

type LayoutNode =
    { Id: LayoutNodeId
      Intent: LayoutIntent
      Visibility: LayoutVisibility
      Measure: ContentMeasure option
      Content: Scene option
      Children: LayoutNode list }

type AvailableSpace =
    { Width: float
      WidthMode: MeasureMode
      Height: float
      HeightMode: MeasureMode }

type LayoutConstraintBound =
    | Bounded of float
    | Unbounded

type LayoutConstraintSource =
    | Viewport
    | Parent
    | IntrinsicProbe
    | Fallback
    | Compatibility

type LayoutConstraints =
    { MinWidth: float
      MaxWidth: LayoutConstraintBound
      MinHeight: float
      MaxHeight: LayoutConstraintBound
      WidthMode: MeasureMode
      HeightMode: MeasureMode
      Source: LayoutConstraintSource
      NormalizedIdentity: string }

type LayoutMeasuredSize =
    { MeasuredWidth: float
      MeasuredHeight: float }

type LayoutMeasurementRequest =
    { ParticipantId: LayoutNodeId
      Constraints: LayoutConstraints
      ParentPath: string
      PassId: string
      LayoutInputKey: string }

type LayoutChildPlacement =
    { ChildId: LayoutNodeId
      Bounds: LayoutBounds
      Visibility: LayoutVisibility
      PlacementIdentity: string }

type IntrinsicAxis =
    | IntrinsicMinWidth
    | IntrinsicMaxWidth
    | IntrinsicMinHeight
    | IntrinsicMaxHeight

type IntrinsicQuerySource =
    | ScrollViewer
    | CustomContainer
    | CompatibilityCheck
    | DiagnosticProbe

type IntrinsicQuery =
    { ParticipantId: LayoutNodeId
      Axis: IntrinsicAxis
      CrossAxisConstraint: float option
      LayoutInputKey: string
      QuerySource: IntrinsicQuerySource
      QueryIdentity: string
      Revision: int }

type IntrinsicDependency =
    { QueryIdentity: string
      ResultIdentity: string }

type IntrinsicSizeResult =
    { QueryIdentity: string
      Size: float
      Dependencies: IntrinsicDependency list
      Accepted: bool
      Diagnostics: LayoutDiagnostic list }

type MeasuredLayoutResult =
    { ParticipantId: LayoutNodeId
      Constraints: LayoutConstraints
      MeasuredSize: LayoutMeasuredSize
      ChildPlacements: LayoutChildPlacement list
      IntrinsicDependencies: IntrinsicDependency list
      CacheEntryId: string
      Diagnostics: LayoutDiagnostic list }

type LayoutCacheEntryKind =
    | MeasuredLayoutEntry
    | IntrinsicLayoutEntry

type LayoutCacheEntry =
    { EntryId: string
      EntryKind: LayoutCacheEntryKind
      ParticipantId: LayoutNodeId
      ConstraintIdentity: string
      LayoutInputKey: string
      ChildDependencyKeys: string list
      ResultIdentity: string
      Revision: int }

type LayoutContentExtentSource =
    | EmptyContent
    | IntrinsicResult
    | MeasuredFallback
    | DiagnosticFallback

type LayoutContentExtent =
    { ContentWidth: float
      ContentHeight: float
      MaxHorizontalOffset: float
      MaxVerticalOffset: float
      ExtentSource: LayoutContentExtentSource
      DependencyKeys: string list
      Diagnostics: LayoutDiagnostic list }

type ComputedBounds =
    { NodeId: LayoutNodeId
      Bounds: LayoutBounds
      Visibility: LayoutVisibility }

type LayoutResult =
    { Bounds: ComputedBounds list
      Diagnostics: LayoutDiagnostic list
      Invalidated: LayoutNodeId list
      Revision: int64 }

type SnapMode =
    | Floor
    | Round
    | Expand

type PixelSnapPolicy =
    { ScaleFactor: float
      Mode: SnapMode }

type LayoutWorkflowModel =
    { Root: LayoutNode
      Available: AvailableSpace
      Result: LayoutResult option
      LastChangedNodeIds: LayoutNodeId list
      PixelSnapPolicy: PixelSnapPolicy }

type LayoutWorkflowMsg =
    | LayoutHostResized of AvailableSpace
    | LayoutVisibilityChanged of LayoutNodeId * LayoutVisibility
    | LayoutIntentChanged of LayoutNodeId * LayoutIntent
    | LayoutMeasurementChanged of LayoutNodeId
    | LayoutEvaluationCompleted of LayoutResult

type LayoutWorkflowEffect =
    | EvaluateLayout
    | EvaluateIncrementalLayout of LayoutNodeId list

type LayoutSizing =
    { DesiredWidth: float option
      DesiredHeight: float option
      HorizontalAlignment: HorizontalAlignment
      VerticalAlignment: VerticalAlignment }

type LayoutChild =
    { Content: Scene
      Sizing: LayoutSizing
      Dock: DockPosition option }

type StackConfig =
    { Bounds: LayoutBounds
      Padding: LayoutPadding
      Spacing: float }

type DockConfig =
    { Bounds: LayoutBounds
      Padding: LayoutPadding
      Spacing: float }

type GraphKind =
    | Directed
    | Undirected

type GraphNode =
    { Id: string
      Label: string
      Style: Color option }

type GraphEdge =
    { Source: string
      Target: string
      Weight: float option
      Label: string option }

type GraphConfig =
    { Kind: GraphKind
      Bounds: LayoutBounds }

type GraphDefinition =
    { Config: GraphConfig
      Nodes: GraphNode list
      Edges: GraphEdge list }

type GraphNodeLayout =
    { Node: GraphNode
      Bounds: LayoutBounds }

type GraphLayoutResult =
    { Nodes: GraphNodeLayout list
      Edges: GraphEdge list }

module Defaults =
    let padding : LayoutPadding =
        { Left = 0.0
          Top = 0.0
          Right = 0.0
          Bottom = 0.0 }

    let layoutGap : LayoutGap = { Row = 0.0; Column = 0.0 }

    let layoutSize : LayoutSize = { Width = None; Height = None }

    let layoutIntent : LayoutIntent =
        { Direction = LayoutDirection.Row
          Wrap = LayoutWrap.NoWrap
          AlignItems = LayoutAlign.Stretch
          AlignSelf = None
          JustifyContent = LayoutAlign.Start
          Padding = padding
          Margin = padding
          Gap = layoutGap
          Size = layoutSize
          MinSize = layoutSize
          MaxSize = layoutSize
          FlexGrow = 0.0
          FlexShrink = 1.0
          FlexBasis = None }

    let layoutNode id : LayoutNode =
        { Id = id
          Intent = layoutIntent
          Visibility = LayoutVisibility.Visible
          Measure = None
          Content = None
          Children = [] }

    let availableSpace width height =
        { Width = width
          WidthMode = MeasureMode.Exactly
          Height = height
          HeightMode = MeasureMode.Exactly }

    let pixelSnapPolicy scaleFactor =
        { ScaleFactor =
            if Double.IsFinite scaleFactor && scaleFactor > 0.0 then
                scaleFactor
            else
                1.0
          Mode = SnapMode.Round }

    let sizing =
        { DesiredWidth = None
          DesiredHeight = None
          HorizontalAlignment = HorizontalAlignment.Stretch
          VerticalAlignment = VerticalAlignment.Stretch }

    let bounds width height =
        { X = 0.0
          Y = 0.0
          Width = width
          Height = height }

    let stackConfig width height : StackConfig =
        { Bounds = bounds width height
          Padding = padding
          Spacing = 0.0 }

    let dockConfig width height : DockConfig =
        { Bounds = bounds width height
          Padding = padding
          Spacing = 0.0 }

    let graphConfig kind width height =
        { Kind = kind
          Bounds = bounds width height }

    let child content =
        { Content = content
          Sizing = sizing
          Dock = None }
