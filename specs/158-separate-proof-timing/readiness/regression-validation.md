# Feature 158 Regression Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `compositor-readiness --feature 158`: package assembled.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --filter "Feature158"`: passed, 8 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --filter "Feature155|Feature156|Feature157|Feature158"`: passed, 27 tests.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 158 --probe-readback --out specs/158-separate-proof-timing/readiness/timing`: passed; proof readback is recorded as `probe-run-excluded`.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 158 --out specs/158-separate-proof-timing/readiness/timing --policy readback-free-timing-v1 --warmup 3 --repetitions 5 --json`: passed; 50 accepted readback-free samples across five scenarios.
- `env -u DISPLAY -u WAYLAND_DISPLAY dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-performance --feature 158 --out specs/158-separate-proof-timing/readiness/timing/unsupported`: passed; unsupported host is `environment-limited` with zero accepted proof artifacts and zero accepted performance artifacts.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 158 --out specs/158-separate-proof-timing/readiness`: passed.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed; final project output was `Controls.Tests` with 876 passed, 1 skipped, total 877, duration 5m12s.
- `git diff --check`: passed.

## Preservation

- Feature 155 proof and parity acceptance remains the correctness gate.
- Feature 156 timing remains context-only and available for comparison.
- Feature 157 damage-scissored no-clear readiness remains accepted for the current stable profile.
- Unsupported-host validation remains fail-closed with zero accepted proof artifacts and zero accepted performance artifacts.
- Shipped P7 performance claim remains `performance-not-accepted`.
