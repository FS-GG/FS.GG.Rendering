# Feature 157 Compatibility Ledger

Status: `accepted-with-recorded-limitations`

## Public API and Diagnostics

- `FS.GG.UI.SkiaViewer.Host.GlHost` adds Feature 157 damage validation and no-clear render-decision helpers.
- `FS.GG.UI.SkiaViewer.Viewer.damageDecisionToken` exposes stable readiness tokens.
- `FS.GG.UI.Testing.CompositorDamageReadiness` validates accepted, fallback-only, rejected, and environment-limited damage packages.
- `Rendering.Harness` adds `compositor-damage --feature 157` and extends `compositor-readiness --feature 157`.

## Compatibility Impact

- Existing Feature 155 proof-set and Feature 156 timing contracts remain source-compatible.
- Full redraw remains the default fallback unless all Feature 157 gates pass.
- The shipped P7 performance claim remains `performance-not-accepted`.
