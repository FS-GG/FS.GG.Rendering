namespace FS.GG.UI.Controls

open FS.GG.UI.Scene
open FS.GG.UI.Layout
// Feature 125: the design-system primitives (ValidationState, VisualState, StyleVariant,
// StyleClass, ResolvedStyle, Theme) now live in FS.GG.UI.DesignSystem; the control-semantic
// types below reference them (e.g. AttrValue's ThemeValue/StyleClassesValue cases).
open FS.GG.UI.DesignSystem

/// Stable string identity of a control instance (`ControlId`), used as the join key
/// across `Bounds`, `EventBindings`, and `BoundIds` for hit-testing and event dispatch.
type ControlId = string
/// String tag naming a control's kind (`ControlKind`), e.g. the lowered form of a
/// `StandardControlKind` such as `"button"` or `"line-chart"`.
type ControlKind = string

/// Feature 175 (FR-001/FR-002): the scroll model owned per `scroll-viewer` ControlId. Pure value;
/// derived geometry (scrollable, thumb height/position) is computed by the `ScrollState` module.
type ScrollState =
    { Offset: float
      ContentHeight: float
      ViewportHeight: float }

/// Feature 175 (FR-001/FR-002): pure transitions and derived geometry over `ScrollState`. Drag,
/// wheel, and keyboard scroll all reduce to `applyScrollDelta`; the thumb derives from the
/// viewport/content ratio. All functions are total and deterministic.
module ScrollState =
    /// A one-pixel-overflow dead-zone: content within 1px of the viewport is treated as non-scrollable.
    val deadZone: float
    /// The minimum thumb height in px so a tall page's thumb stays grabbable.
    val minThumb: float
    /// The zero scroll model (offset 0, no extents).
    val empty: ScrollState
    /// `max(0, ContentHeight - ViewportHeight)` — the largest valid offset.
    val maxOffset: state: ScrollState -> float
    /// True when content exceeds the viewport beyond the dead-zone (a draggable thumb is shown).
    val scrollable: state: ScrollState -> bool
    /// Record measured extents and re-clamp the offset (a shrink cannot leave a stale over-scroll).
    val withExtent: contentHeight: float -> viewportHeight: float -> state: ScrollState -> ScrollState
    /// `Offset' = clamp(Offset + delta, 0, maxOffset)` — no overscroll at either bound.
    val applyScrollDelta: delta: float -> state: ScrollState -> ScrollState
    /// Thumb height from the viewport/content ratio; `0` (no draggable thumb) when not scrollable.
    val thumbHeight: state: ScrollState -> float
    /// Thumb top within a track of `trackHeight`, monotonically tracking the offset; `0` when not scrollable.
    val thumbPosition: trackHeight: float -> state: ScrollState -> float

/// A single plotted datum (`ChartPoint`): `X`/`Y` coordinates plus an optional `Label`
/// for line, bar, pie, and scatter chart kinds.
type ChartPoint =
    { X: float
      Y: float
      Label: string option }

/// A named collection of points (`ChartSeries`): a display `Name` and the ordered
/// `Points` it contributes to a chart control.
type ChartSeries =
    { Name: string
      Points: ChartPoint list }

[<RequireQualifiedAccess>]
/// Closed enumeration (`KnownControl`) of the built-in control kinds the package
/// recognises by name, from `TextBlock` through the chart family to `DataGrid`.
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
/// Closed enumeration (`KnownEvent`) of the built-in event kinds controls raise,
/// e.g. `Click`, `Changed`, `Selected`, `FocusChanged`, and `SortChanged`.
type KnownEvent =
    | Click
    | Changed
    | Selected
    | FocusChanged
    | SortChanged

[<RequireQualifiedAccess>]
/// Closed enumeration (`KnownAttribute`) of the built-in attribute names controls
/// accept, spanning content (`Text`/`Value`), data (`Series`/`Items`/`Nodes`), and
/// grid state (`SelectedRows`/`FocusedCell`).
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
/// The schema-facing control kind (`StandardControlKind`): the built-in kinds plus a
/// `Custom of string` escape hatch for consumer-defined controls.
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
/// The schema-facing event kind (`StandardEventKind`): the built-in events plus a
/// `Custom of string` case for consumer-defined event names.
type StandardEventKind =
    | Click
    | Changed
    | Selected
    | FocusChanged
    | SortChanged
    | Custom of string

