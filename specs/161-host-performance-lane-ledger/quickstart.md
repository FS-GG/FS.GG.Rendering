# Quickstart: Host Performance Lane Ledger

## Prerequisites

- .NET SDK for `net10.0`.
- Restored repository dependencies.
- A capable OpenGL/display profile for accepted same-profile evidence, or an environment where
  unsupported-host output is expected.
- Existing Feature 155, Feature 157, Feature 158, Feature 159, and Feature 160 readiness context
  for profile `probe-08a47c01`.

## 1. Build the Solution

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected: build succeeds. If package-visible `.fsi` changes were made, surface-baseline validation
must also be run before closeout.

## 2. Collect Lane-Scoped Performance Evidence

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 161 \
  --lane host-ledger \
  --out specs/161-host-performance-lane-ledger/readiness/lane-ledger \
  --policy host-lane-ledger-v1 \
  --source-throughput specs/160-performance-validation-throughput/readiness/throughput
```

Expected:

- `lane-ledger/summary.md` is written.
- Ledger entries are written under `lane-ledger/entries/`.
- Raw host fact records are written under `lane-ledger/host-facts/`.
- Every accepted entry includes display server, display identity, renderer identity, direct
  rendering status, refresh rate or reason unavailable, driver identity, package version set,
  CPU/GPU load notes, known environment limits, host profile, run identity, scenario identity,
  policy identity, collection time, and artifact paths.
- Missing, contradictory, stale, cross-run, cross-lane, or noisy evidence is written under
  `lane-ledger/excluded/` with primary reason tokens.
- The command does not claim universal performance.

## 3. Run Unsupported-Host Validation

```bash
env -u DISPLAY -u WAYLAND_DISPLAY \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 161 \
  --lane host-ledger \
  --out specs/161-host-performance-lane-ledger/readiness/lane-ledger/unsupported \
  --policy host-lane-ledger-v1
```

Expected:

- Output is `environment-limited` or `fallback-only`.
- Accepted lane-scoped performance artifacts: `0`.
- The unsupported-host report names the environment limit and links any captured facts.

## 4. Assemble Readiness

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 161 \
  --out specs/161-host-performance-lane-ledger/readiness
```

Expected:

- `readiness/validation-summary.md` links lane entries, host facts, excluded evidence,
  unsupported-host evidence, prior P7 gates, compatibility, package, and regression artifacts.
- The summary identifies the current accepted lane only if collected facts confirm X11 `:1` with
  direct OpenGL on AMD Radeon/Mesa for profile `probe-08a47c01`.
- The summary states that the accepted lane is not generalized to Wayland, indirect GL,
  missing-display, software-raster, virtualized, or unknown lanes unless separately accepted.
- The shipped compositor performance claim remains `performance-not-accepted` unless every
  report-defined timing, reuse, throughput, and lane gate is complete and positive.

## 5. Run Focused Tests

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161"
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature161"
dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature161"
```

Expected: focused Feature 161 tests pass. If public Testing helpers are not added, the
`Testing.Tests` Feature 161 filter may be omitted and the compatibility ledger must state that no
Testing surface changed.

## 6. Run Full Solution Validation

```bash
dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected: full solution validation passes and its command, status, duration, and output artifact
location are recorded under `readiness/full-validation/`. If public surface or package output
changes, record surface-baseline and package validation outcomes in `readiness/package-validation.md`.

## 7. Assemble Final Readiness

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 161 \
  --out specs/161-host-performance-lane-ledger/readiness
```

Expected:

- `readiness/validation-summary.md` gives a reviewer enough information to evaluate lane scope in
  under 5 minutes.
- Complete lane facts, excluded evidence, prior-gate status, unsupported-host behavior,
  compatibility impact, artifact paths, and final claim status are visible.
- Cross-lane evidence is not combined.
- Unsupported-host evidence records zero accepted lane-scoped performance artifacts.
- Feature 161 is not release-ready unless full validation is current and passing.
