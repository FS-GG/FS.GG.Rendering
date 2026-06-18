# Feature 149 Damage Parity Artifacts

## Schema

- `parity.md`: full-frame oracle comparison summary by corpus scenario.
- `fallbacks.md`: explicit fallback reason summary for missing proof, stale proof, failed proof, host mismatch, disabled mode, unsupported host, unsafe damage, resource failure, internal error, and parity failure.
- `scissor-reset.md`: state-reset evidence for scissor/no-clear handling.

## Current Status

Deterministic policy tests cover clipping, deduplication, true union area, movement old/new damage, zero-damage handling, full-frame invalidation, and fallback classification. Real pixel parity remains gated by a passed live proof on a capable host.
