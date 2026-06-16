# Data Model: Ant interaction-pattern docs + `fs-gg-ant-design` agent skill (F6)

This feature has no runtime data model. The "entities" are the documentation artifacts and the structured reference data the coverage check (FR-008) reads. All are files on disk; the only typed values live in the test.

## Entity: Pattern document

One Ant interaction-pattern reference page per catalog control family.

| Field | Source / type | Rule |
|---|---|---|
| File | `docs/product/ant-design/patterns/<family>.md` | One per `Catalog.categories` value (research R1). |
| `family` (front-matter) | string | MUST equal exactly one `Catalog.categories` value; the set of all pattern docs' `family` values MUST equal the full `Catalog.categories` set — no missing, no duplicate (FR-001, SC-001). |
| Body | Markdown prose | States the Ant pattern (states, intents, sizing on the 8-unit grid / `controlHeight 32`, spacing, feedback) and maps it to repo machinery. References local primitives only (FR-002). |
| Design-language assertion | required marker line | Each page MUST contain the fixed "design language only — no React/DOM/HTML/CSS dependency" assertion (FR-007, SC-004, research R7). |
| `## Machine-checked references` | fenced ` ```refs ` block | ≥1 `control:` ref, ≥1 `token:` ref, ≥1 `resolver:` ref, the applicable `policy:` ref, and ≥1 `part:` ref (FR-003, FR-011). Code refs (`control`/`token`/`resolver`/`policy`/`doc`) resolve; `part:` is shape-validated only (FR-008, SC-002, SC-008). |
| Semantic-part mapping | required (FR-011) | For the Ant component(s) the page covers, ≥1 `part:<Component>/<partName>` ref plus prose mapping each part to a repo control region + token + resolver state (see the *Ant semantic-part mapping* entity below). Cites the curated snapshot. |
| Not-adopted notes | required where applicable (FR-010) | Any Ant idea with no faithful local realization recorded here with the reason — including, canonically, Ant's React `classNames`-prop / DOM realization of semantic styling (adopted as concept, not mechanism). |

**Relationships**: one per catalog category; referenced by `README.md` (index) and by the `fs-gg-ant-design` skill; cites the *Ant semantic-parts snapshot* reference document.

## Entity: Ant semantic-part mapping

Within a pattern document, the adoption of Ant's named-region (semantic-part) vocabulary for the Ant component(s) that family covers.

| Field | Source / type | Rule |
|---|---|---|
| `part:` refs | lines in the ` ```refs ` block | `part:<AntComponent>/<partName>` — one per declared Ant semantic part (e.g. `part:Button/icon`). Shape-validated: non-empty component, non-empty part, exactly one `/`. NOT resolved against code (Ant component names are not repo symbols). |
| Part → region mapping | Markdown prose | Each declared part mapped to (a) the repo control region that realizes it, (b) the token-taxonomy entry supplying its material, (c) the resolver visual-state where stateful. Editorial accuracy is a review concern (FR-011); the check enforces only declaration + shape + resolving companion refs. |
| Source | citation | The `<AntComponent>`/`<partName>` vocabulary comes from the curated snapshot `docs/product/ant-design/reference/ant-llms-sources.md` (upstream: `https://ant.design/llms-semantic.md`, with retrieval date). |

**Relationships**: one or more per pattern document, keyed by Ant component; embodies Ant's tokens-as-materials / semantic-styles-as-application split; the React `classNames`/DOM realization is the not-adopted counterpart (FR-010).

## Entity: Ant upstream source-of-truth hub (reference source)

A single central, cited reference document — not a deliverable doc with a `refs` block, and not machine-resolved for `part:`/token mappings.

