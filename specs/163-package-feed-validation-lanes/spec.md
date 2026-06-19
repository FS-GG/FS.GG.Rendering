# Feature Specification: Package Feed Validation Lanes

**Feature Branch**: `163-package-feed-validation-lanes`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "Start the next item in `docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md`: Feature 163, package-feed determinism and validation lanes."

## Context

The retrospective identifies package-version drift and unreliable full-solution validation as the first feature-sized follow-up. Package-consuming samples can silently restore stale `FS.GG.UI.*` packages, and a single aggregate test command can hang or hide which validation lane is blocked. Maintainers need a repeatable way to refresh the local package feed, verify sample package pins, prove package source selection, and run named validation lanes that produce diagnosable evidence instead of an ambiguous pass/fail signal.

## Change Classification

**Tier 1 (repository validation contract and sample restore behavior)**. This feature changes how package-consuming samples prove they are validating current packages and how repository validation evidence is classified. No public UI framework API surface is intended. If planning discovers that reusable validation models belong in a public package, the plan must call that out explicitly with compatibility and migration impact.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Prove samples use current local packages (Priority: P1)

A maintainer can refresh the local package feed, align package-consuming samples with the current `FS.GG.UI.*` package versions, and prove that selected samples do not restore stale framework packages.

**Why this priority**: AntShowcase previously validated against old package pins, weakening its value as a package-only consumer proof. This is the highest-priority trap called out by the retrospective.

**Independent Test**: Introduce a stale package pin in a package-consuming sample, run the package-pin check, and verify that the check fails with the stale package name, expected version, actual version, and sample path. Restore the current pin and verify the check passes.

**Acceptance Scenarios**:

1. **Given** current packable `FS.GG.UI.*` package versions and a selected package-consuming sample, **When** the maintainer runs the package refresh/check workflow, **Then** the workflow reports the current package versions, the sample package pins, and whether every `FS.GG.UI.*` reference matches the current version.
2. **Given** a selected sample has an older `FS.GG.UI.*` package pin, **When** the maintainer runs the check, **Then** the workflow fails and identifies the exact stale reference without requiring a full sample build.
3. **Given** the maintainer requests package-pin refresh, **When** the workflow completes, **Then** the selected sample package pins match the current local package version unless an explicit documented exception exists.

---

### User Story 2 - Prove package source selection is deterministic (Priority: P1)

A maintainer can verify that `FS.GG.UI.*` packages for package-consuming samples resolve only from the configured local feed, while third-party packages continue to resolve from approved external package sources.

**Why this priority**: Matching version strings is not enough if the restore can still use a stale global cache or an unintended package source.

**Independent Test**: Run a cold or isolated package proof for a selected sample and inspect the generated evidence. The proof passes only if it records the package source rules, package/cache locations, and the resolved `FS.GG.UI.*` versions from the local feed.

**Acceptance Scenarios**:

1. **Given** package source rules are configured for a selected sample, **When** restore validation runs, **Then** `FS.GG.UI.*` packages are constrained to the local feed and third-party packages are allowed only from approved external sources.
2. **Given** a restore would resolve an `FS.GG.UI.*` package from any non-local source, **When** restore validation runs, **Then** the workflow fails and reports the violating package and source.
3. **Given** an isolated or cold package proof is requested, **When** the proof completes, **Then** the evidence records the package cache location, feed locations, package versions, and whether any global cache was cleared.

---

### User Story 3 - Run diagnosable validation lanes (Priority: P2)

A maintainer can run named validation lanes with isolated outputs, per-lane logs, result artifacts, timeout handling, and hang diagnostics, instead of relying only on one full-solution command.

**Why this priority**: The retrospective records a full-solution validation run that stopped producing output and had to be canceled. Maintainers need lane-level evidence that says what passed, what failed, and what was incomplete.

**Independent Test**: Run a short successful lane, a lane that exits with a failure, and a lane configured to exceed its timeout. Verify that each lane writes separate logs and is classified correctly in the summary.

**Acceptance Scenarios**:

