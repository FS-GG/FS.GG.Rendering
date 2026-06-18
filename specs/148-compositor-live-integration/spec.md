# Feature Specification: Compositor Live Integration

**Feature Branch**: `148-compositor-live-integration`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The referenced radical rendering architecture report shows that Feature 147 landed the deterministic P7 compositor readiness slice, but it did not complete the live proof and shipped-value work. This specification covers the next P7 slice: prove partial redraw on a real rendering host, integrate damage-scoped redraw with safe full-frame fallback, complete content-versus-placement reuse, add bounded snapshot lifecycle behavior, and record real timing evidence before any compositor performance benefit is claimed.

This is expected to be a Tier 1 feature because it changes observable rendering behavior, diagnostics, readiness evidence, and performance claims. Public surface changes are allowed only where they are needed for users or reviewers to observe readiness, limitations, or compatibility impact.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Prove Live Partial Redraw Safety (Priority: P1)

Framework maintainers can run a live preservation proof on the active rendering host and receive a clear passed, failed, or environment-limited result before partial redraw is accepted.

**Why this priority**: The previous feature deliberately stopped short of live proof. Damage-scoped redraw is unsafe unless unchanged frame regions are known to survive between presents on the current host profile.

**Independent Test**: Run the live preservation proof on a capable host, a host or simulation that does not preserve untouched regions, and an unsupported environment. The feature delivers value when only a passed matching proof can unlock partial redraw readiness.

**Acceptance Scenarios**:

1. **Given** a capable host that preserves unchanged regions, **When** the live proof runs, **Then** the result is passed and includes evidence that unchanged regions remained valid while the damaged region changed.
2. **Given** a host that clears, corrupts, or cannot observe unchanged regions, **When** the live proof runs, **Then** the result is failed or environment-limited and partial redraw remains unavailable.
3. **Given** proof evidence is missing, stale, or from a different host profile, **When** readiness is evaluated, **Then** the evidence is rejected and full-frame redraw remains the safe path.

---

### User Story 2 - Redraw Only Damaged Areas With Safe Fallback (Priority: P1)

Framework users receive the same visual result as full-frame redraw while the compositor redraws only changed areas on hosts that passed the live proof. When the proof is unavailable or the frame requires a full update, the system falls back with explicit diagnostics.

**Why this priority**: This is the first visible P7 payoff. The feature must preserve pixels exactly before it can claim reduced redraw work.

**Independent Test**: Render a representative corpus with full-frame redraw and with proof-gated damage-scoped redraw. Compare every accepted frame and verify fallback reasons for unsupported, stale-proof, full-frame invalidation, and disabled cases.

**Acceptance Scenarios**:

1. **Given** a localized update after a passed live proof, **When** damage-scoped redraw runs, **Then** only the required damaged area is redrawn and the final frame matches the full-frame oracle.
2. **Given** overlapping damaged areas, **When** the frame redraws, **Then** the compositor treats the overlap once and reports the combined redraw area.
3. **Given** a resize, theme change, host mismatch, stale proof, or disabled compositor mode, **When** the frame redraws, **Then** the system uses full-frame redraw and reports the fallback reason.
4. **Given** a damage-scoped frame completes, **When** the next frame starts, **Then** previous scoping state cannot leak into the new frame.

---

### User Story 3 - Reuse Stable Moving Content Safely (Priority: P2)

Framework maintainers can distinguish visual content from placement so stable content that only moves can be reused without serving stale pixels, while changing or churning content is refreshed or demoted.

**Why this priority**: The report identifies content-versus-placement tracking as a high-leverage compositor efficiency requirement after proof-gated redraw.

**Independent Test**: Run stable, moving-only, scrolling, content-changing, and churning scenarios. The feature passes when reuse decisions reduce repeated visual work, preserve frame parity, and reject stale content.

**Acceptance Scenarios**:

1. **Given** content remains visually stable while its placement changes, **When** the frame redraws, **Then** the compositor reuses the content at the new placement and the final frame matches the full-frame oracle.
2. **Given** content changes while placement remains similar, **When** reuse is evaluated, **Then** stale content is rejected and fresh output is produced.
3. **Given** a boundary churns or repeatedly fails to save work, **When** promotion is evaluated, **Then** the boundary is demoted or left unpromoted with a recorded reason.
4. **Given** a movement update exposes both old and new regions, **When** damage is computed, **Then** both affected regions are represented so no stale pixels remain.

---

### User Story 4 - Manage Snapshot Reuse With Bounded Lifecycle (Priority: P2)

Framework maintainers can use a bounded snapshot reuse tier for expensive stable content, including creation, reuse, refresh, eviction, disposal, fallback, and composition evidence.

