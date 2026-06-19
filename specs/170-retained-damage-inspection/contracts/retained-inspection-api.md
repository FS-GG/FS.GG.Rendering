# Contract: Retained Inspection API

## Package Boundary

The API is additive and follows the existing Feature 165 package split:

- `FS.GG.UI.Scene`: dependency-light retained/damage evidence records and stable status tokens.
- `FS.GG.UI.Controls`: retained render adapter that emits artifacts from `RetainedRender.init` and `RetainedRender.step`.
- `FS.GG.UI.Testing`: retained/damage validation, readiness aggregation, Markdown/JSON rendering, and artifact helpers.

The API must not create these package dependencies:

- Scene -> Controls, Layout, Testing, SkiaViewer, SkiaSharp, Yoga.Net, Elmish, or KeyboardInput.
- Controls -> Testing.
- Testing -> Controls or Layout.

## Planned Public Types

Exact names may be adjusted for F# ergonomics during implementation, but the public contract must cover these shapes without modifying existing `VisualInspectionArtifact` fields:

```fsharp
// FS.GG.UI.Scene
type RetainedInspectionStatus
type RetainedNodeStatus
type DamageInspectionStatus
type RetainedFrameTransition
type RetainedNodeInspection
type DamageRegionInspection
type DamageLocalityFinding
type IntentionalDamageException
type RetainedInspectionArtifact
type RetainedInspectionSummary

// FS.GG.UI.Controls
type RetainedControlInspectionRequest<'msg>
type RetainedControlTransition<'msg>

// FS.GG.UI.Testing
type RetainedInspectionRule
type RetainedInspectionValidationCheck
type RetainedInspectionValidationResult
type RetainedInspectionSummarySectionUpdate
```

## Planned Public Modules

```fsharp
// FS.GG.UI.Scene
module RetainedInspection

// FS.GG.UI.Controls
module ControlInspection

// FS.GG.UI.Testing
module RetainedInspectionValidation
module RetainedInspectionReadiness
module RetainedInspectionMarkdown
```

## Scene Model Contracts

`RetainedInspection` owns stable text/status helpers and pure artifact utilities.

Required behavior:

- Status values have stable lowercase text tokens.
- Artifact normalization sorts nodes, regions, transitions, findings, and unsupported facts deterministically.
- Unsupported facts include fact name, owner, reason, diagnostic, and environment-limited classification.
- Existing visual inspection artifacts can be linked or embedded as final-screen evidence without changing their record shape.
- Dirty area values use visible coordinates and true union area.

## Controls Adapter Contracts

`ControlInspection` must expose a retained inspection entry point that runs or consumes the real retained render transition path.

Required behavior:

- Uses `RetainedRender.init` for first-frame evidence and `RetainedRender.step` for subsequent transitions.
- Captures retained/reused, repainted, shifted, added, removed, unaffected, and unsupported node statuses.
- Captures prior and current bounds for shifted nodes when available.
- Captures dirty rectangles, union area, visible dirty area, dirty percentage, repainted count, shifted count, unaffected count, and affected regions.
- Uses existing public inspection node ids for artifact identity; internal retained ids are exposed only as opaque correlation tokens when needed.
- Emits explicit first-frame/no-prior, empty-damage, unsupported-damage, and not-inspected states.
- Does not alter rendered output, event bindings, bounds, diagnostics, or screenshot readiness behavior.

## Testing Contracts

`RetainedInspectionValidation.validate` accepts retained inspection artifacts, rules, expected affected regions, and intentional exceptions.

Required behavior:

- Missing required retained/damage facts produce unsupported or blocking findings, not accepted evidence.
- Full-surface damage for a localized interaction is blocking unless a matching intentional exception exists.
- Dirty regions outside expected affected regions are blocking or review-required according to the rule.
- Shifted nodes are validated separately from repainted nodes.
- Repeated unchanged inputs produce stable node ids and finding ids.
- Invalid and unused exceptions remain visible diagnostics.

## Compatibility Contracts

- Existing `VisualInspectionValidation`, `VisualInspectionReadiness`, and `VisualInspectionMarkdown` remain source-compatible.
- Existing screenshot visual-readiness target counts and reviewer classifications remain unchanged unless an explicit readiness artifact documents a deliberate change.
- Existing `CompositorDamageReadiness` remains supported; retained inspection may link to it but does not replace it in this feature.
- Surface baselines must be refreshed for every package whose `.fsi` changes.
