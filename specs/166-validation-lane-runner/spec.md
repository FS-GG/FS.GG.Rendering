# Feature Specification: Validation Lane Runner

**Feature Branch**: `166-validation-lane-runner`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-19-00-24-framework-and-skills-retrospective.md"

**Resolved Item**: The next unimplemented retrospective item is "split full validation into named lanes with timeouts" from the prioritized action plan and suggested follow-up tasks. Earlier high-priority follow-ups for package-feed validation, shared visual-readiness tooling, and structured render/layout inspection are already covered by Features 163, 164, and 165.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Run Targeted Validation Lanes (Priority: P1)

A maintainer preparing feature readiness can run a named validation lane, or the full required lane set, and receive a clear result without relying on one opaque aggregate validation command.

**Why this priority**: The retrospective identified full-solution validation as unreliable when used as a single gate. The highest value slice is giving maintainers smaller, named checks with independent pass/fail signals.

**Independent Test**: Can be tested by running one documented lane and then the required lane set, confirming that each run produces lane-level status, elapsed time, and artifact locations.

**Acceptance Scenarios**:

1. **Given** a repository with documented validation lanes, **When** a maintainer runs a single named lane, **Then** only that lane's checks are executed and the result names the lane, status, elapsed time, and evidence location.
2. **Given** a maintainer requests the required lane set, **When** the run completes, **Then** the final summary lists every required lane, each lane outcome, and the overall readiness outcome.
3. **Given** an optional aggregate lane is configured, **When** a required lane set is run, **Then** the optional aggregate lane is reported separately and cannot hide a required lane failure.

---

### User Story 2 - Diagnose Hung or Failing Lanes (Priority: P2)

A contributor investigating a stalled or failing validation run can see which lane was active, why it stopped, and where to find the supporting logs.

**Why this priority**: The retrospective recorded a full test run that stopped producing output for several minutes and had to be canceled. The lane runner must make this condition observable and bounded.

**Independent Test**: Can be tested with controlled lanes that pass, fail, exceed the total timeout, produce no progress before the no-progress timeout, hit infrastructure errors, and are canceled by the operator; each outcome must be classified differently.

**Acceptance Scenarios**:

1. **Given** a lane exceeds its configured time budget, **When** the budget expires, **Then** the lane is stopped, marked as timed out, and the summary records the budget, elapsed time, last known activity, and log location.
2. **Given** a lane exits with a validation failure, **When** the lane ends, **Then** the result is marked as failed rather than timed out or canceled.
3. **Given** the operator cancels a run, **When** the runner stops remaining work, **Then** completed lanes keep their actual results and uncompleted lanes are marked canceled, skipped, or not-run with a reason.

---

### User Story 3 - Preserve Reviewable Evidence (Priority: P3)

A reviewer can inspect a compact validation summary and drill into per-lane evidence without reading interleaved console output or guessing whether a caveat was hidden.

**Why this priority**: Readiness evidence must remain honest. Targeted substitute gates, canceled aggregate checks, and optional lane failures need to be visible in review artifacts.

**Independent Test**: Can be tested by running a mixed result set with required, optional, skipped, failed, timed-out, no-progress-timeout, canceled, infrastructure-error, and substitute lanes, then verifying that the human-readable and machine-readable summaries agree.

**Acceptance Scenarios**:

1. **Given** multiple lanes run in one validation session, **When** the session ends, **Then** the summary points to separate evidence for each lane and does not require reading interleaved logs.
2. **Given** a required lane is skipped because a prerequisite is unavailable, **When** the summary is produced, **Then** the overall readiness result is not marked successful and the skip reason is visible.
3. **Given** a targeted lane substitutes for an incomplete aggregate lane, **When** readiness is summarized, **Then** the substitute is identified as a substitute and the incomplete aggregate lane remains visible.

---

### User Story 4 - Avoid Validation Output Races (Priority: P4)

A contributor can run the documented lane set without accidental file-lock or overwrite failures caused by two lanes sharing the same generated output location.

**Why this priority**: The retrospective recorded a validation race when two test commands for the same project and configuration ran concurrently. The lane runner should prevent this known workflow trap.

**Independent Test**: Can be tested by configuring two lanes that would share output and confirming the runner serializes them, isolates them, or refuses the unsafe schedule with an actionable message.

**Acceptance Scenarios**:

1. **Given** two lanes would use the same generated output location, **When** both are requested together, **Then** the runner prevents concurrent writes to that location.
2. **Given** an unsafe lane schedule is rejected, **When** the operator reads the error, **Then** it identifies the conflicting lanes and the action needed to proceed.

### Edge Cases

