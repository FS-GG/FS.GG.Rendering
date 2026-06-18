# Feature Specification: Compositor Live Proof Acceptance

**Feature Branch**: `152-compositor-live-proof`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers the next open item from the referenced radical rendering architecture report. P8 is already accepted through Feature 151, while P7 remains environment-limited for live partial-redraw acceptance. Feature 149 completed the deterministic compositor readiness package and kept partial redraw fallback-gated; this feature closes the remaining capable-host proof, live parity, timing, and readiness decision gap without reopening completed P7 or P8 scope.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Accept the Live Safety Gate (Priority: P1)

Release reviewers need a fresh live proof that shows whether the current presentation host preserves valid undamaged pixels across damage-scoped frames, so partial redraw is accepted only from real capable-host evidence.

**Why this priority**: The report states that P7 cannot claim live partial redraw while the proof remains environment-limited. This is the acceptance gate for every partial-redraw and performance claim.

**Independent Test**: Can be tested by running the live proof on a capable host and verifying that three fresh matching runs produce accepted artifacts, while unsupported, stale, missing, blank, mismatched, or synthetic evidence fails closed.

**Acceptance Scenarios**:

1. **Given** a capable host with a fresh proof run, **When** three matching proof runs complete, **Then** the evidence is accepted only if each run shows damaged pixels updated and undamaged pixels preserved.
2. **Given** missing, stale, blank, synthetic-only, host-mismatched, or proof-method-mismatched evidence, **When** readiness is evaluated, **Then** partial redraw remains unaccepted and the failing condition is recorded.
3. **Given** an unsupported or unavailable presentation environment, **When** the proof runs, **Then** the result is environment-limited, no partial-redraw acceptance is recorded, and full redraw remains the safe path.

---

### User Story 2 - Prove Live Damage-Scoped Visual Parity (Priority: P1)

Maintainers need damage-scoped frames on an accepted host to produce the same visible result as the full-redraw reference across representative live frame transitions.

**Why this priority**: Even with a host safety proof, partial redraw is only useful if the final image remains correct for real frame changes.

**Independent Test**: Can be tested by running a representative live compositor corpus with damage-scoped redraw and full redraw, then comparing final visible outcomes and fallback decisions for every accepted scenario.

**Acceptance Scenarios**:

1. **Given** accepted live proof and a localized frame change, **When** damage-scoped redraw runs, **Then** the final frame matches the full-redraw reference.
2. **Given** a no-change frame after accepted proof, **When** the frame is processed, **Then** preserved output remains valid or the system falls back to full redraw with a recorded reason.
3. **Given** a frame-wide invalidation, resize, changed presentation profile, or invalid damage information, **When** rendering is evaluated, **Then** the system performs full redraw or requires fresh proof before accepting scoped redraw.

---

### User Story 3 - Decide the Live Performance Claim (Priority: P2)

Release reviewers need capable-host timing evidence that either supports a compositor performance claim or explicitly rejects it as inconclusive or not beneficial.

**Why this priority**: The report explicitly says no compositor performance claim is accepted while the live proof is environment-limited. A correct compositor still should not be advertised as faster without comparable live evidence.

**Independent Test**: Can be tested by running repeated timing comparisons on representative live scenarios and confirming that the readiness summary either states an accepted benefit with evidence or refuses the performance claim.

**Acceptance Scenarios**:

1. **Given** accepted live proof, a predeclared threshold/noise policy, and repeated measurements for representative scenarios, **When** timing evidence is assembled, **Then** the summary reports whether damage-scoped redraw satisfies that policy over full redraw.
2. **Given** incomplete, noisy, environment-limited, or non-beneficial measurements, **When** readiness is assembled, **Then** no performance claim is accepted and the reason is visible.
3. **Given** snapshot or reuse evidence is used to support a timing claim, **When** reviewers inspect the evidence, **Then** the claim links to parity, lifecycle, and live timing records for the same accepted host profile, or the evidence is recorded as context-only and cannot support the claim.

---

### User Story 4 - Publish the Final P7 Readiness Decision (Priority: P3)

Package consumers and maintainers need one reviewable readiness entry point that states whether P7 live partial redraw is accepted, environment-limited, failed, or still fallback-gated.

**Why this priority**: Consumers should not infer readiness from scattered proof logs. The package behavior and limitations must be understandable without private context.

