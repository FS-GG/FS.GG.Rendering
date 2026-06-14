namespace Rendering.Harness

/// Tier executors. T0 (deterministic render + retained routing, non-blank offscreen PNG) and
/// T1 (offscreen GPU/CPU readback) need no live desktop — they drive the viewer's offscreen
/// evidence path. Each produces `Evidence` carrying the proof scope from `RunPlan`.
module Tiers =

    /// Run an offscreen tier (T0 or T1) headlessly: render the deterministic demo scene via the
    /// viewer's offscreen readback, assert non-blank (and, for T0, byte-identical re-render), and
    /// build the evidence. Returns the evidence and per-"frame" durations (ms).
    val runOffscreen: tier: Tier -> facts: ProbeFacts -> outDir: string -> Evidence.Evidence * float list
