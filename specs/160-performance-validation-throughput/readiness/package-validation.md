# Feature 160 Package Validation

Status: `accepted-with-recorded-limitations`

## Validation Runs

- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature160"`: passed, 10 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature160&Focused"`: passed, 5 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature160&ReleaseGate"`: passed, 3 tests.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter "Feature160&Scenario"`: passed, 2 tests.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter "Feature160"`: passed, 4 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature160"`: passed, 3 tests.
- `dotnet restore FS.GG.Rendering.slnx --force`: passed, repaired the local `FsCheck 3.3.3` package cache before no-restore validation.
- `dotnet build FS.GG.Rendering.slnx --no-restore`: passed with 0 warnings and 0 errors.
- `dotnet test FS.GG.Rendering.slnx --no-restore`: passed across the solution on final retry; an earlier retry aborted on a host/windowing crash and `SkiaViewer.Tests` passed in isolation before the final retry.
- `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 160 --out specs/160-performance-validation-throughput/readiness`: passed and refreshed this readiness package.

## Package Surface

- Rendering.Harness exposes Feature 160 focused-lane and readiness signatures.
- Testing package exposes `Feature160ThroughputReadiness` for package validation.
- FSI transcripts cover compositor performance authoring and throughput readiness helper authoring.
- The compositor performance claim remains `performance-not-accepted`; Feature 160 only accepts validation throughput.
