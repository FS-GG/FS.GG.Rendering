# Test baseline — full red/green set

- Config: `Debug`
- Projects: 19  ·  Green: 17  ·  Red: 2

| Project | Result | Summary |
|---|---|---|
| `tests/Canvas.Tests/Canvas.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 45 ms - Canvas.Tests.dll (net10.0) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   949, Skipped:     1, Total:   950, Duration: 43 s - Controls.Tests.dll (net10.0) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 66 ms - Diagnostics.Tests.dll (net10.0) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   211, Skipped:    17, Total:   228, Duration: 1 s - Elmish.Tests.dll (net10.0) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 202 ms - KeyboardInput.Tests.dll (net10.0) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 1 s - Layout.Tests.dll (net10.0) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30, Duration: 81 ms - Lib.Tests.dll (net10.0) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     8, Passed:   101, Skipped:     0, Total:   109, Duration: 151 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   209, Skipped:     0, Total:   209, Duration: 1 s - Rendering.Harness.Tests.dll (net10.0) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    77, Skipped:     0, Total:    77, Duration: 248 ms - Scene.Tests.dll (net10.0) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   207, Skipped:     0, Total:   207, Duration: 542 ms - SkiaViewer.Tests.dll (net10.0) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     4, Skipped:     3, Total:     7, Duration: 23 ms - Smoke.Tests.dll (net10.0) |
| `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     3, Skipped:     0, Total:     3, Duration: 129 ms - Symbology.Render.Tests.dll (net10.0) |
| `tests/Symbology.Tests/Symbology.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 97 ms - Symbology.Tests.dll (net10.0) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 178 ms - Testing.Tests.dll (net10.0) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 51 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🔴 FAIL | Failed!  - Failed:     2, Passed:    32, Skipped:     0, Total:    34, Duration: 6 s - ControlsGallery.Tests.dll (net10.0) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 7 s - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   171, Skipped:     1, Total:   172, Duration: 2 s - SecondAntShowcase.Tests.dll (net10.0) |

## Red projects (known pre-existing failures unless this is a regression)
- `tests/Package.Tests/Package.Tests.fsproj` (exit 1): Failed!  - Failed:     8, Passed:   101, Skipped:     0, Total:   109, Duration: 151 ms - Package.Tests.dll (net10.0)
- `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` (exit 1): Failed!  - Failed:     2, Passed:    32, Skipped:     0, Total:    34, Duration: 6 s - ControlsGallery.Tests.dll (net10.0)
