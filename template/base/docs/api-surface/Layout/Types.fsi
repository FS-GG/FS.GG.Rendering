namespace FS.Skia.UI.Layout

open FS.Skia.UI.Scene

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutBounds =
    { X: float
      Y: float
      Width: float
      Height: float }

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutNodeId = string

/// Public contract type exposed by this FS.Skia.UI package.
type HorizontalAlignment =
    | Left
    | Center
    | Right
    | Stretch

/// Public contract type exposed by this FS.Skia.UI package.
type VerticalAlignment =
    | Top
    | Middle
    | Bottom
    | Stretch

/// Public contract type exposed by this FS.Skia.UI package.
type DockPosition =
    | Top
    | Bottom
    | Left
    | Right
    | Fill

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutPadding =
    { Left: float
      Top: float
      Right: float
      Bottom: float }

/// Public contract type exposed by this FS.Skia.UI package.
type MeasureMode =
    | Undefined
    | Exactly
    | AtMost

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutDirection =
    | Row
    | Column

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutWrap =
    | NoWrap
    | Wrap

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutAlign =
    | Auto
    | Start
    | Center
    | End
    | Stretch
    | SpaceBetween
    | SpaceAround
    | SpaceEvenly

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutVisibility =
    | Visible
    | Hidden
    | Collapsed

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutSize =
    { Width: float option
      Height: float option }

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutGap =
    { Row: float
      Column: float }

/// Public contract type exposed by this FS.Skia.UI package.
type DiagnosticSeverity =
    | Info
    | Warning
    | Error

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutDiagnosticCode =
    | InvalidAvailableSpace
    | InvalidLayoutValue
    | DuplicateLayoutNodeId
    | UnsatisfiedConstraint
    | UnmeasurableContent
    | FallbackBoundsApplied
    | UnsupportedLayoutIntent

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutDiagnostic =
    { NodeId: LayoutNodeId option
      Code: LayoutDiagnosticCode
      Severity: DiagnosticSeverity
      Message: string
      Constraint: string option
      FallbackApplied: bool }

/// Public contract type exposed by this FS.Skia.UI package.
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

/// Public contract type exposed by this FS.Skia.UI package.
type MeasureRequest =
    { AvailableWidth: float
      WidthMode: MeasureMode
      AvailableHeight: float
      HeightMode: MeasureMode }

/// Public contract type exposed by this FS.Skia.UI package.
type MeasureResponse =
    { Width: float
      Height: float
      Diagnostics: LayoutDiagnostic list }

/// Public contract type exposed by this FS.Skia.UI package.
type ContentMeasure = MeasureRequest -> MeasureResponse

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutNode =
    { Id: LayoutNodeId
      Intent: LayoutIntent
      Visibility: LayoutVisibility
      Measure: ContentMeasure option
      Content: Scene option
      Children: LayoutNode list }

/// Public contract type exposed by this FS.Skia.UI package.
type AvailableSpace =
    { Width: float
      WidthMode: MeasureMode
      Height: float
      HeightMode: MeasureMode }

/// Public contract type exposed by this FS.Skia.UI package.
type ComputedBounds =
    { NodeId: LayoutNodeId
      Bounds: LayoutBounds
      Visibility: LayoutVisibility }

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutResult =
    { Bounds: ComputedBounds list
      Diagnostics: LayoutDiagnostic list
      Invalidated: LayoutNodeId list
      Revision: int64 }

/// Public contract type exposed by this FS.Skia.UI package.
type SnapMode =
    | Floor
    | Round
    | Expand

