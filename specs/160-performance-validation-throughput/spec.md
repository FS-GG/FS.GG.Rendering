# Feature Specification: Performance Validation Throughput

**Feature Branch**: `160-performance-validation-throughput`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers Feature 160 from the radical rendering architecture report: performance validation throughput. Feature 155 accepted current-host partial-redraw correctness, Feature 157 accepted the no-clear damage-scissored readiness slice, Feature 158 separated proof readback from timing, and Feature 159 added layer-promotion and content/placement reuse evidence. This feature keeps full solution validation as the release gate while adding a focused, bounded performance validation lane so repeated timing iterations do not depend on long-running broad regression suites.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Repeat Focused Performance Iterations Quickly (Priority: P1)

Maintainers need a focused performance validation lane that can be repeated during compositor timing work without waiting for the full broad regression suite each time.

**Why this priority**: The report identifies performance validation throughput as the next P7 blocker. If every timing iteration waits on broad regression wall time, maintainers cannot converge on a non-noisy same-profile performance result efficiently.

**Independent Test**: Can be tested by running the focused performance lane repeatedly for the accepted host profile and verifying that each accepted iteration stays within the declared bound while recording scenario coverage, samples, exclusions, and final status.

**Acceptance Scenarios**:

1. **Given** a maintainer starts a focused P7 performance iteration on the accepted host profile, **When** the focused validation lane completes, **Then** the result reports duration, bound, scenario coverage, sample count, exclusions, host profile, and acceptance status.
2. **Given** broad regression suites would take longer than the focused iteration bound, **When** the focused lane runs, **Then** it does not wait for those broad suites and records that broad validation remains a separate release gate.
3. **Given** a focused iteration exceeds its declared bound, is canceled, or produces only partial evidence, **When** readiness is assembled, **Then** that iteration is excluded from throughput acceptance with a reviewer-visible reason.

---

### User Story 2 - Preserve Full Validation as the Release Gate (Priority: P1)

Release reviewers need focused performance iteration to remain separate from full solution validation, so faster timing loops cannot accidentally replace the release readiness gate.

**Why this priority**: Feature 160 is meant to speed repeated timing work, not weaken quality gates. Full validation still protects broad behavior outside the focused compositor lane.

**Independent Test**: Can be tested by assembling readiness with passing focused evidence but missing or failing full validation and verifying that the feature is not marked release-ready.

**Acceptance Scenarios**:

1. **Given** focused performance validation passes, **When** full solution validation is missing, failing, interrupted, or stale, **Then** the feature readiness summary refuses release-ready status and explains the blocker.
2. **Given** full solution validation passes after focused performance evidence is collected, **When** readiness is assembled, **Then** the summary reports both the focused throughput result and the full validation result as separate evidence.
3. **Given** full validation exposes unrelated drift or regression, **When** focused performance evidence is otherwise accepted, **Then** the drift remains a closeout blocker until it is resolved or explicitly deferred by the feature artifacts.

---

### User Story 3 - Keep Performance Scenario Coverage Comparable (Priority: P2)

Reviewers need focused timing iterations to cover the same declared P7 performance scenario categories as prior accepted evidence unless exclusions are explicit, so faster validation remains comparable.

**Why this priority**: A faster lane is useful only if it still exercises the scenario categories needed for the P7 performance acceptance rule. Silent scenario narrowing would create misleading evidence.

**Independent Test**: Can be tested by comparing a focused iteration summary with the declared P7 scenario set and verifying that every category is covered or has an exclusion that prevents acceptance.

**Acceptance Scenarios**:

1. **Given** the focused lane runs against the declared P7 performance scenarios, **When** readiness is assembled, **Then** the summary shows coverage for each required scenario category and identifies any missing or excluded category.
2. **Given** scenario names, sample policy, warmup policy, host profile, or acceptance rules change from prior evidence, **When** comparison is requested, **Then** the summary states whether the new run supersedes, confirms, or cannot be compared to prior evidence.
3. **Given** an iteration omits a required scenario without an accepted exclusion, **When** throughput readiness is evaluated, **Then** the iteration is not accepted.

---

### User Story 4 - Publish Reviewer-Readable Throughput Evidence (Priority: P2)

Release reviewers and package consumers need one readiness entry point that explains focused iteration bounds, actual durations, excluded evidence, broad validation status, host scope, and final performance claim status.

**Why this priority**: Feature 160 changes how performance evidence is collected and reviewed. The readiness package must make the faster lane auditable rather than hiding risk behind a shorter command.

**Independent Test**: Can be tested by opening the readiness summary and verifying that a reviewer can decide whether throughput is accepted, whether full validation passed, and why the performance claim status was chosen.

**Acceptance Scenarios**:

