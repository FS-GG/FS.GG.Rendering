# Contract: US4 — Split the Testing god-module

**Target**: `src/Testing/Testing.fs` (4,629 lines, the largest `src` file; **~30 top-level modules**
already grouped by domain). **Package**: `FS.GG.UI.Testing`. Inherits all of
[surface-invariance.md](./surface-invariance.md).

## Scope

The ~30 modules cluster cleanly into per-domain files inserted before `Testing.fs` in compile order
(this is the most mechanical of the six splits — the seams are existing module boundaries):

| Domain | Planned file | Representative modules @HEAD |
|--------|--------------|------------------------------|
| Visual | `TestingVisual.fs` | `VisualCaptureMatrix`, `VisualCompleteness`, `VisualReviewerClassifications`, `VisualReadiness`, `VisualReadinessMarkdown`, `VisualInspectionValidation`, `VisualInspectionReadiness`, `VisualInspectionMarkdown` |
| Retained inspection | `TestingRetainedInspection.fs` | `RetainedInspectionValidation`, `RetainedInspectionReadiness`, `RetainedInspectionMarkdown` |
| Evidence | `TestingEvidence.fs` | `EvidenceReports`, `DefaultTextGlyphEvidence`, generated-product/consumer validation |
| Compositor | `TestingCompositor.fs` | `CompositorReadiness`, `CompositorTimingAssertions`, `CompositorDamageReadiness` |
| Feature-readiness | `TestingFeatureReadiness.fs` | `ReadinessValidator`, `Feature159Readiness`, `Feature160ThroughputReadiness`, layout/runtime readiness, file discovery |
| Residual glue / re-exports | `Testing.fs` | shared formatting (`ReadinessFormatting`) + anything that must compile first or last |

> Exact domain assignment is finalized during implementation by dependency order (a module used by
> several domains compiles into the earliest file or stays in the residual). The binding requirement is
> surface union + byte-stability, not this exact table.

## C-T-1 — Surface union preserved

`Testing.fsi` and `FS.GG.UI.Testing.txt` byte-identical. Every public module path is preserved; modules
move files but keep their names. Shared helpers used across domains compile before their consumers
(C-SI-4); a module that would force a back-edge stays put (C-SI-6 / FR-009).

## Acceptance (maps to spec US4)

1. Built package: `.fsi` + surface baseline byte-identical (C-SI-1/2).
2. Regenerated readiness/evidence: every emitted Markdown and JSON file byte-identical (C-SI-5).

## Validation

`scripts/refresh-surface-baselines.fsx` → empty diff; build `FS.GG.UI.Testing` + run the suites that
consume it; byte-diff all emitted readiness/evidence MD+JSON vs baseline (quickstart Step 1, row US4).
