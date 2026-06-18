# Feature 156 Scenario: timing/overlap

Scenario id: `timing/overlap`
Verdict: `noisy`
Confidence decision: `inside-noise-band`
Warmup count: `3`
Measured repetitions: `5`
Noise band ms: `0.673`
Overhead disclosure: `measurement-path-isolated-from-proof-readback`

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `13.465` | `13.885` | `13.885` | `5` |
| damage-scoped | `12.914` | `13.255` | `13.255` | `5` |

## Artifacts

- `scenarios/timing-overlap.md`
- `raw/timing-overlap-full-redraw.csv`
- `raw/timing-overlap-damage-scoped.csv`

## Rejection Reasons

- p50 or p95 difference is inside the declared noise band
