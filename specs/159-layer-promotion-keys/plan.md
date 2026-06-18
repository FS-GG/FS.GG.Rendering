# Implementation Plan: Layer Promotion and Content/Transform Key Split

**Branch**: `159-layer-promotion-keys` | **Date**: 2026-06-18 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/159-layer-promotion-keys/spec.md`

## Summary

Implement Feature 159 by splitting retained content identity from placement/transform identity,
promoting only stable beneficial compositor boundaries, demoting churning or non-beneficial
boundaries, and publishing same-profile reuse evidence. The implementation extends existing
`RetainedRender`, `PictureReplayCache`, Feature 157 damage readiness, and Feature 158 timing policy
without broadening the final shipped compositor performance claim.

## Technical Context

**Language/Version**: F# on .NET `net10.0`; repository `LangVersion=latest`; public surface
curated through `.fsi` files.

**Primary Dependencies**: Existing `FS.GG.UI.Controls`, `FS.GG.UI.SkiaViewer`,
`FS.GG.UI.Scene`, `FS.GG.UI.Testing`, and `Rendering.Harness` projects; SkiaSharp
`4.147.0-preview.3.1`; Silk.NET OpenGL/Windowing `2.23.0`; Expecto `10.2.2`; existing Feature
155 proof, Feature 157 no-clear damage readiness, and Feature 158 measurement-policy helpers.
No new runtime dependency is planned.

**Storage**: Durable Feature 159 evidence under `specs/159-layer-promotion-keys/readiness/`,
including `promotion/attempts/`, `promotion/reuse/`, `promotion/demotions/`,
`promotion/fallbacks/`, `promotion/parity/`, `promotion/unsupported/`, `counters/`, `fsi/`,
`compatibility-ledger.md`, `package-validation.md`, `regression-validation.md`, and
`validation-summary.md`. Transient harness output may be written under a caller-provided `--out`
directory before accepted records are copied into readiness.

**Testing**: Expecto through `dotnet test`; Controls tests for identity splitting, promotion
thresholds, demotion, counters, and parity-oracle behavior; SkiaViewer tests for replay cache
content/placement reuse and safe fallback; Rendering.Harness tests for Feature 159 command
routing, scenario inventory, readiness rendering, unsupported-host behavior, and final claim
status; Package/Testing/FSI coverage for any package-visible diagnostic helper or token; focused
regression tests for Features 155, 157, and 158.

**Target Platform**: Multi-package F# UI/rendering library on .NET `net10.0`; SkiaSharp over
OpenGL for the live viewer host. Accepted Feature 159 reuse targets the same stable host profile
used by Features 155-158: `probe-08a47c01`. Unsupported or unavailable presentation environments
remain fail-closed with zero accepted Feature 159 reuse or promotion artifacts.

**Project Type**: Multi-package F# rendering/UI library plus desktop viewer host and validation
harness.

**Performance Goals**: Placement-only movement reuses recorded content in 100% of accepted
same-profile placement-only scenarios, while 100% of accepted content-change scenarios re-record
or invalidate content. Promotion requires a three-frame stability observation window, parity
success, net-positive saved work over bookkeeping overhead, and the existing 30% repeated-work
reduction threshold; simple-scene overhead must stay within the existing 5% guard. Accepted
readiness requires at least three fresh same-profile attempts and representative scenario coverage
for static retained content, placement-only movement, scrolling or shifted content, nested retained
content, content churn, and fallback.

**Constraints**: Content reuse is fail-closed. Missing, stale, cross-profile, cross-run,
unsupported-host, resource-limited, missing-retained-content, ambiguous identity, parity-failing,
or non-beneficial evidence cannot be accepted. Full redraw and lower safe tiers remain available
for every frame. Feature 159 can accept net-positive reuse/promotion counters, but the shipped
compositor performance claim remains `performance-not-accepted` unless same-profile timing is not
noisy and the later report-defined host-lane gate is also satisfied.

**Scale/Scope**: Narrow P7 optimization and evidence slice across `src/Controls/RetainedRender.*`,
`src/SkiaViewer/PictureReplayCache.*`, `src/SkiaViewer/SkiaViewer.*` only if package-visible
diagnostics are needed, `tests/Rendering.Harness`, focused Controls/SkiaViewer/Package/Testing
tests, readiness docs, and surface baselines if public `.fsi` changes. Out of scope: Feature 160
validation throughput, Feature 161 host performance lane ledger, changing proof/readback
separation, broadening host support, P8 layout, text shaping, overlay behavior, texture promotion
as a public authoring surface, and universal compositor performance acceptance.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Plan Evidence |
|------|--------|---------------|
| Specification and classification | PASS | `spec.md` exists and classifies the work as Tier 1 because observable compositor optimization behavior and package-facing diagnostics may change. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Any public or observable promotion decision, reuse status, counter, demotion reason, readiness helper, or command output is specified in contracts and `.fsi` before implementation and covered by semantic/FSI tests. |
| Visibility lives in `.fsi` | PASS | Package-visible `SkiaViewer`, `Testing`, or harness additions must be declared in `.fsi`; implementation-only promotion heuristics and native resource helpers stay omitted. |
| Idiomatic simplicity | PASS | The plan extends existing retained-render, replay-cache, damage-readiness, and harness patterns without adding a new compositor framework or dependency. |
| Elmish/MVU boundary for stateful or I/O workflows | PASS | Promotion evidence collection is modeled as pure workflow state, messages, and effects; host rendering, parity capture, counter publication, and filesystem writes are interpreted at the edge. |
| Test evidence is mandatory | PASS | Failing-first tests cover identity split, placement-only reuse, content-change invalidation, cheap/stable rejection, churn demotion, unsupported-host zero acceptance, parity rejection, and readiness publication. Synthetic fixtures are rejection-only. |
| Observability and safe failure | PASS | Every rejected, demoted, bypassed, fallback, or environment-limited attempt records a primary reason; full redraw remains the safe fallback. |
| Tier 1 obligations | PASS | `.fsi`, surface baselines, compatibility notes, package validation, readiness evidence, and regression validation are required for any public or consumer-visible delta. |

No constitution violations are required.

## Project Structure

### Documentation (this feature)

```text
specs/159-layer-promotion-keys/
|-- spec.md
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   |-- content-placement-identity.md
|   |-- layer-promotion-command.md
|   |-- promotion-reuse-evidence.md
|   |-- promotion-workflow-effects.md
|   `-- readiness-package.md
`-- readiness/
    |-- promotion/
    |   |-- attempts/
    |   |-- reuse/
    |   |-- demotions/
    |   |-- fallbacks/
    |   |-- parity/
    |   |-- unsupported/
    |   `-- summary.md
    |-- counters/
    |-- fsi/
    |-- compatibility-ledger.md
    |-- package-validation.md
    |-- regression-validation.md
    `-- validation-summary.md
