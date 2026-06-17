# Rebaseline Ledger: Modifier Layer IR Foundation

## Public Surface Ledger

| Artifact | Status | Notes |
|---|---|---|
| `tests/surface-baselines/FS.GG.UI.Scene.txt` | Updated | Adds `GlyphRunGlyph`, `GlyphRunMetrics`, `GlyphRunData`, `GlyphRun`, and `SceneNode+GlyphRun`. |
| `tests/surface-baselines/FS.GG.UI.Controls.txt` | No diff | Internal `Composition` does not change public Controls type surface. |
| `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` | No diff | Type-name baseline is unchanged. `Fonts.buildGlyphRunData` is an additive public module function and is recorded in `compatibility-plan.md`. |

Surface refresh command:

```bash
dotnet fsi scripts/refresh-surface-baselines.fsx
```

Observed result: command completed; `git diff -- tests/surface-baselines` showed only the intentional `FS.GG.UI.Scene` glyph-run type/node additions.

## Pixel Ledger

| Evidence | Status | Pixel delta |
|---|---|---|
| `dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature140-harness` | Passed | No intentional baseline delta recorded. |

Offscreen evidence path: `artifacts/feature140-harness/T1/run.json`.

Recorded environment from the run: `proofLevel` `offscreen-pixels`, backend `x11`, GL renderer `AMD Radeon Graphics (radeonsi, renoir, ACO, DRM 3.64, 7.0.11-arch1-1)`, Mesa `4.6 (Compatibility Profile) Mesa 26.1.2-arch1.1`.

## Disclosure

No visual baseline was re-authored for Feature 140. The feature adds proof coverage and renderer support for glyph-run nodes while preserving non-opt-in text fallback behavior.
