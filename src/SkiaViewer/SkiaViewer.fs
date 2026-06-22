namespace FS.GG.UI.SkiaViewer

open System
open System.Diagnostics
open System.Globalization
open System.IO
open System.Text.Json
open System.Threading
open Elmish
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

module internal RenderLagTrace =
    /// S3 (Feature 175): one structured live-trace event — the event name plus its key/value fields
    /// (e.g. focus/hover/scroll resolution, binding dispatch, model-update/view timing).
    type TraceEvent = { Event: string; Fields: (string * string) list }

    let private stderrEnabled =
        String.Equals(Environment.GetEnvironmentVariable("FS_GG_RENDER_LAG_TRACE"), "1", StringComparison.Ordinal)

    // S3 read-back path: an in-memory ring that captures emitted events so a test or tool can OBSERVE
    // live state programmatically — without the env var and without a repack-to-instrument loop (the
    // friction that made diagnosing the Feature-175 focus lag slow). Independent of the stderr toggle.
    let private captured = System.Collections.Concurrent.ConcurrentQueue<TraceEvent>()
    let mutable private capturing = false

    /// Begin in-memory capture (clears any prior buffer); pair with `drainCapture`. The buffer is
    /// process-global, so a deterministic test should assert on the PRESENCE of its uniquely-named
    /// events rather than an exact list (other activity may emit concurrently).
    let startCapture () =
        captured.Clear()
        capturing <- true

    /// Stop capture and return the events recorded since `startCapture`, in emission order.
    let drainCapture () =
        capturing <- false
        let events = captured.ToArray() |> List.ofArray
        captured.Clear()
        events

    let emit eventName fields =
        if capturing then
            captured.Enqueue { Event = eventName; Fields = fields }

        if stderrEnabled then
            let fieldsText =
                fields
                |> List.map (fun (name, value) -> $"{name}={value}")
                |> String.concat " "

            let suffix = if String.IsNullOrWhiteSpace fieldsText then "" else " " + fieldsText
            let ts = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            let ticks = Stopwatch.GetTimestamp()
            Console.Error.WriteLine($"FS_GG_RENDER_LAG_TRACE ts={ts} ticks={ticks} event={eventName}{suffix}")

type private LegacyHostMsg<'msg> =
    | LegacyLoaded
    | LegacyUpdateTick of float
    | LegacyRenderTick of float
    | LegacyKey of rawKey: string * isDown: bool
    | LegacyPointer of ViewerPointerInput
    | LegacyResized of Size
    | LegacyCloseRequested
    | LegacyDiagnosticReported of Host.RenderDiagnostic
    | LegacyHostEffect of Host.ViewerEffect<LegacyHostMsg<'msg>>
    | LegacyAppMsg of 'msg

type private LegacyQueuedInput =
    | QueuedLegacyKey of rawKey: string * isDown: bool
    | QueuedLegacyPointer of ViewerPointerInput

