# Feature 156 Scenario: timing/edge-clipping

Scenario id: `timing/edge-clipping`
Verdict: `noisy`
Confidence decision: `inside-noise-band`
Warmup count: `3`
Measured repetitions: `5`
Noise band ms: `0.637`
Overhead disclosure: `measurement-path-isolated-from-proof-readback`

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `12.734` | `14` | `14` | `5` |
| damage-scoped | `13.14` | `14.223` | `14.223` | `5` |

## Artifacts

- `scenarios/timing-edge-clipping.md`
- `raw/timing-edge-clipping-full-redraw.csv`
- `raw/timing-edge-clipping-damage-scoped.csv`

## Rejection Reasons

- p50 or p95 difference is inside the declared noise band