**Independent Test**: Can be tested by reviewing the readiness package and confirming it links the accepted or rejected proof set, live parity evidence, timing decision, fallback status, compatibility notes, and any remaining limitations.

**Acceptance Scenarios**:

1. **Given** accepted proof, parity, and timing evidence, **When** readiness is published, **Then** reviewers can identify the accepted host profile, supporting evidence paths, enabled claims, and remaining limitations from one summary.
2. **Given** environment-limited or failed live evidence, **When** readiness is published, **Then** the summary states that partial redraw remains fallback-gated and identifies the blocking evidence.
3. **Given** public diagnostics or package-facing behavior changes, **When** compatibility validation runs, **Then** all intentional changes are documented and undocumented public drift is rejected.

### Edge Cases

- A proof run succeeds once but does not produce three fresh matching capable-host runs.
- Proof artifacts are present but stale, blank, synthetic-only, from a different host profile, or from a different proof method.
- A host preserves pixels for one frame sequence but fails on a no-change, localized-damage, resize, or frame-wide invalidation scenario.
- The damage region is empty, outside the frame, larger than the frame, or conflicts with the reported frame state.
- Timing evidence is noisy, incomplete, non-beneficial, or gathered from an environment that cannot support a performance claim.
- Snapshot or reuse evidence passes deterministic checks but lacks live capable-host timing evidence.
- Existing Feature 149 diagnostics remain valid but are insufficient to accept live partial redraw.
- Broader rendering, package, overlay, text, layout, and render-anywhere regressions must be distinguished from P7 live-proof failures.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST close the remaining P7 live compositor acceptance gap identified in the referenced report: live partial-redraw proof, live damage-scoped parity, capable-host timing, and final readiness decision.
- **FR-002**: The feature MUST use Feature 149 readiness as the baseline and MUST NOT reopen completed Feature 149 deterministic evidence, Feature 151 P8 layout acceptance, or unrelated roadmap work except where required to validate live P7 readiness.
- **FR-003**: The feature MUST be classified as a Tier 1 contracted change because it can change consumer-visible compositor readiness, diagnostics, fallback status, and performance claims.
- **FR-004**: The system MUST classify each live proof attempt as accepted, environment-limited, or failed with a specific reviewer-visible reason.
- **FR-005**: Accepted live proof MUST require at least three fresh matching capable-host runs for the same host profile and proof method.
- **FR-006**: Each accepted live proof run MUST include evidence that damaged pixels changed as expected and undamaged pixels remained valid.
- **FR-007**: Missing, stale, blank, synthetic-only, host-mismatched, proof-method-mismatched, or failed evidence MUST NOT accept partial redraw.
- **FR-008**: Unsupported or unavailable presentation environments MUST complete with an environment-limited classification and MUST record zero accepted partial-redraw artifacts.
- **FR-009**: Damage-scoped output on an accepted host MUST be compared against full-redraw output across a representative live compositor corpus.
- **FR-010**: Damage-scoped rendering MUST fall back to full redraw when proof is not accepted, damage information is invalid, the frame requires full invalidation, or parity cannot be established.
- **FR-011**: Every fallback decision MUST record a reviewer-visible reason and must not be counted as accepted partial-redraw evidence.
- **FR-012**: Host profile, proof method, proof freshness, accepted artifacts, parity status, timing status, fallback status, and limitations MUST be visible in the readiness evidence.
- **FR-013**: Capable-host timing evidence MUST compare full redraw and damage-scoped redraw across representative scenarios before any performance claim is accepted.
- **FR-014**: Performance readiness MUST be reported as inconclusive or rejected when the predeclared threshold/noise policy is absent, measurements are incomplete, environment-limited, noisy, or measurements do not satisfy the policy.
- **FR-015**: Snapshot or reuse timing evidence MUST link to live parity, lifecycle evidence, and live timing evidence for the same accepted host profile before it is included in a performance claim; otherwise it MUST be recorded as context-only evidence.
- **FR-016**: Final readiness MUST state whether P7 live partial redraw is accepted, environment-limited, failed, or fallback-gated, and MUST link each claim to supporting evidence.
- **FR-017**: Public diagnostics, compatibility notes, and package validation MUST identify all intentional consumer-visible changes and reject undocumented public contract drift.
- **FR-018**: Existing parity, determinism, render-anywhere, overlay, text-shaping, layout, package-readiness, and Feature 149 diagnostic guarantees MUST remain valid unless a documented compatibility note explains the change.

