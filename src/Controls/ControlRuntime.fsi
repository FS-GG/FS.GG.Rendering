namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

/// The text caret position (`Index`) within a focused control identified by `ControlId`.
type ControlCaret =
    { ControlId: ControlId
      Index: int }

/// A text selection range (`Start`..`End`) within the control identified by `ControlId`.
type ControlSelection =
    { ControlId: ControlId
      Start: int
      End: int }

/// In-flight IME composition `Text` being entered into the control identified by `ControlId`.
type ControlComposition =
    { ControlId: ControlId
      Text: string }

/// An active pointer drag on `ControlId`, tracking start (`StartX`/`StartY`) and current (`CurrentX`/`CurrentY`) coordinates.
type ControlDrag =
    { ControlId: ControlId
      StartX: float
      StartY: float
      CurrentX: float
      CurrentY: float }

/// An observable side effect emitted by `ControlRuntime.update` when interaction state changes (focus, hover, caret, selection, drag, scroll, diagnostics).
type ControlRuntimeEffect =
    | FocusChanged of ControlId option
    | HoverChanged of ControlId option
    | PressedControlsChanged of ControlId list
    | CaretChanged of ControlCaret option
    | SelectionChanged of ControlSelection option
    | CompositionChanged of ControlComposition option
    | DragChanged of ControlDrag option
    /// Feature 175: the new clamped scroll offset for the named `scroll-viewer`.
    | ScrollChanged of ControlId * float
    | StaleTarget of ControlId
    | CancelledInteraction of ControlId option
    | ReportControlRuntimeDiagnostic of ControlDiagnostic

/// The aggregate runtime interaction state: focused/hovered/pressed controls, `Caret`, `Selection`, `Composition`, `ActiveDrag`, and accumulated `Diagnostics`.
type ControlRuntimeModel =
    { FocusedControl: ControlId option
      HoveredControl: ControlId option
      PressedControls: Set<ControlId>
      Caret: ControlCaret option
      Selection: ControlSelection option
      Composition: ControlComposition option
      ActiveDrag: ControlDrag option
      /// Feature 175: per-`scroll-viewer` scroll model, keyed by ControlId. Absent ⇒ `ScrollState.empty`.
      ScrollOffsets: Map<ControlId, ScrollState>
      Diagnostics: ControlDiagnostic list
      RecentEffects: ControlRuntimeEffect list }

/// Host-owned overlay bridge data. `ControlRuntime` carries this value at the edge
/// without making product-owned overlay visibility part of `ControlRuntimeModel`.
type OverlayRuntimeBridge =
    { Overlay: OverlayState
      Effects: OverlayEffect list }

/// Runtime audit record for an interpreted overlay effect. These records are
/// derived from effects and do not add open or selected state to
/// `ControlRuntimeModel`.
type OverlayRuntimeDispatchRecord =
    { SurfaceId: ControlId option
      Kind: string
      Payload: string option
      ProductVisible: bool }

/// An input message driving the runtime transition, e.g. `FocusControl`, `HoverControl`, `PressControl`, `SetCaret`, `StartDrag`, or `Reset`.
type ControlRuntimeMsg =
    | FocusControl of ControlId option
    | HoverControl of ControlId option
    | PressControl of ControlId
    | ReleaseControl of ControlId
    | SetCaret of ControlCaret option
    | SetSelection of ControlSelection option
    | StartComposition of ControlId * string
    | CommitComposition of ControlId
    | StartDrag of ControlId * float * float
    | MoveDrag of float * float
    | EndDrag
    | FocusLost
    | RemoveControl of ControlId
    | RecoverStaleTarget of ControlId
    | CancelInteraction of ControlId option
    /// Feature 175: record the measured (contentHeight, viewportHeight) for a `scroll-viewer`.
    | SetScrollExtent of ControlId * float * float
    /// Feature 175: apply a scroll delta (drag/wheel/keyboard) to a `scroll-viewer`, clamped.
    | ScrollControl of ControlId * float
    | Reset

/// Feature 112 (FR-007): the targeted runtime-stamp result — the stamped tree plus the number of nodes
/// the targeted walk REBUILT this frame (the changed-state paths: affected identities + ancestor paths).
/// `internal` (the runtime-state stamp is a host-internal concern); reached by the Controls.Elmish host
/// and Controls.Tests via InternalsVisibleTo. `RuntimeStateTouchedNodeCount` is `0` on a no-change frame
/// and far below the node count on a localized hover/focus/press change.
type internal RuntimeStampResult<'msg> =
    { Stamped: Control<'msg>
      RuntimeStateTouchedNodeCount: int }

