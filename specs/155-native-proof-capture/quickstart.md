# Quickstart: Native Proof Capture

## Prerequisites

- Repository restore completed for `net10.0`.
- Current host has a reachable display and direct graphics rendering.
- For unsupported-host validation, use a shell where display variables can be unset.

## Confirm Host Capability

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- probe --json
glxinfo -B
xdpyinfo
```

Expected outcome:

- Harness probe records a display-backed effective backend.
- Graphics probe records direct rendering and a renderer identity.
- Display probe answers successfully.

## Build

```bash
dotnet build FS.GG.Rendering.slnx --no-restore
```

Expected outcome: solution builds without warnings or public-surface drift.

## Focused Validation

```bash
dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature155 --no-build
dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature155 --no-build
dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature155 --no-build
```

Expected outcome:

- Proof workflow transitions and failure paths pass.
- Native capture output and readiness rendering pass.
- Package compatibility and any public surface checks pass.

## Capable-Host Proof Capture

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-live-proof --feature 155 \
  --attempt-count 3 \
  --out specs/155-native-proof-capture/readiness/live-proof/attempts
```

Expected outcome:

- Host is classified as capable.
- Three selected attempts are written.
- Each selected attempt includes sentinel and damage artifacts.
- Attempt quality is fresh, decodable, non-blank, and non-synthetic.
- Damaged pixels update and undamaged pixels preserve sentinel identity.
- `proof-set.md` records `accepted` and selected attempts `3/3`.

## Unsupported-Host Regression

```bash
env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE \
  dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-live-proof --feature 155 \
  --out specs/155-native-proof-capture/readiness/live-proof/unsupported
```

Expected outcome:

- Result is `environment-limited`.
- Accepted partial-redraw artifacts remain `0`.

## Same-Profile Parity and Timing

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-parity --feature 155 \
  --out specs/155-native-proof-capture/readiness/parity

dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-timing --feature 155 \
  --tier damage \
  --scenario-count 5 \
  --repetitions 5 \
  --out specs/155-native-proof-capture/readiness/timing
```

Expected outcome:

- Parity records all required P7 damage paths and accepts only same-profile matching output or safe
  fallback reasons.
- Timing records accepted, rejected, or inconclusive performance status separately from correctness
  readiness.

## Final Readiness

```bash
dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- \
  compositor-readiness --feature 155 \
  --out specs/155-native-proof-capture/readiness

dotnet test FS.GG.Rendering.slnx --no-restore
```

Expected outcome:

- `validation-summary.md` states P7 partial-redraw correctness accepted for the current host profile
  when proof and parity pass.
- Performance claim status is explicit and separate.
- Broad regression validation passes.
