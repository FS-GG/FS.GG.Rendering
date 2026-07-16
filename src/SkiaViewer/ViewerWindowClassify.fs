namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.UI.Scene
open ViewerLaunchSupport
// package namespace last so Viewer DU-cases win unqualified resolution
open FS.GG.UI.SkiaViewer

module internal ViewerWindowClassify =
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

    let tryFirstProductView
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
