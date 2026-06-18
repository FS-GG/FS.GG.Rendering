# Feature Specification: No-Clear Damage-Scissored Render Path

**Feature Branch**: `157-no-clear-damage-scissor`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers Feature 157 from the radical rendering architecture report: the no-clear damage-scissored render path. Feature 155 accepted current-host partial-redraw correctness, and Feature 156 recorded same-profile timing as noisy. This feature turns the accepted correctness proof into a real damage-scoped repaint path that preserves prior frame content outside the declared damage region, updates damaged pixels, and fails closed to full redraw whenever the safety gates are not met.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Use Damage-Scoped Repaint Only When Safe (Priority: P1)

Release reviewers need the renderer to use damage-scoped repaint only when the current host has accepted proof, a retained frame is available, and the frame damage is valid, so partial redraw cannot corrupt the visible frame.

**Why this priority**: The report's next P7 performance step depends on the real render path, but correctness must remain stronger than the performance goal.

**Independent Test**: Can be tested by running representative frame updates on the accepted host profile and verifying that damage-scoped repaint is selected only when every safety gate is present.

**Acceptance Scenarios**:

1. **Given** an accepted current-host proof gate, valid damage, and retained previous frame content, **When** a frame is repainted with the no-clear path, **Then** pixels outside the declared damage region are preserved and pixels inside the damage region reflect the new frame.
2. **Given** the accepted proof gate is missing, stale, cross-profile, or rejected, **When** a frame is repainted, **Then** the frame uses full redraw and records a fail-closed reason.
3. **Given** the previous frame content is unavailable, stale, or disconnected from the current run, **When** damage-scoped repaint is requested, **Then** the frame uses full redraw and does not accept partial-redraw evidence.

---

### User Story 2 - Fall Back for Unsafe or Invalid Damage (Priority: P1)

Maintainers need every unsafe condition to fall back to full redraw with clear diagnostics, so a damaged or partially preserved frame never becomes an accepted output.

**Why this priority**: Damage scissoring is only correct when the retained content and damage region describe the whole visible change. Ambiguous or invalid inputs must not produce partial output.

**Independent Test**: Can be tested by feeding invalid damage, missing retained backing, unsupported-host facts, resource failures, and parity mismatches, then verifying that every case takes full redraw and explains why.

**Acceptance Scenarios**:

1. **Given** a damage region that is empty for a visible change, out of bounds, stale, duplicated, or incomplete, **When** the frame is evaluated, **Then** full redraw is selected with a damage-validation reason.
2. **Given** retained backing cannot be trusted because of host limitations or resource failure, **When** a frame is repainted, **Then** full redraw is selected and the partial-redraw path records zero accepted artifacts.
3. **Given** a damage-scoped frame differs from the equivalent full-redraw frame outside allowed damage, **When** parity is evaluated, **Then** the result is rejected and future frames fall back until a fresh accepted proof is available.

---

### User Story 3 - Publish Reviewable Correctness Evidence (Priority: P2)

Release reviewers and package consumers need one evidence package showing which frames used damage-scoped repaint, which frames fell back, and why the final readiness status is safe.

**Why this priority**: The runtime path should not be accepted from code changes alone. Reviewers need linked proof, parity, fallback, diagnostics, and readiness summaries.

**Independent Test**: Can be tested by opening the readiness summary and verifying that a reviewer can trace accepted runs, rejected runs, scenario coverage, host profile, fallback reasons, parity results, and remaining performance limitations from one entry point.

**Acceptance Scenarios**:

1. **Given** damage-scoped repaint has been exercised, **When** readiness is assembled, **Then** the summary lists accepted attempts, representative scenarios, host profile, damage validation status, parity status, fallback reasons, artifact locations, and final claim status.
2. **Given** unsupported or unavailable presentation environments are exercised, **When** readiness is assembled, **Then** they are marked environment-limited with zero accepted partial-redraw artifacts.
3. **Given** damage-scoped correctness is accepted but later performance gates remain incomplete, **When** readiness is assembled, **Then** the feature is marked correct for the measured profile while the shipped performance claim remains `performance-not-accepted`.

---

### User Story 4 - Preserve Existing Safety Claims and Boundaries (Priority: P2)

Maintainers need Feature 157 to preserve existing P7 correctness, unsupported-host behavior, package validation, and public compatibility boundaries, so adding the runtime path does not weaken earlier acceptance.

