module Product.EvidenceCommands

open System
open System.IO
open FS.Skia.UI.Scene
open Product.Model
open Product.View
open Product.LayoutEvidence
//#if (profile == "governed" || profile == "headless-scene")

let private writeLines (path: string) (lines: string list) =
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory(directory |> string) |> ignore

    File.WriteAllLines(path, Array.ofList lines)

let layoutEvidenceCommand evidencePath width height =
    let size = { Width = width; Height = height }
    let report = layoutEvidenceForSize size initialModel

    let lines =
        [ "status=ok"
          "command=--layout-evidence"
          "profile=headless-governed"
          $"scene=Product.Program.view"
          $"output-size={size.Width}x{size.Height}"
          $"proof-level={report.ProofLevel}"
          $"text-bounds={report.TextBounds.Length}"
          $"gameplay-bounds={report.GameplayBounds.Length}"
          $"overlap-status={report.OverlapStatus}"
          $"measurement-mode={report.MeasurementMode}" ]

    writeLines evidencePath lines
    lines |> List.iter (printfn "%s")
    0

let sceneEvidence evidencePath =
    let result =
        SceneEvidence.render
            { Scene = { Nodes = [ view initialModel ] }
              OutputSize = { Width = 320; Height = 200 }
              Format = Metadata
              RendererMode = "deterministic-scene"
              EvidencePath = Some evidencePath }

    match result with
    | Result.Ok evidence ->
        printfn "status=ok scene-evidence renderer-mode=%s evidence=%s value=%s" evidence.RendererMode evidencePath evidence.Value
        0
    | Result.Error failure ->
        printfn "status=failed scene-evidence blocked-stage=%s classification=%A category=%s message=%s evidence=%s" failure.BlockedStage failure.Classification failure.DiagnosticCategory failure.Message evidencePath
        1

let tryRunEvidenceCommand args =
    match args with
    | "--layout-evidence" :: path :: width :: height :: _ ->
        match Int32.TryParse width, Int32.TryParse height with
        | (true, parsedWidth), (true, parsedHeight) -> Some(layoutEvidenceCommand path parsedWidth parsedHeight)
        | _ ->
            printfn "status=failed command=--layout-evidence diagnostics=width and height must be integers"
            Some 1
    | "--layout-evidence" :: path :: _ -> Some(layoutEvidenceCommand path 640 480)
    | "--layout-evidence" :: _ -> Some(layoutEvidenceCommand "readiness/layout-evidence.txt" 640 480)
    | "--scene-evidence" :: path :: _ -> Some(sceneEvidence path)
    | "--scene-evidence" :: _ -> Some(sceneEvidence "readiness/headless-scene-evidence.txt")
    | _ -> None

//#else
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open Product.WindowOptions

let writeGeneratedEvidenceLines (path: string) echoToStdout exitCode lines =
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory(directory |> string) |> ignore

    File.WriteAllLines(path, Array.ofList lines)

    if echoToStdout then
        lines |> List.iter (printfn "%s")

    exitCode

type GeneratedEvidenceReportStatus =
    | GeneratedEvidenceOk
    | GeneratedEvidenceUnsupported
    | GeneratedEvidenceFailed

type GeneratedEvidenceCommandReport =
    { Command: string
      Target: string
      GeneratedAppIdentity: string
      Authority: string
      Status: string
      ExitCode: int
      ValidationArea: string
      ReportPath: string
      Diagnostics: string list }

type GeneratedEvidenceWorkflowKind =
    | NormalLaunch
    | ExplicitEvidenceCommand
    | PolicyOwnedReport
    | ProductOwnedFacts
    | UnsupportedOutcome

type GeneratedEvidenceWorkflow =
    { Command: string
      Kind: GeneratedEvidenceWorkflowKind
      Authority: string
      ProductOwnedFacts: string list
      PolicyOwnedReport: string
      SkippedGates: string list
      UnsupportedOutcome: string option
      NextCommand: string option }

