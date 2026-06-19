# Feature Specification: Structured Render/Layout Inspection Metadata

**Feature Branch**: `165-render-layout-inspection`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "Start the next item in `docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md`: structured render/layout inspection metadata for deterministic visual assertions."

## Context

The retrospective identifies that visual readiness still depends too heavily on screenshots and human review for issues that should be testable: text overflow, unintended overlap, clipping, visual ordering, section containment, and unpainted surfaces. Feature 164 made the screenshot evidence workflow reusable, but it explicitly left deterministic render/layout inspection metadata out of scope.

This feature adds a structured inspection capability for maintainers, sample owners, and generated-product reviewers. A reviewer should be able to inspect a rendered screen as data, assert layout and text-fit facts without reading rendering internals, and connect any findings back to the page, theme, size, and visual region being validated.

## Change Classification

**Tier 1 (testing and diagnostics contract)**. This feature is expected to add or change reusable inspection and validation behavior. It must identify any public compatibility impact during planning, preserve existing visual output unless an intentional change is documented, and provide evidence that inspection results are stable enough for automated validation.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Detect Text and Layout Defects Deterministically (Priority: P1)

A maintainer can run an inspection pass for a rendered screen and receive structured facts about nodes, regions, bounds, clipping, text extents, and visual ordering so obvious layout defects can fail tests before screenshot review.

**Why this priority**: The retrospective calls out text overlap, clipping, z-order, and section containment as high-value gaps that screenshots currently expose late and manually.

**Independent Test**: Use a small inspection fixture with known contained text, overflowing text, clipped text, overlapping regions, and correct regions. The inspection validation must report every seeded defect with the affected owner, visual region, and failure reason.

**Acceptance Scenarios**:

1. **Given** a rendered screen with named visual regions, **When** inspection metadata is produced, **Then** every inspectable node includes a stable identity, visual kind, final bounds, ownership, effective clipping state, and visual ordering.
2. **Given** text extends outside its owning region, **When** text-fit validation runs, **Then** the finding identifies the text owner, measured text area, containing region, and whether the issue is overflow, clipping, or both.
3. **Given** two ordinary content regions overlap without an explicit exception, **When** region validation runs, **Then** the finding identifies both regions and blocks accepted inspection readiness.
4. **Given** a dense sample page that previously required screenshot-only review, **When** the inspection validation runs, **Then** text containment and region separation can be checked without relying on pixel comparison.

---

### User Story 2 - Validate Paint Coverage and Intentional Exceptions (Priority: P1)

A reviewer can distinguish complete, intentional visual surfaces from missing paint, accidental clipping, and intentional overlap such as overlays or popups.

**Why this priority**: Visual readiness must not accept screens with unpainted root backgrounds or accidental overpaint, but some overlays and clipped areas are valid when explicitly owned and classified.

**Independent Test**: Run inspection validation over cases with a fully painted root, a missing background, an intentional overlay, an unclassified overlap, intentional clipping, and accidental clipping. The validator must accept only the intentional cases with explicit ownership and reason.

**Acceptance Scenarios**:

1. **Given** a screen root or required region has no intentional surface coverage, **When** paint coverage validation runs, **Then** the finding identifies the missing region and prevents accepted readiness.
2. **Given** an overlay, popup, or floating panel intentionally overlaps content, **When** overlap validation runs, **Then** the overlap is accepted only when it is tied to an explicit owner and reason.
3. **Given** clipping is expected for scrollable or bounded content, **When** clipping validation runs, **Then** the result distinguishes intentional clipping from accidental content loss.
4. **Given** visual ordering determines which content appears on top, **When** inspection evidence is reviewed, **Then** the ordering is visible enough for tests to assert critical foreground/background relationships.

---

### User Story 3 - Produce Reviewable Inspection Evidence (Priority: P2)

A reviewer can open an inspection summary and understand which pages, themes, sizes, regions, and findings were validated, without reading raw render data or private implementation details.

**Why this priority**: The inspection artifact should improve readiness review, not create another opaque dump that only framework authors can interpret.

**Independent Test**: Generate inspection evidence for a mixed set of passing and failing screens. Verify that the summary lists counts by status and severity, links each finding to its page/theme/size context, and preserves enough detail to reproduce or triage the issue.

**Acceptance Scenarios**:

