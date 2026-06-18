# Feature Specification: Layer Promotion and Content/Transform Key Split

**Feature Branch**: `159-layer-promotion-keys`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers Feature 159 from the radical rendering architecture report: layer promotion and content/transform key splitting. Feature 155 accepted current-host partial-redraw correctness, Feature 157 accepted the no-clear damage-scissored readiness slice, and Feature 158 separated proof readback from timing. This feature makes the compositor reuse recorded content when only placement changes, promotes stable expensive subtrees only when reuse is beneficial, demotes churning boundaries, and publishes reviewable reuse evidence without broadening the final performance claim.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Reuse Recorded Content for Placement-Only Movement (Priority: P1)

Release reviewers need moving or scrolling content to reuse previously recorded content when the visible content is unchanged, so placement changes do not force unnecessary content work.

**Why this priority**: The report names content/transform key splitting as the highest-leverage remaining compositor efficiency step. Without it, movement-heavy scenarios keep paying content cost even after correctness and timing measurement paths exist.

**Independent Test**: Can be tested by running representative moving and scrolling scenarios and verifying that accepted attempts reuse unchanged content while recording placement changes separately.

**Acceptance Scenarios**:

1. **Given** a subtree whose content remains unchanged while its placement changes, **When** the compositor evaluates the frame, **Then** recorded content is reused and the placement change is recorded separately.
2. **Given** a subtree whose content and placement both change, **When** the compositor evaluates the frame, **Then** the previous recorded content is not reused as if it were unchanged.
3. **Given** placement-only reuse occurs, **When** readiness evidence is assembled, **Then** the summary identifies the scenario, content identity, placement identity, reuse decision, counters, and artifact locations.

---

### User Story 2 - Promote Only Stable Expensive Subtrees (Priority: P1)

Maintainers need promotion decisions to favor stable, expensive subtrees and avoid cheap or unstable content, so promotion overhead does not erase the expected compositor benefit.

**Why this priority**: Over-promotion is a documented R6 risk. Promotion must earn its cost through evidence, not just by creating more retained layers.

**Independent Test**: Can be tested by feeding stable expensive, stable cheap, and unstable expensive scenarios through the promotion decision path and verifying the recorded decisions and counters.

**Acceptance Scenarios**:

1. **Given** a subtree is stable across the required observation window and exceeds the feature's declared cost threshold, **When** promotion is evaluated, **Then** it becomes eligible for retained-layer reuse.
2. **Given** a subtree is stable but below the declared cost threshold, **When** promotion is evaluated, **Then** it is not promoted unless the evidence records a net-positive reason.
3. **Given** a promoted subtree stops being stable, **When** subsequent frames are evaluated, **Then** the subtree is demoted or bypassed with a reviewer-visible reason.

---

### User Story 3 - Fail Closed for Churn, Stale Identity, and Unsafe Reuse (Priority: P1)

Maintainers need stale, cross-profile, churning, incomplete, or ambiguous reuse evidence to fall back safely, so retained-layer reuse cannot corrupt frames or become an unsupported performance claim.

**Why this priority**: Feature 159 changes optimization decisions inside an accepted partial-redraw path. Correctness and safe fallback must remain stronger than reuse or speedup goals.

**Independent Test**: Can be tested by exercising content churn, missing identity metadata, cross-profile evidence, stale retained content, resource failures, and parity mismatches, then verifying that each case rejects promotion or reuse with a clear reason.

**Acceptance Scenarios**:

1. **Given** retained content identity is missing, stale, cross-profile, or disconnected from the current frame, **When** reuse is considered, **Then** reuse is rejected and safe rendering continues.
2. **Given** a promoted boundary churns repeatedly, **When** promotion evidence is evaluated, **Then** the boundary is demoted or marked non-beneficial.
3. **Given** promoted output differs from the equivalent safe output, **When** parity is evaluated, **Then** the attempt is rejected and future reuse for that evidence set fails closed.

---

### User Story 4 - Publish Net-Positive Reuse and Promotion Evidence (Priority: P2)

Release reviewers and package consumers need one readiness package that explains promotion decisions, reuse counters, demotions, parity, host scope, and final performance claim status.

**Why this priority**: The report's performance acceptance rule requires Feature 159 to record net-positive reuse and promotion counters before a compositor performance claim can advance.

**Independent Test**: Can be tested by opening the readiness summary and verifying that a reviewer can determine which scenarios promoted, which reused content, which demoted, whether counters were net-positive, and why the final claim status was chosen.

**Acceptance Scenarios**:

1. **Given** representative Feature 159 scenarios have run, **When** readiness is assembled, **Then** the summary lists promotion decisions, reuse decisions, demotion reasons, counter totals, parity status, host profile, run identity, artifact locations, and final claim status.
2. **Given** counters are missing, noisy, cross-profile, incomplete, or non-beneficial, **When** readiness is assembled, **Then** no positive Feature 159 reuse result is accepted.
3. **Given** Feature 159 records net-positive reuse and promotion counters, **When** later performance gates remain incomplete or noisy, **Then** the shipped compositor performance claim remains `performance-not-accepted`.

