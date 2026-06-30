# Render timing (T014 / SC-004)

**Bound**: a representative scene renders to PNG in **under 5 s** on a standard CI runner (FR-008/SC-004).

## Measured (this environment, `dotnet fsi generate-headless-png.fsx`)

Single `SceneEvidence.renderPng { Width = 800; Height = 600 }` of the representative game scene, 5 samples:

| Sample (sorted) | ms |
|---|---|
| 1 | 11.845 |
| 2 | 11.853 |
| 3 (median) | **11.878** |
| 4 | 12.036 |
| 5 | 12.079 |

- **Median: 11.9 ms** — ~420× under the 5 000 ms bound. ✅
- "Slow is acceptable" (FR-008) was the design budget; the actual CPU raster is fast.

The US1 test `tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs` additionally asserts the render
succeeds well within the budget as part of the determinism/non-blank test; this file records the concrete
measured numbers.
