# Feature 158 Scenario: timing/movement-old-new

Scenario id: `timing/movement-old-new`
Scenario definition: `feature156-required-v1:timing-movement-old-new`
Measurement-separation status: `accepted`
Warmup count: `3`
Measured repetitions: `5`
Included timing samples: `10`
Excluded timing samples: `0`

## Distributions

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `27.286` | `33.824` | `33.824` | `5` |
| damage-scoped | `31.887` | `40.066` | `40.066` | `5` |

## Included Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| `feature158-20260618152718-timing-movement-old-new-full-redraw-001` | `timing/movement-old-new` | `full-redraw` | `25.676` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-full-redraw.csv` |
| `feature158-20260618152718-timing-movement-old-new-full-redraw-002` | `timing/movement-old-new` | `full-redraw` | `31.358` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-full-redraw.csv` |
| `feature158-20260618152718-timing-movement-old-new-full-redraw-003` | `timing/movement-old-new` | `full-redraw` | `24.031` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-full-redraw.csv` |
| `feature158-20260618152718-timing-movement-old-new-full-redraw-004` | `timing/movement-old-new` | `full-redraw` | `33.824` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-full-redraw.csv` |
| `feature158-20260618152718-timing-movement-old-new-full-redraw-005` | `timing/movement-old-new` | `full-redraw` | `27.286` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-full-redraw.csv` |
| `feature158-20260618152718-timing-movement-old-new-damage-scoped-001` | `timing/movement-old-new` | `damage-scoped` | `40.066` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-damage-scoped.csv` |
| `feature158-20260618152718-timing-movement-old-new-damage-scoped-002` | `timing/movement-old-new` | `damage-scoped` | `33.421` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-damage-scoped.csv` |
| `feature158-20260618152718-timing-movement-old-new-damage-scoped-003` | `timing/movement-old-new` | `damage-scoped` | `25.63` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-damage-scoped.csv` |
| `feature158-20260618152718-timing-movement-old-new-damage-scoped-004` | `timing/movement-old-new` | `damage-scoped` | `31.887` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-damage-scoped.csv` |
| `feature158-20260618152718-timing-movement-old-new-damage-scoped-005` | `timing/movement-old-new` | `damage-scoped` | `27.625` | `readback-free` | `included` | `none` | `raw/timing-movement-old-new-damage-scoped.csv` |

## Excluded Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| none | none | none | none | none | none | none | none |

## Proof/Probe Artifacts

- none

## Artifacts

- `scenarios/timing-movement-old-new.md`
- `raw/timing-movement-old-new-full-redraw.csv`
- `raw/timing-movement-old-new-full-redraw.json`
- `raw/timing-movement-old-new-damage-scoped.csv`
- `raw/timing-movement-old-new-damage-scoped.json`

## Diagnostics

- measurement-policy=readback-free-timing-v1
- readback-free direct present path used for accepted samples
