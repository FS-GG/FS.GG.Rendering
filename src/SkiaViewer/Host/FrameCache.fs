namespace FS.GG.UI.SkiaViewer.Host

open SkiaSharp

// Feature 122 (FR-001/002): the most recent fully-painted frame, cached so an idle frame can
// re-present it onto a not-yet-filled swapchain buffer WITHOUT re-walking the scene.
//
// The cached SKImage is GPU-backed: it is a snapshot of the run's Skia surface and its pixels live
// in that run's GRContext. Disposing it after the GRContext is gone is undefined at the Skia level,
// so the run OWNS the release, and releases it while the context is still alive. That ownership is
// the whole reason this cache is a module rather than a bare `let mutable` in the host:
//
//   beginRun   drops any stale reference WITHOUT disposing (its context is already gone)
//   replace    supersedes the previous frame on the paint path (context live)
//   release    disposes and clears from the run's teardown (context still live)
//
// Like the host state it was extracted from, this is a module static: two overlapping `GlHost.run`
// calls still clobber each other's cache. That is the concurrent-run defect tracked by the
// SkiaViewer runtime-correctness epic, not something this cache decides.
module internal FrameCache =

    let mutable private cached: SKImage option = None

    /// The cached frame, if one has been painted this run.
    let current () = cached

    /// Supersede the cached frame with a fresh snapshot, disposing the frame it replaces. Called on
    /// the paint path, where the owning GRContext is live.
    let replace (image: SKImage) =
        cached |> Option.iter (fun previous -> previous.Dispose())
        cached <- Some image

    /// Release the cached frame. Called from the run's teardown BEFORE the GRContext is disposed,
    /// so the image's backing context outlives it. Idempotent.
    let release () =
        cached |> Option.iter (fun image -> image.Dispose())
        cached <- None

    /// Start a run with an empty cache. `release` runs in the previous run's teardown `finally`, so
    /// the cache is already empty here. A frame surviving to this point could only have come from a
    /// run whose teardown did not complete — its GRContext is gone, and disposing a GPU-backed
    /// SKImage after its context is undefined — so drop the reference and leave the stale native
    /// handle to finalization rather than reach through it.
    let beginRun () = cached <- None
