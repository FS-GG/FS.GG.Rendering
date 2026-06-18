# Data Model: Complete P8 Layout Acceptance

## Representative Corpus Case

**Purpose**: One accepted layout or ScrollViewer scenario used to prove P8 breadth.

**Fields**:

- `CaseId`: stable identifier used by tests and readiness ledgers.
- `Category`: layout, ScrollViewer, invalid/diagnostic, dynamic content, cache reuse, or regression.
- `TreeShape`: root, children, nesting, visibility, order, layering, clipping, and scroll structure.
- `Inputs`: constraints, viewport, content identity, layout intent, measurement behavior, child
  order, visibility, and intrinsic dependencies.
- `ExpectedBounds`: deterministic bounds for required participants.
- `ExpectedPlacements`: deterministic child placement and visibility records.
- `ExpectedScrollExtent`: viewport, content extent, and max offsets when the case includes scroll.
- `ExpectedDiagnostics`: required diagnostic codes/severities/messages for invalid, fallback, stale,
  unsupported, or environment-limited behavior.
- `Verdict`: accepted, failed, skipped, environment-limited, or blocked.
- `EvidencePath`: test name, command, or readiness file that proves the verdict.

**Validation rules**:

- Every required case has expected bounds, placements, diagnostics, and verdict fields.
- Scroll cases also have viewport, content extent, and max-offset expectations.
- Accepted cases must use finite deterministic geometry.
- Invalid or contradictory cases cannot be accepted without the expected blocking diagnostic.
- Environment-limited cases cannot count as accepted P8 behavior.

## Representative Layout Corpus

**Purpose**: The full set of layout scenarios required before P8 can be accepted.

**Fields**:

- `CorpusVersion`: feature/corpus revision.
- `Cases`: list of `Representative Corpus Case`.
- `RequiredCoverage`: constrained roots, measured leaves, intrinsic content, invalid constraints,
  dynamic content, layout input changes, child insertion/removal/reorder, visibility changes, and
  diagnostic cases.
- `MissingCoverage`: uncovered required categories.
- `ReadinessStatus`: aggregate status for the layout corpus.

**Validation rules**:

- Required coverage is complete before accepted status.
- Failed, missing, synthetic-only, or environment-limited required cases block accepted status.
- Case ids are stable and unique.

## ScrollViewer Corpus

**Purpose**: The required ScrollViewer case set proving viewport and content extent behavior.

**Fields**:

- `Cases`: empty content, smaller-than-viewport, exact-fit, barely overflowing, substantially
  overflowing, nested scroll, clipped parent, layered parent, text/content natural size, dynamic
  content change, and invalid intrinsic fallback.
- `ExtentSource`: intrinsic result, measured fallback, empty content, or diagnostic fallback.
- `Viewport`: accepted viewport bounds.
- `ContentExtent`: accepted natural content width and height.
- `MaxOffset`: accepted horizontal and vertical max offsets.
- `Diagnostics`: extent and fallback diagnostics.
- `ReadinessStatus`: aggregate status for ScrollViewer acceptance.

**Validation rules**:

- At least the 11 named cases are represented.
- Accepted non-error cases use `Layout.contentExtent`/intrinsic evidence, not rendered descendant
  bounds inspection.
- Content extent is at least viewport extent for non-error states.
- Current offsets are clamped to accepted ranges.
- Invalid intrinsic fallback cases must expose diagnostics and cannot count as ordinary accepted
  intrinsic evidence.

## Measured Reuse Evidence

**Purpose**: Proof that measured layout results are reused only when safe.

**Fields**:

- `ParticipantId`: measured participant.
- `EntryId`: cache entry identity.
- `ConstraintIdentity`: normalized constraints used for measurement.
- `LayoutInputKey`: content, layout intent, visibility, child order, and measurement dependency key.
- `ChildDependencyKeys`: ordered child measurement dependencies.
- `Revision`: evaluator/cache revision.
- `RunKind`: cold full, cold incremental, warm incremental, changed-input incremental, or
  disabled-cache.
- `Outcome`: hit, miss, rejected stale, diagnostic, or blocked.
- `ResultIdentity`: measured output identity.
- `Diagnostics`: reuse or stale-rejection diagnostics.

**Validation rules**:

- Accepted reuse requires exact match of all dependency keys and revision.
- Constraint, viewport, content, layout-affecting attribute, visibility, child-order, measurement
  behavior, and revision changes reject stale entries.
- Duplicate measurement in a normal pass is prevented or produces a diagnostic that blocks
  misleading acceptance.
- Disabled-cache parity must match accepted cache-enabled output except for recorded work metrics.

## Intrinsic Reuse Evidence

**Purpose**: Proof that intrinsic query results are reused only when safe.

**Fields**:

- `QueryIdentity`: intrinsic query key.
- `ParticipantId`: queried participant.
- `Axis`: min width, max width, min height, or max height.
- `CrossAxisConstraint`: relevant cross-axis bound.
- `LayoutInputKey`: content/layout dependency key.
- `IntrinsicDependencies`: child query/result identities consumed by the result.
- `Revision`: evaluator/cache revision.
- `RunKind`: cold full, warm incremental, changed-input incremental, or invalid fallback.
- `Outcome`: hit, miss, rejected stale, unsupported, diagnostic, or blocked.
- `ResultIdentity`: intrinsic output identity.
- `Diagnostics`: unsupported, contradictory, stale, or fallback messages.

