module Issue246LogicalSizeTests

// Issue #246 — a fixed-logical-resolution product could not scale to its window. `GeneratedAppHost.View`
// withholds the window `Size` on purpose (resolution independence), but the affordance that made the
// discipline safe — a logical-size letterbox in the host — did not exist, so a 1280x720 scene rendered
// into a 640x480 surface was simply CLIPPED to the top-left quadrant.
//
// `ViewerOptions.LogicalSize` turns the fit on. These tests pin the pure arithmetic (`LogicalCanvas`),
// prove it through the REAL shared painter onto a raster surface (the top-left-crop regression is a
// pixel fact, not a structural one), and pin the option-validation and pointer-inverse seams.

open System
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

/// Render a node at an exact surface size through the shared painter, returning the PNG path.
let private renderToPng (width: int) (height: int) (scene: SceneNode) =
    let dir = IO.Path.Combine(IO.Path.GetTempPath(), "issue246-logical-size")
    IO.Directory.CreateDirectory dir |> ignore
    let path = IO.Path.Combine(dir, $"scene-{Guid.NewGuid():N}.png")

    let request: ScreenshotEvidenceRequest =
        { Command = "screenshot"
          AppOrSample = "issue246"
          OutputPath = path
          Width = width
          Height = height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 5.0 }

    let options: ViewerOptions =
        { Title = "issue246"
          InitialSize = { Width = width; Height = height }
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None
          LogicalSize = None }

    Viewer.captureScreenshotEvidence request options scene |> ignore
    path

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

/// Tier-T1 raster capability probe (mirrors Feature086SceneTranslateTests / Audit_ReplayCache).
let private rasterAvailable: bool =
    try
        use s = SKSurface.Create(SKImageInfo(8, 8))
        not (isNull s)
    with _ ->
        false

let private withRaster (what: string) (body: unit -> unit) =
    if rasterAvailable then
        body ()
    else
        skiptest (
            sprintf
                "SKIPPED(tier=T1 raster/pixel GL): offscreen SKSurface unavailable on this host (SkiaSharp native/headless) — %s requires the raster/pixel render tier; recorded skipped-with-tier, not a pass (Constitution VI)."
                what)

let private size w h : Size = { Width = w; Height = h }

/// A product that paints its entire fixed logical canvas — the shape whose top-left-quadrant crop
/// was the reported defect.
let private fullCanvas (logical: Size) =
    Rectangle((0.0, 0.0, float logical.Width, float logical.Height), Colors.white)

let private evidenceOptions (logical: Size option) (surface: Size) : ViewerOptions =
    { Title = "issue246"
      InitialSize = surface
      PresentMode = ViewerPresentMode.OffscreenReadback
      FrameRateCap = None
      LogicalSize = logical }

