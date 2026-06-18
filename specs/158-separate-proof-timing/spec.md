# Feature Specification: Separate Proof Readback From Timing

**Feature Branch**: `158-separate-proof-timing`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers Feature 158 from the radical rendering architecture report: separating proof readback from timing. Feature 155 accepted current-host partial-redraw correctness, Feature 156 recorded same-profile timing as noisy, and Feature 157 added the no-clear damage-scissored readiness slice. This feature keeps screenshot/readback validation in the proof path, but removes forced validation readback from performance timing samples unless a run is explicitly marked as a probe. The goal is to ensure performance samples measure render and presentation behavior rather than the cost of proving correctness.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Measure Performance Without Forced Proof Readback (Priority: P1)

Release reviewers need performance timing samples that exclude validation readback from the measured interval, so noisy or slow timing results can be evaluated as render and presentation behavior rather than proof overhead.

**Why this priority**: The report requires Feature 158 before any compositor performance claim can be considered. If timing still includes forced proof readback, the sample cannot answer whether damage-scoped rendering is faster.

**Independent Test**: Can be tested by running the representative timing lane and verifying that every accepted timing sample declares a readback-free measurement policy and excludes proof readback from the measured sample set.

**Acceptance Scenarios**:

1. **Given** a representative timing scenario on the accepted host profile, **When** timing samples are collected, **Then** the accepted performance sample set excludes validation readback from the measured interval.
2. **Given** a timing run accidentally includes validation readback in the measured interval, **When** readiness is assembled, **Then** that sample is excluded from performance acceptance with a reviewer-visible reason.
3. **Given** timing samples are readback-free but still noisy, **When** readiness is assembled, **Then** the feature records the separated measurement result without claiming a shipped compositor speedup.

---

### User Story 2 - Keep Proof Readback Available as an Explicit Probe (Priority: P1)

Maintainers need screenshot/readback validation to remain available for correctness proof and explicit probe runs, so removing readback from timing does not weaken the accepted proof package.

**Why this priority**: Correctness remains the safety gate. The feature must separate measurement cost from proof evidence without deleting the proof mechanism.

**Independent Test**: Can be tested by running an explicit probe path and confirming that readback artifacts are produced, labelled as proof or probe evidence, and excluded from timing acceptance.

**Acceptance Scenarios**:

1. **Given** an explicit proof or probe run is requested, **When** the run completes on a capable host, **Then** it records screenshot/readback evidence separately from performance timing samples.
2. **Given** proof or probe evidence exists for the same host profile, **When** timing readiness is assembled, **Then** the summary links to the proof evidence but does not count proof readback samples as performance timing samples.
3. **Given** a host cannot provide proof readback, **When** readiness is assembled, **Then** the host remains fail-closed for accepted proof and records zero accepted performance artifacts.

---

### User Story 3 - Publish Comparable Readiness Evidence (Priority: P2)

Release reviewers and package consumers need one evidence package that clearly distinguishes readback-free timing, explicit probe evidence, host profile, scenario coverage, exclusions, and final claim status.

**Why this priority**: Feature 156 and Feature 157 evidence remains valuable only if reviewers can see whether the new timing lane is comparable and whether any sample was excluded for readback contamination.

**Independent Test**: Can be tested by opening the readiness summary and verifying that a reviewer can determine the measurement policy, included samples, excluded samples, proof/probe links, host profile, and final performance claim status from one entry point.

**Acceptance Scenarios**:

1. **Given** timing and probe runs have been collected, **When** readiness is assembled, **Then** the summary lists included timing samples, excluded readback/probe samples, scenario coverage, host profile, artifact locations, and final claim status.
2. **Given** old and new timing evidence exist for the same profile, **When** readiness is assembled, **Then** the summary states whether the readback-free lane supersedes, confirms, or only contextualizes the previous noisy timing result.
3. **Given** the timing lane is missing, mixed, cross-profile, or environment-limited, **When** readiness is assembled, **Then** no performance claim is accepted and the reason is visible.

---

### User Story 4 - Preserve Existing Safety Boundaries (Priority: P2)

Maintainers need Feature 158 to preserve current P7 correctness acceptance, unsupported-host fail-closed behavior, package validation, and public compatibility boundaries while changing only the measurement contract.

