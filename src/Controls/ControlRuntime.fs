namespace FS.GG.UI.Controls
open FS.GG.UI.DesignSystem

type ControlCaret =
    { ControlId: ControlId
      Index: int }

type ControlSelection =
    { ControlId: ControlId
      Start: int
      End: int }

type ControlComposition =
    { ControlId: ControlId
      Text: string }

type ControlDrag =
    { ControlId: ControlId
      StartX: float
      StartY: float
      CurrentX: float
      CurrentY: float }

type ControlRuntimeEffect =
    | FocusChanged of ControlId option
    | HoverChanged of ControlId option
    | PressedControlsChanged of ControlId list
    | CaretChanged of ControlCaret option
    | SelectionChanged of ControlSelection option
    | CompositionChanged of ControlComposition option
    | DragChanged of ControlDrag option
    | StaleTarget of ControlId
    | CancelledInteraction of ControlId option
    | ReportControlRuntimeDiagnostic of ControlDiagnostic

type ControlRuntimeModel =
    { FocusedControl: ControlId option
      HoveredControl: ControlId option
      PressedControls: Set<ControlId>
      Caret: ControlCaret option
      Selection: ControlSelection option
      Composition: ControlComposition option
      ActiveDrag: ControlDrag option
      Diagnostics: ControlDiagnostic list
      RecentEffects: ControlRuntimeEffect list }

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
    | Reset

/// Feature 112 (FR-007): the targeted runtime-stamp result (see ControlRuntime.fsi).
type internal RuntimeStampResult<'msg> =
    { Stamped: Control<'msg>
      RuntimeStateTouchedNodeCount: int }

