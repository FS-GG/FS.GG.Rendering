# Feature 156 Scenario: timing/localized-update

Scenario id: `timing/localized-update`
Verdict: `noisy`
Confidence decision: `inside-noise-band`
Warmup count: `3`
Measured repetitions: `5`
Noise band ms: `0.68`
Overhead disclosure: `measurement-path-isolated-from-proof-readback`

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `13.605` | `14.054` | `14.054` | `5` |
| damage-scoped | `13.278` | `14.296` | `14.296` | `5` |

## Artifacts

- `scenarios/timing-localized-update.md`
- `raw/timing-localized-update-full-redraw.csv`
- `raw/timing-localized-update-damage-scoped.csv`

## Rejection Reasons

- p50 or p95 difference is inside the declared noise band
