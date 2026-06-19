namespace FS.GG.UI.SkiaViewer.Host

open System
open Elmish
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

type DiagnosticOptions =
    { Verbose: bool }

[<NoEquality; NoComparison>]
type ViewerConfiguration =
    { Title: string
      InitialSize: Size
      ClearColor: Color option
      TargetFrameRate: int option
      Diagnostics: DiagnosticOptions
      // Optional transform applied to the native WindowOptions just before window
      // creation, so a caller can carry window-startup intent (fullscreen / maximized
      // / windowed-fullscreen / borderless) into the live presented window.
      ConfigureWindow: (Silk.NET.Windowing.WindowOptions -> Silk.NET.Windowing.WindowOptions) option
      // Live present mechanism (feature 118), threaded from ViewerOptions.PresentMode.
      PresentMode: ViewerPresentMode }

type DiagnosticSeverity =
    | Info
    | Warning
    | Error
    | Fatal

type DiagnosticStage =
    | PlatformCheck
    | GlContext
    | GlRenderer
    | GlSurface
    | Framebuffer
    | SkiaContext
    | FrameRender
    | ScreenshotCapture
    | Shutdown

type RenderDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: DiagnosticStage
      Message: string
      Cause: string option }

type ViewerPointerButton =
    | PrimaryButton
    | SecondaryButton
    | MiddleButton

type ViewerEvent =
    | Loaded
    | UpdateTick of elapsedSeconds: float
    | RenderTick of elapsedSeconds: float
    | KeyDown of key: string
    | KeyUp of key: string
    | PointerMoved of x: float * y: float
    | PointerPressed of x: float * y: float * button: ViewerPointerButton
    | PointerReleased of x: float * y: float * button: ViewerPointerButton
    | PointerScrolled of x: float * y: float * deltaX: float * deltaY: float
    | PointerExited
    | Resized of Size
    | CloseRequested
    | DiagnosticReported of RenderDiagnostic

type ScreenshotFormat =
    | Png
    | Jpeg

type ScreenshotRequest =
    { Destination: string
      Format: ScreenshotFormat }

type ViewerEffect<'msg> =
    | InitializeRenderer
    | RenderFrame of Scene
    | CaptureScreenshot of ScreenshotRequest
    | Shutdown
    | ReportDiagnostic of RenderDiagnostic
    | Dispatch of 'msg

type ViewerProgram<'model, 'msg> =
    { Configuration: ViewerConfiguration
      Init: unit -> 'model * Cmd<'msg>
      Update: 'msg -> 'model -> 'model * Cmd<'msg>
      View: 'model -> Scene
      EventMapper: ViewerEvent -> 'msg option
      EffectMapper: 'msg -> ViewerEffect<'msg> option
      Subscriptions: 'model -> (string list * (Dispatch<'msg> -> IDisposable)) list }

