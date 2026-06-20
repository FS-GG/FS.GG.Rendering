# Feature Specification: Automated Control Pass for the Second AntShowcase

**Feature Branch**: `176-test-antshowcase-controls`

**Created**: 2026-06-20

**Status**: Draft

**Input**: User description: "test automatically without user input every control in second antshowcase for visual fidelity and correct and full functionality. fix any problems. write a detailed and comprehensive report of all framework/library related problems/possible improvements in docs/reports."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Every control is exercised automatically and parity is proven (Priority: P1)

A maintainer runs a single automated pass that drives every control in the Second AntShowcase
without any human at the keyboard. The pass exercises each interactive control through its full
range of behavior (click, toggle, type, drag, select, navigate, expand, scroll, etc.) and inspects
every control — interactive and display-only — for visual fidelity. The pass produces a per-control
record stating what was expected, what was observed, and a pass/fail/needs-review verdict, so the
maintainer can see at a glance that the whole catalog behaves correctly.

**Why this priority**: This is the core of the request. Without automated, no-input exercise of every
control and a per-control verdict, there is no way to claim the showcase is fully functional. It is
the minimum viable deliverable and everything else builds on the evidence it produces.

**Independent Test**: Run the automated control pass against the current showcase build and confirm
it emits one classified record per cataloged control (interactive families fully exercised,
display-only controls visually inspected) with no control left unclassified and no human input
required.

**Acceptance Scenarios**:

1. **Given** the full Second AntShowcase catalog, **When** the automated control pass runs end to end,
   **Then** every cataloged control produces exactly one verdict record (pass / fail / needs-review)
   and none are left "unexercised" or "unclassified".
2. **Given** an interactive control with multiple behaviors (e.g. a slider that drags and a switch that
   toggles), **When** the pass exercises it, **Then** each documented behavior of that control is driven
   and its resulting state change is asserted, not just a single representative action.
3. **Given** the pass runs twice on the same build, **When** the two runs are compared, **Then** the
   functional verdicts and captured evidence are deterministic (byte-stable where the framework
   guarantees determinism).
4. **Given** the pass runs in an environment that cannot present a live window, **When** live exercise
   is attempted, **Then** the affected records are marked "environment-limited" rather than silently
   passing or failing.

---

### User Story 2 - Visual fidelity of every control is captured and reviewable (Priority: P1)

The maintainer needs proof that each control *looks* right — correct appearance in both light/dark
appearances and both representative sizes, correct interaction-state styling (hover, focus, active,
selected, disabled, error), and damage-local repaint when state changes. The automated pass captures
visual evidence for each control and surfaces a fidelity verdict, flagging anything that looks wrong
(missing affordance, wrong palette, clipped content, misalignment) for review.

**Why this priority**: "Visual fidelity" is half of the explicit request and is the dimension most
likely to hide regressions that functional assertions miss. It must ship with US1 to satisfy the
brief; functional correctness without visual correctness is not "fully functional".

**Independent Test**: Run the pass and confirm it produces, for each control, visual evidence across
the required appearance/size matrix plus interaction-state captures, each carrying a fidelity verdict
(approved / needs-review / blocked) with reasons attached to any non-approved verdict.

**Acceptance Scenarios**:

1. **Given** any cataloged control, **When** the pass captures its appearance, **Then** evidence exists
   for both appearances (light and dark) and both representative sizes.
2. **Given** an interactive control, **When** the pass drives it into each of its supported interaction
   states, **Then** the resulting visual state (hover/focus/active/selected/disabled/error as
   applicable) is captured and verified to differ from the resting state.
3. **Given** a control whose state changes, **When** it is re-rendered, **Then** repaint is damage-local
   (only the affected region changes) and this is recorded as evidence.
4. **Given** a control that fails visual fidelity (e.g. no hover affordance, clipped text, wrong
   contrast), **When** the pass evaluates it, **Then** the record is marked needs-review/fail with a
   specific, human-readable reason.

---

### User Story 3 - Problems found are fixed and re-verified (Priority: P2)

For every defect the automated pass surfaces — a control that does not respond, a missing visual
affordance, a wrong state transition, a clipping or contrast problem — the maintainer fixes it and the
pass re-verifies the fix. Sample-local defects are fixed in the showcase; defects that originate in the
shared control/framework surface are fixed at the appropriate layer (or, when a fix is too risky to
land in this feature, recorded as a tracked follow-up). The finding log shows each problem moving from
"found" to "fixed and re-verified" (or "deferred with rationale").

**Why this priority**: "Fix any problems" is explicit, but it depends on US1/US2 first surfacing the
problems. It is P2 because the discovery+evidence capability is independently valuable even before any
fix lands, and the set of fixes is unknown until the pass runs.

