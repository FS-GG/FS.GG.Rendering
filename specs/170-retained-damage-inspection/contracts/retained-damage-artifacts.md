# Contract: Retained Damage Artifacts

## Evidence Root

Retained inspection runs write artifacts below the active feature readiness directory:

```text
specs/170-retained-damage-inspection/readiness/retained-inspection/
```

Paths in JSON and Markdown should be project-relative or evidence-root-relative unless an external process path is required for diagnostics.

## Required Files

```text
readiness/retained-inspection/
|-- summary.md
|-- summary.json
|-- validation-log.md
|-- compatibility.md
|-- antshowcase-adoption.md
|-- artifacts/
|   |-- <scope-id>.<transition-id>.retained.json
|   `-- <scope-id>.<transition-id>.retained.md
`-- findings/
    `-- blocking-findings.md
```

## Artifact JSON Fields

Each `*.retained.json` file must include:

- `artifactId`
- `runId`
- `scope`
- `outputSize`
- `presentation`
- `transition`
- `finalVisualArtifact`
- `retainedNodes`
- `damage`
- `findings`
- `unsupportedFacts`
- `relatedVisualEvidence`
- `readinessStatus`
- `diagnostics`
- `generatedAtUtc`

## Retained Node Fields

Each retained node entry must include:

- `nodeId`
- `parentId`
- `retainedIdentity`
- `kind`
- `ownerId`
- `status`
- `priorBounds`
- `currentBounds`
- `affectedRegionIds`
- `repainted`
- `shifted`
- `unsupportedFacts`
- `diagnostics`

## Damage Fields

The damage entry for a transition must include:

- `transitionId`
- `damageStatus`
- `frameBounds`
- `dirtyRectangles`
- `unionBounds`
- `unionArea`
- `visibleDirtyArea`
- `dirtyPercentage`
- `affectedRegionIds`
- `affectedNodeIds`
- `repaintedNodeCount`
- `shiftedNodeCount`
- `unaffectedNodeCount`
- `cause`
- `diagnostics`

Rules:

- `unionArea` and `visibleDirtyArea` use true union semantics.
- `dirtyPercentage` is computed from visible dirty area divided by visible frame area.
- Empty damage uses zero area and an `empty` status.
- Unsupported or unavailable facts are represented explicitly, not by missing fields.

## Finding Fields

Each retained/damage finding must include:

- `findingId`
- `ruleId`
- `severity`
- `transitionId`
- `affectedNodeIds`
- `affectedRegionIds`
- `message`
- `expected`
- `actual`
- `exceptionId`
- `diagnostics`

## Summary JSON Fields

The aggregate `summary.json` must include:

- `runId`
- `overallStatus`
- `artifactCount`
- `inspectedScopes`
- `notInspectedScopes`
- `statusCounts`
- `damageStatusCounts`
- `nodeStatusCounts`
- `dirtyAreaSummaries`
- `blockingFindings`
- `unsupportedFacts`
- `acceptedExceptions`
- `invalidExceptions`
- `relatedVisualEvidence`
- `commandEvidence`
- `caveats`
- `diagnostics`

Validation lanes must be able to decide accepted, blocked, review-required, unsupported, environment-limited, not-inspected, and not-run status from this JSON without parsing Markdown.

## Human Markdown Summary

The human summary must include:

- overall readiness status
- command, elapsed time, result status, and artifact root
- inspected scope and transition table
- dirty area percentage by transition
- repainted, shifted, unaffected, added, removed, and unsupported node counts
- affected visual regions
- blocking and review-required findings
- unsupported/not-inspected facts
- accepted, invalid, and unused intentional exceptions
- links to matching screenshot or visual-readiness evidence when available
- caveats and out-of-scope notes

The summary must not represent unsupported, not-inspected, not-run, or environment-limited retained evidence as accepted.

## Managed Summary Markers

Manual readiness summaries use generated retained-inspection markers:

```text
<!-- FS.GG RETAINED INSPECTION START -->
<!-- FS.GG RETAINED INSPECTION END -->
```

Rules:

- Exactly one start marker and one end marker may be updated.
- Missing markers are inserted deterministically.
- Multiple starts, multiple ends, reversed markers, or one-sided markers fail safely.
- Manual content before and after the generated section remains byte-for-byte unchanged.

## Compatibility Artifact

`compatibility.md` must state:

- Which public `.fsi` files changed.
- Which surface baselines changed.
- Whether existing `VisualInspectionArtifact` behavior changed.
- Whether existing `CompositorDamageReadiness` behavior changed.
- Whether screenshot readiness target counts or reviewer classification rules changed.
- Migration notes for existing inspection consumers, generated products, and AntShowcase.

If the feature is additive, the artifact must say so explicitly.
