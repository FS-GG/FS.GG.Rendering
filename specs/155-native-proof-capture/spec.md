# Feature Specification: Native Proof Capture

**Feature Branch**: `155-native-proof-capture`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "wire the native proof-capture runner/effect interpreter to end-to-end for this capable host. then finish P7"

This specification covers the remaining P7 gap after Feature 154. Feature 154 landed the proof-set acceptance rules, same-profile parity gates, timing decision vocabulary, and readiness reporting, but it did not produce real native sentinel/damage proof artifacts on the capable host now available in this workspace. The current host reports a reachable X11 display, direct OpenGL rendering, a named AMD renderer, Present/DRI3 support, and a stable refresh source. This feature wires the live proof-capture workflow end to end so the already-defined acceptance policy can consume real current-run artifacts instead of an environment-limited placeholder.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture Real Native Proof Attempts (Priority: P1)

Release reviewers need the live proof capture flow to run on the current capable host and produce fresh sentinel and damage artifacts that can be accepted by the existing P7 proof-set rules.

**Why this priority**: P7 cannot leave environment-limited status until real current-run artifacts exist. The machine is capable; the remaining blocker is that the live capture workflow does not yet execute the native evidence path.

**Independent Test**: Can be tested by running the live proof capture flow on this host and verifying that it records three fresh accepted attempts from one host profile.

**Acceptance Scenarios**:

1. **Given** a host with a reachable display, direct graphics rendering, a renderer identity, and readback permission, **When** live proof capture is run, **Then** the host is classified as capable and the capture flow proceeds instead of emitting an environment-limited placeholder.
2. **Given** a capable host and a new proof run, **When** the capture flow executes, **Then** it records a sentinel frame, a damage frame, pixel observations, artifact quality, host profile, proof method, and diagnostics for each attempt.
3. **Given** three fresh attempts from the same host profile and proof method, **When** the proof-set gate is evaluated, **Then** exactly those three attempts are selected and accepted.

---

### User Story 2 - Interpret the Proof Workflow Effects (Priority: P1)

Maintainers need the proof workflow to remain observable as a pure state transition plus an edge interpreter, so capture failures can be diagnosed without hiding I/O inside untestable logic.

**Why this priority**: The constitution requires stateful I/O workflows to expose their state, messages, effects, and interpreter boundary. P7 proof capture is a multi-step native workflow and must stay auditable.

**Independent Test**: Can be tested by exercising the proof workflow transitions without native I/O, then executing the interpreter on the capable host and confirming the emitted effects become real artifacts and follow-up messages.

**Acceptance Scenarios**:

1. **Given** the proof workflow starts, **When** the host profile is detected, **Then** the workflow requests sentinel presentation before damage presentation.
2. **Given** sentinel presentation succeeds, **When** damage presentation is requested, **Then** the workflow records the damage region and requests pixel observation.
3. **Given** pixel observation completes, **When** artifact writing succeeds, **Then** the workflow produces one complete proof attempt with reviewer-visible diagnostics.
4. **Given** profile detection, presentation, readback, observation, artifact writing, or timeout fails, **When** the interpreter reports the failure, **Then** the workflow fails closed with a specific reason and no accepted partial-redraw artifact.

---

### User Story 3 - Finish P7 Partial-Redraw Readiness (Priority: P1)

Package consumers and maintainers need P7 readiness to move from environment-limited to accepted for the current capable host only when real proof and same-profile parity evidence pass.

**Why this priority**: P7 has already shipped fallback-gated safety. Finishing P7 means replacing the placeholder limitation with accepted live partial-redraw readiness for a concrete host profile without making broader or unsupported claims.

**Independent Test**: Can be tested by reviewing the final readiness package and confirming that proof, parity, fallback, timing, compatibility, and limitations all reference the accepted host profile and current run artifacts.

**Acceptance Scenarios**:

1. **Given** an accepted three-attempt proof set, **When** the same-profile damage parity corpus passes or records safe fallback reasons for required scenarios, **Then** partial redraw is accepted for that host profile.
2. **Given** proof acceptance without same-profile parity, **When** readiness is assembled, **Then** partial redraw remains fallback-gated and names parity as the blocker.
3. **Given** proof or parity evidence from another host profile, a stale run, synthetic evidence, or an unsupported environment, **When** readiness is evaluated, **Then** that evidence cannot finish P7 for the current host.

---

### User Story 4 - Preserve Safe Unsupported-Host Behavior (Priority: P2)

Maintainers need unsupported-host validation to remain a separate fail-closed regression path even after this capable host produces accepted proof artifacts.

**Why this priority**: Closing P7 for one capable profile must not accidentally treat unavailable displays, missing renderers, denied readback, or synthetic evidence as accepted proof.

**Independent Test**: Can be tested by running the unsupported-host validation path and confirming it records zero accepted partial-redraw artifacts while the capable-host artifacts remain selected separately.

