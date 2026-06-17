# Feature Specification: Overlay Visual Proof

**Feature Branch**: `145-overlay-visual-proof`

**Created**: 2026-06-17

**Status**: Draft

**Input**: User description: "start next item in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md"

## Context

The active radical rendering report says P0 through P4 are implemented and P5 interaction work has
advanced through Feature 144. Feature 144 completed the pure overlay coordinator, transient widget
metadata, host and runtime routing seams, product-owned date-picker flow, deterministic overlay
corpus evidence, and unsupported-host visual-proof disclosure.

The report names the next action as running real offscreen visual proof on a display/GL-capable host
before continuing into render-anywhere or compositor work. This feature closes that readiness gap:
maintainers need a real visual artifact proving that integrated overlays appear above covered
content, route hits consistently with the visual order, and leave no stale visible surface after
dismissal. When a capable host is unavailable, the system must keep reporting a limitation rather
than converting synthetic or no-display evidence into a visual pass.

This is a Tier 2 validation and readiness feature. It must not change product-facing overlay
behavior, public control APIs, portable scene serialization, browser rendering, compositor
promotion, damage-scissored presentation, intrinsic layout, text shaping, text editing, selection
editing, or widget catalog behavior. If completing the proof requires a public contract or package
surface change, the change must be reclassified as Tier 1 before implementation continues.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture Real Overlay Visual Proof (Priority: P1)

A framework maintainer runs the overlay visual-proof validation on a host that can produce real
offscreen frames. The run creates inspectable visual artifacts and evidence records proving overlay
order, hit-order correlation, and final closed-state cleanup for the representative Feature 144
interaction flow.

**Why this priority**: This is the explicit next action in the rendering architecture report and the
only remaining Feature 144 readiness caveat. Without a real artifact, P5 remains behaviorally
validated but not visually proven on a capable host.

**Independent Test**: Run the visual-proof validation on a capable host and inspect the produced
evidence. The test passes only when the artifacts are real, non-empty, tied to the expected overlay
scenario, and show the required open and closed states.

**Acceptance Scenarios**:

1. **Given** a host capable of producing real offscreen frames, **When** the maintainer runs the
   overlay visual-proof validation, **Then** the run records real visual artifacts for the selected
   overlay scenario and marks the visual-proof status as passed.
2. **Given** a transient surface is open above covered content, **When** the visual proof captures
   the open state, **Then** the artifact shows the surface above the covered content and the
   evidence names the matching topmost hit target.
3. **Given** the interaction flow dismisses the transient surface, **When** the final state is
   captured, **Then** the artifact shows no stale overlay content and no stale hit target remains
   active.
4. **Given** a visual artifact is blank, zero-sized, missing, stale from a previous run, or not tied
   to the current scenario, **When** validation evaluates the run, **Then** the visual-proof status
   fails rather than passing.

---

### User Story 2 - Preserve Honest Unsupported-Host Reporting (Priority: P1)

A maintainer runs the same validation on a host that cannot produce real offscreen frames. The run
does not claim visual success, does not fabricate visual proof, and preserves a clear limitation
record that names the owner, cause, and next proof path.

**Why this priority**: Feature 144 already disclosed unsupported-host visual proof honestly. This
feature must replace that caveat only when real proof exists; otherwise it must keep the limitation
visible and actionable.

**Independent Test**: Run the validation on a known unsupported host and verify that no real visual
pass is claimed, no synthetic image is accepted, and the limitation record contains the required
diagnostic information.

**Acceptance Scenarios**:

1. **Given** the host cannot create a real offscreen frame, **When** visual-proof validation runs,
   **Then** the result is reported as environment-limited rather than passed.
2. **Given** an environment-limited result, **When** readiness evidence is reviewed, **Then** it
   names the owner, host limitation, next proof path, and reason the behavioral evidence remains
   separate from visual proof.
