namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.Layout
// Feature 125: design-system primitives relocated to FS.GG.UI.DesignSystem.
open FS.GG.UI.DesignSystem

type ControlId = string
type ControlKind = string

// Chart data records (feature 080): defined here in Types.fs — which compiles before
// Control.fs — so the renderer/extraction in Control.fs can read X/Y/Label. The public
// `FS.GG.UI.Controls.ChartPoint`/`ChartSeries` names are unchanged (surface-neutral move
// out of Charts.fs); the chart authoring modules stay in Charts.fs.
type ChartPoint =
    { X: float
      Y: float
      Label: string option }

type ChartSeries =
    { Name: string
      Points: ChartPoint list }

[<RequireQualifiedAccess>]
type KnownControl =
    | TextBlock
    | Button
    | TextBox
    | LineChart
    | BarChart
    | PieChart
    | ScatterPlot
    | GraphView
    | DataGrid

[<RequireQualifiedAccess>]
type KnownEvent =
    | Click
    | Changed
    | Selected
    | FocusChanged
    | SortChanged

[<RequireQualifiedAccess>]
type KnownAttribute =
    | Text
    | Value
    | Children
    | Series
    | Values
    | Columns
    | Rows
    | Items
    | Nodes
    | VisibleRange
    | SelectedRows
    | FocusedCell

[<RequireQualifiedAccess>]
type StandardControlKind =
    | TextBlock
    | Button
    | TextBox
    | LineChart
    | BarChart
    | PieChart
    | ScatterPlot
    | GraphView
    | DataGrid
    | Custom of string

[<RequireQualifiedAccess>]
type StandardEventKind =
    | Click
    | Changed
    | Selected
    | FocusChanged
    | SortChanged
    | Custom of string

[<RequireQualifiedAccess>]
type StandardAttributeName =
    | Text
    | Value
    | Children
    | Series
    | Values
    | Columns
    | Rows
    | Items
    | Nodes
    | VisibleRange
    | SelectedRows
    | FocusedCell
    | Custom of string

type StandardAttributeValue<'msg> =
    | StandardText of string
    | StandardBool of bool
    | StandardFloat of float
    | StandardStringList of string list
    | StandardMessage of 'msg
    | StandardEvent of (string -> 'msg)
    | StandardUntyped of obj

type ControlSchema =
    { Kind: StandardControlKind
      RequiredAttributes: StandardAttributeName list
      SupportedAttributes: StandardAttributeName list
      SupportedEvents: StandardEventKind list
      CustomAllowed: bool }

type ControlDiagnosticSeverity =
    | Info
    | Warning
    | Error

type ControlDiagnosticCode =
    | MissingRequiredAttribute
    | DuplicateAttribute
    | UnsupportedStateCombination
    | MissingStableKey
    | HitTestFailed
    | LayoutConflict
    | MissingAccessibilityMetadata
    | ContrastFailure
    | UnsupportedEnvironment
    | KeyCollision
    | StaleGeneratedReference
    | MissingOverlayAnchor
    | StaleOverlayFocusTarget
    | BlockedOverlayDismissal
    | DisabledOverlayTrigger
    | NoFitOverlayPlacement
    | DuplicateOverlayDispatch
    | InvalidOverlayMessage
    | LowerLayerBlocked
    | ScrollIntrinsicUnavailable
    | ScrollExtentFallback
    | UnstableReuseInput
    | OffscreenComposition

type AccessibilityRole =
    | StaticText
    | Button
    | TextBox
    | CheckBox
    | RadioGroup
    | Slider
    | List
    | Grid
    | Menu
    | Tab
    | Dialog
    | Progress
    | Image
    | Chart
    | Graph
    | Custom

type KeyboardOperation =
    { Focusable: bool
      ActivationKeys: string list
      NavigationKeys: string list }

type ContrastEvidence =
    { Foreground: Color
      Background: Color
      Ratio: float
      RequiredRatio: float }

type NavRange =
    { Step: float
      Min: float
      Max: float }

type CollectionPosition =
    { TotalItems: int
      FocusedIndex: int option }

type AccessibilityMetadata =
    { Role: AccessibilityRole
      NameSource: string
      State: string list
      FocusOrder: int option
      Keyboard: KeyboardOperation
      Contrast: ContrastEvidence option
      Navigation: NavRange option
      Collection: CollectionPosition option }

[<RequireQualifiedAccess>]
type ControlEventOrigin =
    | Pointer
    | Keyboard
    | Text
    | Focus
    | Selection
    | Clipboard

type NavPayload =
    | SteppedValue of value: float
    | MovedSelection of index: int * item: string option
    | MovedCell of row: int * col: int

type ControlEvent =
    { Kind: string
      ControlId: ControlId option
      Origin: ControlEventOrigin
      Payload: string option
      Nav: NavPayload option }

type AttrCategory =
    | Content
    | Children
    | Layout
    | Style
    | Theme
    | State
    | Validation
    | Accessibility
    | Event
    | Data
    | Slot

type Control<'msg> =
    { Kind: ControlKind
      Key: ControlId option
      Attributes: Attr<'msg> list
      Children: Control<'msg> list
      Content: string option
      Accessibility: AccessibilityMetadata option }

and Attr<'msg> =
    { Name: string
      Category: AttrCategory
      Value: AttrValue<'msg> }

and AttrValue<'msg> =
    | TextValue of string
    | BoolValue of bool
    | FloatValue of float
    | StringListValue of string list
    | ValidationValue of ValidationState
    | StyleClassesValue of StyleClass list
    | VisualStateValue of VisualState
    | SlotFillsValue of (string * Control<'msg>) list
    | AccessibilityValue of AccessibilityMetadata
    | ThemeValue of Theme
    | ChildValue of Control<'msg>
    | ChildrenValue of Control<'msg> list
    | MessageValue of 'msg
    | EventValue of (ControlEvent -> 'msg)
    | UntypedValue of obj

type ControlDiagnostic =
    { ControlId: ControlId option
      ControlKind: ControlKind
      Code: ControlDiagnosticCode
      Severity: ControlDiagnosticSeverity
      Message: string
      EvidencePath: string option }

type ControlEventBinding<'msg> =
    { ControlId: ControlId
      EventKind: string
      Dispatch: ControlEvent -> 'msg }

type ControlRenderResult<'msg> =
    { Scene: Scene
      Layout: LayoutNode
      Bounds: (ControlId * Rect) list
      Diagnostics: ControlDiagnostic list
      EventBindings: ControlEventBinding<'msg> list
      BoundIds: Set<ControlId>
      NodeCount: int }
