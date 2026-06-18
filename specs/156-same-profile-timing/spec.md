# Feature Specification: Same-Profile Timing Evidence

**Feature Branch**: `156-same-profile-timing`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers Feature 156 from the radical rendering architecture report: same-profile compositor timing evidence. Feature 155 accepted P7 partial-redraw correctness for the current stable host profile, but no compositor performance claim is accepted. This feature creates a repeatable evidence package that compares full redraw against damage-scoped redraw on one stable host profile, records distributions and noise decisions, rejects mixed or incomplete evidence, and keeps the shipped performance claim unaccepted unless the report's later performance gates are also satisfied.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Compare Timing on One Accepted Host Profile (Priority: P1)

Release reviewers need comparable full-redraw and damage-scoped timing evidence from the same stable host profile, so they can tell whether partial redraw is measurably faster in the environment where correctness is already accepted.

**Why this priority**: P7 correctness is accepted for one profile, but performance remains unaccepted. Timing evidence is only meaningful when both paths are measured on the same profile under comparable conditions.

**Independent Test**: Can be tested by running the timing evidence flow for the accepted host profile and verifying that at least five representative scenarios each contain comparable full-redraw and damage-scoped samples after warmup.

**Acceptance Scenarios**:

1. **Given** an accepted P7 host profile and representative compositor scenarios, **When** timing evidence is collected, **Then** each scenario records comparable full-redraw and damage-scoped measurements for the same host profile.
2. **Given** the timing flow starts for a scenario, **When** warmup and measurement complete, **Then** the evidence records warmup count, measured sample count, p50, p95, p99, noise band, confidence decision, and artifact locations.
3. **Given** fewer than five representative scenarios or fewer than five comparable measured repetitions per scenario, **When** timing evidence is evaluated, **Then** no positive timing decision is accepted.

---

### User Story 2 - Reject Noisy, Mixed, or Incomplete Evidence (Priority: P1)

Maintainers need the timing gate to fail closed when samples are noisy, incomplete, cross-profile, environment-limited, or not beneficial, so a weak benchmark cannot become a package-facing performance claim.

**Why this priority**: Performance evidence is easy to overread. The report requires same-profile timing with positive results outside a declared noise threshold before any performance result can be considered.

**Independent Test**: Can be tested by feeding timing evidence with cross-profile samples, missing repetitions, noisy distributions, incomplete artifacts, and non-beneficial results, then verifying each case is rejected with a reviewer-visible reason.

**Acceptance Scenarios**:

1. **Given** timing samples from more than one host profile, **When** the evidence is evaluated, **Then** the result is rejected as cross-profile and cannot support a positive decision.
2. **Given** timing samples inside the declared noise band, **When** the evidence is evaluated, **Then** the result is classified as noisy or inconclusive rather than positive.
3. **Given** damage-scoped timing is slower than or equivalent to full redraw, **When** the evidence is evaluated, **Then** the result is classified as non-beneficial and no performance claim is accepted.

---

### User Story 3 - Publish a Reviewable Timing Evidence Package (Priority: P2)

Release reviewers and package consumers need one timing summary that explains what was measured, what passed, what failed, what was inconclusive, and why the final performance claim is or is not accepted.

**Why this priority**: Timing runs should not require reviewers to reconstruct decisions from scattered logs. The summary must connect every claim to the relevant scenarios, distributions, host profile, and limitations.

**Independent Test**: Can be tested by opening the timing evidence summary and verifying that a reviewer can locate scenario verdicts, distributions, host identity, noise policy, artifacts, and final claim status from one entry point.

**Acceptance Scenarios**:

1. **Given** timing evidence has been collected, **When** the summary is published, **Then** it lists each scenario, both measured paths, distribution metrics, noise policy, confidence decision, artifact paths, and rejection reasons where applicable.
2. **Given** every measured scenario is positive outside the declared noise band, **When** the summary is published, **Then** the timing result is marked positive for the measured profile but does not become a shipped P7 performance claim unless all later report-defined performance gates are present.
3. **Given** one or more scenarios are missing, noisy, incomplete, cross-profile, environment-limited, limited, or non-beneficial, **When** the summary is published, **Then** the overall result states `performance-not-accepted`.

---

### User Story 4 - Preserve Safe Correctness and Unsupported-Host Behavior (Priority: P2)

