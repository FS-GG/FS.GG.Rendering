# Ant Design — central upstream source of truth (LLM documentation set)

This is the **one canonical Ant Design reference for all of FS.GG**. Every Ant fact used in this
repo's pattern docs, recipes, the `fs-gg-ant-design` skill, and agent context should trace back
here rather than to ad-hoc upstream URLs or model memory. (Feature 131 / Workstream F6; decisions
R8–R9.)

**Adoption posture (read first).** FS.GG adopts Ant Design **as a design language only** — its
stable interaction patterns, its named **semantic parts** (component regions), and its split of
**tokens (atomic design materials)** from **semantic styles (how those materials are applied to
regions)**. FS.GG does **not** adopt Ant's *realization mechanism*: React components, the
`classNames`/`styles` props, or the HTML/CSS DOM structure. Ant ideas are realized with this
repo's own primitives — the public token taxonomy (`DesignTokensExt`/`DesignTokens`), the central
`StyleResolver`, the `wcag`/`ant` `ColorPolicy`, and the one semantic control set in `Catalog`.
There is no React/DOM/HTML/CSS dependency.

## The three Ant LLM files

Ant Design publishes three complementary machine-readable documentation files. Each plays a
distinct role; together they are the upstream source of truth.

| File | URL | Role | FS.GG uses it for |
|---|---|---|---|
| `llms.txt` | <https://ant.design/llms.txt> | **Index / navigation** (the llms.txt standard): a table of contents linking design guidance, 70+ component docs, and the semantic docs (EN/CN). | Discovery — *which* Ant doc to read for a given question. |
| `llms-full.txt` | <https://ant.design/llms-full.txt> | **Full aggregated API/usage**: ~74 components with "when to use", examples, prop tables, **component-level design tokens**, and FAQs, in one file. | Grounding the **control** and **token-taxonomy** mappings — the "materials" of each pattern. |
| `llms-semantic.md` | <https://ant.design/llms-semantic.md> | **Semantic-parts model**: each component documented as a set of named semantic parts/regions plus the `classNames` mechanism and an abstract DOM structure. | The **`part:` semantic-part mappings** (FR-011) — the named regions per component. |

**Retrieved**: 2026-06-16. Bump this date (and re-verify the snapshot below) whenever Ant's
upstream docs are re-pulled. This file is the single owner of the retrieval date — do not scatter
raw `ant.design` URLs across other docs; link here instead.

## How the repo machinery realizes Ant's model

- **Tokens (materials)** → `FS.GG.UI.DesignSystem.DesignTokensExt` (`Seed`, `Map`, `Alias`,
  `Component`, `Space`, `Type`, `Density`, `Elevation`) and the flat `DesignTokens`. Ant's
  component design tokens (from `llms-full.txt`) map onto these.
- **Semantic styles (application)** → the central `FS.GG.UI.DesignSystem.StyleResolver`
  (`resolve`, `resolveDefault`, `baseStyleFor`, `neutralPolicy`, `IntentPolicy`) drives the
  stateful styling of a region (hover/active/disabled/selected/focus).
- **Color/contrast policy** → `FS.GG.UI.Color.ColorPolicy` (`wcag`, `ant`).
- **The one semantic control set** → `FS.GG.UI.Controls.Catalog` (52 controls; categories are the
  pattern-doc families). No per-theme control forks.

## Curated semantic-parts snapshot (for `part:` refs)

A curated extract of the Ant components this feature's pattern docs cover, with their **named
semantic parts** exactly as published in `llms-semantic.md` (and, for `Input`/`Table`, the
component `.md` pages). Pattern docs cite this hub and reference these parts via
`part:<Component>/<partName>` lines. The repo region each part maps to is given per pattern doc
(see `../patterns/`), not here — this is the upstream vocabulary only.

| Ant component | Semantic parts (named regions) | Primary FS.GG family |
|---|---|---|
| `Button` | `root`, `content`, `icon` | input |
| `Input` | `root`, `prefix`, `suffix`, `input`, `count` (TextArea: `root`, `textarea`, `count`) | input |
| `Checkbox` | `root`, `icon`, `label` | selection |
| `ColorPicker` | `root`, `body`, `content`, `description`, `popup.root` | selection |
| `Card` | `root`, `header`, `body`, `extra`, `title`, `actions`, `cover` | layout (also chart/graph/custom container chrome) |
| `Tabs` | `root`, `header`, `nav`, `tab`, `tabContent`, `content`, `indicator`, `item`, `popup` | navigation |
| `Modal` | `container`, `mask`, `header`, `body`, `footer`, `content`, `title` | overlay |
| `Alert` | `root`, `section`, `icon`, `title`, `description`, `actions`, `close` | feedback |
| `Table` | `root`, `header`, `body`, `row`, `cell`, `footer`, `container`, `summary` | data |
| `Badge` | `root`, `indicator` | display |

**Not adopted (mechanism).** The `classNames`/`styles` props, the per-part React component
bindings, and the HTML/CSS DOM structure in which these parts live are upstream realization detail
and are **not** part of FS.GG. Where an Ant component has no faithful local realization (e.g. chart
plot internals, pointer-playground/custom surfaces), the relevant pattern doc records it under its
"Not adopted" subsection with the reason.

## Where this hub is cited

- Every pattern doc under `../patterns/` (a `doc:` ref) and the index `../README.md`.
- The `fs-gg-ant-design` skill (`.claude/skills/fs-gg-ant-design/SKILL.md`).
- The product docs index (`docs/product/README.md`), the coding-agent context (`CLAUDE.md`), and
  the most relevant existing product skills — so FS.GG treats these three files as the canonical
  Ant source.
