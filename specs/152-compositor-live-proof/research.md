# Research: Compositor Live Proof Acceptance

## Decision: Treat Feature 152 as the live acceptance closeout for P7

**Rationale**: Feature 149 already completed the deterministic P7 readiness package: proof
contracts, damage-scoped fallback rules, parity and timing evidence formats, public diagnostics,
and readiness output. The remaining gap is not a new compositor architecture. It is the capable-host
acceptance decision that Feature 149 could not claim while the live proof remained
environment-limited.

**Alternatives considered**:

- Re-plan P7 from scratch: rejected because it would duplicate accepted Feature 147-149 contracts
  and increase drift.
- Fold Feature 151 P8 layout evidence into this work: rejected because P8 is already accepted and
  this feature must not reopen layout scope.
- Accept Feature 149 deterministic evidence as sufficient: rejected because the report requires
  live capable-host proof before accepting partial redraw or performance claims.

## Decision: Require a three-run accepted proof set

**Rationale**: One passing live proof can be an accident of timing, stale artifacts, or host state.
Accepted partial redraw requires at least three fresh matching runs for the same host profile and
proof method. Each run must prove both sides of the safety property: damaged pixels update through
the scoped path, and undamaged pixels remain valid from the prior presentation. Artifact quality is
part of the gate, so missing, blank, stale, synthetic-only, host-mismatched, or method-mismatched
evidence fails closed.

**Alternatives considered**:

- Accept one capable-host run: rejected because it is too weak for a presentation-path safety
  claim.
- Accept runs across different host profiles: rejected because preservation depends on display
  server, renderer, driver, framebuffer setup, scale, and proof algorithm.
- Use synthetic simulations to complete the run set: rejected because simulations bypass the host
  that will preserve or corrupt pixels.

## Decision: Keep environment-limited as a non-accepting verdict

**Rationale**: The repository must behave honestly on hosts without a usable display, GL context,
readback path, permission, or timing capability. Such runs should complete quickly, record why
evidence is limited, and keep partial redraw fallback-gated. This distinguishes an unavailable
environment from both a capable-host failure and an accepted proof.

**Alternatives considered**:

- Treat environment-limited as skipped and omit readiness impact: rejected because reviewers need
  one summary that explains why acceptance was not recorded.
- Treat environment-limited as failure: rejected because unsupported CI or host setup may not be a
  compositor defect.
- Override environment limits with deterministic harness output: rejected because deterministic
  evidence cannot establish live presenter preservation.

## Decision: Require same-profile live parity before accepting damage-scoped output

**Rationale**: A host that preserves untouched pixels still must produce the same visible result as
the full-redraw reference for representative frame transitions. Parity evidence must be tied to the
same accepted host profile and proof method, include fallback reasons, and reject scoped redraw for
invalid damage, frame-wide invalidation, resize, host-profile drift, resource failure, or parity
failure.

**Alternatives considered**:

- Use deterministic parity only: rejected because the open gap is live partial-redraw acceptance.
- Compare only localized updates: rejected because no-change, movement, resize, full-frame
  invalidation, and invalid-damage scenarios protect the fallback boundary.
- Accept scoped output without a full-redraw oracle: rejected because P7 correctness is defined by
  visible parity against full redraw.

## Decision: Decide performance claims from same-profile repeated timing evidence

**Rationale**: A correct compositor is not automatically faster. Any performance claim must compare
full redraw and damage-scoped redraw on representative live scenarios for the same accepted host
profile, with enough repetitions to separate warmup, noise, and non-beneficial cases. Incomplete,
environment-limited, noisy, or non-beneficial measurements must explicitly reject or mark the claim
inconclusive.

**Alternatives considered**:

- Claim benefit from reduced damage area or cache-hit counters: rejected because the claim is about
  measured live host behavior.
- Accept timing from a different host profile than the proof/parity profile: rejected because host
  and driver facts materially affect results.
- Report only the best-case scenario: rejected because non-beneficial or churning scenarios must
  prevent overclaiming.

## Decision: Publish one readiness decision package

**Rationale**: Consumers and reviewers should not infer P7 status from scattered proof logs. The
Feature 152 readiness package must aggregate proof status, accepted proof set membership, live
parity, timing decision, fallback status, compatibility impact, and limitations. Any consumer-
visible diagnostic or package-surface change must be documented in the compatibility ledger and
validated through package/surface checks.

**Alternatives considered**:

- Leave evidence only in command output: rejected because FR-016 requires one reviewable readiness
  entry point.
- Publish only a binary accepted/not-accepted flag: rejected because environment-limited, failed,
  fallback-gated, and performance-rejected states require different reviewer actions.
- Delay compatibility documentation until implementation review: rejected because this is a Tier 1
  contracted change and public drift must be controlled during implementation.

## Decision: Avoid new dependencies and reuse Feature 149 routing

**Rationale**: Existing project boundaries and harness commands are sufficient: `SkiaViewer` owns
live host proof, `Controls` owns damage/fallback policy, `Controls.Elmish` owns public frame
diagnostics, `Rendering.Harness` owns artifact/timing/readiness orchestration, and `Testing` owns
consumer-facing helpers. Feature 152 should add feature routing, run-set aggregation, evidence
quality checks, and readiness decision logic without broad new packages.

**Alternatives considered**:

- Add a separate proof runner or external benchmarking tool: rejected because it would complicate
  package validation and duplicate harness responsibilities.
- Move proof orchestration into product packages: rejected because filesystem/process/timing I/O
  belongs at the harness edge.
- Add new public Scene or layout primitives: rejected because this feature does not change the
  authoring protocol.
