namespace FS.GG.UI.SkiaViewer.Host

open System
open Elmish
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type DiagnosticOptions =
    { Verbose: bool }

[<NoEquality; NoComparison>]
/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type ViewerConfiguration =
    { Title: string
      InitialSize: Size
      ClearColor: Color option
      TargetFrameRate: int option
      Diagnostics: DiagnosticOptions
      /// Optional transform applied to the native WindowOptions just before window
      /// creation, carrying window-startup intent into the live presented window.
      ConfigureWindow: (Silk.NET.Windowing.WindowOptions -> Silk.NET.Windowing.WindowOptions) option
      /// Live present mechanism (feature 118), threaded from `ViewerOptions.PresentMode`.
      /// `renderFrame` branches on this; the default `OffscreenReadback` keeps the proven
      /// readback present path byte-identical.
      PresentMode: ViewerPresentMode }

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type DiagnosticSeverity =
    | Info
    | Warning
    | Error
    | Fatal

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
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

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type RenderDiagnostic =
    { Severity: DiagnosticSeverity
      Stage: DiagnosticStage
      Message: string
      Cause: string option }

/// Mouse button identity carried by host pointer press/release events (075, FR-013).
type ViewerPointerButton =
    | PrimaryButton
    | SecondaryButton
    | MiddleButton

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type ViewerEvent =
    | Loaded
    | UpdateTick of elapsedSeconds: float
    | RenderTick of elapsedSeconds: float
    | KeyDown of key: string
    | KeyUp of key: string
    | PointerMoved of x: float * y: float
    // 075 (FR-013): press/release now carry the originating mouse button. Arity
    // change is source-breaking only for matchers; the sole existing matcher
    // (SkiaViewer.fs) is updated in lockstep.
    | PointerPressed of x: float * y: float * button: ViewerPointerButton
    | PointerReleased of x: float * y: float * button: ViewerPointerButton
    // 075 (FR-014): wheel/scroll with signed per-axis delta.
    | PointerScrolled of x: float * y: float * deltaX: float * deltaY: float
    // 075 (FR-007): pointer left the window / host lost pointer focus — drives cancel.
    | PointerExited
    | Resized of Size
    | CloseRequested
    | DiagnosticReported of RenderDiagnostic

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type ScreenshotFormat =
    | Png
    | Jpeg

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type ScreenshotRequest =
    { Destination: string
      Format: ScreenshotFormat }

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type ViewerEffect<'msg> =
    | InitializeRenderer
    | RenderFrame of Scene
    | CaptureScreenshot of ScreenshotRequest
    | Shutdown
    | ReportDiagnostic of RenderDiagnostic
    | Dispatch of 'msg

/// Viewer host contract type (moved from the FS.GG.UI monolith, retyped onto FS.GG.UI.Scene).
type ViewerProgram<'model, 'msg> =
    { Configuration: ViewerConfiguration
      Init: unit -> 'model * Cmd<'msg>
      Update: 'msg -> 'model -> 'model * Cmd<'msg>
      View: 'model -> Scene
      EventMapper: ViewerEvent -> 'msg option
      EffectMapper: 'msg -> ViewerEffect<'msg> option
      Subscriptions: 'model -> (string list * (Dispatch<'msg> -> IDisposable)) list }

/// Structured host diagnostics (moved with the host; behaviour preserved).
module Diagnostics =
    /// Public contract function exposed by this FS.GG.UI package.
    val create: severity: DiagnosticSeverity -> stage: DiagnosticStage -> message: string -> cause: string option -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val unsupportedPlatform: platform: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val invalidConfiguration: message: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val glUnavailable: detail: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val missingCapability: capability: string -> detail: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val invalidPath: detail: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val unavailableFont: family: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val frameRenderFailed: detail: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val screenshotFailed: detail: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val shutdownFailed: detail: string -> RenderDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val startupFailed: stage: DiagnosticStage -> detail: string -> RenderDiagnostic
    /// Feature 157: frame diagnostic for no-clear damage-scoped decisions and fallback reasons.
    val damageScopedDecision: decision: string -> reason: string option -> RenderDiagnostic
    /// Converts a host render diagnostic into the shared runtime diagnostics taxonomy.
    val toRuntimeDiagnostic:
        context: FS.GG.UI.Diagnostics.DiagnosticContext ->
        diagnostic: RenderDiagnostic ->
            FS.GG.UI.Diagnostics.RuntimeDiagnostic
