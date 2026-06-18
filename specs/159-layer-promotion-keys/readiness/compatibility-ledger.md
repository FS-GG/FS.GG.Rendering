# Feature 159 Compatibility Ledger

Status: `accepted-with-recorded-limitations`

## Public Surface

- `FS.GG.UI.Controls` public package surface remains unchanged; Feature 159 retained-render helpers are internal diagnostics.
- `FS.GG.UI.SkiaViewer` public package surface remains unchanged; split replay diagnostics are internal to the viewer package.
- `FS.GG.UI.Testing` adds package-visible `Feature159Readiness` helper records and status tokens.
- `Rendering.Harness` adds `compositor-promotion --feature 159` and extends `compositor-readiness --feature 159`.

## Claim Boundary

- Feature 159 may accept net-positive reuse/promotion counters.
- The shipped P7 performance claim remains `performance-not-accepted` until same-profile timing and host-lane gates also pass.

## Surface Evidence

- `readiness/fsi/FS.GG.UI.Controls.txt`
- `readiness/fsi/FS.GG.UI.SkiaViewer.txt`
- `readiness/fsi/FS.GG.UI.Testing.txt`
- `tests/surface-baselines/FS.GG.UI.Testing.txt` intentionally adds `Feature159Readiness`, `Feature159ReadinessCheck`, `Feature159ReadinessStatus`, `Feature159ReadinessValidationResult`, and `Feature159ScenarioEvidence`.
- `tests/surface-baselines/FS.GG.UI.Controls.txt` and `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` remain unchanged by Feature 159.