### Key Entities *(include if feature involves data)*

- **Live Proof Attempt**: One capable-host, unsupported-host, or failed proof run used to evaluate whether partial redraw can be accepted.
- **Accepted Proof Set**: The group of fresh matching live proof attempts required before partial redraw can be marked accepted.
- **Host Profile**: The recorded presentation environment identity used to decide whether proof evidence matches the environment being accepted.
- **Proof Artifact**: The frame capture, readback, or summary evidence that shows damaged-region updates and undamaged-region preservation.
- **Damage-Scoped Parity Result**: The comparison between damage-scoped output and the full-redraw reference for a live scenario.
- **Fallback Decision**: The recorded reason the system used or retained full redraw instead of accepting partial redraw.
- **Timing Evidence**: The repeated capable-host measurements used to accept, reject, or mark inconclusive a compositor performance claim.
- **P7 Readiness Summary**: The single review entry point that aggregates proof status, parity status, timing decision, fallback status, compatibility impact, and limitations.
- **Compatibility Ledger**: The record of consumer-visible diagnostic, readiness, package, or behavior changes caused by this feature.

### Scope and Classification

- In scope: live capable-host proof acceptance, proof rejection rules, environment-limited handling, damage-scoped live parity, timing claim decision, readiness summary, compatibility notes, and package-facing validation.
- Out of scope: new layout semantics, P8 acceptance work, new portable scene protocol semantics, new widget interaction behavior, text-shaping expansion, browser backend production work, and broad compositor redesign not needed for live acceptance.
- Expected classification: Tier 1, because this feature changes acceptance status, diagnostics, and possibly consumer-visible compositor behavior.
- Public surface changes are allowed only when needed for readiness, diagnostics, or compatibility; otherwise the feature should reuse the existing Feature 149 evidence and diagnostic vocabulary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 3 fresh matching capable-host live proof runs are accepted for the same host profile and proof method before P7 partial redraw is marked accepted.
- **SC-002**: 100% of accepted live proof runs include non-missing, non-blank, non-synthetic artifacts showing both damaged-region updates and undamaged-region preservation.
- **SC-003**: The representative live compositor corpus reaches 100% final-frame parity between damage-scoped output and full-redraw reference output for every accepted scenario.
- **SC-004**: 100% of unsupported-host runs complete with an environment-limited classification in under 2 minutes and record zero accepted partial-redraw artifacts.
- **SC-005**: 100% of stale, missing, blank, synthetic-only, host-mismatched, proof-method-mismatched, invalid-damage, and failed-parity cases fail closed with a reviewer-visible reason.
- **SC-006**: Timing evidence covers a predeclared threshold/noise policy, at least 5 representative live scenarios, and at least 5 comparable repetitions per scenario before any performance benefit is accepted.
- **SC-007**: If timing evidence lacks a threshold/noise policy, is incomplete, environment-limited, noisy, or non-beneficial, the readiness summary explicitly records no accepted performance claim.
- **SC-008**: A reviewer can determine from one readiness summary in under 5 minutes whether P7 live partial redraw is accepted, environment-limited, failed, or fallback-gated and where each supporting evidence file lives.
- **SC-009**: Package and public contract validation pass with zero undocumented compositor readiness or diagnostic changes.
- **SC-010**: Focused regression validation for Feature 149 diagnostics and adjacent rendering readiness surfaces records accepted or explicitly limited verdicts before final readiness is accepted.

## Assumptions

- "Next item" means the remaining P7 live partial-redraw acceptance gap because the report states P8 is accepted through Feature 151 and P7 remains environment-limited.
- Feature 147, Feature 148, and Feature 149 are the starting baseline for compositor diagnostics, deterministic readiness, safe fallback, reuse, snapshot, and timing vocabulary.
- A capable presentation host may not be available in every environment; environment-limited evidence is acceptable only when it does not claim partial-redraw or performance acceptance.
- Synthetic or simulated evidence may support diagnostics, but it cannot satisfy live proof acceptance.
- Performance claims require live comparable timing evidence; absent or inconclusive timing evidence must preserve correctness and report no accepted performance benefit.
- Exact command names, artifact formats, host-profile fields, and validation task order are planning details for the next phase.
