# Feature 160 Regression Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature160"`: passed, 10 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter "Feature160"`: passed, 4 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature160"`: passed, 3 tests.
- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed after `dotnet restore FS.GG.Rendering.slnx --force` repaired the local package cache.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed across the solution on final retry; an earlier retry aborted on a host/windowing crash and `SkiaViewer.Tests` passed in isolation before the final retry.

## Preservation

- Feature 155 proof correctness remains preserved by broad solution validation.
- Feature 157 no-clear damage readiness remains preserved by broad solution validation.
- Feature 158 readback-free timing separation and required scenario set remain preserved by focused Feature 160 tests.
- Feature 159 reuse/promotion readiness remains a separate performance-claim gate.
- Unsupported-host output remains fail-closed with zero accepted same-profile performance artifacts.
- Public-surface drift is recorded in Feature 160 FSI evidence.