### Edge Cases

- Content remains unchanged but placement or scroll offset changes every frame.
- Content changes without a placement change.
- Content and placement both change in the same frame.
- A boundary appears stable during warmup but churns during measured attempts.
- A subtree is expensive once but cheap across the representative run.
- A subtree is stable and expensive but too large or resource-heavy to retain safely.
- Promotion or reuse evidence comes from a different host profile, renderer identity, package version, scenario definition, run identity, or artifact set.
- Retained content is evicted, unavailable, stale, or disconnected from the current frame.
- A promoted output matches inside the damage region but drifts outside it.
- Unsupported-host validation runs in the same checkout as accepted-host reuse artifacts.
- Existing proof, timing, package, compatibility, or public-surface validation exposes undocumented drift while promotion logic is being added.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST distinguish content identity from placement or transform identity for retained compositor candidates.
- **FR-002**: Placement-only movement MUST be eligible to reuse previously recorded content when content identity, host profile, run identity, scenario identity, retained content state, and parity evidence are valid.
- **FR-003**: Content changes MUST invalidate content reuse even when placement identity is unchanged.
- **FR-004**: The feature MUST evaluate promotion candidates using declared stability and cost criteria before accepting retained-layer promotion.
- **FR-005**: Stable expensive subtrees that meet the declared criteria MUST be eligible for promotion and reuse evidence.
- **FR-006**: Cheap, unstable, stale, missing-metadata, cross-profile, resource-limited, or parity-failing candidates MUST NOT be accepted as promoted reuse evidence.
- **FR-007**: Churning promoted boundaries MUST demote, bypass promotion, or record a non-beneficial decision with a reviewer-visible reason.
- **FR-008**: Accepted reuse evidence MUST cover representative scenarios including static retained content, placement-only movement, scrolling or shifted content, nested retained content, content churn, and fallback.
- **FR-009**: Accepted Feature 159 evidence MUST include at least three fresh same-profile attempts before the feature is marked accepted for the measured host profile.
- **FR-010**: Every accepted attempt MUST link promotion decision, content identity, placement identity, reuse decision, reuse counters, demotion status, parity status, host profile, run identity, scenario identity, and artifact locations.
- **FR-011**: Every rejected, demoted, bypassed, or fallback attempt MUST record a reason that distinguishes instability, low cost, stale identity, cross-profile evidence, missing retained content, resource limitation, unsupported host, parity mismatch, and non-beneficial counters.
- **FR-012**: Promotion and reuse counters MUST distinguish avoided content work, placement-only reuse, content re-recording, demotions, fallback decisions, and promotion overhead.
- **FR-013**: Net-positive Feature 159 reuse MUST be accepted only when the required same-profile scenarios show beneficial reuse or promotion counters without rejected safety gates.
- **FR-014**: Unsupported hosts, unavailable presentation environments, cross-profile evidence, stale retained content, and missing parity evidence MUST produce zero accepted Feature 159 reuse artifacts.
- **FR-015**: The final readiness summary MUST state whether layer promotion and content/transform key splitting are accepted, non-beneficial, fallback-only, rejected, or environment-limited for the measured host profile.
- **FR-016**: The shipped compositor performance claim MUST remain `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 records net-positive reuse and promotion counters, and the report-defined host-lane scoping gate is satisfied.
- **FR-017**: The feature MUST preserve existing P7 correctness acceptance, safe full-redraw fallback, unsupported-host fail-closed behavior, proof/readback separation, package validation, and public-surface drift checks.
- **FR-018**: The feature MUST be classified as Tier 1 because it changes observable compositor optimization behavior and may add package-facing diagnostics, readiness status, or compatibility notes.
- **FR-019**: Public compatibility changes are allowed only when needed to expose promotion decisions, reuse status, counter evidence, demotion reasons, readiness status, artifact inspection, or package-facing validation; any such change MUST be documented and validated.

### Key Entities *(include if feature involves data)*

- **Promotion Candidate**: A subtree being considered for retained-layer promotion based on stability, cost, host scope, and safety evidence.
- **Retained Layer**: A reusable recorded representation of promoted content that may be repositioned when content identity is unchanged.
- **Content Identity**: The fingerprint or equivalent evidence that determines whether recorded visual content is unchanged.
- **Placement Identity**: The placement, scroll, or transform evidence that determines how unchanged content is positioned for the current frame.
- **Promotion Decision**: The accepted, rejected, bypassed, demoted, non-beneficial, fallback-only, or environment-limited classification for a candidate.
- **Reuse Decision**: The per-attempt decision to reuse recorded content, re-record content, fall back, or reject evidence.
- **Demotion Reason**: The reviewer-visible explanation for why a previously promoted candidate is no longer retained.
- **Reuse Counters**: The evidence totals for avoided content work, placement-only reuse, content re-recording, demotions, fallback decisions, and promotion overhead.
- **Parity Result**: The comparison between promoted/reused output and the equivalent safe output for the same scenario.
- **Host Profile**: The stable presentation environment identity used to decide whether proof, timing, reuse, and readiness evidence are comparable.
- **Readiness Summary**: The review entry point that aggregates scenarios, promotion decisions, counters, demotions, parity, host scope, compatibility impact, artifacts, and final claim status.
- **Performance Claim Status**: The reviewer-visible status that remains unaccepted until timing, reuse, and host-lane gates are all satisfied.

### Scope and Classification

- In scope: layer-promotion eligibility, content versus placement identity separation, placement-only reuse, content-change invalidation, demotion for churn, reuse and promotion counters, representative scenarios, parity evidence, unsupported-host behavior, readiness summaries, compatibility notes, and package-facing validation.
- Out of scope: performance validation throughput improvements, the full host performance lane ledger, changing proof readback separation, changing the accepted correctness proof requirement, broadening host support, changing P8 layout acceptance, changing text shaping, changing overlay behavior, or claiming universal compositor performance.
- Expected classification: Tier 1, because the feature changes observable compositor optimization behavior and can add consumer-visible diagnostics, readiness summaries, counter evidence, and compatibility notes.
- Public surface changes are allowed only when needed for promotion decisions, reuse status, counter evidence, demotion reasons, readiness status, artifact inspection, or compatibility evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 5 representative compositor scenarios cover static retained content, placement-only movement, scrolling or shifted content, nested retained content, content churn, and fallback.
- **SC-002**: At least 3 fresh same-profile attempts record accepted Feature 159 evidence before the feature is marked accepted for the measured host profile.
- **SC-003**: 100% of accepted placement-only movement scenarios reuse recorded content without accepting stale or cross-profile content identity.
- **SC-004**: 100% of accepted content-change scenarios re-record or invalidate content rather than reusing obsolete content.
- **SC-005**: 100% of churning, stale, missing-metadata, cross-profile, unsupported-host, resource-limited, and parity-failing cases demote, reject, bypass, or fall back with reviewer-visible reasons.
- **SC-006**: 100% of accepted attempts include promotion decision, content identity, placement identity, reuse decision, reuse counters, demotion status, parity status, host profile, run identity, scenario identity, and artifact locations.
- **SC-007**: 100% of accepted promoted/reused outputs match the equivalent safe output for every validated scenario, with zero unexplained pixel drift.
- **SC-008**: Accepted Feature 159 readiness records net-positive reuse and promotion counters for the measured host profile, or explicitly records `non-beneficial` / `performance-not-accepted`.
- **SC-009**: Unsupported-host validation completes in under 2 minutes and records zero accepted Feature 159 reuse or promotion artifacts.
- **SC-010**: A reviewer can determine promoted scenarios, reused scenarios, demoted scenarios, counter totals, parity status, host scope, compatibility impact, artifact paths, and final claim status from one summary in under 5 minutes.
- **SC-011**: Existing Feature 155 correctness readiness, Feature 157 damage-scissored readiness, Feature 158 readback-free measurement separation, full-redraw fallback, and unsupported-host fail-closed behavior remain valid.
- **SC-012**: Focused promotion, reuse, fallback, unsupported-host, package, and public-compatibility validation pass with zero undocumented consumer-visible drift before the feature is marked ready for implementation closeout.
- **SC-013**: The shipped compositor performance claim remains `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 records net-positive counters, and the report-defined host-lane scoping gate is satisfied.

