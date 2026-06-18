# Feature 157 Regression Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature157 --no-build`: passed, 8/8.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature155|Feature156|Feature157" --no-build`: passed, 19/19.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature157 --no-build`: passed, 5/5.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature157 --no-build`: passed, 4/4.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature157 --no-build`: passed, 5/5.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature157 --no-restore`: passed, 5/5 after synthetic disclosure labels.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature157 --no-restore`: passed, 8/8 after synthetic disclosure labels.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature157 --no-restore`: passed, 4/4 after synthetic disclosure labels.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature157 --no-restore`: passed, 5/5 after synthetic disclosure labels.
- Unsupported-host command with display variables unset: passed in 10s with `environment-limited` and zero accepted partial-redraw artifacts.
- `git diff --check`: passed.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: started and showed early project passes, then was interrupted after several minutes without output; broad-suite completion is recorded as tooling-limited for this run rather than passed.

## Preservation

- Feature 155 proof and parity acceptance remains the correctness gate.
- Feature 156 timing remains context-only and `performance-not-accepted`.
- Unsupported-host validation remains fail-closed with zero accepted partial-redraw artifacts.
