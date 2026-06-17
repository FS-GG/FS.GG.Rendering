# Quickstart: Compositor Damage Redraw Validation

This guide lists the validation scenarios expected after implementation. It is a run guide, not
implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Native Skia/OpenGL dependencies already used by `SkiaViewer`.
- A capable display/headless GL environment for accepted present-path proof, or an environment that
  can honestly report `environment-limited`.
- Existing rendering harness artifacts and corpus support from prior retained/render-anywhere
  features.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds with warnings as errors.

## Present-Path Proof Validation

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature147PresentPathProof
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-present-proof --out specs/147-compositor-damage-redraw/readiness/present-proof
```

Expected outcome:

- Capable host profiles produce `passed` proof with host profile facts and artifact identities.
- Fresh-clearing, simulated failing, missing display, or unsupported observation paths return
  `failed` or `environment-limited`.
- Damage-scissored redraw remains disabled for missing, stale, failed, host-mismatched, or
  environment-limited proof.

## Damage-Scissored Redraw Parity

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature147Damage
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature147ScissorRedraw
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --out specs/147-compositor-damage-redraw/readiness/parity
```

Expected outcome:

- Localized update, overlapping damage, movement/scrolling, resize, theme change, and full-frame
  invalidation scenarios match the full-redraw oracle.
- Damage union area counts overlaps once and never exceeds frame area.
- Full-frame invalidations use full redraw or a full-frame damage region.
- Scissor state does not leak into later full redraw or readback paths.

## Promotion and Placement Reuse

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature147Promotion
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature147CompositorMetrics
```

Expected outcome:

- Stable boundaries promote only after observation and parity evidence.
- Placement-only movement reuses content at the new placement while damaging old and new covered
  regions.
- Content changes reject stale content and force fresh output.
- Churning or no-benefit boundaries demote or remain unpromoted with diagnostics.
- Frame metrics expose deterministic promotion/reuse/demotion evidence for the harness.

## Snapshot Resource Tier

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature147Snapshot
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature147Snapshot
```

Expected outcome:

- Snapshot resources are allocated only for expensive stable content with passing parity.
- Resource entry count and byte estimate stay within configured budget.
- Invalid, stale, unsupported, or over-budget snapshots refresh, evict, demote, or fall back before
  use.
- Unsupported host profiles report limitations and do not claim snapshot readiness.

## Performance Probes

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-perf --tier damage --out specs/147-compositor-damage-redraw/readiness/perf
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-perf --tier promotion --out specs/147-compositor-damage-redraw/readiness/perf
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-perf --tier snapshot --out specs/147-compositor-damage-redraw/readiness/perf
```

Expected outcome:

- Damage tier reports parity plus repaint-work reduction on target damage scenarios.
- Promotion tier shows at least 30% repeated-work reduction on stable/moving corpus.
- Simple/churning corpus stays within 5% overhead or the responsible tier demotes.
- Snapshot tier shows at least 20% frame-cost improvement on expensive stable corpus before it is
  reported ready.
- Each report names baseline tier, target tier, corpus, thresholds, environment, and verdict.

## Readiness Package

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature147
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --out specs/147-compositor-damage-redraw/readiness
```

Expected outcome:

- `validation-summary.md` names ready, limited, rejected, and skipped tiers.
- `compatibility-ledger.md` records public metrics, diagnostics, baselines, observable behavior
  changes, release notes, and migration guidance.
- Failed or environment-limited tiers are visible and cannot count as shipped benefits.
- Reviewers can determine tier status and supporting artifacts within 10 minutes.

## Public Contract and Package Validation

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature147
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface
```

Expected outcome:

- Public surface baselines reflect intentional Tier 1 additions.
- Semantic FSI coverage exercises any new public proof, metrics, harness, or testing surfaces.
- Compatibility ledger matches the refreshed surface baseline deltas.

## Full Readiness Pass

```bash
dotnet test FS.GG.Rendering.slnx
dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local
```

Expected outcome: relevant tests pass, package output succeeds, and readiness artifacts under
`specs/147-compositor-damage-redraw/readiness/` contain proof, parity, performance, snapshot,
compatibility, and validation-summary evidence.
