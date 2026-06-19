# Contract: Visual Readiness Artifacts

## Evidence Root

Every readiness run writes artifacts below a caller-selected evidence root, for example:

```text
specs/164-shared-visual-readiness/readiness/antshowcase-preferred/
```

All report paths are relative to this root unless explicitly marked as command output paths.

## Capture Artifact Paths

Default relative path pattern:

```text
<size-role>/<theme-id>/<page-id>.png
```

AntShowcase may preserve its current path layout, such as:

```text
light/button.png
dark/button.png
```

Required fields per capture record:

- `targetId`
- `pageId`
- `themeId`
- `width`
- `height`
- `relativePath`
- `status`
- `byteCount`
- `contentHash`
- `observedWidth`
- `observedHeight`
- `reason`
- `diagnostics`

## Reviewer Classification Markdown

The reviewer template is a Markdown table with one row per target.

Required columns:

```text
targetId | pageId | themeId | size | severity | defectClass | readinessImpact | reviewer | timestamp | notes
```

Severity vocabulary:

- `none`
- `minor`
- `major`
- `blocking`

Rows that still contain `pending review` are treated as missing reviewer classifications.

## Machine-Readable Summary

Generated JSON must include:

- `runId`
- `evidenceRoot`
- `targetCount`
- `requiredTargetCount`
- `captureStatusCounts`
- `reviewerStatusCounts`
- `readinessStatus`
- `targets`
- `captures`
- `reviewerClassifications`
- `contactSheets`
- `caveats`
- `diagnostics`

Validation lanes must be able to decide target counts, status counts, reviewer state, contact sheet locations, and overall readiness from JSON without parsing Markdown.

## Human Markdown Summary

Generated Markdown must include:

- overall readiness
- target counts
- capture status counts
- reviewer status counts
- degraded and blocked target list
- contact sheet links
- caveats and accepted exceptions
- diagnostics that require maintainer action

The summary must not collapse complete, degraded, missing, blocked, and pending-review states into one ambiguous pass/fail line.

## Managed Summary Markers

Manual readiness summaries use these markers for generated visual-readiness content:

```text
<!-- FS.GG VISUAL READINESS START -->
<!-- FS.GG VISUAL READINESS END -->
```

Rules:

- exactly one start and one end marker may be updated
- missing markers are inserted in a deterministic location
- multiple starts, multiple ends, reversed markers, or one-sided markers fail safely
- manual content before and after the section remains byte-for-byte unchanged

## Contact Sheet Manifest

The shared package records contact sheet metadata even when image composition is sample-owned.

Required fields:

- `sheetId`
- `relativePath`
- `size`
- `themeId`
- `targetIds`
- `missingTargetIds`
- `diagnostics`

For Feature 164, AntShowcase keeps the PNG contact-sheet writer in `samples/AntShowcase/AntShowcase.App/VisualReadiness.fs`.

