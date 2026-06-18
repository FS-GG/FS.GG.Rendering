# Feature 158 Scenario: timing/no-change

Scenario id: `timing/no-change`
Scenario definition: `feature156-required-v1:timing-no-change`
Measurement-separation status: `accepted`
Warmup count: `3`
Measured repetitions: `5`
Included timing samples: `10`
Excluded timing samples: `0`

## Distributions

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `30.987` | `36.005` | `36.005` | `5` |
| damage-scoped | `33.898` | `43.453` | `43.453` | `5` |

## Included Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| `feature158-20260618152718-timing-no-change-full-redraw-001` | `timing/no-change` | `full-redraw` | `26.42` | `readback-free` | `included` | `none` | `raw/timing-no-change-full-redraw.csv` |
| `feature158-20260618152718-timing-no-change-full-redraw-002` | `timing/no-change` | `full-redraw` | `19.755` | `readback-free` | `included` | `none` | `raw/timing-no-change-full-redraw.csv` |
| `feature158-20260618152718-timing-no-change-full-redraw-003` | `timing/no-change` | `full-redraw` | `36.005` | `readback-free` | `included` | `none` | `raw/timing-no-change-full-redraw.csv` |
| `feature158-20260618152718-timing-no-change-full-redraw-004` | `timing/no-change` | `full-redraw` | `30.987` | `readback-free` | `included` | `none` | `raw/timing-no-change-full-redraw.csv` |
| `feature158-20260618152718-timing-no-change-full-redraw-005` | `timing/no-change` | `full-redraw` | `33.053` | `readback-free` | `included` | `none` | `raw/timing-no-change-full-redraw.csv` |
| `feature158-20260618152718-timing-no-change-damage-scoped-001` | `timing/no-change` | `damage-scoped` | `33.898` | `readback-free` | `included` | `none` | `raw/timing-no-change-damage-scoped.csv` |
| `feature158-20260618152718-timing-no-change-damage-scoped-002` | `timing/no-change` | `damage-scoped` | `43.453` | `readback-free` | `included` | `none` | `raw/timing-no-change-damage-scoped.csv` |
| `feature158-20260618152718-timing-no-change-damage-scoped-003` | `timing/no-change` | `damage-scoped` | `40.18` | `readback-free` | `included` | `none` | `raw/timing-no-change-damage-scoped.csv` |
| `feature158-20260618152718-timing-no-change-damage-scoped-004` | `timing/no-change` | `damage-scoped` | `28.725` | `readback-free` | `included` | `none` | `raw/timing-no-change-damage-scoped.csv` |
| `feature158-20260618152718-timing-no-change-damage-scoped-005` | `timing/no-change` | `damage-scoped` | `29.195` | `readback-free` | `included` | `none` | `raw/timing-no-change-damage-scoped.csv` |

## Excluded Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| none | none | none | none | none | none | none | none |

## Proof/Probe Artifacts

- none

## Artifacts

- `scenarios/timing-no-change.md`
- `raw/timing-no-change-full-redraw.csv`
- `raw/timing-no-change-full-redraw.json`
- `raw/timing-no-change-damage-scoped.csv`
- `raw/timing-no-change-damage-scoped.json`

## Diagnostics

- measurement-policy=readback-free-timing-v1
- readback-free direct present path used for accepted samples
