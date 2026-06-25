namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.Layout
// Feature 125: design-system primitives relocated to FS.GG.UI.DesignSystem.
open FS.GG.UI.DesignSystem

type ControlId = string
type ControlKind = string

/// Feature 175 (FR-001/FR-002): the scroll model owned per `scroll-viewer` ControlId. Pure value;
/// derived geometry (scrollable, thumb height/position) is computed by the `ScrollState` module.
/// Defined here (before Control.fs) so both the paint path and `ControlRuntimeModel` can use it.
type ScrollState =
    { Offset: float
      ContentHeight: float
      ViewportHeight: float }

/// Feature 175 (FR-001/FR-002): pure transitions and derived geometry over `ScrollState`. Drag,
/// wheel, and keyboard scroll all reduce to `applyScrollDelta`; the thumb derives from the ratio.
module ScrollState =
    // A one-pixel-overflow dead-zone: content within 1px of the viewport is treated as
    // non-scrollable (an exact fit must not present a flickering draggable thumb, FR-002).
    let deadZone = 1.0
    // Minimum thumb height so a tall page's thumb stays grabbable.
    let minThumb = 12.0

    let empty = { Offset = 0.0; ContentHeight = 0.0; ViewportHeight = 0.0 }

    /// max(0, ContentHeight - ViewportHeight) — the largest valid offset.
    let maxOffset (state: ScrollState) = max 0.0 (state.ContentHeight - state.ViewportHeight)

    /// The region can scroll only when content exceeds the viewport beyond the dead-zone.
    let scrollable (state: ScrollState) = state.ContentHeight > state.ViewportHeight + deadZone

    /// Record measured extents (host sets these per frame); re-clamp so a shrink cannot leave a
    /// stale over-scroll offset.
    let withExtent (contentHeight: float) (viewportHeight: float) (state: ScrollState) =
        let next = { state with ContentHeight = contentHeight; ViewportHeight = viewportHeight }
        { next with Offset = next.Offset |> max 0.0 |> min (maxOffset next) }

    /// Offset' = clamp(Offset + delta, 0, maxOffset). No overscroll at either bound (FR-001/FR-002).
    let applyScrollDelta (delta: float) (state: ScrollState) =
        { state with Offset = state.Offset + delta |> max 0.0 |> min (maxOffset state) }

    /// Thumb height from the viewport/content ratio; 0 (no draggable thumb) when not scrollable.
    let thumbHeight (state: ScrollState) =
        if not (scrollable state) then 0.0
        else max minThumb (state.ViewportHeight * state.ViewportHeight / state.ContentHeight)

    /// Thumb top within a track of `trackHeight`, monotonically tracking the offset; 0 when not
    /// scrollable.
    let thumbPosition (trackHeight: float) (state: ScrollState) =
        let m = maxOffset state
        if not (scrollable state) || m <= 0.0 then 0.0
        else state.Offset / m * max 0.0 (trackHeight - thumbHeight state)

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
    | EditedText of text: string

type ControlEvent =
    { Kind: string
      ControlId: ControlId option
      Origin: ControlEventOrigin
      Nav: NavPayload option }

/// Feature 184 (US3): typed projections of a `ControlEvent`'s `Nav` outcome — the single typed
/// replacement for the retired stringly `Payload : string option`. `navText` yields the string an
/// event carries (free-text edit or moved-selection item); `navValue` the stepped float (slider /
/// numeric / boolean-as-0/1); `navCell` the moved grid cell indices.
module ControlEvent =
    let navValue (ev: ControlEvent) : float option =
        match ev.Nav with
        | Some(SteppedValue v) -> Some v
        | _ -> None

    let navText (ev: ControlEvent) : string option =
        match ev.Nav with
        | Some(EditedText t) -> Some t
        | Some(MovedSelection(_, item)) -> item
        | _ -> None

    let navCell (ev: ControlEvent) : (int * int) option =
        match ev.Nav with
        | Some(MovedCell(row, col)) -> Some(row, col)
        | _ -> None

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
    | SceneValue of FS.GG.UI.Scene.Scene
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
