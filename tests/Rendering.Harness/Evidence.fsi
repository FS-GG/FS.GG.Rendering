namespace Rendering.Harness

/// The evidence-artifact contract: `run.json` + `metrics.csv` + `summary.md`. The formatters are
/// pure (testable without I/O); `write` persists them. Every artifact carries `proofLevel` and a
/// non-empty `notAuthoritativeFor` so it cannot overclaim.
module Evidence =

    /// One run's machine-readable evidence (the `run.json` shape).
    type Evidence =
        { RunId: string
          Tier: Tier
          Subcommand: string
          Status: RunStatus
          SkipReason: string option
          ProofLevel: ProofLevel
          AuthoritativeFor: string list
          NotAuthoritativeFor: string list
          Facts: ProbeFacts
          Frames: int
          P50Ms: float option
          P95Ms: float option
          P99Ms: float option
          Artifacts: string list }

    /// Overlay parity evidence gathered by Feature 144 harness tests.
    type OverlayEvidence =
        { ReplayLog: string list
          ProductMessages: string list
          HitOrder: string list
          Diagnostics: string list }

    /// Stable string forms used in the artifacts.
    val tierToken: tier: Tier -> string
    val proofToken: proof: ProofLevel -> string
    val statusToken: status: RunStatus -> string
    val backendToken: backend: Backend -> string

    /// Render the `run.json` body (pure).
    val toJson: evidence: Evidence -> string

    /// Render `metrics.csv` from per-frame durations in milliseconds (pure).
    val metricsCsv: frameMs: float list -> string

    /// Render the human `summary.md` restating what the run proves and does NOT prove (pure).
    val toSummary: evidence: Evidence -> string

    /// Stable one-line summary for overlay parity evidence.
    val overlaySummary: evidence: OverlayEvidence -> string

    /// Compute p50/p95/p99 (ms) from per-frame durations; `None` when there are no frames.
    val percentiles: frameMs: float list -> (float option * float option * float option)

    /// Persist `run.json`, `metrics.csv`, and `summary.md` into `dir`; returns the `run.json` path.
    val write: dir: string -> evidence: Evidence -> frameMs: float list -> string
