# Feature 161 Regression Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `compositor-readiness --feature 161`: package assembled.
- Feature 155, 157, 158, 159, and 160 preservation evidence remains linked.
- Unsupported-host validation records zero accepted lane-scoped performance artifacts.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed, exit code 0.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature161"`: passed, 11 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature161"`: passed, 4 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature161"`: passed, 4 tests.

## Preservation

- Feature 155 proof correctness remains preserved.
- Feature 157 no-clear damage-scissored readiness remains preserved.
- Feature 158 readback-free timing separation remains preserved.
- Feature 159 reuse/promotion evidence remains a separate performance-claim gate.
- Feature 160 throughput evidence remains accepted only within its focused validation boundary.
- Full-redraw fallback and unsupported-host fail-closed behavior remain unchanged.
- Public-surface drift is recorded in Feature 161 FSI evidence.
