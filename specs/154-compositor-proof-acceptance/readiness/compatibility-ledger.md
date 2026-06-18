# Feature 154 Compatibility Ledger

## Public API and Diagnostics

- `CompositorProof.AcceptedProofSet` remains the authoritative exact-three selected-attempt proof-set vocabulary.
- `CompositorReadiness` remains the package-visible readiness helper for accepted, fallback-gated, failed, environment-limited, missing-evidence, and compatibility-blocked outcomes.
- No new public `.fsi` surface is required beyond the Feature 153 proof/readiness contracts for this environment-limited closeout.
- Controls and Controls.Elmish compositor diagnostics continue to expose proof status, damage union, scissor candidate suppression, fallback reason, and resource counters.

## Fallback and Readiness Vocabulary

- `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` remain non-accepting states.
- Unsupported hosts record zero accepted partial-redraw artifacts.
- Partial redraw remains full-redraw fallback-gated unless proof-set acceptance and same-profile parity acceptance are both current.

## Migration Guidance

- Consumers should treat Feature 154 as the final P7 readiness package, not as a new proof vocabulary.
- Existing hosts continue full redraw unless the readiness summary records accepted proof and accepted same-profile parity evidence.

## Synthetic Disclosure

- Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites.
- Synthetic evidence is rejection-path coverage only.
