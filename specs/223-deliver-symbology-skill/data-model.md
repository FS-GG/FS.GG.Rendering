# Data Model: Deliver the Symbology Product Skill to Consumers

This feature carries no runtime data structures. The "entities" here are the manifest/registry
records and the parity predicate state the feature changes. Each is described with its fields, the
delta this feature applies, and the validation that holds it true.

## Entities

### 1. Product-skill source pair (in `.template.config/template.json` `sources[]`)

The unit by which a product skill is emitted into a generated app. Each product skill is **two**
entries (one per consumer surface).

| Field | Value for symbology |
|---|---|
| `condition` | `(profile == "app" \|\| profile == "headless-scene" \|\| profile == "governed" \|\| profile == "sample-pack" \|\| profile == "game")` — **no** `lifecycle` clause |
| `source` | `template/product-skills/fs-gg-symbology/` |
| `target` (entry A) | `.agents/skills/fs-gg-symbology/` |
| `target` (entry B) | `.claude/skills/fs-gg-symbology/` |

**Delta**: two entries **added**. Shape is identical to the existing `fs-gg-scene` pair
(`template.json:253-262`). **Validation**: `Feature219EmitFrameworkSkillsTests` G-EMIT — every
product-skill source is lifecycle-independent, profile-gated, and present on both `.agents/skills/`
and `.claude/skills/`.

### 2. Consumer wrapper (`fs-gg-product-symbology`)

A short `SKILL.md` on each repo-root wrapper surface that routes an invocation to the canonical
product-skill content. Mirrors `fs-gg-product-scene`.

| Field | Value |
|---|---|
| Surfaces | `.claude/skills/fs-gg-product-symbology/SKILL.md` **and** `.agents/skills/fs-gg-product-symbology/SKILL.md` |
| `name` (frontmatter) | `fs-gg-product-symbology` |
| `description` | the product-facing one-liner (matches the symbology skill's intent; e.g. *"Author legible unit-symbology … in a generated FS.GG.UI product."*) |
| Body | "Claude-active"/"Codex-active wrapper …" + relative pointer `../../../template/product-skills/fs-gg-symbology/SKILL.md` (same routing shape as the six) |

**Delta**: two files **added**. **Validation**: parity harness reports no `MissingWrapper` for
symbology once present (US2/SC-002); removing them re-reports it (US3/SC-003).

### 3. Parity missing-wrapper predicate (`SkillParity.missingWrapperFindings`)

State: for each `requiresWrapper` canonical entry and each wrapper surface, decide *satisfied* vs.
*MissingWrapper finding*.

| Satisfaction path | Before | After |
|---|---|---|
| `names.Contains canonicalName` (bare name) | satisfies **all** canonical kinds | satisfies all kinds **except** `template/product-skills` entries |
| `exposedAsAlias` (`fs-gg-product-*` present) | satisfies | satisfies (unchanged) |
| `antCanonicalSelfExposed` | satisfies | satisfies (unchanged) |

**Delta**: add `isProductSkill = path under template/product-skills`; gate the bare-name match with
`not isProductSkill`. **Validation**: `Feature223SymbologyParityTests` (new) + the existing
`Feature168ParityFindingTests` regression (the six stay clean).

### 4. Profile → framework-skill matrix (the 219 contract)

`Feature219EmitFrameworkSkillsTests.expectedFrameworkSkills` — the asserted per-profile skill set.

| Profile | Before | After (symbology added) |
|---|---|---|
| `app` | scene, skiaviewer, elmish, keyboard-input, ui-widgets | + **symbology** |
| `headless-scene` | scene | + **symbology** |
| `governed` | scene, testing | + **symbology** |
| `sample-pack` | scene, skiaviewer, elmish | + **symbology** |
| `game` | *(no row in 219)* | scene, skiaviewer, elmish, keyboard-input, ui-widgets, **symbology** — new assertion (P1) |

**Delta**: add symbology to every shipping row; add the `game` row (or assert game emit alongside).
**Validation**: G-EMIT matrix test.

### 5. Lifecycle validator report token

`scripts/validate-lifecycle-template.fsx` emits a status line for symbology.

| | Before | After |
|---|---|---|
| Token | `symbology: not-vendored` (`:418`) | `symbology: vendored` |
| Unwired product-skill dirs | `{ fs-gg-symbology }` | `{ }` (empty) |

**Validation**: `Feature219EmitFrameworkSkillsTests` G-NODANGLE-SYMB.

### 6. `fs-gg-ui-template` cross-repo contract entry (in `FS-GG/.github`)

The registry record whose coherent-set version reflects what consumers can resolve.

| Field | Delta |
|---|---|
| `registry/dependencies.yml` `fs-gg-ui-template` | status/notes updated: symbology now vendored as a product skill (additive content) |
| `docs/registry/compatibility.md` | projection refreshed |
| Coherent-set version | bumped by the release flow on republish (not hard-coded in this feature) |
| Coordination board #35 | moved to Done |

**Validation**: handled via the `cross-repo-coordination` skill; out-of-repo, asserted by the issue
acceptance checklist (FR-008/SC-006).

## State transition (red → green) the feature must demonstrate

1. **Before**: symbology source absent; parity green-over-hole; G-NODANGLE-SYMB asserts
   `not-vendored`; emit matrix omits symbology.
2. **After manifest + wrapper + parity-fix + test edits**: symbology emits to every scene-bearing
   profile on both surfaces; parity reports `MissingWrapper` iff the alias is removed; matrix and
   validator record `vendored`; the six remain regression-clean.

Each edited test must be shown failing against the pre-edit state and passing after (Principle V).
