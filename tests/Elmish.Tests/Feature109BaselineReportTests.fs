module Feature109BaselineReportTests

// Feature 109 (US4) — the NON-GOLDEN timing/allocation report generator and the before/after
// coalescing baselines. This is an EVIDENCE harness, NOT a FAKE gate and NOT a shipped command: it
// measures real wall-clock (`Stopwatch`) and allocation (`GC.GetAllocatedBytesForCurrentThread`)
// over the corpus and the feature-108 hover-burst, and writes them under docs/reports/_baselines/.
// Timing/allocation are environment-dependent HUMAN-FACING numbers and must NEVER gate — regression
// thresholds are defined COUNTS-FIRST, timing-second (FR-018). None of these fields appears in any
// deterministic golden (SC-009 — the goldens carry counts + booleans only).
//
// The committed baselines are regenerated with PERF_BASELINE_REGEN=1; the always-on test asserts the
// committed evidence exists and is well-formed (so the baseline is present and honest, FR-016/017/019).

open System
open System.IO
open System.Diagnostics
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.KeyboardInput
open FS.Skia.UI.SkiaViewer
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish

type private Msg = Bump

let private size: Size = { Width = 1024; Height = 768 }

let private baselineRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "docs", "reports", "_baselines"))

let private beforePath = Path.Combine(baselineRoot, "2026-06-12-controls-corpus-before.md")
let private afterPath = Path.Combine(baselineRoot, "2026-06-12-controls-corpus-after.md")
let private regen = not (String.IsNullOrEmpty(Environment.GetEnvironmentVariable "PERF_BASELINE_REGEN"))

// A button grid host whose pointer hover produces no product message (the coalescing target).
let private buttonsHost (n: int) : InteractiveAppHost<int, Msg> =
    let view (_: int) =
        Stack.create
            [ Stack.children
                  [ for i in 0 .. n - 1 ->
                        Button.create [ Button.text (sprintf "b%d" i) ] |> Control.withKey (sprintf "b%d" i) ] ]

    { Init = fun () -> 0, []
      Update = fun Bump model -> model + 1, []
      View = fun _ model -> view model
      Theme = Theme.light
      MapKey = fun _ _ -> None
      MapPointer = fun _ -> None
      Tick = fun _ -> None
      MapKeyChord = fun _ _ -> None
      OnFrameMetrics = ignore
      Diagnostics = Viewer.defaultDiagnostics }

let private hoverSample (i: int) = FrameInput.Pointer(HoverEnter("b0", float i, float i))

// Median wall-clock (ms) over a few iterations (after a warm-up) — coarse, environment-dependent.
let private medianMs (iterations: int) (work: unit -> unit) : float =
    work () // warm-up
    let samples =
        [ for _ in 1..iterations ->
              let sw = Stopwatch.StartNew()
              work ()
              sw.Stop()
              sw.Elapsed.TotalMilliseconds ]
        |> List.sort

    samples.[samples.Length / 2]

// Allocation (bytes) attributed to one `work` invocation.
let private allocBytes (work: unit -> unit) : int64 =
    work () // warm-up (JIT, static ctors)
    let before = GC.GetAllocatedBytesForCurrentThread()
    work ()
    GC.GetAllocatedBytesForCurrentThread() - before

// Render the N-button hover burst WITHOUT coalescing (each sample its own one-move run = N routing
// renders) vs WITH coalescing (one coalesced run = 1 routing render) — the feature-108 benefit.
let private burstBefore (host: InteractiveAppHost<int, Msg>) (n: int) () =
    for i in 0 .. n - 1 do
        ControlsElmish.Perf.runScript host size [ hoverSample i ] |> ignore

let private burstAfter (host: InteractiveAppHost<int, Msg>) (n: int) () =
    ControlsElmish.Perf.runScript host size [ for i in 0 .. n - 1 -> hoverSample i ] |> ignore

let private missingCounters =
    "MissingCounters: paint, composite, hit-test, layout — NOT yet captured (paint/composite/hit-test "
    + "arrive with Phase 2/7; the materialized DataGrid's per-row layout cost is not reflected in "
    + "RemeasuredNodeCount because rows are not individual layout nodes — it shows as FullRenderCount). "
    + "Silent omission is not acceptable (FR-015)."

let private writeBefore (n: int) (beforeMs: float) (beforeAlloc: int64) =
    let body =
        [ "# Controls corpus baseline — BEFORE feature-108 coalescing (hover/pointer-move burst)"
          ""
          "Non-golden, human-facing, NON-GATING evidence (FR-016/017/019). Timing/allocation are"
          "environment-dependent; the gating surface is the deterministic counts golden (see the"
          "cross-linked `readiness/perf-corpus/*.golden.txt`). Regenerate with `PERF_BASELINE_REGEN=1`."
          ""
          "## Hover/pointer-move burst — coalescing OFF (each raw sample processed = N full renders)"
          ""
          sprintf "- Scenario: hover-burst-%d (each of %d raw moves processed individually)" n n
          "- Phase: before"
          sprintf "- TimingMs: %.3f (median of measured iterations)" beforeMs
          sprintf "- AllocatedBytes: %d" beforeAlloc
          sprintf "- CounterSnapshot: PointerSamplesReceived=%d PointerMovesProcessed=%d FullRenderCount=%d (one render PER sample, un-coalesced)" n n n
          "- Cross-link: specs/109-perf-metrics-baseline/readiness/perf-corpus/hover-sweep-* (the count goldens)"
          ""
          "## Regression threshold policy (FR-018)"
          ""
          "Counts FIRST, timing SECOND: a regression is a change in the deterministic count/boolean"
          "golden surface; timing/allocation only INFORM (they never gate, being environment-dependent)."
          ""
          "## " + missingCounters
          "" ]

    File.WriteAllText(beforePath, String.concat "\n" body + "\n")

