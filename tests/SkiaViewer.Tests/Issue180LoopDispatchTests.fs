module Issue180LoopDispatchTests

// Issue #180 — `Animation.tickSubscription` dispatches from a `System.Threading.Timer` callback, i.e.
// a threadpool thread. `GlHost.run` is single-threaded by construction: it mutates the current model
// and the effect state (`lastScene`, `pendingScene`, `pendingScreenshots`) without a lock, and paints
// through a thread-affine GL context. Handing that subscription the run's raw `dispatch` therefore
// raced the model and drove Skia off the loop thread.
//
// `LoopDispatch` (internal; reached via InternalsVisibleTo, as feature 121's `shouldAdvanceFrame` is)
// is the hand-off `run` now wraps subscription dispatch in. These tests drive the REAL
// `Animation.tickSubscription` against a simulated loop: the gated loop must never run `update` off
// its own thread and must never observe a torn counter, and the ungated one is kept as the witness
// that the subscription really does arrive off-thread.

open System
open System.Collections.Concurrent
open System.Threading
open Expecto
open Elmish
open FS.GG.UI.Elmish
open FS.GG.UI.SkiaViewer.Host

/// Fast enough that the recurring timer fires hundreds of times inside `loopDuration`, so "the timer
/// fired at all" never rests on a tight wall-clock margin. Paired with the un-slept loop and the spin
/// in `dispatch`, these also make the ungated race land on essentially every tick (measured: ~285 torn
/// reads per ~283 ticks). That is what gives the gated run's monotonic-counter assertion teeth — with
/// a narrower window the ungated shape tears only ~1 run in 4, and "no torn reads" would pass just as
/// happily without the fix.
let private tickInterval = TimeSpan.FromMilliseconds 1.0
let private loopDuration = TimeSpan.FromMilliseconds 300.0

type private Msg =
    | Tick of TimeSpan
    | LoopInput

/// What one simulated run observed. `Counter` only ever increments, so a value that repeats or goes
/// backwards in `Observed` is a torn read-modify-write across two threads.
type private RunReport =
    { LoopThreadId: int
      /// Distinct threads that actually executed `update`.
      UpdateThreads: int Set
      /// Every counter value `update` produced, in the order it produced them.
      Observed: int list
      Ticks: int }

let private onThread (body: unit -> unit) =
    let thread = Thread(ThreadStart(body), IsBackground = true)
    thread.Start()
    thread

/// Run the real animation subscription alongside a simulated update/render loop on a dedicated thread.
/// When `gated`, subscriptions get a `LoopDispatch`-guarded dispatch and the loop drains it — exactly
/// what `GlHost.run` does. When not, they get the raw dispatch — exactly the bug.
let private simulateRun (gated: bool) : RunReport =
    let mutable report = Unchecked.defaultof<RunReport>

    let body () =
        let gate = LoopDispatch.forCurrentThread<Msg> ()
        let loopThreadId = Environment.CurrentManagedThreadId

        // Deliberately unsynchronized, mirroring the host's `currentModel` and effect statics. The
        // recording sinks around it are concurrent, so the ungated run reports the race instead of
        // corrupting the recorder and failing for the wrong reason.
        let mutable counter = 0
        let mutable ticks = 0
        let observed = ConcurrentQueue<int>()
        let updateThreads = ConcurrentQueue<int>()

        let dispatch msg =
            updateThreads.Enqueue Environment.CurrentManagedThreadId

            match msg with
            | Tick _ -> ticks <- ticks + 1
            | LoopInput -> ()

            // A read-modify-write on unsynchronized state: the shape `currentModel <- nextModel` has.
            // The spin widens the window between read and write so a real race is observed rather
            // than merely possible.
            let next = counter + 1
            Thread.SpinWait 400
            counter <- next
            observed.Enqueue counter

        let subscriptionDispatch =
            if gated then LoopDispatch.guard gate dispatch else dispatch

        let started =
            Animation.tickSubscription (fun () -> true) Tick tickInterval ()
            |> List.map (fun (_id, start) -> start subscriptionDispatch)

        // The loop: drain whatever off-thread subscriptions queued, then do this frame's own dispatch.
        // Both run on this thread, so the guarded run serializes everything onto it.
        let stopwatch = Diagnostics.Stopwatch.StartNew()

        while stopwatch.Elapsed < loopDuration do
            if gated then
                LoopDispatch.drain gate dispatch |> ignore

            dispatch LoopInput

        started |> List.iter (fun disposable -> disposable.Dispose())

        report <-
            { LoopThreadId = loopThreadId
              UpdateThreads = Set.ofSeq updateThreads
              Observed = List.ofSeq observed
              Ticks = ticks }

    let thread = onThread body

    if not (thread.Join(loopDuration + TimeSpan.FromSeconds 10.0)) then
        failtest "simulated loop thread did not finish"

    report

