# Feature 148 Damage Parity Artifacts

## Schema

- `parity.md`: full-frame oracle comparison summary by corpus scenario.
- `fallbacks.md`: explicit fallback reason summary for missing proof, stale proof, failed proof, host mismatch, disabled mode, unsupported host, unsafe damage, full-frame invalidation, and parity failure.
- `scissor-reset.md`: state-reset evidence for scissor/no-clear handling.

## Corpus

Localized, overlapping, edge, movement old/new, resize, theme/global, stale-proof, disabled, unsupported, and parity-failure scenarios are mandatory before readiness can advance.

## Current Status

Deterministic policy tests cover clipping, deduplication, true union area, movement old/new damage, full-frame invalidation, and fallback classification. Real pixel parity remains gated by a passed live proof on a capable host.
