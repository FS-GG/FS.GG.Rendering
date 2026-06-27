module Feature140GlyphRunRenderingTests

open System
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private font: FontSpec = { Family = None; Size = 18.0; Weight = None }

let private renderToPng (width: int) (height: int) (scene: SceneNode) =
    let dir = IO.Path.Combine(IO.Path.GetTempPath(), "fs140-glyph-run")
    IO.Directory.CreateDirectory dir |> ignore
    let path = IO.Path.Combine(dir, $"scene-{Guid.NewGuid():N}.png")

    let request: ScreenshotEvidenceRequest =
        { Command = "screenshot"
          AppOrSample = "feature140"
          OutputPath = path
          Width = width
          Height = height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 5.0 }

    let options: ViewerOptions =
        { Title = "feature140"; InitialSize = { Width = width; Height = height }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

    Viewer.captureScreenshotEvidence request options scene, path

/// Tier-T1 raster capability probe (mirrors Audit_ReplayCache, feature 203 US4): `SKSurface.Create`
/// returns null (does not throw) when the native raster backend is unavailable on a headless host.
let private rasterAvailable: bool =
    try
        use s = SKSurface.Create(SKImageInfo(8, 8))
        not (isNull s)
    with _ -> false

/// Run the raster body when the surface tier is present; otherwise record a deterministic skip-with-tier
/// (Constitution VI) — never an intermittent red, never a faked pass. A genuine defect on a raster-capable
/// host still fails loudly inside `body`. (The measurement/structural tests need no surface and are not
/// guarded.)
let private withRaster (what: string) (body: unit -> unit) =
    if rasterAvailable then body ()
    else
        skiptest (
            sprintf
                "SKIPPED(tier=T1 raster/pixel GL): offscreen SKSurface unavailable on this host (SkiaSharp native/headless) — %s requires the raster/pixel render tier; recorded skipped-with-tier, not a pass (Constitution VI)."
                what)

let private litPixelCount (path: string) =
    use bitmap = SKBitmap.Decode path
    Expect.isNotNull bitmap "screenshot PNG decodes"

    seq {
        for x in 0 .. bitmap.Width - 1 do
            for y in 0 .. bitmap.Height - 1 do
                if bitmap.GetPixel(x, y).Alpha > 0uy then
                    yield 1
    }
    |> Seq.length

// Sequenced (feature 203, US4/T024): renders through the shared, single-threaded SceneRenderer.
[<Tests>]
let tests =
    testSequenced
    <| testList
        "Feature140 glyph-run rendering proof"
        [ test "bundled-font glyph-run proof measures the same advance it draws" {
              let data = Fonts.buildGlyphRunData "Stable" font
              let real = Fonts.realMeasure "Stable" font

              Expect.floatClose Accuracy.medium data.Metrics.Advance real.Width "glyph-run proof advance comes from the bundled-font renderer"
              Expect.floatClose Accuracy.medium (data.Glyphs |> List.sumBy _.Advance) data.Metrics.Advance "glyph advances sum to the draw advance"
              Expect.isNonEmpty data.Fingerprint "real proof has a stable fingerprint"
          }

          test "glyph-run proof paints visible pixels through the shared SceneRenderer" {
              withRaster "glyph-run pixel proof" (fun () ->
              let data = Fonts.buildGlyphRunData "GlyphRun" font
              let scene = Scene.glyphRun { X = 24.0; Y = 48.0 } data (Paint.fill Colors.white)
              let result, path = renderToPng 220 90 (Group [ scene ])

              Expect.equal result.Status ScreenshotOk "image evidence captured"
              Expect.isGreaterThan (litPixelCount path) 0 "glyph-run proof renders visible pixels")
          }

          test "non-opt-in text fallback compatibility is unchanged" {
              let legacy = Scene.textAt { X = 20.0; Y = 40.0 } "Legacy" Colors.white
              let proof = Scene.glyphRun { X = 20.0; Y = 40.0 } (Fonts.buildGlyphRunData "Legacy" font) (Paint.fill Colors.white)

              Expect.contains (Scene.describe legacy) TextElement "legacy text remains TextElement"
              Expect.contains (Scene.describe proof) GlyphRunElement "proof node is explicit opt-in"
          } ]
