# Feature Specification: Skill Parity and Evidence Guidance

**Feature Branch**: `168-skill-parity-evidence`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md"

**Resolved Item**: The next unimplemented retrospective item is the skill parity and evidence-guidance follow-up. The report's detailed section labels this as Feature 167, but Feature 167 is already used by the implemented input/render responsiveness work; the dependency graph identifies the skill/parity follow-up as Feature 168. This specification uses the non-conflicting branch and feature directory `168-skill-parity-evidence`.

## Context

The retrospective found that several repository traps were repeatedly rediscovered during implementation and merge work: package-consuming samples can drift from current local packages, readiness evidence can be ignored unless allowlisted, same-project validation can fail when concurrent test runs share output directories, generated readiness summaries can erase manual caveats, and screenshot-ready samples still need separate responsiveness evidence. Agent skills helped with domain orientation, but the guidance is spread across agent-specific wrappers and can drift from canonical repository guidance.

This feature makes those lessons durable. Maintainers, contributors, and coding agents need consistent skill guidance, a parity check that detects missing or stale wrappers, and a generated report that shows whether Claude, Codex, local agent, and package-owned skill surfaces point at the same repository rules.

## Change Classification

**Tier 1 (repository workflow and validation guidance contract)**. This feature changes the contributor and agent workflow contract for validation, readiness evidence, and skill synchronization. It is not expected to change runtime `FS.GG.UI.*` behavior or public rendering APIs. If planning discovers public package skill surfaces need compatibility-significant changes, the plan must identify the affected surface and migration impact.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Follow Repository Trap Guidance (Priority: P1)

A coding agent or contributor touching samples, readiness artifacts, or validation workflows can load the relevant skill and see the repository-specific checks required before claiming readiness.

**Why this priority**: The retrospective's main skill finding is that agents missed recurring local traps unless they were remembered from prior conversation context. The highest-value slice is making those checks visible at the point of work.

**Independent Test**: Inspect every relevant updated skill and verify that it names the required guidance for package-pin drift, readiness evidence allowlisting, validation output isolation, visual readiness, responsiveness diagnostics, post-merge package bump validation, and evidence honesty.

**Acceptance Scenarios**:

1. **Given** an agent is working on a package-consuming sample, **When** it consults the sample or implementation skill, **Then** the guidance tells it to compare package references against current package versions and use the existing package-feed proof workflow before claiming sample validation.
2. **Given** a feature requires committed readiness artifacts, **When** an agent consults implementation or readiness guidance, **Then** the guidance warns that feature readiness directories are ignored by default and requires an ignore-status check before staging evidence.
3. **Given** an agent is planning validation commands, **When** it consults validation guidance, **Then** it is warned not to run two test invocations for the same project and configuration concurrently unless their outputs are isolated.
4. **Given** an agent reports validation readiness, **When** a full gate was canceled, timed out, environment-limited, or replaced by targeted substitute gates, **Then** the guidance requires the caveat to remain visible rather than marking the gate green.

---

### User Story 2 - Detect Skill Wrapper Drift (Priority: P1)

A maintainer can run a parity check and identify missing, stale, wrapper-only, or broken skill surfaces across supported agent directories.

**Why this priority**: Skill guidance only stays useful if wrappers and canonical sources remain synchronized. A checker prevents one agent surface from silently losing repository rules.

**Independent Test**: Run the parity check against controlled fixture or dry-run cases with a missing wrapper, stale description, broken target path, wrapper-only entry, and canonical drift. Each case must be detected with the affected skill name, agent surface, finding type, and remediation hint.

**Acceptance Scenarios**:

1. **Given** a canonical skill exists without a corresponding supported-agent wrapper, **When** the parity check runs, **Then** it reports the missing wrapper and names the canonical source.
2. **Given** a wrapper points to a missing or invalid source path, **When** the parity check runs, **Then** it reports a broken-target finding and fails the parity status.
3. **Given** a wrapper description or routed source is stale relative to the canonical guidance, **When** the parity check runs, **Then** it reports drift without modifying files unless an explicit update mode is requested later.
4. **Given** all supported skill surfaces are synchronized, **When** the parity check runs, **Then** it reports a passing status with counts by agent surface and skill category.

---

### User Story 3 - Preserve Visual and Responsiveness Evidence Honesty (Priority: P2)

A maintainer or agent working on screenshots, contact sheets, visual inspections, or live interactivity sees consistent guidance that real evidence is preferred and limitations must be disclosed.

**Why this priority**: The retrospective showed that a sample can have useful screenshots while still lacking accepted live responsiveness proof, and generated summaries can remove important manual caveats.

