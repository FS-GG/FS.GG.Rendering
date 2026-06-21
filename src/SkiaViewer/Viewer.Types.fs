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

type ViewerOptions =
    { Title: string
      InitialSize: Size
      PresentMode: ViewerPresentMode
      FrameRateCap: int option }

type ViewerLaunchMode =
    | InteractiveWindow
    | PersistentEvidence

type ViewerCloseReason =
    | UserClose
    | AppRequestedClose
    | EvidenceRequestedClose
    | FrameworkRequestedClose
    | HostSystemClose
    | TimeoutClose
    | FailureDrivenClose

type ViewerObservedValue =
    | Observed of bool
    | Unsupported
    | Unavailable

type ViewerWindowResizePolicy =
    | Resizable
    | FixedSize

type ViewerWindowMaximizePolicy =
    | Maximizable
    | NotMaximizable

[<RequireQualifiedAccess>]
type ViewerWindowStartupState =
    | Normal
    | Maximized
    | Minimized
    | Fullscreen
    | WindowedFullscreen

type ViewerWindowPosition =
    | Centered
    | Coordinates of x: int * y: int

type ViewerBackendPreference =
    | DefaultBackend
    | Vulkan
    | OpenGL
    | Software

type ViewerWindowOptionStatus =
    | Honored
    | Degraded
    | UnsupportedOption
    | FailedOption

type ViewerWindowBehaviorRequest =
    { ResizePolicy: ViewerWindowResizePolicy
      MaximizePolicy: ViewerWindowMaximizePolicy
      StartupState: ViewerWindowStartupState
      StartupPosition: ViewerWindowPosition option
      BackendPreference: ViewerBackendPreference option }

type ViewerWindowOptionResult =
    { Option: string
      Requested: string
      Observed: string option
      Status: ViewerWindowOptionStatus
      Message: string }

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

type ViewerVisualEvidenceKind =
    | Image
    | PixelReadback
    | MetadataHash
    | UnsupportedHost

type ViewerVisualEvidenceArtifact =
    { Kind: ViewerVisualEvidenceKind
      Path: string option
      ImageDecodable: bool option
      ProvesSceneRendering: bool
      ProvesDesktopVisibility: bool
      Message: string }

type ViewerFailureClass =
    | EnvironmentSession
    | WindowVisibility
    | WindowOptions
    | VisualEvidence
    | PackageVerification
    | VerificationDepthFailure
    | AppLifecycle
    | ProductDefectFailure

type ViewerInputDispatchStatus =
    | Verified
    | NotVerified
    | NotRequired

type ViewerDiagnosticLevel =
    | Error
    | Warning
    | Info
    | Debug
    | Trace

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

type ViewerRunFailureClassification =
    | UnsupportedEnvironment
    | PackageResolution
    | VerificationDepth
    | AppLifecycle
    | ProductDefect

type ViewerDiagnosticEvent =
    { Level: ViewerDiagnosticLevel
      Category: ViewerDiagnosticCategory
      Message: string
      FrameIndex: int option
      Stage: ViewerRunBlockedStage option
      Elapsed: TimeSpan option }

type ViewerDiagnosticsOptions =
    { MinimumLevel: ViewerDiagnosticLevel
      Categories: Set<ViewerDiagnosticCategory>
      FrameLogLimit: int option
      Sink: (ViewerDiagnosticEvent -> unit) option
      Verbose: bool }

[<RequireQualifiedAccess>]
type ViewerResponsivenessInputKind =
    | PointerMove
    | PointerDiscrete
    | KeyDown
    | KeyUp
    | Wheel
    | Resize
    | Tick
    | Lifecycle

[<RequireQualifiedAccess>]
type ViewerResponsivenessVisibleResponse =
    | PresentedFrame
    | NoVisibleResponse
    | Failed
    | EnvironmentLimited
    | NotRun

