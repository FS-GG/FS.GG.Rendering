# Feature Specification: Compositor Damage Redraw

**Feature Branch**: `147-compositor-damage-redraw`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The referenced radical rendering architecture report states that P0 through P6 are implemented and landed through Feature 146, and that P7 compositor work remains unimplemented. This specification covers P7: proving that partial redraw is safe, then turning the existing damage and replay foundations into a compositor that can repaint only changed regions, promote stable content, reuse moving content without re-recording it, and use a higher-cost visual reuse tier only when evidence shows it is worth the cost.

The key risk is correctness, not ambition. Partial redraw can corrupt the frame if the presentation path does not preserve untouched pixels between frames. This feature therefore starts with a present-path proof. Damage-scissored redraw and every promotion tier remain disabled unless their parity and probe evidence is available.

This is expected to be a Tier 1 feature because it changes observable rendering behavior, performance evidence, frame metrics, and readiness contracts. If planning proves there is no public surface or observable behavior change, the plan may narrow the classification, but it must document why the Tier 1 default no longer applies.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Prove Partial Redraw Is Safe (Priority: P1)

Framework maintainers can run a present-path proof that determines whether the current rendering host preserves untouched frame regions between presents. The system reports a clear pass, fail, or environment-limited result before any damage-scissored redraw is accepted.

**Why this priority**: The report identifies this as the first compositor deliverable. Without proof that untouched pixels survive between frames, clipping redraw to the damage region can leave blank or stale regions and corrupt output.

**Independent Test**: Run the present-path proof on a capable host and on an unsupported or fresh-clearing host. The proof passes only when untouched regions remain valid across a controlled frame sequence; otherwise compositor scissoring stays disabled with an actionable reason.

**Acceptance Scenarios**:

1. **Given** a host that preserves untouched frame regions, **When** the present-path proof runs, **Then** it records a passed verdict with evidence that unchanged regions remain visually valid.
2. **Given** a host that clears or cannot prove preservation between frames, **When** the proof runs, **Then** it records a failed or environment-limited verdict and prevents damage-scissored redraw from being claimed as ready.
3. **Given** the proof result is missing, stale, or from a different host profile, **When** compositor readiness is evaluated, **Then** partial redraw is treated as not ready.

---

### User Story 2 - Repaint Only Damaged Regions Without Changing Pixels (Priority: P1)

Maintainers can enable damage-scissored frame redraw after the present-path proof passes. Changed frame regions are repainted, untouched regions are preserved, and the final frame is visually identical to the full-redraw oracle.

**Why this priority**: Damage-scissored redraw is the first visible compositor payoff. It must reduce repaint work without changing the user's pixels.

**Independent Test**: Render a representative corpus with full redraw and with damage-scissored redraw enabled, including idle, localized change, scrolling, resize, theme switch, and overlapping damage cases. Compare the final visual output and diagnostics for every frame.

**Acceptance Scenarios**:

1. **Given** a localized content change after a successful present-path proof, **When** damage-scissored redraw runs, **Then** only the damage union is repainted and the final frame matches the full-redraw oracle.
2. **Given** overlapping damage regions, **When** the frame redraws, **Then** the repainted area is the union of the regions rather than the sum of overlapping areas.
3. **Given** a frame-wide invalidation such as resize or theme change, **When** the frame redraws, **Then** the compositor repaints the full affected area instead of preserving stale pixels.
4. **Given** damage-scissored redraw is disabled or unsupported, **When** the same scene renders, **Then** the frame falls back to the full-redraw path with the same visible result.

---

### User Story 3 - Reuse Stable and Moving Content Safely (Priority: P2)

Maintainers can let the compositor promote stable subtrees for reuse and distinguish content changes from placement-only movement. Stable content is reused, moving content is re-positioned without being rebuilt, and churning content is demoted before it becomes a cost.

**Why this priority**: The report calls out promotion and content/placement key separation as the highest-leverage compositor efficiency work after scissoring. The value is real only if reuse never serves stale content and churning areas are not over-promoted.

**Independent Test**: Run stable, scrolling, moving-only, content-changing, and churning frame sequences. Verify reuse decisions, demotions, output parity, and work-reduction metrics against the full-redraw oracle.

**Acceptance Scenarios**:

1. **Given** a subtree that remains visually stable across the configured observation window, **When** promotion is evaluated, **Then** it becomes eligible for reuse only after evidence shows it is stable and large enough to matter.
2. **Given** promoted content moves without changing visually, **When** the frame redraws, **Then** the compositor reuses the content at the new placement and the output matches the full-redraw oracle.
3. **Given** promoted content changes, **When** the frame redraws, **Then** the old content is rejected and a fresh visual result is produced.
4. **Given** a boundary churns every frame or fails to save work, **When** promotion is evaluated, **Then** it is demoted or left unpromoted and the decision is recorded.

