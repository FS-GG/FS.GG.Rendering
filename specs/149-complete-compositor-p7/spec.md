# Feature Specification: Complete P7 Compositor

**Feature Branch**: `149-complete-compositor-p7`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md" interpreted as the next unimplemented roadmap item after Feature 148: complete the remaining P7 compositor work before starting P8 intrinsic layout.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Prove Partial Redraw Is Safe (Priority: P1)

A maintainer needs a live compositor proof that determines whether the current presentation host preserves previous-frame pixels well enough for damage-scoped redraw, so partial redraw is never enabled from assumption alone.

**Why this priority**: This is the gate for every other P7 compositor behavior. If the host cannot prove previous pixels remain valid, damage-scoped drawing can corrupt frames.

**Independent Test**: Can be tested by running the compositor readiness proof on a capable host and verifying that accepted evidence includes frame artifacts, pixel-preservation results, and a clear safe-to-enable or fallback decision.

**Acceptance Scenarios**:

1. **Given** a capable presentation host, **When** the live compositor proof runs across repeated frame updates, **Then** the evidence shows damaged pixels changed as expected, undamaged pixels remained valid, and partial redraw is marked accepted.
2. **Given** a host without the required presentation capability, **When** the live compositor proof runs, **Then** the result is classified as environment-limited and no partial-redraw acceptance is recorded.
3. **Given** a live proof that detects stale, blank, or inconsistent pixels, **When** readiness is assembled, **Then** the compositor remains fallback-gated and the failure cause is visible in the evidence summary.

---

### User Story 2 - Render Damage-Scoped Frames With Full-Redraw Fallback (Priority: P1)

A product or test harness user needs damage-scoped rendering to produce the same visible result as full redraw, while automatically falling back to full redraw whenever proof, damage data, or host state is insufficient.

**Why this priority**: The feature only delivers value if the optimized path is visually correct and safe failure is automatic.

**Independent Test**: Can be tested by comparing a representative compositor corpus through damage-scoped and full-redraw modes and checking that all visible output matches within the accepted tolerance.

**Acceptance Scenarios**:

1. **Given** accepted live proof and a frame with a bounded damage region, **When** the frame is rendered in damage-scoped mode, **Then** the final image matches the full-redraw reference.
2. **Given** missing proof, unsupported host capability, invalid damage bounds, or an internal compositor error, **When** a frame is rendered, **Then** the system uses full redraw and records the fallback reason.
3. **Given** a zero-damage frame after a valid prior frame, **When** rendering completes, **Then** the prior valid image is preserved or full redraw is selected if preservation cannot be guaranteed.

---

### User Story 3 - Validate Reuse, Snapshot, and Timing Readiness (Priority: P2)

A maintainer needs evidence that compositor reuse and snapshot decisions are correct, resource lifetimes are bounded, and timing probes show whether the optimized paths provide a real benefit.

**Why this priority**: P7 is a performance feature, so correctness alone is not enough. The repository must avoid shipping complexity that cannot demonstrate measurable benefit.

**Independent Test**: Can be tested by running reuse, snapshot, and timing evidence commands over the compositor corpus and confirming each report ties decisions to observable frame outcomes.

**Acceptance Scenarios**:

1. **Given** a stable expensive scene area that only changes placement, **When** consecutive frames are evaluated, **Then** the evidence records reuse instead of unnecessary content refresh and the visible result remains correct.
2. **Given** a promoted snapshot resource that is reused, replaced, or discarded, **When** readiness is assembled, **Then** the snapshot lifecycle is visible and no stale resource is used in accepted output.
3. **Given** timing probes for full redraw, damage-scoped redraw, and snapshot-assisted redraw, **When** the evidence summary is reviewed, **Then** it reports enough comparable measurements to support or reject a performance claim.

---

### User Story 4 - Publish Consumer-Visible Compositor Readiness (Priority: P3)

A package consumer or downstream generated product needs stable public diagnostics and documentation that explain compositor readiness, accepted evidence, fallback reasons, and remaining limitations without relying on private repository internals.

**Why this priority**: P7 changes package behavior and evidence surfaces. Consumers need a stable contract and maintainers need package validation to catch drift.

**Independent Test**: Can be tested by using the released package contract to query compositor proof, damage parity, reuse, snapshot, timing, and readiness summaries.

**Acceptance Scenarios**:

1. **Given** the feature package is installed, **When** a consumer requests compositor readiness diagnostics, **Then** the public surface exposes proof status, fallback status, corpus status, timing status, and limitations.
2. **Given** public contract records are refreshed, **When** package validation runs, **Then** only the documented compositor changes are reported.
3. **Given** a generated readiness report, **When** maintainers review it, **Then** they can tell within one summary whether P7 is accepted, environment-limited, or blocked by a defect.

### Edge Cases

