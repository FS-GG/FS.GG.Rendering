# Quickstart: Compositor Live Integration Validation

This guide lists the validation scenarios expected after implementation. It is a run guide, not
implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Native Skia/OpenGL dependencies already used by `SkiaViewer`.
- A capable display/headless GL profile for accepted live proof and real timing, or an environment
  that honestly reports `environment-limited`.
- Existing Feature 147 compositor diagnostics, proof contracts, harness commands, and readiness
  artifacts as the baseline.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds with warnings as errors.

## Live Preservation Proof

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148LiveProof
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --out specs/148-compositor-live-integration/readiness/live-proof
```

Expected outcome:

- Capable host profiles produce `passed` proof with matching untouched and damaged samples.
- Non-preserving hosts or simulations produce `failed`.
- Missing display, unsupported readback, timeout, or permissions produce `environment-limited`.
- Missing, stale, failed, environment-limited, synthetic-only, host-mismatched, or algorithm-
  mismatched proof cannot unlock damage-scoped redraw.

## Damage-Scoped Redraw Parity

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148Damage
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148Damage
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --feature 148 --out specs/148-compositor-live-integration/readiness/parity
```

Expected outcome:

- Localized update, overlapping damage, edge damage, movement/scrolling, resize, theme/global
  change, stale proof, and disabled mode scenarios match the full-frame oracle.
- Damage union area counts overlaps once and never exceeds frame area.
- Full-frame invalidations use full redraw or a full-frame damage region.
- Scissor/no-clear state resets before full redraw, readback, and subsequent frames.
- Fallback reasons are visible for unsupported, stale-proof, full-frame invalidation, disabled, and
  parity-failure paths.

## Content and Placement Reuse

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148Reuse
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature148CompositorMetrics
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-reuse --out specs/148-compositor-live-integration/readiness/reuse
```

Expected outcome:

- Stable content promotes only after observation and parity evidence.
- Placement-only movement reuses content at the new placement while damaging old and new covered
  regions.
- Content changes reject stale content and produce fresh output.
- Churning or non-beneficial boundaries demote or remain unpromoted with diagnostics.
- Frame metrics and harness evidence expose deterministic reuse, refresh, fallback, and demotion
  reasons.

## Snapshot Lifecycle

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148Snapshot
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148Snapshot
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-snapshots --out specs/148-compositor-live-integration/readiness/snapshots
```

Expected outcome:

- Expensive stable content creates and composes snapshots only after parity and benefit evidence.
- Entry count and byte estimate stay within configured budget.
- Invalid, stale, unsupported, or over-budget snapshots refresh, evict, demote, bypass, or fall
  back before use.
- Simple or churning content rejects or demotes snapshot reuse before sustained overhead.
- Unsupported host profiles disclose limitations and do not claim snapshot readiness.

## Real Timing Probes

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --tier damage --out specs/148-compositor-live-integration/readiness/timing
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --tier placement --out specs/148-compositor-live-integration/readiness/timing
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --tier snapshot --out specs/148-compositor-live-integration/readiness/timing
```

Expected outcome:

- Damage tier reports parity and reduced redraw work on target damage scenarios.
- Placement-only reuse reports at least 30% repeated-work reduction on moving/scrolling corpus.
- Simple/churning scenarios stay within 5% overhead or the responsible tier is demoted/rejected.
- Snapshot tier reports at least 20% frame-cost improvement on expensive stable corpus before it is
  marked ready.
- Each report names host profile, baseline tier, target tier, corpus, thresholds, environment, and
  verdict.
- Environment-limited timing is visible and cannot count as a shipped performance benefit.

## Readiness Package

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature148
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 148 --out specs/148-compositor-live-integration/readiness
```

Expected outcome:

- `validation-summary.md` names ready, limited, rejected, and skipped tiers.
- `compatibility-ledger.md` records public metrics, diagnostics, baselines, observable behavior
  changes, release notes, and migration guidance.
- Every ready tier links to live proof, parity, fallback, resource, timing, and compatibility
  evidence.
- Failed, rejected, skipped, or environment-limited tiers are visible and cannot count as shipped
  benefits.
- Reviewers can determine tier status and supporting artifacts within 10 minutes.

## Public Contract and Package Validation

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature148
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface
```

Expected outcome:

- Public surface baselines reflect intentional Tier 1 additions.
- Semantic FSI coverage exercises new public proof, diagnostics, metrics, harness, testing, or
  readiness surfaces.
- Compatibility ledger matches the refreshed surface baseline deltas.

## Full Readiness Pass

```bash
dotnet test FS.GG.Rendering.slnx
dotnet pack FS.GG.Rendering.slnx -c Release -o ~/.local/share/nuget-local
```

Expected outcome: relevant tests pass, package output succeeds, and readiness artifacts under
`specs/148-compositor-live-integration/readiness/` contain live proof, parity, fallback, reuse,
snapshot, timing, compatibility, and validation-summary evidence.
