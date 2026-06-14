namespace Rendering.Harness

/// Pure tier planning: given a tier and the probe facts, decide the claimable proof scope and
/// whether the tier runs, skips, or is failed-classified. No I/O — the testable core that
/// enforces "no overclaim" (every plan lists what it is NOT authoritative for) and "safe
/// failure" (missing capability degrades, never crashes).
module RunPlan =

    /// The pure decision for one tier run.
    type RunPlan =
        { Tier: Tier
          ClaimableProof: ProofLevel
          AuthoritativeFor: string list
          /// Always non-empty for every tier — the claims this run may NOT make.
          NotAuthoritativeFor: string list
          Degradation: Degradation
          /// True only when the tier is T3 AND present facts (swap-control + vblank source)
          /// are both known. `vsync-faithful` may be claimed iff this is true.
          VsyncFaithfulAllowed: bool }

    /// Compute the run plan for `tier` given probe `facts`.
    val plan: tier: Tier -> facts: ProbeFacts -> RunPlan
