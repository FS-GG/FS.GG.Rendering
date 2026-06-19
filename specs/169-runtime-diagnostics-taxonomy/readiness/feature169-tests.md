# Feature 169 Test Evidence

Recorded on 2026-06-19 Europe/Vienna for `169-runtime-diagnostics-taxonomy`.

## Semantic API Checks

- `dotnet fsi scripts/diagnostics-prelude.fsx` before the implementation failed as expected with `FS0078` because `src/Diagnostics/bin/Release/net10.0/FS.GG.UI.Diagnostics.dll` did not exist yet.
- `dotnet build src/Diagnostics/Diagnostics.fsproj -c Release` passed.
- `dotnet fsi scripts/diagnostics-prelude.fsx` after implementation passed with `diagnostics-prelude: status=blocked groups=2 console-lines=7`.

## Focused Feature 169 Tests

- `dotnet test tests/Diagnostics.Tests/Diagnostics.Tests.fsproj -c Release --filter Feature169`: 14 passed.
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter Feature169`: 2 passed. A first `--no-restore` attempt hit a stale missing FsCheck cache and was rerun with restore.
- `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release --no-restore --filter Feature169`: 2 passed.
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj -c Release --filter Feature169`: 1 passed.
- `dotnet test tests/Testing.Tests/Testing.Tests.fsproj -c Release --filter Feature169`: 2 passed.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj -c Release --no-restore --filter Feature169`: 3 passed.
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --filter Feature169`: 2 passed.

## Build And Surface Checks

- `dotnet build FS.GG.Rendering.slnx`: passed with 0 warnings and 0 errors.
- `dotnet build FS.GG.Rendering.slnx -c Release`: passed with 0 warnings and 0 errors.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed and refreshed `tests/surface-baselines/FS.GG.UI.Diagnostics.txt`, `FS.GG.UI.Controls.txt`, `FS.GG.UI.Controls.Elmish.txt`, `FS.GG.UI.SkiaViewer.txt`, and `FS.GG.UI.Testing.txt`.
- `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --no-build --filter "Surface baselines"`: 11 passed.
- Broad `dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release --filter Surface` still exposes the existing Feature 156 fixture mismatch for `compositor-readiness --feature 156`; the Feature 169 surface-baseline subset passed.

## Artifact Checks

- `specs/169-runtime-diagnostics-taxonomy/readiness/diagnostics-fixture-summary.json` and `.md` were rendered from the public diagnostics API using the mixed synthetic fixture.
- `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- diagnostics --out specs/169-runtime-diagnostics-taxonomy/readiness/sample --json`: passed and wrote JSON, Markdown, and JSONL artifacts.
- `dotnet run --project samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore -- diagnostics --out specs/169-runtime-diagnostics-taxonomy/readiness/sample-verbose --verbose`: passed and printed the verbose grouped console summary.
