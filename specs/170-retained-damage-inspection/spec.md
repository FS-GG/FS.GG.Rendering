# Feature Specification: Retained Render Damage Inspection

**Feature Branch**: `170-retained-damage-inspection`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md"

**Resolved Item**: The next unstarted retrospective follow-up after the completed Feature 169 work is the remaining Feature 165 inspection expansion: add retained-render inspection emission, add damage inspection fields for dirty area and affected nodes, migrate a representative AntShowcase visual-shell assertion to structured inspection evidence, and replace the missing legacy validation target with a canonical repository validation entry point.

## Context

Feature 165 added the first structured visual inspection contract for deterministic layout, paint, text, and unsupported-fact validation. The retrospective still calls out a deeper gap: maintainers can inspect a bounded rendered screen, but they cannot yet see post-retained-render damage facts that explain whether a localized state change repainted or shifted too much of the scene.

This feature extends inspection readiness so reviewers can evaluate retained output and damage locality from structured evidence. It should make full-surface dirty regions, broad repaint sets, shifted nodes, missing damage facts, and legacy validation-command drift visible before screenshot review or manual diagnosis.

## Change Classification

**Tier 1 (testing and readiness evidence contract)**. This feature is expected to extend reusable inspection evidence and validation behavior. Planning must identify any public compatibility impact, migration needs, surface-baseline updates, and readiness evidence required for existing inspection consumers.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Inspect Final Retained Output (Priority: P1)

A framework maintainer can inspect the final retained-render state for a screen and receive stable, reviewable facts about the nodes and regions that were retained, reused, repainted, shifted, or unsupported.

**Why this priority**: The current inspection slice validates structured screen facts, but the retrospective identifies retained-render and damage information as the next missing layer for explaining broad repaint behavior.

**Independent Test**: Run a retained-render inspection fixture with unchanged nodes, changed nodes, shifted nodes, and unsupported damage facts. Verify the artifact reports each case with stable identities, status, and affected region context.

**Acceptance Scenarios**:

1. **Given** a screen is rendered through the retained path, **When** retained inspection evidence is produced, **Then** the artifact identifies the inspected screen, final visual regions, retained nodes, affected nodes, and unsupported facts.
2. **Given** a node remains visually unchanged between two retained frames, **When** damage inspection is summarized, **Then** the node is reported as reused or unaffected rather than silently counted as repainted.
3. **Given** a node changes position between frames, **When** retained inspection is validated, **Then** the shifted node is visible with its prior and current bounds.
4. **Given** retained damage data cannot be produced for a scope, **When** evidence is summarized, **Then** the scope is marked unsupported or not-inspected with a reviewer-readable reason.

---

### User Story 2 - Validate Damage Locality (Priority: P1)

A test author can assert that a localized interaction does not dirty or repaint unrelated regions without relying on pixel comparison or manual screenshot review.

**Why this priority**: The retrospective found localized interactions reporting broad dirty areas. Damage locality must become a deterministic readiness signal, not an ad hoc diagnostic.

**Independent Test**: Use a fixture with one localized change, one full-surface change, overlapping dirty areas, and a shifted layout case. Verify validation distinguishes localized damage from broad or unexplained damage.

**Acceptance Scenarios**:

1. **Given** one localized control changes state, **When** damage validation runs, **Then** the dirty area, repainted nodes, shifted nodes, and unaffected regions are reported separately.
2. **Given** dirty areas overlap, **When** the dirty area is summarized, **Then** the reported dirty region uses the actual union area rather than double-counting overlaps.
3. **Given** a localized interaction dirties the full visible surface, **When** validation evaluates locality, **Then** the result is flagged as a blocking or review-required finding with the affected scope.
4. **Given** a layout shift causes additional nodes to move, **When** validation summarizes the interaction, **Then** shifted nodes are reported separately from repainted nodes.

---

### User Story 3 - Move a Visual-Shell Assertion to Structured Evidence (Priority: P2)

An AntShowcase maintainer can replace at least one representative visual-shell assertion with structured inspection evidence while keeping existing screenshot readiness behavior intact.

**Why this priority**: The retrospective explicitly asks for sample adoption so the new inspection facts prove practical value in a real consumer workflow.

**Independent Test**: Run the selected AntShowcase visual-shell assertion before and after migration. Verify it uses structured inspection evidence, produces equivalent or stronger reviewer value, and does not change required screenshot counts.

**Acceptance Scenarios**:

