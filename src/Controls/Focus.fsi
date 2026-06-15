namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

/// Feature 094 (E4) — the pure focus model: a deterministic single tab order derived purely from
/// `AccessibilityMetadata`, keyboard traversal over that order, and classification of a delivered
/// key against the focused control's `KeyboardOperation`. Pure, total, deterministic — no I/O, no
/// live window, property-testable to >=1000 generated combinations (SC-006). The `ControlId`<->`RetainedId`
/// binding lives at the host seam (`Controls.Elmish.routeFocusedKey`), so `RetainedId` is absent here (R4).

/// One focusable stop in the computed tab order, derived purely from AccessibilityMetadata.
type FocusStop =
    { Control: ControlId
      Role: AccessibilityRole
      Keyboard: KeyboardOperation
      FocusOrder: int option }

/// The deterministic single tab order over a view's focusable controls (FR-001).
/// Stops are in traversal order: FocusOrder ascending, None last, document-order tiebreak.
type TabOrder =
    { Stops: FocusStop list }

/// A traversal command derived from an unconsumed traversal key (FR-002).
type FocusMove =
    | Next
    | Previous

/// Feature 100 (R5): closed selection-move direction for a linear-selection role (Home/End fold
/// to First/Last). Exhaustive. `RequireQualifiedAccess` keeps `Previous`/`Next` distinct from the
/// E4 `FocusMove` cases (Direction.Previous etc.).
[<RequireQualifiedAccess>]
type Direction =
    | Previous
    | Next
    | First
    | Last

/// Feature 100 (R5): the closed, role-derived classification of a focused control's navigation
/// key (FR-001). One role maps to exactly one case. `ValueStep` carries a SIGNED STEP DELTA
/// (declared `NavRange.Step` x key sign; Home/End fold to a delta that clamps to Min/Max), NOT a
/// resolved value — the host applies it to the live value and clamps. `SelectionMove`/`GridMove`
/// carry only direction/2-D delta; the host reads the live selection/grid model.
type NavIntent =
    | ValueStep of delta: float
    | SelectionMove of Direction
    | GridMove of rowDelta: int * colDelta: int

/// How a delivered key routes against the focused control's KeyboardOperation (FR-003/FR-007).
/// Closed -> the host's match is total. Text delivery is the host's E1 seam, consulted first,
/// so there is no text case here. Feature 100 (R5): the `Navigate` case now CARRIES a closed,
/// role-derived `NavIntent`.
type KeyRouting =
    | Activate
    | Navigate of NavIntent
    | Traverse of FocusMove
    | Fallthrough

/// The pure focus model: derive tab `order`, `traverse` it, and `route` a delivered key against the focused control.
module Focus =

    /// Derive the deterministic tab order from a lowered Control tree (FR-001): a pre-order walk
    /// that keeps controls whose `Accessibility.Keyboard.Focusable = true`, ordered by
    /// (FocusOrder ascending with None last, then document/pre-order index). A focusable control
    /// is a SINGLE stop — its subtree is not descended for further stops (a composite is one tab
    /// stop, clarified). Non-focusable controls never appear. Pure, total; never throws.
    val order: control: Control<'msg> -> TabOrder

    /// Pure traversal reduction (FR-002): (order, current focus, move) -> next focus.
    /// None + Next -> first; None + Previous -> last; wraps cyclically at both ends; a current
    /// id absent from the order resolves to the first stop (Next) / last stop (Previous), or None
    /// if the order is empty (stale-target recovery — clarified).
    /// Total/deterministic: identical inputs -> identical output.
    val traverse: order: TabOrder -> current: ControlId option -> move: FocusMove -> ControlId option

    /// Route a normalized key against the focused control's `role` + KeyboardOperation
    /// (FR-003/FR-007; FR-001/FR-006 for the navigation classification). `key` is the normalized
    /// key name matched against Activation/NavigationKeys; `isTab`/`shift` describe a traversal
    /// candidate. The control's own consumption wins: membership in ActivationKeys -> Activate;
    /// membership in NavigationKeys is classified by `role` (+ the declared `navRange` for a value
    /// role) into a closed `NavIntent` -> `Navigate intent`; both are tested BEFORE the Tab test, so
    /// a control that lists a traversal key consumes it. A navigation key whose role/range cannot
    /// form an intent (e.g. a value role with no `navRange`) -> Fallthrough (FR-008 no-op). Only an
    /// unconsumed Tab/Shift+Tab -> Traverse (Next/Previous by `shift`). Otherwise Fallthrough.
    /// `route` is the SINGLE role-specific branch (FR-006); pure, total; never throws.
    val route:
        role: AccessibilityRole ->
        keyboard: KeyboardOperation ->
        navRange: NavRange option ->
        key: string ->
        isTab: bool ->
        shift: bool ->
            KeyRouting

    /// Feature 108 (US1, FR-001..005): stamp `VisualState.Focused` on the single focusable control
    /// whose identity (`Key ?? structural path`, the feature-098 unification minted root "0", child
    /// `path + "." + index`, the SAME id `collectBoundsWith`/dispatch use) equals `focused`. Every
    /// other control is untouched; a control already carrying a consumer-set non-`Normal` state (e.g.
    /// `Disabled`) keeps it — `Focused` never overrides it (spec edge case). `None` returns the tree
    /// byte-identical — no stamp, at-rest output unchanged (SC-012). Reaches keyed AND unkeyed
    /// focusable controls (path makes same-kind siblings distinct, FR-002); structural/non-focusable
    /// elements are never stamped (FR-004); at most one control carries the ring (FR-003). Consumers
    /// reflect their own focus model by calling `markFocused model.Focused (view …)` in `view`
    /// (FR-005). Pure, total; never throws.
    val markFocused: focused: ControlId option -> control: Control<'msg> -> Control<'msg>
