# Feature 152 Damage-Scoped Live Parity

| Scenario | Verdict |
|----------|---------|
| `damage/localized-update` | requires-same-profile-live-proof |
| `damage/no-change` | requires-same-profile-live-proof |
| `damage/movement-old-new` | requires-same-profile-live-proof |
| `damage/edge-clipped` | requires-same-profile-live-proof |
| `damage/resize` | full-redraw-fallback |
| `damage/full-frame-invalidation` | full-redraw-fallback |
| `damage/invalid-damage` | full-redraw-fallback |
| `damage/unsupported` | environment-limited |
| `damage/resource-failure` | fallback |
| `damage/parity-failure` | rejected |

Damage-scoped output can be accepted only after the same host profile has an accepted three-run proof set.
Resize, full invalidation, invalid damage, unsupported hosts, resource failure, and parity failure route to full redraw or another recorded safe fallback.