1. **Given** AntShowcase has a visual-shell assertion that currently depends on indirect checks, **When** the assertion is migrated, **Then** it validates against structured inspection evidence for the selected page, theme, and size.
2. **Given** screenshot readiness is run for the same sample scope, **When** the structured assertion is added, **Then** screenshot capture counts and reviewer-classification requirements remain unchanged unless a deliberate change is documented.
3. **Given** the structured assertion fails, **When** a reviewer opens the evidence summary, **Then** the affected shell region and rule are identifiable without reading raw render data.

---

### User Story 4 - Run Canonical Validation (Priority: P2)

A contributor can run a documented repository validation entry point for retained inspection and damage evidence without depending on a missing or stale local wrapper.

**Why this priority**: The retrospective notes that a missing legacy validation target creates avoidable friction. The new evidence must be supported by a maintained command path.

**Independent Test**: Follow the documented validation command from a clean checkout. Verify it runs the retained inspection and sample adoption checks, records evidence, and fails clearly when a required prerequisite is missing.

**Acceptance Scenarios**:

1. **Given** a contributor follows the documented command, **When** validation runs, **Then** retained inspection, damage locality, and sample adoption checks execute through a canonical repository entry point.
2. **Given** a prerequisite is missing, **When** validation starts, **Then** the failure message identifies the missing prerequisite and the next action without referring to an unavailable wrapper.
3. **Given** validation completes, **When** readiness evidence is reviewed, **Then** the exact command, result status, and artifact locations are recorded.

### Edge Cases

- The first inspected frame has no prior frame for damage comparison.
- A frame has no visual change and should report empty damage rather than missing evidence.
- Dirty regions overlap, nest, or extend outside the visible surface.
- A node is repainted without moving, shifted without repainting, both shifted and repainted, or removed entirely.
- Virtualized, hidden, clipped, or off-screen nodes appear in the retained tree.
- A broad repaint is intentional for a root-level theme or size change and needs a scoped exception.
- Damage facts are unavailable for one subsystem while layout inspection facts are available.
- Repeated runs over unchanged inputs must not produce unstable node or finding identifiers.
- The selected AntShowcase assertion encounters environment-limited evidence.
- Validation artifacts from a prior run remain in the output directory.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST produce retained inspection evidence for each supported inspected screen after the retained render state is finalized; for this feature, supported screens/scopes are the retained-render representative fixtures, the selected AntShowcase `charts-statistical` full-shell assertion at preferred size in light and dark themes, and the retained-inspection validation-lane outputs.
- **FR-002**: Retained inspection evidence MUST include stable screen identity, run identity, target size, presentation variant when applicable, inspected scope, and overall readiness status.
- **FR-003**: Retained node evidence MUST distinguish retained or reused nodes, repainted nodes, shifted nodes, removed nodes, added nodes, unaffected nodes, and unsupported node facts.
- **FR-004**: Damage evidence MUST report dirty region union, visible dirty area, dirty area percentage, repainted node count, shifted node count, unaffected node count, and either affected visual regions or an explicit unsupported/not-inspected reason when affected regions cannot be produced.
- **FR-005**: Dirty area calculations MUST use unioned regions so overlapping dirty areas are not double-counted.
- **FR-006**: Damage evidence MUST distinguish empty damage, localized damage, broad damage, full-surface damage, unsupported damage, and not-inspected damage.
- **FR-007**: Validation MUST flag dirty regions outside the declared expected affected regions, dirty percentage above the transition's declared maximum dirty percentage, or full-surface damage for localized interactions unless the evidence includes a scoped intentional exception.
- **FR-008**: Validation MUST report shifted nodes separately from repainted nodes so layout movement and paint work can be reviewed independently.
- **FR-009**: Evidence summaries MUST group retained and damage findings by screen, presentation variant, target size, interaction or frame transition, visual region, severity, and readiness status.
- **FR-010**: Evidence MUST be available in both machine-readable and reviewer-readable forms.
- **FR-011**: Unsupported or unavailable retained/damage facts MUST be reported explicitly and MUST NOT be counted as accepted inspection evidence.
- **FR-012**: Repeated runs over unchanged representative inputs MUST keep retained node identities, finding identifiers, and summary grouping stable except for explicitly dynamic values.
- **FR-013**: At least one representative AntShowcase visual-shell assertion MUST consume structured inspection evidence for a real page, theme, and size.
- **FR-014**: Adding the structured AntShowcase assertion MUST preserve existing screenshot readiness counts and reviewer-classification requirements unless a deliberate change is documented.
- **FR-015**: The feature MUST provide a documented canonical validation entry point for retained inspection and damage evidence.
- **FR-016**: Validation evidence MUST record the command used, result status, elapsed time, artifact locations, and any environment-limited or skipped scope.
- **FR-017**: Planning MUST identify public compatibility impact, migration notes, and validation evidence for existing inspection consumers.

