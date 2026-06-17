# Feature 142 Validation Log

Status: focused validation passed; broad package-readiness target is limited by missing historical readiness artifacts in this checkout.

- `dotnet restore FS.GG.Rendering.slnx`: PASS on 2026-06-17.
- `dotnet build FS.GG.Rendering.slnx --no-restore`: PASS on 2026-06-17 with 0 warnings and 0 errors.
- `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 49 passed.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 107 passed.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 22 passed.
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 157 passed, 17 skipped.
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: PASS, 797 passed, 1 skipped.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: PASS; additive shaped-text surface baseline deltas recorded for `FS.GG.UI.Scene` and `FS.GG.UI.SkiaViewer`.

- `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-build --logger "console;verbosity=minimal"`: FAIL due missing pre-existing package-readiness artifacts and checkout-local wrapper inputs, including `readiness/surface-baselines/*`, `scripts/controls-prelude.fsx`, and `specs/035-api-discovery-names/readiness/*`. This is classified as a pre-existing package-readiness limitation, not a Feature 142 shaping regression.