**Independent Test**: Review the updated visual-readiness, product-testing, implementation, and merge guidance. Each relevant skill must distinguish accepted evidence from degraded, synthetic, environment-limited, pending-review, and substitute evidence.

**Acceptance Scenarios**:

1. **Given** screenshot evidence is available, **When** an agent follows visual-readiness guidance, **Then** it prefers real captures, requires reviewer classification before accepted readiness, and records degraded capture reasons explicitly.
2. **Given** generated readiness summaries are refreshed, **When** an agent follows the guidance, **Then** manual caveats and reviewer notes are preserved or moved outside generated-only sections rather than overwritten.
3. **Given** an interactive sample is screenshot-ready, **When** responsiveness guidance applies, **Then** the agent is told to validate pointer and keyboard activation separately and distinguish input routing cost from post-update render or presentation cost.
4. **Given** live timing cannot be captured in the current environment, **When** readiness is summarized, **Then** the guidance requires an environment-limited or substitute-evidence status instead of accepted live responsiveness.

---

### User Story 4 - Review a Skill Parity Report (Priority: P3)

A reviewer can open a generated parity report and quickly understand which skill surfaces are current, which guidance rules are covered, and what remains incomplete.

**Why this priority**: Reviewers need an artifact that proves skill synchronization without reading every wrapper manually.

**Independent Test**: Generate the parity report after updating skills. A reviewer must be able to identify overall parity status, required guidance coverage, affected skill surfaces, and any remaining findings from the report alone.

**Acceptance Scenarios**:

1. **Given** the parity check completes, **When** the report is generated, **Then** it lists supported agent surfaces, canonical sources, wrapper counts, finding counts, and required guidance-rule coverage.
2. **Given** findings remain, **When** a reviewer reads the report, **Then** each finding includes the skill name, agent surface, category, severity, and suggested next action.
3. **Given** no findings remain, **When** a reviewer reads the report, **Then** it shows a passing parity status and the date of the check.
4. **Given** a skill intentionally differs between agent surfaces, **When** the report lists it, **Then** the exception is explicit and does not hide unrelated drift.

### Edge Cases

- A canonical skill exists in more than one location with different descriptions or guidance.
- A supported agent wrapper exists without a canonical repository source.
- A wrapper references a source path that no longer exists.
- A skill is intentionally advisory for one agent surface but unavailable for another.
- A package-owned skill contains guidance that conflicts with a wrapper-level summary.
- A generated report from a prior run exists and would otherwise be overwritten without showing when it was regenerated.
- A feature has no committed readiness artifacts, so ignore allowlisting guidance should not force unnecessary `.gitignore` changes.
- A sample intentionally targets an older package version for compatibility proof.
- A validation run uses isolated outputs, so same-project concurrency may be allowed only when the isolation is explicit.
- Live responsiveness evidence is unavailable because the environment lacks a visible presentation surface.
- A canceled, timed-out, skipped, synthetic, or environment-limited gate is present alongside passing targeted checks.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST identify canonical skill sources and supported agent-wrapper surfaces before any wrapper guidance is updated.
- **FR-002**: Skill guidance for sample work MUST require package-consuming samples to be checked for stale `FS.GG.UI.*` package references before sample validation is claimed.
- **FR-003**: Skill guidance for package-consuming samples MUST point contributors to the repository's existing package-feed proof and validation-lane workflows rather than relying on manual version inspection alone.
- **FR-004**: Skill guidance for readiness artifacts MUST state that feature readiness evidence is ignored by default unless allowlisted, and MUST require an ignore-status check before evidence is treated as committed.
- **FR-005**: Skill guidance for validation MUST warn against concurrent test runs for the same project and configuration unless their output locations are explicitly isolated.
- **FR-006**: Skill guidance for visual readiness MUST require real screenshot evidence when available, explicit degraded-capture disclosure, reviewer classification before accepted readiness, and preservation of manual caveats when generated summaries are refreshed.
- **FR-007**: Skill guidance for responsiveness MUST require interactive samples to validate pointer activation and keyboard activation separately from screenshot readiness, and MUST distinguish routing latency from post-update render or presentation latency.
- **FR-008**: Skill guidance for merge and post-merge work MUST require package bump evidence, local-feed packing, sample package-pin alignment, sample restore or validation after package changes, and readiness ledger updates when package versions change.
- **FR-009**: Skill guidance for evidence honesty MUST prohibit reporting canceled, timed-out, skipped, synthetic, substitute, or environment-limited checks as fully green without visible caveats.
- **FR-010**: The parity check MUST compare canonical skill sources with supported wrappers and report missing wrappers, wrapper-only skills, stale descriptions, broken source paths, and canonical/wrapper drift.
- **FR-011**: The parity check MUST provide a non-destructive mode that reports findings without modifying skill files.
- **FR-012**: The parity check MUST support a controlled fixture or dry-run case that proves missing, stale, broken-target, wrapper-only, and drift findings are detectable.
- **FR-013**: The generated parity report MUST include overall status, checked date, supported agent surfaces, canonical source count, wrapper count, finding count by severity, required guidance-rule coverage, and remediation notes for unresolved findings.
- **FR-014**: Wrapper guidance MUST not fork or contradict canonical package or repository skill guidance; intentional exceptions MUST be listed explicitly in the parity report.
- **FR-015**: The feature MUST preserve existing skill metadata needed for discovery and invocation unless planning explicitly marks a metadata change as part of the migration.
- **FR-016**: The feature MUST include reviewable evidence showing the parity check result, generated report, and required guidance-rule coverage after skill updates.