### Scope Boundaries

- In scope: retained inspection evidence, damage locality fields, dirty region union semantics, shifted/repainted/unaffected node reporting, retained/damage validation summaries, one AntShowcase structured assertion migration, and a maintained validation command path.
- Initial supported inspected screens/scopes: retained-render fixtures covering unchanged content, localized paint change, layout shift, added content, removed content, broad damage, and unsupported facts; the AntShowcase `charts-statistical` full-shell visual-shell assertion at preferred size in light and dark themes; and retained-inspection validation-lane output. Other screens/scopes remain unsupported or not-inspected until a future feature declares them supported.
- Out of scope: screenshot capture changes, contact-sheet generation, broad visual redesign, input scheduler changes, render-thread separation, performance optimization beyond exposing damage facts, and replacing the full visual-readiness workflow.

### Key Entities

- **Retained Inspection Artifact**: Structured evidence for the finalized retained-render state of one inspected screen or frame transition.
- **Retained Node Fact**: A stable fact about one visual node, including whether it was reused, repainted, shifted, added, removed, unaffected, or unsupported.
- **Damage Region**: The visible area reported as dirty or affected for a frame transition, including union area and relationship to visual regions.
- **Damage Locality Finding**: A validation result that reports localized, broad, full-surface, unsupported, or intentional damage behavior.
- **Shifted Node**: A node whose final bounds changed between inspected frames or transitions.
- **Repainted Node**: A node whose visual output needed repainting for the inspected transition.
- **Intentional Damage Exception**: A scoped reviewer-visible reason for broad damage that is expected for a particular transition, region, or screen state.
- **Validation Entry Point**: The documented command path contributors use to run retained inspection, damage locality, and sample adoption checks.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A representative retained inspection fixture reports retained, repainted, shifted, added, removed, unaffected, and unsupported node facts with 100% expected classification.
- **SC-002**: Dirty region union area is calculated correctly for 100% of fixture cases with overlapping, nested, empty, localized, and full-surface dirty regions.
- **SC-003**: A localized interaction fixture that dirties the full visible surface is flagged as blocked or review-required in 100% of runs.
- **SC-004**: Repeated runs over unchanged representative inputs produce stable node identities and finding identifiers for 100% of non-dynamic retained nodes.
- **SC-005**: At least one AntShowcase visual-shell assertion uses structured inspection evidence and keeps existing screenshot readiness counts unchanged.
- **SC-006**: A reviewer can identify dirty area percentage, repainted node count, shifted node count, and the affected visual regions from the summary in under 2 minutes.
- **SC-007**: The documented validation entry point completes the representative retained inspection and sample adoption checks in under 5 minutes on a maintainer workstation, excluding package restore time.
- **SC-008**: Validation evidence records command, status, elapsed time, and artifact locations for 100% of retained inspection readiness runs.

### Measurement Definitions

- **Representative retained inspection fixture** means a bounded fixture named during planning that includes unchanged content, localized paint change, layout shift, added content, removed content, broad damage, and unsupported facts.
- **Localized interaction fixture** means an interaction whose expected visual effect is limited to one named control or region, as documented by the test setup.
- **Broad damage** means a localized transition dirties regions outside its declared expected affected regions or exceeds the scenario-specific maximum dirty percentage documented by the test or sample assertion; full-surface damage is always blocking unless a scoped intentional exception is present.
- **Stable identity** excludes explicitly dynamic fields such as timestamps, generated run identifiers, and machine-specific paths.
- **Maintainer workstation** means the same class of local development machine used for existing repository validation evidence; any headless or environment-limited substitutions must be disclosed.

## Assumptions

- The "next item" is interpreted from the retrospective's remaining Feature 165 follow-up list because the broader Feature 163, 164, 166, 167, 168, and 169 follow-ups are already documented as implemented.
- Retained/damage inspection extends the current structured inspection model; it does not replace screenshot evidence or human visual review.
- Broad damage may be valid for root-level theme, density, or size changes when an intentional exception is documented.
- The selected AntShowcase assertion should be representative and low-risk, proving adoption without forcing a full sample migration in this feature.
- The missing legacy validation target is treated as a validation-workflow gap; the desired outcome is a maintained command path, not preservation of any specific wrapper name.