[<RequireQualifiedAccess>]
type ViewerResponsivenessEnvironmentStatus =
    | Measured
    | MissingBoundary
    | LowPrecisionTimestamp
    | NonMonotonicTimestamp
    | NoVisibleSurface
    | HeadlessSubstitute
    | WriteFailed
    | Failed

[<RequireQualifiedAccess>]
type ViewerResponsivenessReadiness =
    | Accepted
    | Rejected
    | Blocked
    | Incomplete
    | EnvironmentLimited
    | Failed

type ViewerResponsivenessPhaseTiming =
    { ReceiptDuration: TimeSpan option
      QueueDelay: TimeSpan option
      RoutingDuration: TimeSpan option
      UpdateDuration: TimeSpan option
      ViewDuration: TimeSpan option
      RetainedStepDuration: TimeSpan option
      LayoutDuration: TimeSpan option
      TextDuration: TimeSpan option
      PaintDuration: TimeSpan option
      PresentDuration: TimeSpan option
      TotalInputToVisibleDuration: TimeSpan option }

type ViewerResponsivenessDirtyRegion =
    { DirtyRectCount: int option
      DirtyArea: int option
      RepaintedNodeCount: int option
      Status: ViewerResponsivenessEnvironmentStatus }

type ViewerLatencyRecord =
    { RecordId: string
      RunId: string
      InputSequenceId: int64
      InputKind: ViewerResponsivenessInputKind
      InputName: string option
      Page: string option
      ControlGroup: string option
      ReceiptTimestamp: DateTimeOffset
      QueueDepthAtReceipt: int
      QueueDepthAtDrain: int
      CoalescedMovementCount: int
      ProductMessageCount: int
      ProductStateChanged: bool
      RuntimeStateChanged: bool
      VisibleResponse: ViewerResponsivenessVisibleResponse
      PresentedFrameId: int64 option
      EnvironmentStatus: ViewerResponsivenessEnvironmentStatus
      PhaseTiming: ViewerResponsivenessPhaseTiming
      DirtyRegion: ViewerResponsivenessDirtyRegion option
      LongFrame: bool
      Diagnostics: string list }

type ViewerResponsivenessBudget =
    { InputReceiptP95: TimeSpan
      InputReceiptMax: TimeSpan
      InputToVisibleP95: TimeSpan
      InputToVisibleMax: TimeSpan
      LongFrameThreshold: TimeSpan }

type ViewerResponsivenessFailedBudget =
    { Kind: string
      Scope: string option
      InputKind: ViewerResponsivenessInputKind option
      Measured: TimeSpan
      Budget: TimeSpan }

type ViewerResponsivenessGroupSummary =
    { Page: string option
      InputKind: ViewerResponsivenessInputKind
      ControlGroup: string option
      Count: int
      P50: TimeSpan option
      P95: TimeSpan option
      Max: TimeSpan option
      LongFrameCount: int
      Readiness: ViewerResponsivenessReadiness }

type ViewerResponsivenessSlowInteraction =
    { RecordId: string
      InputSequenceId: int64
      TotalInputToVisible: TimeSpan option
      DominantPhase: string option }

type ViewerResponsivenessSummary =
    { RunId: string
      Scope: string
      OverallReadiness: ViewerResponsivenessReadiness
      StartedUtc: DateTimeOffset
      CompletedUtc: DateTimeOffset
      RecordsPath: string
      Budgets: ViewerResponsivenessBudget
      FirstFailedBudget: ViewerResponsivenessFailedBudget option
      Groups: ViewerResponsivenessGroupSummary list
      SlowestInteractions: ViewerResponsivenessSlowInteraction list
      EnvironmentLimitations: string list
      Diagnostics: string list }

type ViewerResponsivenessOptions =
    { Enabled: bool
      RunId: string option
      OutputRoot: string option
      Budget: ViewerResponsivenessBudget
      Sink: (ViewerLatencyRecord -> unit) option }

