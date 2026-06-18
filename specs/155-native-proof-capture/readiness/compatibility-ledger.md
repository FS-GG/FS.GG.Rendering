# Feature 155 Compatibility Ledger

## Public API and Diagnostics

- Feature 155 reuses the Feature 154 proof-set, parity, timing, fallback, and readiness vocabulary.
- No new public `.fsi` surface is required for the harness-only native closeout path.
- Existing hosts continue full redraw unless the readiness summary records accepted proof and same-profile parity evidence.

## Migration Guidance

- Consumers can treat accepted Feature 155 readiness as a current-host P7 correctness closeout, not a universal host guarantee.
- Performance remains a separate claim and is not accepted by this closeout.

## Synthetic Disclosure

- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites.
- Synthetic evidence is rejection-path coverage only.