**Why this priority**: Measurement separation is a performance-evidence improvement, not permission to relax proof, fallback, or host-scope rules.

**Independent Test**: Can be tested by running focused proof, timing, unsupported-host, package, and compatibility checks before closeout and verifying that any consumer-visible drift is intentional and documented.

**Acceptance Scenarios**:

1. **Given** Feature 155 and Feature 157 correctness readiness is accepted for the measured profile, **When** Feature 158 timing separation is added, **Then** correctness acceptance remains scoped to the same host/profile rules.
2. **Given** unsupported or unavailable presentation environments are exercised, **When** readiness is assembled, **Then** they are marked environment-limited or fallback-only with zero accepted proof or performance artifacts.
3. **Given** public compatibility changes are needed for measurement policy or readiness status, **When** validation completes, **Then** they are classified, documented, and validated through the package boundary.

### Edge Cases

- A timing sample includes validation readback during the measured interval.
- A proof or probe run is accidentally mixed into the performance acceptance sample set.
- A timing run claims readback-free measurement but does not record enough metadata to verify that claim.
- Timing and proof artifacts come from different host profiles, display environments, renderer identities, package versions, run identities, or scenario definitions.
- The proof path succeeds but readback-free timing remains noisy.
- The readback-free timing lane appears faster but later required performance gates are still incomplete.
- Unsupported-host validation runs in the same checkout as accepted-host timing artifacts.
- Previous Feature 156 timing evidence cannot be compared because scenario labels, host profile, or sample policy changed without disclosure.
- Explicit probe readback fails after timing samples have already been collected.
- Existing proof, package, compatibility, or public-surface validation exposes undocumented drift while measurement separation is being added.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST separate correctness proof readback from performance timing samples.
- **FR-002**: Accepted timing samples MUST define a measured interval that excludes validation readback; if readback occurs outside the interval, the timing artifact MUST make that boundary verifiable.
- **FR-003**: Screenshot/readback validation MUST remain available for proof and explicit probe runs.
- **FR-004**: Proof or probe runs that include readback MUST be classified as non-performance evidence and MUST NOT contribute samples, distribution statistics, or scenario completion to the accepted performance timing set.
- **FR-005**: Every timing artifact MUST identify its measurement policy, including whether validation readback was absent, outside the measured interval, or intentionally included for a probe.
- **FR-006**: Timing samples with missing, ambiguous, contradictory, or unverifiable readback policy metadata MUST be excluded from performance acceptance.
- **FR-007**: Timing, proof, and probe evidence from different host profiles, display environments, renderer identities, package versions, run identities, or scenario definitions MUST NOT be combined into one accepted timing result.
- **FR-008**: The timing lane MUST cover the same representative scenario categories used by the current P7 performance evidence unless a difference is explicitly documented.
- **FR-009**: The readiness package MUST distinguish included timing samples, excluded probe samples, excluded contaminated samples, unsupported-host results, and fallback-only results.
- **FR-010**: The readiness package MUST state whether readback-free timing supersedes, confirms, or only contextualizes the previous noisy timing evidence.
- **FR-011**: Unsupported hosts, unavailable presentation environments, failed proof readback, and cross-profile evidence MUST produce zero accepted performance artifacts.
- **FR-012**: The final readiness summary MUST state whether measurement separation is accepted, rejected, fallback-only, or environment-limited for the measured host profile.
- **FR-013**: The shipped compositor performance claim MUST remain `performance-not-accepted` unless all report-defined later performance gates are also satisfied.
- **FR-014**: The feature MUST preserve existing P7 correctness acceptance, safe full-redraw fallback, unsupported-host fail-closed behavior, package validation, and public-surface drift checks.
- **FR-015**: The feature MUST be classified as Tier 1 because it changes consumer-visible performance readiness semantics and may add package-facing diagnostics or compatibility notes.
- **FR-016**: Public compatibility changes are allowed only when needed to expose measurement policy, readiness status, exclusion reasons, probe evidence, or package-facing validation; any such change MUST be documented and validated.

### Key Entities *(include if feature involves data)*

