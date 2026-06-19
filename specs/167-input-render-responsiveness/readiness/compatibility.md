# Feature 167 Compatibility Evidence

Status: passed on 2026-06-19.

Compatibility expectations:

- Existing viewer launch functions remain callable with existing arguments.
- `ControlsElmish.Perf.runScript` remains deterministic and clock-free.
- Diagnostics-disabled compatibility writes no latency records and preserves deterministic metrics.
- AntShowcase Enter/Space key-down activation remains unchanged; key-up remains non-activating.

Validation commands and results:

- `dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --no-restore --filter "Interaction|Pointer|Focus"`: 111 passed.
- `dotnet test tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj -c Release --no-restore`: 20 passed.
- `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj -c Release --no-restore --filter "Interaction|Responsiveness|Feature167"`: 10 passed.
- `dotnet restore FS.GG.Rendering.slnx`: passed.
- `dotnet build FS.GG.Rendering.slnx -c Release --no-restore`: passed.
- `dotnet build samples/AntShowcase/AntShowcase.App/AntShowcase.App.fsproj -c Release --no-restore`: passed against `0.1.29-preview.1` package pins.
- `dotnet fsi scripts/refresh-surface-baselines.fsx`: passed.
- `git diff --stat tests/surface-baselines/`: `FS.GG.UI.SkiaViewer.txt` +25 public types; `FS.GG.UI.Controls.Elmish.txt` +2 public types.
- `dotnet pack FS.GG.Rendering.slnx -c Release --no-build -o ~/.local/share/nuget-local`: passed, produced `0.1.29-preview.1` packages.

Compatibility note: `runPresentedPersistentWindow` now queues key/pointer work and drains it on the
frame/update loop. The callback path still maps low-level Silk events into small wrapper messages,
while product `MapKey`/`MapPointer`, model updates, and retained view recomposition are deferred to
the paced loop. Existing public launch signatures are unchanged.