**Why this priority**: Snapshot reuse can produce meaningful wins for expensive stable visuals, but it can also waste memory or serve stale frames unless lifecycle and budget behavior are visible.

**Independent Test**: Run expensive-stable, simple, churning, over-budget, invalid-resource, and unsupported-host scenarios. The feature passes when snapshots are used only where beneficial, stay within budget, and fall back safely.

**Acceptance Scenarios**:

1. **Given** expensive stable content with measured reuse value, **When** snapshot reuse is enabled, **Then** the compositor composes the snapshot into the frame and records a benefit.
2. **Given** a snapshot reaches budget, becomes invalid, or its content changes, **When** the frame redraws, **Then** the snapshot is refreshed, evicted, or bypassed without stale output.
3. **Given** simple or churning content, **When** snapshot eligibility is evaluated, **Then** the snapshot tier is rejected or demoted before sustained overhead is accepted.
4. **Given** the host cannot support snapshot reuse, **When** rendering continues, **Then** lower tiers or full-frame redraw produce the same visible result and the limitation is disclosed.

---

### User Story 5 - Claim Real Timing Wins With Reviewable Evidence (Priority: P2)

Release reviewers can inspect one readiness package that ties live proof, parity, fallback, reuse decisions, snapshot lifecycle, and real timing probes to tier verdicts before P7 is reported as delivering performance value.

**Why this priority**: The previous feature intentionally made no shipped performance claim. This slice is complete only when the evidence proves both correctness and a real benefit.

**Independent Test**: Produce readiness evidence from capable, unsupported, beneficial, non-beneficial, failed-parity, and environment-limited runs. Reviewers can identify which tiers are ready, limited, rejected, or skipped without tracing raw logs.

**Acceptance Scenarios**:

1. **Given** a tier has matching live proof, frame parity, fallback behavior, resource lifecycle evidence, and timing benefit, **When** readiness is assembled, **Then** the tier can be marked ready.
2. **Given** a tier lacks live proof, fails parity, exceeds overhead limits, or only has environment-limited evidence, **When** readiness is assembled, **Then** the tier is limited or rejected and cannot be counted as a shipped benefit.
3. **Given** diagnostics, public metrics, or compatibility behavior change, **When** release evidence is reviewed, **Then** the impact and migration notes are present.

### Edge Cases

- The active host reports rendering capability but does not preserve untouched regions between presents.
- Live proof succeeds for one host profile but the active host profile changes before readiness is evaluated.
- Damage touches frame edges, overlaps itself, or covers the full frame.
- A frame-wide change occurs after several damage-scoped frames.
- Content moves and exposes both the old and new screen regions.
- Content changes while retaining similar bounds or placement.
- A promoted boundary becomes unstable after promotion.
- Snapshot resources hit budget, become invalid, or lose their measured benefit.
- A simple scene pays more compositor overhead than it saves.
- Timing probes run on an environment that cannot produce comparable evidence.
- Parity failure, host limitation, stale proof, and performance regression must be distinguishable in reports.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST produce a live preservation proof with a passed, failed, or environment-limited verdict for the active host profile.
- **FR-002**: Damage-scoped redraw MUST remain unavailable for readiness claims unless the live preservation proof has passed for the active host profile.
- **FR-003**: The system MUST reject missing, stale, synthetic-only, failed, environment-limited, or host-mismatched proof evidence when evaluating readiness.
- **FR-004**: When damage-scoped redraw is enabled, every accepted frame MUST visually match the full-frame redraw oracle for the representative corpus.
- **FR-005**: The system MUST use full-frame redraw when proof is unavailable, proof is invalid, compositor mode is disabled, or a frame-wide invalidation occurs.
- **FR-006**: The system MUST report fallback reasons in a form that maintainers and release reviewers can inspect.
- **FR-007**: The system MUST represent combined damage accurately, including overlapping damage, edge damage, movement old regions, movement new regions, and full-frame invalidations.
- **FR-008**: The system MUST prevent damage-scoping state from leaking between frames.
- **FR-009**: The system MUST distinguish visual content identity from placement identity for reusable boundaries.
- **FR-010**: Placement-only movement MUST reuse stable content only when the final frame remains identical to the full-frame oracle.
- **FR-011**: Any content change, resource change, host-profile change, or render-affecting input change MUST invalidate reuse that could produce stale output.
- **FR-012**: Churning or non-beneficial boundaries MUST be demoted or left unpromoted with a recorded reason.
- **FR-013**: Snapshot reuse MUST be bounded by an explicit resource budget and MUST support creation, reuse, refresh, eviction, disposal, and fallback evidence.
- **FR-014**: Snapshot reuse MUST NOT be reported as ready unless it preserves frame parity and shows a measured benefit on its target corpus.
- **FR-015**: Real timing probes MUST compare each enabled compositor tier against the appropriate lower tier or full-frame baseline on beneficial and non-beneficial scenarios.
- **FR-016**: A tier MUST NOT be claimed as a shipped performance benefit unless proof, parity, resource, fallback, and timing evidence all pass.
- **FR-017**: Readiness evidence MUST classify each tier as ready, limited, rejected, or skipped and explain the reason.
- **FR-018**: Any public metric, diagnostic, compatibility impact, or baseline change MUST be documented before readiness is claimed.
- **FR-019**: Repeated same-seed readiness runs MUST produce stable scenario identifiers, verdict categories, and artifact references.

