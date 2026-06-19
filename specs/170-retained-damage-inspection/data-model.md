# Data Model: Retained Render Damage Inspection

## Retained Inspection Artifact

Machine-checkable evidence for one retained-render screen or frame transition.

Fields:

- `ArtifactId`: stable run-local artifact id.
- `RunId`: validation or readiness run id.
- `Scope`: inspected page, sample, control tree, or fixture scope.
- `OutputSize`: visible logical output size.
- `Presentation`: theme, density, size role, or other variant label.
- `Transition`: optional frame transition identity.
- `FinalVisualArtifact`: optional linked final-screen `VisualInspectionArtifact`.
- `RetainedNodes`: retained node facts for the transition.
- `Damage`: damage region and locality facts for the transition.
- `Findings`: retained/damage validation findings.
- `UnsupportedFacts`: unavailable retained or damage facts.
- `RelatedVisualEvidence`: links to screenshot/readiness evidence when available.
- `ReadinessStatus`: accepted, blocked, review-required, unsupported, environment-limited, not-inspected, or not-run.
- `Diagnostics`: actionable messages.
- `GeneratedAtUtc`: artifact generation time.

Validation rules:

- Scope, output size, presentation, and readiness status are required.
- Accepted readiness requires explicit retained/damage facts for required scopes, no unexcepted blocking findings, and no required unsupported facts.
- The artifact must be deterministic after normalization except for explicitly dynamic fields such as `RunId` and `GeneratedAtUtc`.
- A linked final-screen artifact must preserve the existing visual inspection contract and is not required for unsupported retained scopes.

## Frame Transition

Describes the before/after frame pair inspected by retained rendering.

Fields:

- `TransitionId`: stable id such as `hover-localized-0-1`.
- `PriorFrameId`: prior frame id, absent on first-frame inspection.
- `CurrentFrameId`: current frame id.
- `InteractionId`: optional user interaction or scenario id.
- `ExpectedAffectedRegionIds`: declared regions that may be dirtied.
- `MaximumDirtyPercentage`: optional scenario-specific broad-damage threshold.
- `IntentionalExceptions`: scoped broad/full-surface allowances.

Validation rules:

- First frames are classified as first-frame/no-prior rather than missing evidence.
- Localized interactions must declare expected affected region ids or an equivalent affected scope.
- Intentional exceptions require affected ids and reviewer-readable reasons.

## Retained Node Fact

Stable fact about one visual node in a retained transition.

Fields:

- `NodeId`: public inspection node id.
- `ParentId`: optional parent node id.
- `RetainedIdentity`: opaque retained correlation token when available.
- `Kind`: reviewer-facing visual node kind.
- `OwnerId`: authored control key or owner id when available.
- `Status`: retained/reused, repainted, shifted, added, removed, unaffected, or unsupported.
- `PriorBounds`: node bounds before the transition.
- `CurrentBounds`: node bounds after the transition.
- `AffectedRegionIds`: visual regions that contain or intersect the node.
- `Repainted`: true when paint work occurred.
- `Shifted`: true when bounds changed.
- `UnsupportedFacts`: node-scoped retained/damage facts that could not be produced.
- `Diagnostics`: node-specific notes.

Validation rules:

- A shifted node must include prior and current bounds unless the missing bound is reported as unsupported.
- A repainted node is not automatically shifted; a shifted node is not automatically repainted.
- Added nodes have no prior bounds; removed nodes have no current bounds.
- Stable unchanged inputs keep `NodeId` and finding ids stable across repeated runs.

## Damage Region

Visible dirty area reported for one transition.

Fields:

- `TransitionId`: owning transition.
- `DamageStatus`: empty, localized, broad, full-surface, unsupported, or not-inspected.
- `FrameBounds`: visible output bounds.
- `DirtyRectangles`: clipped dirty rectangles in visible coordinates.
- `UnionBounds`: bounding rectangle for the union when useful for reviewers.
- `UnionArea`: true union area of dirty rectangles.
- `VisibleDirtyArea`: dirty area clamped to the visible frame.
- `DirtyPercentage`: visible dirty area divided by visible frame area.
- `AffectedRegionIds`: visual regions intersecting the dirty union.
- `AffectedNodeIds`: nodes contributing to or intersecting damage.
- `Cause`: scenario or retained-render cause token.
- `Diagnostics`: region-specific notes.

Validation rules:

- Overlapping rectangles are counted once.
- Visible dirty area never exceeds visible frame area.
- Empty damage reports zero area and zero rectangles, not unsupported evidence.
- Full-surface damage for a localized interaction is blocking unless a matching intentional exception exists.
- Unsupported or not-inspected damage cannot be counted as accepted locality evidence.

## Damage Locality Finding

