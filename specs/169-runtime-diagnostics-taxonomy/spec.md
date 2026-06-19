# Feature Specification: Runtime Diagnostics Taxonomy

**Feature Branch**: `169-runtime-diagnostics-taxonomy`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md"

**Resolved Item**: The next unimplemented retrospective item after Feature 168 is Feature 169, runtime diagnostics taxonomy. It addresses the report finding that environment warnings, expected backend-cost diagnostics, recoverable rendering limitations, and actual readiness blockers currently appear in the same textual stream and can be misread during sample and readiness runs.

## Context

Interactive and readiness workflows can emit useful but mixed diagnostic messages. Some messages describe local environment noise, some describe expected backend cost, some describe recoverable rendering limitations, and some should block readiness. Contributors and reviewers need the runtime to classify these diagnostics consistently so sample runs are less alarming by default, readiness summaries are honest, and tests can assert blocker status without parsing unstructured console text.

## Change Classification

**Tier 1 (diagnostic contract and readiness behavior)**. This feature is expected to introduce or formalize observable diagnostic records, summaries, and readiness interpretation. Planning must identify any public package surface, compatibility impact, migration notes, and evidence needed for sample and readiness consumers.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Understand Sample Runtime Diagnostics (Priority: P1)

A sample maintainer runs an interactive or evidence command and sees diagnostics grouped by severity and category, with expected environment and backend-cost messages separated from actual readiness blockers.

**Why this priority**: The retrospective records that benign environment warnings and expected backend-cost diagnostics looked like possible failures. The first value is making sample output understandable without hiding important facts.

**Independent Test**: Run a representative diagnostic fixture containing environment noise, backend-cost information, a recoverable rendering limitation, a developer-action warning, and a readiness blocker. Verify the summary groups every item by category and reports whether blockers exist.

**Acceptance Scenarios**:

1. **Given** a sample run emits an environment warning, **When** diagnostics are summarized, **Then** the warning is grouped as environment-related and does not by itself mark readiness as blocked.
2. **Given** a control emits an expected backend-cost diagnostic, **When** diagnostics are summarized, **Then** the diagnostic is visible as backend cost and is not presented as a runtime failure.
3. **Given** a diagnostic indicates an actual readiness blocker, **When** diagnostics are summarized, **Then** the blocker is highlighted with severity, category, source, and action guidance.
4. **Given** the run has no blockers, **When** the default console summary is printed, **Then** it clearly states the non-blocking diagnostic counts without requiring verbose output.

---

### User Story 2 - Gate Readiness by Blocker Status (Priority: P1)

A readiness reviewer can rely on diagnostic summaries so informational or expected warnings remain visible, while only blocker-class diagnostics or unreviewed classification gaps prevent accepted readiness.

**Why this priority**: Readiness evidence should be neither too permissive nor too noisy. Informational diagnostics should not fail a run, but true blockers and unclassified surprises must not be buried.

**Independent Test**: Evaluate readiness summaries over fixtures with only informational diagnostics, with warnings, with a blocker, and with an unclassified diagnostic. Verify readiness status and caveats match the diagnostic content.

**Acceptance Scenarios**:

1. **Given** a run contains only informational environment and backend-cost diagnostics, **When** readiness is evaluated, **Then** diagnostics remain visible and readiness is not blocked by those diagnostics.
2. **Given** a run contains a readiness blocker, **When** readiness is evaluated, **Then** the run is blocked and the blocker can be traced to the original diagnostic.
3. **Given** a run contains an unclassified diagnostic, **When** readiness is evaluated, **Then** the run requires developer review until the diagnostic is classified or explicitly accepted with a scoped reason.

---

### User Story 3 - Review Structured Diagnostic Artifacts (Priority: P2)

A maintainer or reviewer can open a machine-checkable diagnostic artifact and determine counts, sources, categories, severities, blocker status, repeated-message counts, and recommended actions without reading raw console logs.

**Why this priority**: Readiness lanes and reviewers need stable artifacts. Console output is useful for humans during a run, but durable evidence needs structured records and summaries.

**Independent Test**: Generate a diagnostic artifact from a mixed fixture and verify that a validation lane can compute category counts, severity counts, blocker status, and repeated-message totals without parsing prose.

**Acceptance Scenarios**:

1. **Given** a run emits diagnostics from multiple runtime areas, **When** an artifact is written, **Then** each diagnostic has stable category, severity, source, message, occurrence count, and action fields.
2. **Given** repeated identical diagnostics occur, **When** the summary is generated, **Then** repeated messages are consolidated with an occurrence count while preserving the first and last occurrence context.
3. **Given** a readiness summary links to diagnostic evidence, **When** a reviewer opens it, **Then** the reviewer can identify blocker status and category counts from the summary and artifact.

---

### User Story 4 - Keep Console Output Concise by Default (Priority: P3)

A person running a sample sees a compact diagnostic summary by default, while detailed records remain available through artifacts or verbose output when investigation is needed.

**Why this priority**: Clear default output reduces false alarms during normal sample use, but investigation still needs the full diagnostic trail.

**Independent Test**: Run the same diagnostic fixture in default and verbose modes. Verify default output remains compact and grouped, while verbose output or artifacts expose all individual diagnostics.

**Acceptance Scenarios**:

1. **Given** a run emits many repeated non-blocking diagnostics, **When** default output is printed, **Then** the console shows grouped counts rather than every repeated line.
2. **Given** verbose output is requested, **When** diagnostics are printed, **Then** individual diagnostic details are available without changing readiness classification.
3. **Given** the diagnostic artifact cannot be written, **When** the run completes, **Then** the console reports the artifact failure as a developer-action warning.

### Edge Cases

