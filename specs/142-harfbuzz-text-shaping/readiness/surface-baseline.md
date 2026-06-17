# Feature 142 Surface Baseline

Status: public surface intentionally widened.

Intentional deltas:

- `FS.GG.UI.Scene` adds dependency-light shaped text evidence records, provider evidence, fallback modes, and shaped metric projection helpers.
- `FS.GG.UI.SkiaViewer.Fonts` adds provider lifecycle/status and shaped result builders.
- `FS.GG.UI.SkiaViewer.Text` adds install, clear, status, and shape readback helpers.

No Scene package dependency on SkiaSharp, HarfBuzzSharp, SkiaViewer, Controls, Elmish, Yoga, or Silk.NET is introduced.

Surface evidence:

- `dotnet fsi scripts/refresh-surface-baselines.fsx`: PASS.
- `tests/surface-baselines/FS.GG.UI.Scene.txt`: additive shaped-text evidence records, provider evidence, fallback DUs, direction/script DUs, and run/glyph record types.
- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt`: additive `FS.GG.UI.SkiaViewer.Fonts+TextShapingProviderStatus`.
