# Feature 156 Scenario: timing/no-change

Scenario id: `timing/no-change`
Verdict: `noisy`
Confidence decision: `inside-noise-band`
Warmup count: `3`
Measured repetitions: `5`
Noise band ms: `0.682`
Overhead disclosure: `measurement-path-isolated-from-proof-readback`

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `13.645` | `14.785` | `14.785` | `5` |
| damage-scoped | `13.179` | `13.363` | `13.363` | `5` |

## Artifacts

- `scenarios/timing-no-change.md`
- `raw/timing-no-change-full-redraw.csv`
- `raw/timing-no-change-damage-scoped.csv`

## Rejection Reasons

- p50 or p95 difference is inside the declared noise band
