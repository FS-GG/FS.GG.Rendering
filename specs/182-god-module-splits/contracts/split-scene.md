# Contract: US3 — Split the Scene god-module

**Target**: `src/Scene/Scene.fs` (2,077 lines; `VisualInspection*`/`RetainedInspection*` type block from
~432; `cleanToken`/`duplicateIds`/`finding` dedup started-but-unfinished; `realTextMeasurer` +
`measurementVersionBucket` module-level mutable). **Package**: `FS.GG.UI.Scene` (dependency-free root,
referenced by 17 projects). Inherits all of [surface-invariance.md](./surface-invariance.md).

## Scope

Move sub-modules + the type block into own files inserted before `Scene.fs` in compile order:

| Concern | Planned file |
|---------|--------------|
| The ~767-line type block (`VisualInspection*`/`RetainedInspection*`/related types) | `SceneTypes.fs` |
| `VisualInspection` sub-module | `VisualInspection.fs` |
| `RetainedInspection` sub-module | `RetainedInspection.fs` |
| `LayoutEvidence` sub-module | `LayoutEvidence.fs` |
| `SceneEvidence` sub-module | `SceneEvidence.fs` |
| Root primitives + isolated `realTextMeasurer` (residual) | `Scene.fs` |

## C-S-1 — Finish the inspection dedup (FR-006)

Complete the started `cleanToken`/`duplicateIds`/`finding` dedup so `VisualInspection` and
`RetainedInspection` share one implementation, **iff** inspection records (tokens, findings, serialized
form) stay byte-identical to baseline. A genuinely divergent call site is left explicit (C-SI-6 /
FR-009). Because `FS.GG.UI.Scene` is the dependency-free root, the shared helper must compile before
both inspection files.

## C-S-2 — Isolate the mutable side-channel (Edge case)

`realTextMeasurer` / `measurementVersionBucket` are isolated into a contained module **without changing
initialization timing or first-use semantics**. Mutation is retained (Constitution III); only its
location changes. The inspection-record + scene-hash byte-diff guards observable behavior.

> Out of scope (spec): cross-project DU migration of `RetainedInspectionStatus`/`VisualInspectionStatus`
> ownership — US3 isolates the mutable but does NOT move the DU.

## C-S-3 — Surface union preserved

`Scene.fsi` and `FS.GG.UI.Scene.txt` byte-identical. Public sub-module paths (`Scene.VisualInspection`,
etc.) keep their exact names; extracted files preserve those module paths so no public symbol relocates.

## Acceptance (maps to spec US3)

1. Built package: `.fsi` + surface baseline byte-identical (C-SI-1/2).
2. Finished dedup: visual + retained inspection records' tokens, findings, serialized form unchanged
   (C-S-1).

## Validation

`scripts/refresh-surface-baselines.fsx` → empty diff; build `FS.GG.UI.Scene` + run Scene tests + codec
round-trip suite; byte-diff visual/retained inspection records vs baseline (quickstart Step 1, row US3).

## Implementation Outcome (2026-06-21) — RETAINED per FR-009 (C-SI-6)

**Retained (not split), maintainer-approved scope decision.** `FS.GG.UI.Scene` is the **dependency-free
root referenced by 17 projects** — the highest-blast-radius package — and its public type block + the
`VisualInspection*`/`RetainedInspection*` sub-modules are pervasively cross-referenced with the
`realTextMeasurer`/`measurementVersionBucket` module-level mutable side-channel. Carving `SceneTypes`
out of the residual reproduces the exact **namespace-type unqualified-resolution hazard** proven on US1
(record-field proximity / DU-case-vs-opened-type), here against 17 downstream consumers; and the
**FR-006 `cleanToken`/`duplicateIds`/`finding` dedup** is a behavior-affecting change that must keep
inspection records (tokens, findings, serialized form) byte-identical. Per **byte-stable output wins**
(FR-002/003) and the maintainer's scope decision (tractable stories only: US4/US5/US6), `Scene.fs` is
**left in its current form**. `FS.GG.UI.Scene.txt` stays byte-identical. Recorded SC-005 size exception;
SC-006 dedup (FR-006) retained-with-reason; mutable isolation (C-S-2) retained.
