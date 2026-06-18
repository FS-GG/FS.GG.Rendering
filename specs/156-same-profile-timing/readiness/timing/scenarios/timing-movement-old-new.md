# Feature 156 Scenario: timing/movement-old-new

Scenario id: `timing/movement-old-new`
Verdict: `noisy`
Confidence decision: `inside-noise-band`
Warmup count: `3`
Measured repetitions: `5`
Noise band ms: `0.666`
Overhead disclosure: `measurement-path-isolated-from-proof-readback`

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `13.317` | `13.465` | `13.465` | `5` |
| damage-scoped | `12.908` | `13.117` | `13.117` | `5` |

## Artifacts

- `scenarios/timing-movement-old-new.md`
- `raw/timing-movement-old-new-full-redraw.csv`
- `raw/timing-movement-old-new-damage-scoped.csv`

## Rejection Reasons

- p50 or p95 difference is inside the declared noise band