**Acceptance Scenarios**:

1. **Given** display variables are intentionally unavailable, **When** unsupported-host validation runs, **Then** the result remains environment-limited and records zero accepted partial-redraw artifacts.
2. **Given** accepted capable-host evidence exists, **When** unsupported-host validation runs later, **Then** it does not overwrite, select, or weaken the accepted proof set.
3. **Given** a user reviews the readiness package, **When** both capable-host and unsupported-host evidence are present, **Then** the summary clearly separates accepted host claims from unsupported-host limitations.

---

### User Story 5 - Publish a Reviewable P7 Closeout (Priority: P3)

Reviewers need one P7 closeout package that states what is accepted, what is only safe fallback, and whether any performance claim is supported.

**Why this priority**: Consumers should not infer readiness from individual logs. The closeout must distinguish partial-redraw correctness acceptance from any measured performance claim.

**Independent Test**: Can be tested by opening the readiness summary and verifying that a reviewer can identify selected attempts, host profile, artifact paths, parity status, timing status, fallback status, compatibility impact, and remaining limitations.

**Acceptance Scenarios**:

1. **Given** accepted proof and accepted same-profile parity, **When** readiness is published, **Then** the summary states P7 partial-redraw readiness is accepted for the current host profile.
2. **Given** timing evidence is missing, noisy, inconclusive, or non-beneficial, **When** readiness is published, **Then** no performance claim is accepted even if partial-redraw correctness is accepted.
3. **Given** timing evidence satisfies the declared threshold and noise policy, **When** readiness is published, **Then** the supported performance claim and its scope are stated separately from correctness readiness.

### Edge Cases

- The host exposes both X11 and Wayland display variables; the selected proof profile must be stable and explicit.
- The host probes as capable but native presentation, readback, artifact writing, or pixel decoding fails.
- Only one or two attempts complete successfully.
- Three attempts complete but use different host profiles, proof methods, framebuffer sizes, or stale timestamps.
- Captured images are blank, undecodable, synthetic, copied from an older run, or missing from disk.
- Damaged pixels update but undamaged pixels lose the sentinel identity.
- Undamaged pixels preserve the sentinel identity but the damaged region does not update.
- The window is occluded, minimized, hidden, resized, moved across displays, or otherwise changes capture conditions during the run.
- The unsupported-host validation path runs before or after capable-host acceptance.
- Same-profile parity passes required safe-fallback scenarios but fails a scenario that must match full redraw.
- Timing evidence is noisy, incomplete, cross-profile, non-beneficial, or unavailable after correctness readiness is accepted.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST run the live proof capture flow on the current capable host and MUST NOT classify that host as environment-limited when display, renderer, readback, permission, and timeout checks pass.
- **FR-002**: The feature MUST use the Feature 154 proof-set acceptance vocabulary and MUST NOT redefine accepted proof, accepted parity, fallback-gated, environment-limited, or performance-claim semantics.
- **FR-003**: The feature MUST expose the native proof workflow through state, messages, effects, a pure transition, and an edge interpreter for native I/O.
- **FR-004**: The edge interpreter MUST execute profile detection, sentinel presentation, damage presentation, pixel observation, artifact quality evaluation, artifact writing, and failure reporting.
- **FR-005**: The capture flow MUST produce exactly three selected accepted attempts before accepting the proof set for the current host profile.
- **FR-006**: Each accepted attempt MUST include fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts tied to the current run identity.
- **FR-007**: Each accepted attempt MUST verify that damaged pixels update and undamaged pixels preserve the sentinel identity.
- **FR-008**: The accepted proof set MUST reject missing, stale, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, failed-pixel, incomplete, timed-out, or artifact-write-failed evidence with reviewer-visible reasons.
- **FR-009**: The capture output MUST record host profile, display environment, renderer identity, proof method, framebuffer size, attempt identity, selected attempts, artifact paths, and diagnostics.
- **FR-010**: The unsupported-host path MUST remain environment-limited, MUST record zero accepted partial-redraw artifacts, and MUST NOT select unsupported evidence into the accepted proof set.
- **FR-011**: Same-profile damage parity MUST run after proof acceptance and MUST cover the required P7 damage scenarios before partial redraw can be accepted for the host profile.
- **FR-012**: Partial redraw MUST remain fallback-gated unless the current host profile has both an accepted proof set and accepted same-profile parity.
- **FR-013**: The final P7 readiness package MUST state proof-set status, selected attempt identities, artifact locations, host profile, parity status, timing status, fallback status, compatibility impact, unsupported-host result, and remaining limitations.
- **FR-014**: Timing MUST remain a separate decision from correctness readiness and MUST accept no performance claim unless comparable same-profile evidence satisfies the declared threshold and noise policy.
- **FR-015**: Existing Feature 154 synthetic rejection tests MUST remain disclosed and MUST continue to prove rejection paths only, never acceptance.
- **FR-016**: Existing P0-P8 readiness evidence outside P7 proof capture MUST remain valid unless the readiness package documents an intentional compatibility change.
- **FR-017**: The feature MUST be classified as Tier 1 because it can change public readiness status, diagnostics, fallback behavior, package evidence, and consumer-visible P7 claims.

