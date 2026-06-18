# Feature Specification: Compositor Proof Acceptance

**Feature Branch**: `154-compositor-proof-acceptance`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers the next open item from the radical rendering architecture report after Feature 153. P0 through P8 are implemented or accepted, but P7 live partial-redraw acceptance remains environment-limited because no stable capable host has produced three accepted sentinel and damage readback attempts. Feature 153 added the proof interpreter, selected-attempt identity, and host-readiness classification. This feature uses that vocabulary to accept or reject the live compositor proof gate, run the same-profile damage-scoped parity corpus, decide whether any performance claim is justified, and publish one final readiness verdict without accepting evidence from unsupported, stale, synthetic, mismatched, or incomplete runs.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Accept a Three-Run Capable-Host Proof Set (Priority: P1)

Release reviewers need three fresh matching live proof attempts from one stable capable host profile, so partial-redraw readiness is based on repeatable real presentation evidence rather than environment-limited policy.

**Why this priority**: Feature 153 can classify proof attempts, but the report still requires real sentinel and damage readback artifacts before partial redraw can move past fallback-gated status.

**Independent Test**: Can be tested by running three live proof attempts on the same capable host profile and verifying that the proof set accepts only when all selected attempts are fresh, matching, non-synthetic, decodable, non-blank, and individually accepted.

**Acceptance Scenarios**:

1. **Given** three fresh accepted attempts from the same capable host profile and proof method, **When** the proof set is evaluated, **Then** the proof set is accepted and lists the exact selected attempt identities.
2. **Given** fewer than three accepted attempts, stale attempts, mismatched host profiles, mismatched proof methods, or mixed attempt classifications, **When** the proof set is evaluated, **Then** the proof set fails closed with specific reviewer-visible reasons.
3. **Given** an unsupported or unavailable presentation environment, **When** proof evaluation runs, **Then** the result remains environment-limited, records zero accepted partial-redraw artifacts, and preserves the full-redraw fallback.

---

### User Story 2 - Prove Same-Profile Damage-Scoped Parity (Priority: P1)

Maintainers need the accepted host profile to produce the same final visible output for damage-scoped redraw as the full-redraw reference across representative frame transitions.

**Why this priority**: A passing host proof establishes that preservation is possible; it does not by itself prove that real compositor scenarios remain visually correct.

**Independent Test**: Can be tested by running the representative damage-scoped parity corpus on the same accepted host profile and verifying parity or fallback decisions for every scenario.

**Acceptance Scenarios**:

1. **Given** an accepted proof set and a localized update scenario, **When** damage-scoped redraw is compared with full redraw, **Then** the final visible output matches or the scenario is not accepted.
2. **Given** no-change, movement, overlap, edge clipping, resize, full invalidation, invalid damage, unsupported host, or resource-failure scenarios, **When** the corpus is evaluated, **Then** each scenario records an accepted parity verdict or a safe fallback reason.
3. **Given** parity evidence from a different host profile or stale proof set, **When** readiness is evaluated, **Then** that evidence cannot unlock partial redraw for the current profile.

---

### User Story 3 - Decide the Live Performance Claim (Priority: P2)

Release reviewers need a same-profile timing decision that accepts, rejects, or marks inconclusive any compositor performance claim based on comparable live measurements.

**Why this priority**: The report explicitly separates safety proof from performance claims. Correct partial redraw should not be marketed as faster unless the live evidence supports it.

**Independent Test**: Can be tested by measuring representative live scenarios with a declared threshold and noise policy, then confirming the readiness summary accepts a benefit only when the evidence satisfies that policy.

**Acceptance Scenarios**:

1. **Given** accepted proof and parity evidence plus repeated same-profile timing measurements, **When** timing is evaluated against the declared policy, **Then** the performance claim is accepted only if the measured benefit satisfies the policy.
2. **Given** missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial timing evidence, **When** readiness is assembled, **Then** no performance claim is accepted and the reason is visible.
3. **Given** reuse or snapshot evidence without same-profile live timing and parity, **When** reviewers inspect the claim, **Then** that evidence is recorded as context-only and cannot support a performance claim.

---

### User Story 4 - Publish the Final P7 Readiness Verdict (Priority: P3)

Package consumers and maintainers need one reviewable readiness entry point that states whether P7 live partial redraw is accepted, failed, environment-limited, or still fallback-gated.

**Why this priority**: Consumers should not infer readiness from scattered proof, parity, and timing logs. The final status must connect each accepted claim to its evidence and each limitation to its reason.