- Unknown lane names are rejected before any validation work begins.
- Duplicate lane names or duplicate result identifiers are reported as configuration errors.
- A lane that produces no visible progress before timeout is still stopped and classified.
- A lane that cannot write its log or evidence directory fails with an infrastructure error before reporting validation success.
- A canceled run preserves completed lane results and clearly marks unstarted or interrupted lanes as canceled, skipped, or not-run.
- Optional lane failures do not mark required readiness as successful, but they remain visible to reviewers.
- Required lane skips are treated as readiness blockers unless explicitly documented as environment-limited.
- Re-running a lane does not overwrite previous evidence without leaving a distinct run record or visible replacement notice.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a documented set of named validation lanes covering at least build verification, package/library validation, Controls validation, rendering or harness validation, package-consuming sample validation, and aggregate solution validation.
- **FR-002**: Operators MUST be able to run all required lanes or select one or more named lanes without editing repository files.
- **FR-003**: Each lane MUST declare whether it is required, optional, or informational for readiness review.
- **FR-004**: Each lane MUST have a declared time budget, and timeout results MUST include the budget, elapsed time, lane name, and last known activity.
- **FR-005**: Each lane run MUST produce lane-specific evidence that includes operator-visible output, detailed logs, elapsed time, status, and failure details when applicable.
- **FR-006**: The system MUST produce both a concise human-readable summary and a structured run record for every validation session.
- **FR-007**: Lane outcomes MUST distinguish passed, failed, timed-out, no-progress-timeout, canceled, skipped, infrastructure-error, environment-limited, and not-run states.
- **FR-008**: The overall readiness result MUST be unsuccessful when any required lane fails, times out, has no progress before the no-progress budget, is canceled, is skipped or not run without an accepted environment limitation, is environment-limited without accepted limitation, or has an infrastructure error.
- **FR-009**: Optional and informational lane results MUST be included in summaries but MUST NOT override required lane outcomes.
- **FR-010**: The system MUST report progress during long-running lanes often enough that an operator can identify the active lane and last visible activity within 60 seconds.
- **FR-011**: The system MUST prevent unsafe concurrent lane execution when lanes would share generated output locations, either by serializing them, isolating their outputs, or rejecting the schedule with an actionable error.
- **FR-012**: The system MUST preserve evidence for each lane separately so reviewers can inspect one lane without searching through unrelated lane output.
- **FR-013**: The system MUST record canceled or incomplete aggregate validation as incomplete, even when targeted substitute lanes pass.
- **FR-014**: Existing validation checks MUST remain directly runnable outside the lane runner so the feature does not remove established contributor workflows.
- **FR-015**: The feature MUST NOT change public framework behavior or runtime product behavior.
- **FR-016**: The feature MUST include validation evidence for pass, fail, total-timeout, no-progress-timeout, cancellation, skipped or not-run, unknown lane, infrastructure-error, environment-limited, and unsafe-concurrency scenarios.

### Key Entities

- **Validation Lane**: A named validation unit with purpose, readiness role, time budget, expected evidence, and any scheduling constraints.
- **Validation Session**: One operator-requested run containing selected lanes, start and end times, overall outcome, and references to produced evidence.
- **Lane Result**: The outcome of one lane within a session, including status, elapsed time, reason, progress markers, and evidence locations.
- **Lane Evidence**: Logs, result files, summaries, and diagnostic artifacts produced by a lane.
- **Run Policy**: The readiness role, timeout, cancellation behavior, progress expectations, and concurrency constraints that govern lane execution.
- **Validation Summary**: The reviewer-facing and structured record that aggregates lane results without hiding incomplete or substituted gates.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A maintainer can run a single documented lane or the required lane set with one documented operator action and see a final summary within 10 seconds after the last lane finishes.
- **SC-002**: 100% of lane results include lane name, readiness role, status, elapsed time, evidence location, and a reason for any non-passing outcome.
- **SC-003**: A deliberately non-completing lane is stopped within its configured time budget plus 30 seconds and is marked timed-out or no-progress-timeout in both summary formats.
- **SC-004**: Reviewers can identify the first failed, timed-out, no-progress-timeout, canceled, skipped, not-run, environment-limited, or infrastructure-error required lane from the concise summary in under 1 minute without reading detailed logs.
- **SC-005**: Running the documented required lane set produces separate evidence for at least five named lanes and one aggregate summary.
- **SC-006**: No documented lane pair can create a generated-output file lock or overwrite race when requested together; unsafe pairs are serialized, isolated, or rejected before concurrent execution starts.
- **SC-007**: The aggregate summary never reports readiness success when any required lane did not pass, verified across pass, fail, total-timeout, no-progress-timeout, cancellation, skipped or not-run, environment-limited, and infrastructure-error scenarios.
- **SC-008**: Existing direct validation workflows remain available, confirmed by documenting the lane runner as an orchestration layer rather than a replacement.

## Assumptions

- "Next item" refers to the next unimplemented retrospective follow-up after Features 163, 164, and 165: the validation lane runner with timeouts and progress logging.
- This feature is a Tier 1 contracted validation tooling change: it changes the repository-maintainer validation contract in `Rendering.Harness.ValidationLanes` and the validation CLI, but does not change public `FS.GG.UI.*` runtime package behavior.
- Package-feed validation, visual-readiness helpers, render/layout inspection metadata, responsiveness diagnostics, and input/render scheduler rewrites remain separate features unless a lane only invokes their existing checks.
- The full aggregate solution check remains useful as an optional or informational signal, but required readiness should come from named lanes with clearer evidence.
- Lane definitions may evolve over time, but each active lane must keep a documented purpose, owner-facing name, readiness role, timeout, and evidence expectations.
- Environment-limited validation is acceptable only when the limitation is visible in the summary and does not silently convert a missing required check into success.
