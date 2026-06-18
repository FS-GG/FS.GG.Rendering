# Feature 152 Compatibility Ledger

## Public Metrics and Diagnostics

- `CompositorProof` adds accepted proof-set vocabulary for three-run capable-host acceptance.
- `FS.GG.UI.Testing.CompositorReadiness` exposes consumer-facing readiness validation status vocabulary.
- Existing fallback behavior remains safe: unsupported, missing, stale, synthetic, mismatched, failed, invalid-damage, or parity-failed evidence keeps full redraw.

## Baseline References

- `readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` records proof-set surface exposure.
- `readiness/surface-baselines/FS.GG.UI.Testing.txt` records consumer readiness helper exposure.
- `readiness/surface-baselines/FS.GG.UI.Controls.txt` and `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` remain regression references.

## Release Notes Draft

- P7 live partial redraw is accepted only from a three-run same-profile live proof set plus same-profile parity.
- Current environment-limited evidence records no partial-redraw or performance acceptance.

## Migration Guidance

- Consumers should treat `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` as non-accepting readiness states.
- Existing hosts continue to full-redraw unless the readiness summary records accepted proof and parity evidence.

## Limitations

- Synthetic simulations are failure-path tests only and cannot accept live proof.
- Capable-host timing remains required for any performance claim.