---

### User Story 4 - Use Snapshot Reuse Only When It Pays Off (Priority: P3)

Maintainers can enable a higher-cost snapshot reuse tier for expensive stable content. The tier is selected only when probe evidence shows a net win, remains bounded, and falls back safely when it is unsupported or loses value.

**Why this priority**: Snapshot reuse can improve expensive stable scenes, but overuse wastes memory and can slow simple scenes. It must be evidence-gated and bounded.

**Independent Test**: Run the agreed expensive-scene corpus, simple-scene corpus, and churn corpus with the snapshot tier enabled and disabled. Verify output parity, resource bounds, demotion behavior, and measured net benefit.

**Acceptance Scenarios**:

1. **Given** a stable expensive subtree with measured reuse benefit, **When** the snapshot tier is enabled, **Then** the compositor reuses the snapshot and records a net performance win.
2. **Given** a simple or churning subtree, **When** the snapshot tier is evaluated, **Then** it is not promoted or is demoted before it causes a sustained slowdown.
3. **Given** snapshot resources reach the configured budget or become invalid, **When** the frame redraws, **Then** the compositor releases or refreshes them without stale output.
4. **Given** the host cannot support snapshot reuse, **When** the feature runs, **Then** it reports the limitation and uses lower tiers or full redraw without accepting misleading evidence.

---

### User Story 5 - Review Compositor Readiness Evidence (Priority: P2)

Release reviewers can inspect one readiness package that connects present-path proof, parity results, promotion decisions, work-reduction counters, performance probes, limitations, and compatibility impact.

**Why this priority**: P7 is a performance feature with correctness risk. Reviewers need concise evidence that the compositor is safe and actually beneficial before it is treated as shipped.

**Independent Test**: Review the readiness package after representative runs and confirm it names the corpus, host capability result, pass/fail status, parity verdicts, performance deltas, limitations, and any public or observable behavior impact.

**Acceptance Scenarios**:

1. **Given** all compositor tiers are enabled on a capable host, **When** readiness is reviewed, **Then** the evidence states which tiers passed, which were skipped, and why.
2. **Given** a tier fails parity or performance probes, **When** readiness is reviewed, **Then** the tier is marked not ready and cannot be counted as a shipped benefit.
3. **Given** public metrics, diagnostics, or observable rendering behavior change, **When** release notes are prepared, **Then** the compatibility impact and migration guidance are available.

### Edge Cases

- The host reports rendering capability but clears the frame between presents.
- Damage regions overlap, touch frame edges, or cover the full frame.
- The frame size, scale, theme, resource availability, or text provider changes between frames.
- A promoted boundary moves while its content remains stable.
- A promoted boundary changes content but keeps the same placement.
- A stable boundary becomes unstable after promotion and must be demoted.
- Snapshot resources hit the resource budget or become invalid during a run.
- A simple scene pays more overhead for compositor bookkeeping than it saves.
- A parity failure could be caused by presentation, damage calculation, promotion, snapshot reuse, resource lifecycle, or evidence bookkeeping; the report must distinguish these categories.
- The feature is attempted on a host where real presentation proof or snapshot evidence cannot be produced.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a present-path proof that determines whether untouched frame regions remain valid between presents on the current host profile.
- **FR-002**: Damage-scissored redraw MUST remain disabled or marked not ready unless the present-path proof has passed for the current host profile.
- **FR-003**: When damage-scissored redraw is enabled, the final frame MUST match the full-redraw oracle for every representative frame in the agreed corpus.
- **FR-004**: The system MUST repaint the union of damaged regions, count overlaps once, and repaint the full affected area for frame-wide invalidations.
- **FR-005**: The system MUST fall back to full redraw when partial redraw is unsupported, disabled, inconclusive, or unsafe.
- **FR-006**: The compositor MUST promote only boundaries with observed stability and enough expected work reduction to justify reuse.
- **FR-007**: The compositor MUST demote or avoid promotion for boundaries that churn, fail parity, exceed budgets, or produce no measured benefit.
- **FR-008**: The compositor MUST distinguish content identity from placement so placement-only movement can reuse stable content while content changes force fresh output.
- **FR-009**: Every reuse tier MUST preserve visual parity with the full-redraw oracle and MUST reject stale content when any render-affecting input changes.
- **FR-010**: The snapshot reuse tier MUST be bounded by an explicit resource budget and MUST release, refresh, or demote resources deterministically.
- **FR-011**: Performance probes MUST compare compositor tiers against the full-redraw or lower-tier baseline on stable, moving, expensive, simple, and churning scenarios before any benefit is claimed.
- **FR-012**: A tier MUST NOT be reported as a shipped performance benefit unless it shows a net win on its target corpus and does not exceed the accepted overhead limit on non-beneficial scenes.
- **FR-013**: Compositor diagnostics MUST record host proof status, damage area, fallback reason, promotion and demotion decisions, reuse hits and misses, snapshot resource status, parity verdict, and performance delta for review.
- **FR-014**: Environment-limited, synthetic, stale, or incomplete evidence MUST NOT be accepted as proof that a compositor tier is ready.
- **FR-015**: Any public metrics, compatibility impact, baseline update, or observable rendering behavior change MUST be recorded with migration guidance and release notes before readiness is claimed.
- **FR-016**: The feature MUST preserve deterministic same-seed results for all headless evidence and stable scenario identifiers across repeated runs.

