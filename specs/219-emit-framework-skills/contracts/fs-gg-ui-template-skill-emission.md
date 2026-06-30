# Contract delta: `fs-gg-ui-template` — framework-skill emission follows the profile, not the lifecycle

**Contract id**: `fs-gg-ui-template` (registry `FS-GG/.github` `registry/dependencies.yml`)
**Owner**: rendering · **Consumers**: templates, sdd · **Classification**: Tier 1, additive / parameter-surface-neutral
**Status**: Proposed (this feature) — Accepted on merge + registry update

## What changes

The set of files `dotnet new fs-gg-ui` emits **changes for `--lifecycle sdd` and `--lifecycle none`** (additive) and is **unchanged for `--lifecycle spec-kit`** (the default).

| Surface | Before | After |
|---|---|---|
| Template parameters (`name`/`productName`/`lifecycle`/`profile`/`designSystem`/`feedback`) | — | **unchanged** (no add/remove/rename) |
| Framework `fs-gg-*` skills under `.agents/skills/` + `.claude/skills/` | emitted only when `lifecycle=spec-kit` | emitted whenever the **profile** calls for them, under **every** lifecycle |
| Lifecycle workspace (`.specify/`, constitution, `speckit-*` command skills, agent-context tree) | `spec-kit` only | **unchanged** (`spec-kit` only) |
| `docs/skillist-reference.md` catalog | emitted on every lifecycle (dangling under `sdd`/`none`) | emitted under `spec-kit` only (no dangling) |
| `fs-gg-symbology` skill | unwired (incidental under `spec-kit` blanket copy only) | wired as a product-vendored skill on scene-bearing profiles, every lifecycle |

## Contract guarantees (assertable)

1. **G-EMIT** — For every profile `P`, `dotnet new fs-gg-ui --lifecycle L --profile P` emits the same `fs-gg-*` skill set for `L ∈ {spec-kit, sdd, none}` (the set defined by `P` in data-model.md). *(FR-001/FR-002)*
2. **G-NOREG** — For every profile `P`, `--lifecycle spec-kit --profile P` is **byte-for-byte identical** to the pre-change template (and to the no-`--lifecycle` default). *(FR-004 / SC-003)*
3. **G-WORKSPACE** — The lifecycle workspace (`.specify/`, constitution, `speckit-*`, agent-context `CLAUDE.md`/`AGENTS.md`, the blanket authoring-skills copy) is emitted **iff** `lifecycle=spec-kit`. *(FR-003)*
4. **G-CATALOG** — Every skill reference in an emitted `docs/skillist-reference.md` resolves to a skill present in that product; no catalog is emitted that lists absent skills. *(FR-005/FR-006)*
5. **G-NODANGLE-SYMB** — No `template/product-skills/<id>/` directory remains present-but-unreferenced; `fs-gg-symbology` is either wired (G-EMIT applies) or explicitly recorded as not-vendored. *(FR-007)*

## How the contract is verified (evidence)

- **Env-free verdict-core** (CI-cheap, no `dotnet new`): the amended `scripts/validate-lifecycle-template.fsx` re-derives the 3-category gating straight from `template.json` and `failwith`s on any mis-gated source; `Feature204LifecycleTemplateTests.fs` mirrors it.
- **Live `dotnet new` matrix** (`FS_GG_RUN_LIFECYCLE_VALIDATION=1`): 3 lifecycles × 4 profiles real instantiation + byte-diffs prove G-EMIT, G-NOREG, G-WORKSPACE.
- **Positive presence** (`Feature219EmitFrameworkSkillsTests.fs`): asserts `sdd`/`none` products contain the profile-appropriate `fs-gg-*` `SKILL.md` files (G-EMIT), the catalog is non-dangling (G-CATALOG), and symbology status is resolved (G-NODANGLE-SYMB).

## Registry delta (FR-009)

`registry/dependencies.yml` → `fs-gg-ui-template.parameters.lifecycle.notes`: refine *"Gates `.specify/`, constitution, `.agents/`, `.template.config/generated/`"* to state that **framework product-skills under `.agents/skills/fs-gg-*` / `.claude/skills/fs-gg-*` are not lifecycle-gated — they follow the product profile and emit under every lifecycle**; only the lifecycle workspace is gated. Mark additive / surface-neutral. Update `docs/registry/compatibility.md` projection. New feed exposure (if any) is versioned by the merge/release flow.

## Backward compatibility

- **Existing `spec-kit` consumers**: zero diff (G-NOREG).
- **`sdd`/`none` consumers**: strictly more files (framework skills appear); no file removed; no parameter changed. Skills are advisory — they add no gate and block no build (constitution "Local Skills").
- **Downstream composition (`templates`, `sdd`)**: the SDD scaffold path (`lifecycle=sdd`) and the full-stack composition now carry framework skills automatically; no consumer action required to opt in.
