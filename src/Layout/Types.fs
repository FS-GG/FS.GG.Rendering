namespace FS.Skia.UI.Layout

open FS.Skia.UI.Scene
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
    let padding =
        { Left = 0.0
          Top = 0.0
          Right = 0.0
          Bottom = 0.0 }

    let layoutGap = { Row = 0.0; Column = 0.0 }

    let layoutSize = { Width = None; Height = None }

    let layoutIntent =
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

    let layoutNode id =
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
