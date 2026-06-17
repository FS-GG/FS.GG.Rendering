# Damage Parity Evidence

This directory contains Feature147 damage and full-redraw oracle parity artifacts.

## Artifact Schema

- `parity.md`: generated corpus verdict table from `compositor-parity`.
- Scenario ids are stable identifiers from `readiness/corpus.md`.
- Verdict values: `passed`, `failed`, `skipped`, or `environment-limited`.
- Fallback categories include missing proof, stale proof, failed proof, host mismatch, disabled compositor, full-frame invalidation, unsafe damage, and unsupported host.

## Evidence Expectations

- Accepted damage-scissored frames must match the full-redraw oracle.
- Damage union area counts overlap once and never exceeds frame area.
- Full-frame invalidation uses a full-frame damage region or full-redraw fallback.
- Scissor state must not leak into later full-redraw or readback paths.