Maintainers need timing work to preserve the accepted correctness boundary and unsupported-host fail-closed behavior, so performance investigation cannot weaken P7 safety claims.

**Why this priority**: Feature 156 is about measuring performance, not redefining correctness acceptance, fallback behavior, or unsupported-host policy.

**Independent Test**: Can be tested by running accepted-host and unsupported-host readiness checks before and after timing evidence collection and confirming their correctness and fallback classifications remain unchanged.

**Acceptance Scenarios**:

1. **Given** Feature 155 correctness evidence is accepted for a stable host profile, **When** timing evidence is collected, **Then** correctness acceptance remains tied to the same proof and parity requirements.
2. **Given** an unsupported or unavailable presentation environment, **When** timing evidence is requested, **Then** it fails closed as environment-limited and records zero accepted performance artifacts.
3. **Given** timing evidence is positive but later performance gates are absent, **When** readiness is evaluated, **Then** partial-redraw correctness remains accepted for the profile while the shipped performance claim remains unaccepted.

### Edge Cases

- Timing samples are collected from different host profiles, display servers, renderer identities, refresh rates, or package versions.
- Warmup completes for one measured path but not the other.
- A scenario records fewer than five comparable measured repetitions for either path.
- Timing distributions overlap inside the declared noise band.
- Damage-scoped redraw is slower than full redraw for one or more representative scenarios.
- Artifact paths are missing, stale, duplicated from a previous run, unreadable, or disconnected from the run identity.
- Timing includes proof readback or validation overhead that may dominate the measurement.
- The accepted correctness profile is unavailable during timing collection.
- Unsupported-host timing runs occur in the same checkout as accepted-host timing artifacts.
- Existing proof, parity, package, or public-surface validation exposes undocumented drift while timing evidence is being added.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST produce same-profile timing evidence comparing full-redraw and damage-scoped redraw behavior for the accepted P7 host profile.
- **FR-002**: The feature MUST use the existing P7 proof, parity, fallback, and performance decision vocabulary and MUST NOT redefine correctness acceptance.
- **FR-003**: The feature MUST be classified as Tier 1 because it can add package-facing performance evidence, diagnostics, readiness summaries, and consumer-visible claims.
- **FR-004**: Timing evidence MUST include at least five representative compositor scenarios before any positive timing decision is accepted.
- **FR-005**: Each representative scenario MUST include at least five comparable measured repetitions for the full-redraw path and at least five comparable measured repetitions for the damage-scoped path after documented warmup.
- **FR-006**: Timing evidence MUST record warmup count, measured sample count, p50, p95, p99, declared noise band, confidence decision, scenario verdict, overall verdict, run identity, host profile, and artifact locations.
- **FR-007**: The noise policy and positive-result threshold MUST be declared before evidence is evaluated.
- **FR-008**: Evidence from different host profiles, display environments, renderer identities, package versions, scenario definitions, or run identities MUST NOT be mixed into a positive timing decision.
- **FR-009**: Missing, stale, unreadable, duplicated, incomplete, noisy, cross-profile, environment-limited, or non-beneficial evidence MUST fail closed with reviewer-visible reasons.
- **FR-010**: The timing summary MUST classify each scenario as positive, noisy, non-beneficial, incomplete, rejected, limited, or environment-limited.
- **FR-011**: The overall timing result MUST state `performance-not-accepted` unless all required scenario evidence is comparable, complete, same-profile, and positive outside the declared noise band.
- **FR-012**: A positive Feature 156 timing result MUST NOT by itself become a shipped P7 performance claim while the report's later performance gates remain incomplete.
- **FR-013**: The timing evidence MUST disclose whether proof readback or validation overhead is included and MUST mark the result as limited when that overhead cannot be separated from the measured path.
- **FR-014**: Unsupported or unavailable presentation environments MUST produce an environment-limited timing result with zero accepted performance artifacts.
- **FR-015**: The final evidence package MUST link timing results to the accepted correctness profile, proof/parity readiness status, artifact paths, compatibility notes, and remaining limitations.
- **FR-016**: Existing P0-P8 acceptance evidence, Feature 155 correctness readiness, unsupported-host fail-closed behavior, package checks, and public-surface drift checks MUST remain valid unless an intentional compatibility change is documented.

