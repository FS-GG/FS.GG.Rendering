# Feature 159 Layer Promotion Summary

Run identity: `feature159-readiness-20260618164758`
Feature 159 status: `accepted`
Policy id: `layer-promotion-v1`
Accepted profile id: `probe-08a47c01`
Measured profile id: `probe-08a47c01`
Unsupported-host reason: `none`
Accepted attempts: `5`
Net saved work: `48`
Shipped P7 performance claim: `performance-not-accepted`

## Scenario Coverage

| Scenario | Promotion | Reuse | Primary reason | Net saved work | Artifact |
|----------|-----------|-------|----------------|----------------|----------|
| `promotion/static-retained` | `promoted` | `content-recorded` | `none` | `12` | `attempts/promotion-static-retained.md` |
| `promotion/placement-only-move` | `promoted` | `content-reused-placement-updated` | `none` | `10` | `attempts/promotion-placement-only-move.md` |
| `promotion/scroll-shifted` | `promoted` | `content-reused-placement-updated` | `none` | `9` | `attempts/promotion-scroll-shifted.md` |
| `promotion/nested-retained` | `promoted` | `content-reused-placement-updated` | `none` | `14` | `attempts/promotion-nested-retained.md` |
| `promotion/content-change` | `kept` | `content-re-recorded` | `none` | `3` | `attempts/promotion-content-change.md` |
| `promotion/churn-demotion` | `demoted` | `reuse-rejected` | `instability` | `0` | `attempts/promotion-churn-demotion.md` |
| `promotion/fallback-safe` | `fallback-only` | `fallback-full-redraw` | `missing-retained-content` | `0` | `attempts/promotion-fallback-safe.md` |

## Required Scenarios

- `promotion/static-retained`
- `promotion/placement-only-move`
- `promotion/scroll-shifted`
- `promotion/nested-retained`
- `promotion/content-change`
- `promotion/churn-demotion`
- `promotion/fallback-safe`

## Diagnostics

- readiness package assembled
- Feature 155 proof gate preserved
- Feature 157 damage readiness preserved
- Feature 158 measurement separation preserved
- performance-not-accepted preserved
