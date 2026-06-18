# Quickstart: Layer Promotion and Content/Transform Key Split

## Prerequisites

- .NET SDK for `net10.0`.
- Restored repository dependencies.
- A capable OpenGL/display profile for accepted same-profile evidence, or an environment where
  unsupported-host output is expected.
- Existing Feature 155, Feature 157, and Feature 158 readiness context for profile
  `probe-08a47c01`.

## 1. Build the Solution

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected: build succeeds. If package-visible `.fsi` changes were made, surface-baseline validation
must also be run before closeout.

## 2. Collect Promotion and Reuse Evidence

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-promotion --feature 159 \
  --out specs/159-layer-promotion-keys/readiness/promotion \
  --policy layer-promotion-v1 \
  --attempts 3
```

Expected:

- `promotion/summary.md` is written.
- Attempt records are written under `promotion/attempts/`.
- Reuse records identify content identity and placement identity separately.
- Demotion and fallback records use stable primary reason tokens.
- Counters distinguish avoided content work, placement-only reuse, content re-recording,
  demotions, fallback decisions, and promotion overhead.
- Accepted records target same-profile host `probe-08a47c01`.

## 3. Assemble Readiness

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 159 \
  --out specs/159-layer-promotion-keys/readiness
```

Expected:

- `readiness/validation-summary.md` links promotion attempts, reuse evidence, demotions,
  fallbacks, counters, parity, compatibility, package, and regression artifacts.
- Final Feature 159 status is `accepted`, `non-beneficial`, `fallback-only`, `rejected`, or
  `environment-limited`.
- Shipped compositor performance claim remains `performance-not-accepted` unless later timing and
  host-lane gates are also present and positive.

## 4. Run Unsupported-Host Validation

```bash
env -u DISPLAY -u WAYLAND_DISPLAY \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-promotion --feature 159 \
  --out specs/159-layer-promotion-keys/readiness/promotion/unsupported \
  --policy layer-promotion-v1 \
  --attempts 1
```

Expected:

- Command completes in under 2 minutes.
- Output is `environment-limited`.
- Accepted Feature 159 reuse artifacts: `0`.
- Accepted Feature 159 promotion artifacts: `0`.

## 5. Run Focused Tests

```bash
dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-restore --filter "Feature159"
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-restore --filter "Feature159"
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature159"
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature159"
dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature159"
```

Expected: focused Feature 159 tests pass. If public surface changes are made, refresh/check
surface baselines and record the outcome in `readiness/package-validation.md`.

## 6. Record Regression Validation

Run focused Feature 155, Feature 157, and Feature 158 filters that protect proof correctness,
no-clear damage-scissored readiness, proof/readback separation, unsupported-host fail-closed
behavior, package validation, and public compatibility checks. Record outcomes in
`readiness/regression-validation.md` before implementation closeout.