module ControlRuntime =
    let empty =
        { FocusedControl = None
          HoveredControl = None
          PressedControls = Set.empty
          Caret = None
          Selection = None
          Composition = None
          ActiveDrag = None
          Diagnostics = []
          RecentEffects = [] }

    let init () =
        empty, ([]: ControlRuntimeEffect list)

    let withEffects effects model =
        { model with RecentEffects = effects }, effects

    let staleDiagnostic controlId =
        Diagnostics.create
            (Some controlId)
            "control-runtime"
            StaleGeneratedReference
            ControlDiagnosticSeverity.Warning
            $"Stale interaction target '{controlId}' was recovered by ControlRuntime."

    let cancelledDiagnostic controlId =
        Diagnostics.create
            controlId
            "control-runtime"
            HitTestFailed
            ControlDiagnosticSeverity.Info
            "Control interaction was cancelled before completion."

    let clearTarget controlId model =
        { model with
            FocusedControl = model.FocusedControl |> Option.filter ((<>) controlId)
            HoveredControl = model.HoveredControl |> Option.filter ((<>) controlId)
            PressedControls = model.PressedControls.Remove controlId
            Caret = model.Caret |> Option.filter (fun caret -> caret.ControlId <> controlId)
            Selection = model.Selection |> Option.filter (fun selection -> selection.ControlId <> controlId)
            Composition = model.Composition |> Option.filter (fun composition -> composition.ControlId <> controlId)
            ActiveDrag = model.ActiveDrag |> Option.filter (fun drag -> drag.ControlId <> controlId) }

    let update msg model =
        match msg with
        | FocusControl controlId ->
            { model with FocusedControl = controlId }
            |> withEffects [ FocusChanged controlId ]
        | HoverControl controlId ->
            { model with HoveredControl = controlId }
            |> withEffects [ HoverChanged controlId ]
        | PressControl controlId ->
            let pressed = model.PressedControls.Add controlId
            { model with PressedControls = pressed }
            |> withEffects [ PressedControlsChanged(Set.toList pressed) ]
        | ReleaseControl controlId ->
            let pressed = model.PressedControls.Remove controlId
            { model with PressedControls = pressed }
            |> withEffects [ PressedControlsChanged(Set.toList pressed) ]
        | SetCaret caret ->
            { model with Caret = caret }
            |> withEffects [ CaretChanged caret ]
        | SetSelection selection ->
            { model with Selection = selection }
            |> withEffects [ SelectionChanged selection ]
        | StartComposition(controlId, text) ->
            let composition = Some { ControlId = controlId; Text = text }
            { model with Composition = composition }
            |> withEffects [ CompositionChanged composition ]
        | CommitComposition controlId ->
            let composition =
                model.Composition
                |> Option.filter (fun current -> current.ControlId <> controlId)

            { model with Composition = composition }
            |> withEffects [ CompositionChanged composition ]
        | StartDrag(controlId, x, y) ->
            let drag =
                Some
                    { ControlId = controlId
                      StartX = x
                      StartY = y
                      CurrentX = x
                      CurrentY = y }

            { model with ActiveDrag = drag }
            |> withEffects [ DragChanged drag ]
        | MoveDrag(x, y) ->
            let drag =
                model.ActiveDrag
                |> Option.map (fun current -> { current with CurrentX = x; CurrentY = y })

            { model with ActiveDrag = drag }
            |> withEffects [ DragChanged drag ]
        | EndDrag ->
            { model with ActiveDrag = None }
            |> withEffects [ DragChanged None ]
        | FocusLost ->
            { model with
                FocusedControl = None
                HoveredControl = None
                PressedControls = Set.empty
                ActiveDrag = None }
            |> withEffects [ FocusChanged None; HoverChanged None; PressedControlsChanged []; DragChanged None ]
        | RemoveControl controlId ->
            let next = clearTarget controlId model
            let diagnostic = staleDiagnostic controlId

            { next with Diagnostics = diagnostic :: next.Diagnostics }
            |> withEffects [ StaleTarget controlId; ReportControlRuntimeDiagnostic diagnostic ]
        | RecoverStaleTarget controlId ->
            let diagnostic = staleDiagnostic controlId

            { model with Diagnostics = diagnostic :: model.Diagnostics }
            |> withEffects [ StaleTarget controlId; ReportControlRuntimeDiagnostic diagnostic ]
        | CancelInteraction controlId ->
            let diagnostic = cancelledDiagnostic controlId

            { model with
                PressedControls = Set.empty
                Caret = None
                Selection = None
                Composition = None
                ActiveDrag = None
                Diagnostics = diagnostic :: model.Diagnostics }
            |> withEffects [ CancelledInteraction controlId; DragChanged None; ReportControlRuntimeDiagnostic diagnostic ]
        | Reset ->
            empty |> withEffects []

    let diagnostics model =
        model.Diagnostics

    // Feature 096 (R1): the pure, total, deterministic projection from live interaction state to a
    // single VisualState. The runtime-derivable precedence is the tail of FR-002's full closed order
    // (`Disabled > Validation > Loading > Pressed > Selected > Focused > Hover > Normal`); the head
    // states (`Disabled`/`Validation`/`Loading`) are consumer-set, never derived, so the projection
    // never returns one. No per-kind branching — a plain ordered cascade over the runtime model; an
    // id named by no interaction state resolves to `Normal`, never an exception (FR-002, SC-004).
    let deriveVisualState (model: ControlRuntimeModel) (controlId: ControlId) : VisualState =
        if model.PressedControls.Contains controlId then
            Pressed
        // Feature 102 (R8): forward-looking branch. The live host (`ControlsElmish`) never populates
        // `model.Selection` (the text-range selection model), so on the real render path this branch does
        // not fire today — only a consumer-set `Selected` reaches a control. Kept (not removed) so a future
        // host that tracks a text-range selection derives `Selected` here without a code change.
        elif model.Selection |> Option.exists (fun s -> s.ControlId = controlId) then
            Selected
        elif model.FocusedControl = Some controlId then
            Focused
        elif model.HoveredControl = Some controlId then
            Hover
        else
            Normal

    // Feature 096 (R1): replace-or-append the last-writer `visualState` attribute that
    // `ControlInternals.visualStateOf` reads. Pure; the prior attribute (if any) is dropped so the
    // single carrier channel never accumulates stale state (FR-003).
    let setVisualState (state: VisualState) (control: Control<'msg>) : Control<'msg> =
        { control with
            Attributes =
                (control.Attributes |> List.filter (fun a -> a.Name <> "visualState"))
                @ [ Attr.visualState state ] }

    // Feature 096 (R1): internal host bridge. Stamps each control's derived VisualState onto the
    // lowered Control<'msg> tree in the ControlId domain (pre-reconcile), preserving a consumer-set
    // non-Normal attribute and emitting NOTHING at Normal (byte-identity at rest). Reached by
    // Controls.Tests / Elmish.Tests / the Controls.Elmish host via InternalsVisibleTo. NOT in the
    // .fsi → automatically internal.
    let rec applyRuntimeVisualState (model: ControlRuntimeModel) (control: Control<'msg>) : Control<'msg> =
        let id = control.Key |> Option.defaultValue control.Kind
        // Recurse the structural Children channel first; the bridge is a pure tree walk.
        let withChildren =
            { control with Children = control.Children |> List.map (applyRuntimeVisualState model) }

        // Consumer-set non-Normal state wins and is returned unchanged (FR-003). A consumer Normal /
        // absent attribute lets the derived interaction state fill the slot; a derived Normal emits
        // NOTHING, so a Normal-and-unset node is byte-identical to the un-bridged build (FR-005).
        if ControlInternals.visualStateOf control.Attributes <> Normal then
            withChildren
        else
            match deriveVisualState model id with
            | Normal -> withChildren
            | derived -> setVisualState derived withChildren

    // Feature 112 (FR-001..FR-005): the FINAL visual state of a control under `model`, read from the
    // FRESH (un-stamped) node so the consumer-set state is unambiguous (a consumer non-Normal attribute
    // wins; else the runtime-derived state). Mirrors `applyRuntimeVisualState`'s precedence exactly, so
    // the targeted stamp lands on the same per-node state the full oracle would.
    let private finalVisualState (model: ControlRuntimeModel) (fresh: Control<'msg>) : VisualState =
        match ControlInternals.visualStateOf fresh.Attributes with
        | Normal -> deriveVisualState model (fresh.Key |> Option.defaultValue fresh.Kind)
        | consumerSet -> consumerSet

    // Feature 112 (FR-001/FR-004/FR-005/FR-007): the targeted parallel walk. Re-stamp only the controls
    // whose final state changed between `prev` and `cur`, reusing every unchanged subtree from
    // `prevStamped`. Returns (node, touchedNodeCount, changed?). A reused node returns the prev-stamped
    // instance (already carrying `finalState prev = finalState cur`); a rebuilt node is the fresh node
    // with `finalState cur` stamped. A child-count mismatch self-heals by oracle-stamping that subtree
    // (FR-006) so the walk is total even on an unexpected structural shift.
    let rec private targetedWalk
        (prev: ControlRuntimeModel)
        (cur: ControlRuntimeModel)
        (prevStamped: Control<'msg>)
        (fresh: Control<'msg>)
        : Control<'msg> * int * bool =
        if prevStamped.Children.Length <> fresh.Children.Length then
            // Structural misalignment (not expected on the model-unchanged path): oracle-stamp the
            // fresh subtree fully so the result is still byte-identical to the oracle (FR-006).
            applyRuntimeVisualState cur fresh, Control.count fresh, true
        else
            let finalCur = finalVisualState cur fresh
            let finalPrev = finalVisualState prev fresh

            let childResults =
                List.map2 (fun p f -> targetedWalk prev cur p f) prevStamped.Children fresh.Children

            let anyChildChanged = childResults |> List.exists (fun (_, _, changed) -> changed)
            let touchedChildren = childResults |> List.sumBy (fun (_, t, _) -> t)
            let selfChanged = finalCur <> finalPrev

            if not selfChanged && not anyChildChanged then
                // Final state unchanged AND nothing below changed: reuse the prev-stamped node verbatim
                // (it already carries `finalCur`), touching nothing (FR-004).
                prevStamped, 0, false
            else
                // Rebuild from the FRESH node (a clean base) with `finalCur` stamped; a `Normal` final
                // state emits NO `visualState` attribute, matching the oracle's byte-identity at rest.
                let rebuiltChildren = childResults |> List.map (fun (c, _, _) -> c)
                let baseNode = { fresh with Children = rebuiltChildren }

                let stamped =
                    match finalCur with
                    | Normal -> baseNode
                    | s -> setVisualState s baseNode

                stamped, touchedChildren + 1, true

    let applyRuntimeVisualStateTargeted
        (prev: ControlRuntimeModel)
        (cur: ControlRuntimeModel)
        (prevStamped: Control<'msg>)
        (fresh: Control<'msg>)
        : RuntimeStampResult<'msg> =
        let stamped, touched, _ = targetedWalk prev cur prevStamped fresh

        { Stamped = stamped
          RuntimeStateTouchedNodeCount = touched }

    // Feature 112 (FR-002/FR-006): the live route choice. Targeted when a prior stamped frame + model
    // are supplied (a model-unchanged repaint); else the full-tree oracle over the fresh tree.
    let runtimeStampFor
        (prior: (ControlRuntimeModel * Control<'msg>) option)
        (cur: ControlRuntimeModel)
        (fresh: Control<'msg>)
        : RuntimeStampResult<'msg> =
        match prior with
        | Some(prevModel, prevStamped) -> applyRuntimeVisualStateTargeted prevModel cur prevStamped fresh
        | None ->
            { Stamped = applyRuntimeVisualState cur fresh
              RuntimeStateTouchedNodeCount = Control.count fresh }