Validation result for retained damage behavior.

Fields:

- `FindingId`: stable id from rule id, transition id, and affected ids.
- `RuleId`: retained/damage validation rule token.
- `Severity`: info, warning, blocking, unsupported, or environment-limited.
- `TransitionId`: affected transition.
- `AffectedNodeIds`: nodes involved.
- `AffectedRegionIds`: regions involved.
- `Message`: reviewer-readable explanation.
- `Expected`: expected locality or retained behavior.
- `Actual`: observed retained/damage behavior.
- `ExceptionId`: applied intentional exception, if any.
- `Diagnostics`: supporting details.

Validation rules:

- Blocking findings prevent accepted readiness.
- Unsupported required damage facts prevent accepted readiness unless the scope is explicitly environment-limited.
- Finding ids remain stable across repeated unchanged inputs.

## Intentional Damage Exception

Reviewed allowance for broad or full-surface damage that is expected for a scope.

Fields:

- `ExceptionId`: stable id.
- `RuleId`: rule being excepted.
- `ScopeId`: inspected scope.
- `TransitionId`: affected transition.
- `AffectedIds`: affected regions or nodes.
- `Reason`: reviewer-readable justification.
- `ExpiresWith`: optional feature, issue, or follow-up marker.

Validation rules:

- Exceptions with empty scope, affected ids, or reason are invalid.
- Exceptions apply only to matching rule, transition, and affected ids.
- Exceptions do not hide findings; they mark them accepted-by-exception in summaries.
- Invalid or unused exceptions remain visible diagnostics.

## Retained Inspection Summary

Reviewer-readable and machine-readable rollup for one or more retained inspection artifacts.

Fields:

- `RunId`: validation run id.
- `OverallStatus`: accepted, blocked, review-required, unsupported, environment-limited, not-inspected, or not-run.
- `ArtifactCount`: number of artifacts included.
- `InspectedScopes`: scopes with retained/damage evidence.
- `NotInspectedScopes`: declared scopes without inspection.
- `StatusCounts`: counts by readiness status.
- `DamageStatusCounts`: counts by empty/localized/broad/full-surface/unsupported/not-inspected.
- `NodeStatusCounts`: counts by retained/repainted/shifted/added/removed/unaffected/unsupported.
- `DirtyAreaSummaries`: dirty percentage and affected regions by transition.
- `BlockingFindings`: blocking retained/damage findings.
- `UnsupportedFacts`: required unsupported facts.
- `AcceptedExceptions`: applied exceptions.
- `InvalidExceptions`: invalid or stale exceptions.
- `RelatedVisualEvidence`: screenshot or visual-readiness links.
- `CommandEvidence`: command, status, elapsed time, and artifact locations.
- `Caveats`: environment limitations or scoped exclusions.
- `Diagnostics`: summary diagnostics.

Validation rules:

- Summary JSON must be sufficient for validation lanes without parsing Markdown.
- Unsupported, not-inspected, and not-run scopes remain visible and cannot be represented as accepted.
- Command evidence is required for feature readiness runs.

## Validation Entry Point

Maintained command path that runs retained inspection readiness checks.

Fields:

- `Command`: canonical command text.
- `LaneId`: validation-lane id, initially `retained-inspection`.
- `ResultStatus`: passed, failed, blocked, environment-limited, timed-out, or infrastructure-error.
- `Elapsed`: wall-clock duration.
- `ArtifactRoot`: output directory.
- `Artifacts`: summary, logs, JSON, TRX, and retained inspection artifacts.
- `PrerequisiteFailures`: missing SDK, missing restore, stale package feed, or unavailable sample/harness facts.

Validation rules:

- The command must fail clearly when prerequisites are missing.
- Validation evidence records command, result status, elapsed time, and artifact locations.
- A stale or unavailable legacy wrapper is never referenced as the canonical path.

## State Transitions

Retained inspection scope:

```text
declared -> not-run
declared -> inspected -> accepted
declared -> inspected -> blocked
declared -> inspected -> unsupported
declared -> inspected -> environment-limited
declared -> not-inspected
```

Damage status:

```text
no-prior-frame -> not-inspected-first-frame
transition-inspected -> empty
transition-inspected -> localized
transition-inspected -> broad
transition-inspected -> full-surface
transition-inspected -> unsupported
```

Retained node status:

```text
present-before-and-after -> reused
present-before-and-after -> unaffected
present-before-and-after -> repainted
present-before-and-after -> shifted
present-before-and-after -> shifted-and-repainted
absent-before-present-after -> added
present-before-absent-after -> removed
required-fact-missing -> unsupported
```

Finding:

```text
detected -> blocking
detected -> warning
detected -> unsupported
detected -> accepted-by-exception
detected -> resolved
```
