# Feature 189 — per-phase feedback (T034)

## Process friction

- **`.fsi`-pairing governance is folder-sensitive, not documented in the plan.** The plan's
  Constitution-II line said "new internal modules + paired `.fsi`", but `SurfaceAreaTests`'s
  `US4 maintained package surfaces` test only enforces `.fsi`-pairing for **top-directory** `.fs`
  in `src/Controls` — files under `src/Controls/Internal/` are exempt (AttrKeys/Hashing precedent).
  This was discovered only when the test went red mid-refactor. *Generalizable:* `/speckit-plan`
  for a Controls split should state up front that `module internal` helpers belong in `Internal/`
  (no `.fsi`) and reserve top-dir+`.fsi` for curated internal surfaces (ControlKindRegistry-style).

- **The plan's LayoutEval/NodeAssembly split implied a module cycle.** `layoutNode→renderScene` and
  `paintNode→toLayout` form a cycle if `layoutNode` is placed in LayoutEval. The compile-probe task
  (T006) was meant to catch this; doing the *real* incremental extraction (build after each module)
  caught it instead and resolved it (keep `layoutNode` residual). *Generalizable:* a quick call-graph
  scan (`grep` cross-refs between proposed modules) before authoring tasks would surface such cycles
  at planning time.

## Generalizable-code candidates

- **The `ControlInternals` thin re-export facade pattern** (eta-expanded `let x … = NewModule.x …`)
  let a god-module split happen with **zero** consumer/test edits while preserving the `.fsi`
  contract. This is the reusable mechanism for any "extract from a god module whose members are
  referenced as `Module.member` across the tree" — worth a documented recipe in the decomposition
  playbook.

- **Painter-completeness oracle without a painter table.** Asserting "every rich catalog kind ≠ the
  unknown-kind `emptyState` fallback" gives machine-enforced dispatch completeness (FR-007) with zero
  production change — a cheaper alternative to building a registry painter table when the latter hits
  genericity obstacles.

## Severity

- Low/process: the `.fsi`-folder and cycle frictions cost a few build cycles but were caught by the
  governance test and incremental builds respectively. No correctness impact (byte-identity held
  throughout). The painter-table genericity is a genuine F# constraint, not a process miss — the plan
  had already reserved the FR-005 deviation latitude.
