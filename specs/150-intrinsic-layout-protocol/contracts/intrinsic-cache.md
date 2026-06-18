# Intrinsic Cache Contract

## Purpose

Define how intrinsic-size queries and layout cache entries are keyed, reused, invalidated, and
reported. The contract protects the existing full/incremental layout parity guarantee while adding
natural-size discovery for containers such as ScrollViewer.

## Intrinsic Query Contract

An intrinsic query includes:

- participant id,
- intrinsic axis,
- relevant cross-axis constraint,
- normalized layout/input identity,
- query source,
- evaluator/cache revision.

For equivalent query inputs, the result must be deterministic. Unsupported or contradictory queries
produce diagnostics and cannot drive accepted container sizes or scroll extents.

## Cache Entry Contract

Measured and intrinsic cache entries include:

- participant id and entry kind,
- normalized constraint or query identity,
- content and layout-affecting input key,
- ordered child dependency keys,
- result identity,
- evaluator/cache revision.

Reuse requires every key component to match. Partial matches are misses. Stale entries are ignored
and may emit diagnostics, but they must not produce accepted stale layout output.

## Invalidation Sources

At minimum, cache invalidates for:

- constraints or viewport changes,
- content/measure callback changes,
- layout-affecting attributes and `LayoutIntent` changes,
- visibility changes,
- child insertion, removal, reorder, or identity change,
- intrinsic query dependency changes,
- evaluator/cache algorithm revision changes.

## Single-Measure Rule

During one normal layout pass, a participant is measured once for the same constraints and input
key. Containers that need natural size use intrinsic query APIs. The implementation may share
internal work between normal measurement and intrinsic queries only when the shared result is keyed
by the same dependencies.

## Acceptance

The contract is accepted when:

- tests cover at least 5 layout-affecting input-change categories;
- stale measured and intrinsic results are rejected or recomputed;
- warm incremental layout matches cold full layout for bounds, placements, scroll extents, and
  diagnostics;
- cache-hit evidence names the dependency keys that made reuse safe.
