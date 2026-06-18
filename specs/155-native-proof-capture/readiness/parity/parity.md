# Feature 155 Same-Profile Damage-Scoped Parity

Status: `accepted`
Proof-set gate: `accepted`
Host profile binding: `same-profile-current-host`

| Scenario | Verdict | Reason |
|----------|---------|--------|
| `damage/localized-update` | accepted | same-profile damage-scoped output matches full-redraw reference |
| `damage/no-change` | accepted | same-profile damage-scoped output matches full-redraw reference |
| `damage/movement` | accepted | same-profile damage-scoped output matches full-redraw reference |
| `damage/overlap` | accepted | same-profile damage-scoped output matches full-redraw reference |
| `damage/edge-clipping` | accepted | same-profile damage-scoped output matches full-redraw reference |
| `damage/resize` | accepted | same-profile damage-scoped output matches full-redraw reference |
| `damage/full-invalidation` | fallback | safe full-redraw fallback reason recorded |
| `damage/invalid-damage` | fallback | safe full-redraw fallback reason recorded |
| `damage/unsupported-host` | fallback | safe full-redraw fallback reason recorded |
| `damage/resource-failure` | fallback | safe full-redraw fallback reason recorded |

Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw.
