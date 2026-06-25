# No-regression diff (T022) — Symbology Live Board (M6)

Full discovery-based baseline (`scripts/baseline-tests.fsx`, every `*.Tests.fsproj` across `tests/` and
`samples/`) run before the feature (`baseline.md`) and after (`baseline-after.md`), Debug config.

## Red set — identical before and after (zero new reds)

| Project | Before | After | Verdict |
|---|---|---|---|
| `tests/Package.Tests` | 🔴 8 failed / 101 passed | 🔴 **8 failed** / 101 passed | unchanged — pre-existing |
| `samples/ControlsGallery/ControlsGallery.Tests` | 🔴 2 failed / 32 passed | 🔴 **2 failed** / 32 passed | unchanged — pre-existing |
| `tests/SymbologyBoard.Tests` | *(did not exist)* | 🟢 **5 passed** / 0 failed | new — green |

Every other project is 🟢 in both runs. The two pre-existing reds (`Package.Tests` public-surface gate and
`ControlsGallery.Tests`) carry **identical failure counts** before and after, so this additive Tier-2 sample
introduced **zero new regressions** and **zero public-surface drift** (FR-012/SC-005): `Package.Tests` stays
at exactly 8 failures — the sample adds no `.fsi`/baseline.

The new `tests/SymbologyBoard.Tests` (reproducibility, seed-divergence, on-board invariant, non-empty/zero-
area board) is fully green.
