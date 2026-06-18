# Feature 154 Regression Validation

Status: `passed`

- Focused Feature154 tests must pass for SkiaViewer, Rendering.Harness, Controls, Elmish, Testing, and Package suites.
- Broad solution validation must preserve Feature 153 proof interpreter behavior and adjacent layout, render-anywhere, text-shaping, overlay, package, and public-surface checks.

## Focused Results

- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature154 --no-build`: passed, 10 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature154 --no-build`: passed, 9 tests.
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature154 --no-build`: passed, 2 tests.
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature154 --no-build`: passed, 2 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature154 --no-build`: passed, 3 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature154 --no-build`: passed, 3 tests.

## Broad Result

- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed. The final long-running project reported `Controls.Tests`: 876 passed, 1 skipped, 877 total, duration 4m 33s; earlier projects also completed without failures.
