# Contract: Damage-Scoped Parity Corpus

## Scope

This contract defines the same-profile parity evidence required after proof-set acceptance. It
compares damage-scoped redraw with the full-redraw reference on the accepted host profile and
records accepted parity or safe fallback for every required scenario.

## Public or Observable Surface

Any public scenario ids, result tokens, harness command arguments, readiness fields, or package
helpers must be declared in `.fsi` before implementation and covered by semantic tests. The
observable surface must support:

- Running the parity corpus for Feature 154.
- Binding parity evidence to the accepted proof host profile.
- Recording reference output, scoped output, diff or comparison identity, fallback reason, and
  verdict for each scenario.
- Rejecting stale, cross-profile, missing, or undecodable parity evidence.

## Required Scenarios

The corpus must record verdicts for at least these paths:

- `damage/localized-update`
- `damage/no-change`
- `damage/movement`
- `damage/overlap`
- `damage/edge-clipping`
- `damage/resize`
- `damage/full-invalidation`
- `damage/invalid-damage`
- `damage/unsupported-host`
- `damage/resource-failure`

Additional representative scenarios may be added as context, but they cannot replace the required
paths.

## Verdict Rules

- `accepted`: same-profile damage-scoped output matches the full-redraw reference for a scenario
  that is eligible for scoped redraw.
- `fallback`: the scenario safely retains full redraw with a visible reason, for example full
  invalidation, invalid damage, unsupported host, or resource failure.
- `failed`: scoped output differs from the full-redraw reference or artifacts are invalid when the
  scenario should have been accepted.
- `environment-limited`: presentation host, readback, resource, permission, or setup limits prevent
  honest parity evidence.

The parity gate is accepted only when all eligible scenarios are `accepted` and every non-accepted
required scenario records a safe fallback or environment-limited reason consistent with the proof
status.

## Required Output

The parity package under `readiness/parity/` records:

- Host profile id used for the corpus.
- Proof-set id that authorized the corpus.
- Scenario ids and verdicts.
- Reference and scoped artifact paths when available.
- Fallback or failure reasons.
- Statement that cross-profile or stale parity evidence cannot unlock partial redraw.

## Acceptance Tests

- Accepted proof plus same-profile localized update parity records matching final visible output.
- No-change, movement, overlap, edge clipping, resize, full invalidation, invalid damage,
  unsupported host, and resource-failure scenarios each record an accepted or safe fallback
  verdict.
- Cross-profile or stale parity evidence remains fallback-gated.
- A mismatched final visible output fails the parity gate and keeps full redraw active.
