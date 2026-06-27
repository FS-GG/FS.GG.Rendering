# Test baseline — full red/green set

- Config: `Release`
- Projects: 21  ·  Green: 20  ·  Red: 1

| Project | Result | Summary |
|---|---|---|
| `tests/Build.Tests/Build.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     6, Skipped:     0, Total:     6, Duration: 15 ms - Build.Tests.dll (net10.0) |
| `tests/Canvas.Tests/Canvas.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    17, Skipped:     0, Total:    17, Duration: 30 ms - Canvas.Tests.dll (net10.0) |
| `tests/Controls.Tests/Controls.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   949, Skipped:     1, Total:   950, Duration: 21 s - Controls.Tests.dll (net10.0) |
| `tests/Diagnostics.Tests/Diagnostics.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 57 ms - Diagnostics.Tests.dll (net10.0) |
| `tests/Elmish.Tests/Elmish.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   211, Skipped:    17, Total:   228, Duration: 607 ms - Elmish.Tests.dll (net10.0) |
| `tests/KeyboardInput.Tests/KeyboardInput.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 33 ms - KeyboardInput.Tests.dll (net10.0) |
| `tests/Layout.Tests/Layout.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    79, Skipped:     0, Total:    79, Duration: 722 ms - Layout.Tests.dll (net10.0) |
| `tests/Lib.Tests/Lib.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    30, Skipped:     0, Total:    30, Duration: 54 ms - Lib.Tests.dll (net10.0) |
| `tests/Package.Tests/Package.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   132, Skipped:     0, Total:   132, Duration: 92 ms - Package.Tests.dll (net10.0) |
| `tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   209, Skipped:     0, Total:   209, Duration: 654 ms - Rendering.Harness.Tests.dll (net10.0) |
| `tests/Scene.Tests/Scene.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    77, Skipped:     0, Total:    77, Duration: 128 ms - Scene.Tests.dll (net10.0) |
| `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   207, Skipped:     0, Total:   207, Duration: 649 ms - SkiaViewer.Tests.dll (net10.0) |
| `tests/Smoke.Tests/Smoke.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:     4, Skipped:     3, Total:     7, Duration: 10 ms - Smoke.Tests.dll (net10.0) |
| `tests/Symbology.Render.Tests/Symbology.Render.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    20, Skipped:     0, Total:    20, Duration: 133 ms - Symbology.Render.Tests.dll (net10.0) |
| `tests/Symbology.Tests/Symbology.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   447, Skipped:     0, Total:   447, Duration: 275 ms - Symbology.Tests.dll (net10.0) |
| `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` | 🔴 FAIL | (no summary line; build/restore failure) |
| `tests/Testing.Tests/Testing.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   104, Skipped:     0, Total:   104, Duration: 114 ms - Testing.Tests.dll (net10.0) |
| `samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    88, Skipped:     0, Total:    88, Duration: 2 s - AntShowcase.Tests.dll (net10.0) |
| `samples/ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    34, Skipped:     0, Total:    34, Duration: 702 ms - ControlsGallery.Tests.dll (net10.0) |
| `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 236 ms - SampleApps.Tests.dll (net10.0) |
| `samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj` | 🟢 PASS | Passed!  - Failed:     0, Passed:   171, Skipped:     1, Total:   172, Duration: 1 s - SecondAntShowcase.Tests.dll (net10.0) |

## Red projects (known pre-existing failures unless this is a regression)
- `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` (exit 1): (no summary line; build/restore failure)

## T020 — baseline diff vs T001 (pre-feature)

- **T001 (before)**: 21 projects · 21 green · 0 red.
- **T020 (after)**: 21 projects · 20 green · 1 red — `tests/SymbologyBoard.Tests` (sample
  `SymbologyBoard.fsproj` build) reported `MSB6006: "dotnet" exited with code 134` (SIGABRT — an
  **F# compiler abort under concurrent baseline load**, not a test failure).
- **Caveat (disclosed, not summarized green)**: this red is a **transient/environment-limited**
  FSC crash, **re-run isolated → green (11/11 passed)**. It is unrelated to Feature 207: the
  SymbologyBoard sample does not reference `src/Meta` (the only new source); the feature added a
  dependencies-only metapackage, two Package.Tests, and the slnx entry — none touch the symbology
  board. No genuine new red was introduced by the feature.
- Net: **no feature-attributable regression**; the single red is a disclosed transient.
