namespace FS.GG.UI.Controls.Elmish

open System
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open Elmish

type AdapterDiagnostic =
    { Code: string
      Message: string
      Source: string }

type AdapterEffect<'msg> =
    | DispatchProductMessage of 'msg
    | DispatchControlRuntimeMessage of ControlRuntimeMsg
    | DispatchKeyboardMessage of KeyboardMsg
    | DispatchHostCommand of string
    | ReportAdapterDiagnostic of AdapterDiagnostic

type AdapterCommand<'msg> = AdapterEffect<'msg> list

type AdapterSubscription<'msg> =
    { Id: string
      Subscribe: unit -> AdapterCommand<'msg> }

type AdapterProgram<'model, 'msg> =
    { Init: unit -> 'model * AdapterCommand<'msg>
      Update: 'msg -> 'model -> 'model * AdapterCommand<'msg>
      View: 'model -> Control<'msg>
      Subscriptions: 'model -> AdapterSubscription<'msg> list }

/// Feature 111 (US1, FR-001): the closed trigger taxonomy naming why a frame ran (see ControlsElmish.fsi).
[<RequireQualifiedAccess>]
type FrameCause =
    | Idle
    | PointerMove
    | PointerDiscrete
    | Key
    | Tick
    | Resize
    | Theme

/// Feature 108/109/110/111 (US1, FR-001/002): per-frame structured work/timing signal (see ControlsElmish.fsi).
type FrameMetrics =
    { ProductModelChanged: bool
      ViewCalled: bool
      FullRenderCount: int
      RemeasuredNodeCount: int
      MemoHitCount: int
      MemoMissCount: int
      VirtualItemsMaterialized: int
      VirtualItemsTotal: int
      RepaintedNodeCount: int
      DirtyRectCount: int
      DirtyArea: int
      PictureCacheHitCount: int
      PictureCacheMissCount: int
      PictureCacheEntryCount: int
      TextMeasureCacheHitCount: int
      TextMeasureCacheMissCount: int
      LayoutInvalidatedNodeCount: int
      PointerSamplesReceived: int
      PointerMovesProcessed: int
      FullRenderFallbackCount: int
      FrameCause: FrameCause
      DiffRan: bool
      LayoutRan: bool
      PaintRan: bool
      FrameDuration: TimeSpan
      // Feature 120 (US1): non-golden live per-phase present timing (Zero on the deterministic path).
      PaintDuration: TimeSpan
      ComposeDuration: TimeSpan
      // Feature 120 (US3, FR-014): backend replay-cache per-frame counters (deterministic golden model).
      ReplayHitCount: int
      ReplayMissCount: int
      ReplayRecordCount: int
      ReplaySkippedNodeCount: int
      ReplayCacheNativeBytes: int }

/// Feature 108 (US3, FR-009): one ordered step of the deterministic perf driver.
[<RequireQualifiedAccess>]
type FrameInput<'msg> =
    | Key of ViewerKey * KeyModifiers
    | Pointer of PointerInteraction
    | Tick of TimeSpan
    | Idle

type InteractiveAppHost<'model, 'msg> =
    { Init: unit -> 'model * ViewerEffect list
      Update: 'msg -> 'model -> 'model * ViewerEffect list
      View: Size -> 'model -> Control<'msg>
      Theme: Theme
      MapKey: ViewerKey -> bool -> 'msg option
      MapPointer: PointerInteraction -> 'msg option
      Tick: TimeSpan -> 'msg option
      MapKeyChord: ViewerKey -> KeyModifiers -> 'msg option
      OnFrameMetrics: FrameMetrics -> unit
      Diagnostics: ViewerDiagnosticsOptions }

/// Verdict of a responds-proof (feature 090, FR-006): `Responsive` when a real input applied to the
/// running host produced a visible change in the rendered output (`before` ≠ `after`), `Inert` when
/// it did not. An inert host (renders but does not respond) can only yield `Inert`.
type RespondsVerdict =
    | Responsive
    | Inert

/// A captured input→visible-change responds-proof (feature 090, FR-006/FR-007): the `Before` frame,
/// the `After` frame produced by applying a real dispatched interaction (route → `host.Update` fold →
/// re-render, exactly as the live repaint loop), and the `Verdict`. A distinct evidence class from a
/// render-only screenshot (one frame, no interaction) and from the offscreen `runInteractivePointerOnce`
/// route probe (model layer only): an app that renders but does not respond yields identical frames and
/// an `Inert` verdict, so "renders" cannot be passed off as "responds".
type RespondsProof =
    { Before: Scene
      After: Scene
      Verdict: RespondsVerdict }

module AdapterCmd =
    let none: Cmd<'msg> = Cmd.none

    let ofMessage (msg: 'msg) : AdapterCommand<'msg> = [ DispatchProductMessage msg ]

    let productMessages (command: AdapterCommand<'msg>) : 'msg list =
        command
        |> List.choose (function
            | DispatchProductMessage msg -> Some msg
            | _ -> None)

    let toCmd (route: AdapterEffect<'msg> -> 'msg) (command: AdapterCommand<'msg>) : Cmd<'msg> =
        command
        |> List.map (fun effect -> (fun (dispatch: Dispatch<'msg>) -> dispatch (route effect)))

module ControlsElmish =
    let diagnostic source code message =
        { Source = source
          Code = code
          Message = message }

    let interpretKeyboardEffect mapCommand effect =
        match effect with
        | CommandResolved command -> [ DispatchProductMessage(mapCommand command) ]
        | KeyStateChanged _
        | LayoutChanged _
        | ModeChanged _
        | PendingSequenceChanged _
        | StateDisplayChanged _ -> []
        | RequestHostKeyCapture key -> [ DispatchHostCommand $"capture-key:{key}" ]
        | ReportKeyboardDiagnostic keyboardDiagnostic ->
            [ ReportAdapterDiagnostic(diagnostic "keyboard-input" keyboardDiagnostic.Code keyboardDiagnostic.Message) ]

    let interpretControlEffect mapRuntime effect =
        match effect with
        | FocusChanged controlId ->
            [ DispatchControlRuntimeMessage(FocusControl controlId)
              DispatchProductMessage(mapRuntime (FocusControl controlId)) ]
        | HoverChanged controlId ->
            [ DispatchControlRuntimeMessage(HoverControl controlId)
              DispatchProductMessage(mapRuntime (HoverControl controlId)) ]
        | PressedControlsChanged _
        | CaretChanged _
        | SelectionChanged _
        | CompositionChanged _
        | DragChanged _
        | CancelledInteraction _ -> []
        | StaleTarget controlId ->
            [ ReportAdapterDiagnostic(diagnostic "control-runtime" "StaleTarget" $"Stale control target '{controlId}' was ignored by the Controls adapter.") ]
        | ReportControlRuntimeDiagnostic controlDiagnostic ->
            [ ReportAdapterDiagnostic(diagnostic "control-runtime" (string controlDiagnostic.Code) controlDiagnostic.Message) ]

    let interpretPointerEffect (mapInteraction: PointerInteraction -> 'msg option) (interaction: PointerInteraction) =
        match interaction with
        | Diagnostic pointerDiagnostic ->
            [ ReportAdapterDiagnostic(diagnostic "pointer" (string pointerDiagnostic.Code) pointerDiagnostic.Message) ]
        | meaningful ->
            match mapInteraction meaningful with
            | Some msg -> [ DispatchProductMessage msg ]
            | None -> []

    let interpretPointerOutcome
        (mapInteraction: PointerInteraction -> 'msg option)
        (interactions: PointerInteraction list)
        (runtimeMessages: ControlRuntimeMsg list)
        =
        (runtimeMessages |> List.map DispatchControlRuntimeMessage)
        @ (interactions |> List.collect (interpretPointerEffect mapInteraction))

    let subscriptions (keyboard: AdapterSubscription<'msg> list) (controls: AdapterSubscription<'msg> list) =
        keyboard @ controls

    let program init update view subscriptions =
        { Init = init
          Update = update
          View = view
          Subscriptions = subscriptions }

    let widgetView (view: 'model -> Widget<'msg>) : 'model -> Control<'msg> =
        view >> Widget.toControl

    let programOfWidget init update view subscriptions =
        program init update (widgetView view) subscriptions

    // FR-001/FR-003 (feature 090): a pointer Click is binding-eligible for an authored control's
    // click-equivalent bindings (`onClick`→"click", a click-driven toggle `onChanged`→"changed", a
    // click-driven `onSelected`→"selected"). Other interactions (hover/drag/scroll) are not
    // binding-eligible here and go straight to `MapPointer`.
    let clickEquivalentKinds = [ "click"; "changed"; "selected" ]

    // Resolve the authored bindings (if any) a single interaction should dispatch. `Some msgs` means
    // an authored binding consumed the interaction (MapPointer is NOT consulted for it); `None` means
    // no authored binding matched, so the host falls back to `MapPointer` with the raw interaction.
    let bindingMessagesFor (rendered: ControlRenderResult<'msg>) (interaction: PointerInteraction) : 'msg list option =
        match interaction with
        | Click(control, _, _, _) ->
            match Control.nearestAuthored rendered control with
            | Some authored ->
                let matched =
                    rendered.EventBindings
                    |> List.filter (fun binding ->
                        binding.ControlId = authored
                        && List.contains binding.EventKind clickEquivalentKinds)

                match matched with
                | [] -> None
                | bindings ->
                    bindings
                    |> List.map (fun binding ->
                        binding.Dispatch
                            { Kind = binding.EventKind
                              ControlId = Some authored
                              Origin = ControlEventOrigin.Pointer
                              Payload = None
                              Nav = None })
                    |> Some
            | None -> None
        | _ -> None

    // Translate a native viewer pointer input into the neutral 075 `PointerSample` the gesture fold
    // consumes. Pure/total; shared by the preserved full-render oracle and the feature-110 retained
    // route so both feed `Pointer.update` the identical sample (a precondition of dispatch parity).
    let private pointerSampleOf (input: ViewerPointerInput) : PointerSample =
        let phase =
            match input.Phase with
            | ViewerPointerPhaseKind.Moved -> PointerPhase.Moved
            | ViewerPointerPhaseKind.Pressed -> PointerPhase.Pressed
            | ViewerPointerPhaseKind.Released -> PointerPhase.Released
            | ViewerPointerPhaseKind.Wheel -> PointerPhase.Wheel
            | ViewerPointerPhaseKind.Exited -> PointerPhase.Exited

        let button =
            input.Button
            |> Option.map (fun b ->
                match b with
                | ViewerPointerButtonKind.Primary -> PointerButton.Primary
                | ViewerPointerButtonKind.Secondary -> PointerButton.Secondary
                | ViewerPointerButtonKind.Middle -> PointerButton.Middle)

        { Phase = phase
          X = input.X
          Y = input.Y
          Button = button
          DeltaX = input.DeltaX
          DeltaY = input.DeltaY }

    // The single pointer-routing step the interactive host performs per native sample: render the
    // current Control tree at the live extent, hit-test the laid-out bounds via the shipped 075
    // pipeline (Pointer.update over the LayoutResult, incl. the 4px click/drag fold), then route the
    // emitted interactions through interpretPointerOutcome host.MapPointer to product messages.
    // Returns the advanced PointerState (threaded across samples) + the product messages. Exposed so
    // a headless test exercises the EXACT routing runInteractiveApp wires (research D6 honest bar).
    // Feature 110: PRESERVED unchanged as the parity oracle and the counted full-render fallback
    // (FR-007); the normal live route is now `routeRetainedPointer` (no per-sample full render).
    let routeInteractivePointer
        (host: InteractiveAppHost<'model, 'msg>)
        (state: PointerState)
        (size: Size)
        (model: 'model)
        (input: ViewerPointerInput)
        : PointerState * 'msg list =
        match Pointer.toMsg (pointerSampleOf input) with
        | None -> state, []
        | Some pointerMsg ->
            let rendered = Control.renderTree host.Theme size (host.View size model)

            let available: FS.GG.UI.Layout.AvailableSpace =
                { Width = float size.Width
                  WidthMode = FS.GG.UI.Layout.Exactly
                  Height = float size.Height
                  HeightMode = FS.GG.UI.Layout.Exactly }

            let layoutResult = FS.GG.UI.Layout.Layout.evaluate available rendered.Layout
            let policy = FS.GG.UI.Layout.Defaults.pixelSnapPolicy 1.0

            let state', interactions, _runtimeMessages =
                Pointer.update policy layoutResult pointerMsg state

            // FR-001/FR-003 (feature 090): authored EventBindings win; MapPointer is the fallback.
            // For each interaction the host (1) recovers the authored control id via
            // `Control.nearestAuthored` (so a hit on an inner positional node inside a container-keyed
            // composite resolves to the authored container id), (2) looks up `rendered.EventBindings`
            // for a binding on that id whose `EventKind` is click-equivalent, and (3) dispatches the
            // bound message — WITHOUT also offering the interaction to `MapPointer` (no double-advance).
            // An interaction with no consuming binding (no match, or recovery `None`) falls back to
            // `MapPointer` with the raw interaction exactly as before, so existing `MapPointer`-only
            // consumers are bit-for-bit unchanged (additive). Interaction order is preserved.
            let messages =
                interactions
                |> List.collect (fun interaction ->
                    match bindingMessagesFor rendered interaction with
                    | Some msgs -> msgs
                    | None ->
                        interpretPointerEffect host.MapPointer interaction
                        |> AdapterCmd.productMessages)

            state', messages

    // Feature 110 (FR-002/FR-003): resolve a single interaction's authored bindings from the RETAINED
    // frame — the mirror of `bindingMessagesFor`, reading the retained frame's `EventBindings` instead
    // of a freshly rendered tree. `Some msgs` = an authored binding consumed the interaction (MapPointer
    // is NOT consulted); `None, false` = no authored binding matched (the host falls back to MapPointer,
    // exactly as the oracle's `None`); `None, true` = the retained frame could NOT resolve a bindable
    // `Click` hit (`retainedHitTest` returned `None` over a point the `Click` named), so the caller must
    // fall back to the full-render oracle and count it (FR-007/FR-009). Byte-identical to the oracle:
    // `retainedHitTest x y` lands on the same node `Pointer.update` hit (same cached geometry) and the
    // `authoredControlIds` lookup climbs to the same authored id `nearestAuthored` would.
    let private retainedBindingMessages
        (retained: RetainedRender<'msg>)
        (render: ControlRenderResult<'msg>)
        (interaction: PointerInteraction)
        : 'msg list option * bool =
        match interaction with
        | Click(_, _, x, y) ->
            match RetainedRender.retainedHitTest x y retained with
            | None -> None, true
            | Some rid ->
                match Map.tryFind rid (RetainedRender.authoredControlIds render.BoundIds retained) with
                | None -> None, false
                | Some authored ->
                    let matched =
                        render.EventBindings
                        |> List.filter (fun binding ->
                            binding.ControlId = authored
                            && List.contains binding.EventKind clickEquivalentKinds)

                    match matched with
                    | [] -> None, false
                    | bindings ->
                        bindings
                        |> List.map (fun binding ->
                            binding.Dispatch
                                { Kind = binding.EventKind
                                  ControlId = Some authored
                                  Origin = ControlEventOrigin.Pointer
                                  Payload = None
                                  Nav = None })
                        |> Some,
                        false
        | _ -> None, false

    // Feature 110 (FR-001/FR-002/FR-003): route ONE already-resolved interaction from the retained frame.
    // No `host.View`/`Control.renderTree` for routing on the normal path. Returns (messages, fallback
    // count): a resolvable interaction returns 0; an unresolvable bindable hit runs the preserved
    // full-render oracle (`Control.renderTree` + `bindingMessagesFor`) ONCE and returns 1, dispatching
    // identically to the oracle (the fallback IS the oracle).
    let routeRetainedInteraction
        (host: InteractiveAppHost<'model, 'msg>)
        (size: Size)
        (model: 'model)
        (retained: RetainedRender<'msg>)
        (render: ControlRenderResult<'msg>)
        (interaction: PointerInteraction)
        : 'msg list * int =
        match retainedBindingMessages retained render interaction with
        | Some msgs, _ -> msgs, 0
        | None, false -> interpretPointerEffect host.MapPointer interaction |> AdapterCmd.productMessages, 0
        | None, true ->
            // Counted full-render fallback (FR-007/FR-009): a fresh render + the oracle's resolution.
            let rendered = Control.renderTree host.Theme size (host.View size model)

            match bindingMessagesFor rendered interaction with
            | Some msgs -> msgs, 1
            | None -> interpretPointerEffect host.MapPointer interaction |> AdapterCmd.productMessages, 1

    // Feature 110 (FR-001/FR-004/FR-006): the live retained pointer route. Same gesture fold as the
    // oracle, but over the retained frame's CACHED `LayoutResult` (no fresh layout eval) and resolving
    // each interaction from the retained frame (no fresh render). Returns the advanced PointerState, the
    // product messages, and the summed `FullRenderFallbackCount`.
    let routeRetainedPointer
        (host: InteractiveAppHost<'model, 'msg>)
        (retained: RetainedRender<'msg>)
        (render: ControlRenderResult<'msg>)
        (state: PointerState)
        (size: Size)
        (model: 'model)
        (input: ViewerPointerInput)
        : PointerState * 'msg list * int =
        match Pointer.toMsg (pointerSampleOf input) with
        | None -> state, [], 0
        | Some pointerMsg ->
            let policy = FS.GG.UI.Layout.Defaults.pixelSnapPolicy 1.0

            let state', interactions, _runtimeMessages =
                Pointer.update policy retained.Layout pointerMsg state

            let mutable fallbacks = 0

            let messages =
                interactions
                |> List.collect (fun interaction ->
                    let msgs, fb = routeRetainedInteraction host size model retained render interaction
                    fallbacks <- fallbacks + fb
                    msgs)

            state', messages, fallbacks

    // 092 (FR-004): resolve a click to the stable RetainedId of the control under it, via the
    // retained tree's per-node boxes — replaces the 090 `ControlId` `hitTest |> nearestAuthored`
    // path, which collapses unkeyed same-kind siblings onto one id and disagrees with
    // `nearestAuthored`'s scheme. `None` for a true gap / outside the root.
    let resolveFocus (retained: RetainedRender<'msg>) (x: float) (y: float) : RetainedId option =
        RetainedRender.retainedHitTest x y retained

    // Find a retained node by its stable identity (the focused control's node).
    let rec private tryFindNode (id: RetainedId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
        if n.Identity = id then
            Some n
        else
            n.Children |> List.tryPick (tryFindNode id)

    // Read a control's current text value (the first-focus seed, FR-005): the `text`/`value`
    // attribute, else its `Content`, else empty.
    let private controlTextValue (c: Control<'msg>) : string =
        let fromAttr name =
            c.Attributes
            |> List.tryPick (fun a ->
                if a.Name = name then
                    match a.Value with
                    | TextValue v -> Some v
                    | _ -> None
                else
                    None)

        fromAttr "text"
        |> Option.orElseWith (fun () -> fromAttr "value")
        |> Option.orElseWith (fun () -> c.Content)
        |> Option.defaultValue ""

    // Kind-derived line mode (FR-005): a `text-area` is multi-line; every other kind single-line.
    // Fixes the 090 hard-coded-`SingleLine` defect that truncated multi-line fields.
    let private lineModeOf (c: Control<'msg>) : TextInputMode =
        if c.Kind = "text-area" then MultiLine else SingleLine

    /// 092 focus-aware text routing on the RETAINED structure (FR-005/FR-006), replacing the 090
    /// `ControlId`-keyed seam: deliver `msg` to the focused control's `RetainedId`-keyed `TextInput`
    /// state held in `retained.StateByIdentity[id].Text`. On the FIRST keystroke after focus (no
    /// existing `Text` entry) the model is seeded from the control's current value + kind-derived
    /// line mode, so the keystroke APPENDS to the pre-filled value instead of discarding it (fixes
    /// the 090 empty-seed / hard-coded-`SingleLine` defects). Returns the next retained structure
    /// (with the advanced text state, which `step` carries across a positional shift) and ALL of the
    /// focused control's matched `onChanged` product messages — every binding, not just the first
    /// (FR-006). When `focused` is `None` or names no live node, nothing is delivered and the
    /// structure is returned unchanged. Scope: routing seam only — caret/selection/IME-UX/undo and
    /// general focus/tab-traversal are trajectory item E4.
    let routeFocusedText
        (retained: RetainedRender<'msg>)
        (focused: RetainedId option)
        (msg: TextInputMsg)
        : RetainedRender<'msg> * 'msg list =
        match focused with
        | Some id ->
            match tryFindNode id retained.Root with
            | Some node ->
                let priorState = retained.StateByIdentity |> Map.tryFind id

                // The carried draft is authoritative while focused; the model value re-seeds the
                // draft ONLY on the focus-acquisition transition (no existing Text entry), never on
                // an ordinary re-render — so a same-frame model change cannot overwrite typing.
                let model0 =
                    match priorState |> Option.bind (fun s -> s.Text) with
                    | Some existing -> existing
                    | None ->
                        let controlId = node.Control.Key |> Option.defaultValue node.Control.Kind
                        fst (TextInput.init controlId (lineModeOf node.Control) (controlTextValue node.Control))

                let model', _effects = TextInput.update msg model0

                let newState =
                    { (priorState |> Option.defaultValue { Animation = None; Text = None }) with
                        Text = Some model' }

                let retained' =
                    { retained with
                        StateByIdentity = Map.add id newState retained.StateByIdentity }

                // FR-006: dispatch EVERY matched `onChanged` binding on the focused control (the 090
                // path dropped all but the first via `List.tryHead`).
                let productMessages =
                    ControlInternals.eventBindingsOf node.Control
                    |> List.filter (fun binding -> binding.EventKind = "changed")
                    |> List.map (fun binding ->
                        binding.Dispatch
                            { Kind = "changed"
                              ControlId = Some binding.ControlId
                              Origin = ControlEventOrigin.Text
                              Payload = Some model'.DraftText
                              Nav = None })

                retained', productMessages
            | None -> retained, []
        | None -> retained, []

    // Read a control's current numeric `value` (the slider/numeric step base), defaulting to the
    // renderer's own default (sliderGeom uses 0.5) when absent.
    let private controlFloatValue (c: Control<'msg>) (deflt: float) : float =
        c.Attributes
        |> List.tryPick (fun a ->
            if a.Name = "value" then
                match a.Value with
                | FloatValue v -> Some v
                | TextValue t ->
                    match Double.TryParse(t, Globalization.CultureInfo.InvariantCulture) with
                    | true, v -> Some v
                    | _ -> None
                | _ -> None
            else
                None)
        |> Option.defaultValue deflt

    // Feature 100 (R5): the last value of a named attribute on the lowered control (the renderer's
    // `tryLast` convention — the last write wins), used to read the selection/value model below.
    let private lastAttrValue (name: string) (c: Control<'msg>) : AttrValue<'msg> option =
        c.Attributes
        |> List.filter (fun a -> a.Name = name)
        |> List.tryLast
        |> Option.map (fun a -> a.Value)

    // The linear-selection model: the authored item ids and the current selected id.
    let private controlItems (c: Control<'msg>) : string list =
        match lastAttrValue "items" c with
        | Some(StringListValue values) -> values
        | _ -> []

    let private controlSelectedItem (c: Control<'msg>) : string option =
        match lastAttrValue "value" c with
        | Some(TextValue value) -> Some value
        | _ -> None

    // The grid model: row keys (row dimension), column keys (column dimension), and current cell.
    let private dataGridRowKeys (c: Control<'msg>) : string list =
        match lastAttrValue "rows" c with
        | Some(UntypedValue o) ->
            match o with
            | :? (DataGridRow list) as rows -> rows |> List.map (fun r -> r.Key)
            | :? (DataGridRow array) as rows -> rows |> Array.toList |> List.map (fun r -> r.Key)
            | _ -> []
        | _ -> []

    let private dataGridColumnKeys (c: Control<'msg>) : string list =
        match lastAttrValue "columns" c with
        | Some(UntypedValue o) ->
            match o with
            | :? (DataGridColumn list) as cols -> cols |> List.map (fun col -> col.Key)
            | :? (DataGridColumn array) as cols -> cols |> Array.toList |> List.map (fun col -> col.Key)
            | _ -> []
        | _ -> []

    let private dataGridFocusedCell (c: Control<'msg>) : DataGridFocusedCell option =
        match lastAttrValue "focusedCell" c with
        | Some(UntypedValue o) ->
            match o with
            | :? (DataGridFocusedCell option) as cell -> cell
            | :? DataGridFocusedCell as cell -> Some cell
            | _ -> None
        | _ -> None

    // FR-003 / research R-2: the control's SELECTION binding — `EventKind = "selected"`, falling back
    // to `"changed"` (a radio-group binds `onChanged`, so the fallback is what makes it operable).
    let private selectionBindings (ownBindings: ControlEventBinding<'msg> list) : ControlEventBinding<'msg> list =
        match ownBindings |> List.filter (fun b -> b.EventKind = "selected") with
        | [] -> ownBindings |> List.filter (fun b -> b.EventKind = "changed")
        | selected -> selected

    let private dispatchNav
        (bindings: ControlEventBinding<'msg> list)
        (nodeId: ControlId)
        (kind: string)
        (payload: string option)
        (nav: NavPayload)
        : 'msg list =
        bindings
        |> List.map (fun b ->
            b.Dispatch
                { Kind = kind
                  ControlId = Some nodeId
                  Origin = ControlEventOrigin.Keyboard
                  Payload = payload
                  Nav = Some nav })

    // FR-002/FR-007: a value/range role's step. `delta` is the signed step (or a Home/End jump) from
    // `Focus.route`; the host reads the live value + declared `NavRange` and clamps. A default-step
    // slider ({0.1;0;1}) produces a value byte-identical to the pre-R5 `steppedValue` path; a clamp
    // no-op (`target = current`, already at the bound) dispatches NOTHING (FR-009).
    let private resolveValueStep (c: Control<'msg>) (nodeId: ControlId) (ownBindings: ControlEventBinding<'msg> list) (delta: float) : 'msg list =
        let range =
            c.Accessibility
            |> Option.bind (fun m -> m.Navigation)
            |> Option.defaultValue { Step = 0.1; Min = 0.0; Max = 1.0 }

        let current = controlFloatValue c 0.5
        let target = Math.Clamp(current + delta, range.Min, range.Max)

        if target = current then
            []
        else
            let payload = target.ToString(Globalization.CultureInfo.InvariantCulture)

            dispatchNav
                (ownBindings |> List.filter (fun b -> b.EventKind = "changed"))
                nodeId
                "changed"
                (Some payload)
                (SteppedValue target)

    // FR-003/FR-009: a linear-selection role's move. Reads the item count + current index; an empty
    // group or an unresolvable current index dispatches NOTHING; the new index is clamped to
    // [0, n-1] and a clamp no-op (clamped = current) dispatches NOTHING.
    let private resolveSelectionMove (c: Control<'msg>) (nodeId: ControlId) (ownBindings: ControlEventBinding<'msg> list) (dir: Direction) : 'msg list =
        let items = controlItems c
        let n = List.length items

        if n = 0 then
            []
        else
            match controlSelectedItem c |> Option.bind (fun sel -> items |> List.tryFindIndex (fun item -> item = sel)) with
            | None -> []
            | Some i ->
                let target =
                    match dir with
                    | Direction.Previous -> i - 1
                    | Direction.Next -> i + 1
                    | Direction.First -> 0
                    | Direction.Last -> n - 1

                let clamped = max 0 (min (n - 1) target)

                if clamped = i then
                    []
                else
                    let itemId = List.item clamped items
                    dispatchNav (selectionBindings ownBindings) nodeId "selected" (Some itemId) (MovedSelection(clamped, Some itemId))

    // FR-004/FR-009: a grid role's 2-D move. Reads dims (row/column counts) + current cell; an empty
    // grid or an unresolvable current cell dispatches NOTHING; the new cell is clamped to the grid
    // and an edge clamp no-op dispatches NOTHING.
    let private resolveGridMove (c: Control<'msg>) (nodeId: ControlId) (ownBindings: ControlEventBinding<'msg> list) (rowDelta: int, colDelta: int) : 'msg list =
        let rowKeys = dataGridRowKeys c
        let colKeys = dataGridColumnKeys c
        let rows = List.length rowKeys
        let cols = List.length colKeys

        if rows = 0 || cols = 0 then
            []
        else
            match dataGridFocusedCell c with
            | None -> []
            | Some cell ->
                match (rowKeys |> List.tryFindIndex (fun k -> k = cell.RowKey)), (colKeys |> List.tryFindIndex (fun k -> k = cell.ColumnKey)) with
                | Some r, Some col ->
                    let newRow = max 0 (min (rows - 1) (r + rowDelta))
                    let newCol = max 0 (min (cols - 1) (col + colDelta))

                    if newRow = r && newCol = col then
                        []
                    else
                        let cellId = sprintf "%s:%s" (List.item newRow rowKeys) (List.item newCol colKeys)
                        dispatchNav (selectionBindings ownBindings) nodeId "selected" (Some cellId) (MovedCell(newRow, newCol))
                | _ -> []

    // FR-006: the uniform per-intent resolver. Branches on the INTENT (not the control kind) — the
    // only role-specific logic is `Focus.route`'s role -> `NavIntent` classification. Pure.
    let private resolveNavIntent (node: RetainedNode<'msg>) (nodeId: ControlId) (ownBindings: ControlEventBinding<'msg> list) (intent: NavIntent) : 'msg list =
        match intent with
        | ValueStep delta -> resolveValueStep node.Control nodeId ownBindings delta
        | SelectionMove dir -> resolveSelectionMove node.Control nodeId ownBindings dir
        | GridMove(rowDelta, colDelta) -> resolveGridMove node.Control nodeId ownBindings (rowDelta, colDelta)

    // Normalize a host `ViewerKey` (+ a leading `Shift+` on an `Unknown` raw) to the (keyName, isTab)
    // pair `Focus.route` matches against `Activation`/`NavigationKeys`. A bare/`Shift+`-prefixed "Tab"
    // is the traversal candidate (isTab = true); every other key is a plain name (isTab = false).
    let private normalizeFocusKey (key: ViewerKey) : string * bool =
        match key with
        | Enter -> "Enter", false
        | Space -> "Space", false
        | ArrowLeft -> "ArrowLeft", false
        | ArrowRight -> "ArrowRight", false
        | ArrowUp -> "ArrowUp", false
        | ArrowDown -> "ArrowDown", false
        | ViewerKey.Unknown raw ->
            let bare =
                if raw.StartsWith("Shift+", StringComparison.OrdinalIgnoreCase) then
                    raw.Substring 6
                else
                    raw

            if String.Equals(bare, "Tab", StringComparison.OrdinalIgnoreCase) then
                "Tab", true
            else
                raw, false
        | other -> ViewerKeyboard.toKeyId other, false

    /// E4 (FR-003/FR-006/FR-007): route a delivered key to the current `FocusedControl` over the
    /// RETAINED tree, generalizing the 092 `routeFocusedText` text seam to all interactive kinds.
    /// Resolves the focused control via its stable `RetainedId`, reads its `KeyboardOperation`, and
    /// applies `Focus.route`: `Activate` fires the control's authored activation bindings (the same
    /// message a pointer activation dispatches, once); `Navigate` steps a value control and fires its
    /// `onChanged` bindings; `Traverse` emits a `FocusControl` for `Focus.traverse`; `Fallthrough`
    /// emits nothing (the host then consults `host.MapKey`). The E1 text seam is consulted by the
    /// host BEFORE this, so text delivery is unchanged (SC-003). Total; never throws.
    let routeFocusedKey
        (retained: RetainedRender<'msg>)
        (focused: RetainedId option)
        (order: TabOrder)
        (key: ViewerKey)
        (shift: bool)
        : RetainedRender<'msg> * ControlRuntimeMsg list * 'msg list =
        match focused with
        | None -> retained, [], []
        | Some id ->
            match tryFindNode id retained.Root with
            | None -> retained, [], []
            | Some node ->
                let nodeId = node.Control.Key |> Option.defaultValue node.Control.Kind

                let keyboard =
                    node.Control.Accessibility
                    |> Option.map (fun m -> m.Keyboard)
                    |> Option.defaultValue
                        { Focusable = false
                          ActivationKeys = []
                          NavigationKeys = [] }

                let keyName, isTab = normalizeFocusKey key

                // The focused control's OWN authored bindings (a focusable composite is a single
                // stop, so descendant bindings are excluded by the id filter).
                let ownBindings =
                    ControlInternals.eventBindingsOf node.Control
                    |> List.filter (fun b -> b.ControlId = nodeId)

                // Feature 100 (R5): the focused control's role + declared NavRange drive the
                // role-derived NavIntent classification in `Focus.route` (FR-001/FR-006).
                let role =
                    node.Control.Accessibility
                    |> Option.map (fun m -> m.Role)
                    |> Option.defaultValue AccessibilityRole.Custom

                let navRange = node.Control.Accessibility |> Option.bind (fun m -> m.Navigation)

                match Focus.route role keyboard navRange keyName isTab shift with
                | Activate ->
                    // The pointer-equivalent activation message(s) — the same click-equivalent
                    // bindings the pointer path dispatches — fired ONCE each (no double-dispatch).
                    let messages =
                        ownBindings
                        |> List.filter (fun b -> List.contains b.EventKind clickEquivalentKinds)
                        |> List.map (fun b ->
                            b.Dispatch
                                { Kind = b.EventKind
                                  ControlId = Some nodeId
                                  Origin = ControlEventOrigin.Keyboard
                                  Payload = None
                                  Nav = None })

                    retained, [], messages
                | Navigate intent ->
                    // FR-001/FR-006: dispatch through the uniform per-intent resolver (value step /
                    // selection move / grid move). The resolver reads the live value/selection/grid
                    // model and dual-sets `Payload` + the closed `Nav`; a boundary/empty/unset case
                    // is a designed no-op with no spurious dispatch (FR-009).
                    let messages = resolveNavIntent node nodeId ownBindings intent
                    retained, [], messages
                | Traverse move ->
                    let next = Focus.traverse order (Some nodeId) move
                    retained, [ FocusControl next ], []
                | Fallthrough -> retained, [], []

    /// Build a responds-proof verdict from a before/after frame pair (feature 090, FR-006):
    /// `Responsive` when the frames differ (a real input produced a visible change), `Inert` when
    /// identical. The reusable core the pointer and text responds-proof captures share.
    let respondsProofOf (before: Scene) (after: Scene) : RespondsProof =
        { Before = before
          After = after
          Verdict = (if before <> after then Responsive else Inert) }

    /// Capture an input→visible-change responds-proof for a pointer interaction on the running host
    /// (feature 090, FR-006/FR-007): render the BEFORE frame, route the interaction through the real
    /// `routeInteractivePointer` adapter path, fold the produced messages with `host.Update`, render
    /// the AFTER frame, and emit both frames + a verdict. A host whose live window is inert (an
    /// authored binding dropped, so the route produces no message) yields identical frames and an
    /// `Inert` verdict — it cannot be passed off as a responds-proof. Reuses the production render
    /// path (`Control.renderTree`); no live Vulkan window required (render-only capture).
    let captureRespondsProof
        (host: InteractiveAppHost<'model, 'msg>)
        (state: PointerState)
        (size: Size)
        (model: 'model)
        (input: ViewerPointerInput)
        : RespondsProof =
        let before = (Control.renderTree host.Theme size (host.View size model)).Scene
        let _, messages = routeInteractivePointer host state size model input
        let model' = messages |> List.fold (fun current msg -> fst (host.Update msg current)) model
        let after = (Control.renderTree host.Theme size (host.View size model')).Scene
        respondsProofOf before after

    // Map a native key (key-down) to the text-seam message it inserts. Only printable keys produce
    // text; editing keys (Backspace, arrows, …) are E4 scope (FR-008a) and fall through to MapKey.
    let textMsgOfKey (key: ViewerKey) : TextInputMsg option =
        match key with
        | Letter c -> Some(InsertText(string c))
        | Digit n -> Some(InsertText(string n))
        | Space -> Some(InsertText " ")
        | _ -> None

    // Feature 122 (FR-005): the shared interactive-host body, parameterized by the terminal viewer
    // launcher so `runInteractiveApp` (default windowed-fullscreen) and
    // `runInteractiveAppWithWindowBehavior` (explicit window behavior) reuse the EXACT same
    // message→update→retained-step + clock/visual-state/pointer wiring — no parallel logic.
    let runInteractiveAppWithLauncher
        (launch: ViewerOptions -> InteractiveViewerHost<'model, 'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>)
        (options: ViewerOptions)
        (host: InteractiveAppHost<'model, 'msg>)
        =
        // Durable pointer coordination state (hover/press/4px-fold), threaded across samples.
        let pointerState = ref (Pointer.init ())
        // Feature 092 (E2): focus is now keyed by the STABLE `RetainedId` (was `ControlId`), and the
        // focused control's `TextInput` state lives in `RetainedRender.StateByIdentity[id].Text` — no
        // parallel `ControlId`-keyed text-model map. Because `step` carries `StateByIdentity` to the
        // matched identity across the diff, focus + in-progress text + the per-control animation clock
        // survive an unrelated re-render even when the control's position shifts (FR-001/2/3). This is
        // the half 091 left unwired: 091 carried the map but the host never read/wrote it.
        // Feature 094 (E4): generalized from the 092 text-only `focusedText` to the host's single
        // focus identity (still a stable `RetainedId`). The E1 text seam, `routeFocusedKey`
        // activation/navigation, and Tab-traversal all read/write this one ref.
        let focused = ref (None: RetainedId option)
        // The retained render structure (the wired keyed reconciler, 067), the single home of
        // per-control UI state. Mutation is confined to this closure at the interpreter edge
        // (constitution III); the consumer `view` stays pure.
        let retained = ref (None: RetainedRender<'msg> option)
        // Feature 110 (FR-002): the most recent retained frame's `ControlRenderResult`
        // (`EventBindings`/`BoundIds`/`Bounds`), retained so pointer routing reads the frame's bindings
        // WITHOUT a fresh `Control.renderTree`. Seeded from `r0.Render` on the first paint and updated to
        // `s.Render` each step; only `s.Render.Scene` was consumed before — `s.Render` itself was dropped.
        let lastRender = ref (None: ControlRenderResult<'msg> option)
        // Feature 111 (FR-003): the un-stamped `host.View size model` output for the current `(model,
        // size)`, cached so a model-unchanged repaint (a host-owned hover/focus/animation change) reuses
        // it and SKIPS `host.View`. `host.View` is pure in `(model, size)`, so the reused tree equals a
        // fresh call — the full-tree visual-state stamp + `step` still run, only the view call is skipped
        // (FR-009, byte-identical). Keyed by reference identity of the model: a reference-type model that
        // did not change is the same instance (reuse); a value-type model is never `ReferenceEquals` so it
        // re-views (a safe, byte-identical fallback — the deterministic view-skip surface is `Perf.runScript`).
        let lastView = ref (None: (Size * 'model * Control<'msg>) option) // mutable: hot path / per frame
        // Feature 112 (FR-001/FR-002): the previous frame's runtime model, so a model-unchanged repaint
        // can compute which identities left a hover/focus/press state and re-stamp only those (the
        // targeted stamp). Updated each paint; `None` until the first stamp.
        let lastRuntimeModel = ref (None: ControlRuntimeModel option) // mutable: hot path / per frame
        // Diff/first-frame diagnostics (e.g. KeyCollision from duplicate sibling keys) surfaced
        // through the host's diagnostics stderr channel — never silently dropped; de-duped so a
        // standing collision is reported once, not every frame. The path stays total in their presence.
        let surfacedDiagnostics = ref (Set.empty: Set<string>)

        // Feature 108 (US4, FR-011/012): the per-frame pointer-move coalescing accumulator. A native
        // MOVE sample (hover/drag) is buffered here (latest wins) and processed at the NEXT sample
        // boundary, collapsing a burst of moves to at most one PROCESSED move (one render + hit-test)
        // while discrete interactions are never coalesced or dropped. Mutation is confined to this
        // interpreter closure (constitution III); the consumer `view`/`update` stay pure.
        let pendingMove = ref (None: ViewerPointerInput option) // mutable: hot path / per frame
        let pointerSampleCount = ref 0 // mutable: hot path / per frame
        // Feature 108 (US2, FR-006): the most recent retained-step work record, so `OnFrameMetrics`
        // can report the frame's `RemeasuredNodeCount` (the live observability sink; the byte-stable
        // determinism surface is `Perf.runScript`).
        let lastWorkReduction = ref (None: WorkReductionRecord option)
        // Feature 120 (US1): the most recent backend present timing (paint-walk, flush+swap) captured by the
        // OpenGL host and reported as live-only, non-golden `FrameMetrics.PaintDuration`/`ComposeDuration`.
        let lastPresentTiming = ref (TimeSpan.Zero, TimeSpan.Zero) // mutable: hot path / per frame

        let surface (diags: ControlDiagnostic list) =
            for d in diags do
                let key = sprintf "%A|%A|%s" d.Code d.ControlId d.Message

                if not (Set.contains key surfacedDiagnostics.Value) then
                    surfacedDiagnostics.Value <- Set.add key surfacedDiagnostics.Value
                    eprintfn "[ControlDiagnostic %A] %s" d.Severity d.Message

        // Feature 096 (R1): assemble a READ-ONLY `ControlRuntimeModel` from the host's live pointer +
        // focus state so `ControlRuntime.applyRuntimeVisualState` can stamp the derived VisualState onto
        // the freshly-produced tree BEFORE the reconciler diffs it (pre-reconcile, in the `ControlId`
        // domain). Hover/press are already `ControlId`-keyed on `pointerState`; `focused` is a stable
        // `RetainedId` resolved back to its `ControlId` via the PRIOR retained tree. On the first frame
        // there is no prior tree, so `focused` resolves to `None` (research §D5) and focus indication
        // begins only once focus is established by post-render interaction. Selection is a consumer
        // (text-range) concern — the host derives none, so the bridge fills only the runtime tail.
        let assembleRuntimeModel (prior: RetainedRender<'msg> option) : ControlRuntimeModel =
            let focusedControlId =
                match focused.Value, prior with
                | Some rid, Some r ->
                    tryFindNode rid r.Root
                    |> Option.map (fun node -> node.Control.Key |> Option.defaultValue node.Control.Kind)
                | _ -> None

            { fst (ControlRuntime.init ()) with
                HoveredControl = pointerState.Value.Hover
                PressedControls =
                    pointerState.Value.Presses
                    |> Map.toList
                    |> List.map (fun (_, candidate) -> candidate.Control)
                    |> Set.ofList
                FocusedControl = focusedControlId }

        // Produce the production scene for (size, model) through the retained reconciler. The first
        // frame seeds the retained structure and paints ONCE (FR-009 — no second `Control.renderTree`,
        // first-frame collisions surfaced immediately); later frames diff + reuse. Output is
        // byte-identical to a full rebuild (FR-005, proven by the wired round-trip property suite).
        // Feature 096 (R1): the runtime visual-state bridge is applied to `host.View size model` before
        // `init`/`step`, so a hover/press/focus change becomes a scoped reconciler `Update` patch on
        // exactly that subtree, and a `Normal`-and-unset tree stamps nothing (byte-identity at rest).
        // Feature 111 (FR-003): the un-stamped consumer view for `(size, model)`, REUSING the cache when
        // the model instance and size are unchanged so `host.View` is skipped on a host-owned
        // hover/focus/animation repaint. The runtime visual-state stamp is applied to the reused tree by
        // the caller (it always runs — FR-009), so output is byte-identical.
        let viewFor (size: Size) (model: 'model) : Control<'msg> =
            match lastView.Value with
            | Some(cachedSize, cachedModel, cachedView) when cachedSize = size && obj.ReferenceEquals(model, cachedModel) ->
                cachedView
            | _ ->
                let v = host.View size model
                lastView.Value <- Some(size, model, v)
                v

        let renderRetained (size: Size) (model: 'model) : Scene =
            match retained.Value with
            | None ->
                let runtimeModel = assembleRuntimeModel None
                // First frame: no prior stamped tree to narrow against → full-tree oracle (FR-006).
                let stamp = ControlRuntime.runtimeStampFor None runtimeModel (viewFor size model)
                lastRuntimeModel.Value <- Some runtimeModel
                let r0 = RetainedRender.init host.Theme size stamp.Stamped
                surface r0.Diagnostics
                retained.Value <- Some r0.Retained
                lastRender.Value <- Some r0.Render
                r0.Render.Scene
            | Some prev ->
                let runtimeModel = assembleRuntimeModel (Some prev)
                // Feature 112 (FR-001/FR-002): on a model-unchanged repaint (the view cache would hit),
                // narrow the runtime-state stamp to only the changed identities via the TARGETED stamp,
                // reusing `prev.Root.Control` (the previous stamped tree). On a model-changing frame the
                // whole view is rebuilt anyway, so use the full-tree oracle (`prior = None`).
                let modelUnchanged =
                    match lastView.Value with
                    | Some(cachedSize, cachedModel, _) -> cachedSize = size && obj.ReferenceEquals(model, cachedModel)
                    | None -> false

                let fresh = viewFor size model

                let prior =
                    if modelUnchanged then
                        lastRuntimeModel.Value |> Option.map (fun pm -> pm, prev.Root.Control)
                    else
                        None

                let stamp = ControlRuntime.runtimeStampFor prior runtimeModel fresh
                lastRuntimeModel.Value <- Some runtimeModel
                let s = RetainedRender.step host.Theme size prev stamp.Stamped
                surface s.Diagnostics
                lastWorkReduction.Value <- Some s.WorkReduction
                retained.Value <- Some s.Retained
                lastRender.Value <- Some s.Render
                s.Render.Scene

        // A focused node is a TEXT control (the E1 seam owns its printable keys); every other
        // focusable kind routes through `routeFocusedKey`.
        let isTextNode (node: RetainedNode<'msg>) : bool =
            (match node.Control.Accessibility with
             | Some m -> m.Role = AccessibilityRole.TextBox
             | None -> false)
            || List.contains node.Control.Kind [ "text-box"; "text-area"; "numeric-input" ]

        // FR-006: a press sets focus to the focusable control under it (its accessibility metadata
        // declares `Focusable = true`). Resolve a `FocusControl next` ControlId back to a stable
        // `RetainedId` so traversal keeps tracking the moved focus across frames.
        let retainedIdOfControl (r: RetainedRender<'msg>) (controlId: ControlId) : RetainedId option =
            let rec find (n: RetainedNode<'msg>) =
                let nId = n.Control.Key |> Option.defaultValue n.Control.Kind

                if nId = controlId then
                    Some n.Identity
                else
                    n.Children |> List.tryPick find

            find r.Root

        // Feature 108/109/110 (US1, FR-001/007): emit one `OnFrameMetrics` for a processed pointer
        // frame. This is the live, BEST-EFFORT observability sink (the authoritative byte-stable surface
        // is `Perf.runScript`): `productModelChanged` is the proxy "a product message was produced this
        // frame" available at this seam (the viewer applies the fold downstream). Feature 110: routing
        // now reads the RETAINED frame, so the only full render `processInput` can perform is the counted
        // oracle FALLBACK — `fullRenderFallbackCount` IS the frame's routing full-render count, so it is
        // both `FullRenderCount` and `FullRenderFallbackCount` here (the model-driven repaint is the
        // viewer's separate paint cycle, not this sink). `duration` is the real wall-clock of that work
        // (FR-012, EXCLUDED from goldens — the golden surface reports 0).
        // Feature 111: the live sink reports the INPUT-side phases this seam performs — the only `host.View`
        // it can run is a counted oracle fallback (so `ViewCalled`/`DiffRan` track that), and the
        // model-driven repaint is the viewer's SEPARATE `renderRetained` cycle (not observed here), so
        // `PaintRan`/`LayoutRan` stay `false`. The authoritative, full per-phase record is `Perf.runScript`.
        let emitFrameMetrics (cause: FrameCause) (samples: int) (movesProcessed: int) (productModelChanged: bool) (fullRenderFallbackCount: int) (duration: TimeSpan) =
            host.OnFrameMetrics
                { ProductModelChanged = productModelChanged
                  ViewCalled = fullRenderFallbackCount > 0
                  FullRenderCount = fullRenderFallbackCount
                  RemeasuredNodeCount = lastWorkReduction.Value |> Option.map (fun w -> w.RemeasuredNodeCount) |> Option.defaultValue 0
                  // Feature 113 (Phase 5): the last retained-step's memo tally (live `OnFrameMetrics` sink).
                  MemoHitCount = lastWorkReduction.Value |> Option.map (fun w -> w.MemoHits) |> Option.defaultValue 0
                  MemoMissCount = lastWorkReduction.Value |> Option.map (fun w -> w.MemoMisses) |> Option.defaultValue 0
                  // Feature 114 (Phase 6): the last retained-step's virtualization tally (live sink).
                  VirtualItemsMaterialized = lastWorkReduction.Value |> Option.map (fun w -> w.VirtualMaterialized) |> Option.defaultValue 0
                  VirtualItemsTotal = lastWorkReduction.Value |> Option.map (fun w -> w.VirtualTotal) |> Option.defaultValue 0
                  // Feature 116 (Phase 7): the last retained-step's damage + picture-cache tallies (live sink).
                  RepaintedNodeCount = lastWorkReduction.Value |> Option.map (fun w -> w.RepaintedNodeCount) |> Option.defaultValue 0
                  DirtyRectCount = lastWorkReduction.Value |> Option.map (fun w -> w.DirtyRectCount) |> Option.defaultValue 0
                  DirtyArea = lastWorkReduction.Value |> Option.map (fun w -> w.DirtyArea) |> Option.defaultValue 0
                  PictureCacheHitCount = lastWorkReduction.Value |> Option.map (fun w -> w.PictureCacheHits) |> Option.defaultValue 0
                  PictureCacheMissCount = lastWorkReduction.Value |> Option.map (fun w -> w.PictureCacheMisses) |> Option.defaultValue 0
                  PictureCacheEntryCount = lastWorkReduction.Value |> Option.map (fun w -> w.PictureCacheEntryCount) |> Option.defaultValue 0
                  // Feature 117 (Phase 8): the last retained-step's text-cache tally + dirty-set size (live sink).
                  TextMeasureCacheHitCount = lastWorkReduction.Value |> Option.map (fun w -> w.TextMeasureCacheHits) |> Option.defaultValue 0
                  TextMeasureCacheMissCount = lastWorkReduction.Value |> Option.map (fun w -> w.TextMeasureCacheMisses) |> Option.defaultValue 0
                  LayoutInvalidatedNodeCount = lastWorkReduction.Value |> Option.map (fun w -> w.LayoutInvalidatedNodeCount) |> Option.defaultValue 0
                  PointerSamplesReceived = samples
                  PointerMovesProcessed = movesProcessed
                  FullRenderFallbackCount = fullRenderFallbackCount
                  FrameCause = cause
                  DiffRan = fullRenderFallbackCount > 0
                  LayoutRan = false
                  PaintRan = false
                  FrameDuration = duration
                  // Feature 120 (US1): live backend present timing (non-golden), read from the OpenGL host's
                  // last present (one-frame lag, live diagnostic only); (US3) replay model counts.
                  PaintDuration = (lastPresentTiming.Value <- FS.GG.UI.SkiaViewer.Host.GlHost.lastPresentTiming(); fst lastPresentTiming.Value)
                  ComposeDuration = snd lastPresentTiming.Value
                  ReplayHitCount = lastWorkReduction.Value |> Option.map (fun w -> w.ReplayHits) |> Option.defaultValue 0
                  ReplayMissCount = lastWorkReduction.Value |> Option.map (fun w -> w.ReplayMisses) |> Option.defaultValue 0
                  ReplayRecordCount = lastWorkReduction.Value |> Option.map (fun w -> w.ReplayRecords) |> Option.defaultValue 0
                  ReplaySkippedNodeCount = lastWorkReduction.Value |> Option.map (fun w -> w.ReplaySkippedNodes) |> Option.defaultValue 0
                  ReplayCacheNativeBytes = lastWorkReduction.Value |> Option.map (fun w -> w.ReplayCacheNativeBytes) |> Option.defaultValue 0 }

        // The single pointer-routing step (the pre-108 `mapPointer` body): focus-on-click + the feature-
        // 110 RETAINED route (`routeRetainedPointer`) — no per-sample `host.View` + `Control.renderTree`.
        // Returns the product messages and the route's `FullRenderFallbackCount` (0 on the normal path;
        // each unresolvable bindable hit runs one preserved oracle render and counts +1). Shared by the
        // discrete and coalesced-move paths.
        let processInput (input: ViewerPointerInput) (size: Size) (model: 'model) : 'msg list * int =
            // Focus-on-click (FR-004/FR-006): a press resolves to the `RetainedId` under the point via
            // the retained tree's per-node boxes (distinguishing unkeyed same-kind siblings); if that
            // control is FOCUSABLE (per its accessibility metadata) it becomes the focus target, so a
            // later key reaches it through the text seam or `routeFocusedKey`. A press on a
            // non-focusable region leaves the current focus UNCHANGED (it is not silently cleared).
            (match input.Phase, retained.Value with
             | ViewerPointerPhaseKind.Pressed, Some r ->
                 match resolveFocus r input.X input.Y with
                 | Some id ->
                     match tryFindNode id r.Root with
                     | Some node when
                         node.Control.Accessibility
                         |> Option.exists (fun m -> m.Keyboard.Focusable)
                         -> focused.Value <- Some id
                     | _ -> ()
                 | None -> ()
             | _ -> ())

            match retained.Value, lastRender.Value with
            | Some r, Some render ->
                let state', messages, fallbacks = routeRetainedPointer host r render pointerState.Value size model input
                pointerState.Value <- state'
                messages, fallbacks
            | _ ->
                // No retained frame yet (a pointer sample before the first paint seeded the frame, not
                // expected in the live loop where paint precedes input): fall back to the preserved
                // oracle so routing is still correct, counting the full render it performs.
                let state', messages = routeInteractivePointer host pointerState.Value size model input
                pointerState.Value <- state'
                messages, 1

        // Feature 108 (US4, FR-011/012): pointer-move coalescing on the live loop. A MOVE sample is
        // buffered (latest position wins) and the PREVIOUSLY-buffered move is processed at the next
        // sample boundary — so a burst of K moves yields at most one processed move (one render +
        // hit-test) per boundary, while every discrete interaction (press/release/click/drag
        // begin/end/cancel/scroll/secondary) is processed in arrival order, never coalesced or
        // dropped. The authoritative, byte-stable coalescing surface is `Perf.runScript`; here the
        // identical predicate drives the live loop and feeds best-effort `OnFrameMetrics`.
        let mapPointer (input: ViewerPointerInput) (size: Size) (model: 'model) : 'msg list =
            pointerSampleCount.Value <- pointerSampleCount.Value + 1

            match input.Phase with
            | ViewerPointerPhaseKind.Moved ->
                // Process the previously-deferred move (≤1 per boundary), then defer this one. Feature
                // 110: a flushed move routes from the retained frame and performs ZERO routing renders
                // (FullRenderCount = 0) unless it must fall back; the first move of a burst defers
                // without processing (no emit).
                let sw = System.Diagnostics.Stopwatch.StartNew()

                let flushedMsgs, flushedFallbacks =
                    match pendingMove.Value with
                    | Some prev ->
                        pendingMove.Value <- None
                        processInput prev size model
                    | None -> [], 0

                sw.Stop()
                pendingMove.Value <- Some input

                // This Moved sample carries into the next frame's count; report the flushed move now.
                let samples = pointerSampleCount.Value - 1
                pointerSampleCount.Value <- 1

                if samples > 0 then
                    emitFrameMetrics FrameCause.PointerMove samples 1 (not (List.isEmpty flushedMsgs)) flushedFallbacks sw.Elapsed

                flushedMsgs
            | _ ->
                // Discrete interaction: flush any pending move first (arrival order preserved), then
                // process the discrete event in the same frame it arrived (a click is never dropped).
                // Feature 110: routing performs ZERO full renders; the frame's full-render count is the
                // summed oracle-fallback count (normally 0).
                let sw = System.Diagnostics.Stopwatch.StartNew()

                let moveFlushed, (moveMsgs, moveFallbacks) =
                    match pendingMove.Value with
                    | Some prev ->
                        pendingMove.Value <- None
                        true, processInput prev size model
                    | None -> false, ([], 0)

                let discreteMsgs, discreteFallbacks = processInput input size model
                sw.Stop()
                let samples = pointerSampleCount.Value
                pointerSampleCount.Value <- 0
                let msgs = moveMsgs @ discreteMsgs
                let fallbackCount = moveFallbacks + discreteFallbacks
                emitFrameMetrics FrameCause.PointerDiscrete samples (if moveFlushed then 1 else 0) (not (List.isEmpty msgs)) fallbackCount sw.Elapsed
                msgs

        // Feature 108 (US5, FR-016): the unconsumed-key fallthrough. The modifier-aware `MapKeyChord`
        // seam is consulted BEFORE `MapKey`: a chord like `Ctrl+L` survives the backend as
        // `ViewerKey.Unknown "Ctrl+L"`, so its modifiers are recovered here via
        // `normalizeEventWithModifiers` and offered to `MapKeyChord`. The default `MapKeyChord`
        // returns `None`, so an unmodified key routes through `MapKey` exactly as before (SC-012).
        let chordFallthrough (key: ViewerKey) : 'msg list =
            let baseKey, mods =
                match key with
                | ViewerKey.Unknown raw ->
                    let bk, _, m =
                        ViewerKeyboard.normalizeEventWithModifiers { RawKey = raw; Direction = ViewerKeyDirection.KeyDown }

                    bk, m
                | _ -> key, ViewerKeyboard.noModifiers

            match host.MapKeyChord baseKey mods with
            | Some msg -> [ msg ]
            | None -> host.MapKey key true |> Option.toList

        let mapKey (key: ViewerKey) (pressed: bool) : 'msg list =
            // Feature 094 (E4) focus-first key routing. Only key-down (`pressed`) is routed; key-up
            // falls straight through. Precedence (R3): (1) E1 text seam for a focused TEXT control's
            // printable keys, (2) `routeFocusedKey` for activation / navigation / Tab-traversal,
            // (3) `MapKeyChord`/`host.MapKey` for anything no focused control and no traversal consumed.
            if not pressed then
                host.MapKey key false |> Option.toList
            else
                match retained.Value with
                | None -> chordFallthrough key
                | Some r ->
                    let focusedNode = focused.Value |> Option.bind (fun id -> tryFindNode id r.Root)

                    // (1) E1 text seam — unchanged delivery for a focused text control's printable keys.
                    let textHandled =
                        match textMsgOfKey key, focused.Value, focusedNode with
                        | Some textMsg, Some id, Some node when isTextNode node ->
                            let r', msgs = routeFocusedText r (Some id) textMsg
                            retained.Value <- Some r'
                            Some msgs
                        | _ -> None

                    match textHandled with
                    | Some msgs -> msgs
                    | None ->
                        // (2) routeFocusedKey — the tab order is derived from the retained tree's
                        // root control (the lowered view), so no model/size is needed here.
                        let order = Focus.order r.Root.Control

                        let shift =
                            match key with
                            | ViewerKey.Unknown raw -> raw.StartsWith("Shift+", StringComparison.OrdinalIgnoreCase)
                            | _ -> false

                        let r', controlMsgs, productMsgs = routeFocusedKey r focused.Value order key shift
                        retained.Value <- Some r'

                        // Apply focus-update messages to the host's focus identity (map the next
                        // ControlId back to its stable RetainedId).
                        for cm in controlMsgs do
                            match cm with
                            | FocusControl next ->
                                focused.Value <- next |> Option.bind (retainedIdOfControl r')
                            | _ -> ()

                        // (3) Fall through to the chord/`host.MapKey` seam only when nothing was consumed.
                        match productMsgs, controlMsgs with
                        | [], [] -> chordFallthrough key
                        | _ -> productMsgs

        // Feature 099 (R4): the host animation seam (contract C1). Each frame the viewer hands us the
        // injected per-frame `delta`; we ADVANCE every live per-identity clock in
        // `retained.Value.StateByIdentity` by it BEFORE the next `renderRetained` (which then paints
        // the already-advanced clocks and applies the stamped-VisualState retarget via
        // `RetainedRender.step`). The advance is the ONLY writer of the carried clock from the host
        // loop — a pure function of the injected delta (no `Date.now`/wall-clock). We then DELEGATE to
        // `host.Tick delta` so the consumer's own tick message is unaffected (no swallow, no
        // double-dispatch). When no identity has an active clock this is observably a pass-through.
        let wrappedTick (delta: TimeSpan) : 'msg option =
            // Feature 121 (US2, FR-004): advance per-identity clocks only when at least one is active.
            // `advanceStateClocks` returns the map reference-equal when nothing is animating, so we skip
            // even the record copy — an idle live tick allocates nothing (the prior `Map.map` made a
            // fresh map every tick). Active clocks advance exactly as before (features 099/103 unchanged).
            match retained.Value with
            | Some r ->
                let advanced = RetainedRender.advanceStateClocks delta r.StateByIdentity

                if not (obj.ReferenceEquals(advanced, r.StateByIdentity)) then
                    retained.Value <- Some { r with StateByIdentity = advanced }
            | None -> ()

            host.Tick delta

        let viewerHost: InteractiveViewerHost<'model, 'msg> =
            { Init = host.Init
              Update = host.Update
              View = fun size model -> SceneNode.Group [ renderRetained size model ]
              MapKey = mapKey
              MapPointer = mapPointer
              Tick = wrappedTick
              Diagnostics = host.Diagnostics }

        launch options viewerHost

    let runInteractiveApp (options: ViewerOptions) (host: InteractiveAppHost<'model, 'msg>) =
        runInteractiveAppWithLauncher Viewer.runInteractiveViewer options host

    /// Feature 122 (FR-003/005): as `runInteractiveApp` with an explicit window behavior threaded into
    /// the live launch (startup-state / resize / maximize / position / backend), so a generated app's
    /// `--window-startup normal` actually applies to the controls window instead of only the options
    /// report. `runInteractiveApp` is unchanged (default windowed-fullscreen).
    let runInteractiveAppWithWindowBehavior
        (options: ViewerOptions)
        (behavior: ViewerWindowBehaviorRequest)
        (host: InteractiveAppHost<'model, 'msg>)
        =
        runInteractiveAppWithLauncher
            (fun launchOptions viewerHost ->
                Viewer.runInteractiveViewerWithWindowBehavior launchOptions behavior viewerHost)
            options
            host

    // Feature 108 (US3, FR-009/010): the pure, headless, deterministic frame driver. Nested in
    // `ControlsElmish` so it reuses the SAME message→update→retained-step + binding-resolution +
    // coalescing primitives the live `runInteractiveApp` loop uses (no parallel logic).
    module Perf =
        // FR-011: the move predicate shared with the live loop's coalescing — hover/drag moves
        // collapse; every other interaction is discrete.
        let private isMoveInteraction (interaction: PointerInteraction) : bool =
            match interaction with
            | HoverEnter _
            | HoverLeave _
            | DragMove _ -> true
            | _ -> false

        // Group the script into frames: consecutive pointer-MOVE inputs coalesce into ONE frame;
        // every other input (key, discrete pointer, tick, idle) is its own frame. Order preserved.
        let private toFrames (script: FrameInput<'msg> list) : FrameInput<'msg> list list =
            let frames = System.Collections.Generic.List<FrameInput<'msg> list>()
            let current = System.Collections.Generic.List<FrameInput<'msg>>()

            let flush () =
                if current.Count > 0 then
                    frames.Add(List.ofSeq current)
                    current.Clear()

            for input in script do
                match input with
                | FrameInput.Pointer interaction when isMoveInteraction interaction -> current.Add input
                | other ->
                    flush ()
                    frames.Add [ other ]

            flush ()
            List.ofSeq frames

        let runScript
            (host: InteractiveAppHost<'model, 'msg>)
            (size: Size)
            (script: FrameInput<'msg> list)
            : FrameMetrics list =
            let mutable model = fst (host.Init())
            let mutable retained: RetainedRender<'msg> option = None
            // Feature 110 (FR-002): carry the retained frame's `ControlRenderResult` alongside the
            // threaded retained value, so a routed interaction reads `EventBindings`/`BoundIds` without a
            // fresh render. Kept in lock-step with `retained` by `renderStep`/`ensureRetained`.
            let mutable lastRender: ControlRenderResult<'msg> option = None
            // Feature 113 (Phase 5): the last retained-step's memo tally (hits, misses), captured by
            // `renderStep`/`repaintCached` so each per-frame `FrameMetrics` reports it. The first frame
            // seeds via `init` (no work record), so it stays (0, 0) until a `step` runs.
            let mutable lastMemo: int * int = 0, 0
            // Feature 114 (Phase 6): the last retained-step's virtualization tally (materialized, total),
            // captured by `renderStep`/`repaintCached` so each per-frame `FrameMetrics` reports it. Stays
            // (0, 0) until a `step` runs (the first frame seeds via `init`, which has no work record).
            let mutable lastVirtual: int * int = 0, 0
            // Feature 116 (Phase 7): the last retained-step's damage tally (repainted-node, dirty-rect,
            // dirty-area) and picture-cache tally (hits, misses, entry-count), captured by
            // `renderStep`/`repaintCached`. Stay zero until a `step` runs.
            let mutable lastDamage: int * int * int = 0, 0, 0
            let mutable lastPicture: int * int * int = 0, 0, 0
            // Feature 120 (US3): replay hits, misses, records, skipped-nodes, native-bytes (deterministic model).
            let mutable lastReplay: int * int * int * int * int = 0, 0, 0, 0, 0
            // Feature 117 (Phase 8): the last retained-step's text-cache tally (hits, misses) and the
            // layout dirty-set size, captured by `renderStep`/`repaintCached`. Stay zero until a `step`
            // runs (the first frame seeds via `init`, which has no work record).
            let mutable lastTextCache: int * int = 0, 0
            let mutable lastInvalidated: int = 0

            // Render the retained step for the current model, returning the frame's
            // RemeasuredNodeCount (the first frame seeds via `init`, which has no work record -> 0).
            let renderStep () : int =
                let next = host.View size model

                match retained with
                | None ->
                    let r0 = RetainedRender.init host.Theme size next
                    retained <- Some r0.Retained
                    lastRender <- Some r0.Render
                    lastMemo <- 0, 0
                    lastVirtual <- 0, 0
                    0
                | Some prev ->
                    let s = RetainedRender.step host.Theme size prev next
                    retained <- Some s.Retained
                    lastRender <- Some s.Render
                    lastMemo <- s.WorkReduction.MemoHits, s.WorkReduction.MemoMisses
                    lastVirtual <- s.WorkReduction.VirtualMaterialized, s.WorkReduction.VirtualTotal
                    lastDamage <- s.WorkReduction.RepaintedNodeCount, s.WorkReduction.DirtyRectCount, s.WorkReduction.DirtyArea
                    lastPicture <- s.WorkReduction.PictureCacheHits, s.WorkReduction.PictureCacheMisses, s.WorkReduction.PictureCacheEntryCount
                    lastReplay <- s.WorkReduction.ReplayHits, s.WorkReduction.ReplayMisses, s.WorkReduction.ReplayRecords, s.WorkReduction.ReplaySkippedNodes, s.WorkReduction.ReplayCacheNativeBytes
                    lastTextCache <- s.WorkReduction.TextMeasureCacheHits, s.WorkReduction.TextMeasureCacheMisses
                    lastInvalidated <- s.WorkReduction.LayoutInvalidatedNodeCount
                    s.WorkReduction.RemeasuredNodeCount

            // Feature 111 (FR-003/FR-004): re-sample the overlay for an animation-only tick WITHOUT
            // calling `host.View`. The model is unchanged, so `host.View size model` would return a tree
            // structurally equal to `prev.Root.Control` (the tree the previous view produced for this same
            // model) — stepping `prev` against it yields the byte-identical all-`Keep` diff + overlay. The
            // VIEW phase is skipped (`ViewCalled = false`, `FullRenderCount` unchanged) while the PAINT
            // phase runs. Returns the frame's RemeasuredNodeCount.
            let repaintCached () : int =
                match retained with
                | Some prev ->
                    let s = RetainedRender.step host.Theme size prev prev.Root.Control
                    retained <- Some s.Retained
                    lastRender <- Some s.Render
                    lastMemo <- s.WorkReduction.MemoHits, s.WorkReduction.MemoMisses
                    lastVirtual <- s.WorkReduction.VirtualMaterialized, s.WorkReduction.VirtualTotal
                    lastDamage <- s.WorkReduction.RepaintedNodeCount, s.WorkReduction.DirtyRectCount, s.WorkReduction.DirtyArea
                    lastPicture <- s.WorkReduction.PictureCacheHits, s.WorkReduction.PictureCacheMisses, s.WorkReduction.PictureCacheEntryCount
                    lastReplay <- s.WorkReduction.ReplayHits, s.WorkReduction.ReplayMisses, s.WorkReduction.ReplayRecords, s.WorkReduction.ReplaySkippedNodes, s.WorkReduction.ReplayCacheNativeBytes
                    lastTextCache <- s.WorkReduction.TextMeasureCacheHits, s.WorkReduction.TextMeasureCacheMisses
                    lastInvalidated <- s.WorkReduction.LayoutInvalidatedNodeCount
                    s.WorkReduction.RemeasuredNodeCount
                | None -> 0

            // Feature 110: seed the retained frame for the CURRENT model if none exists yet — the
            // "initial render" precondition the retained route needs to hit-test a `Click`. This is NOT a
            // routing full render (it is the initial frame the live host paints before any input), so it
            // is uncounted; a move/hover interaction never needs it (it resolves to `MapPointer` with no
            // hit-test), so the move-only corpus scenarios seed nothing and route with zero full renders.
            let ensureRetained () =
                match retained with
                | Some _ -> ()
                | None ->
                    let next = host.View size model
                    let r0 = RetainedRender.init host.Theme size next
                    retained <- Some r0.Retained
                    lastRender <- Some r0.Render

            let applyMessages (msgs: 'msg list) =
                model <- msgs |> List.fold (fun acc msg -> fst (host.Update msg acc)) model

            // Feature 110: route a single pointer interaction to product messages from the RETAINED
            // frame — NO `host.View` + `Control.renderTree` for routing. Returns (messages, fallback
            // count). A binding-eligible `Click` is resolved via `routeRetainedInteraction` (which seeds
            // the retained frame if needed and counts a real oracle fallback); every other interaction
            // resolves directly to `host.MapPointer` (the oracle's non-`Click` path) with no hit-test and
            // no render, so a hover/drag burst performs zero routing full renders.
            let routeInteraction (interaction: PointerInteraction) : 'msg list * int =
                match interaction with
                | Click _ ->
                    ensureRetained ()

                    match retained, lastRender with
                    | Some r, Some render -> routeRetainedInteraction host size model r render interaction
                    | _ -> interpretPointerEffect host.MapPointer interaction |> AdapterCmd.productMessages, 0
                | _ -> interpretPointerEffect host.MapPointer interaction |> AdapterCmd.productMessages, 0

            // Feature 109 (US1): did a product message actually change the model across the fold? An
            // empty message list never changes it; a non-empty list changed it iff the folded model's
            // reference identity moved (`obj.ReferenceEquals` — honest for both reference-type models,
            // where an idempotent `update` returns the same instance → `false`, and value-type models,
            // where the guard keeps a no-message frame `false`). No `'model` equality constraint added.
            let productModelChanged (before: 'model) (after: 'model) (msgs: 'msg list) : bool =
                not (List.isEmpty msgs) && not (obj.ReferenceEquals(before, after))

            let zero =
                { ProductModelChanged = false
                  ViewCalled = false
                  FullRenderCount = 0
                  RemeasuredNodeCount = 0
                  MemoHitCount = 0
                  MemoMissCount = 0
                  VirtualItemsMaterialized = 0
                  VirtualItemsTotal = 0
                  RepaintedNodeCount = 0
                  DirtyRectCount = 0
                  DirtyArea = 0
                  PictureCacheHitCount = 0
                  PictureCacheMissCount = 0
                  PictureCacheEntryCount = 0
                  TextMeasureCacheHitCount = 0
                  TextMeasureCacheMissCount = 0
                  LayoutInvalidatedNodeCount = 0
                  PointerSamplesReceived = 0
                  PointerMovesProcessed = 0
                  FullRenderFallbackCount = 0
                  FrameCause = FrameCause.Idle
                  DiffRan = false
                  LayoutRan = false
                  PaintRan = false
                  FrameDuration = TimeSpan.Zero
                  // Feature 120: timing is live-only (Zero on the deterministic path); replay counts come
                  // from the per-frame `lastReplay` model set by `renderStep`/`repaintCached`.
                  PaintDuration = TimeSpan.Zero
                  ComposeDuration = TimeSpan.Zero
                  ReplayHitCount = 0
                  ReplayMissCount = 0
                  ReplayRecordCount = 0
                  ReplaySkippedNodeCount = 0
                  ReplayCacheNativeBytes = 0 }

            toFrames script
            |> List.map (fun frame ->
                // Feature 113 (Phase 5): clear the per-frame memo tally before processing this frame, so a
                // frame that runs no render reports 0/0 (the previous frame's render must not bleed
                // through). `renderStep`/`repaintCached` overwrite it when they actually run.
                lastMemo <- 0, 0
                lastVirtual <- 0, 0
                lastDamage <- 0, 0, 0
                lastPicture <- 0, 0, 0
                lastReplay <- 0, 0, 0, 0, 0
                lastTextCache <- 0, 0
                lastInvalidated <- 0

                match frame with
                | FrameInput.Pointer _ :: _ when frame |> List.forall (function
                                                                       | FrameInput.Pointer p -> isMoveInteraction p
                                                                       | _ -> false) ->
                    // Coalesced move frame: K samples, ONE processed move (the latest). Feature 110: the
                    // move routes from the retained frame and performs ZERO routing full renders
                    // (`FullRenderCount` no longer counts a routing render); only a model-driven re-render
                    // (`hasMsgs`) or a counted oracle fallback adds to it.
                    let k = List.length frame
                    let before = model

                    let msgs, fallbacks =
                        match List.last frame with
                        | FrameInput.Pointer interaction -> routeInteraction interaction // 0 routing renders
                        | _ -> [], 0

                    let hasMsgs = not (List.isEmpty msgs)
                    applyMessages msgs
                    let remeasured = if hasMsgs then renderStep () else 0 // step render iff a message rebuilt
                    let fullRenderCount = fallbacks + (if hasMsgs then 1 else 0)

                    { zero with
                        ProductModelChanged = productModelChanged before model msgs
                        ViewCalled = fullRenderCount > 0
                        FullRenderCount = fullRenderCount
                        RemeasuredNodeCount = remeasured
                        MemoHitCount = fst lastMemo
                        MemoMissCount = snd lastMemo
                        VirtualItemsMaterialized = fst lastVirtual
                        VirtualItemsTotal = snd lastVirtual
                        RepaintedNodeCount = (let (r, _, _) = lastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = lastDamage in rc)
                        DirtyArea = (let (_, _, da) = lastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = lastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = lastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = lastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = lastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = lastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = lastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = lastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = lastReplay in b)
                        TextMeasureCacheHitCount = fst lastTextCache
                        TextMeasureCacheMissCount = snd lastTextCache
                        LayoutInvalidatedNodeCount = lastInvalidated
                        PointerSamplesReceived = k
                        PointerMovesProcessed = 1
                        FullRenderFallbackCount = fallbacks
                        FrameCause = FrameCause.PointerMove
                        DiffRan = hasMsgs
                        LayoutRan = remeasured > 0
                        PaintRan = hasMsgs }
                | [ FrameInput.Idle ] -> zero
                | [ FrameInput.Tick delta ] ->
                    // Advance every live clock by the injected delta. Feature 111 (FR-004): an
                    // animation-only tick (a live clock, no consumer message) is a PAINT-ONLY frame — it
                    // re-samples the overlay via `repaintCached` (stepping the unchanged retained tree) with
                    // NO `host.View`, so `ViewCalled = false`, `FullRenderCount = 0`, `PaintRan = true`
                    // (was `ViewCalled = true`/`FullRenderCount = 1` pre-111). A consumer `Tick` MESSAGE
                    // changes the model and rebuilds via `host.View` as a normal model frame.
                    let hadAnimation =
                        match retained with
                        | Some r -> r.StateByIdentity |> Map.exists (fun _ s -> s.Animation.IsSome)
                        | None -> false

                    match retained with
                    | Some r ->
                        retained <-
                            Some
                                { r with
                                    StateByIdentity =
                                        r.StateByIdentity
                                        |> Map.map (fun _ s ->
                                            { s with Animation = s.Animation |> Option.map (RetainedRender.advance delta) }) }
                    | None -> ()

                    let before = model
                    let tickMsgs = host.Tick delta |> Option.toList
                    let hasMsgs = not (List.isEmpty tickMsgs)
                    applyMessages tickMsgs

                    // hasMsgs -> model frame (host.View runs); animation-only -> view-free overlay; else nothing.
                    let viewRan, remeasured, paintRan =
                        if hasMsgs then true, renderStep (), true
                        elif hadAnimation then false, repaintCached (), true
                        else false, 0, false

                    { zero with
                        ProductModelChanged = productModelChanged before model tickMsgs
                        ViewCalled = viewRan
                        FullRenderCount = (if viewRan then 1 else 0)
                        RemeasuredNodeCount = remeasured
                        MemoHitCount = fst lastMemo
                        MemoMissCount = snd lastMemo
                        VirtualItemsMaterialized = fst lastVirtual
                        VirtualItemsTotal = snd lastVirtual
                        RepaintedNodeCount = (let (r, _, _) = lastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = lastDamage in rc)
                        DirtyArea = (let (_, _, da) = lastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = lastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = lastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = lastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = lastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = lastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = lastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = lastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = lastReplay in b)
                        TextMeasureCacheHitCount = fst lastTextCache
                        TextMeasureCacheMissCount = snd lastTextCache
                        LayoutInvalidatedNodeCount = lastInvalidated
                        FrameCause = FrameCause.Tick
                        DiffRan = viewRan
                        LayoutRan = remeasured > 0
                        PaintRan = paintRan }
                | [ FrameInput.Key(k, mods) ] ->
                    let before = model

                    let msgs =
                        match host.MapKeyChord k mods with
                        | Some m -> [ m ]
                        | None -> host.MapKey k true |> Option.toList

                    let hasMsgs = not (List.isEmpty msgs)
                    applyMessages msgs
                    let remeasured = if hasMsgs then renderStep () else 0
                    let fullRenderCount = if hasMsgs then 1 else 0

                    { zero with
                        ProductModelChanged = productModelChanged before model msgs
                        ViewCalled = fullRenderCount > 0
                        FullRenderCount = fullRenderCount
                        RemeasuredNodeCount = remeasured
                        MemoHitCount = fst lastMemo
                        MemoMissCount = snd lastMemo
                        VirtualItemsMaterialized = fst lastVirtual
                        VirtualItemsTotal = snd lastVirtual
                        RepaintedNodeCount = (let (r, _, _) = lastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = lastDamage in rc)
                        DirtyArea = (let (_, _, da) = lastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = lastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = lastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = lastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = lastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = lastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = lastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = lastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = lastReplay in b)
                        TextMeasureCacheHitCount = fst lastTextCache
                        TextMeasureCacheMissCount = snd lastTextCache
                        LayoutInvalidatedNodeCount = lastInvalidated
                        FrameCause = FrameCause.Key
                        DiffRan = hasMsgs
                        LayoutRan = remeasured > 0
                        PaintRan = hasMsgs }
                | [ FrameInput.Pointer interaction ] ->
                    // A discrete pointer interaction: one sample, never a coalesced move. Feature 110:
                    // routing resolves from the retained frame and performs ZERO routing full renders;
                    // `FullRenderCount` counts only a model-driven re-render (`hasMsgs`) and any counted
                    // oracle fallback.
                    let before = model
                    let msgs, fallbacks = routeInteraction interaction // 0 routing renders
                    let hasMsgs = not (List.isEmpty msgs)
                    applyMessages msgs
                    let remeasured = if hasMsgs then renderStep () else 0
                    let fullRenderCount = fallbacks + (if hasMsgs then 1 else 0)

                    { zero with
                        ProductModelChanged = productModelChanged before model msgs
                        ViewCalled = fullRenderCount > 0
                        FullRenderCount = fullRenderCount
                        RemeasuredNodeCount = remeasured
                        MemoHitCount = fst lastMemo
                        MemoMissCount = snd lastMemo
                        VirtualItemsMaterialized = fst lastVirtual
                        VirtualItemsTotal = snd lastVirtual
                        RepaintedNodeCount = (let (r, _, _) = lastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = lastDamage in rc)
                        DirtyArea = (let (_, _, da) = lastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = lastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = lastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = lastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = lastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = lastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = lastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = lastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = lastReplay in b)
                        TextMeasureCacheHitCount = fst lastTextCache
                        TextMeasureCacheMissCount = snd lastTextCache
                        LayoutInvalidatedNodeCount = lastInvalidated
                        PointerSamplesReceived = 1
                        FullRenderFallbackCount = fallbacks
                        FrameCause = FrameCause.PointerDiscrete
                        DiffRan = hasMsgs
                        LayoutRan = remeasured > 0
                        PaintRan = hasMsgs }
                | _ -> zero)
