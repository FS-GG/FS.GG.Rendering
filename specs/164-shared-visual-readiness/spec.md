# Feature Specification: Shared Visual Readiness Tooling

**Feature Branch**: `164-shared-visual-readiness`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "Start the next item in `docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md`: shared visual-readiness tooling in `FS.GG.UI.Testing`."

## Context

The retrospective identifies that AntShowcase now has real screenshot evidence, completeness checks, contact sheets, reviewer-defect classification, and readiness summaries, but most of that workflow is owned by the sample app. Future generated products and samples need the same evidence discipline without copying sample-specific readiness code or risking generated summaries that erase manual validation notes.

This feature creates a reusable visual-readiness capability for repository validation and package-consuming samples. Samples remain responsible for their page registry, theme choices, accepted sizes, and actual page rendering. The shared capability owns the common evidence workflow: target matrix expansion, artifact completeness classification, reviewer classification records, contact sheet reporting, readiness summaries, and preservation of manual summary context.

## Change Classification

**Tier 1 (testing package contract and readiness workflow behavior)**. This feature is expected to add or change reusable validation surface in the testing/readiness boundary and to change how samples produce visual-readiness evidence. Planning must identify any public package surface, dependency impact, compatibility notes, and migration path for AntShowcase.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Validate Screenshot Evidence Consistently (Priority: P1)

A sample maintainer can declare pages, themes, sizes, and output locations, then run a shared visual-readiness workflow that validates whether every required screenshot artifact exists, is readable, matches the requested dimensions, and has an explicit readiness status.

**Why this priority**: Screenshot completeness and correctness are the core visual-readiness proof. Without a shared validator, every sample must recreate the same target matrix and failure classification logic.

**Independent Test**: Use a small representative visual-readiness fixture with known complete, missing, wrong-size, undecodable, and degraded screenshots. The shared workflow must classify each artifact correctly and produce a complete evidence summary without sample-specific classification code.

**Acceptance Scenarios**:

1. **Given** a sample declares 3 pages, 2 themes, and 2 accepted sizes, **When** the visual-readiness target matrix is produced, **Then** the workflow reports 12 deterministic targets with stable page, theme, size, and output identifiers.
2. **Given** a target screenshot is missing, **When** completeness validation runs, **Then** that target is reported as missing and the overall readiness state is not accepted.
3. **Given** a target screenshot has the wrong dimensions or cannot be decoded, **When** completeness validation runs, **Then** the target is reported with the exact classification and the expected versus observed evidence where available.
4. **Given** a target screenshot is intentionally degraded because real capture is unavailable, **When** readiness is summarized, **Then** the degraded state and reason are visible and cannot be mistaken for a fully accepted capture.

---

### User Story 2 - Require Reviewer Classification Before Acceptance (Priority: P1)

A reviewer can classify visual evidence by page, theme, size, severity, and notes, and the readiness decision remains pending or blocked until required reviewer records exist and no blocking defect is present.

**Why this priority**: Feature 162 proved that real screenshots are necessary but not sufficient; accepted readiness also needs explicit human review and defect classification.

**Independent Test**: Generate a reviewer-classification template for a known target matrix, fill it with no defects, minor defects, and one blocking defect across separate runs, and verify the readiness decision changes accordingly.

**Acceptance Scenarios**:

1. **Given** screenshot evidence exists but reviewer classifications are absent, **When** the readiness decision is computed, **Then** the result is pending review rather than accepted.
2. **Given** a reviewer records a blocking visual defect for any required target, **When** the readiness decision is computed, **Then** the result is blocked and identifies the target and reviewer note.
3. **Given** every required target has a reviewer classification and no blocking defects, **When** the readiness decision is computed, **Then** the visual evidence can be accepted if all other required evidence is complete.

---

### User Story 3 - Produce Reviewable Contact Sheets and Summaries (Priority: P2)

A maintainer can produce contact sheets and summaries from the validated evidence so reviewers can inspect visual coverage, counts, paths, statuses, caveats, and readiness outcome without reading sample internals.

**Why this priority**: Contact sheets made Feature 162 reviewable. Future products need the same reviewer-friendly output with consistent summary rules.

**Independent Test**: Run the shared reporting workflow over a fixture matrix with mixed statuses and verify that contact sheet order, target labels, summary counts, artifact links, caveats, and readiness outcome match the source evidence.

**Acceptance Scenarios**:

1. **Given** multiple pages and themes have captured screenshots, **When** contact sheets are generated, **Then** each sheet uses deterministic ordering and labels that let a reviewer map every tile back to its page, theme, and size.
2. **Given** evidence contains complete, degraded, missing, and blocked targets, **When** the human-readable summary is generated, **Then** the summary shows counts for each status and does not collapse them into a single ambiguous pass/fail result.
3. **Given** machine-readable evidence is generated, **When** a validation lane consumes it, **Then** the lane can determine target counts, statuses, reviewer state, and overall readiness without parsing prose.

