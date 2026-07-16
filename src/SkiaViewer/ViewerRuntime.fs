namespace FS.GG.UI.SkiaViewer

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text.Json
open System.Threading
open Elmish
open FS.GG.Audio.Core
open FS.GG.UI.Canvas
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open SkiaSharp
open Silk.NET.Input
open Silk.NET.Maths
open Silk.NET.Windowing
// Viewer.Types.fs carved the public type block out of this file; re-open the package namespace
// AFTER the third-party opens so the Viewer types/DU-cases win unqualified-name resolution exactly
// as they did when defined in-file (record-field proximity + DU-case-vs-opened-type; byte-stable).
open FS.GG.UI.SkiaViewer

type private LegacyHostMsg<'msg> =
    | LegacyLoaded
    | LegacyUpdateTick of float
    | LegacyRenderTick of float
    | LegacyKey of rawKey: string * isDown: bool
    | LegacyPointer of ViewerPointerInput
    | LegacyResized of Size
    | LegacyFramebufferResized of Size
    | LegacyCloseRequested
    | LegacyDiagnosticReported of Host.RenderDiagnostic
    | LegacyHostEffect of Host.ViewerEffect<LegacyHostMsg<'msg>>
    | LegacyAppMsg of 'msg

type private LegacyQueuedInput =
    | QueuedLegacyKey of rawKey: string * isDown: bool
    | QueuedLegacyPointer of ViewerPointerInput

