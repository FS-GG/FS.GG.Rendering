# Feature 158 Scenario: timing/edge-clipping

Scenario id: `timing/edge-clipping`
Scenario definition: `feature156-required-v1:timing-edge-clipping`
Measurement-separation status: `accepted`
Warmup count: `3`
Measured repetitions: `5`
Included timing samples: `10`
Excluded timing samples: `0`

## Distributions

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `30.55` | `34.903` | `34.903` | `5` |
| damage-scoped | `25.946` | `33.491` | `33.491` | `5` |

## Included Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| `feature158-20260618152718-timing-edge-clipping-full-redraw-001` | `timing/edge-clipping` | `full-redraw` | `27.069` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-full-redraw.csv` |
| `feature158-20260618152718-timing-edge-clipping-full-redraw-002` | `timing/edge-clipping` | `full-redraw` | `31.394` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-full-redraw.csv` |
| `feature158-20260618152718-timing-edge-clipping-full-redraw-003` | `timing/edge-clipping` | `full-redraw` | `34.903` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-full-redraw.csv` |
| `feature158-20260618152718-timing-edge-clipping-full-redraw-004` | `timing/edge-clipping` | `full-redraw` | `30.55` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-full-redraw.csv` |
| `feature158-20260618152718-timing-edge-clipping-full-redraw-005` | `timing/edge-clipping` | `full-redraw` | `28.142` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-full-redraw.csv` |
| `feature158-20260618152718-timing-edge-clipping-damage-scoped-001` | `timing/edge-clipping` | `damage-scoped` | `25.334` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-damage-scoped.csv` |
| `feature158-20260618152718-timing-edge-clipping-damage-scoped-002` | `timing/edge-clipping` | `damage-scoped` | `32.515` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-damage-scoped.csv` |
| `feature158-20260618152718-timing-edge-clipping-damage-scoped-003` | `timing/edge-clipping` | `damage-scoped` | `25.946` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-damage-scoped.csv` |
| `feature158-20260618152718-timing-edge-clipping-damage-scoped-004` | `timing/edge-clipping` | `damage-scoped` | `33.491` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-damage-scoped.csv` |
| `feature158-20260618152718-timing-edge-clipping-damage-scoped-005` | `timing/edge-clipping` | `damage-scoped` | `23.31` | `readback-free` | `included` | `none` | `raw/timing-edge-clipping-damage-scoped.csv` |

## Excluded Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| none | none | none | none | none | none | none | none |

## Proof/Probe Artifacts

- none

## Artifacts

- `scenarios/timing-edge-clipping.md`
- `raw/timing-edge-clipping-full-redraw.csv`
- `raw/timing-edge-clipping-full-redraw.json`
- `raw/timing-edge-clipping-damage-scoped.csv`
- `raw/timing-edge-clipping-damage-scoped.json`

## Diagnostics

- measurement-policy=readback-free-timing-v1
- readback-free direct present path used for accepted samples
