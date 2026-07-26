# 0108 — Performance evidence proves production representativeness

## Status

Accepted.

## Context

Digest-bound expected workloads prove that reviewed source and measured source agree, but they do not
prove that a test-constructed model is production-reachable, that every shipped cost driver is
covered, that declared input traversed the host route, or that a headless process observed a
presentation metric. A fast, internally consistent workload can therefore measure the wrong game.

## Decision

The generated game owns three independent witnesses:

1. Every workload carries an opaque FS.GG.Game runner-issued journey receipt or declares
   `synthetic-constructed` provenance. The receipt has no public constructor and binds the runner,
   composition authority, route/scenario/test identities, input/script/trace digests, initial and
   terminal fingerprints, predicate, result, and bounded steps. A canonical factory establishes
   normal-play provenance by serving as the journey's boot seam; caller-authored factory labels or
   hash records cannot substitute. Synthetic construction remains valid for component, stress, and
   throughput claims, but cannot establish representative normal play.
2. `performanceCostDrivers` is independent of `expectedWorkloads`. It names inspectable scale
   sources and required workload bindings, and cross-checks every production gameplay visual.
   Workloads emit observed scale and observed routing separately from declared stimulus. Headless
   present/drop facts are `unsupported` with a reason, never confident zeroes.
3. A fresh-context critic cold-reads the exact intent, definitions, receipts, cost inventory, raw
   evidence, host facts, capability, and rubric. The verdict lives in an attributable external review
   system at the exact landing commit and discloses either a separate subagent or a separated-pass
   fallback. In-repo JSON, author-entered reviewer identity, or a same-context mode string cannot
   establish independence. Only `supported` is green; the critic cannot change samples, mint
   provenance, waive machine failures, or upgrade unsupported capability.

The generated artifact advances from schema 2 to schema 3. Schema-2 readers remain recognizable
during migration, but their claim meaning is explicitly `legacy-unreviewed`, not representative.
Manual supplemental view cost is `component-only-supplemental`; it cannot complete a normal-play
composition.

## Rollout

Rendering publishes the additive scaffold and guidance first. If the published Contracts handoff
needs new fields, SDD owns that schema release and Governance adopts the released shape afterward.
Games may migrate old artifacts during the compatibility window, but representative readiness
requires schema 3 receipts, coverage, truthful capability fields, and the exact-commit external
critic verdict. The emitted `representativeReady: false` is deliberate: authored source cannot stamp
its own independent approval.

## Consequences

New gameplay visuals or cost drivers fail closed until their performance disposition is explicit.
Maximum scale is supported by production configuration plus observed counters rather than prose.
Narrow synthetic and component measurements remain useful without being mislabeled as normal-play
or live-compositor evidence.