---

### User Story 4 - Preserve Manual Readiness Notes (Priority: P2)

A maintainer can regenerate visual-readiness summaries repeatedly without losing manually authored caveats, package validation notes, full-solution test limitations, or reviewer context outside the generated section.

**Why this priority**: The retrospective records that a generated visual-readiness summary overwrote richer manual validation notes. Regeneration must be safe before this workflow can be reused broadly.

**Independent Test**: Create a summary with manual sections before and after the generated content, regenerate the visual-readiness section twice, and verify that manual text outside the managed generated area remains byte-for-byte unchanged.

**Acceptance Scenarios**:

1. **Given** a readiness summary already contains manual notes outside the visual-readiness generated area, **When** the visual summary is regenerated, **Then** the manual notes are preserved.
2. **Given** a summary has no generated visual-readiness area yet, **When** generation runs, **Then** the generated content is inserted in a predictable location without deleting existing content.
3. **Given** a generated summary cannot be safely updated, **When** generation runs, **Then** the workflow fails with an actionable message instead of overwriting manual content.

---

### User Story 5 - Adopt the Shared Workflow in AntShowcase First (Priority: P3)

An AntShowcase maintainer can migrate the existing visual-readiness workflow to the shared capability while preserving current evidence behavior, accepted sizes, reviewer gates, and summary meaning.

**Why this priority**: AntShowcase is the proven source workflow and the first regression target for the shared capability, but reusable contracts should be validated before broad adoption.

**Independent Test**: Run the migrated AntShowcase visual-readiness workflow against its preferred-size and minimum-size evidence sets and verify that target counts, reviewer requirements, status classifications, contact sheets, and summaries match the current accepted behavior.

**Acceptance Scenarios**:

1. **Given** AntShowcase preferred-size evidence for 19 pages across 2 themes, **When** the shared workflow validates it, **Then** it reports the same 38 required captures and accepted reviewer state as the existing readiness package.
2. **Given** AntShowcase minimum-size representative evidence for 6 pages across 2 themes, **When** the shared workflow validates it, **Then** it reports the same 12 required captures and accepted reviewer state as the existing readiness package.
3. **Given** AntShowcase supplies page rendering and theme selection, **When** the shared workflow runs, **Then** the sample no longer owns duplicated generic evidence classification, reviewer parsing, contact-sheet reporting, or summary-preservation rules.

### Edge Cases

- A target matrix contains duplicate page, theme, size, or output identifiers.
- A screenshot file exists but is zero bytes, corrupt, undecodable, or has unexpected dimensions.
- A capture is intentionally degraded, but no reason is supplied.
- Reviewer classifications reference a target that is not part of the current matrix.
- Required reviewer classifications are missing for one theme or one accepted size.
- A blocking reviewer defect exists alongside otherwise complete screenshots.
- Old screenshot files remain in an output directory after the current target matrix changes.
- Contact sheet generation cannot include one target because the image is missing or undecodable.
- A summary file contains manual notes but no existing generated section.
- A summary file contains more than one generated section or malformed section markers.
- Evidence paths are relative to different working directories.
- A sample intentionally excludes a page, theme, or size from a particular readiness run.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The shared visual-readiness capability MUST let a sample describe visual pages, themes, accepted sizes, and output locations without embedding sample-specific page rendering in the shared workflow.
- **FR-002**: The capability MUST expand declared pages, themes, and sizes into a deterministic target matrix with stable identifiers and paths.
- **FR-003**: The capability MUST detect duplicate or ambiguous target identifiers before capture or validation begins.
- **FR-004**: The capability MUST classify each required screenshot artifact as complete, missing, wrong-size, undecodable, degraded, or otherwise blocked with an actionable reason.
- **FR-005**: The capability MUST record expected dimensions, observed dimensions when available, artifact path, byte count when available, and a stable content identity for each validated capture.
- **FR-006**: The capability MUST make degraded capture explicit with a required reason and MUST prevent degraded captures from being reported as fully complete.
- **FR-007**: The capability MUST support stale-artifact detection or cleanup so old screenshots cannot silently satisfy a changed target matrix.
- **FR-008**: The capability MUST create reviewer-classification templates that cover every required target in a readiness run.
- **FR-009**: The capability MUST parse reviewer classifications and report missing, duplicate, malformed, unknown-target, minor, major, and blocking classifications.
- **FR-010**: The readiness decision MUST remain pending review until all required reviewer classifications exist.
- **FR-011**: The readiness decision MUST be blocked when any required capture is missing, wrong-size, undecodable, malformed, or marked with a blocking reviewer defect, unless the spec or plan records an accepted exception.
- **FR-012**: The capability MUST generate contact sheets or equivalent reviewer-friendly visual indexes for captured evidence with deterministic ordering and visible target labels.
- **FR-013**: The capability MUST generate both human-readable and machine-checkable summaries containing target counts, status counts, reviewer state, contact sheet locations, caveats, and overall readiness.
- **FR-014**: Generated summaries MUST preserve manual content outside the generated visual-readiness section or write to a clearly generated file that cannot overwrite manual validation notes.
- **FR-015**: Summary regeneration MUST fail safely with an actionable message when existing generated-section markers are malformed or ambiguous.
- **FR-016**: AntShowcase MUST be able to adopt the shared capability while preserving its current preferred-size and minimum-size visual-readiness evidence semantics.
- **FR-017**: The feature MUST document repeatable validation steps, expected evidence artifacts, readiness statuses, and migration expectations for the first adopter sample.
- **FR-018**: Planning MUST identify whether any new image-processing or reporting dependency is introduced, which package boundary owns it, and why it does not burden runtime-only consumers.

