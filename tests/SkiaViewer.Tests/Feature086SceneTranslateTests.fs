module Feature086SceneTranslateTests

// Feature 086 US5 (FR-013/014) — `Scene.translate` offsets EVERY descendant kind
// uniformly (proven on a Path scene, via the real shared painter onto a raster
// SKBitmap), nesting composes additively, and `Scene.sizedText` renders glyphs at an
// explicit size while a bare `Text` keeps its default-font rendering. Failing-first
// against a framework that lacks the Translate/SizedText primitives.

open System
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private renderToPng (width: int) (height: int) (scene: SceneNode) =
    let dir = IO.Path.Combine(IO.Path.GetTempPath(), "fs086-translate")
    IO.Directory.CreateDirectory dir |> ignore
    let path = IO.Path.Combine(dir, $"scene-{Guid.NewGuid():N}.png")

    let request: ScreenshotEvidenceRequest =
        { Command = "screenshot"
          AppOrSample = "feature086"
          OutputPath = path
          Width = width
          Height = height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 5.0 }

    let options: ViewerOptions =
        { Title = "feature086"; InitialSize = { Width = width; Height = height }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

    let result = Viewer.captureScreenshotEvidence request options scene
    result, path

let private litPoints (path: string) =
    use bitmap = SKBitmap.Decode path
    Expect.isNotNull bitmap "screenshot PNG decodes"
    [ for x in 0 .. bitmap.Width - 1 do
          for y in 0 .. bitmap.Height - 1 do
              if bitmap.GetPixel(x, y).Alpha > 0uy then
                  yield x, y ]

let private bbox (pts: (int * int) list) =
    let xs = pts |> List.map fst
    let ys = pts |> List.map snd
    List.min xs, List.min ys, List.max xs, List.max ys

// A diagonal triangle Path well inside a 200x200 canvas (exercises a non-rectangular kind).
let private trianglePath =
    let spec =
        Path.create
            Winding
            [ Path.moveTo 40.0 40.0
              Path.lineTo 80.0 40.0
              Path.lineTo 60.0 80.0
              Path.close ]

    Path(spec, Paint.fill (Colors.rgb 220uy 80uy 60uy))

/// Tier-T1 raster capability probe (mirrors Audit_ReplayCache, feature 203 US4): `SKSurface.Create`
/// returns null (does not throw) when the native raster backend is unavailable on a headless host.
let private rasterAvailable: bool =
    try
        use s = SKSurface.Create(SKImageInfo(8, 8))
        not (isNull s)
    with _ -> false

/// Run the raster body when the surface tier is present; otherwise record a deterministic skip-with-tier
/// (Constitution VI) — never an intermittent red, never a faked pass. A genuine defect on a raster-capable
/// host still fails loudly inside `body`.
let private withRaster (what: string) (body: unit -> unit) =
    if rasterAvailable then body ()
    else
        skiptest (
            sprintf
                "SKIPPED(tier=T1 raster/pixel GL): offscreen SKSurface unavailable on this host (SkiaSharp native/headless) — %s requires the raster/pixel render tier; recorded skipped-with-tier, not a pass (Constitution VI)."
                what)

// Sequenced (feature 203, US4/T024): renders through the shared, single-threaded SceneRenderer.
[<Tests>]
let feature086SceneTranslateTests =
    testSequenced
    <| testList "Feature 086 Scene.translate / sizedText (US5)" [

        test "translate shifts every painted pixel of a Path sub-scene by exactly (dx,dy) (FR-013)" {
          withRaster "translate pixel-shift proof" (fun () ->
            let baseScene = trianglePath
            let dx, dy = 50, 30
            let translated = Translate((float dx, float dy), { Nodes = [ baseScene ] })

            let _, basePath = renderToPng 200 200 baseScene
            let _, shiftedPath = renderToPng 200 200 translated

            let bMinX, bMinY, bMaxX, bMaxY = bbox (litPoints basePath)
            let sMinX, sMinY, sMaxX, sMaxY = bbox (litPoints shiftedPath)

            Expect.equal (sMinX - bMinX) dx "left edge shifts by dx"
            Expect.equal (sMaxX - bMaxX) dx "right edge shifts by dx"
            Expect.equal (sMinY - bMinY) dy "top edge shifts by dy"
            Expect.equal (sMaxY - bMaxY) dy "bottom edge shifts by dy")
        }

        test "nested translate composes additively (sum of offsets) (FR-013)" {
          withRaster "nested translate additivity" (fun () ->
            let nested =
                Translate((20.0, 0.0), { Nodes = [ Translate((35.0, 0.0), { Nodes = [ trianglePath ] }) ] })

            let combined = Translate((55.0, 0.0), { Nodes = [ trianglePath ] })

            let _, nestedPath = renderToPng 200 200 nested
            let _, combinedPath = renderToPng 200 200 combined

            Expect.equal (bbox (litPoints nestedPath)) (bbox (litPoints combinedPath)) "translate a then b == translate (a+b)")
        }

        test "sizedText renders larger glyphs at a larger explicit size; bare Text still renders (FR-014)" {
          withRaster "sizedText glyph extent" (fun () ->
            let small = SizedText((40.0, 90.0), "WWWW", 12.0, Colors.white)
            let large = SizedText((40.0, 90.0), "WWWW", 36.0, Colors.white)
            let bare = Text((40.0, 90.0), "WWWW", Colors.white)

            let _, smallPath = renderToPng 320 160 small
            let _, largePath = renderToPng 320 160 large
            let _, barePath = renderToPng 320 160 bare

            let smallPts = litPoints smallPath
            let largePts = litPoints largePath
            let barePts = litPoints barePath

            Expect.isNonEmpty smallPts "small sized text renders pixels"
            Expect.isNonEmpty barePts "bare Text still renders pixels (back-compat)"

            let width (pts: (int * int) list) =
                let minX, _, maxX, _ = bbox pts
                maxX - minX

            Expect.isGreaterThan (width largePts) (width smallPts) "a larger explicit size yields wider glyph extent")
        }

        test "sizedText is structurally a SizedTextElement; translate wraps as TranslateElement" {
            Expect.contains (Scene.describe (Scene.sizedText (10.0, 10.0) "hi" 14.0 Colors.white)) SizedTextElement "sizedText describes as SizedTextElement"
            let wrapped = Scene.translate 5.0 5.0 (Scene.rectangle (0.0, 0.0, 4.0, 4.0) Colors.white)
            Expect.contains (Scene.describe wrapped) TranslateElement "translate describes as TranslateElement"
        }
    ]