1. **Given** a maintainer selects validation lanes, **When** the lane runner starts, **Then** each lane has a distinct name, command description, result location, log location, timeout policy, and status.
2. **Given** a lane exceeds its timeout or is canceled, **When** the runner writes the summary, **Then** the lane is marked timed out or canceled and is not counted as green.
3. **Given** a test lane stops making progress, **When** hang diagnostics are enabled for that lane, **Then** the runner preserves enough evidence to identify the lane, command, elapsed time, and diagnostic artifact location.
4. **Given** two validation lanes can run concurrently, **When** they use the default runner configuration, **Then** their outputs do not share result directories or generated runtime output paths.

---

### User Story 4 - Read an honest readiness summary (Priority: P3)

A reviewer can open one validation summary and understand package proof status, lane status, cache/source locations, and any caveats such as skipped, canceled, timed-out, or environment-limited lanes.

**Why this priority**: Feature readiness must not hide incomplete evidence. A concise summary prevents canceled aggregate validation from being mistaken for a complete green signal.

**Independent Test**: Generate a summary from mixed lane results containing one pass, one failure, one timeout, one skipped lane, and one canceled lane. Verify that every status is visible and the overall readiness state is blocked or incomplete rather than accepted.

**Acceptance Scenarios**:

1. **Given** mixed validation lane results, **When** the summary is generated, **Then** it classifies each lane as passed, failed, timed out, hung, skipped, canceled, or not run.
2. **Given** package proof was run, **When** a reviewer reads the summary, **Then** they can identify the current package version, selected samples, local feed, package cache, and source-selection proof.
3. **Given** any required lane is failed, timed out, hung, canceled, or not run, **When** the summary computes overall readiness, **Then** it does not report the feature as fully ready.

### Edge Cases

- A repository checkout has no package-consuming samples selected for validation.
- A selected sample intentionally targets an older `FS.GG.UI.*` package version for compatibility verification.
- Packable `FS.GG.UI.*` projects do not all share the same package version.
- The local feed is empty, missing one package, or contains multiple versions of the same package.
- A package restore succeeds from a global cache even though the local feed proof is stale.
- A validation lane is canceled manually after some sub-steps passed.
- A validation lane times out without producing a final test result.
- Two validation lanes run at the same time and would normally compete for the same output path.
- A full aggregate lane is skipped because focused lanes already provide the required evidence.
- A host cannot produce graphical or hang-dump diagnostics; the summary must disclose the limitation instead of treating the lane as green.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The validation workflow MUST discover the current source-controlled `FS.GG.UI.*` package versions that are intended for local package-consuming sample validation.
- **FR-002**: The validation workflow MUST list every selected sample `FS.GG.UI.*` package reference with package id, source file path, declared version, and expected current version.
- **FR-003**: The package-pin check MUST fail when any selected sample references a stale `FS.GG.UI.*` version, unless the sample has an explicit compatibility exception recorded in validation evidence.
- **FR-004**: The package refresh workflow MUST allow maintainers to align selected sample `FS.GG.UI.*` package pins to the current package versions.
- **FR-005**: Package source validation MUST prove that `FS.GG.UI.*` packages resolve only from the configured local feed for selected package-consuming samples.
- **FR-006**: Package source validation MUST allow approved third-party package sources without weakening the local-only rule for `FS.GG.UI.*` packages.
- **FR-007**: A cold or isolated package proof MUST record package source locations, package cache location, selected samples, resolved package versions, and whether global package caches were cleared.
- **FR-008**: Validation lanes MUST be named and independently runnable, including at minimum package proof, selected sample validation, Controls validation, rendering/harness validation, and aggregate solution validation.
- **FR-009**: Each validation lane MUST write lane-specific logs and result artifacts without sharing generated output paths with another concurrently running lane.
- **FR-010**: Test-oriented validation lanes MUST support a bounded no-progress or hang-detection policy and MUST preserve diagnostic evidence when that policy triggers.
- **FR-011**: The validation summary MUST classify each lane as passed, failed, timed out, hung, skipped, canceled, not run, or environment-limited.
- **FR-012**: The validation summary MUST NOT classify the overall gate as green when any required lane is failed, timed out, hung, canceled, not run, or environment-limited without an explicit accepted exception.
- **FR-013**: The workflow MUST distinguish a focused lane success from a completed aggregate full-solution validation so reviewers can see when an aggregate gate was skipped, canceled, or timed out.
- **FR-014**: The default workflow MUST avoid destructive global package-cache clearing; destructive cache clearing MUST be an explicit cold-proof mode and must be recorded in evidence.
- **FR-015**: The workflow MUST document the commands, expected evidence files, lane statuses, and package/source proof so a maintainer can repeat the validation without external instructions.

