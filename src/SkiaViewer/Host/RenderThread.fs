namespace FS.GG.UI.SkiaViewer.Host

open System

// F-CORE-4 / Issue #180: the render loop's thread-affinity guard for the process-wide render statics.
//
// `GlHost.run` is single-threaded — Silk drives the window from one thread, so `DoEvents`/`DoUpdate`/
// `DoRender` and every input callback run on it (see the `LoopDispatchGate` note in `OpenGl.fs`).
// Every module static the run mutates WITHOUT a lock — the present-timing carriers, the idle-skip /
// represent counters, the live-authoring-size override, and the `FrameCache` snapshot — is correct
// only because each read and write happens on that one loop thread. That was an unstated assumption:
// a diagnostic reader reaching one of these statics from another thread would race it silently.
//
// This turns the assumption into an enforced invariant. The run `claim`s the loop thread on entry and
// `release`s it in teardown; the accessors an off-thread caller could actually reach (`lastPresentTiming`,
// `setLiveAuthoringSizeOverride`, `FrameCache.*`) `verify` first, so an off-thread access fails loudly
// and immediately — naming the offending seam — instead of tearing render state. The guard is INERT
// between runs (no owner claimed), so unit tests that drive `FrameCache` directly, and any accessor
// read before the first run, are unaffected.
//
// This does NOT make the host multi-instance safe: two overlapping `GlHost.run` calls still clobber
// each other's statics — the acknowledged single-instance defect the runtime-correctness epic tracks.
// It closes only the off-thread-access hazard F-CORE-4 flagged.
module internal RenderThread =

    // The managed thread id of the run that owns the render statics, or `None` between runs. Written
    // only by `claim`/`release` on the loop thread; read by `verify` from any thread. A reference read
    // of an `int option` is atomic, and the only states another thread can observe are "owned by the
    // loop thread" (trip) and "no live run" (inert) — exactly the decision `verify` needs.
    let mutable private owner: int option = None

    /// Record the calling thread as the render-static owner for this run. Called once on `GlHost.run`
    /// entry, on the loop thread, before any static is reset.
    let claim () = owner <- Some Environment.CurrentManagedThreadId

    /// Clear the ownership claim. Called from the run's teardown `finally`, so the statics are unowned
    /// (and the guard inert) between runs. Idempotent.
    let release () = owner <- None

    /// The owning loop-thread id, or `None` between runs. For tests/diagnostics.
    let ownerThreadId () = owner

    /// Fail loudly if a render static is being touched off the owning loop thread. A no-op when no run
    /// owns the statics. `context` names the accessor so the raised error points at the offending seam.
    let verify (context: string) =
        match owner with
        | Some ownerId when ownerId <> Environment.CurrentManagedThreadId ->
            invalidOp (
                sprintf
                    "%s: the SkiaViewer render statics are single-threaded (Issue #180) but were touched from thread %d; the render loop owns them on thread %d. Route the access through the loop (see LoopDispatchGate)."
                    context
                    Environment.CurrentManagedThreadId
                    ownerId
            )
        | _ -> ()
