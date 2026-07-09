module Issue178PaintResourceTests

#nowarn "3261" // `ImageCache.resolve` returns null for a missing/undecodable source, as `SceneRenderer` does

open System
open System.IO
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Three per-frame allocation leaks on the paint path (issue #178):
//
//   1. `configurePaint` assigned freshly created shaders / colour filters / mask filters / image
//      filters / path effects into an `SKPaint`. Disposing that paint releases only the paint's own
//      native handle, so every frame drawing a gradient or a shadow left those objects to the
//      finalizer thread. Assignment takes the paint's own native reference, so the wrapper can be
//      disposed immediately — `subObjectsReleased` counts those disposals.
//   2. `Image` nodes called `SKImage.FromEncodedData` inside `paintNode`, re-reading and re-decoding
//      the file every frame. Now a bounded LRU keyed on file identity.
//   3. `fontCache` was keyed on a continuous `size` and never evicted. Now a bounded LRU.
//
// The shared claim these assert is the one the leak violated: **repainting the same scene N times
// leaves the same number of live native objects as painting it once.** Disposal is observable without
// a GPU — `SKObject.Handle` is zeroed by `Dispose` (the oracle `Issue177FrameCacheLifetimeTests` uses).
//
// Sequenced: `ImageCache` and `Fonts`' caches are module statics, and the eviction cases dispose
// entries that a concurrently running test could otherwise be drawing with.

let private isDisposed (o: SKObject) = o.Handle = nativeint 0

let private red = Colors.rgb 255uy 0uy 0uy
let private green = Colors.rgb 0uy 255uy 0uy
let private blue = Colors.rgb 0uy 0uy 255uy

/// A paint that exercises every one of the five native sub-object kinds `configurePaint` creates.
let private allSubObjectsPaint =
    { Fill = Some Colors.white
      Stroke = Some { Width = 3.0; Cap = Butt; Join = Miter; Miter = 4.0 }
      Opacity = 1.0
      Antialias = true
      BlendMode = SrcOver
      Shader = Some(LinearGradient({ X = 0.0; Y = 0.0 }, { X = 64.0; Y = 64.0 }, [ red; blue ]))
      ColorFilter = BlendColor(green, Multiply)
      MaskFilter = Blur 1.5
      ImageFilter = DropShadow(2.0, 2.0, 3.0, Colors.black)
      PathEffect = Dash([ 6.0; 3.0 ], 0.0) }

[<Literal>]
let private subObjectsPerPaint = 5

let private paintFrame (node: SceneNode) =
    use surface = SKSurface.Create(SKImageInfo(64, 64))
    surface.Canvas.Clear(SKColors.White)
    SceneRenderer.paintNode surface.Canvas node
    use image = surface.Snapshot()
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    data.ToArray()

let private writePng (path: string) (fill: SKColor) =
    use surface = SKSurface.Create(SKImageInfo(8, 8))
    surface.Canvas.Clear fill
    use image = surface.Snapshot()
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    File.WriteAllBytes(path, data.ToArray())

let private tempDir () =
    let dir = Path.Combine(Path.GetTempPath(), "issue178-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory dir |> ignore
    dir

[<Tests>]
let tests =
    testSequenced
    <| testList
        "Per-frame paint resources (issue #178)"
        [ test "every frame releases every native sub-object its paints created" {
              let node = PaintedRectangle({ X = 8.0; Y = 8.0; Width = 48.0; Height = 48.0 }, allSubObjectsPaint)

              // The leak was per-frame, so the assertion has to be per-frame: N repaints must release
              // exactly N frames' worth. A dropped `release` shows up as a shortfall that grows with N.
              for frames in [ 1; 8; 64 ] do
                  SceneRenderer.resetSubObjectsReleased ()

                  for _ in 1..frames do
                      paintFrame node |> ignore

                  Expect.equal
                      SceneRenderer.subObjectsReleased
                      (frames * subObjectsPerPaint)
                      (sprintf "%d frames should each release %d sub-objects" frames subObjectsPerPaint)
          }

          test "disposing the sub-objects leaves the rendered pixels unchanged" {
              // The paint takes its own native reference on assignment, so releasing our wrapper before
              // the draw cannot change output. Two independent frames must also agree byte for byte.
              let node = PaintedRectangle({ X = 8.0; Y = 8.0; Width = 48.0; Height = 48.0 }, allSubObjectsPaint)
              let first = paintFrame node
              let second = paintFrame node

              Expect.isGreaterThan first.Length 0 "the gradient/shadow frame encodes to a non-empty PNG"
              Expect.equal second first "repainting the same scene is byte-identical"
          }

          test "a paint with no shader or filters creates no sub-objects to release" {
              let plain =
                  { allSubObjectsPaint with
                      Shader = None
                      ColorFilter = NoColorFilter
                      MaskFilter = NoMaskFilter
                      ImageFilter = NoImageFilter
                      PathEffect = NoPathEffect }

              SceneRenderer.resetSubObjectsReleased ()
              paintFrame (PaintedRectangle({ X = 0.0; Y = 0.0; Width = 8.0; Height = 8.0 }, plain)) |> ignore
              Expect.equal SceneRenderer.subObjectsReleased 0 "nothing was created, so nothing is released"
          }

          // Skia declines to build a degenerate path effect and SkiaSharp returns `null` for it. Both of
          // these pass `configurePaint`'s guards (`radius >= 0.0`, `not intervals.IsEmpty`), so the
          // release step must treat a null wrapper as nothing to dispose rather than dereferencing it.
          test "a degenerate path effect paints instead of dereferencing a null sub-object" {
              let rect = { X = 8.0; Y = 8.0; Width = 48.0; Height = 48.0 }

              for label, effect in [ "Corner 0.0", Corner 0.0; "Dash [0; 0]", Dash([ 0.0; 0.0 ], 0.0) ] do
                  let node = PaintedRectangle(rect, { allSubObjectsPaint with PathEffect = effect })

                  Expect.isGreaterThan
                      (paintFrame node).Length
                      0
                      (sprintf "%s yields no path effect, and the frame still paints" label)
          }

          // `File.Exists` is total over any string; `FileInfo` throws on one that cannot name a file.
          // An `Image` node with an unset source must draw the placeholder, not raise out of `paintNode`.
          test "an Image node with an unnameable source draws the placeholder" {
              let rect = 0.0, 0.0, 32.0, 32.0

              for label, source in [ "unset", ""; "whitespace", "   "; "absent", "no/such/file.png" ] do
                  Expect.isGreaterThan
                      (paintFrame (SceneNode.Image(rect, source))).Length
                      0
                      (sprintf "an Image whose source is %s paints the placeholder outline" label)
          }

          test "repainting an Image node decodes once, not once per frame" {
              let dir = tempDir ()

              try
                  let file = Path.Combine(dir, "sprite.png")
                  writePng file SKColors.Red
                  SceneRenderer.ImageCache.dispose ()

                  let node = SceneNode.Image((0.0, 0.0, 8.0, 8.0), file)

                  for _ in 1..32 do
                      paintFrame node |> ignore

                  Expect.equal (SceneRenderer.ImageCache.count ()) 1 "32 frames of one image hold one cache entry"
              finally
                  SceneRenderer.ImageCache.dispose ()
                  Directory.Delete(dir, true)
          }

          test "an edited file re-decodes rather than serving the stale bitmap" {
              let dir = tempDir ()

              try
                  let file = Path.Combine(dir, "sprite.png")
                  SceneRenderer.ImageCache.dispose ()

                  writePng file SKColors.Red
                  let redImage = SceneRenderer.ImageCache.resolve file
                  Expect.isFalse (isNull redImage) "the red image decodes"

                  // Distinct content *and* length, and a distinct write time, so file identity changes.
                  writePng file SKColors.Blue
                  File.SetLastWriteTimeUtc(file, DateTime.UtcNow.AddSeconds 5.0)
                  let blueImage = SceneRenderer.ImageCache.resolve file

                  Expect.isFalse (isNull blueImage) "the edited image decodes"
                  Expect.isFalse (Object.ReferenceEquals(redImage, blueImage)) "the edit is not served from the stale entry"
              finally
                  SceneRenderer.ImageCache.dispose ()
                  Directory.Delete(dir, true)
          }

          test "the image cache is bounded and disposes what it evicts" {
              let dir = tempDir ()

              try
                  SceneRenderer.ImageCache.dispose ()
                  let cap = SceneRenderer.ImageCache.cap

                  let first = Path.Combine(dir, "first.png")
                  writePng first SKColors.Red
                  let evicted = SceneRenderer.ImageCache.resolve first
                  Expect.isFalse (isNull evicted) "the first image decodes"

                  // `cap` further distinct sources push the least-recently-used entry — the first — out.
                  for i in 1..cap do
                      let path = Path.Combine(dir, sprintf "fill-%d.png" i)
                      writePng path (SKColor(byte i, 0uy, 0uy, 255uy))
                      SceneRenderer.ImageCache.resolve path |> ignore

                  Expect.equal (SceneRenderer.ImageCache.count ()) cap "the cache never exceeds its cap"
                  Expect.isTrue (isDisposed evicted) "the evicted image's native handle was released"
              finally
                  SceneRenderer.ImageCache.dispose ()
                  Directory.Delete(dir, true)
          }

          test "a missing or undecodable source is cached, not retried every frame" {
              let dir = tempDir ()

              try
                  SceneRenderer.ImageCache.dispose ()
                  let missing = Path.Combine(dir, "absent.png")
                  let corrupt = Path.Combine(dir, "corrupt.png")
                  File.WriteAllText(corrupt, "this is not a PNG")

                  let node = Group [ { Nodes = [ SceneNode.Image((0.0, 0.0, 8.0, 8.0), missing); SceneNode.Image((0.0, 0.0, 8.0, 8.0), corrupt) ] } ]

                  for _ in 1..16 do
                      paintFrame node |> ignore

                  Expect.isTrue (isNull (SceneRenderer.ImageCache.resolve missing)) "a missing file resolves to null"
                  Expect.isTrue (isNull (SceneRenderer.ImageCache.resolve corrupt)) "an undecodable file resolves to null"
                  Expect.equal (SceneRenderer.ImageCache.count ()) 2 "16 frames hold one entry per failed source"
              finally
                  SceneRenderer.ImageCache.dispose ()
                  Directory.Delete(dir, true)
          }

          test "the font cache is bounded under animated font sizes and disposes what it evicts" {
              // The leak: `size` is continuous, so a product tweening it grew `fontCache` forever.
              let spec size : FontSpec =
                  { Family = Some "Noto Sans"
                    Size = size
                    Weight = None }

              let survivor = Fonts.resolveFont (spec 11.5)
              Expect.isFalse (isDisposed survivor) "the font under test starts alive"

              // Enough distinct sizes to push `survivor` — and everything older — past the cap.
              for i in 1..(Fonts.fontCacheCap * 4) do
                  Fonts.resolveFont (spec (12.0 + float i * 0.25)) |> ignore

              Expect.equal (Fonts.fontCacheCount ()) Fonts.fontCacheCap "the cache settles at its cap, however many sizes are drawn"
              Expect.isTrue (isDisposed survivor) "the evicted font's native handle was released"
          }

          test "re-resolving the same font spec reuses the cached SKFont" {
              let spec: FontSpec =
                  { Family = Some "Noto Sans"
                    Size = 24.0
                    Weight = None }

              let first = Fonts.resolveFont spec
              let before = Fonts.fontCacheCount ()

              for _ in 1..64 do
                  Fonts.resolveFont spec |> ignore

              Expect.isTrue (Object.ReferenceEquals(first, Fonts.resolveFont spec)) "the same spec returns the same font"
              Expect.equal (Fonts.fontCacheCount ()) before "64 repeats add no entries"
          } ]
