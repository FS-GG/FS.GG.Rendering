# Feature 153 Proof-Set Decision

Status: `environment-limited`

Selected attempts: `0/3`

Freshness window: `24:00:00`

## Decision

No accepted proof set exists for the current environment. Proof-set acceptance requires exactly three selected fresh matching capable-host attempts. Missing, stale, synthetic-only, blank, undecodable, failed, environment-limited, host-mismatched, or proof-method-mismatched attempts fail closed.

Partial redraw remains fallback-gated until this proof set is accepted and later same-profile parity also passes.

No compositor performance claim is accepted.
