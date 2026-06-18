# Feature 161 Compatibility Ledger

Status: `accepted-with-recorded-limitations`

## Public Surface

- `FS.GG.UI.Testing` adds package-visible `Feature161HostLaneReadiness` helper records and status tokens.
- `Rendering.Harness` adds `compositor-performance --feature 161 --lane host-ledger` and `compositor-readiness --feature 161` evidence routes.
- Runtime compositor rendering behavior is unchanged; the feature changes reviewer-visible performance readiness semantics only.

## Compatibility Impact

- Host lane facts are additive diagnostics for package and release review.
- Evidence from X11 `:1` direct OpenGL AMD/Mesa is not generalized to Wayland, indirect GL, missing-display, software-raster, virtualized, or unknown lanes.
- The shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are all accepted for one named lane.

## Surface Evidence

- `readiness/fsi/FS.GG.UI.Testing.txt`
- `readiness/fsi/Rendering.Harness.Compositor.txt`
- `readiness/fsi/Rendering.Harness.Perf.txt`