- Capable-host detection succeeds, but pixel-preservation evidence shows that the host clears or invalidates undamaged pixels between frames.
- The damage region is empty, outside the frame, larger than the frame, or split across multiple disjoint areas.
- A scene area moves without content changes, requiring placement reuse without stale content.
- A scene area changes content without placement changes, requiring content refresh without relying on placement-only reuse.
- Snapshot resources are evicted, recreated, reused after resizing, or invalidated by content changes.
- Timing probes are noisy, incomplete, or run on an environment that cannot support a performance claim.
- Evidence generation partially succeeds, such as proof artifacts passing while package-surface validation fails.
- Existing disabled-cache, full-redraw, render-anywhere, overlay, and text-shaping parity expectations must remain valid.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST complete the remaining P7 compositor scope identified after Feature 148: live proof, damage-scoped frame acceptance, reuse evidence, snapshot composition, timing evidence, readiness assembly, consumer-visible diagnostics, documentation, and validation.
- **FR-002**: The feature MUST explicitly exclude P8 intrinsic-layout work from this feature's scope.
- **FR-003**: The feature MUST be treated as a Tier 1 contracted change because it expands consumer-visible compositor diagnostics, readiness evidence, and package-facing behavior.
- **FR-004**: The system MUST produce a live compositor proof that classifies the presentation host as accepted, environment-limited, or failed with a specific reason.
- **FR-005**: Accepted live proof MUST include artifacts that demonstrate both damaged-region updates and preservation of valid undamaged regions.
- **FR-006**: The system MUST NOT mark partial redraw accepted when proof artifacts are missing, synthetic-only, stale, blank, inconsistent, or environment-limited.
- **FR-007**: The system MUST provide a damage-scoped frame mode that can update bounded changed regions while preserving the final visible result of a full redraw.
- **FR-008**: The system MUST automatically use full redraw when live proof is not accepted, damage information is invalid, required resources are unavailable, or damage-scoped rendering reports an error.
- **FR-009**: Every fallback MUST record a user-readable reason that can be included in readiness evidence.
- **FR-010**: The system MUST compare damage-scoped rendering against full-redraw references across a representative compositor corpus.
- **FR-011**: The system MUST track reuse decisions separately for content changes and placement-only changes so maintainers can verify why a frame reused or refreshed a scene area.
- **FR-012**: The system MUST support snapshot resource lifecycle evidence covering creation, reuse, replacement, eviction, and invalidation.
- **FR-013**: Snapshot-assisted output MUST be included in compositor parity evidence before any readiness claim depends on snapshots.
- **FR-014**: The system MUST capture timing evidence for full redraw, damage-scoped redraw, and snapshot-assisted redraw over enough repeated runs to distinguish a measured benefit from noise.
- **FR-015**: Timing summaries MUST avoid performance claims when measurements are incomplete, environment-limited, or inconclusive.
- **FR-016**: Public compositor diagnostics MUST expose proof status, damage parity status, reuse status, snapshot status, timing status, fallback status, and remaining limitations.
- **FR-017**: Readiness artifacts MUST be written in a stable, reviewable form that links each acceptance claim to its supporting evidence.
- **FR-018**: Documentation MUST explain how consumers interpret accepted, environment-limited, fallback, and failed compositor states.
- **FR-019**: Public contract and package validation MUST identify all intentional consumer-visible changes and reject undocumented drift.
- **FR-020**: Existing parity, determinism, render-anywhere, overlay, text-shaping, and package-readiness guarantees MUST remain valid unless an intentional change is documented in the readiness evidence.

### Key Entities *(include if feature involves data)*

- **Live Compositor Proof**: The evidence record that determines whether the host can safely preserve undamaged pixels across damage-scoped frames.
- **Damage Region**: The bounded area or areas of a frame that changed and are eligible for scoped redraw.
- **Frame Artifact**: A captured output image, readback result, or summary used to compare damaged and undamaged frame regions.
- **Fallback Decision**: The recorded reason damage-scoped rendering was not used for a frame or run.
- **Reuse Decision**: The per-frame explanation of whether content was refreshed, placement was reused, or full redraw was selected.
- **Snapshot Resource**: A promoted reusable visual resource whose lifecycle and visible output must be validated.
- **Timing Probe**: A repeated measurement set used to compare full redraw, damage-scoped redraw, and snapshot-assisted redraw.
- **Compositor Readiness Report**: The assembled maintainer-facing summary that states whether P7 is accepted, environment-limited, or failed.
- **Public Diagnostic Surface**: The consumer-visible package contract for compositor proof, parity, reuse, snapshot, timing, and readiness state.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a capable host, at least 3 consecutive live proof runs produce accepted artifacts with zero stale-region, blank-region, or missing-artifact failures.
- **SC-002**: The representative compositor corpus reaches 100% visual parity between damage-scoped output and full-redraw references within the repository's accepted tolerance.
- **SC-003**: On an unsupported host, readiness completes with an environment-limited classification in under 2 minutes and records zero accepted partial-redraw artifacts.
- **SC-004**: Reuse and snapshot evidence covers at least 5 representative frame transitions, including content-only change, placement-only change, mixed change, no change, and resource invalidation.
- **SC-005**: Timing evidence reports comparable repeated measurements for full redraw, damage-scoped redraw, and snapshot-assisted redraw across at least 5 representative scenarios, or explicitly marks the performance result inconclusive.
- **SC-006**: A maintainer can determine from one readiness summary whether P7 is accepted, environment-limited, or failed, and can identify the supporting evidence path for each claim within 5 minutes.
- **SC-007**: Package validation and public contract validation pass with only documented compositor changes.
- **SC-008**: Focused regression validation for previous P5, P6, and P7 evidence surfaces passes with no undocumented behavior changes.

## Assumptions

- "Next item" means completing the open P7 compositor scope from the named report before starting P8 intrinsic layout.
- Feature 147 and Feature 148 artifacts are the starting baseline and are not re-specified here except where their open tasks remain part of P7 completion.
- A capable graphics/presentation host may not be available in every environment; environment-limited evidence is acceptable only when it does not claim partial-redraw acceptance.
- Synthetic or simulated evidence may support diagnostics, but it is not sufficient to accept live partial redraw.
- Performance benefit is a required readiness claim only when timing evidence is complete and comparable; otherwise the feature must report the result as inconclusive.
- Public package and diagnostic surface changes are expected and must follow the repository's Tier 1 public contract discipline.
