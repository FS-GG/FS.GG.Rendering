# Phase 1 Data Model: Automated Control Pass for the Second AntShowcase

**Feature**: `176-test-antshowcase-controls` | **Date**: 2026-06-20

Entities, validation rules, and lifecycle transitions for the control pass. These describe the
*shape of the evidence* the runner produces; they reuse existing `FS.GG.UI.*` artifact types
(`VisualInspectionArtifact`, `RetainedInspectionArtifact`, `VisualCaptureTarget`,
`VisualReadinessReport`) rather than redefining them — the entities below are the sample-local
records that compose those artifacts into a per-control verdict.

---

## Entity: Control Verdict Record

One per cataloged control. The atomic unit of "every control was exercised and classified".

| Field | Type | Notes |
|-------|------|-------|
| `ControlId` | string | Catalog id from `CoverageMap` (e.g. `slider`, `switch`, `line-chart`). |
| `Family` | string | Interaction/display family (e.g. `selection-toggles`, `charts-statistical`). |
| `PageContext` | string list | Page(s) the control was exercised on (catalog page; template pages if reachable). |
| `Classification` | `Interactive` \| `DisplayOnly` | Interactive iff ≥1 documented behavior (D2). |
| `ClassificationReason` | string | Required for `DisplayOnly` (why it has no interactive behavior). |
| `BehaviorsExercised` | BehaviorOutcome list | One per documented behavior (empty for display-only). |
| `InteractionStates` | InteractionStateOutcome list | hover/focus/active/selected/disabled/error captured. |
| `VisualEvidence` | VisualEvidenceItem list | appearance × size × state captures (see entity below). |
| `DamageEvidence` | DamageOutcome list | retained-diff per state transition (FR-005). |
| `FunctionalVerdict` | `Pass` \| `Fail` \| `NeedsReview` \| `EnvironmentLimited` \| `NotApplicable` | `NotApplicable` only for display-only functional dimension. |
| `VisualVerdict` | `Approved` \| `NeedsReview` \| `Blocked` \| `EnvironmentLimited` | Fidelity verdict. |
| `Findings` | string list | Finding ids linked to this control (see Finding entity). |
| `Diagnostics` | string list | Notes, environment caveats, time-dependent flags. |

**BehaviorOutcome**: `{ BehaviorId; Description; Expected; Observed; Verdict }` where `Verdict ∈
{ Pass; Fail; NeedsReview; EnvironmentLimited }`.

**InteractionStateOutcome**: `{ State (Hover|Focus|Active|Selected|Disabled|Error); DiffersFromRest:
bool; EvidenceRef; Verdict }`.

**DamageOutcome**: `{ TransitionId; DamageStatus (Empty|Localized|Broad|FullSurface|Unsupported);
DirtyPercentage; AffectedRegionIds; Verdict }` — sourced from `DamageRegionInspection`.

### Validation rules

- **VR-1 (completeness)**: The set of `ControlId` across all records MUST equal
  `CoverageMap.catalogIds ()` exactly — no missing, no duplicate (FR-006, SC-001).
- **VR-2 (classified)**: Every record has a non-empty `Classification`; every `DisplayOnly` record
  has a non-empty `ClassificationReason`. No record may carry `Unexercised`/`Unclassified`.
- **VR-3 (behavior coverage)**: For `Interactive` records, `BehaviorsExercised` covers every
  documented behavior in the control's interaction contract (FR-002, SC-002).
- **VR-4 (state delta)**: Each `InteractionStateOutcome` with a supported state has
  `DiffersFromRest = true` or a `Fail`/`NeedsReview` verdict with a reason (FR-004).
- **VR-5 (verdict shape)**: `FunctionalVerdict = NotApplicable` is permitted only when
  `Classification = DisplayOnly`.

---

## Entity: Visual Evidence Item

A captured appearance of a control for one appearance × size × interaction-state cell. Wraps the
existing `VisualCaptureRecord` / `VisualInspectionArtifact`.

| Field | Type | Notes |
|-------|------|-------|
| `TargetId` | string | `controlId/appearance/size/state` deterministic id. |
| `Appearance` | `antLight` \| `antDark` | Both required per control (FR-003). |
| `Size` | `preferred` \| `minimum` | 1600×1000 and 1280×800 (FR-003). |
| `State` | `Rest` \| interaction state | `Rest` for display-only; interaction states for interactive. |
| `CapturePath` | string | Relative path under `readiness/visual-evidence/`. |
| `CaptureStatus` | `Complete` \| `Missing` \| `WrongSize` \| `Undecodable` \| `Degraded` \| `Blocked` | From `VisualCompleteness`. |
| `FidelityVerdict` | `Approved` \| `NeedsReview` \| `Blocked` | Reasons required when not `Approved`. |
| `Reasons` | string list | Human-readable cause for non-approved fidelity (missing affordance, clipped, wrong contrast…). |

### Validation rules

- **VR-6 (matrix coverage)**: Every control has evidence for both appearances × both sizes at `Rest`
  (FR-003, SC-003); every interactive control additionally has evidence for each supported
  interaction state (FR-004, SC-003).