### Key Entities

- **Visual Page**: A sample-owned screen or page that participates in a visual-readiness run, identified by a stable id and reviewer-facing title.
- **Visual Theme**: A named visual mode or theme variant included in a readiness run.
- **Accepted Size**: A viewport or render size that the sample declares as required visual-readiness coverage.
- **Capture Target**: One required page, theme, size, and output location combination.
- **Capture Artifact**: The screenshot or evidence file produced for a capture target.
- **Capture Record**: The validation result for one capture target, including artifact metadata and completeness status.
- **Reviewer Classification**: A reviewer decision for a capture target, including severity, defect class, reviewer identity, timestamp, and notes.
- **Contact Sheet**: A reviewer-facing visual index that groups capture artifacts for faster inspection.
- **Visual Readiness Report**: The combined machine-checkable record of target matrix, capture records, reviewer classifications, contact sheets, caveats, and readiness decision.
- **Generated Summary Section**: The bounded generated portion of a readiness summary that can be regenerated without changing manual content outside it.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A fixture containing complete, missing, wrong-size, undecodable, degraded, and blocking-review targets is classified with 100% expected statuses.
- **SC-002**: AntShowcase preferred-size evidence validates 38 required targets across 19 pages and 2 themes with the same accepted outcome as the existing readiness package.
- **SC-003**: AntShowcase minimum-size evidence validates 12 required targets across 6 pages and 2 themes with the same accepted outcome as the existing readiness package.
- **SC-004**: Summary regeneration preserves manual content outside the generated visual-readiness section byte-for-byte across at least 3 consecutive regenerations.
- **SC-005**: A missing reviewer classification or blocking reviewer defect prevents accepted readiness in 100% of validation runs.
- **SC-006**: A reviewer can identify total targets, missing or degraded captures, blocking defects, contact sheet locations, and overall readiness from the human-readable summary in under 2 minutes.
- **SC-007**: A new sample can define a 3 page, 2 theme, 2 size visual-readiness matrix and receive the expected 12 targets, completeness summary, reviewer template, and readiness report without duplicating generic evidence-classification logic.
- **SC-008**: Migrating AntShowcase removes or centralizes at least 50% of sample-owned generic visual-readiness workflow code while preserving sample-owned page registry and rendering choices.
- **SC-009**: Malformed generated-section markers are detected before write in 100% of summary-preservation tests.

## Assumptions

- AntShowcase is the first adopter and regression target for the shared visual-readiness workflow.
- Real screenshot capture remains preferred evidence whenever the local environment can produce it.
- Samples continue to own page selection, theme aliases, accepted sizes, and rendering callbacks.
- The shared capability owns generic evidence classification, reviewer records, contact-sheet reporting, and summary-preservation behavior.
- Generated visual-readiness content may live in managed sections or generated-only files; the plan will choose the least risky approach.
- Public testing-package surface changes are acceptable if the plan documents compatibility and migration impact.
- This feature focuses on visual-readiness evidence workflow, not deterministic inspection of layout bounds or input responsiveness timing.

## Out of Scope

- Structured render/layout inspection metadata for text bounds, clipping, z-order, or paint ownership.
- Responsiveness diagnostics, input-to-present latency budgets, or scheduler changes.
- Package-feed validation lanes, package-pin refresh, or local feed proof behavior already covered by Feature 163.
- `.gitignore` readiness allowlisting and skill guidance for staging evidence artifacts.
- Creating a new visual design for AntShowcase pages.
- Replacing human visual review with automated screenshot diffing.
- Publishing visual evidence outside the repository readiness workflow.
