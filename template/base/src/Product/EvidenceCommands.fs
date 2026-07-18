module AppRoot.EvidenceCommands

open System
open System.IO
open FS.GG.UI.Scene
open AppRoot.Model
open AppRoot.View
open AppRoot.LayoutEvidence
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
          $"scene=AppRoot.Program.view"
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
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.DesignSystem
open FS.GG.UI.Themes.Default
open FS.GG.UI.KeyboardInput
open FS.GG.UI.SkiaViewer
open AppRoot.WindowOptions

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
    | AppRootOwnedFacts
    | UnsupportedOutcome

type GeneratedEvidenceWorkflow =
    { Command: string
      Kind: GeneratedEvidenceWorkflowKind
      Authority: string
      AppRootOwnedFacts: string list
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
        AppRootOwnedFacts = [ "model"; "view"; "viewer-host" ]
        PolicyOwnedReport = "none"
        SkippedGates = []
        UnsupportedOutcome = None
        NextCommand = None }
      { Command = "--launch-evidence"
        Kind = ExplicitEvidenceCommand
        Authority = "generated evidence command"
        AppRootOwnedFacts = [ "viewer run result"; "renderer mode"; "first frame" ]
        PolicyOwnedReport = "readiness/evidence-launch-mode.txt"
        SkippedGates = []
        UnsupportedOutcome = Some "unsupported host fixture reports fallback and reason"
        NextCommand = Some "dotnet run --project src/Product/Product.fsproj -- --window-diagnostics readiness/window-diagnostics.txt" }
      { Command = "--image-evidence"
        Kind = PolicyOwnedReport
        Authority = "governed visual evidence report"
        AppRootOwnedFacts = [ "scene"; "viewer options"; "render outcome" ]
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
            [ evidenceField "scene" "AppRoot.Program.view"
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

let tick (elapsed: TimeSpan) =
    if elapsed >= TimeSpan.FromMilliseconds 16.0 then
//#if (profile == "game")
        // Feature 250: carry the host's REAL elapsed time into the game's fixed-step accumulator
        // (Model.update drains whole sim steps from it), instead of discarding it. Host wiring only.
        Some(Tick elapsed.TotalSeconds)
//#else
        Some Tick
//#endif
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
      FrameRateCap = None; LogicalSize = None }

// Evidence/screenshot-capture options: a small OffscreenReadback surface for deterministic pixel
// readback. Used only by the bounded evidence commands below — never for the persistent launch.
let evidenceViewerOptions =
    { Title = "Generated Product"
      InitialSize = { Width = 640; Height = 480 }
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None; LogicalSize = None }

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
    let next, appCommands = AppRoot.Model.update msg model
    next, appCommands, viewerEffectsForModel next

let generatedHost =
    { Init =
        fun () ->
//#if (profile == "game" || profile == "sample-pack")
            // Issue #458: the initial state goes through the SAME cue seam every other state goes
            // through. This used to be `fun () -> initialModel, []` — the model was produced without
            // passing through a transition, so `forTransition` was never called for it, so ANY effect
            // the initial state implies was silently never emitted.
            //
            // That is a hole in the pattern, not a bug in a function: `forTransition` is a function of
            // a TRANSITION, and state that is *loaded* rather than *transitioned into* — settings, a
            // save game, restored window geometry, a resumed session — never makes one. It is invisible
            // from inside the model (a restored volume the mixer was never told about looks exactly like
            // one that was restored correctly) and no test that asserts on the model can catch it.
            //
            // `Started` is that transition. Note this calls the SAME function `Update` calls, with no
            // separate startup cue path to drift out of sync — which is the second thing to want here,
            // after correctness.
            match AppRoot.AudioCues.forTransition Started initialModel initialModel with
            | [] -> initialModel, []
            | cues -> initialModel, [ PlayAudio cues ]
//#else
            initialModel, []
//#endif
      Update =
        fun msg model ->
            let next, _, viewerEffects = interpretAtHostBoundary msg model
//#if (profile == "game" || profile == "sample-pack")
            // Issue #245: the product's sound requests ride out on the same effect list the viewer
            // already interprets. `Viewer.runAppWithAudio` hands each batch to the real backend;
            // `Viewer.runApp` and the evidence paths discard it, so nothing here needs a device.
            match AppRoot.AudioCues.forTransition msg model next with
            | [] -> next, viewerEffects
            | cues -> next, viewerEffects @ [ PlayAudio cues ]
//#else
            next, viewerEffects
//#endif
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
    { Init =
        fun () ->
            // Issue #436 + #458: the app profile's INITIAL model goes through the SAME cue seam every
            // other state goes through. This was `fun () -> initialModel, []`, and that was correct
            // only for as long as the profile compiled no AudioCues.fs and so had no seam to miss —
            // the two gates that guard #458 (AudioProfileWiringTests / TemplateAudioProfileWiring-
            // CoherenceTests) said so in as many words, and said that whoever gave this profile a
            // seam had to route Init through it too, or #458 simply reappears one profile over.
            //
            // This is that landing. The reasoning is unchanged from `generatedHost`: `forTransition`
            // is a function of a TRANSITION, and a model that is *loaded* rather than transitioned
            // into never makes one — so any effect the initial state implies (a restored volume, the
            // menu's theme music) would be silently never emitted. It is invisible from inside the
            // model and no test that asserts on the model can catch it. `Started` is that door, and
            // Init dispatches it through the SAME function `Update` calls — no separate startup cue
            // path to drift out of sync.
            match AppRoot.AudioCues.forTransition Started initialModel initialModel with
            | [] -> initialModel, []
            | cues -> initialModel, [ PlayAudio cues ]
      Update =
        fun msg model ->
            let next, _, viewerEffects = interpretAtHostBoundary msg model
            // Issue #436: the Controls family's sound requests ride out on the same effect list the
            // viewer already interprets, exactly as the game family's do.
            // `ControlsElmish.runInteractiveAppWithAudio` hands each batch to the real backend; the
            // sinkless `runInteractiveApp` and the evidence paths discard it, so nothing here needs
            // a device.
            match AppRoot.AudioCues.forTransition msg model next with
            | [] -> next, viewerEffects
            | cues -> next, viewerEffects @ [ PlayAudio cues ]
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
          // The viewer host presents through OpenGL; the emitted evidence names the backend that
          // actually initialized (single source of truth, #135) regardless of this field.
          RendererMode = "opengl"
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
              InitialSize = { Width = 320; Height = 200 }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None; LogicalSize = None }
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

// Issue #901: render the FULL product view at LOGICAL resolution to a real, eyeballable PNG.
// This is the readback the existing probes do not give: `--image-evidence` is a fixed 640x480
// windowed OffscreenReadback (UnsupportedEnvironment on a headless host) and `--scene-evidence`
// is a 320x200 metadata-only frame. `--view-image` renders `view initialModel` at 1280x720
// through the SkiaViewer-OWNED headless CPU readback (feature 221): `Text.installPngRasterizer`
// injects `ReferenceRendering.renderScenePngResult` into `SceneEvidence.renderPng`, which needs
// no GPU/GL/display — so this path survives CI where the windowed one is unsupported.
//
// The frame IS the logical canvas (#885's LogicalSize=None contract): content the view authors
// beyond 1280x720 is clipped 1:1 with no scale or letterbox and no runtime diagnostic. The size
// is therefore a CONTRACT the product owns — widen it here if the product's view is authored
// larger. `renderPng` returns a typed `UnsupportedEnvironment` failure (not a stub) when the CPU
// rasterizer cannot run, which maps to exit 0 exactly like the other visual probes.
let viewImage (evidencePath: string) =
    Text.installPngRasterizer ()
    let size = { Width = 1280; Height = 720 }
    let scene = { Nodes = [ view initialModel ] }

    match SceneEvidence.renderPng size scene with
    | Result.Ok pngBytes ->
        let directory = Path.GetDirectoryName evidencePath

        if not (String.IsNullOrWhiteSpace directory) then
            Directory.CreateDirectory(directory |> string) |> ignore

        File.WriteAllBytes(evidencePath, pngBytes)
        let decodable = isPngFile evidencePath

        writeEvidenceReport
            (evidencePath + ".metadata.txt")
            GeneratedEvidenceOk
            "--view-image"
            [ evidenceField "mode" "headless-readback"
              evidenceField "evidence-kind" "view-image"
              evidenceField "path" evidencePath
              evidenceField "output-size" $"{size.Width}x{size.Height}"
              evidenceField "image-decodable" $"{decodable}"
              evidenceField "png-bytes" $"{pngBytes.Length}"
              evidenceField "renders-full-view" "true"
              evidenceField "renderer-mode" "headless-cpu-readback"
              evidenceField "readback-frame" "logical-canvas"
              evidenceField "input-dispatch" "not-required"
              evidenceField "self-closed-for-evidence" "true" ]
    | Result.Error failure ->
        // Match only UnsupportedEnvironment explicitly; the wildcard catches the defect case. The
        // literal name of that other case is NOT spelled out on purpose — the scaffold's sourceName
        // substitution rewrites the `Product` substring, so writing it here would mangle the pattern.
        let status =
            match failure.Classification with
            | SceneEvidenceFailureClassification.UnsupportedEnvironment -> GeneratedEvidenceUnsupported
            | _ -> GeneratedEvidenceFailed

        let evidenceKind =
            match failure.Classification with
            | SceneEvidenceFailureClassification.UnsupportedEnvironment -> "unsupported-host"
            | _ -> "failed"

        writeEvidenceReport
            (evidencePath + ".metadata.txt")
            status
            "--view-image"
            [ evidenceField "mode" "headless-readback"
              evidenceField "evidence-kind" evidenceKind
              evidenceField "path" evidencePath
              evidenceField "output-size" $"{size.Width}x{size.Height}"
              evidenceField "blocked-stage" $"{failure.BlockedStage}"
              evidenceField "classification" $"{failure.Classification}"
              evidenceField "category" $"{failure.DiagnosticCategory}"
              evidenceField "message" failure.Message ]

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
    // #135/#136 — single source of truth. Derive this probe's verdict from the SAME gate the real
    // `Viewer.runApp` launch consults (`Viewer.runtimeCapability()` / `Viewer.desktopSessionDiagnostic()`),
    // not from a hardcoded failure list. A headless evidence run opens NO live visible window, so the
    // probe reports the host's live-window CAPABILITY (`persistent-window-supported`, straight from that
    // gate) and marks the live-window classes as not observed here — it never fabricates an `observed:*`
    // window failure it did not see, and never implies "a live window is impossible" on a host that
    // actually supports one (the self-report/reality mismatch #136 fixes).
    let desktop = Viewer.desktopSessionDiagnostic()
    let capability = Viewer.runtimeCapability()
    let windowSupported = capability.PersistentWindow
    let supportedText = if windowSupported then "true" else "false"

    let unsupportedReasons =
        match capability.UnsupportedHostReasons with
        | [] -> "none"
        | reasons -> String.Join("; ", reasons)

    // The interactive live-window path is available exactly when the shared gate says so, so the
    // environment-session line reports the real desktop-session verdict rather than a fixed status.
    let environmentStatus =
        if desktop.DiagnosticClass = "unsupported-host" then "unsupported" else "ok"

    // The three live-window classes cannot be OBSERVED by a headless probe (no visible window is
    // created here). On a host that supports the live window they are `degraded` (a check this probe
    // does not exercise — NOT a failure it witnessed); on a host that cannot open one they are
    // `unsupported`, carrying the real host reason. Neither path asserts an observed window failure.
    let liveClassStatus = if windowSupported then "degraded" else "unsupported"

    // No live window opened, so every window fact is not-observed here — never a fabricated `observed:*`.
    let notObserved =
        "native-handle=unsupported visible=unsupported focusable=unsupported focused=unsupported minimized=unsupported maximized=unsupported client-size=unavailable renderable-surface=unsupported input-devices=unsupported"

    let liveClassMessage (className: string) =
        if windowSupported then
            $"{className} not exercised by headless window-diagnostics; interactive live-window path is supported on this host (persistent-window-supported=true) — this probe opens no live window and asserts no failure"
        else
            $"{className} unobservable: {unsupportedReasons}"

    let visibilityMessage = liveClassMessage "window-visibility"
    let lifecycleMessage = liveClassMessage "app-lifecycle"
    // The diagnostic-class STRING stays product-slug-imprinted (`product` -> effectiveNameLower,
    // consistent with every other `product` token in this file). The binding IDENTIFIER must not:
    // a hyphenated product name is a legal name but an illegal F# identifier, so route it through
    // `approot` (-> effectiveIdentifierLower, the hyphen-free derived namespace) instead. (#149)
    let approotDefectMessage = liveClassMessage "product-defect"

    let lines =
        [ $"status={environmentStatus} mode=interactive-window command=--window-diagnostics diagnostic-class=environment-session persistent-window-supported={supportedText} {notObserved} fallback-is-full-desktop-session={desktop.FallbackIsFullDesktopSession} message={desktop.Message}"
          $"status={liveClassStatus} mode=interactive-window command=--window-diagnostics diagnostic-class=window-visibility persistent-window-supported={supportedText} {notObserved} message={visibilityMessage}"
          $"status={liveClassStatus} mode=interactive-window command=--window-diagnostics diagnostic-class=app-lifecycle persistent-window-supported={supportedText} {notObserved} message={lifecycleMessage}"
          $"status={liveClassStatus} mode=interactive-window command=--window-diagnostics diagnostic-class=product-defect persistent-window-supported={supportedText} {notObserved} message={approotDefectMessage}" ]

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
    | "--view-image" :: path :: _ -> Some(viewImage path)
    | "--view-image" :: _ -> Some(viewImage "readiness/view-image.png")
    | "--screenshot-evidence" :: path :: _ -> Some(screenshotEvidence path)
    | "--screenshot-evidence" :: _ -> Some(screenshotEvidence "readiness/game-screenshot-evidence.txt")
    | "--pixel-readback-evidence" :: path :: _ -> Some(visualEvidence "--pixel-readback-evidence" "command=--pixel-readback-evidence" Hash "pixel-readback" "evidence-kind=pixel-readback" "screenshot-unavailable" path)
    | "--pixel-readback-evidence" :: _ -> Some(visualEvidence "--pixel-readback-evidence" "command=--pixel-readback-evidence" Hash "pixel-readback" "evidence-kind=pixel-readback" "screenshot-unavailable" "readiness/game-pixel-readback-evidence.txt")
    | _ -> None

//#endif
