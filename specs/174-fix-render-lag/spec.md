# Feature Specification: Fix Render Lag

**Feature Branch**: `174-fix-render-lag`

**Created**: 2026-06-19

**Status**: Draft

**Input**: User description: "implement performance fixes"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Immediate Control Feedback (Priority: P1)

As a person using the desktop showcase, I want button activations and similar
small interactions to show their visual result without a visible pause, so the
application feels responsive during normal use.

**Why this priority**: Small model-changing interactions are the most common
responsiveness path and currently expose the lag even when no layout work is
required.

**Independent Test**: Run the representative button activation scenario after
the first frame is visible and verify that the visible follow-up frame appears
within the responsiveness budget while preserving the same visible result.

**Acceptance Scenarios**:

1. **Given** the showcase is open on the button page after initial rendering,
   **When** the user activates the primary button, **Then** the updated visible
   state appears without a perceptible stall.
2. **Given** the same button activation is measured repeatedly in a live desktop
   session, **When** the follow-up frame is reported, **Then** the measurement
   identifies input handling, model update, frame preparation, painting, and
   presentation as separate contributions.
3. **Given** the optimized interaction completes, **When** visual and interaction
   parity checks are run, **Then** the button remains visually equivalent and
   activates the same user action as before.

---

### User Story 2 - Fast Page Navigation (Priority: P2)

As a person reviewing the showcase, I want navigation between pages with dense
content to complete quickly, so comparing controls does not require waiting
after each page change.

**Why this priority**: Page changes currently demonstrate the largest
input-to-visible delay and include both content changes and retained visual
reuse.

**Independent Test**: Run the representative navigation scenario from the
current page to the text and numeric input page and verify that the next visible
frame meets the navigation responsiveness budget.

**Acceptance Scenarios**:

1. **Given** the showcase is open on a catalog page, **When** the user navigates
   to the text and numeric input page, **Then** the destination page becomes
   visibly available within the navigation budget.
2. **Given** the destination page contains a larger nested visual tree, **When**
   the page-change frame is prepared, **Then** stable chrome and unchanged
   regions do not cause repeated full-scene work that dominates the frame.
3. **Given** the navigation scenario completes, **When** visual evidence and
   interaction routing are checked, **Then** the destination page retains the
   same visible content and controls as before.

---

### User Story 3 - Reliable Performance Regression Evidence (Priority: P3)

As a maintainer, I want repeatable performance evidence for representative
interactive frames, so future changes cannot reintroduce second-scale stalls
without being detected.

**Why this priority**: The root cause was only isolated after adding phase-level
diagnostics; the fix needs durable evidence, not a one-off manual trace.

**Independent Test**: Run the performance validation workflow on the button and
page-change scenarios and confirm it reports latency budgets, phase attribution,
visual parity, and any environment limitation explicitly.

**Acceptance Scenarios**:

1. **Given** a machine with a supported live desktop rendering environment,
   **When** the performance validation runs, **Then** it produces actionable
   latency records for the representative interactions.
2. **Given** the live environment is unavailable, **When** validation is
   requested, **Then** the result is classified as environment-limited rather
   than reported as a passing responsiveness result.
3. **Given** the optimized rendering path is measured, **When** a future change
   regresses beyond the accepted budget, **Then** the validation fails or reports
   a clear regression requiring review.

### Edge Cases

- Dense nested pages with many unchanged parent containers must not recreate a
  second-scale stall while only a small visible region changes.
- Pages with no replayable or cacheable boundaries must still improve; the fix
  cannot depend on optional replay hits being present.
- Theme changes, animations, overlays, scroll viewers, and data-rich controls
  must retain their visual behavior even when performance metadata is optimized.
- Large page navigation may still include meaningful paint cost; the evidence
  must distinguish paint cost from pre-paint frame preparation so follow-up work
  is not misdiagnosed.
- Unsupported or headless rendering environments must produce an explicit
  limitation instead of synthetic live-performance claims.

## Requirements *(mandatory)*

### Change Classification

- **Tier**: Tier 2 internal performance change.
- **Public API impact**: No public API or package contract changes are expected.
  If planning discovers that a public surface must change, the feature must be
  reclassified as Tier 1 and include surface-baseline and migration work.

### Functional Requirements

- **FR-001**: The system MUST reduce input-to-visible latency for representative
  small model-changing interactions after initial rendering.
- **FR-002**: The system MUST reduce input-to-visible latency for representative
  page navigation with dense nested content.
- **FR-003**: The optimized path MUST preserve the same visible output,
  interaction targets, accessibility-facing metadata, and user-observable
  behavior for covered scenarios.
- **FR-004**: Performance-related metadata and evidence collection MUST scale
  with the changed or required visual work rather than repeatedly processing the
  same full visual content on every affected parent.
- **FR-005**: The first-frame preparation path SHOULD improve where it shares
  the same costly work as interactive frame preparation.
- **FR-006**: The system MUST provide phase-attributed responsiveness evidence
  that separates input handling, model update, frame preparation, paint, and
  presentation latency.
- **FR-007**: The system MUST retain explicit environment-limited reporting when
  live performance evidence cannot be collected.
- **FR-008**: The feature MUST include automated regression coverage for the
  representative button activation and page navigation scenarios.
- **FR-009**: The feature MUST include parity checks showing that performance
  improvements do not change the rendered result or routing behavior.
- **FR-010**: The feature MUST avoid adding new external dependencies unless the
  plan reclassifies the work and justifies the dependency.

### Key Entities

- **Representative Interaction Scenario**: A repeatable user action used to
  measure responsiveness, such as button activation or page navigation.
- **Latency Record**: A measured result that reports end-to-end visible response
  time plus separately named phase contributions.
- **Parity Evidence**: Proof that the optimized path preserves visual output,
  interaction routing, and user-observable behavior.
- **Environment Limitation**: A classified condition explaining why live
  performance evidence could not be collected in the current environment.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In a supported live desktop session, the representative button
  activation shows its follow-up visual state within 150 ms for the median run
  and within 250 ms for at least 95% of accepted runs.
- **SC-002**: In a supported live desktop session, representative page navigation
  shows the destination page within 250 ms for the median run and within 500 ms
  for at least 95% of accepted runs.
- **SC-003**: The largest non-paint preparation contribution for each
  representative interaction is reduced by at least 80% from the documented
  2026-06-19 baseline.
- **SC-004**: The initial visible frame preparation time is reduced by at least
  50% from the documented 2026-06-19 baseline where the same bottleneck is
  present.
- **SC-005**: Visual and interaction parity checks for the optimized scenarios
  pass with no intentional user-visible changes other than improved response
  time.
- **SC-006**: Performance validation produces a clear pass, fail, or
  environment-limited result for every required scenario; no required scenario
  is silently skipped.

## Assumptions

- The primary target is the interactive frame-preparation path exercised by the
  Second Ant Showcase live desktop scenarios diagnosed on 2026-06-19.
- The button activation and page navigation traces from 2026-06-19 are the
  baseline for percentage-improvement criteria.
- Existing rendering semantics, event routing, accessibility-facing metadata,
  diagnostics, package identities, and public surfaces remain unchanged.
- A supported live desktop rendering environment is required for final
  responsiveness claims; unavailable environments may only produce classified
  limitation evidence.
- Large paint costs discovered after frame preparation is fixed are treated as a
  separate follow-up unless they are caused by the same repeated preparation
  work.