/// A counter that only ever increments must produce 1,2,3,… — a repeat or a regression is a lost update.
let private tornReads (observed: int list) =
    observed
    |> List.pairwise
    |> List.filter (fun (previous, next) -> next <> previous + 1)

/// Queue `messages` from a thread that is not the gate's loop thread, and wait for it.
let private queueOffThread (guarded: Dispatch<Msg>) (messages: Msg list) =
    let producer = onThread (fun () -> messages |> List.iter guarded)

    if not (producer.Join(TimeSpan.FromSeconds 10.0)) then
        failtest "producer thread did not finish"

[<Tests>]
let tests =
    testList
        "Issue180 LoopDispatch"
        [ test "guard runs a loop-thread dispatch inline" {
            let gate = LoopDispatch.forCurrentThread<Msg> ()
            let seen = ResizeArray<Msg>()
            let guarded = LoopDispatch.guard gate (fun msg -> seen.Add msg)

            guarded LoopInput

            Expect.sequenceEqual seen [ LoopInput ] "a loop-thread dispatch must not be deferred"
            Expect.equal (LoopDispatch.pending gate) 0 "nothing should have been queued"
          }

          test "guard queues a dispatch raised off the loop thread" {
              let gate = LoopDispatch.forCurrentThread<Msg> ()
              let seen = ResizeArray<Msg>()
              let guarded = LoopDispatch.guard gate (fun msg -> seen.Add msg)

              queueOffThread guarded [ LoopInput ]

              Expect.isEmpty seen "an off-thread dispatch must not run inline"
              Expect.equal (LoopDispatch.pending gate) 1 "it must be queued for the loop to drain"
          }

          test "drain replays queued messages on the draining thread, in order" {
              let gate = LoopDispatch.forCurrentThread<Msg> ()
              let queued = [ Tick(TimeSpan.FromMilliseconds 1.0); LoopInput; Tick(TimeSpan.FromMilliseconds 2.0) ]

              queueOffThread (LoopDispatch.guard gate ignore) queued

              let seen = ResizeArray<Msg>()
              let threads = ResizeArray<int>()

              let ran =
                  LoopDispatch.drain gate (fun msg ->
                      threads.Add Environment.CurrentManagedThreadId
                      seen.Add msg)

              Expect.equal ran 3 "drain reports how many it replayed"
              Expect.sequenceEqual seen queued "FIFO order is preserved"
              Expect.equal (Set.ofSeq threads) (Set.ofList [ Environment.CurrentManagedThreadId ]) "replay happens on the draining thread"
              Expect.equal (LoopDispatch.pending gate) 0 "the queue is emptied"
          }

          test "drain off the loop thread replays nothing and keeps the queue" {
              let gate = LoopDispatch.forCurrentThread<Msg> ()

              queueOffThread (LoopDispatch.guard gate ignore) [ LoopInput ]

              let mutable ran = -1
              let mutable escaped = 0

              let drainer =
                  onThread (fun () -> ran <- LoopDispatch.drain gate (fun _ -> escaped <- escaped + 1))

              if not (drainer.Join(TimeSpan.FromSeconds 10.0)) then
                  failtest "drainer thread did not finish"

              Expect.equal ran 0 "draining off the loop thread must replay nothing"
              Expect.equal escaped 0 "…and must not run a single message"
              Expect.equal (LoopDispatch.pending gate) 1 "the message stays queued for the real loop"
          }

          test "drain is bounded by the depth it observed on entry" {
              let gate = LoopDispatch.forCurrentThread<Msg> ()

              queueOffThread (LoopDispatch.guard gate ignore) (List.replicate 64 LoopInput)

              let ran = LoopDispatch.drain gate ignore

              Expect.equal ran 64 "every message queued before entry is replayed"
              Expect.equal (LoopDispatch.pending gate) 0 "and none is left behind"
          }

          // The witness: this is what the host did before the gate existed. It asserts only the
          // thread, which is certain — a threadpool callback is never the loop thread. The torn reads
          // that follow from it are a race, so they are left unasserted here rather than made into a
          // test that can go red on a machine where the timer never happens to collide.
          test "ungated, the animation tick really does dispatch off the loop thread" {
              let report = simulateRun false

              Expect.isGreaterThan report.Ticks 1 "the recurring timer must fire past the immediate first frame"

              Expect.isTrue
                  (report.UpdateThreads |> Set.exists (fun threadId -> threadId <> report.LoopThreadId))
                  "without the gate, update runs on the timer's threadpool thread — the race this issue reports"
          }

          // The criterion: the animation subscription drives the loop, and the model is never torn.
          test "gated, the animation tick only updates on the loop thread and never tears the model" {
              let report = simulateRun true

              Expect.isGreaterThan report.Ticks 1 "the recurring timer must fire, or this proves nothing"

              Expect.equal
                  report.UpdateThreads
                  (Set.ofList [ report.LoopThreadId ])
                  "every update must run on the loop thread"

              Expect.isEmpty (tornReads report.Observed) "a monotonic counter must never repeat or regress"
          } ]

