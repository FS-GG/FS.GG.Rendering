# Quickstart: Compositor Live Proof Acceptance

This guide lists the validation scenarios expected after implementation. It is a run guide, not
implementation code.

## Prerequisites

- .NET SDK capable of building `net10.0`.
- Native Skia/OpenGL dependencies already used by `SkiaViewer`.
- A capable display or headless GL profile for accepted live proof and real timing, or an
  environment that honestly reports `environment-limited`.
- Feature 149 readiness artifacts and contracts as the baseline.

## Setup

```bash
dotnet restore FS.GG.Rendering.slnx
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds with warnings as errors.

## Live Proof Run Set

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature152LiveProof
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature152LiveProof
for run in 1 2 3; do
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --feature 152 --out specs/152-compositor-live-proof/readiness/live-proof/run-$run
done
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 152 --out specs/152-compositor-live-proof/readiness
```

Expected outcome:

- Capable hosts produce at least three fresh matching accepted proof attempts for the same host
  profile and proof method.
- Every accepted attempt includes non-missing, non-blank, non-synthetic artifacts showing damaged
  pixels changed and undamaged pixels remained valid.
- Missing, stale, blank, synthetic-only, host-mismatched, proof-method-mismatched, failed, or
  environment-limited evidence fails closed.
- The readiness summary records whether the proof set is accepted, failed, environment-limited, or
  fallback-gated.

## Unsupported Host Path

```bash
env -u DISPLAY -u WAYLAND_DISPLAY dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --feature 152 --out specs/152-compositor-live-proof/readiness/live-proof/unsupported
```

Expected outcome:

- The command completes in under 2 minutes.
- The verdict is `environment-limited`.
- Zero accepted partial-redraw artifacts are recorded.
- The limitation is visible in `validation-summary.md`.

## Damage-Scoped Live Parity

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature152Damage
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature152Damage
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature152Parity
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --feature 152 --out specs/152-compositor-live-proof/readiness/parity
```

Expected outcome:

- On the same accepted host profile, damage-scoped live output matches the full-redraw oracle for
  every accepted representative scenario.
- Localized update, no-change, movement, overlapping damage, edge-clipped damage, resize,
  frame-wide invalidation, invalid damage, unsupported host, and resource-failure paths all record
  explicit verdicts.
- Unsafe paths use full redraw or another safe fallback with a reviewer-visible reason.
- Failed, skipped, or environment-limited parity does not count as accepted partial-redraw
  evidence.

## Timing Claim Decision

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature152Timing
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --feature 152 --tier damage --out specs/152-compositor-live-proof/readiness/timing
```

Expected outcome:

- Timing evidence covers at least 5 representative live scenarios with at least 5 comparable
  repetitions per scenario before any benefit is accepted.
- Reports include host profile, proof set, parity links, baseline full-redraw metrics,
  damage-scoped metrics, thresholds, warmup/measured frame counts, and verdict.
- Incomplete, noisy, environment-limited, or non-beneficial measurements record no accepted
  performance claim.
- The readiness summary separates correctness acceptance from performance-claim acceptance.

## Public Diagnostics and Package Validation

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature152
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature152
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface
```

Expected outcome:

- Public surface baselines reflect only intentional compositor readiness or diagnostic deltas.
- Semantic FSI coverage exercises any new public proof, fallback, parity, timing, readiness, or
  testing-helper surface.
- The compatibility ledger documents all public API, diagnostic, fallback, readiness, and package
  effects.
- Undocumented public drift fails validation.

## Readiness Package

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature152Readiness
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 152 --out specs/152-compositor-live-proof/readiness
```

Expected outcome:

- `validation-summary.md` states whether P7 live partial redraw is accepted,
  environment-limited, failed, or fallback-gated.
- Every accepted correctness claim links to proof-set and parity evidence.
- The performance claim is accepted only with same-profile timing evidence, otherwise rejected or
  inconclusive.
- Failed, rejected, skipped, incomplete, inconclusive, or environment-limited evidence is visible
  and cannot count as a shipped benefit.
- A reviewer can determine status and supporting artifact paths from one summary in under
  5 minutes.

## Regression and Pack Validation

```bash
dotnet test FS.GG.Rendering.slnx --no-restore
./fake.sh build -t PackageSurfaceCheck
./fake.sh build -t PackLocal
```

Expected outcome:

- Feature 149 diagnostics, deterministic readiness, fallback guarantees, and adjacent rendering
  readiness surfaces remain valid.
- Feature 151 P8 layout acceptance remains valid and independent from P7 compositor claims.
- Render-anywhere, overlay, text-shaping, layout, package-readiness, and surface-baseline
  guarantees pass or record explicit non-accepting limitations.
- Package output succeeds under `~/.local/share/nuget-local/`.
