# Feature 158 Scenario: timing/localized-update

Scenario id: `timing/localized-update`
Scenario definition: `feature156-required-v1:timing-localized-update`
Measurement-separation status: `accepted`
Warmup count: `3`
Measured repetitions: `5`
Included timing samples: `10`
Excluded timing samples: `0`

## Distributions

| Path | p50 ms | p95 ms | p99 ms | Samples |
|------|--------|--------|--------|---------|
| full-redraw | `31.949` | `33.709` | `33.709` | `5` |
| damage-scoped | `28.019` | `33.068` | `33.068` | `5` |

## Included Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| `feature158-20260618152718-timing-localized-update-full-redraw-001` | `timing/localized-update` | `full-redraw` | `33.262` | `readback-free` | `included` | `none` | `raw/timing-localized-update-full-redraw.csv` |
| `feature158-20260618152718-timing-localized-update-full-redraw-002` | `timing/localized-update` | `full-redraw` | `31.949` | `readback-free` | `included` | `none` | `raw/timing-localized-update-full-redraw.csv` |
| `feature158-20260618152718-timing-localized-update-full-redraw-003` | `timing/localized-update` | `full-redraw` | `33.709` | `readback-free` | `included` | `none` | `raw/timing-localized-update-full-redraw.csv` |
| `feature158-20260618152718-timing-localized-update-full-redraw-004` | `timing/localized-update` | `full-redraw` | `21.404` | `readback-free` | `included` | `none` | `raw/timing-localized-update-full-redraw.csv` |
| `feature158-20260618152718-timing-localized-update-full-redraw-005` | `timing/localized-update` | `full-redraw` | `31.286` | `readback-free` | `included` | `none` | `raw/timing-localized-update-full-redraw.csv` |
| `feature158-20260618152718-timing-localized-update-damage-scoped-001` | `timing/localized-update` | `damage-scoped` | `25.433` | `readback-free` | `included` | `none` | `raw/timing-localized-update-damage-scoped.csv` |
| `feature158-20260618152718-timing-localized-update-damage-scoped-002` | `timing/localized-update` | `damage-scoped` | `22.573` | `readback-free` | `included` | `none` | `raw/timing-localized-update-damage-scoped.csv` |
| `feature158-20260618152718-timing-localized-update-damage-scoped-003` | `timing/localized-update` | `damage-scoped` | `28.019` | `readback-free` | `included` | `none` | `raw/timing-localized-update-damage-scoped.csv` |
| `feature158-20260618152718-timing-localized-update-damage-scoped-004` | `timing/localized-update` | `damage-scoped` | `30.194` | `readback-free` | `included` | `none` | `raw/timing-localized-update-damage-scoped.csv` |
| `feature158-20260618152718-timing-localized-update-damage-scoped-005` | `timing/localized-update` | `damage-scoped` | `33.068` | `readback-free` | `included` | `none` | `raw/timing-localized-update-damage-scoped.csv` |

## Excluded Samples

| Sample | Scenario | Path | Duration ms | Policy | Status | Reason | Artifact |
|--------|----------|------|-------------|--------|--------|--------|----------|
| none | none | none | none | none | none | none | none |

## Proof/Probe Artifacts

- none

## Artifacts

- `scenarios/timing-localized-update.md`
- `raw/timing-localized-update-full-redraw.csv`
- `raw/timing-localized-update-full-redraw.json`
- `raw/timing-localized-update-damage-scoped.csv`
- `raw/timing-localized-update-damage-scoped.json`

## Diagnostics

- measurement-policy=readback-free-timing-v1
- readback-free direct present path used for accepted samples