1. **Given** multiple pages, themes, and target sizes are inspected, **When** the summary is generated, **Then** each result is grouped by page, theme, size, region, severity, and readiness status.
2. **Given** an inspection finding is produced, **When** a reviewer opens the summary, **Then** the reviewer can identify the affected visual area and expected containment or paint rule in under one minute.
3. **Given** screenshot evidence also exists for the same page/theme/size, **When** inspection evidence is summarized, **Then** the summary can reference the matching visual evidence without requiring screenshots to exist for inspection validation itself.
4. **Given** an inspection pass has no blocking findings, **When** readiness is summarized, **Then** the result explicitly states what was inspected and what remains outside deterministic inspection.

---

### User Story 4 - Adopt Inspection Incrementally Without Regressing Visual Output (Priority: P3)

A sample owner can add inspection validation to existing readiness workflows gradually, while preserving current screenshot behavior and clearly labeling unsupported or unavailable inspection facts.

**Why this priority**: The capability should improve confidence without forcing an all-at-once migration or silently changing rendering behavior.

**Independent Test**: Enable inspection on a representative sample page while leaving existing screenshot evidence unchanged. The sample should produce inspection evidence, keep prior visual readiness artifacts valid, and report unsupported inspection facts explicitly.

**Acceptance Scenarios**:

1. **Given** an existing visual-readiness workflow, **When** inspection validation is added, **Then** existing screenshot capture counts and reviewer classification rules remain valid unless a deliberate change is documented.
2. **Given** some node or visual fact cannot be inspected yet, **When** evidence is generated, **Then** the artifact marks the fact as unsupported or unavailable instead of omitting it silently.
3. **Given** inspection validation is adopted for only selected pages at first, **When** readiness is summarized, **Then** the summary distinguishes inspected, not-inspected, not-run, unsupported, environment-limited, accepted, and blocked scopes.
4. **Given** a future generated product wants the same validation, **When** it follows the inspection evidence contract, **Then** it can reuse the same expectations for containment, text fit, paint coverage, and intentional exceptions.

---

### Edge Cases

- Visual nodes without stable author-supplied names still need deterministic generated identities for repeatable evidence.
- Intentional overlaps, floating layers, overlays, popups, and clipped scroll regions must be accepted only with explicit classification.
- Empty, hidden, virtualized, or off-screen content must be distinguishable from missing inspection data.
- Transformed, scaled, or nested content must either report bounds in a coordinate space that reviewers can compare consistently or be explicitly marked unsupported or environment-limited until transform-aware bounds are available.
- Text with long words, wrapping, truncation, ellipsis, and multiline layout must report fit status without assuming one-line text.
- Theme or density changes may alter bounds, but repeated runs for the same inputs must remain stable.
- Environment-limited inspection must be labeled and must not be counted as accepted deterministic evidence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST produce a structured inspection artifact for each inspected rendered screen.
- **FR-002**: Each inspection artifact MUST include the page or screen identity, theme or presentation variant when applicable, target size, inspection timestamp or run identity, and overall inspection status.
- **FR-003**: Each inspectable visual node MUST include a stable identity, visual kind, final bounds, owning region or parent, child relationship, effective clipping state, and visual ordering.
- **FR-004**: Text-bearing nodes MUST include text area, owning region, baseline or vertical placement facts, fit status, clipping status, and any truncation or wrapping classification available to the inspector.
- **FR-005**: Surface and paint-bearing nodes MUST expose enough role and coverage information to determine whether required root and section backgrounds are intentionally painted.
- **FR-006**: The system MUST report unavailable, unsupported, hidden, virtualized, or not-applicable inspection facts explicitly.
- **FR-007**: The system MUST validate that required regions remain within their owning container and that ordinary content regions do not overlap unless an explicit exception exists.
- **FR-008**: The system MUST validate that text remains inside its intended owner or report the exact overflow, clipping, wrapping, or truncation classification.
- **FR-009**: The system MUST validate that required root and section surfaces have intentional paint coverage.
- **FR-010**: The system MUST allow intentional overlap and clipping exceptions only when they include an owner, reason, affected regions, and reviewable classification.
- **FR-011**: Inspection summaries MUST group findings by page or screen, presentation variant, target size, region, severity, and readiness status.
- **FR-012**: Inspection evidence MUST be available in both machine-readable and reviewer-readable forms.
- **FR-013**: Repeated inspection runs for unchanged inputs MUST keep node identities, ordering, finding identifiers, and summary grouping stable.
- **FR-014**: Blocking inspection findings MUST prevent accepted readiness for the inspected scope until fixed or explicitly reclassified as an intentional exception.
- **FR-015**: The feature MUST preserve existing screenshot evidence workflows and must not require screenshots for deterministic inspection validation.
- **FR-016**: Adoption MUST distinguish coverage state (inspected, not-inspected, not-run) from readiness state (accepted, blocked, unsupported, environment-limited) for partial inspection rollout.
- **FR-017**: Planning MUST identify any public compatibility impact, migration needs, and validation evidence required for the inspection contract.

