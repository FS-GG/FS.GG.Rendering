# Feature Specification: Complete P8 Layout Acceptance

**Feature Branch**: `151-complete-p8-layout`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers the next open item from the referenced radical rendering architecture report: closing the remaining P8/R3b radical layout acceptance bar after the first intrinsic-layout slice. Feature 150 established the public layout, intrinsic, ScrollViewer, metrics, and readiness surfaces; this feature turns that focused slice into accepted P8 readiness by expanding the representative corpus, proving measured and intrinsic reuse behavior, running broad retained/default layout regressions, and publishing full solution and package evidence.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Prove the Representative Layout Corpus (Priority: P1)

Framework maintainers need the intrinsic layout protocol to be exercised across the full representative corpus named by the roadmap and Feature 150 readiness notes, so P8 acceptance is based on breadth rather than focused smoke coverage.

**Why this priority**: The report explicitly keeps P8 open for the full representative layout and ScrollViewer corpus. Without this corpus, cache and regression claims are too narrow to close the phase.

**Independent Test**: Can be tested by running the accepted representative corpus and verifying every case records expected bounds, placements, scroll extents, diagnostics, and readiness status.

**Acceptance Scenarios**:

1. **Given** a finite constrained root, measured leaves, intrinsic-capable content, and dynamic content cases, **When** the corpus is evaluated, **Then** each case records deterministic bounds, placements, and diagnostics.
2. **Given** empty, exact-fit, overflowing, nested, clipped, layered, and dynamic scroll content cases, **When** scroll ranges are evaluated, **Then** each case records accepted viewport and content extents.
3. **Given** invalid constraints, contradictory intrinsic responses, or unavailable intrinsic data, **When** the corpus is evaluated, **Then** every case records an explicit diagnostic or fallback verdict without accepting misleading layout results.

---

### User Story 2 - Accept Measured and Intrinsic Cache Reuse (Priority: P1)

Maintainers need measured and intrinsic layout reuse to be accepted only when all relevant layout inputs still match, so incremental layout remains equivalent to full layout while avoiding unnecessary repeated work.

**Why this priority**: The report names measured and intrinsic cache reuse as a remaining P8 acceptance gap. This is the main behavioral risk after the protocol surface exists.

**Independent Test**: Can be tested by evaluating cold, warm, and changed-input layout runs over the corpus and checking accepted reuse, required misses, stale rejection, and full/incremental equivalence.

**Acceptance Scenarios**:

1. **Given** two equivalent layout evaluations, **When** the second evaluation runs, **Then** measured and intrinsic results are reused with recorded evidence.
2. **Given** constraints, content identity, layout-affecting inputs, child order, visibility, or intrinsic dependencies change, **When** layout is reevaluated, **Then** stale measured and intrinsic results are rejected.
3. **Given** incremental layout accepts a cache hit, **When** the matching full layout result is compared, **Then** bounds, placements, scroll extents, diagnostics, and result identities remain equivalent.

---

### User Story 3 - Run Broad Regression Evidence (Priority: P2)

Release reviewers need evidence that completing P8 does not regress retained rendering, default layout, overlay state, render-anywhere, text shaping, compositor readiness, disabled-cache parity, or package compatibility outside documented layout changes.

**Why this priority**: Feature 150 readiness is focused. The report keeps broad retained/default layout regression evidence open before final P8 acceptance can be claimed.

**Independent Test**: Can be tested by running the agreed regression set and confirming every accepted result is linked from the P8 readiness summary, with failures or environment limits recorded as blockers or limitations.

**Acceptance Scenarios**:

1. **Given** existing retained rendering and default layout parity guarantees, **When** the broad regression suite runs, **Then** accepted results show no undocumented layout, rendering, or diagnostic deltas.
2. **Given** prior overlay, render-anywhere, text-shaping, and compositor-readiness evidence, **When** focused compatibility checks run, **Then** each check either passes or records an explicit environment-limited status without claiming unsupported behavior.
3. **Given** a regression failure, **When** readiness is evaluated, **Then** final P8 acceptance remains blocked until the failure is fixed or explicitly scoped out with reviewer-visible rationale.