**Independent Test**: Can be tested by opening the readiness summary and confirming it links the selected proof set, same-profile parity corpus, timing decision, fallback status, unsupported-host regression, compatibility impact, and remaining limitations.

**Acceptance Scenarios**:

1. **Given** accepted proof, accepted same-profile parity, and an accepted or explicitly rejected timing decision, **When** readiness is published, **Then** reviewers can identify the accepted host profile, supported claims, unsupported claims, and evidence paths from one summary.
2. **Given** failed or environment-limited proof, failed parity, or incomplete timing, **When** readiness is published, **Then** the summary keeps partial redraw fallback-gated and names the blocking evidence.
3. **Given** consumer-visible diagnostic, readiness, package, or behavior changes, **When** compatibility validation is reviewed, **Then** all intentional changes are documented and undocumented public drift is rejected.

### Edge Cases

- Only one or two capable-host attempts are accepted.
- Proof attempts pass individually but come from different host profiles or proof methods.
- Sentinel or damage artifacts are missing, stale, blank, undecodable, synthetic-only, or copied from a prior run.
- Damaged pixels update but undamaged pixels are not preserved.
- Undamaged pixels are preserved but the damaged region does not update.
- A parity scenario requires full invalidation, has invalid damage, changes resource availability, or changes presentation profile.
- Unsupported-host validation runs in the same checkout as accepted capable-host evidence.
- Timing evidence is noisy, incomplete, cross-profile, non-beneficial, or lacks a declared threshold and noise policy.
- Existing proof and parity evidence is valid, but package or public-surface validation reveals undocumented consumer-visible drift.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST close the remaining Feature 153 environment limit by attempting a real capable-host live proof-set acceptance for P7 partial redraw.
- **FR-002**: The feature MUST use the existing Feature 153 proof interpreter and accepted-proof-set vocabulary as the acceptance language and MUST NOT redefine unrelated compositor readiness policy.
- **FR-003**: The feature MUST be classified as a Tier 1 contracted change because it can change consumer-visible compositor readiness, fallback status, diagnostics, and performance claims.
- **FR-004**: A proof set MUST require exactly three selected accepted attempts from the same host profile and proof method before the host proof gate is accepted.
- **FR-005**: Each selected accepted attempt MUST include fresh, decodable, non-blank, non-synthetic sentinel and damage evidence tied to the current run identity.
- **FR-006**: Each selected accepted attempt MUST show both damaged-pixel update and undamaged-pixel preservation.
- **FR-007**: Missing, stale, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, failed-pixel, or incomplete evidence MUST fail closed with reviewer-visible reasons.
- **FR-008**: Unsupported or unavailable presentation environments MUST produce an environment-limited result with zero accepted partial-redraw artifacts.
- **FR-009**: The representative damage-scoped parity corpus MUST run against the same host profile as the accepted proof set before partial redraw can be marked accepted for that profile.
- **FR-010**: The parity corpus MUST cover localized update, no-change, movement, overlap, edge clipping, resize, full invalidation, invalid damage, unsupported host, and resource-failure paths.
- **FR-011**: Every accepted parity scenario MUST match the full-redraw reference result; every non-accepted scenario MUST retain full redraw with a visible fallback reason.
- **FR-012**: Partial redraw MUST remain fallback-gated unless proof-set acceptance and same-profile parity acceptance are both present and current.
- **FR-013**: Timing evidence MUST declare its threshold and noise policy before any performance claim is accepted.
- **FR-014**: Timing evidence MUST cover at least five representative live scenarios with at least five comparable repetitions per scenario before any performance benefit is accepted.
- **FR-015**: Missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial timing evidence MUST produce no accepted performance claim.
- **FR-016**: The final readiness summary MUST state proof-set status, parity status, timing status, fallback status, accepted host profile, selected attempt identities, artifact locations, compatibility impact, and remaining limitations.
- **FR-017**: The unsupported-host path MUST remain a regression scenario and MUST NOT be counted as accepted proof, parity, or timing evidence.
- **FR-018**: Public diagnostics, package-facing readiness changes, and behavior changes MUST include compatibility notes and validation evidence before readiness can be accepted.
- **FR-019**: Existing P0-P8 acceptance evidence, Feature 153 interpreter behavior, layout acceptance, render-anywhere behavior, text-shaping behavior, overlay behavior, package checks, and public-surface drift checks MUST remain valid unless a compatibility note documents the intentional change.

