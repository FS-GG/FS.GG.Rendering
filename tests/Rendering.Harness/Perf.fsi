namespace Rendering.Harness

/// Performance tier (T3). Drives the viewer's bounded-run path to record render-timing evidence.
/// It declares whether a mode is deterministic / live-host / timing evidence, and — critically —
/// only claims `vsync-faithful` when the probe supplies present facts (swap-control + vblank).
/// The headless bounded path yields throughput timing, NOT a faithful per-frame vsync distribution
/// (that needs the live tier); the evidence discloses this so it cannot overclaim.
module Perf =

    type PerfMode =
        | Throughput
        | Paced60
        | PacedNative
        | StressResize
        | InputLatency

    /// The evidence kind a mode produces.
    type PerfKind =
        | DeterministicKind
        | LiveHostKind
        | TimingKind

    /// Parse a `--mode` token; `None` if unrecognised.
    val parseMode: token: string -> PerfMode option

    /// The evidence kind declared by a mode.
    val kindOf: mode: PerfMode -> PerfKind

    /// Run the T3 perf tier for `mode` over `frames` bounded frames; build the evidence.
    val runPerf: mode: PerfMode -> frames: int -> facts: ProbeFacts -> outDir: string -> Evidence.Evidence * float list
