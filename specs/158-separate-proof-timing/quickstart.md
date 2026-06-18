# Quickstart: Separate Proof Readback From Timing

## Prerequisites

- .NET SDK for `net10.0`.
- Restored repository dependencies.
- A capable OpenGL/display profile for accepted same-profile timing, or an environment where
  unsupported-host output is expected.
- Existing Feature 155/157 proof evidence for profile `probe-08a47c01` when collecting accepted
  timing evidence.

## 1. Build the Harness

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected: build succeeds. If package-visible `.fsi` changes were made, surface-baseline validation
must also be run before closeout.

## 2. Run Readback-Free Timing

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 158 \
  --out specs/158-separate-proof-timing/readiness/timing \
  --policy readback-free-timing-v1 \
  --warmup 3 \
  --repetitions 5
```

Expected:

- `timing/summary.md` is written.
- Required scenario reports are written under `timing/scenarios/`.
- Raw sample files include measurement policy and inclusion status.
- Accepted samples declare `readback-free` or `readback-outside-measurement`.
- Any contaminated, missing-policy, cross-profile, or unsupported samples are listed under
  `timing/excluded/`.

## 3. Run an Explicit Probe

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 158 --probe-readback \
  --out specs/158-separate-proof-timing/readiness/timing
```

Expected:

- Probe/readback artifacts are written or linked from `readiness/proof-probes/`.
- Probe samples declare `probe-readback-included`.
- Probe samples are excluded from performance acceptance with reason `probe-run-excluded`.
- Existing readback-free `timing/summary.md` and `timing/summary.json` files are preserved when
  the probe writes into the same timing output directory.

## 4. Assemble Readiness

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 158 \
  --out specs/158-separate-proof-timing/readiness
```

Expected:

- `validation-summary.md` links timing, excluded samples, proof/probe evidence, compatibility,
  package, and regression artifacts.
- Final measurement-separation status is `accepted`, `rejected`, `fallback-only`, or
  `environment-limited`.
- Shipped performance claim remains `performance-not-accepted`.
- The summary states whether Feature 158 supersedes, confirms, or only contextualizes Feature 156
  noisy timing evidence.

## 5. Run Unsupported-Host Validation

```bash
env -u DISPLAY -u WAYLAND_DISPLAY \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 158 \
  --out specs/158-separate-proof-timing/readiness/timing/unsupported
```

Expected:

- Command completes in under 2 minutes.
- Output is `environment-limited`.
- Accepted proof artifacts: `0`.
- Accepted performance artifacts: `0`.

## 6. Run Focused Tests

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature158"
dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature158"
dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature158"
```

Expected: focused Feature 158 tests pass. If public surface changes are made, run the repository
surface-baseline refresh/check and record the outcome in `readiness/package-validation.md`.

## 7. Record Regression Validation

Run the focused Feature 155, Feature 156, and Feature 157 filters that protect current proof,
timing, no-clear damage-scissored readiness, fallback, unsupported-host, and package boundaries.
Record outcomes in `readiness/regression-validation.md` before implementation closeout.
