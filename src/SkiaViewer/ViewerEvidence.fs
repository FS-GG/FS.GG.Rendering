namespace FS.GG.UI.SkiaViewer

open System
open System.IO
open System.Globalization
open FS.GG.UI.Scene
open SkiaSharp
// Re-open the package namespace AFTER the third-party opens so the Viewer types/DU-cases (e.g.
// `ViewerVisualEvidenceKind.Image`) win unqualified-name resolution over `Scene.Image` — the same
// ordering SkiaViewer.fs relies on.
open FS.GG.UI.SkiaViewer

module internal ViewerEvidence =
    let observedValueText value =
        match value with
        | ViewerObservedValue.Observed true -> "observed:true"
        | ViewerObservedValue.Observed false -> "observed:false"
        | ViewerObservedValue.Unsupported -> "unsupported"
        | ViewerObservedValue.Unavailable -> "unavailable"

    let writeEvidence (path: string) (evidence: ViewerRunEvidence) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        let summary = evidence.LastDiagnosticSummary |> Option.defaultValue ""

        let lines =
            [ $"framesRendered={evidence.FramesRendered}"
              $"elapsedMs={evidence.Elapsed.TotalMilliseconds}"
              $"initialOutputSize={evidence.InitialOutputSize.Width}x{evidence.InitialOutputSize.Height}"
              $"rendererMode={evidence.RendererMode}"
              $"lastDiagnosticSummary={summary}" ]

        IO.File.WriteAllLines(path, lines)

    let writeLaunchOutcome (path: string) (outcome: ViewerLaunchOutcome) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        let command = outcome.Command |> Option.defaultValue ""
        let blockedStage = outcome.BlockedStage |> Option.map string |> Option.defaultValue ""
        let classification = outcome.Classification |> Option.map string |> Option.defaultValue ""
        let category = outcome.Category |> Option.map string |> Option.defaultValue ""
        let closeReason = outcome.CloseReason |> Option.map string |> Option.defaultValue ""
        let failureClass = outcome.FailureClass |> Option.map string |> Option.defaultValue ""

        let lines =
            [ $"status={outcome.Status}"
              $"mode={outcome.Mode}"
              $"command={command}"
              $"renderer-mode={outcome.RendererMode}"
              $"window-opened={outcome.WindowOpened}"
              $"window-visible={observedValueText outcome.WindowVisible}"
              $"first-frame-presented={outcome.FirstFramePresented}"
              $"close-reason={closeReason}"
              $"user-close-observed={outcome.UserCloseObserved}"
              $"app-close-observed={outcome.AppCloseObserved}"
              $"evidence-close-observed={outcome.EvidenceCloseObserved}"
              $"self-closed-for-evidence={outcome.SelfClosedForEvidence}"
              $"input-dispatch={outcome.InputDispatch}"
              $"exit-path={outcome.ExitPath}"
              $"window-diagnostic-count={outcome.WindowDiagnostics.Length}"
              $"option-result-count={outcome.OptionResults.Length}"
              $"visual-evidence-count={outcome.VisualEvidence.Length}"
              $"failure-class={failureClass}"
              $"blocked-stage={blockedStage}"
              $"classification={classification}"
              $"category={category}"
              $"message={outcome.Message}" ]

        IO.File.WriteAllLines(path, lines)

    let writeLaunchFailure (path: string) mode command (failure: ViewerRunFailure) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        let summary = failure.LastDiagnosticSummary |> Option.defaultValue ""

        let lines =
            [ "status=failed"
              $"mode={mode}"
              $"command={command}"
              $"blocked-stage={failure.BlockedStage}"
              $"classification={failure.Classification}"
              $"category={failure.DiagnosticCategory}"
              $"message={failure.Message}"
              $"last-diagnostic-summary={summary}" ]

        IO.File.WriteAllLines(path, lines)

    let ensureParentDirectory (path: string) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

    let isPngPath (path: string) =
        String.Equals(IO.Path.GetExtension(path), ".png", StringComparison.OrdinalIgnoreCase)

    let imageDecodable (path: string) =
        try
            use image = SKImage.FromEncodedData(path)
            not (isNull image)
        with _ ->
            false

    let drawScreenshotScene (canvas: SKCanvas) scene =
        // Feature 063 (FR-001/002): the image-evidence path delegates to the single
        // shared exhaustive `SceneRenderer.paintNode` — the SAME painter the interactive
        // host uses. Every primitive (Line/Path/Arc/real-glyph Text/…) renders to real
        // pixels; the prior placeholder-rect wildcard that masqueraded as "scene visible"
        // is deleted.
        // Feature 136 (FR-001/T017): clear the text-fallback disclosure accumulator so the report read
        // back after this capture (via `Text.fallbackReport`) reflects exactly this page's render.
        SceneRenderer.resetFallbackEvents ()
        SceneRenderer.paintNode canvas scene

    let pngDimensionsAndNonBlank (path: string) : (int * int) option * ScreenshotPixelContentValidation =
        try
            use bitmap = SKBitmap.Decode(path)

            if Object.ReferenceEquals(bitmap, null) then
                None, PixelContentUnreadable "SkiaSharp could not decode screenshot PNG."
            else
                let mutable nonBlank = false
                let mutable y = 0

                while y < bitmap.Height && not nonBlank do
                    let mutable x = 0

                    while x < bitmap.Width && not nonBlank do
                        let pixel = bitmap.GetPixel(x, y)
                        if pixel.Alpha > 0uy then
                            nonBlank <- true
                        x <- x + 1

                    y <- y + 1

                let validation = if nonBlank then PixelContentNonBlank else PixelContentBlank
                Some(bitmap.Width, bitmap.Height), validation
        with ex ->
            None, PixelContentUnreadable ex.Message

    // Issue #885: the readback frame IS `size`, and this writer draws the scene into it 1:1 with NO
    // logical→physical mapping of its own. Any `ViewerOptions.LogicalSize` letterboxing (#246) is the
    // job of `ViewerLaunchSupport.presentedFor` in the run loops UPSTREAM — the `runBounded` path fits
    // the scene before it reaches here, the generated-app loop authors its `View` directly at
    // `InitialSize` (so the fit is inert) — and this writer performs none of it: it rasterizes whatever
    // scene it is handed into `size`. So when no `LogicalSize` fit applied, content authored beyond
    // `[0,0 .. size.Width,size.Height]` is clipped: no scale, no letterbox, no diagnostic — the raster
    // simply never touches those pixels. That is the "renders at a smaller frame than the logical
    // canvas" clip #885 names. The frame size is not inferable from the scene (no whole-scene
    // content-bounds walker exists over the `SceneNode` primitive set), so honesty here is a CONTRACT,
    // not a runtime check: pass a `size` that covers the authored content, or set `LogicalSize` upstream
    // so a larger canvas is fit into the frame. The capture frame is recorded in the evidence — the
    // run-evidence writer emits `initialOutputSize=WxH` (see `writeEvidence`) and the written PNG's own
    // dimensions are read back by `pngDimensionsAndNonBlank` — so a reader can compare the capture frame
    // against the canvas the product authored in.
    let writeSceneImageEvidence path (size: FS.GG.UI.Scene.Size) scene =
        ensureParentDirectory path
        let width = max 1 size.Width
        let height = max 1 size.Height

        use bitmap = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)
        use canvas = new SKCanvas(bitmap)
        canvas.Clear(SKColors.Transparent)
        drawScreenshotScene canvas scene

        use image = SKImage.FromBitmap(bitmap)
        use data = image.Encode(SKEncodedImageFormat.Png, 90)

        if isNull data then
            false
        else
            IO.File.WriteAllBytes(path, data.ToArray())

            imageDecodable path

    let writeTextEvidence path lines =
        ensureParentDirectory path
        IO.File.WriteAllLines(path, lines |> List.toArray)

    // Issue #444: `WriteVisualEvidence` carries an already-formed artifact record — the product (or a
    // readiness workflow) decided what the evidence says. So this serializes the record verbatim in the
    // same kebab-key text shape the bounded path's evidence uses, rather than re-deriving a verdict the
    // caller has already reached. `Path` is the artifact's own subject (the image it describes), which
    // is NOT the path being written here, so both are recorded.
    let writeVisualEvidenceArtifact (path: string) (artifact: ViewerVisualEvidenceArtifact) =
        // Lower-cased deliberately: `$"{bool}"` renders .NET's `True`/`False`, but every other evidence
        // line this module writes spells these `true`/`false` (see `visualEvidenceArtifacts`), and a
        // reader that greps `proves-scene-rendering=true` must not miss it on case alone.
        let flag value = if value then "true" else "false"
        let subject = artifact.Path |> Option.defaultValue ""
        let decodable = artifact.ImageDecodable |> Option.map flag |> Option.defaultValue ""

        writeTextEvidence
            path
            [ $"kind={artifact.Kind}"
              $"path={subject}"
              $"image-decodable={decodable}"
              $"proves-scene-rendering={flag artifact.ProvesSceneRendering}"
              $"proves-desktop-visibility={flag artifact.ProvesDesktopVisibility}"
              $"message={artifact.Message}" ]

    let sceneFromNode node =
        { Nodes = [ node ] }

    // Feature 105 (US3, FR-009): the closed set of renderer-mode dispatch values, parsed ONCE at
    // the edge so the case-insensitive comparison is an exhaustive DU match instead of a chain of
    // string equalities. Every public `RendererMode` output/serialized field stays an unchanged
    // string; this DU types only the internal dispatch. An unrecognized mode parses to `Default`
    // (the prior string-comparison fallthrough), preserving behaviour exactly. Hidden from
    // consumers by absence from SkiaViewer.fsi.
    [<RequireQualifiedAccess>]
    type RendererModeKind =
        | Default
        | Skia
        | DeterministicScene
        | UnsupportedHost
        | MetadataHash
        | PixelReadback

    let parseRendererMode (mode: string) : RendererModeKind =
        if String.Equals(mode, "unsupported-host", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.UnsupportedHost
        elif String.Equals(mode, "metadata-hash", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.MetadataHash
        elif String.Equals(mode, "pixel-readback", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.PixelReadback
        elif String.Equals(mode, "skia", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.Skia
        elif String.Equals(mode, "deterministic-scene", StringComparison.OrdinalIgnoreCase) then
            RendererModeKind.DeterministicScene
        else
            RendererModeKind.Default

    let visualEvidenceArtifacts (request: ViewerRunRequest) (options: ViewerOptions) (scene: SceneNode) =
        match request.EvidencePath with
        | None -> []
        | Some path ->
            match parseRendererMode request.RendererMode with
            | RendererModeKind.UnsupportedHost ->
                [ { Kind = UnsupportedHost
                    Path = None
                    ImageDecodable = None
                    ProvesSceneRendering = false
                    ProvesDesktopVisibility = false
                    Message = "Visual evidence is unsupported on this host." } ]
            | RendererModeKind.MetadataHash ->
                match SceneEvidence.renderHash options.InitialSize (sceneFromNode scene) with
                | Result.Ok evidence ->
                    writeTextEvidence
                        path
                        [ "evidence-kind=metadata-hash"
                          $"path={path}"
                          $"hash={evidence.Value}"
                          "proves-scene-rendering=false"
                          "proves-desktop-visibility=false" ]

                    [ { Kind = MetadataHash
                        Path = Some path
                        ImageDecodable = None
                        ProvesSceneRendering = false
                        ProvesDesktopVisibility = false
                        Message = "Metadata/hash evidence is labeled separately from image evidence." } ]
                | Result.Error failure ->
                    [ { Kind = UnsupportedHost
                        Path = None
                        ImageDecodable = None
                        ProvesSceneRendering = false
                        ProvesDesktopVisibility = false
                        Message = failure.Message } ]
            | RendererModeKind.PixelReadback ->
                match SceneEvidence.renderHash options.InitialSize (sceneFromNode scene) with
                | Result.Ok evidence ->
                    writeTextEvidence
                        path
                        [ "evidence-kind=pixel-readback"
                          $"path={path}"
                          "fallback-reason=screenshot-unavailable"
                          $"hash={evidence.Value}"
                          "proves-scene-rendering=true"
                          "proves-desktop-visibility=false" ]

                    [ { Kind = PixelReadback
                        Path = Some path
                        ImageDecodable = None
                        ProvesSceneRendering = true
                        ProvesDesktopVisibility = false
                        Message = "Pixel-readback fallback proves scene rendering but not desktop visibility." } ]
                | Result.Error failure ->
                    [ { Kind = UnsupportedHost
                        Path = None
                        ImageDecodable = None
                        ProvesSceneRendering = false
                        ProvesDesktopVisibility = false
                        Message = failure.Message } ]
            | RendererModeKind.Default
            | RendererModeKind.Skia
            | RendererModeKind.DeterministicScene ->
                if isPngPath path then
                    let decodable = writeSceneImageEvidence path options.InitialSize scene

                    [ { Kind = Image
                        Path = Some path
                        ImageDecodable = Some decodable
                        ProvesSceneRendering = decodable
                        ProvesDesktopVisibility = false
                        Message =
                            if decodable then
                                "Image evidence is a decodable scene-rendering artifact; desktop visibility remains a separate claim."
                            else
                                "Image evidence was requested but SkiaSharp could not write a decodable PNG artifact." } ]
                else
                    match SceneEvidence.renderHash options.InitialSize (sceneFromNode scene) with
                    | Result.Ok evidence ->
                        writeTextEvidence
                            path
                            [ "evidence-kind=metadata-hash"
                              $"path={path}"
                              $"hash={evidence.Value}"
                              "proves-scene-rendering=false"
                              "proves-desktop-visibility=false" ]

                        [ { Kind = MetadataHash
                            Path = Some path
                            ImageDecodable = None
                            ProvesSceneRendering = false
                            ProvesDesktopVisibility = false
                            Message = "Non-image evidence path is recorded as metadata/hash evidence." } ]
                    | Result.Error failure ->
                        [ { Kind = UnsupportedHost
                            Path = None
                            ImageDecodable = None
                            ProvesSceneRendering = false
                            ProvesDesktopVisibility = false
                            Message = failure.Message } ]