1. **Given** focused iterations and full validation have run, **When** readiness is assembled, **Then** one summary lists iteration durations, declared bounds, scenario coverage, exclusions, full validation status, host profile, artifact locations, and final claim status.
2. **Given** focused iterations pass but timing remains noisy or host-lane scoping is incomplete, **When** readiness is assembled, **Then** throughput may be accepted while the shipped compositor performance claim remains `performance-not-accepted`, with the noisy timing called out as a remaining performance-claim gate rather than an exclusion reason by itself.
3. **Given** the host is unsupported or presentation is unavailable, **When** the focused lane runs, **Then** it fails closed within the declared bound and records zero accepted same-profile performance artifacts.

### Edge Cases

- Focused performance validation passes but full solution validation is missing, stale, interrupted, or failing.
- Full solution validation passes but focused iterations are timed out, partial, cross-profile, or missing scenario coverage.
- Focused throughput is accepted while noisy same-profile timing still prevents shipped compositor performance claim acceptance.
- A focused run produces partial samples before hitting its declared time bound.
- Scenario labels, sample policy, warmup policy, or acceptance thresholds change from prior Feature 156, Feature 158, or Feature 159 evidence.
- Focused evidence is accidentally mixed with artifacts from a different host profile, package version, renderer identity, run identity, or scenario definition.
- Unsupported-host validation runs in the same checkout as accepted-host performance artifacts.
- A faster focused lane hides a broad regression unless full validation is reviewed separately.
- Feature 161 host performance lane ledger remains incomplete after Feature 160 throughput is accepted.
- Existing proof, package, compatibility, or public-surface validation exposes undocumented drift while validation throughput is being added.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST define a focused performance validation lane that is separate from full solution validation.
- **FR-002**: The focused lane MUST declare a per-iteration time bound before acceptance evidence is collected.
- **FR-003**: A focused iteration that exceeds its declared bound, is canceled, or produces partial evidence MUST be classified as excluded evidence with a reviewer-visible reason and MUST NOT be accepted as throughput evidence.
- **FR-004**: The focused lane MUST run without invoking the broad release validation gate as part of each repeated timing iteration.
- **FR-005**: Full solution validation MUST remain the release gate and MUST NOT be replaced by focused performance validation.
- **FR-006**: Readiness MUST report focused throughput status and full validation status independently.
- **FR-007**: The feature MUST refuse release-ready status when full validation is missing, failing, interrupted, stale, or undocumented, even if focused performance validation passes. Full validation is stale when its recorded commit, validation command, package/surface baseline, or readiness artifact set does not match the implementation state being marked ready.
- **FR-008**: Focused iterations MUST cover the declared P7 performance scenario categories unless a missing category is explicitly excluded and prevents acceptance.
- **FR-009**: The focused iteration artifact schema MUST include duration, declared bound, scenario coverage, sample count, inclusion status, exclusion reason when applicable, host profile, run identity, scenario identity, and artifact locations.
- **FR-010**: Cross-profile, stale, mixed-policy, missing-metadata, timed-out, canceled, partial, unsupported-host, environment-limited, scenario-coverage-missing, sample-policy-mismatch, run-identity-mismatch, artifact-unreadable, or readback-contaminated focused iterations MUST NOT be accepted.
- **FR-011**: Accepted throughput readiness MUST include at least three fresh focused iterations from the same host profile that all complete within the declared bound. A fresh iteration is produced for the current implementation state, package/surface baseline, policy id, scenario definition id, and readiness run.
- **FR-012**: The readiness package MUST distinguish throughput acceptance from performance claim acceptance.
- **FR-013**: The shipped compositor performance claim MUST remain `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 reuse and promotion counters are net-positive, Feature 160 throughput is accepted, and the report-defined host-lane scoping gate is satisfied.
- **FR-014**: Unsupported hosts, unavailable presentation environments, cross-profile evidence, or missing host facts MUST produce zero accepted same-profile performance artifacts.
- **FR-015**: The feature MUST preserve existing P7 correctness acceptance, proof/readback separation, safe full-redraw fallback, unsupported-host fail-closed behavior, package validation, and public-surface drift checks.
- **FR-016**: The feature MUST be classified as Tier 1 because it changes consumer-visible validation readiness semantics and may add package-facing diagnostics, validation routes, or compatibility notes.
- **FR-017**: Public compatibility changes are allowed only when needed to expose focused validation status, throughput bounds, exclusion reasons, readiness status, artifact inspection, or package-facing validation; any such change MUST be documented and validated.

### Key Entities *(include if feature involves data)*

- **Focused Performance Lane**: The bounded validation path used for repeated P7 timing iterations without waiting for the broad release gate.
- **Broad Release Gate**: The full solution validation required before the feature can be marked release-ready.
- **Validation Bound**: The declared maximum duration for a focused iteration before its evidence is excluded.
- **Focused Iteration**: One same-profile run of the focused performance lane, including duration, scenario coverage, samples, exclusions, and artifacts.
- **Scenario Coverage**: The declared P7 performance scenario categories exercised by a focused iteration.
- **Excluded Evidence**: A focused iteration or sample rejected because it is timed out, partial, canceled, cross-profile, stale, mixed-policy, missing metadata, unsupported, environment-limited, scenario-coverage-missing, sample-policy-mismatch, run-identity-mismatch, artifact-unreadable, readback-contaminated, or otherwise non-comparable. Noisy same-profile timing is reported as a remaining performance-claim gate but is not an exclusion reason by itself.
- **Throughput Readiness Result**: The accepted, rejected, environment-limited, fallback-only, or blocked status for Feature 160 validation throughput.
- **Host Profile**: The stable presentation environment identity used to decide whether performance evidence is comparable.
- **Readiness Summary**: The review entry point that aggregates focused iterations, bounds, exclusions, broad validation, host scope, artifacts, compatibility impact, and final claim status.
- **Performance Claim Status**: The reviewer-visible outcome that remains unaccepted until timing, reuse, throughput, and host-lane gates are all satisfied.

### Scope and Classification

- In scope: focused performance validation throughput, declared iteration bounds, timeout and partial-result exclusion, comparable scenario coverage, separation from full solution validation, readiness summaries, unsupported-host behavior, compatibility notes, and package-facing validation.
- Out of scope: accepting a final compositor performance claim by itself, replacing full solution validation, changing the correctness proof requirement, changing proof/readback separation, changing layer-promotion or content/placement reuse behavior, adding the full host performance lane ledger, broadening accepted host support, changing P8 layout acceptance, changing text shaping, or changing overlay behavior.
- Expected classification: Tier 1, because the feature changes consumer-visible validation readiness semantics and may add package-facing diagnostics, validation status, compatibility notes, or artifact inspection.
- Public surface changes are allowed only when needed for focused validation status, throughput bounds, exclusion reasons, readiness status, artifact inspection, or package-facing validation.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Accepted Feature 160 throughput readiness includes at least 3 fresh same-profile focused iterations that each complete in under 10 minutes.
- **SC-002**: 100% of focused iteration artifacts conform to the FR-009 schema, including duration, declared bound, scenario coverage, sample count, host profile, run identity, inclusion status, exclusion reason when applicable, and artifact location.
- **SC-003**: 100% of required P7 performance scenario categories are covered by each accepted focused iteration, or the iteration is excluded with a reviewer-visible reason.
- **SC-004**: 0 focused iterations carrying an exclusion reason are counted as accepted throughput evidence.
- **SC-005**: 100% of readiness summaries report focused throughput status and full validation status as separate decisions.
- **SC-006**: 0 readiness summaries mark the feature release-ready when full solution validation is missing, failing, interrupted, stale, or undocumented.
- **SC-007**: A reviewer can determine focused throughput status, broad release-gate status, scenario coverage, excluded evidence, host scope, compatibility impact, artifact paths, and final performance claim status from one summary in under 5 minutes.
- **SC-008**: Unsupported-host validation completes in under 2 minutes and records zero accepted same-profile performance artifacts.
- **SC-009**: Existing Feature 155 correctness readiness, Feature 157 damage-scissored readiness, Feature 158 readback-free measurement separation, Feature 159 reuse/promotion evidence, full-redraw fallback, and unsupported-host fail-closed behavior remain valid.
- **SC-010**: Focused throughput, unsupported-host, package, compatibility, and full validation evidence pass with zero undocumented consumer-visible drift before the feature is marked ready for implementation closeout.
- **SC-011**: The shipped compositor performance claim remains `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 records net-positive reuse and promotion counters, Feature 160 throughput is accepted, and the report-defined host-lane scoping gate is satisfied.

## Assumptions

- "Next item" refers to the first unchecked item in the report's feature-level tracker and planned P7 performance follow-up list: Feature 160, performance validation throughput.
- Feature 155 and Feature 157 correctness evidence remain the safety baseline for partial-redraw and no-clear damage-scissored behavior.
- Feature 158 remains the measurement-policy baseline; proof/readback probe evidence must not be counted as accepted timing evidence.
- Feature 159 remains the reuse and promotion evidence baseline; this feature does not change layer-promotion or content/placement reuse behavior.
- Full solution validation is still required before closeout, but repeated timing iterations should use a focused bounded lane.
- The accepted host profile remains the current same-profile P7 performance lane unless later evidence explicitly changes it.
- Feature 161 host performance lane ledger remains separate follow-up work. Feature 160 can accept validation throughput without accepting a final shipped compositor performance claim.
- Exact command names, filter names, artifact filenames, and validation task order are planning details for the next phase.
