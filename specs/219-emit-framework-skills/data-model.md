# Phase 1 Data Model: Emit Framework Skills On Every Lifecycle

No runtime datastore. The "entities" are the template's emission classification, the lifecycle×profile→skill-set relation, and the validation report's state. These drive the `template.json`, validator, test, and registry edits.

## Entity: Template source category

The Feature 204 gate (validator + test) partitions every `template.json` `sources[]` entry into exactly one category. This feature changes the partition from 2 to 3 categories.

| Category | Recognized by (in priority order) | Lifecycle condition | Profile condition | Examples |
|---|---|---|---|---|
| **Framework product-skill** | `source` starts with `template/product-skills/` | MUST NOT contain `lifecycle == "spec-kit"` | MUST be present (profile predicate) | `template/product-skills/fs-gg-scene/` → `.agents/skills/fs-gg-scene/` |
| **Lifecycle workspace** | not framework-skill, AND `target` under `.specify/`/`.agents/`/`.claude/`/`CLAUDE.md`/`AGENTS.md`, OR `source` == `.template.config/generated/`, OR `target` == `docs/skillist-reference.md` (named exception) | MUST contain `lifecycle == "spec-kit"` | any | `.agents/skills/` blanket copy; `.specify/`; `template/base/.agents/`; generated tree; `docs/skillist-reference.md` (after R4) |
| **Product** | everything else | MUST NOT contain `lifecycle == "spec-kit"` | any | `template/base/` → `./`; `template/fragments/samples/` → `samples/` |

**Classification rule (must be applied in this order)**: framework-product-skill **first** (by `source` prefix), then lifecycle-workspace (by `target` prefix / generated tree / skillist exception), then product. The current code tests target-prefix first and so would mis-bucket the decoupled framework skills — fixing the order is the core validator edit.

**Validation invariants** (env-free verdict-core; mirrored in the Feature 204 test):
- Every framework-product-skill source: `¬contains(lifecycle=="spec-kit")` ∧ `has-profile-predicate`.
- Every lifecycle-workspace source: `contains(lifecycle=="spec-kit")`.
- Every product source: `¬contains(lifecycle=="spec-kit")`.
- Counts (fixed against post-edit `template.json` at implementation): `framework-skill >= 12`, `lifecycle-workspace >= 6`, `product >= 3`. (Pre-edit: lifecycle-workspace `>= 18` included the 12 skill sources; they move out, so the threshold drops.)

## Entity: lifecycle × profile → emitted framework-skill set

The relation this feature makes lifecycle-independent. After the change the **skill set depends only on profile**; lifecycle only toggles the *lifecycle workspace*, not these skills.

| profile | framework skills emitted (ALL lifecycles after change) |
|---|---|
| `app` | fs-gg-scene, fs-gg-skiaviewer, fs-gg-elmish, fs-gg-keyboard-input, fs-gg-ui-widgets, **fs-gg-symbology** |
| `headless-scene` | fs-gg-scene, **fs-gg-symbology** |
| `governed` | fs-gg-scene, fs-gg-testing, **fs-gg-symbology** |
| `sample-pack` | fs-gg-scene, fs-gg-skiaviewer, fs-gg-elmish, **fs-gg-symbology** |

(**fs-gg-symbology** is newly wired per R5, on the scene-profile set = all four profiles. If R5's verify-on-implement fails, symbology rows revert to absent and the change becomes "record as not-vendored.")

**Before vs after, by lifecycle** (presence of the framework skills above):

| lifecycle | before | after |
|---|---|---|
| `spec-kit` | present (via blanket copy + per-skill overwrite) | **present — byte-identical** (GV-3 proof) |
| `sdd` | **absent (the bug)** | **present** (FR-001) |
| `none` | **absent (the bug)** | **present** (FR-001) |

The lifecycle workspace (`.specify/`, constitution, `speckit-*` command skills, agent-context tree, `docs/skillist-reference.md`) stays: present under `spec-kit`, absent under `sdd`/`none` — unchanged by this feature (FR-003).

## Entity: skill catalog (`docs/skillist-reference.md`)

| Field | Before | After |
|---|---|---|
| emission gate | ungated `copyOnly` (all lifecycles) | `lifecycle == "spec-kit"` only (R4) |
| listed ids | full ~44-id registry | unchanged content; emitted only where the full set ships |
| dangling under `sdd`/`none` | yes (lists ~44, 0–6 present) | **no — not emitted** (FR-006) |
| substitution | `copyOnly` (no `sourceName` rewrite) | unchanged (`copyOnly` preserved) |

**Deferred (bounded follow-up, not this feature)**: a product-accurate, profile-scoped catalog listing only the vendored `fs-gg-*` skills with product-relative paths, so a catalog *can* ship under `sdd`/`none`. Tracked in research R4 alternatives.

## Entity: validation report (`readiness/lifecycle-template-validation.md`)

Gitignored artifact the validator writes; the Feature 204 test asserts its lines. State transitions (lines that change meaning):

- `gated-condition: all gated source entries carry lifecycle == "spec-kit"` → re-worded to the 3-category statement (e.g. `gated-condition: lifecycle-workspace sources carry lifecycle == "spec-kit"; framework product-skill sources are profile-gated and lifecycle-independent`).
- `sdd/<profile>: … gated-absent=ok product-present=ok diff-vs-default=gated-only` → semantics updated so `.agents/skills/fs-gg-*` counts as **product-present**, not gated-absent; `diff-vs-default` excludes the framework-skill paths (present in both default and sdd).
- New positive lines (consumed by the new `Feature219` test): `sdd/<profile>: framework-skills-present=ok (<n> SKILL.md)`, `none/<profile>: framework-skills-present=ok`, `catalog-dangling: none`, `symbology: wired (scene-profiles)` (or `symbology: not-vendored`).
- `provenance:` and `result: pass` semantics unchanged.

## State transition: board item / cross-repo request

`FS-GG/FS.GG.Rendering#30` and its Coordination board item:

`Backlog` → **`In progress`** (done at `/speckit-specify`) → `In review` (PR open) → `Done` (merged + registry updated + #30 closed). The registry coherence for `fs-gg-ui-template` is updated as part of the `Done` transition (FR-009).
