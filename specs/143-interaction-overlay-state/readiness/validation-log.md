# Feature 143 Validation Log

Date: 2026-06-17

## Commands Run

- `dotnet build FS.GG.Rendering.slnx`
  - Result: PASS after restore and compile.
- `dotnet build FS.GG.Rendering.slnx --no-restore`
  - Result: PASS, 0 warnings, 0 errors.
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter Feature143`
  - Result: PASS, 20 passed.
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --filter Feature143`
  - Result: PASS, 3 passed.
- `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --no-build --filter Feature143`
  - Result: PASS, 2 passed.
- `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-build --filter Feature143`
  - Result: PASS, 1 passed.
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj --filter Feature143`
  - Result: PASS, 1 passed.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`
  - Result: PASS; refreshed public surface baselines.
- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-build --filter "Feature140|Feature141|Feature142|PublicSurface"`
  - Result: PASS, 29 passed.
- `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --no-build --filter "Feature141|Feature142"`
  - Result: PASS, 1 passed.
- `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj --no-build`
  - Result: PASS, 18 passed.

## Not Yet Run

- Full AntShowcase broad suite.
- Real offscreen GL visual proof for Feature 143 overlays.