```

### Source Code (repository root)

```text
src/
|-- Controls/
|   |-- RetainedRender.fsi
|   `-- RetainedRender.fs
|-- SkiaViewer/
|   |-- PictureReplayCache.fsi
|   |-- PictureReplayCache.fs
|   |-- SkiaViewer.fsi
|   `-- SkiaViewer.fs
`-- Testing/
    |-- Testing.fsi
    `-- Testing.fs

tests/
|-- Controls.Tests/
|   |-- Feature159IdentitySplitTests.fs
|   |-- Feature159PromotionDecisionTests.fs
|   `-- Feature159ReuseCounterTests.fs
|-- SkiaViewer.Tests/
|   `-- Feature159ReplayReuseTests.fs
|-- Rendering.Harness/
|   |-- Compositor.fsi
|   |-- Compositor.fs
|   `-- Cli.fs
|-- Rendering.Harness.Tests/
|   |-- Feature159PromotionEvidenceTests.fs
|   `-- Feature159ReadinessPackageTests.fs
|-- Package.Tests/
|   `-- Feature159CompatibilityTests.fs
`-- Testing.Tests/
    `-- Feature159ReadinessHelperTests.fs
```

**Structure Decision**: `Controls.RetainedRender` owns content/placement identity production,
promotion eligibility, demotion, work-reduction counters, and safe retained decisions because it
has retained identity, prior frame, layout, and control-tree context. `SkiaViewer.PictureReplayCache`
owns the backend recorded-content reuse and native picture lifecycle, but it must consume split
content and placement evidence rather than accepting stale composite keys. `Rendering.Harness`
owns `compositor-promotion --feature 159`, scenario orchestration, parity capture, unsupported-host
results, and readiness rendering. `Testing` owns package-facing readiness assertions only if
generated products or package validation need stable helpers.

