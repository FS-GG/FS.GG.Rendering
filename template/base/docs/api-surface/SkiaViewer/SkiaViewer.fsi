namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene

/// Public contract type exposed by this FS.GG.UI package.
type ViewerOptions =
    { Title: string
      InitialSize: Size
      /// Live present mechanism. Defaults to `ViewerPresentMode.DirectToSwapchain` (feature 119) — the
      /// readback-free direct present on the OpenGL backend (the scene is drawn straight onto the default
      /// framebuffer and presented by the toolkit buffer swap, no per-frame GPU→CPU readback). Set to
      /// `ViewerPresentMode.OffscreenReadback` only for evidence/screenshot capture that needs a readback.
      /// (Feature 120, FR-016: corrects stale feature-118 text that named `OffscreenReadback` as the
      /// default; the shipped default lives at `Viewer.defaultConfiguration`, which uses
      /// `DirectToSwapchain` — the docstring is brought into agreement with it.)
      PresentMode: ViewerPresentMode
      /// Optional consumer frame-rate cap for the live persistent interactive loop (feature 121,
      /// FR-001/FR-002). `None` keeps the default 60 FPS — the exact pre-feature-121 behaviour. `Some n`
      /// (n > 0) bounds BOTH the update and the present cadence of the native event loop, so a host
      /// without a blocking compositor does not free-run the present loop; `Some n` with n <= 0 is
      /// rejected at startup validation. Ignored by the offscreen/evidence (`runBounded`) path, which
      /// does not use the persistent event loop.
      FrameRateCap: int option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerLaunchMode =
    | InteractiveWindow
    | PersistentEvidence

/// Public contract type exposed by this FS.GG.UI package.
type ViewerCloseReason =
    | UserClose
    | AppRequestedClose
    | EvidenceRequestedClose
    | FrameworkRequestedClose
    | HostSystemClose
    | TimeoutClose
    | FailureDrivenClose

/// Public contract type exposed by this FS.GG.UI package.
type ViewerObservedValue =
    | Observed of bool
    | Unsupported
    | Unavailable

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowResizePolicy =
    | Resizable
    | FixedSize

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowMaximizePolicy =
    | Maximizable
    | NotMaximizable

[<RequireQualifiedAccess>]
/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowStartupState =
    | Normal
    | Maximized
    | Minimized
    | Fullscreen
    /// Borderless coverage of the monitor work area (no title bar / resize chrome,
    /// no exclusive-mode resolution change). Distinct from exclusive `Fullscreen`.
    | WindowedFullscreen

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowPosition =
    | Centered
    | Coordinates of x: int * y: int

/// Public contract type exposed by this FS.GG.UI package.
type ViewerBackendPreference =
    | DefaultBackend
    | Vulkan
    | OpenGL
    | Software

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowOptionStatus =
    | Honored
    | Degraded
    | UnsupportedOption
    | FailedOption

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowBehaviorRequest =
    { ResizePolicy: ViewerWindowResizePolicy
      MaximizePolicy: ViewerWindowMaximizePolicy
      StartupState: ViewerWindowStartupState
      StartupPosition: ViewerWindowPosition option
      BackendPreference: ViewerBackendPreference option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowOptionResult =
    { Option: string
      Requested: string
      Observed: string option
      Status: ViewerWindowOptionStatus
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowStateDiagnostic =
    { WindowInitialized: bool
      NativeHandle: ViewerObservedValue
      Visible: ViewerObservedValue
      Focusable: ViewerObservedValue
      Focused: ViewerObservedValue
      Minimized: ViewerObservedValue
      Maximized: ViewerObservedValue
      ClientSize: string option
      RenderableSurfaceAvailable: ViewerObservedValue
      Backend: string option
      InputDevicesAvailable: ViewerObservedValue
      FailureClass: string option
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerVisualEvidenceKind =
    | Image
    | PixelReadback
    | MetadataHash
    | UnsupportedHost

/// Public contract type exposed by this FS.GG.UI package.
type ViewerVisualEvidenceArtifact =
    { Kind: ViewerVisualEvidenceKind
      Path: string option
      ImageDecodable: bool option
      ProvesSceneRendering: bool
      ProvesDesktopVisibility: bool
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerFailureClass =
    | EnvironmentSession
    | WindowVisibility
    | WindowOptions
    | VisualEvidence
    | PackageVerification
    | VerificationDepthFailure
    | AppLifecycle
    | ProductDefectFailure

/// Public contract type exposed by this FS.GG.UI package.
type ViewerInputDispatchStatus =
    | Verified
    | NotVerified
    | NotRequired

/// Public contract type exposed by this FS.GG.UI package.
type ViewerDiagnosticLevel =
    | Error
    | Warning
    | Info
    | Debug
    | Trace

/// Public contract type exposed by this FS.GG.UI package.
type ViewerDiagnosticCategory =
    | Startup
    | EnvironmentSession
    | Input
    | Frame
    | Renderer
    | OpenGl
    | Skia
    | Framebuffer
    | Scene
    | Screenshot

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunBlockedStage =
    | DesktopPrerequisite
    | ProcessLaunch
    | WindowCreation
    | FirstFrameRender
    | Observation
    | Capture
    | InputVerification
    | ControlledExit
    | ArtifactWrite
    | Window
    | Surface
    | Renderer
    | GlContext
    | Scene
    | Readback
    | App
    | Timeout
    | Unknown

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunFailureClassification =
    | UnsupportedEnvironment
    | PackageResolution
    | VerificationDepth
    | AppLifecycle
    | ProductDefect

/// Public contract type exposed by this FS.GG.UI package.
type ViewerDiagnosticEvent =
    { Level: ViewerDiagnosticLevel
      Category: ViewerDiagnosticCategory
      Message: string
      FrameIndex: int option
      Stage: ViewerRunBlockedStage option
      Elapsed: TimeSpan option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerDiagnosticsOptions =
    { MinimumLevel: ViewerDiagnosticLevel
      Categories: Set<ViewerDiagnosticCategory>
      FrameLogLimit: int option
      Sink: (ViewerDiagnosticEvent -> unit) option
      Verbose: bool }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerEvidenceTarget =
    | FirstFrame
    | FrameCount of int
    | Duration of TimeSpan

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunRequest =
    { Target: ViewerEvidenceTarget
      Timeout: TimeSpan
      Diagnostics: ViewerDiagnosticsOptions
      RendererMode: string
      EvidencePath: string option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunEvidence =
    { FramesRendered: int
      Elapsed: TimeSpan
      InitialOutputSize: Size
      RendererMode: string
      LastDiagnosticSummary: string option
      EvidencePath: string option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunFailure =
    { BlockedStage: ViewerRunBlockedStage
      Classification: ViewerRunFailureClassification
      DiagnosticCategory: ViewerDiagnosticCategory
      Message: string
      LastDiagnosticSummary: string option }

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotEvidenceStatus =
    | ScreenshotOk
    | ScreenshotUnsupported
    | ScreenshotFailed

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotEvidenceRequest =
    { Command: string
      AppOrSample: string
      OutputPath: string
      Width: int
      Height: int
      RendererMode: string
      CaptureMode: ScreenshotCaptureMode
      HostFacts: string list
      Timeout: TimeSpan }

and ScreenshotCaptureMode =
    | ViewerRenderTargetPng

/// Public contract type exposed by this FS.GG.UI package.
type ViewerOpenStatus =
    | ViewerOpenConfirmed
    | ViewerOpenUnsupported
    | ViewerOpenFailed
    | ViewerOpenUnknown

/// Public contract type exposed by this FS.GG.UI package.
type FirstFrameStatus =
    | FirstFramePresentedStatus
    | FirstFrameNotPresentedStatus
    | FirstFrameUnknownStatus

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotCaptureAvailability =
    | CaptureAvailable
    | CaptureUnavailable of reason: string
    | CaptureAvailabilityUnknown of reason: string

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotCaptureSource =
    | LiveViewerWindow
    | DeterministicSceneRender
    | PixelReadbackSource
    | NoCaptureSource

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotPixelContentValidation =
    | PixelContentNonBlank
    | PixelContentBlank
    | PixelContentUnreadable of reason: string
    | PixelContentNotValidated of reason: string

/// Public contract type exposed by this FS.GG.UI package.
type ScreenshotEvidenceResult =
    { Status: ScreenshotEvidenceStatus
      Command: string
      AppOrSample: string
      HostFacts: string list
      CaptureMode: ScreenshotCaptureMode
      EvidenceKind: string
      OutputPath: string option
      ScreenshotPath: string option
      Width: int option
      Height: int option
      PixelContentValidation: ScreenshotPixelContentValidation
      RendererMode: string
      FramesRendered: int option
      ViewerOpenStatus: ViewerOpenStatus
      FirstFrameStatus: FirstFrameStatus
      CaptureAvailability: ScreenshotCaptureAvailability
      CaptureSource: ScreenshotCaptureSource
      DeterministicFallbackKind: string option
      ProvesScreenshot: bool
      BlockedStage: ViewerRunBlockedStage option
      Classification: ViewerRunFailureClassification option
      Category: ViewerDiagnosticCategory option
      Message: string
      Timestamp: DateTimeOffset
      UnsupportedHostReason: string option
      Fallback: string option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRuntimeCapability =
    { PersistentWindow: bool
      BoundedSmoke: bool
      KeyboardInput: bool
      RendererMode: string
      UnsupportedHostReasons: string list
      MissingPackageCapabilities: string list }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerDesktopSessionDiagnostic =
    { RuntimeDirectory: string option
      RuntimeDirectoryExists: bool
      RuntimeDirectoryOwnerSuitable: bool
      RuntimeDirectoryPermissionsSuitable: bool
      DisplayVariable: string option
      DisplaySocket: string option
      DisplaySocketExists: bool
      SessionBus: string option
      FallbackRuntimeDirectory: string option
      FallbackIsFullDesktopSession: bool
      DiagnosticClass: string
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerLaunchOutcome =
    { Status: string
      Mode: string
      Command: string option
      RendererMode: string
      WindowOpened: bool
      WindowVisible: ViewerObservedValue
      FirstFramePresented: bool
      CloseReason: ViewerCloseReason option
      UserCloseObserved: bool
      AppCloseObserved: bool
      EvidenceCloseObserved: bool
      SelfClosedForEvidence: bool
      InputDispatch: string
      ExitPath: bool
      WindowDiagnostics: ViewerWindowStateDiagnostic list
      OptionResults: ViewerWindowOptionResult list
      VisualEvidence: ViewerVisualEvidenceArtifact list
      FailureClass: ViewerFailureClass option
      BlockedStage: ViewerRunBlockedStage option
      Classification: ViewerRunFailureClassification option
      Category: ViewerDiagnosticCategory option
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerWindowObservationResult =
    { DiagnosticSource: string
      Command: string option
      HostFacts: string list
      ViewerFacts: string list
      ViewerWindowOpened: bool
      ViewerFirstFramePresented: bool
      ViewerWindowVisible: ViewerObservedValue
      ExternalObservationAttempted: bool
      ExternalWindowMatched: bool option
      CaptureAttempted: bool
      CaptureSucceeded: bool option
      BlockedStage: ViewerRunBlockedStage option
      Classification: ViewerRunFailureClassification option
      MissingFacts: string list
      Message: string }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerLifecycleState =
    | NotStarted
    | CheckingDesktopSession
    | StartingWindow
    | WindowCreated
    | VisibilityChecking
    | InteractiveRunning
    | EvidenceRunning
    | FirstFramePresented
    | CloseRequested
    | Closing
    | UserCloseObservedState
    | AppCloseObservedState
    | EvidenceCloseObservedState
    | InaccessibleWindow
    | Failed
    | Unsupported

/// Public contract type exposed by this FS.GG.UI package.
type ViewerModel =
    { Options: ViewerOptions
      WindowBehavior: ViewerWindowBehaviorRequest
      IsRunning: bool
      LifecycleState: ViewerLifecycleState
      FirstFramePresented: bool
      UserCloseObserved: bool
      InputDispatch: ViewerInputDispatchStatus
      LastScene: SceneNode option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunModel =
    { Request: ViewerRunRequest
      FramesRendered: int
      StartedAt: DateTimeOffset option
      LastDiagnostic: ViewerDiagnosticEvent option
      Completed: Result<ViewerRunEvidence, ViewerRunFailure> option }

/// Public contract type exposed by this FS.GG.UI package.
type ViewerMsg =
    | Start
    | StartInteractive
    | StartEvidence of ViewerRunRequest
    | Stop
    | DesktopSessionChecked of ViewerDesktopSessionDiagnostic
    | WindowCreated of ViewerWindowStateDiagnostic
    | VisibilityCheckStarted of ViewerWindowStateDiagnostic
    | VisibilityObserved of ViewerWindowStateDiagnostic
    | Render of SceneNode
    | KeyEvent of ViewerKeyEvent
    | DiagnosticCaptured of ViewerDiagnosticEvent
    | FramePresented of Size
    | UserCloseObserved
    | AppCloseRequested
    | EvidenceCloseRequested
    | HostCloseObserved
    | EvidenceTargetReached
    | RunFailed of ViewerRunFailure
    | RunTimedOut

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunMsg =
    | BeginRun
    | RunStarted of DateTimeOffset
    | RecordFrame of Size
    | RecordDiagnostic of ViewerDiagnosticEvent
    | CompleteRun
    | FailRun of ViewerRunFailure
    | TimeoutRun

/// Public contract type exposed by this FS.GG.UI package.
type ViewerEffect =
    | OpenWindow of title: string * size: Size
    | ApplyWindowOptions of ViewerWindowBehaviorRequest
    | QueryNativeWindowState
    | RenderScene of SceneNode
    | CloseWindow
    | DispatchInput of ViewerKey * isDown: bool
    | EmitDiagnostic of ViewerDiagnosticEvent
    | CheckDesktopSession
    | StartBoundedRun of ViewerRunRequest
    | CaptureScreenshot of path: string
    | CaptureImageEvidence of path: string
    | ReadPixels
    | WriteVisualEvidence of path: string * artifact: ViewerVisualEvidenceArtifact
    | WriteRunEvidence of path: string * evidence: ViewerRunEvidence

/// Public contract type exposed by this FS.GG.UI package.
type ViewerRunEffect =
    | OpenBoundedWindow of ViewerRunRequest
    | RequestFrame
    | CaptureOutputSize
    | StopBoundedRun
    | PersistRunEvidence of ViewerRunEvidence

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceWorkflowModel =
    { Request: ScreenshotEvidenceRequest
      ViewerOpenStatus: ViewerOpenStatus
      FirstFrameStatus: FirstFrameStatus
      CaptureAvailability: ScreenshotCaptureAvailability
      OutputPath: string option
      Result: ScreenshotEvidenceResult option
      Diagnostics: string list }

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceWorkflowMsg =
    | LaunchStarted
    | LaunchCompleted of ViewerOpenStatus
    | FirstFrameObserved of FirstFrameStatus
    | CaptureCapabilityKnown of ScreenshotCaptureAvailability
    | CaptureSucceeded of path: string * width: int * height: int * source: ScreenshotCaptureSource
    | CaptureUnsupported of reason: string * fallbackKind: string option
    | CaptureFailed of message: string
    | EvidenceReportWritten of path: string

/// Public contract type exposed by this FS.GG.UI package.
type EvidenceWorkflowEffect =
    | LaunchViewerForEvidence of ScreenshotEvidenceRequest
    | CaptureViewerScreenshot of outputPath: string
    | ValidateScreenshotArtifact of path: string
    | WriteScreenshotEvidenceReport of ScreenshotEvidenceResult
    | CleanupEvidenceViewer
    | CollectProcessOutput
    | ValidateGeneratedGuidance

/// Public contract type exposed by this FS.GG.UI package.
type GeneratedAppHost<'model,'msg> =
    { Init: unit -> 'model * ViewerEffect list
      Update: 'msg -> 'model -> 'model * ViewerEffect list
      View: 'model -> SceneNode
      MapKey: ViewerKey -> bool -> 'msg option
      Tick: TimeSpan -> 'msg option
      Diagnostics: ViewerDiagnosticsOptions }

[<RequireQualifiedAccess>]
/// Framework-neutral pointer button identity surfaced to the interactive host (085).
type ViewerPointerButtonKind =
    | Primary
    | Secondary
    | Middle

[<RequireQualifiedAccess>]
/// The kind of raw pointer sample the interactive host delivers (085).
type ViewerPointerPhaseKind =
    | Moved
    | Pressed
    | Released
    | Wheel
    | Exited

/// A host-independent pointer sample raised by the live window for the interactive host
/// (085). X/Y are in the swapchain/scene coordinate space.
type ViewerPointerInput =
    { Phase: ViewerPointerPhaseKind
      X: float
      Y: float
      Button: ViewerPointerButtonKind option
      DeltaX: float
      DeltaY: float }

/// Pointer-aware, size-aware durable host variant (feature 085). Mirrors `GeneratedAppHost`
/// field-for-field PLUS a model-aware pointer seam (`MapPointer`) and a size-carrying `View`.
/// Controls-free lower runner; the Control/PointerInteraction-aware `InteractiveAppHost`
/// (FS.GG.UI.Controls.Elmish) adapts onto it (research D3-AMEND). `GeneratedAppHost` and
/// `Viewer.runApp` are left intact (FR-006).
/// Feature 091 (E2, behavioral note — signature unchanged): the per-message repaint stores the
/// `SceneNode` the host's `View` produces (the existing `currentScene <- host.View …` seam). The
/// viewer is framework-neutral and does not itself diff (its `View` yields an opaque `SceneNode`,
/// not a `Control<'msg>` tree); the keyed-reconciliation retained path lives at the controls
/// adapter edge (`Controls.Elmish.runInteractiveApp`), whose `View` produces each frame by diffing
/// the next tree against a retained previous tree (`module internal RetainedRender`). So when the
/// host is the controls adapter, this repaint is already O(changed-subtree) and byte-identical to a
/// full rebuild (FR-004/FR-005); a generic host that supplies its own `View` is unchanged.
///
/// Feature 092 (FR-006): `MapKey` returns `'msg list` (was `'msg option`) so one key can dispatch
/// SEVERAL product messages in order — e.g. a focused control with more than one `onChanged`
/// binding. `[]` = the key is unhandled by the host seam (was `None`); a non-empty list is folded
/// through `Update` in order. Migration is mechanical: `Some m` → `[ m ]`, `None` → `[]`. The
/// sibling `GeneratedAppHost.MapKey` is DELIBERATELY left at `'msg option`: it backs the
/// non-interactive `Viewer.runApp` path (generated projects, samples) where multi-message keys are
/// not needed, and widening it would churn the template/generated host for no behavioral gain.
type InteractiveViewerHost<'model,'msg> =
    { Init: unit -> 'model * ViewerEffect list
      Update: 'msg -> 'model -> 'model * ViewerEffect list
      View: Size -> 'model -> SceneNode
      MapKey: ViewerKey -> bool -> 'msg list
      MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list
      Tick: TimeSpan -> 'msg option
      Diagnostics: ViewerDiagnosticsOptions }

/// Public contract module exposed by this FS.GG.UI package.
module Viewer =
    /// Public contract function exposed by this FS.GG.UI package.
    val init: options: ViewerOptions -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val initWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val update: msg: ViewerMsg -> model: ViewerModel -> ViewerModel * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val initRun: request: ViewerRunRequest -> ViewerRunModel * ViewerRunEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateRun: msg: ViewerRunMsg -> model: ViewerRunModel -> ViewerRunModel * ViewerRunEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultDiagnostics: ViewerDiagnosticsOptions
    /// Public contract function exposed by this FS.GG.UI package.
    val defaultWindowBehavior: ViewerWindowBehaviorRequest
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowBehavior: request: ViewerWindowBehaviorRequest -> ViewerWindowOptionResult list
    /// Public contract function exposed by this FS.GG.UI package.
    val validateWindowLaunchBehavior: initialSize: Size -> request: ViewerWindowBehaviorRequest -> ViewerWindowOptionResult list
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyWindowState: diagnostic: ViewerWindowStateDiagnostic -> ViewerLifecycleState
    /// Public contract function exposed by this FS.GG.UI package.
    val shouldCaptureDiagnostic: options: ViewerDiagnosticsOptions -> diagnostic: ViewerDiagnosticEvent -> bool
    /// Public contract function exposed by this FS.GG.UI package.
    val captureDiagnostic: options: ViewerDiagnosticsOptions -> diagnostic: ViewerDiagnosticEvent -> ViewerDiagnosticEvent option
    /// Public contract function exposed by this FS.GG.UI package.
    val failureFromDiagnostic: diagnostic: ViewerDiagnosticEvent -> ViewerRunFailure
    /// Public contract function exposed by this FS.GG.UI package.
    val classifyWindowObservation: outcome: ViewerLaunchOutcome -> externalObservationAttempted: bool -> externalWindowMatched: bool option -> captureAttempted: bool -> captureSucceeded: bool option -> ViewerWindowObservationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val desktopSessionDiagnostic: unit -> ViewerDesktopSessionDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val runtimeCapability: unit -> ViewerRuntimeCapability
    /// Public contract function exposed by this FS.GG.UI package.
    val run: options: ViewerOptions -> scene: SceneNode -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runApp: options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runAppWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Feature 085 — pointer-aware, size-aware durable launch. Routes native pointer events
    /// and window resizes to the host and renders the size-aware `View`; additive to
    /// `runApp`/`runAppWithWindowBehavior`, which stay intact (FR-004/FR-006/FR-009).
    val runInteractiveViewer: options: ViewerOptions -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runInteractiveViewer` with an explicit window behavior.
    val runInteractiveViewerWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runAppEvidence: request: ViewerRunRequest -> options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runBounded: request: ViewerRunRequest -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runUntilFirstFrame: options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runForFrames: frameCount: int -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val captureScreenshotEvidence: request: ScreenshotEvidenceRequest -> options: ViewerOptions -> scene: SceneNode -> ScreenshotEvidenceResult
    /// Public contract function exposed by this FS.GG.UI package.
    val initEvidenceWorkflow: request: ScreenshotEvidenceRequest -> EvidenceWorkflowModel * EvidenceWorkflowEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val updateEvidenceWorkflow: msg: EvidenceWorkflowMsg -> model: EvidenceWorkflowModel -> EvidenceWorkflowModel * EvidenceWorkflowEffect list

/// Public contract module exposed by this FS.GG.UI package.
module GeneratedAppHost =
    /// Public contract function exposed by this FS.GG.UI package.
    val dispatchKey: host: GeneratedAppHost<'model,'msg> -> raw: ViewerKeyEvent -> model: 'model -> 'model * ViewerEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val smoke: host: GeneratedAppHost<'model,'msg> -> request: ViewerRunRequest -> Result<ViewerRunEvidence, ViewerRunFailure>