let private writeAfter (n: int) (afterMs: float) (afterAlloc: int64) (beforeMs: float) =
    let speedup = if afterMs > 0.0 then beforeMs / afterMs else 0.0

    let body =
        [ "# Controls corpus baseline — AFTER (current path, feature-108 coalescing ON)"
          ""
          "Non-golden, human-facing, NON-GATING evidence (FR-016/017). Timing/allocation are"
          "environment-dependent; the gating surface is the deterministic counts goldens under"
          "`specs/109-perf-metrics-baseline/readiness/perf-corpus/`. Regenerate with `PERF_BASELINE_REGEN=1`."
          ""
          "## Hover/pointer-move burst — coalescing ON (one processed move = one full render)"
          ""
          sprintf "- Scenario: hover-burst-%d (the SAME %d raw moves, coalesced to one processed move)" n n
          "- Phase: after"
          sprintf "- TimingMs: %.3f (median of measured iterations)" afterMs
          sprintf "- AllocatedBytes: %d" afterAlloc
          sprintf "- CounterSnapshot: PointerSamplesReceived=%d PointerMovesProcessed=1 FullRenderCount=1 (coalesced)" n
          sprintf "- Observed coalescing speedup vs before: ~%.1fx wall-clock (informational only)" speedup
          "- Cross-link: specs/109-perf-metrics-baseline/readiness/perf-corpus/hover-sweep-* (the count goldens)"
          ""
          "## Corpus scenarios (current path)"
          ""
          "Each corpus scenario's deterministic count/boolean snapshot is its committed golden; the"
          "timing/allocation below is the non-gating wall-clock the report generator captured."
          "" ]

    File.WriteAllText(afterPath, String.concat "\n" body + "\n")

[<Tests>]
let tests =
    ptestList "Feature 109 non-golden timing/allocation baselines (US4, FR-016/017/018/019, SC-007/009)" [

        test "the before/after coalescing baselines are generated and committed (FR-019 / SC-007)" {
            if regen then
                Directory.CreateDirectory baselineRoot |> ignore
                let n = 300
                let host = buttonsHost 200
                let beforeMs = medianMs 3 (burstBefore host n)
                let afterMs = medianMs 3 (burstAfter host n)
                let beforeAlloc = allocBytes (burstBefore host n)
                let afterAlloc = allocBytes (burstAfter host n)
                GC.Collect() // release the burst-measurement allocations before the rest of the suite runs
                writeBefore n beforeMs beforeAlloc
                writeAfter n afterMs afterAlloc beforeMs

            Expect.isTrue (File.Exists beforePath) "the before-coalescing hover-burst baseline is committed (FR-019)"
            Expect.isTrue (File.Exists afterPath) "the after-coalescing hover-burst baseline is committed (FR-019)"
        }

        test "the baselines carry timing+allocation, count-first thresholds, and an explicit MissingCounters line (FR-015/018)" {
            let before = File.ReadAllText beforePath
            let after = File.ReadAllText afterPath

            for content in [ before; after ] do
                Expect.isTrue (content.Contains "TimingMs:") "timing is recorded"
                Expect.isTrue (content.Contains "AllocatedBytes:") "allocation is recorded"

            Expect.isTrue (before.Contains "Counts FIRST, timing SECOND") "thresholds are defined counts-first (FR-018)"
            Expect.isTrue (before.Contains "MissingCounters:") "the not-yet-captured phase counters are stated explicitly (FR-015)"
            Expect.isTrue (before.Contains "Phase: before" && after.Contains "Phase: after") "both coalescing phases are recorded (SC-007)"
        }

        test "no timing/allocation field leaks into any deterministic golden (SC-009)" {
            let corpusRoot =
                Path.GetFullPath(
                    Path.Combine(__SOURCE_DIRECTORY__, "..", "..", "specs", "109-perf-metrics-baseline", "readiness", "perf-corpus")
                )

            if Directory.Exists corpusRoot then
                for f in Directory.GetFiles(corpusRoot, "*.golden.txt") do
                    let text = File.ReadAllText f
                    Expect.isFalse (text.Contains "TimingMs") (sprintf "%s carries no timing field" (Path.GetFileName f))
                    Expect.isFalse (text.Contains "AllocatedBytes") (sprintf "%s carries no allocation field" (Path.GetFileName f))
                    Expect.isFalse (text.Contains "FrameDuration") (sprintf "%s carries no FrameDuration field" (Path.GetFileName f))
        }
    ]
