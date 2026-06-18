namespace FS.GG.UI.SkiaViewer.Host

#nowarn "9"
#nowarn "51"
#nowarn "3261"
#nowarn "3391"
#nowarn "44"

open System
open System.IO
open System.Runtime.InteropServices
open Elmish
open Silk.NET.Core.Contexts
open Silk.NET.Input
open Silk.NET.Maths
open Silk.NET.Windowing
open Silk.NET.OpenGL
open SkiaSharp
open FS.GG.UI.Scene
// The shared scene painter (feature 063): both this interactive host and the
// image-evidence path delegate to `SceneRenderer.paintNode`.
open FS.GG.UI.SkiaViewer
// Open the host namespace last so the host's own DiagnosticSeverity/DiagnosticStage/RenderDiagnostic
// (richer than the Scene package's) take precedence over the Scene-vocabulary names brought in above.
open FS.GG.UI.SkiaViewer.Host

module GlResources =
    type ResourceCategory =
        | GlContext
        | GlSurface
        | GrContext
        | Framebuffer
        | SkiaSurface
        | SkiaGpu

    type OwnershipState =
        | Acquired
        | Transferred
        | Released

    type OwnedResource =
        { Id: string
          Category: ResourceCategory
          AcquireStage: string
          Owner: string
          TransferPoint: string option
          ReleaseAction: string
          State: OwnershipState }

    type ReleaseRecord =
        { Id: string
          Category: ResourceCategory
          Stage: string
          Order: int }

    type ResourceLedger =
        { Owned: OwnedResource list
          Released: ReleaseRecord list }

    let empty = { Owned = []; Released = [] }

    let acquire id category acquireStage owner releaseAction ledger =
        let resource =
            { Id = id
              Category = category
              AcquireStage = acquireStage
              Owner = owner
              TransferPoint = None
              ReleaseAction = releaseAction
              State = Acquired }

        { ledger with Owned = ledger.Owned @ [ resource ] }

    let transfer id transferPoint ledger =
        let update (resource: OwnedResource) =
            if resource.Id = id && resource.State <> Released then
                { resource with
                    State = Transferred
                    TransferPoint = Some transferPoint }
            else
                resource

        { ledger with Owned = ledger.Owned |> List.map update }

    let acquired (ledger: ResourceLedger) =
        ledger.Owned
        |> List.filter (fun resource -> resource.State <> Released)

    let releaseAll stage ledger =
        let releasable = acquired ledger |> List.rev

        let records =
            releasable
            |> List.mapi (fun index resource ->
                { Id = resource.Id
                  Category = resource.Category
                  Stage = stage
                  Order = index + 1 })

        let releasedIds = records |> List.map _.Id |> Set.ofList

        let owned =
            ledger.Owned
            |> List.map (fun resource ->
                if releasedIds.Contains resource.Id then
                    { resource with State = Released }
                else
                    resource)

        { Owned = owned
          Released = ledger.Released @ records },
        records

module GlStartup =
    type StartupStage =
        { Name: string
          Order: int
          Resource: GlResources.ResourceCategory option
          DiagnosticStage: string }

    type StartupFailureCase =
        { FailedStage: StartupStage
          AcquiredBeforeFailure: GlResources.OwnedResource list
          ExpectedReleaseOrder: GlResources.ResourceCategory list
          ObservedReleaseOrder: GlResources.ResourceCategory list
          DiagnosticStage: string
          DiagnosticCause: string
          Synthetic: bool }

    let stages =
        [ { Name = "create-gl-context"
            Order = 10
            Resource = Some GlResources.GlContext
            DiagnosticStage = "GlContext" }
          { Name = "acquire-window-surface"
            Order = 20
            Resource = Some GlResources.GlSurface
            DiagnosticStage = "GlSurface" }
          { Name = "create-skia-gl-context"
            Order = 30
            Resource = Some GlResources.GrContext
            DiagnosticStage = "SkiaContext" }
          { Name = "wrap-default-framebuffer"
            Order = 40
            Resource = Some GlResources.Framebuffer
            DiagnosticStage = "Framebuffer" }
          { Name = "create-skia-surface"
            Order = 50
            Resource = Some GlResources.SkiaSurface
            DiagnosticStage = "Framebuffer" }
          { Name = "create-skia-gpu-context"
            Order = 60
            Resource = Some GlResources.SkiaGpu
            DiagnosticStage = "SkiaContext" } ]

    let stageByName name =
        stages |> List.tryFind (fun stage -> stage.Name = name)

    let releaseAction category =
        match category with
        | GlResources.GlContext -> "destroy-gl-context"
        | GlResources.GlSurface -> "release-window-surface"
        | GlResources.GrContext -> "dispose-GRContext"
        | GlResources.Framebuffer -> "release-framebuffer-target"
        | GlResources.SkiaSurface -> "dispose-SKSurface"
        | GlResources.SkiaGpu -> "dispose-GRContext"

    let acquireStage ledger stage =
        match stage.Resource with
        | None -> ledger
        | Some category ->
            GlResources.acquire
                $"{stage.Name}-resource"
                category
                stage.Name
                "GlHost.run"
                (releaseAction category)
                ledger

    let acquireBefore failedStage =
        stages
        |> List.filter (fun stage -> stage.Order < failedStage.Order)
        |> List.fold acquireStage GlResources.empty

    let simulateFailure failedStageName =
        // SYNTHETIC: symbolic resource handles force each startup failure path; real native smoke is the live GL launch under readiness/.
        let failedStage =
            stageByName failedStageName
            |> Option.defaultWith (fun () -> invalidArg (nameof failedStageName) $"Unknown startup stage: {failedStageName}")

        let ledger = acquireBefore failedStage
        let acquired = GlResources.acquired ledger
        let _, releases = GlResources.releaseAll failedStage.Name ledger

        { FailedStage = failedStage
          AcquiredBeforeFailure = acquired
          ExpectedReleaseOrder = acquired |> List.rev |> List.map _.Category
          ObservedReleaseOrder = releases |> List.map _.Category
          DiagnosticStage = failedStage.DiagnosticStage
          DiagnosticCause = $"{failedStage.Name} failed with synthetic native error"
          Synthetic = true }

    let simulateSuccessfulShutdown () =
        // SYNTHETIC: symbolic successful acquisition verifies idempotent reverse cleanup order without opening a real GL context.
        let ledger = stages |> List.fold acquireStage GlResources.empty
        let _, firstRelease = GlResources.releaseAll "shutdown" ledger
        let afterFirst, _ = GlResources.releaseAll "shutdown" ledger
        let _, secondRelease = GlResources.releaseAll "shutdown" afterFirst

        firstRelease @ secondRelease

