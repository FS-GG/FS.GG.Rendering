# Phase 1 Data Model: fs-gg-layout consumer product-skill

This feature has no runtime data model. The "entities" are the content/config artifacts and the enumerations that track the shipped product-skill set. Each is listed with its shape, the fields that change, and the validation (which gate proves it).

## E1 — `fs-gg-layout` consumer skill body

- **Location**: `template/product-skills/fs-gg-layout/SKILL.md`
- **Frontmatter**: `name: fs-gg-layout`; `description:` a one-line consumer summary (e.g. "Generated product guidance for laying out an FS.GG.UI product — split the screen into HUD and gameplay/content regions responsively and keep active items inside the gameplay region.").
- **Body sections** (mirror `fs-gg-styling`): Scope · Public/consumer surface · Compute regions responsively · Keep an active item inside the gameplay region · The `LayoutEvidence` shape · Boundary (out: layout engine internals) · Build & Test Commands · Generated Product · Related · Sources.
- **Validation**: discovered by the parity/leak harness; passes Feature 225 leak guard (no framework tokens) and is inventoried by skill-parity with its wrapper.

## E2 — `fs-gg-product-layout` wrapper pair

- **Location**: `.agents/skills/fs-gg-product-layout/SKILL.md` (Codex-active) and `.claude/skills/fs-gg-product-layout/SKILL.md` (Claude-active).
- **Shape**: thin alias — same `name: fs-gg-product-layout` + matching `description:`, body pointing to `../../../template/product-skills/fs-gg-layout/SKILL.md`.
- **Validation**: skill-parity pairs it to the canonical body (no `MissingWrapper`/`WrapperOnly` finding); relative path resolves (no `BrokenTarget`).

## E3 — Template emission wiring

- **Location**: `.template.config/template.json` → `sources[]`.
- **Change**: append two entries, each `condition: "(profile == \"app\" || profile == \"game\")"`, `source: "template/product-skills/fs-gg-layout/"`, `target` = `.agents/skills/fs-gg-layout/` and `.claude/skills/fs-gg-layout/` respectively. No `lifecycle` clause.
- **Validation**: Feature 219 derives the per-profile set from these entries (app+game must now include `fs-gg-layout`, both surfaces present, lifecycle-independent); Feature 204 counts framework-skill sources (`>=18`).

## E4 — Shipped skill catalog

- **Location**: `template/base/docs/skillist-reference.md` → "Product capability skills" table.
- **Change**: one row — `fs-gg-layout` | `.agents/skills/fs-gg-layout/SKILL.md` | `app, game`.
- **Validation**: Feature 224 currency check — the row resolves to a real SKILL.md whose `name:` equals `fs-gg-layout`, path is a consumer location, and no row dangles / no shipped skill is unlisted.

## E5 — Feature 225 backstop set

- **Location**: `tests/Package.Tests/Feature225ProductSkillVocabularyTests.fs` → `expectedProductSkillIds`.
- **Change**: add `"fs-gg-layout"` (set grows 8 → 9); update the "covers the N expected ids" test label from 7/8 to the new count as needed.
- **Validation**: the "discovery did not narrow" test — discovered set ⊇ backstop; and the live-findings leak test stays at zero findings.

## E6 — Emission-matrix floors

- **Feature 219** (`Feature219EmitFrameworkSkillsTests.fs`): add `fs-gg-layout` to the `app` and `game` rows of `expectedFrameworkSkills` (7 → 8 skills each); raise the source-count floor `>=16` → `>=18` and update the `8×2=16` comment to `9×2=18`.
- **Feature 204** (`Feature204LifecycleTemplateTests.fs`): raise the framework-source floor `>=16` → `>=18` and its comment.
- **Validation**: both suites pass the exact-set / count assertions post-wiring.

## E7 — Generated parity report

- **Location**: `docs/reports/skills-parity.md` (harness-generated, `<!-- SKILL-PARITY -->` markers).
- **Change**: regenerate via the `skill-parity` CLI / `scripts/check-agent-skill-parity.fsx`; canonical count +1, wrapper count +2, overall status `Passed`, zero findings.
- **Validation**: it is a generated artifact — regenerate, never hand-edit; confirm `Overall status: Passed`.

## E8 — Cross-link (Related)

- **Location**: the `Related` section of `template/product-skills/fs-gg-scene/SKILL.md` (and a reciprocal `[[fs-gg-layout]]` in the new body). Confirm direction against the shipped skills during authoring.
- **Validation**: informational; parity/guidance-coverage read shows no dangling `[[...]]` link asymmetry.

## Relationships

`E1` is the source of truth; `E2` aliases it; `E3` vendors it into products; `E4`/`E5`/`E6` are the enumerations that must include it; `E7` is the generated proof that `E1`+`E2` are paired; `E8` is discoverability. A change to `E1`'s `name:` would ripple to `E2`, `E4`, `E5`, and `E6`.
