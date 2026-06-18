# Feature 159 Layer Promotion Summary

Run identity: `feature159-20260618170401`
Feature 159 status: `environment-limited`
Policy id: `layer-promotion-v1`
Accepted profile id: `probe-08a47c01`
Measured profile id: `probe-eb6566e6`
Unsupported-host reason: `missing display`
Accepted attempts: `0`
Net saved work: `0`
Shipped P7 performance claim: `performance-not-accepted`

## Scenario Coverage

| Scenario | Promotion | Reuse | Primary reason | Net saved work | Artifact |
|----------|-----------|-------|----------------|----------------|----------|
| `promotion/static-retained` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |
| `promotion/placement-only-move` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |
| `promotion/scroll-shifted` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |
| `promotion/nested-retained` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |
| `promotion/content-change` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |
| `promotion/churn-demotion` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |
| `promotion/fallback-safe` | `environment-limited` | `environment-limited` | `unsupported-host` | `0` | `unsupported/validation.md` |

## Required Scenarios

- `promotion/static-retained`
- `promotion/placement-only-move`
- `promotion/scroll-shifted`
- `promotion/nested-retained`
- `promotion/content-change`
- `promotion/churn-demotion`
- `promotion/fallback-safe`

## Diagnostics

- promotion evidence package assembled
- policy layer-promotion-v1 enforced
- performance-not-accepted preserved