### Scope Boundaries

- In scope: structured inspection artifacts, deterministic containment/text/paint/overlap validation, reviewer summaries, machine-readable evidence, intentional exception classification, and incremental adoption by samples or generated products.
- Out of scope: screenshot capture, contact sheet generation, reviewer defect workflow replacement, input-to-present latency diagnostics, scheduler changes, rendering performance optimization, and broad visual design fixes discovered by inspection.

### Key Entities

- **Inspection Artifact**: The structured evidence for one inspected screen, including context, visual nodes, findings, unsupported facts, and readiness status.
- **Visual Node**: A rendered visual element or grouping with identity, kind, bounds, ownership, clipping, ordering, paint role, and child relationships.
- **Text Run Inspection**: The measured text facts for a node, including text area, placement, fit status, clipping, wrapping, truncation, and owning region.
- **Region Boundary**: A named area such as a root, shell region, content region, overlay area, or feedback/status region used for containment and overlap validation.
- **Inspection Finding**: An accepted result, warning, blocking failure, unsupported fact, or environment-limited fact tied to a rule, severity, visual context, and affected nodes or regions.
- **Intentional Exception**: A reviewed classification that permits a specific overlap, clipping, or unavailable fact because it is expected behavior.
- **Inspection Summary**: A human-readable and machine-readable rollup of inspected scope, finding counts, readiness status, caveats, and links to related evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A curated defect corpus covering text overflow, accidental clipping, unclassified overlap, missing paint coverage, and incorrect visual ordering is detected with 100% recall and no more than one false positive per defect class.
- **SC-002**: At least 90% of current screenshot-reviewed layout defect classes from the retrospective can be asserted through inspection metadata without pixel comparison.
- **SC-003**: A reviewer can locate the page, visual region, and failing rule for each blocking inspection finding in under one minute using the generated summary.
- **SC-004**: Repeated runs over unchanged representative screens produce stable node identities and finding identifiers for 100% of inspected nodes that are not explicitly dynamic.
- **SC-005**: Inspection validation for the representative sample coverage completes in under two minutes on a maintainer workstation and reports any skipped or unsupported scope explicitly.
- **SC-006**: Existing screenshot readiness evidence for adopted samples remains valid, with no unexplained change in required screenshot counts or reviewer-classification requirements.
- **SC-007**: Readiness summaries clearly distinguish accepted, blocked, unsupported, environment-limited, not-inspected, and not-run inspection states for 100% of reported scopes.

### Measurement Definitions

- **Current screenshot-reviewed layout defect classes** for SC-002 are the retrospective classes carried into this feature: text overflow or fit failure, accidental clipping, unclassified overlap, incorrect foreground/background visual ordering, region containment failure, and missing root or section paint coverage.
- **Representative sample coverage** for SC-005 is the initial bounded sample or generated-product screen set named in readiness evidence. It must include at least one root surface, one section or content region, one text-bearing node, one overlap or clipping case, one unsupported or environment-limited fact when such a fact exists, and the related screenshot-readiness evidence link when available.
- **Two-minute validation evidence** for SC-005 is measured as elapsed wall-clock time for the documented representative inspection validation command and recorded with the command output.

## Assumptions

- The "next item" is interpreted as the next distinct retrospective feature after package-feed validation lanes and shared visual-readiness tooling: structured render/layout inspection metadata.
- Maintainers, sample owners, generated-product reviewers, and test authors are the primary users.
- Inspection complements screenshot evidence and human review; it does not replace reviewer judgment for subjective design quality.
- Initial adoption may focus on representative samples before expanding to every generated product.
- Intentional overlays, clipped scroll regions, and floating layers are valid only when explicitly classified.
- Responsiveness diagnostics and input/render scheduler changes are separate follow-up work.
