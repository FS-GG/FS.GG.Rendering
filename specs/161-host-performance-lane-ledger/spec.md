# Feature Specification: Host Performance Lane Ledger

**Feature Branch**: `161-host-performance-lane-ledger`

**Created**: 2026-06-18

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

This specification covers Feature 161 from the radical rendering architecture report: the host performance lane ledger. Feature 155 accepted current-host partial-redraw correctness, Feature 157 accepted the no-clear damage-scissored readiness slice, Feature 158 separated proof readback from timing, Feature 159 recorded layer-promotion and content/placement reuse evidence, and Feature 160 accepted bounded performance validation throughput. This feature scopes performance evidence to explicit host lanes by recording the display, renderer, driver, refresh, package, load, and environment facts needed to prevent timing results from being generalized beyond the lane that produced them.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Complete Host Lane Facts (Priority: P1)

Release reviewers need every compositor timing run to carry complete host facts, so they can tell which display and GPU lane produced the evidence before considering any performance claim.

**Why this priority**: The report explicitly blocks the shipped compositor performance claim until host-lane scoping exists. Without complete lane facts, timing evidence cannot distinguish the accepted X11 direct-rendering lane from Wayland, indirect rendering, missing display, or software-raster environments.

**Independent Test**: Can be tested by assembling readiness for a timing run and verifying that the host lane ledger records all required facts or rejects the run with a reviewer-visible reason.

**Acceptance Scenarios**:

1. **Given** a same-profile timing run completes on a capable host, **When** readiness is assembled, **Then** the ledger records display server, display identity, renderer identity, direct rendering status, refresh rate or reason unavailable, driver identity, package versions, CPU/GPU load notes, environment limits, host profile, run identity, scenario identity, timing policy identity, collection time, and artifact locations.
2. **Given** any required host fact is missing, contradictory, unreadable, or stale, **When** readiness is assembled, **Then** the timing run is not accepted as host-scoped performance evidence and the missing fact is named.
3. **Given** historical timing evidence exists from prior P7 features, **When** the Feature 161 ledger is assembled, **Then** the summary states whether that evidence is confirmed, superseded, contextual only, or unusable for lane-scoped performance acceptance.

---

### User Story 2 - Scope Performance Claims to Known Lanes (Priority: P1)

Package consumers and maintainers need any compositor performance claim to name the exact host lane it applies to, so a result from one lane is not treated as a universal rendering guarantee.

**Why this priority**: The current accepted lane is X11 `:1` with direct OpenGL on AMD Radeon/Mesa. The report says results from that lane must not be generalized to Wayland, indirect GL, missing-display, or software-raster hosts.

**Independent Test**: Can be tested by reviewing the readiness summary and verifying that it names the accepted lane, lists non-generalized lanes, and refuses cross-lane aggregation.

**Acceptance Scenarios**:

1. **Given** timing evidence is accepted for one host lane, **When** the performance claim status is evaluated, **Then** the claim scope names that lane and states that other lanes are not covered unless separately accepted.
2. **Given** timing artifacts come from different display servers, renderers, direct-rendering modes, driver identities, package versions, or host profiles, **When** readiness is assembled, **Then** those artifacts are not combined into one accepted performance result.
3. **Given** a user inspects the readiness summary, **When** they compare the measured lane with their own environment, **Then** they can decide whether the performance evidence applies to their lane without reading raw artifacts.

---

### User Story 3 - Fail Closed for Unsupported or Mismatched Lanes (Priority: P1)

Maintainers need unsupported, mismatched, noisy, cross-profile, or environment-limited performance runs to fail closed, so correctness remains accepted while unsupported performance claims remain unaccepted.

**Why this priority**: Host-lane scoping is a claim-discipline feature. It must not weaken the safe full-redraw fallback, correctness acceptance, unsupported-host behavior, or the existing rule that noisy timing cannot create a shipped performance claim.

**Independent Test**: Can be tested by assembling readiness for unsupported, missing-display, indirect-rendering, software-raster, mismatched-profile, stale-version, and noisy timing cases and verifying that each records zero accepted lane-scoped performance artifacts.

**Acceptance Scenarios**:

1. **Given** a host has no usable display, indirect rendering, software rasterization, or unknown renderer facts, **When** timing readiness is assembled, **Then** the result is environment-limited or fallback-only with zero accepted performance artifacts.
2. **Given** timing remains noisy for an otherwise known lane, **When** the ledger is complete, **Then** the lane facts are preserved but the shipped compositor performance claim remains `performance-not-accepted`.
3. **Given** host facts change between proof, timing, and readiness assembly, **When** the evidence is evaluated, **Then** the run is rejected or split by lane rather than accepted as one comparable result.

---

### User Story 4 - Publish Reviewer-Readable Lane Evidence (Priority: P2)

Release reviewers need one readiness entry point that explains host lane facts, excluded evidence, environment limits, prior-gate status, compatibility impact, and final claim status.

