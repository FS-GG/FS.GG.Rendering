# Research: Compositor Proof Acceptance

## Decision: Feature 154 extends Feature 153 instead of redefining proof policy

**Rationale**: Feature 153 already introduced the live proof interpreter, selected-attempt
identity, host-readiness classification, proof-set readiness tokens, and exact-three proof-set
evaluation. Feature 154 is the acceptance closeout: it must produce or reject real capable-host
evidence, then connect proof, parity, timing, fallback, compatibility, and limitations in one
readiness package.

**Alternatives considered**:

- Redefine P7 readiness policy in a new module. Rejected because FR-002 requires Feature 153's
  vocabulary to remain authoritative.
- Treat Feature 152 as the primary model. Rejected because Feature 153 superseded it with proof
  interpreter and selected-attempt identity details that this feature must use.

## Decision: Accept proof only from exactly three fresh matching capable-host attempts

**Rationale**: The existing `CompositorProof.evaluateProofSet` shape already models the required
gate: three accepted attempts, same host profile, same proof method, fresh artifacts, accepted
artifact quality, damaged-pixel update, and undamaged-pixel preservation. Feature 154 should add
Feature 154 command routing, artifacts, and readiness summaries around that behavior rather than
weakening the existing fail-closed semantics.

**Alternatives considered**:

- Accept one successful live attempt. Rejected because the spec and report require repeatability.
- Accept three attempts across similar host profiles. Rejected because host-profile drift can hide
  environment-specific presentation behavior.
- Accept synthetic or copied artifacts for acceptance. Rejected because constitution evidence rules
  and FR-005 require fresh, real, non-synthetic artifacts.

## Decision: Keep unsupported hosts as regression evidence with zero accepted artifacts

**Rationale**: Unsupported or unavailable presentation environments are expected in CI and on
developer machines without display/readback capability. They still validate safe failure: the
system must classify the run as `environment-limited`, record the reason, finish quickly, and keep
full redraw active.

**Alternatives considered**:

- Skip unsupported-host runs entirely. Rejected because FR-008 and FR-017 require reviewer-visible
  unsupported-host behavior.
- Treat unsupported evidence as partial acceptance. Rejected because it would allow environment
  limits to unlock partial redraw.

## Decision: Parity uses the accepted proof host profile

**Rationale**: A proof set only shows that a host can preserve undamaged pixels for the proof
method. It does not prove representative compositor scenarios. The damage-scoped parity corpus must
run against the same accepted host profile and compare final visible output with the full-redraw
reference for localized update, no-change, movement, overlap, edge clipping, resize, full
invalidation, invalid damage, unsupported host, and resource-failure paths.

**Alternatives considered**:

- Reuse deterministic parity from prior features as acceptance. Rejected because the spec requires
  same-profile live parity for this readiness decision.
- Run parity on a different available host. Rejected because cross-profile parity cannot unlock the
  accepted proof profile.

## Decision: Timing is an explicit claim decision, separate from safety readiness

**Rationale**: Performance claims require a declared threshold/noise policy, at least five
representative live scenarios, and at least five comparable repetitions per scenario. The timing
decision can be accepted, rejected, or inconclusive. Rejected or inconclusive timing prevents a
performance benefit claim but does not by itself prove partial redraw unsafe when proof and parity
are accepted.

**Alternatives considered**:

- Infer performance benefit from damage reduction counters. Rejected because snapshot/reuse/damage
  counters are context-only without same-profile live timing.
- Require a positive performance claim for safety acceptance. Rejected because the spec separates
  correctness proof from marketing/performance claims.

## Decision: Publish one readiness package under Feature 154

**Rationale**: Reviewers need one entry point that names the accepted host profile, selected proof
attempts, parity status, timing status, fallback state, compatibility impact, package validation,
regression validation, artifact paths, and remaining limitations. Feature 154 must not require
reviewers to infer acceptance from scattered Feature 152 and Feature 153 files.

**Alternatives considered**:

- Update only Feature 153 readiness files. Rejected because this feature has its own acceptance,
  parity, timing, compatibility, and final verdict scope.
- Publish only machine-readable artifacts. Rejected because the spec requires reviewer-visible
  reasons and a summary inspectable in under five minutes.

## Decision: Preserve Tier 1 `.fsi` and package validation discipline

**Rationale**: The feature can change public diagnostics, readiness status, fallback behavior,
package-facing helpers, and performance claims. Public deltas require `.fsi` updates before
implementation, semantic tests, surface baselines, compatibility notes, package validation, and
focused regression evidence.

**Alternatives considered**:

- Treat readiness files as internal-only documentation. Rejected because consumer-facing fallback,
  diagnostics, and readiness claims are observable behavior in this repository.