module Diagnostics =
    let create severity stage message cause : RenderDiagnostic =
        { Severity = severity
          Stage = stage
          Message = message
          Cause = cause }

    let unsupportedPlatform (platform: string) =
        create Fatal PlatformCheck $"Unsupported platform '{platform}'. OpenGL desktop support is limited to Windows and Linux." None

    let invalidConfiguration message =
        create DiagnosticSeverity.Error DiagnosticStage.PlatformCheck message None

    let glUnavailable detail =
        create DiagnosticSeverity.Fatal DiagnosticStage.GlContext "OpenGL initialization is unavailable. The viewer has no fallback renderer." (Some detail)

    let missingCapability (capability: string) detail =
        create DiagnosticSeverity.Warning DiagnosticStage.SkiaContext $"Skia capability '{capability}' is unavailable on the active OpenGL renderer." (Some detail)

    let invalidPath detail =
        create DiagnosticSeverity.Error DiagnosticStage.FrameRender "Invalid path declaration." (Some detail)

    let unavailableFont (family: string) =
        create DiagnosticSeverity.Warning DiagnosticStage.FrameRender $"Font family '{family}' is unavailable; platform font resolution will choose a substitute." None

    let frameRenderFailed detail =
        create DiagnosticSeverity.Error DiagnosticStage.FrameRender "OpenGL/Skia frame rendering failed. The viewer has no fallback renderer." (Some detail)

    let screenshotFailed detail =
        create DiagnosticSeverity.Error DiagnosticStage.ScreenshotCapture "Screenshot capture failed." (Some detail)

    let shutdownFailed detail =
        create DiagnosticSeverity.Warning DiagnosticStage.Shutdown "Renderer shutdown failed." (Some detail)

    let startupFailed stage detail =
        create DiagnosticSeverity.Fatal stage "OpenGL initialization failed. The viewer has no fallback renderer." (Some detail)

    let damageScopedDecision (decision: string) reason =
        let cause = reason |> Option.filter (String.IsNullOrWhiteSpace >> not)
        create DiagnosticSeverity.Info DiagnosticStage.Framebuffer $"Feature157 damage render decision: {decision}." cause

    let private runtimeSeverity severity =
        match severity with
        | DiagnosticSeverity.Info -> FS.GG.UI.Diagnostics.DiagnosticSeverity.Informational
        | DiagnosticSeverity.Warning -> FS.GG.UI.Diagnostics.DiagnosticSeverity.Warning
        | DiagnosticSeverity.Error
        | DiagnosticSeverity.Fatal -> FS.GG.UI.Diagnostics.DiagnosticSeverity.Error

    let private runtimeCategory diagnostic =
        match diagnostic.Stage with
        | DiagnosticStage.PlatformCheck
        | DiagnosticStage.GlContext
        | DiagnosticStage.GlRenderer
        | DiagnosticStage.GlSurface
        | DiagnosticStage.SkiaContext -> FS.GG.UI.Diagnostics.DiagnosticCategory.Environment
        | DiagnosticStage.Framebuffer when diagnostic.Message.Contains("damage render decision", StringComparison.OrdinalIgnoreCase) ->
            FS.GG.UI.Diagnostics.DiagnosticCategory.BackendCost
        | DiagnosticStage.ScreenshotCapture
        | DiagnosticStage.Shutdown -> FS.GG.UI.Diagnostics.DiagnosticCategory.DeveloperAction
        | DiagnosticStage.FrameRender ->
            match diagnostic.Severity with
            | DiagnosticSeverity.Warning -> FS.GG.UI.Diagnostics.DiagnosticCategory.RenderingLimitation
            | _ -> FS.GG.UI.Diagnostics.DiagnosticCategory.ReadinessBlocker
        | DiagnosticStage.Framebuffer -> FS.GG.UI.Diagnostics.DiagnosticCategory.RenderingLimitation

    let private codeFor diagnostic =
        if diagnostic.Message.Contains("damage render decision", StringComparison.OrdinalIgnoreCase) then
            "DamageScopedDecision"
        else
            string diagnostic.Stage

    let private actionFor diagnostic =
        match runtimeCategory diagnostic with
        | FS.GG.UI.Diagnostics.DiagnosticCategory.Environment ->
            "Confirm native host capability or record an accepted environment limitation."
        | FS.GG.UI.Diagnostics.DiagnosticCategory.BackendCost ->
            "No action required unless this appears in a performance-blocked lane."
        | FS.GG.UI.Diagnostics.DiagnosticCategory.RenderingLimitation ->
            "Review the rendering limitation and fallback behavior."
        | FS.GG.UI.Diagnostics.DiagnosticCategory.ReadinessBlocker ->
            "Fix the viewer render failure before accepting readiness."
        | FS.GG.UI.Diagnostics.DiagnosticCategory.DeveloperAction ->
            "Review the host artifact or shutdown diagnostic."

    let toRuntimeDiagnostic context diagnostic =
        let source =
            FS.GG.UI.Diagnostics.RuntimeDiagnostics.source
                (Some "FS.GG.UI.SkiaViewer")
                "opengl-host"
                None
                None

        let message =
            match diagnostic.Cause with
            | Some cause when not (String.IsNullOrWhiteSpace cause) -> $"{diagnostic.Message} Cause: {cause}"
            | _ -> diagnostic.Message

        FS.GG.UI.Diagnostics.RuntimeDiagnostics.create
            source
            (Some(codeFor diagnostic))
            (Some(runtimeSeverity diagnostic.Severity))
            (Some(runtimeCategory diagnostic))
            message
            (Some(actionFor diagnostic))
            context
