# Contract: Visual Inspection API

## Package Boundary

The public API is additive and split by package responsibility:

- `FS.GG.UI.Scene`: dependency-light inspection model and stable status/finding vocabulary.
- `FS.GG.UI.Controls`: adapter that derives inspection artifacts from rendered control trees.
- `FS.GG.UI.Testing`: validators, readiness aggregation, generated Markdown/JSON, and generated-product assertions over inspection artifacts.

The API must not create these package dependencies:

- Scene -> Controls, Layout, Testing, SkiaViewer, SkiaSharp, Yoga.Net, Elmish, or KeyboardInput.
- Controls -> Testing.
- Testing -> Controls or Layout.

## Planned Public Types

Exact F# names may be adjusted for ergonomics during implementation, but the public contract must cover these shapes:

```fsharp
// FS.GG.UI.Scene
type VisualInspectionStatus
type VisualInspectionSeverity
type VisualInspectionMeasurementMode
type VisualInspectionFitStatus
type VisualInspectionNodeKind
type VisualInspectionPaintRole
type VisualInspectionSurfaceRole
type VisualInspectionScope
type VisualInspectionNode
type VisualTextInspection
type VisualRegionBoundary
type VisualPaintCoverage
type VisualClipFact
type VisualInspectionFinding
type VisualInspectionUnsupportedFact
type VisualInspectionArtifact
type VisualInspectionSummary

// FS.GG.UI.Testing
type VisualInspectionRule
type VisualInspectionException
type VisualInspectionValidationCheck
type VisualInspectionValidationResult
type VisualInspectionSummarySectionUpdate
```

## Planned Public Modules

```fsharp
// FS.GG.UI.Scene
module VisualInspection

// FS.GG.UI.Controls
module ControlInspection

// FS.GG.UI.Testing
module VisualInspectionValidation
module VisualInspectionReadiness
module VisualInspectionMarkdown
```

## Scene Model Contracts

`VisualInspection` owns stable text/status helpers and pure artifact utilities.

Required behavior:

- Status and severity values have stable lowercase text tokens.
- Artifact helpers never infer missing facts as passing facts.
- Unsupported facts include fact name, reason, and diagnostic.
- Summary helpers preserve deterministic ordering by scope, region, node, and finding id.

## Controls Adapter Contracts

`ControlInspection.inspect` or the equivalent Controls entry point accepts a rendered control scope and returns a `VisualInspectionArtifact`.

Required behavior:

- Uses `Control.renderTree` semantics for full layout/paint inspection.
- Reuses the existing `Key ?? structural-path` identity scheme used by bounds and event bindings.
- Emits final logical bounds for laid-out controls.
- Emits text inspection facts for text-bearing controls when text and owner bounds are available.
- Emits clip facts for containers, scroll viewers, overlay surfaces, and clipped scene nodes when available.
- Emits paint coverage facts for required root/surface/content roles when available.
- Emits unsupported facts when transforms, text metrics, hidden content, virtualized content, or paint coverage cannot be represented.
- Does not alter rendered output or event bindings.

## Testing Validation Contracts

`VisualInspectionValidation.validate` accepts an artifact plus validation rules and intentional exceptions, then returns a result with findings, readiness status, and diagnostics.

Required behavior:

- Missing required region -> blocking finding.
- Text outside owner bounds -> blocking finding unless a matching intentional exception exists.
- Accidental clipping -> blocking finding.
- Unclassified overlap between ordinary regions -> blocking finding.
- Missing paint coverage for required root/section surfaces -> blocking finding.
- Unsupported required facts -> unsupported or environment-limited status, not accepted.
- Invalid exceptions -> diagnostics and no effect on blocking findings.
- Repeated unchanged inputs -> stable finding ids.

## Readiness Aggregation Contracts

`VisualInspectionReadiness` aggregates one or more artifacts and validation results.

Required behavior:

- Accepted requires all required scopes inspected, no blocking findings, no required unsupported facts, and no invalid required exceptions.
- Blocked applies when any required scope has blocking findings.
- Incomplete applies when required scopes are not run or not inspected.
- Environment-limited applies only when required facts cannot be produced due to an explicitly recorded host/environment limitation.
- Not-inspected and unsupported states remain visible and cannot be collapsed into passed.

## Markdown and JSON Contracts

`VisualInspectionMarkdown` renders generated summaries and updates managed sections.

Required behavior:

- Human summary includes overall status, inspected scopes, region/node counts, finding counts, blocking findings, unsupported facts, accepted exceptions, invalid exceptions, and caveats.
- JSON includes all machine-readable fields required to recompute readiness without parsing prose.
- Managed-section updates preserve manual text outside markers.
- Unsafe marker states fail without producing a writable update.