**Validation rules**:

- Accepted reuse requires matching query identity, input key, dependency identities, and revision.
- Unsupported or contradictory queries cannot drive accepted ScrollViewer extent.
- Dynamic content and intrinsic dependency changes update query/result identity and reject stale
  cached answers.

## Full/Incremental Parity Result

**Purpose**: The comparison proving incremental layout remains equivalent to full layout.

**Fields**:

- `CaseId`: linked corpus case.
- `FullResultIdentity`: full evaluation identity.
- `ColdIncrementalIdentity`: cold incremental identity.
- `WarmIncrementalIdentity`: warm incremental identity.
- `ChangedInputIncrementalIdentity`: changed-input identity where applicable.
- `BoundsEquivalent`: boolean or failed reason.
- `PlacementsEquivalent`: boolean or failed reason.
- `ScrollExtentsEquivalent`: boolean or failed reason.
- `DiagnosticsEquivalent`: boolean or failed reason.
- `AcceptedReuse`: measured and intrinsic reuse records accepted for the warm run.
- `RejectedReuse`: stale entries rejected for changed-input runs.
- `Verdict`: accepted, failed, skipped, environment-limited, or blocked.

**Validation rules**:

- Accepted cases require equivalent observable bounds, placements, scroll extents, diagnostics, and
  result identities for modes that apply.
- Changed-input runs must show the expected changed geometry or diagnostics and reject stale reuse.
- Any mismatch blocks final P8 acceptance until classified.

## Regression Evidence Item

**Purpose**: One broad validation result outside the focused corpus.

**Fields**:

- `Area`: retained rendering, default layout, disabled-cache parity, overlay, render-anywhere, text
  shaping, compositor readiness, public surface, package validation, or full solution.
- `Command`: validation command or script.
- `ExpectedOutcome`: accepted, explicitly limited, or classified failure.
- `ActualOutcome`: accepted, failed, skipped, environment-limited, synthetic-only, or unrelated.
- `Classification`: P8 regression, pre-existing unrelated failure, environment limitation,
  documented non-blocking limitation, or blocker.
- `EvidencePath`: output, readiness file, or test path.
- `Diagnostics`: reviewer-visible notes.

**Validation rules**:

- At least the 8 prior guarantee areas required by the spec are present.
- P8 regressions and unclassified failures block accepted readiness.
- Environment-limited evidence must name why it is limited and what behavior is not claimed.
- Synthetic-only evidence cannot replace required real evidence.

## Layout Compatibility Ledger

**Purpose**: Public record of consumer-visible layout or diagnostic changes.

**Fields**:

- `Surface`: package/module/control/test surface.
- `Change`: observed consumer-visible change.
- `Intentional`: whether the change is intended.
- `Migration`: guidance or `None` for no action.
- `SurfaceBaselineDelta`: linked baseline diff when public API changes.
- `EvidencePath`: linked test/readiness evidence.
- `Status`: accepted, blocked, or needs review.

**Validation rules**:

- Every public surface or behavior delta is listed.
- Unintentional or undocumented deltas block final accepted readiness.
- Surface baseline changes match `.fsi` and package semantic tests.

## P8 Readiness Summary

**Purpose**: The single review entry point for final P8 acceptance.

**Fields**:

- `Feature`: `151-complete-p8-layout`.
- `P8Status`: accepted, incomplete, failed, skipped, environment-limited, or blocked.
- `CorpusStatus`: aggregate representative layout corpus status.
- `ScrollViewerStatus`: aggregate ScrollViewer corpus status.
- `ReuseStatus`: measured/intrinsic reuse and stale-rejection status.
- `ParityStatus`: full/incremental parity status.
- `RegressionStatus`: broad regression evidence status.
- `PackageStatus`: full solution and package validation status.
- `CompatibilityStatus`: public compatibility ledger status.
- `Limitations`: non-accepted or follow-up scope.
- `EvidenceLinks`: paths to all required readiness artifacts.

**Validation rules**:

- Accepted P8 status requires all required statuses to be accepted or explicitly non-blocking.
- Missing, failed, unclassified, synthetic-only, or environment-limited required evidence prevents
  accepted P8 status.
- A maintainer can review the summary and linked evidence in under 10 minutes.

## State Transitions

```text
planned -> implemented -> evidence-running -> classified -> accepted
                 |                 |              |
                 v                 v              v
              failed --------> blocked <----- environment-limited
```

- `planned`: case or evidence item is defined but not implemented.
- `implemented`: automated test or readiness writer exists.
- `evidence-running`: command/test/package validation has been executed.
- `classified`: result is accepted, failed, unrelated, skipped, synthetic-only, or limited.
- `accepted`: required item has passing real evidence and no blocking limitations.
- `blocked`: missing, failed, stale, unclassified, or incompatible evidence prevents readiness.
- `environment-limited`: limitation is explicit and cannot be counted as accepted behavior.
