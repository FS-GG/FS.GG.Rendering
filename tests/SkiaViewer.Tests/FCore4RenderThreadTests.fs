module FCore4RenderThreadTests

// F-CORE-4 — the process-wide render statics (`GlHost`'s present-timing carriers, the idle-skip /
// represent counters, the live-authoring-size override, and `FrameCache`'s cached snapshot) are
// mutated without a lock and are correct only because `GlHost.run` is single-threaded (Issue #180).
// That was an unstated assumption; a diagnostic reader reaching one of these statics off the loop
// thread would race it silently.
//
// `RenderThread` turns the assumption into an enforced invariant: the run `claim`s the loop thread on
// entry and `release`s it in teardown, and the cross-run/cross-module accessors `verify` first. These
// tests drive the guard directly — the owning thread reads normally, an off-thread touch fails loudly
// and names the offending seam, and between runs the guard is inert (so the Issue #177 direct-call
// `FrameCache` tests, and any accessor read before the first run, are unaffected).
//
// Teeth: if `verify` did NOT actually compare the calling thread (a no-op guard), the off-thread cases
// below would capture no exception and RED. Each test releases the claim in a `finally` so a mutated
// `owner` never leaks into another sequenced test.

open System
open System.Threading
open Expecto
open FS.GG.UI.SkiaViewer.Host

/// Run `body` on a fresh background thread and return whatever it threw (or `None`). Joins before
/// returning, so the observation is complete and no thread outlives the assertion.
let private capturingOffThread (body: unit -> unit) : exn option =
    let mutable captured: exn option = None

    let thread =
        Thread(
            ThreadStart(fun () ->
                try
                    body ()
                with ex ->
                    captured <- Some ex),
            IsBackground = true
        )

    thread.Start()
    thread.Join()
    captured

[<Tests>]
let tests =
    testSequenced
    <| testList
        "RenderThread affinity guard (F-CORE-4)"
        [ test "verify passes on the owning thread and trips off it, naming the accessor" {
              try
                  RenderThread.claim ()
                  // The owning (this) thread may touch a static freely.
                  RenderThread.verify "on-thread-accessor"

                  // Another thread touching a static while this run owns them fails loudly.
                  let offThread = capturingOffThread (fun () -> RenderThread.verify "off-thread-accessor")

                  Expect.isSome offThread "an off-thread access must trip the affinity guard"
                  Expect.stringContains offThread.Value.Message "off-thread-accessor" "the error names the offending accessor"
                  Expect.stringContains offThread.Value.Message "single-threaded" "the error explains the invariant it enforces"
              finally
                  RenderThread.release ()

              // With no run owning the statics, the guard is inert on any thread.
              let afterRelease = capturingOffThread (fun () -> RenderThread.verify "post-release")
              Expect.isNone afterRelease "between runs (no owner claimed) the guard is inert"
          }

          test "ownerThreadId tracks claim and release" {
              Expect.isNone (RenderThread.ownerThreadId ()) "no owner before a run claims"

              try
                  RenderThread.claim ()

                  Expect.equal
                      (RenderThread.ownerThreadId ())
                      (Some Environment.CurrentManagedThreadId)
                      "claim records the calling loop thread"
              finally
                  RenderThread.release ()

              Expect.isNone (RenderThread.ownerThreadId ()) "release clears the owner"
          }

          test "FrameCache.current trips off-thread while a run owns the statics, inert otherwise" {
              // Unowned — exactly how the Issue #177 direct-call lifetime tests run: cross-thread access
              // is inert, so those tests are unaffected by the new guard.
              let unowned = capturingOffThread (fun () -> FrameCache.current () |> ignore)
              Expect.isNone unowned "with no run active, a direct FrameCache read is unaffected"

              try
                  RenderThread.claim ()
                  // The owning thread reads the (empty) cache normally.
                  Expect.isNone (FrameCache.current ()) "the owning thread reads the cache"

                  // Off the owning thread the read fails loudly instead of racing the cached SKImage handle.
                  let offThread = capturingOffThread (fun () -> FrameCache.current () |> ignore)

                  Expect.isSome offThread "an off-thread FrameCache read must trip the guard"
                  Expect.stringContains offThread.Value.Message "FrameCache.current" "the error names the FrameCache seam"
              finally
                  RenderThread.release ()
          } ]
