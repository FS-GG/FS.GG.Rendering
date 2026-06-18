# Feature 149 Reuse Evidence

| Scenario | Verdict | Reason |
|----------|---------|--------|
| `reuse/stable-boundary` | ready-policy | stable parity-clean boundary may promote |
| `reuse/placement-only` | ready-policy | content identity is stable and old/new placement regions are damaged |
| `reuse/mixed-change` | refresh | content changes force fresh output before reuse |
| `reuse/no-change` | skip | no visible work is required after a valid prior frame |
| `reuse/content-changing` | demoted | content identity changed |
| `reuse/churning` | demoted | unstable boundary cannot promote |
| `reuse/no-benefit` | demoted | measured overhead exceeds saved work |
| `reuse/failed-parity` | rejected | parity failure dominates |
| `reuse/same-seed` | ready-policy | deterministic same-seed evidence expected |

Reuse claims stay behind output parity, visible old/new movement damage, and benefit checks.