[<RequireQualifiedAccess>]
/// The schema-facing attribute name (`StandardAttributeName`): the built-in attribute
/// names plus a `Custom of string` case for consumer-defined attributes.
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

/// Schema-facing attribute value (`StandardAttributeValue`): a typed union over the
/// primitive shapes an attribute may carry — `StandardText`/`StandardBool`/`StandardFloat`,
/// a `StandardStringList`, a `StandardMessage`/`StandardEvent` payload, or `StandardUntyped`.
type StandardAttributeValue<'msg> =
    | StandardText of string
    | StandardBool of bool
    | StandardFloat of float
    | StandardStringList of string list
    | StandardMessage of 'msg
    | StandardEvent of (string -> 'msg)
    | StandardUntyped of obj

/// Per-kind authoring contract (`ControlSchema`): the control `Kind`, its
/// `RequiredAttributes` and `SupportedAttributes`, the `SupportedEvents` it raises, and
/// whether `CustomAllowed` extension attributes are permitted.
type ControlSchema =
    { Kind: StandardControlKind
      RequiredAttributes: StandardAttributeName list
      SupportedAttributes: StandardAttributeName list
      SupportedEvents: StandardEventKind list
      CustomAllowed: bool }

/// Severity level of a `ControlDiagnostic` (`ControlDiagnosticSeverity`): `Info`,
/// `Warning`, or `Error`, ordered from advisory to authoring-blocking.
type ControlDiagnosticSeverity =
    | Info
    | Warning
    | Error

/// Closed classification (`ControlDiagnosticCode`) of an authoring or runtime defect,
/// from `MissingRequiredAttribute` and `MissingStableKey` to `ContrastFailure`,
/// `KeyCollision`, `StaleGeneratedReference`, and the feature-113 `UnstableReuseInput`.
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
    /// Feature 113 (Phase 5): an always-new input (a rebuilt `UntypedValue`, a per-frame event
    /// closure, an unstable key) that compared unequal across two builds of the same model and so
    /// defeats memoized subtree reuse. Reported by `Diagnostics.stabilityReport`; advisory only.
    | UnstableReuseInput
    /// Feature 116 (Phase 7): a control whose paint requires OFFSCREEN COMPOSITION — a non-opaque
    /// opacity group over a multi-node subtree, a clip (`ClipNode`), or a drop-shadow/image-filter
    /// (`DropShadow`). Offscreen composition allocates a separate layer + composite (a real backend
    /// cost and a cache-defeating boundary), so the framework surfaces it as actionable advisory
    /// context. Advisory ONLY: never fails a build, never alters rendered output (matching the
    /// `KeyCollision` non-blocking model).
    | OffscreenComposition

/// Accessibility/semantic role of a control (`AccessibilityRole`), e.g. `Button`,
/// `Slider`, `Grid`, or `Chart`; drives keyboard routing and assistive-tech naming,
/// with `Custom` for roles outside the built-in set.
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

/// Keyboard contract of a control (`KeyboardOperation`): whether it is `Focusable`,
/// the `ActivationKeys` that trigger it, and the `NavigationKeys` it consumes for
/// internal movement.
type KeyboardOperation =
    { Focusable: bool
      ActivationKeys: string list
      NavigationKeys: string list }

/// Recorded contrast measurement (`ContrastEvidence`): the `Foreground`/`Background`
/// colors, the measured `Ratio`, and the `RequiredRatio` it is checked against.
type ContrastEvidence =
    { Foreground: Color
      Background: Color
      Ratio: float
      RequiredRatio: float }

/// Declared value/range metadata (`NavRange`) for slider/progress/numeric roles.
/// Feature 100 (R5): declared range metadata for value/range roles — the SOLE source of
/// step/bounds, replacing the host's hardcoded 0.1 / 0..1 slider constant (FR-002). A
/// DEFAULT-step slider declares <c>{ Step = 0.1; Min = 0.0; Max = 1.0 }</c> so the pre-R5
/// numeric path is reproduced byte-identically (FR-007). Validation: <c>Min &lt;= Max</c>;
/// <c>Step &gt; 0</c>.
type NavRange =
    { Step: float
      Min: float
      Max: float }

/// Feature 114 (Phase 6): the logical size + current position of a virtualized collection
/// (e.g. a DataGrid), reported to assistive technology INDEPENDENT of how many items are
/// materialized. <c>TotalItems</c> is the total logical item count (from the collection's
/// <c>RowCount</c>/<c>ItemCount</c>); <c>FocusedIndex</c> is the focused item's logical index
/// within that total (<c>None</c> when nothing is focused). Both are computed from the logical
/// model, never from the realized slice (FR-012).
type CollectionPosition =
    { TotalItems: int
      FocusedIndex: int option }

/// Per-control accessibility record (`AccessibilityMetadata`): the semantic `Role`,
/// `NameSource`, current `State` flags, optional `FocusOrder`, the `Keyboard` contract,
/// optional `Contrast` evidence, optional value-range `Navigation` metadata, and the optional
/// virtualized-`Collection` total/position.
type AccessibilityMetadata =
    { Role: AccessibilityRole
      NameSource: string
      State: string list
      FocusOrder: int option
      Keyboard: KeyboardOperation
      Contrast: ContrastEvidence option
      /// Feature 100 (R5): the declared value/range step + bounds for a range role
      /// (<c>Some</c> for Slider/Progress/numeric value roles), <c>None</c> otherwise. Read by
      /// both <c>Focus.route</c> and the host per-intent resolver.
      Navigation: NavRange option
      /// Feature 114 (Phase 6): the total logical item count + current focused position for a
      /// virtualized collection control (<c>Some</c> for a virtualized DataGrid), <c>None</c> for
      /// every non-collection control (so at-rest a11y for existing controls is byte-identical).
      Collection: CollectionPosition option }

[<RequireQualifiedAccess>]
/// Input source that produced a `ControlEvent` (`ControlEventOrigin`): `Pointer`,
/// `Keyboard`, `Text`, `Focus`, `Selection`, or `Clipboard`.
type ControlEventOrigin =
    | Pointer
    | Keyboard
    | Text
    | Focus
    | Selection
    | Clipboard

/// Typed navigation outcome (`NavPayload`): `SteppedValue` for a value change,
/// `MovedSelection` for a selection move, or `MovedCell` for grid cell movement.
/// Feature 100 (R5): the closed set of navigation-outcome payload shapes (FR-005, SC-005).
/// Mirrors <c>NavIntent</c> one-to-one; exhaustively matched at the host edge.
type NavPayload =
    | SteppedValue of value: float
    | MovedSelection of index: int * item: string option
    | MovedCell of row: int * col: int

/// A dispatched control event (`ControlEvent`): its `Kind`, the source `ControlId`, the
/// `Origin` input source, an optional string `Payload`, and an optional typed `Nav` outcome.
type ControlEvent =
    { Kind: string
      ControlId: ControlId option
      Origin: ControlEventOrigin
      Payload: string option
      /// Feature 100 (R5): the closed typed navigation outcome for a focused-key navigation
      /// dispatch. A selection move dual-sets <c>Payload</c> (the moved item id, for existing
      /// string consumers) AND <c>Nav</c> (the closed <c>MovedSelection</c>); non-navigation
      /// events leave it <c>None</c>. <c>Payload : string option</c> is retained for backward
      /// compatibility (research R-3).
      Nav: NavPayload option }

/// Classification (`AttrCategory`) of what an attribute affects — `Content`, `Children`,
/// `Layout`, `Style`, `Theme`, `State`, `Validation`, `Accessibility`, `Event`, `Data`, or
/// `Slot` — used to route the attribute during lowering.
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
    /// Feature 095 (E5): the category under which named slot fills ride the `Attr` mechanism,
    /// mirroring E3's `Style`. Closed; only the internal `ControlInternals.slotFill` builder
    /// produces it — there is NO public free-form slot builder (the typed `Props` slot fields are
    /// the only sanctioned authoring path, FR-001).
    | Slot

/// The core declarative control node (`Control<'msg>`): its `Kind`, optional stable `Key`
/// identity, its `Attributes` and `Children`, optional text `Content`, and optional
/// `Accessibility` metadata. The unit of the authoring tree and the reconciler diff.
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
    /// Feature 093 (E3): an ordered list of attached style classes (list order = attach order).
    /// Rides the existing `Attr` mechanism under `AttrCategory.Style`; absent ≡ `[]` ≡ base.
    | StyleClassesValue of StyleClass list
    /// Feature 093 (E3): the control's current `VisualState`, consumed by `Style.resolve`. Rides
    /// the `Attr` mechanism so it travels WITH the control through the keyed reconciler diff — a
    /// state-driven look therefore survives a sibling-shifting re-render under E2's retained
    /// identity (FR-006, SC-005). Absent ≡ `Normal` ≡ the behaviour-preserving base case.
    | VisualStateValue of VisualState
    /// Feature 095 (E5): an ordered association list from declared slot NAME to the consumer's
    /// fill sub-tree. Rides the existing `Attr` mechanism under `AttrCategory.Slot` (the same shape
    /// E3 used for `StyleClassesValue`); a control carries at most one `Slot`-category attribute,
    /// last-writer-wins. The slot NAME is internal plumbing — a name ABSENT from this list is an
    /// unfilled slot (renders default), a name PRESENT is filled (renders the sub-tree, even when
    /// the sub-tree is empty). A slot fill is a static `Control<'msg>` value, NOT a data-bound
    /// template (FR-008). Lowering injects the fills into the control's `Children`, so they inherit
    /// E1–E4 + E2 retained identity by construction (FR-004, FR-005).
    | SlotFillsValue of (string * Control<'msg>) list
    | AccessibilityValue of AccessibilityMetadata
    | ThemeValue of Theme
    | ChildValue of Control<'msg>
    | ChildrenValue of Control<'msg> list
    | MessageValue of 'msg
    | EventValue of (ControlEvent -> 'msg)
    | UntypedValue of obj

/// A reported authoring/runtime issue (`ControlDiagnostic`): the offending `ControlId`
/// and `ControlKind`, the diagnostic `Code` and `Severity`, a human-readable `Message`,
/// and an optional `EvidencePath`.
type ControlDiagnostic =
    { ControlId: ControlId option
      ControlKind: ControlKind
      Code: ControlDiagnosticCode
      Severity: ControlDiagnosticSeverity
      Message: string
      EvidencePath: string option }

/// A wired event handler (`ControlEventBinding<'msg>`): binds a `ControlId` and
/// `EventKind` to a `Dispatch` function turning a `ControlEvent` into a host message.
type ControlEventBinding<'msg> =
    { ControlId: ControlId
      EventKind: string
      Dispatch: ControlEvent -> 'msg }

/// Output of rendering a control tree (`ControlRenderResult<'msg>`): the painted `Scene`,
/// the `Layout` root, the per-control `Bounds`, any `Diagnostics`, the `EventBindings` and
/// their `BoundIds`, and the total `NodeCount`.
type ControlRenderResult<'msg> =
    { Scene: Scene
      Layout: LayoutNode
      /// Evaluated absolute bounds of every laid-out control, keyed by `ControlId`
      /// (one entry per laid-out control instance). Populated by `Control.renderTree`
      /// from the computed `LayoutResult`; the preview `Control.render` leaves it empty.
      /// A host joins this with `EventBindings` (also keyed by `ControlId`) for hit-testing.
      Bounds: (ControlId * Rect) list
      Diagnostics: ControlDiagnostic list
      EventBindings: ControlEventBinding<'msg> list
      /// Canonical ids (the unified `Key ?? structural-path` scheme) of every node
      /// carrying at least one event binding. The same scheme as `EventBindings` and
      /// `Bounds`, so a recovered id is a direct membership/lookup key. Populated by
      /// `renderTree` and `render` (and the retained path); read by `nearestAuthored`.
      BoundIds: Set<ControlId>
      NodeCount: int }