module GlHost =
    type ScissorRect =
        { X: int
          Y: int
          Width: int
          Height: int }

    type ScissorDecision =
        | Scissored of ScissorRect list
        | FullRedraw of reason: string

    type LiveProofHostFacts =
        { Display: string option
          WaylandDisplay: string option
          SessionType: string option
          Renderer: string option
          ReadbackAvailable: bool
          PermissionGranted: bool
          TimedOut: bool }

    [<RequireQualifiedAccess>]
    type LiveProofHostReadiness =
        | Capable
        | MissingDisplay
        | MissingRenderer
        | ReadbackUnavailable
        | PermissionDenied
        | Timeout
        | HostError of string

    /// The GL/Skia framebuffer state. The render target + `SKSurface` wrap FBO 0 and are
    /// recreated on resize (FR-006), sized from the window framebuffer pixels (high-DPI/Wayland
    /// correct). Recreated, not leaked: the prior surface/target are disposed before re-wrapping.
    type FramebufferState =
        { mutable Surface: SKSurface option
          mutable RenderTarget: GRBackendRenderTarget option
          mutable Width: int
          mutable Height: int }

    type FrameSnapshot =
        { Width: int
          Height: int
          ColorType: SKColorType
          Pixels: byte[] }

    let trace configuration message =
        if configuration.Diagnostics.Verbose then
            Console.Error.WriteLine($"FS.GG.UI GlHost: {message}")

    let toNativeSize (size: Size) =
        Vector2D<int>(size.Width, size.Height)

    let drawScene scene (canvas: SKCanvas) =
        // Feature 063 (FR-001): delegate to the single shared exhaustive painter.
        scene.Nodes |> List.iter (SceneRenderer.paintNode canvas)

    // Feature 120 (US1, FR-001/002): the most recent present's per-phase durations — the scene→canvas
    // paint walk (incl. surface clear + canvas flush) vs the GL flush + buffer-swap (compose/present).
    // Live-only, non-golden; surfaced to `FrameMetrics.PaintDuration`/`ComposeDuration`.
    let mutable lastPaintDuration = System.TimeSpan.Zero
    let mutable lastComposeDuration = System.TimeSpan.Zero
    let lastPresentTiming () : System.TimeSpan * System.TimeSpan = lastPaintDuration, lastComposeDuration

    // Feature 120 (US2, FR-004/005/006): the last presented scene (for the idle-skip decision). A
    // present is skipped only when the scene is structurally unchanged AND the framebuffer size is
    // unchanged, so the double-buffered front buffer still holds the last presented frame (re-presenting
    // is "no scene work"). The first frame and any forced repaint always present.
    let mutable lastPresentedScene: Scene option = None
    let mutable skippedPresentCount = 0

    // Feature 122 (FR-001/002): the most recent fully-painted frame, cached so an idle frame can
    // re-present it onto a not-yet-filled swapchain buffer WITHOUT re-walking the scene. On a
    // multi-buffer swapchain (e.g. Wayland windowed-fullscreen) an idle frame that skipped the buffer
    // swap outright would otherwise rotate an undrawn (black) buffer into view — the interleaved-black
    // blink the Spread3 consumer reported. The bounded re-present keeps every buffer populated.
    let mutable lastGoodFrame: SKImage option = None
    let mutable idleRepresentsRemaining = 0
    let mutable representedCount = 0
    // Swapchain buffers to keep populated after a change before fully idling: 3 covers typical
    // triple-buffering on Wayland. Not a public knob — the framework fix removes the consumer-visible
    // need (FR-004 deferred).
    let [<Literal>] bufferFillDepth = 3

    /// Feature 120 (US2, FR-004/005/006): the pure present-or-skip decision — present iff this is the
    /// first frame, the scene changed, or the framebuffer size changed. Testable in isolation (T016).
    let shouldPresent (prev: Scene option) (next: Scene) (sizeChanged: bool) : bool =
        sizeChanged
        || match prev with
           | None -> true
           | Some p -> not (obj.ReferenceEquals(p, next) || p = next)

    [<RequireQualifiedAccess>]
    /// Feature 122 (FR-001/002): what the live DirectToSwapchain host does for one frame.
    type PresentAction =
        | PaintAndPresent
        | RepresentLastGood
        | SkipPresent

    /// Feature 122 (FR-001/002): the pure present decision. `PaintAndPresent` when the scene or
    /// framebuffer size changed (`shouldPresent`); otherwise `RepresentLastGood` while buffers may
    /// still be undrawn (`idleRepresentsRemaining > 0`); otherwise `SkipPresent` (full idle, the
    /// feature-120/121 no-scene-work path). Keeping every swapchain buffer populated stops a
    /// multi-buffer compositor from rotating an undrawn black buffer into view. Testable (T011).
    let planPresent (prev: Scene option) (next: Scene) (sizeChanged: bool) (idleRepresentsRemaining: int) : PresentAction =
        if shouldPresent prev next sizeChanged then PresentAction.PaintAndPresent
        elif idleRepresentsRemaining > 0 then PresentAction.RepresentLastGood
        else PresentAction.SkipPresent

    /// Feature 121 (US1, FR-002): the pure frame-pacing decision — advance (update + present) this
    /// iteration iff at least `frameInterval` seconds have elapsed since the last advance. Gates BOTH
    /// `DoUpdate` and `DoRender` so a `ViewerOptions.FrameRateCap` actually bounds render cadence, not
    /// just update. Testable in isolation (T006).
    let shouldAdvanceFrame (lastFrameTime: float) (now: float) (frameInterval: float) : bool =
        now - lastFrameTime >= frameInterval

    let normalizeScissorRects frameWidth frameHeight (rects: ScissorRect list) =
        let clamp lo hi value = min hi (max lo value)

        rects
        |> List.choose (fun rect ->
            let x0 = clamp 0 frameWidth rect.X
            let y0 = clamp 0 frameHeight rect.Y
            let x1 = clamp 0 frameWidth (rect.X + rect.Width)
            let y1 = clamp 0 frameHeight (rect.Y + rect.Height)
            let width = x1 - x0
            let height = y1 - y0

            if width <= 0 || height <= 0 then
                None
            else
                Some { X = x0; Y = y0; Width = width; Height = height })
        |> List.distinct

    let scissorArea (rects: ScissorRect list) =
        rects |> List.sumBy (fun rect -> rect.Width * rect.Height)

    let decideScissorRedraw (proof: CompositorProof.ProofReadiness) fullFrameInvalidation damage frameWidth frameHeight =
        match proof with
        | CompositorProof.ProofReadiness.Ready when fullFrameInvalidation -> FullRedraw "full-frame invalidation"
        | CompositorProof.ProofReadiness.Ready ->
            match normalizeScissorRects frameWidth frameHeight damage with
            | [] -> FullRedraw "empty damage"
            | rects -> Scissored rects
        | CompositorProof.ProofReadiness.Missing -> FullRedraw "missing present-path proof"
        | CompositorProof.ProofReadiness.Stale -> FullRedraw "stale present-path proof"
        | CompositorProof.ProofReadiness.HostMismatch -> FullRedraw "host-mismatched present-path proof"
        | CompositorProof.ProofReadiness.Failed reason -> FullRedraw $"failed present-path proof: {reason}"
        | CompositorProof.ProofReadiness.EnvironmentLimited reason -> FullRedraw $"environment-limited present-path proof: {reason}"

    let classifyLiveProofHost facts =
        if facts.TimedOut then
            LiveProofHostReadiness.Timeout
        elif not facts.PermissionGranted then
            LiveProofHostReadiness.PermissionDenied
        elif facts.Display.IsNone && facts.WaylandDisplay.IsNone then
            LiveProofHostReadiness.MissingDisplay
        elif facts.Renderer |> Option.forall String.IsNullOrWhiteSpace then
            LiveProofHostReadiness.MissingRenderer
        elif not facts.ReadbackAvailable then
            LiveProofHostReadiness.ReadbackUnavailable
        else
            LiveProofHostReadiness.Capable

    let private displayEnvironment facts =
        match facts.SessionType |> Option.map (_.Trim().ToLowerInvariant()) with
        | Some "x11" -> CompositorProof.HostDisplayEnvironment.X11
        | Some "wayland" -> CompositorProof.HostDisplayEnvironment.Wayland
        | _ when facts.Display.IsSome -> CompositorProof.HostDisplayEnvironment.X11
        | _ when facts.WaylandDisplay.IsSome -> CompositorProof.HostDisplayEnvironment.Wayland
        | _ -> CompositorProof.HostDisplayEnvironment.MissingDisplay

    let liveProofHostProfile facts : CompositorProof.HostProfile =
        let environment = displayEnvironment facts
        let environmentToken =
            match environment with
            | CompositorProof.HostDisplayEnvironment.X11 -> "x11"
            | CompositorProof.HostDisplayEnvironment.Wayland -> "wayland"
            | CompositorProof.HostDisplayEnvironment.Headless -> "headless"
            | CompositorProof.HostDisplayEnvironment.MissingDisplay -> "missing-display"
            | CompositorProof.HostDisplayEnvironment.Unknown -> "unknown"

        let renderer = facts.Renderer |> Option.filter (String.IsNullOrWhiteSpace >> not)
        let readiness =
            classifyLiveProofHost facts
            |> sprintf "%A"
            |> fun value -> value.ToLowerInvariant()

        { ProfileId = $"feature153-{environmentToken}-{readiness}"
          Backend = "OpenGL"
          Renderer = renderer
          PresentMode = ViewerPresentMode.DirectToSwapchain
          FramebufferSize = { Width = 640; Height = 480 }
          Scale = Some 1.0
          DisplayEnvironment = environment
          ProofAlgorithmVersion = CompositorProof.proofAlgorithmVersion }

    let bind result next =
        match result with
        | Ok value -> next value
        | Result.Error diagnostic -> Result.Error diagnostic

    let createWindow configuration =
        try
            let mutable options = WindowOptions.Default
            options.Title <- configuration.Title
            options.Size <- toNativeSize configuration.InitialSize
            options.IsVisible <- true
            // OpenGL core context — the backend that supports SkiaSharp's complete GL interop
            // (feature 119). The exact SKSurface-over-render-target wrap that returns null on
            // Vulkan (#1502) succeeds on GL.
            options.API <- GraphicsAPI(ContextAPI.OpenGL, ContextProfile.Core, ContextFlags.Default, APIVersion(3, 3))
            options.FramesPerSecond <- configuration.TargetFrameRate |> Option.defaultValue 60 |> float
            options.UpdatesPerSecond <- options.FramesPerSecond
            // Feature 124 (FR-001): the present path swaps buffers explicitly (renderFrameDirect /
            // representLastGoodFrame call `window.SwapBuffers()`). Silk.NET's default
            // `ShouldSwapAutomatically = true` ALSO swaps after every `DoRender()`, so each frame was
            // swapped TWICE — the second swap presenting an undefined back buffer, which a compositor
            // shows as a black flash (worse for longer paints, the GPU being mid-draw). Disabling the
            // automatic swap leaves exactly one explicit present per frame and the flicker is gone.
            options.ShouldSwapAutomatically <- false
            // Carry window-startup intent (fullscreen / maximized / windowed-fullscreen
            // / borderless) into the live window before creation.
            match configuration.ConfigureWindow with
            | Some configure -> options <- configure options
            | None -> ()
            Ok(Window.Create options)
        with ex ->
            Result.Error(Diagnostics.startupFailed GlSurface $"Silk.NET window creation failed: {ex.Message}")

    let initializeWindow (window: IWindow) =
        try
            window.Initialize()

            if window.IsInitialized then
                Ok()
            else
                Result.Error(Diagnostics.startupFailed GlSurface "Silk.NET window did not initialize.")
        with ex ->
            Result.Error(Diagnostics.startupFailed GlSurface $"Silk.NET window initialization failed: {ex.Message}")

    /// Create the Skia GL GPU context over the window's current GL context. The proc-address
    /// loader is wired from Silk.NET's context — the parameterless `GRGlInterface.Create()`
    /// returns null on Linux Mesa, so the explicit loader is mandatory (validated by the
    /// feature-119 feasibility spike).
    let createSkiaContext configuration (window: IWindow) =
        try
            // Initialise the GL API binding for this window/thread (context is current after
            // Initialize()); SkiaSharp draws through the same context via the proc loader.
            window.CreateOpenGL() |> ignore

            match box window.GLContext with
            | null -> Result.Error(Diagnostics.startupFailed GlContext "Silk.NET window exposed no GL context.")
            | _ ->
                let glContext = window.GLContext

                let getProc =
                    GRGlGetProcedureAddressDelegate(fun name ->
                        match glContext.TryGetProcAddress name with
                        | true, addr -> addr
                        | _ -> IntPtr.Zero)

                trace configuration "creating Skia GL interface"
                let glInterface = GRGlInterface.CreateOpenGl getProc

                if isNull glInterface then
                    Result.Error(Diagnostics.startupFailed GlContext "SkiaSharp could not assemble a GL interface from the active context.")
                else
                    trace configuration "creating Skia GRContext (GL)"
                    let context = GRContext.CreateGl glInterface

                    if isNull context then
                        Result.Error(Diagnostics.startupFailed SkiaContext "SkiaSharp did not create an OpenGL GPU context.")
                    else
                        Ok(context, glInterface)
        with ex ->
            Result.Error(Diagnostics.startupFailed SkiaContext $"SkiaSharp OpenGL GPU context creation failed: {ex.Message}")

    let private glRgba8 = uint32 (SKColorType.Rgba8888.ToGlSizedFormat())

    /// Ensure the FBO-0-bound `SKSurface` matches the current framebuffer pixel size, recreating
    /// it (leak-free) when the window resized (FR-006). Returns the live surface.
    let ensureFramebufferSurface configuration (window: IWindow) (context: GRContext) (framebuffer: FramebufferState) =
        let fbSize = window.FramebufferSize
        let width = max 1 fbSize.X
        let height = max 1 fbSize.Y

        match framebuffer.Surface with
        | Some surface when framebuffer.Width = width && framebuffer.Height = height -> Ok surface
        | _ ->
            framebuffer.Surface |> Option.iter (fun s -> s.Dispose())
            framebuffer.RenderTarget |> Option.iter (fun rt -> rt.Dispose())
            framebuffer.Surface <- None
            framebuffer.RenderTarget <- None

            try
                let fbInfo = GRGlFramebufferInfo(0u, glRgba8)
                let renderTarget = new GRBackendRenderTarget(width, height, 0, 8, fbInfo)
                let surface = SKSurface.Create(context, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888)

                if renderTarget.IsValid && not (isNull surface) then
                    framebuffer.RenderTarget <- Some renderTarget
                    framebuffer.Surface <- Some surface
                    framebuffer.Width <- width
                    framebuffer.Height <- height
                    trace configuration $"wrapped default framebuffer (FBO 0) at {width}x{height}"
                    Ok surface
                else
                    renderTarget.Dispose()
                    if not (isNull surface) then surface.Dispose()
                    Result.Error(Diagnostics.startupFailed Framebuffer "SkiaSharp could not wrap the window's default framebuffer (FBO 0) as an SKSurface.")
            with ex ->
                Result.Error(Diagnostics.startupFailed Framebuffer $"OpenGL framebuffer wrap failed: {ex.Message}")

    /// Offscreen render → GPU→CPU readback. Backs the on-demand evidence/screenshot routine
    /// (FR-004), independent of the live present path, and the explicit OffscreenReadback mode.
    let renderSceneToPixels configuration (context: GRContext) (width: int) (height: int) scene =
        try
            let imageInfo = SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)

            use surface =
                SKSurface.Create(context, true, imageInfo, 1, GRSurfaceOrigin.TopLeft)

            if isNull surface then
                Result.Error(Diagnostics.create Error FrameRender "SkiaSharp did not create an offscreen GL surface for scene rendering." None)
            else
                let clear =
                    configuration.ClearColor
                    |> Option.defaultValue Colors.black
                    |> SceneRenderer.skColor

                surface.Canvas.Clear clear
                drawScene scene surface.Canvas
                surface.Canvas.Flush()
                surface.Flush()
                context.Flush()
                context.Submit(true)

                let rowBytes = imageInfo.RowBytes
                let pixels = Array.zeroCreate<byte> (rowBytes * height)
                let handle = GCHandle.Alloc(pixels, GCHandleType.Pinned)

                try
                    let ok = surface.ReadPixels(imageInfo, handle.AddrOfPinnedObject(), rowBytes, 0, 0)

                    if ok then
                        Ok pixels
                    else
                        Result.Error(Diagnostics.create Error FrameRender "SkiaSharp could not read the rendered scene pixels." None)
                finally
                    handle.Free()
        with ex ->
            Result.Error(Diagnostics.create Error FrameRender "Skia scene rendering failed." (Some ex.Message))

    /// DirectToSwapchain (the GL default, FR-001/FR-007): draw the scene straight onto the
    /// FBO-0-bound `SKSurface` and present with the toolkit buffer swap — **no GPU→CPU readback**,
    /// no staging buffer, no command pool, no queue stall. Empty `Pixels` signals "no readback;
    /// capture renders on demand" (FR-004).
    let renderFrameDirect configuration (window: IWindow) (context: GRContext) (framebuffer: FramebufferState) scene =
        try
            bind (ensureFramebufferSurface configuration window context framebuffer) (fun surface ->
                let clear =
                    configuration.ClearColor
                    |> Option.defaultValue Colors.black
                    |> SceneRenderer.skColor

                // Feature 120 (US1): time the scene→canvas paint walk separately from flush + swap.
                SceneRenderer.activeReplayCache |> Option.iter PictureReplayCache.resetCounters
                let paintSw = System.Diagnostics.Stopwatch.StartNew()
                surface.Canvas.Clear clear
                drawScene scene surface.Canvas
                surface.Canvas.Flush()
                paintSw.Stop()
                // Feature 122 (FR-001): cache this fully-painted frame so idle frames can re-present it
                // onto not-yet-filled swapchain buffers without re-walking the scene.
                lastGoodFrame |> Option.iter (fun img -> img.Dispose())
                lastGoodFrame <- Some(surface.Snapshot())
                let composeSw = System.Diagnostics.Stopwatch.StartNew()
                context.Flush()
                window.SwapBuffers()
                composeSw.Stop()
                lastPaintDuration <- paintSw.Elapsed
                lastComposeDuration <- composeSw.Elapsed

                Ok
                    { Width = framebuffer.Width
                      Height = framebuffer.Height
                      ColorType = SKColorType.Rgba8888
                      Pixels = [||] })
        with ex ->
            Result.Error(Diagnostics.frameRenderFailed ex.Message)

    /// Feature 122 (FR-001/002): re-present the cached last good frame onto the current swapchain
    /// buffer — a single image blit + buffer swap, NO scene walk — so an idle frame still fills the
    /// buffer it presents and a multi-buffer compositor never rotates an undrawn (black) buffer in.
    let representLastGoodFrame configuration (window: IWindow) (context: GRContext) (framebuffer: FramebufferState) (image: SKImage) =
        try
            bind (ensureFramebufferSurface configuration window context framebuffer) (fun surface ->
                surface.Canvas.DrawImage(image, 0f, 0f)
                surface.Canvas.Flush()
                context.Flush()
                window.SwapBuffers()

                Ok
                    { Width = framebuffer.Width
                      Height = framebuffer.Height
                      ColorType = SKColorType.Rgba8888
                      Pixels = [||] })
        with ex ->
            Result.Error(Diagnostics.frameRenderFailed ex.Message)

    /// OffscreenReadback present mode on GL: display the scene on the framebuffer AND read back
    /// the rendered pixels (snapshot carries them). The readback is the explicit, opt-in path;
    /// the live default is `renderFrameDirect`.
    let renderFrameReadback configuration (window: IWindow) (context: GRContext) (framebuffer: FramebufferState) scene =
        try
            bind (ensureFramebufferSurface configuration window context framebuffer) (fun surface ->
                let clear =
                    configuration.ClearColor
                    |> Option.defaultValue Colors.black
                    |> SceneRenderer.skColor

                SceneRenderer.activeReplayCache |> Option.iter PictureReplayCache.resetCounters
                let paintSw = System.Diagnostics.Stopwatch.StartNew()
                surface.Canvas.Clear clear
                drawScene scene surface.Canvas
                surface.Canvas.Flush()
                paintSw.Stop()
                let composeSw = System.Diagnostics.Stopwatch.StartNew()
                context.Flush()
                window.SwapBuffers()
                composeSw.Stop()
                lastPaintDuration <- paintSw.Elapsed
                lastComposeDuration <- composeSw.Elapsed

                bind (renderSceneToPixels configuration context framebuffer.Width framebuffer.Height scene) (fun pixels ->
                    Ok
                        { Width = framebuffer.Width
                          Height = framebuffer.Height
                          ColorType = SKColorType.Rgba8888
                          Pixels = pixels }))
        with ex ->
            Result.Error(Diagnostics.frameRenderFailed ex.Message)

    /// Dispatch on the configured present mode. `DirectToSwapchain` is the readback-free default;
    /// `OffscreenReadback` keeps the readback path for evidence/fallback. `report` carries
    /// live-only, non-golden present diagnostics (FR-005/FR-007).
    let renderFrame configuration (window: IWindow) (context: GRContext) (framebuffer: FramebufferState) (announced: bool ref) (report: RenderDiagnostic -> unit) scene =
        // Feature 120/122 (US2 + FR-001/002): on the live DirectToSwapchain present, an unchanged scene
        // performs NO scene walk. But rather than skipping the buffer swap outright (feature 120, which
        // left an undrawn buffer to rotate in as black on a multi-buffer Wayland swapchain), the host
        // re-presents the cached last good frame — a single image blit, NO scene walk — for a bounded
        // number of idle frames until every buffer is populated, then fully idles (`SkipPresent`, the
        // byte-identical feature-120/121 no-work path). Readback present always renders.
        let fbSize = window.FramebufferSize
        let sizeChanged = framebuffer.Width <> max 1 fbSize.X || framebuffer.Height <> max 1 fbSize.Y

        let directLive =
            configuration.PresentMode = ViewerPresentMode.DirectToSwapchain
            && framebuffer.Surface.IsSome

        let action =
            if directLive then
                planPresent lastPresentedScene scene sizeChanged idleRepresentsRemaining
            else
                PresentAction.PaintAndPresent

        let idleSnapshot () =
            { Width = framebuffer.Width
              Height = framebuffer.Height
              ColorType = SKColorType.Rgba8888
              Pixels = [||] }

        match action, lastGoodFrame with
        | PresentAction.SkipPresent, _ ->
            skippedPresentCount <- skippedPresentCount + 1
            lastPaintDuration <- System.TimeSpan.Zero
            lastComposeDuration <- System.TimeSpan.Zero
            Ok(idleSnapshot ())
        | PresentAction.RepresentLastGood, Some image ->
            representedCount <- representedCount + 1
            idleRepresentsRemaining <- idleRepresentsRemaining - 1
            lastPaintDuration <- System.TimeSpan.Zero
            lastComposeDuration <- System.TimeSpan.Zero
            representLastGoodFrame configuration window context framebuffer image
        | _ ->
            // PaintAndPresent (or RepresentLastGood before any frame is cached → paint a real one).
            lastPresentedScene <- Some scene
            idleRepresentsRemaining <- bufferFillDepth - 1

            match configuration.PresentMode with
            | ViewerPresentMode.OffscreenReadback ->
                renderFrameReadback configuration window context framebuffer scene
            | ViewerPresentMode.DirectToSwapchain ->
                match renderFrameDirect configuration window context framebuffer scene with
                | Ok snapshot ->
                    // FR-007: announce the live present mode once (Category = Framebuffer via the
                    // Stage→Category mapping), non-golden.
                    if not announced.Value then
                        announced.Value <- true
                        report (Diagnostics.create Info Framebuffer "present-mode=DirectToSwapchain readback=false (live frames render straight onto the default framebuffer)." None)

                    Ok snapshot
                | Result.Error diagnostic -> Result.Error diagnostic

    let dispatchViewerEvent program dispatch event =
        program.EventMapper event
        |> Option.iter dispatch

    let addDisposable (items: ResizeArray<IDisposable>) dispose =
        items.Add
            { new IDisposable with
                member _.Dispose() = dispose () }

    let attachWindowEventMapping program (window: IWindow) onClosing dispatch =
        let disposables = ResizeArray<IDisposable>()

        let loadedHandler =
            Action(fun () -> dispatchViewerEvent program dispatch Loaded)

        window.add_Load loadedHandler
        addDisposable disposables (fun () -> window.remove_Load loadedHandler)

        let updateHandler =
            Action<float>(fun elapsedSeconds -> dispatchViewerEvent program dispatch (UpdateTick elapsedSeconds))

        window.add_Update updateHandler
        addDisposable disposables (fun () -> window.remove_Update updateHandler)

        let renderHandler =
            Action<float>(fun elapsedSeconds -> dispatchViewerEvent program dispatch (RenderTick elapsedSeconds))

        window.add_Render renderHandler
        addDisposable disposables (fun () -> window.remove_Render renderHandler)

        let resizeHandler =
            Action<Vector2D<int>>(fun size ->
                dispatchViewerEvent
                    program
                    dispatch
                    (Resized
                        { Width = size.X
                          Height = size.Y }))

        window.add_Resize resizeHandler
        addDisposable disposables (fun () -> window.remove_Resize resizeHandler)

        let closingHandler =
            Action(fun () ->
                onClosing ()
                dispatchViewerEvent program dispatch CloseRequested)

        window.add_Closing closingHandler
        addDisposable disposables (fun () -> window.remove_Closing closingHandler)

        { new IDisposable with
            member _.Dispose() =
                for disposable in Seq.rev disposables do
                    disposable.Dispose() }

    let attachInputEventMapping program (window: IWindow) dispatch =
        try
            let input = window.CreateInput()
            let disposables = ResizeArray<IDisposable>()

            addDisposable disposables (fun () -> input.Dispose())

            for keyboard in input.Keyboards do
                let keyDownHandler =
                    Action<IKeyboard, Key, int>(fun _ key _ -> dispatchViewerEvent program dispatch (KeyDown(key.ToString())))

                keyboard.add_KeyDown keyDownHandler
                addDisposable disposables (fun () -> keyboard.remove_KeyDown keyDownHandler)

                let keyUpHandler =
                    Action<IKeyboard, Key, int>(fun _ key _ -> dispatchViewerEvent program dispatch (KeyUp(key.ToString())))

                keyboard.add_KeyUp keyUpHandler
                addDisposable disposables (fun () -> keyboard.remove_KeyUp keyUpHandler)

            // 075 (FR-013): map the Silk.NET button identity to the host contract.
            let toViewerButton (button: MouseButton) =
                match button with
                | MouseButton.Left -> PrimaryButton
                | MouseButton.Right -> SecondaryButton
                | MouseButton.Middle -> MiddleButton
                | _ -> PrimaryButton

            for mouse in input.Mice do
                let pointerMoveHandler =
                    Action<IMouse, System.Numerics.Vector2>(fun _ position ->
                        dispatchViewerEvent program dispatch (PointerMoved(float position.X, float position.Y)))

                mouse.add_MouseMove pointerMoveHandler
                addDisposable disposables (fun () -> mouse.remove_MouseMove pointerMoveHandler)

                let pointerPressedHandler =
                    Action<IMouse, MouseButton>(fun mouse button ->
                        let position = mouse.Position
                        dispatchViewerEvent program dispatch (PointerPressed(float position.X, float position.Y, toViewerButton button)))

                mouse.add_MouseDown pointerPressedHandler
                addDisposable disposables (fun () -> mouse.remove_MouseDown pointerPressedHandler)

                let pointerReleasedHandler =
                    Action<IMouse, MouseButton>(fun mouse button ->
                        let position = mouse.Position
                        dispatchViewerEvent program dispatch (PointerReleased(float position.X, float position.Y, toViewerButton button)))

                mouse.add_MouseUp pointerReleasedHandler
                addDisposable disposables (fun () -> mouse.remove_MouseUp pointerReleasedHandler)

                // 075 (FR-014): mouse wheel → signed per-axis scroll delta.
                let pointerScrollHandler =
                    Action<IMouse, ScrollWheel>(fun mouse wheel ->
                        let position = mouse.Position
                        dispatchViewerEvent program dispatch (PointerScrolled(float position.X, float position.Y, float wheel.X, float wheel.Y)))

                mouse.add_Scroll pointerScrollHandler
                addDisposable disposables (fun () -> mouse.remove_Scroll pointerScrollHandler)

            // 075 (FR-007): window blur / focus-loss drives the deterministic pointer-cancel path.
            let focusChangedHandler =
                Action<bool>(fun focused ->
                    if not focused then
                        dispatchViewerEvent program dispatch PointerExited)

            window.add_FocusChanged focusChangedHandler
            addDisposable disposables (fun () -> window.remove_FocusChanged focusChangedHandler)

            Ok
                { new IDisposable with
                    member _.Dispose() =
                        for disposable in Seq.rev disposables do
                            disposable.Dispose() }
        with ex ->
            Result.Error(Diagnostics.startupFailed PlatformCheck $"Silk.NET input event mapping failed: {ex.Message}")

    let encodeSnapshot (request: ScreenshotRequest) snapshot =
        try
            let directory = Path.GetDirectoryName request.Destination

            if not (String.IsNullOrWhiteSpace directory) then
                Directory.CreateDirectory directory |> ignore

            let imageInfo =
                SKImageInfo(snapshot.Width, snapshot.Height, snapshot.ColorType, SKAlphaType.Premul)

            let handle = GCHandle.Alloc(snapshot.Pixels, GCHandleType.Pinned)

            try
                use pixmap =
                    new SKPixmap(imageInfo, handle.AddrOfPinnedObject(), imageInfo.RowBytes)

                use image = SKImage.FromPixels pixmap

                if isNull image then
                    Result.Error(Diagnostics.screenshotFailed "SkiaSharp could not create an image from the last rendered GL frame.")
                else
                    let format =
                        match request.Format with
                        | Png -> SKEncodedImageFormat.Png
                        | Jpeg -> SKEncodedImageFormat.Jpeg

                    use data = image.Encode(format, 90)

                    if isNull data then
                        Result.Error(Diagnostics.screenshotFailed "SkiaSharp could not encode the screenshot image.")
                    else
                        use stream = File.Open(request.Destination, FileMode.Create, FileAccess.Write, FileShare.None)
                        data.SaveTo stream
                        Ok()
            finally
                handle.Free()
        with ex ->
            Result.Error(Diagnostics.screenshotFailed ex.Message)

    let run program : Result<unit, RenderDiagnostic> =
        let mutable currentModel = Unchecked.defaultof<_>
        let mutable window: IWindow option = None
        let mutable windowEventMapping: IDisposable option = None
        let mutable inputEventMapping: IDisposable option = None
        let mutable activeSubscriptions: IDisposable list = []
        let mutable grContext: GRContext option = None
        let mutable glInterface: GRGlInterface option = None
        let framebuffer: FramebufferState = { Surface = None; RenderTarget = None; Width = 0; Height = 0 }
        let announced = ref false
        let mutable pendingScene: Scene option = None
        let mutable pendingScreenshots: ScreenshotRequest list = []
        let mutable renderScene: (Scene -> Result<FrameSnapshot, RenderDiagnostic>) option = None
        // Feature 118/119 (FR-004): on-demand offscreen-readback capture, decoupled from the live
        // present (the direct present path performs no readback). `lastScene` is the most recent
        // rendered scene, re-rendered offscreen on demand for screenshots/evidence.
        let mutable captureScene: (Scene -> Result<FrameSnapshot, RenderDiagnostic>) option = None
        let mutable lastScene: Scene option = None
        let mutable lastFrame: FrameSnapshot option = None
        let mutable shutdownRequested = false
        // Feature 120 (US3): the backend replay cache for this run; set as the active painter cache so
        // `CachedSubtree` boundaries record/replay. Reset the idle-skip carrier so a new run repaints.
        let replayCache = PictureReplayCache.create true
        SceneRenderer.activeReplayCache <- Some replayCache
        lastPresentedScene <- None
        skippedPresentCount <- 0
        // Feature 122 (FR-001): fresh present-buffer-fill state per run.
        lastGoodFrame |> Option.iter (fun img -> img.Dispose())
        lastGoodFrame <- None
        idleRepresentsRemaining <- 0
        representedCount <- 0

        let requestShutdown closeWindow =
            shutdownRequested <- true

            match window with
            | Some w ->
                if closeWindow && not w.IsClosing then
                    try
                        w.Close()
                    with _ ->
                        ()

                try
                    w.IsClosing <- true
                with _ ->
                    ()
            | None -> ()

        let disposeSubscriptions () =
            activeSubscriptions
            |> List.iter (fun subscription -> subscription.Dispose())

            activeSubscriptions <- []

        let rec saveScreenshot request snapshot =
            match encodeSnapshot request snapshot with
            | Ok() -> Ok()
            | Result.Error diagnostic ->
                dispatchViewerEvent program dispatch (DiagnosticReported diagnostic)
                Result.Error diagnostic

        and flushPendingScreenshots snapshot =
            let requests = pendingScreenshots
            pendingScreenshots <- []

            requests
            |> List.fold
                (fun state request ->
                    match state with
                    | Result.Error diagnostic -> Result.Error diagnostic
                    | Ok() -> saveScreenshot request snapshot)
                (Ok())

        and interpretEffect effect =
            match effect with
            | InitializeRenderer -> Ok()
            | RenderFrame scene ->
                match renderScene with
                | Some render ->
                    lastScene <- Some scene

                    match render scene with
                    | Ok snapshot ->
                        lastFrame <- Some snapshot

                        // Direct present yields no readback pixels; flush any deferred captures
                        // by rendering the scene on demand through the offscreen routine (FR-004).
                        if snapshot.Pixels.Length > 0 then
                            flushPendingScreenshots snapshot
                        else
                            match captureScene with
                            | Some capture when not (List.isEmpty pendingScreenshots) ->
                                match capture scene with
                                | Ok captureSnapshot -> flushPendingScreenshots captureSnapshot
                                | Result.Error diagnostic ->
                                    dispatchViewerEvent program dispatch (DiagnosticReported diagnostic)
                                    Result.Error diagnostic
                            | _ -> Ok()
                    | Result.Error diagnostic ->
                        dispatchViewerEvent program dispatch (DiagnosticReported diagnostic)
                        Result.Error diagnostic
                | None ->
                    pendingScene <- Some scene
                    Ok()
            | CaptureScreenshot request ->
                // On-demand capture (FR-004): prefer the readback pixels already captured by the
                // offscreen present path; otherwise (direct present mode) render the last scene on
                // demand through the offscreen readback routine — never gated on the present mode.
                let captureOnDemand () =
                    match lastScene |> Option.orElse pendingScene, captureScene with
                    | Some scene, Some capture ->
                        match capture scene with
                        | Ok snapshot ->
                            lastFrame <- Some snapshot
                            saveScreenshot request snapshot
                        | Result.Error diagnostic ->
                            dispatchViewerEvent program dispatch (DiagnosticReported diagnostic)
                            Result.Error diagnostic
                    | _ ->
                        let diagnostic =
                            Diagnostics.screenshotFailed "Screenshot capture was requested before the first successful OpenGL/Skia frame."

                        dispatchViewerEvent program dispatch (DiagnosticReported diagnostic)
                        Result.Error diagnostic

                match lastFrame with
                | Some snapshot when snapshot.Pixels.Length > 0 -> saveScreenshot request snapshot
                | _ ->
                    match pendingScene with
                    | Some _ when Option.isNone captureScene ->
                        pendingScreenshots <- pendingScreenshots @ [ request ]
                        Ok()
                    | _ -> captureOnDemand ()
            | Shutdown ->
                requestShutdown true
                disposeSubscriptions ()
                Ok()
            | ReportDiagnostic diagnostic ->
                if program.Configuration.Diagnostics.Verbose then
                    Console.Error.WriteLine($"FS.GG.UI diagnostic: {diagnostic.Stage}: {diagnostic.Message}")

                Ok()
            | Dispatch msg ->
                dispatch msg
                Ok()

        and dispatch msg =
            match program.EffectMapper msg with
            | Some effect ->
                interpretEffect effect |> ignore
            | None ->
                let nextModel, cmd = program.Update msg currentModel
                currentModel <- nextModel

                cmd
                |> List.iter (fun effect -> effect dispatch)

        let startSubscriptions () =
            disposeSubscriptions ()

            activeSubscriptions <-
                program.Subscriptions currentModel
                |> List.map (fun (_, subscribe) -> subscribe dispatch)

        let runEventLoop (createdWindow: IWindow) =
            if not shutdownRequested then
                trace program.Configuration "entering Silk.NET event loop"
                let frameInterval =
                    program.Configuration.TargetFrameRate
                    |> Option.defaultValue 60
                    |> max 1
                    |> fun frameRate -> 1.0 / float frameRate

                let stopwatch = System.Diagnostics.Stopwatch.StartNew()
                let mutable lastFrameTime = stopwatch.Elapsed.TotalSeconds

                while not createdWindow.IsClosing && not shutdownRequested do
                    createdWindow.DoEvents()

                    if shutdownRequested then
                        try
                            createdWindow.IsClosing <- true
                        with _ ->
                            ()
                    else
                        let now = stopwatch.Elapsed.TotalSeconds

                        // Feature 121 (US1, FR-002): gate BOTH update and present by the frame interval so
                        // the FrameRateCap bounds render cadence (was: DoRender every poll iteration → the
                        // free-run the ControlsShowcase4 feedback observed). Input (`DoEvents`, above) stays
                        // responsive every iteration; only the paced update+present is held back. The
                        // feature-120 paint-skip still applies inside DoRender when it does run.
                        if shouldAdvanceFrame lastFrameTime now frameInterval then
                            lastFrameTime <- now
                            createdWindow.DoUpdate()

                            if not shutdownRequested && not createdWindow.IsClosing then
                                createdWindow.DoRender()

                        Threading.Thread.Sleep(1)

        let execute () =
            try
                let initialModel, initialCmd = program.Init()
                currentModel <- initialModel
                initialCmd |> List.iter (fun effect -> effect dispatch)
                startSubscriptions ()

                bind (
                    bind (
                        bind (createWindow program.Configuration) (fun createdWindow ->
                            window <- Some createdWindow
                            windowEventMapping <- Some(attachWindowEventMapping program createdWindow (fun () -> requestShutdown false) dispatch)
                            bind (initializeWindow createdWindow) (fun () -> Ok createdWindow)))
                        (fun createdWindow ->
                            bind (attachInputEventMapping program createdWindow dispatch) (fun inputMapping ->
                                inputEventMapping <- Some inputMapping
                                Ok createdWindow)))
                    (fun createdWindow ->
                        bind (createSkiaContext program.Configuration createdWindow) (fun (context, glIface) ->
                            grContext <- Some context
                            glInterface <- Some glIface

                            // Live-only, non-golden present diagnostics (FR-005 fallback,
                            // FR-007 present-mode Info) flow over the existing diagnostic channel.
                            let report diagnostic =
                                dispatchViewerEvent program dispatch (DiagnosticReported diagnostic)

                            renderScene <-
                                Some(renderFrame program.Configuration createdWindow context framebuffer announced report)

                            // On-demand offscreen-readback capture routine (FR-004), independent of
                            // the present mode — sized from the live framebuffer.
                            captureScene <-
                                Some(fun scene ->
                                    let width = if framebuffer.Width > 0 then framebuffer.Width else max 1 createdWindow.FramebufferSize.X
                                    let height = if framebuffer.Height > 0 then framebuffer.Height else max 1 createdWindow.FramebufferSize.Y

                                    bind (renderSceneToPixels program.Configuration context width height scene) (fun pixels ->
                                        Ok
                                            { Width = width
                                              Height = height
                                              ColorType = SKColorType.Rgba8888
                                              Pixels = pixels }))

                            let scene =
                                pendingScene
                                |> Option.defaultValue (program.View currentModel)

                            pendingScene <- None
                            bind (interpretEffect (RenderFrame scene)) (fun () ->
                                runEventLoop createdWindow
                                Ok())))
            with ex ->
                Result.Error(Diagnostics.frameRenderFailed ex.Message)

        try
            execute ()
        finally
            disposeSubscriptions ()

            match inputEventMapping with
            | Some mapping -> mapping.Dispose()
            | None -> ()

            match windowEventMapping with
            | Some mapping -> mapping.Dispose()
            | None -> ()

            framebuffer.Surface |> Option.iter (fun s -> s.Dispose())
            framebuffer.RenderTarget |> Option.iter (fun rt -> rt.Dispose())

            // Feature 120 (US3, FR-013): release all resident native pictures on teardown.
            PictureReplayCache.dispose replayCache
            SceneRenderer.activeReplayCache <- None

            match grContext with
            | Some context -> context.Dispose()
            | None -> ()

            match glInterface with
            | Some glIface -> glIface.Dispose()
            | None -> ()

            match window with
            | Some w ->
                try
                    w.IsClosing <- true
                with _ ->
                    ()

                w.Dispose()
            | None -> ()
