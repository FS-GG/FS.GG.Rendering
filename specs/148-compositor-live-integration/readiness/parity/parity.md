# Feature 148 Damage Parity

| Scenario | Verdict |
|----------|---------|
| `damage/idle` | passed-policy |
| `damage/localized-update` | passed-policy |
| `damage/overlap` | passed-policy |
| `damage/frame-edge` | passed-policy |
| `damage/movement-old-new` | passed-policy |
| `damage/resize` | passed-policy |
| `damage/theme-global` | passed-policy |
| `damage/stale-proof` | passed-policy |
| `damage/disabled` | passed-policy |
| `damage/unsupported` | environment-limited |
| `damage/parity-failure` | rejected-sample |

Full-frame oracle parity is mandatory before accepting a damage-scoped redraw tier.
Current deterministic evidence covers policy and fallback categories; live pixel parity still requires a passed live proof.
