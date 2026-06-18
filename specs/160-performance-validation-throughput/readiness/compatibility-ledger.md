# Feature 160 Compatibility Ledger

Status: `accepted-with-recorded-limitations`

## Public Surface

- `FS.GG.UI.Testing` adds package-visible `Feature160ThroughputReadiness` helper records and status tokens.
- `Rendering.Harness` adds `compositor-performance --feature 160 --lane focused` and `compositor-readiness --feature 160` evidence routes.
- Controls and SkiaViewer package identities are unchanged.

## Compatibility Impact

- The helper is additive and validates readiness packages; it does not change runtime rendering behavior.
- The shipped compositor performance claim remains `performance-not-accepted` until same-profile timing, Feature 159 reuse/promotion, Feature 160 throughput, and Feature 161 host-lane gates are complete.

## Surface Evidence

- `readiness/fsi/FS.GG.UI.Testing.txt`
- `readiness/fsi/Rendering.Harness.Compositor.txt`
