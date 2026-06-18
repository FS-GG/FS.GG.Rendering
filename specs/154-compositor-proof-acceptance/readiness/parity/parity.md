# Feature 154 Same-Profile Damage-Scoped Parity

Status: `fallback-gated`
Proof-set gate: `environment-limited`
Host profile binding: `same-profile-required`

| Scenario | Verdict | Reason |
|----------|---------|--------|
| `damage/localized-update` | fallback-gated | requires accepted same-profile proof set before acceptance |
| `damage/no-change` | fallback-gated | requires accepted same-profile proof set before acceptance |
| `damage/movement` | fallback-gated | requires accepted same-profile proof set before acceptance |
| `damage/overlap` | fallback-gated | requires accepted same-profile proof set before acceptance |
| `damage/edge-clipping` | fallback-gated | requires accepted same-profile proof set before acceptance |
| `damage/resize` | fallback-gated | requires accepted same-profile proof set before acceptance |
| `damage/full-invalidation` | fallback | safe full-redraw fallback reason recorded |
| `damage/invalid-damage` | fallback | safe full-redraw fallback reason recorded |
| `damage/unsupported-host` | fallback | safe full-redraw fallback reason recorded |
| `damage/resource-failure` | fallback | safe full-redraw fallback reason recorded |

Cross-profile, stale, missing, undecodable, or environment-limited parity evidence cannot unlock partial redraw.