- **VR-7 (reasoned non-approval)**: Any `FidelityVerdict ≠ Approved` carries ≥1 reason (US2 AC4).
- **VR-8 (degradation)**: `CaptureStatus = Degraded/Blocked` maps the item's verdict to
  `EnvironmentLimited`, never silent `Approved` (FR-008).

---

## Entity: Finding

A discovered defect and its lifecycle. Shared schema with the readiness finding log.

| Field | Type | Notes |
|-------|------|-------|
| `FindingId` | string | Stable id (e.g. `F176-007`). |
| `Description` | string | What is wrong. |
| `AffectedControls` | string list | Catalog ids. |
| `Classification` | `SampleLocal` \| `FrameworkShared` | Where the defect lives (FR-011). |
| `Tier` | `Tier1` \| `Tier2` | Tier1 iff fix touches public `FS.GG.UI.*` surface. |
| `Severity` | `Critical` \| `High` \| `Medium` \| `Low` | Triage. |
| `Lifecycle` | `Found` \| `FixedAndReVerified` \| `Deferred` | Terminal = `FixedAndReVerified` or `Deferred`. |
| `BeforeEvidence` | string | Ref to pre-fix verdict/visual evidence. |
| `AfterEvidence` | string option | Ref to post-fix re-verification (required when `FixedAndReVerified`). |
| `DeferralRationale` | string option | Required when `Deferred`. |
| `FollowUpRef` | string option | Tracked follow-up (required when `Deferred`). |

### Lifecycle transitions

```text
Found ──fix applied + pass re-run shows control passing──▶ FixedAndReVerified   (AfterEvidence required)
Found ──cannot safely fix in this feature───────────────▶ Deferred             (DeferralRationale + FollowUpRef required)
```

### Validation rules

- **VR-9 (terminal)**: At feature completion every Finding is `FixedAndReVerified` or `Deferred`;
  zero remain `Found` (FR-009, FR-010, SC-006).
- **VR-10 (shared-layer fix)**: A `FrameworkShared` finding that is fixed has `Tier = Tier1` and its
  fix carries `.fsi` + surface-baseline + test obligations (FR-011).
- **VR-11 (re-verify evidence)**: `FixedAndReVerified` requires both `BeforeEvidence` and
  `AfterEvidence` (US3 AC1).
- **VR-12 (no regression)**: After all in-scope fixes, no interactive control is non-functional and
  no record regresses vs the pre-fix baseline (FR-012, SC-007).

---

## Entity: Framework/Library Report

The consolidated `docs/reports/` document (one file). Aggregates `FrameworkShared` findings plus
authoring-friction and improvement opportunities.

| Field (section) | Notes |
|-----------------|-------|
| Lead metadata | Report date/UTC, author, source merge SHA, status. |
| Executive summary | 1–2 line impact statement. |
| Part-structured findings | Each: Evidence / Root cause / Impact / Mitigation / Recommendation + Effort/Risk. |
| Prioritisation table | ID, Item, Severity, Effort, Leverage. |
| Phased roadmap | Phase A/B/C with dependencies. |
| Evidence appendix | Anchors to readiness artifacts and code (file:line). |

### Validation rules

- **VR-13 (location/convention)**: Resides under `docs/reports/` with the feature-scoped filename
  convention and links back to this feature (FR-013, SC-008).
- **VR-14 (entry completeness)**: Every framework/library entry carries severity, sample-vs-framework
  classification, supporting evidence/reference, and a concrete recommendation (FR-014).
- **VR-15 (separation/order)**: Framework improvements are visually separated from sample-local
  fixes and ordered by priority/impact (FR-014, US4 AC3).
- **VR-16 (coverage)**: The report covers every category the pass exercised (SC-008).

---

## Supporting types (reused, not redefined)

| Type | Owner | Role in the pass |
|------|-------|------------------|
| `InputScript` / `InputStep` | `Rendering.Harness.Input` | Scripted unattended input per behavior/state. |
| `VisualInspectionArtifact` | `FS.GG.UI.Scene` / `ControlInspection` | Per-state structural+visual facts. |
| `RetainedInspectionArtifact` / `DamageRegionInspection` | `FS.GG.UI.Scene` | State-transition + damage-locality evidence. |
| `VisualCaptureTarget` / `VisualReadinessReport` | `FS.GG.UI.Testing` | Appearance × size matrix + completeness. |
| `DiagnosticSummary` / `ViewerWindowStateDiagnostic` | `FS.GG.UI.Diagnostics` / `SkiaViewer` | Environment-limited detection. |
| `LaneResult` / `OverallReadiness` | `Rendering.Harness.ValidationLanes` | Optional lane integration for the pass run. |

## State of a Control Verdict Record (lifecycle)

```text
Unexercised ──runner drives behaviors + captures evidence──▶ Exercised ──classify + verdict──▶ Classified
                                                                   │
                                              no live window ──────┴──▶ EnvironmentLimited (explicit, non-silent)
```

A record may only be emitted in `Classified` or `EnvironmentLimited` state; `Unexercised` is never
a terminal emitted state (VR-2).
