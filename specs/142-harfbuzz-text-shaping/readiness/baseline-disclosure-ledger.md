# Feature 142 Baseline Disclosure Ledger

Status: active.

Pure fallback baseline changes: zero.

Intentional deltas:

| Scenario | Change Type | Reason | Migration | Versioning | Evidence |
|---|---|---|---|---|---|
| SkiaViewer text shaping | dependency | Adds `SkiaSharp.HarfBuzz` and matching HarfBuzzSharp native assets at the SkiaViewer edge | Scene callers do not reference the dependency; SkiaViewer consumers restore the new packages | preview-compatible patch/minor signal | `Directory.Packages.props`, `src/SkiaViewer/SkiaViewer.fsproj` |
| Shaped text public evidence | public surface | Exposes provider, run, glyph, metrics, diagnostics, and fingerprint data | Consumers can keep using old text APIs; new APIs are additive | preview-compatible additive surface | `src/Scene/Scene.fsi`, `src/SkiaViewer/Fonts.fsi`, `src/SkiaViewer/SkiaViewer.fsi` |
| HarfBuzz glyph drawing | pixel | Successful shaped glyph-runs paint stored glyph ids and positions | Intentional improved text shaping; pure fallback remains available | preview-compatible rendering delta | `src/SkiaViewer/SceneRenderer.fs`, Feature 142 tests |
| Surface baselines | package surface | `FS.GG.UI.Scene` and `FS.GG.UI.SkiaViewer` baselines include additive shaped-text/provider types | Additive only; existing text APIs remain | preview-compatible additive surface | `tests/surface-baselines/FS.GG.UI.Scene.txt`, `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` |
