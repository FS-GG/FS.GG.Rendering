# Contract: `FS.GG.UI.Testing` Visual Readiness API

## Package Boundary

The public API is additive in `FS.GG.UI.Testing` and is declared in `src/Testing/Testing.fsi`. Implementation lives in `src/Testing/Testing.fs`. The public surface baseline `readiness/surface-baselines/FS.GG.UI.Testing.txt` must include every new public type and module.

The API owns:

- visual page/theme/size/target records
- deterministic matrix expansion
- duplicate and path validation
- PNG artifact completeness classification
- degraded capture records and reasons
- reviewer template writing/parsing/validation
- readiness status aggregation
- generated Markdown and JSON summary content
- managed-section update safety

The API does not own:

- sample page registry construction
- theme resolution
- rendering callbacks
- `Viewer.captureScreenshotEvidence`
- contact-sheet PNG composition
- AntShowcase-specific page lists or accepted size choices

## Planned Public Types

The implementation phase may adjust exact names for F# ergonomics, but the public contract must cover these shapes:

```fsharp
type VisualSize
type VisualTheme
type VisualPage
type VisualCaptureTarget
type VisualCaptureStatus
type VisualCaptureArtifact
type VisualCaptureRecord
type VisualReviewerSeverity
type VisualReviewerClassification
type VisualReviewerValidationResult
type VisualContactSheet
type VisualReadinessStatus
type VisualReadinessReport
type VisualSummarySectionUpdate
```

## Planned Public Modules

```fsharp
module VisualCaptureMatrix
module VisualCompleteness
module VisualReviewerClassifications
module VisualReadiness
module VisualReadinessMarkdown
```

## Function Contracts

### Matrix Expansion

`VisualCaptureMatrix.expand` accepts pages, themes, sizes, and an output path policy and returns ordered capture targets or diagnostics.

Required behavior:

- order is deterministic: size, theme, page unless a caller-supplied order is encoded in declarations
- duplicate page ids, theme ids, size roles, target ids, or relative paths are rejected
- target paths are relative and cannot escape the evidence root
- a 3 page x 2 theme x 2 size declaration produces 12 targets

### PNG Completeness

`VisualCompleteness.validate` accepts an evidence root and target list and returns one capture record per target.

Required behavior:

- missing target path -> `missing`
- zero-byte/corrupt/non-PNG artifact -> `undecodable`
- readable PNG with wrong dimensions -> `wrong-size`
- readable PNG with expected dimensions -> `complete`
- caller-supplied degraded result with empty reason -> `blocked`
- stale files outside the current target matrix are reported as diagnostics or cleanup candidates and do not satisfy targets

### Reviewer Classifications

`VisualReviewerClassifications.writeTemplate` creates a Markdown table with one row per target.

`VisualReviewerClassifications.parse` accepts template Markdown and the current target matrix, returning parsed records plus diagnostics.

Required behavior:

- missing row -> pending review
- duplicate row -> diagnostic
- unknown target -> diagnostic
- malformed severity or readiness impact -> diagnostic
- `blocking` severity -> blocked readiness
- all required targets with `none`, `minor`, or `major` and no other blocking evidence -> reviewer gate can pass

### Readiness Aggregation

`VisualReadiness.evaluate` accepts targets, capture records, reviewer classifications, contact sheets, caveats, and accepted exceptions, returning a `VisualReadinessReport`.

Required behavior:

- all required complete + all reviewed + no blocking defect -> `accepted`
- all complete + missing reviewer rows -> `pending-review`
- any missing/wrong-size/undecodable/blocked capture -> `blocked`
- degraded captures with reasons -> `environment-limited` unless a plan records an accepted exception
- report contains target counts, status counts, reviewer counts, contact sheet paths, caveats, and diagnostics

### Markdown and Managed Sections

`VisualReadinessMarkdown.renderSummary` converts a report to generated Markdown.

`VisualReadinessMarkdown.updateManagedSection` updates one bounded generated section in an existing summary file.

Required behavior:

- manual content outside markers remains byte-for-byte unchanged
- absent markers are inserted at a deterministic location
- malformed or ambiguous markers fail without writing
- machine-readable JSON is generated separately from human Markdown

## Compatibility

This feature must not remove or rename existing `FS.GG.UI.Testing` members. Existing screenshot evidence validators remain supported. New public surface requires a baseline update and migration notes for AntShowcase.

