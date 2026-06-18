# Feature 152 Damage-Scoped Live Parity Evidence Index

Status: `environment-limited`

Damage-scoped live parity can be accepted only for the same host profile and proof method as an accepted three-run proof set. The current local run lacks that proof set, so all live parity acceptance remains gated.

## Required Corpus

| Scenario | Current Verdict |
|----------|-----------------|
| `damage/localized-update` | requires same-profile live proof |
| `damage/no-change` | requires same-profile live proof |
| `damage/movement-old-new` | requires same-profile live proof |
| `damage/edge-clipped` | requires same-profile live proof |
| `damage/resize` | full-redraw fallback |
| `damage/full-frame-invalidation` | full-redraw fallback |
| `damage/invalid-damage` | full-redraw fallback |
| `damage/unsupported` | `environment-limited` |
| `damage/resource-failure` | full-redraw fallback |
| `damage/parity-failure` | rejected |

The full-redraw oracle remains the correctness reference. Failed, skipped, fallback, and environment-limited rows are not accepted partial-redraw evidence.

