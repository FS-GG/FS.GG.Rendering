module Issue400NativeResolutionTests

// Issue #400 — native-resolution HiDPI, the follow-up to #364. #364 fixed the *visible* defect: on a
// scaled display the host fits the logical scene onto the physical framebuffer at present time, so a
// product fills the window. But the product still rendered at LOGICAL resolution (upscaled), and an
// opt-in `OffscreenReadback` capture left the scene in its top-left corner.
//
// #400 advertises the PHYSICAL framebuffer size to a size-aware product so it draws at native
// resolution, rescales pointer input by the physical/logical ratio, and fits the HiDPI readback.
// These tests pin the pure arithmetic the interactive host composes — the pointer rescale
// (`LogicalCanvas.toPhysicalPoint`), the invariance that keeps a letterboxed (`LogicalSize`) product's
// pointer mapping unchanged, the native view-size rule, and the readback fit — none of which need a
// live GL window. The GL integration (the readback pixels, the present-time identity fit) is exercised
// at scale 1 by the present-mode / native-capture tests, where the fit is the identity.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private size w h : Size = { Width = w; Height = h }

// The interactive host's seams, extracted as the pure rules it applies inline, so the composition is
// asserted without a persistent window (which the CI host cannot always open).

/// The Size the host hands `View` (issue #400): the fixed `LogicalSize` when set, else the physical
/// framebuffer — so a size-unaware product renders at native resolution instead of the logical window.
let private nativeViewSize (logicalSize: Size option) (framebuffer: Size) =
    logicalSize |> Option.defaultValue framebuffer

/// The pointer coordinate the host routes to `MapPointer` (issue #400): scale the logical sample into
/// physical FIRST, then (for a `LogicalSize` product) invert the letterbox fit; without one the
/// physical coordinate IS the product's space.
let private routedPointer (logicalSize: Size option) (window: Size) (framebuffer: Size) (x: float) (y: float) =
    let px, py = LogicalCanvas.toPhysicalPoint window framebuffer x y

    match logicalSize with
    | Some logical -> LogicalCanvas.toLogicalPoint logical framebuffer px py
    | None -> (px, py)

