# Feature 153 Compatibility Ledger

## Public API and Diagnostics

- `CompositorProof.AcceptedProofSet` records selected attempt ids and the freshness window for exact three-run proof-set review.
- `GlHost.LiveProofHostFacts` and `GlHost.LiveProofHostReadiness` classify capable and unsupported host inputs before accepting evidence.
- `Viewer.liveProofInterpreterSupported` exposes whether a viewer program shape can host live proof effects.
- `FS.GG.UI.Testing.CompositorReadiness` remains the consumer-facing readiness validation helper.

## Fallback and Readiness Vocabulary

- `environment-limited`, `fallback-gated`, `failed`, and `missing-evidence` remain non-accepting states.
- Unsupported hosts record zero accepted partial-redraw artifacts.
- Partial redraw remains full-redraw fallback-gated unless proof and later same-profile parity pass.

## Migration Guidance

Consumers should treat Feature 153 as proof-readiness evidence, not as a performance claim. Existing hosts continue full redraw unless the readiness summary records accepted proof and parity evidence.

## Synthetic Disclosure

Synthetic tests are named with `Synthetic` and carry `// SYNTHETIC:` comments at use sites. Synthetic evidence is rejection-path coverage only.
