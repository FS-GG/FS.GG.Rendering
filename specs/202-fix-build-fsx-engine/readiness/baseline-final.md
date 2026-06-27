# Test baseline — full red/green set

- Config: `Debug`
- Projects: 21  ·  Green: 16  ·  Red: 5

| Project | Result | Summary |
|---|---|---|
| `tests/Build.Tests/Build.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 17 ms - Build.Tests.dll (net10.0) |
| `tests/Canvas.Tests/Canvas.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 22 ms - Canvas.Tests.dll (net10.0) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   949, Skipped:     1, Total:   950, Duration: 21 s - Controls.Tests.dll (net10.0) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 26 ms - Diagnostics.Tests.dll (net10.0) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   211, Skipped:    17, Total:   228, Duration: 606 ms - Elmish.Tests.dll (net10.0) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 32 ms - KeyboardInput.Tests.dll (net10.0) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 660 ms - Layout.Tests.dll (net10.0) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30, Duration: 49 ms - Lib.Tests.dll (net10.0) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     8, Passed:   102, Skipped:     0, Total:   110, Duration: 76 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   209, Skipped:     0, Total:   209, Duration: 644 ms - Rendering.Harness.Tests.dll (net10.0) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    77, Skipped:     0, Total:    77, Duration: 124 ms - Scene.Tests.dll (net10.0) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     1, Passed:   189, Skipped:     0, Total:   190, Duration: 153 ms - SkiaViewer.Tests.dll (net10.0) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     4, Skipped:     3, Total:     7, Duration: 9 ms - Smoke.Tests.dll (net10.0) |
| `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 90 ms - Symbology.Render.Tests.dll (net10.0) |
| `tests/Symbology.Tests/Symbology.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   447, Skipped:     0, Total:   447, Duration: 253 ms - Symbology.Tests.dll (net10.0) |
| `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 41 ms - SymbologyBoard.Tests.dll (net10.0) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 95 ms - Testing.Tests.dll (net10.0) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     2, Passed:    86, Skipped:     0, Total:    88, Duration: 2 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     2, Passed:    32, Skipped:     0, Total:    34, Duration: 709 ms - ControlsGallery.Tests.dll (net10.0) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 248 ms - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     6, Passed:   165, Skipped:     1, Total:   172, Duration: 1 s - SecondAntShowcase.Tests.dll (net10.0) |

## Red projects (known pre-existing failures unless this is a regression)
- `tests/Package.Tests/Package.Tests.fsproj` (exit 1): Failed!  - Failed:     8, Passed:   102, Skipped:     0, Total:   110, Duration: 76 ms - Package.Tests.dll (net10.0)
- `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` (exit 1): Failed!  - Failed:     1, Passed:   189, Skipped:     0, Total:   190, Duration: 153 ms - SkiaViewer.Tests.dll (net10.0)
- `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` (exit 1): Failed!  - Failed:     2, Passed:    86, Skipped:     0, Total:    88, Duration: 2 s - AntShowcase.Tests.dll (net10.0)
- `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` (exit 1): Failed!  - Failed:     2, Passed:    32, Skipped:     0, Total:    34, Duration: 709 ms - ControlsGallery.Tests.dll (net10.0)
- `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` (exit 1): Failed!  - Failed:     6, Passed:   165, Skipped:     1, Total:   172, Duration: 1 s - SecondAntShowcase.Tests.dll (net10.0)
