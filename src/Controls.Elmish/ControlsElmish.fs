namespace FS.GG.UI.Controls.Elmish

open System
open FS.GG.Audio.Core
open FS.GG.UI.Controls
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open global.Elmish
open FS.GG.UI.DesignSystem

module private RenderLagTrace =
    let private enabled =
        String.Equals(Environment.GetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE"), "1", StringComparison.Ordinal)

    let emit eventName fields =
        if enabled then
            let fieldsText =
                fields
                |> List.map (fun (name, value) -> $"{name}={value}")
                |> String.concat " "

            let suffix = if String.IsNullOrWhiteSpace fieldsText then "" else " " + fieldsText
            let ts = DateTimeOffset.UtcNow.ToString("O", Globalization.CultureInfo.InvariantCulture)
            let ticks = System.Diagnostics.Stopwatch.GetTimestamp()
            Console.Error.WriteLine($"FS_GG_RENDER_LAG_TRACE ts={ts} ticks={ticks} event={eventName}{suffix}")

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

type CompositorFrameDiagnostics =
    { ProofStatus: string
      DamageUnionArea: int
      ScissorCandidateArea: int
      FallbackReason: string option
      PromotionDecisionCount: int
      ReuseHitCount: int
      ReuseMissCount: int
      DemotionCount: int
      SnapshotResourceBytes: int }

type LayoutWorkMetrics =
    { LayoutWorkCount: int
      IntrinsicQueryWorkCount: int
      IntrinsicCacheHitCount: int
      IntrinsicCacheMissCount: int
      IntrinsicInvalidationCount: int }

type ResponsivenessTimingContribution =
    { RoutingDuration: TimeSpan
      UpdateDuration: TimeSpan
      RetainedStepDuration: TimeSpan
      LayoutDuration: TimeSpan
      TextDuration: TimeSpan
      ProductMessageCount: int
      ProductModelChanged: bool
      RuntimeStateChanged: bool
      NoVisibleResponseReason: string option }

type DiagnosticsDisabledCompatibility =
    { FrameMetricsUnchanged: bool
      RecordsWritten: int
      ClockFreePerfScript: bool }

/// Feature 108 (US3, FR-009): one ordered step of the deterministic perf driver.
[<RequireQualifiedAccess>]
type FrameInput<'msg> =
    | Key of ViewerKey * KeyModifiers
    | Pointer of PointerInteraction
    | Tick of TimeSpan
    | Idle

type LiveScriptRunResult =
    { Outcome: ViewerLaunchOutcome
      Metrics: FrameMetrics list }

/// Deterministic, headless evidence for the Controls paced-pointer composition.  The queue drains
/// come from the Viewer runner; `ModelUpdates` counts messages actually folded through the Controls
/// host, so a receipt cannot claim a coalesced aim merely because it was accepted by the lower queue.
type internal DeterministicPointerPacingResult<'model> =
    { Model: 'model
      Drains: ViewerFrameDrain list
      Metrics: ViewerPointerPacingMetrics list }

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

type InteractiveAppGamepadHost<'model, 'msg> =
    { Host: InteractiveAppHost<'model, 'msg>
      Gamepad: GamepadFrameSource<'msg> }

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

    /// Issue #457: the companion `productMessages` never had. `productMessages` keeps the product
    /// messages and DISCARDS every other effect — so a `ReportAdapterDiagnostic` that an interpreter
    /// had carefully constructed, typed, and given a code and a message was dropped on the floor by
    /// the one function every pointer routing path went through. A routing site that wants the
    /// messages must also be able to see the diagnostics; this is how.
    let diagnostics (command: AdapterCommand<'msg>) : AdapterDiagnostic list =
        command
        |> List.choose (function
            | ReportAdapterDiagnostic d -> Some d
            | _ -> None)

    let toCmd (route: AdapterEffect<'msg> -> 'msg) (command: AdapterCommand<'msg>) : Cmd<'msg> =
        command
        |> List.map (fun effect -> (fun (dispatch: Dispatch<'msg>) -> dispatch (route effect)))

module ControlsElmish =
    // Issue #460: THE definition of pointer-move coalescing. Both frame loops call it — the live
    // `runInteractiveApp` loop (which coalesces raw SAMPLES) and `Perf.runScript` (which coalesces
    // INTERACTIONS) — so the two cannot drift apart by hand-copying.
    //
    // They coalesce different alphabets because they must, and that asymmetry is the whole subtlety:
    //
    //   * The live loop sees `PointerSample`s and coalesces them BEFORE the hit-test — avoiding the
    //     hit-test is the entire point of coalescing. Whichever sample survives is then fully routed
    //     by `Pointer.update`, which RE-DERIVES every interaction it implies (hover enter/leave, drag
    //     begin/move) from the hit against `PointerState`. So dropping intermediate samples can never
    //     lose a state transition: a `HoverLeave` is always re-emitted, paired with its `HoverEnter`,
    //     from the one surviving sample.
    //
    //   * `Perf` scripts are written in the alphabet `Pointer.update` PRODUCES, so its coalescing
    //     drops already-derived interactions — and nothing re-derives them. Dropping the wrong one
    //     destroys an event the live host would have delivered.
    //
    // Hence two predicates that are NOT interchangeable, and the distinction the old code missed
    // (it kept only `List.last` of a coalesced frame, silently annihilating a `HoverLeave` that was
    // not last — the exact pair `Pointer.update` emits for a move that changes the hit).
    // `Coalescing.Parity` in Elmish.Tests gates the correspondence against the real state machine.
    //
    // Internal (not public API): a contract between the framework and its tests, reached via
    // `InternalsVisibleTo` — the same footing as `routeRetainedInteraction`.
    module internal Coalescing =
        /// LIVE side. A sample is coalescible iff it carries only a position update: the live loop
        /// buffers these latest-wins and processes at most one per boundary. Every other phase
        /// (press/release/wheel/exit) is a discrete event, processed in arrival order, never dropped.
        let isCoalescibleSample (phase: PointerPhase) : bool =
            match phase with
            | PointerPhase.Moved -> true
            | PointerPhase.Pressed
            | PointerPhase.Released
            | PointerPhase.Wheel
            | PointerPhase.Exited -> false

        /// PERF side, GROUPING: which interactions belong to ONE coalesced move frame. This mirrors
        /// the live loop's frame boundaries — a run of move-derived interactions is the one frame a
        /// burst of `Moved` samples would have produced.
        let isMoveInteraction (interaction: PointerInteraction) : bool =
            match interaction with
            | HoverEnter _
            | HoverLeave _
            | DragMove _ -> true
            | _ -> false

        /// PERF side, DROP: within a coalesced frame, only a POSITIONAL update may be superseded by a
        /// later one — it is the interaction-level image of a dropped sample. Live drops the sample
        /// that would have produced an intermediate `HoverEnter`/`DragMove`, so `Perf` may drop those.
        ///
        /// `HoverLeave` is NOT positional. It is a state transition that `Pointer.update` re-derives
        /// and emits from the SURVIVING sample (paired with the enter), so the live host always
        /// delivers it. Dropping it in `Perf` would lose an event the product really sees — issue #460.
        let isSupersedablePosition (interaction: PointerInteraction) : bool =
            match interaction with
            | HoverEnter _
            | DragMove _ -> true
            | _ -> false

        /// The interactions a coalesced move frame actually routes: every non-supersedable
        /// interaction in arrival order, plus the LAST positional one. For the canonical hit-changing
        /// move this yields `[HoverLeave prior; HoverEnter next]` — exactly what `Pointer.update`
        /// returns for the single surviving sample, in the same order, in one frame.
        let routedInCoalescedFrame (frame: PointerInteraction list) : PointerInteraction list =
            let lastPositional =
                frame
                |> List.indexed
                |> List.filter (snd >> isSupersedablePosition)
                |> List.tryLast
                |> Option.map fst

            frame
            |> List.indexed
            |> List.filter (fun (i, interaction) ->
                not (isSupersedablePosition interaction) || Some i = lastPositional)
            |> List.map snd

    let diagnostic source code message =
        { Source = source
          Code = code
          Message = message }

    // Issue #457: `pointer/UnresolvedControlId` — a routed `PointerInteraction` named a `ControlId` that
    // NO control in the frame carries. Deliberately NOT the existing `HitTestMiss`, which the geometric
    // hit-test raises when a press lands on empty space: that is a routine thing a user does (and it has
    // no id to name — `PointerDiagnostic.Control` is honestly `None` there), whereas this one is always a
    // defect in the caller. One code cannot be both routine and a defect, and conflating them is what
    // makes the level unpickable — so they are separate codes at separate levels.
    let internal unresolvedControlIdCode = "UnresolvedControlId"

    // Issue #456 (epic FS-GG/.github#416): `keyboard-input/HostKeyCaptureNotInterpreted`. Emitting
    // `KeyboardEffect.RequestHostKeyCapture` arms a capture that NOTHING completes: `ViewerEffect` has no
    // case that carries a `KeyboardEffect` (`DispatchInput` is host->product only), so the request cannot
    // reach a host at all. This arm used to lower it to `DispatchHostCommand "capture-key:{key}"` — an
    // `AdapterEffect` the framework never interprets either, whose only consumer in the tree turns it into
    // a LOG STRING. A consumer who did everything right got silence; the decoy is what made it look wired.
    // Per the epic's rule, the surface now says what it cannot do, and names the seam that can.
    let internal hostKeyCaptureNotInterpretedCode = "HostKeyCaptureNotInterpreted"

    // Issue #457: hand each adapter diagnostic to the host's diagnostics sink, THEN extract the product
    // messages. Every pointer routing site used to call `AdapterCmd.productMessages` alone, which
    // filtered `ReportAdapterDiagnostic` out — so the escape hatch the silent-no-op family reaches for
    // ("just emit a diagnostic; every site has a channel in scope") was itself silent on this path.
    //
    // Diagnostics go through the SAME `ViewerDiagnosticsOptions` filter the live viewer applies
    // (`Viewer.shouldCaptureDiagnostic`), so a product's configured `MinimumLevel`/`Categories` govern
    // what it sees — the filter is the product's policy, not a hardcoded drop. Both levels below pass
    // the default options (`MinimumLevel = Info`, `Categories` includes `Input`). A host with no sink —
    // the default — observes nothing and behaves byte-identically to before (SC: at-rest unchanged).
    let internal routeAdapterDiagnostics (options: ViewerDiagnosticsOptions) (command: AdapterCommand<'msg>) : 'msg list =
        match options.Sink with
        | Some sink ->
            command
            |> AdapterCmd.diagnostics
            |> List.iter (fun d ->
                let level =
                    // A named id that resolves to nothing is a caller defect; everything else on this
                    // channel (a geometric miss, a stale target) is a routine runtime condition.
                    if d.Code = unresolvedControlIdCode then
                        ViewerDiagnosticLevel.Warning
                    else
                        ViewerDiagnosticLevel.Info

                let event =
                    { Level = level
                      Category = ViewerDiagnosticCategory.Input
                      Message = $"[{d.Source}/{d.Code}] {d.Message}"
                      FrameIndex = None
                      Stage = None
                      Elapsed = None }

                if Viewer.shouldCaptureDiagnostic options event then
                    sink event)
        | None -> ()

        AdapterCmd.productMessages command

    // Feature 186 (US1, FR-001/SC-001/SC-007): the SINGLE site that names all 32 `FrameMetrics`
    // fields. Every full-construction caller delegates here, so adding a new per-frame metric is a
    // one-site edit and it appears on every frame-emit path. Values are byte-identical to the former
    // hand-spelled records (FR-007). Internal by absence from `ControlsElmish.fsi`. The grouped
    // tuple inputs mirror the per-frame work-reduction carriers threaded by `runScriptCore` (US2).
    let private buildFrameMetrics
        (frameCause: FrameCause)
        (productModelChanged: bool)
        (viewCalled: bool)
        (fullRenderCount: int)
        (remeasuredNodeCount: int)
        (diffRan: bool)
        (layoutRan: bool)
        (paintRan: bool)
        (pointerSamplesReceived: int)
        (pointerMovesProcessed: int)
        (fullRenderFallbackCount: int)
        (frameDuration: TimeSpan)
        (paintDuration: TimeSpan)
        (composeDuration: TimeSpan)
        (memo: int * int)
        (virtual': int * int)
        (damage: int * int * int)
        (picture: int * int * int)
        (replay: int * int * int * int * int)
        (textCache: int * int)
        (layoutInvalidatedNodeCount: int)
        : FrameMetrics =
        let memoHits, memoMisses = memo
        let virtualMaterialized, virtualTotal = virtual'
        let repaintedNodeCount, dirtyRectCount, dirtyArea = damage
        let pictureHits, pictureMisses, pictureEntries = picture
        let replayHits, replayMisses, replayRecords, replaySkipped, replayBytes = replay
        let textHits, textMisses = textCache

        { ProductModelChanged = productModelChanged
          ViewCalled = viewCalled
          FullRenderCount = fullRenderCount
          RemeasuredNodeCount = remeasuredNodeCount
          MemoHitCount = memoHits
          MemoMissCount = memoMisses
          VirtualItemsMaterialized = virtualMaterialized
          VirtualItemsTotal = virtualTotal
          RepaintedNodeCount = repaintedNodeCount
          DirtyRectCount = dirtyRectCount
          DirtyArea = dirtyArea
          PictureCacheHitCount = pictureHits
          PictureCacheMissCount = pictureMisses
          PictureCacheEntryCount = pictureEntries
          TextMeasureCacheHitCount = textHits
          TextMeasureCacheMissCount = textMisses
          LayoutInvalidatedNodeCount = layoutInvalidatedNodeCount
          PointerSamplesReceived = pointerSamplesReceived
          PointerMovesProcessed = pointerMovesProcessed
          FullRenderFallbackCount = fullRenderFallbackCount
          FrameCause = frameCause
          DiffRan = diffRan
          LayoutRan = layoutRan
          PaintRan = paintRan
          FrameDuration = frameDuration
          PaintDuration = paintDuration
          ComposeDuration = composeDuration
          ReplayHitCount = replayHits
          ReplayMissCount = replayMisses
          ReplayRecordCount = replayRecords
          ReplaySkippedNodeCount = replaySkipped
          ReplayCacheNativeBytes = replayBytes }

    let interpretKeyboardEffect mapCommand effect =
        match effect with
        | CommandResolved command -> [ DispatchProductMessage(mapCommand command) ]
        | KeyStateChanged _
        | LayoutChanged _
        | ModeChanged _
        | PendingSequenceChanged _
        | StateDisplayChanged _ -> []
        | RequestHostKeyCapture key ->
            [ ReportAdapterDiagnostic(
                  diagnostic
                      "keyboard-input"
                      hostKeyCaptureNotInterpretedCode
                      $"RequestHostKeyCapture '{key}' is not interpreted by any host: no ViewerEffect carries a KeyboardEffect, so the request never reaches one and the capture never fires. Capture the key in the product instead: forward the raw key out of the host's MapKey — MapKey = fun key isDown -> Some(YourMsg(ViewerKeyboard.toKeyId key, isDown)) — and route it in update, where the keymap and the capture state live."
              ) ]
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
        // Feature 175: scroll offset is owned by the host's scroll state (and surfaced via the
        // scroll-viewer's optional OnChanged binding), not re-dispatched as a runtime message here.
        | ScrollChanged _
        | CancelledInteraction _ -> []
        | StaleTarget controlId ->
            [ ReportAdapterDiagnostic(diagnostic "control-runtime" "StaleTarget" $"Stale control target '{controlId}' was ignored by the Controls adapter.") ]
        | ReportControlRuntimeDiagnostic controlDiagnostic ->
            [ ReportAdapterDiagnostic(diagnostic "control-runtime" (string controlDiagnostic.Code) controlDiagnostic.Message) ]

    let interpretOverlayEffect mapOpen mapDispatch mapFocus effect =
        match effect with
        | RequestOpenStateChange(surface, isOpen) -> [ AdapterEffect.DispatchProductMessage(mapOpen surface isOpen) ]
        | OverlayEffect.DispatchProductMessage(surface, payload) -> [ AdapterEffect.DispatchProductMessage(mapDispatch surface payload) ]
        | RequestFocus focus ->
            [ yield DispatchControlRuntimeMessage(FocusControl focus)
              match mapFocus focus with
              | Some msg -> yield AdapterEffect.DispatchProductMessage msg
              | None -> () ]
        | ReportOverlayDiagnostic controlDiagnostic ->
            [ ReportAdapterDiagnostic(diagnostic "overlay-state" (string controlDiagnostic.Code) controlDiagnostic.Message) ]
        | ConsumeInput
        | AllowPassThrough -> []
        | RecordTopmostHit decision ->
            [ ReportAdapterDiagnostic(diagnostic "overlay-state" "TopmostHit" decision.Input) ]

    let interpretOverlayOutcome mapOpen mapDispatch mapFocus effects =
        effects |> List.collect (interpretOverlayEffect mapOpen mapDispatch mapFocus)

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

    let compositorDiagnostics proofReady fallbackReason (metrics: FrameMetrics) =
        let fallback =
            if proofReady then
                fallbackReason
            else
                fallbackReason |> Option.orElse (Some "present proof is not ready")

        { ProofStatus = if proofReady then "passed" else "not-ready"
          DamageUnionArea = metrics.DirtyArea
          ScissorCandidateArea = if proofReady && fallback.IsNone then metrics.DirtyArea else 0
          FallbackReason = fallback
          PromotionDecisionCount = metrics.PictureCacheHitCount + metrics.PictureCacheMissCount
          ReuseHitCount = metrics.PictureCacheHitCount + metrics.ReplayHitCount
          ReuseMissCount = metrics.PictureCacheMissCount + metrics.ReplayMissCount
          DemotionCount = if metrics.ReplaySkippedNodeCount = 0 && metrics.ReplayMissCount > 0 then 1 else 0
          SnapshotResourceBytes = metrics.ReplayCacheNativeBytes }

    let layoutMetrics (metrics: FrameMetrics) =
        { LayoutWorkCount = metrics.RemeasuredNodeCount
          IntrinsicQueryWorkCount = 0
          IntrinsicCacheHitCount = 0
          IntrinsicCacheMissCount = if metrics.LayoutRan then metrics.LayoutInvalidatedNodeCount else 0
          IntrinsicInvalidationCount = metrics.LayoutInvalidatedNodeCount }

    let responsivenessTimingContribution (metrics: FrameMetrics) =
        let nonNegative (value: TimeSpan) =
            if value < TimeSpan.Zero then TimeSpan.Zero else value

        let framePreparation =
            metrics.FrameDuration - metrics.PaintDuration - metrics.ComposeDuration
            |> nonNegative

        { RoutingDuration = TimeSpan.Zero
          UpdateDuration = if metrics.ProductModelChanged then framePreparation else TimeSpan.Zero
          RetainedStepDuration = framePreparation
          LayoutDuration = if metrics.LayoutRan then framePreparation else TimeSpan.Zero
          TextDuration = TimeSpan.Zero
          ProductMessageCount = if metrics.ProductModelChanged then 1 else 0
          ProductModelChanged = metrics.ProductModelChanged
          RuntimeStateChanged =
            metrics.PointerSamplesReceived > 0
            || metrics.PointerMovesProcessed > 0
            || metrics.FrameCause = FrameCause.Resize
            || metrics.FrameCause = FrameCause.Theme
          NoVisibleResponseReason =
            if metrics.ProductModelChanged || metrics.PaintRan || metrics.LayoutRan then
                None
            else
                Some "no product/runtime/paint change" }

    let private deterministicFrameMetricsShape (metrics: FrameMetrics) =
        metrics.ProductModelChanged,
        metrics.ViewCalled,
        metrics.FullRenderCount,
        metrics.RemeasuredNodeCount,
        metrics.MemoHitCount,
        metrics.MemoMissCount,
        metrics.VirtualItemsMaterialized,
        metrics.VirtualItemsTotal,
        metrics.RepaintedNodeCount,
        metrics.DirtyRectCount,
        metrics.DirtyArea,
        metrics.PictureCacheHitCount,
        metrics.PictureCacheMissCount,
        metrics.PictureCacheEntryCount,
        metrics.TextMeasureCacheHitCount,
        metrics.TextMeasureCacheMissCount,
        metrics.LayoutInvalidatedNodeCount,
        metrics.PointerSamplesReceived,
        metrics.PointerMovesProcessed,
        metrics.FullRenderFallbackCount,
        metrics.FrameCause,
        metrics.DiffRan,
        metrics.LayoutRan,
        metrics.PaintRan,
        metrics.PaintDuration,
        metrics.ComposeDuration,
        metrics.ReplayHitCount,
        metrics.ReplayMissCount,
        metrics.ReplayRecordCount,
        metrics.ReplaySkippedNodeCount,
        metrics.ReplayCacheNativeBytes

    let diagnosticsDisabledCompatibility before after =
        let beforeShape = before |> List.map deterministicFrameMetricsShape
        let afterShape = after |> List.map deterministicFrameMetricsShape

        { FrameMetricsUnchanged = beforeShape = afterShape
          RecordsWritten = 0
          ClockFreePerfScript =
            after
            |> List.forall (fun metrics ->
                metrics.FrameDuration = TimeSpan.Zero
                && metrics.PaintDuration = TimeSpan.Zero
                && metrics.ComposeDuration = TimeSpan.Zero) }

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

    let adapterDiagnosticToRuntimeDiagnostic context diagnostic =
        let source =
            FS.GG.UI.Diagnostics.RuntimeDiagnostics.source
                (Some "FS.GG.UI.Controls.Elmish")
                diagnostic.Source
                None
                None

        FS.GG.UI.Diagnostics.RuntimeDiagnostics.create
            source
            (Some diagnostic.Code)
            (Some FS.GG.UI.Diagnostics.DiagnosticSeverity.Warning)
            (Some FS.GG.UI.Diagnostics.DiagnosticCategory.DeveloperAction)
            diagnostic.Message
            (Some "Review the adapter diagnostic and update routing or runtime wiring.")
            context

    // FR-001/FR-003 (feature 090): a pointer Click is binding-eligible for an authored control's
    // click-equivalent bindings (`onClick`→"click", a click-driven toggle `onChanged`→"changed", a
    // click-driven `onSelected`→"selected"). Other interactions (hover/drag/scroll) are not
    // binding-eligible here and go straight to `MapPointer`.
    let clickEquivalentKinds = [ "click"; "changed"; "selected" ]

    let private tryFindControlById (root: Control<'msg>) (controlId: ControlId) : Control<'msg> option =
        let rec loop path (control: Control<'msg>) =
            let id = control.Key |> Option.defaultValue path
            if id = controlId then
                Some control
            else
                control.Children
                |> List.mapi (fun index child -> path + "." + string index, child)
                |> List.tryPick (fun (childPath, child) -> loop childPath child)

        loop "0" root

    let private dispatchBindings
        (origin: ControlEventOrigin)
        (controlId: ControlId)
        (kind: string)
        (nav: NavPayload option)
        (bindings: ControlEventBinding<'msg> list)
        : 'msg list =
        bindings
        |> List.map (fun binding ->
            binding.Dispatch
                { Kind = kind
                  ControlId = Some controlId
                  Origin = origin
                  Nav = nav })

    let private sliderChangedMessages
        (rendered: ControlRenderResult<'msg>)
        (root: Control<'msg>)
        (authored: ControlId)
        (x: float)
        (origin: ControlEventOrigin)
        : 'msg list option =
        match tryFindControlById root authored with
        | Some control when control.Kind = "slider" ->
            let bindings =
                rendered.EventBindings
                |> List.filter (fun binding -> binding.ControlId = authored && binding.EventKind = "changed")

            match bindings, rendered.Bounds |> List.tryFind (fun (id, _) -> id = authored) with
            | [], _ -> None
            | _, None -> None
            | _, Some(_, bounds) ->
                let value = Math.Clamp((x - bounds.X) / max 1.0 bounds.Width, 0.0, 1.0)

                dispatchBindings origin authored "changed" (Some(SteppedValue value)) bindings
                |> Some
        | _ -> None

    // Resolve the authored bindings (if any) a single interaction should dispatch. `Some msgs` means
    // an authored binding consumed the interaction (MapPointer is NOT consulted for it); `None` means
    // no authored binding matched, so the host falls back to `MapPointer` with the raw interaction.
    // Feature 175: a boolean toggle (switch / check-box) reports its NEW value via the `changed`
    // binding's payload — but a CLICK carries no payload, so without this every click dispatched the
    // `onChangedBool` default (`false`), i.e. the control could be turned OFF but never back ON. Read
    // the control's current `selected` state and dispatch `not current` (the mirror of
    // `sliderChangedMessages`, which computes a slider's value from x). `toggle-button` is unaffected:
    // it bakes `not IsOn` into an `onClick` message at view time, so it needs no payload.
    let private booleanToggleKinds = Set.ofList [ "switch"; "check-box" ]

    let private toggleChangedMessages
        (rendered: ControlRenderResult<'msg>)
        (root: Control<'msg>)
        (authored: ControlId)
        (origin: ControlEventOrigin)
        : 'msg list option =
        match tryFindControlById root authored with
        | Some control when booleanToggleKinds.Contains control.Kind ->
            let bindings =
                rendered.EventBindings
                |> List.filter (fun binding -> binding.ControlId = authored && binding.EventKind = "changed")

            match bindings with
            | [] -> None
            | bindings ->
                let current =
                    control.Attributes
                    |> List.tryPick (fun a ->
                        if a.Name = "selected" then
                            match a.Value with
                            | BoolValue v -> Some v
                            | _ -> None
                        else
                            None)
                    |> Option.defaultValue false

                // Feature 184 (US3): report the new boolean state typed as `SteppedValue 1.0/0.0`
                // (read back by `ChangeAdapters.onChangedBool` as `>= 0.5`).
                let newState = if not current then 1.0 else 0.0
                dispatchBindings origin authored "changed" (Some(SteppedValue newState)) bindings |> Some
        | _ -> None

    // Feature 241 (Review P10 / C3): the value-from-position discrete-option kinds. `radio-group`
    // (vertical) and `tabs` (horizontal) each lay their options out along one axis; a click reports the
    // option UNDER the cursor as a `MovedSelection(index, Some value)` — read back by
    // `ChangeAdapters.onChangedString` via `navText`. Without this a click fell through to
    // `Payload = None`, so `onChangedString` saw `""`: a live wrong-value dispatch shipped as a KNOWN
    // GAPS comment. `stringItemsOf`/`changedBindingsFor`/`boundsOf` factor the shared read; each kind
    // supplies only its axis→index formula, which MUST mirror its geometry (`WidgetGeometry.radioGeom`
    // caps the row height at `theme.ControlHeight`; `tabsGeom` splits the width evenly).
    let private stringItemsOf (control: Control<'msg>) : string list =
        control.Attributes
        |> List.tryPick (fun a ->
            if a.Name = "items" then
                match a.Value with
                | StringListValue items -> Some items
                | _ -> None
            else
                None)
        |> Option.defaultValue []

    let private changedBindingsFor (rendered: ControlRenderResult<'msg>) (authored: ControlId) =
        rendered.EventBindings
        |> List.filter (fun binding -> binding.ControlId = authored && binding.EventKind = "changed")

    let private boundsOf (rendered: ControlRenderResult<'msg>) (authored: ControlId) : Rect option =
        rendered.Bounds |> List.tryFind (fun (id, _) -> id = authored) |> Option.map snd

    // `radio-group`: options stack vertically; the clicked option is `floor((y - top) / rowH)` with the
    // SAME `rowH = min theme.ControlHeight (height / n)` `radioGeom` paints (`WidgetGeometry.fs`),
    // clamped to a valid option index. F-CTL-1: the cap MUST read `theme.ControlHeight` — a hardcoded
    // literal (was `28.0`) diverges from the painter (32.0 in every shipped theme), so a click on a
    // legibly-tall group lands in the wrong band and dispatches the wrong option.
    let private radioGroupChangedMessages
        (theme: Theme)
        (rendered: ControlRenderResult<'msg>)
        (root: Control<'msg>)
        (authored: ControlId)
        (y: float)
        (origin: ControlEventOrigin)
        : 'msg list option =
        match tryFindControlById root authored with
        | Some control when control.Kind = "radio-group" ->
            match stringItemsOf control, changedBindingsFor rendered authored, boundsOf rendered authored with
            | [], _, _
            | _, [], _
            | _, _, None -> None
            | items, bindings, Some bounds ->
                let n = List.length items
                let rowH = min theme.ControlHeight (bounds.Height / float n)
                let index = Math.Clamp(int (floor ((y - bounds.Y) / max 1.0 rowH)), 0, n - 1)
                let value = List.item index items
                dispatchBindings origin authored "changed" (Some(MovedSelection(index, Some value))) bindings
                |> Some
        | _ -> None

    // `tabs`: tabs sit side by side; the clicked tab is `floor((x - left) / tabWidth)` with the SAME
    // even `tabWidth = width / n` `tabsGeom` paints, clamped to a valid tab index.
    let private tabsChangedMessages
        (rendered: ControlRenderResult<'msg>)
        (root: Control<'msg>)
        (authored: ControlId)
        (x: float)
        (origin: ControlEventOrigin)
        : 'msg list option =
        match tryFindControlById root authored with
        | Some control when control.Kind = "tabs" ->
            match stringItemsOf control, changedBindingsFor rendered authored, boundsOf rendered authored with
            | [], _, _
            | _, [], _
            | _, _, None -> None
            | items, bindings, Some bounds ->
                let n = List.length items
                let tabWidth = bounds.Width / float n
                let index = Math.Clamp(int (floor ((x - bounds.X) / max 1.0 tabWidth)), 0, n - 1)
                let value = List.item index items
                dispatchBindings origin authored "changed" (Some(MovedSelection(index, Some value))) bindings
                |> Some
        | _ -> None

    // `numeric-input`: a click focuses the field, it does NOT edit the number (that is keyboard entry),
    // so it must report the control's CURRENT `value` — NOT fabricate `0.0`. Reads the "value"
    // `FloatValue` and echoes it as `SteppedValue` (read back by `onChangedFloat` via `navValue`),
    // turning the P10 reset-to-0.0 dispatch into a faithful no-op change.
    let private numericInputChangedMessages
        (rendered: ControlRenderResult<'msg>)
        (root: Control<'msg>)
        (authored: ControlId)
        (origin: ControlEventOrigin)
        : 'msg list option =
        match tryFindControlById root authored with
        | Some control when control.Kind = "numeric-input" ->
            match changedBindingsFor rendered authored with
            | [] -> None
            | bindings ->
                let current =
                    control.Attributes
                    |> List.tryPick (fun a ->
                        if a.Name = "value" then
                            match a.Value with
                            | FloatValue v -> Some v
                            | _ -> None
                        else
                            None)
                    |> Option.defaultValue 0.0

                dispatchBindings origin authored "changed" (Some(SteppedValue current)) bindings |> Some
        | _ -> None

    /// F3 — the activation-value contract: how a control kind computes its `changed` payload from a
    /// click, given the rendered frame, the control tree, the authored id, the click x AND y, and the
    /// origin. `Some msgs` means the kind owns the click and reports its activated value; `None` falls
    /// through to the generic `Payload = None` click bindings. (Feature 241 threads y so a vertically
    /// laid-out kind — `radio-group` — can resolve the option under the cursor.)
    type private ActivationValueComputer<'msg> =
        ControlRenderResult<'msg> -> Control<'msg> -> ControlId -> float -> float -> ControlEventOrigin -> 'msg list option

    /// The activation-value REGISTRY: control kind → its activation-value computer. `bindingMessagesFor`
    /// consults this (keyed by the control's `Kind`) before the generic `Payload = None` fallback, so a
    /// value-bearing kind declares its click→payload computation in ONE place instead of growing an
    /// `if kind = …` cascade in the router. Registered: `slider` (value from x), the boolean toggles
    /// `switch`/`check-box` (flip `selected`), the discrete-option kinds `radio-group` (option from y)
    /// and `tabs` (tab from x), and `numeric-input` (echo the current value).
    ///
    /// Feature 241 (Review P10 / C3) closed the former KNOWN GAPS: `radio-group`/`tabs` no longer
    /// fall through to `onChangedString` → `""`, and `numeric-input` no longer resets `onChangedFloat`
    /// to `0.0`. Still UNREGISTERED (deliberately): `segmented`/`rate` have no public `onChanged`
    /// binding yet, so there is no payload to compute; `toggle-button` bakes `not IsOn` into an
    /// `onClick` at view time, so it needs no payload (the toggle-authoring split is F6). Each becomes
    /// correct by REGISTERING a computer here once it grows a `changed` binding.
    // `theme` is threaded in so the geometry-mirroring computers (radio-group) can cap on the SAME
    // `theme.ControlHeight` the painter uses (F-CTL-1); kinds with no theme-dependent geometry ignore it.
    let private activationValueComputers (theme: Theme) : (string * ActivationValueComputer<'msg>) list =
        [ "slider", (fun rendered root authored x _ origin -> sliderChangedMessages rendered root authored x origin)
          "switch", (fun rendered root authored _ _ origin -> toggleChangedMessages rendered root authored origin)
          "check-box", (fun rendered root authored _ _ origin -> toggleChangedMessages rendered root authored origin)
          "radio-group", (fun rendered root authored _ y origin -> radioGroupChangedMessages theme rendered root authored y origin)
          "tabs", (fun rendered root authored x _ origin -> tabsChangedMessages rendered root authored x origin)
          "numeric-input", (fun rendered root authored _ _ origin -> numericInputChangedMessages rendered root authored origin) ]

    /// Consult the activation-value registry for the authored control's kind. The registry key is
    /// authoritative: an unregistered kind returns `None` (→ generic `Payload = None` click bindings).
    let private activationValueFor
        (theme: Theme)
        (rendered: ControlRenderResult<'msg>)
        (root: Control<'msg>)
        (authored: ControlId)
        (x: float)
        (y: float)
        (origin: ControlEventOrigin)
        : 'msg list option =
        tryFindControlById root authored
        |> Option.bind (fun control ->
            activationValueComputers theme
            |> List.tryFind (fun (kind, _) -> kind = control.Kind)
            |> Option.bind (fun (_, compute) -> compute rendered root authored x y origin))

    let bindingMessagesFor (theme: Theme) (rendered: ControlRenderResult<'msg>) (root: Control<'msg>) (interaction: PointerInteraction) : 'msg list option =
        match interaction with
        | Click(control, _, x, y) ->
            match Control.nearestAuthored rendered control with
            | Some authored ->
                // F3: consult the activation-value registry (keyed by control kind) for the click's
                // payload; fall through to the generic `Payload = None` click bindings when unregistered.
                match activationValueFor theme rendered root authored x y ControlEventOrigin.Pointer with
                | Some msgs -> Some msgs
                | None ->
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
                                  Nav = None })
                        |> Some
            | None -> None
        | DragMove(control, PointerButton.Primary, x, _)
        | DragEnd(control, PointerButton.Primary, x, _) ->
            Control.nearestAuthored rendered control
            |> Option.bind (fun authored -> sliderChangedMessages rendered root authored x ControlEventOrigin.Pointer)
        | _ -> None

    // Issue #457: a ROUTED `PointerInteraction` (a `FrameInput.Pointer`, or one replayed onto the
    // retained frame) carries an ALREADY-RESOLVED `ControlId`. `Pointer.update`'s geometric hit-test —
    // the only place `HitTestMiss` is raised — never runs for it, so an interaction naming an id that
    // simply does not exist sails straight through: `bindingMessagesFor` returns `None`, `MapPointer`
    // declines, and NOTHING happens. "No authored binding matched" and "that id does not exist" were
    // indistinguishable, so a typo'd id produced a test that drives nothing — and if its assertion was a
    // negative one ("the screen did not change"), it PASSED.
    //
    // Only ACTIVATING interactions are checked — the ones `bindingMessagesFor` above can consume. A
    // hover/exit legitimately names a control that is not in the frame (scripts spell "the pointer is
    // over nothing" as a `HoverEnter` at an absent id), so flagging those would cry wolf. `Bounds` keys
    // every laid-out control instance and is populated on both routing paths (only the unused preview
    // `Control.render` leaves it empty), which makes it the frame's honest id set.
    let private unresolvedControlDiagnostics
        (rendered: ControlRenderResult<'msg>)
        (interaction: PointerInteraction)
        : AdapterCommand<'msg> =
        let carried id =
            rendered.Bounds |> List.exists (fun (known, _) -> known = id)

        let unresolved (id: ControlId) =
            [ ReportAdapterDiagnostic(
                  diagnostic
                      "pointer"
                      unresolvedControlIdCode
                      $"Pointer interaction named control id '{id}', which no control in the current frame carries — it dispatched nothing. Check for a typo'd, mis-cased, or stale ControlId, or a control that is not currently rendered."
              ) ]

        match interaction with
        | Click(id, _, _, _) when not (carried id) -> unresolved id
        | DragMove(id, PointerButton.Primary, _, _)
        | DragEnd(id, PointerButton.Primary, _, _) when not (carried id) -> unresolved id
        | _ -> []

    // Issue #457: the single MapPointer-fallback seam for an interaction no authored binding consumed.
    // Every pointer routing site funnels through here, so the two halves of the fix land once: the
    // unresolved-id diagnostic is raised, and BOTH it and whatever `interpretPointerEffect` lowered
    // (including a genuine geometric `HitTestMiss`) reach the host's diagnostics sink instead of being
    // filtered out by `AdapterCmd.productMessages`. Product messages are extracted exactly as before.
    let private routePointerFallback
        (options: ViewerDiagnosticsOptions)
        (mapPointer: PointerInteraction -> 'msg option)
        (rendered: ControlRenderResult<'msg>)
        (interaction: PointerInteraction)
        : 'msg list =
        match options.Sink with
        // No sink: nothing can observe a diagnostic, so do not go looking for one. The `Bounds` scan in
        // `unresolvedControlDiagnostics` is O(laid-out controls) and a primary-button DRAG re-enters this
        // path once per sample, so it is not a free check to leave running. At rest — the default, and
        // every pre-existing test — this is byte-for-byte the pre-#457 path.
        | None -> interpretPointerEffect mapPointer interaction |> AdapterCmd.productMessages
        | Some _ ->
            unresolvedControlDiagnostics rendered interaction
            @ interpretPointerEffect mapPointer interaction
            |> routeAdapterDiagnostics options

    // The viewer→neutral phase mapping, factored out of `pointerSampleOf` (issue #460) so the live
    // loop can ask `Coalescing.isCoalescibleSample` about an incoming input without building a whole
    // `PointerSample` on the pointer-move hot path.
    let private pointerPhaseOf (phase: ViewerPointerPhaseKind) : PointerPhase =
        match phase with
        | ViewerPointerPhaseKind.Moved -> PointerPhase.Moved
        | ViewerPointerPhaseKind.Pressed -> PointerPhase.Pressed
        | ViewerPointerPhaseKind.Released -> PointerPhase.Released
        | ViewerPointerPhaseKind.Wheel -> PointerPhase.Wheel
        | ViewerPointerPhaseKind.Exited -> PointerPhase.Exited

    // Translate a native viewer pointer input into the neutral 075 `PointerSample` the gesture fold
    // consumes. Pure/total; shared by the preserved full-render oracle and the feature-110 retained
    // route so both feed `Pointer.update` the identical sample (a precondition of dispatch parity).
    let private pointerSampleOf (input: ViewerPointerInput) : PointerSample =
        let phase = pointerPhaseOf input.Phase

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

    // Feature 191 (US2, C6/FR-006): a `canvas` bound via `Canvas.onPointer` carries its raw-sample
    // handler as a boxed `UntypedValue` under the "onPointer" attribute (it forwards the RAW
    // PointerSample, not an interpreted Click, so it rides a dedicated channel rather than EventBindings).
    let private canvasPointerHandler (control: Control<'msg>) : (PointerSample -> 'msg) option =
        control.Attributes
        |> List.rev
        |> List.tryPick (fun attr ->
            if attr.Name = "onPointer" then
                match attr.Value with
                | UntypedValue v ->
                    match v with
                    | :? (PointerSample -> 'msg) as f -> Some f
                    | _ -> None
                | _ -> None
            else
                None)

    // Feature 191 (US2, C6/FR-007): a focused `canvas` bound via `Canvas.onKey` carries its raw-key
    // handler as a boxed `UntypedValue` under the "onKey" attribute.
    let private canvasKeyHandler (control: Control<'msg>) : (ViewerKey -> KeyModifiers -> 'msg) option =
        control.Attributes
        |> List.rev
        |> List.tryPick (fun attr ->
            if attr.Name = "onKey" then
                match attr.Value with
                | UntypedValue v ->
                    match v with
                    | :? (ViewerKey -> KeyModifiers -> 'msg) as f -> Some f
                    | _ -> None
                | _ -> None
            else
                None)

    let rec private collectControls (c: Control<'msg>) : Control<'msg> list =
        c :: (c.Children |> List.collect collectControls)

    // Forward the raw pointer sample (in canvas-local coordinates) to every keyed `canvas` bound to
    // `onPointer` whose laid-out box contains the sample. An out-of-box sample dispatches nothing
    // (FR-006). Keyed canvases only — the canvas id (its `Key`) joins the sample to its `Bounds` box.
    let private canvasPointerMessages (rendered: ControlRenderResult<'msg>) (root: Control<'msg>) (sample: PointerSample) : 'msg list =
        collectControls root
        |> List.choose (fun c ->
            match c.Kind, c.Key with
            | "canvas", Some key ->
                canvasPointerHandler c
                |> Option.bind (fun handler ->
                    rendered.Bounds
                    |> List.tryFind (fun (id, _) -> id = key)
                    |> Option.bind (fun (_, box) ->
                        if
                            sample.X >= box.X
                            && sample.X <= box.X + box.Width
                            && sample.Y >= box.Y
                            && sample.Y <= box.Y + box.Height
                        then
                            Some(handler { sample with X = sample.X - box.X; Y = sample.Y - box.Y })
                        else
                            None))
            | _ -> None)

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
            let current = host.View size model
            let rendered = Control.renderTree host.Theme size current

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
                    match bindingMessagesFor host.Theme rendered current interaction with
                    | Some msgs -> msgs
                    | None -> routePointerFallback host.Diagnostics host.MapPointer rendered interaction)

            // Feature 191 (US2, FR-006): forward the raw sample (canvas-local) to an in-box canvas bound
            // to onPointer. Independent of the gesture fold so move/wheel reach the canvas too; appended
            // so existing EventBinding/MapPointer routing is bit-for-bit unchanged (additive).
            state', messages @ canvasPointerMessages rendered current (pointerSampleOf input)

    /// Issue #1159: the Controls-first composition used by the paced launcher. The raw mapper is
    /// deliberately invoked after the normal routing result, so an authored binding remains first and
    /// a product's continuous-pointer message cannot replace Controls hit-testing.
    let routeInteractivePointerWithRawFallback
        (host: InteractiveAppHost<'model, 'msg>)
        (mapRawPointer: ViewerPointerInput -> Size -> 'model -> 'msg list)
        (state: PointerState)
        (size: Size)
        (model: 'model)
        (input: ViewerPointerInput)
        : PointerState * 'msg list =
        let state', messages = routeInteractivePointer host state size model input
        state', messages @ mapRawPointer input size model

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
        (theme: Theme)
        (retained: RetainedRender<'msg>)
        (render: ControlRenderResult<'msg>)
        (interaction: PointerInteraction)
        : 'msg list option * bool =
        match interaction with
        | Click(_, _, x, y) ->
            match RetainedRender.retainedHitTest x y retained with
            | None -> None, true
            | Some _ -> bindingMessagesFor theme render retained.Root.Control interaction, false
        | DragMove(_, PointerButton.Primary, _, _)
        | DragEnd(_, PointerButton.Primary, _, _) -> bindingMessagesFor theme render retained.Root.Control interaction, false
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
        match retainedBindingMessages host.Theme retained render interaction with
        | Some msgs, _ -> msgs, 0
        | None, false -> routePointerFallback host.Diagnostics host.MapPointer render interaction, 0
        | None, true ->
            // Counted full-render fallback (FR-007/FR-009): a fresh render + the oracle's resolution.
            let current = host.View size model
            let rendered = Control.renderTree host.Theme size current

            match bindingMessagesFor host.Theme rendered current interaction with
            | Some msgs -> msgs, 1
            | None -> routePointerFallback host.Diagnostics host.MapPointer rendered interaction, 1

    // Feature 175 (FR-001) / F6: wheel-delta normalization. Raw `ViewerPointerInput.DeltaY` from the
    // viewer is a few units per notch (the GL backend forwards the OS wheel count verbatim, it is NOT
    // pre-normalized to pixels), so it is scaled to pixels HERE — the SINGLE framework-side wheel→pixels
    // seam, so no product/host reinvents a multiplier. The companion keyboard step is
    // `Pointer.scrollLineStep` (40 px/line); at ~3 units/notch this gives ~48 px/notch, a little over one
    // line — the responsive-without-overshoot target. Change scroll feel here, in one place.
    let private wheelScrollStep = 16.0

    // Feature 175 (FR-001/FR-009): the `scroll-viewer` ids in the current tree (Key ?? structural path,
    // matching `render.Bounds` keys).
    let rec private collectScrollViewerIds (path: string) (c: Control<'msg>) : ControlId list =
        let id = c.Key |> Option.defaultValue path
        let here = if c.Kind = "scroll-viewer" then [ id ] else []
        here @ (c.Children |> List.mapi (fun i ch -> collectScrollViewerIds (path + "." + string i) ch) |> List.concat)

    // Feature 175: the innermost `scroll-viewer` whose painted bounds contain (x, y), or None.
    let private enclosingScrollViewer (retained: RetainedRender<'msg>) (render: ControlRenderResult<'msg>) (x: float) (y: float) : ControlId option =
        let svIds = collectScrollViewerIds "0" retained.Root.Control |> Set.ofList
        render.Bounds
        |> List.filter (fun (id, _) -> svIds.Contains id)
        |> List.filter (fun (_, r: Rect) -> x >= r.X && x < r.X + r.Width && y >= r.Y && y < r.Y + r.Height)
        |> List.sortBy (fun (_, r) -> r.Width * r.Height) // innermost wins (smallest area)
        |> List.tryHead
        |> Option.map fst

    // Feature 175 (FR-001): resolve each `Scroll` interaction to (scroll-viewer id, deltaY,
    // contentHeight, viewportHeight) so the host can advance its persistent offset (clamped). A scroll
    // over no scroll-viewer, or a viewer whose extent can't be measured, is dropped.
    let private resolveScrollDeltas (retained: RetainedRender<'msg>) (render: ControlRenderResult<'msg>) (interactions: PointerInteraction list) =
        interactions
        |> List.choose (fun interaction ->
            match interaction with
            | Scroll(_, _, deltaY, x, y) ->
                enclosingScrollViewer retained render x y
                |> Option.bind (fun svId ->
                    Control.scrollViewport render svId
                    |> Option.map (fun vp -> svId, deltaY, vp.ContentHeight, vp.Viewport.Height))
            | _ -> None)

    // Feature 110 (FR-001/FR-004/FR-006): the live retained pointer route. Same gesture fold as the
    // oracle, but over the retained frame's CACHED `LayoutResult` (no fresh layout eval) and resolving
    // each interaction from the retained frame (no fresh render). Returns the advanced PointerState, the
    // product messages, the summed `FullRenderFallbackCount`, and (feature 175) the resolved scroll deltas.
    let routeRetainedPointer
        (host: InteractiveAppHost<'model, 'msg>)
        (retained: RetainedRender<'msg>)
        (render: ControlRenderResult<'msg>)
        (state: PointerState)
        (size: Size)
        (model: 'model)
        (input: ViewerPointerInput)
        : PointerState * 'msg list * int * (ControlId * float * float * float) list =
        match Pointer.toMsg (pointerSampleOf input) with
        | None -> state, [], 0, []
        | Some pointerMsg ->
            let policy = FS.GG.UI.Layout.Defaults.pixelSnapPolicy 1.0

            // F2 (Feature 175 FR-009): resolve the offset-aware queryable layout through the SINGLE
            // seam `RetainedRender.hitTestLayout` (which re-applies the scroll-offset shift to the RAW
            // incremental-cache `retained.Layout`), so pointer hit-testing inside a scrolled region
            // resolves the control actually under the pointer — matching the painted (scrolled) position
            // and `resolveFocus`'s already-shifted node boxes. No caller re-derives the shift inline.
            let hitLayout = RetainedRender.hitTestLayout retained

            let state', interactions, _runtimeMessages =
                Pointer.update policy hitLayout pointerMsg state

            let mutable fallbacks = 0

            let messages =
                interactions
                |> List.collect (fun interaction ->
                    let msgs, fb = routeRetainedInteraction host size model retained render interaction
                    fallbacks <- fallbacks + fb
                    msgs)

            state', messages, fallbacks, resolveScrollDeltas retained render interactions

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
                        // Feature 232 (#44): the unified `Key ?? path` id (non-correctness label —
                        // TextInput.init only echoes it in effect payloads — moved for scheme consistency).
                        let controlId =
                            RetainedRender.retainedCanonicalId id retained
                            |> Option.defaultValue (node.Control.Key |> Option.defaultValue node.Control.Kind)
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
                              // Feature 184 (US3): edited text now rides the typed `Nav` as `EditedText`.
                              Nav = Some(EditedText model'.DraftText) })

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
        (nav: NavPayload)
        : 'msg list =
        bindings
        |> List.map (fun b ->
            b.Dispatch
                { Kind = kind
                  ControlId = Some nodeId
                  Origin = ControlEventOrigin.Keyboard
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
            dispatchNav
                (ownBindings |> List.filter (fun b -> b.EventKind = "changed"))
                nodeId
                "changed"
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
                    dispatchNav (selectionBindings ownBindings) nodeId "selected" (MovedSelection(clamped, Some itemId))

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
                        dispatchNav (selectionBindings ownBindings) nodeId "selected" (MovedCell(newRow, newCol))
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
            | Some node when node.Control.Kind = "canvas" && (canvasKeyHandler node.Control).IsSome ->
                // Feature 191 (US2, FR-007): a focused canvas bound to onKey receives the raw key +
                // modifiers BEFORE default navigation. routeFocusedKey carries only `shift`, so the other
                // modifiers are reported false here (the chord path carries the full set when wired).
                let handler = (canvasKeyHandler node.Control).Value
                let mods: KeyModifiers = { Ctrl = false; Alt = false; Shift = shift; Meta = false }
                retained, [], [ handler key mods ]
            | Some node ->
                // Feature 232 (#44): the focused node's UNIFIED full-tree id (`Key ?? path`), resolved
                // from its durable `RetainedId`, so it matches `Focus.order`/`traverse` AND the full-tree
                // event bindings below. Falls back to the node-local id only if the id is not resolvable.
                let nodeId =
                    RetainedRender.retainedCanonicalId id retained
                    |> Option.defaultValue (node.Control.Key |> Option.defaultValue node.Control.Kind)

                let keyboard =
                    node.Control.Accessibility
                    |> Option.map (fun m -> m.Keyboard)
                    |> Option.defaultValue
                        { Focusable = false
                          ActivationKeys = []
                          NavigationKeys = [] }

                let keyName, isTab = normalizeFocusKey key

                // The focused control's OWN authored bindings (a focusable composite is a single
                // stop, so descendant bindings are excluded by the id filter). Feature 232 (#44): filter
                // the FULL-TREE bindings (rooted at the real root "0") by the focused node's full-tree
                // `Key ?? path` id — the node-re-rooted `eventBindingsOf node.Control` keyed an UNKEYED
                // node's own binding at "0" while `nodeId` was its `Kind`, so nothing matched and the
                // keypress dispatched nothing. Keyed nodes are unaffected (Key wins in both framings).
                let ownBindings =
                    ControlInternals.eventBindingsOf retained.Root.Control
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
    let private ignoreRawPointer (_: ViewerPointerInput) (_: Size) (_: 'model) : 'msg list = []

    let runInteractiveAppWithLauncher
        (launch: ViewerOptions -> InteractiveViewerHost<'model, 'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>)
        (options: ViewerOptions)
        (mapRawPointer: ViewerPointerInput -> Size -> 'model -> 'msg list)
        (host: InteractiveAppHost<'model, 'msg>)
        =
        // Feature 182/issue #1046: the interpreter-edge cells and their pointer/diagnostic transitions
        // live behind the typed `FrameLoopLifecycle` seam. This remains runtime state, NOT the Elmish
        // `Model` (constitution IV).
        let loopState = FrameLoopLifecycle.create<'model, 'msg> ()

        let surface (diags: ControlDiagnostic list) =
            for d in diags do
                if FrameLoopLifecycle.surfaceDiagnosticOnce d loopState then
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
                // Feature 232 (#44): resolve the durable `RetainedId` to its unified `Key ?? path`
                // (not `Key ?? Kind`), so `FocusedControl` matches the re-pointed visual-state bridge
                // and an UNKEYED focused control gets the focus ring.
                match loopState.Focused, prior with
                | Some rid, Some r -> RetainedRender.retainedCanonicalId rid r
                | _ -> None

            { fst (ControlRuntime.init ()) with
                HoveredControl = loopState.PointerState.Hover
                PressedControls =
                    loopState.PointerState.Presses
                    |> Map.toList
                    |> List.map (fun (_, candidate) -> candidate.Control)
                    |> Set.ofList
                FocusedControl = focusedControlId
                // Feature 175: carry the host's persistent scroll offsets into the runtime model so the
                // scroll bridge (`applyScrollOffsets`) can stamp them onto the tree this frame.
                ScrollOffsets = loopState.ScrollOffsets }

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
            match loopState.LastView with
            | Some(cachedSize, cachedModel, cachedView) when cachedSize = size && obj.ReferenceEquals(model, cachedModel) ->
                RenderLagTrace.emit "elmish-product-view-cache-hit" []
                cachedView
            | _ ->
                let sw = System.Diagnostics.Stopwatch.StartNew()
                RenderLagTrace.emit "elmish-product-view-start" []
                let v = host.View size model
                sw.Stop()
                RenderLagTrace.emit
                    "elmish-product-view-end"
                    [ "durationMs", sw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture) ]
                loopState.LastView <- Some(size, model, v)
                v

        let renderRetained (size: Size) (model: 'model) : Scene =
            match loopState.Retained with
            | None ->
                let totalSw = System.Diagnostics.Stopwatch.StartNew()
                RenderLagTrace.emit "elmish-render-retained-start" [ "path", "init" ]
                let runtimeModel = assembleRuntimeModel None
                // First frame: no prior stamped tree to narrow against → full-tree oracle (FR-006).
                let stampSw = System.Diagnostics.Stopwatch.StartNew()
                let stamp = ControlRuntime.runtimeStampFor None runtimeModel (viewFor size model)
                stampSw.Stop()
                RenderLagTrace.emit
                    "elmish-runtime-stamp-end"
                    [ "path", "init"
                      "durationMs", stampSw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture) ]
                loopState.LastRuntimeModel <- Some runtimeModel
                // Feature 175: stamp live scroll offsets after the visual-state stamp (identity at rest).
                let stampedScene = ControlRuntime.applyScrollOffsets runtimeModel stamp.Stamped
                let initSw = System.Diagnostics.Stopwatch.StartNew()
                let r0 = RetainedRender.init host.Theme size stampedScene
                initSw.Stop()
                RenderLagTrace.emit
                    "elmish-retained-init-end"
                    [ "durationMs", initSw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture) ]
                surface r0.Diagnostics
                loopState.Retained <- Some r0.Retained
                loopState.LastRender <- Some r0.Render
                totalSw.Stop()
                RenderLagTrace.emit
                    "elmish-render-retained-end"
                    [ "path", "init"
                      "durationMs", totalSw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture) ]
                r0.Render.Scene
            | Some prev ->
                let totalSw = System.Diagnostics.Stopwatch.StartNew()
                let runtimeModel = assembleRuntimeModel (Some prev)
                // Feature 112 (FR-001/FR-002): on a model-unchanged repaint (the view cache would hit),
                // narrow the runtime-state stamp to only the changed identities via the TARGETED stamp,
                // reusing `prev.Root.Control` (the previous stamped tree). On a model-changing frame the
                // whole view is rebuilt anyway, so use the full-tree oracle (`prior = None`).
                let modelUnchanged =
                    match loopState.LastView with
                    | Some(cachedSize, cachedModel, _) -> cachedSize = size && obj.ReferenceEquals(model, cachedModel)
                    | None -> false

                RenderLagTrace.emit
                    "elmish-render-retained-start"
                    [ "path", "step"
                      "modelUnchanged", string modelUnchanged ]
                let fresh = viewFor size model

                let prior =
                    if modelUnchanged then
                        loopState.LastRuntimeModel |> Option.map (fun pm -> pm, prev.Root.Control)
                    else
                        None

                let stampSw = System.Diagnostics.Stopwatch.StartNew()
                let stamp = ControlRuntime.runtimeStampFor prior runtimeModel fresh
                stampSw.Stop()
                RenderLagTrace.emit
                    "elmish-runtime-stamp-end"
                    [ "path", "step"
                      "durationMs", stampSw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture) ]
                loopState.LastRuntimeModel <- Some runtimeModel
                // Feature 175: stamp live scroll offsets after the visual-state stamp (identity at rest).
                let stampedScene = ControlRuntime.applyScrollOffsets runtimeModel stamp.Stamped
                let stepSw = System.Diagnostics.Stopwatch.StartNew()
                let s = RetainedRender.step host.Theme size prev stampedScene
                stepSw.Stop()
                RenderLagTrace.emit
                    "elmish-retained-step-end"
                    [ "durationMs", stepSw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture)
                      "remeasured", string s.WorkReduction.RemeasuredNodeCount
                      "repainted", string s.WorkReduction.RepaintedNodeCount
                      "dirtyRects", string s.WorkReduction.DirtyRectCount
                      "replayHits", string s.WorkReduction.ReplayHits
                      "replayMisses", string s.WorkReduction.ReplayMisses ]
                surface s.Diagnostics
                loopState.LastWorkReduction <- Some s.WorkReduction
                loopState.Retained <- Some s.Retained
                loopState.LastRender <- Some s.Render
                totalSw.Stop()
                RenderLagTrace.emit
                    "elmish-render-retained-end"
                    [ "path", "step"
                      "durationMs", totalSw.Elapsed.TotalMilliseconds.ToString("0.###", Globalization.CultureInfo.InvariantCulture) ]
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
            // Feature 232 (#44): match each node by the unified `Key ?? path` (root "0", child i ->
            // path + "." + i) — the scheme `Focus.order`/`traverse` now produce — not `Key ?? Kind`,
            // so a traverse `next` id for an UNKEYED control resolves back to its `RetainedId`.
            let rec find (path: string) (n: RetainedNode<'msg>) =
                let nId = n.Control.Key |> Option.defaultValue path

                if nId = controlId then
                    Some n.Identity
                else
                    n.Children |> List.mapi (fun i c -> i, c) |> List.tryPick (fun (i, c) -> find (path + "." + string i) c)

            find "0" r.Root

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
            // Feature 186 (US1): delegate the 32-field construction to `buildFrameMetrics`. Values are
            // read into locals in the SAME order as the former record-field initialisation so the
            // live present-timing side effect (Feature 120) still runs between the work-reduction reads
            // and the replay reads — byte-identical to the hand-spelled record (FR-007).
            let geti (f: WorkReductionRecord -> int) = loopState.LastWorkReduction |> Option.map f |> Option.defaultValue 0
            let viewCalled = fullRenderFallbackCount > 0
            let remeasured = geti (fun w -> w.RemeasuredNodeCount)
            // Feature 113 (Phase 5): the last retained-step's memo tally (live `OnFrameMetrics` sink).
            let memo = geti (fun w -> w.MemoHits), geti (fun w -> w.MemoMisses)
            // Feature 114 (Phase 6): the last retained-step's virtualization tally (live sink).
            let virtual' = geti (fun w -> w.VirtualMaterialized), geti (fun w -> w.VirtualTotal)
            // Feature 116 (Phase 7): the last retained-step's damage + picture-cache tallies (live sink).
            let damage = geti (fun w -> w.RepaintedNodeCount), geti (fun w -> w.DirtyRectCount), geti (fun w -> w.DirtyArea)
            let picture = geti (fun w -> w.PictureCacheHits), geti (fun w -> w.PictureCacheMisses), geti (fun w -> w.PictureCacheEntryCount)
            // Feature 117 (Phase 8): the last retained-step's text-cache tally + dirty-set size (live sink).
            let textCache = geti (fun w -> w.TextMeasureCacheHits), geti (fun w -> w.TextMeasureCacheMisses)
            let layoutInvalidated = geti (fun w -> w.LayoutInvalidatedNodeCount)
            // Feature 120 (US1): live backend present timing (non-golden), read from the OpenGL host's
            // last present (one-frame lag, live diagnostic only); (US3) replay model counts.
            let paintDuration = (loopState.LastPresentTiming <- FS.GG.UI.SkiaViewer.Host.GlHost.lastPresentTiming(); fst loopState.LastPresentTiming)
            let composeDuration = snd loopState.LastPresentTiming
            let replay = geti (fun w -> w.ReplayHits), geti (fun w -> w.ReplayMisses), geti (fun w -> w.ReplayRecords), geti (fun w -> w.ReplaySkippedNodes), geti (fun w -> w.ReplayCacheNativeBytes)

            host.OnFrameMetrics(
                buildFrameMetrics
                    cause
                    productModelChanged
                    viewCalled
                    fullRenderFallbackCount
                    remeasured
                    viewCalled
                    false
                    false
                    samples
                    movesProcessed
                    fullRenderFallbackCount
                    duration
                    paintDuration
                    composeDuration
                    memo
                    virtual'
                    damage
                    picture
                    replay
                    textCache
                    layoutInvalidated)

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
            (match input.Phase, loopState.Retained with
             | ViewerPointerPhaseKind.Pressed, Some r ->
                 match resolveFocus r input.X input.Y with
                 | Some id ->
                     match tryFindNode id r.Root with
                     | Some node when
                         node.Control.Accessibility
                         |> Option.exists (fun m -> m.Keyboard.Focusable)
                         -> loopState.Focused <- Some id
                     | _ -> ()
                 | None -> ()
             | _ -> ())

            match loopState.Retained, loopState.LastRender with
            | Some r, Some render ->
                let state', messages, fallbacks, scrollDeltas = routeRetainedPointer host r render loopState.PointerState size model input
                loopState.PointerState <- state'
                // Feature 175 (FR-001/FR-002): advance the persistent scroll offset for each scrolled
                // viewer (re-clamped against the measured extent; wheel-down increases the offset). The
                // raw wheel delta is only a few units per notch, so scale it to a usable pixel step
                // (~3 units → ~48 px) — otherwise scrolling crawls.
                for (svId, deltaY, contentHeight, viewportHeight) in scrollDeltas do
                    let next =
                        loopState.ScrollOffsets
                        |> Map.tryFind svId
                        |> Option.defaultValue ScrollState.empty
                        |> ScrollState.withExtent contentHeight viewportHeight
                        |> ScrollState.applyScrollDelta (-deltaY * wheelScrollStep)
                    loopState.ScrollOffsets <- Map.add svId next loopState.ScrollOffsets
                // The viewer's pacing policy has already selected this sample. Preserve the Controls
                // route first, then offer that same folded sample to the raw fallback.
                messages @ mapRawPointer input size model, fallbacks
            | _ ->
                // No retained frame yet (a pointer sample before the first paint seeded the frame, not
                // expected in the live loop where paint precedes input): fall back to the preserved
                // oracle so routing is still correct, counting the full render it performs.
                let state', messages = routeInteractivePointer host loopState.PointerState size model input
                loopState.PointerState <- state'
                messages @ mapRawPointer input size model, 1

        // Feature 108 (US4, FR-011/012): pointer-move coalescing on the live loop. A MOVE sample is
        // buffered (latest position wins) and the PREVIOUSLY-buffered move is processed at the next
        // sample boundary — so a burst of K moves yields at most one processed move (one render +
        // hit-test) per boundary, while every discrete interaction (press/release/click/drag
        // begin/end/cancel/scroll/secondary) is processed in arrival order, never coalesced or
        // dropped.
        //
        // Issue #460: the predicate is `Coalescing.isCoalescibleSample` — the SAME definition
        // `Perf.runScript` uses, not a hand-copy of it. Note what is dropped here: a raw SAMPLE, before
        // the hit-test. `routeInteractivePointer` re-derives every interaction the surviving sample
        // implies, so coalescing on this side cannot lose a hover-leave or a drag-begin. `Perf` drops
        // already-derived interactions and therefore needs the stricter `isSupersedablePosition` rule;
        // `Coalescing.Parity` in Elmish.Tests gates the two against each other.
        let mapPointer (input: ViewerPointerInput) (size: Size) (model: 'model) : 'msg list =
            FrameLoopLifecycle.recordPointerSample loopState

            match pointerPhaseOf input.Phase with
            | phase when Coalescing.isCoalescibleSample phase ->
                // Process the previously-deferred move (≤1 per boundary), then defer this one. Feature
                // 110: a flushed move routes from the retained frame and performs ZERO routing renders
                // (FullRenderCount = 0) unless it must fall back; the first move of a burst defers
                // without processing (no emit).
                let sw = System.Diagnostics.Stopwatch.StartNew()

                let flushedMsgs, flushedFallbacks =
                    match FrameLoopLifecycle.takePendingMove loopState with
                    | Some prev ->
                        processInput prev size model
                    | None -> [], 0

                sw.Stop()
                FrameLoopLifecycle.deferMove input loopState

                // This Moved sample carries into the next frame's count; report the flushed move now.
                let samples = FrameLoopLifecycle.completeMoveBoundary loopState

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
                    match FrameLoopLifecycle.takePendingMove loopState with
                    | Some prev ->
                        true, processInput prev size model
                    | None -> false, ([], 0)

                let discreteMsgs, discreteFallbacks = processInput input size model
                sw.Stop()
                let samples = FrameLoopLifecycle.completeDiscreteBoundary loopState
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
            let sw = System.Diagnostics.Stopwatch.StartNew()

            let messages =
                if not pressed then
                    host.MapKey key false |> Option.toList
                else
                    match loopState.Retained with
                    | None -> chordFallthrough key
                    | Some r ->
                        let focusedNode = loopState.Focused |> Option.bind (fun id -> tryFindNode id r.Root)

                        // (1) E1 text seam — unchanged delivery for a focused text control's printable keys.
                        let textHandled =
                            match textMsgOfKey key, loopState.Focused, focusedNode with
                            | Some textMsg, Some id, Some node when isTextNode node ->
                                let r', msgs = routeFocusedText r (Some id) textMsg
                                loopState.Retained <- Some r'
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

                            let r', controlMsgs, productMsgs = routeFocusedKey r loopState.Focused order key shift
                            loopState.Retained <- Some r'

                            // Apply focus-update messages to the host's focus identity (map the next
                            // ControlId back to its stable RetainedId).
                            for cm in controlMsgs do
                                match cm with
                                | FocusControl next ->
                                    loopState.Focused <- next |> Option.bind (retainedIdOfControl r')
                                | _ -> ()

                            // (3) Fall through to the chord/`host.MapKey` seam only when nothing was consumed.
                            match productMsgs, controlMsgs with
                            | [], [] -> chordFallthrough key
                            | _ -> productMsgs

            sw.Stop()
            emitFrameMetrics FrameCause.Key 0 0 (not (List.isEmpty messages)) 0 sw.Elapsed
            messages

        // Feature 099 (R4): the host animation seam (contract C1). Each frame the viewer hands us the
        // injected per-frame `delta`; we ADVANCE every live per-identity clock in
        // `loopState.Retained.StateByIdentity` by it BEFORE the next `renderRetained` (which then paints
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
            match loopState.Retained with
            | Some r ->
                let advanced = RetainedRender.advanceStateClocks delta r.StateByIdentity

                if not (obj.ReferenceEquals(advanced, r.StateByIdentity)) then
                    loopState.Retained <- Some { r with StateByIdentity = advanced }
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
        runInteractiveAppWithLauncher Viewer.runInteractiveViewer options ignoreRawPointer host

    let runInteractiveAppWithGamepad
        (options: ViewerOptions)
        (gamepadHost: InteractiveAppGamepadHost<'model, 'msg>)
        =
        runInteractiveAppWithLauncher
            (fun launchOptions viewerHost ->
                Viewer.runInteractiveViewerWithGamepad
                    launchOptions
                    { Host = viewerHost
                      Gamepad = gamepadHost.Gamepad })
            options
            ignoreRawPointer
            gamepadHost.Host

    /// Issue #1159: compose the lower viewer's frame-paced input route with the retained Controls host.
    /// The raw callback sees only folded samples and is invoked after Controls hit-testing/bindings.
    let runInteractiveAppWithPointerPacing
        (options: ViewerOptions)
        (pointerPacing: ViewerPointerPacingOptions)
        (mapRawPointer: ViewerPointerInput -> Size -> 'model -> 'msg list)
        (host: InteractiveAppHost<'model, 'msg>)
        =
        runInteractiveAppWithLauncher
            (fun launchOptions viewerHost -> Viewer.runInteractiveViewerWithPointerPacing launchOptions pointerPacing viewerHost)
            options
            mapRawPointer
            host

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
            ignoreRawPointer
            host

    // Issue #429: audio reaches the Controls host family the same way window behavior does — by
    // swapping the terminal viewer launcher, NOT by forking `runInteractiveAppWithLauncher`. The
    // message→update→retained-step wiring below is therefore the exact same code path with or without
    // sound, so the two cannot drift apart.
    let runInteractiveAppWithAudio
        (options: ViewerOptions)
        (audioSink: AudioEffect list -> unit)
        (host: InteractiveAppHost<'model, 'msg>)
        =
        runInteractiveAppWithLauncher
            (fun launchOptions viewerHost -> Viewer.runInteractiveViewerWithAudio launchOptions audioSink viewerHost)
            options
            ignoreRawPointer
            host

    /// Issue #429: `runInteractiveAppWithAudio` with an explicit window behavior — the audio-capable
    /// sibling of `runInteractiveAppWithWindowBehavior`.
    let runInteractiveAppWithWindowBehaviorAndAudio
        (options: ViewerOptions)
        (behavior: ViewerWindowBehaviorRequest)
        (audioSink: AudioEffect list -> unit)
        (host: InteractiveAppHost<'model, 'msg>)
        =
        runInteractiveAppWithLauncher
            (fun launchOptions viewerHost ->
                Viewer.runInteractiveViewerWithWindowBehaviorAndAudio launchOptions behavior audioSink viewerHost)
            options
            ignoreRawPointer
            host

    /// Issue #641 — every sound request in an effect batch, flattened in dispatch order; non-audio
    /// effects are dropped. The Controls-family name for the narrowing the generated-app family has
    /// had since #245, so `… |> audioRequests |> Audio.interpret` yields the same `AudioEvidence` on
    /// both families and a product that changes host does not change its audio assertions.
    ///
    /// It DELEGATES rather than re-deriving `PlayAudio`: one implementation, two names. A second copy
    /// of a four-line `List.collect` is exactly the drift #429 had to unpick — there, the interactive
    /// fold had quietly stopped honouring `PlayAudio` while its twin still did, and nothing objected
    /// because each copy was locally correct. Pair it with `Perf.runScriptToEffects`, which is the
    /// headless fold that produces the `ViewerEffect list` this narrows.
    let audioRequests (effects: ViewerEffect list) : AudioEffect list =
        GeneratedAppHost.audioRequests effects

    module Live =
        let internal runDeterministicPointerPacing policy receivedAt frames =
            Viewer.runDeterministicPacing policy receivedAt frames

        /// Issue #1159: deterministic counterpart of the live paced launcher.  It deliberately uses
        /// the Viewer queue/drain runner, then feeds every drained sample through the normal Controls
        /// pointer router plus the additive raw fallback before folding its messages through `Update`.
        /// This gives product tests a no-window receipt for raw/folded/update/present counters while
        /// retaining authored bindings and discrete ordering.
        let internal runDeterministicPointerPacingThroughControls
            (policy: ViewerContinuousPointerPolicy)
            (receivedAt: DateTimeOffset)
            (size: Size)
            (mapRawPointer: ViewerPointerInput -> Size -> 'model -> 'msg list)
            (host: InteractiveAppHost<'model, 'msg>)
            (frames: ViewerPointerInput list list)
            : DeterministicPointerPacingResult<'model> =
            let payloads = System.Collections.Generic.Dictionary<string, ViewerPointerInput>()
            let mutable nextPayload = 0
            let queueFrames =
                frames
                |> List.map (fun samples ->
                    samples
                    |> List.map (fun input ->
                        let payload = string nextPayload
                        nextPayload <- nextPayload + 1
                        payloads.Add(payload, input)
                        let kind =
                            match input.Phase with
                            | ViewerPointerPhaseKind.Moved -> ViewerResponsivenessInputKind.PointerMove
                            | _ -> ViewerResponsivenessInputKind.PointerDiscrete
                        kind, payload))

            let drains = Viewer.runDeterministicPacing policy receivedAt queueFrames
            let mutable model, _effects = host.Init()
            let mutable pointerState = Pointer.init ()
            let metrics = ResizeArray<ViewerPointerPacingMetrics>()

            drains
            |> List.iteri (fun frameIndex drain ->
                let inputs =
                    drain.DiscreteInputs
                    @ (drain.CoalescedPointer |> Option.toList)
                    |> List.map (fun envelope -> payloads.[envelope.Payload])
                let mutable updateCount = 0
                for input in inputs do
                    let nextState, messages =
                        routeInteractivePointerWithRawFallback host mapRawPointer pointerState size model input
                    pointerState <- nextState
                    for message in messages do
                        let nextModel, _ = host.Update message model
                        model <- nextModel
                        updateCount <- updateCount + 1
                let repaintCause =
                    match drain.DiscreteInputs.IsEmpty, drain.CoalescedPointer.IsSome with
                    | false, true -> ViewerPointerRepaintCause.MixedPointer
                    | false, false -> ViewerPointerRepaintCause.DiscretePointer
                    | true, true -> ViewerPointerRepaintCause.ContinuousPointer
                    | true, false -> ViewerPointerRepaintCause.ContinuousPointer
                metrics.Add
                    { RawSamplesReceived = frames.[frameIndex].Length
                      FoldedSamplesApplied = inputs.Length
                      CoalescedSamples = drain.CoalescedMovementCount
                      ModelUpdates = updateCount
                      PresentedFrames = int64 (frameIndex + 1)
                      RepaintCause = repaintCause
                      FullRenderFallbacks = 0 })

            { Model = model
              Drains = drains
              Metrics = metrics |> Seq.toList }

        /// Issue #1159: drive a raw viewer script through the public paced Controls launcher.
        let runPointerPacingScript options pointerPacing mapRawPointer host script =
            let observed = ResizeArray<FrameMetrics>()
            let observingHost = { host with OnFrameMetrics = fun metric -> observed.Add metric; host.OnFrameMetrics metric }

            match
                runInteractiveAppWithLauncher
                    (fun launchOptions viewerHost ->
                        Viewer.runInteractiveViewerScriptWithPointerPacing launchOptions pointerPacing script viewerHost)
                    options
                    mapRawPointer
                    observingHost
            with
            | Result.Ok outcome -> Result.Ok { Outcome = outcome; Metrics = observed |> Seq.toList }
            | Result.Error failure -> Result.Error failure

        let private viewerButton button =
            match button with
            | PointerButton.Primary -> ViewerPointerButtonKind.Primary
            | PointerButton.Secondary -> ViewerPointerButtonKind.Secondary
            | PointerButton.Middle -> ViewerPointerButtonKind.Middle

        let private pointer phase x y button deltaX deltaY =
            ViewerScriptInput.Pointer
                { Phase = phase
                  X = x
                  Y = y
                  Button = button
                  DeltaX = deltaX
                  DeltaY = deltaY }

        let private moved x y =
            pointer ViewerPointerPhaseKind.Moved x y None 0.0 0.0

        let private pressed button x y =
            pointer ViewerPointerPhaseKind.Pressed x y (Some(viewerButton button)) 0.0 0.0

        let private released button x y =
            pointer ViewerPointerPhaseKind.Released x y (Some(viewerButton button)) 0.0 0.0

        let private wheel deltaX deltaY x y =
            pointer ViewerPointerPhaseKind.Wheel x y None deltaX deltaY

        let private keyWithModifiers key (mods: KeyModifiers) =
            let prefixes =
                [ if mods.Ctrl then "Ctrl"
                  if mods.Alt then "Alt"
                  if mods.Shift then "Shift"
                  if mods.Meta then "Meta" ]

            match prefixes with
            | [] -> key
            | values -> ViewerKey.Unknown(String.concat "+" (values @ [ ViewerKeyboard.toKeyId key ]))

        let private interactionToScript interaction =
            match interaction with
            | HoverEnter(_, x, y) -> [ moved x y ]
            | HoverLeave _ -> [ pointer ViewerPointerPhaseKind.Exited 0.0 0.0 None 0.0 0.0 ]
            | PressedDown(_, button, x, y) -> [ pressed button x y ]
            | ReleasedUp(_, button, x, y) -> [ released button x y ]
            | Click(_, button, x, y) -> [ pressed button x y; released button x y ]
            | DragBegin(_, button, startX, startY) -> [ pressed button startX startY ]
            | DragMove(_, button, x, y) ->
                let startX = max 0.0 (x - 24.0)
                [ pressed button startX y; moved x y; released button x y ]
            | DragEnd(_, button, x, y) -> [ released button x y ]
            | DragCancelled _ -> [ ViewerScriptInput.WaitFrame ]
            | Scroll(_, deltaX, deltaY, x, y) -> [ wheel deltaX deltaY x y ]
            | FocusMovedByPointer _ -> [ ViewerScriptInput.WaitFrame ]
            | Diagnostic _ -> [ ViewerScriptInput.WaitFrame ]

        let private frameInputToScript input =
            match input with
            | FrameInput.Key(key, mods) -> [ ViewerScriptInput.Key(keyWithModifiers key mods, true) ]
            | FrameInput.Pointer interaction -> interactionToScript interaction
            | FrameInput.Tick _
            | FrameInput.Idle -> [ ViewerScriptInput.WaitFrame ]

        let private toViewerScript script =
            script |> List.collect frameInputToScript

        // Issue #438: ONE scripted body, parameterised by the sink — deliberately not a sinkless copy
        // plus an audio copy. Two byte-identical folds are exactly what #429 had to unpick (the
        // interactive one had quietly stopped honouring `PlayAudio` while its twin still did), so the
        // metrics-observing wiring below is shared by the sinkless and audio-capable runners and cannot
        // drift between them. `ignore` is what makes the existing runners behave exactly as before.
        let private runScriptCore
            (options: ViewerOptions)
            (behavior: ViewerWindowBehaviorRequest)
            (audioSink: AudioEffect list -> unit)
            (host: InteractiveAppHost<'model, 'msg>)
            (script: FrameInput<'msg> list)
            =
            let observed = ResizeArray<FrameMetrics>()

            let observingHost =
                { host with
                    OnFrameMetrics =
                        fun metric ->
                            observed.Add metric
                            host.OnFrameMetrics metric }

            let viewerScript = toViewerScript script

            match
                runInteractiveAppWithLauncher
                    (fun launchOptions viewerHost ->
                        Viewer.runInteractiveViewerScriptWithWindowBehaviorAndAudio
                            launchOptions
                            behavior
                            viewerScript
                            audioSink
                            viewerHost)
                    options
                    ignoreRawPointer
                    observingHost
            with
            | Result.Ok outcome ->
                Result.Ok
                    { Outcome = outcome
                      Metrics = observed |> Seq.toList }
            | Result.Error failure -> Result.Error failure

        let runScriptWithWindowBehavior
            (options: ViewerOptions)
            (behavior: ViewerWindowBehaviorRequest)
            (host: InteractiveAppHost<'model, 'msg>)
            (script: FrameInput<'msg> list)
            =
            runScriptCore options behavior ignore host script

        /// Issue #438: the audio-capable sibling of `runScriptWithWindowBehavior`.
        let runScriptWithWindowBehaviorAndAudio
            (options: ViewerOptions)
            (behavior: ViewerWindowBehaviorRequest)
            (audioSink: AudioEffect list -> unit)
            (host: InteractiveAppHost<'model, 'msg>)
            (script: FrameInput<'msg> list)
            =
            runScriptCore options behavior audioSink host script

        let runScript
            (options: ViewerOptions)
            (host: InteractiveAppHost<'model, 'msg>)
            (script: FrameInput<'msg> list)
            =
            runScriptWithWindowBehavior options Viewer.defaultWindowBehavior host script

        /// Issue #438: the audio-capable sibling of `runScript`.
        let runScriptWithAudio
            (options: ViewerOptions)
            (audioSink: AudioEffect list -> unit)
            (host: InteractiveAppHost<'model, 'msg>)
            (script: FrameInput<'msg> list)
            =
            runScriptCore options Viewer.defaultWindowBehavior audioSink host script

    // Feature 108 (US3, FR-009/010): the pure, headless, deterministic frame driver. Nested in
    // `ControlsElmish` so it reuses the SAME message→update→retained-step + binding-resolution
    // primitives the live `runInteractiveApp` loop uses (`routeRetainedInteraction`,
    // `buildFrameMetrics`, `interpretPointerEffect`, `RetainedRender.step` are literally the same
    // functions), and the same `Coalescing` definition of what a move frame is.
    //
    // Issue #460 — the FRAME LOOP itself is not shared: this is an independent fold, and the live
    // loop's is another. What keeps the two honest is `Coalescing` (one definition, called by both)
    // plus the `Coalescing.Parity` test, which drives the real `Pointer.update` and fails if the two
    // sides ever disagree about which inputs a coalesced frame drops. Do not re-add a blanket "no
    // parallel logic" claim here: it was asserted for years over logic that WAS parallel, and it
    // retired exactly the suspicion that would have caught the hover-leave `Perf` was dropping.
    module Perf =
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
                | FrameInput.Pointer interaction when Coalescing.isMoveInteraction interaction -> current.Add input
                | other ->
                    flush ()
                    frames.Add [ other ]

            flush ()
            List.ofSeq frames

        /// Feature 186 (US2, FR-004/C-SCRIPT-STATE): the named per-frame metric carriers that
        /// `runScriptCore` threads from the last retained `step` into each frame's `FrameMetrics`
        /// (via `buildFrameMetrics`). Replaces the 7 loose `let mutable last*` bindings. `mutable`
        /// fields preserve the exact set/clear order (a frame that runs no render reports zeros).
        /// Internal by absence from `ControlsElmish.fsi`.
        type private FrameScriptState =
            { mutable LastMemo: int * int // mutable: hot path
              mutable LastVirtual: int * int // mutable: hot path
              mutable LastDamage: int * int * int // mutable: hot path
              mutable LastPicture: int * int * int // mutable: hot path
              mutable LastReplay: int * int * int * int * int // mutable: hot path
              mutable LastTextCache: int * int // mutable: hot path
              mutable LastInvalidated: int } // mutable: hot path

        // Issue #641: ONE fold, parameterised by the sink — the same shape the scripted Live runners
        // take (#438), and deliberately not a sinkless copy plus a recording copy: two folds that must
        // agree are two folds that drift, which is exactly what #429 had to unpick. `ignore` is what
        // makes `runScript`/`runScriptToModel` behave as before AND pay nothing for a recording they
        // would discard — `ViewerEffect` carries whole `SceneNode`s, so retaining every batch of a long
        // responsiveness script would hold scene graphs alive for the run.
        //
        // `recordEffects` sees `Init`'s batch first, then every `Update`'s, in dispatch order. Seeding
        // from `Init` is not a detail: a product that restores a saved volume at startup emits it THERE,
        // and that request-at-startup is the one the `Started` trap turns into silence.
        let private runScriptCore
            (recordEffects: ViewerEffect list -> unit)
            (host: InteractiveAppHost<'model, 'msg>)
            (size: Size)
            (script: FrameInput<'msg> list)
            : 'model * FrameMetrics list =
            let initModel, initEffects = host.Init()
            let mutable model = initModel
            recordEffects initEffects
            let mutable retained: RetainedRender<'msg> option = None
            // Feature 110 (FR-002): carry the retained frame's `ControlRenderResult` alongside the
            // threaded retained value, so a routed interaction reads `EventBindings`/`BoundIds` without a
            // fresh render. Kept in lock-step with `retained` by `renderStep`/`ensureRetained`.
            let mutable lastRender: ControlRenderResult<'msg> option = None
            // Feature 186 (US2): the last retained-step's metric carriers, named on `fs`. Each is
            // captured by `renderStep`/`repaintCached` and feeds the per-frame `buildFrameMetrics`;
            // all stay zero until a `step` runs (the first frame seeds via `init`, no work record):
            //   LastMemo (hits, misses) — Feature 113; LastVirtual (materialized, total) — Feature 114;
            //   LastDamage (repainted-node, dirty-rect, dirty-area) + LastPicture (hits, misses,
            //   entry-count) — Feature 116; LastReplay (hits, misses, records, skipped-nodes,
            //   native-bytes) — Feature 120; LastTextCache (hits, misses) + LastInvalidated
            //   (layout dirty-set size) — Feature 117.
            let fs =
                { LastMemo = 0, 0
                  LastVirtual = 0, 0
                  LastDamage = 0, 0, 0
                  LastPicture = 0, 0, 0
                  LastReplay = 0, 0, 0, 0, 0
                  LastTextCache = 0, 0
                  LastInvalidated = 0 }

            // Render the retained step for the current model, returning the frame's
            // RemeasuredNodeCount (the first frame seeds via `init`, which has no work record -> 0).
            let renderStep () : int =
                let next = host.View size model

                match retained with
                | None ->
                    let r0 = RetainedRender.init host.Theme size next
                    retained <- Some r0.Retained
                    lastRender <- Some r0.Render
                    fs.LastMemo <- 0, 0
                    fs.LastVirtual <- 0, 0
                    0
                | Some prev ->
                    let s = RetainedRender.step host.Theme size prev next
                    retained <- Some s.Retained
                    lastRender <- Some s.Render
                    fs.LastMemo <- s.WorkReduction.MemoHits, s.WorkReduction.MemoMisses
                    fs.LastVirtual <- s.WorkReduction.VirtualMaterialized, s.WorkReduction.VirtualTotal
                    fs.LastDamage <- s.WorkReduction.RepaintedNodeCount, s.WorkReduction.DirtyRectCount, s.WorkReduction.DirtyArea
                    fs.LastPicture <- s.WorkReduction.PictureCacheHits, s.WorkReduction.PictureCacheMisses, s.WorkReduction.PictureCacheEntryCount
                    fs.LastReplay <- s.WorkReduction.ReplayHits, s.WorkReduction.ReplayMisses, s.WorkReduction.ReplayRecords, s.WorkReduction.ReplaySkippedNodes, s.WorkReduction.ReplayCacheNativeBytes
                    fs.LastTextCache <- s.WorkReduction.TextMeasureCacheHits, s.WorkReduction.TextMeasureCacheMisses
                    fs.LastInvalidated <- s.WorkReduction.LayoutInvalidatedNodeCount
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
                    fs.LastMemo <- s.WorkReduction.MemoHits, s.WorkReduction.MemoMisses
                    fs.LastVirtual <- s.WorkReduction.VirtualMaterialized, s.WorkReduction.VirtualTotal
                    fs.LastDamage <- s.WorkReduction.RepaintedNodeCount, s.WorkReduction.DirtyRectCount, s.WorkReduction.DirtyArea
                    fs.LastPicture <- s.WorkReduction.PictureCacheHits, s.WorkReduction.PictureCacheMisses, s.WorkReduction.PictureCacheEntryCount
                    fs.LastReplay <- s.WorkReduction.ReplayHits, s.WorkReduction.ReplayMisses, s.WorkReduction.ReplayRecords, s.WorkReduction.ReplaySkippedNodes, s.WorkReduction.ReplayCacheNativeBytes
                    fs.LastTextCache <- s.WorkReduction.TextMeasureCacheHits, s.WorkReduction.TextMeasureCacheMisses
                    fs.LastInvalidated <- s.WorkReduction.LayoutInvalidatedNodeCount
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

            // Issue #641: the ONE place this fold calls `host.Update`, so recording the batch here
            // records every message the script dispatches — keyed, ticked, or pointer-routed — and no
            // future frame kind can add a dispatch that silently escapes the recording.
            let applyMessages (msgs: 'msg list) =
                model <-
                    msgs
                    |> List.fold
                        (fun acc msg ->
                            let next, produced = host.Update msg acc
                            recordEffects produced
                            next)
                        model

            // Feature 110: route a single pointer interaction to product messages from the RETAINED
            // frame — NO `host.View` + `Control.renderTree` for routing. Returns (messages, fallback
            // count). A binding-eligible `Click` is resolved via `routeRetainedInteraction` (which seeds
            // the retained frame if needed and counts a real oracle fallback); every other interaction
            // resolves directly to `host.MapPointer` (the oracle's non-`Click` path) with no hit-test and
            // no render, so a hover/drag burst performs zero routing full renders.
            let routeInteraction (interaction: PointerInteraction) : 'msg list * int =
                // Issue #457: the MapPointer fallback, with the frame's id set when we have one — this is
                // the headless SCRIPTED route, the one where a hand-written `Click(id, …)` naming a
                // control that does not exist used to be indistinguishable from no input at all. Before
                // the first frame is drawn there is no id set to check against, so only the interpreter's
                // own diagnostics can be routed; they are no longer dropped either way.
                let fallback () =
                    match lastRender with
                    | Some render -> routePointerFallback host.Diagnostics host.MapPointer render interaction
                    | None ->
                        interpretPointerEffect host.MapPointer interaction
                        |> routeAdapterDiagnostics host.Diagnostics

                match interaction with
                | Click _ ->
                    ensureRetained ()

                    match retained, lastRender with
                    | Some r, Some render -> routeRetainedInteraction host size model r render interaction
                    | _ -> fallback (), 0
                | _ -> fallback (), 0

            // Feature 109 (US1): did a product message actually change the model across the fold? An
            // empty message list never changes it; a non-empty list changed it iff the folded model's
            // reference identity moved (`obj.ReferenceEquals` — honest for both reference-type models,
            // where an idempotent `update` returns the same instance → `false`, and value-type models,
            // where the guard keeps a no-message frame `false`). No `'model` equality constraint added.
            let productModelChanged (before: 'model) (after: 'model) (msgs: 'msg list) : bool =
                not (List.isEmpty msgs) && not (obj.ReferenceEquals(before, after))

            // Feature 186 (US1): the all-zero seed for the per-frame `{ zero with … }` partials below.
            // Built via the single `buildFrameMetrics` site (FR-001); timing is live-only (Zero on the
            // deterministic path) and replay counts come from the per-frame `fs.LastReplay` model set by
            // `renderStep`/`repaintCached`. Byte-identical to the former hand-spelled record (FR-007).
            let zero =
                buildFrameMetrics
                    FrameCause.Idle
                    false
                    false
                    0
                    0
                    false
                    false
                    false
                    0
                    0
                    0
                    TimeSpan.Zero
                    TimeSpan.Zero
                    TimeSpan.Zero
                    (0, 0)
                    (0, 0)
                    (0, 0, 0)
                    (0, 0, 0)
                    (0, 0, 0, 0, 0)
                    (0, 0)
                    0

            toFrames script
            |> List.map (fun frame ->
                // Feature 113 (Phase 5): clear the per-frame memo tally before processing this frame, so a
                // frame that runs no render reports 0/0 (the previous frame's render must not bleed
                // through). `renderStep`/`repaintCached` overwrite it when they actually run.
                fs.LastMemo <- 0, 0
                fs.LastVirtual <- 0, 0
                fs.LastDamage <- 0, 0, 0
                fs.LastPicture <- 0, 0, 0
                fs.LastReplay <- 0, 0, 0, 0, 0
                fs.LastTextCache <- 0, 0
                fs.LastInvalidated <- 0

                match frame with
                | FrameInput.Pointer _ :: _ when frame |> List.forall (function
                                                                       | FrameInput.Pointer p -> Coalescing.isMoveInteraction p
                                                                       | _ -> false) ->
                    // Coalesced move frame: K samples, ONE processed move. Feature 110: the move routes
                    // from the retained frame and performs ZERO routing full renders (`FullRenderCount`
                    // no longer counts a routing render); only a model-driven re-render (`hasMsgs`) or a
                    // counted oracle fallback adds to it.
                    //
                    // Issue #460: "one processed move" means one surviving SAMPLE, not one surviving
                    // interaction. This used to route `List.last frame` and discard the rest, which
                    // silently annihilated a `HoverLeave` that was not last — and `[HoverLeave prior;
                    // HoverEnter next]` is precisely what `Pointer.update` emits, as a pair, for a move
                    // that changes the hit. The live host routes both (it re-derives them from the one
                    // surviving sample); `Perf` dropped the leave, so hover-out never fired headlessly
                    // and the suite stayed green. `routedInCoalescedFrame` drops only SUPERSEDED
                    // POSITIONS — the interaction-level image of the samples the live loop drops.
                    let k = List.length frame
                    let before = model

                    let routed =
                        frame
                        |> List.choose (function
                            | FrameInput.Pointer interaction -> Some interaction
                            | _ -> None)
                        |> Coalescing.routedInCoalescedFrame

                    // Route every surviving interaction in arrival order, summing the per-interaction
                    // fallback counts — the same shape `routeInteractivePointer` uses for the several
                    // interactions ONE live sample derives: they all route against the SAME retained
                    // frame, and the messages are applied afterwards, once.
                    let results = routed |> List.map routeInteraction // 0 routing renders
                    let msgs = results |> List.collect fst
                    let fallbacks = results |> List.sumBy snd

                    let hasMsgs = not (List.isEmpty msgs)
                    applyMessages msgs
                    let remeasured = if hasMsgs then renderStep () else 0 // step render iff a message rebuilt
                    let fullRenderCount = fallbacks + (if hasMsgs then 1 else 0)

                    { zero with
                        ProductModelChanged = productModelChanged before model msgs
                        ViewCalled = fullRenderCount > 0
                        FullRenderCount = fullRenderCount
                        RemeasuredNodeCount = remeasured
                        MemoHitCount = fst fs.LastMemo
                        MemoMissCount = snd fs.LastMemo
                        VirtualItemsMaterialized = fst fs.LastVirtual
                        VirtualItemsTotal = snd fs.LastVirtual
                        RepaintedNodeCount = (let (r, _, _) = fs.LastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = fs.LastDamage in rc)
                        DirtyArea = (let (_, _, da) = fs.LastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = fs.LastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = fs.LastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = fs.LastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = fs.LastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = fs.LastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = fs.LastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = fs.LastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = fs.LastReplay in b)
                        TextMeasureCacheHitCount = fst fs.LastTextCache
                        TextMeasureCacheMissCount = snd fs.LastTextCache
                        LayoutInvalidatedNodeCount = fs.LastInvalidated
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
                        MemoHitCount = fst fs.LastMemo
                        MemoMissCount = snd fs.LastMemo
                        VirtualItemsMaterialized = fst fs.LastVirtual
                        VirtualItemsTotal = snd fs.LastVirtual
                        RepaintedNodeCount = (let (r, _, _) = fs.LastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = fs.LastDamage in rc)
                        DirtyArea = (let (_, _, da) = fs.LastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = fs.LastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = fs.LastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = fs.LastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = fs.LastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = fs.LastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = fs.LastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = fs.LastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = fs.LastReplay in b)
                        TextMeasureCacheHitCount = fst fs.LastTextCache
                        TextMeasureCacheMissCount = snd fs.LastTextCache
                        LayoutInvalidatedNodeCount = fs.LastInvalidated
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
                        MemoHitCount = fst fs.LastMemo
                        MemoMissCount = snd fs.LastMemo
                        VirtualItemsMaterialized = fst fs.LastVirtual
                        VirtualItemsTotal = snd fs.LastVirtual
                        RepaintedNodeCount = (let (r, _, _) = fs.LastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = fs.LastDamage in rc)
                        DirtyArea = (let (_, _, da) = fs.LastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = fs.LastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = fs.LastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = fs.LastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = fs.LastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = fs.LastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = fs.LastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = fs.LastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = fs.LastReplay in b)
                        TextMeasureCacheHitCount = fst fs.LastTextCache
                        TextMeasureCacheMissCount = snd fs.LastTextCache
                        LayoutInvalidatedNodeCount = fs.LastInvalidated
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
                        MemoHitCount = fst fs.LastMemo
                        MemoMissCount = snd fs.LastMemo
                        VirtualItemsMaterialized = fst fs.LastVirtual
                        VirtualItemsTotal = snd fs.LastVirtual
                        RepaintedNodeCount = (let (r, _, _) = fs.LastDamage in r)
                        DirtyRectCount = (let (_, rc, _) = fs.LastDamage in rc)
                        DirtyArea = (let (_, _, da) = fs.LastDamage in da)
                        PictureCacheHitCount = (let (h, _, _) = fs.LastPicture in h)
                        PictureCacheMissCount = (let (_, m, _) = fs.LastPicture in m)
                        PictureCacheEntryCount = (let (_, _, e) = fs.LastPicture in e)
                        ReplayHitCount = (let (h, _, _, _, _) = fs.LastReplay in h)
                        ReplayMissCount = (let (_, m, _, _, _) = fs.LastReplay in m)
                        ReplayRecordCount = (let (_, _, r, _, _) = fs.LastReplay in r)
                        ReplaySkippedNodeCount = (let (_, _, _, s, _) = fs.LastReplay in s)
                        ReplayCacheNativeBytes = (let (_, _, _, _, b) = fs.LastReplay in b)
                        TextMeasureCacheHitCount = fst fs.LastTextCache
                        TextMeasureCacheMissCount = snd fs.LastTextCache
                        LayoutInvalidatedNodeCount = fs.LastInvalidated
                        PointerSamplesReceived = 1
                        FullRenderFallbackCount = fallbacks
                        FrameCause = FrameCause.PointerDiscrete
                        DiffRan = hasMsgs
                        LayoutRan = remeasured > 0
                        PaintRan = hasMsgs }
                | _ -> zero)
            // `List.map` is eager, so the mutable `model` now holds the FINAL post-script model.
            |> fun frames -> model, frames

        /// Fold an ordered `FrameInput` script over the host's pure `Update` + `RetainedRender.step`,
        /// returning the per-frame `FrameMetrics` (consecutive pointer-MOVE inputs coalesce into one
        /// frame). Pure, headless, byte-stable in its count/bool fields (SC-003/004/005).
        let runScript host size script : FrameMetrics list =
            runScriptCore ignore host size script |> snd

        /// As `runScript`, but also returns the FINAL folded model. Lets a caller render the
        /// POST-interaction frame — e.g. capture an offscreen screenshot of the scene AFTER a
        /// scroll/hover/focus/click script — closing the "drive interaction → see resulting frame"
        /// loop without a live window (Feature 175 S1). Same pure, headless, byte-stable fold.
        let runScriptToModel host size script : 'model * FrameMetrics list =
            runScriptCore ignore host size script

        /// Issue #641: as `runScriptToModel`, but ALSO returns every `ViewerEffect` the script's
        /// `Init` and `Update` calls REQUESTED, in dispatch order (`Init`'s batch first). Same pure,
        /// headless fold — no window, no device, no GL.
        ///
        /// This is the Controls family's record-only assertion path, and it exists because the model
        /// cannot answer the question. A restored volume the mixer was never TOLD about is
        /// indistinguishable, from inside the model, from one that was applied — so the `Started` trap
        /// is not merely untested on this family without it, it is structurally uncatchable. Assert at
        /// the sink, not at the model:
        ///
        ///     let _, effects, _ = Perf.runScriptToEffects host size script
        ///     effects |> audioRequests |> Audio.interpret    // AudioEvidence, same as the game family
        ///
        /// REQUESTED, not PERFORMED — the list is what the product ASKED FOR. Nothing here interprets
        /// it, and the Controls host does not honour all of it: a `Persist` is DROPPED live, with a
        /// warning diagnostic, because this family owns no persistence seam (#535). Seeing one recorded
        /// here is not evidence that it would happen.
        ///
        /// It is also NOT a claim of frame-for-frame parity with the live loop. What holds is what this
        /// fold already guarantees (see the `Perf` module comment): no state transition is lost, and the
        /// `Update` calls a script drives are the product's own. The two loops coalesce different
        /// alphabets — a MOVE-derived request may be counted differently live — so do not read this as
        /// "what the live sink would receive, effect for effect". What it IS: the product's real fold,
        /// not a test-local re-derivation of it. That is the point — a hand-rolled fold in a test
        /// asserts what the TEST does, and the bug being hunted is the product loop doing something else.
        let runScriptToEffects host size script : 'model * ViewerEffect list * FrameMetrics list =
            let requested = ResizeArray<ViewerEffect>()
            let model, frames = runScriptCore (fun batch -> requested.AddRange batch) host size script
            model, List.ofSeq requested, frames
