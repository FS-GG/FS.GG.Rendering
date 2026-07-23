namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.UI.Scene
open Silk.NET.Maths
open Silk.NET.Windowing
// Re-open the package namespace last so Viewer DU-cases win unqualified resolution
// (mirrors SkiaViewer.fs).
open FS.GG.UI.SkiaViewer

module internal ViewerLaunchSupport =
    let makeFailure stage classification category message (lastDiagnostic: ViewerDiagnosticEvent option) =
        { BlockedStage = stage
          Classification = classification
          DiagnosticCategory = category
          Message = message
          LastDiagnosticSummary = lastDiagnostic |> Option.map _.Message }

    /// Issue #396: guard the FIRST product `View` — the startup frame produced before a persistent
    /// window opens, and the single frame of a one-shot run. Unlike `tryProductStep` (which drops a
    /// throwing *runtime* frame and keeps the last-good scene), there is no prior scene to fall back
    /// to here, so a throw cannot be dropped: it fails the run as an `App`-stage `ProductDefect` — the
    /// classification #365 established for every product-code fault — reporting one diagnostic and
    /// returning the typed failure rather than escaping as an uncaught exception.

    let validateRequest (request: ViewerRunRequest) =
        if request.Timeout <= TimeSpan.Zero then
            Result.Error(makeFailure App ProductDefect Startup "Viewer run timeout must be positive." None)
        else
            match request.Target with
            | FrameCount count when count <= 0 ->
                Result.Error(makeFailure App ProductDefect Startup "Viewer run frame count must be positive." None)
            | Duration duration when duration <= TimeSpan.Zero ->
                Result.Error(makeFailure App ProductDefect Startup "Viewer run duration must be positive." None)
            | _ -> Result.Ok()

    let validateOptions options =
        if String.IsNullOrWhiteSpace options.Title then
            Result.Error(makeFailure App ProductDefect Startup "Viewer title must not be empty." None)
        elif options.InitialSize.Width <= 0 || options.InitialSize.Height <= 0 then
            Result.Error(makeFailure Window ProductDefect Startup "Viewer initial output size must be positive." None)
        elif (match options.FrameRateCap with
              | Some cap -> cap <= 0
              | None -> false) then
            Result.Error(makeFailure Window ProductDefect Startup "Viewer frame-rate cap must be positive." None)
        elif (match options.LogicalSize with
              | Some logical -> logical.Width <= 0 || logical.Height <= 0
              | None -> false) then
            Result.Error(makeFailure Window ProductDefect Startup "Viewer logical size must be positive." None)
        else
            Result.Ok()

    /// Issue #246: fit a scene authored in `options.LogicalSize` onto a surface of `surfaceSize`.
    /// Every path that owns a surface routes through this, so `LogicalSize` cannot be honored by one
    /// launch entry point and silently dropped by the next. Inert when no logical size is set.

    let presentedForLogical (logicalSize: Size option) (surfaceSize: Size) (scene: SceneNode) =
        match logicalSize with
        | Some logical -> LogicalCanvas.present logical surfaceSize scene
        | None -> scene

    let presentedFor (options: ViewerOptions) (surfaceSize: Size) (scene: SceneNode) =
        presentedForLogical options.LogicalSize surfaceSize scene

    // #363: the XWayland backend pin now lives in the host as `GlHost.withWindowBackendOverride`,
    // scoped to window `Create`/`Initialize` only. It used to wrap the entire run loop here, which
    // nulled `WAYLAND_DISPLAY` process-wide and held its lock for the whole (potentially multi-hour)
    // session; the inline window paths below apply it narrowly around their own creation instead.

    let unsupportedHostFailure () =
        let isSupportedOs = OperatingSystem.IsWindows() || OperatingSystem.IsLinux()

        if not isSupportedOs then
            Some(makeFailure Window UnsupportedEnvironment EnvironmentSession $"Viewer smoke is unsupported on {Environment.OSVersion.Platform}." None)
        elif OperatingSystem.IsLinux()
             && String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable "DISPLAY")
             && String.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable "WAYLAND_DISPLAY") then
            Some(makeFailure Window UnsupportedEnvironment EnvironmentSession "Viewer smoke requires DISPLAY or WAYLAND_DISPLAY on Linux." None)
        else
            None

    let persistentUnsupportedFailure capability =
        let message =
            match capability.UnsupportedHostReasons with
            | [] -> "Persistent viewer window is unavailable in this host."
            | reasons -> String.Join("; ", reasons)

        makeFailure Window UnsupportedEnvironment EnvironmentSession message None

    let launchOk inputDispatch windowOpened firstFramePresented closeReason windowDiagnostics optionResults message =
        let userCloseObserved = closeReason = Some UserClose
        let appCloseObserved = closeReason = Some AppRequestedClose
        let evidenceCloseObserved = closeReason = Some EvidenceRequestedClose

        { Status = "ok"
          Mode = "interactive-window"
          Command = None
          // A successful interactive launch presented through the live OpenGL host; name that
          // backend from the single source of truth rather than a fixed guess (#135).
          RendererMode = Host.GlHost.backendLabel
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

    let toNativeSize (size: Size) =
        Vector2D<int>(size.Width, size.Height)

    /// Resolve the default monitor's work-area origin/size for windowed-fullscreen
    /// coverage. Returns None on a headless / no-display host so callers degrade to
    /// honest render-only behavior rather than fabricating a geometry.

    let tryResolveWorkArea () : (Vector2D<int> * Vector2D<int>) option =
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

    let tryObserved read =
        try
            Observed(read ())
        with _ ->
            Unavailable

    let nodeToScene node : Scene =
        { Nodes = [ node ] }

    let toViewerFailure (diagnostic: Host.RenderDiagnostic) =
        let stage =
            match diagnostic.Stage with
            | Host.DiagnosticStage.PlatformCheck -> ViewerRunBlockedStage.Window
            | Host.DiagnosticStage.Window -> ViewerRunBlockedStage.Window
            | Host.DiagnosticStage.GlSurface -> ViewerRunBlockedStage.Surface
            | Host.DiagnosticStage.GlContext
            | Host.DiagnosticStage.GlRenderer -> ViewerRunBlockedStage.Renderer
            | Host.DiagnosticStage.Framebuffer -> ViewerRunBlockedStage.GlContext
            | Host.DiagnosticStage.SkiaContext
            | Host.DiagnosticStage.FrameRender -> ViewerRunBlockedStage.Renderer
            | Host.DiagnosticStage.ScreenshotCapture -> ViewerRunBlockedStage.Readback
            | Host.DiagnosticStage.Input
            | Host.DiagnosticStage.App
            | Host.DiagnosticStage.Shutdown -> ViewerRunBlockedStage.App

        let category =
            match diagnostic.Stage with
            | Host.DiagnosticStage.Window -> ViewerDiagnosticCategory.Window
            | Host.DiagnosticStage.GlContext
            | Host.DiagnosticStage.GlRenderer
            | Host.DiagnosticStage.GlSurface
            | Host.DiagnosticStage.Framebuffer -> ViewerDiagnosticCategory.OpenGl
            | Host.DiagnosticStage.SkiaContext -> ViewerDiagnosticCategory.Skia
            | Host.DiagnosticStage.FrameRender -> ViewerDiagnosticCategory.Frame
            | Host.DiagnosticStage.ScreenshotCapture -> ViewerDiagnosticCategory.Screenshot
            | Host.DiagnosticStage.App -> ViewerDiagnosticCategory.Scene
            | Host.DiagnosticStage.Input
            | Host.DiagnosticStage.PlatformCheck
            | Host.DiagnosticStage.Shutdown -> ViewerDiagnosticCategory.Startup

        makeFailure stage UnsupportedEnvironment category diagnostic.Message None

    let toViewerPointerButtonKind (button: Host.ViewerPointerButton) =
        match button with
        | Host.ViewerPointerButton.PrimaryButton -> ViewerPointerButtonKind.Primary
        | Host.ViewerPointerButton.SecondaryButton -> ViewerPointerButtonKind.Secondary
        | Host.ViewerPointerButton.MiddleButton -> ViewerPointerButtonKind.Middle