3. **Given** synthetic fixtures, deterministic logs, or no-display fallback evidence are available,
   **When** visual-proof validation evaluates success, **Then** those artifacts are not accepted as
   a substitute for a real visual artifact.

---

### User Story 3 - Correlate Visual Proof With Existing Behavioral Evidence (Priority: P2)

A framework maintainer reviews the visual-proof run alongside Feature 144 behavioral evidence. The
visual artifact, scenario identity, input sequence, open or closed state, hit decision, focus result,
and dispatch evidence are connected closely enough that a reviewer can determine whether the pixels
match the behavior already proven by deterministic tests.

**Why this priority**: A screenshot by itself is weak evidence. The value comes from tying the image
to the deterministic overlay corpus, so visual order, hit order, focus, and product dispatch can be
reviewed together.

**Independent Test**: Run the representative overlay flow and verify that every visual artifact is
associated with a scenario name, expected state, hit-order decision, focus state, and product
dispatch summary.

**Acceptance Scenarios**:

1. **Given** a captured open-state artifact, **When** a reviewer opens its evidence record, **Then**
   the record identifies the overlay scenario, input step, expected open surface, topmost hit target,
   and focus state.
2. **Given** a captured selection or dismissal step, **When** evidence is reviewed, **Then** the
   visual artifact is tied to exactly one product-visible selection or close request when the flow
   expects one.
3. **Given** visual proof and behavioral evidence disagree, **When** validation evaluates the run,
   **Then** the proof fails and the discrepancy is reported as an overlay validation issue rather
   than hidden in readiness notes.

---

### User Story 4 - Close or Preserve the P5 Readiness Caveat (Priority: P2)

A maintainer uses the new readiness record to decide whether the Feature 144 visual-proof caveat is
closed. If real proof passed, P5 is no longer blocked on visual evidence. If the host remains
unsupported, the readiness record keeps the caveat open with a concrete next step.

**Why this priority**: The report recommends closing this proof path before continuing the later
render-anywhere and compositor workstreams. The outcome needs to be clear enough for planning.

**Independent Test**: Review the readiness files after both capable-host and unsupported-host runs.
Confirm that the status, artifact paths, limitation details, and next-workstream guidance are
unambiguous.

**Acceptance Scenarios**:

1. **Given** real visual proof has passed, **When** readiness is reviewed, **Then** the record
   identifies the artifacts and states that the Feature 144 visual-proof caveat is closed.
2. **Given** real visual proof is environment-limited, **When** readiness is reviewed, **Then** the
   record keeps the caveat open and repeats the next proof path.
3. **Given** a later workstream is about to start, **When** maintainers check readiness, **Then** they
   can determine within minutes whether the visual-proof gate is closed or still environment-gated.

### Edge Cases

- A host reports visual capability but fails while creating or presenting the offscreen frame.
- A run produces a file that is blank, transparent, zero-sized, unreadable, or left over from an
  earlier run.
- A visual artifact captures the open surface but the hit-order evidence names a different topmost
  target.
- The open-state proof passes but the final closed-state proof still shows stale overlay pixels or
  stale hit targets.
- The validation host has different display scale, font availability, color profile, or windowing
  behavior from a previous capable host.
- The unsupported-host path has deterministic behavioral evidence but no real visual artifact.
- A failure could be caused by environment setup, visual capture, overlay behavior, or artifact
  bookkeeping; the report must distinguish these categories.
- The feature is attempted in an environment where real visual proof cannot be produced during local
  implementation.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a real visual-proof validation path for the integrated overlay
  flow delivered by Feature 144.
- **FR-002**: The validation path MUST determine and report whether the current host can produce
  real offscreen visual artifacts before claiming visual success.
- **FR-003**: A capable-host run MUST produce inspectable, non-empty visual artifacts for at least
  one representative open overlay state and one final closed state.
- **FR-004**: The open-state visual artifact MUST prove that the transient surface appears above
  covered content and is associated with the topmost eligible hit target.
- **FR-005**: The final closed-state visual artifact MUST prove that dismissed overlay content is no
  longer visible or hit-testable.