| Field | Source / type | Rule |
|---|---|---|
| File | `docs/product/ant-design/reference/ant-llms-sources.md` | Exactly one. The canonical Ant reference for FS.GG (FR-012). |
| Source catalog | section | MUST name and link all three Ant LLM files with their roles + a retrieval date: `https://ant.design/llms.txt` (index), `https://ant.design/llms-full.txt` (full API/usage + component tokens), `https://ant.design/llms-semantic.md` (semantic parts). |
| Posture | prose | MUST state that only the design-language concept (named regions; tokens-vs-semantic-styles; Ant's stable patterns) is adopted, not the React `classNames`/DOM mechanism. |
| Curated snapshot | section | Per covered Ant component: its named semantic parts (regions) as published upstream, for pattern docs to cite via `part:` refs. |
| Citations | n/a | MUST be cited by every pattern doc, the skill, and `README.md` via `doc:` refs; surfaced repo-wide from the product docs index, `CLAUDE.md`, and key existing skills (review concern). |

**Relationships**: the one canonical Ant reference; cited by the pattern docs, the skill, the index, and repo-level agent context; removes any network dependency from the coverage check (which verifies existence + all-three-files-listed + skill/README citation via `Upstream_source_hub_is_central`, but never reads it for `part:`/token resolution).

## Entity: Enterprise page-template recipe

One recipe per Ant enterprise page template.

| Field | Source / type | Rule |
|---|---|---|
| File | `docs/product/ant-design/templates/<template>.md` | One per name in the fixed set `{workbench, list, detail, form, result, exception}` (FR-006, SC-003). |
| `template` (front-matter) | string | MUST equal exactly one of the six names; all six MUST be present exactly once. |
| `status` (front-matter) | string | MUST be `groundwork` — marks the recipe as forward-looking for D3/G3, not shipped behavior (FR-006). |
| Body | Markdown prose | Page structure composed of existing control families + tokens; names the control families it draws on. |
| `## Machine-checked references` | fenced ` ```refs ` block | References existing control families/tokens only; every ref resolves. |

**Relationships**: one per enterprise template; targets future Workstream D3 (kits) and G3 (Ant showcase).

## Entity: `fs-gg-ant-design` agent skill

A single advisory skill file.

| Field | Source / type | Rule |
|---|---|---|
| File | `.claude/skills/fs-gg-ant-design/SKILL.md` | Exactly one. |
| Front-matter | YAML | MUST parse and contain `name: "fs-gg-ant-design"` and a non-empty `description`, matching the repo skill format (research R5). |
| Advisory posture | n/a | MUST NOT define a gate, blocking step, or readiness condition (FR-005). |
| `## Machine-checked references` | fenced ` ```refs ` block | Links the pattern docs (`doc:` refs) and the public F1–F5 seams (`token:`/`resolver:`/`policy:` refs); every ref resolves (FR-008, SC-002). |
| Layering reminder | required content | States the one-control-set/no-per-theme-fork rule and the no-React/DOM rule (FR-007). |

**Relationships**: references the pattern docs and the public token/resolver/policy surface.

## Reference grammar (read by the coverage check)

Typed references inside a ` ```refs ` block, one per line, `prefix:value`:

| Prefix | Resolves against | Source of truth |
|---|---|---|
| `control:` | a catalog control id | `Catalog.supportedControls |> List.map (fun c -> c.Id)` |
| `resolver:` | a public `StyleResolver` member | reflection over `FS.GG.UI.DesignSystem.StyleResolver` (`baseStyleFor`/`neutralPolicy`/`resolve`/`resolveDefault`/`IntentPolicy`) |
| `token:` | `<Module>.<member>` in the public token surface | reflection over `FS.GG.UI.DesignSystem` token types (`DesignTokensExt.*` nested modules, flat `DesignTokens`) |
| `policy:` | a color-policy name | `ColorPolicy.byName` (via existing `Color` IVT) — `wcag`, `ant` |
| `doc:` | a repo-relative file path | `File.Exists` relative to the citing doc |
| `part:` | `<AntComponent>/<partName>` (Ant semantic part) | **shape only** — non-empty component, non-empty part, exactly one `/`. Declared upstream-vocabulary reference (snapshot-sourced); not resolved against any repo symbol. |

**Validation rules** (enforced by `Feature131AntPatternDocsTests`):
- Family completeness: `{pattern docs' family} == Catalog.categories` (bijective; SC-001).
- Template completeness: all six template names present exactly once, each `status: groundwork` (SC-003).
- Reference resolution: every typed *code* ref (`control`/`token`/`resolver`/`policy`/`doc`) in every doc and the skill resolves; otherwise the test fails naming the dangling ref (SC-002).
- Semantic-part declaration: every pattern doc declares ≥1 well-shaped `part:` ref plus the companion control/token/resolver refs (FR-011, SC-008); `part:` is shape-checked, never code-resolved.
- Required-content: every pattern doc has the design-language assertion line; the skill has the layering + no-React/DOM reminders (SC-004).
- Surface neutrality is **out of band** — proven by the unchanged surface/token baselines, not by this test (SC-005).

## State transitions

None. All artifacts are static documents; the check is a pure read-and-assert with no state.
