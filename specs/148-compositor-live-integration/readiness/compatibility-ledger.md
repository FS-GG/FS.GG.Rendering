# Feature 148 Compatibility Ledger

## Public Metrics and Diagnostics

- Some deterministic policy tiers have ready evidence; live-host tiers remain proof-gated.
- `CompositorFrameDiagnostics` remains the public derived metric surface for proof status, damage area, fallback reason, reuse counters, demotions, and snapshot bytes.
- Feature148 harness routes add live proof, parity, reuse, snapshot, timing, and readiness evidence without removing Feature147 command names.

## Baseline References

- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the compositor diagnostics surface.
- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract.
- `tests/surface-baselines/FS.GG.UI.Controls.txt`, `FS.GG.UI.Testing.txt`, and `FS.GG.UI.Scene.txt` remain checked for no unintended deltas.

## Release Notes Draft

- Partial redraw remains disabled unless a fresh matching live proof passes for the active host profile.
- Placement/reuse and snapshot claims require parity plus threshold evidence against the required lower-tier baseline.

## Migration Guidance

- Existing hosts continue to full-redraw by default.
- Hosts opting into compositor tiers should retain the proof, parity, timing, and ledger artifacts for review.

## Limitations

- Environment-limited host observations are diagnostic only.
- Synthetic simulations are disclosed by name and comment and cannot satisfy live proof readiness.