### Scope Boundaries

- In scope: skill guidance updates, wrapper synchronization, parity checking, generated parity reporting, evidence-honesty guidance, and references to existing validation workflows introduced by earlier features.
- Out of scope: changing runtime rendering behavior, adding new package-feed validation functionality, adding new visual capture APIs, adding new responsiveness diagnostics APIs, replacing the validation lane runner, or changing package versioning policy.
- Follow-up scope: automatic wrapper regeneration, continuous integration enforcement, or richer marketplace publishing can be planned later if the non-destructive parity check proves useful.

### Key Entities

- **Canonical Skill Source**: The repository or package-owned skill text treated as the authority for a capability's guidance.
- **Agent Wrapper**: An agent-specific skill entry that exposes canonical guidance to a supported coding-agent surface.
- **Guidance Rule**: A required repository instruction covering a known trap such as package drift, readiness allowlisting, validation output isolation, visual evidence, responsiveness evidence, post-merge package updates, or evidence honesty.
- **Parity Check**: A non-destructive validation run that compares canonical skill sources and wrappers.
- **Parity Finding**: A detected synchronization issue, including missing wrapper, wrapper-only entry, stale description, broken source path, guidance-rule gap, or intentional exception.
- **Parity Report**: A reviewer-readable artifact summarizing checked skill surfaces, findings, guidance-rule coverage, and remediation status.
- **Evidence Caveat**: A visible explanation that validation evidence is canceled, timed out, skipped, substitute, synthetic, degraded, environment-limited, pending review, or otherwise not accepted.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of relevant updated skills cover the seven required guidance themes: package-pin drift, readiness evidence allowlisting, validation output isolation, visual readiness, responsiveness diagnostics, post-merge package bump validation, and evidence honesty.
- **SC-002**: The parity check detects 100% of controlled missing-wrapper, wrapper-only, stale-description, broken-target, and canonical-drift cases in fixture or dry-run validation.
- **SC-003**: After skill updates, the generated parity report shows zero unresolved high-severity findings for supported agent surfaces.
- **SC-004**: A reviewer can identify overall parity status, unresolved findings, and required guidance-rule coverage from the parity report in under 2 minutes.
- **SC-005**: 100% of unresolved parity findings include a skill name, agent surface, category, severity, and suggested next action.
- **SC-006**: No updated wrapper contradicts its canonical source; any intentional wrapper-specific difference is listed as an explicit exception in the report.
- **SC-007**: Evidence guidance in implementation, testing, sample, visual-readiness, responsiveness, and merge-related skills consistently prevents canceled, timed-out, skipped, synthetic, substitute, or environment-limited checks from being reported as accepted without caveats.
- **SC-008**: The final readiness evidence includes the parity check output, generated parity report, and a brief summary of guidance-rule coverage.

## Assumptions

- "Next item" is interpreted from the report's branch-order and dependency graph as skill parity and evidence guidance, using feature number 168 to avoid conflicting with the already implemented responsiveness feature.
- Supported agent surfaces initially include the repository's local agent skills and Claude wrappers; Codex skill coverage is represented by the repository or installed skill sources available to this checkout where applicable.
- Existing package-feed proof, validation lane, visual-readiness, inspection, and responsiveness workflows are available for skills to reference; this feature updates guidance and parity checks rather than reimplementing those workflows.
- Skill guidance is advisory under the constitution, but it should still be accurate, discoverable, and reviewable.
- Readiness artifacts are committed only when they are deliverables for this feature; when committed, ignore allowlisting and ignore-status proof are required.
- The first parity checker is expected to be non-destructive by default. Automatic wrapper repair can be deferred unless planning proves it is necessary for the MVP.
