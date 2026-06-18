# Feature 156 Compatibility Ledger

Status: `accepted`

## Public API and Diagnostics

- `FS.GG.UI.Testing.CompositorTimingAssertions` adds package-visible timing summary validation for Feature 156.
- `FS.GG.UI.SkiaViewer.CompositorProof` adds timing path and proof-overhead disclosure helpers used by viewer-facing tests.
- `Rendering.Harness` adds `compositor-performance --feature 156` and `compositor-readiness --feature 156` evidence routes.

## Compatibility Impact

- Existing Feature 155 proof, parity, fallback, and correctness vocabulary remains authoritative.
- `performance-not-accepted` remains the shipped P7 performance claim until later gates pass.
- New timing helpers are additive and do not change package identities.

## Migration Guidance

- Consumers should treat `noisy`, `non-beneficial`, `incomplete`, `rejected`, `limited`, and `environment-limited` as non-accepting timing states.
- Positive Feature 156 timing is scoped to `probe-08a47c01` and is not a universal host performance claim.
