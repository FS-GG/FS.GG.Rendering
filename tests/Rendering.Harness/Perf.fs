namespace Rendering.Harness

open System
open System.IO
open System.Diagnostics
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module Perf =

    type PerfMode =
        | Throughput
        | Paced60
        | PacedNative
        | StressResize
        | InputLatency

    type PerfKind =
        | DeterministicKind
        | LiveHostKind
        | TimingKind

    let parseMode token =
        match token with
        | "throughput" -> Some Throughput
        | "paced-60" -> Some Paced60
        | "paced-native" -> Some PacedNative
        | "stress-resize" -> Some StressResize
        | "input-latency" -> Some InputLatency
        | _ -> None

    let kindOf mode =
        match mode with
        | Throughput -> TimingKind
        | Paced60 -> TimingKind
        | PacedNative -> TimingKind
        | StressResize -> LiveHostKind
        | InputLatency -> TimingKind

    let scene: SceneNode = Rectangle((20.0, 20.0, 160.0, 120.0), Colors.white)

    // One offscreen render, timed (ms). Uses the proven headless capture path.
    let renderOnce (path: string) : float =
        let request: ScreenshotEvidenceRequest =
            { Command = "perf"
              AppOrSample = "harness"
              OutputPath = path
              Width = 200
              Height = 160
              RendererMode = "viewer-render-target"
              CaptureMode = ViewerRenderTargetPng
              HostFacts = []
              Timeout = TimeSpan.FromSeconds 10.0 }
        let options: ViewerOptions =
            { Title = "harness-perf"
              InitialSize = { Width = 200; Height = 160 }
              PresentMode = ViewerPresentMode.OffscreenReadback
              FrameRateCap = None }
        let sw = Stopwatch.StartNew()
        Viewer.captureScreenshotEvidence request options scene |> ignore
        sw.Stop()
        sw.Elapsed.TotalMilliseconds

    let runPerf (mode: PerfMode) (frames: int) (facts: ProbeFacts) (outDir: string) : Evidence.Evidence * float list =
        let p = RunPlan.plan T3 facts
        let n = max 1 frames
        Directory.CreateDirectory outDir |> ignore
        let framePath = Path.Combine(outDir, "perf-frame.png")
        // Real per-frame OFFSCREEN render timing (not live-present / vsync). Warm up once.
        renderOnce framePath |> ignore
        let frameMs = [ for _ in 1..n -> renderOnce framePath ]
        let p50, p95, p99 = Evidence.percentiles frameMs
        // Honest scope: this is offscreen render throughput. It is NOT live-present timing and NOT
        // vsync-faithful — RunPlan already withholds vsync-faithful (no present facts), and we add
        // the live-present caveats so the artifact cannot be read as faithful frame pacing.
        let evidence: Evidence.Evidence =
            { Evidence.RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
              Evidence.Tier = T3
              Evidence.Subcommand = "perf"
              Evidence.Status = RunStatus.Passed
              Evidence.SkipReason = None
              Evidence.ProofLevel = p.ClaimableProof
              Evidence.AuthoritativeFor = [ "offscreen-render-throughput" ]
              Evidence.NotAuthoritativeFor =
                p.NotAuthoritativeFor @ [ "paint-compose-swap-timing"; "live-present-timing" ]
              Evidence.Facts = facts
              Evidence.Frames = n
              Evidence.P50Ms = p50
              Evidence.P95Ms = p95
              Evidence.P99Ms = p99
              Evidence.Artifacts = [ "metrics.csv"; "summary.md" ] }
        evidence, frameMs
