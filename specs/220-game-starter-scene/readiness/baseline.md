# Test baseline — full red/green set

- Config: `Debug`
- Projects: 21  ·  Green: 4  ·  Red: 17

| Project | Result | Summary |
|---|---|---|
| `tests/Build.Tests/Build.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Canvas.Tests/Canvas.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     1, Passed:   153, Skipped:     0, Total:   154, Duration: 95 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Symbology.Tests/Symbology.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 2 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 753 ms - ControlsGallery.Tests.dll (net10.0) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 255 ms - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   171, Skipped:     1, Total:   172, Duration: 1 s - SecondAntShowcase.Tests.dll (net10.0) |

## Red projects (known pre-existing failures unless this is a regression)
- `tests/Build.Tests/Build.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Canvas.Tests/Canvas.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Controls.Tests/Controls.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Elmish.Tests/Elmish.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Layout.Tests/Layout.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Lib.Tests/Lib.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Package.Tests/Package.Tests.fsproj` (exit 1): Failed!  - Failed:     1, Passed:   153, Skipped:     0, Total:   154, Duration: 95 ms - Package.Tests.dll (net10.0)
- `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Scene.Tests/Scene.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Smoke.Tests/Smoke.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Symbology.Tests/Symbology.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
- `tests/Testing.Tests/Testing.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