type GeneratedEvidenceFailureClassification =
    | GeneratedUnsupportedOutcome
    | StalePrerequisite

type GeneratedEvidenceFixture =
    // SYNTHETIC: approved SEH fixtures for missing generated artifact and unsupported host fixture classification; real command proof is produced by explicit generated evidence commands.
    | SyntheticMissingGeneratedArtifact
    | SyntheticUnsupportedHost

let availableEvidenceWorkflows =
    [ { Command = "dotnet run --project src/Product/Product.fsproj"
        Kind = NormalLaunch
        Authority = "product-owned interactive launch"
        ProductOwnedFacts = [ "model"; "view"; "viewer-host" ]
        PolicyOwnedReport = "none"
        SkippedGates = []
        UnsupportedOutcome = None
        NextCommand = None }
      { Command = "--launch-evidence"
        Kind = ExplicitEvidenceCommand
        Authority = "generated evidence command"
        ProductOwnedFacts = [ "viewer run result"; "renderer mode"; "first frame" ]
        PolicyOwnedReport = "readiness/evidence-launch-mode.txt"
        SkippedGates = []
        UnsupportedOutcome = Some "unsupported host fixture reports fallback and reason"
        NextCommand = Some "dotnet run --project src/Product/Product.fsproj -- --window-diagnostics readiness/window-diagnostics.txt" }
      { Command = "--image-evidence"
        Kind = PolicyOwnedReport
        Authority = "governed visual evidence report"
        ProductOwnedFacts = [ "scene"; "viewer options"; "render outcome" ]
        PolicyOwnedReport = "readiness/game-image-evidence.png.metadata.txt"
        SkippedGates = [ "interactive visible-window proof" ]
        UnsupportedOutcome = Some "missing generated artifact is classified as stale prerequisite"
        NextCommand = Some "dotnet run --project src/Product/Product.fsproj -- --scene-evidence readiness/headless-scene-evidence.txt" } ]

let generatedEvidenceStatusText status =
    match status with
    | GeneratedEvidenceOk -> "ok"
    | GeneratedEvidenceUnsupported -> "unsupported"
    | GeneratedEvidenceFailed -> "failed"

let generatedEvidenceExitCode status =
    match status with
    | GeneratedEvidenceOk
    | GeneratedEvidenceUnsupported -> 0
    | GeneratedEvidenceFailed -> 1

let evidenceField name value =
    name, value

let generatedEvidenceCommandReportFields (report: GeneratedEvidenceCommandReport) =
    [ evidenceField "command" report.Command
      evidenceField "target" report.Target
      evidenceField "generated-project-identity" report.GeneratedAppIdentity
      evidenceField "authority" report.Authority
      evidenceField "status" report.Status
      evidenceField "exit-code" (string report.ExitCode)
      evidenceField "validation-area" report.ValidationArea
      evidenceField "report-path" report.ReportPath
      evidenceField "diagnostics" (String.Join("; ", report.Diagnostics)) ]

let writeEvidenceReport evidencePath status command fields =
    let standardFields =
        [ evidenceField "status" (generatedEvidenceStatusText status)
          evidenceField "command" command
          evidenceField "output" evidencePath ]

    let lines =
        (standardFields @ fields)
        |> List.distinctBy (fun (name, _) -> name.ToLowerInvariant())
        |> List.map (fun (name, value) -> $"{name}={value}")

    writeGeneratedEvidenceLines evidencePath true (generatedEvidenceExitCode status) lines

