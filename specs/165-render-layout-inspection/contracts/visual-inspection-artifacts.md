# Contract: Visual Inspection Artifacts

## Evidence Root

Inspection runs write artifacts below the active feature readiness directory, for example:

```text
specs/165-render-layout-inspection/readiness/inspection/
```

Paths in summaries should be project-relative or evidence-root-relative unless an external command path is required for diagnostics.

## Required Files

```text
readiness/inspection/
|-- summary.md
|-- summary.json
|-- artifacts/
|   |-- <scope-id>.inspection.json
|   `-- <scope-id>.inspection.md
|-- findings/
|   `-- blocking-findings.md
`-- compatibility.md
```

## Artifact JSON Fields

Each `*.inspection.json` file must include:

- `artifactId`
- `scope`
- `outputSize`
- `presentation`
- `readinessStatus`
- `nodes`
- `regions`
- `textRuns`
- `paintCoverage`
- `clipFacts`
- `findings`
- `unsupportedFacts`
- `diagnostics`
- `generatedAtUtc`

## Node Fields

Each node entry must include:

- `nodeId`
- `parentId`
- `kind`
- `ownerId`
- `bounds`
- `clip`
- `zOrder`
- `paintRole`
- `surfaceRole`
- `textRunIds`
- `children`
- `dynamic`
- `unsupportedFacts`

## Finding Fields

Each finding entry must include:

- `findingId`
- `ruleId`
- `severity`
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
- `notRunScopes`
- `statusCounts`
- `findingCounts`
- `blockingFindings`
- `unsupportedFacts`
- `acceptedExceptions`
- `invalidExceptions`
- `relatedVisualEvidence`
- `caveats`
- `diagnostics`

Validation lanes must be able to decide accepted/blocked/incomplete/environment-limited status from this JSON without parsing Markdown.

## Human Markdown Summary

The human summary must include:

- overall readiness status
- inspected scope table
- status counts
- finding counts by severity and rule
- blocking finding table
- unsupported fact table
- accepted intentional exceptions
- invalid or unused exceptions
- links to matching screenshot or visual-readiness evidence when available
- caveats and out-of-scope notes

The summary must not represent unsupported, not-inspected, not-run, or environment-limited evidence as passed.

## Managed Summary Markers

Manual readiness summaries use these markers for generated inspection content:

```text
<!-- FS.GG VISUAL INSPECTION START -->
<!-- FS.GG VISUAL INSPECTION END -->
```

Rules:

- Exactly one start marker and one end marker may be updated.
- Missing markers are inserted in a deterministic location.
- Multiple starts, multiple ends, reversed markers, or one-sided markers fail safely.
- Manual content before and after the generated section remains byte-for-byte unchanged.

## Compatibility Artifact

`compatibility.md` must state:

- Which public `.fsi` files changed.
- Which surface baselines changed.
- Whether existing `LayoutEvidenceReport` behavior changed.
- Whether screenshot visual-readiness evidence changed.
- Migration notes for sample or generated-product consumers.

If the feature is purely additive, the artifact must say so explicitly.