module Viewer =
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

    module DiagnosticsFiltering =
        let levelRank level =
            match level with
            | ViewerDiagnosticLevel.Error -> 0
            | ViewerDiagnosticLevel.Warning -> 1
            | ViewerDiagnosticLevel.Info -> 2
            | ViewerDiagnosticLevel.Debug -> 3
            | ViewerDiagnosticLevel.Trace -> 4

        let frameAllowed options (diagnostic: ViewerDiagnosticEvent) =
            match diagnostic.Category, options.FrameLogLimit, diagnostic.FrameIndex with
            | ViewerDiagnosticCategory.Frame, Some limit, Some frameIndex -> limit > 0 && frameIndex <= limit
            | ViewerDiagnosticCategory.Frame, Some limit, None -> limit <> 0
            | ViewerDiagnosticCategory.Frame, None, _ -> true
            | _ -> true

        let shouldCapture options (diagnostic: ViewerDiagnosticEvent) =
            let categoryAllowed =
                options.Verbose
                || Set.isEmpty options.Categories
                || Set.contains diagnostic.Category options.Categories

            levelRank diagnostic.Level <= levelRank options.MinimumLevel
            && categoryAllowed
            && frameAllowed options diagnostic

        let capture options (diagnostic: ViewerDiagnosticEvent) =
            if shouldCapture options diagnostic then
                options.Sink |> Option.iter (fun sink -> sink diagnostic)
                Some diagnostic
            else
                None

    let shouldCaptureDiagnostic options diagnostic =
        DiagnosticsFiltering.shouldCapture options diagnostic

    let captureDiagnostic options diagnostic =
        DiagnosticsFiltering.capture options diagnostic

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

    module WindowBehaviorValidation =
        let optionResult option requested observed status message =
            { Option = option
              Requested = requested
              Observed = observed
              Status = status
              Message = message }

        let validateBehavior (request: ViewerWindowBehaviorRequest) =
            let resize =
                match request.ResizePolicy with
                | Resizable -> optionResult "resize" "resizable" (Some "resizable") Honored "Resize policy can be honored by the viewer host."
                | FixedSize -> optionResult "resize" "fixed-size" (Some "fixed-size") Honored "Fixed-size window policy can be honored by the viewer host."

            let maximize =
                match request.MaximizePolicy with
                | Maximizable -> optionResult "maximize" "maximizable" (Some "maximizable") Honored "Maximize policy can be honored by the viewer host."
                | NotMaximizable -> optionResult "maximize" "not-maximizable" (Some "not-maximizable") Honored "Maximize-disabled policy can be honored by the viewer host."

            let startupState =
                match request.StartupState with
                | ViewerWindowStartupState.Normal -> optionResult "startup-state" "normal" (Some "normal") Honored "Normal startup state can be honored by the viewer host."
                | ViewerWindowStartupState.Maximized -> optionResult "startup-state" "maximized" (Some "maximized") Honored "Maximized startup state can be requested."
                | ViewerWindowStartupState.Minimized -> optionResult "startup-state" "minimized" None UnsupportedOption "Minimized startup is not accepted for visible interactive launch validation."
                | ViewerWindowStartupState.Fullscreen -> optionResult "startup-state" "fullscreen" (Some "fullscreen") Honored "Fullscreen startup can be honored by the viewer host."
                | ViewerWindowStartupState.WindowedFullscreen -> optionResult "startup-state" "windowed-fullscreen" (Some "windowed-fullscreen") Honored "Windowed-fullscreen startup (borderless work-area coverage) can be honored by the viewer host."

            let startupPosition =
                match request.StartupPosition with
                | None -> optionResult "startup-position" "" None UnsupportedOption "No startup position was requested."
                | Some Centered -> optionResult "startup-position" "centered" (Some "centered") Honored "Centered startup can be requested."
                | Some(Coordinates(x, y)) when x < 0 || y < 0 ->
                    optionResult "startup-position" $"{x},{y}" None FailedOption "Startup coordinates must be non-negative."
                | Some(Coordinates(x, y)) ->
                    optionResult "startup-position" $"{x},{y}" (Some $"{x},{y}") Honored "Startup coordinates can be requested."

            let backend =
                match request.BackendPreference with
                | None -> optionResult "backend" "" (Some "default") Degraded "No backend requested; default backend will be selected."
                | Some ViewerBackendPreference.DefaultBackend -> optionResult "backend" "default" (Some "default") Honored "Default backend will be selected."
                | Some ViewerBackendPreference.OpenGL -> optionResult "backend" "opengl" (Some "opengl") Honored "OpenGL backend can be requested."
                | Some ViewerBackendPreference.Vulkan -> optionResult "backend" "vulkan" None UnsupportedOption "Vulkan backend is no longer supported; this viewer host presents through OpenGL (feature 119)."
                | Some ViewerBackendPreference.Software -> optionResult "backend" "software" None UnsupportedOption "Software backend preference is not supported by this viewer host."

            [ resize; maximize; startupState; startupPosition; backend ]

        let validateLaunch (initialSize: Size) request =
            let initialSizeResult =
                if initialSize.Width <= 0 || initialSize.Height <= 0 then
                    optionResult
                        "initial-size"
                        $"{initialSize.Width}x{initialSize.Height}"
                        None
                        FailedOption
                        "Initial window size must be positive before native window creation."
                else
                    optionResult
                        "initial-size"
                        $"{initialSize.Width}x{initialSize.Height}"
                        (Some $"{initialSize.Width}x{initialSize.Height}")
                        Honored
                        "Initial window size is positive and can be requested."

            initialSizeResult :: validateBehavior request

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

    let private makeFailure stage classification category message (lastDiagnostic: ViewerDiagnosticEvent option) =
        { BlockedStage = stage
          Classification = classification
          DiagnosticCategory = category
          Message = message
          LastDiagnosticSummary = lastDiagnostic |> Option.map _.Message }

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

    let private validateRequest (request: ViewerRunRequest) =
        if request.Timeout <= TimeSpan.Zero then
            Result.Error(makeFailure App ProductDefect Startup "Viewer run timeout must be positive." None)
        else
            match request.Target with
            | FrameCount count when count <= 0 ->
                Result.Error(makeFailure App ProductDefect Startup "Viewer run frame count must be positive." None)
            | Duration duration when duration <= TimeSpan.Zero ->
                Result.Error(makeFailure App ProductDefect Startup "Viewer run duration must be positive." None)
            | _ -> Result.Ok()

    let private validateOptions options =
        if String.IsNullOrWhiteSpace options.Title then
            Result.Error(makeFailure App ProductDefect Startup "Viewer title must not be empty." None)
        elif options.InitialSize.Width <= 0 || options.InitialSize.Height <= 0 then
            Result.Error(makeFailure Window ProductDefect Startup "Viewer initial output size must be positive." None)
        elif (match options.FrameRateCap with
              | Some cap -> cap <= 0
              | None -> false) then
            Result.Error(makeFailure Window ProductDefect Startup "Viewer frame-rate cap must be positive." None)
        else
            Result.Ok()

    let private nativeWindowEnvironmentLock = obj()

    let private withNativeWindowEnvironment action =
        if OperatingSystem.IsLinux()
           && not (String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable "DISPLAY"))
           && not (String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable "WAYLAND_DISPLAY")) then
            lock nativeWindowEnvironmentLock (fun () ->
                let previousWayland = Environment.GetEnvironmentVariable "WAYLAND_DISPLAY"

                try
                    Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", null)
                    action()
                finally
                    Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", previousWayland))
        else
            action()

    let private unsupportedHostFailure () =
        let isSupportedOs = OperatingSystem.IsWindows() || OperatingSystem.IsLinux()

        if not isSupportedOs then
            Some(makeFailure Window UnsupportedEnvironment EnvironmentSession $"Viewer smoke is unsupported on {Environment.OSVersion.Platform}." None)
        elif OperatingSystem.IsLinux()
             && String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable "DISPLAY")
             && String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable "WAYLAND_DISPLAY") then
            Some(makeFailure Window UnsupportedEnvironment EnvironmentSession "Viewer smoke requires DISPLAY or WAYLAND_DISPLAY on Linux." None)
        else
            None

    module HostCapability =
        let envOption name : string option =
            match Environment.GetEnvironmentVariable name with
            | null -> None
            | value when String.IsNullOrWhiteSpace value -> None
            | value -> Some value

        let desktopSessionDiagnostic () =
            let runtimeDirectory = envOption "XDG_RUNTIME_DIR"

            let runtimeDirectoryExists =
                runtimeDirectory |> Option.exists IO.Directory.Exists

            let displayVariable =
                let wayland = envOption "WAYLAND_DISPLAY"
                let x11 = envOption "DISPLAY"

                match wayland, x11 with
                | Some value, _ -> Some $"WAYLAND_DISPLAY={value}"
                | None, Some value -> Some $"DISPLAY={value}"
                | None, None -> None

            let displaySocket =
                let wayland = envOption "WAYLAND_DISPLAY"
                let x11 = envOption "DISPLAY"

                match runtimeDirectory, wayland, x11 with
                | Some runtimeDir, Some wayland, _ ->
                    Some(IO.Path.Combine(runtimeDir, wayland))
                | _, _, Some display ->
                    let number = display.TrimStart(':').Split('.').[0]
                    Some($"/tmp/.X11-unix/X{number}")
                | _ -> None

            let displaySocketExists =
                displaySocket |> Option.exists IO.File.Exists

            let sessionBus = envOption "DBUS_SESSION_BUS_ADDRESS"

            let fallback = IO.Path.Combine(IO.Path.GetTempPath(), "fs-gg-ui-runtime")

            let blockedReason =
                if not (OperatingSystem.IsLinux()) then
                    None
                elif runtimeDirectory.IsNone then
                    Some "XDG_RUNTIME_DIR is missing; interactive Linux launch is blocked before app lifecycle debugging."
                elif not runtimeDirectoryExists then
                    Some "XDG_RUNTIME_DIR does not exist; interactive Linux launch is blocked before app lifecycle debugging."
                elif displayVariable.IsNone then
                    Some "DISPLAY or WAYLAND_DISPLAY is missing; interactive Linux launch is blocked before app lifecycle debugging."
                elif displaySocket.IsSome && not displaySocketExists then
                    Some "Display socket is missing; interactive Linux launch is blocked before app lifecycle debugging."
                else
                    None

            let diagnosticClass, message =
                if not (OperatingSystem.IsLinux()) then
                    "environment-session-not-required", "Desktop session diagnostic is not required on this host."
                else
                    match blockedReason with
                    | Some reason -> "unsupported-host", reason
                    | None -> "environment-session-ready", "Desktop session prerequisites are present."

            { RuntimeDirectory = runtimeDirectory
              RuntimeDirectoryExists = runtimeDirectoryExists
              RuntimeDirectoryOwnerSuitable = runtimeDirectoryExists
              RuntimeDirectoryPermissionsSuitable = runtimeDirectoryExists
              DisplayVariable = displayVariable
              DisplaySocket = displaySocket
              DisplaySocketExists = displaySocketExists
              SessionBus = sessionBus
              FallbackRuntimeDirectory = Some fallback
              FallbackIsFullDesktopSession = false
              DiagnosticClass = diagnosticClass
              Message = message }

        let unsupportedHostReasons () =
            let reasons = ResizeArray<string>()

            if not (OperatingSystem.IsWindows() || OperatingSystem.IsLinux()) then
                reasons.Add($"persistent windows are unsupported on {Environment.OSVersion.Platform}")

            if OperatingSystem.IsLinux() then
                let diagnostic = desktopSessionDiagnostic()

                if diagnostic.DiagnosticClass = "unsupported-host" then
                    reasons.Add(diagnostic.Message)

            List.ofSeq reasons

        let runtimeCapability () =
            let unsupportedReasons = unsupportedHostReasons ()

            { PersistentWindow = List.isEmpty unsupportedReasons
              BoundedSmoke = true
              KeyboardInput = true
              RendererMode = "skia"
              UnsupportedHostReasons = unsupportedReasons
              MissingPackageCapabilities = [] }

    let desktopSessionDiagnostic () =
        HostCapability.desktopSessionDiagnostic ()

    let private unsupportedHostReasons () =
        HostCapability.unsupportedHostReasons ()

    let runtimeCapability () =
        HostCapability.runtimeCapability ()

    let private persistentUnsupportedFailure capability =
        let message =
            match capability.UnsupportedHostReasons with
            | [] -> "Persistent viewer window is unavailable in this host."
            | reasons -> String.Join("; ", reasons)

        makeFailure Window UnsupportedEnvironment EnvironmentSession message None

    let private observedValueText value =
        match value with
        | ViewerObservedValue.Observed true -> "observed:true"
        | ViewerObservedValue.Observed false -> "observed:false"
        | ViewerObservedValue.Unsupported -> "unsupported"
        | ViewerObservedValue.Unavailable -> "unavailable"

    let private launchOk inputDispatch windowOpened firstFramePresented closeReason windowDiagnostics optionResults message =
        let userCloseObserved = closeReason = Some UserClose
        let appCloseObserved = closeReason = Some AppRequestedClose
        let evidenceCloseObserved = closeReason = Some EvidenceRequestedClose

        { Status = "ok"
          Mode = "interactive-window"
          Command = None
          RendererMode = "skia"
          WindowOpened = windowOpened
          WindowVisible =
            if windowOpened && firstFramePresented then
                ViewerObservedValue.Observed true
            else
                ViewerObservedValue.Observed false
          FirstFramePresented = firstFramePresented
          CloseReason = closeReason
          UserCloseObserved = userCloseObserved
          AppCloseObserved = appCloseObserved
          EvidenceCloseObserved = evidenceCloseObserved
          SelfClosedForEvidence = false
          InputDispatch = inputDispatch
          ExitPath = closeReason.IsSome
          WindowDiagnostics = windowDiagnostics
          OptionResults = optionResults
          VisualEvidence = []
          FailureClass = None
          BlockedStage = None
          Classification = None
          Category = None
          Message = message }

    let private toNativeSize (size: Size) =
        Vector2D<int>(size.Width, size.Height)

    /// Resolve the default monitor's work-area origin/size for windowed-fullscreen
    /// coverage. Returns None on a headless / no-display host so callers degrade to
    /// honest render-only behavior rather than fabricating a geometry.
    let private tryResolveWorkArea () : (Vector2D<int> * Vector2D<int>) option =
        try
            let monitor = Silk.NET.Windowing.Monitor.GetMainMonitor null

            if isNull (box monitor) then
                None
            else
                let bounds = monitor.Bounds

                if bounds.Size.X > 0 && bounds.Size.Y > 0 then
                    Some(bounds.Origin, bounds.Size)
                else
                    None
        with _ ->
            None

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

    let private tryObserved read =
        try
            Observed(read ())
        with _ ->
            Unavailable

    let private restoreVisibleWindow forceTopMost (behavior: ViewerWindowBehaviorRequest) (options: ViewerOptions) (window: IWindow) =
        // Re-apply the *requested* startup state when restoring visibility, so the
        // visibility repair never silently downgrades Fullscreen / Maximized /
        // WindowedFullscreen to a plain Normal window.
        let setState state =
            try
                window.WindowState <- state
            with _ ->
                ()

        match behavior.StartupState, tryResolveWorkArea () with
        | ViewerWindowStartupState.WindowedFullscreen, Some(origin, size) ->
            // Borderless work-area coverage; degrade to the initial size if bounds
            // cannot resolve (handled by the None arm below).
            (try
                window.WindowBorder <- WindowBorder.Hidden
             with _ ->
                 ())

            (try
                window.Position <- origin
             with _ ->
                 ())

            (try
                window.Size <- size
             with _ ->
                 ())

            setState WindowState.Normal
        | ViewerWindowStartupState.WindowedFullscreen, None ->
            (try
                window.WindowBorder <- WindowBorder.Hidden
             with _ ->
                 ())

            setState WindowState.Normal
        | ViewerWindowStartupState.Fullscreen, _ -> setState WindowState.Fullscreen
        | ViewerWindowStartupState.Maximized, _ -> setState WindowState.Maximized
        | (ViewerWindowStartupState.Normal | ViewerWindowStartupState.Minimized), _ ->
            (try
                window.Size <- toNativeSize options.InitialSize
             with _ ->
                 ())

            setState WindowState.Normal

        try
            window.IsVisible <- true
        with _ ->
            ()

        try
            window.TopMost <- forceTopMost
        with _ ->
            ()

        try
            window.Focus()
        with _ ->
            ()

        try
            window.DoEvents()
        with _ ->
            ()

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
          Backend =
            match windowState with
            | Some state -> Some $"skia;window-state={state}"
            | None -> Some "skia"
          InputDevicesAvailable = inputAvailable
          FailureClass = failureClass
          Message = message }

    let private nodeToScene node : Scene =
        { Nodes = [ node ] }

    let private toViewerFailure (diagnostic: Host.RenderDiagnostic) =
        let stage =
            match diagnostic.Stage with
            | Host.DiagnosticStage.PlatformCheck -> ViewerRunBlockedStage.Window
            | Host.DiagnosticStage.GlSurface -> ViewerRunBlockedStage.Surface
            | Host.DiagnosticStage.GlContext
            | Host.DiagnosticStage.GlRenderer -> ViewerRunBlockedStage.Renderer
            | Host.DiagnosticStage.Framebuffer -> ViewerRunBlockedStage.GlContext
            | Host.DiagnosticStage.SkiaContext
            | Host.DiagnosticStage.FrameRender -> ViewerRunBlockedStage.Renderer
            | Host.DiagnosticStage.ScreenshotCapture -> ViewerRunBlockedStage.Readback
            | Host.DiagnosticStage.Shutdown -> ViewerRunBlockedStage.App

        let category =
            match diagnostic.Stage with
            | Host.DiagnosticStage.GlContext
            | Host.DiagnosticStage.GlRenderer
            | Host.DiagnosticStage.GlSurface
            | Host.DiagnosticStage.Framebuffer -> ViewerDiagnosticCategory.OpenGl
            | Host.DiagnosticStage.SkiaContext -> ViewerDiagnosticCategory.Skia
            | Host.DiagnosticStage.FrameRender -> ViewerDiagnosticCategory.Frame
            | Host.DiagnosticStage.ScreenshotCapture -> ViewerDiagnosticCategory.Screenshot
            | Host.DiagnosticStage.PlatformCheck
            | Host.DiagnosticStage.Shutdown -> ViewerDiagnosticCategory.Startup

        makeFailure stage UnsupportedEnvironment category diagnostic.Message None

    let private toViewerPointerButtonKind (button: Host.ViewerPointerButton) =
        match button with
        | Host.ViewerPointerButton.PrimaryButton -> ViewerPointerButtonKind.Primary
        | Host.ViewerPointerButton.SecondaryButton -> ViewerPointerButtonKind.Secondary
        | Host.ViewerPointerButton.MiddleButton -> ViewerPointerButtonKind.Middle

    let private runPresentedPersistentWindow options behavior diagnostics inputDispatch getScene onTick onKey onPointer onResize inputVerified scriptInputs =
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

        match withNativeWindowEnvironment (fun () -> Host.Viewer.run program) with
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
                  Backend = Some "opengl-presenter"
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

    let private runPersistentWindow options behavior diagnostics inputDispatch renderScene onTick onKey inputVerified =
        let windowOpened = ref false
        let framePresented = ref false
        // FR-015/016 (data-model §9): a bounded pre-ready FIFO. Native key events that arrive
        // before the render pipeline signals ready (first presented frame) are buffered in order
        // and flushed once ready, so no early keystroke is dropped at window warm-up. Past the cap
        // we drop-oldest with a structured diagnostic (Principle VII — explicit degradation, never
        // silent loss). Silk windowing is single-threaded (load/update/render/input callbacks share
        // one thread), so a plain mutable queue on the host edge needs no locking.
        let warmupCapacity = 64
        let warmupQueue = System.Collections.Generic.Queue<string * bool>()
        let mutable flushWarmup: unit -> unit = fun () -> ()
        let closeReason: ViewerCloseReason option ref = ref None
        let lastDiagnostic = ref None
        let windowDiagnostics = ResizeArray<ViewerWindowStateDiagnostic>()

        let capture diagnostic =
            lastDiagnostic := Some(dispatchDiagnostic diagnostics diagnostic)

        let removeHandlers (window: IWindow) handlers =
            handlers
            |> List.iter (fun remove ->
                try
                    remove window
                with _ ->
                    ())

        withNativeWindowEnvironment (fun () ->
            try
                let mutable windowOptions = WindowOptions.Default
                windowOptions.Title <- options.Title
                windowOptions.Size <- toNativeSize options.InitialSize
                windowOptions.IsVisible <- true
                windowOptions.API <- GraphicsAPI.Default
                windowOptions.FramesPerSecond <- 60.0
                windowOptions.UpdatesPerSecond <- 60.0
                windowOptions <- applyWindowBehaviorToOptions behavior windowOptions

                let window = Window.Create windowOptions

                let loadedHandler =
                    Action(fun () ->
                        windowOpened := true
                        restoreVisibleWindow true behavior options window

                        capture
                            { Level = ViewerDiagnosticLevel.Info
                              Category = ViewerDiagnosticCategory.Startup
                              Message = $"persistent viewer window opened for '{options.Title}'"
                              FrameIndex = None
                              Stage = Some Window
                              Elapsed = Some TimeSpan.Zero })

                let renderHandler =
                    Action<float>(fun elapsedSeconds ->
                        if not !framePresented then
                            framePresented := true
                            renderScene ()

                            capture
                                { Level = ViewerDiagnosticLevel.Info
                                  Category = ViewerDiagnosticCategory.Frame
                                  Message = "persistent viewer frame presented"
                                  FrameIndex = Some 1
                                  Stage = None
                                  Elapsed = Some(TimeSpan.FromSeconds elapsedSeconds) })

                let updateHandler =
                    Action<float>(fun elapsedSeconds ->
                        // Once the pipeline is ready, drain any keystrokes buffered during warm-up
                        // (in order) before advancing the tick, so no early input is lost.
                        if !framePresented then
                            flushWarmup ()

                        if onTick(TimeSpan.FromSeconds elapsedSeconds) && not window.IsClosing then
                            closeReason := Some AppRequestedClose
                            window.Close())

                let closingHandler =
                    Action(fun () ->
                        if closeReason.Value.IsNone then
                            closeReason := Some UserClose

                        capture
                            { Level = ViewerDiagnosticLevel.Info
                              Category = ViewerDiagnosticCategory.Startup
                              Message = "persistent viewer close requested"
                              FrameIndex = None
                              Stage = Some Window
                              Elapsed = None })

                window.add_Load loadedHandler
                window.add_Update updateHandler
                window.add_Render renderHandler
                window.add_Closing closingHandler

                let handlers =
                    [ fun (w: IWindow) -> w.remove_Load loadedHandler
                      fun (w: IWindow) -> w.remove_Update updateHandler
                      fun (w: IWindow) -> w.remove_Render renderHandler
                      fun (w: IWindow) -> w.remove_Closing closingHandler ]

                try
                    window.Initialize()

                    if not window.IsInitialized then
                        Result.Error(
                            makeFailure
                                Window
                                UnsupportedEnvironment
                                Startup
                                "Silk.NET persistent viewer window did not initialize."
                                !lastDiagnostic
                        )
                    else
                        restoreVisibleWindow true behavior options window
                        windowOpened := true
                        let inputDisposables = ResizeArray<IDisposable>()
                        let mutable inputAvailable = ViewerObservedValue.Unavailable

                        match onKey with
                        | Some dispatchKey ->
                            try
                                let deliverKey (raw: string) (isDown: bool) =
                                    if dispatchKey raw isDown && not window.IsClosing then
                                        closeReason := Some AppRequestedClose
                                        window.Close()

                                // Drain the warm-up FIFO in capture order.
                                flushWarmup <-
                                    fun () ->
                                        while warmupQueue.Count > 0 do
                                            let raw, isDown = warmupQueue.Dequeue()
                                            deliverKey raw isDown

                                // Before the first frame: buffer (bounded, drop-oldest with a
                                // diagnostic). After: drain any residual buffer in order, then
                                // dispatch directly.
                                let onKeyEvent (raw: string) (isDown: bool) =
                                    if !framePresented then
                                        flushWarmup ()
                                        deliverKey raw isDown
                                    else
                                        if warmupQueue.Count >= warmupCapacity then
                                            warmupQueue.Dequeue() |> ignore

                                            capture
                                                { Level = ViewerDiagnosticLevel.Warning
                                                  Category = ViewerDiagnosticCategory.Input
                                                  Message = $"key warm-up buffer overflow (cap={warmupCapacity}); dropped oldest pre-ready key event"
                                                  FrameIndex = None
                                                  Stage = Some App
                                                  Elapsed = None }

                                        warmupQueue.Enqueue((raw, isDown))

                                let input = window.CreateInput()
                                inputDisposables.Add(input)
                                inputAvailable <- ViewerObservedValue.Observed(input.Keyboards.Count > 0)

                                for keyboard in input.Keyboards do
                                    let keyDownHandler =
                                        Action<IKeyboard, Key, int>(fun _ key _ -> onKeyEvent (key.ToString()) true)

                                    keyboard.add_KeyDown keyDownHandler
                                    inputDisposables.Add
                                        { new IDisposable with
                                            member _.Dispose() = keyboard.remove_KeyDown keyDownHandler }

                                    let keyUpHandler =
                                        Action<IKeyboard, Key, int>(fun _ key _ -> onKeyEvent (key.ToString()) false)

                                    keyboard.add_KeyUp keyUpHandler
                                    inputDisposables.Add
                                        { new IDisposable with
                                            member _.Dispose() = keyboard.remove_KeyUp keyUpHandler }
                            with ex ->
                                capture
                                    { Level = ViewerDiagnosticLevel.Warning
                                      Category = ViewerDiagnosticCategory.Input
                                      Message = $"persistent viewer input mapping unavailable: {ex.Message}"
                                      FrameIndex = None
                                      Stage = Some App
                                      Elapsed = None }
                        | None -> ()

                        let initializedDiagnostic =
                            windowStateDiagnostic
                                "persistent viewer initialized; native visibility facts captured where supported"
                                None
                                window
                                (ViewerObservedValue.Observed true)
                                inputAvailable

                        windowDiagnostics.Add initializedDiagnostic

                        try
                            let mutable visibilityRepairFrames = 120

                            while not window.IsClosing do
                                if visibilityRepairFrames > 0 then
                                    restoreVisibleWindow (visibilityRepairFrames > 60) behavior options window
                                    visibilityRepairFrames <- visibilityRepairFrames - 1

                                window.DoEvents()
                                window.DoUpdate()
                                window.DoRender()
                                Thread.Sleep(1)

                            Result.Ok(
                                launchOk
                                    inputDispatch
                                    !windowOpened
                                    !framePresented
                                    !closeReason
                                    (List.ofSeq windowDiagnostics)
                                    (validateWindowLaunchBehavior options.InitialSize behavior)
                                    "Persistent viewer launch completed after user or host close."
                            )
                        finally
                            for disposable in Seq.rev inputDisposables do
                                disposable.Dispose()
                finally
                    removeHandlers window handlers
                    window.Dispose()
            with ex ->
                Result.Error(
                    makeFailure
                        Window
                        UnsupportedEnvironment
                        Startup
                        $"Silk.NET persistent viewer launch failed: {ex.Message}"
                        !lastDiagnostic
                ))

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
          RendererMode = model.Request.RendererMode
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

    let private writeEvidence (path: string) (evidence: ViewerRunEvidence) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        let summary = evidence.LastDiagnosticSummary |> Option.defaultValue ""

        let lines =
            [ $"framesRendered={evidence.FramesRendered}"
              $"elapsedMs={evidence.Elapsed.TotalMilliseconds}"
              $"initialOutputSize={evidence.InitialOutputSize.Width}x{evidence.InitialOutputSize.Height}"
              $"rendererMode={evidence.RendererMode}"
              $"lastDiagnosticSummary={summary}" ]

        IO.File.WriteAllLines(path, lines)

    let private writeLaunchOutcome (path: string) (outcome: ViewerLaunchOutcome) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        let command = outcome.Command |> Option.defaultValue ""
        let blockedStage = outcome.BlockedStage |> Option.map string |> Option.defaultValue ""
        let classification = outcome.Classification |> Option.map string |> Option.defaultValue ""
        let category = outcome.Category |> Option.map string |> Option.defaultValue ""
        let closeReason = outcome.CloseReason |> Option.map string |> Option.defaultValue ""
        let failureClass = outcome.FailureClass |> Option.map string |> Option.defaultValue ""

        let lines =
            [ $"status={outcome.Status}"
              $"mode={outcome.Mode}"
              $"command={command}"
              $"renderer-mode={outcome.RendererMode}"
              $"window-opened={outcome.WindowOpened}"
              $"window-visible={observedValueText outcome.WindowVisible}"
              $"first-frame-presented={outcome.FirstFramePresented}"
              $"close-reason={closeReason}"
              $"user-close-observed={outcome.UserCloseObserved}"
              $"app-close-observed={outcome.AppCloseObserved}"
              $"evidence-close-observed={outcome.EvidenceCloseObserved}"
              $"self-closed-for-evidence={outcome.SelfClosedForEvidence}"
              $"input-dispatch={outcome.InputDispatch}"
              $"exit-path={outcome.ExitPath}"
              $"window-diagnostic-count={outcome.WindowDiagnostics.Length}"
              $"option-result-count={outcome.OptionResults.Length}"
              $"visual-evidence-count={outcome.VisualEvidence.Length}"
              $"failure-class={failureClass}"
              $"blocked-stage={blockedStage}"
              $"classification={classification}"
              $"category={category}"
              $"message={outcome.Message}" ]

        IO.File.WriteAllLines(path, lines)

    let private writeLaunchFailure (path: string) mode command (failure: ViewerRunFailure) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        let summary = failure.LastDiagnosticSummary |> Option.defaultValue ""

        let lines =
            [ "status=failed"
              $"mode={mode}"
              $"command={command}"
              $"blocked-stage={failure.BlockedStage}"
              $"classification={failure.Classification}"
              $"category={failure.DiagnosticCategory}"
              $"message={failure.Message}"
              $"last-diagnostic-summary={summary}" ]

        IO.File.WriteAllLines(path, lines)

    let private ensureParentDirectory (path: string) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

    let private isPngPath (path: string) =
        String.Equals(IO.Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)

    let private imageDecodable (path: string) =
        try
            use image = SKImage.FromEncodedData(path)
            not (isNull image)
        with _ ->
            false

    let private drawScreenshotScene (canvas: SKCanvas) scene =
        // Feature 063 (FR-001/002): the image-evidence path delegates to the single
        // shared exhaustive `SceneRenderer.paintNode` — the SAME painter the interactive
        // host uses. Every primitive (Line/Path/Arc/real-glyph Text/…) renders to real
        // pixels; the prior placeholder-rect wildcard that masqueraded as "scene visible"
        // is deleted.
        // Feature 136 (FR-001/T017): clear the text-fallback disclosure accumulator so the report read
        // back after this capture (via `Text.fallbackReport`) reflects exactly this page's render.
        SceneRenderer.resetFallbackEvents ()
        SceneRenderer.paintNode canvas scene

    let private pngDimensionsAndNonBlank (path: string) : (int * int) option * ScreenshotPixelContentValidation =
        try
            use bitmap = SKBitmap.Decode(path)

            if Object.ReferenceEquals(bitmap, null) then
                None, PixelContentUnreadable "SkiaSharp could not decode screenshot PNG."
            else
                let mutable nonBlank = false
                let mutable y = 0

                while y < bitmap.Height && not nonBlank do
                    let mutable x = 0

                    while x < bitmap.Width && not nonBlank do
                        let pixel = bitmap.GetPixel(x, y)
                        if pixel.Alpha > 0uy then
                            nonBlank <- true
                        x <- x + 1

                    y <- y + 1

                let validation = if nonBlank then PixelContentNonBlank else PixelContentBlank
                Some(bitmap.Width, bitmap.Height), validation
        with ex ->
            None, PixelContentUnreadable ex.Message

    let private writeSceneImageEvidence path (size: FS.GG.UI.Scene.Size) scene =
        ensureParentDirectory path
        let width = max 1 size.Width
        let height = max 1 size.Height

        use bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)
        use canvas = new SKCanvas(bitmap)
        canvas.Clear(SKColors.Transparent)
        drawScreenshotScene canvas scene

        use image = SKImage.FromBitmap(bitmap)
        use data = image.Encode(SKEncodedImageFormat.Png, 90)

        if isNull data then
            false
        else
            IO.File.WriteAllBytes(path, data.ToArray())

            imageDecodable path

    let private writeTextEvidence path lines =
        ensureParentDirectory path
        IO.File.WriteAllLines(path, lines |> List.toArray)

    let private sceneFromNode node =
        { Nodes = [ node ] }

    // Feature 105 (US3, FR-009): the closed set of renderer-mode dispatch values, parsed ONCE at
    // the edge so the case-insensitive comparison is an exhaustive DU match instead of a chain of
    // string equalities. Every public `RendererMode` output/serialized field stays an unchanged
    // string; this DU types only the internal dispatch. An unrecognized mode parses to `Default`
    // (the prior string-comparison fallthrough), preserving behaviour exactly. Hidden from
    // consumers by absence from SkiaViewer.fsi.
    [<RequireQualifiedAccess>]
    type private RendererModeKind =
        | Default
        | Skia
        | DeterministicScene
        | UnsupportedHost
        | MetadataHash
        | PixelReadback

    let private parseRendererMode (mode: string) : RendererModeKind =
        if String.Equals(mode, "unsupported-host", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.UnsupportedHost
        elif String.Equals(mode, "metadata-hash", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.MetadataHash
        elif String.Equals(mode, "pixel-readback", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.PixelReadback
        elif String.Equals(mode, "skia", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.Skia
        elif String.Equals(mode, "deterministic-scene", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.DeterministicScene
        else
            RendererModeKind.Default

    let private visualEvidenceArtifacts (request: ViewerRunRequest) (options: ViewerOptions) (scene: SceneNode) =
        match request.EvidencePath with
        | None -> []
        | Some path ->
            match parseRendererMode request.RendererMode with
            | RendererModeKind.UnsupportedHost ->
                [ { Kind = UnsupportedHost
                    Path = None
                    ImageDecodable = None
                    ProvesSceneRendering = false
                    ProvesDesktopVisibility = false
                    Message = "Visual evidence is unsupported on this host." } ]
            | RendererModeKind.MetadataHash ->
                match SceneEvidence.renderHash options.InitialSize (sceneFromNode scene) with
                | Result.Ok evidence ->
                    writeTextEvidence
                        path
                        [ "evidence-kind=metadata-hash"
                          $"path={path}"
                          $"hash={evidence.Value}"
                          "proves-scene-rendering=false"
                          "proves-desktop-visibility=false" ]

                    [ { Kind = MetadataHash
                        Path = Some path
                        ImageDecodable = None
                        ProvesSceneRendering = false
                        ProvesDesktopVisibility = false
                        Message = "Metadata/hash evidence is labeled separately from image evidence." } ]
                | Result.Error failure ->
                    [ { Kind = UnsupportedHost
                        Path = None
                        ImageDecodable = None
                        ProvesSceneRendering = false
                        ProvesDesktopVisibility = false
                        Message = failure.Message } ]
            | RendererModeKind.PixelReadback ->
                match SceneEvidence.renderHash options.InitialSize (sceneFromNode scene) with
                | Result.Ok evidence ->
                    writeTextEvidence
                        path
                        [ "evidence-kind=pixel-readback"
                          $"path={path}"
                          "fallback-reason=screenshot-unavailable"
                          $"hash={evidence.Value}"
                          "proves-scene-rendering=true"
                          "proves-desktop-visibility=false" ]

                    [ { Kind = PixelReadback
                        Path = Some path
                        ImageDecodable = None
                        ProvesSceneRendering = true
                        ProvesDesktopVisibility = false
                        Message = "Pixel-readback fallback proves scene rendering but not desktop visibility." } ]
                | Result.Error failure ->
                    [ { Kind = UnsupportedHost
                        Path = None
                        ImageDecodable = None
                        ProvesSceneRendering = false
                        ProvesDesktopVisibility = false
                        Message = failure.Message } ]
            | RendererModeKind.Default
            | RendererModeKind.Skia
            | RendererModeKind.DeterministicScene ->
                if isPngPath path then
                    let decodable = writeSceneImageEvidence path options.InitialSize scene

                    [ { Kind = Image
                        Path = Some path
                        ImageDecodable = Some decodable
                        ProvesSceneRendering = decodable
                        ProvesDesktopVisibility = false
                        Message =
                            if decodable then
                                "Image evidence is a decodable scene-rendering artifact; desktop visibility remains a separate claim."
                            else
                                "Image evidence was requested but SkiaSharp could not write a decodable PNG artifact." } ]
                else
                    match SceneEvidence.renderHash options.InitialSize (sceneFromNode scene) with
                    | Result.Ok evidence ->
                        writeTextEvidence
                            path
                            [ "evidence-kind=metadata-hash"
                              $"path={path}"
                              $"hash={evidence.Value}"
                              "proves-scene-rendering=false"
                              "proves-desktop-visibility=false" ]

                        [ { Kind = MetadataHash
                            Path = Some path
                            ImageDecodable = None
                            ProvesSceneRendering = false
                            ProvesDesktopVisibility = false
                            Message = "Non-image evidence path is recorded as metadata/hash evidence." } ]
                    | Result.Error failure ->
                        [ { Kind = UnsupportedHost
                            Path = None
                            ImageDecodable = None
                            ProvesSceneRendering = false
                            ProvesDesktopVisibility = false
                            Message = failure.Message } ]

    module VisualEvidenceHandling =
        let artifacts request options scene =
            visualEvidenceArtifacts request options scene

    let runBounded (request: ViewerRunRequest) options (scene: SceneNode) =
        ignore scene
        match validateRequest request with
        | Result.Error failure -> Result.Error failure
        | Result.Ok() ->
            match validateOptions options with
            | Result.Error failure -> Result.Error failure
            | Result.Ok() ->
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

                    withNativeWindowEnvironment (fun () ->
                        try
                            let mutable windowOptions = WindowOptions.Default
                            windowOptions.Title <- options.Title
                            windowOptions.Size <- toNativeSize options.InitialSize
                            windowOptions.IsVisible <- true
                            windowOptions.API <- GraphicsAPI.Default
                            windowOptions.FramesPerSecond <- 60.0
                            windowOptions.UpdatesPerSecond <- 60.0

                            let window = Window.Create windowOptions

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
                                window.Initialize()

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
                                        request.EvidencePath |> Option.iter (fun path -> writeEvidence path evidence)
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

                runPresentedPersistentWindow
                    options
                    defaultWindowBehavior
                    defaultDiagnostics
                    "not-applicable"
                    (fun () -> scene)
                    (fun _ -> false)
                    None
                    None
                    None
                    (fun () -> true)
                    None

    let runAppWithWindowBehavior options behavior (host: GeneratedAppHost<'model, 'msg>) =
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
                    let mutable currentScene = host.View currentModel
                    let mutable inputDispatch = "false"

                    let interpretEffects effects =
                        effects
                        |> List.fold
                            (fun closeRequested effect ->
                                match effect with
                                | RenderScene scene ->
                                    currentScene <- scene
                                    closeRequested
                                | DispatchInput _ ->
                                    inputDispatch <- "true"
                                    closeRequested
                                | CloseWindow -> true
                                | EmitDiagnostic diagnostic ->
                                    captureDiagnostic host.Diagnostics diagnostic |> ignore
                                    closeRequested
                                | OpenWindow _
                                | ApplyWindowOptions _
                                | QueryNativeWindowState
                                | StartBoundedRun _
                                | CheckDesktopSession
                                | CaptureScreenshot _
                                | CaptureImageEvidence _
                                | ReadPixels
                                | WriteVisualEvidence _
                                | WriteRunEvidence _ -> closeRequested)
                            false

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

                    let dispatchHostMsg msg =
                        let next, effects = host.Update msg currentModel
                        currentModel <- next
                        currentScene <- host.View currentModel
                        interpretEffects effects

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
                            currentScene <- runtimeStateRepaint false currentScene (fun () -> host.View currentModel)
                            false

                    let inputVerified () =
                        not (requireInputDispatchVerification ()) || inputDispatch = "true"

                    match runPresentedPersistentWindow options behavior host.Diagnostics inputDispatch (fun () -> currentScene) handleTick (Some handleKey) None None inputVerified None with
                    | Result.Ok outcome ->
                        Result.Ok(
                            { outcome with
                                InputDispatch = inputDispatch
                                OptionResults = validateWindowLaunchBehavior options.InitialSize behavior
                                ExitPath = initialCloseRequested || outcome.ExitPath
                                Message = "Persistent generated app host launch completed after intentional close." }
                        )
                    | Result.Error failure -> Result.Error failure

    let runApp options host =
        runAppWithWindowBehavior options defaultWindowBehavior host

    // Feature 085 — pointer-aware, size-aware durable launch. Mirrors
    // `runAppWithWindowBehavior` but routes native pointer events and resizes to the host,
    // and renders a size-aware `View`. `runApp`/`GeneratedAppHost` are untouched (FR-006).
    let private runInteractiveViewerWithWindowBehaviorCore options behavior script (host: InteractiveViewerHost<'model,'msg>) =
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
                    let mutable currentSize = options.InitialSize
                    let mutable currentScene = host.View currentSize currentModel
                    let mutable inputDispatch = "false"

                    let interpretEffects effects =
                        effects
                        |> List.fold
                            (fun closeRequested effect ->
                                match effect with
                                | RenderScene scene ->
                                    currentScene <- scene
                                    closeRequested
                                | DispatchInput _ ->
                                    inputDispatch <- "true"
                                    closeRequested
                                | CloseWindow -> true
                                | EmitDiagnostic diagnostic ->
                                    captureDiagnostic host.Diagnostics diagnostic |> ignore
                                    closeRequested
                                | OpenWindow _
                                | ApplyWindowOptions _
                                | QueryNativeWindowState
                                | StartBoundedRun _
                                | CheckDesktopSession
                                | CaptureScreenshot _
                                | CaptureImageEvidence _
                                | ReadPixels
                                | WriteVisualEvidence _
                                | WriteRunEvidence _ -> closeRequested)
                            false

                    let initialCloseRequested = interpretEffects initEffects

                    let dispatchHostMsg msg =
                        let msgText = (sprintf "%A" msg).Replace(" ", "_").Replace(Environment.NewLine, "_")
                        let updateSw = Stopwatch.StartNew()
                        RenderLagTrace.emit "model-update-start" [ "msg", msgText ]
                        let next, effects = host.Update msg currentModel
                        updateSw.Stop()
                        RenderLagTrace.emit
                            "model-update-end"
                            [ "msg", msgText
                              "durationMs", updateSw.Elapsed.TotalMilliseconds.ToString("0.###", CultureInfo.InvariantCulture) ]
                        currentModel <- next
                        let viewSw = Stopwatch.StartNew()
                        RenderLagTrace.emit "view-start" [ "msg", msgText ]
                        currentScene <- host.View currentSize currentModel
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
                        currentScene <- runtimeStateRepaint (not (List.isEmpty msgs)) currentScene (fun () -> host.View currentSize currentModel)
                        closeRequested

                    let handlePointer (input: ViewerPointerInput) =
                        let pointerSw = Stopwatch.StartNew()
                        RenderLagTrace.emit
                            "pointer-route-start"
                            [ "phase", string input.Phase
                              "x", input.X.ToString("0.###", CultureInfo.InvariantCulture)
                              "y", input.Y.ToString("0.###", CultureInfo.InvariantCulture) ]
                        let msgs = host.MapPointer input currentSize currentModel
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
                        currentScene <- runtimeStateRepaint (not (List.isEmpty msgs)) currentScene (fun () -> host.View currentSize currentModel)

                        closeRequested

                    let handleResize (size: Size) =
                        currentSize <- size
                        currentScene <- host.View currentSize currentModel

                    let inputVerified () =
                        not (requireInputDispatchVerification ()) || inputDispatch = "true"

                    match
                        runPresentedPersistentWindow
                            options
                            behavior
                            host.Diagnostics
                            inputDispatch
                            (fun () -> currentScene)
                            handleTick
                            (Some handleKey)
                            (Some handlePointer)
                            (Some handleResize)
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
        runInteractiveViewerWithWindowBehaviorCore options behavior None host

    let runInteractiveViewer options host =
        runInteractiveViewerWithWindowBehavior options defaultWindowBehavior host

    let runInteractiveViewerScriptWithWindowBehavior options behavior script host =
        runInteractiveViewerWithWindowBehaviorCore options behavior (Some script) host

    let runInteractiveViewerScript options script host =
        runInteractiveViewerScriptWithWindowBehavior options defaultWindowBehavior script host

    let runAppEvidence (request: ViewerRunRequest) options (host: GeneratedAppHost<'model, 'msg>) =
        let model, _ = host.Init()
        let scene = host.View model

        match runBounded request options scene with
        | Result.Ok evidence ->
            let visualEvidence = VisualEvidenceHandling.artifacts request options scene

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
                  CaptureSource = LiveViewerWindow
                  DeterministicFallbackKind = None
                  ProvesScreenshot = true
                  BlockedStage = None
                  Classification = None
                  Category = None
                  Message = "Screenshot artifact captured from viewer render target."
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
                        "capture-source=live-viewer-window"
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
                  CaptureSource = LiveViewerWindow
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

module GeneratedAppHost =
    let dispatchKey (host: GeneratedAppHost<'model, 'msg>) raw model =
        let key, isDown = ViewerKeyboard.normalizeEvent raw

        match host.MapKey key isDown with
        | Some msg -> host.Update msg model
        | None -> model, [ DispatchInput(key, isDown) ]

    let smoke (host: GeneratedAppHost<'model, 'msg>) (request: ViewerRunRequest) =
        let model, _ = host.Init()
        let scene = host.View model
        let size =
            match request.Target with
            | FirstFrame
            | FrameCount _ -> { Width = 1; Height = 1 }
            | Duration _ -> { Width = 1; Height = 1 }

        Viewer.runBounded request { Title = "Generated App"; InitialSize = size; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } scene

/// Feature 136 (R2/FR-001/FR-002): the rendering-edge text seam. Hosts install the bundled-font
/// real-metrics measurer once before building/laying out control scenes so box sizing equals draw
/// width (no clip), and read back the per-page fallback/tofu disclosure after a render (T017).
module Text =

    /// Install the bundled-font real-metrics measurer into the `Scene` measurement seam so control
    /// box sizing uses true advances. Idempotent; call once at host startup before layout.
    let installMeasurer () = Fonts.installMeasurementSeam ()

    /// Install the HarfBuzz-backed shaping provider and matching shaped measurement seam.
    let installShapingProvider () = Fonts.installShapingProvider ()

    /// Clear the shaping provider and use explicit fallback text measurement/render evidence.
    let clearShapingProvider () = Fonts.clearShapingProvider ()

    /// Read the active shaping provider state and diagnostics.
    let shapingProviderStatus () = Fonts.shapingProviderStatus ()

    /// Shape a text value through the active provider/fallback path for diagnostic readback.
    let shapeText text font = Fonts.shapeText text font

    /// Clear the text-fallback disclosure accumulator (the screenshot path also clears it per capture).
    let resetFallbackDisclosure () = SceneRenderer.resetFallbackEvents ()

    /// Aggregate disclosure (substituted/tofu counts + affected code points) for the most recent render.
    let fallbackReport () : Fonts.FallbackReport =
        SceneRenderer.fallbackEvents |> List.ofSeq |> Fonts.report

    /// Structured diagnostic lines for every non-authored character in the most recent render (FR-001).
    let fallbackDiagnostics () : string list =
        SceneRenderer.fallbackEvents |> List.ofSeq |> Fonts.diagnostics
