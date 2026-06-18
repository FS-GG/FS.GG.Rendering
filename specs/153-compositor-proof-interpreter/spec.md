# Feature Specification: Compositor Proof Interpreter

**Feature Branch**: `153-compositor-proof-interpreter`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers the next open item from the radical rendering architecture report. P0 through P8 are already implemented or accepted, while P7 live compositor readiness remains environment-limited after Feature 152. The next slice is the real host-backed proof interpreter: run live proof attempts, capture evidence for sentinel and damage-scoped frames, classify each attempt through the existing proof-set vocabulary, and keep partial redraw fallback-gated unless three fresh matching capable-host attempts are accepted.

This feature does not claim the full P7 performance win. It creates the live proof evidence needed before later same-host parity and timing gates can safely decide whether to unlock partial redraw or publish a performance claim.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Produce Real Live Proof Attempts (Priority: P1)

Release reviewers need live proof attempts that exercise the presentation host and produce reviewable frame evidence, so the compositor can move beyond environment-limited policy text.

**Why this priority**: Feature 152 already defines acceptance rules, but current evidence records zero accepted partial-redraw artifacts. Without real live attempts, partial redraw must remain fallback-gated.

**Independent Test**: Can be tested by running the live proof on a capable host and verifying that each attempt records host identity, frame evidence, freshness, artifact quality, and an accepted, failed, or environment-limited classification.

**Acceptance Scenarios**:

1. **Given** a capable presentation host, **When** a live proof attempt runs, **Then** it records a host profile, proof method, sentinel-frame evidence, damage-frame evidence, freshness information, and a classification.
2. **Given** a capable presentation host where the damaged frame updates expected pixels and preserves undamaged pixels, **When** the attempt is evaluated, **Then** the attempt is accepted and its artifacts are marked usable for proof-set evaluation.
3. **Given** a capable presentation host where the damaged frame is stale, blank, missing, synthetic-only, or does not preserve undamaged pixels, **When** the attempt is evaluated, **Then** the attempt fails closed with a reviewer-visible reason.

---

### User Story 2 - Preserve Unsupported-Host Safety (Priority: P1)

Maintainers need unsupported or unavailable presentation environments to remain safe and explicit, so local or continuous validation can run without accidentally accepting partial redraw from incomplete evidence.

**Why this priority**: The current readiness limitation is environment-related. The feature must improve capable-host proof without weakening unsupported-host behavior.

**Independent Test**: Can be tested by running the proof with no capable presentation environment and verifying that it exits with an environment-limited result, records the blocking cause, and accepts no partial-redraw artifacts.

**Acceptance Scenarios**:

1. **Given** no usable presentation host, **When** the live proof runs, **Then** it records an environment-limited result and zero accepted partial-redraw artifacts.
2. **Given** host setup or capture permissions prevent reliable frame evidence, **When** the proof attempt is evaluated, **Then** the result is environment-limited or failed rather than accepted.
3. **Given** a previous accepted attempt from another host profile, **When** the current host is unsupported or mismatched, **Then** the previous evidence cannot accept the current run.

---

### User Story 3 - Aggregate the Three-Run Proof Set (Priority: P2)

Reviewers need one decision over three fresh matching live attempts, so acceptance is based on repeated capable-host evidence rather than a one-off run.

**Why this priority**: Feature 152 requires three fresh matching capable-host attempts before partial redraw can be accepted. The interpreter slice must feed that decision directly.

**Independent Test**: Can be tested by running three proof attempts on the same capable host profile and verifying that the proof-set decision accepts only when all three attempts match and individually pass.

**Acceptance Scenarios**:

1. **Given** three accepted proof attempts from the same host profile and proof method, **When** the proof set is evaluated, **Then** the proof set is accepted and links all three evidence records.
2. **Given** fewer than three accepted attempts, **When** the proof set is evaluated, **Then** the proof set is not accepted and identifies the missing attempt count.
3. **Given** accepted attempts from different host profiles, different proof methods, stale runs, or mixed accepted and failed results, **When** the proof set is evaluated, **Then** the proof set fails closed with specific reasons.

