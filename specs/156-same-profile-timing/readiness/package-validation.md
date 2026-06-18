# Feature 156 Package Validation

Status: `accepted-with-recorded-limitations`

## Surface and Package Checks

- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed with 0 warnings and 0 errors after Feature 156 code and evidence updates.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature156 --no-build`: passed, 3 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature156 --no-build`: VSTest reported no matching test cases for this Expecto module filter; package coverage was run without the VSTest filter.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-build`: passed, 80 tests.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed; refreshed `FS.GG.UI.SkiaViewer` with 267 public types and `FS.GG.UI.Testing` with 79 public types.
- `dotnet pack FS.GG.Rendering.slnx --no-build -c Debug -o /tmp/fs-gg-rendering-pack-feature156`: passed; produced local `0.1.18-preview.1` packages for packable projects.

## Public Surface

- SkiaViewer and Testing surface baselines are refreshed when `.fsi` public timing helpers change.
- Package FSI transcript coverage is recorded under `readiness/fsi/`.
- Added SkiaViewer timing-path and proof-overhead disclosure helpers are additive.
- Added Testing `CompositorTimingAssertions` helper is additive and keeps `performance-not-accepted` separate from timing verdicts.

## FSI Transcript Evidence

- `fsi/compositor-performance-authoring.fsx` and `.log`: pass, cover SkiaViewer timing path, proof-overhead disclosure, Testing summary validation, policy id, and `performance-not-accepted`.
- `fsi/compositor-readiness-authoring.fsx` and `.log`: pass, cover readiness rendering, `CompositorReadiness.validate`, timing verdict text, and `performance-not-accepted`.