### Key Entities

- **Host profile**: The observable identity of the rendering host for which proof and readiness evidence are valid.
- **Live preservation proof**: Evidence that unchanged frame regions remain valid while only a known damaged region changes.
- **Damage-scoped frame**: A frame rendered by updating only the regions affected by changes.
- **Full-frame redraw oracle**: The reference visual result produced without partial redraw or compositor reuse.
- **Fallback reason**: The recorded explanation for choosing full-frame redraw or a lower compositor tier.
- **Reusable boundary**: A visual region considered for repeated use across frames.
- **Content identity**: The render-affecting identity of reusable visual content.
- **Placement identity**: The position or transform of reusable content within a frame.
- **Snapshot resource**: A bounded reusable visual artifact for expensive stable content.
- **Timing probe**: A comparable measurement run that records benefit or overhead for a compositor tier.
- **Tier verdict**: The readiness classification for a compositor tier: ready, limited, rejected, or skipped.
- **Readiness package**: The reviewable evidence set that connects proof, parity, fallback, reuse, snapshot, timing, and compatibility decisions.

### Scope and Classification

- In scope: live preservation proof, proof-gated damage-scoped redraw, full-frame fallback diagnostics, content-versus-placement reuse, expanded corpus execution, bounded snapshot lifecycle and composition, real timing probes, tier verdicts, compatibility notes, and readiness evidence.
- Out of scope: intrinsic layout protocol work, browser backend production work, new text shaping behavior, widget interaction changes, design-system changes, and unrelated Controls parity cleanup.
- Expected classification: Tier 1, because the feature affects observable rendering behavior, diagnostics, readiness artifacts, compatibility notes, and performance claims.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of target host profiles produce a live preservation verdict of passed, failed, or environment-limited before partial redraw readiness is evaluated.
- **SC-002**: Damage-scoped redraw matches the full-frame redraw oracle for 100% of accepted frames in the representative corpus.
- **SC-003**: The representative corpus covers at least localized update, overlapping damage, edge damage, movement or scrolling, resize, theme or global visual change, expensive stable content, simple content, and churning content.
- **SC-004**: Placement-only reuse reduces repeated visual work by at least 30% on the agreed moving or scrolling corpus while preserving parity on every accepted frame.
- **SC-005**: Non-beneficial simple or churning scenarios remain within 5% of the lower-tier or full-frame baseline, or the responsible tier is rejected or demoted.
- **SC-006**: Snapshot reuse shows at least a 20% frame-cost improvement on the agreed expensive stable corpus before it is marked ready.
- **SC-007**: 100% of stale-proof, host-mismatch, stale-content, invalid-resource, over-budget, unsupported-host, failed-parity, and missing-timing cases fail safely with actionable diagnostics.
- **SC-008**: 100% of ready tier verdicts link to live proof, parity, fallback, resource, timing, and compatibility evidence.
- **SC-009**: Release reviewers can determine within 10 minutes which compositor tiers are ready, limited, rejected, or skipped and why.
- **SC-010**: Repeated same-seed readiness runs produce stable scenario identifiers, tier verdicts, parity outcomes, and artifact references.

## Assumptions

- Feature 147's deterministic proof contracts, corpus definitions, diagnostics, policy helpers, and readiness artifacts are the starting point for this follow-up slice.
- The next roadmap item is the remaining P7 compositor work, not P8 intrinsic layout, because the report lists P7 exit criteria that remain open after Feature 147.
- Environment-limited results are acceptable only when clearly labeled and excluded from ready tier verdicts.
- Planning may tune exact host profiles, corpora, and resource budgets, but must preserve the success criteria and safe-failure requirements in this specification.
- Any broader public surface changes will be justified during planning and recorded as compatibility impact.
