# Feature 158 Scenario: timing/no-change

Scenario id: `timing/no-change`
Scenario definition: `feature156-required-v1:timing-no-change`
Measurement-separation status: `environment-limited`
Warmup count: `3`
Measured repetitions: `5`
Included timing samples: `0`
Excluded timing samples: `1`

## Distributions

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `missing` | `missing` | `missing` | `0` |
| damage-scoped | `missing` | `missing` | `missing` | `0` |

## Included Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| none | none | none | none | none | none | none | none |

## Excluded Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| `feature158-failclosed-timing-no-change` | `timing/no-change` | `full-redraw` | `NaN` | `missing` | `excluded` | `environment-limited` | `excluded/environment-limited.md` |

## Proof/Probe Artifacts

- none

## Artifacts

- `scenarios/timing-no-change.md`

## Diagnostics

- environment-limited