**Why this priority**: This feature builds on accepted correctness evidence. It must not reopen unrelated P0-P8 acceptance, P8 layout behavior, text shaping, overlays, or general host support.

**Independent Test**: Can be tested by running focused correctness, fallback, package, and compatibility checks before closeout and verifying that any consumer-visible drift is intentional and documented.

**Acceptance Scenarios**:

1. **Given** earlier P7 proof and readiness evidence, **When** Feature 157 is added, **Then** correctness acceptance remains scoped to the same host/profile rules and unsupported hosts still fail closed.
2. **Given** a public compatibility change is required for diagnostics or readiness status, **When** the feature is planned, **Then** the change is classified, documented, and validated through the public package boundary.
3. **Given** no public compatibility change is required, **When** validation completes, **Then** public-surface drift checks report no undocumented consumer-visible changes.

### Edge Cases

- The damage region is empty while the frame content changed.
- The damage region is non-empty but outside the visible frame bounds.
- The damage region is stale, duplicated from a prior run, or disconnected from the current frame identity.
- The accepted proof belongs to a different host profile, display environment, renderer identity, package version, or run identity.
- The previous frame cannot be retained, cannot be trusted, or is lost during resource pressure.
- The host reports support but the preserved pixels are not actually stable across frames.
- Damage-scoped output updates the damaged pixels but also changes pixels outside the declared damage region.
- Full-redraw fallback succeeds after damage-scoped repaint fails.
- Unsupported-host validation runs in the same checkout as accepted-host artifacts.
- Existing proof, parity, package, compatibility, or public-surface validation exposes undocumented drift while the render path is being added.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST provide a damage-scoped repaint mode that can preserve existing frame content outside a declared damage region while repainting the damaged region.
- **FR-002**: Damage-scoped repaint MUST be eligible only when the current run has an accepted proof gate, matching host profile, trusted retained previous frame content, valid damage, available resources, and passing parity evidence.
- **FR-003**: The system MUST use full redraw by default whenever any eligibility gate is missing, rejected, stale, cross-profile, incomplete, or unverifiable.
- **FR-004**: Accepted damage-scoped frames MUST demonstrate that untouched pixels are preserved and damaged pixels are updated for representative frame updates.
- **FR-005**: Invalid, stale, incomplete, out-of-bounds, or ambiguous damage MUST fail closed to full redraw with a reviewer-visible reason.
- **FR-006**: Unsupported hosts, unavailable presentation environments, missing retained backing, resource failures, proof mismatches, and parity mismatches MUST produce full-redraw fallback and zero accepted partial-redraw artifacts.
- **FR-007**: Evidence from different host profiles, display environments, renderer identities, package versions, run identities, or scenario definitions MUST NOT be combined into an accepted damage-scoped result.
- **FR-008**: The feature MUST include representative scenarios covering static preserved content, localized changes, moving content, scrolling or shifted content, and nested retained content.
- **FR-009**: The readiness package MUST include at least three fresh accepted current-host attempts before the damage-scoped render path is marked accepted for the measured profile.
- **FR-010**: Each accepted attempt MUST link to frame identity, damage validation status, preserved-pixel evidence, damaged-pixel evidence, parity status, host profile, run identity, and artifact locations.
- **FR-011**: Each rejected or fallback attempt MUST record a reason that distinguishes proof rejection, host limitation, missing retained content, invalid damage, resource failure, parity mismatch, and environment limitation.
- **FR-012**: The feature MUST preserve the existing unsupported-host fail-closed policy and MUST NOT accept partial-redraw artifacts from unsupported or unavailable presentation environments.
- **FR-013**: The final readiness summary MUST state whether the no-clear damage-scissored render path is accepted, fallback-only, rejected, or environment-limited for the measured host profile.
- **FR-014**: The final readiness summary MUST keep the shipped compositor performance claim at `performance-not-accepted` unless all report-defined later performance gates are also satisfied.
- **FR-015**: The feature MUST be classified as Tier 1 because it changes observable rendering behavior and may add consumer-visible readiness diagnostics or compatibility notes.
- **FR-016**: Public compatibility changes are allowed only when needed to expose damage-scoped readiness status, diagnostics, fallback reasons, or package-facing validation; any such change MUST be documented and validated.
- **FR-017**: Existing P0-P8 acceptance evidence, Feature 155 correctness readiness, Feature 156 timing evidence, unsupported-host behavior, package validation, and public-surface drift checks MUST remain valid unless an intentional compatibility change is documented.

### Key Entities *(include if feature involves data)*