/// MVU runtime tracking control focus, hover, press, caret/selection, composition, drag, scroll, and derived visual state.
module ControlRuntime =
    /// Seeds an empty `ControlRuntimeModel` with no focus or interaction and its initial effects.
    val init: unit -> ControlRuntimeModel * ControlRuntimeEffect list
    /// Feature 175: the scroll model currently owned for `controlId` (`ScrollState.empty` if none).
    val scrollState: controlId: ControlId -> model: ControlRuntimeModel -> ScrollState
    /// Pure transition applying `msg` to `model`, returning the next model and the `ControlRuntimeEffect` list it raises.
    val update: msg: ControlRuntimeMsg -> model: ControlRuntimeModel -> ControlRuntimeModel * ControlRuntimeEffect list
    /// Returns the `ControlDiagnostic` list currently accumulated in `model`.
    val diagnostics: model: ControlRuntimeModel -> ControlDiagnostic list

    /// Feature 096 (R1): the pure, total, deterministic projection from live
    /// interaction state to a single VisualState. Selects the highest-ranked
    /// runtime-derivable state for `controlId` under the fixed closed order
    /// Pressed > Selected > Focused > Hover > Normal (the runtime-derivable tail of
    /// FR-002's Disabled > Validation > Loading > Pressed > Selected > Focused > Hover
    /// > Normal). A control named by no interaction state yields `Normal`. No per-kind
    /// branching; identical inputs always yield an identical result.
    val deriveVisualState: model: ControlRuntimeModel -> controlId: ControlId -> VisualState

    /// Feature 096 (R1): internal host bridge — NOT public surface. Stamps each control's derived
    /// VisualState onto the lowered Control<'msg> tree in the ControlId domain (pre-reconcile),
    /// preserving a consumer-set non-Normal attribute and emitting NOTHING at Normal (byte-identity
    /// at rest). Declared `internal` so the Controls.Elmish host and Controls.Tests / Elmish.Tests
    /// reach it via InternalsVisibleTo without enlarging the package's public contract.
    val internal applyRuntimeVisualState: model: ControlRuntimeModel -> control: Control<'msg> -> Control<'msg>

    /// Feature 112 (FR-001/FR-004/FR-005/FR-007): the TARGETED runtime visual-state stamp — re-stamps
    /// only the controls whose FINAL visual state changed between `prev` and `cur`, reusing every
    /// unchanged subtree from `prevStamped` (which, on the live model-unchanged path, has the same
    /// structure as `fresh`). `finalState M node = if visualStateOf node.Attributes <> Normal then that
    /// consumer-set state else deriveVisualState M (node.Key ?? node.Kind)`, computed from the `fresh`
    /// (un-stamped) node so the consumer state is unambiguous. A node is reused (touched 0) when its
    /// final state is unchanged and no descendant changed; else it is rebuilt from `fresh` with
    /// `finalState cur` stamped. Byte-identical to `applyRuntimeVisualState cur fresh` (the full oracle).
    /// A per-node structural misalignment (child-count mismatch) self-heals by oracle-stamping that
    /// subtree (FR-006). Returns the stamped tree + the rebuilt-node count. `internal`; tests reach it
    /// via InternalsVisibleTo.
    val internal applyRuntimeVisualStateTargeted:
        prev: ControlRuntimeModel ->
        cur: ControlRuntimeModel ->
        prevStamped: Control<'msg> ->
        fresh: Control<'msg> ->
            RuntimeStampResult<'msg>

    /// Feature 112 (FR-002/FR-006): the live route choice as a pure, deterministically-testable helper.
    /// Returns the TARGETED result (`applyRuntimeVisualStateTargeted`) when `prior = Some(prevModel,
    /// prevStamped)` (a model-unchanged frame with a prior stamped tree), else the FULL-tree oracle
    /// result over `fresh` (`applyRuntimeVisualState cur fresh`, touched = the whole node count — a
    /// first / model-changing frame re-builds the tree anyway). Encapsulates the `renderRetained` route
    /// decision so it is covered without driving the live loop. `internal`.
    val internal runtimeStampFor:
        prior: (ControlRuntimeModel * Control<'msg>) option ->
        cur: ControlRuntimeModel ->
        fresh: Control<'msg> ->
            RuntimeStampResult<'msg>

    /// Feature 175 (FR-001): internal host bridge — stamp the live scroll offset onto each
    /// `scroll-viewer` node whose id holds a positive offset (so `evaluateLayout` shifts its
    /// descendants). Emits nothing at offset 0/absent (byte-identity at rest). Keyed by `Key ?? Kind`,
    /// mirroring `applyRuntimeVisualState`. `internal`; reached by the Controls.Elmish host and tests
    /// via InternalsVisibleTo.
    val internal applyScrollOffsets: model: ControlRuntimeModel -> control: Control<'msg> -> Control<'msg>

    /// Builds the explicit host bridge for overlay state and effects without mutating product state.
    val attachOverlayEffects: overlay: OverlayState -> effects: OverlayEffect list -> OverlayRuntimeBridge

    /// Builds deterministic dispatch records for the overlay effects currently
    /// attached to the runtime bridge.
    val overlayDispatchRecords: bridge: OverlayRuntimeBridge -> OverlayRuntimeDispatchRecord list