- **Proof Readback**: Screenshot or frame readback evidence used to validate correctness.
- **Explicit Probe Run**: A deliberately requested validation run that may include readback and is not part of performance acceptance.
- **Timing Sample**: A measured render or presentation sample intended for performance evaluation.
- **Measurement Policy**: The declared rule for whether readback is absent, outside measurement, or intentionally included for a probe.
- **Host Profile**: The stable presentation environment identity used to decide whether timing, proof, and readiness evidence are comparable.
- **Scenario Definition**: The representative frame-update case measured by the timing lane and, when needed, validated by probe evidence.
- **Excluded Sample**: A sample removed from performance acceptance because it is a probe, contains readback contamination, lacks metadata, is cross-profile, or is environment-limited.
- **Readiness Summary**: The review entry point that aggregates timing inclusion, exclusions, proof/probe links, host scope, compatibility impact, and final claim status.
- **Performance Claim Status**: The reviewer-visible outcome that remains unaccepted until measurement separation and later report-defined gates are all satisfied.

### Scope and Classification

- In scope: separating proof readback from timing, explicit probe classification, measurement-policy metadata, mixed-sample rejection, same-profile comparability, readiness summaries, unsupported-host behavior, compatibility notes, and package-facing validation.
- Out of scope: layer promotion, content and transform key splitting, performance validation throughput improvements, a full host performance lane ledger, changing the correctness proof requirement, broadening accepted host support, changing P8 layout acceptance, changing text shaping, changing overlay behavior, or claiming universal compositor performance.
- Expected classification: Tier 1, because the feature changes consumer-visible readiness semantics and may add package-facing diagnostics, compatibility notes, or validation status.
- Public surface changes are allowed only when needed for measurement policy, exclusion reasons, readiness status, probe evidence, or package-facing validation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of accepted timing samples declare that validation readback is absent from the measured interval.
- **SC-002**: 0 proof or probe samples that include validation readback are counted in the accepted performance timing sample set.
- **SC-003**: 100% of timing artifacts include measurement policy, host profile, scenario identity, inclusion status, exclusion reason when applicable, and artifact location.
- **SC-004**: At least one explicit probe path demonstrates that screenshot/readback proof remains available and is recorded separately from timing acceptance.
- **SC-005**: 100% of contaminated, mixed, missing-policy, cross-profile, unsupported-host, and environment-limited samples are excluded from performance acceptance with reviewer-visible reasons.
- **SC-006**: The readiness summary includes a reviewer checklist that can be completed in under 5 minutes to determine whether the timing lane is readback-free, which samples were included, which were excluded, which proof/probe evidence supports correctness, and why the final claim status was chosen.
- **SC-007**: Unsupported-host validation completes in under 2 minutes and records zero accepted proof or performance artifacts.
- **SC-008**: Existing Feature 155 correctness readiness, Feature 157 damage-scissored readiness, full-redraw fallback, and unsupported-host fail-closed behavior remain valid.
- **SC-009**: Focused proof, timing, unsupported-host, package, and public-compatibility validation pass with zero undocumented consumer-visible drift before the feature is marked ready for implementation closeout.
- **SC-010**: The shipped compositor performance claim remains `performance-not-accepted` unless the report-defined Feature 159 and Feature 161 gates are also satisfied.

## Assumptions

- "Next item" refers to the first unchecked item in the report's "Planned P7 performance follow-up features" tracker: Feature 158, separate proof readback from timing.
- Feature 155 and Feature 157 correctness evidence remain the safety baseline for deciding whether partial-redraw behavior can be trusted.
- Feature 156 timing evidence remains noisy and does not establish a shipped performance claim.
- The proof path must continue to use screenshot/readback evidence where needed; this feature changes timing eligibility, not correctness proof standards.
- Full redraw remains the safe fallback and remains available for every frame.
- The readiness package can reuse existing P7 vocabulary for proof gates, host profiles, environment-limited results, artifact identity, fallback status, and performance claim status.
- Feature 159, Feature 160, and Feature 161 remain separate follow-up work. This feature may accept measurement separation for the measured profile, but it does not by itself accept a final compositor performance claim.
- Exact command names, artifact filenames, scenario labels, and validation task order are planning details for the next phase.