- **Host Proof Gate**: The accepted current-host correctness proof that allows damage-scoped repaint to be considered for a run.
- **Host Profile**: The stable presentation environment identity used to determine whether proof, render attempts, and readiness evidence are comparable.
- **Retained Frame State**: The previous frame content and identity that must be trusted before any no-clear repaint can preserve untouched pixels.
- **Damage Region**: The declared visible area that must be repainted for the current frame.
- **Damage-Scoped Frame Attempt**: One frame repaint attempt that uses the retained previous frame plus the current damage region.
- **Full-Redraw Fallback**: The safe frame repaint behavior used whenever damage-scoped repaint is not eligible or not accepted.
- **Damage Validation Result**: The complete, invalid, stale, out-of-bounds, empty, or ambiguous classification for a damage region.
- **Parity Result**: The comparison between damage-scoped output and the equivalent safe output for the same frame.
- **Fallback Reason**: The reviewer-visible explanation for why a frame used full redraw or why partial redraw evidence was rejected.
- **Readiness Summary**: The review entry point that aggregates accepted attempts, rejected attempts, scenarios, host profile, artifact paths, compatibility impact, and final claim status.

### Scope and Classification

- In scope: no-clear damage-scoped repaint, eligibility gates, retained-frame trust checks, damage validation, preserved-pixel and damaged-pixel correctness evidence, full-redraw fallback, unsupported-host behavior, parity checks, readiness summaries, compatibility notes, and package-facing validation.
- Out of scope: separating proof readback from timing paths, changing the Feature 156 timing policy, adding layer promotion, splitting content and placement identity, optimizing performance-validation throughput, creating the full host performance lane ledger, changing P8 layout acceptance, changing text shaping, changing overlay behavior, or claiming universal compositor performance.
- Expected classification: Tier 1, because the feature changes observable rendering behavior and can add consumer-visible diagnostics, readiness status, compatibility notes, and package-facing validation.
- Public surface changes are allowed only when needed for readiness status, fallback diagnostics, artifact inspection, or compatibility evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of damage-scoped repaint attempts are accepted only when all eligibility gates pass for the same host profile and run identity.
- **SC-002**: At least 3 fresh accepted current-host attempts demonstrate preserved untouched pixels and updated damaged pixels across at least 5 representative scenarios.
- **SC-003**: 100% of accepted scenarios include damage validation status, preserved-pixel evidence, damaged-pixel evidence, parity status, host profile, run identity, and artifact locations.
- **SC-004**: 100% of invalid damage, stale proof, cross-profile evidence, missing retained content, unsupported-host, resource-failure, and parity-mismatch cases fall back to full redraw with a reviewer-visible reason.
- **SC-005**: Unsupported-host validation completes in under 2 minutes and records zero accepted partial-redraw artifacts.
- **SC-006**: Accepted damage-scoped outputs match the equivalent safe output for every validated scenario, with zero unexplained pixel drift outside the declared damage region.
- **SC-007**: A reviewer can determine accepted attempts, rejected attempts, scenario coverage, fallback reasons, host scope, compatibility impact, artifact paths, and final claim status from one summary in under 5 minutes.
- **SC-008**: Existing Feature 155 correctness readiness and Feature 156 timing evidence remain valid, and unsupported hosts still fail closed.
- **SC-009**: Focused correctness, fallback, unsupported-host, package, and public-compatibility validation pass with zero undocumented consumer-visible drift before the feature is marked ready for implementation closeout.
- **SC-010**: The shipped compositor performance claim remains `performance-not-accepted` unless all report-defined later performance gates are also satisfied.

## Assumptions

- "Next item" refers to the first unchecked item in the report's "Planned P7 performance follow-up features" tracker: Feature 157, no-clear damage-scissored render path.
- Feature 155 current-host correctness acceptance is the safety baseline for deciding whether damage-scoped repaint can be considered.
- Feature 156 timing evidence remains noisy and does not by itself establish a shipped performance claim.
- Full redraw is always the safe fallback and remains available for every frame.
- The readiness package can reuse existing P7 vocabulary for proof gates, parity, environment-limited results, artifact identity, fallback status, and performance claim status.
- Feature 158, Feature 159, Feature 160, and Feature 161 remain separate follow-up work. This feature may make the real damage-scoped render path correct for the measured profile, but it does not by itself accept a final compositor performance claim.
- Exact command names, artifact filenames, scenario labels, and validation task order are planning details for the next phase.