---

### User Story 4 - Publish Final P8 Readiness (Priority: P3)

Package consumers and maintainers need one reviewable readiness package that states whether P8 is accepted, what evidence supports that status, what compatibility impact exists, and what work remains outside the phase.

**Why this priority**: P8 is a Tier 1 layout behavior change. Consumers should not have to reconstruct readiness from scattered logs or infer whether Feature 150 limitations are closed.

**Independent Test**: Can be tested by reviewing the readiness package in under 10 minutes and confirming it links accepted corpus, cache, regression, full solution, package, compatibility, and limitation evidence.

**Acceptance Scenarios**:

1. **Given** the final P8 readiness summary, **When** a reviewer opens it, **Then** they can identify accepted evidence, blockers, limitations, compatibility impact, and package readiness from one entry point.
2. **Given** full solution and package validation results, **When** readiness is summarized, **Then** the evidence states exact accepted, failed, skipped, or environment-limited verdicts.
3. **Given** future layout work remains outside P8, **When** readiness is published, **Then** those items are listed as follow-up scope rather than hidden acceptance gaps.

### Edge Cases

- Empty containers, single-child containers, deep nesting, and reordered children.
- Zero-sized, very small, very large, finite, unbounded, invalid, and contradictory constraints.
- Content that fits exactly, barely overflows, substantially overflows, or changes natural size after initial layout.
- Nested scrollable content and scrollable content inside clipped or layered parents.
- Text, image, and composed controls whose natural size depends on content.
- Layout-affecting attribute, visibility, child-order, and intrinsic-dependency changes after prior cache entries exist.
- Repeated equivalent evaluations that should accept reuse without changing observable results.
- Stale measured or intrinsic entries that must be rejected before they affect layout.
- Environment-limited visual or compositor checks that must not be counted as accepted evidence.
- Pre-existing unrelated test failures that must be distinguished from P8 regressions before readiness is claimed.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST close the remaining P8/R3b acceptance gaps named by the referenced report: full representative corpus, measured and intrinsic cache reuse, broad retained/default layout regression evidence, and full solution/package validation.
- **FR-002**: The feature MUST be classified as a Tier 1 contracted change because it validates and may refine observable layout behavior and package readiness.
- **FR-003**: The representative layout corpus MUST include constrained roots, measured leaves, intrinsic-capable content, invalid constraints, dynamic content, layout-affecting input changes, child insertion/removal/reorder, visibility changes, and diagnostic cases.
- **FR-004**: The representative ScrollViewer corpus MUST include empty, smaller-than-viewport, exact-fit, barely overflowing, substantially overflowing, nested scroll, clipped parent, layered parent, text/content natural size, dynamic content change, and invalid intrinsic fallback cases.
- **FR-005**: Every corpus case MUST record expected bounds, placements, scroll extents where relevant, diagnostics, and an accepted/failed/skipped/environment-limited verdict.
- **FR-006**: Repeated equivalent layout evaluations MUST record deterministic result identities and accepted measured/intrinsic reuse where reuse is expected.
- **FR-007**: Layout cache evidence MUST reject stale measured or intrinsic entries when constraints, viewport, content identity, measurement behavior, layout-affecting inputs, visibility, child order, intrinsic dependencies, or cache revision change.
- **FR-008**: Full layout, cold incremental layout, warm incremental layout, and changed-input incremental layout MUST remain equivalent for accepted corpus cases.
- **FR-009**: Duplicate measurement during a single normal layout pass MUST be detected and either prevented or reported with an explicit diagnostic that does not allow misleading acceptance.
- **FR-010**: Diagnostics MUST explain invalid constraints, rejected intrinsic results, unsupported intrinsic queries, stale reuse, fallback bounds, and environment-limited evidence in reviewer-visible language.
- **FR-011**: Broad regression evidence MUST cover retained rendering parity, default layout compatibility, disabled-cache parity, overlay behavior, render-anywhere packaging, text-shaping evidence, compositor-readiness evidence, public surface compatibility, and package validation.
- **FR-012**: Any regression failure or unrelated pre-existing failure encountered during broad validation MUST be classified before final readiness is accepted.
- **FR-013**: Final readiness evidence MUST include a single summary linking corpus, cache/reuse, full/incremental parity, ScrollViewer, compatibility, regression, full solution, package, and limitation evidence.
- **FR-014**: Final readiness MUST state whether P8 is accepted, incomplete, failed, skipped, or environment-limited, and MUST not claim accepted P8 readiness while any required evidence is missing or failed.
- **FR-015**: Public documentation and compatibility notes MUST explain the final P8 status, consumer-visible behavior, diagnostics, migration impact, and follow-up scope.
- **FR-016**: The feature MUST NOT introduce a new general-purpose constraint solver, compositor partial-redraw acceptance, browser rendering backend, overlay interaction behavior, or text-shaping behavior unless recorded as separate follow-up work.

