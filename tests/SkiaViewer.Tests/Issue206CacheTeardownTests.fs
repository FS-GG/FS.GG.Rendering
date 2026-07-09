module Issue206CacheTeardownTests

#nowarn "3261" // `ImageCache.resolve` returns null for a missing/undecodable source, as `SceneRenderer` does

open System
open System.IO
open Expecto
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

// Issue #206. #178 replaced two per-frame allocations with bounded caches and shipped the teardown for
// both — `SceneRenderer.ImageCache.dispose` and `Fonts.disposeCaches` — without calling either. So a
// `GlHost.run` teardown released the replay cache and left up to `ImageCache.cap` decoded images and a
// full font cache resident, where the pre-#178 code disposed each image in the frame that decoded it.
//
// The invariant, stated as `PictureReplayCache` already states it:
//
//   a run's teardown releases every native object its frames left resident, and the next run
//   repopulates rather than serving a disposed handle.
//
// A real two-`GlHost.run` test would need a live GL context, which no headless test host has — the
// same limitation `Issue177FrameCacheLifetimeTests` documents, and it resolves it the same way: drive
// the caches directly, over raster objects, which observe disposal identically (`SKObject.Handle` is
// zeroed by `Dispose`). Together these make "teardown leaves a resident native object" unreachable at
// the cache boundary, and `GlHost.run`'s `finally` calls exactly these two functions.
//
// Sequenced: both caches are module statics, and `dispose` here would otherwise free objects a
// concurrently running test is drawing with.

let private isDisposed (o: SKObject) = o.Handle = nativeint 0

let private writePng (path: string) (fill: SKColor) =
    use surface = SKSurface.Create(SKImageInfo(8, 8))
    surface.Canvas.Clear fill
    use image = surface.Snapshot()
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    use file = File.OpenWrite path
    data.SaveTo file

let private tempDir () =
    let dir = Path.Combine(Path.GetTempPath(), "issue206-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory dir |> ignore
    dir

let private sansSpec: FontSpec =
    { Family = Some "Noto Sans"
      Size = 21.0
      Weight = None }

[<Tests>]
let tests =
    testSequenced
    <| testList
        "cache teardown (issue #206)"
        [ test "teardown disposes every decoded image and empties the image cache" {
              let dir = tempDir ()

              try
                  let file = Path.Combine(dir, "sprite.png")
                  writePng file SKColors.Red

                  let image = SceneRenderer.ImageCache.resolve file
                  Expect.isNotNull image "the source decodes"
                  Expect.equal (SceneRenderer.ImageCache.count ()) 1 "one resident entry"

                  SceneRenderer.ImageCache.dispose ()

                  Expect.equal (SceneRenderer.ImageCache.count ()) 0 "teardown empties the cache"
                  Expect.isTrue (isDisposed image) "the decoded image's native handle was released"
              finally
                  SceneRenderer.ImageCache.dispose ()
                  Directory.Delete(dir, true)
          }

          test "a run after teardown re-decodes rather than serving a disposed image" {
              let dir = tempDir ()

              try
                  let file = Path.Combine(dir, "sprite.png")
                  writePng file SKColors.Blue

                  let first = SceneRenderer.ImageCache.resolve file
                  SceneRenderer.ImageCache.dispose ()
                  let second = SceneRenderer.ImageCache.resolve file

                  Expect.isNotNull second "the source decodes again after teardown"
                  Expect.isFalse (isDisposed second) "the second run's image is live"
                  Expect.isFalse (Object.ReferenceEquals(first, second)) "it is a fresh decode, not the disposed one"
                  Expect.equal (SceneRenderer.ImageCache.count ()) 1 "the cache repopulated"
              finally
                  SceneRenderer.ImageCache.dispose ()
                  Directory.Delete(dir, true)
          }

          test "teardown disposes every cached font and empties the font caches" {
              let font = Fonts.resolveFont sansSpec
              Expect.isGreaterThan (Fonts.fontCacheCount ()) 0 "resolving a spec makes a resident font"

              Fonts.disposeCaches ()

              Expect.equal (Fonts.fontCacheCount ()) 0 "teardown empties the font cache"
              Expect.isTrue (isDisposed font) "the cached font's native handle was released"
          }

          test "a run after teardown rebuilds the fonts rather than serving a disposed one" {
              Fonts.resolveFont sansSpec |> ignore
              Fonts.disposeCaches ()

              let rebuilt = Fonts.resolveFont sansSpec

              Expect.isFalse (isDisposed rebuilt) "the second run's font is live"
              Expect.equal (Fonts.fontCacheCount ()) 1 "the cache repopulated"

              // The typeface behind it was disposed too, so this measures only if it was rebuilt with it.
              Expect.isGreaterThan (float (rebuilt.MeasureText "fs.gg")) 0.0 "the rebuilt font measures text"
          }

          test "text still resolves and draws after a teardown" {
              Fonts.disposeCaches ()

              use surface = SKSurface.Create(SKImageInfo(64, 32))
              surface.Canvas.Clear SKColors.White

              let node =
                  TextRun
                      { Text = "fs.gg"
                        Position = { X = 2.0; Y = 20.0 }
                        Font = sansSpec
                        Paint = { Paint.fill Colors.black with Antialias = true } }

              SceneRenderer.paintNode surface.Canvas node

              use snapshot = surface.Snapshot()
              use data = snapshot.Encode(SKEncodedImageFormat.Png, 100)
              Expect.isGreaterThan (data.ToArray()).Length 0 "a frame after teardown paints text"
              Expect.isGreaterThan (Fonts.fontCacheCount ()) 0 "and repopulated the font cache"
          } ]