## Assumptions

- "Next item" refers to the first unchecked item in the report's feature-level tracker and planned P7 performance follow-up list: Feature 159, layer promotion and content/transform key split.
- Feature 155 and Feature 157 correctness evidence remain the safety baseline for deciding whether partial-redraw and no-clear damage-scissored behavior can be trusted.
- Feature 158 remains the measurement-policy baseline; proof/readback probe evidence must not be counted as accepted timing evidence.
- Feature 156 timing evidence is still noisy in the current report, so Feature 159 can accept reuse and promotion evidence without accepting the final shipped compositor performance claim.
- "Stable expensive subtree" means a candidate whose content identity remains unchanged across a declared observation window and whose recorded cost is high enough to justify promotion; exact thresholds are planning decisions.
- "Placement-only movement" includes movement, scrolling, or transform changes where content identity is unchanged and host/profile/run identity remains comparable.
- Full redraw remains the safe fallback and remains available for every frame.
- The readiness package can reuse existing P7 vocabulary for host profiles, accepted attempts, parity, environment-limited results, fallback status, artifact identity, and performance claim status.
- Feature 160 performance validation throughput and Feature 161 host performance lane ledger remain separate follow-up work. Feature 159 may satisfy the reuse/promotion counter gate, but it does not by itself accept a final compositor performance claim.
- Exact command names, artifact filenames, threshold values, scenario labels, and validation task order are planning details for the next phase.
