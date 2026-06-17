# Feature 147 Compatibility Ledger

## Public Metrics and Diagnostics

- Derived compositor diagnostics are available for damage and fallback review.
- Existing `FrameMetrics` damage, picture-cache, replay, and timing fields remain the base observable channel.
- `CompositorFrameDiagnostics` exposes derived proof readiness, fallback, damage, and cache reuse counters without changing the base `FrameMetrics` contract.

## Baseline References

- `tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt` records the public diagnostics delta.
- `tests/surface-baselines/FS.GG.UI.SkiaViewer.txt` records the present-path proof contract.

## Release Notes Draft

- Damage-scissored redraw is proof-gated and falls back to full redraw on missing, stale, failed, host-mismatched, synthetic, or environment-limited evidence.
- Promotion and snapshot tiers are reported only when parity and threshold evidence are present.

## Migration Guidance

- Existing hosts continue to full-redraw unless a fresh matching proof and parity evidence enable a compositor tier.
- Consumers can inspect the derived diagnostics helper before opting into tier-specific readiness claims.

## Limitations

- Environment-limited host observations are recorded but do not enable readiness.
- Snapshot tier evidence may remain skipped until a capable host can run the performance probe.
