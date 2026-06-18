# Feature 149 Damage Parity

| Scenario | Verdict |
|----------|---------|
| `damage/idle` | passed-policy |
| `damage/localized-update` | passed-policy |
| `damage/overlap` | passed-policy |
| `damage/frame-edge` | passed-policy |
| `damage/movement-old-new` | passed-policy |
| `damage/resize` | passed-policy |
| `damage/theme-global` | passed-policy |
| `damage/zero-damage` | passed-policy |
| `damage/stale-proof` | passed-policy |
| `damage/disabled` | passed-policy |
| `damage/unsupported` | environment-limited |
| `damage/resource-failure` | fallback |
| `damage/internal-error` | fallback |
| `damage/parity-failure` | rejected-sample |

Full-frame oracle parity remains mandatory before accepting damage-scoped redraw.
Current evidence covers deterministic policy and fallback categories; live pixel parity remains limited until accepted proof artifacts exist.
