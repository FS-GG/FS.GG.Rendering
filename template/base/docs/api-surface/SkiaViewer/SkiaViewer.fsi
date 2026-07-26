// See skill: fs-gg-skiaviewer
namespace FS.GG.UI.SkiaViewer

open System
open FS.GG.Audio.Core
open FS.GG.UI.Canvas
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
      FrameRateCap: int option
      /// Issue #246: the fixed logical resolution a product renders in. `None` (the pre-#246
      /// behaviour) means the product renders directly in output-surface coordinates. `Some logical`
      /// makes the host scale that canvas uniformly to the output surface and center it, clipping
      /// content to the canvas and leaving letterbox bars on the surplus axis — so a fixed-resolution
      /// game fills whatever window or offscreen evidence surface it is given.
      ///
      /// When set, the logical size is also what a size-aware `InteractiveViewerHost.View` is handed,
      /// and pointer input is mapped back into logical coordinates before `MapPointer` sees it: the
      /// product never observes the window size. `Some` with a non-positive extent is rejected at
      /// startup validation. See `LogicalCanvas`.
      LogicalSize: Size option }

/// Issue #246: how a fixed logical canvas maps onto the actual output surface — a uniform
/// scale plus the centering offset that puts the unused surface into letterbox bars.
type LogicalCanvasFit =
    { Scale: float
      OffsetX: float
      OffsetY: float }

/// Issue #246: the letterbox seam for a fixed-logical-resolution product.
///
/// The game family withholds the window `Size` from `view` on purpose, so a product renders
/// in its own logical coordinate space and the host fits that space to whatever surface it
/// was given. `ViewerOptions.LogicalSize` turns that fit on; everything here is the pure
/// arithmetic behind it, so a product can assert the mapping with no window and no device.
[<RequireQualifiedAccess>]
module LogicalCanvas =

    /// The uniform (aspect-preserving) fit of `logical` centered inside `actual`.
    /// A non-positive extent on either size yields the identity fit rather than a division by zero.
    val fit: logical: Size -> actual: Size -> LogicalCanvasFit

    /// Wrap a scene authored in `logical` coordinates so it renders scaled and centered in `actual`.
    /// Content is always clipped to the logical canvas — a product cannot draw into the letterbox
    /// bars, and that does not become a fact about the current window size. Under the identity fit
    /// the scale is elided and the clip is a visual no-op, so an unscaled render stays
    /// pixel-identical to one taken without a `LogicalSize`.
    val present: logical: Size -> actual: Size -> node: SceneNode -> SceneNode

    /// Map a point in `actual` (window/surface) coordinates back into `logical` coordinates —
    /// the inverse of `present`, for routing pointer input to a letterboxed product. Points inside
    /// a letterbox bar map outside the logical canvas, which is faithful: nothing is drawn there.
    val toLogicalPoint: logical: Size -> actual: Size -> x: float -> y: float -> float * float

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
[<RequireQualifiedAccess>]
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
    /// Runtime native-window behavior changes and their observable outcomes.
    | Window
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
    /// #141: the offscreen CPU scene raster the capture really performs (no GL context, no live window).
    | OffscreenSceneRaster
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
/// Feature 183 (US3): the four window-observation inputs `classifyWindowObservation` takes, named so
/// the two bools and two bool-options cannot be transposed at the call site. Values/results unchanged.
type WindowObservationInputs =
    { ExternalObservationAttempted: bool
      ExternalWindowMatched: bool option
      CaptureAttempted: bool
      CaptureSucceeded: bool option }

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
    /// Apply a live windowed, borderless, maximized, or fullscreen transition on the persistent
    /// native host. Unsupported live changes are reported through Window diagnostics.
    | ApplyWindowOptions of ViewerWindowBehaviorRequest
    /// Select the logical coordinate space used by the live viewer. The viewer owns both the
    /// logical-to-surface presentation fit and the inverse native-pointer mapping; products and
    /// Controls hosts continue to author, lay out, and hit-test directly in this size.
    | ApplyLogicalCanvas of Size
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
    /// Issue #245 — a batch of sound requests a product's `update` emitted, in dispatch order.
    /// Pure data: no device handle, no closure. Only `runAppWithAudio` realizes it, by handing the
    /// batch to the caller-supplied sink; `runApp` and the evidence paths discard it (a viewer
    /// owns no audio device). Effects within one batch are played in list order.
    | PlayAudio of effects: AudioEffect list
    /// Issue #535 — a batch of save/load requests a product's `update` emitted, in dispatch order.
    /// Pure data: no file handle, no stream, no closure.
    ///
    /// THIS CASE IS THE POINT OF #535. Until it existed, no `ViewerEffect` carried a
    /// `PersistenceEffect`, so a product could request a save and no host could ever see it: the
    /// record-only interpreter was the only thing that would ever consume one, and it records and
    /// drops. A product's save requests had nowhere to go, and nothing said so.
    ///
    /// Only `runAppWithPersistence` / `runAppWithAudioAndPersistence` realize it — by handing the batch to the caller-supplied sink and
    /// dispatching each `PersistenceOutcome` the sink returns back into `update` as a `'msg`. `runApp`
    /// and `runAppWithAudio` discard it, exactly as they discard `PlayAudio`: a viewer owns no save
    /// location, and inventing one would be worse than owning none.
    | Persist of effects: PersistenceEffect list

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