type ViewerInputPriorityLane =
    | Discrete
    | Continuous
    | Lifecycle
    | Background

type ViewerInputEnvelope =
    { SequenceId: int64
      ReceivedAt: DateTimeOffset
      InputKind: ViewerResponsivenessInputKind
      PriorityLane: ViewerInputPriorityLane
      ReceiptQueueDepth: int
      Payload: string }

type ViewerInputQueue =
    { Discrete: ViewerInputEnvelope list
      LatestContinuousPointer: ViewerInputEnvelope option
      ContinuousCoalescedCount: int
      Lifecycle: ViewerInputEnvelope list
      NextSequenceId: int64
      MaxObservedDepth: int }

type ViewerFrameDrain =
    { BatchId: int64
      DiscreteInputs: ViewerInputEnvelope list
      CoalescedPointer: ViewerInputEnvelope option
      CoalescedMovementCount: int
      QueueDepthBeforeDrain: int
      QueueDepthAfterDrain: int
      DrainReason: string }

type ViewerDirtyState =
    { ProductModelChanged: bool
      RuntimeStateChanged: bool
      SizeChanged: bool
      ThemeChanged: bool
      SceneDirty: bool
      DirtyRegionSummary: ViewerResponsivenessDirtyRegion option
      Reason: string list }

type ViewerEvidenceTarget =
    | FirstFrame
    | FrameCount of int
    | Duration of TimeSpan

type ViewerRunRequest =
    { Target: ViewerEvidenceTarget
      Timeout: TimeSpan
      Diagnostics: ViewerDiagnosticsOptions
      RendererMode: string
      EvidencePath: string option }

type ViewerRunEvidence =
    { FramesRendered: int
      Elapsed: TimeSpan
      InitialOutputSize: Size
      RendererMode: string
      LastDiagnosticSummary: string option
      EvidencePath: string option }

type ViewerRunFailure =
    { BlockedStage: ViewerRunBlockedStage
      Classification: ViewerRunFailureClassification
      DiagnosticCategory: ViewerDiagnosticCategory
      Message: string
      LastDiagnosticSummary: string option }

[<RequireQualifiedAccess>]
type ViewerTimingPath =
    | FullRedraw
    | DamageScoped

[<RequireQualifiedAccess>]
type ViewerDamageDecision =
    | DamageScopedAccepted
    | FullRedraw
    | SkipNoChange
    | Rejected
    | EnvironmentLimited

type ScreenshotEvidenceStatus =
    | ScreenshotOk
    | ScreenshotUnsupported
    | ScreenshotFailed

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

type ViewerOpenStatus =
    | ViewerOpenConfirmed
    | ViewerOpenUnsupported
    | ViewerOpenFailed
    | ViewerOpenUnknown

type FirstFrameStatus =
    | FirstFramePresentedStatus
    | FirstFrameNotPresentedStatus
    | FirstFrameUnknownStatus

type ScreenshotCaptureAvailability =
    | CaptureAvailable
    | CaptureUnavailable of reason: string
    | CaptureAvailabilityUnknown of reason: string

type ScreenshotCaptureSource =
    | LiveViewerWindow
    | DeterministicSceneRender
    | PixelReadbackSource
    | NoCaptureSource

type ScreenshotPixelContentValidation =
    | PixelContentNonBlank
    | PixelContentBlank
    | PixelContentUnreadable of reason: string
    | PixelContentNotValidated of reason: string

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

type ViewerRuntimeCapability =
    { PersistentWindow: bool
      BoundedSmoke: bool
      KeyboardInput: bool
      RendererMode: string
      UnsupportedHostReasons: string list
      MissingPackageCapabilities: string list }

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

type ViewerModel =
    { Options: ViewerOptions
      WindowBehavior: ViewerWindowBehaviorRequest
      IsRunning: bool
      LifecycleState: ViewerLifecycleState
      FirstFramePresented: bool
      UserCloseObserved: bool
      InputDispatch: ViewerInputDispatchStatus
      LastScene: SceneNode option }