[<Tests>]
let issue400NativeResolutionTests =
    testList
        "Issue 400 native-resolution HiDPI"
        [

          // --- toPhysicalPoint: the logical->physical pointer rescale ---------------------------------

          test "toPhysicalPoint is the identity at scale 1 (window == framebuffer)" {
              // The scale-1 invariant: on an unscaled display the pointer is untouched, so the entire
              // X11/scale-1 test surface behaves exactly as before #400.
              Expect.equal (LogicalCanvas.toPhysicalPoint (size 800 600) (size 800 600) 123.0 77.0) (123.0, 77.0) "no rescale when the framebuffer equals the window"
          }

          test "toPhysicalPoint scales a pointer by an integer HiDPI ratio" {
              // A 2x display: a logical point at the window center lands at the physical center.
              Expect.equal (LogicalCanvas.toPhysicalPoint (size 800 600) (size 1600 1200) 400.0 300.0) (800.0, 600.0) "a 2x framebuffer doubles the pointer coordinate"
          }

          test "toPhysicalPoint scales each axis independently under non-square DPI" {
              // Width 3x, height 2x — the axes must not share one ratio.
              Expect.equal (LogicalCanvas.toPhysicalPoint (size 100 100) (size 300 200) 10.0 10.0) (30.0, 20.0) "each axis uses its own physical/logical ratio"
          }

          test "toPhysicalPoint is inert on a degenerate size rather than dividing by zero" {
              Expect.equal (LogicalCanvas.toPhysicalPoint (size 0 600) (size 1600 1200) 5.0 5.0) (5.0, 5.0) "a zero window width yields the input unchanged"
              Expect.equal (LogicalCanvas.toPhysicalPoint (size 800 600) (size 1600 0) 5.0 5.0) (5.0, 5.0) "a zero framebuffer height yields the input unchanged"
          }

          // --- native view-size selection -------------------------------------------------------------

          test "without a LogicalSize the product is handed the PHYSICAL framebuffer (native resolution)" {
              // The #400 change: a size-aware product renders at full framebuffer resolution, not the
              // logical window. Before #400 this was the window size.
              Expect.equal (nativeViewSize None (size 1600 1200)) (size 1600 1200) "a size-aware product draws at native framebuffer resolution"
          }

          test "a LogicalSize product still renders in its fixed logical canvas, not the framebuffer" {
              // Native resolution does not override an explicit fixed-resolution game; the host fits its
              // canvas to the framebuffer instead.
              Expect.equal (nativeViewSize (Some(size 320 240)) (size 1600 1200)) (size 320 240) "a fixed-resolution product keeps its logical canvas"
          }

          // --- pointer routing composed end to end ----------------------------------------------------

          test "without a LogicalSize the routed pointer IS the physical coordinate (matches the native View)" {
              // The product hit-tests in framebuffer space, so the routed pointer must be the physical
              // coordinate — the same space `nativeViewSize` handed its `View`.
              let rx, ry = routedPointer None (size 800 600) (size 1600 1200) 400.0 300.0
              Expect.equal (rx, ry) (800.0, 600.0) "a native product receives the pointer in framebuffer coordinates"
          }

          test "a LogicalSize product's pointer mapping is UNCHANGED by native resolution (invariance)" {
              // The key correctness property: scaling the sample logical->physical and then inverting the
              // letterbox fit against the PHYSICAL surface must equal the pre-#400 mapping (invert the
              // fit against the LOGICAL window directly). The physical scale cancels the larger fit scale
              // exactly, so a fixed-resolution game sees the same logical hit test at any display scale.
              let logical, window = size 320 240, size 800 600

              for scale in [ 1; 2; 3 ] do
                  let framebuffer = size (window.Width * scale) (window.Height * scale)

                  for x, y in [ 0.0, 0.0; 400.0, 300.0; 733.0, 111.0; 800.0, 600.0 ] do
                      let native = routedPointer (Some logical) window framebuffer x y
                      let pre400 = LogicalCanvas.toLogicalPoint logical window x y
                      Expect.floatClose Accuracy.high (fst native) (fst pre400) $"x invariant at scale {scale} for ({x},{y})"
                      Expect.floatClose Accuracy.high (snd native) (snd pre400) $"y invariant at scale {scale} for ({x},{y})"
          }

          // --- HiDPI readback fit ---------------------------------------------------------------------

          // Issue #400 part 2: the `OffscreenReadback` pixels now render through the SAME logical->physical
          // fit as the displayed frame (`renderSceneToPixels` takes an authoring size and calls
          // `drawSceneFitted`), instead of drawing the logical scene 1:1 into the top-left corner of the
          // physical-sized readback. The correctness reduces to the fit already proven for the display in
          // #364: a logical canvas fitted onto a 2x physical target fills it. At scale 1 the authoring
          // size equals the target, the fit is the identity, and the readback is byte-identical to before.
          test "the HiDPI readback fits the logical scene onto the physical target (fills it, not a corner)" {
              let logical, physical = size 400 300, size 800 600
              let fit = LogicalCanvas.fit logical physical
              Expect.equal fit.Scale 2.0 "the readback upscales the logical authoring size onto the 2x framebuffer"
              Expect.equal (fit.OffsetX, fit.OffsetY) (0.0, 0.0) "a uniform 2x readback fill has no letterbox bars"
          }

          test "a scale-1 readback authoring size equals the target, so the fit is the identity (no regression)" {
              let fb = size 800 600
              Expect.equal (LogicalCanvas.fit fb fb) { Scale = 1.0; OffsetX = 0.0; OffsetY = 0.0 } "at scale 1 the readback is byte-identical to a 1:1 draw"
          } ]
