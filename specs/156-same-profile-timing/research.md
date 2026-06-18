# Research: Same-Profile Timing Evidence

## Decision: Bind Feature 156 timing to the Feature 155 accepted host profile

**Rationale**: Feature 155 accepted P7 live partial-redraw correctness only for stable host profile
`probe-08a47c01`, with proof and same-profile parity accepted and timing still inconclusive.
Performance timing is meaningful only when full-redraw and damage-scoped samples come from that
same profile, run identity, renderer identity, display environment, package version, and scenario
definition.

**Alternatives considered**:

- Accept timing from any capable OpenGL host. Rejected because cross-profile timing can hide
  compositor, driver, display-server, and refresh differences.
- Reuse Feature 155 timing output. Rejected because it recorded an inconclusive decision and did
  not provide comparable distributions for both paths.

## Decision: Add `compositor-performance --feature 156` as the canonical command

**Rationale**: The report explicitly asks for `compositor-performance --feature 156`. Existing
`compositor-timing` output stays useful for prior Feature 148/149/152/154/155 context, but Feature
156 needs a clearer command that owns sample collection, policy evaluation, raw distributions,
scenario verdicts, and the final summary.

**Alternatives considered**:

- Extend only `compositor-timing`. Rejected because older commands have context-only and
  inconclusive semantics that should not be overloaded with Feature 156 acceptance rules.
- Add an external benchmark runner. Rejected because the repository already has probe, viewer,
  evidence, and readiness infrastructure and the constitution minimizes new dependencies.

## Decision: Use quantified policy `same-profile-live-threshold-v2`

**Rationale**: Feature 154 introduced `same-profile-live-threshold-v1`, but it left the positive
threshold qualitative. Feature 156 needs a predeclared numeric rule before evaluating evidence.
The policy uses a per-scenario noise band of `max(0.25 ms, 5% of full-redraw p50)`. A scenario is
positive only when damage-scoped p50 and p95 are each faster than full redraw by at least that
band, and damage-scoped p99 is not worse than full-redraw p99 by more than the same band.

**Alternatives considered**:

- Use only p50 improvement. Rejected because p50 alone can hide tail regressions.
- Use a fixed percentage only. Rejected because tiny millisecond values need an absolute floor.
- Accept results inside the noise band. Rejected because the feature must fail closed on noisy or
  marginal evidence.

## Decision: Require five damage-benefit scenarios for a positive Feature 156 timing result

**Rationale**: The minimum positive-decision scenario set is:
`timing/localized-update`, `timing/no-change`, `timing/movement-old-new`, `timing/overlap`, and
`timing/edge-clipping`. These cover localized repaint, idle/no-op, old/new placement damage,
overlap union accounting, and edge clipping without depending on the later full no-clear renderer.
Additional fallback or stress scenarios such as resize, full invalidation, unsupported host, and
resource failure are useful rejection evidence but cannot substitute for the five positive-decision
scenarios.

**Alternatives considered**:

- Count resize/full-invalidation as required positive scenarios. Rejected because safe full-redraw
  fallback can be the correct behavior and should not be forced into a performance-benefit gate.
- Accept fewer than five scenarios with more repetitions. Rejected because the spec requires at
  least five representative scenarios before any positive decision.

## Decision: Use default warmup `3` and measured repetitions `5`, both recorded per path

**Rationale**: The spec requires documented warmup and at least five comparable measured
repetitions per scenario per path. A small fixed default warmup keeps the command bounded while
making startup effects explicit. More repetitions may be configured, but fewer than five measured
samples after warmup is always incomplete.

**Alternatives considered**:

- No warmup. Rejected because first-use Skia/OpenGL and cache effects would pollute the evidence.
- Make the default much larger. Rejected because Feature 156 should provide a bounded reviewer
  workflow; heavier repeated timing belongs in the later validation-throughput work.

## Decision: Record minimal lane facts now and leave the full performance lane ledger to Feature 161

**Rationale**: Feature 156 must reject mixed host profiles, display environments, renderer
identities, package versions, run identities, and scenario definitions. It therefore records those
facts directly in each timing run and scenario artifact. Feature 161 remains responsible for a
broader reusable lane ledger with driver, load, refresh, and environment notes across runs.

**Alternatives considered**:

- Wait for Feature 161 before collecting timing. Rejected because Feature 156 is explicitly the
  next follow-up and can still fail closed with local host facts.
- Build the full ledger now. Rejected because it expands scope beyond the same-profile timing
  evidence package.

## Decision: Keep Feature 156 timing separate from shipped P7 performance acceptance

**Rationale**: The report requires later gates before a shipped compositor performance claim:
Feature 157 damage-scissored rendering, Feature 158 readback separation, Feature 159 net-positive
reuse/promotion counters, and Feature 161 lane scoping. Feature 156 may report a positive timing
result for the measured profile, but the shipped P7 performance claim remains
`performance-not-accepted` until the later gates are present and same-profile positive.

**Alternatives considered**:

- Let a positive Feature 156 result ship the performance claim. Rejected because the report's P7
  performance acceptance rule requires later gates.
- Block correctness readiness on Feature 156 timing. Rejected because Feature 155 correctness is
  already accepted for the stable profile and remains a separate safety claim.