type ViewerRunModel =
    { Request: ViewerRunRequest
      FramesRendered: int
      StartedAt: DateTimeOffset option
      LastDiagnostic: ViewerDiagnosticEvent option
      Completed: Result<ViewerRunEvidence, ViewerRunFailure> option }

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

type ViewerRunMsg =
    | BeginRun
    | RunStarted of DateTimeOffset
    | RecordFrame of Size
    | RecordDiagnostic of ViewerDiagnosticEvent
    | CompleteRun
    | FailRun of ViewerRunFailure
    | TimeoutRun

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

type ViewerRunEffect =
    | OpenBoundedWindow of ViewerRunRequest
    | RequestFrame
    | CaptureOutputSize
    | StopBoundedRun
    | PersistRunEvidence of ViewerRunEvidence

type EvidenceWorkflowModel =
    { Request: ScreenshotEvidenceRequest
      ViewerOpenStatus: ViewerOpenStatus
      FirstFrameStatus: FirstFrameStatus
      CaptureAvailability: ScreenshotCaptureAvailability
      OutputPath: string option
      Result: ScreenshotEvidenceResult option
      Diagnostics: string list }

type EvidenceWorkflowMsg =
    | LaunchStarted
    | LaunchCompleted of ViewerOpenStatus
    | FirstFrameObserved of FirstFrameStatus
    | CaptureCapabilityKnown of ScreenshotCaptureAvailability
    | CaptureSucceeded of path: string * width: int * height: int * source: ScreenshotCaptureSource
    | CaptureUnsupported of reason: string * fallbackKind: string option
    | CaptureFailed of message: string
    | EvidenceReportWritten of path: string

type EvidenceWorkflowEffect =
    | LaunchViewerForEvidence of ScreenshotEvidenceRequest
    | CaptureViewerScreenshot of outputPath: string
    | ValidateScreenshotArtifact of path: string
    | WriteScreenshotEvidenceReport of ScreenshotEvidenceResult
    | CleanupEvidenceViewer
    | CollectProcessOutput
    | ValidateGeneratedGuidance

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
/// (085). X/Y are in the swapchain/scene coordinate space; consumers hit-test against the
/// scene they rendered for the same `Size`.
type ViewerPointerInput =
    { Phase: ViewerPointerPhaseKind
      X: float
      Y: float
      Button: ViewerPointerButtonKind option
      DeltaX: float
      DeltaY: float }

/// Pointer-aware, size-aware durable host variant (feature 085). Mirrors `GeneratedAppHost`
/// field-for-field PLUS a model-aware pointer seam (`MapPointer`) and a size-carrying `View`,
/// so the existing `GeneratedAppHost` construction sites and the durable
/// `Viewer.runApp viewerOptions generatedHost` GovernanceTests literal are unbroken (FR-006).
/// This is the Controls-free lower runner; the Control/PointerInteraction-aware
/// `InteractiveAppHost` (FS.GG.UI.Controls.Elmish) adapts onto it (research D3-AMEND).
type InteractiveViewerHost<'model,'msg> =
    { Init: unit -> 'model * ViewerEffect list
      Update: 'msg -> 'model -> 'model * ViewerEffect list
      View: Size -> 'model -> SceneNode
      // 092 (FR-006): `'msg list` (was `'msg option`) — one key can dispatch several messages in
      // order; `[]` = unhandled. Folded through `Update` exactly like the pointer `'msg list` path.
      MapKey: ViewerKey -> bool -> 'msg list
      MapPointer: ViewerPointerInput -> Size -> 'model -> 'msg list
      Tick: TimeSpan -> 'msg option
      Diagnostics: ViewerDiagnosticsOptions }

[<RequireQualifiedAccess>]
type ViewerScriptInput =
    | Key of key: ViewerKey * isDown: bool
    | Pointer of ViewerPointerInput
    | WaitFrame