[<Tests>]
let issue246LogicalSizeTests =
    testSequenced
    <| testList
        "Issue 246 ViewerOptions.LogicalSize letterbox"
        [

          test "fit of a 16:9 canvas into a 4:3 surface scales uniformly and centers vertically" {
              let fit = LogicalCanvas.fit (size 1280 720) (size 640 480)
              // width ratio 0.5 < height ratio 0.667, so width binds and the surplus height bars.
              Expect.equal fit.Scale 0.5 "the smaller axis ratio wins (uniform, aspect-preserving)"
              Expect.equal fit.OffsetX 0.0 "the binding axis has no bar"
              Expect.equal fit.OffsetY 60.0 "the surplus axis is centered: (480 - 720*0.5) / 2"
          }

          test "fit of a 9:16 canvas into a 4:3 surface pillarboxes horizontally" {
              let fit = LogicalCanvas.fit (size 720 1280) (size 640 480)
              Expect.equal fit.Scale 0.375 "height binds: 480/1280"
              Expect.equal fit.OffsetX 185.0 "(640 - 720*0.375) / 2"
              Expect.equal fit.OffsetY 0.0 "the binding axis has no bar"
          }

          test "fit is the identity when the canvas already equals the surface" {
              let fit = LogicalCanvas.fit (size 640 480) (size 640 480)
              Expect.equal fit { Scale = 1.0; OffsetX = 0.0; OffsetY = 0.0 } "an exact fit neither scales nor offsets"
          }

          test "a non-positive extent yields the identity fit rather than dividing by zero" {
              Expect.equal (LogicalCanvas.fit (size 0 720) (size 640 480)).Scale 1.0 "zero logical width is inert"
              Expect.equal (LogicalCanvas.fit (size 1280 720) (size 640 0)).Scale 1.0 "zero surface height is inert"
          }

          // Issue #364 — the SAME fit the host's `drawSceneFitted` now applies at present time to map a
          // LOGICAL scene (window.Size) onto the PHYSICAL framebuffer (window.FramebufferSize). At an
          // integer HiDPI scale the logical canvas UPSCALES to fill the whole surface (no bars); at scale
          // 1 the fit is the identity, which is why `drawSceneFitted` is byte-identical to a bare draw
          // and the entire X11/scale-1 test surface is unaffected.
          test "an integer HiDPI scale upscales the logical canvas to fill the physical surface, no bars (#364)" {
              let fit = LogicalCanvas.fit (size 800 600) (size 1600 1200)
              Expect.equal fit.Scale 2.0 "a 2x display doubles the logical canvas"
              Expect.equal fit.OffsetX 0.0 "a uniform 2x fit centers with no horizontal bar"
              Expect.equal fit.OffsetY 0.0 "a uniform 2x fit centers with no vertical bar"
          }

          test "a HiDPI scale onto a differently-shaped surface stays aspect-preserving and bars the surplus (#364)" {
              // 800x600 (4:3) onto a 1600x1000 (8:5) surface: height binds (1.667 < 2.0), so pillarbox.
              let fit = LogicalCanvas.fit (size 800 600) (size 1600 1000)
              Expect.floatClose Accuracy.high fit.Scale (1000.0 / 600.0) "height binds: 1000/600"
              Expect.floatClose Accuracy.high fit.OffsetX ((1600.0 - 800.0 * (1000.0 / 600.0)) / 2.0) "surplus width is centered"
              Expect.equal fit.OffsetY 0.0 "the binding axis has no bar"
          }

          test "at scale 1 the fit is the identity, so the host draws the scene unchanged (#364)" {
              // The scale-1 invariant `drawSceneFitted` guards on: logical == physical -> identity ->
              // delegate to the bare `drawScene`. This is what keeps every scale-1 host present untouched.
              let fit = LogicalCanvas.fit (size 1280 720) (size 1280 720)
              Expect.equal fit { Scale = 1.0; OffsetX = 0.0; OffsetY = 0.0 } "physical == logical is a no-op fit"
          }

          // The HiDPI regression proper, as a pixel fact: a logical canvas presented onto a LARGER
          // physical surface fills it edge to edge. Before #364 the host drew the logical scene 1:1 into
          // the top-left `1/scale²` corner of the physical framebuffer — this proves it now fills the
          // surface, the upscale analog of the #246 top-left-crop proof below.
          test "a logical canvas fills a 2x physical surface edge to edge, not the top-left quadrant (#364)" {
              withRaster "HiDPI upscale pixel proof" (fun () ->
                  let logical, physical = size 400 300, size 800 600
                  let presented = LogicalCanvas.present logical physical (fullCanvas logical)
                  let minX, minY, maxX, maxY = bbox (litPoints (renderToPng physical.Width physical.Height presented))

                  Expect.equal (minX, minY) (0, 0) "the upscaled canvas reaches the top-left origin"
                  Expect.equal maxX (physical.Width - 1) "the upscaled canvas reaches the right edge (not a 400px corner)"
                  Expect.equal maxY (physical.Height - 1) "the upscaled canvas reaches the bottom edge (not a 300px corner)")
          }

          test "present elides the scale under an identity fit but still clips to the canvas" {
              let node = fullCanvas (size 640 480)

              match LogicalCanvas.present (size 640 480) (size 640 480) node with
              | ClipNode(RectClip canvas, { Nodes = [ inner ] }) ->
                  Expect.equal inner node "the node is carried through untransformed"
                  Expect.equal canvas { X = 0.0; Y = 0.0; Width = 640.0; Height = 480.0 } "still clipped to the canvas"
              | other -> failtestf "expected a bare clip with no scale, got %A" other
          }


          test "present wraps in a clipped perspective transform carrying the fit" {
              match LogicalCanvas.present (size 1280 720) (size 640 480) (fullCanvas (size 1280 720)) with
              | PerspectiveNode(transform, { Nodes = [ ClipNode(RectClip canvas, _) ] }) ->
                  Expect.equal transform.M11 0.5 "uniform x scale"
                  Expect.equal transform.M22 0.5 "uniform y scale"
                  Expect.equal transform.M13 0.0 "x offset"
                  Expect.equal transform.M23 60.0 "y offset"
                  // The clip is INSIDE the transform, so it is expressed in logical coordinates.
                  Expect.equal canvas { X = 0.0; Y = 0.0; Width = 1280.0; Height = 720.0 } "clipped to the logical canvas"
              | other -> failtestf "expected a clipped perspective transform, got %A" other
          }

          test "toLogicalPoint inverts present: the surface center is the logical center" {
              let logical, surface = size 1280 720, size 640 480
              let x, y = LogicalCanvas.toLogicalPoint logical surface 320.0 240.0
              Expect.equal x 640.0 "surface center maps to logical center x"
              Expect.equal y 360.0 "surface center maps to logical center y"

              // The letterbox bar is outside the canvas, and says so.
              let _, barY = LogicalCanvas.toLogicalPoint logical surface 320.0 10.0
              Expect.isLessThan barY 0.0 "a point in the top bar maps above the logical canvas"
          }

          test "toLogicalPoint round-trips the fit for an arbitrary logical point" {
              let logical, surface = size 1280 720, size 800 800
              let fit = LogicalCanvas.fit logical surface
              let lx, ly = 400.0, 250.0
              let sx, sy = lx * fit.Scale + fit.OffsetX, ly * fit.Scale + fit.OffsetY
              let rx, ry = LogicalCanvas.toLogicalPoint logical surface sx sy
              Expect.floatClose Accuracy.high rx lx "x round-trips"
              Expect.floatClose Accuracy.high ry ly "y round-trips"
          }

          // The regression proper: pixels, through the real painter, at a surface size that differs
          // from the logical canvas. Before #246 this rendered the top-left quadrant.
          test "a 1280x720 canvas fills a 640x480 surface scaled and letterboxed, not cropped (AC-2)" {
              withRaster "logical-size letterbox pixel proof" (fun () ->
                  let logical, surface = size 1280 720, size 640 480
                  let presented = LogicalCanvas.present logical surface (fullCanvas logical)
                  let minX, minY, maxX, maxY = bbox (litPoints (renderToPng surface.Width surface.Height presented))

                  Expect.equal minX 0 "the scaled canvas reaches the left edge"
                  Expect.equal maxX (surface.Width - 1) "the scaled canvas reaches the right edge (not a 320px crop)"
                  Expect.equal minY 60 "the top letterbox bar is 60px"
                  Expect.equal maxY (surface.Height - 61) "the bottom letterbox bar is 60px")
          }

          test "without a LogicalSize the same canvas is cropped to the top-left quadrant (the #246 defect)" {
              withRaster "unscaled crop control" (fun () ->
                  let logical, surface = size 1280 720, size 640 480
                  // No `present` wrapper — exactly what the host did before this issue.
                  let minX, minY, maxX, maxY = bbox (litPoints (renderToPng surface.Width surface.Height (fullCanvas logical)))

                  Expect.equal (minX, minY) (0, 0) "the crop starts at the origin"
                  Expect.equal maxX (surface.Width - 1) "clipped by the surface, not scaled to it"
                  Expect.equal maxY (surface.Height - 1) "no letterbox bar exists — the canvas simply overflows")
          }

          test "content outside the logical canvas is clipped into the bars, never over them" {
              withRaster "letterbox bar clip" (fun () ->
                  let logical, surface = size 1280 720, size 640 480
                  // A rect that overflows the logical canvas downward; the bar must stay empty.
                  let overflowing = Rectangle((0.0, 0.0, 1280.0, 2000.0), Colors.white)
                  let presented = LogicalCanvas.present logical surface overflowing
                  let _, _, _, maxY = bbox (litPoints (renderToPng surface.Width surface.Height presented))

                  Expect.equal maxY (surface.Height - 61) "overflow is clipped at the logical canvas edge, not painted into the bar")
          }

          test "a non-positive LogicalSize is rejected at option validation" {
              let scene = Group []

              match Viewer.runUntilFirstFrame (evidenceOptions (Some(size 0 720)) (size 640 480)) scene with
              | Result.Error failure ->
                  Expect.equal failure.Classification ProductDefect "a zero logical extent is a product defect"
                  Expect.stringContains failure.Message "logical size" "the diagnostic names the logical size"
              | Result.Ok evidence -> failtestf "expected a logical-size validation failure, got %A" evidence

              match Viewer.runUntilFirstFrame (evidenceOptions (Some(size 1280 -1)) (size 640 480)) scene with
              | Result.Error failure -> Expect.stringContains failure.Message "logical size" "a negative extent is rejected too"
              | Result.Ok evidence -> failtestf "expected a logical-size validation failure, got %A" evidence
          }

          test "a positive LogicalSize clears option validation and is otherwise inert" {
              // With a valid logical size, validation passes and the frame-count (0) is what fails —
              // proving the option was accepted, not rejected.
              match Viewer.runForFrames 0 (evidenceOptions (Some(size 1280 720)) (size 640 480)) (Group []) with
              | Result.Error failure ->
                  Expect.stringContains failure.Message "frame count" "the logical size cleared validation; the frame count did not"
              | Result.Ok evidence -> failtestf "expected a frame-count validation failure, got %A" evidence
          }

          // The fit must not be honored by one launch entry point and dropped by the next: every
          // surface-owning path routes through `presentedFor`. A scene-taking launch (`Viewer.run`,
          // `runBounded` and its wrappers) carries a LogicalSize just as a host-taking one does.
          test "runBounded fits a scene-taking launch to its bounded surface, not just the host paths" {
              withRaster "scene-taking launch letterbox" (fun () ->
                  let logical, surface = size 1280 720, size 640 480
                  let evidencePath = IO.Path.Combine(IO.Path.GetTempPath(), $"issue246-bounded-{Guid.NewGuid():N}.png")

                  let request: ViewerRunRequest =
                      { Target = FirstFrame
                        Timeout = TimeSpan.FromSeconds 5.0
                        Diagnostics = Viewer.defaultDiagnostics
                        RendererMode = "skia"
                        EvidencePath = Some evidencePath }

                  match Viewer.runBounded request (evidenceOptions (Some logical) surface) (fullCanvas logical) with
                  | Result.Error failure ->
                      Expect.equal failure.Classification UnsupportedEnvironment "only an unsupported environment may fail here"
                  | Result.Ok _ ->
                      Expect.isTrue (IO.File.Exists evidencePath) "the bounded run wrote its PNG evidence"
                      // The written evidence is the scene runBounded actually rendered. Unfitted, this
                      // would be the top-left crop (bars absent); fitted, the 60px bars are there.
                      let _, minY, _, maxY = bbox (litPoints evidencePath)
                      Expect.equal minY 60 "the bounded surface got the top letterbox bar"
                      Expect.equal maxY (surface.Height - 61) "the bounded surface got the bottom letterbox bar")
          }

          test "runAppEvidence letterboxes a fixed-resolution generated app onto the evidence surface (AC-2)" {
              let logical, surface = size 1280 720, size 640 480

              let host: GeneratedAppHost<int, unit> =
                  { Init = fun () -> 0, []
                    Update = fun () model -> model, []
                    // The signature at the heart of #246: no `Size` is offered, so the product draws
                    // in its own logical space and the host owes it the fit.
                    View = fun _ -> fullCanvas logical
                    Tick = fun _ -> None
                    MapKey = fun _ _ -> None
                    Diagnostics = Viewer.defaultDiagnostics }

              let request: ViewerRunRequest =
                  { Target = FirstFrame
                    Timeout = TimeSpan.FromSeconds 2.0
                    Diagnostics = Viewer.defaultDiagnostics
                    RendererMode = "skia"
                    EvidencePath = None }

              match Viewer.runAppEvidence request (evidenceOptions (Some logical) surface) host with
              | Result.Ok outcome ->
                  Expect.equal outcome.Mode "persistent-evidence" "the evidence run completed with a LogicalSize set"
                  Expect.isTrue outcome.EvidenceCloseObserved "a letterboxed evidence run still closes for evidence"
              | Result.Error failure ->
                  // Honest environment skip: an unsupported host must not masquerade as a pass.
                  Expect.equal failure.Classification UnsupportedEnvironment "only an unsupported environment may fail here"
          }
        ]