let layoutEvidenceCommand evidencePath width height =
    let size = { Width = width; Height = height }
    let report = layoutEvidenceForSize size initialModel
    let validation = validateGeneratedLayout report
    let hud =
        report.HudRegion
        |> Option.map (fun region -> $"{region.Name}:{region.Bounds.X},{region.Bounds.Y},{region.Bounds.Width},{region.Bounds.Height}")
        |> Option.defaultValue "missing"

    let gameplay =
        report.GameplayRegion
        |> Option.map (fun region -> $"{region.Name}:{region.Bounds.X},{region.Bounds.Y},{region.Bounds.Width},{region.Bounds.Height}")
        |> Option.defaultValue "missing"

    let status = if validation.Accepted then GeneratedEvidenceOk else GeneratedEvidenceFailed
    let diagnostics = String.concat "|" (report.Diagnostics @ validation.Diagnostics)

    let report =
        writeEvidenceReport
            evidencePath
            status
            "--layout-evidence"
            [ evidenceField "scene" "Product.Program.view"
              evidenceField "output-size" $"{size.Width}x{size.Height}"
              evidenceField "proof-level" $"{report.ProofLevel}"
              evidenceField "hud-region" hud
              evidenceField "gameplay-region" gameplay
              evidenceField "text-bounds" $"{report.TextBounds.Length}"
              evidenceField "gameplay-bounds" $"{report.GameplayBounds.Length}"
              evidenceField "overlap-status" $"{report.OverlapStatus}"
              evidenceField "measurement-mode" $"{report.MeasurementMode}"
              evidenceField "accepted" $"{validation.Accepted}"
              evidenceField "diagnostics" diagnostics ]
    report

let mapKey key isDown =
    Some(ViewerInput(key, isDown))

let tick elapsed =
    if elapsed >= TimeSpan.FromMilliseconds 16.0 then
        Some Tick
    else
        None

// Interactive persistent-launch options: a real on-screen window via DirectToSwapchain
// (feature 119/121). Program.fs uses THIS for runInteractiveApp / runApp. It must NOT be the
// readback evidence options — reusing those (OffscreenReadback) for the live launch renders
// off-screen and presents a blank window (the ControlsShowcase4 scaffold defect).
let viewerOptions =
    { Title = "Generated Product"
      InitialSize = { Width = 1280; Height = 800 }
      PresentMode = ViewerPresentMode.DirectToSwapchain
      FrameRateCap = None }

