# Feature 158 Scenario: timing/overlap

Scenario id: `timing/overlap`
Scenario definition: `feature156-required-v1:timing-overlap`
Measurement-separation status: `accepted`
Warmup count: `3`
Measured repetitions: `5`
Included timing samples: `10`
Excluded timing samples: `0`

## Distributions

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `26.153` | `40.289` | `40.289` | `5` |
| damage-scoped | `27.379` | `34.087` | `34.087` | `5` |

## Included Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| `feature158-20260618152718-timing-overlap-full-redraw-001` | `timing/overlap` | `full-redraw` | `27.983` | `readback-free` | `included` | `none` | `raw/timing-overlap-full-redraw.csv` |
| `feature158-20260618152718-timing-overlap-full-redraw-002` | `timing/overlap` | `full-redraw` | `23.882` | `readback-free` | `included` | `none` | `raw/timing-overlap-full-redraw.csv` |
| `feature158-20260618152718-timing-overlap-full-redraw-003` | `timing/overlap` | `full-redraw` | `25.881` | `readback-free` | `included` | `none` | `raw/timing-overlap-full-redraw.csv` |
| `feature158-20260618152718-timing-overlap-full-redraw-004` | `timing/overlap` | `full-redraw` | `40.289` | `readback-free` | `included` | `none` | `raw/timing-overlap-full-redraw.csv` |
| `feature158-20260618152718-timing-overlap-full-redraw-005` | `timing/overlap` | `full-redraw` | `26.153` | `readback-free` | `included` | `none` | `raw/timing-overlap-full-redraw.csv` |
| `feature158-20260618152718-timing-overlap-damage-scoped-001` | `timing/overlap` | `damage-scoped` | `25.366` | `readback-free` | `included` | `none` | `raw/timing-overlap-damage-scoped.csv` |
| `feature158-20260618152718-timing-overlap-damage-scoped-002` | `timing/overlap` | `damage-scoped` | `34.087` | `readback-free` | `included` | `none` | `raw/timing-overlap-damage-scoped.csv` |
| `feature158-20260618152718-timing-overlap-damage-scoped-003` | `timing/overlap` | `damage-scoped` | `30.889` | `readback-free` | `included` | `none` | `raw/timing-overlap-damage-scoped.csv` |
| `feature158-20260618152718-timing-overlap-damage-scoped-004` | `timing/overlap` | `damage-scoped` | `27.379` | `readback-free` | `included` | `none` | `raw/timing-overlap-damage-scoped.csv` |
| `feature158-20260618152718-timing-overlap-damage-scoped-005` | `timing/overlap` | `damage-scoped` | `24.682` | `readback-free` | `included` | `none` | `raw/timing-overlap-damage-scoped.csv` |

## Excluded Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| none | none | none | none | none | none | none | none |

## Proof/Probe Artifacts

- none

## Artifacts

- `scenarios/timing-overlap.md`
- `raw/timing-overlap-full-redraw.csv`
- `raw/timing-overlap-full-redraw.json`
- `raw/timing-overlap-damage-scoped.csv`
- `raw/timing-overlap-damage-scoped.json`

## Diagnostics

- measurement-policy=readback-free-timing-v1
- readback-free direct present path used for accepted samples
