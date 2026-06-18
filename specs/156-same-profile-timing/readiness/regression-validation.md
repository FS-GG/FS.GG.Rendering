# Feature 156 Regression Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature156 --no-build`: passed, 7 tests.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature156 --no-build`: passed, 3 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature156 --no-build`: passed, 3 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-build`: passed, 80 tests.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 156 --profile probe-08a47c01 --policy same-profile-live-threshold-v2 --warmup 3 --repetitions 5 --out specs/156-same-profile-timing/readiness/timing --json`: passed; timing verdict `noisy`.
- `env -u DISPLAY -u WAYLAND_DISPLAY -u XDG_SESSION_TYPE dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 156 --out specs/156-same-profile-timing/readiness/timing/unsupported --warmup 1 --repetitions 1`: passed; timing verdict `environment-limited`, accepted performance artifacts `0`, under 2 minutes.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 156 --out specs/156-same-profile-timing/readiness`: passed; package assembled and readiness summary reports `noisy`.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed; the slowest project was `Controls.Tests` at 5 m 21 s, with 876 passed, 1 skipped.
- `git diff --check`: passed.

## Safety Boundary

- Feature 155 correctness acceptance remains the P7 safety baseline.
- Unsupported-host validation remains fail-closed with zero accepted performance artifacts.
- Shipped P7 performance claim remains `performance-not-accepted`.
- Feature 160 remains a validation-throughput follow-up and is not a performance-acceptance gate.