### Key Entities *(include if feature involves data)*

- **Native Proof Run**: A current execution on a detected capable host that owns one proof-set attempt sequence, run identity, diagnostics, and output location.
- **Host Capability Record**: The facts that decide whether the current machine can run accepted live proof capture or must fail closed as unsupported.
- **Proof Workflow State**: The observable state of the live proof workflow as it moves through profile detection, sentinel presentation, damage presentation, observation, artifact writing, and completion.
- **Proof Workflow Effect**: A requested native action emitted by the pure workflow transition and executed only by the edge interpreter.
- **Proof Attempt Artifact**: The reviewer-visible output for one attempt, including sentinel frame, damage frame, observations, artifact quality, proof metadata, and diagnostics.
- **Accepted Proof Set**: The three selected accepted attempts from one host profile and one proof method that unlock the proof gate.
- **Same-Profile Parity Result**: The damage-scenario result proving final output parity or recording a safe full-redraw fallback for the same accepted host profile.
- **Timing Decision**: The accepted, rejected, or inconclusive status for a performance claim, scoped separately from correctness readiness.
- **P7 Closeout Summary**: The final readiness entry point that aggregates proof, parity, timing, fallback, unsupported-host, compatibility, and limitations.

### Scope and Classification

- In scope: native capable-host proof capture, effect interpretation, three-attempt acceptance, artifact quality evaluation, unsupported-host regression, same-profile parity closeout, timing decision publication, and P7 readiness update.
- Out of scope: redefining the proof acceptance rules, accepting synthetic evidence, claiming performance without timing evidence, changing P8 layout acceptance, adding a new rendering backend, changing text shaping, changing overlay behavior, or restoring unrelated tooling wrappers.
- Expected classification: Tier 1, because the feature changes consumer-visible readiness, fallback status, diagnostics, and package evidence.
- Public surface changes are allowed only when needed to make the proof workflow, interpreter result, artifacts, or readiness status observable through the existing package boundaries.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On the current capable host, the live proof capture flow classifies the host as capable and produces no environment-limited placeholder for the capable run.
- **SC-002**: A single capable-host closeout run produces 3 selected accepted proof attempts from one host profile and one proof method.
- **SC-003**: 100% of selected attempts include fresh, decodable, non-blank, non-synthetic sentinel and damage artifacts tied to the current run identity.
- **SC-004**: 100% of selected attempts verify damaged-pixel update and undamaged-pixel preservation.
- **SC-005**: 100% of missing, stale, blank, synthetic-only, undecodable, host-mismatched, proof-method-mismatched, failed-pixel, incomplete, timed-out, and artifact-write-failed cases fail closed with visible reasons.
- **SC-006**: The same-profile parity closeout records verdicts for all 10 required P7 damage paths before partial redraw is accepted for the host profile.
- **SC-007**: The final readiness summary reports proof-set status as accepted, selected attempts as `3/3`, and partial-redraw readiness as accepted for the current host profile when proof and parity pass.
- **SC-008**: Unsupported-host validation completes in under 2 minutes and records zero accepted partial-redraw artifacts.
- **SC-009**: A reviewer can locate the selected attempts, sentinel artifacts, damage artifacts, host profile, parity verdicts, timing decision, fallback status, and remaining limitations from the closeout summary in under 5 minutes.
- **SC-010**: No performance claim is accepted unless timing evidence satisfies the declared threshold and noise policy; otherwise the closeout records no accepted performance claim while preserving correctness readiness.
- **SC-011**: Focused P7 proof-capture, proof-acceptance, parity, unsupported-host, package, and public-surface validation pass with zero undocumented consumer-visible drift.
- **SC-012**: Broad regression validation passes or records only unrelated, pre-existing limitations that do not affect the P7 closeout claim.

## Assumptions

- The current workspace host is a capable target for live proof capture: it has a reachable display, direct graphics rendering, renderer identity, Present/DRI3 support, and readback-capable graphics device access.
- When both X11 and Wayland display variables are present, the accepted proof profile will choose one stable display environment and record it explicitly.
- "Finish P7" means accepting live partial-redraw correctness readiness for the current capable host profile, not claiming universal support across every possible host.
- A performance benefit is not required to accept partial-redraw correctness; performance must remain a separately measured and separately stated claim.
- Existing Feature 154 proof-set acceptance, parity, timing, compatibility, and readiness vocabulary is the baseline for this work.
- Unsupported-host validation remains required even when capable-host proof acceptance succeeds.
