# Layout Protocol Contract

## Purpose

Define the observable constraints-down, sizes-up contract for `FS.GG.UI.Layout` participants.
The implementation may preserve existing function names where compatible, but the public `.fsi`
surface must expose equivalent records/functions for constraints, measured size, child placement,
intrinsic query support, diagnostics, and deterministic cache dependencies.

## Public Surface Obligations

- Public contract changes are drafted in `src/Layout/*.fsi` before `.fs` bodies.
- Existing `Layout.evaluate`, `Layout.evaluateIncremental`, `Layout.renderComputed`, and
  hit-test/snap helpers remain source-compatible unless an intentional Tier 1 compatibility delta is
  documented.
- The Layout package remains independent of Controls, SkiaViewer, KeyboardInput, charts, and
  harness projects.
- Public records are inspectable from FSI and package tests.

## Measurement Contract

1. Parent supplies normalized layout constraints to each participant.
2. Participant returns an accepted measured size, child placements, diagnostics, and cache
   dependency evidence.
3. Accepted measured size satisfies constraints unless a diagnostic records explicit degradation.
4. Measurement is deterministic for equivalent constraints and layout inputs.
5. A normal pass measures the same participant for the same input at most once.
6. Natural-size discovery outside the normal measurement result is performed through explicit
   intrinsic queries, not descendant render inspection.

## Placement Contract

1. Child placements are relative to the measured parent.
2. Placement records include child id, finite bounds, visibility, and deterministic identity.
3. Full and incremental layout produce equivalent placement records for equivalent inputs.
4. Collapsed and hidden visibility semantics remain compatible with existing Layout behavior unless
   a compatibility ledger entry documents an intentional change.

## Diagnostics Contract

Diagnostics must be emitted for:

- invalid or contradictory constraints,
- measured sizes that require degradation,
- unsupported or unavailable intrinsic queries,
- stale or rejected cache reuse,
- fallback bounds or content measurement,
- intentional compatibility differences.

Error diagnostics prevent accepted misleading layout results. Warning diagnostics may allow safe
degradation when the fallback is explicit and reviewable.

## Acceptance

The contract is accepted when:

- semantic FSI tests exercise the new public layout records/functions;
- `Layout.Tests` prove deterministic measure/place behavior, invalid-constraint diagnostics, and
  equivalent repeated evaluation;
- public surface baselines include only documented Feature150 deltas;
- package compatibility notes explain any source or behavior impact.