### Key Entities

- **Present-path proof**: The capability result showing whether the host preserves untouched frame regions between presents and whether partial redraw may be enabled.
- **Damage region set**: The frame regions that must be repainted for a given update, including their union area and full-frame invalidation status.
- **Full-redraw oracle**: The reference frame produced without compositor reuse or scissoring, used to validate visual parity.
- **Compositor boundary**: A reusable visual region evaluated for promotion, demotion, movement reuse, and snapshot reuse.
- **Promotion decision**: The recorded decision to promote, keep, demote, or reject a boundary based on stability, expected benefit, parity, and budget.
- **Content identity**: The render-affecting identity of a boundary's visual content, distinct from its placement in the frame.
- **Placement identity**: The position or transform applied to stable content without changing the content itself.
- **Snapshot resource**: A bounded reusable visual artifact for expensive stable content, with lifecycle and budget evidence.
- **Performance probe**: A representative measurement run that compares enabled compositor tiers against the appropriate baseline and records benefit or overhead.
- **Compositor readiness package**: The reviewable evidence connecting proof status, parity, performance, limitations, and compatibility impact.

### Scope and Classification

- This feature is the P7 Compositor item from the radical rendering roadmap.
- In scope: present-path proof, damage-scissored redraw, promotion and demotion heuristics, content/placement identity separation, bounded snapshot reuse, parity oracles, performance probes, diagnostics, readiness evidence, and compatibility notes.
- Out of scope: new layout semantics, intrinsic layout protocol, browser backend production work, portable scene protocol changes unrelated to compositor evidence, text shaping expansion, widget behavior changes, and product showcase redesign.
- Expected classification: Tier 1, because the feature may alter observable rendering behavior, public metrics or diagnostics, readiness artifacts, and performance claims.
- Public surface changes are allowed only when planning shows they are needed for observability or compatibility; otherwise compositor mechanics should remain behind existing rendering and evidence contracts.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of target host profiles produce a present-path verdict of passed, failed, or environment-limited before damage-scissored redraw is considered ready.
- **SC-002**: Damage-scissored redraw matches the full-redraw oracle for 100% of frames in the agreed representative corpus.
- **SC-003**: The damage corpus covers at least idle, localized update, overlapping damage, scrolling or movement, resize, theme change, and full-frame invalidation scenarios.
- **SC-004**: Promotion and placement-only reuse reduce repeated visual work by at least 30% on the agreed stable or moving corpus while preserving visual parity on every frame.
- **SC-005**: Churning and simple-scene scenarios remain within 5% of the lower-tier or full-redraw baseline, or the responsible tier is automatically demoted and not reported as a benefit.
- **SC-006**: Snapshot reuse shows at least a 20% frame-cost improvement on the agreed expensive stable corpus before it is reported as ready.
- **SC-007**: 100% of stale-content, invalid-resource, over-budget, unsupported-host, and missing-proof negative cases fail safely with actionable diagnostics and no accepted misleading artifact.
- **SC-008**: Repeated same-seed readiness runs produce stable scenario identifiers, tier verdicts, parity outcomes, and compatibility status.
- **SC-009**: Release reviewers can determine within 10 minutes which compositor tiers are ready, limited, or rejected and why.

## Assumptions

- P0 through P6 of the referenced radical rendering roadmap are available as the starting point, including the modifier/layer foundation, retained renderer unification, shaped text evidence, overlay visual proof, and render-anywhere protocol.
- The user intends the next roadmap phase, P7 Compositor, rather than the report's separate note about unrelated Controls transient-metadata parity failures.
- The representative corpora may reuse existing rendering harness, showcase, cache, replay, damage, overlay, and render-anywhere scenarios when they cover the required cases.
- Environment-limited results are acceptable only when clearly labeled and excluded from accepted readiness evidence.
- Planning will decide the concrete thresholds, host profiles, resource budgets, and public metric shape, but any changes must preserve the measurable outcomes in this specification.