### Key Entities *(include if feature involves data)*

- **Host Profile**: The stable presentation environment identity used to decide whether timing evidence is comparable with accepted P7 correctness evidence.
- **Timing Run**: One evidence collection session with a run identity, host profile, scenario set, warmup policy, artifact locations, and overall verdict.
- **Timing Scenario**: A representative compositor workload measured through both full-redraw and damage-scoped paths.
- **Measured Path**: One of the comparable paths being timed for a scenario: full redraw or damage-scoped redraw.
- **Warmup Record**: The frames or repetitions excluded from measurement so startup effects do not become timing evidence.
- **Sample Distribution**: The measured repetitions for one path in one scenario, including p50, p95, p99, and sample count.
- **Noise Policy**: The declared threshold and decision rules used to classify measured differences as positive, noisy, or non-beneficial.
- **Scenario Verdict**: The positive, rejected, noisy, incomplete, non-beneficial, limited, or environment-limited result for one timing scenario.
- **Timing Evidence Summary**: The review entry point that aggregates scenario verdicts, distributions, host profile, artifact paths, compatibility impact, limitations, and overall performance status.

### Scope and Classification

- In scope: same-profile timing evidence, representative scenario coverage, warmup and sample accounting, distribution reporting, noise classification, rejection reasons, unsupported-host timing behavior, readiness summary updates, compatibility notes, and package-facing validation.
- Out of scope: implementing the later no-clear damage-scissored render path, separating proof readback from timing paths, adding layer promotion or content/transform key splitting, creating a host performance lane ledger beyond the profile facts needed for same-profile rejection, changing P8 layout acceptance, changing text shaping, changing overlay behavior, or claiming universal compositor performance.
- Expected classification: Tier 1, because the feature can add consumer-visible timing evidence, readiness summaries, diagnostics, and performance-claim status.
- Public surface changes are allowed only when needed to expose timing evidence, scenario verdicts, rejection reasons, or readiness status through existing package boundaries.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Timing evidence includes at least 5 representative compositor scenarios measured on the accepted P7 host profile.
- **SC-002**: 100% of representative scenarios include both full-redraw and damage-scoped measurements from the same host profile and run identity.
- **SC-003**: 100% of representative scenarios include at least 5 comparable measured repetitions for each measured path after documented warmup.
- **SC-004**: 100% of scenario summaries include p50, p95, p99, measured sample count, warmup count, declared noise band, confidence decision, and artifact locations.
- **SC-005**: 100% of missing, stale, unreadable, duplicated, incomplete, noisy, cross-profile, environment-limited, limited, and non-beneficial evidence cases fail closed with reviewer-visible reasons.
- **SC-006**: The overall timing result states `performance-not-accepted` unless every required scenario is complete, same-profile, comparable, and positive outside the declared noise band.
- **SC-007**: A positive timing result remains scoped to the measured host profile and does not become a shipped P7 performance claim unless later report-defined performance gates are also satisfied.
- **SC-008**: Unsupported-host timing validation completes in under 2 minutes and records zero accepted performance artifacts.
- **SC-009**: A reviewer can determine measured profile, scenario verdicts, distribution metrics, noise policy, artifact paths, limitations, and final performance status from one summary in under 5 minutes.
- **SC-010**: Focused timing, proof-readiness, unsupported-host, package, and public-surface validation pass with zero undocumented consumer-visible drift before the feature is marked ready for implementation closeout.

## Assumptions

- "Next item" refers to the report section "Planned P7 performance follow-up features," specifically Feature 156: same-profile timing evidence.
- Feature 155 correctness acceptance for the current stable host profile is the baseline; this feature measures performance evidence but does not reopen correctness acceptance.
- The timing evidence flow can reuse the accepted host profile identity from existing P7 readiness artifacts.
- A feature-owned noise policy will be documented before measurement results are interpreted; the specification requires the policy and threshold to exist but leaves the exact numeric threshold to planning.
- Feature 157, Feature 158, Feature 159, Feature 160, and Feature 161 remain separate follow-up work. Feature 156 may produce positive timing evidence, but it cannot by itself establish the final shipped compositor performance claim. Feature 160 improves validation throughput and is not itself a performance-acceptance gate.
- Exact command names, artifact filenames, scenario labels, and validation task order are planning details for the next phase.
