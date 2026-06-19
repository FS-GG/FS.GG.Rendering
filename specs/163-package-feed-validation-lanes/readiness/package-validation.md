# Package Validation

Status: `accepted`

Required validation records:

- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature163"`: passed, 13 tests.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter "Feature163"`: passed, 4 tests.
- `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode check --out specs/163-package-feed-validation-lanes/readiness/package-proof`: passed; wrote `package-proof/package-versions.md` and `package-proof/package-pins.md`.
- `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --sample samples/AntShowcase --mode proof --isolated-cache specs/163-package-feed-validation-lanes/readiness/package-proof/nuget-cache --out specs/163-package-feed-validation-lanes/readiness/package-proof`: passed; wrote `package-proof/source-proof.md`, `package-proof/source-proof.json`, generated NuGet config, restore log, and copied assets.
- `dotnet fsi scripts/run-validation-lanes.fsx --lane package-proof --lane antshowcase-sample --lane controls --lane rendering-harness --out specs/163-package-feed-validation-lanes/readiness/lanes`: passed; wrote `lanes/summary.md`, `lanes/summary.json`, per-lane logs, result JSON files, and TRX files for dotnet test lanes.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed after building missing Debug package assemblies. It refreshed a pre-existing `FS.GG.UI.Testing` baseline drift for Feature 160/161 helper surfaces; Feature 163 adds no package-visible UI API.

AntShowcase selected-sample proof:

- `samples/AntShowcase/AntShowcase.Core/AntShowcase.Core.fsproj`, `samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj`, and `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` pin `FS.GG.UI.*` packages to the source-controlled package version.
- `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` no longer references framework projects under `src/`.

Focused readiness status: `ready`. Aggregate full-solution validation remains an optional lane and
was not selected for the focused Feature 163 evidence run.