## Phase 0: Research

See [research.md](research.md). All planning unknowns are resolved:

- Feature 159 uses policy id `layer-promotion-v1` and harness command
  `compositor-promotion --feature 159`.
- Content identity is a local-content fingerprint over render-affecting inputs; placement identity
  is the boundary box, transform, scroll/offset, scale, and coverage evidence used to position or
  damage unchanged content.
- Promotion uses a three-frame stability observation window, existing `30%` repeated-work
  threshold, and net-positive saved-work-over-overhead rule.
- Required scenario coverage is `promotion/static-retained`, `promotion/placement-only-move`,
  `promotion/scroll-shifted`, `promotion/nested-retained`, `promotion/content-change`,
  `promotion/churn-demotion`, and `promotion/fallback-safe`.
- Accepted Feature 159 readiness requires at least three fresh same-profile attempts on
  `probe-08a47c01`; unsupported hosts publish zero accepted reuse or promotion artifacts.
- Feature 159 may accept reuse/promotion evidence, but final shipped performance remains
  `performance-not-accepted` until later timing and host-lane gates also pass.

## Phase 1: Design and Contracts

See [data-model.md](data-model.md) for entities, validation rules, and state transitions.

Observable contracts:

- [Content and Placement Identity Contract](contracts/content-placement-identity.md)
- [Layer Promotion Command Contract](contracts/layer-promotion-command.md)
- [Promotion and Reuse Evidence Contract](contracts/promotion-reuse-evidence.md)
- [Promotion Workflow Effects Contract](contracts/promotion-workflow-effects.md)
- [Readiness Package Contract](contracts/readiness-package.md)

Validation guide:

- [quickstart.md](quickstart.md)

## Post-Design Constitution Check

| Gate | Status | Design Evidence |
|------|--------|-----------------|
| Specification and classification | PASS | Scope and Tier 1 classification remain unchanged after design. |
| Spec -> FSI -> semantic tests -> implementation | PASS | Contracts identify public/observable promotion, reuse, demotion, counter, command, and readiness surfaces before implementation; tasks must put FSI and semantic tests before package-visible implementation. |
| Visibility lives in `.fsi` | PASS | Public diagnostic tokens and readiness helpers are declared in signature files; private identity hashing and replay-cache storage stay implementation-only. |
| Idiomatic simplicity | PASS | Design reuses existing `RetainedRender.promotionDecision`, Feature 157 damage scenarios, Feature 158 policy/claim discipline, and harness markdown renderers. |
| Elmish/MVU boundary | PASS | `promotion-workflow-effects.md` defines model, messages, effects, and edge interpreter responsibilities for stateful evidence collection. |
| Test evidence | PASS | Quickstart and contracts require focused tests, real same-profile attempts where available, unsupported-host regression, package validation, compatibility evidence, and broad P7 regression validation. |
| Observability and safe failure | PASS | Attempt status, primary reason, content identity, placement identity, counters, parity, host facts, artifact paths, and final claim status are required output fields. |
| Tier 1 obligations | PASS | Compatibility, package validation, public-surface drift checks, and readiness closeout are required artifacts for any public or consumer-visible delta. |

No post-design constitution violations are required.

## Complexity Tracking

No constitution violations or complexity exceptions are introduced.
