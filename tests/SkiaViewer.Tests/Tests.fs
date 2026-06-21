module SkiaViewerCapabilityTests

open System
open System.Collections.Generic
open Expecto
open FS.GG.UI.KeyboardInput
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

type HostModel =
    { Count: int
      Closed: bool }

type HostMsg =
    | Increment
    | Close

let livePersistentTestsEnabled () =
    String.Equals(Environment.GetEnvironmentVariable "FS_SKIA_RUN_LIVE_PERSISTENT_TESTS", "1", StringComparison.Ordinal)

let environmentOverrideLock = obj()

let withEnvironment variables action =
    lock environmentOverrideLock (fun () ->
        let previous =
            variables
            |> List.map (fun (name, _) -> name, Environment.GetEnvironmentVariable name)

        try
            for name, value in variables do
                Environment.SetEnvironmentVariable(name, value)

            action()
        finally
            for name, value in previous do
                Environment.SetEnvironmentVariable(name, value))

let isPngFile path =
    if not (IO.File.Exists path) then
        false
    else
        let signature = IO.File.ReadAllBytes(path) |> Array.truncate 8
        signature = [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

[<Tests>]
let tests =
    testList "SkiaViewer MVU contract" [
        test "init emits window-open effect" {
            let model, effects = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            Expect.isFalse model.IsRunning "viewer starts stopped"
            Expect.exists effects (function OpenWindow("Product", { Width = 640; Height = 480 }) -> true | _ -> false) "init emits open effect"
        }

        test "render updates model and emits render effect" {
            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let scene = Group []
            let next, effects = Viewer.update (Render scene) model
            Expect.equal next.LastScene (Some scene) "last scene is stored"
            Expect.exists effects (function RenderScene rendered when rendered = scene -> true | _ -> false) "render effect is emitted"
        }

        test "screenshot evidence reports explicit unsupported capture without claiming proof" {
            let outputPath = IO.Path.Combine(IO.Path.GetTempPath(), $"skia-viewer-screenshot-{Guid.NewGuid():N}.md")
            let result =
                Viewer.captureScreenshotEvidence
                    { Command = "dotnet run -- --screenshot-evidence readiness/screenshot-evidence.md"
                      AppOrSample = "skia-viewer-test"
                      OutputPath = outputPath
                      Width = 320
                      Height = 200
                      RendererMode = "skia"
                      CaptureMode = ViewerRenderTargetPng
                      HostFacts = [ "host=test" ]
                      Timeout = TimeSpan.FromSeconds 2.0 }
                    { Title = "Product"; InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
                    (Rectangle((0.0, 0.0, 16.0, 16.0), Colors.white))

            Expect.equal result.Status ScreenshotOk "viewer render-target capture is accepted as screenshot proof"
            Expect.equal result.EvidenceKind "screenshot" "result names screenshot evidence kind"
            Expect.isSome result.ScreenshotPath "accepted result claims a screenshot artifact"
            Expect.equal result.ViewerOpenStatus ViewerOpenConfirmed "viewer open status is explicit"
            Expect.equal result.FirstFrameStatus FirstFramePresentedStatus "first-frame status is explicit"
            Expect.equal result.CaptureSource LiveViewerWindow "capture source is live viewer render target"
            Expect.isTrue result.ProvesScreenshot "live viewer render target proves screenshot"
            Expect.equal result.PixelContentValidation PixelContentNonBlank "accepted screenshot is non-blank"
            Expect.isNone result.Fallback "accepted screenshot does not use deterministic fallback"
            Expect.isNone result.UnsupportedHostReason "accepted screenshot has no unsupported-host reason"
        }

        test "evidence workflow init and update emit pure screenshot effects and success fields" {
            let request =
                { Command = "dotnet run -- --screenshot-evidence readiness/screenshot-evidence.md"
                  AppOrSample = "skia-viewer-test"
                  OutputPath = "readiness/screenshot-evidence.md"
                  Width = 320
                  Height = 200
                  RendererMode = "skia"
                  CaptureMode = ViewerRenderTargetPng
                  HostFacts = [ "host=test" ]
                  Timeout = TimeSpan.FromSeconds 2.0 }

            let model, effects = Viewer.initEvidenceWorkflow request
            Expect.equal model.ViewerOpenStatus ViewerOpenUnknown "workflow starts before launch facts are known"
            Expect.exists effects (function LaunchViewerForEvidence launched when launched = request -> true | _ -> false) "workflow requests viewer launch at interpreter edge"

            let launched, launchEffects = Viewer.updateEvidenceWorkflow (LaunchCompleted ViewerOpenConfirmed) model
            Expect.equal launched.ViewerOpenStatus ViewerOpenConfirmed "launch completion updates model"
            Expect.isEmpty launchEffects "launch fact alone emits no filesystem or capture work"

            let firstFrame, captureEffects = Viewer.updateEvidenceWorkflow (FirstFrameObserved FirstFramePresentedStatus) launched
            Expect.equal firstFrame.FirstFrameStatus FirstFramePresentedStatus "first-frame fact is preserved"
            Expect.exists captureEffects (function CaptureViewerScreenshot "readiness/screenshot-evidence.md" -> true | _ -> false) "first frame requests screenshot capture"

            let completed, writeEffects =
                Viewer.updateEvidenceWorkflow
                    (CaptureSucceeded("readiness/artifacts/screenshot.png", 320, 200, LiveViewerWindow))
                    firstFrame

            match completed.Result with
            | Some result ->
                Expect.equal result.Status ScreenshotOk "successful workflow result is ok"
                Expect.equal result.EvidenceKind "screenshot" "successful workflow result is screenshot evidence"
                Expect.equal result.ScreenshotPath (Some "readiness/artifacts/screenshot.png") "successful result has PNG artifact path"
                Expect.equal result.Width (Some 320) "successful result has positive width"
                Expect.equal result.Height (Some 200) "successful result has positive height"
                Expect.equal result.AppOrSample "skia-viewer-test" "successful result preserves app/sample identity"
                Expect.equal result.CaptureMode ViewerRenderTargetPng "successful result preserves capture mode"
                Expect.equal result.HostFacts [ "host=test" ] "successful result preserves host facts"
                Expect.equal result.PixelContentValidation PixelContentNonBlank "successful result records non-blank pixel validation"
                Expect.equal result.ViewerOpenStatus ViewerOpenConfirmed "successful result preserves viewer-open fact"
                Expect.equal result.FirstFrameStatus FirstFramePresentedStatus "successful result preserves first-frame fact"
                Expect.equal result.CaptureAvailability CaptureAvailable "successful result records capture availability"
                Expect.equal result.CaptureSource LiveViewerWindow "successful result records live-window source"
                Expect.isTrue result.ProvesScreenshot "live-window source proves screenshot evidence"
                Expect.exists writeEffects (function WriteScreenshotEvidenceReport written when written = result -> true | _ -> false) "successful result is handed to report writer effect"
            | None -> failtest "expected screenshot workflow result"
        }

        test "evidence workflow unsupported result separates viewer and capture capability facts" {
            let request =
                { Command = "--screenshot-evidence"
                  AppOrSample = "skia-viewer-test"
                  OutputPath = "readiness/screenshot-evidence.md"
                  Width = 320
                  Height = 200
                  RendererMode = "skia"
                  CaptureMode = ViewerRenderTargetPng
                  HostFacts = [ "host=test" ]
                  Timeout = TimeSpan.FromSeconds 2.0 }

            let model, _ = Viewer.initEvidenceWorkflow request
            let launched, _ = Viewer.updateEvidenceWorkflow (LaunchCompleted ViewerOpenConfirmed) model
            let firstFrame, _ = Viewer.updateEvidenceWorkflow (FirstFrameObserved FirstFramePresentedStatus) launched
            let completed, effects =
                Viewer.updateEvidenceWorkflow
                    (CaptureUnsupported("viewer host does not expose screenshot capture", Some "deterministic-scene-evidence"))
                    firstFrame

            match completed.Result with
            | Some result ->
                Expect.equal result.Status ScreenshotUnsupported "unsupported capture stays unsupported"
                Expect.equal result.ViewerOpenStatus ViewerOpenConfirmed "viewer open fact is not collapsed into capture support"
                Expect.equal result.FirstFrameStatus FirstFramePresentedStatus "first-frame fact is retained"
                Expect.equal result.CaptureAvailability (CaptureUnavailable "viewer host does not expose screenshot capture") "capture availability has reason"
                Expect.equal result.CaptureSource DeterministicSceneRender "fallback source is deterministic render"
                Expect.equal result.DeterministicFallbackKind (Some "deterministic-scene-evidence") "fallback kind is explicit"
                Expect.isFalse result.ProvesScreenshot "unsupported fallback does not prove screenshot"
                Expect.exists effects (function WriteScreenshotEvidenceReport written when written = result -> true | _ -> false) "unsupported result is still reportable"
            | None -> failtest "expected unsupported screenshot workflow result"
        }

        test "viewer boundary guardrails preserve screenshot visual capability and window diagnostics" {
            let scene = Rectangle((0.0, 0.0, 24.0, 24.0), Colors.white)
            let options = { Title = "Product"; InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let outputPath = IO.Path.Combine(IO.Path.GetTempPath(), $"skia-viewer-screenshot-{Guid.NewGuid():N}.md")

            let capability = Viewer.runtimeCapability()
            Expect.isTrue capability.BoundedSmoke "bounded smoke capability stays available for evidence runs"
            Expect.isTrue capability.KeyboardInput "keyboard input capability remains part of the viewer contract"
            Expect.equal capability.RendererMode "skia" "renderer mode is reported as a host capability"

            let screenshot =
                Viewer.captureScreenshotEvidence
                    { Command = "dotnet run -- --screenshot-evidence readiness/screenshot-evidence.md"
                      AppOrSample = "skia-viewer-test"
                      OutputPath = outputPath
                      Width = 320
                      Height = 200
                      RendererMode = "skia"
                      CaptureMode = ViewerRenderTargetPng
                      HostFacts = [ "host=test" ]
                      Timeout = TimeSpan.FromSeconds 2.0 }
                    options
                    scene

            Expect.equal screenshot.Status ScreenshotOk "screenshot capture writes a real PNG artifact"
            Expect.isNone screenshot.Fallback "accepted screenshots do not use deterministic fallback"
            Expect.isSome screenshot.ScreenshotPath "accepted screenshots claim an image path"
            Expect.contains screenshot.Diagnostics "status=ok" "accepted screenshot diagnostics keep stable status field"
            Expect.exists screenshot.Diagnostics (fun item -> item.StartsWith("scene-capabilities=", StringComparison.Ordinal)) "unsupported screenshot diagnostics include scene capability facts"

            let windowResults =
                Viewer.validateWindowLaunchBehavior
                    { Width = 0; Height = 200 }
                    { Viewer.defaultWindowBehavior with
                        StartupState = ViewerWindowStartupState.Fullscreen
                        BackendPreference = Some ViewerBackendPreference.Software }

            Expect.exists windowResults (fun item -> item.Option = "initial-size" && item.Status = FailedOption && item.Message.Contains "positive") "window launch diagnostics keep positive-size validation"
            Expect.exists windowResults (fun item -> item.Option = "startup-state" && item.Status = Honored && item.Message.Contains "Fullscreen") "fullscreen startup is now honored, not host-unsupported"
            Expect.exists windowResults (fun item -> item.Option = "backend" && item.Status = UnsupportedOption && item.Message.Contains "not supported") "unsupported backend preferences remain explicit diagnostics"
        }

        test "bounded run init and update expose pure lifecycle effects" {
            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "vulkan"
                  EvidencePath = Some "readiness/logs/viewer-smoke.json" }

            let model, effects = Viewer.initRun request
            Expect.equal model.FramesRendered 0 "bounded run starts with no frames"
            Expect.exists effects (function OpenBoundedWindow opened when opened.RendererMode = request.RendererMode -> true | _ -> false) "bounded run requests window opening at interpreter edge"

            let size = { Width = 320; Height = 200 }
            let afterFrame, frameEffects = Viewer.updateRun (RecordFrame size) model
            Expect.equal afterFrame.FramesRendered 1 "recorded frame increments count"
            Expect.exists frameEffects (function StopBoundedRun -> true | _ -> false) "first-frame target stops when evidence is collected"
        }

        test "bounded run records first-frame evidence with positive dimensions elapsed time and summary" {
            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "vulkan"
                  EvidencePath = Some "readiness/logs/viewer-smoke.txt" }

            let diagnostic =
                { Level = Info
                  Category = Frame
                  Message = "frame 1 presented"
                  FrameIndex = Some 1
                  Stage = None
                  Elapsed = Some(TimeSpan.FromMilliseconds 16.0) }

            let model, _ = Viewer.initRun request
            let started, startEffects = Viewer.updateRun (RunStarted(DateTimeOffset.UnixEpoch)) model
            Expect.equal started.StartedAt (Some DateTimeOffset.UnixEpoch) "start time is supplied by interpreter message"
            Expect.exists startEffects (function RequestFrame -> true | _ -> false) "started run requests the first frame"

            let withDiagnostic, _ = Viewer.updateRun (RecordDiagnostic diagnostic) started
            let completed, effects = Viewer.updateRun (RecordFrame { Width = 320; Height = 200 }) withDiagnostic

            match completed.Completed with
            | Some(Ok evidence) ->
                Expect.equal evidence.FramesRendered 1 "first-frame target captures one frame"
                Expect.isGreaterThan evidence.Elapsed TimeSpan.Zero "elapsed time is positive"
                Expect.equal evidence.InitialOutputSize { Width = 320; Height = 200 } "output size is captured"
                Expect.equal evidence.RendererMode "vulkan" "renderer mode is preserved"
                Expect.equal evidence.LastDiagnosticSummary (Some "frame 1 presented") "last diagnostic summary is preserved"
                Expect.equal evidence.EvidencePath request.EvidencePath "evidence path is preserved"
            | other -> failtestf "expected first-frame evidence, got %A" other

            Expect.exists effects (function StopBoundedRun -> true | _ -> false) "bounded run stops after the target"
        }

        test "bounded run validates positive frame counts timeouts and durations" {
            let options = { Title = "Product"; InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let scene = Group []

            let invalidFrameRequest =
                { Target = FrameCount 0
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "vulkan"
                  EvidencePath = None }

            let invalidTimeoutRequest =
                { invalidFrameRequest with
                    Target = FirstFrame
                    Timeout = TimeSpan.Zero }

            let invalidDurationRequest =
                { invalidFrameRequest with
                    Target = Duration TimeSpan.Zero
                    Timeout = TimeSpan.FromSeconds 2.0 }

            [ invalidFrameRequest, "frame count"
              invalidTimeoutRequest, "timeout"
              invalidDurationRequest, "duration" ]
            |> List.iter (fun (request, expected) ->
                match Viewer.runBounded request options scene with
                | Result.Error failure ->
                    Expect.equal failure.Classification ProductDefect $"{expected} validation is a product defect"
                    Expect.equal failure.BlockedStage App $"{expected} validation is blocked by app/request configuration"
                    Expect.stringContains failure.Message "positive" $"{expected} failure is actionable"
                | Result.Ok evidence -> failtestf "expected %s validation failure, got %A" expected evidence)
        }

        test "bounded run frame-count target stops after the exact positive frame count" {
            let request =
                { Target = FrameCount 3
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "vulkan"
                  EvidencePath = None }

            let model, _ = Viewer.initRun request

            let finalModel =
                [ 1..3 ]
                |> List.fold
                    (fun current frame ->
                        let diagnostic =
                            { Level = Info
                              Category = Frame
                              Message = $"frame {frame} presented"
                              FrameIndex = Some frame
                              Stage = None
                              Elapsed = Some(TimeSpan.FromMilliseconds(float frame * 16.0)) }

                        let withDiagnostic, _ = Viewer.updateRun (RecordDiagnostic diagnostic) current
                        Viewer.updateRun (RecordFrame { Width = 320; Height = 200 }) withDiagnostic |> fst)
                    model

            match finalModel.Completed with
            | Some(Ok evidence) ->
                Expect.equal evidence.FramesRendered 3 "exact frame target is captured"
                Expect.equal evidence.LastDiagnosticSummary (Some "frame 3 presented") "last frame diagnostic is summarized"
            | other -> failtestf "expected frame-count evidence, got %A" other
        }

        test "forced pre-frame failures classify blocked stages and unsupported host capabilities" {
            let cases: (ViewerRunBlockedStage * ViewerRunFailureClassification * ViewerDiagnosticCategory) list =
                [ DesktopPrerequisite, UnsupportedEnvironment, EnvironmentSession
                  ProcessLaunch, UnsupportedEnvironment, Startup
                  WindowCreation, UnsupportedEnvironment, Startup
                  FirstFrameRender, UnsupportedEnvironment, Frame
                  Observation, UnsupportedEnvironment, Startup
                  Capture, UnsupportedEnvironment, Screenshot
                  InputVerification, UnsupportedEnvironment, Input
                  ControlledExit, UnsupportedEnvironment, Startup
                  ArtifactWrite, UnsupportedEnvironment, Startup
                  Window, UnsupportedEnvironment, Startup
                  Surface, UnsupportedEnvironment, Startup
                  ViewerRunBlockedStage.Renderer, UnsupportedEnvironment, ViewerDiagnosticCategory.Renderer
                  ViewerRunBlockedStage.GlContext, UnsupportedEnvironment, ViewerDiagnosticCategory.Framebuffer
                  Readback, UnsupportedEnvironment, Screenshot
                  ViewerRunBlockedStage.Scene, ProductDefect, ViewerDiagnosticCategory.Scene
                  App, ProductDefect, Startup
                  Timeout, ProductDefect, Startup
                  Unknown, ProductDefect, Startup ]

            cases
            |> List.iter (fun (stage, classification, category) ->
                let diagnostic =
                    { Level = ViewerDiagnosticLevel.Error
                      Category = category
                      Message = $"blocked at {stage}"
                      FrameIndex = None
                      Stage = Some stage
                      Elapsed = Some TimeSpan.Zero }

                let failure = Viewer.failureFromDiagnostic diagnostic
                Expect.equal failure.BlockedStage stage $"{stage} blocked stage is preserved"
                Expect.equal failure.Classification classification $"{stage} classification is preserved"
                Expect.equal failure.DiagnosticCategory category $"{stage} diagnostic category is preserved"
                Expect.equal failure.LastDiagnosticSummary (Some diagnostic.Message) $"{stage} keeps summary")
        }

        test "external observation failure preserves viewer-owned launch facts instead of headless classification" {
            let outcome =
                { Status = "ok"
                  Mode = "interactive-window"
                  Command = Some "dotnet run -- --launch-evidence readiness/persistent-launch-evidence.md"
                  RendererMode = "skia"
                  WindowOpened = true
                  WindowVisible = ViewerObservedValue.Observed true
                  FirstFramePresented = true
                  CloseReason = Some EvidenceRequestedClose
                  UserCloseObserved = false
                  AppCloseObserved = false
                  EvidenceCloseObserved = true
                  SelfClosedForEvidence = true
                  InputDispatch = "not-required"
                  ExitPath = true
                  WindowDiagnostics = []
                  OptionResults = []
                  VisualEvidence = []
                  FailureClass = None
                  BlockedStage = None
                  Classification = None
                  Category = None
                  Message = "viewer-owned facts passed" }

            let observation =
                Viewer.classifyWindowObservation
                    outcome
                    { ExternalObservationAttempted = true
                      ExternalWindowMatched = Some false
                      CaptureAttempted = false
                      CaptureSucceeded = None }

            Expect.equal observation.ViewerWindowOpened true "viewer-owned window-opened fact is preserved"
            Expect.equal observation.ViewerFirstFramePresented true "viewer-owned first-frame fact is preserved"
            Expect.equal observation.DiagnosticSource "real-launch" "observation diagnostics name real launch as source"
            Expect.equal observation.Command outcome.Command "observation diagnostics preserve command"
            Expect.contains observation.HostFacts "mode=interactive-window" "host facts include mode"
            Expect.contains observation.ViewerFacts "window-opened=True" "viewer facts include window-opened"
            Expect.equal observation.BlockedStage (Some Observation) "external title/window miss is observation-blocked"
            Expect.notEqual observation.BlockedStage (Some DesktopPrerequisite) "external observation miss is not a desktop-prerequisite/headless classification"
            Expect.isFalse (observation.Message.Contains("headless-only", StringComparison.OrdinalIgnoreCase)) "message does not claim headless-only"
            Expect.contains observation.MissingFacts "external-window-match" "missing external fact is named"
        }

        test "window observation diagnostics include capture facts and missing fact names" {
            let outcome =
                { Status = "ok"
                  Mode = "interactive-window"
                  Command = Some "dotnet run -- --launch-evidence readiness/persistent-launch-evidence.md"
                  RendererMode = "skia"
                  WindowOpened = true
                  WindowVisible = ViewerObservedValue.Observed true
                  FirstFramePresented = true
                  CloseReason = Some EvidenceRequestedClose
                  UserCloseObserved = false
                  AppCloseObserved = false
                  EvidenceCloseObserved = true
                  SelfClosedForEvidence = true
                  InputDispatch = "not-required"
                  ExitPath = true
                  WindowDiagnostics = []
                  OptionResults = []
                  VisualEvidence = []
                  FailureClass = None
                  BlockedStage = None
                  Classification = None
                  Category = None
                  Message = "viewer-owned facts passed" }

            let observation =
                Viewer.classifyWindowObservation
                    outcome
                    { ExternalObservationAttempted = false
                      ExternalWindowMatched = None
                      CaptureAttempted = true
                      CaptureSucceeded = Some false }

            Expect.equal observation.BlockedStage (Some Capture) "capture failure is capture-blocked when viewer facts pass"
            Expect.equal observation.Classification (Some UnsupportedEnvironment) "capture failure is a host observation limitation"
            Expect.equal observation.CaptureAttempted true "capture attempt is recorded"
            Expect.equal observation.CaptureSucceeded (Some false) "capture failure fact is recorded"
            Expect.contains observation.MissingFacts "capture-succeeded" "missing capture fact is named"
            Expect.contains observation.ViewerFacts "first-frame-presented=True" "viewer facts include first frame"
        }

        test "bounded run timeout uses last diagnostic summary and stops without shell timeout" {
            let request =
                { Target = Duration(TimeSpan.FromSeconds 10.0)
                  Timeout = TimeSpan.FromMilliseconds 1.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "vulkan"
                  EvidencePath = None }

            let diagnostic =
                { Level = Warning
                  Category = Startup
                  Message = "waiting for first frame"
                  FrameIndex = None
                  Stage = Some Timeout
                  Elapsed = Some(TimeSpan.FromMilliseconds 1.0) }

            let model, _ = Viewer.initRun request
            let withDiagnostic, _ = Viewer.updateRun (RecordDiagnostic diagnostic) model
            let timedOut, effects = Viewer.updateRun TimeoutRun withDiagnostic

            match timedOut.Completed with
            | Some(Result.Error failure) ->
                Expect.equal failure.BlockedStage Timeout "timeout stage is explicit"
                Expect.equal failure.Classification ProductDefect "timeout is product-defect classification"
                Expect.equal failure.LastDiagnosticSummary (Some "waiting for first frame") "last diagnostic summary is retained"
            | other -> failtestf "expected timeout failure, got %A" other

            Expect.exists effects (function StopBoundedRun -> true | _ -> false) "timeout stops the bounded run internally"
        }

        test "diagnostics and viewer key events flow through public update effects" {
            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let diagnostic =
                { Level = Info
                  Category = Startup
                  Message = "window-created"
                  FrameIndex = None
                  Stage = Some Window
                  Elapsed = Some TimeSpan.Zero }

            let _, diagnosticEffects = Viewer.update (DiagnosticCaptured diagnostic) model
            Expect.exists diagnosticEffects (function EmitDiagnostic emitted when emitted.Message = diagnostic.Message -> true | _ -> false) "diagnostic capture is emitted to the edge"

            let _, keyEffects =
                Viewer.update
                    (KeyEvent { RawKey = "Enter"; Direction = ViewerKeyDirection.KeyDown })
                    model

            Expect.exists keyEffects (function DispatchInput(Enter, true) -> true | _ -> false) "viewer key events dispatch normalized input"
        }

        test "interactive lifecycle update remains pure and emits interpreter effects" {
            let model, initEffects = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

            Expect.equal model.LifecycleState NotStarted "init starts before native work"
            Expect.equal model.FirstFramePresented false "init has no frame evidence"
            Expect.equal model.UserCloseObserved false "init has no close evidence"
            Expect.exists initEffects (function OpenWindow("Product", { Width = 640; Height = 480 }) -> true | _ -> false) "init requests window opening at the edge"

            let started, startEffects = Viewer.update StartInteractive model
            Expect.equal started.LifecycleState CheckingDesktopSession "interactive start enters desktop-session precheck state"
            Expect.exists startEffects (function CheckDesktopSession -> true | _ -> false) "interactive start requests desktop preflight as an effect"

            let framed, frameEffects = Viewer.update (FramePresented { Width = 640; Height = 480 }) started
            Expect.equal framed.LifecycleState ViewerLifecycleState.FirstFramePresented "first frame is modeled without completing launch"
            Expect.equal framed.FirstFramePresented true "first-frame flag is retained"
            Expect.exists frameEffects (function EmitDiagnostic diagnostic when diagnostic.Category = Frame -> true | _ -> false) "frame presentation emits diagnostic effect"

            let keyed, keyEffects =
                Viewer.update
                    (KeyEvent { RawKey = "Space"; Direction = ViewerKeyDirection.KeyDown })
                    framed

            Expect.equal keyed.InputDispatch Verified "keyboard dispatch updates public input status"
            Expect.exists keyEffects (function DispatchInput(Space, true) -> true | _ -> false) "keyboard dispatch stays at interpreter boundary"

            let closing, closeEffects = Viewer.update ViewerMsg.UserCloseObserved keyed
            Expect.equal closing.UserCloseObserved true "user close is recorded in the model"
            Expect.equal closing.LifecycleState UserCloseObservedState "user close remains distinct from framework or evidence close"
            Expect.exists closeEffects (function CloseWindow -> true | _ -> false) "user close requests window close"
        }

        test "runApp interactive first-frame lifecycle stays open until explicit close for 30 second hold" {
            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let running, startEffects = Viewer.update StartInteractive model
            let firstFrame, frameEffects = Viewer.update (FramePresented { Width = 640; Height = 480 }) running

            Expect.equal firstFrame.LifecycleState ViewerLifecycleState.FirstFramePresented "first frame is a lifecycle milestone, not completion"
            Expect.isTrue firstFrame.IsRunning "interactive lifecycle remains running after first frame"
            Expect.isFalse firstFrame.UserCloseObserved "first frame does not synthesize user close"
            Expect.isFalse (frameEffects |> List.exists (function CloseWindow -> true | _ -> false)) "first frame must not emit close"
            Expect.exists startEffects (function CheckDesktopSession -> true | _ -> false) "interactive launch still runs desktop precheck at the edge"

            let closed, closeEffects = Viewer.update ViewerMsg.UserCloseObserved firstFrame
            Expect.equal closed.LifecycleState UserCloseObservedState "explicit user close is the completion path"
            Expect.isTrue closed.UserCloseObserved "explicit close is recorded"
            Expect.exists closeEffects (function CloseWindow -> true | _ -> false) "explicit close emits close effect"
        }

        test "interactive launch close reasons complete only after explicit user app host or failure close" {
            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let running, _ = Viewer.update StartInteractive model
            let firstFrame, frameEffects = Viewer.update (FramePresented { Width = 640; Height = 480 }) running

            Expect.equal firstFrame.LifecycleState ViewerLifecycleState.FirstFramePresented "first frame does not complete the interactive launch"
            Expect.isTrue firstFrame.IsRunning "interactive launch remains open after first frame"
            Expect.isFalse firstFrame.UserCloseObserved "first frame is not reported as user close"
            Expect.isFalse (frameEffects |> List.exists (function CloseWindow -> true | _ -> false)) "first frame does not request evidence close"

            let userClosed, userEffects = Viewer.update ViewerMsg.UserCloseObserved firstFrame
            Expect.equal userClosed.LifecycleState UserCloseObservedState "user close completes through user-close state"
            Expect.isTrue userClosed.UserCloseObserved "user close is the only user-close-observed path"
            Expect.exists userEffects (function CloseWindow -> true | _ -> false) "user close closes the window"

            let appClosed, appEffects = Viewer.update AppCloseRequested firstFrame
            Expect.equal appClosed.LifecycleState CloseRequested "app close completes through app close-requested state"
            Expect.isFalse appClosed.UserCloseObserved "app close is not user close"
            Expect.exists appEffects (function CloseWindow -> true | _ -> false) "app close closes the window"

            let hostClosed, hostEffects = Viewer.update HostCloseObserved firstFrame
            Expect.equal hostClosed.LifecycleState Closing "host close completes through host closing state"
            Expect.isFalse hostClosed.UserCloseObserved "host close is not user close"
            Expect.exists hostEffects (function CloseWindow -> true | _ -> false) "host close closes the window"

            let failure =
                { BlockedStage = Window
                  Classification = UnsupportedEnvironment
                  DiagnosticCategory = EnvironmentSession
                  Message = "window host failed"
                  LastDiagnosticSummary = Some "window host failed" }

            let failed, failureEffects = Viewer.update (RunFailed failure) firstFrame
            Expect.equal failed.LifecycleState Failed "failure close completes through failed state"
            Expect.isFalse failed.UserCloseObserved "failure close is not user close"
            Expect.exists failureEffects (function EmitDiagnostic diagnostic when diagnostic.Message = "window host failed" -> true | _ -> false) "failure emits diagnostics instead of evidence close"
        }

        test "visibility diagnostics classify taskbar-only inaccessible windows before app lifecycle debugging" {
            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

            let created =
                { WindowInitialized = true
                  NativeHandle = ViewerObservedValue.Observed true
                  Visible = ViewerObservedValue.Unavailable
                  Focusable = ViewerObservedValue.Unavailable
                  Focused = ViewerObservedValue.Unavailable
                  Minimized = ViewerObservedValue.Unavailable
                  Maximized = ViewerObservedValue.Unavailable
                  ClientSize = Some "640x480"
                  RenderableSurfaceAvailable = ViewerObservedValue.Unavailable
                  Backend = Some "skia"
                  InputDevicesAvailable = ViewerObservedValue.Unavailable
                  FailureClass = None
                  Message = "native handle created" }

            let windowCreated, createdEffects = Viewer.update (WindowCreated created) model
            Expect.equal windowCreated.LifecycleState ViewerLifecycleState.WindowCreated "window creation is modeled before visibility success"
            Expect.exists createdEffects (function QueryNativeWindowState -> true | _ -> false) "window creation asks the interpreter for native state"

            let taskbarOnly =
                { created with
                    Visible = ViewerObservedValue.Observed false
                    Focusable = ViewerObservedValue.Observed false
                    RenderableSurfaceAvailable = ViewerObservedValue.Observed true
                    FailureClass = Some "window-visibility"
                    Message = "window has taskbar entry but no accessible visible surface" }

            let inaccessible, diagnosticEffects = Viewer.update (VisibilityObserved taskbarOnly) windowCreated
            Expect.equal inaccessible.LifecycleState InaccessibleWindow "taskbar-only state is degraded before app lifecycle debugging"
            Expect.exists diagnosticEffects (function EmitDiagnostic diagnostic when diagnostic.Message.Contains "taskbar entry" -> true | _ -> false) "visibility diagnostic is emitted"
        }

        test "visibility diagnostics classify inaccessible observed window states as degraded or failed" {
            let visibleBase =
                { WindowInitialized = true
                  NativeHandle = ViewerObservedValue.Observed true
                  Visible = ViewerObservedValue.Observed true
                  Focusable = ViewerObservedValue.Observed true
                  Focused = ViewerObservedValue.Unsupported
                  Minimized = ViewerObservedValue.Observed false
                  Maximized = ViewerObservedValue.Observed false
                  ClientSize = Some "640x480"
                  RenderableSurfaceAvailable = ViewerObservedValue.Observed true
                  Backend = Some "skia"
                  InputDevicesAvailable = ViewerObservedValue.Observed true
                  FailureClass = None
                  Message = "visible window surface observed" }

            let cases =
                [ "taskbar-only",
                  { visibleBase with
                      Visible = ViewerObservedValue.Observed false
                      Focusable = ViewerObservedValue.Observed false
                      FailureClass = Some "window-visibility"
                      Message = "taskbar-only window has no accessible visible surface" }
                  "hidden",
                  { visibleBase with
                      Visible = ViewerObservedValue.Observed false
                      FailureClass = Some "window-visibility"
                      Message = "hidden window is not a visible launch" }
                  "minimized-only",
                  { visibleBase with
                      Minimized = ViewerObservedValue.Observed true
                      FailureClass = Some "window-visibility"
                      Message = "minimized-only window is not accessible" }
                  "off-screen",
                  { visibleBase with
                      Focusable = ViewerObservedValue.Observed false
                      FailureClass = Some "window-visibility"
                      Message = "off-screen window cannot be selected" }
                  "unmapped",
                  { visibleBase with
                      WindowInitialized = false
                      NativeHandle = ViewerObservedValue.Observed false
                      Visible = ViewerObservedValue.Observed false
                      FailureClass = Some "window-visibility"
                      Message = "unmapped window never became visible" }
                  "zero-sized",
                  { visibleBase with
                      ClientSize = Some "0x0"
                      FailureClass = Some "window-visibility"
                      Message = "zero-sized window has no usable client surface" }
                  "surface-less",
                  { visibleBase with
                      RenderableSurfaceAvailable = ViewerObservedValue.Observed false
                      FailureClass = Some "window-visibility"
                      Message = "surface-less window cannot present frames" } ]

            for label, diagnostic in cases do
                let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
                let checking, _ = Viewer.update (VisibilityCheckStarted diagnostic) model
                let next, effects = Viewer.update (VisibilityObserved diagnostic) checking
                Expect.equal next.LifecycleState InaccessibleWindow $"{label} must not be reported as visible interactive success"
                Expect.exists effects (function EmitDiagnostic event when event.Message.Contains diagnostic.Message -> true | _ -> false) $"{label} emits the native visibility diagnostic"
        }

        test "visibility diagnostics classify unsupported or unavailable native state separately from visible success" {
            let baseDiagnostic =
                { WindowInitialized = true
                  NativeHandle = ViewerObservedValue.Unsupported
                  Visible = ViewerObservedValue.Unsupported
                  Focusable = ViewerObservedValue.Unsupported
                  Focused = ViewerObservedValue.Unsupported
                  Minimized = ViewerObservedValue.Unsupported
                  Maximized = ViewerObservedValue.Unsupported
                  ClientSize = None
                  RenderableSurfaceAvailable = ViewerObservedValue.Unsupported
                  Backend = None
                  InputDevicesAvailable = ViewerObservedValue.Unsupported
                  FailureClass = Some "environment-session"
                  Message = "native window-state fields are unsupported on this host" }

            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let unsupported, unsupportedEffects = Viewer.update (VisibilityObserved baseDiagnostic) model
            Expect.equal unsupported.LifecycleState Unsupported "unsupported native visibility facts are not visible launch success"
            Expect.exists unsupportedEffects (function EmitDiagnostic event when event.Message.Contains "unsupported" -> true | _ -> false) "unsupported state emits a diagnostic"

            let unavailable =
                { baseDiagnostic with
                    NativeHandle = ViewerObservedValue.Unavailable
                    Visible = ViewerObservedValue.Unavailable
                    RenderableSurfaceAvailable = ViewerObservedValue.Unavailable
                    FailureClass = Some "window-visibility"
                    Message = "native window-state fields are unavailable before app lifecycle debugging" }

            let unavailableModel, _ = Viewer.update (VisibilityObserved unavailable) model
            Expect.equal unavailableModel.LifecycleState InaccessibleWindow "unavailable observable facts degrade rather than report visible success"
        }

        test "launch outcomes separate evidence close from user close and expose visibility fields" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update = fun _ model -> model, []
                  View = fun _ -> Group []
                  MapKey = fun _ _ -> None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "skia"
                  EvidencePath = None }

            match Viewer.runAppEvidence request { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
            | Result.Ok outcome ->
                Expect.equal outcome.Mode "persistent-evidence" "explicit evidence runs do not report interactive mode"
                Expect.equal outcome.CloseReason (Some EvidenceRequestedClose) "evidence close has its own close reason"
                Expect.isFalse outcome.UserCloseObserved "evidence close is not user close"
                Expect.isTrue outcome.EvidenceCloseObserved "evidence close compatibility field is true"
                Expect.equal outcome.WindowVisible ViewerObservedValue.Unsupported "bounded evidence does not claim desktop visibility"
            | Result.Error failure ->
                Expect.equal failure.Classification UnsupportedEnvironment "unsupported hosts fail before claiming an evidence close"
                Expect.equal failure.BlockedStage Window "unsupported hosts are blocked at window/session setup"
                Expect.equal failure.DiagnosticCategory EnvironmentSession "unsupported hosts keep environment diagnostics separate from app lifecycle"
        }

        test "MVU lifecycle transitions emit effects for session window visibility close timeout and failure states" {
            let options = { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let model, _ = Viewer.init options

            let started, startEffects = Viewer.update StartInteractive model
            Expect.equal started.LifecycleState CheckingDesktopSession "start enters session precheck"
            Expect.exists startEffects (function CheckDesktopSession -> true | _ -> false) "session precheck is an effect"

            let sessionReady =
                { RuntimeDirectory = Some "/tmp/runtime"
                  RuntimeDirectoryExists = true
                  RuntimeDirectoryOwnerSuitable = true
                  RuntimeDirectoryPermissionsSuitable = true
                  DisplayVariable = Some "DISPLAY=:99"
                  DisplaySocket = Some "/tmp/.X11-unix/X99"
                  DisplaySocketExists = true
                  SessionBus = Some "unix:path=/tmp/bus"
                  FallbackRuntimeDirectory = None
                  FallbackIsFullDesktopSession = false
                  DiagnosticClass = "environment-session-ready"
                  Message = "Desktop session prerequisites are present." }

            let starting, startingEffects = Viewer.update (DesktopSessionChecked sessionReady) started
            Expect.equal starting.LifecycleState StartingWindow "ready session starts window creation"
            Expect.exists startingEffects (function OpenWindow("Product", { Width = 640; Height = 480 }) -> true | _ -> false) "window opening stays at interpreter edge"

            let diagnostic =
                { WindowInitialized = true
                  NativeHandle = ViewerObservedValue.Observed true
                  Visible = ViewerObservedValue.Observed true
                  Focusable = ViewerObservedValue.Observed true
                  Focused = ViewerObservedValue.Unsupported
                  Minimized = ViewerObservedValue.Observed false
                  Maximized = ViewerObservedValue.Observed false
                  ClientSize = Some "640x480"
                  RenderableSurfaceAvailable = ViewerObservedValue.Observed true
                  Backend = Some "skia"
                  InputDevicesAvailable = ViewerObservedValue.Observed true
                  FailureClass = None
                  Message = "visible window surface observed" }

            let created, createdEffects = Viewer.update (WindowCreated diagnostic) starting
            Expect.equal created.LifecycleState ViewerLifecycleState.WindowCreated "window-created state is represented"
            Expect.exists createdEffects (function QueryNativeWindowState -> true | _ -> false) "window creation queries native state"

            let checking, checkingEffects = Viewer.update (VisibilityCheckStarted diagnostic) created
            Expect.equal checking.LifecycleState VisibilityChecking "visibility-checking state is represented"
            Expect.exists checkingEffects (function QueryNativeWindowState -> true | _ -> false) "visibility check requests native state"

            let running, runningEffects = Viewer.update (VisibilityObserved diagnostic) checking
            Expect.equal running.LifecycleState InteractiveRunning "visible surface transitions to interactive running"
            Expect.exists runningEffects (function EmitDiagnostic event when event.Message.Contains "visible window" -> true | _ -> false) "visibility observation emits diagnostics"

            let requestedClose, requestCloseEffects = Viewer.update AppCloseRequested running
            Expect.equal requestedClose.LifecycleState CloseRequested "app close requests are distinct from user close"
            Expect.exists requestCloseEffects (function CloseWindow -> true | _ -> false) "app close emits close effect"

            let unsupportedSession = { sessionReady with DiagnosticClass = "unsupported-host"; Message = "DISPLAY is missing" }
            let unsupported, unsupportedEffects = Viewer.update (DesktopSessionChecked unsupportedSession) started
            Expect.equal unsupported.LifecycleState Unsupported "unsupported session is explicit"
            Expect.exists unsupportedEffects (function EmitDiagnostic event when event.Category = EnvironmentSession -> true | _ -> false) "unsupported session emits environment diagnostic"

            let inaccessibleDiagnostic = { diagnostic with Visible = ViewerObservedValue.Observed false; FailureClass = Some "window-visibility"; Message = "taskbar only" }
            let inaccessible, _ = Viewer.update (VisibilityObserved inaccessibleDiagnostic) checking
            Expect.equal inaccessible.LifecycleState InaccessibleWindow "inaccessible native windows are not success"

            let timedOut, timeoutEffects = Viewer.update RunTimedOut running
            Expect.equal timedOut.LifecycleState Failed "timeout is a failure transition"
            Expect.exists timeoutEffects (function EmitDiagnostic event when event.Stage = Some Timeout -> true | _ -> false) "timeout emits diagnostic"

            let failure =
                { BlockedStage = Window
                  Classification = UnsupportedEnvironment
                  DiagnosticCategory = Startup
                  Message = "window failed"
                  LastDiagnosticSummary = Some "window failed" }

            let failed, failureEffects = Viewer.update (RunFailed failure) running
            Expect.equal failed.LifecycleState Failed "run failure is a failure transition"
            Expect.exists failureEffects (function EmitDiagnostic event when event.Message = "window failed" -> true | _ -> false) "failure emits diagnostic"
        }

        test "runApp semantic outcome reports interactive mode and never evidence self-close" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update = fun _ model -> model, []
                  View = fun _ -> Group []
                  MapKey = fun _ _ -> None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            if livePersistentTestsEnabled() then
                match Viewer.runApp { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
                | Result.Ok outcome ->
                    Expect.equal outcome.Mode "interactive-window" "runApp reports the interactive launch mode"
                    Expect.equal outcome.SelfClosedForEvidence false "runApp never reports evidence self-close"
                    Expect.equal outcome.UserCloseObserved true "successful interactive result requires an explicit close path"
                    Expect.notEqual outcome.Mode "persistent-evidence" "runApp is not the evidence launch path"
                | Result.Error failure ->
                    Expect.equal failure.Classification UnsupportedEnvironment "unsupported hosts report environment failure before lifecycle debugging"
                    Expect.equal failure.BlockedStage Window "unsupported hosts are blocked before window lifecycle"
            else
                let capability = Viewer.runtimeCapability()
                Expect.equal capability.BoundedSmoke true "bounded evidence remains available without live persistent test opt-in"
                Expect.equal capability.KeyboardInput true "keyboard mapping remains part of the interactive contract"
        }

        test "evidence lifecycle update emits bounded effects and failure transitions" {
            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "skia"
                  EvidencePath = Some "readiness/evidence-launch-mode.md" }

            let model, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let evidence, evidenceEffects = Viewer.update (StartEvidence request) model
            Expect.equal evidence.LifecycleState EvidenceRunning "evidence start is distinct from interactive start"
            Expect.exists evidenceEffects (function StartBoundedRun started when started.Target = FirstFrame -> true | _ -> false) "evidence mode starts bounded interpreter effect"

            let targetReached, targetEffects = Viewer.update EvidenceTargetReached evidence
            Expect.equal targetReached.LifecycleState Closing "evidence completion closes the run"
            Expect.exists targetEffects (function CloseWindow -> true | _ -> false) "evidence completion closes through an effect"

            let timedOut, timeoutEffects = Viewer.update RunTimedOut evidence
            Expect.equal timedOut.LifecycleState Failed "timeout is represented in model state"
            Expect.exists timeoutEffects (function EmitDiagnostic diagnostic when diagnostic.Stage = Some Timeout -> true | _ -> false) "timeout emits a diagnostic effect"

            let failure =
                { BlockedStage = App
                  Classification = AppLifecycle
                  DiagnosticCategory = Startup
                  Message = "host failed"
                  LastDiagnosticSummary = Some "host failed" }

            let failed, failureEffects = Viewer.update (RunFailed failure) evidence
            Expect.equal failed.LifecycleState Failed "failure is represented in model state"
            Expect.exists failureEffects (function EmitDiagnostic diagnostic when diagnostic.Message = "host failed" -> true | _ -> false) "failure emits diagnostic effect"
        }

        test "persistent run exposes launch outcome fields or unsupported-host diagnostics" {
            let options = { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let scene = Group []

            if livePersistentTestsEnabled() then
                match Viewer.run options scene with
                | Result.Ok outcome ->
                    Expect.equal outcome.Status "ok" "persistent launch reports ok status"
                    Expect.equal outcome.Mode "interactive-window" "normal launch mode is explicitly interactive"
                    Expect.equal outcome.WindowOpened true "window-opened evidence is explicit"
                    Expect.equal outcome.FirstFramePresented true "interactive launch reports first-frame presentation"
                    Expect.equal outcome.SelfClosedForEvidence false "interactive launch is not reported as evidence self-close"
                    Expect.equal outcome.UserCloseObserved true "successful interactive outcome requires explicit close observation"
                    Expect.equal outcome.ExitPath true "intentional exit path is explicit"
                    Expect.equal outcome.InputDispatch "not-applicable" "scene-only launch marks input dispatch not applicable"
                    Expect.isNone outcome.BlockedStage "successful launch has no blocked stage"
                    Expect.isNone outcome.Classification "successful launch has no failure classification"
                | Result.Error failure ->
                    Expect.equal failure.Classification UnsupportedEnvironment "headless or unsupported hosts are classified separately from product defects"
                    Expect.equal failure.BlockedStage Window "persistent launch is blocked before window creation"
                    Expect.stringContains failure.Message "DISPLAY" "unsupported Linux diagnostics name the missing display host"
            else
                let capability = Viewer.runtimeCapability()
                Expect.equal capability.BoundedSmoke true "bounded evidence remains available without live persistent test opt-in"
                Expect.equal capability.KeyboardInput true "keyboard input remains part of the persistent contract"
        }

        test "desktop diagnostics expose environment-session classification before lifecycle debugging" {
            let diagnostic = Viewer.desktopSessionDiagnostic()

            Expect.isTrue (diagnostic.DiagnosticClass.StartsWith("environment-session", StringComparison.Ordinal) || diagnostic.DiagnosticClass = "unsupported-host") "desktop diagnostics use environment/session or unsupported-host class"
            Expect.equal diagnostic.FallbackIsFullDesktopSession false "private runtime fallback is never a full desktop session"
            Expect.isNonEmpty diagnostic.Message "diagnostic message is reviewer-visible"
        }

        test "desktop diagnostics report missing Linux session prerequisites and fallback limitations" {
            if OperatingSystem.IsLinux() then
                withEnvironment
                    [ "XDG_RUNTIME_DIR", null
                      "WAYLAND_DISPLAY", null
                      "DISPLAY", null
                      "DBUS_SESSION_BUS_ADDRESS", null ]
                    (fun () ->
                        let diagnostic = Viewer.desktopSessionDiagnostic()

                        Expect.isNone diagnostic.RuntimeDirectory "missing XDG_RUNTIME_DIR is reported"
                        Expect.isFalse diagnostic.RuntimeDirectoryExists "missing runtime directory does not exist"
                        Expect.isFalse diagnostic.RuntimeDirectoryOwnerSuitable "missing runtime directory is not owner-suitable"
                        Expect.isFalse diagnostic.RuntimeDirectoryPermissionsSuitable "missing runtime directory is not permission-suitable"
                        Expect.isNone diagnostic.DisplayVariable "missing display variables are reported"
                        Expect.isNone diagnostic.DisplaySocket "missing display socket is reported"
                        Expect.isFalse diagnostic.DisplaySocketExists "missing display socket does not exist"
                        Expect.isNone diagnostic.SessionBus "missing session bus is reported"
                        Expect.isSome diagnostic.FallbackRuntimeDirectory "fallback runtime directory is named for diagnostics"
                        Expect.isFalse diagnostic.FallbackIsFullDesktopSession "fallback is explicitly not a full desktop session"
                        Expect.equal diagnostic.DiagnosticClass "unsupported-host" "missing session prerequisites classify as unsupported host"
                        Expect.stringContains diagnostic.Message "XDG_RUNTIME_DIR" "message names the missing runtime directory")
            else
                skiptest "Linux desktop-session diagnostics are not applicable on this host"
        }

        test "desktop diagnostics accept present Wayland socket runtime directory and session bus" {
            if OperatingSystem.IsLinux() then
                let runtimeDirectory =
                    IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-runtime-{Guid.NewGuid():N}")

                IO.Directory.CreateDirectory runtimeDirectory |> ignore
                let waylandSocket = IO.Path.Combine(runtimeDirectory, "wayland-0")
                IO.File.WriteAllText(waylandSocket, "")

                try
                    withEnvironment
                        [ "XDG_RUNTIME_DIR", runtimeDirectory
                          "WAYLAND_DISPLAY", "wayland-0"
                          "DISPLAY", null
                          "DBUS_SESSION_BUS_ADDRESS", "unix:path=/tmp/fs-gg-bus" ]
                        (fun () ->
                            let diagnostic = Viewer.desktopSessionDiagnostic()

                            Expect.equal diagnostic.RuntimeDirectory (Some runtimeDirectory) "runtime directory path is reported"
                            Expect.isTrue diagnostic.RuntimeDirectoryExists "runtime directory existence is reported"
                            Expect.isTrue diagnostic.RuntimeDirectoryOwnerSuitable "owned runtime directory is owner-suitable"
                            Expect.isTrue diagnostic.RuntimeDirectoryPermissionsSuitable "runtime directory permissions are suitable"
                            Expect.equal diagnostic.DisplayVariable (Some "WAYLAND_DISPLAY=wayland-0") "Wayland display variable is preferred"
                            Expect.equal diagnostic.DisplaySocket (Some waylandSocket) "Wayland socket path is derived from runtime directory"
                            Expect.isTrue diagnostic.DisplaySocketExists "Wayland socket existence is reported"
                            Expect.equal diagnostic.SessionBus (Some "unix:path=/tmp/fs-gg-bus") "session bus is reported"
                            Expect.equal diagnostic.DiagnosticClass "environment-session-ready" "present prerequisites classify as ready"
                            Expect.stringContains diagnostic.Message "present" "message confirms prerequisites")
                finally
                    IO.Directory.Delete(runtimeDirectory, true)
            else
                skiptest "Linux desktop-session diagnostics are not applicable on this host"
        }

        test "runtime capability distinguishes persistent window bounded smoke keyboard and unsupported reasons" {
            let capability = Viewer.runtimeCapability()

            Expect.isTrue capability.BoundedSmoke "bounded smoke remains available as explicit evidence helper"
            Expect.isTrue capability.KeyboardInput "keyboard input capability is reported"
            Expect.equal capability.RendererMode "skia" "renderer mode is reported independently from host support"
            Expect.isEmpty capability.MissingPackageCapabilities "current package exposes the persistent contract and has no package-capability gap"

            if capability.PersistentWindow then
                Expect.isEmpty capability.UnsupportedHostReasons "supported hosts do not report unsupported reasons"
            else
                Expect.isNonEmpty capability.UnsupportedHostReasons "unsupported hosts report actionable reasons"

            capability.UnsupportedHostReasons
            |> List.iter (fun reason ->
                Expect.isFalse (capability.MissingPackageCapabilities |> List.contains reason) "unsupported host reasons are not reported as missing package capabilities")
        }

        test "window behavior validation reports resize maximize startup state position and backend results" {
            let results =
                Viewer.validateWindowBehavior
                    { Viewer.defaultWindowBehavior with
                        ResizePolicy = FixedSize
                        MaximizePolicy = NotMaximizable
                        StartupState = ViewerWindowStartupState.Maximized
                        StartupPosition = Some(Coordinates(20, 30))
                        BackendPreference = Some ViewerBackendPreference.OpenGL }

            Expect.hasLength results 5 "one result is returned for each supported option family"
            Expect.exists results (fun item -> item.Option = "resize" && item.Status = Honored && item.Observed = Some "fixed-size") "resize policy is reported"
            Expect.exists results (fun item -> item.Option = "maximize" && item.Status = Honored && item.Observed = Some "not-maximizable") "maximize policy is reported"
            Expect.exists results (fun item -> item.Option = "startup-state" && item.Status = Honored && item.Observed = Some "maximized") "startup state is reported"
            Expect.exists results (fun item -> item.Option = "startup-position" && item.Status = Honored && item.Observed = Some "20,30") "startup position is reported"
            Expect.exists results (fun item -> item.Option = "backend" && item.Status = Honored && item.Observed = Some "opengl") "backend preference is reported"
        }

        test "window behavior validation rejects invalid coordinates and unsupported backend settings with diagnostics" {
            let results =
                Viewer.validateWindowBehavior
                    { Viewer.defaultWindowBehavior with
                        StartupState = ViewerWindowStartupState.Minimized
                        StartupPosition = Some(Coordinates(-1, 10))
                        BackendPreference = Some ViewerBackendPreference.Software }

            Expect.exists results (fun item -> item.Option = "startup-state" && item.Status = UnsupportedOption && item.Message.Contains "visible interactive") "minimized startup is unsupported for visible launch validation"
            Expect.exists results (fun item -> item.Option = "startup-position" && item.Status = FailedOption && item.Message.Contains "non-negative") "invalid coordinates fail validation"
            Expect.exists results (fun item -> item.Option = "backend" && item.Status = UnsupportedOption && item.Message.Contains "not supported") "unsupported backend is explicit"
        }

        test "default window behavior starts in windowed fullscreen" {
            // SC-001 / FR-003: a no-flag launch opens borderless over the work area.
            Expect.equal Viewer.defaultWindowBehavior.StartupState ViewerWindowStartupState.WindowedFullscreen "windowed fullscreen is the new no-flag default startup state"
        }

        test "fullscreen and windowed fullscreen validate as honored while minimized stays unsupported" {
            // SC-002 / FR-002: neither fullscreen state may report UnsupportedOption.
            let resultFor state =
                Viewer.validateWindowBehavior { Viewer.defaultWindowBehavior with StartupState = state }
                |> List.find (fun item -> item.Option = "startup-state")

            let launchResultFor state =
                Viewer.validateWindowLaunchBehavior { Width = 800; Height = 600 } { Viewer.defaultWindowBehavior with StartupState = state }
                |> List.find (fun item -> item.Option = "startup-state")

            Expect.equal (resultFor ViewerWindowStartupState.Fullscreen).Status Honored "fullscreen is honored in validateWindowBehavior"
            Expect.equal (resultFor ViewerWindowStartupState.WindowedFullscreen).Status Honored "windowed fullscreen is honored in validateWindowBehavior"
            Expect.equal (resultFor ViewerWindowStartupState.Minimized).Status UnsupportedOption "minimized is not a visible interactive launch state"

            Expect.equal (launchResultFor ViewerWindowStartupState.Fullscreen).Status Honored "fullscreen is honored in validateWindowLaunchBehavior"
            Expect.equal (launchResultFor ViewerWindowStartupState.WindowedFullscreen).Status Honored "windowed fullscreen is honored in validateWindowLaunchBehavior"
            Expect.equal (launchResultFor ViewerWindowStartupState.Minimized).Status UnsupportedOption "minimized stays unsupported in validateWindowLaunchBehavior"
        }

        test "fullscreen and windowed fullscreen are distinct selectable states, not aliases" {
            // T008 distinctness invariant. The concrete WindowState/WindowBorder mapping
            // (applyWindowBehaviorToOptions) is internal to the viewer and exercised by the
            // real visible-window launch evidence; the public surface proves the two states
            // are distinct (different requested/observed values), never aliases.
            let resultFor state =
                Viewer.validateWindowBehavior { Viewer.defaultWindowBehavior with StartupState = state }
                |> List.find (fun item -> item.Option = "startup-state")

            let fullscreen = resultFor ViewerWindowStartupState.Fullscreen
            let windowedFullscreen = resultFor ViewerWindowStartupState.WindowedFullscreen

            Expect.equal fullscreen.Observed (Some "fullscreen") "exclusive fullscreen observes the fullscreen value"
            Expect.equal windowedFullscreen.Observed (Some "windowed-fullscreen") "windowed fullscreen observes a distinct value"
            Expect.notEqual fullscreen.Observed windowedFullscreen.Observed "fullscreen and windowed fullscreen are never aliased"
        }

        test "window launch behavior validation includes positive size constraints and all public option families" {
            let behavior =
                { Viewer.defaultWindowBehavior with
                    ResizePolicy = FixedSize
                    MaximizePolicy = NotMaximizable
                    StartupState = ViewerWindowStartupState.Normal
                    StartupPosition = Some Centered
                    BackendPreference = Some ViewerBackendPreference.DefaultBackend }

            let valid = Viewer.validateWindowLaunchBehavior { Width = 640; Height = 480 } behavior
            let invalid = Viewer.validateWindowLaunchBehavior { Width = 0; Height = 480 } behavior

            Expect.hasLength valid 6 "initial size is validated with the five window behavior option families"
            Expect.exists valid (fun item -> item.Option = "initial-size" && item.Status = Honored && item.Observed = Some "640x480") "positive initial size is honored"
            Expect.exists valid (fun item -> item.Option = "resize" && item.Status = Honored && item.Observed = Some "fixed-size") "resize policy remains part of launch validation"
            Expect.exists valid (fun item -> item.Option = "maximize" && item.Status = Honored && item.Observed = Some "not-maximizable") "maximize policy remains part of launch validation"
            Expect.exists valid (fun item -> item.Option = "startup-state" && item.Status = Honored && item.Observed = Some "normal") "startup state remains part of launch validation"
            Expect.exists valid (fun item -> item.Option = "startup-position" && item.Status = Honored && item.Observed = Some "centered") "startup position remains part of launch validation"
            Expect.exists valid (fun item -> item.Option = "backend" && item.Status = Honored && item.Observed = Some "default") "backend preference remains part of launch validation"
            Expect.exists invalid (fun item -> item.Option = "initial-size" && item.Status = FailedOption && item.Message.Contains "positive") "non-positive size fails before native launch"
        }

        test "init and start effects carry public window behavior through the MVU boundary" {
            let behavior =
                { Viewer.defaultWindowBehavior with
                    ResizePolicy = FixedSize
                    MaximizePolicy = NotMaximizable
                    StartupState = ViewerWindowStartupState.Maximized
                    StartupPosition = Some(Coordinates(32, 48))
                    BackendPreference = Some ViewerBackendPreference.Vulkan }

            let model, initEffects = Viewer.initWithWindowBehavior { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } behavior
            let sessionReady =
                { RuntimeDirectory = Some "/run/user/1000"
                  RuntimeDirectoryExists = true
                  RuntimeDirectoryOwnerSuitable = true
                  RuntimeDirectoryPermissionsSuitable = true
                  DisplayVariable = Some "DISPLAY=:0"
                  DisplaySocket = Some "/tmp/.X11-unix/X0"
                  DisplaySocketExists = true
                  SessionBus = Some "unix:path=/run/user/1000/bus"
                  FallbackRuntimeDirectory = None
                  FallbackIsFullDesktopSession = false
                  DiagnosticClass = "desktop-session"
                  Message = "desktop session ready" }

            let _, readyEffects = Viewer.update (DesktopSessionChecked sessionReady) model

            Expect.equal model.WindowBehavior behavior "model owns the requested behavior"
            Expect.exists initEffects (function ApplyWindowOptions request -> request = behavior | _ -> false) "init emits option application for interpreter startup"
            Expect.exists readyEffects (function ApplyWindowOptions request -> request = behavior | _ -> false) "desktop-session transition applies requested options before visibility checks"
        }

        test "persistent run preserves bounded APIs as explicit separate helpers" {
            let options = { Title = "Product"; InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let scene = Group []

            let invalidPersistent = Viewer.run { options with Title = "" } scene
            let invalidBounded =
                Viewer.runBounded
                    { Target = FrameCount 0
                      Timeout = TimeSpan.FromSeconds 1.0
                      Diagnostics = Viewer.defaultDiagnostics
                      RendererMode = "skia"
                      EvidencePath = None }
                    options
                    scene

            match invalidPersistent, invalidBounded with
            | Result.Error persistentFailure, Result.Error boundedFailure ->
                Expect.equal persistentFailure.Classification ProductDefect "persistent option validation remains product-defect classification"
                Expect.equal boundedFailure.Classification ProductDefect "bounded request validation remains product-defect classification"
                Expect.stringContains boundedFailure.Message "frame count" "bounded helper keeps its own request validation"
            | other -> failtestf "expected separate persistent and bounded validation failures, got %A" other
        }

        test "bounded helper APIs remain explicitly callable regression" {
            let scene = Group []
            let invalidOptions = { Title = ""; InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let validOptions = { Title = "Product"; InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

            match Viewer.runUntilFirstFrame invalidOptions scene with
            | Result.Error failure ->
                Expect.equal failure.Classification ProductDefect "first-frame helper keeps option validation"
                Expect.stringContains failure.Message "title" "first-frame helper reports title validation"
            | Result.Ok evidence -> failtestf "expected first-frame validation failure, got %A" evidence

            match Viewer.runForFrames 0 validOptions scene with
            | Result.Error failure ->
                Expect.equal failure.Classification ProductDefect "frame-count helper keeps request validation"
                Expect.stringContains failure.Message "frame count" "frame-count helper reports frame validation"
            | Result.Ok evidence -> failtestf "expected frame-count validation failure, got %A" evidence
        }

        test "generated app host public boundary maps keyboard tick update and close effects" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update =
                    fun msg model ->
                        match msg with
                        | Increment -> { model with Count = model.Count + 1 }, [ RenderScene(Group []) ]
                        | Close -> { model with Closed = true }, [ CloseWindow ]
                  View = fun model -> Text((0.0, 0.0), $"count {model.Count}", { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy })
                  MapKey = fun key isDown -> if isDown && key = Space then Some Increment else None
                  Tick = fun elapsed -> if elapsed > TimeSpan.Zero then Some Increment else None
                  Diagnostics = Viewer.defaultDiagnostics }

            let model, effects =
                GeneratedAppHost.dispatchKey
                    host
                    { RawKey = "Space"
                      Direction = ViewerKeyDirection.KeyDown }
                    { Count = 0; Closed = false }

            Expect.equal model.Count 1 "keyboard dispatch routes through host update"
            Expect.exists effects (function RenderScene _ -> true | _ -> false) "host update effects are emitted at the viewer boundary"
            Expect.equal (host.Tick(TimeSpan.FromMilliseconds 16.0)) (Some Increment) "tick mapping is public and pure"

            if livePersistentTestsEnabled() then
                match Viewer.runApp { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
                | Result.Ok outcome ->
                    Expect.equal outcome.Mode "interactive-window" "runApp reports interactive-window mode"
                    Expect.equal outcome.Status "ok" "runApp reports ok status on supported hosts"
                    Expect.equal outcome.SelfClosedForEvidence false "runApp is not evidence self-close"
                | Result.Error failure ->
                    Expect.equal failure.Classification UnsupportedEnvironment "runApp reports unsupported host separately from product defects"
            else
                Expect.equal (host.Tick(TimeSpan.FromMilliseconds 16.0)) (Some Increment) "tick mapping remains testable without opening a live window"
        }

        test "generated host MVU boundary asserts init update effects keyboard tick first-frame and close" {
            let host =
                { Init =
                    fun () ->
                        { Count = 0; Closed = false },
                        [ EmitDiagnostic
                              { Level = Info
                                Category = Startup
                                Message = "generated host init"
                                FrameIndex = None
                                Stage = None
                                Elapsed = None } ]
                  Update =
                    fun msg model ->
                        match msg with
                        | Increment ->
                            { model with Count = model.Count + 1 },
                            [ RenderScene(Text((0.0, 0.0), $"count {model.Count + 1}", { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy })) ]
                        | Close -> { model with Closed = true }, [ CloseWindow ]
                  View = fun model -> Text((0.0, 0.0), $"count {model.Count}", { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy })
                  MapKey = fun key isDown -> if isDown && key = ArrowLeft then Some Increment else None
                  Tick = fun elapsed -> if elapsed >= TimeSpan.FromMilliseconds 16.0 then Some Increment else None
                  Diagnostics = Viewer.defaultDiagnostics }

            let initial, initEffects = host.Init()
            Expect.equal initial.Count 0 "generated host init returns initial model"
            Expect.exists initEffects (function EmitDiagnostic diagnostic when diagnostic.Message = "generated host init" -> true | _ -> false) "generated host init exposes startup effects"

            let afterKey, keyEffects =
                GeneratedAppHost.dispatchKey
                    host
                    { RawKey = "ArrowLeft"
                      Direction = ViewerKeyDirection.KeyDown }
                    initial

            Expect.equal afterKey.Count 1 "keyboard input dispatch reaches host update"
            Expect.exists keyEffects (function RenderScene _ -> true | _ -> false) "keyboard update emits render refresh"

            match host.Tick(TimeSpan.FromMilliseconds 16.0) with
            | Some tickMsg ->
                let afterTick, tickEffects = host.Update tickMsg afterKey
                Expect.equal afterTick.Count 2 "time-based tick progression reaches host update"
                Expect.exists tickEffects (function RenderScene _ -> true | _ -> false) "tick progression emits render refresh"
            | None -> failtest "expected tick message after 16ms"

            let viewer, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let running, _ = Viewer.update StartInteractive viewer
            let firstFrame, frameEffects = Viewer.update (FramePresented { Width = 640; Height = 480 }) running
            Expect.equal firstFrame.LifecycleState ViewerLifecycleState.FirstFramePresented "first-frame state is represented in viewer model"
            Expect.isFalse (frameEffects |> List.exists (function CloseWindow -> true | _ -> false)) "first-frame state does not close the host"

            let closed, closeEffects = host.Update Close afterKey
            Expect.isTrue closed.Closed "explicit close message updates generated host model"
            Expect.exists closeEffects (function CloseWindow -> true | _ -> false) "explicit close emits close effect"
        }

        test "generated host observes input availability and native close through viewer lifecycle" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update =
                    fun msg model ->
                        match msg with
                        | Increment -> { model with Count = model.Count + 1 }, [ RenderScene(Group []) ]
                        | Close -> { model with Closed = true }, [ CloseWindow ]
                  View = fun model -> Text((0.0, 0.0), $"count {model.Count}", { Red = 255uy; Green = 255uy; Blue = 255uy; Alpha = 255uy })
                  MapKey = fun key isDown -> if isDown && key = Space then Some Increment else None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            let initial, initEffects = host.Init()
            Expect.equal initial.Count 0 "generated host init is pure and returns initial state"
            Expect.isEmpty initEffects "generated host init has no native side effects"

            let afterKey, keyEffects =
                GeneratedAppHost.dispatchKey
                    host
                    { RawKey = "Space"
                      Direction = ViewerKeyDirection.KeyDown }
                    initial

            Expect.equal afterKey.Count 1 "manual keyboard input is available through generated host mapping"
            Expect.exists keyEffects (function RenderScene _ -> true | _ -> false) "manual input emits render refresh effect"

            let diagnostic =
                { WindowInitialized = true
                  NativeHandle = ViewerObservedValue.Observed true
                  Visible = ViewerObservedValue.Observed true
                  Focusable = ViewerObservedValue.Observed true
                  Focused = ViewerObservedValue.Observed true
                  Minimized = ViewerObservedValue.Observed false
                  Maximized = ViewerObservedValue.Observed false
                  ClientSize = Some "640x480"
                  RenderableSurfaceAvailable = ViewerObservedValue.Observed true
                  Backend = Some "skia"
                  InputDevicesAvailable = ViewerObservedValue.Observed true
                  FailureClass = None
                  Message = "visible window with keyboard input observed" }

            let viewer, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let running, _ = Viewer.update StartInteractive viewer
            let visible, visibilityEffects = Viewer.update (VisibilityObserved diagnostic) running
            Expect.equal visible.LifecycleState InteractiveRunning "input-capable visible window reaches interactive running"
            Expect.exists visibilityEffects (function EmitDiagnostic event when event.Message.Contains "keyboard input" -> true | _ -> false) "input-device observation is diagnostic evidence"

            let firstFrame, frameEffects = Viewer.update (FramePresented { Width = 640; Height = 480 }) visible
            Expect.equal firstFrame.LifecycleState ViewerLifecycleState.FirstFramePresented "first frame is modeled after input-capable visibility"
            Expect.isFalse (frameEffects |> List.exists (function CloseWindow -> true | _ -> false)) "first frame does not close interactive host"

            let nativeClosed, nativeCloseEffects = Viewer.update HostCloseObserved firstFrame
            Expect.equal nativeClosed.LifecycleState Closing "native host close is explicit and distinct from user close"
            Expect.isFalse nativeClosed.UserCloseObserved "native close does not claim manual user close"
            Expect.exists nativeCloseEffects (function CloseWindow -> true | _ -> false) "native close emits close effect"
        }

        test "runAppEvidence is explicit and keeps invalid evidence requests separate from interactive launch" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update = fun _ model -> model, []
                  View = fun _ -> Group []
                  MapKey = fun _ _ -> None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            let invalidEvidence =
                { Target = FirstFrame
                  Timeout = TimeSpan.Zero
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "skia"
                  EvidencePath = None }

            match Viewer.runAppEvidence invalidEvidence { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
            | Result.Error failure ->
                Expect.equal failure.Classification ProductDefect "invalid evidence request is product configuration failure"
                Expect.equal failure.BlockedStage App "invalid evidence request is rejected before window lifecycle"
                Expect.stringContains failure.Message "positive" "timeout validation is actionable"
            | Result.Ok outcome -> failtestf "expected invalid evidence request to fail, got %A" outcome
        }

        test "runAppEvidence explicit launch reports persistent evidence self-close and first-frame fields" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update = fun _ model -> model, []
                  View = fun _ -> Group []
                  MapKey = fun _ _ -> None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            let evidencePath =
                IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-run-app-evidence-{Guid.NewGuid():N}.txt")

            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "skia"
                  EvidencePath = Some evidencePath }

            match Viewer.runAppEvidence request { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
            | Result.Ok outcome ->
                Expect.equal outcome.Status "ok" "explicit evidence launch succeeds through bounded interpreter"
                Expect.equal outcome.Mode "persistent-evidence" "evidence launch reports persistent-evidence mode"
                Expect.equal outcome.Command (Some "runAppEvidence") "evidence launch names explicit API"
                Expect.equal outcome.FirstFramePresented true "first-frame evidence is reported"
                Expect.equal outcome.SelfClosedForEvidence true "evidence launch self-closes after target"
                Expect.equal outcome.UserCloseObserved false "evidence launch does not claim user close"
                Expect.equal outcome.InputDispatch "not-required" "first-frame evidence does not require input dispatch"
                Expect.equal outcome.ExitPath true "bounded evidence has an explicit exit path"
                Expect.isNone outcome.BlockedStage "successful evidence has no blocked stage"
                Expect.isNone outcome.Classification "successful evidence has no classification"

                let evidenceText = IO.File.ReadAllText evidencePath
                Expect.stringContains evidenceText "status=ok" "serialized evidence records status"
                Expect.stringContains evidenceText "mode=persistent-evidence" "serialized evidence records persistent evidence mode"
                Expect.stringContains evidenceText "command=runAppEvidence" "serialized evidence records command"
                Expect.stringContains evidenceText "window-opened=True" "serialized evidence records window-opened status"
                Expect.stringContains evidenceText "self-closed-for-evidence=True" "serialized evidence records self-close semantics"
                Expect.stringContains evidenceText "input-dispatch=not-required" "serialized evidence records input-dispatch status"
                Expect.stringContains evidenceText "first-frame-presented=True" "serialized evidence records first-frame status"
                Expect.stringContains evidenceText "exit-path=True" "serialized evidence records exit path"
                Expect.stringContains evidenceText "blocked-stage=" "serialized evidence records blocked stage field"
                Expect.stringContains evidenceText "classification=" "serialized evidence records classification field"
                Expect.stringContains evidenceText "category=" "serialized evidence records category field"
                Expect.stringContains evidenceText "message=" "serialized evidence records message field"
            | Result.Error failure -> failtestf "expected explicit evidence launch success, got %A" failure
        }

        test "requested image evidence writes decodable image artifact and explicit proof claims" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update = fun _ model -> model, []
                  View = fun _ -> Group []
                  MapKey = fun _ _ -> None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            let imagePath =
                IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-requested-image-evidence-{Guid.NewGuid():N}.png")

            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "skia"
                  EvidencePath = Some imagePath }

            try
                match Viewer.runAppEvidence request { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
                | Result.Ok outcome ->
                    Expect.equal outcome.Mode "persistent-evidence" "image evidence is an explicit evidence launch mode"
                    Expect.exists outcome.VisualEvidence (fun item -> item.Kind = ViewerVisualEvidenceKind.Image) "requested image evidence records an image artifact"
                    Expect.exists outcome.VisualEvidence (fun item -> item.ImageDecodable = Some true) "requested image evidence records decodability"
                    Expect.exists outcome.VisualEvidence (fun item -> item.ProvesSceneRendering && item.ProvesDesktopVisibility = false) "scene-rendering and desktop-visibility claims are explicit"
                    Expect.isTrue (isPngFile imagePath) "requested image evidence path contains a decodable PNG image"
                | Result.Error failure -> failtestf "expected requested image evidence result, got %A" failure
            finally
                if IO.File.Exists imagePath then
                    IO.File.Delete imagePath
        }

        test "metadata hash evidence is labeled separately and never written as screenshot image" {
            let host =
                { Init = fun () -> { Count = 0; Closed = false }, []
                  Update = fun _ model -> model, []
                  View = fun _ -> Group []
                  MapKey = fun _ _ -> None
                  Tick = fun _ -> None
                  Diagnostics = Viewer.defaultDiagnostics }

            let metadataPath =
                IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-metadata-hash-evidence-{Guid.NewGuid():N}.txt")

            let request =
                { Target = FirstFrame
                  Timeout = TimeSpan.FromSeconds 2.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "metadata-hash"
                  EvidencePath = Some metadataPath }

            try
                match Viewer.runAppEvidence request { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None } host with
                | Result.Ok outcome ->
                    Expect.exists outcome.VisualEvidence (fun item -> item.Kind = ViewerVisualEvidenceKind.MetadataHash) "metadata evidence is labeled metadata-hash"
                    Expect.isFalse (outcome.VisualEvidence |> List.exists (fun item -> item.Kind = ViewerVisualEvidenceKind.Image && item.Path = Some metadataPath)) "metadata/hash artifacts are not mislabeled as image evidence"
                    Expect.exists outcome.VisualEvidence (fun item -> item.Kind = ViewerVisualEvidenceKind.MetadataHash && not item.ProvesDesktopVisibility) "metadata/hash evidence does not claim desktop visibility"
                    Expect.isFalse (isPngFile metadataPath) "metadata/hash output is not a screenshot image"
                | Result.Error failure -> failtestf "expected metadata/hash evidence result, got %A" failure
            finally
                if IO.File.Exists metadataPath then
                    IO.File.Delete metadataPath
        }

        test "evidence launch timeout and failure paths are explicit and bounded" {
            let request =
                { Target = Duration(TimeSpan.FromSeconds 10.0)
                  Timeout = TimeSpan.FromMilliseconds 1.0
                  Diagnostics = Viewer.defaultDiagnostics
                  RendererMode = "skia"
                  EvidencePath = None }

            let model, _ = Viewer.initRun request
            let diagnostic =
                { Level = Warning
                  Category = Startup
                  Message = "waiting for evidence target"
                  FrameIndex = None
                  Stage = Some Timeout
                  Elapsed = Some(TimeSpan.FromMilliseconds 1.0) }

            let withDiagnostic, _ = Viewer.updateRun (RecordDiagnostic diagnostic) model
            let timedOut, timeoutEffects = Viewer.updateRun TimeoutRun withDiagnostic

            match timedOut.Completed with
            | Some(Result.Error failure) ->
                Expect.equal failure.BlockedStage Timeout "timeout blocked stage is explicit"
                Expect.equal failure.Classification ProductDefect "timeout is a bounded evidence failure"
                Expect.equal failure.LastDiagnosticSummary (Some "waiting for evidence target") "timeout keeps last diagnostic summary"
            | other -> failtestf "expected timeout evidence failure, got %A" other

            Expect.exists timeoutEffects (function StopBoundedRun -> true | _ -> false) "timeout stops bounded evidence"

            let launchModel, _ = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            let evidenceModel, evidenceEffects = Viewer.update (StartEvidence request) launchModel
            Expect.equal evidenceModel.LifecycleState EvidenceRunning "evidence launch enters evidence-running state"
            Expect.exists evidenceEffects (function StartBoundedRun started when started.Target = request.Target -> true | _ -> false) "evidence launch emits bounded run effect"
        }

        test "diagnostic filtering honors categories and level thresholds across startup input renderer and readback categories" {
            let options =
                { Viewer.defaultDiagnostics with
                    MinimumLevel = Warning
                    Categories = Set.ofList [ ViewerDiagnosticCategory.Startup; Input; ViewerDiagnosticCategory.Renderer; Screenshot ] }

            let diagnostic level category message =
                { Level = level
                  Category = category
                  Message = message
                  FrameIndex = None
                  Stage = None
                  Elapsed = Some TimeSpan.Zero }

            let captured =
                [ diagnostic Error ViewerDiagnosticCategory.Startup "startup failed"
                  diagnostic Warning Input "input fallback"
                  diagnostic Warning ViewerDiagnosticCategory.Renderer "renderer fallback"
                  diagnostic Warning Screenshot "readback unavailable" ]

            captured
            |> List.iter (fun item ->
                Expect.isTrue (Viewer.shouldCaptureDiagnostic options item) $"captures {item.Category} {item.Level}")

            Expect.isFalse (Viewer.shouldCaptureDiagnostic options (diagnostic Info ViewerDiagnosticCategory.Startup "startup info")) "info below warning threshold is filtered"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic options (diagnostic Warning OpenGl "opengl detail")) "unselected category is filtered"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic options (diagnostic Error Skia "skia detail")) "unselected Skia category is filtered"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic options (diagnostic Error ViewerDiagnosticCategory.Framebuffer "framebuffer detail")) "unselected framebuffer category is filtered"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic options (diagnostic Error ViewerDiagnosticCategory.Scene "scene detail")) "unselected scene category is filtered"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic options (diagnostic Error Frame "frame detail")) "unselected frame category is filtered"
        }

        test "frame sampling excludes repeated per-frame diagnostics unless enabled and bounded by the frame limit" {
            let frame index =
                { Level = Info
                  Category = ViewerDiagnosticCategory.Frame
                  Message = $"frame {index} presented"
                  FrameIndex = Some index
                  Stage = None
                  Elapsed = Some(TimeSpan.FromMilliseconds(float index * 16.0)) }

            let startup =
                { Level = Info
                  Category = ViewerDiagnosticCategory.Startup
                  Message = "window-created"
                  FrameIndex = None
                  Stage = Some Window
                  Elapsed = Some TimeSpan.Zero }

            let startupOnly =
                { Viewer.defaultDiagnostics with
                    Categories = Set.ofList [ ViewerDiagnosticCategory.Startup ]
                    FrameLogLimit = Some 0 }

            Expect.isTrue (Viewer.shouldCaptureDiagnostic startupOnly startup) "startup diagnostics are still captured"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic startupOnly (frame 1)) "startup-only diagnostics exclude frame messages"

            let sampledFrames =
                { startupOnly with
                    Categories = Set.ofList [ ViewerDiagnosticCategory.Startup; ViewerDiagnosticCategory.Frame ]
                    FrameLogLimit = Some 2 }

            Expect.isTrue (Viewer.shouldCaptureDiagnostic sampledFrames (frame 1)) "first sampled frame is captured"
            Expect.isTrue (Viewer.shouldCaptureDiagnostic sampledFrames (frame 2)) "second sampled frame is captured"
            Expect.isFalse (Viewer.shouldCaptureDiagnostic sampledFrames (frame 3)) "frame diagnostics stop after the configured limit"

            let unlimitedFrames = { sampledFrames with FrameLogLimit = None }
            Expect.isTrue (Viewer.shouldCaptureDiagnostic unlimitedFrames (frame 25)) "unbounded frame diagnostics are explicit"
        }

        test "diagnostic sink captures startup input renderer and frame categories in-process" {
            let captured = ResizeArray<ViewerDiagnosticEvent>()

            let options =
                { Viewer.defaultDiagnostics with
                    Categories = Set.ofList [ ViewerDiagnosticCategory.Startup; Input; ViewerDiagnosticCategory.Renderer; ViewerDiagnosticCategory.Frame ]
                    FrameLogLimit = Some 1
                    Sink = Some captured.Add }

            let diagnostic category message frame =
                { Level = Info
                  Category = category
                  Message = message
                  FrameIndex = frame
                  Stage = None
                  Elapsed = Some TimeSpan.Zero }

            [ diagnostic ViewerDiagnosticCategory.Startup "window-created" None
              diagnostic Input "enter dispatched" None
              diagnostic ViewerDiagnosticCategory.Renderer "renderer-ready" None
              diagnostic ViewerDiagnosticCategory.Frame "frame 1 presented" (Some 1)
              diagnostic ViewerDiagnosticCategory.Frame "frame 2 presented" (Some 2) ]
            |> List.iter (Viewer.captureDiagnostic options >> ignore)

            let categories = captured |> Seq.map _.Category |> Set.ofSeq
            Expect.equal categories (Set.ofList [ ViewerDiagnosticCategory.Startup; Input; ViewerDiagnosticCategory.Renderer; ViewerDiagnosticCategory.Frame ]) "sink captures selected categories without stderr scraping"
            Expect.equal captured.Count 4 "frame sampling limits repeated per-frame sink messages"
            Expect.exists captured (fun item -> item.Message = "enter dispatched") "input diagnostic is capturable"
            Expect.exists captured (fun item -> item.Message = "renderer-ready") "renderer diagnostic is capturable"
        }

        test "viewer update emits categorized diagnostics for startup input scene frame and failure milestones" {
            let model, initEffects = Viewer.init { Title = "Product"; InitialSize = { Width = 640; Height = 480 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            Expect.exists initEffects (function EmitDiagnostic diagnostic when diagnostic.Category = ViewerDiagnosticCategory.Startup && diagnostic.Message.Contains "Product" -> true | _ -> false) "startup emits categorized diagnostic"

            let _, renderEffects = Viewer.update (Render(Group [])) model
            Expect.exists renderEffects (function EmitDiagnostic diagnostic when diagnostic.Category = ViewerDiagnosticCategory.Scene -> true | _ -> false) "scene render emits categorized diagnostic"

            let _, inputEffects =
                Viewer.update
                    (KeyEvent { RawKey = "Space"; Direction = ViewerKeyDirection.KeyDown })
                    model

            Expect.exists inputEffects (function EmitDiagnostic diagnostic when diagnostic.Category = Input && diagnostic.Message.Contains "Space" -> true | _ -> false) "input emits raw and normalized key diagnostic"

            let _, frameEffects = Viewer.update (FramePresented { Width = 640; Height = 480 }) model
            Expect.exists frameEffects (function EmitDiagnostic diagnostic when diagnostic.Category = ViewerDiagnosticCategory.Frame && diagnostic.Message.Contains "640x480" -> true | _ -> false) "frame milestone emits categorized diagnostic"

            let failure =
                { BlockedStage = ViewerRunBlockedStage.GlContext
                  Classification = UnsupportedEnvironment
                  DiagnosticCategory = ViewerDiagnosticCategory.Framebuffer
                  Message = "framebuffer unavailable"
                  LastDiagnosticSummary = Some "framebuffer unavailable" }

            let _, failureEffects = Viewer.update (RunFailed failure) model
            Expect.exists failureEffects (function EmitDiagnostic diagnostic when diagnostic.Category = ViewerDiagnosticCategory.Framebuffer && diagnostic.Stage = Some ViewerRunBlockedStage.GlContext -> true | _ -> false) "framebuffer failure preserves category and stage"
        }
    ]