### Key Entities *(include if feature involves data)*

- **Proof Attempt**: One live run that records sentinel evidence, damage evidence, host profile, proof method, freshness, artifact quality, and attempt classification.
- **Accepted Proof Set**: The group of exactly three selected accepted proof attempts required to accept the host proof gate.
- **Host Profile**: The recorded presentation environment identity used to decide whether proof, parity, and timing evidence belong to the same capable host.
- **Proof Method**: The recorded evidence-producing method used to compare proof attempts and reject mixed evidence.
- **Sentinel Evidence**: The frame evidence that establishes the known starting presentation state for a proof attempt.
- **Damage Evidence**: The frame evidence that shows whether damage-scoped redraw updates damaged pixels and preserves undamaged pixels.
- **Damage-Scoped Parity Result**: The scenario-level comparison between damage-scoped output and full-redraw reference output on the accepted host profile.
- **Fallback Decision**: The recorded reason full redraw remains active for a scenario, host, proof set, or readiness state.
- **Timing Decision**: The accepted, rejected, or inconclusive decision for any performance claim based on same-profile live measurements.
- **P7 Readiness Summary**: The single review entry point that aggregates proof, parity, timing, fallback, compatibility, limitation, and consumer-facing status.

### Scope and Classification

- In scope: three-run capable-host proof-set acceptance, unsupported-host regression behavior, same-profile damage-scoped parity corpus, live timing decision, final P7 readiness summary, fallback status, compatibility notes, and package-facing validation.
- Out of scope: changing P8 layout acceptance, adding new text-shaping scope, adding new overlay behavior, adding a new portable rendering backend, redesigning the proof vocabulary, enabling performance claims from synthetic or environment-limited evidence, and restoring unrelated tooling wrappers that are not prerequisites for compositor proof acceptance.
- Expected classification: Tier 1, because this feature can alter public readiness status, diagnostics, fallback behavior, and package-facing claims.
- Public surface changes are allowed only when needed to expose proof, parity, timing, fallback, or readiness decisions already justified by the report and Feature 153 vocabulary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Exactly 3 fresh matching capable-host proof attempts are selected and accepted before the host proof gate is marked accepted.
- **SC-002**: 100% of accepted proof attempts include fresh, decodable, non-blank, non-synthetic sentinel and damage evidence.
- **SC-003**: 100% of accepted proof attempts verify both damaged-pixel update and undamaged-pixel preservation.
- **SC-004**: 100% of stale, missing, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, failed-pixel, and incomplete proof cases fail closed with visible reasons.
- **SC-005**: The representative same-profile parity corpus records verdicts for at least 10 required paths: localized update, no-change, movement, overlap, edge clipping, resize, full invalidation, invalid damage, unsupported host, and resource failure.
- **SC-006**: 100% of accepted parity scenarios match the full-redraw reference result.
- **SC-007**: Unsupported-host validation completes in under 2 minutes and records zero accepted partial-redraw artifacts.
- **SC-008**: Timing evidence covers a declared threshold/noise policy, at least 5 representative live scenarios, and at least 5 comparable repetitions per scenario before any performance benefit is accepted.
- **SC-009**: If timing evidence is missing, noisy, incomplete, cross-profile, environment-limited, or non-beneficial, the readiness summary records no accepted performance claim.
- **SC-010**: A reviewer can determine proof-set status, selected attempts, artifact locations, host profile, parity status, timing status, fallback status, and remaining limitations from one readiness summary in under 5 minutes.
- **SC-011**: Package and public contract validation pass with zero undocumented compositor readiness, diagnostic, or fallback-behavior changes.
- **SC-012**: Focused regression validation records accepted or explicitly limited verdicts for Feature 153 interpreter behavior and adjacent rendering readiness surfaces before final readiness is accepted.

## Assumptions

- "Next item" refers to the report section "Remaining steps to remove Feature 153 environment limits."
- Feature 153's interpreter, selected-attempt identity, host-readiness classification, and proof-set vocabulary are the starting baseline.
- A stable capable presentation host may be available for acceptance, but unsupported environments must continue to provide safe environment-limited regression evidence.
- Accepted host proof is necessary but not sufficient for partial-redraw readiness; same-profile parity must also pass.
- Performance claims require same-profile live timing evidence and remain unaccepted when evidence is absent, noisy, incomplete, environment-limited, or non-beneficial.
- Exact command names, artifact filenames, host-profile fields, and validation task order are planning details for the next phase.
