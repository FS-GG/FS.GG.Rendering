module Issue177FrameCacheLifetimeTests

open Expecto
open SkiaSharp
open FS.GG.UI.SkiaViewer.Host

// The cached last-good frame is a GPU-backed SKImage owned by its run's GRContext. Disposing it
// after that context is torn down is undefined at the Skia level, so the invariant is:
//
//   a frame is disposed by the run that painted it, while that run's GRContext is still alive.
//
// Before this was fixed, `GlHost.run` never released the cache in its teardown `finally` — the only
// cleanup sat at the START of the NEXT run, which disposed an image whose GRContext had already been
// disposed by the previous run's teardown. It survived only because the stale native handle happened
// not to be reused.
//
// A real two-`GlHost.run` test would need a live GL context, which no headless test host has (the
// repo's own startup/shutdown coverage is likewise synthetic — see `GlStartup.simulateSuccessfulShutdown`).
// So these drive `FrameCache` directly, over RASTER images, which observe disposal identically:
// `SKObject.Handle` is zeroed by `Dispose`. Together the cases below make dispose-after-teardown
// unreachable — teardown always empties the cache, and a run's start never disposes what it finds.
//
// Sequenced: `FrameCache` is a module static, exactly as the host state it was extracted from.

/// A raster stand-in for the host's GPU-backed frame snapshot. Disposal is observable the same way.
let private frame () =
    use surface = SKSurface.Create(SKImageInfo(2, 2))
    surface.Snapshot()

let private isDisposed (image: SKImage) = image.Handle = nativeint 0

[<Tests>]
let tests =
    testSequenced
    <| testList
        "FrameCache lifetime (issue #177)"
        [ test "the run's teardown disposes the cached frame and empties the cache" {
              FrameCache.beginRun ()
              let painted = frame ()
              FrameCache.replace painted

              FrameCache.release ()

              Expect.isTrue (isDisposed painted) "teardown disposes the frame it cached"
              Expect.isNone (FrameCache.current ()) "teardown leaves the cache empty"
          }

          test "a run's start never disposes a frame left behind by a dead context" {
              // Reachable only if a previous teardown did not complete: the image's GRContext is
              // already gone, so `beginRun` must drop the reference rather than dispose through it.
              FrameCache.beginRun ()
              let orphan = frame ()
              FrameCache.replace orphan

              FrameCache.beginRun ()

              Expect.isFalse (isDisposed orphan) "a new run does not dispose a frame it does not own"
              Expect.isNone (FrameCache.current ()) "a new run starts with an empty cache"
              orphan.Dispose()
          }

          test "two sequential runs each release their own frame, and no frame outlives its run" {
              // Run 1.
              FrameCache.beginRun ()
              let first = frame ()
              FrameCache.replace first
              FrameCache.release ()

              // Run 2 starts with nothing to dispose — the previous teardown already emptied the cache,
              // so there is no image here whose context has been torn down.
              FrameCache.beginRun ()
              Expect.isNone (FrameCache.current ()) "run 2 inherits no frame from run 1"

              let second = frame ()
              FrameCache.replace second
              FrameCache.release ()

              Expect.isTrue (isDisposed first) "run 1's frame was released by run 1"
              Expect.isTrue (isDisposed second) "run 2's frame was released by run 2"
              Expect.isNone (FrameCache.current ()) "no frame survives the last run"
          }

          test "the paint path disposes the frame it supersedes" {
              FrameCache.beginRun ()
              let superseded = frame ()
              FrameCache.replace superseded
              let latest = frame ()
              FrameCache.replace latest

              Expect.isTrue (isDisposed superseded) "replacing the cached frame disposes the old one"
              Expect.isFalse (isDisposed latest) "the newly cached frame stays alive for re-present"
              Expect.equal (FrameCache.current () |> Option.map isDisposed) (Some false) "the cache holds a live frame"

              FrameCache.release ()
          }

          test "release is idempotent" {
              FrameCache.beginRun ()
              FrameCache.replace (frame ())

              FrameCache.release ()
              FrameCache.release ()

              Expect.isNone (FrameCache.current ()) "a second teardown pass releases nothing and does not throw"
          } ]
