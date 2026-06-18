# Feature 158 Compatibility Ledger

Status: `accepted-with-recorded-limitations`

## Public API and Diagnostics

- No new `FS.GG.UI.Testing` public helper surface is introduced by Feature 158.
- No new `FS.GG.UI.SkiaViewer` public helper surface is introduced by Feature 158.
- `Rendering.Harness` adds `compositor-performance --feature 158`, `compositor-performance --feature 158 --probe-readback`, and `compositor-readiness --feature 158` evidence routes.
- Harness-visible `.fsi` contracts add measurement policy, proof/probe exclusion, and readiness-package records for reviewer evidence.

## Compatibility Impact

- Existing Feature 155 proof-set, Feature 156 timing, and Feature 157 damage readiness contracts remain source-compatible.
- Proof readback remains available only as proof/probe evidence and is excluded from performance acceptance.
- The shipped P7 performance claim remains `performance-not-accepted` until Feature 159 and Feature 161 gates pass.

## Public Surface Drift

- Package surface baselines for `FS.GG.UI.Testing` and `FS.GG.UI.SkiaViewer` are unchanged for Feature 158.
- Harness command output shape is additive and documented through readiness artifacts.
