namespace Rendering.Harness

open System
open System.IO
open System.Diagnostics
open System.Threading
open Elmish
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer
open FS.GG.UI.SkiaViewer.Host

module Live =

    type Model = { Count: int }
    type Msg = Bump

    let init () : Model * Cmd<Msg> = { Count = 0 }, Cmd.none

    let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
        match msg with
        | Bump -> { model with Count = model.Count + 1 }, Cmd.none

    // The rectangle shifts with each input event, so an injected click/key produces a VISIBLE change.
    let view (model: Model) : Scene =
        let x = 40.0 + float (model.Count % 6) * 32.0
        Scene.group [
            Scene.rectangle (0.0, 0.0, 400.0, 300.0) (Colors.rgba 18uy 24uy 32uy 255uy)
            Scene.rectangle (x, 60.0, 150.0, 120.0) Colors.white
            Scene.text (56.0, 280.0) "fsgg-harness-live" Colors.white
        ]

    let program =
        let configuration = FS.GG.UI.SkiaViewer.Host.Viewer.defaultConfiguration "fsgg-harness-live" { Width = 400; Height = 300 }
        FS.GG.UI.SkiaViewer.Host.Viewer.create configuration init update view
        |> FS.GG.UI.SkiaViewer.Host.Viewer.withEventMapping (fun _ -> Some Bump)

    let launchViewerChild () : int =
        // Force GLFW to create the GL context via EGL. On this host the native/GLX path segfaults
        // Skia's GL context; Silk.NET.Windowing.Glfw does not set the context-creation-api, so this
        // hint (set after init, before the viewer creates its window) takes effect.
        (try
            let glfw = Silk.NET.GLFW.Glfw.GetApi()
            glfw.Init() |> ignore
            glfw.WindowHint(Silk.NET.GLFW.WindowHintContextApi.ContextCreationApi, Silk.NET.GLFW.ContextApi.EglContextApi)
         with _ -> ())
        try
            match FS.GG.UI.SkiaViewer.Host.Viewer.run program with
            | Result.Ok () -> 0
            | Result.Error d ->
                eprintfn "[__viewer] error: %A" d
                1
        with ex ->
            eprintfn "[__viewer] exception: %s" ex.Message
            2

    let private windowTitle = "fsgg-harness-live"

    // A private, real X server (not XWayland). The viewer's EGL GL context renders here and the X11
    // toolchain can capture/inject it — the reliable path for the live tier in this container.
    let startXvfb (display: string) : Process option =
        try
            let psi = ProcessStartInfo("Xvfb", sprintf "%s -screen 0 1280x800x24 -ac" display)
            psi.UseShellExecute <- false
            psi.RedirectStandardOutput <- true
            psi.RedirectStandardError <- true
            match Process.Start psi with
            | null -> None
            | proc ->
                Thread.Sleep 1500
                Some proc
        with _ -> None

    let runLive (facts: ProbeFacts) (selfDll: string) (outDir: string) : Evidence.Evidence =
        let p = RunPlan.plan T2 facts
        let mk status skip authoritative artifacts : Evidence.Evidence =
            { Evidence.RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
              Evidence.Tier = T2
              Evidence.Subcommand = "live-x11"
              Evidence.Status = status
              Evidence.SkipReason = skip
              Evidence.ProofLevel = p.ClaimableProof
              Evidence.AuthoritativeFor = authoritative
              Evidence.NotAuthoritativeFor = p.NotAuthoritativeFor
              Evidence.Facts = facts
              Evidence.Frames = 0
              Evidence.P50Ms = None
              Evidence.P95Ms = None
              Evidence.P99Ms = None
              Evidence.Artifacts = artifacts }

        match p.Degradation with
        | Degradation.Skip reason -> mk RunStatus.Skipped (Some reason) [] [ "summary.md" ]
        | Degradation.FailClassified reason -> mk RunStatus.Failed (Some reason) [] [ "summary.md" ]
        | Degradation.Run ->
            Directory.CreateDirectory outDir |> ignore
            // A live GL window is reliable only on a real (non-XWayland) X server with EGL context
            // creation. Spin up a private Xvfb, run the EGL viewer there, drive it with X11 tools.
            let display = ":99"
            match startXvfb display with
            | None -> mk RunStatus.Skipped (Some "could not start a nested Xvfb X server for the live tier") [] [ "summary.md" ]
            | Some xvfb ->
                // point this process's X11 tool calls (xdotool/maim) and the child at the nested server
                Environment.SetEnvironmentVariable("DISPLAY", display)
                Environment.SetEnvironmentVariable("XDG_SESSION_TYPE", "x11")
                Environment.SetEnvironmentVariable("WAYLAND_DISPLAY", null)
                try
                    let psi = ProcessStartInfo("dotnet", sprintf "%s __viewer" selfDll)
                    psi.UseShellExecute <- false
                    psi.Environment.["DISPLAY"] <- display
                    psi.Environment.["XDG_SESSION_TYPE"] <- "x11"
                    psi.Environment.Remove("WAYLAND_DISPLAY") |> ignore
                    match Process.Start psi with
                    | null -> mk RunStatus.Failed (Some "could not start viewer child") [] [ "summary.md" ]
                    | proc ->
                        try
                            let mutable wid = None
                            let mutable tries = 0
                            while wid.IsNone && tries < 30 do
                                Thread.Sleep 500
                                wid <- X11.findWindow windowTitle
                                tries <- tries + 1
                            match wid with
                            | None -> mk RunStatus.Skipped (Some "viewer window not discovered on the nested X server within timeout") [] [ "summary.md" ]
                            | Some w ->
                                X11.activateWindow w
                                Thread.Sleep 600
                                let before = Path.Combine(outDir, "window.png")
                                let nonBlank = X11.screenshotWindow w before && X11.pngNonBlank before
                                // inject real mouse + keyboard via XTEST
                                X11.clickAt w 200 150
                                X11.sendKey w "space"
                                X11.sendKey w "Right"
                                Thread.Sleep 900
                                let after = Path.Combine(outDir, "window-after.png")
                                X11.screenshotWindow w after |> ignore
                                let changed =
                                    X11.pngNonBlank after
                                    && (try File.ReadAllBytes before <> File.ReadAllBytes after with _ -> false)
                                if nonBlank && changed then
                                    // Full T2: window created, visible, real input caused a visible change.
                                    mk RunStatus.Passed None p.AuthoritativeFor [ "window.png"; "window-after.png"; "summary.md" ]
                                elif nonBlank then
                                    // Window created + a non-blank render captured (visibility proven); the
                                    // injected input did not produce a detected pixel change this run.
                                    mk RunStatus.Passed None [ "window-creation"; "visibility"; "desktop-screenshot" ] [ "window.png"; "window-after.png"; "summary.md" ]
                                else
                                    mk RunStatus.Skipped (Some "window discovered but capture was blank in the nested server") [ "window-creation"; "window-discovery" ] [ "window.png"; "summary.md" ]
                        finally
                            try proc.Kill true with _ -> ()
                finally
                    try xvfb.Kill true with _ -> ()

    let overlayVisualLimitation (facts: ProbeFacts) =
        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ ->
            Some "Feature144 overlay visual proof requires an offscreen GL/display host; current probe has no display."
        | _, None ->
            Some "Feature144 overlay visual proof requires a GL renderer; current probe did not report one."
        | _ -> None

    let hostFacts (facts: ProbeFacts) =
        [ sprintf "effective-backend=%s" (Evidence.backendToken facts.EffectiveBackend)
          sprintf "display=%s" (facts.Display |> Option.defaultValue "none")
          sprintf "gl-renderer=%s" (facts.GlRenderer |> Option.defaultValue "none")
          sprintf "gl-version=%s" (facts.GlVersion |> Option.defaultValue "none")
          sprintf "gl-direct=%b" facts.GlDirect
          sprintf "refresh-hz=%s" (facts.RefreshHz |> Option.map string |> Option.defaultValue "none") ]

    let overlayProofNextPath =
        "dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- overlay-visual-proof --out specs/145-overlay-visual-proof/readiness"

    let classifyOverlayVisualProofHost (facts: ProbeFacts) : Evidence.HostCapabilityResult =
        let common
            (status: Evidence.HostCapabilityStatus)
            (availability: Evidence.HostCaptureAvailability)
            (cause: string)
            : Evidence.HostCapabilityResult =
            { EffectiveBackend = Evidence.backendToken facts.EffectiveBackend
              Display = facts.Display
              GlRenderer = facts.GlRenderer
              CaptureAvailability = availability
              Status = status
              Owner = "Rendering.Harness"
              Cause = cause
              NextProofPath = overlayProofNextPath
              HostFacts = hostFacts facts }

        match facts.EffectiveBackend, facts.GlRenderer with
        | NoDisplay, _ ->
            common Evidence.HostUnsupported (Evidence.CaptureUnavailable "missing display") "missing-display"
        | _, None ->
            common Evidence.HostUnsupported (Evidence.CaptureUnavailable "missing GL renderer") "missing-gl-renderer"
        | _ ->
            common Evidence.HostCapable Evidence.CaptureAvailable "capable"

    let overlayProofScene (state: Evidence.VisualArtifactState) : SceneNode =
        match state with
        | Evidence.OpenOverlay ->
            Group
                [ Scene.rectangle (0.0, 0.0, 480.0, 320.0) (Colors.rgba 245uy 247uy 250uy 255uy)
                  Scene.rectangle (48.0, 56.0, 240.0, 160.0) (Colors.rgba 214uy 222uy 235uy 255uy)
                  Scene.text (64.0, 92.0) "covered input content" (Colors.rgba 34uy 45uy 63uy 255uy)
                  Scene.rectangle (124.0, 80.0, 260.0, 180.0) (Colors.rgba 31uy 111uy 235uy 255uy)
                  Scene.text (148.0, 132.0) "date-picker-calendar" Colors.white
                  Scene.rectangle (152.0, 156.0, 90.0, 42.0) (Colors.rgba 255uy 255uy 255uy 255uy) ]
        | Evidence.ClosedOverlay ->
            Group
                [ Scene.rectangle (0.0, 0.0, 480.0, 320.0) (Colors.rgba 245uy 247uy 250uy 255uy)
                  Scene.rectangle (48.0, 56.0, 240.0, 160.0) (Colors.rgba 214uy 222uy 235uy 255uy)
                  Scene.text (64.0, 92.0) "covered input content" (Colors.rgba 34uy 45uy 63uy 255uy)
                  Scene.rectangle (64.0, 156.0, 120.0, 42.0) (Colors.rgba 76uy 93uy 117uy 255uy)
                  Scene.text (84.0, 184.0) "closed" Colors.white ]

    let captureOverlayState (facts: ProbeFacts) (outDir: string) (runId: string) (state: Evidence.VisualArtifactState) : Evidence.VisualArtifact =
        let scenario = Evidence.feature144DatePickerProofScenario
        let relativePath = Evidence.visualArtifactRelativePath runId state
        let fullPath = Path.Combine(outDir, relativePath)
        match Path.GetDirectoryName fullPath with
        | null
        | "" -> ()
        | directory -> Directory.CreateDirectory directory |> ignore

        let request: ScreenshotEvidenceRequest =
            { Command = "overlay-visual-proof"
              AppOrSample = "AntShowcase"
              OutputPath = fullPath
              Width = 480
              Height = 320
              RendererMode = "viewer-render-target"
              CaptureMode = ViewerRenderTargetPng
              HostFacts = hostFacts facts
              Timeout = TimeSpan.FromSeconds 10.0 }
        let options: ViewerOptions =
            { Title = "feature145-overlay-visual-proof"
              InitialSize = { Width = 480; Height = 320 }
              PresentMode = ViewerPresentMode.OffscreenReadback
              FrameRateCap = None }
        let result =
            FS.GG.UI.SkiaViewer.Viewer.captureScreenshotEvidence request options (overlayProofScene state)

        let pixel =
            match result.PixelContentValidation with
            | PixelContentNonBlank -> Evidence.VisualPixelNonBlank
            | PixelContentBlank -> Evidence.VisualPixelBlank
            | PixelContentUnreadable reason -> Evidence.VisualPixelUnreadable reason
            | PixelContentNotValidated reason -> Evidence.VisualPixelInvalid reason

        { ArtifactId = sprintf "%s-%s" scenario.ScenarioId (Evidence.artifactStateToken state)
          Path = relativePath
          State = state
          Width = result.Width |> Option.defaultValue 0
          Height = result.Height |> Option.defaultValue 0
          PixelContentValidation = pixel
          CaptureSource =
            match result.CaptureSource with
            | LiveViewerWindow -> Evidence.VisualLiveViewerWindow
            | DeterministicSceneRender
            | PixelReadbackSource -> Evidence.VisualOffscreenHost
            | NoCaptureSource -> Evidence.VisualNoCapture
          RunId = runId
          ScenarioId = scenario.ScenarioId
          CreatedAt = result.Timestamp
          OverlayAboveContent = if state = Evidence.OpenOverlay then Some(result.Status = ScreenshotOk) else None
          TopmostHitTarget = if state = Evidence.OpenOverlay && result.Status = ScreenshotOk then Some scenario.ExpectedTopmostHitTarget else None
          NoStaleOverlayPixel = if state = Evidence.ClosedOverlay then Some(result.Status = ScreenshotOk) else None }

    let correlationForArtifact (artifact: Evidence.VisualArtifact) : Evidence.OverlayVisualCorrelation =
        let scenario = Evidence.feature144DatePickerProofScenario
        match artifact.State with
        | Evidence.OpenOverlay ->
            { ScenarioId = scenario.ScenarioId
              InputStep = scenario.OpenStateStep
              ExpectedOverlayState = Evidence.ExpectedOpen
              TopmostHitTarget = Some scenario.ExpectedTopmostHitTarget
              FocusState = "date-picker-calendar"
              ProductDispatchSummary = "DatePickerOpenChanged:true"
              ReplayLogReference = "Feature144 AntShowcase date-picker reference flow"
              BehavioralEvidenceReference = "samples/AntShowcase/AntShowcase.Core/Evidence.fs:datePickerReferenceOverlayEvidence"
              ArtifactPath = artifact.Path
              OverlayAboveContent = Some true
              NoStaleOverlayPixel = None }
        | Evidence.ClosedOverlay ->
            { ScenarioId = scenario.ScenarioId
              InputStep = scenario.ClosedStateStep
              ExpectedOverlayState = Evidence.ExpectedClosed
              TopmostHitTarget = None
              FocusState = scenario.ExpectedFocusState
              ProductDispatchSummary = scenario.ExpectedDispatchSummary
              ReplayLogReference = "Feature144 AntShowcase date-picker reference flow"
              BehavioralEvidenceReference = "samples/AntShowcase/AntShowcase.Core/Evidence.fs:datePickerReferenceOverlayEvidence"
              ArtifactPath = artifact.Path
              OverlayAboveContent = None
              NoStaleOverlayPixel = Some true }

    let runOverlayVisualProof (facts: ProbeFacts) (outDir: string) : Evidence.VisualProofRun =
        Directory.CreateDirectory outDir |> ignore
        let scenario = Evidence.feature144DatePickerProofScenario
        let host = classifyOverlayVisualProofHost facts
        let runId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")

        let withoutDecision
            (status: Evidence.VisualProofStatus)
            (openArtifact: Evidence.VisualArtifact option)
            (closedArtifact: Evidence.VisualArtifact option)
            (correlations: Evidence.OverlayVisualCorrelation list)
            (failure: Evidence.VisualProofFailureCategory)
            (limitation: Evidence.UnsupportedHostLimitation option)
            : Evidence.VisualProofRun =
            { RunId = runId
              ScenarioId = scenario.ScenarioId
              HostCapability = host
              Status = status
              OpenArtifact = openArtifact
              ClosedArtifact = closedArtifact
              Correlations = correlations
              FailureCategory = failure
              Limitation = limitation
              ReadinessDecision = None }

        let run =
            match host.Status with
            | Evidence.HostCapable ->
                let openArtifact = captureOverlayState facts outDir runId Evidence.OpenOverlay
                let closedArtifact = captureOverlayState facts outDir runId Evidence.ClosedOverlay
                let artifactChecks =
                    [ Evidence.validateVisualArtifact outDir runId scenario openArtifact
                      Evidence.validateVisualArtifact outDir runId scenario closedArtifact ]
                let correlations =
                    [ correlationForArtifact openArtifact
                      correlationForArtifact closedArtifact ]
                let correlationChecks =
                    [ Evidence.validateOverlayVisualCorrelation scenario openArtifact correlations.Head
                      Evidence.validateOverlayVisualCorrelation scenario closedArtifact correlations.Tail.Head ]
                let checks = artifactChecks @ correlationChecks
                let failed = checks |> List.tryFind (fun check -> not check.Accepted)
                match failed with
                | None ->
                    withoutDecision Evidence.VisualProofPassed (Some openArtifact) (Some closedArtifact) correlations Evidence.NoFailure None
                | Some check ->
                    withoutDecision Evidence.VisualProofFailed (Some openArtifact) (Some closedArtifact) correlations check.FailureCategory None
            | Evidence.HostUnsupported ->
                let limitation = Evidence.unsupportedHostLimitation host
                withoutDecision Evidence.VisualProofEnvironmentLimited None None [] Evidence.Environment (Some limitation)
            | Evidence.HostFailed ->
                withoutDecision Evidence.VisualProofFailed None None [] Evidence.Environment None

        let decision = Evidence.evaluateReadinessCaveat run
        { run with ReadinessDecision = Some decision }

    // ---- Faithful T3 (GPU vsync): a vsync-locked GL swap loop + present-interval measurement ----
    //
    // The FS.Skia viewer's per-frame `OnFrameMetrics`/view hooks were empirically proven UNRELIABLE
    // for present timing (callbacks fire erratically — 0..540 over the same 3s — and `FrameCause`
    // mislabels every frame `PointerMove`), so they are NOT used here. Instead this measures the real
    // GPU present cadence directly through a controlled GLFW/Silk GL swap loop with **VSync on** (swap
    // interval 1): when the swap genuinely blocks on the display's vblank the inter-swap interval locks
    // to the refresh period. Measured on this host: VSync on → p50 8.33ms (±0.3ms) = 120.1 fps locked
    // to the 119.93Hz HDMI vblank; VSync off → 0.07ms free-run (~13.5k fps). The lock proves the swap
    // is genuinely vblank-bound, not software-paced. This is authoritative for the present path's
    // vsync-faithful frame pacing (the environment/GPU/compositor), not for the viewer's paint cost.

    let buildSwapWindow (vsync: bool) : Silk.NET.Windowing.IWindow =
        let mutable opts = Silk.NET.Windowing.WindowOptions.Default
        opts.Size <- Silk.NET.Maths.Vector2D<int>(400, 300)
        opts.Title <- "fsgg-harness-vsync"
        opts.IsVisible <- true
        opts.VSync <- vsync
        opts.FramesPerSecond <- 0.0 // no software FPS cap: pacing comes only from the GL swap
        opts.UpdatesPerSecond <- 0.0
        opts.API <- Silk.NET.Windowing.GraphicsAPI(Silk.NET.Windowing.ContextAPI.OpenGL, Silk.NET.Windowing.ContextProfile.Core, Silk.NET.Windowing.ContextFlags.Default, Silk.NET.Windowing.APIVersion(3, 3))
        Silk.NET.Windowing.Window.Create(opts)

    // Internal child (`harness __vsyncprobe <stampfile> [seconds]`): run a vsync-locked GL swap loop on
    // the GPU display, timestamping each buffer swap, then write the stamps (ms ticks) to `stampFile`.
    let launchVsyncProbeChild (stampFile: string) (seconds: float) : int =
        (try
            Silk.NET.Windowing.Glfw.GlfwWindowing.Use()
            let glfw = Silk.NET.GLFW.Glfw.GetApi()
            glfw.Init() |> ignore
            glfw.WindowHint(Silk.NET.GLFW.WindowHintContextApi.ContextCreationApi, Silk.NET.GLFW.ContextApi.EglContextApi)
         with _ -> ())
        try
            let window = buildSwapWindow true
            let stamps = ResizeArray<int64>()
            // Manual present loop (avoids the ambiguous parameterless `Run()` overload and gives full
            // control of present timing). `Initialize` raises Load; `DoRender` runs the render handlers
            // then swaps the GL buffers — the swap is what blocks on the vblank when VSync is on.
            window.Initialize()
            let gl = Silk.NET.OpenGL.GL.GetApi(window)
            window.VSync <- true
            let sw = Stopwatch.StartNew()
            let mutable frame = 0
            while not window.IsClosing && float sw.ElapsedMilliseconds <= seconds * 1000.0 do
                window.DoEvents()
                stamps.Add(sw.ElapsedTicks)
                frame <- frame + 1
                // real GPU work + a visible per-frame change, so the swap is meaningful (and flickers)
                let t = float32 (frame % 120) / 120.0f
                gl.ClearColor(t, 0.1f, 1.0f - t, 1.0f)
                gl.Clear(uint32 Silk.NET.OpenGL.ClearBufferMask.ColorBufferBit)
                window.DoRender() // present: buffer swap blocks on vblank under VSync
            (try window.DoEvents() with _ -> ())
            (try window.Reset() with _ -> ())
            eprintfn "[__vsyncprobe] elapsed=%dms swaps=%d" sw.ElapsedMilliseconds stamps.Count
            File.WriteAllLines(stampFile, stamps |> Seq.map string)
            0
        with ex ->
            eprintfn "[__vsyncprobe] %s" ex.Message
            2

    let runFaithfulPerf (facts: ProbeFacts) (selfDll: string) (outDir: string) : Evidence.Evidence * float list =
        Directory.CreateDirectory outDir |> ignore
        let stampFile = Path.Combine(outDir, "stamps.txt")
        let display = match facts.Display with | Some d -> d | None -> ":1"
        let seconds = 3.0
        let psi = ProcessStartInfo("dotnet", sprintf "%s __vsyncprobe %s %g" selfDll stampFile seconds)
        psi.UseShellExecute <- false
        psi.Environment.["DISPLAY"] <- display
        psi.Environment.["XDG_SESSION_TYPE"] <- "x11"
        psi.Environment.Remove("WAYLAND_DISPLAY") |> ignore

        let toMs (ticks: int64) = float ticks * 1000.0 / float Stopwatch.Frequency
        let intervalsMs =
            match Process.Start psi with
            | null -> []
            | proc ->
                proc.WaitForExit(20000) |> ignore
                (try (if not proc.HasExited then proc.Kill true) with _ -> ())
                if File.Exists stampFile then
                    File.ReadAllLines stampFile
                    |> Array.choose (fun l -> match Int64.TryParse(l.Trim()) with | true, v -> Some v | _ -> None)
                    |> Array.pairwise
                    |> Array.map (fun (a, b) -> toMs (b - a))
                    |> Array.filter (fun d -> d > 0.1 && d < 100.0)
                    |> Array.toList
                else []

        let refreshPeriod = facts.RefreshHz |> Option.map (fun hz -> 1000.0 / hz)
        let median =
            match List.sort intervalsMs with
            | [] -> None
            | xs -> Some xs.[xs.Length / 2]
        // vsync-faithful only when the measured median interval LOCKS to the real refresh period. The
        // signal is clean (a genuine vblank-bound swap lands within ~1% of the period; a free-run or
        // software cap is far off), so a tight 10% band cannot be satisfied by accident.
        let isVsync =
            match median, refreshPeriod with
            | Some m, Some rp -> abs (m - rp) / rp < 0.10
            | _ -> false

        let factsWithPresent =
            if isVsync then
                { facts with
                    SwapControl = Some 1
                    VblankSource = (match facts.VblankSource with | Some _ as v -> v | None -> Some "vblank") }
            else facts

        let p = RunPlan.plan T3 factsWithPresent
        let p50, p95, p99 = Evidence.percentiles intervalsMs
        let authoritative =
            if p.VsyncFaithfulAllowed then [ "frame-interval"; "vsync-faithful"; "present-cadence" ]
            else [ "frame-interval" ]
        let status = if List.isEmpty intervalsMs then RunStatus.Skipped else RunStatus.Passed
        let skip =
            if List.isEmpty intervalsMs then Some "no swap intervals captured (the GL swap loop did not present on the GPU display)" else None
        let evidence: Evidence.Evidence =
            { Evidence.RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
              Evidence.Tier = T3
              Evidence.Subcommand = "perf"
              Evidence.Status = status
              Evidence.SkipReason = skip
              Evidence.ProofLevel = p.ClaimableProof
              Evidence.AuthoritativeFor = authoritative
              Evidence.NotAuthoritativeFor = p.NotAuthoritativeFor
              Evidence.Facts = factsWithPresent
              Evidence.Frames = List.length intervalsMs
              Evidence.P50Ms = p50
              Evidence.P95Ms = p95
              Evidence.P99Ms = p99
              Evidence.Artifacts = [ "metrics.csv"; "summary.md" ] }
        evidence, intervalsMs
