# Feature 146 Validation Summary

Validated from branch `146-render-anywhere-protocol`.

## Passed

| Command | Result |
|---|---|
| `dotnet build FS.GG.Rendering.slnx --no-restore` | PASS |
| `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature146 --no-restore` | PASS, 15 tests |
| `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature146 --no-restore` | PASS, 4 tests |
| `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature146 --no-restore` | PASS, 7 tests |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature146 --no-restore` | PASS, 4 tests |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Surface` | PASS, 15 tests |
| `dotnet test tests/Package.Tests/Package.Tests.fsproj` | PASS, 46 tests |
| `dotnet fsi scripts/refresh-surface-baselines.fsx` | PASS; copied refreshed baselines to `readiness/surface-baselines/` |
| `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-reference --out specs/146-render-anywhere-protocol/readiness/reference` | PASS; wrote three PNG reference artifacts and `reference/summary.md` |
| `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-browser-feasibility --out specs/146-render-anywhere-protocol/readiness/browser` | PASS; wrote fallback feasibility report |
| `dotnet fsi scripts/controls-prelude.fsx` | PASS |
| `dotnet fsi scripts/input-prelude.fsx` | PASS |
| `dotnet fsi scripts/controls-elmish-prelude.fsx` | PASS |
| `dotnet pack FS.GG.Rendering.slnx -c Release -o /home/developer/.local/share/nuget-local --no-restore` | PASS; produced `0.1.8-preview.1` local packages |

## Focused Evidence

- Round-trip and inspection corpus: `roundtrip/corpus.md`.
- Reference oracle evidence: `reference/summary.md` plus per-scenario PNG and metadata files.
- Browser feasibility: `browser/browser-feasibility.md`; current decision is a documented CanvasKit command-stream proof/fallback, not a production browser backend claim.
- Compatibility ledger: `compatibility-ledger.md`.
- Package-readiness artifacts restored for `specs/035-api-discovery-names` and `specs/036-archive-readiness-api-docs` so `Package.Tests` is green.

## Full Solution Caveat

`dotnet test FS.GG.Rendering.slnx` must be run with Wayland disabled on this host. With `WAYLAND_DISPLAY=wayland-0` still set, a display-backed test host crashed while loading `libdecor-gtk.so`. Re-running with:

```bash
env -u WAYLAND_DISPLAY DISPLAY=:1 XDG_SESSION_TYPE=x11 GDK_BACKEND=x11 SDL_VIDEODRIVER=x11 GLFW_PLATFORM=x11 WINIT_UNIX_BACKEND=x11 MSBUILDDISABLENODEREUSE=1 dotnet test FS.GG.Rendering.slnx --logger "console;verbosity=minimal"
```

avoided the libdecor crash and progressed into managed tests, but `Controls.Tests` reported existing typed-lowering parity failures where typed Menu/Dialog/DatePicker output includes `transientWidgetMetadata` and the legacy comparison expectation does not. The run then stopped making progress after the verbose failure output and was terminated. Feature 146 does not edit Controls production code; this is recorded as an unrelated Controls parity/readiness issue in the current checkout, not as a portable scene protocol failure.
