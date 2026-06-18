# Feature 148 Content/Placement Reuse

| Scenario | Verdict | Reason |
|----------|---------|--------|
| `reuse/stable-boundary` | ready-policy | stable parity-clean boundary may promote |
| `reuse/moving-only` | ready-policy | old and new placement regions are damaged |
| `reuse/scrolling` | ready-policy | repeated-work reduction target is 30% |
| `reuse/content-changing` | demoted | content identity changed |
| `reuse/theme-resource-change` | demoted | resource/theme invalidation refreshes content |
| `reuse/churning` | demoted | unstable boundary cannot promote |
| `reuse/no-benefit` | demoted | overhead exceeds benefit |
| `reuse/failed-parity` | rejected | parity failure dominates |
| `reuse/same-seed` | ready-policy | deterministic same-seed evidence expected |

Placement reuse is accepted only when output parity remains clean and movement damages both old and new regions.
