# Data Model: Structured Render/Layout Inspection Metadata

## Inspection Artifact

Machine-checkable evidence for one inspected screen, page, or rendered control tree.

- `ArtifactId`: stable run-local id.
- `Scope`: page/screen/control identity being inspected.
- `OutputSize`: logical output size.
- `Presentation`: theme, density, or visual variant label when applicable.
- `Nodes`: ordered visual node list or tree.
- `Regions`: named region boundaries.
- `Findings`: validation findings.
- `UnsupportedFacts`: unavailable or unsupported inspection facts.
- `ReadinessStatus`: accepted, blocked, incomplete, unsupported, environment-limited, not-inspected, or not-run.
- `Diagnostics`: actionable messages.

Validation rules:

- Scope and output size are required.
- Node ids and region ids are unique within an artifact.
- Unsupported facts must name the missing fact and reason.
- Accepted readiness requires no blocking findings and no required unsupported facts.

## Visual Node

One inspectable rendered element or grouping.

- `NodeId`: stable id, using authored key when available and structural path otherwise.
- `ParentId`: parent node id when nested.
- `Kind`: reviewer-facing visual/control/scene kind.
- `OwnerId`: authored control or region owner when available.
- `Bounds`: final logical bounds.
- `Clip`: effective clip bounds and clipping classification when available.
- `ZOrder`: deterministic visual ordering.
- `PaintRole`: background, surface, border, foreground, content, overlay, or unknown.
- `SurfaceRole`: root, shell, content, overlay, popup, feedback, or custom role when known.
- `TextRuns`: text facts owned by the node.
- `Children`: child node ids.
- `Dynamic`: whether identity or geometry is expected to change between runs.

Validation rules:

- Static nodes must have stable ids and deterministic order across unchanged runs.
- Visible nodes need non-negative finite bounds.
- Nodes without bounds must be marked hidden, virtualized, unsupported, or not applicable.
- Overlay/floating nodes must carry enough role information to justify intentional overlap.

## Text Run Inspection

Measured text facts for one rendered or control-owned text run.

- `TextId`: stable id tied to owning node.
- `OwnerNodeId`: node that owns the text.
- `Text`: inspected text or safe excerpt when full text is not appropriate.
- `TextBounds`: measured text area.
- `OwnerBounds`: containing visual bounds.
- `Baseline`: vertical placement fact when available.
- `MeasurementMode`: exact, approximate, unsupported, or unavailable.
- `FitStatus`: inside, overflow, clipped, wrapped, truncated, unsupported, or unavailable.
- `Diagnostics`: measurement and classification notes.

Validation rules:

- Exact and approximate facts must be distinguished.
- Overflow or clipping creates a finding unless an explicit exception matches the owner and rule.
- Wrapped or truncated text is accepted only when the owning control classifies that behavior as intentional.

## Region Boundary

Named visual area used for containment and overlap validation.

- `RegionId`: stable id.
- `Name`: reviewer-facing name.
- `Role`: root, shell, navigation, content, feedback, overlay, popup, or custom role.
- `Bounds`: final logical bounds.
- `Required`: whether absence or missing paint blocks readiness.
- `OwnerNodeIds`: nodes belonging to the region.
- `AllowedOverlapRoles`: roles that may overlap this region with explicit classification.

Validation rules:

- Required regions need finite bounds and paint coverage.
- Ordinary content regions may not overlap other ordinary content regions.
- Overlay/popup overlap is accepted only through an intentional exception.

## Paint Coverage Fact

Evidence that a region or node has intentional visual surface coverage.

- `TargetId`: region or node being covered.
- `PaintRole`: background, surface, border, foreground, or content.
- `CoverageBounds`: bounds of the paint contribution.
- `CoverageStatus`: complete, partial, missing, unsupported, or unavailable.
- `Reason`: required for partial, unsupported, and unavailable statuses.

Validation rules:

- Required root and section backgrounds need complete or explicitly accepted coverage.
- Missing required coverage creates a blocking finding.
- Partial coverage must state whether it is intentional.

## Clip Fact

Evidence about clipping applied to a node or region.

- `NodeId`: affected node.
- `ClipBounds`: effective clip area when available.
- `ClipStatus`: none, intentional, accidental, unsupported, or unavailable.
- `Reason`: required for intentional, unsupported, and unavailable statuses.
- `AffectedTextRunIds`: text runs affected by clipping.

Validation rules:

- Accidental clipping creates a blocking finding.
- Intentional clipping must match an exception or an owned scroll/bounded-content role.
- Unsupported clipping facts prevent accepted deterministic inspection when the clipped content is required.

## Inspection Finding

One rule result requiring review or action.

- `FindingId`: stable id from rule id and affected ids.
- `RuleId`: rule that produced the finding.
- `Severity`: pass, info, warning, blocking, unsupported, or environment-limited.
- `AffectedNodeIds`: nodes involved.
- `AffectedRegionIds`: regions involved.
- `Message`: reviewer-readable explanation.
- `Expected`: expected condition.
- `Actual`: observed condition.
- `ExceptionId`: intentional exception that accepted the finding, if any.
- `Diagnostics`: extra details for triage.

Validation rules:

- Blocking findings prevent accepted readiness.
- Unsupported required facts prevent accepted readiness unless the inspected scope is explicitly environment-limited.
- Finding ids must be stable across repeated runs for unchanged inputs.

## Intentional Exception

Reviewed allowance for a specific overlap, clipping, unsupported fact, or unavailable fact.

- `ExceptionId`: stable id.
- `RuleId`: rule being excepted.
- `OwnerId`: node, region, or sample owner responsible for the exception.
- `AffectedIds`: exact node or region ids the exception applies to.
- `Reason`: reviewer-readable justification.
- `ExpiresWith`: optional scope or follow-up marker for temporary exceptions.

Validation rules:

- Exceptions with empty owners, affected ids, or reasons are invalid.
- Exceptions apply only to matching rule and affected ids.
- Invalid exceptions create diagnostics and do not convert blocking findings to accepted findings.

## Inspection Summary

Reviewer-readable and machine-readable rollup of one or more artifacts.

- `RunId`: stable run id.
- `Artifacts`: inspected artifact ids and paths.
- `StatusCounts`: counts by readiness status.
- `FindingCounts`: counts by severity and rule.
- `UnsupportedFacts`: required unsupported facts.
- `Exceptions`: accepted and invalid exceptions.
- `RelatedVisualEvidence`: optional links to screenshots or visual-readiness reports.
- `OverallStatus`: accepted, blocked, incomplete, unsupported, environment-limited, not-inspected, or not-run.
- `Caveats`: limitations and scope boundaries.

Validation rules:

- The summary must distinguish inspected, not-inspected, unsupported, failed, environment-limited, and not-run scopes.
- Machine-readable summaries must be sufficient for validation lanes without parsing prose.
- Manual notes outside generated sections must be preserved when summaries are regenerated.

## State Transitions

Inspection scope:

```text
declared -> not-run
declared -> inspected -> accepted
declared -> inspected -> blocked
declared -> inspected -> unsupported
declared -> inspected -> environment-limited
declared -> not-inspected
```

Finding:

```text
detected -> blocking
detected -> warning
detected -> unsupported
detected -> accepted-by-exception
detected -> resolved
```

Exception:

```text
declared -> valid -> applied
declared -> invalid -> ignored-with-diagnostic
declared -> valid -> unused-with-diagnostic
```