/// Public contract type exposed by this FS.Skia.UI package.
type PixelSnapPolicy =
    { ScaleFactor: float
      Mode: SnapMode }

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutWorkflowModel =
    { Root: LayoutNode
      Available: AvailableSpace
      Result: LayoutResult option
      LastChangedNodeIds: LayoutNodeId list
      PixelSnapPolicy: PixelSnapPolicy }

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutWorkflowMsg =
    | LayoutHostResized of AvailableSpace
    | LayoutVisibilityChanged of LayoutNodeId * LayoutVisibility
    | LayoutIntentChanged of LayoutNodeId * LayoutIntent
    | LayoutMeasurementChanged of LayoutNodeId
    | LayoutEvaluationCompleted of LayoutResult

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutWorkflowEffect =
    | EvaluateLayout
    | EvaluateIncrementalLayout of LayoutNodeId list

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutSizing =
    { DesiredWidth: float option
      DesiredHeight: float option
      HorizontalAlignment: HorizontalAlignment
      VerticalAlignment: VerticalAlignment }

/// Public contract type exposed by this FS.Skia.UI package.
type LayoutChild =
    { Content: Scene
      Sizing: LayoutSizing
      Dock: DockPosition option }

/// Public contract type exposed by this FS.Skia.UI package.
type StackConfig =
    { Bounds: LayoutBounds
      Padding: LayoutPadding
      Spacing: float }

/// Public contract type exposed by this FS.Skia.UI package.
type DockConfig =
    { Bounds: LayoutBounds
      Padding: LayoutPadding
      Spacing: float }

/// Public contract type exposed by this FS.Skia.UI package.
type GraphKind =
    | Directed
    | Undirected

/// Public contract type exposed by this FS.Skia.UI package.
type GraphNode =
    { Id: string
      Label: string
      Style: Color option }

/// Public contract type exposed by this FS.Skia.UI package.
type GraphEdge =
    { Source: string
      Target: string
      Weight: float option
      Label: string option }

/// Public contract type exposed by this FS.Skia.UI package.
type GraphConfig =
    { Kind: GraphKind
      Bounds: LayoutBounds }

/// Public contract type exposed by this FS.Skia.UI package.
type GraphDefinition =
    { Config: GraphConfig
      Nodes: GraphNode list
      Edges: GraphEdge list }

/// Public contract type exposed by this FS.Skia.UI package.
type GraphNodeLayout =
    { Node: GraphNode
      Bounds: LayoutBounds }

/// Public contract type exposed by this FS.Skia.UI package.
type GraphLayoutResult =
    { Nodes: GraphNodeLayout list
      Edges: GraphEdge list }

/// Public contract module exposed by this FS.Skia.UI package.
module Defaults =
    /// Public contract function exposed by this FS.Skia.UI package.
    val padding : LayoutPadding
    /// Public contract function exposed by this FS.Skia.UI package.
    val layoutGap : LayoutGap
    /// Public contract function exposed by this FS.Skia.UI package.
    val layoutSize : LayoutSize
    /// Public contract function exposed by this FS.Skia.UI package.
    val layoutIntent : LayoutIntent
    /// Public contract function exposed by this FS.Skia.UI package.
    val layoutNode : id: LayoutNodeId -> LayoutNode
    /// Public contract function exposed by this FS.Skia.UI package.
    val availableSpace : width: float -> height: float -> AvailableSpace
    /// Public contract function exposed by this FS.Skia.UI package.
    val pixelSnapPolicy : scaleFactor: float -> PixelSnapPolicy
    /// Public contract function exposed by this FS.Skia.UI package.
    val sizing : LayoutSizing
    /// Public contract function exposed by this FS.Skia.UI package.
    val bounds : width: float -> height: float -> LayoutBounds
    /// Public contract function exposed by this FS.Skia.UI package.
    val stackConfig : width: float -> height: float -> StackConfig
    /// Public contract function exposed by this FS.Skia.UI package.
    val dockConfig : width: float -> height: float -> DockConfig
    /// Public contract function exposed by this FS.Skia.UI package.
    val graphConfig : kind: GraphKind -> width: float -> height: float -> GraphConfig
    /// Public contract function exposed by this FS.Skia.UI package.
    val child : content: Scene -> LayoutChild
