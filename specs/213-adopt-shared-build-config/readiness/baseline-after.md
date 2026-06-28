# Test baseline — full red/green set

- Config: `Debug`
- Projects: 21  ·  Green: 20  ·  Red: 1

| Project | Result | Summary |
|---|---|---|
| `tests/Build.Tests/Build.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 21 ms - Build.Tests.dll (net10.0) |
| `tests/Canvas.Tests/Canvas.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 23 ms - Canvas.Tests.dll (net10.0) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   949, Skipped:     1, Total:   950, Duration: 20 s - Controls.Tests.dll (net10.0) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 28 ms - Diagnostics.Tests.dll (net10.0) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   211, Skipped:    17, Total:   228, Duration: 657 ms - Elmish.Tests.dll (net10.0) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 34 ms - KeyboardInput.Tests.dll (net10.0) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 719 ms - Layout.Tests.dll (net10.0) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30, Duration: 48 ms - Lib.Tests.dll (net10.0) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   139, Skipped:     0, Total:   139, Duration: 119 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   209, Skipped:     0, Total:   209, Duration: 751 ms - Rendering.Harness.Tests.dll (net10.0) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    77, Skipped:     0, Total:    77, Duration: 138 ms - Scene.Tests.dll (net10.0) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   207, Skipped:     0, Total:   207, Duration: 667 ms - SkiaViewer.Tests.dll (net10.0) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     4, Skipped:     3, Total:     7, Duration: 14 ms - Smoke.Tests.dll (net10.0) |
| `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 111 ms - Symbology.Render.Tests.dll (net10.0) |
| `tests/Symbology.Tests/Symbology.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   447, Skipped:     0, Total:   447, Duration: 386 ms - Symbology.Tests.dll (net10.0) |
| `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 48 ms - SymbologyBoard.Tests.dll (net10.0) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 124 ms - Testing.Tests.dll (net10.0) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 3 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 254 ms - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   171, Skipped:     1, Total:   172, Duration: 1 s - SecondAntShowcase.Tests.dll (net10.0) |

## Red projects (known pre-existing failures unless this is a regression)
- `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` (exit 1): (no summary line; build/restore failure)