**Independent Test**: Take any defect recorded by the pass, apply its fix, re-run the relevant slice of
the pass, and confirm the finding log transitions that defect to "fixed and re-verified" with fresh
evidence — or to "deferred" with an explicit rationale and a follow-up reference.

**Acceptance Scenarios**:

1. **Given** a defect surfaced by the pass, **When** a fix is applied, **Then** re-running the pass shows
   that control passing and the finding log links the before/after evidence.
2. **Given** a defect that originates in the shared control/framework surface, **When** it is fixed,
   **Then** the fix is made at the shared layer (with the visibility/contract and test obligations that
   implies) rather than papered over in the sample.
3. **Given** a defect that cannot be safely fixed within this feature, **When** it is deferred, **Then**
   the finding log records why and points to a tracked follow-up, and the control is marked accordingly
   rather than as a silent pass.
4. **Given** all in-scope fixes are applied, **When** the full pass is re-run, **Then** no interactive
   control remains non-functional and no control regresses relative to the pre-fix baseline.

---

### User Story 4 - Comprehensive framework/library report is delivered (Priority: P2)

The maintainer receives a single, detailed report under `docs/reports/` that consolidates every
framework- and library-level problem and improvement opportunity discovered during the pass:
authoring friction, missing or awkward APIs, surfaces where the sample had to work around the
framework, visual-state or input gaps, testing-helper limitations, determinism caveats, and concrete
recommendations. The report distinguishes sample-local issues from framework/library issues and
prioritizes the framework improvements.

**Why this priority**: The report is an explicit deliverable and is the durable, cross-feature value of
the exercise (it feeds future framework work). It is P2 because it is authored from the findings the
pass produces, so it follows US1–US3.

**Independent Test**: Open the report in `docs/reports/` and confirm it lists framework/library problems
and improvement opportunities, each with evidence/reference, severity, sample-vs-framework
classification, and a recommendation — and that it covers the categories the pass exercised.

**Acceptance Scenarios**:

1. **Given** the completed pass, **When** the report is written, **Then** it resides under
   `docs/reports/` following the repository's report naming convention and links back to this feature.
2. **Given** a framework/library problem discovered during the pass, **When** it is recorded in the
   report, **Then** it carries a severity, a sample-vs-framework classification, supporting evidence,
   and a concrete recommendation.
3. **Given** the report is complete, **When** a reader scans it, **Then** framework/library improvements
   are clearly separated from sample-local fixes and ordered by priority/impact.

---

### Edge Cases

- **Display-only controls**: Controls with no interactive behavior (typography, badges, static charts,
  layout primitives) must be visually inspected and explicitly classified as display-only, never
  reported as "failed functionality" for lacking a state change.
- **Controls with overlay/transient surfaces** (tooltip, popover, drawer, dialog, tour, toast): the pass
  must drive the trigger and verify the transient surface appears/dismisses, not just the trigger
  control.
- **Controls requiring continuous input** (slider drag, scroll thumb drag): the pass must verify
  continuous feedback (no catch-up lag, offset tracks input), not only the start/end states.
- **No-overflow / empty states**: scroll regions with no overflow, empty lists/grids, and disabled
  controls must render their correct "nothing to do" affordance without error.
- **Environment cannot present a window**: live-only checks must degrade to an explicit
  "environment-limited" outcome with a non-zero, well-defined signal rather than a false pass.
- **Non-deterministic surfaces** (animation clocks, time pickers): the pass must pin time so evidence is
  reproducible, or explicitly mark the surface as time-dependent.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST exercise every cataloged control in the Second AntShowcase through an
  automated pass that requires no human input at run time.
- **FR-002**: For each interactive control, the pass MUST drive every documented behavior of that control
  (not merely one representative action) and assert the resulting functional state change.
- **FR-003**: For every control (interactive and display-only), the pass MUST capture visual evidence
  across both appearances (light/dark) and both representative sizes.
- **FR-004**: For each interactive control, the pass MUST drive and capture each supported interaction
  state (hover, focus, active, selected/checked, disabled, error/validation as applicable) and verify
  each differs from the resting state.
- **FR-005**: The pass MUST verify that state-change repaints are damage-local (only the affected region
  re-renders) and record this as evidence.
- **FR-006**: The pass MUST emit exactly one classified verdict record per cataloged control, leaving no
  control unexercised or unclassified; display-only controls MUST be classified as such with a reason.
- **FR-007**: The functional verdicts and captured evidence MUST be deterministic across repeated runs on
  the same build (byte-stable where the framework guarantees determinism); time- and animation-dependent
  surfaces MUST be pinned or explicitly flagged.