- A diagnostic has a severity but no category.
- A diagnostic has a category but no severity.
- A diagnostic source reports conflicting severity and category values.
- The same diagnostic is emitted many times during one run.
- Diagnostics are emitted before a run identifier or output directory is available.
- A diagnostic artifact cannot be written because the output directory is missing or unwritable.
- Environment warnings appear on a different output stream from structured runtime diagnostics.
- A recoverable rendering limitation and a readiness blocker occur in the same run.
- A prior run's blocker artifact remains in the output directory after a later clean run.
- A diagnostic is intentionally accepted for a scoped scenario and must remain visible as an exception.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST classify runtime diagnostics by severity and category before they are used in sample summaries or readiness decisions.
- **FR-002**: The severity model MUST distinguish informational, warning, and error-level diagnostics using stable reviewer-facing labels, examples, and action guidance that do not require implementation knowledge.
- **FR-003**: The category model MUST distinguish environment, backend-cost, rendering-limitation, readiness-blocker, and developer-action diagnostics.
- **FR-004**: Expected environment and backend-cost diagnostics MUST remain visible while not blocking readiness unless they are explicitly elevated by a documented rule.
- **FR-005**: Readiness evaluation MUST block accepted status when a readiness-blocker diagnostic is present.
- **FR-006**: Unclassified or partially classified diagnostics MUST require developer review before diagnostics can be reported as fully accepted.
- **FR-007**: Diagnostic summaries MUST include counts by severity, counts by category, blocker count, unclassified count, and links or references to detailed evidence when available.
- **FR-008**: Diagnostic records MUST preserve source, category, severity, message, occurrence count, first occurrence context, last occurrence context, and recommended action.
- **FR-009**: Repeated identical diagnostics MUST be aggregated in summaries without losing the occurrence count.
- **FR-010**: Default sample output MUST present a compact grouped summary while preserving detailed diagnostics in artifacts or verbose output.
- **FR-011**: Diagnostic artifact failures MUST be surfaced as developer-action warnings and MUST not silently remove diagnostic evidence from readiness summaries.
- **FR-012**: Readiness summaries MUST distinguish non-blocking diagnostics, blocker diagnostics, unclassified diagnostics, and accepted diagnostic exceptions.
- **FR-013**: Accepted diagnostic exceptions MUST include a scoped reason, the affected category or source, and enough context for a reviewer to confirm the exception is intentional.
- **FR-014**: Tests MUST be able to assert that expected backend-cost diagnostics are informational and that blocker diagnostics change readiness outcome.
- **FR-015**: The feature MUST preserve existing diagnostic meaning during migration; renamed, reclassified, or newly blocking diagnostics must be documented for reviewers.

### Key Entities

- **Runtime Diagnostic**: A message or event emitted during an interactive, rendering, sample, or readiness workflow that may need human or automated interpretation.
- **Diagnostic Severity**: The user-facing importance level of a diagnostic: informational, warning, or error.
- **Diagnostic Category**: The reason group for a diagnostic, such as environment, backend cost, rendering limitation, readiness blocker, or developer action.
- **Diagnostic Source**: The runtime area, sample command, or validation lane that produced the diagnostic.
- **Diagnostic Artifact**: A durable, machine-checkable record of diagnostics emitted during one run.
- **Diagnostic Summary**: A reviewer-facing rollup of diagnostic counts, blocker status, exceptions, and links to detailed evidence.
- **Diagnostic Exception**: A scoped, documented acceptance of a known diagnostic that would otherwise need review or block readiness.
- **Readiness Diagnostic Status**: The readiness interpretation derived from diagnostics: accepted, blocked, review required, or environment-limited.

### Scope Boundaries

- In scope: diagnostic taxonomy, severity/category assignment, grouped console summaries, machine-checkable artifacts, readiness summary integration, diagnostic exception handling, and tests that assert diagnostic classification.
- Out of scope: choosing a repository-wide structured logging provider, adding remote telemetry collection, optimizing render performance, changing input scheduling, creating new visual-readiness APIs, or replacing existing validation lanes.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A representative fixture containing environment, backend-cost, rendering-limitation, developer-action, and readiness-blocker diagnostics is classified with 100% expected severities and categories.
- **SC-002**: In 100% of readiness fixture runs, informational environment and backend-cost diagnostics remain visible but do not block readiness by themselves.
- **SC-003**: In 100% of readiness fixture runs, a readiness-blocker diagnostic prevents accepted readiness and identifies the blocking source.
- **SC-004**: In 100% of classification-gap fixture runs, unclassified or partially classified diagnostics require developer review rather than being reported as fully accepted.
- **SC-005**: A reviewer can identify blocker status, category counts, severity counts, and accepted exceptions from the summary and artifact in under 2 minutes.
- **SC-006**: A run with at least 100 repeated identical non-blocking diagnostics reports one grouped summary entry with the correct occurrence count.
- **SC-007**: Default console output for a mixed diagnostic fixture stays within 12 summary lines while still reporting whether blockers or review-required diagnostics exist.
- **SC-008**: Diagnostic artifact write failures are reported in 100% of artifact-failure tests and are visible in the final run summary.

## Assumptions

- This feature targets diagnostics emitted by local runtime, sample, and readiness workflows; it is not a general-purpose observability platform.
- Existing human-readable diagnostic messages can remain, but they will gain or map to structured severity and category information.
- Default console output should optimize for quick human interpretation; artifacts should preserve detail for review and automated checks.
- Headless or local desktop environment issues are expected inputs to the taxonomy and should be classified without hiding actual blockers.
- If planning discovers public diagnostic surface changes, those changes must follow the repository's Tier 1 public-surface and compatibility rules.