/// Public contract type exposed by this FS.GG.UI package.
/// Issue #911: the `GeneratedAppHost.auditKeyWiring` result — a declared input surface partitioned
/// into the events that route through `MapKey` to a product message and the DEAD ones that route to
/// none. The scene-host analogue of the click path's `ControlRenderResult.BoundIds`: where that guard
/// answers "a button that renders is a button that dispatches", `Dead` answers the sibling question
/// for this host — "a key that is bound is a key that dispatches" — and a non-empty `Dead` over a
/// product's declared keymap is a bound key that dispatches nothing.
type SceneHostKeyWiring<'msg> =
    { Wired: (ViewerKeyEvent * 'msg) list
      Dead: ViewerKeyEvent list }

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

[<RequireQualifiedAccess>]
/// Policy for replaceable continuous pointer samples at the interactive viewer boundary.
/// `CoalesceLatestPerFrame` retains only the newest `Moved` sample for each presentation boundary;
/// `Immediate` applies every move. Press/release/wheel/exit remain lossless in both modes.
type ViewerContinuousPointerPolicy =
    | CoalesceLatestPerFrame
    | Immediate

[<RequireQualifiedAccess>]
type ViewerPointerRepaintCause =
    | ContinuousPointer
    | DiscretePointer
    | MixedPointer

/// Live counters emitted after each pointer-input batch is drained.
type ViewerPointerPacingMetrics =
    { RawSamplesReceived: int
      FoldedSamplesApplied: int
      CoalescedSamples: int
      ModelUpdates: int
      PresentedFrames: int64
      RepaintCause: ViewerPointerRepaintCause
      FullRenderFallbacks: int }

type ViewerPointerPacingOptions =
    { ContinuousPolicy: ViewerContinuousPointerPolicy
      OnMetrics: ViewerPointerPacingMetrics -> unit }

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
    val classifyWindowObservation: outcome: ViewerLaunchOutcome -> inputs: WindowObservationInputs -> ViewerWindowObservationResult
    /// Public contract function exposed by this FS.GG.UI package.
    val desktopSessionDiagnostic: unit -> ViewerDesktopSessionDiagnostic
    /// Public contract function exposed by this FS.GG.UI package.
    val runtimeCapability: unit -> ViewerRuntimeCapability

    /// Public contract function exposed by this FS.GG.UI package.
    val run: program: ViewerProgram<'model, 'msg> -> Result<unit, RenderDiagnostic>
    /// Issue #444 — EVIDENCE EFFECTS ARE HONORED HERE. A host that emits `CaptureScreenshot`,
    /// `CaptureImageEvidence`, `WriteVisualEvidence` or `WriteRunEvidence` from `Init`/`Update` gets the
    /// file written. Before #444 all four were discarded by the launch loop — no file, no error, and the
    /// run still reported success — which made a green "evidence collected" verdict unfalsifiable.
    ///
    /// What lands on disk. `CaptureScreenshot`/`CaptureImageEvidence` — and `WriteRunEvidence` when its
    /// path ends in `.png`, the same rule `runBounded` applies — rasterize the CURRENT SCENE offscreen
    /// through the shared CPU painter, so the image depicts the scene the product drew, NOT the presented
    /// GL framebuffer: read it as "the product rendered this", not as "the desktop showed this".
    /// `WriteRunEvidence` to any other path writes the textual run summary, and `WriteVisualEvidence`
    /// always serializes the artifact record it carries (rasterizing would discard that payload).
    ///
    /// A write that fails does not throw and does not take the window down; it raises an
    /// `Error`/`Screenshot`/`ArtifactWrite` diagnostic naming the effect, the path and the reason, so the
    /// failure is observable on `Diagnostics` rather than silent.
    val runApp: options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runApp` (including the Issue #444 evidence-effect handling), with an explicit window behavior.
    val runAppWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #245 — as `runApp`, but every `ViewerEffect.PlayAudio` batch the host emits is handed to
    /// `audioSink` in dispatch order instead of being discarded. This is the seam from a product's pure
    /// `update` to real playback: pass `FS.GG.Audio.Host.Audio.play backend` and a scaffolded game's
    /// sound requests reach the device with no edit to the durable `Program.fs`. Additive —
    /// `runApp`/`runAppWithWindowBehavior` stay intact and keep discarding audio (FR-006), and the viewer
    /// still owns no audio device: the backend's lifetime belongs to the caller.
    val runAppWithAudio: options: ViewerOptions -> audioSink: (AudioEffect list -> unit) -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #245 — `runAppWithAudio` with an explicit window behavior, completing the pairing that
    /// `runApp`/`runAppWithWindowBehavior` already have. The generated game template uses this when a
    /// `--window-*` flag is supplied and `runAppWithAudio` otherwise.
    val runAppWithWindowBehaviorAndAudio: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> audioSink: (AudioEffect list -> unit) -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #535 — the launch that gives a product's save/load requests somewhere to GO, and an answer
    /// to come BACK on.
    ///
    /// `persistenceSink` PERFORMS the batch: it is the only thing in the process that owns a save
    /// location, because the framework deliberately does not own the `SaveSlot` -> path mapping. Every
    /// `PersistenceOutcome` it returns is handed to `mapOutcome` and, if that yields a message, dispatched
    /// into `update` exactly as a key or a tick would be — so a `Load` is finally answerable. An outcome
    /// `mapOutcome` returns `None` for is dropped: the host has still answered, and inventing a message
    /// would be the framework deciding what "no save" means to a game.
    ///
    /// THE SINK RUNS ON THE WINDOW THREAD. It is called from the effect fold, which the tick and key
    /// handlers drive, so a slow save STALLS THE FRAME for as long as the write takes. Unlike the audio sink
    /// (fire-and-forget, `-> unit`), this one must return an answer, so it cannot be made async without a
    /// different seam. Keep it fast, or hand the work to your own worker and answer later with a message.
    ///
    /// DO NOT emit a persistence effect from the handler for a persistence outcome. The dispatch is
    /// SYNCHRONOUS RECURSION, not the Elmish dispatch queue: it recurses on one stack and terminates in a
    /// StackOverflowException, which .NET cannot catch — the process dies with no diagnostic.
    ///
    /// Before this existed, no `ViewerEffect` carried a `PersistenceEffect` at all: a product could request
    /// a save and no host could ever see it. `runApp` and `runAppWithAudio` still discard `Persist` — but
    /// they now say so on the diagnostics channel rather than dropping it in silence (#416).
    val runAppWithPersistence:
        options: ViewerOptions ->
        persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        host: GeneratedAppHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>

    /// Issue #535 — sound AND saves. Without this pairing, adopting persistence would mean giving up
    /// audio, which is the kind of forced choice that gets a seam worked around instead of used.
    val runAppWithAudioAndPersistence:
        options: ViewerOptions ->
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        host: GeneratedAppHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #979 — window behavior AND sound AND saves, the last corner of the generated-app launcher
    /// matrix. `runAppWithWindowBehaviorAndAudio` (audio, no saves) and `runAppWithAudioAndPersistence`
    /// (saves, but `defaultWindowBehavior`) each drop one capability the other keeps, so a `--window-*`
    /// launch that also needs durable saves had no path and silently lost one on whichever branch it took.
    /// This threads all four — `behavior`, `audioSink`, `persistenceSink`, `mapOutcome` — through the same
    /// generated-app launch as its siblings; the audio and persistence sinks behave exactly as they do in
    /// `runAppWithAudioAndPersistence` (see there for the sink contract). Additive: the existing overloads
    /// stay intact and keep their defaults.
    val runAppWithWindowBehaviorAndAudioAndPersistence:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        audioSink: (AudioEffect list -> unit) ->
        persistenceSink: (PersistenceEffect list -> PersistenceOutcome list) ->
        mapOutcome: (PersistenceOutcome -> 'msg option) ->
        host: GeneratedAppHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Feature 085 — pointer-aware, size-aware durable launch. Routes native pointer events
    /// and window resizes to the host and renders the size-aware `View`; additive to
    /// `runApp`/`runAppWithWindowBehavior`, which stay intact (FR-004/FR-006/FR-009).
    val runInteractiveViewer: options: ViewerOptions -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// As `runInteractiveViewer` with an explicit window behavior.
    val runInteractiveViewerWithWindowBehavior: options: ViewerOptions -> behavior: ViewerWindowBehaviorRequest -> host: InteractiveViewerHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #429 — `runInteractiveViewer` with an audio sink, so the pointer/size-aware host family
    /// can request sound. Before this, audio was reachable only through `runAppWithAudio`, whose
    /// `GeneratedAppHost` has no pointer: a product that needed both got silence, because the
    /// interactive loop discarded `PlayAudio`. `audioSink` receives every batch in dispatch order,
    /// exactly as it does under `runAppWithAudio`; `runInteractiveViewer` (no sink) is unchanged.
    val runInteractiveViewerWithAudio:
        options: ViewerOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #429 — `runInteractiveViewerWithAudio` with an explicit window behavior, completing the
    /// pairing the sinkless interactive runners already have.
    val runInteractiveViewerWithWindowBehaviorAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Default retained pointer policy: latest `Moved` sample per presented-frame boundary; discrete
    /// events are lossless. The callback is `ignore`.
    val defaultPointerPacingOptions: ViewerPointerPacingOptions
    /// Enqueue using the same explicit continuous-pointer policy as the interactive live host.
    val enqueueInputWithPointerPolicy:
        policy: ViewerContinuousPointerPolicy ->
        receivedAt: DateTimeOffset ->
        inputKind: ViewerResponsivenessInputKind ->
        payload: string ->
        queue: ViewerInputQueue ->
            ViewerInputEnvelope * ViewerInputQueue
    /// `runInteractiveViewer` with an explicit continuous-pointer policy and live pacing counters.
    val runInteractiveViewerWithPointerPacing:
        options: ViewerOptions ->
        pointerPacing: ViewerPointerPacingOptions ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Pointer pacing and audio share the same interactive launch fold.
    val runInteractiveViewerWithPointerPacingAndAudio:
        options: ViewerOptions ->
        pointerPacing: ViewerPointerPacingOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Pointer pacing plus explicit window behavior on the same retained launch path.
    val runInteractiveViewerWithWindowBehaviorAndPointerPacing:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        pointerPacing: ViewerPointerPacingOptions ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Full interactive launch: explicit window behavior, pointer pacing metrics, and audio.
    val runInteractiveViewerWithWindowBehaviorAndPointerPacingAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        pointerPacing: ViewerPointerPacingOptions ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #438 — `runInteractiveViewerScript` with an audio sink. #429 gave the interactive family a
    /// sink but only on its NON-scripted entry points; the scripted runners kept passing `ignore`, so a
    /// scripted product's `PlayAudio` was still dropped with no error and no diagnostic. That mattered
    /// more than the count of entry points suggests: the scripted runners are what the evidence and
    /// responsiveness tooling drives, so "audio was requested during a scripted run" was the one thing
    /// about sound that could not be observed. `audioSink` receives every batch in dispatch order, from
    /// the same shared fold the live loops use; `runInteractiveViewerScript` (no sink) is unchanged.
    val runInteractiveViewerScriptWithAudio:
        options: ViewerOptions ->
        script: ViewerScriptInput list ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Issue #438 — `runInteractiveViewerScriptWithAudio` with an explicit window behavior, completing
    /// the pairing the sinkless scripted runners already have.
    val runInteractiveViewerScriptWithWindowBehaviorAndAudio:
        options: ViewerOptions ->
        behavior: ViewerWindowBehaviorRequest ->
        script: ViewerScriptInput list ->
        audioSink: (AudioEffect list -> unit) ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    val runInteractiveViewerScriptWithPointerPacing:
        options: ViewerOptions ->
        pointerPacing: ViewerPointerPacingOptions ->
        script: ViewerScriptInput list ->
        host: InteractiveViewerHost<'model,'msg> ->
            Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    val runAppEvidence: request: ViewerRunRequest -> options: ViewerOptions -> host: GeneratedAppHost<'model,'msg> -> Result<ViewerLaunchOutcome, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    /// Drives a real bounded Silk.NET window and reports `FramesRendered` = the number of frame
    /// callbacks the window fired. The window itself is NOT painted with `scene` (on-screen
    /// presentation is `run`/`runApp`); instead, when the request's `EvidencePath` names a `.png`
    /// the scene is rasterized to real pixels through the shared CPU painter, so image evidence
    /// genuinely depicts `scene`. Read `FramesRendered` as window/frame-cadence proof, not as
    /// "the scene was presented on screen" (P6 / R4).
    val runBounded: request: ViewerRunRequest -> options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    /// Bounded run stopping at the first frame callback; see `runBounded` for what the evidence
    /// proves (window/frame cadence; scene depicted only in `.png` evidence, not on the live surface).
    val runUntilFirstFrame: options: ViewerOptions -> scene: SceneNode -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Public contract function exposed by this FS.GG.UI package.
    /// Bounded run stopping after `frameCount` frame callbacks; see `runBounded` for what the
    /// evidence proves (window/frame cadence; scene depicted only in `.png` evidence).
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
    /// Issue #245 — every sound request in an effect batch, flattened in dispatch order; non-audio
    /// effects are dropped. This is exactly what `runAppWithAudio` feeds its sink, exposed as a pure
    /// function so a product can assert what a frame requested without opening a window or a device:
    /// `dispatchKey host raw model |> snd |> audioRequests |> Audio.interpret` yields `AudioEvidence`.
    val audioRequests: effects: ViewerEffect list -> AudioEffect list
    /// Public contract function exposed by this FS.GG.UI package.
    val smoke: host: GeneratedAppHost<'model,'msg> -> request: ViewerRunRequest -> Result<ViewerRunEvidence, ViewerRunFailure>
    /// Issue #911: the scene-host analogue of `ControlsElmish.Perf.runScriptToModel` (#461). Fold an
    /// ordered key-event script through the host's own `dispatchKey` (`normalizeEvent -> MapKey ->
    /// Update`) from `Init`'s model to the FINAL model, returning it with every `ViewerEffect` requested
    /// in order (`Init`'s batch first). No window, no device, no GL — it lets a `game`-family product,
    /// which routes input through this scene-host rather than the Controls click path, write an
    /// end-to-end "played through the host" test purely and headlessly. It drives the KEY source only;
    /// `Tick` is folded by the caller, or checked via `reachableMessages`.
    val runKeyScriptToModel: host: GeneratedAppHost<'model,'msg> -> script: ViewerKeyEvent list -> 'model * ViewerEffect list
    /// Issue #911: the scene-host analogue of `ControlRenderResult.BoundIds` — "a key that is bound is a
    /// key that dispatches". Partition a declared input surface into the events that route through
    /// `MapKey` to a product message (`Wired`) and the DEAD ones that route to none (`Dead`). A non-empty
    /// `Dead` over a product's declared keymap is a bound key that dispatches nothing. Reflection-free,
    /// total, pure — consults only `MapKey` (via `normalizeEvent`, as the live runtime does), never `Update`.
    val auditKeyWiring: host: GeneratedAppHost<'model,'msg> -> probe: ViewerKeyEvent list -> SceneHostKeyWiring<'msg>
    /// Issue #911: every product message the host's runtime SOURCES (`MapKey` over `probe`, and `Tick`
    /// when `tickSample` is `Some dt`) can produce, in probe order — the raw material for the stronger
    /// "handled-but-unwired" check. A `Msg` case handled in `Update` but absent from this list is
    /// dispatched by no source (the Rougue1 defect). This package uses no reflection, so it returns what
    /// IS reachable — as `BoundIds` returns a set the product checks against — and the product asserts its
    /// handled `Msg` universe is covered.
    val reachableMessages: host: GeneratedAppHost<'model,'msg> -> probe: ViewerKeyEvent list -> tickSample: TimeSpan option -> 'msg list