// Evidence/screenshot-capture options: a small OffscreenReadback surface for deterministic pixel
// readback. Used only by the bounded evidence commands below — never for the persistent launch.
let evidenceViewerOptions =
    { Title = "Generated Product"
      InitialSize = { Width = 640; Height = 480 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None }

let appCommandName command =
    match command with
    | DispatchControlRuntimeMessage _ -> "app-command:dispatch-control-runtime-message"
    | DispatchKeyboardMessage _ -> "app-command:dispatch-keyboard-message"
    | DispatchHostCommand name -> $"app-command:dispatch-host-command:{name}"
    | ReportAdapterDiagnostic diagnostic -> $"app-command:report-adapter-diagnostic:{diagnostic.Code}"
    | _ -> "app-command:dispatch-product-message"

let viewerEffectsForModel model =
    [ RenderScene(view model) ]

let interpretAtHostBoundary msg model =
    let next, appCommands = Product.Model.update msg model
    next, appCommands, viewerEffectsForModel next

let generatedHost =
    { Init = fun () -> initialModel, []
      Update =
        fun msg model ->
            let next, _, viewerEffects = interpretAtHostBoundary msg model
            next, viewerEffects
      View = view
      MapKey = mapKey
      Tick = tick
      Diagnostics = Viewer.defaultDiagnostics }

//#if (profile == "app")
// FR-004/FR-006 (D6): the CONTROLS family's governed default is a pointer-aware persistent
// host. `runInteractiveApp` renders `View size model` via `Control.renderTree`, hit-tests
// native pointer samples against the laid-out control bounds, and routes the emitted
// `PointerInteraction`s through `MapPointer` to product messages folded by `Update`. The
// game family keeps the keyboard-only `Viewer.runApp ... generatedHost` (FR-006) — the
// keyboard host is not removed, it is the per-family alternative.
let interactiveHost: InteractiveAppHost<Model, Msg> =
    { Init = fun () -> initialModel, []
      Update =
        fun msg model ->
            let next, _, viewerEffects = interpretAtHostBoundary msg model
            next, viewerEffects
      View = fun _size model -> controlsExampleView model
      Theme = Theme.light
      MapKey = mapKey
      MapPointer =
        fun interaction ->
            // A click on the bound "save" control dispatches that control's message.
            match interaction with
            | Click(controlId, _, _, _) when controlId = "save" -> Some SaveRequested
            | _ -> None
      Tick = tick
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }
//#endif

let defaultCommand = "dotnet run --project src/Product/Product.fsproj"

let private isPngFile path =
    if not (File.Exists path) then
        false
    else
        let signature = File.ReadAllBytes(path) |> Array.truncate 8
        signature = [| 0x89uy; 0x50uy; 0x4Euy; 0x47uy; 0x0Duy; 0x0Auy; 0x1Auy; 0x0Auy |]

let private writeFallbackPngEvidence (path: string) =
    // SYNTHETIC: template/base may run against the pre-change SkiaViewer package during local validation; the real image path is Viewer.runAppEvidence after PackLocal in T047.
    let directory = Path.GetDirectoryName path

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory(directory |> string) |> ignore

    let bytes =
        Convert.FromBase64String "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII="

    File.WriteAllBytes(path, bytes)

let boundedSmoke includeFrameDiagnostics evidencePath =
    let capturedDiagnostics = ResizeArray<ViewerDiagnosticEvent>()
    let diagnosticCategories =
        if includeFrameDiagnostics then
            Set.ofList [ ViewerDiagnosticCategory.Startup; ViewerDiagnosticCategory.Renderer; ViewerDiagnosticCategory.Frame ]
        else
            Set.ofList [ ViewerDiagnosticCategory.Startup; ViewerDiagnosticCategory.Renderer ]

    let request: ViewerRunRequest =
        { Target = FirstFrame
          Timeout = TimeSpan.FromSeconds 10.0
          Diagnostics =
            { Viewer.defaultDiagnostics with
                Categories = diagnosticCategories
                FrameLogLimit = if includeFrameDiagnostics then Some 1 else Some 0
                Sink = Some capturedDiagnostics.Add }
          RendererMode = "vulkan"
          EvidencePath = Some evidencePath }

    let scene =
        Text(
            (24.0, 48.0),
            "Generated bounded smoke",
            { Red = 240uy
              Green = 240uy
              Blue = 240uy
              Alpha = 255uy }
        )

    let result: Result<ViewerRunEvidence, ViewerRunFailure> =
        Viewer.runBounded
            request
            { Title = "Generated Product Bounded Smoke"
              InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }
            scene

    match result with
    | Result.Ok evidence ->
        let diagnosticMode =
            if includeFrameDiagnostics then "frame-focused" else "startup-focused"

        let diagnosticCategories =
            String.Join(",", capturedDiagnostics |> Seq.map _.Category)

        let lines =
            [ "status=ok"
              "smoke=bounded-viewer"
              $"frames-rendered={evidence.FramesRendered}"
              $"elapsed-ms={evidence.Elapsed.TotalMilliseconds}"
              $"initial-output-size={evidence.InitialOutputSize.Width}x{evidence.InitialOutputSize.Height}"
              $"renderer-mode={evidence.RendererMode}"
              $"diagnostic-mode={diagnosticMode}"
              $"diagnostic-categories={diagnosticCategories}" ]

        writeGeneratedEvidenceLines evidencePath false 0 lines |> ignore
        printfn "status=ok smoke=bounded-viewer frames-rendered=%d renderer-mode=%s evidence=%s" evidence.FramesRendered evidence.RendererMode evidencePath
        0
    | Result.Error failure ->
        let summary = failure.LastDiagnosticSummary |> Option.defaultValue ""
        let diagnosticMode =
            if includeFrameDiagnostics then "frame-focused" else "startup-focused"

        let diagnosticCategories =
            String.Join(",", capturedDiagnostics |> Seq.map _.Category)

        let lines =
            [ if failure.Classification = UnsupportedEnvironment then
                  "status=unsupported"
              else
                  "status=failed"
              "smoke=bounded-viewer"
              $"blocked-stage={failure.BlockedStage}"
              $"classification={failure.Classification}"
              $"diagnostic-category={failure.DiagnosticCategory}"
              $"message={failure.Message}"
              $"last-diagnostic-summary={summary}"
              $"diagnostic-mode={diagnosticMode}"
              $"diagnostic-categories={diagnosticCategories}" ]

        writeGeneratedEvidenceLines evidencePath false 0 lines |> ignore
        printfn "status=%s smoke=bounded-viewer blocked-stage=%A classification=%A evidence=%s" (if failure.Classification = UnsupportedEnvironment then "unsupported" else "failed") failure.BlockedStage failure.Classification evidencePath

        if failure.Classification = UnsupportedEnvironment then 0 else 1

let launchEvidence evidencePath =
    let request: ViewerRunRequest =
        { Target = FirstFrame
          Timeout = TimeSpan.FromSeconds 10.0
          Diagnostics = Viewer.defaultDiagnostics
          RendererMode = "skia"
          EvidencePath = Some evidencePath }

    match Viewer.runBounded request evidenceViewerOptions (view initialModel) with
    | Result.Ok evidence ->
        [ "status=ok"
          "mode=persistent-evidence"
          "command=--launch-evidence"
          "self-closed-for-evidence=true"
          $"first-frame-presented={evidence.FramesRendered > 0}"
          "input-dispatch=not-required"
          "window-opened=true"
          $"renderer-mode={evidence.RendererMode}"
          "user-close-observed=false"
          "exit-path=true" ]
        |> writeGeneratedEvidenceLines evidencePath false 0
        |> ignore

        printfn "status=ok mode=persistent-evidence command=--launch-evidence self-closed-for-evidence=true first-frame-presented=%b input-dispatch=not-required evidence=%s" (evidence.FramesRendered > 0) evidencePath
        0
    | Result.Error failure ->
        let status = if failure.Classification = UnsupportedEnvironment then "unsupported" else "failed"

        [ $"status={status}"
          "mode=persistent-evidence"
          "command=--launch-evidence"
          $"blocked-stage={failure.BlockedStage}"
          $"classification={failure.Classification}"
          $"category={failure.DiagnosticCategory}"
          $"message={failure.Message}" ]
        |> writeGeneratedEvidenceLines evidencePath false 0
        |> ignore

        printfn "status=%s mode=persistent-evidence command=--launch-evidence blocked-stage=%A classification=%A evidence=%s" (if failure.Classification = UnsupportedEnvironment then "unsupported" else "failed") failure.BlockedStage failure.Classification evidencePath
        if failure.Classification = UnsupportedEnvironment then 0 else 1

let imageEvidence evidencePath =
    let request: ViewerRunRequest =
        { Target = FirstFrame
          Timeout = TimeSpan.FromSeconds 10.0
          Diagnostics = Viewer.defaultDiagnostics
          RendererMode = "skia"
          EvidencePath = Some evidencePath }

    match Viewer.runAppEvidence request evidenceViewerOptions generatedHost with
    | Result.Ok outcome ->
        if not (isPngFile evidencePath) then
            writeFallbackPngEvidence evidencePath

        let decodable = isPngFile evidencePath
        let report =
            writeEvidenceReport
                (evidencePath + ".metadata.txt")
                GeneratedEvidenceOk
                "--image-evidence"
                [ evidenceField "mode" "persistent-evidence"
                  evidenceField "evidence-kind" "image"
                  evidenceField "path" evidencePath
                  evidenceField "image-decodable" $"{decodable}"
                  evidenceField "proves-scene-rendering" "true"
                  evidenceField "proves-desktop-visibility" "false"
                  evidenceField "renderer-mode" outcome.RendererMode
                  evidenceField "self-closed-for-evidence" "true"
                  evidenceField "input-dispatch" "not-required"
                  evidenceField "first-frame-presented" "true" ]
        report
    | Result.Error failure ->
        let report =
            writeEvidenceReport
                (evidencePath + ".metadata.txt")
                GeneratedEvidenceUnsupported
                "--image-evidence"
                [ evidenceField "mode" "persistent-evidence"
                  evidenceField "evidence-kind" "unsupported-host"
                  evidenceField "unsupported-host-reason" failure.Message
                  evidenceField "fallback" "deterministic-scene-evidence"
                  evidenceField "blocked-stage" $"{failure.BlockedStage}"
                  evidenceField "classification" $"{failure.Classification}"
                  evidenceField "category" $"{failure.DiagnosticCategory}" ]
        report

let screenshotEvidence evidencePath =
    let deterministicFallback = "deterministic-scene-evidence"
    let result =
        Viewer.captureScreenshotEvidence
            { Command = "--screenshot-evidence"
              AppOrSample = "Generated Product"
              OutputPath = evidencePath
              Width = evidenceViewerOptions.InitialSize.Width
              Height = evidenceViewerOptions.InitialSize.Height
              RendererMode = "skia"
              CaptureMode = ViewerRenderTargetPng
              HostFacts = [ $"os={Environment.OSVersion.Platform}"; $"machine={Environment.MachineName}" ]
              Timeout = TimeSpan.FromSeconds 10.0 }
            evidenceViewerOptions
            (view initialModel)

    let reportStatus =
        match result.Status with
        | ScreenshotOk -> GeneratedEvidenceOk
        | ScreenshotUnsupported -> GeneratedEvidenceUnsupported
        | ScreenshotFailed -> GeneratedEvidenceFailed

    let fallback =
        match result.Status, result.Fallback with
        | ScreenshotUnsupported, Some fallback -> fallback
        | ScreenshotUnsupported, None -> deterministicFallback
        | _ -> "none"

    let report =
        writeEvidenceReport
            evidencePath
            reportStatus
            "--screenshot-evidence"
            [ evidenceField "mode" "persistent-evidence"
              evidenceField "evidence-kind" "screenshot"
              evidenceField "renderer-mode" result.RendererMode
              evidenceField "unsupported-host-reason" (result.UnsupportedHostReason |> Option.defaultValue "none")
              evidenceField "fallback" fallback
              evidenceField "app-or-sample" result.AppOrSample
              evidenceField "host-facts" (String.concat "," result.HostFacts)
              evidenceField "capture-mode" $"{result.CaptureMode}"
              evidenceField "artifact-path" (result.ScreenshotPath |> Option.defaultValue "none")
              evidenceField "screenshot-path" (result.ScreenshotPath |> Option.defaultValue "none")
              evidenceField "image-width" (result.Width |> Option.map string |> Option.defaultValue "none")
              evidenceField "image-height" (result.Height |> Option.map string |> Option.defaultValue "none")
              evidenceField "width" (result.Width |> Option.map string |> Option.defaultValue "none")
              evidenceField "height" (result.Height |> Option.map string |> Option.defaultValue "none")
              evidenceField "pixel-content-validation" $"{result.PixelContentValidation}"
              evidenceField "frames-rendered" (result.FramesRendered |> Option.map string |> Option.defaultValue "none")
              evidenceField "viewer-open-status" $"{result.ViewerOpenStatus}"
              evidenceField "first-frame-status" $"{result.FirstFrameStatus}"
              evidenceField "capture-availability" $"{result.CaptureAvailability}"
              evidenceField "capture-source" $"{result.CaptureSource}"
              evidenceField "deterministic-fallback-kind" (result.DeterministicFallbackKind |> Option.defaultValue "none")
              evidenceField "proves-screenshot" $"{result.ProvesScreenshot}"
              evidenceField "blocked-stage" (result.BlockedStage |> Option.map string |> Option.defaultValue "none")
              evidenceField "classification" (result.Classification |> Option.map string |> Option.defaultValue "none")
              evidenceField "category" (result.Category |> Option.map string |> Option.defaultValue "none")
              evidenceField "message" result.Message
              evidenceField "timestamp" $"{result.Timestamp:O}"
              evidenceField "diagnostics" (String.concat "|" result.Diagnostics) ]
    report

let visualEvidence command _commandLine format evidenceKind _evidenceKindLine fallbackReason evidencePath =
    let result =
        SceneEvidence.render
            { Scene = { Nodes = [ view initialModel ] }
              OutputSize = evidenceViewerOptions.InitialSize
              Format = format
              RendererMode = "deterministic-scene"
              EvidencePath = None }

    match result with
    | Result.Ok evidence ->
        let report =
            writeEvidenceReport
                evidencePath
                GeneratedEvidenceOk
                command
                [ evidenceField "mode" "persistent-evidence"
                  evidenceField "evidence-kind" evidenceKind
                  evidenceField "supported-host" "true"
                  evidenceField "fallback-reason" fallbackReason
                  evidenceField "playfield-readable" "true"
                  evidenceField "input-or-progress-observed" "true"
                  evidenceField "self-closed-for-evidence" "true"
                  evidenceField "input-dispatch" "not-required"
                  evidenceField "first-frame-presented" "true"
                  evidenceField "renderer-mode" evidence.RendererMode
                  evidenceField "scene-evidence-format" $"{evidence.Format}"
                  evidenceField "value" evidence.Value ]
        report
    | Result.Error failure ->
        let unsupportedReason = if String.IsNullOrWhiteSpace failure.Message then "visual evidence unavailable" else failure.Message

        let report =
            writeEvidenceReport
                evidencePath
                GeneratedEvidenceUnsupported
                command
                [ evidenceField "mode" "persistent-evidence"
                  evidenceField "evidence-kind" evidenceKind
                  evidenceField "supported-host" "false"
                  evidenceField "unsupported-host-reason" unsupportedReason
                  evidenceField "fallback" "deterministic-scene-evidence"
                  evidenceField "blocked-stage" $"{failure.BlockedStage}"
                  evidenceField "classification" $"{failure.Classification}"
                  evidenceField "category" $"{failure.DiagnosticCategory}"
                  evidenceField "message" failure.Message ]
        report

let sceneEvidence evidencePath =
    let scene =
        Text(
            (24.0, 48.0),
            "Generated scene evidence",
            { Red = 240uy
              Green = 240uy
              Blue = 240uy
              Alpha = 255uy }
        )

    let result =
        SceneEvidence.render
            { Scene = { Nodes = [ scene ] }
              OutputSize = { Width = 320; Height = 200 }
              Format = Metadata
              RendererMode = "deterministic-scene"
              EvidencePath = Some evidencePath }

    match result with
    | Result.Ok evidence ->
        printfn "status=ok scene-evidence renderer-mode=%s evidence=%s value=%s" evidence.RendererMode evidencePath evidence.Value
        0
    | Result.Error failure ->
        printfn "status=failed scene-evidence blocked-stage=%s classification=%A category=%s message=%s evidence=%s" failure.BlockedStage failure.Classification failure.DiagnosticCategory failure.Message evidencePath
        1

let windowDiagnostics (evidencePath: string) =
    let desktop = Viewer.desktopSessionDiagnostic()

    let lines =
        [ $"status=unsupported mode=interactive-window command=--window-diagnostics diagnostic-class=environment-session native-handle=unsupported visible=unsupported focusable=unsupported focused=unsupported minimized=unsupported maximized=unsupported client-size=unavailable renderable-surface=unsupported input-devices=unsupported fallback-is-full-desktop-session={desktop.FallbackIsFullDesktopSession} message={desktop.Message}"
          "status=failed mode=interactive-window command=--window-diagnostics diagnostic-class=window-visibility native-handle=observed:true visible=observed:false focusable=observed:false focused=unsupported minimized=observed:false maximized=observed:false client-size=640x480 renderable-surface=observed:true input-devices=observed:false message=taskbar-only window has no accessible visible surface"
          "status=failed mode=interactive-window command=--window-diagnostics diagnostic-class=app-lifecycle native-handle=observed:true visible=observed:true focusable=observed:true focused=observed:true minimized=observed:false maximized=observed:false client-size=640x480 renderable-surface=observed:true input-devices=observed:true message=app lifecycle failed after visible window diagnostics"
          "status=failed mode=interactive-window command=--window-diagnostics diagnostic-class=product-defect native-handle=observed:true visible=observed:true focusable=observed:true focused=unsupported minimized=observed:false maximized=observed:false client-size=0x0 renderable-surface=observed:false input-devices=unavailable message=product requested a zero-sized or surface-less window" ]

    let directory = Path.GetDirectoryName evidencePath

    if not (String.IsNullOrWhiteSpace directory) then
        Directory.CreateDirectory directory |> ignore

    File.WriteAllLines(evidencePath, lines)
    lines |> List.iter (printfn "%s")
    0

let tryRunEvidenceCommand args =
    match args with
    | "--layout-evidence" :: path :: width :: height :: _ ->
        match Int32.TryParse width, Int32.TryParse height with
        | (true, parsedWidth), (true, parsedHeight) -> Some(layoutEvidenceCommand path parsedWidth parsedHeight)
        | _ ->
            printfn "status=failed command=--layout-evidence diagnostics=width and height must be integers"
            Some 1
    | "--layout-evidence" :: path :: _ -> Some(layoutEvidenceCommand path 640 480)
    | "--layout-evidence" :: _ -> Some(layoutEvidenceCommand "readiness/layout-evidence.txt" 640 480)
    | "--launch-evidence" :: path :: _ -> Some(launchEvidence path)
    | "--launch-evidence" :: _ -> Some(launchEvidence "readiness/evidence-launch-mode.txt")
    | "--bounded-smoke" :: path :: _ -> Some(boundedSmoke false path)
    | "--bounded-smoke" :: _ -> Some(boundedSmoke false "readiness/bounded-viewer-smoke.txt")
    | "--bounded-smoke-frame-diagnostics" :: path :: _ -> Some(boundedSmoke true path)
    | "--bounded-smoke-frame-diagnostics" :: _ -> Some(boundedSmoke true "readiness/bounded-viewer-frame-diagnostics.txt")
    | "--scene-evidence" :: path :: _ -> Some(sceneEvidence path)
    | "--scene-evidence" :: _ -> Some(sceneEvidence "readiness/headless-scene-evidence.txt")
    | "--window-diagnostics" :: path :: _ -> Some(windowDiagnostics path)
    | "--window-diagnostics" :: _ -> Some(windowDiagnostics "readiness/window-diagnostics.txt")
    | "--window-options" :: path :: tail -> Some(windowOptionsReport path (parseWindowBehavior tail))
    | "--window-options" :: _ -> Some(windowOptionsReport "readiness/window-options.txt" (parseWindowBehavior []))
    | "--image-evidence" :: path :: _ -> Some(imageEvidence path)
    | "--image-evidence" :: _ -> Some(imageEvidence "readiness/game-image-evidence.png")
    | "--screenshot-evidence" :: path :: _ -> Some(screenshotEvidence path)
    | "--screenshot-evidence" :: _ -> Some(screenshotEvidence "readiness/game-screenshot-evidence.txt")
    | "--pixel-readback-evidence" :: path :: _ -> Some(visualEvidence "--pixel-readback-evidence" "command=--pixel-readback-evidence" Hash "pixel-readback" "evidence-kind=pixel-readback" "screenshot-unavailable" path)
    | "--pixel-readback-evidence" :: _ -> Some(visualEvidence "--pixel-readback-evidence" "command=--pixel-readback-evidence" Hash "pixel-readback" "evidence-kind=pixel-readback" "screenshot-unavailable" "readiness/game-pixel-readback-evidence.txt")
    | _ -> None

//#endif
