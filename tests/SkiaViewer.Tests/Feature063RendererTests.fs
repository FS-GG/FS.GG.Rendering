module Feature063RendererTests

// Feature 063 (FR-001/002): the image-evidence renderer must faithfully draw every
// scene primitive. These tests drive the *public* image-evidence entry point
// (`Viewer.captureScreenshotEvidence`, which renders onto a raster `SKBitmap` with no
// window) and inspect the decoded PNG. They are failing-first against the pre-fix
// renderer, whose `drawScreenshotScene` routed `Line`/`Path`/… to a single 40x40 teal
// placeholder rect at (8,8)-(48,48) and drew `Text` as a solid filled block.

open System
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// The pre-fix placeholder occupied (8,8)-(48,48). A primitive drawn well to the right of
// that box leaves pixels at x >= 100 only when it is *actually* rendered, so "lit pixels
// exist at x >= 100" is the discriminating visual proof.
let private placeholderRightEdge = 48

let private renderToPng (width: int) (height: int) (scene: SceneNode) =
    let dir = IO.Path.Combine(IO.Path.GetTempPath(), "fs063-renderer")
    IO.Directory.CreateDirectory dir |> ignore
    let path = IO.Path.Combine(dir, $"scene-{Guid.NewGuid():N}.png")

    let request: ScreenshotEvidenceRequest =
        { Command = "screenshot"
          AppOrSample = "feature063"
          OutputPath = path
          Width = width
          Height = height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 5.0 }

    let options: ViewerOptions =
        { Title = "feature063"; InitialSize = { Width = width; Height = height }; PresentMode = ViewerPresentMode.OffscreenReadback; FrameRateCap = None }

    let result = Viewer.captureScreenshotEvidence request options scene
    result, path

/// Decode the PNG into a lit-pixel predicate (alpha > 0) plus its dimensions.
let private decodeLit (path: string) =
    use bitmap = SKBitmap.Decode path
    Expect.isNotNull bitmap "screenshot PNG decodes"
    let w, h = bitmap.Width, bitmap.Height
    let lit = Array2D.init w h (fun x y -> bitmap.GetPixel(x, y).Alpha > 0uy)
    w, h, lit

let private litPixels (lit: bool[,]) =
    seq {
        for x in 0 .. Array2D.length1 lit - 1 do
            for y in 0 .. Array2D.length2 lit - 1 do
                if lit.[x, y] then
                    yield x, y
    }

[<Tests>]
let tests =
    testList "Feature063 image-evidence renderer" [
        test "Line and Path render to pixels beyond the placeholder box (SC-001)" {
            // Terrain polyline + filled ground, both well to the right of (8,8)-(48,48).
            let terrain = Scene.line { X = 20.0; Y = 180.0 } { X = 300.0; Y = 190.0 } (Paint.stroke (Colors.rgb 210uy 210uy 70uy) 3.0)

            let ground =
                let spec =
                    Path.create
                        Winding
                        [ Path.moveTo 0.0 239.0
                          Path.lineTo 319.0 239.0
                          Path.lineTo 319.0 205.0
                          Path.lineTo 0.0 215.0
                          Path.close ]

                Scene.path spec (Paint.fill (Colors.rgb 80uy 120uy 80uy))

            let scene = Group [ terrain; ground ]
            let result, path = renderToPng 320 240 scene

            Expect.equal result.Status ScreenshotOk "image evidence captured"

            let _, _, lit = decodeLit path
            let rightOfPlaceholder =
                litPixels lit |> Seq.filter (fun (x, _) -> x > placeholderRightEdge + 60) |> Seq.length

            Expect.isGreaterThan
                rightOfPlaceholder
                0
                "Line/Path must render real pixels well to the right of the old 40x40 placeholder box, not collapse onto it"
        }

        test "Text renders real glyphs, not a solid filled block (SC-001)" {
            let scene = Group [ Scene.textAt { X = 120.0; Y = 120.0 } "HUD" Colors.white ]
            let result, path = renderToPng 320 160 scene
            Expect.equal result.Status ScreenshotOk "image evidence captured"

            let _, _, lit = decodeLit path

            // Tight bounding box of the lit text pixels, then its fill fraction. A solid
            // placeholder rect fills its box (~1.0); real glyphs leave interior gaps.
            let pts = litPixels lit |> Seq.toList
            Expect.isNonEmpty pts "Text must render some pixels"
            let xs = pts |> List.map fst
            let ys = pts |> List.map snd
            let minX, maxX = List.min xs, List.max xs
            let minY, maxY = List.min ys, List.max ys
            let area = (maxX - minX + 1) * (maxY - minY + 1)
            let fillFraction = float pts.Length / float area

            Expect.isLessThan
                fillFraction
                0.85
                "Text must render as glyphs (interior gaps) rather than a solid filled rectangle"
        }

        test "node count is structural; the image is the visual proof on a Line-only scene (SC-002)" {
            let line = Scene.line { X = 20.0; Y = 180.0 } { X = 300.0; Y = 185.0 } (Paint.stroke Colors.white 2.0)
            let scene = Group [ line ]

            // Structural view: describe reports a Line element — node count alone says "visible".
            let kinds = Scene.describe { Nodes = [ scene ] }
            Expect.contains kinds LineElement "describe reports the Line structurally"

            // Visual proof: the Line must paint real pixels beyond the placeholder box.
            let result, path = renderToPng 320 240 scene
            Expect.equal result.Status ScreenshotOk "image evidence captured"

            let _, _, lit = decodeLit path
            let rightOfPlaceholder =
                litPixels lit |> Seq.filter (fun (x, _) -> x > placeholderRightEdge + 60) |> Seq.length

            Expect.isGreaterThan
                rightOfPlaceholder
                0
                "a Line-only scene must prove visibility through rendered pixels, not a placeholder substitution that node-count checks accept"
        }
    ]
