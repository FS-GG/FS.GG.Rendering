# Quickstart: Performance Validation Throughput

## Prerequisites

- .NET SDK for `net10.0`.
- Restored repository dependencies.
- A capable OpenGL/display profile for accepted same-profile evidence, or an environment where
  unsupported-host output is expected.
- Existing Feature 155, Feature 157, Feature 158, and Feature 159 readiness context for profile
  `probe-08a47c01`.

## 1. Build the Solution

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected: build succeeds. If package-visible `.fsi` changes were made, surface-baseline validation
must also be run before closeout.

## 2. Collect Focused Throughput Evidence

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 160 \
  --lane focused \
  --out specs/160-performance-validation-throughput/readiness/throughput \
  --policy focused-throughput-v1 \
  --attempts 3 \
  --max-iteration-minutes 10
```

Expected:

- `throughput/summary.md` is written.
- At least three fresh same-profile iteration records are written under `throughput/iterations/`.
- Each accepted iteration completes under 10 minutes.
- Each accepted iteration covers the five required timing scenarios.
- Each accepted iteration records duration, bound, sample count, scenario coverage, host profile,
  run identity, inclusion status, and artifact paths.
- Excluded evidence is written under `throughput/excluded/` with primary reason tokens.
- The focused command does not run broad release validation as part of each iteration.

## 3. Run Unsupported-Host Validation

```bash
env -u DISPLAY -u WAYLAND_DISPLAY \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 160 \
  --lane focused \
  --out specs/160-performance-validation-throughput/readiness/throughput/unsupported \
  --policy focused-throughput-v1 \
  --attempts 1 \
  --max-iteration-minutes 2
```

Expected:

- Command completes in under 2 minutes.
- Output is `environment-limited`.
- Accepted same-profile performance artifacts: `0`.

## 4. Assemble Readiness Before Full Validation

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 160 \
  --out specs/160-performance-validation-throughput/readiness
```

Expected when full validation has not yet been recorded:

- `readiness/validation-summary.md` reports focused throughput status separately from full
  validation status.
- Release-ready status is blocked if full validation is missing, failing, interrupted, stale, or
  undocumented.
- Shipped compositor performance claim remains `performance-not-accepted` unless all later
  report-defined gates are present and positive.

## 5. Run Focused Tests

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature160"
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature160"
dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature160"
```

Expected: focused Feature 160 tests pass. If public Testing helpers are not added, the
`Testing.Tests` Feature 160 filter may be omitted and the compatibility ledger must state that no
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
  compositor-readiness --feature 160 \
  --out specs/160-performance-validation-throughput/readiness
```

Expected:

- `readiness/validation-summary.md` links throughput iterations, excluded evidence,
  unsupported-host evidence, full validation, compatibility, package, and regression artifacts.
- Focused throughput status and full validation status are separate decisions.
- Feature 160 is not release-ready unless full validation is current and passing.
- The shipped compositor performance claim remains `performance-not-accepted` unless same-profile
  timing is not noisy, Feature 159 reuse/promotion counters are net-positive, Feature 160
  throughput is accepted, and Feature 161 host-lane scoping is accepted.
