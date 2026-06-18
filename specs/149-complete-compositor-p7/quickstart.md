# Quickstart: Complete P7 Compositor Validation

This guide lists the validation scenarios expected after implementation. It is a run guide, not
implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Native Skia/OpenGL dependencies already used by `SkiaViewer`.
- A capable display/headless GL profile for accepted live proof and real timing, or an environment
  that honestly reports `environment-limited`.
- Feature 147 and Feature 148 compositor contracts, tests, harness commands, and readiness
  artifacts as the baseline.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds with warnings as errors.

## Live Proof Acceptance

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149LiveProof
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --feature 149 --out specs/149-complete-compositor-p7/readiness/live-proof
```

Expected outcome:

- Capable hosts produce accepted proof with matching untouched and damaged samples.
- At least 3 consecutive capable-host proof runs produce accepted artifacts with no stale, blank,
  or missing-artifact failures.
- Non-preserving, stale, blank, or simulated failure hosts produce `failed`.
- Missing display, unsupported readback, timeout, or permissions produce `environment-limited`.
- Missing, stale, failed, environment-limited, synthetic-only, blank, host-mismatched, or
  algorithm-mismatched proof cannot unlock damage-scoped redraw.

## Damage-Scoped Redraw Parity

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature149Damage
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149Damage
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --feature 149 --out specs/149-complete-compositor-p7/readiness/parity
```

Expected outcome:

- Accepted-proof damage-scoped frames match full-redraw references for every representative corpus
  frame.
- Localized update, overlapping damage, edge damage, movement/scrolling, resize, theme/global
  change, stale proof, disabled mode, unsupported host, and parity-failure scenarios record clear
  verdicts.
- Damage union area counts overlaps once and never exceeds frame area.
- Zero-damage frames preserve prior valid output only when preservation can be guaranteed.
- Full-frame invalidations use full redraw or a full-frame damage region.
- Scissor/no-clear state resets before full redraw, readback, and subsequent frames.
- Fallback reasons are visible for unsupported, stale-proof, full-frame invalidation, disabled,
  resource-failure, internal-error, and parity-failure paths.

## Reuse and Snapshot Evidence

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature149Reuse
dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature149CompositorMetrics
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149Snapshot
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-reuse --feature 149 --out specs/149-complete-compositor-p7/readiness/reuse
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-snapshots --feature 149 --out specs/149-complete-compositor-p7/readiness/snapshots
```

Expected outcome:

- Stable content promotes only after observation and parity evidence.
- Placement-only movement reuses content at the new placement while damaging old and new covered
  regions.
- Content changes reject stale content and produce fresh output.
- Churning or non-beneficial boundaries demote or remain unpromoted with diagnostics.
- Expensive stable content creates, composes, refreshes, replaces, evicts, and disposes snapshots
  with bounded resource evidence.
- Invalid, stale, unsupported, or over-budget snapshots refresh, evict, demote, bypass, or fall
  back before use.
- Snapshot-assisted output participates in full-redraw oracle parity before readiness claims.

## Timing Probes

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --feature 149 --tier damage --out specs/149-complete-compositor-p7/readiness/timing
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --feature 149 --tier placement --out specs/149-complete-compositor-p7/readiness/timing
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --feature 149 --tier snapshot --out specs/149-complete-compositor-p7/readiness/timing
```

Expected outcome:

- Reports include host profile, baseline tier, target tier, corpus, warmup frames, measured
  frames, thresholds, environment facts, and verdict.
- Damage tier reports comparable measurements against full redraw.
- Placement/replay reuse reports comparable measurements against the lower redraw tier and full
  redraw where relevant.
- Snapshot tier reports comparable measurements against lower reuse tiers and full redraw.
- Beneficial corpora and non-beneficial corpora are both represented.
- Incomplete, noisy, or environment-limited timing is marked inconclusive/limited and cannot count
  as a shipped performance benefit.

## Public Diagnostics and Package Validation

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature149
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature149
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface
```

Expected outcome:

- Public surface baselines reflect only intentional compositor additions.
- Semantic FSI coverage exercises new public proof, fallback, parity, reuse, snapshot, timing, and
  readiness diagnostics.
- Testing helpers remain consumer-validation focused and do not pull broad implementation
  projects into generated products.
- Compatibility ledger matches the refreshed surface baseline deltas.

## Readiness Package

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature149
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 149 --out specs/149-complete-compositor-p7/readiness
```

Expected outcome:

- `validation-summary.md` states whether P7 is accepted, environment-limited, failed, or
  incomplete.
- `compatibility-ledger.md` records public metrics, diagnostics, baselines, observable behavior
  changes, release notes, and migration guidance.
- Every accepted claim links to live proof, parity, fallback, resource, timing, and compatibility
  evidence.
- Failed, rejected, skipped, incomplete, inconclusive, or environment-limited tiers are visible and
  cannot count as shipped benefits.
- A maintainer can determine P7 status and supporting artifact paths from one summary.

## Regression and Pack Validation

```bash
dotnet test FS.GG.Rendering.slnx
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t PackLocal
```

Expected outcome:

- Existing P5, P6, and P7 evidence surfaces pass with no undocumented behavior changes.
- Disabled-cache, full-redraw, render-anywhere, overlay, text-shaping, package-readiness, and
  surface-baseline guarantees remain valid.
- Package output succeeds under `~/.local/share/nuget-local/`.