**Why this priority**: Feature 161 completes the report-defined performance claim scoping gate. The evidence must be quick to audit and durable enough for future users to understand why a claim is accepted, scoped, or still unaccepted.

**Independent Test**: Can be tested by opening the readiness summary and verifying that a reviewer can determine lane completeness, applicable scope, excluded evidence, prior P7 gate status, and final performance claim status from one entry point.

**Acceptance Scenarios**:

1. **Given** Feature 161 readiness is assembled, **When** a reviewer opens the summary, **Then** it lists lane facts, accepted and rejected runs, missing facts, environment limits, prior performance gates, compatibility impact, artifact locations, and final claim status.
2. **Given** package or compatibility output changes are needed to expose lane status, **When** validation completes, **Then** those changes are documented, validated, and tied to the readiness summary.
3. **Given** the final performance claim remains unaccepted, **When** readiness is assembled, **Then** the summary names the remaining blockers instead of implying that host-lane scoping alone proves a speedup.

### Edge Cases

- A timing run records display facts but omits renderer, driver, refresh-rate, package-version, or load facts.
- Host facts are collected before or after the run but do not match the timing artifact's run identity.
- Display server changes between proof, timing, and readiness assembly.
- Direct rendering is unavailable, false, or not verifiable.
- The renderer reports a software raster path, indirect GL, a missing display, or a virtualized environment.
- Refresh rate is unknown, variable, or inconsistent with timing samples.
- Multiple GPUs, driver layers, or display devices are present and the measured device is ambiguous.
- CPU/GPU load, thermal throttling, power mode, or concurrent workloads make the timing lane noisy or non-representative.
- Package versions, surface baselines, scenario definitions, policy ids, or readiness artifacts differ between timing runs being compared.
- Existing accepted correctness evidence exists for the host but performance timing remains noisy.
- Unsupported-host validation runs in the same checkout as accepted-host artifacts.
- A future host lane has complete facts but lacks Feature 159 reuse evidence, Feature 160 throughput evidence, or non-noisy timing.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST maintain a host performance lane ledger for every timing run considered for P7 compositor performance acceptance.
- **FR-002**: Each ledger entry MUST record display server, display identity, renderer identity, direct rendering status, refresh rate or reason unavailable, driver identity, package version set, CPU/GPU load notes, known environment limits, host profile, run identity, scenario identity, timing policy identity, collection time, and artifact locations.
- **FR-003**: Timing evidence with missing, ambiguous, contradictory, stale, unreadable, or cross-run host facts MUST NOT be accepted as lane-scoped performance evidence.
- **FR-004**: The readiness summary MUST identify the current accepted host lane as X11 `:1` with direct OpenGL on AMD Radeon/Mesa only when the collected facts confirm that lane for the accepted timing run.
- **FR-005**: The readiness summary MUST state that evidence from the current accepted lane is not generalized to Wayland, indirect GL, missing-display, software-raster, virtualized, or otherwise different host lanes unless those lanes have separately accepted evidence.
- **FR-006**: Timing artifacts from different display servers, renderer identities, direct-rendering modes, driver identities, package version sets, host profiles, scenario definitions, timing policies, or run identities MUST NOT be combined into one accepted performance result.
- **FR-007**: Every rejected, excluded, environment-limited, fallback-only, noisy, or non-comparable timing run MUST record a reviewer-visible reason.
- **FR-008**: The ledger MUST tie each timing run to the relevant prior P7 gates: correctness acceptance, damage-scissored readiness, proof/readback separation, reuse and promotion evidence, and throughput evidence.
- **FR-009**: The shipped compositor performance claim MUST remain `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 reuse and promotion counters are net-positive, Feature 160 throughput is accepted, and Feature 161 host-lane facts are complete and scoped for the claimed lane.
- **FR-010**: Unsupported hosts, unavailable presentation environments, indirect rendering, software rasterization, cross-profile evidence, stale package versions, and missing host facts MUST produce zero accepted lane-scoped performance artifacts.
- **FR-011**: The ledger MUST preserve complete host facts even when the performance claim remains unaccepted, so rejected or noisy runs remain auditable.
- **FR-012**: The feature MUST preserve existing P7 correctness acceptance, proof/readback separation, layer-promotion evidence, throughput evidence, safe full-redraw fallback, unsupported-host fail-closed behavior, package validation, and public-surface drift checks.
- **FR-013**: The feature MUST be classified as Tier 1 because it changes consumer-visible performance readiness semantics and may add package-facing diagnostics, compatibility notes, validation routes, or artifact inspection.
- **FR-014**: Public compatibility changes are allowed only when needed to expose host lane facts, lane completeness, exclusion reasons, performance claim scope, readiness status, artifact inspection, or package-facing validation; any such change MUST be documented and validated.

### Key Entities *(include if feature involves data)*

- **Host Performance Lane**: A specific environment category for timing evidence, defined by display server, display identity, renderer, direct-rendering mode, driver, refresh behavior, package versions, host profile, scenario policy, and environment notes.
- **Host Fact Set**: The required facts that make a timing run comparable and reviewable for a lane.
- **Lane Ledger Entry**: The durable record connecting one timing run to its host facts, run identity, scenario identity, policy identity, package versions, load notes, environment limits, artifacts, inclusion status, and exclusion reason when applicable.
- **Timing Run**: A measured compositor performance run that may be accepted, rejected, noisy, environment-limited, fallback-only, or contextual for a host lane.
- **Claim Scope**: The reviewer-visible statement of which host lane a performance result applies to and which lanes are not covered.
- **Environment Limit**: A host condition that prevents accepted performance evidence, such as missing display, indirect rendering, software rasterization, unavailable renderer facts, virtualized presentation, or non-comparable load.
- **Package Version Set**: The package and source identity associated with a timing run, used to prevent stale or mixed-version evidence from being accepted.
- **Load Note**: The recorded CPU/GPU load, thermal, power, or concurrent-workload context that helps reviewers interpret timing noise.
- **Readiness Summary**: The review entry point that aggregates lane entries, exclusions, prior P7 gate status, compatibility impact, artifacts, and final performance claim status.
- **Performance Claim Status**: The reviewer-visible outcome that remains unaccepted until timing, reuse, throughput, and host-lane gates are all satisfied for a named lane.

### Scope and Classification

- In scope: host lane facts, lane ledger entries, claim scope statements, cross-lane rejection, environment-limit reporting, load notes, package-version comparability, prior-gate linkage, readiness summaries, compatibility notes, unsupported-host behavior, and package-facing validation.
- Out of scope: changing compositor rendering behavior, broadening accepted host support, accepting noisy timing, replacing full solution validation, changing correctness proof requirements, changing proof/readback separation, changing layer-promotion behavior, changing performance throughput policy, changing P8 layout acceptance, changing text shaping, or changing overlay behavior.
- Expected classification: Tier 1, because the feature changes consumer-visible performance readiness semantics and can add diagnostics, compatibility notes, validation status, or artifact inspection.
- Public surface changes are allowed only when needed for host lane facts, completeness status, exclusion reasons, claim scope, readiness status, artifact inspection, or compatibility evidence.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of timing runs considered by Feature 161 readiness have either a complete host fact set or a reviewer-visible exclusion reason.
- **SC-002**: 0 timing runs with missing, ambiguous, contradictory, stale, unreadable, cross-run, or cross-lane host facts are counted as accepted lane-scoped performance evidence.
- **SC-003**: 0 timing artifacts from different host lanes are combined into one accepted performance result.
- **SC-004**: 100% of accepted lane ledger entries include display server, display identity, renderer identity, direct rendering status, refresh rate or reason unavailable, driver identity, package version set, CPU/GPU load notes, known environment limits, host profile, run identity, scenario identity, policy identity, collection time, and artifact locations.
- **SC-005**: The readiness summary states the current accepted lane and lists non-generalized lanes in a form a reviewer can interpret in under 5 minutes.
- **SC-006**: Unsupported-host, missing-display, indirect-rendering, software-raster, and unknown-renderer validation records zero accepted lane-scoped performance artifacts.
- **SC-007**: 100% of noisy but otherwise complete lane entries preserve their host facts while keeping the shipped compositor performance claim `performance-not-accepted`.
- **SC-008**: The final performance claim status remains `performance-not-accepted` unless same-profile timing is not noisy, Feature 159 reuse and promotion counters are net-positive, Feature 160 throughput is accepted, and Feature 161 host-lane facts are complete for the claimed lane.
- **SC-009**: Existing Feature 155 correctness readiness, Feature 157 damage-scissored readiness, Feature 158 readback-free timing separation, Feature 159 reuse/promotion evidence, Feature 160 throughput evidence, full-redraw fallback, and unsupported-host fail-closed behavior remain valid.
- **SC-010**: Focused lane-ledger, unsupported-host, package, compatibility, and full validation evidence pass with zero undocumented consumer-visible drift before the feature is marked ready for implementation closeout.

## Assumptions

- "Next item" refers to the first unchecked item in the report's feature-level tracker and planned P7 performance follow-up list: Feature 161, host performance lane ledger.
- Feature 155 and Feature 157 correctness evidence remain the safety baseline for accepted partial-redraw and no-clear damage-scissored behavior.
- Feature 158 remains the measurement-policy baseline; proof/readback probe evidence must not be counted as accepted timing evidence.
- Feature 159 remains the reuse and promotion evidence baseline; this feature records lane scope but does not change layer-promotion or content/placement reuse behavior.
- Feature 160 throughput remains accepted for the current host profile; this feature adds host-lane scoping before a final performance claim can be considered.
- The current lane described by the report is X11 `:1` with direct OpenGL on AMD Radeon/Mesa and stable profile `probe-08a47c01`.
- Full redraw remains the safe fallback for every frame and every unsupported or non-comparable host lane.
- Exact command names, filter names, artifact filenames, field encoding, and validation task order are planning details for the next phase.