- **FR-008**: When the runtime environment cannot present a live window, the pass MUST mark affected
  records "environment-limited" with a well-defined signal, never a silent pass or fail.
- **FR-009**: The system MUST maintain a finding log that records each discovered defect and its lifecycle
  state (found → fixed-and-re-verified, or deferred-with-rationale).
- **FR-010**: Every defect surfaced by the pass MUST be fixed and re-verified, OR explicitly deferred with
  a documented rationale and a tracked follow-up reference.
- **FR-011**: Defects originating in the shared control/framework surface MUST be fixed at the appropriate
  shared layer (honoring the repository's visibility-in-`.fsi`, contract, and test-evidence obligations),
  not worked around solely in the sample.
- **FR-012**: After all in-scope fixes, the full pass MUST show no interactive control non-functional and
  no regression versus the pre-fix baseline.
- **FR-013**: The system MUST produce a comprehensive framework/library report under `docs/reports/`,
  following the repository's report naming convention and linking back to this feature.
- **FR-014**: The report MUST record each framework/library problem and improvement opportunity with a
  severity, a sample-vs-framework classification, supporting evidence/reference, and a concrete
  recommendation, with framework improvements separated from sample-local fixes and ordered by priority.
- **FR-015**: The pass MUST drive transient/overlay surfaces (tooltip, popover, drawer, dialog, tour,
  toast, popconfirm) via their triggers and verify appearance and dismissal of the transient surface.

### Key Entities

- **Control Verdict Record**: One per cataloged control. Captures control id, family, interactive vs
  display-only classification, behaviors exercised, expected vs observed result, functional verdict, and
  visual-fidelity verdict.
- **Visual Evidence Item**: A captured appearance of a control for a given appearance × size × interaction
  state, with a fidelity verdict (approved / needs-review / blocked) and reasons for non-approval.
- **Finding**: A discovered defect. Captures description, affected control(s), sample-vs-framework
  classification, severity, lifecycle state (found / fixed-and-re-verified / deferred), and links to
  before/after evidence.
- **Framework/Library Report**: The consolidated `docs/reports/` document aggregating findings classified
  as framework/library, with severities, recommendations, and prioritization.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of cataloged Second AntShowcase controls produce a classified verdict record; 0 controls
  are left unexercised or unclassified.
- **SC-002**: 100% of interactive controls have every documented behavior exercised and asserted (verified
  against the control inventory, not a representative subset).
- **SC-003**: 100% of controls have visual evidence across both appearances and both representative sizes;
  100% of interactive controls have evidence for each of their supported interaction states.
- **SC-004**: The pass is fully unattended — it completes start to finish with zero human input events.
- **SC-005**: Repeated runs on the same build yield identical functional verdicts and byte-stable evidence
  for all surfaces the framework guarantees as deterministic.
- **SC-006**: 100% of defects surfaced by the pass reach a terminal lifecycle state (fixed-and-re-verified
  or deferred-with-rationale); 0 defects are left in an open/unaddressed state at feature completion.
- **SC-007**: After fixes, 0 interactive controls remain non-functional and 0 controls regress versus the
  pre-fix baseline.
- **SC-008**: A single framework/library report exists under `docs/reports/` covering every exercised
  category, with each entry carrying severity, classification, evidence, and a recommendation.

## Assumptions

- **"Every control"** means every control cataloged in the Second AntShowcase control inventory
  (the catalog pages plus the controls reachable from template pages); both interactive and display-only
  controls are in scope, with interactive controls assessed for functionality and all controls assessed
  for visual fidelity.
- **"Without user input"** means the pass is driven entirely by deterministic, scripted input — the
  repository's existing headless input-scripting and live-responsiveness mechanisms — with no human at
  the keyboard or mouse during the run.
- **Visual fidelity is captured automatically; final aesthetic sign-off may remain a human review step.**
  The pass automates the *exercising* and *capture* and applies programmatic checks (state differs,
  damage-local, palette/contrast where checkable); subjective approval can still be a review verdict on
  the captured evidence.
- **Fixes land where the defect lives**: sample-local defects are fixed in the showcase; shared-surface
  defects are fixed in the framework/controls layer when low-risk within this feature, otherwise deferred
  with a tracked follow-up and documented in the report.
- **The report follows existing `docs/reports/` conventions** (timestamped/descriptive filename, the same
  structural style as prior framework retrospectives) and is authored from the pass's findings.
- **Existing testing infrastructure is reused**: the deterministic input scripting, control/visual/retained
  inspection helpers, responsiveness runner, and validation-lane runner are the mechanisms used to drive
  and assert the pass rather than building new bespoke harnesses.
- **Two appearances and two representative sizes** are the visual matrix, consistent with the showcase's
  existing visual-readiness evidence convention.