### Key Entities *(include if feature involves data)*

- **Representative Layout Corpus**: The accepted set of layout and ScrollViewer scenarios used to decide whether P8 behavior is broad enough for readiness.
- **Measured Reuse Evidence**: Records showing when measured layout results are reused, missed, or rejected and why.
- **Intrinsic Reuse Evidence**: Records showing when intrinsic size results are reused, missed, or rejected and why.
- **Full/Incremental Parity Result**: The comparison of full, cold incremental, warm incremental, and changed-input layout outcomes for a corpus case.
- **Regression Evidence Set**: The broad validation results for prior rendering, layout, overlay, protocol, text, compositor, public surface, and package guarantees.
- **P8 Readiness Summary**: The single review entry point that aggregates evidence, status, compatibility impact, limitations, and follow-up scope.
- **Layout Compatibility Ledger**: The record of intentional and unintentional layout behavior differences visible to consumers.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of the representative layout corpus records accepted bounds, placements, diagnostics, and readiness verdicts, with no missing expected result fields.
- **SC-002**: ScrollViewer validation covers at least 11 named content cases and accepts expected viewport, content extent, and max offset outcomes for every non-skipped case.
- **SC-003**: Cache and invalidation evidence covers at least 8 layout-affecting change categories and shows zero accepted stale measured or intrinsic results.
- **SC-004**: 100% of accepted corpus cases produce equivalent observable results between full, cold incremental, warm incremental, and changed-input incremental evaluations where those modes apply.
- **SC-005**: 100% of invalid, contradictory, unsupported, stale, or fallback cases produce actionable diagnostics and no accepted misleading layout result.
- **SC-006**: Broad regression validation records accepted or explicitly limited verdicts for at least 8 prior guarantee areas: retained rendering, default layout, disabled-cache parity, overlay, render-anywhere, text shaping, compositor readiness, and package/public surface compatibility.
- **SC-007**: Full solution build/test and local package validation produce recorded accepted verdicts before P8 readiness is marked accepted.
- **SC-008**: Final P8 readiness can be reviewed from one summary in under 10 minutes, including links or paths to corpus, cache/reuse, parity, ScrollViewer, compatibility, regression, full solution, package, and limitation evidence.
- **SC-009**: Public compatibility notes list 100% of intentional consumer-visible layout or diagnostic changes and identify zero undocumented public contract deltas.

## Assumptions

- "Next item" means the remaining P8/R3b acceptance work because the referenced report states P0-P6 are landed, P7 is implemented with environment-limited live acceptance, and Feature 150 completed only the first P8 slice.
- Feature 150's public layout, intrinsic, ScrollViewer, metrics, Testing, and readiness surfaces are the starting point; this feature completes acceptance rather than replacing that slice.
- The default general-purpose layout model remains in place; a relational layout container or solver is outside this feature.
- Environment-limited compositor proof from P7 does not block P8 layout acceptance, but P8 must not claim new compositor partial-redraw or performance acceptance.
- Broad validation may encounter unrelated pre-existing failures; those must be classified and recorded before final readiness status is decided.
- Exact task order, file names, public naming, and validation commands are planning details for the next phase.