---

### User Story 4 - Publish Reviewable Proof Readiness (Priority: P3)

Package consumers and maintainers need the readiness summary to state what the new live interpreter proved, what remains fallback-gated, and what work is still required before a performance claim.

**Why this priority**: The report separates proof, parity, and timing gates. Consumers should not infer that partial redraw or performance is accepted just because live proof attempts can run.

**Independent Test**: Can be tested by reviewing the readiness package and confirming that it links proof attempts, proof-set status, unsupported-host behavior, fallback status, and remaining parity and timing gates from one place.

**Acceptance Scenarios**:

1. **Given** an accepted three-run proof set, **When** readiness is assembled, **Then** the summary states that the host proof gate is accepted for that profile and that same-profile parity and timing gates remain separate decisions.
2. **Given** environment-limited or failed proof evidence, **When** readiness is assembled, **Then** the summary states that partial redraw remains fallback-gated and identifies the blocking evidence.
3. **Given** public diagnostics or package-visible readiness changes, **When** compatibility validation is reviewed, **Then** intentional changes are documented and undocumented public drift is rejected.

### Edge Cases

- A proof run captures a sentinel frame but no damage frame.
- A damage frame is decodable but blank, stale, synthetic-only, or from a previous run.
- Damaged pixels update correctly but undamaged pixels are not preserved.
- Undamaged pixels are preserved but the damaged region does not update.
- The host profile changes between attempts.
- The proof method changes between attempts.
- Only one or two attempts are accepted.
- A previous proof set exists but is stale for the current host or current build.
- Unsupported-host validation runs in the same checkout as a capable-host proof.
- Proof evidence exists, but same-host parity and timing evidence are not yet available.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST implement the next report item by producing real host-backed live proof attempts for the P7 compositor readiness gate.
- **FR-002**: The feature MUST use the existing Feature 152 proof-set vocabulary as the acceptance language and MUST NOT redefine the broader P7 readiness policy.
- **FR-003**: The feature MUST be classified as a Tier 1 contracted change because it changes consumer-visible compositor readiness evidence, diagnostics, or fallback status.
- **FR-004**: Each proof attempt MUST record the host profile, proof method, attempt freshness, sentinel-frame evidence, damage-frame evidence, artifact quality, and final attempt classification.
- **FR-005**: A proof attempt MUST be accepted only when it shows the damaged frame updating expected damaged pixels while preserving expected undamaged pixels.
- **FR-006**: Missing, stale, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, or quality-failed evidence MUST NOT accept a proof attempt.
- **FR-007**: Unsupported or unavailable presentation environments MUST produce an environment-limited result with a specific reason and zero accepted partial-redraw artifacts.
- **FR-008**: Proof artifacts MUST be reviewable after the run and MUST identify which attempt, host profile, proof method, and frame role they support.
- **FR-009**: The feature MUST support aggregating exactly three fresh matching capable-host attempts into one proof-set decision.
- **FR-010**: A proof set MUST be accepted only when all three attempts are individually accepted, fresh, host-matching, and proof-method-matching.
- **FR-011**: Fewer than three accepted attempts, mixed classifications, stale attempts, mismatched host profiles, or mismatched proof methods MUST fail the proof set closed with reviewer-visible reasons.
- **FR-012**: The readiness summary MUST state whether the live proof interpreter produced an accepted proof set, an environment-limited result, or a failed proof set.
- **FR-013**: The readiness summary MUST state that partial redraw remains fallback-gated unless the proof set is accepted and later same-profile parity requirements are also met.
- **FR-014**: The readiness summary MUST state that performance claims remain unaccepted until later same-profile live timing evidence satisfies a declared threshold and noise policy.
- **FR-015**: Existing deterministic compositor diagnostics, Feature 152 proof-set rules, layout acceptance, render-anywhere behavior, text-shaping behavior, overlay behavior, package checks, and public-surface drift checks MUST remain valid unless a compatibility note documents the intentional change.
- **FR-016**: Public diagnostics or package-facing readiness changes MUST include compatibility notes and validation evidence before the feature is considered ready for planning closeout.

