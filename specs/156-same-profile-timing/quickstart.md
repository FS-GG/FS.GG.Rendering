# Quickstart: Same-Profile Timing Evidence

## Prerequisites

- Repository restore completed for `net10.0`.
- Feature 155 proof and same-profile parity evidence is present under
  `specs/155-native-proof-capture/readiness/`.
- Current host matches Feature 155 accepted profile `probe-08a47c01`.
- For unsupported-host validation, use a shell where display variables can be unset.

## Confirm Baseline Host and Correctness Evidence

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- probe --json
sed -n '1,120p' specs/155-native-proof-capture/readiness/validation-summary.md
```

Expected outcome:

- Probe output records the current display-backed OpenGL host facts.
- Feature 155 summary states proof set `accepted`, parity status `accepted`, fallback status
  `partial-redraw-accepted`, accepted host profile `probe-08a47c01`, and performance claim
  `not-accepted`.

## Build

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds without warnings or public-surface drift.

## Focused Validation

```bash
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature156 --no-build
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature156 --no-build
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature156 --no-build
```

Expected outcome:

- Policy and distribution tests pass.
- Same-profile rejection, incomplete sample rejection, noisy/non-beneficial rejection, and
  unsupported-host regression tests pass.
- Package compatibility and FSI coverage pass for any public surface delta.

## Collect Same-Profile Timing Evidence

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 156 \
  --profile probe-08a47c01 \
  --policy same-profile-live-threshold-v2 \
  --warmup 3 \
  --repetitions 5 \
  --out specs/156-same-profile-timing/readiness/timing
```

Expected outcome:

- The run binds to host profile `probe-08a47c01`.
- The command records five required scenarios:
  `timing/localized-update`, `timing/no-change`, `timing/movement-old-new`, `timing/overlap`,
  and `timing/edge-clipping`.
- Each scenario records full-redraw and damage-scoped samples with warmup count, measured sample
  count, p50, p95, p99, noise band, verdict, confidence decision, and artifact paths.
- `timing/summary.md` states the Feature 156 timing verdict for the measured profile.
- The shipped P7 performance claim remains `performance-not-accepted` until later performance
  gates pass.

## Unsupported-Host Regression

```bash
env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-performance --feature 156 \
  --out specs/156-same-profile-timing/readiness/timing/unsupported
```

Expected outcome:

- Result is `environment-limited`.
- Accepted performance artifacts remain `0`.
- The command completes in under 2 minutes.

## Final Readiness Package

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 156 \
  --out specs/156-same-profile-timing/readiness

dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected outcome:

- `validation-summary.md` links Feature 155 proof/parity, timing summary, scenario reports,
  raw samples, unsupported-host output, compatibility notes, package validation, and regression
  validation.
- No noisy, incomplete, cross-profile, environment-limited, limited, or non-beneficial result is
  counted as positive timing evidence.
- Broad regression validation passes.
