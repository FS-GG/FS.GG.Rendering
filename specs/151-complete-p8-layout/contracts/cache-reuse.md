# Cache Reuse Contract

## Purpose

Define when measured and intrinsic layout results may be reused, when they must miss, and how stale
entries are rejected before P8 can be accepted.

## Measured Result Reuse

Measured reuse requires exact match of:

- participant id;
- entry kind;
- normalized layout constraints;
- content and measurement identity;
- layout-affecting inputs and `LayoutIntent`;
- visibility;
- ordered child identity and child dependency keys;
- evaluator/cache revision.

Partial matches are misses. Stale matches are rejected and may emit diagnostics, but cannot produce
accepted layout output.

## Intrinsic Result Reuse

Intrinsic reuse requires exact match of:

- query identity;
- participant id;
- intrinsic axis;
- cross-axis constraint;
- layout input key;
- child intrinsic dependency identities;
- evaluator/cache revision.

Unsupported, contradictory, rejected, or missing intrinsic results cannot drive accepted
ScrollViewer extent or container size.

## Required Invalidation Categories

Evidence must cover at least these change categories:

- root constraints or viewport;
- content identity or measurement behavior;
- layout-affecting attributes;
- child insertion or removal;
- child reorder;
- visibility;
- intrinsic dependency result;
- evaluator/cache revision.

## Run Modes

Acceptance compares:

- cold full layout;
- cold incremental layout;
- warm incremental layout with expected cache hits;
- changed-input incremental layout with expected misses or stale rejection;
- disabled-cache parity where available.

## Acceptance Rules

- Accepted warm reuse must name the dependency keys that made reuse safe.
- Accepted full and incremental outputs must match for bounds, placements, scroll extents,
  diagnostics, and result identities where those modes apply.
- Stale measured or intrinsic entries must be rejected before they influence accepted output.
- Duplicate measurement during one normal layout pass must be prevented or reported with a blocking
  diagnostic.
- Cache misses caused by valid changed inputs are not failures; accepted stale hits are failures.