### Key Entities

- **Packable Framework Package**: A source-controlled `FS.GG.UI.*` package intended to be packed into the configured local feed for consumer validation.
- **Package-Consuming Sample**: A sample project that validates the framework through package references rather than direct source project references.
- **Package Pin**: A source-controlled package reference in a sample, including package id, declared version, file path, and any compatibility exception.
- **Local Package Feed**: The configured package source used to validate current `FS.GG.UI.*` packages before publication.
- **Package Source Rule**: A validation rule that constrains which package source may satisfy a package id or package id pattern.
- **Package Cache Proof**: Evidence showing the package cache and source locations used for a validation run.
- **Validation Lane**: A named validation unit with command description, timeout/hang policy, output locations, and status.
- **Lane Result**: The outcome of one lane run, including status, elapsed time, logs, result artifacts, diagnostics, and caveats.
- **Validation Summary**: A human-readable and machine-checkable record of package proof, lane results, overall readiness, and incomplete evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of stale `FS.GG.UI.*` package pins in selected samples are detected before sample build or test execution begins.
- **SC-002**: For AntShowcase, 100% of `FS.GG.UI.*` package references match the current local package version after the refresh workflow runs.
- **SC-003**: Package source validation records package source locations, package cache location, selected samples, and resolved `FS.GG.UI.*` versions for every package proof run.
- **SC-004**: A selected sample restore fails validation if any `FS.GG.UI.*` package can resolve from a non-local source.
- **SC-005**: Each validation lane writes a separate log and result location, and two concurrently eligible lanes do not write to the same result directory under the default configuration.
- **SC-006**: A lane that exceeds its configured timeout or hang policy is classified as timed out or hung within one validation summary update, and is never counted as passed.
- **SC-007**: A mixed validation run containing passed, failed, timed-out, hung, skipped, canceled, not-run, and environment-limited lanes shows all eight statuses in the final summary.
- **SC-008**: A reviewer can identify the current package version, selected samples, local feed, package cache, required lane list, and incomplete lanes from the summary in under 2 minutes.
- **SC-009**: The documented workflow lets a maintainer repeat the package proof and at least one selected sample validation lane from a clean checkout without relying on prior conversation context.

## Assumptions

- AntShowcase is the first package-consuming sample that must be covered by this feature.
- The configured local package feed is `~/.local/share/nuget-local/`, matching the repository constitution.
- Source-controlled `FS.GG.UI.*` package versions are the authority for what package-consuming samples should validate unless a compatibility exception is explicitly recorded.
- Package-consuming samples should continue to validate packages rather than direct source project references.
- The feature may add repository tooling and sample validation configuration, but public UI framework API changes are not expected.
- Aggregate full-solution validation remains useful, but focused lanes with clear evidence can be authoritative when the aggregate lane is skipped or incomplete and the limitation is disclosed.

## Out of Scope

- Publishing packages to an external package registry.
- Changing the repository package versioning policy.
- Fixing the underlying cause of any existing long-running or hung tests.
- Replacing all continuous integration behavior.
- Implementing visual-readiness, render-inspection, or responsiveness diagnostics beyond making them possible validation lanes in a later feature.
- Updating agent skills for package drift, readiness evidence, or test parallelism; those are covered by the later skill-guidance feature from the retrospective.