module internal ViewerRuntime =
    open ViewerEvidence
    open ViewerLaunchSupport

    let timingPathToken path =
        match path with
        | ViewerTimingPath.FullRedraw -> "full-redraw"
        | ViewerTimingPath.DamageScoped -> "damage-scoped"

    let timingPathCanSupportClaim path proofReadbackIncluded validationReadbackIncluded =
        match path with
        | ViewerTimingPath.FullRedraw
        | ViewerTimingPath.DamageScoped -> not proofReadbackIncluded && not validationReadbackIncluded

    let damageDecisionToken decision =
        match decision with
        | ViewerDamageDecision.DamageScopedAccepted -> "damage-scoped-accepted"
        | ViewerDamageDecision.FullRedraw -> "full-redraw"
        | ViewerDamageDecision.SkipNoChange -> "skip-no-change"
        | ViewerDamageDecision.Rejected -> "rejected"
        | ViewerDamageDecision.EnvironmentLimited -> "environment-limited"

    let shouldCaptureDiagnostic options diagnostic =
        DiagnosticsFiltering.shouldCapture options diagnostic

    let captureDiagnostic options diagnostic =
        DiagnosticsFiltering.capture options diagnostic

    /// Issue #365: the counterpart to the GL host's `Diagnostics.productStepFailed`, for the presented
    /// interactive host. Staged at `App` (application code) — never `Frame`/render — so a throwing
    /// product step is not mislabeled a render failure.
    let productDefectDiagnostic (phase: string) (message: string) : ViewerDiagnosticEvent =
        { Level = ViewerDiagnosticLevel.Error
          Category = ViewerDiagnosticCategory.Scene
          Message =
            sprintf
                "Product %s raised an exception (%s). The input was dropped and the persistent window kept alive."
                phase
                message
          FrameIndex = None
          Stage = Some ViewerRunBlockedStage.App
          Elapsed = None }

    /// Issue #365: guard a presented-host product `Update`/`View`. A throwing step used to escape the
    /// Silk callback and tear the persistent window down (mislabeled `frameRenderFailed` by the GL
    /// host's outer handler). Here the step is dropped, reported as an `App`-stage defect, and the
    /// window kept alive on its last-good model/scene. A product-code fault is deterministic, so the
    /// same input is not retried.
    let tryProductStep (report: ViewerDiagnosticEvent -> unit) (phase: string) (step: unit -> 'a) : 'a option =
        try
            Some(step ())
        with ex ->
            report (productDefectDiagnostic phase ex.Message)
            None

    let private dispatchDiagnostic options (diagnostic: ViewerDiagnosticEvent) =
        captureDiagnostic options diagnostic |> Option.defaultValue diagnostic

    let defaultDiagnostics =
        { MinimumLevel = ViewerDiagnosticLevel.Info
          Categories =
            Set.ofList
                [ ViewerDiagnosticCategory.Startup
                  ViewerDiagnosticCategory.Input
                  ViewerDiagnosticCategory.EnvironmentSession
                  ViewerDiagnosticCategory.Renderer
                  ViewerDiagnosticCategory.OpenGl
                  ViewerDiagnosticCategory.Skia
                  ViewerDiagnosticCategory.Framebuffer
                  ViewerDiagnosticCategory.Scene
                  ViewerDiagnosticCategory.Screenshot ]
          FrameLogLimit = Some 0
          Sink = None
          Verbose = false }

    let defaultResponsivenessBudget =
        { InputReceiptP95 = TimeSpan.FromMilliseconds 4.0
          InputReceiptMax = TimeSpan.FromMilliseconds 16.0
          InputToVisibleP95 = TimeSpan.FromMilliseconds 50.0
          InputToVisibleMax = TimeSpan.FromMilliseconds 150.0
          LongFrameThreshold = TimeSpan.FromMilliseconds 50.0 }

    let defaultResponsivenessOptions =
        { Enabled = false
          RunId = None
          OutputRoot = None
          Budget = defaultResponsivenessBudget
          Sink = None }

    let responsivenessInputKindToken kind = ViewerResponsiveness.responsivenessInputKindToken kind

    let responsivenessVisibleResponseToken response =
        ViewerResponsiveness.responsivenessVisibleResponseToken response

    let responsivenessEnvironmentStatusToken status =
        ViewerResponsiveness.responsivenessEnvironmentStatusToken status

    let responsivenessReadinessToken readiness = ViewerResponsiveness.responsivenessReadinessToken readiness

    let emptyInputQueue = ViewerInputQueueOps.emptyInputQueue

    let inputQueueDepth queue = ViewerInputQueueOps.inputQueueDepth queue

    let enqueueInput receivedAt inputKind payload queue =
        ViewerInputQueueOps.enqueueInput receivedAt inputKind payload queue

    let drainInputQueue batchId drainReason queue =
        ViewerInputQueueOps.drainInputQueue batchId drainReason queue

    let dirtyState
        productModelChanged
        runtimeStateChanged
        sizeChanged
        themeChanged
        (dirtyRegion: ViewerResponsivenessDirtyRegion option)
        reason
        =
        ViewerInputQueueOps.dirtyState productModelChanged runtimeStateChanged sizeChanged themeChanged dirtyRegion reason

    let dirtyStateRequiresRecompose dirty = ViewerInputQueueOps.dirtyStateRequiresRecompose dirty

    /// F1 (Feature 175 general repaint signal): the single "runtime-state changed → repaint" policy,
    /// shared by EVERY viewer loop. After an input, if it produced product messages then
    /// `dispatchHostMsg` already re-derived the scene from the new model; if it produced NONE, runtime
    /// state (focus traversal, hover, scroll offsets) may still have changed with NO model change, so
    /// re-derive from `host.View` — the single source reflecting model + every runtime ref — so the
    /// change renders on THIS input, not the next (the "focus one click behind" / dead-hover /
    /// dead-scroll class). Centralizing it here keeps the key-only and full-interactive loops from
    /// drifting: previously only the full-interactive loop refreshed, so the key-only loop silently
    /// reintroduced the one-frame-behind bug for focus/scroll keys.
    let internal runtimeStateRepaint (producedMessages: bool) (current: 'scene) (deriveScene: unit -> 'scene) : 'scene =
        if producedMessages then
            current
        else
            RenderLagTrace.emit "runtime-state-repaint" [ "cause", "no-message-input" ]
            deriveScene ()

    // S3 (Feature 175): the structured live-trace read-back path, surfaced as plain `(event, fields)`
    // tuples so a test or tool can observe live state programmatically — no env var, no repack.
    let internal traceStartCapture () = RenderLagTrace.startCapture ()
    let internal traceDrainCapture () : (string * (string * string) list) list =
        RenderLagTrace.drainCapture () |> List.map (fun e -> e.Event, e.Fields)
    let internal traceEmit (eventName: string) (fields: (string * string) list) = RenderLagTrace.emit eventName fields

    let createResponsivenessRunId () = ViewerResponsiveness.createResponsivenessRunId ()

    let latencyRecordToJsonLine (latency: ViewerLatencyRecord) =
        ViewerResponsiveness.latencyRecordToJsonLine latency

    let summarizeResponsivenessRecords
        (runId: string)
        (scope: string)
        (recordsPath: string)
        (startedUtc: DateTimeOffset)
        (completedUtc: DateTimeOffset)
        (budget: ViewerResponsivenessBudget)
        (records: ViewerLatencyRecord list)
        : ViewerResponsivenessSummary
        =
        ViewerResponsiveness.summarizeResponsivenessRecords runId scope recordsPath startedUtc completedUtc budget records

    let responsivenessSummaryToJson (summary: ViewerResponsivenessSummary) =
        ViewerResponsiveness.responsivenessSummaryToJson summary

    let responsivenessSummaryToMarkdown (summary: ViewerResponsivenessSummary) =
        ViewerResponsiveness.responsivenessSummaryToMarkdown summary

    let writeResponsivenessRun (outputRoot: string) (summary: ViewerResponsivenessSummary) (records: ViewerLatencyRecord list) =
        ViewerResponsiveness.writeResponsivenessRun outputRoot summary records
    let defaultWindowBehavior =
        { ResizePolicy = Resizable
          MaximizePolicy = Maximizable
          StartupState = ViewerWindowStartupState.WindowedFullscreen
          StartupPosition = Some Centered
          BackendPreference = Some DefaultBackend }

    let validateWindowBehavior request =
        WindowBehaviorValidation.validateBehavior request

    let validateWindowLaunchBehavior (initialSize: Size) request =
        WindowBehaviorValidation.validateLaunch initialSize request

    let classifyWindowState diagnostic =
        let clientSizePositive =
            match diagnostic.ClientSize with
            | Some size ->
                let parts = size.Split('x', 'X')

                if parts.Length = 2 then
                    match Int32.TryParse parts.[0], Int32.TryParse parts.[1] with
                    | (true, width), (true, height) -> width > 0 && height > 0
                    | _ -> false
                else
                    false
            | None -> true

        match diagnostic.FailureClass with
        | Some failureClass when failureClass = "environment-session" || failureClass = "unsupported-host" -> Unsupported
        | _ ->
            match diagnostic.Visible with
            | ViewerObservedValue.Unsupported -> Unsupported
            | ViewerObservedValue.Observed true ->
                let hasNativeWindow =
                    diagnostic.WindowInitialized
                    && diagnostic.NativeHandle <> ViewerObservedValue.Observed false

                let accessible =
                    hasNativeWindow
                    && diagnostic.Focusable <> ViewerObservedValue.Observed false
                    && diagnostic.Minimized <> ViewerObservedValue.Observed true
                    && clientSizePositive
                    && diagnostic.RenderableSurfaceAvailable <> ViewerObservedValue.Observed false

                if accessible then
                    InteractiveRunning
                else
                    InaccessibleWindow
            | ViewerObservedValue.Observed false
            | ViewerObservedValue.Unavailable -> InaccessibleWindow

    let failureFromDiagnostic diagnostic =
        let stage = diagnostic.Stage |> Option.defaultValue Unknown

        let classification =
            match stage with
            | DesktopPrerequisite
            | ProcessLaunch
            | WindowCreation
            | Observation
            | Capture
            | InputVerification
            | ControlledExit
            | ArtifactWrite
            | Window
            | Surface
            | Renderer
            | GlContext
            | FirstFrameRender
            | Readback -> UnsupportedEnvironment
            | Scene
            | App
            | Timeout
            | Unknown -> ProductDefect

        { BlockedStage = stage
          Classification = classification
          DiagnosticCategory = diagnostic.Category
          Message = diagnostic.Message
          LastDiagnosticSummary = Some diagnostic.Message }

    let private tryFirstProductView
        (report: ViewerDiagnosticEvent -> unit)
        (phase: string)
        (view: unit -> 'scene)
        : Result<'scene, ViewerRunFailure> =
        try
            Result.Ok(view ())
        with ex ->
            let diagnostic =
                { productDefectDiagnostic phase ex.Message with
                    Message =
                        sprintf
                            "Product %s raised an exception (%s) producing its first frame; the run cannot start (App-stage product defect, not a render failure)."
                            phase
                            ex.Message }

            report diagnostic
            Result.Error(failureFromDiagnostic diagnostic)

    let classifyWindowObservation outcome (inputs: WindowObservationInputs) =
        let externalObservationAttempted = inputs.ExternalObservationAttempted
        let externalWindowMatched = inputs.ExternalWindowMatched
        let captureAttempted = inputs.CaptureAttempted
        let captureSucceeded = inputs.CaptureSucceeded
        let viewerFactsPresent = outcome.WindowOpened && outcome.FirstFramePresented

        let externalObservationMissing =
            externalObservationAttempted
            && externalWindowMatched <> Some true

        let captureMissing =
            captureAttempted
            && captureSucceeded <> Some true

        let missingFacts =
            [ if not outcome.WindowOpened then
                  "viewer-window-opened"
              if not outcome.FirstFramePresented then
                  "viewer-first-frame-presented"
              if externalObservationMissing then
                  "external-window-match"
              if captureMissing then
                  "capture-succeeded" ]

        let blockedStage, classification, message =
            if viewerFactsPresent && externalObservationMissing then
                Some Observation,
                Some UnsupportedEnvironment,
                "External window observation did not match, but viewer-owned window and first-frame facts are present."
            elif viewerFactsPresent && captureMissing then
                Some Capture,
                Some UnsupportedEnvironment,
                "Capture did not succeed, but viewer-owned window and first-frame facts are present."
            else
                outcome.BlockedStage, outcome.Classification, outcome.Message

        let hostFacts =
            [ $"mode={outcome.Mode}"
              $"renderer-mode={outcome.RendererMode}"
              $"exit-path={outcome.ExitPath}" ]

        let observedText =
            match outcome.WindowVisible with
            | ViewerObservedValue.Observed true -> "observed:true"
            | ViewerObservedValue.Observed false -> "observed:false"
            | ViewerObservedValue.Unsupported -> "unsupported"
            | ViewerObservedValue.Unavailable -> "unavailable"

        let viewerFacts =
            [ $"window-opened={outcome.WindowOpened}"
              $"first-frame-presented={outcome.FirstFramePresented}"
              $"window-visible={observedText}"
              $"input-dispatch={outcome.InputDispatch}" ]

        { DiagnosticSource = "real-launch"
          Command = outcome.Command
          HostFacts = hostFacts
          ViewerFacts = viewerFacts
          ViewerWindowOpened = outcome.WindowOpened
          ViewerFirstFramePresented = outcome.FirstFramePresented
          ViewerWindowVisible = outcome.WindowVisible
          ExternalObservationAttempted = externalObservationAttempted
          ExternalWindowMatched = externalWindowMatched
          CaptureAttempted = captureAttempted
          CaptureSucceeded = captureSucceeded
          BlockedStage = blockedStage
          Classification = classification
          MissingFacts = missingFacts
          Message = message }

    let desktopSessionDiagnostic () =
        HostCapability.desktopSessionDiagnostic ()

    let private unsupportedHostReasons () =
        HostCapability.unsupportedHostReasons ()

    let runtimeCapability () =
        HostCapability.runtimeCapability ()

    let applyWindowBehaviorToOptions behavior (windowOptions: WindowOptions) =
        let mutable applied = windowOptions

        match behavior.ResizePolicy with
        | Resizable -> applied.WindowBorder <- WindowBorder.Resizable
        | FixedSize -> applied.WindowBorder <- WindowBorder.Fixed

        match behavior.StartupState with
        | ViewerWindowStartupState.Normal -> applied.WindowState <- WindowState.Normal
        | ViewerWindowStartupState.Maximized -> applied.WindowState <- WindowState.Maximized
        | ViewerWindowStartupState.Minimized -> applied.WindowState <- WindowState.Minimized
        | ViewerWindowStartupState.Fullscreen -> applied.WindowState <- WindowState.Fullscreen
        | ViewerWindowStartupState.WindowedFullscreen ->
            // Borderless coverage of the monitor work area: hidden chrome + work-area
            // geometry, no exclusive-mode resolution change (WindowState stays Normal).
            applied.WindowBorder <- WindowBorder.Hidden
            applied.WindowState <- WindowState.Normal

            match tryResolveWorkArea () with
            | Some(origin, size) ->
                applied.Position <- origin
                applied.Size <- size
            | None -> ()

        match behavior.StartupPosition with
        | Some(Coordinates(x, y)) -> applied.Position <- Vector2D<int>(x, y)
        | Some Centered
        | None -> ()

        match behavior.BackendPreference with
        | Some ViewerBackendPreference.DefaultBackend
        | Some ViewerBackendPreference.OpenGL
        | None -> applied.API <- GraphicsAPI.Default
        | Some ViewerBackendPreference.Vulkan
        | Some ViewerBackendPreference.Software -> ()

        applied

    let windowStateDiagnostic message failureClass (window: IWindow) renderableSurface inputAvailable =
        let sizeText =
            try
                Some $"{window.Size.X}x{window.Size.Y}"
            with _ ->
                None

        let windowState =
            try
                Some window.WindowState
            with _ ->
                None

        { WindowInitialized = window.IsInitialized
          NativeHandle = ViewerObservedValue.Observed window.IsInitialized
          Visible = tryObserved (fun () -> window.IsVisible)
          Focusable = ViewerObservedValue.Unsupported
          Focused = ViewerObservedValue.Unsupported
          Minimized =
            match windowState with
            | Some WindowState.Minimized -> ViewerObservedValue.Observed true
            | Some _ -> ViewerObservedValue.Observed false
            | None -> ViewerObservedValue.Unavailable
          Maximized =
            match windowState with
            | Some WindowState.Maximized -> ViewerObservedValue.Observed true
            | Some _ -> ViewerObservedValue.Observed false
            | None -> ViewerObservedValue.Unavailable
          ClientSize = sizeText
          RenderableSurfaceAvailable = renderableSurface
          // Name the real backend (single source of truth), not a fixed guess — this window was
          // presented through the OpenGL host, so "skia" was an unreliable self-label (#135).
          Backend =
            match windowState with
            | Some state -> Some $"{Host.GlHost.backendLabel};window-state={state}"
            | None -> Some Host.GlHost.backendLabel
          InputDevicesAvailable = inputAvailable
          FailureClass = failureClass
          Message = message }

    let private runPresentedPersistentWindow options behavior diagnostics inputDispatch getScene onTick onKey onPointer onResize onFramebufferResize inputVerified scriptInputs =
        let windowOpened = ref false
        let framePresented = ref false
        let closeReason: ViewerCloseReason option ref = ref None
        let mutable inputQueue = emptyInputQueue
        let mutable nextDrainBatchId = 1L
        let queuedPayloads = System.Collections.Generic.Dictionary<int64, LegacyQueuedInput>()
        let scriptedInputs = scriptInputs |> Option.map List.toArray
        let mutable scriptedIndex = 0
        let mutable scriptedCompletionFrames = 0

        let configuration =
            { Host.Viewer.defaultConfiguration options.Title options.InitialSize with
                ClearColor = Some Colors.black
                // Feature 121 (US1, FR-001): honor the consumer FrameRateCap, defaulting to 60 when unset.
                TargetFrameRate = (options.FrameRateCap |> Option.orElse (Some 60))
                Diagnostics = { Verbose = false }
                PresentMode = options.PresentMode
                // Carry the requested startup state (fullscreen / maximized /
                // windowed-fullscreen / borderless) into the live presented window —
                // previously `behavior` only reached the diagnostic report.
                ConfigureWindow = Some(applyWindowBehaviorToOptions behavior) }

        let renderCurrentScene () =
            getScene ()
            |> nodeToScene

        let pointerInputKind input =
            match input.Phase with
            | ViewerPointerPhaseKind.Moved -> ViewerResponsivenessInputKind.PointerMove
            | ViewerPointerPhaseKind.Wheel -> ViewerResponsivenessInputKind.Wheel
            | ViewerPointerPhaseKind.Exited -> ViewerResponsivenessInputKind.Lifecycle
            | ViewerPointerPhaseKind.Pressed
            | ViewerPointerPhaseKind.Released -> ViewerResponsivenessInputKind.PointerDiscrete

        let payloadNumber (value: float) =
            value.ToString("0.###", CultureInfo.InvariantCulture)

        let pointerPayload input =
            match input.Phase with
            | ViewerPointerPhaseKind.Moved -> $"move:{payloadNumber input.X},{payloadNumber input.Y}"
            | ViewerPointerPhaseKind.Pressed -> $"press:{payloadNumber input.X},{payloadNumber input.Y}"
            | ViewerPointerPhaseKind.Released -> $"release:{payloadNumber input.X},{payloadNumber input.Y}"
            | ViewerPointerPhaseKind.Wheel -> $"wheel:{payloadNumber input.DeltaX},{payloadNumber input.DeltaY}"
            | ViewerPointerPhaseKind.Exited -> "pointer-exited"

        let enqueueQueuedInput kind payloadText payload =
            let envelope, nextQueue = enqueueInput DateTimeOffset.UtcNow kind payloadText inputQueue
            inputQueue <- nextQueue
            queuedPayloads[envelope.SequenceId] <- payload
            RenderLagTrace.emit
                "input-queued"
                [ "seq", string envelope.SequenceId
                  "kind", responsivenessInputKindToken kind
                  "payload", payloadText
                  "receiptDepth", string envelope.ReceiptQueueDepth
                  "queueDepth", string (inputQueueDepth inputQueue) ]

        let enqueueScriptInput input =
            match input with
            | ViewerScriptInput.Key(key, isDown) ->
                let rawKey = ViewerKeyboard.toKeyId key

                enqueueQueuedInput
                    (if isDown then ViewerResponsivenessInputKind.KeyDown else ViewerResponsivenessInputKind.KeyUp)
                    rawKey
                    (QueuedLegacyKey(rawKey, isDown))
            | ViewerScriptInput.Pointer input ->
                enqueueQueuedInput (pointerInputKind input) (pointerPayload input) (QueuedLegacyPointer input)
            | ViewerScriptInput.WaitFrame -> ()

        let pumpScriptInput () =
            match scriptedInputs with
            | Some inputs when !framePresented && scriptedIndex < inputs.Length ->
                let input = inputs.[scriptedIndex]
                RenderLagTrace.emit
                    "script-input-pump"
                    [ "scriptIndex", string scriptedIndex
                      "scriptRemaining", string (inputs.Length - scriptedIndex) ]
                scriptedIndex <- scriptedIndex + 1
                scriptedCompletionFrames <- 0
                enqueueScriptInput input
            | _ -> ()

        let scriptWantsClose () =
            match scriptedInputs with
            | Some inputs ->
                scriptedIndex >= inputs.Length
                && scriptedCompletionFrames > 0
                && inputQueueDepth inputQueue = 0
            | None -> false

        let handleQueuedPayload payload =
            match payload with
            | QueuedLegacyKey(rawKey, isDown) ->
                match onKey with
                | Some handle when handle rawKey isDown ->
                    closeReason := Some AppRequestedClose
                    true
                | _ -> false
            | QueuedLegacyPointer input ->
                match onPointer with
                | Some handle when handle input ->
                    closeReason := Some AppRequestedClose
                    true
                | _ -> false

        let drainQueuedInputs () =
            if inputQueueDepth inputQueue = 0 then
                false
            else
                let drainStarted = DateTimeOffset.UtcNow
                let drain, nextQueue = drainInputQueue nextDrainBatchId "frame-update" inputQueue
                inputQueue <- nextQueue
                nextDrainBatchId <- nextDrainBatchId + 1L
                RenderLagTrace.emit
                    "input-drain-start"
                    [ "batch", string drain.BatchId
                      "queueBefore", string drain.QueueDepthBeforeDrain
                      "queueAfter", string drain.QueueDepthAfterDrain
                      "coalesced", string drain.CoalescedMovementCount ]
                let discreteInputs, deferredInputs =
                    drain.DiscreteInputs
                    |> List.partition (fun envelope -> envelope.PriorityLane = Discrete)

                let orderedInputs =
                    discreteInputs
                    @ (match drain.CoalescedPointer with
                       | Some pointer -> [ pointer ]
                       | None -> [])
                    @ deferredInputs

                let closeRequested =
                    orderedInputs
                    |> List.fold
                        (fun closeRequested envelope ->
                            let found, payload = queuedPayloads.TryGetValue envelope.SequenceId

                            if found then
                                let queueDelay = drainStarted - envelope.ReceivedAt
                                RenderLagTrace.emit
                                    "input-handle-start"
                                    [ "seq", string envelope.SequenceId
                                      "kind", responsivenessInputKindToken envelope.InputKind
                                      "payload", envelope.Payload
                                      "queueDelayMs", queueDelay.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) ]
                                queuedPayloads.Remove envelope.SequenceId |> ignore
                                let handled = handleQueuedPayload payload
                                RenderLagTrace.emit
                                    "input-handle-end"
                                    [ "seq", string envelope.SequenceId
                                      "handled", string handled ]
                                handled || closeRequested
                            else
                                closeRequested)
                        false

                queuedPayloads.Clear()
                RenderLagTrace.emit
                    "input-drain-end"
                    [ "batch", string drain.BatchId
                      "handled", string orderedInputs.Length
                      "closeRequested", string closeRequested ]
                closeRequested

        let init () =
            (), Cmd.none

        let updateLegacy msg () =
            match msg with
            | LegacyLoaded ->
                windowOpened := true
                (), Cmd.ofMsg (LegacyHostEffect(Host.ViewerEffect.RenderFrame(renderCurrentScene ())))
            | LegacyUpdateTick elapsedSeconds ->
                pumpScriptInput ()
                let closeFromQueuedInput = drainQueuedInputs ()
                let closeFromScript = scriptWantsClose ()

                if closeFromQueuedInput || closeFromScript || onTick(TimeSpan.FromSeconds elapsedSeconds) then
                    closeReason := Some AppRequestedClose
                    (), Cmd.ofMsg (LegacyHostEffect Host.ViewerEffect.Shutdown)
                else
                    (), Cmd.none
            | LegacyRenderTick _ ->
                framePresented := true
                match scriptedInputs with
                | Some inputs when scriptedIndex >= inputs.Length ->
                    scriptedCompletionFrames <- scriptedCompletionFrames + 1
                | _ -> ()
                RenderLagTrace.emit
                    "render-frame-requested"
                    [ "scriptedIndex", string scriptedIndex
                      "completionFrames", string scriptedCompletionFrames ]
                (), Cmd.ofMsg (LegacyHostEffect(Host.ViewerEffect.RenderFrame(renderCurrentScene ())))
            | LegacyKey(rawKey, isDown) ->
                enqueueQueuedInput
                    (if isDown then ViewerResponsivenessInputKind.KeyDown else ViewerResponsivenessInputKind.KeyUp)
                    rawKey
                    (QueuedLegacyKey(rawKey, isDown))

                (), Cmd.none
            | LegacyPointer input ->
                // Feature 124: the pointer handler (run in the `when` guard) already folded any
                // resulting messages into the model. Do NOT emit a per-event RenderFrame — a fast mouse
                // produces hundreds of pointer events/sec, and one full repaint each bypassed the
                // FrameRateCap (renders spiked to ~3x the cap) and backed the loop up, so input arrived
                // in stutters/bursts. The paced RenderTick (60Hz) presents the updated scene, exactly as
                // the LegacyKey path above already relies on.
                enqueueQueuedInput (pointerInputKind input) (pointerPayload input) (QueuedLegacyPointer input)
                (), Cmd.none
            | LegacyResized size ->
                onResize |> Option.iter (fun handle -> handle size)
                (), Cmd.ofMsg (LegacyHostEffect(Host.ViewerEffect.RenderFrame(renderCurrentScene ())))
            | LegacyFramebufferResized size ->
                // Issue #400: the PHYSICAL framebuffer changed. A size-aware host uses it to advertise
                // native resolution / rescale pointer; it does not itself force a present (the paired
                // `LegacyResized` above already re-derives and repaints), so this only updates state.
                onFramebufferResize |> Option.iter (fun handle -> handle size)
                (), Cmd.none
            | LegacyCloseRequested ->
                if closeReason.Value.IsNone then
                    closeReason := Some UserClose

                (), Cmd.none
            | LegacyDiagnosticReported diagnostic ->
                captureDiagnostic
                    diagnostics
                    { Level =
                        match diagnostic.Severity with
                        | Host.DiagnosticSeverity.Fatal
                        | Host.DiagnosticSeverity.Error -> ViewerDiagnosticLevel.Error
                        | Host.DiagnosticSeverity.Warning -> ViewerDiagnosticLevel.Warning
                        | Host.DiagnosticSeverity.Info -> ViewerDiagnosticLevel.Info
                      // Feature 118 (FR-007): carry the backend stage into the consumer-facing
                      // category so the live present-mode / readback diagnostic surfaces as
                      // Swapchain (or Frame), not Renderer. All other stages keep Renderer.
                      Category =
                        match diagnostic.Stage with
                        | Host.DiagnosticStage.Framebuffer -> ViewerDiagnosticCategory.Framebuffer
                        | Host.DiagnosticStage.FrameRender -> ViewerDiagnosticCategory.Frame
                        | _ -> ViewerDiagnosticCategory.Renderer
                      Message = diagnostic.Message
                      FrameIndex = None
                      Stage = None
                      Elapsed = None }
                |> ignore

                (), Cmd.none
            | LegacyHostEffect _
            | LegacyAppMsg _ -> (), Cmd.none

        let eventMapper event =
            match event with
            | Host.ViewerEvent.Loaded -> Some LegacyLoaded
            | Host.ViewerEvent.UpdateTick elapsed -> Some(LegacyUpdateTick elapsed)
            | Host.ViewerEvent.RenderTick elapsed -> Some(LegacyRenderTick elapsed)
            | Host.ViewerEvent.KeyDown key -> Some(LegacyKey(key, true))
            | Host.ViewerEvent.KeyUp key -> Some(LegacyKey(key, false))
            | Host.ViewerEvent.CloseRequested -> Some LegacyCloseRequested
            | Host.ViewerEvent.DiagnosticReported diagnostic -> Some(LegacyDiagnosticReported diagnostic)
            | Host.ViewerEvent.Resized size -> Some(LegacyResized size)
            | Host.ViewerEvent.FramebufferResized size -> Some(LegacyFramebufferResized size)
            | Host.ViewerEvent.PointerMoved(x, y) ->
                Some(LegacyPointer { Phase = ViewerPointerPhaseKind.Moved; X = x; Y = y; Button = None; DeltaX = 0.0; DeltaY = 0.0 })
            | Host.ViewerEvent.PointerPressed(x, y, button) ->
                Some(LegacyPointer { Phase = ViewerPointerPhaseKind.Pressed; X = x; Y = y; Button = Some(toViewerPointerButtonKind button); DeltaX = 0.0; DeltaY = 0.0 })
            | Host.ViewerEvent.PointerReleased(x, y, button) ->
                Some(LegacyPointer { Phase = ViewerPointerPhaseKind.Released; X = x; Y = y; Button = Some(toViewerPointerButtonKind button); DeltaX = 0.0; DeltaY = 0.0 })
            | Host.ViewerEvent.PointerScrolled(x, y, deltaX, deltaY) ->
                Some(LegacyPointer { Phase = ViewerPointerPhaseKind.Wheel; X = x; Y = y; Button = None; DeltaX = deltaX; DeltaY = deltaY })
            | Host.ViewerEvent.PointerExited ->
                Some(LegacyPointer { Phase = ViewerPointerPhaseKind.Exited; X = 0.0; Y = 0.0; Button = None; DeltaX = 0.0; DeltaY = 0.0 })

        let effectMapper msg =
            match msg with
            | LegacyHostEffect effect -> Some effect
            | _ -> None

        let program =
            Host.Viewer.create configuration init updateLegacy (fun () -> renderCurrentScene ())
            |> Host.Viewer.withEventMapping eventMapper
            |> Host.Viewer.withEffectMapping effectMapper

        match Host.Viewer.run program with
        | Ok() ->
            let visibleDiagnostic =
                { WindowInitialized = !windowOpened
                  NativeHandle = ViewerObservedValue.Observed !windowOpened
                  Visible =
                    if !framePresented then
                        ViewerObservedValue.Observed true
                    else
                        ViewerObservedValue.Unavailable
                  Focusable = ViewerObservedValue.Unsupported
                  Focused = ViewerObservedValue.Unsupported
                  Minimized = ViewerObservedValue.Unsupported
                  Maximized = ViewerObservedValue.Unsupported
                  ClientSize = Some $"{options.InitialSize.Width}x{options.InitialSize.Height}"
                  RenderableSurfaceAvailable =
                    if !framePresented then
                        ViewerObservedValue.Observed true
                    else
                        ViewerObservedValue.Unavailable
                  // Single source of truth for the backend label (#135); this path already
                  // presented through the OpenGL/Skia framebuffer, as the message states.
                  Backend = Some Host.GlHost.backendLabel
                  InputDevicesAvailable = ViewerObservedValue.Unsupported
                  FailureClass = None
                  Message = "persistent viewer presented frames through the OpenGL/Skia framebuffer" }

            if not (inputVerified ()) then
                Result.Error(
                    makeFailure
                        App
                        AppLifecycle
                        Input
                        "Persistent viewer did not observe required input dispatch before close."
                        None
                )
            else
                Result.Ok(
                    launchOk
                        inputDispatch
                        !windowOpened
                        !framePresented
                        !closeReason
                        [ visibleDiagnostic ]
                        (validateWindowLaunchBehavior options.InitialSize behavior)
                        "Persistent viewer launch completed after user or host close."
                )
        | Result.Error diagnostic -> Result.Error(toViewerFailure diagnostic)

    let private effectsContainClose effects =
        effects
        |> List.exists (function
            | CloseWindow -> true
            | _ -> false)

    let private requireInputDispatchVerification () =
        String.Equals(
            Environment.GetEnvironmentVariable "FS_SKIA_REQUIRE_INPUT_DISPATCH",
            "1",
            StringComparison.Ordinal
        )

    let initWithWindowBehavior options behavior =
        let diagnostic =
            { Level = ViewerDiagnosticLevel.Info
              Category = ViewerDiagnosticCategory.Startup
              Message = $"viewer window open requested for '{options.Title}'"
              FrameIndex = None
              Stage = Some Window
              Elapsed = None }

        { Options = options
          WindowBehavior = behavior
          IsRunning = false
          LifecycleState = NotStarted
          FirstFramePresented = false
          UserCloseObserved = false
          InputDispatch = NotRequired
          LastScene = None },
        [ OpenWindow(options.Title, options.InitialSize)
          ApplyWindowOptions behavior
          EmitDiagnostic diagnostic ]

    let init options = initWithWindowBehavior options defaultWindowBehavior

    let update msg model =
        match msg with
        | Start
        | StartInteractive -> { model with IsRunning = true; LifecycleState = CheckingDesktopSession }, [ CheckDesktopSession ]
        | StartEvidence request -> { model with IsRunning = true; LifecycleState = EvidenceRunning }, [ StartBoundedRun request ]
        | Stop -> { model with IsRunning = false; LifecycleState = Closing }, [ CloseWindow ]
        | DesktopSessionChecked diagnostic ->
            let event =
                { Level =
                    if diagnostic.DiagnosticClass = "unsupported-host" then
                        ViewerDiagnosticLevel.Error
                    else
                        ViewerDiagnosticLevel.Info
                  Category = ViewerDiagnosticCategory.EnvironmentSession
                  Message = diagnostic.Message
                  FrameIndex = None
                  Stage = Some Window
                  Elapsed = None }

            if diagnostic.DiagnosticClass = "unsupported-host" then
                { model with IsRunning = false; LifecycleState = Unsupported }, [ EmitDiagnostic event ]
            else
                { model with LifecycleState = StartingWindow },
                [ OpenWindow(model.Options.Title, model.Options.InitialSize)
                  ApplyWindowOptions model.WindowBehavior
                  EmitDiagnostic event ]
        | WindowCreated diagnostic ->
            { model with LifecycleState = ViewerLifecycleState.WindowCreated },
            [ EmitDiagnostic
                  { Level = ViewerDiagnosticLevel.Info
                    Category = ViewerDiagnosticCategory.Startup
                    Message = diagnostic.Message
                    FrameIndex = None
                    Stage = Some Window
                    Elapsed = None }
              QueryNativeWindowState ]
        | VisibilityCheckStarted diagnostic ->
            { model with LifecycleState = VisibilityChecking },
            [ EmitDiagnostic
                  { Level = ViewerDiagnosticLevel.Info
                    Category = ViewerDiagnosticCategory.Startup
                    Message = diagnostic.Message
                    FrameIndex = None
                    Stage = Some Window
                    Elapsed = None }
              QueryNativeWindowState ]
        | VisibilityObserved diagnostic ->
            let lifecycle = classifyWindowState diagnostic

            { model with LifecycleState = lifecycle },
            [ EmitDiagnostic
                  { Level = ViewerDiagnosticLevel.Info
                    Category = ViewerDiagnosticCategory.Startup
                    Message = diagnostic.Message
                    FrameIndex = None
                    Stage = Some Window
                    Elapsed = None } ]
        | Render scene ->
            let diagnostic =
                { Level = ViewerDiagnosticLevel.Debug
                  Category = ViewerDiagnosticCategory.Scene
                  Message = "viewer scene render requested"
                  FrameIndex = None
                  Stage = Some ViewerRunBlockedStage.Scene
                  Elapsed = None }

            { model with LastScene = Some scene },
            [ RenderScene scene
              EmitDiagnostic diagnostic ]
        | KeyEvent event ->
            let key, isDown = ViewerKeyboard.normalizeEvent event
            let direction = if isDown then "down" else "up"
            let diagnostic =
                { Level = ViewerDiagnosticLevel.Info
                  Category = ViewerDiagnosticCategory.Input
                  Message = $"viewer input {direction}: raw='{event.RawKey}' normalized='{key}'"
                  FrameIndex = None
                  Stage = None
                  Elapsed = None }

            { model with InputDispatch = Verified },
            [ DispatchInput(key, isDown)
              EmitDiagnostic diagnostic ]
        | DiagnosticCaptured diagnostic -> model, [ EmitDiagnostic diagnostic ]
        | FramePresented size ->
            let diagnostic =
                { Level = ViewerDiagnosticLevel.Debug
                  Category = ViewerDiagnosticCategory.Frame
                  Message = $"viewer frame presented at {size.Width}x{size.Height}"
                  FrameIndex = None
                  Stage = None
                  Elapsed = None }

            { model with
                FirstFramePresented = true
                LifecycleState = FirstFramePresented },
            [ EmitDiagnostic diagnostic ]
        | UserCloseObserved ->
            { model with
                IsRunning = false
                UserCloseObserved = true
                LifecycleState = UserCloseObservedState },
            [ CloseWindow ]
        | AppCloseRequested ->
            { model with
                IsRunning = false
                LifecycleState = CloseRequested },
            [ CloseWindow ]
        | EvidenceCloseRequested ->
            { model with
                IsRunning = false
                LifecycleState = EvidenceCloseObservedState },
            [ CloseWindow ]
        | HostCloseObserved ->
            { model with
                IsRunning = false
                LifecycleState = Closing },
            [ CloseWindow ]
        | EvidenceTargetReached -> { model with IsRunning = false; LifecycleState = Closing }, [ CloseWindow ]
        | RunFailed failure ->
            let diagnostic =
                { Level = ViewerDiagnosticLevel.Error
                  Category = failure.DiagnosticCategory
                  Message = failure.Message
                  FrameIndex = None
                  Stage = Some failure.BlockedStage
                  Elapsed = None }

            { model with LifecycleState = Failed }, [ EmitDiagnostic diagnostic ]
        | RunTimedOut ->
            let failureDiagnostic =
                { Level = ViewerDiagnosticLevel.Error
                  Category = ViewerDiagnosticCategory.Startup
                  Message = "Viewer run timed out before requested evidence was collected."
                  FrameIndex = None
                  Stage = Some Timeout
                  Elapsed = None }

            { model with LifecycleState = Failed }, [ EmitDiagnostic failureDiagnostic ]

    let initRun (request: ViewerRunRequest) =
        { Request = request
          FramesRendered = 0
          StartedAt = None
          LastDiagnostic = None
          Completed = None },
        [ OpenBoundedWindow request ]

    let private elapsedForCompletion (model: ViewerRunModel) =
        model.LastDiagnostic
        |> Option.bind _.Elapsed
        |> Option.defaultValue (TimeSpan.FromMilliseconds 1.0)

    let completeEvidence size (model: ViewerRunModel) : ViewerRunEvidence =
        { FramesRendered = model.FramesRendered
          Elapsed = elapsedForCompletion model
          InitialOutputSize = size
          // A completed bounded run rendered its frames through the live OpenGL host, so the
          // evidence names the backend that actually initialized — NOT the caller's requested
          // `RendererMode` (which could be any label, e.g. a stale "vulkan"). Deriving from the
          // single source of truth is what stops the self-report from disagreeing with the real
          // launch path (#135).
          RendererMode = Host.GlHost.backendLabel
          LastDiagnosticSummary = model.LastDiagnostic |> Option.map _.Message
          EvidencePath = model.Request.EvidencePath }

    let private targetReached (model: ViewerRunModel) =
        match model.Request.Target with
        | FirstFrame -> model.FramesRendered >= 1
        | FrameCount count -> count > 0 && model.FramesRendered >= count
        | Duration duration -> elapsedForCompletion model >= duration

    let updateRun (msg: ViewerRunMsg) (model: ViewerRunModel) =
        match msg with
        | BeginRun -> model, [ OpenBoundedWindow model.Request ]
        | RunStarted instant -> { model with StartedAt = Some instant }, [ RequestFrame ]
        | RecordFrame size ->
            let next = { model with FramesRendered = model.FramesRendered + 1 }

            if targetReached next then
                let evidence = completeEvidence size next
                { next with Completed = Some(Result.Ok evidence) }, [ StopBoundedRun ]
            else
                next, [ RequestFrame ]
        | RecordDiagnostic diagnostic -> { model with LastDiagnostic = Some diagnostic }, []
        | CompleteRun ->
            let evidence = completeEvidence { Width = 1; Height = 1 } model
            { model with Completed = Some(Result.Ok evidence) }, [ PersistRunEvidence evidence ]
        | FailRun failure -> { model with Completed = Some(Result.Error failure) }, [ StopBoundedRun ]
        | TimeoutRun ->
            let failure =
                { BlockedStage = Timeout
                  Classification = ProductDefect
                  DiagnosticCategory = ViewerDiagnosticCategory.Startup
                  Message = "Viewer run timed out before requested evidence was collected."
                  LastDiagnosticSummary = model.LastDiagnostic |> Option.map _.Message }

            { model with Completed = Some(Result.Error failure) }, [ StopBoundedRun ]

    let private startupDiagnostic elapsed message : ViewerDiagnosticEvent =
        { Level = ViewerDiagnosticLevel.Info
          Category = ViewerDiagnosticCategory.Startup
          Message = message
          FrameIndex = None
          Stage = Some Window
          Elapsed = Some elapsed }

    let private frameDiagnostic frame elapsed : ViewerDiagnosticEvent =
        { Level = ViewerDiagnosticLevel.Info
          Category = ViewerDiagnosticCategory.Frame
          Message = $"frame {frame} presented"
          FrameIndex = Some frame
          Stage = None
          Elapsed = Some elapsed }

    module VisualEvidenceHandling =
        let artifacts request options scene =
            visualEvidenceArtifacts request options scene

    // R4/P6: a bounded run drives a real Silk.NET window and counts its frame callbacks, but it does
    // NOT present `scene` on the live GL surface (that is `run`/`runApp`). It no longer `ignore`s the
    // scene: when an evidence artifact is requested for a `.png` path the scene is rasterized to real
    // pixels through the shared CPU painter, so the evidence genuinely depicts the scene instead of a
    // window that drew nothing. Non-image evidence paths keep the textual run summary. Disclosed in
    // SkiaViewer.fsi so callers do not read "frames rendered" as "scene presented on screen".
    let private writeRunEvidence path (options: ViewerOptions) (scene: SceneNode) (evidence: ViewerRunEvidence) =
        if isPngPath path then
            writeSceneImageEvidence path options.InitialSize scene |> ignore
        else
            writeEvidence path evidence

    let runBounded (request: ViewerRunRequest) options (scene: SceneNode) =
        match validateRequest request with
        | Result.Error failure -> Result.Error failure
        | Result.Ok() ->
            match validateOptions options with
            | Result.Error failure -> Result.Error failure
            | Result.Ok() ->
                // Issue #246: the bounded surface is `InitialSize`, and it never resizes.
                let scene = presentedFor options options.InitialSize scene
                match unsupportedHostFailure () with
                | Some failure ->
                    let diagnostic =
                        { Level = ViewerDiagnosticLevel.Error
                          Category = failure.DiagnosticCategory
                          Message = failure.Message
                          FrameIndex = None
                          Stage = Some failure.BlockedStage
                          Elapsed = Some TimeSpan.Zero }

                    dispatchDiagnostic request.Diagnostics diagnostic |> ignore
                    Result.Error { failure with LastDiagnosticSummary = Some failure.Message }
                | None ->
                    let start = DateTimeOffset.UtcNow
                    let model, _ = initRun request
                    let model, _ = updateRun (RunStarted start) model

                    let startup = dispatchDiagnostic request.Diagnostics (startupDiagnostic TimeSpan.Zero "bounded viewer run started")
                    let mutable current: ViewerRunModel = updateRun (RecordDiagnostic startup) model |> fst
                    let mutable frame = 0
                    let stopwatch = Stopwatch.StartNew()

                    // #363: only window creation runs under the XWayland backend override; the
                    // bounded render loop below runs outside it (see GlHost.withWindowBackendOverride).
                    (
                        try
                            let mutable windowOptions = WindowOptions.Default
                            windowOptions.Title <- options.Title
                            windowOptions.Size <- toNativeSize options.InitialSize
                            windowOptions.IsVisible <- true
                            windowOptions.API <- GraphicsAPI.Default
                            windowOptions.FramesPerSecond <- 60.0
                            windowOptions.UpdatesPerSecond <- 60.0

                            let window = Host.GlHost.withWindowBackendOverride (fun () -> Window.Create windowOptions)

                            let loadedHandler =
                                Action(fun () ->
                                    let diagnostic =
                                        dispatchDiagnostic
                                            request.Diagnostics
                                            { Level = ViewerDiagnosticLevel.Info
                                              Category = ViewerDiagnosticCategory.Startup
                                              Message = $"bounded viewer window opened for '{options.Title}'"
                                              FrameIndex = None
                                              Stage = Some Window
                                              Elapsed = Some stopwatch.Elapsed }

                                    current <- updateRun (RecordDiagnostic diagnostic) current |> fst)

                            let renderHandler =
                                Action<float>(fun _ ->
                                    if current.Completed.IsNone then
                                        frame <- frame + 1
                                        let elapsed = stopwatch.Elapsed
                                        let diagnostic = dispatchDiagnostic request.Diagnostics (frameDiagnostic frame elapsed)
                                        let withDiagnostic, _ = updateRun (RecordDiagnostic diagnostic) current

                                        if elapsed > request.Timeout then
                                            current <- updateRun TimeoutRun withDiagnostic |> fst
                                        else
                                            current <- updateRun (RecordFrame options.InitialSize) withDiagnostic |> fst

                                        if current.Completed.IsSome && not window.IsClosing then
                                            window.Close())

                            window.add_Load loadedHandler
                            window.add_Render renderHandler

                            let handlers =
                                [ fun (w: IWindow) -> w.remove_Load loadedHandler
                                  fun (w: IWindow) -> w.remove_Render renderHandler ]

                            try
                                Host.GlHost.withWindowBackendOverride (fun () -> window.Initialize())

                                if not window.IsInitialized then
                                    Result.Error(
                                        makeFailure
                                            Window
                                            UnsupportedEnvironment
                                            Startup
                                            "Silk.NET bounded viewer window did not initialize."
                                            current.LastDiagnostic
                                    )
                                else
                                    while not window.IsClosing && current.Completed.IsNone do
                                        if stopwatch.Elapsed > request.Timeout then
                                            current <- updateRun TimeoutRun current |> fst
                                            window.Close()
                                        else
                                            window.DoEvents()
                                            window.DoUpdate()
                                            window.DoRender()
                                            Thread.Sleep(1)

                                    match current.Completed with
                                    | Some(Result.Ok evidence) ->
                                        request.EvidencePath |> Option.iter (fun path -> writeRunEvidence path options scene evidence)
                                        Result.Ok evidence
                                    | Some(Result.Error failure) -> Result.Error failure
                                    | None ->
                                        Result.Error(
                                            makeFailure
                                                Timeout
                                                ProductDefect
                                                Startup
                                                "Viewer run timed out before requested evidence was collected."
                                                current.LastDiagnostic
                                        )
                            finally
                                handlers
                                |> List.iter (fun remove ->
                                    try
                                        remove window
                                    with _ ->
                                        ())

                                window.Dispose()
                        with ex ->
                            match current.Completed with
                            | Some(Result.Ok evidence) ->
                                request.EvidencePath |> Option.iter (fun path -> writeEvidence path evidence)
                                Result.Ok evidence
                            | Some(Result.Error failure) -> Result.Error failure
                            | None ->
                                Result.Error(
                                    makeFailure
                                        Window
                                        UnsupportedEnvironment
                                        Startup
                                        $"Silk.NET bounded viewer launch failed: {ex.Message}"
                                        current.LastDiagnostic
                                ))

    let runUntilFirstFrame options (scene: SceneNode) =
        let request: ViewerRunRequest =
            { Target = FirstFrame
              Timeout = TimeSpan.FromSeconds 10.0
              Diagnostics = defaultDiagnostics
              RendererMode = "default"
              EvidencePath = None }

        runBounded request options scene

    let runForFrames frameCount options (scene: SceneNode) =
        let request: ViewerRunRequest =
            { Target = FrameCount frameCount
              Timeout = TimeSpan.FromSeconds 10.0
              Diagnostics = defaultDiagnostics
              RendererMode = "default"
              EvidencePath = None }

        runBounded request options scene

    let run options scene =
        match validateOptions options with
        | Result.Error failure -> Result.Error failure
        | Result.Ok() ->
            let capability = runtimeCapability ()

            if not capability.PersistentWindow then
                Result.Error(persistentUnsupportedFailure capability)
            else
                let model, _ = init options
                let _, _ = update Start model
                // Issue #246: a static scene authored in a logical canvas is fitted to the live
                // surface too, and refitted when the window resizes.
                let mutable currentSurfaceSize = options.InitialSize

                runPresentedPersistentWindow
                    options
                    defaultWindowBehavior
                    defaultDiagnostics
                    "not-applicable"
                    (fun () -> presentedFor options currentSurfaceSize scene)
                    (fun _ -> false)
                    None
                    None
                    (Some(fun size -> currentSurfaceSize <- size))
                    // Issue #400: the non-interactive generated-app path authors in the logical window and
                    // lets the present-time fit scale up — it does not advertise native resolution.
                    None
                    (fun () -> true)
                    None

    /// Issue #444: the evidence effects a product emits from a persistent loop. The highest-severity
    /// member of the silent-no-op family (#416) — `CaptureScreenshot`, `CaptureImageEvidence`,
    /// `WriteVisualEvidence` and `WriteRunEvidence` sat in the discard group beside the window-lifecycle
    /// effects, so a product's `Update` asked for evidence and got no file, no error, and a run that
    /// reported success. Compose that with SDD#349 — the lifecycle never opens the `artifacts:` path it
    /// records — and the verdict is green with nothing behind it, unfalsifiable end to end.
    ///
    /// So these are HONORED, not merely announced. Audio needed a caller-supplied sink because the viewer
    /// owns no audio device; evidence is different — every writer already existed in this module, private,
    /// serving the bounded path. `WriteRunEvidence`/`WriteVisualEvidence` carry their payload and are
    /// written verbatim. The two capture effects rasterize the CURRENT scene through the same shared CPU
    /// painter `runBounded` uses, so they depict the SCENE and not the presented GL framebuffer — the same
    /// disclosure `runBounded` already carries, restated on the `runApp` family in SkiaViewer.fsi.
    ///
    /// Evidence I/O must never take a live render loop down with it, so a failed write is caught and
    /// reported as an `Error`/`Screenshot`/`ArtifactWrite` diagnostic naming the effect, the path and the
    /// reason. That reason string is the failure leg #266 asks for: evidence that did NOT get written now
    /// says so, on the diagnostics channel that was already wired five lines above the old discard.
    let internal productEvidenceSink
        (onDiagnostic: ViewerDiagnosticEvent -> unit)
        (sceneSize: unit -> FS.GG.UI.Scene.Size)
        (currentScene: unit -> SceneNode)
        (effect: ViewerEffect)
        : unit =
        // "failed to write", not "did not write": `writeSceneImageEvidence` returns false both when the
        // encode produced nothing (no file) and when the file it wrote will not decode again. Claiming
        // nothing was written would be a lie in the second case, and the whole point here is to stop
        // saying reassuring things about evidence that is not there.
        let report (effectName: string) (path: string) (reason: string) =
            onDiagnostic
                { Level = ViewerDiagnosticLevel.Error
                  Category = ViewerDiagnosticCategory.Screenshot
                  Message = $"{effectName} failed to write '{path}': {reason}"
                  FrameIndex = None
                  Stage = Some ViewerRunBlockedStage.ArtifactWrite
                  Elapsed = None }

        let attempt (effectName: string) (path: string) (write: unit -> unit) =
            if String.IsNullOrWhiteSpace path then
                report effectName path "the effect named an empty path"
            else
                try
                    write ()
                with ex ->
                    report effectName path ex.Message

        let rasterize (effectName: string) (path: string) =
            if not (writeSceneImageEvidence path (sceneSize ()) (currentScene ())) then
                report effectName path "the rasterized scene did not produce a readable PNG"

        match effect with
        | CaptureScreenshot path -> attempt "CaptureScreenshot" path (fun () -> rasterize "CaptureScreenshot" path)
        | CaptureImageEvidence path -> attempt "CaptureImageEvidence" path (fun () -> rasterize "CaptureImageEvidence" path)
        | WriteRunEvidence(path, evidence) ->
            // The SAME rule the bounded path applies (`writeRunEvidence`): a `.png` evidence path gets the
            // rasterized scene, any other path gets the textual run summary. Writing text unconditionally
            // here would mean one effect, two behaviours depending on which host ran it — the exact
            // two-copies drift #429 removed from this fold, reintroduced one level down.
            attempt "WriteRunEvidence" path (fun () ->
                if isPngPath path then
                    rasterize "WriteRunEvidence" path
                else
                    writeEvidence path evidence)
        // NOT rasterized even for a `.png` path, unlike the two above: this effect CARRIES the artifact
        // record the product already decided on, and rasterizing would silently discard that payload.
        | WriteVisualEvidence(path, artifact) ->
            attempt "WriteVisualEvidence" path (fun () -> writeVisualEvidenceArtifact path artifact)
        // Enumerated, not a `| _ -> ()` wildcard, and deliberately so: a wildcard in the very function
        // whose job is to stop effects vanishing would silently swallow the NEXT evidence effect somebody
        // adds to `ViewerEffect`. Listed out, the compiler makes that addition a decision instead.
        | RenderScene _
        | DispatchInput _
        | CloseWindow
        | EmitDiagnostic _
        | PlayAudio _
        | Persist _
        | OpenWindow _
        | ApplyWindowOptions _
        | QueryNativeWindowState
        | StartBoundedRun _
        | CheckDesktopSession
        | ReadPixels -> ()

    /// Issue #429: the ONE effect interpretation both persistent loops perform — the generated-app loop
    /// and the pointer/size-aware interactive loop. They were separate, byte-identical folds, and they
    /// drifted: the interactive copy left `PlayAudio` in the discard group, so a product on the
    /// interactive host got silence with nothing in the type system objecting (#429). Sharing the fold
    /// is what stops the two host families diverging on effect handling again.
    ///
    /// `internal` for the same reason `runtimeStateRepaint` is: the live loops are GL/timing-bound and
    /// not drivable headless, so this is the seam a regression test asserts the policy on directly.
    /// The mutations each loop performs are passed in, keeping the fold itself free of loop state.
    /// Returns whether the batch requested a close.
    let internal interpretViewerEffects
        (audioSink: AudioEffect list -> unit)
        (persistenceSink: PersistenceEffect list -> unit)
        (onScene: SceneNode -> unit)
        (onInputDispatch: unit -> unit)
        (onDiagnostic: ViewerDiagnosticEvent -> unit)
        (evidenceSink: ViewerEffect -> unit)
        (effects: ViewerEffect list)
        : bool =
        effects
        |> List.fold
            (fun closeRequested effect ->
                match effect with
                | RenderScene scene ->
                    onScene scene
                    closeRequested
                | DispatchInput _ ->
                    onInputDispatch ()
                    closeRequested
                | CloseWindow -> true
                | EmitDiagnostic diagnostic ->
                    onDiagnostic diagnostic
                    closeRequested
                | PlayAudio batch ->
                    audioSink batch
                    closeRequested
                // #535 — the whole point: a product's save/load requests now REACH a host. The sink is
                // where the outcome is dispatched back into `update`, so a `Load` can finally be answered.
                // `runApp`/`runAppWithAudio` pass `ignore` here, which is the honest behaviour for a host
                // that owns no save location — a request that goes nowhere, rather than a save that
                // silently did not happen.
                | Persist batch ->
                    persistenceSink batch
                    closeRequested
                // Issue #444: evidence is WRITTEN, not discarded. These four used to fall through to the
                // group below and vanish — no file, no error, success. `productEvidenceSink` honors them
                // and reports any failed write on the diagnostics channel.
                | CaptureScreenshot _
                | CaptureImageEvidence _
                | WriteVisualEvidence _
                | WriteRunEvidence _ ->
                    evidenceSink effect
                    closeRequested
                // Structurally inapplicable INSIDE a running persistent loop, and that is why they are
                // dropped — the honesty the interactive loop's sibling comment already had and this fold
                // did not (#444). `OpenWindow`/`ApplyWindowOptions`/`StartBoundedRun`/`CheckDesktopSession`
                // are launch-time lifecycle steps: the loop is past them, holding the window they ask for.
                // `QueryNativeWindowState` and `ReadPixels` are queries, and a fold returning `bool` has no
                // channel to answer on — a product cannot observe a reply that has nowhere to go. None of
                // the six names a path, so none of them can silently fail to write one.
                | OpenWindow _
                | ApplyWindowOptions _
                | QueryNativeWindowState
                | StartBoundedRun _
                | CheckDesktopSession
                | ReadPixels -> closeRequested)
            false

    /// #535 — the outcome dispatch, EXTRACTED so it can be tested.
    ///
    /// `internal` for exactly the reason `interpretViewerEffects` is: the launch body bails at
    /// `capability.PersistentWindow` long before it builds this, so headless CI can never reach the wiring
    /// through `runAppWithPersistence`. A test that re-implements the launch body's shape locally pins the
    /// AUTHOR'S MODEL of the code, not the code — and the ordering bug this seam shipped with was caught by
    /// a human reading it, with 350 green tests attached. So the loop lives here, where a test can call it.
    ///
    /// Returns whether any dispatched message asked to close.
    let internal dispatchPersistenceBatch
        (sink: PersistenceEffect list -> PersistenceOutcome list)
        (mapOutcome: PersistenceOutcome -> 'msg option)
        (dispatch: 'msg -> bool)
        (batch: PersistenceEffect list)
        : bool =
        sink batch
        |> List.fold
            (fun closeRequested outcome ->
                match mapOutcome outcome with
                // An outcome the product does not map is DROPPED deliberately: a host that reports `Absent`
                // to a product with no message for it has still ANSWERED, and inventing a message would be
                // the framework deciding what "no save" means to a game.
                | None -> closeRequested
                | Some msg -> dispatch msg || closeRequested)
            false

    // Issue #245 — the one generated-app launch body. `audioSink` receives every `PlayAudio` batch in
    // dispatch order; the viewer itself owns no audio device, so realizing a batch is entirely the
    // caller's business (the template hands in `FS.GG.Audio.Host.Audio.play backend`). `runApp` and
    // `runAppWithWindowBehavior` pass `ignore`, which is why they keep behaving exactly as before.
    let private runGeneratedApp
        options
        behavior
        (audioSink: AudioEffect list -> unit)
        // An OPTION, not a no-op function. `(fun _ -> [])` and "a sink that legitimately returned nothing"
        // are indistinguishable from inside, so a launch given no sink could not tell that it was dropping a
        // product's save requests — and therefore could not say so. #416: a dropped request must SAY SO.
        (persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) option)
        (mapOutcome: PersistenceOutcome -> 'msg option)
        (host: GeneratedAppHost<'model, 'msg>)
        =
        match validateOptions options with
        | Result.Error failure -> Result.Error failure
        | Result.Ok() ->
            let optionFailures =
                validateWindowLaunchBehavior options.InitialSize behavior
                |> List.filter (fun result -> result.Status = FailedOption)

            if not (List.isEmpty optionFailures) then
                let message =
                    optionFailures
                    |> List.map (fun result -> $"{result.Option}: {result.Message}")
                    |> String.concat "; "

                Result.Error(makeFailure Window ProductDefect ViewerDiagnosticCategory.Startup message None)
            else
                let capability = runtimeCapability ()

                if not capability.PersistentWindow then
                    Result.Error(persistentUnsupportedFailure capability)
                else
                    let model, initEffects = host.Init()
                    let mutable currentModel = model
                    // Issue #365: a product `Update`/`View` fault is an App-stage defect captured through
                    // the host's diagnostics, never a window teardown.
                    let reportProductDefect ev = captureDiagnostic host.Diagnostics ev |> ignore
                    // Issue #396: guard the FIRST product View — there is no last-good scene to fall
                    // back to as the runtime `safeView` does, so a first-frame throw fails the run as a
                    // startup App-stage ProductDefect instead of escaping as an uncaught exception.
                    match tryFirstProductView reportProductDefect "View" (fun () -> host.View currentModel) with
                    | Result.Error failure -> Result.Error failure
                    | Result.Ok initialScene ->
                        let mutable currentScene = initialScene
                        let mutable inputDispatch = "false"
                        // Issue #365: guard the product `Update`/`View` so one throwing step drops that input
                        // and keeps the persistent window on its last-good scene, rather than escaping to a
                        // teardown mislabeled `frameRenderFailed`.
                        let safeView model =
                            tryProductStep reportProductDefect "View" (fun () -> host.View model)
                            |> Option.defaultValue currentScene
                        // Issue #246: `GeneratedAppHost.View` withholds the window size on purpose, so the
                        // product always draws in its own coordinate space. Track the live surface so a
                        // `LogicalSize` product can be fitted to it; without one this stays inert and the
                        // presented scene is the view output verbatim.
                        let mutable currentSurfaceSize = options.InitialSize

                        let presentScene () = presentedFor options currentSurfaceSize currentScene

                        let handleResize (size: Size) = currentSurfaceSize <- size

                        // Bound once per launch, not per call: `interpretEffects` runs on every dispatched
                        // message and every tick, so building these closures inline would allocate three
                        // per frame.
                        let onScene scene = currentScene <- scene
                        let onInputDispatch () = inputDispatch <- "true"
                        let onDiagnostic diagnostic = captureDiagnostic host.Diagnostics diagnostic |> ignore

                        // Issue #444: evidence rasterizes at `InitialSize`, the space `GeneratedAppHost.View`
                        // authors in — it is handed no window size on purpose (#246), so the live surface is
                        // a present-time fit and not the scene's own coordinate space. This is the size
                        // `runBounded` rasterizes evidence at too.
                        let evidenceSink =
                            productEvidenceSink onDiagnostic (fun () -> options.InitialSize) (fun () -> currentScene)

                        // #535 — a `Load` must be ANSWERED, and the answer is a `'msg`. That means the
                        // persistence sink has to re-enter `dispatchHostMsg`, which is defined below (it
                        // needs `currentModel`, which the fold deliberately does not have). A forward
                        // reference through a mutable is what ties that knot; the alternative is a second
                        // copy of the update path, and a second copy of the update path is exactly how
                        // #429's two effect folds drifted until one of them silently dropped audio.
                        //
                        // #535 — a `Load` must be ANSWERED, and the answer is a `'msg`, so the persistence
                        // sink has to re-enter `dispatchHostMsg`, which itself interprets effects. That knot
                        // is tied with `let rec … and …`, NOT with a forward-declared mutable.
                        //
                        // The mutable version shipped a bug and it is worth naming: `dispatchOutcome` started
                        // as `ignore` and was assigned AFTER `interpretEffects initEffects`, so a product that
                        // loaded its save on `Init` — the single most common persistence pattern — had the save
                        // read off the disk and the outcome dropped on the floor. Mutual recursion makes that
                        // unrepresentable: there is no window in which the dispatcher is not yet itself.
                        //
                        // RE-ENTRANCY IS SYNCHRONOUS RECURSION, and it is NOT the benign Elmish self-feed:
                        // dispatchOutcome -> dispatchHostMsg -> interpretEffects -> persistenceBatchSink is ONE
                        // STACK. A product that emits a `Persist` in response to its own save outcome recurses
                        // until StackOverflowException — which .NET cannot catch, so `tryProductStep` will NOT
                        // save it and the process dies with no diagnostic. Do not emit a persistence effect
                        // from the handler for a persistence outcome.
                        let mutable outcomeCloseRequested = false

                        let rec interpretEffects effects =
                            let closeRequested =
                                interpretViewerEffects audioSink persistenceBatchSink onScene onInputDispatch onDiagnostic evidenceSink effects

                            // Sticky: a close is terminal, so an outcome-driven message that asked to close must
                            // not be forgotten by the next batch that did not.
                            closeRequested || outcomeCloseRequested

                        and persistenceBatchSink batch =
                            match persistenceSink with
                            | Some sink ->
                                if dispatchPersistenceBatch sink mapOutcome dispatchHostMsg batch then
                                    outcomeCloseRequested <- true
                            | None ->
                                // #416 — A DROPPED REQUEST MUST SAY SO. This launch was given no sink, so it
                                // owns no save location and cannot perform the batch. Dropping it is the honest
                                // behaviour; dropping it SILENTLY is the silent-no-op this epic exists to kill,
                                // and `Persistence.fs` says so itself: "candor in a comment is not a mechanism —
                                // it does not survive being called from another file."
                                onDiagnostic
                                    { Level = ViewerDiagnosticLevel.Warning
                                      Category = Frame
                                      Message =
                                        $"A product emitted {List.length batch} PersistenceEffect(s), but this launch has no persistence sink — the requests were DROPPED and nothing was saved, loaded or deleted. Launch with Viewer.runAppWithPersistence (or runAppWithAudioAndPersistence) and supply a sink that performs the I/O."
                                      FrameIndex = None
                                      Stage = None
                                      Elapsed = None }

                        and dispatchHostMsg msg =
                            match tryProductStep reportProductDefect "Update" (fun () -> host.Update msg currentModel) with
                            | None -> false // product Update threw; drop the message, keep window + last-good scene
                            | Some(next, effects) ->
                                currentModel <- next
                                currentScene <- safeView currentModel
                                interpretEffects effects

                        let initialCloseRequested = interpretEffects initEffects

                        let _, _ =
                            update
                                Start
                                { Options = options
                                  WindowBehavior = behavior
                                  IsRunning = false
                                  LifecycleState = NotStarted
                                  FirstFramePresented = false
                                  UserCloseObserved = false
                                  InputDispatch = NotRequired
                                  LastScene = None }

                        let handleTick elapsed =
                            match host.Tick elapsed with
                            | Some msg -> dispatchHostMsg msg
                            | None -> false

                        let handleKey rawKey isDown =
                            let key, normalizedDown =
                                ViewerKeyboard.normalizeEvent
                                    { RawKey = rawKey
                                      Direction =
                                        if isDown then
                                            ViewerKeyDirection.KeyDown
                                        else
                                            ViewerKeyDirection.KeyUp }

                            match host.MapKey key normalizedDown with
                            | Some msg ->
                                inputDispatch <- "true"
                                dispatchHostMsg msg
                            | None ->
                                inputDispatch <- "false"
                                // F1: a key that maps to no product message may still have changed
                                // host-internal runtime state (focus traversal, scroll keys); re-derive so
                                // it renders on THIS key. (Previously only the full-interactive loop did this.)
                                currentScene <- runtimeStateRepaint false currentScene (fun () -> safeView currentModel)
                                false

                        let inputVerified () =
                            not (requireInputDispatchVerification ()) || inputDispatch = "true"

                        match runPresentedPersistentWindow options behavior host.Diagnostics inputDispatch presentScene handleTick (Some handleKey) None (Some handleResize) None inputVerified None with
                        | Result.Ok outcome ->
                            Result.Ok(
                                { outcome with
                                    InputDispatch = inputDispatch
                                    OptionResults = validateWindowLaunchBehavior options.InitialSize behavior
                                    ExitPath = initialCloseRequested || outcome.ExitPath
                                    Message = "Persistent generated app host launch completed after intentional close." }
                            )
                        | Result.Error failure -> Result.Error failure

    let runAppWithWindowBehavior options behavior (host: GeneratedAppHost<'model, 'msg>) =
        runGeneratedApp options behavior ignore None (fun (_: PersistenceOutcome) -> None) host

    let runApp options host =
        runAppWithWindowBehavior options defaultWindowBehavior host

    let runAppWithWindowBehaviorAndAudio options behavior audioSink (host: GeneratedAppHost<'model, 'msg>) =
        runGeneratedApp options behavior audioSink None (fun (_: PersistenceOutcome) -> None) host

    let runAppWithAudio options audioSink (host: GeneratedAppHost<'model, 'msg>) =
        runGeneratedApp options defaultWindowBehavior audioSink None (fun (_: PersistenceOutcome) -> None) host

    // #535 — the seam a product actually needs: the sink PERFORMS the save/load (it is the only thing in
    // the process that owns a save location), and every `PersistenceOutcome` it returns is dispatched back
    // into `update` through `mapOutcome`. That is what makes a `Load` answerable; before this, a product
    // could ask and nothing could reply.
    //
    // The sink is the caller's, and deliberately so: the framework does not own the slot -> path mapping
    // (`SaveSlot` is an opaque, product-owned name), so a viewer that invented one would be guessing where
    // a player's saves live.
    let runAppWithPersistence options persistenceSink mapOutcome (host: GeneratedAppHost<'model, 'msg>) =
        runGeneratedApp options defaultWindowBehavior ignore (Some persistenceSink) mapOutcome host

    // A game usually wants both. Without this, adopting persistence would mean giving up sound — which is
    // the kind of forced choice that gets a seam worked around rather than used.
    let runAppWithAudioAndPersistence options audioSink persistenceSink mapOutcome (host: GeneratedAppHost<'model, 'msg>) =
        runGeneratedApp options defaultWindowBehavior audioSink (Some persistenceSink) mapOutcome host

    // Feature 085 — pointer-aware, size-aware durable launch. Mirrors
    // `runAppWithWindowBehavior` but routes native pointer events and resizes to the host,
    // and renders a size-aware `View`. `runApp`/`GeneratedAppHost` are untouched (FR-006).
    //
    // Issue #429 — `audioSink` receives every `PlayAudio` batch in dispatch order, exactly as it does
    // in `runGeneratedApp`. The two host families are now symmetric in audio: a product that needs a
    // pointer AND sound (every game with a menu) no longer has to choose. The sinkless entry points
    // pass `ignore`, which is why they keep behaving exactly as before.
    let private runInteractiveViewerWithWindowBehaviorCore
        options
        behavior
        script
        (audioSink: AudioEffect list -> unit)
        (host: InteractiveViewerHost<'model,'msg>)
        =
        match validateOptions options with
        | Result.Error failure -> Result.Error failure
        | Result.Ok() ->
            let optionFailures =
                validateWindowLaunchBehavior options.InitialSize behavior
                |> List.filter (fun result -> result.Status = FailedOption)

            if not (List.isEmpty optionFailures) then
                let message =
                    optionFailures
                    |> List.map (fun result -> $"{result.Option}: {result.Message}")
                    |> String.concat "; "

                Result.Error(makeFailure Window ProductDefect ViewerDiagnosticCategory.Startup message None)
            else
                let capability = runtimeCapability ()

                if not capability.PersistentWindow then
                    Result.Error(persistentUnsupportedFailure capability)
                else
                    let model, initEffects = host.Init()
                    let mutable currentModel = model
                    // Issue #246/#400: `currentSurfaceSize` is the PHYSICAL framebuffer the product
                    // renders onto at native resolution; `currentWindowSize` is the LOGICAL window Silk
                    // reports pointer input in. `currentSize` is the space the product's `View`/pointer
                    // map speak — the fixed `LogicalSize` when set, otherwise the physical framebuffer.
                    // At scale 1 all three coincide and every seam behaves exactly as before #400; on a
                    // scaled display the product draws at full framebuffer resolution and the host owns
                    // the fit. Both start at `InitialSize`; the load-time `FramebufferResized` seed
                    // (issue #400) supplies the true physical size before the first steady-state frame.
                    let mutable currentSurfaceSize = options.InitialSize
                    let mutable currentWindowSize = options.InitialSize
                    let viewSize () = options.LogicalSize |> Option.defaultValue currentSurfaceSize
                    let mutable currentSize = viewSize ()
                    // Issue #365: a product `Update`/`View` fault is an App-stage defect captured through
                    // the host's diagnostics, never a window teardown.
                    let reportProductDefect ev = captureDiagnostic host.Diagnostics ev |> ignore
                    // Issue #396: guard the FIRST product View — as in the generated-app runner, a
                    // first-frame throw has no last-good scene to fall back to, so it fails the run as a
                    // startup App-stage ProductDefect rather than escaping as an uncaught exception.
                    match tryFirstProductView reportProductDefect "View" (fun () -> host.View currentSize currentModel) with
                    | Result.Error failure -> Result.Error failure
                    | Result.Ok initialScene ->
                        let mutable currentScene = initialScene
                        let mutable inputDispatch = "false"
                        // Issue #365: guard the product `Update`/`View` so one throwing step drops that input
                        // and keeps the persistent window on its last-good scene, rather than escaping to a
                        // teardown mislabeled `frameRenderFailed`.
                        let safeView size model =
                            tryProductStep reportProductDefect "View" (fun () -> host.View size model)
                            |> Option.defaultValue currentScene

                        let presentScene () = presentedFor options currentSurfaceSize currentScene

                        // Bound once per launch, not per call: `interpretEffects` runs on every dispatched
                        // message, tick and pointer sample, so building these closures inline would
                        // allocate three per frame.
                        let onScene scene = currentScene <- scene
                        let onInputDispatch () = inputDispatch <- "true"
                        let onDiagnostic diagnostic = captureDiagnostic host.Diagnostics diagnostic |> ignore

                        // Issue #444: evidence rasterizes at `currentSize` — the space this host's `View`
                        // actually authors in (the fixed `LogicalSize` when set, else the physical
                        // framebuffer), so the image depicts what the product drew.
                        let evidenceSink =
                            productEvidenceSink onDiagnostic (fun () -> currentSize) (fun () -> currentScene)

                        // #535 — the interactive (Controls) host family owns no persistence seam yet, and
                        // `InteractiveViewerHost.Update` returns `ViewerEffect list`, so a product on THIS host
                        // can emit a `Persist` and there is nothing to perform it.
                        //
                        // It is dropped — and it SAYS SO. #429 is the precedent: the interactive fold left
                        // `PlayAudio` in the discard group and a product got silence with nothing objecting. A
                        // named no-op fixes that for a reader of this file and for nobody else; the product
                        // author needs it on the diagnostics channel, which is right here in scope.
                        let persistenceBatchSink (batch: PersistenceEffect list) =
                            onDiagnostic
                                { Level = ViewerDiagnosticLevel.Warning
                                  Category = Frame
                                  Message =
                                    $"A product emitted {List.length batch} PersistenceEffect(s) on the interactive (Controls) host, which has no persistence seam — the requests were DROPPED and nothing was saved, loaded or deleted. The generated-app host supports this via Viewer.runAppWithPersistence; the interactive equivalent does not exist yet."
                                  FrameIndex = None
                                  Stage = None
                                  Elapsed = None }

                        let interpretEffects effects =
                            interpretViewerEffects audioSink persistenceBatchSink onScene onInputDispatch onDiagnostic evidenceSink effects

                        let initialCloseRequested = interpretEffects initEffects

                        let dispatchHostMsg msg =
                            let msgText = (sprintf "%A" msg).Replace(" ", "_").Replace(Environment.NewLine, "_")
                            let updateSw = Stopwatch.StartNew()
                            RenderLagTrace.emit "model-update-start" [ "msg", msgText ]

                            match tryProductStep reportProductDefect "Update" (fun () -> host.Update msg currentModel) with
                            | None ->
                                // Issue #365: a throwing product Update drops this input and keeps the window
                                // alive on its last-good model/scene, rather than tearing it down.
                                updateSw.Stop()
                                RenderLagTrace.emit
                                    "model-update-end"
                                    [ "msg", msgText
                                      "durationMs", updateSw.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)
                                      "dropped", "true" ]

                                false
                            | Some(next, effects) ->
                                updateSw.Stop()
                                RenderLagTrace.emit
                                    "model-update-end"
                                    [ "msg", msgText
                                      "durationMs", updateSw.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) ]
                                currentModel <- next
                                let viewSw = Stopwatch.StartNew()
                                RenderLagTrace.emit "view-start" [ "msg", msgText ]
                                currentScene <- safeView currentSize currentModel
                                viewSw.Stop()
                                RenderLagTrace.emit
                                    "view-end"
                                    [ "msg", msgText
                                      "durationMs", viewSw.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) ]
                                let effectsSw = Stopwatch.StartNew()
                                interpretEffects effects
                                |> fun closeRequested ->
                                    effectsSw.Stop()
                                    RenderLagTrace.emit
                                        "effects-end"
                                        [ "msg", msgText
                                          "durationMs", effectsSw.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture)
                                          "closeRequested", string closeRequested ]
                                    closeRequested

                        let handleTick elapsed =
                            match host.Tick elapsed with
                            | Some msg -> dispatchHostMsg msg
                            | None -> false

                        let handleKey rawKey isDown =
                            let key, normalizedDown =
                                ViewerKeyboard.normalizeEvent
                                    { RawKey = rawKey
                                      Direction =
                                        if isDown then
                                            ViewerKeyDirection.KeyDown
                                        else
                                            ViewerKeyDirection.KeyUp }

                            let msgs = host.MapKey key normalizedDown
                            if not (List.isEmpty msgs) then
                                RenderLagTrace.emit
                                    "key-routed"
                                    [ "key", rawKey
                                      "isDown", string normalizedDown
                                      "messageCount", string msgs.Length ]
                                inputDispatch <- "true"
                            let closeRequested = msgs |> List.fold (fun close msg -> dispatchHostMsg msg || close) false
                            // F1 general repaint signal: if the key produced no product message it may still
                            // have changed runtime state (focus traversal, scroll keys); re-derive so it
                            // renders on THIS key, not the next. (When messages ran, dispatchHostMsg already
                            // re-derived, so this is a no-op.)
                            currentScene <- runtimeStateRepaint (not (List.isEmpty msgs)) currentScene (fun () -> safeView currentSize currentModel)
                            closeRequested

                        let handlePointer (input: ViewerPointerInput) =
                            let pointerSw = Stopwatch.StartNew()
                            // The trace keeps SURFACE coordinates, matching what the input queue recorded for
                            // this same event — the two are correlated during render-lag analysis, so they must
                            // speak one coordinate space.
                            RenderLagTrace.emit
                                "pointer-route-start"
                                [ "phase", string input.Phase
                                  "x", input.X.ToString("0.###", CultureInfo.InvariantCulture)
                                  "y", input.Y.ToString("0.###", CultureInfo.InvariantCulture) ]

                            // Issue #400: Silk delivers the pointer in LOGICAL window coordinates
                            // (`IMouse.Position`), but the product now renders and hit-tests in the PHYSICAL
                            // framebuffer space (native resolution). Scale into physical FIRST, so every
                            // downstream mapping speaks the surface the scene was drawn onto.
                            let physicalX, physicalY =
                                LogicalCanvas.toPhysicalPoint currentWindowSize currentSurfaceSize input.X input.Y

                            // Issue #246: a `LogicalSize` product draws through the letterbox scale+offset,
                            // so route the inverse (now from the physical surface) or every hit test is wrong
                            // by exactly that transform. Without one, the physical coordinates ARE the
                            // product's space. `DeltaX/DeltaY` are wheel ticks, not positions, so unscaled.
                            let routed =
                                match options.LogicalSize with
                                | Some logical ->
                                    let x, y = LogicalCanvas.toLogicalPoint logical currentSurfaceSize physicalX physicalY
                                    { input with X = x; Y = y }
                                | None -> { input with X = physicalX; Y = physicalY }

                            let msgs = host.MapPointer routed currentSize currentModel
                            pointerSw.Stop()
                            RenderLagTrace.emit
                                "pointer-route-end"
                                [ "phase", string input.Phase
                                  "messageCount", string msgs.Length
                                  "durationMs", pointerSw.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) ]

                            if not (List.isEmpty msgs) then
                                inputDispatch <- "true"

                            let closeRequested = msgs |> List.fold (fun close msg -> dispatchHostMsg msg || close) false

                            // F1 general repaint signal: `MapPointer` may mutate host-internal runtime state
                            // (focus/hover/scroll) WITHOUT producing a product message, leaving the model
                            // unchanged so `dispatchHostMsg` never re-derived — the "focus one click behind",
                            // dead-hover, and dead-scroll class. `runtimeStateRepaint` re-derives from
                            // `host.View` (the single source reflecting model + every runtime ref) on the
                            // no-message path, and is a no-op when messages already drove a re-derive.
                            currentScene <- runtimeStateRepaint (not (List.isEmpty msgs)) currentScene (fun () -> safeView currentSize currentModel)

                            closeRequested

                        let handleResize (size: Size) =
                            // Issue #400: `Resized` carries the LOGICAL window size (Silk `window.Size`) —
                            // the space Silk reports pointer input in. The product renders at the PHYSICAL
                            // framebuffer size (owned by `handleFramebufferResize`), so a logical resize only
                            // updates the pointer-scaling reference; the paired `FramebufferResized` owns the
                            // surface change and any re-derive.
                            currentWindowSize <- size

                        let handleFramebufferResize (size: Size) =
                            currentSurfaceSize <- size
                            // Issue #400: the live scene is authored at this physical size, so the present
                            // path must NOT upscale it a second time — publish it as the fit's authoring size
                            // (identity fit). `GlHost.run` clears the override per run.
                            Host.GlHost.setLiveAuthoringSizeOverride (Some size)
                            // Re-derive only when the space the product draws in actually moved. With a
                            // `LogicalSize` it never does — `presentScene` alone applies the new fit — so a
                            // fixed-resolution game does not re-render once per framebuffer-resize tick.
                            let nextViewSize = viewSize ()

                            if nextViewSize <> currentSize then
                                currentSize <- nextViewSize
                                currentScene <- safeView currentSize currentModel

                        let inputVerified () =
                            not (requireInputDispatchVerification ()) || inputDispatch = "true"

                        match
                            runPresentedPersistentWindow
                                options
                                behavior
                                host.Diagnostics
                                inputDispatch
                                presentScene
                                handleTick
                                (Some handleKey)
                                (Some handlePointer)
                                (Some handleResize)
                                (Some handleFramebufferResize)
                                inputVerified
                                script
                        with
                        | Result.Ok outcome ->
                            Result.Ok(
                                { outcome with
                                    InputDispatch = inputDispatch
                                    OptionResults = validateWindowLaunchBehavior options.InitialSize behavior
                                    ExitPath = initialCloseRequested || outcome.ExitPath
                                    Message = "Persistent interactive viewer launch completed after intentional close." }
                            )
                        | Result.Error failure -> Result.Error failure

    let runInteractiveViewerWithWindowBehavior options behavior host =
        runInteractiveViewerWithWindowBehaviorCore options behavior None ignore host

    let runInteractiveViewer options host =
        runInteractiveViewerWithWindowBehavior options defaultWindowBehavior host

    let runInteractiveViewerScriptWithWindowBehavior options behavior script host =
        runInteractiveViewerWithWindowBehaviorCore options behavior (Some script) ignore host

    let runInteractiveViewerScript options script host =
        runInteractiveViewerScriptWithWindowBehavior options defaultWindowBehavior script host

    let runInteractiveViewerWithWindowBehaviorAndAudio options behavior audioSink (host: InteractiveViewerHost<'model,'msg>) =
        runInteractiveViewerWithWindowBehaviorCore options behavior None audioSink host

    let runInteractiveViewerWithAudio options audioSink (host: InteractiveViewerHost<'model,'msg>) =
        runInteractiveViewerWithWindowBehaviorAndAudio options defaultWindowBehavior audioSink host

    // Issue #438: the scripted siblings of the two above. #429 threaded an `audioSink` through
    // `runInteractiveViewerWithWindowBehaviorCore`, but the SCRIPTED entry points kept handing it
    // `ignore` — so a product driven by a script emitted `PlayAudio` and the batch was dropped with no
    // error and no diagnostic, which is the very silent discard #429 was filed about, surviving in the
    // one path the evidence/responsiveness tooling actually drives. These pass the sink the core has
    // always accepted; the sinkless `runInteractiveViewerScript*` are unchanged.
    let runInteractiveViewerScriptWithWindowBehaviorAndAudio
        options
        behavior
        script
        audioSink
        (host: InteractiveViewerHost<'model,'msg>)
        =
        runInteractiveViewerWithWindowBehaviorCore options behavior (Some script) audioSink host

    let runInteractiveViewerScriptWithAudio options script audioSink (host: InteractiveViewerHost<'model,'msg>) =
        runInteractiveViewerScriptWithWindowBehaviorAndAudio options defaultWindowBehavior script audioSink host

    let runAppEvidence (request: ViewerRunRequest) options (host: GeneratedAppHost<'model, 'msg>) =
        let model, _ = host.Init()
        let reportProductDefect ev = captureDiagnostic host.Diagnostics ev |> ignore
        // Issue #396: guard the one-shot product View. A throw here is an App-stage ProductDefect (the
        // #365 classification), reported and written to the evidence path exactly like a bounded-run
        // failure, rather than escaping runAppEvidence as an uncaught exception.
        match tryFirstProductView reportProductDefect "View" (fun () -> host.View model) with
        | Result.Error failure ->
            request.EvidencePath |> Option.iter (fun path -> writeLaunchFailure path "persistent-evidence" "runAppEvidence" failure)
            Result.Error failure
        | Result.Ok scene ->
            // Issue #246: `runBounded` fits the scene to the evidence surface itself, so hand it the raw
            // view output; the visual artifacts render off the same surface and need the fitted scene.
            match runBounded request options scene with
            | Result.Ok evidence ->
                let visualEvidence =
                    VisualEvidenceHandling.artifacts request options (presentedFor options options.InitialSize scene)

                let outcome =
                    { Status = "ok"
                      Mode = "persistent-evidence"
                      Command = Some "runAppEvidence"
                      RendererMode = evidence.RendererMode
                      WindowOpened = true
                      WindowVisible = ViewerObservedValue.Unsupported
                      FirstFramePresented = evidence.FramesRendered > 0
                      CloseReason = Some EvidenceRequestedClose
                      UserCloseObserved = false
                      AppCloseObserved = false
                      EvidenceCloseObserved = true
                      SelfClosedForEvidence = true
                      InputDispatch = "not-required"
                      ExitPath = true
                      WindowDiagnostics = []
                      OptionResults = []
                      VisualEvidence = visualEvidence
                      FailureClass = None
                      BlockedStage = None
                      Classification = None
                      Category = None
                      Message = "Persistent evidence launch completed after evidence target." }

                request.EvidencePath
                |> Option.iter (fun path ->
                    if not (isPngPath path) && parseRendererMode request.RendererMode <> RendererModeKind.MetadataHash then
                        writeLaunchOutcome path outcome)

                Result.Ok outcome
            | Result.Error failure ->
                request.EvidencePath |> Option.iter (fun path -> writeLaunchFailure path "persistent-evidence" "runAppEvidence" failure)
                Result.Error failure

    let private captureScreenshotEvidenceResult (request: ScreenshotEvidenceRequest) (options: ViewerOptions) scene : ScreenshotEvidenceResult =
        let diagnostics =
            [ if request.Width <= 0 then
                  "screenshot width must be positive"
              if request.Height <= 0 then
                  "screenshot height must be positive"
              if request.Timeout <= TimeSpan.Zero then
                  "screenshot timeout must be positive" ]

        if not diagnostics.IsEmpty then
            { Status = ScreenshotFailed
              Command = request.Command
              AppOrSample = request.AppOrSample
              HostFacts = request.HostFacts
              CaptureMode = request.CaptureMode
              EvidenceKind = "screenshot"
              OutputPath = Some request.OutputPath
              ScreenshotPath = None
              Width = None
              Height = None
              PixelContentValidation = PixelContentNotValidated "request validation failed before capture"
              RendererMode = request.RendererMode
              FramesRendered = None
              ViewerOpenStatus = ViewerOpenUnknown
              FirstFrameStatus = FirstFrameUnknownStatus
              CaptureAvailability = CaptureAvailabilityUnknown "request validation failed before host launch"
              CaptureSource = NoCaptureSource
              DeterministicFallbackKind = None
              ProvesScreenshot = false
              BlockedStage = Some ViewerRunBlockedStage.Capture
              Classification = Some ProductDefect
              Category = Some ViewerDiagnosticCategory.Screenshot
              Message = "Screenshot evidence request validation failed."
              Timestamp = DateTimeOffset.UnixEpoch
              UnsupportedHostReason = None
              Fallback = None
              Diagnostics = diagnostics }
        else
            let screenshotPath =
                if isPngPath request.OutputPath then
                    request.OutputPath
                else
                    IO.Path.ChangeExtension(request.OutputPath, ".png") |> string

            let screenshotSize: FS.GG.UI.Scene.Size = { Width = request.Width; Height = request.Height }
            let written = writeSceneImageEvidence screenshotPath screenshotSize scene
            let dimensions, pixelValidation = pngDimensionsAndNonBlank screenshotPath

            match written, dimensions, pixelValidation with
            | true, Some(width, height), PixelContentNonBlank ->
                { Status = ScreenshotOk
                  Command = request.Command
                  AppOrSample = request.AppOrSample
                  HostFacts = request.HostFacts
                  CaptureMode = request.CaptureMode
                  EvidenceKind = "screenshot"
                  OutputPath = Some request.OutputPath
                  ScreenshotPath = Some screenshotPath
                  Width = Some width
                  Height = Some height
                  PixelContentValidation = PixelContentNonBlank
                  RendererMode = request.RendererMode
                  FramesRendered = Some 1
                  ViewerOpenStatus = ViewerOpenConfirmed
                  FirstFrameStatus = FirstFramePresentedStatus
                  CaptureAvailability = CaptureAvailable
                  // #141: this path always rasterizes offscreen through `writeSceneImageEvidence`
                  // (CPU `SKBitmap`, no GL context, no window), so it names the offscreen scene raster
                  // it really performs — not a `LiveViewerWindow` that never opened. `ProvesScreenshot`
                  // stays true: a real, non-blank pixel artifact genuinely was produced (only the
                  // capture-source/message misled; renderer-mode=skia is honest — see the issue).
                  CaptureSource = OffscreenSceneRaster
                  DeterministicFallbackKind = None
                  ProvesScreenshot = true
                  BlockedStage = None
                  Classification = None
                  Category = None
                  Message = "Screenshot artifact rendered by offscreen CPU scene raster (no live viewer window)."
                  Timestamp = DateTimeOffset.UtcNow
                  UnsupportedHostReason = None
                  Fallback = None
                  Diagnostics =
                      [ "status=ok"
                        "evidence-kind=screenshot"
                        $"artifact-path={screenshotPath}"
                        $"image-width={width}"
                        $"image-height={height}"
                        "pixel-content-validation=non-blank"
                        "capture-source=offscreen-scene-raster"
                        "proves-screenshot=true"
                        $"scene-capabilities={Scene.describe { Nodes = [ scene ] } |> List.length}" ] }
            | _ ->
                let message =
                    match pixelValidation with
                    | PixelContentBlank -> "Screenshot PNG was blank."
                    | PixelContentUnreadable reason -> reason
                    | PixelContentNotValidated reason -> reason
                    | PixelContentNonBlank -> "Screenshot PNG write failed."

                { Status = ScreenshotFailed
                  Command = request.Command
                  AppOrSample = request.AppOrSample
                  HostFacts = request.HostFacts
                  CaptureMode = request.CaptureMode
                  EvidenceKind = "screenshot"
                  OutputPath = Some request.OutputPath
                  ScreenshotPath = if IO.File.Exists screenshotPath then Some screenshotPath else None
                  Width = dimensions |> Option.map fst
                  Height = dimensions |> Option.map snd
                  PixelContentValidation = pixelValidation
                  RendererMode = request.RendererMode
                  FramesRendered = Some 1
                  ViewerOpenStatus = ViewerOpenConfirmed
                  FirstFrameStatus = FirstFramePresentedStatus
                  CaptureAvailability = CaptureAvailable
                  // #141: same offscreen CPU raster path as the success branch — name the raster it
                  // actually ran, not a live viewer window.
                  CaptureSource = OffscreenSceneRaster
                  DeterministicFallbackKind = None
                  ProvesScreenshot = false
                  BlockedStage = Some ViewerRunBlockedStage.Capture
                  Classification = Some ProductDefect
                  Category = Some ViewerDiagnosticCategory.Screenshot
                  Message = message
                  Timestamp = DateTimeOffset.UtcNow
                  UnsupportedHostReason = None
                  Fallback = None
                  Diagnostics = diagnostics @ [ $"failure={message}" ] }

    module ScreenshotEvidenceHandling =
        let capture request options scene =
            captureScreenshotEvidenceResult request options scene

    let captureScreenshotEvidence request options scene =
        ScreenshotEvidenceHandling.capture request options scene

    let initEvidenceWorkflow (request: ScreenshotEvidenceRequest) =
        let model: EvidenceWorkflowModel =
            { Request = request
              ViewerOpenStatus = ViewerOpenUnknown
              FirstFrameStatus = FirstFrameUnknownStatus
              CaptureAvailability = CaptureAvailabilityUnknown "capture capability not yet checked"
              OutputPath = Some request.OutputPath
              Result = None
              Diagnostics = [] }

        model, [ LaunchViewerForEvidence request ]

    let updateEvidenceWorkflow (msg: EvidenceWorkflowMsg) (model: EvidenceWorkflowModel) =
        match msg with
        | LaunchStarted ->
            { model with Diagnostics = model.Diagnostics @ [ "launch-started=true" ] },
            [ CollectProcessOutput ]
        | LaunchCompleted status ->
            { model with ViewerOpenStatus = status },
            []
        | FirstFrameObserved status ->
            { model with FirstFrameStatus = status },
            [ CaptureViewerScreenshot model.Request.OutputPath ]
        | CaptureCapabilityKnown availability ->
            { model with CaptureAvailability = availability },
            []
        | CaptureSucceeded(path, width, height, source) ->
            let result: ScreenshotEvidenceResult =
                { Status = ScreenshotOk
                  Command = model.Request.Command
                  AppOrSample = model.Request.AppOrSample
                  HostFacts = model.Request.HostFacts
                  CaptureMode = model.Request.CaptureMode
                  EvidenceKind = "screenshot"
                  OutputPath = model.OutputPath
                  ScreenshotPath = Some path
                  Width = Some width
                  Height = Some height
                  PixelContentValidation = PixelContentNonBlank
                  RendererMode = model.Request.RendererMode
                  FramesRendered = Some 1
                  ViewerOpenStatus = model.ViewerOpenStatus
                  FirstFrameStatus = model.FirstFrameStatus
                  CaptureAvailability = CaptureAvailable
                  CaptureSource = source
                  DeterministicFallbackKind = None
                  ProvesScreenshot = source = LiveViewerWindow
                  BlockedStage = None
                  Classification = None
                  Category = None
                  Message = "Screenshot artifact captured from live viewer output."
                  Timestamp = DateTimeOffset.UnixEpoch
                  UnsupportedHostReason = None
                  Fallback = None
                  Diagnostics =
                      model.Diagnostics
                      @ [ "status=ok"
                          "evidence-kind=screenshot"
                          $"screenshot-path={path}"
                          $"dimensions={width}x{height}"
                          $"capture-source={source}" ] }

            { model with
                CaptureAvailability = CaptureAvailable
                Result = Some result },
            [ ValidateScreenshotArtifact path
              WriteScreenshotEvidenceReport result
              CleanupEvidenceViewer ]
        | CaptureUnsupported(reason, fallbackKind) ->
            let result: ScreenshotEvidenceResult =
                { Status = ScreenshotUnsupported
                  Command = model.Request.Command
                  AppOrSample = model.Request.AppOrSample
                  HostFacts = model.Request.HostFacts
                  CaptureMode = model.Request.CaptureMode
                  EvidenceKind = "screenshot"
                  OutputPath = model.OutputPath
                  ScreenshotPath = None
                  Width = None
                  Height = None
                  PixelContentValidation = PixelContentNotValidated reason
                  RendererMode = model.Request.RendererMode
                  FramesRendered = None
                  ViewerOpenStatus = model.ViewerOpenStatus
                  FirstFrameStatus = model.FirstFrameStatus
                  CaptureAvailability = CaptureUnavailable reason
                  CaptureSource = fallbackKind |> Option.map (fun _ -> DeterministicSceneRender) |> Option.defaultValue NoCaptureSource
                  DeterministicFallbackKind = fallbackKind
                  ProvesScreenshot = false
                  BlockedStage = Some Capture
                  Classification = Some UnsupportedEnvironment
                  Category = Some ViewerDiagnosticCategory.Screenshot
                  Message = reason
                  Timestamp = DateTimeOffset.UnixEpoch
                  UnsupportedHostReason = Some reason
                  Fallback = fallbackKind
                  Diagnostics = model.Diagnostics @ [ "status=unsupported"; $"unsupported-host-reason={reason}" ] }

            { model with
                CaptureAvailability = CaptureUnavailable reason
                Result = Some result },
            [ WriteScreenshotEvidenceReport result ]
        | CaptureFailed message ->
            let result: ScreenshotEvidenceResult =
                { Status = ScreenshotFailed
                  Command = model.Request.Command
                  AppOrSample = model.Request.AppOrSample
                  HostFacts = model.Request.HostFacts
                  CaptureMode = model.Request.CaptureMode
                  EvidenceKind = "screenshot"
                  OutputPath = model.OutputPath
                  ScreenshotPath = None
                  Width = None
                  Height = None
                  PixelContentValidation = PixelContentNotValidated message
                  RendererMode = model.Request.RendererMode
                  FramesRendered = None
                  ViewerOpenStatus = model.ViewerOpenStatus
                  FirstFrameStatus = model.FirstFrameStatus
                  CaptureAvailability = model.CaptureAvailability
                  CaptureSource = NoCaptureSource
                  DeterministicFallbackKind = None
                  ProvesScreenshot = false
                  BlockedStage = Some Capture
                  Classification = Some ProductDefect
                  Category = Some ViewerDiagnosticCategory.Screenshot
                  Message = message
                  Timestamp = DateTimeOffset.UnixEpoch
                  UnsupportedHostReason = None
                  Fallback = None
                  Diagnostics = model.Diagnostics @ [ $"failure={message}" ] }

            { model with Result = Some result },
            [ WriteScreenshotEvidenceReport result ]
        | EvidenceReportWritten path ->
            { model with OutputPath = Some path; Diagnostics = model.Diagnostics @ [ $"report-written={path}" ] },
            []