### Key Entities *(include if feature involves data)*

- **Proof Attempt**: One live attempt to validate whether a presentation host can support damage-scoped redraw safely.
- **Host Profile**: The recorded presentation environment identity used to decide whether attempts belong to the same capable host.
- **Proof Method**: The recorded method used to produce and evaluate the sentinel and damage-scoped frame evidence.
- **Sentinel Frame Evidence**: The proof artifact and metadata used to establish the starting frame for a live attempt.
- **Damage Frame Evidence**: The proof artifact and metadata used to show whether damaged pixels update and undamaged pixels remain valid.
- **Artifact Quality Decision**: The pass, fail, or limitation result for decodability, blankness, freshness, synthetic status, and run identity.
- **Proof Set Decision**: The aggregate accepted, failed, or environment-limited result over the required three matching attempts.
- **Fallback Status**: The readiness decision that keeps full redraw active when live proof is not accepted or later gates are incomplete.
- **Readiness Summary**: The review entry point that links attempts, proof-set status, unsupported-host behavior, compatibility impact, and remaining gates.

### Scope and Classification

- In scope: live proof attempts, sentinel and damage-frame evidence, proof artifact quality, three-run proof-set aggregation, unsupported-host behavior, fallback status, readiness summary, and compatibility validation.
- Out of scope: enabling partial redraw by default, accepting the representative damage-scoped parity corpus, accepting performance claims, changing layout semantics, changing text shaping, adding browser production backends, and redesigning compositor policy beyond what live proof attempts require.
- Expected classification: Tier 1, because this feature can change compositor readiness evidence and consumer-visible fallback diagnostics.
- Public surface changes are allowed only when needed to expose proof attempts, proof-set decisions, or readiness diagnostics already justified by the report and Feature 152 vocabulary.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A capable-host proof run produces at least 1 sentinel-frame artifact, at least 1 damage-frame artifact, and a complete proof-attempt classification for each attempt.
- **SC-002**: 100% of accepted proof attempts include decodable, non-blank, non-synthetic, fresh evidence for both sentinel and damage-frame roles.
- **SC-003**: 100% of accepted proof attempts verify both damaged-pixel update and undamaged-pixel preservation.
- **SC-004**: A proof set is accepted only after exactly 3 fresh matching capable-host attempts are individually accepted for the same host profile and proof method.
- **SC-005**: 100% of missing, stale, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, failed-pixel, and incomplete-attempt cases fail closed with visible reasons.
- **SC-006**: Unsupported-host validation completes in under 2 minutes and records zero accepted partial-redraw artifacts.
- **SC-007**: A reviewer can determine the proof-set status, supporting artifact locations, host profile, proof method, and remaining gates from one readiness summary in under 5 minutes.
- **SC-008**: The readiness summary explicitly states whether partial redraw remains fallback-gated and whether performance claims remain unaccepted.
- **SC-009**: Focused compatibility validation records zero undocumented public readiness or diagnostic drift.
- **SC-010**: Existing Feature 152 proof-set checks and adjacent rendering readiness checks continue to report accepted or explicitly limited verdicts after the new interpreter evidence is added.

## Assumptions

- "Next item" refers to the report's first next step after Feature 152: a real host-backed proof interpreter for live sentinel and damage-scoped frame evidence.
- Feature 152's proof-set acceptance vocabulary remains the authoritative policy for three-run proof decisions.
- Some local or automated environments will not have a capable presentation host; those environments should remain useful for unsupported-host regression evidence without accepting partial redraw.
- A live proof-set acceptance is necessary but not sufficient to publish a compositor performance claim.
- Same-profile damage-scoped parity and live timing evidence are planned as later gates after the interpreter can produce accepted proof attempts.
- Exact command names, artifact filenames, host-profile fields, and validation task order are planning details for the next phase.