- **FR-006**: Each visual artifact MUST be associated with scenario identity, input step, expected
  overlay state, hit decision, focus state, and product dispatch summary.
- **FR-007**: Validation MUST fail when required artifacts are missing, blank, zero-sized, stale,
  unreadable, or disconnected from the current scenario.
- **FR-008**: Unsupported-host runs MUST report an environment-limited status with owner, cause, next
  proof path, and trust rationale for any separate behavioral evidence.
- **FR-009**: Synthetic fixtures, deterministic logs, placeholders, and unsupported-host limitation
  records MUST NOT be accepted as real visual proof.
- **FR-010**: Repeated equivalent capable-host runs MUST produce stable scenario names, pass/fail
  decisions, evidence labels, and readiness status.
- **FR-011**: Visual proof MUST be reviewed alongside existing deterministic overlay behavior,
  routing, focus, dispatch, and rendering-parity evidence.
- **FR-012**: A mismatch between visual artifacts and behavioral evidence MUST fail readiness with a
  diagnostic category that distinguishes environment failure, capture failure, overlay behavior
  failure, and evidence bookkeeping failure.
- **FR-013**: Readiness records MUST state whether the Feature 144 visual-proof caveat is closed or
  remains environment-gated.
- **FR-014**: The feature MUST NOT change product-facing overlay behavior, public control APIs,
  portable scene serialization, browser rendering, compositor behavior, intrinsic layout, text
  shaping, text editing, selection editing, or widget catalog behavior.
- **FR-015**: Any unavoidable public contract, package surface, or compatibility change MUST be
  documented as a Tier 1 reclassification before implementation proceeds.

### Key Entities

- **Visual proof run**: A validation execution that attempts to prove overlay behavior using real
  visual artifacts and records whether the host was capable.
- **Host capability result**: The readiness status that distinguishes capable-host proof, unsupported
  environment limitation, and validation failure.
- **Visual artifact**: A human-inspectable output from the real rendering host for a specific overlay
  scenario state.
- **Overlay scenario**: The representative interaction flow whose open, hit, dispatch, focus, and
  closed states are being proven.
- **Evidence record**: The reviewable metadata that connects a visual artifact to scenario identity,
  expected state, hit decision, focus result, product dispatch, and readiness status.
- **Readiness caveat**: The Feature 144 visual-proof limitation that is either closed by real proof
  or preserved with a next proof path.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: On a capable host, 100% of selected visual-proof scenarios produce non-empty real
  visual artifacts and complete evidence records.
- **SC-002**: The proof covers at least one open overlay above covered content, the associated
  topmost hit target, and one final closed state with no stale overlay content.
- **SC-003**: Unsupported-host runs accept 0 synthetic, placeholder, or deterministic-log-only
  artifacts as real visual proof and always produce an environment-limited readiness record.
- **SC-004**: Three repeated capable-host runs produce identical scenario names, pass/fail decisions,
  evidence labels, and readiness status.
- **SC-005**: A maintainer can determine from the readiness records in under 5 minutes whether the
  Feature 144 visual-proof caveat is closed or still environment-gated.
- **SC-006**: The feature completes with no product-facing overlay behavior changes and no public
  control API changes unless a Tier 1 reclassification is recorded first.

## Assumptions

- Feature 144 behavioral validation, routing evidence, deterministic corpus evidence, and rendering
  parity evidence are already available and remain the behavioral baseline for this feature.
- The first proof scope is a representative overlay scenario tied to Feature 144 readiness, not an
  exhaustive visual sweep of every transient widget category.
- Real visual proof may require a host with display and GL renderer support that is not available in
  every local or continuous-validation environment.
- When a capable host is unavailable, completing implementation may still leave the readiness caveat
  open until the validation is run in the required environment.
- Environment-specific visual differences are acceptable only when the pass/fail decision and
  evidence labels remain stable and the visual artifact still proves the required overlay states.
