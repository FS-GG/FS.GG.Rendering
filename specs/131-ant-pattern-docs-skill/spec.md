# Feature Specification: Ant interaction-pattern docs + `fs-gg-ant-design` agent skill

**Feature Branch**: `131-ant-pattern-docs-skill`

**Created**: 2026-06-16

**Status**: Draft

**Input**: User description: "next item of FS gg" → resolved to Workstream **F6** (the final, optional, docs-first item of the Ant Design adoption arc): Ant interaction-pattern documentation per control family, plus the `fs-gg-ant-design` agent skill that translates Ant Design's stable ideas into this repository's token / control / renderer / policy machinery.

**Change Classification**: **Tier 2 (internal change)** — this feature adds documentation and one advisory agent skill only. It introduces no public API surface, no new package, no new dependency, no token-value change, and no change to observable rendering behavior. Per the constitution, `.fsi` files and surface-area baselines remain untouched; the design-token-drift gate is unaffected because no tokens change.

## Why this feature (context)

Workstream F (Ant Design adoption as a *design language*, not a React/DOM dependency) has now landed its four engineering pillars:

- **F1 (126)** — Ant-derived token taxonomy (seed → map → alias → component).
- **F2 (127)** — policy-driven color/contrast validation (`wcag` / `ant`).
- **F3 (128)** — `--design-system` template parameter selecting the policy.
- **F4 (129)** — central visual-state style resolver (`theme → kind → intent → states → style`).
- **F5 (130)** — promotion of the consumer-facing token taxonomy and resolver surface to the public API.

F6 is the **knowledge layer** over that machinery. It does not add new runtime capability; it makes the already-shipped capability *discoverable and correctly applied* by (a) documenting, per control family, how Ant's interaction patterns are realized with this repo's existing controls + tokens + policy + resolver, and (b) packaging that mapping as an advisory agent skill so a contributor (human or agent) building Ant-styled UI reaches for the right local seams instead of inventing new ones or reaching for React/DOM idioms. It is also the documentation groundwork that the later Workstream D2 (concrete Ant theme) and D3 (kits / enterprise page templates) build on.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Per-control-family Ant interaction-pattern docs (Priority: P1)

A developer building an Ant-styled screen opens the documentation and finds one reference page per control family (mirroring the catalog families used by the Controls Gallery: display, buttons, text/numeric input, selection/toggles, data/collections, layout/containers, navigation/menus, overlays/feedback, charts, and pointer-playground/custom). Each page states the Ant Design interaction pattern for that family (states, intents such as primary/default/dashed/text/link and danger, sizing on the 8-unit grid / `controlHeight 32`, spacing, feedback) and maps it onto **this repository's existing machinery** — which control to compose, which token-taxonomy entries supply the values, which visual-state the resolver drives, and which color policy validates the pairing.

Crucially, each page also adopts Ant's **semantic-parts model** (the canonical, machine-readable form of which is published at `https://ant.design/llms-semantic.md`): for the Ant component(s) it covers, it enumerates the Ant **semantic parts** — the named component regions such as `root`, `header`, `body`, `icon`, and `content` — and maps each part to (a) the corresponding repo control/region, (b) the token-taxonomy entry that supplies that region's material, and (c) the resolver visual-state where applicable. This embodies Ant's core split — **tokens = atomic design materials, semantic styles = how those materials are applied to named regions** — translated entirely into local primitives. The developer can therefore translate any Ant semantic slot to the corresponding repo control region, token, and resolver state without reading framework source and without introducing any React/DOM concept (Ant's own realization of semantic styling — React `classNames` props over an HTML/CSS DOM — is explicitly *not* adopted).

**Why this priority**: This is the MVP and the substance of F6. The per-family mapping is the artifact that turns the F1–F5 machinery into something a contributor can apply on purpose. It delivers value even if the agent skill (US2) and the page-template recipes (US3) are never written.

**Independent Test**: Open the docs; confirm there is exactly one pattern page per catalog control family and that each page names a concrete repo control, at least one token-taxonomy entry, the resolver visual-state vocabulary, a color policy, and — for the Ant component(s) it covers — the Ant semantic parts mapped to repo control regions + tokens. Run an automated honesty check that fails if any referenced control id, token entry, or resolver/policy symbol does not exist in the current public surface.

**Acceptance Scenarios**:

1. **Given** the catalog control families, **When** the docs are built, **Then** every family has exactly one Ant interaction-pattern page (none missing, none duplicated).
2. **Given** a pattern page, **When** the honesty check parses its references, **Then** every named control, token-taxonomy entry, and resolver/policy symbol resolves against the current public API surface (no dangling or invented references).
3. **Given** any pattern page, **When** a reviewer reads it, **Then** it expresses the pattern in terms of local primitives only (tokens / controls / renderer / policy / resolver) and references no React, DOM, HTML, or CSS construct as an implementation requirement.
4. **Given** a pattern page that covers one or more Ant components, **When** a reader looks for an Ant semantic part (a named region such as `header` or `icon`), **Then** the page declares the Ant component, names that semantic part, and maps it to a repo control region plus the token entry that supplies its material (and the resolver visual-state where applicable), so the reader can apply the part without consulting the upstream `classNames`/DOM mechanism.

---

### User Story 2 - `fs-gg-ant-design` agent skill (Priority: P2)

A contributor (human or coding agent) working on Ant-styled UI in this repository invokes or consults the `fs-gg-ant-design` skill. The skill is a single advisory `SKILL.md` (following the repo's existing skill format and the constitution's "Local Skills" model — advisory, never a gate) that translates Ant Design's stable ideas into this repo's machinery: it points to the public token taxonomy, the `wcag`/`ant` color policy, the central style resolver, the one-control-set-many-themes layering rule, the per-family pattern docs from US1, and the central Ant source-of-truth hub (the three Ant LLM files) so contributors and agents draw Ant facts from one canonical place. When a task matches "build/adjust Ant-styled UI," the skill steers the contributor to the correct local seam instead of generic or React-derived guidance.

**Why this priority**: The skill multiplies the docs' value by making them reachable at the moment of work, but it is strictly additive over US1 and explicitly optional/low-priority in the plan. US1 alone is shippable.

**Independent Test**: Confirm the skill file exists at its documented location with valid front-matter (name, description) matching the repo's skill format, that it links to the US1 pattern docs and the relevant public F1–F5 surface, and that an automated check confirms every machinery path / symbol / doc link it cites resolves.

**Acceptance Scenarios**:

1. **Given** the repo skill format, **When** the `fs-gg-ant-design` skill is validated, **Then** its front-matter parses and contains the required fields, and it is marked advisory (not a mandatory gate).
2. **Given** the skill body, **When** the honesty check runs, **Then** every cited token entry, policy, resolver symbol, control, and doc link resolves (no dangling references).
3. **Given** a contributor consulting the skill for an Ant control task, **When** they follow it, **Then** it directs them to compose existing controls + tokens + resolver + policy and explicitly warns against forking controls per theme or adding a React/DOM dependency.

---

### User Story 3 - Enterprise page-template recipes (Priority: P3)

A developer planning an Ant-style application screen reads recipe docs for Ant's enterprise page templates — workbench, list, detail, form, result, and exception — expressed as compositions over this repo's existing controls and layout primitives. Each recipe describes the page's structure, the control families it draws on, and the token/spacing rules it follows, and is explicitly framed as the target shape for the future Workstream D3 kits and the G3 Ant showcase (without implementing them here).

**Why this priority**: These recipes are forward-looking groundwork for D3/G3 rather than something consumed today, so they are the lowest-priority slice. They are valuable as captured intent but are not required for US1/US2 to deliver value.

**Independent Test**: Confirm a recipe doc exists for each of the six named enterprise page templates, each referencing only existing control families and tokens, and that the honesty check passes on its references. Confirm each recipe states it is a recipe/intent, not an implemented kit.

**Acceptance Scenarios**:

1. **Given** the six enterprise page templates, **When** the docs are built, **Then** each has exactly one recipe doc.
2. **Given** a recipe, **When** the honesty check runs, **Then** every referenced control family and token resolves, and the recipe is clearly marked as forward-looking (target for D3/G3), not as shipped behavior.

---

### Edge Cases

- **A referenced control/token/resolver symbol is renamed or removed later.** The automated honesty check MUST fail, surfacing the broken reference so docs cannot silently drift from the code (same discipline as the gallery's catalog-coverage check).
- **A new control family is added to the catalog.** The per-family completeness check MUST fail until a corresponding Ant pattern page exists, so coverage stays exhaustive.
- **An Ant idea has no faithful local realization** (e.g., it depends on DOM/CSS behavior with no Skia/F#-IR analogue). The docs MUST record it as explicitly out of scope / not adopted, with the reason, rather than implying a mapping that does not exist.
- **The skill is consulted in a non-Ant context.** The skill MUST scope itself to Ant-styled UI work and defer to general guidance otherwise, never blocking or overriding other work (advisory only).

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The feature MUST provide exactly one Ant interaction-pattern reference document per catalog control family, covering the full set of families used by the Controls Gallery (display, buttons, text/numeric input, selection/toggles, data/collections, layout/containers, navigation/menus, overlays/feedback, charts, pointer-playground/custom). *Coverage is anchored to the code-derived `Catalog.categories` set (the 11 lowercase categories `display, input, selection, layout, navigation, overlay, feedback, data, chart, graph, custom`), which is a strict superset of these 10 Gallery families — the only delta is that `chart` and `graph` get separate docs rather than a merged "charts" page. See research R1 for the rationale and the index (`README.md`) cross-map from each Gallery family label to its catalog category.*
- **FR-002**: Each pattern document MUST express the Ant pattern in terms of this repository's existing machinery only — concrete control(s), token-taxonomy entries, the central style resolver's visual-state/intent vocabulary, and the applicable color policy — and MUST NOT require any React, DOM, HTML, or CSS construct.
- **FR-003**: Each pattern document MUST reference the now-public F1–F5 surface (token taxonomy, central style resolver, and the `wcag`/`ant` color-policy concept) so a reader is pointed at the real, shipped seams rather than invented ones.
- **FR-004**: The feature MUST provide a single advisory agent skill, `fs-gg-ant-design`, as a `SKILL.md` conforming to the repository's existing skill format, that maps Ant Design's stable ideas onto the repo's token/control/renderer/policy machinery and links to the per-family pattern docs.
- **FR-005**: The agent skill MUST be advisory per the constitution's Local Skills model — it MUST NOT introduce a mandatory gate, blocking step, or any change that conditions build/test/merge readiness on its use.
- **FR-006**: The feature MUST provide enterprise page-template recipe documents for the six Ant templates (workbench, list, detail, form, result, exception), each composed only of existing control families and tokens and explicitly marked as forward-looking groundwork for Workstream D3 (kits) and G3 (Ant showcase), not as implemented behavior.
- **FR-007**: The docs and the skill MUST reinforce the constitution's layering rule — one semantic control set styled by themes, no per-theme control forks (no `AntButton` behavior copies) — and the rule that Ant is adopted as a design language only, with no React/DOM dependency. Enforcement is distributed and matches what the coverage check can verify: every pattern doc MUST carry the design-language-only assertion line, and the pattern index (`README.md`) plus the `fs-gg-ant-design` skill MUST state the one-semantic-control-set / no-per-theme-fork layering rule. The automated check verifies the design-language assertion on every pattern doc and the layering statement in the skill; the index reinforces the layering rule for readers (a review item, not a machine assertion).
- **FR-008**: The feature MUST add an automated honesty/coverage check (in the spirit of the gallery's catalog-coverage check and the existing docs-build checks) that fails if (a) any control family lacks a pattern page or has more than one, (b) any of the six enterprise templates lacks a recipe, or (c) any control id, token-taxonomy entry, resolver/policy symbol, or internal doc link referenced by the docs or the skill does not resolve.
- **FR-009**: The feature MUST NOT change any public API surface, token value, theme shape, or observable rendering output; the surface-drift and design-token-drift gates MUST remain green with no baseline regeneration required.
- **FR-010**: Where an Ant idea has no faithful local realization, the docs MUST record it as explicitly not adopted (with the reason), rather than describing a mapping that does not exist. In particular, the docs MUST distinguish the **adopted semantic-parts concept** (named component regions; tokens-as-materials / semantic-styles-as-application) from the **not-adopted Ant realization mechanism** — React `classNames` props over an HTML/CSS DOM structure — and MUST record that mechanism as the canonical not-adopted item, so readers never mistake the portable concept for the framework binding.
- **FR-011**: Each pattern document MUST, for the Ant component(s) it covers, enumerate the Ant **semantic parts** (named regions, e.g. `root`, `header`, `body`, `icon`, `content`) drawn from the upstream source `https://ant.design/llms-semantic.md` (via the central hub), and map each enumerated part to (a) the repo control/region that realizes it, (b) the token-taxonomy entry that supplies that region's material, and (c) the central resolver's visual-state where the part is stateful. The mapping MUST be expressed in local primitives only (no React/DOM/HTML/CSS). The automated honesty/coverage check (FR-008) verifies *structure and reference integrity* of this mapping — that each pattern doc declares its Ant component(s) and that the named semantic parts and their mapped control/token/resolver references are present and resolve — while editorial accuracy of the part-to-region mapping remains a review concern.
- **FR-012**: The feature MUST establish a single, central **Ant upstream source-of-truth hub** (a repo-resident reference document) that catalogs the three Ant LLM files — `llms.txt` (index/navigation), `llms-full.txt` (full API/usage + component design tokens), and `llms-semantic.md` (semantic parts) — recording each file's URL, role, and a retrieval date, and stating the adopted-concept / not-adopted-mechanism posture (FR-010). The pattern docs, the `fs-gg-ant-design` skill, and the relevant repo-level agent context (the product docs index, the coding-agent context file, and the most relevant existing product skills) MUST cite this central hub as the canonical Ant source rather than ad-hoc upstream URLs, so all of FS.GG treats the three files as the one authoritative Ant reference. This remains Tier 2 (docs/agent-context only): it adds no public API, package, dependency, token value, or behavior change.

### Key Entities *(include if data involved)*

- **Ant interaction-pattern document**: One reference page per control family. Captures the Ant pattern (states, intents, sizing on the 8-unit grid / `controlHeight 32`, spacing, feedback) and its mapping to repo control(s), token-taxonomy entries, resolver visual-state/intent vocabulary, and color policy. Also contains the **semantic-part mapping** (below). Relationship: one per catalog control family; every family has exactly one.
- **Ant semantic-part mapping**: Within a pattern document, the enumeration of an Ant component's named regions (semantic parts, e.g. `root`/`header`/`body`/`icon`/`content`, sourced from `https://ant.design/llms-semantic.md`) and, per part, the repo control region + token-taxonomy entry + resolver visual-state that realizes it. Embodies Ant's tokens-as-materials / semantic-styles-as-application split in local primitives. Relationship: one or more per pattern document, keyed by the Ant component covered; the React `classNames`/DOM realization is recorded as not-adopted (FR-010).
- **`fs-gg-ant-design` agent skill**: A single advisory `SKILL.md` translating Ant's stable ideas to the repo's machinery and linking to the pattern documents. Relationship: references the pattern docs and the public F1–F5 surface; is never a gate.
- **Enterprise page-template recipe**: One recipe per Ant enterprise template (workbench, list, detail, form, result, exception), composed of existing control families and tokens, marked as forward-looking groundwork. Relationship: one per named template; targets D3 kits and the G3 showcase.
- **Ant upstream source-of-truth hub**: A single repo-resident reference document cataloging the three Ant LLM files (`llms.txt` index, `llms-full.txt` full API/usage + component tokens, `llms-semantic.md` semantic parts) with each file's URL, role, and a retrieval date, plus the curated in-repo snapshot of the relevant component slots and the adopted-concept / not-adopted-mechanism posture. Relationship: cited centrally by the pattern docs, the `fs-gg-ant-design` skill, the product docs index, the coding-agent context file, and the most relevant existing product skills; it is the one canonical Ant reference for FS.GG.
- **Docs honesty/coverage check**: An automated check asserting per-family and per-template completeness, that every referenced control/token/resolver/policy/doc symbol resolves, that each pattern doc declares well-shaped semantic-part refs, and that the central source-of-truth hub exists, lists all three Ant LLM files, and is cited by the skill and the index. Relationship: gates the docs against drift from the code and from the canonical Ant source set.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: 100% of catalog control families have exactly one Ant interaction-pattern document (zero families missing, zero duplicated), confirmed by the automated coverage check.
- **SC-002**: 100% of references in the pattern docs and the skill (control ids, token-taxonomy entries, resolver/policy symbols, internal doc links) resolve against the current public surface — zero dangling or invented references.
- **SC-003**: All six enterprise page-template recipes (workbench, list, detail, form, result, exception) exist, each marked as forward-looking groundwork, confirmed by the coverage check.
- **SC-004**: Zero React/DOM/HTML/CSS constructs appear as implementation requirements anywhere in the docs or skill (Ant adopted as design language only), confirmed by review and reinforced in every pattern page.
- **SC-005**: The public-API surface-drift gate and the design-token-drift gate remain green with no baseline regeneration, confirming the feature changed no public surface, token value, or rendering behavior.
- **SC-006**: A contributor (or agent) given an Ant-styled control task can, using only the `fs-gg-ant-design` skill and the pattern docs, identify the correct repo control, token entries, resolver state, and color policy for that pattern without consulting framework source code.
- **SC-007**: The full existing test suite remains green and the documented build/test commands are unaffected, since the feature adds no compiled code.
- **SC-008**: For every Ant component covered by a pattern doc, a contributor can — from the pattern docs alone — identify the repo control region, the token entry, and the resolver visual-state for each of that component's enumerated Ant semantic parts; the semantic-part mapping is complete (every declared part is mapped) and every mapped reference resolves against the current public surface, confirmed by the coverage check.
- **SC-009**: There is exactly one central Ant source-of-truth hub, it catalogs all three Ant LLM files (`llms.txt`, `llms-full.txt`, `llms-semantic.md`) with their roles and a retrieval date, and the `fs-gg-ant-design` skill and the pattern index both cite it — so a contributor or agent can locate the canonical Ant upstream source (and the right file for a given need) from one place, confirmed by the coverage check.

## Assumptions

- **Source material**: The Ant Design adoption analysis (`docs/reports/2026-06-09-1538-ant-design-ui-story-adoption-analysis.md`) originated in the sibling `FS-Skia-UI` repository and is **not present in this repo**. F6 relies on the stable Ant facts already captured in this repo's plan and the landed F1–F5 work (Ant brand blue `#1677ff`; functional success/warning/error/info families; 8-unit grid; `controlHeight 32`; seed → map → alias → component taxonomy; `wcag`/`ant` policies; central resolver). The planning phase MAY pull additional detail from the sibling report if it is made available, but the spec does not depend on it.
- **Authoritative Ant upstream source-of-truth (the three Ant LLM files)**: Ant Design publishes three complementary machine-readable documentation files, and this feature adopts all three as the **canonical, repo-wide upstream source of truth** for Ant facts — replacing reliance on general/model knowledge: (1) `https://ant.design/llms.txt` — the *index/navigation* (llms.txt standard: a table of contents linking design guidance + 70+ component docs + semantic docs, EN/CN); (2) `https://ant.design/llms-full.txt` — the *full aggregated API/usage* (74 components: when-to-use, examples, prop tables, component design tokens, FAQs), the source for control/token-taxonomy mappings; (3) `https://ant.design/llms-semantic.md` — the *semantic-parts model* (named regions per component + the tokens-as-materials / semantic-styles-as-application split), the source for the `part:` mappings (FR-011). A single **central source-of-truth hub** cataloging these three files (roles, URLs, retrieval date) plus a curated in-repo snapshot of the relevant component slots is saved in the repo so docs, the skill, and other FS.GG agent context cite a stable, in-repo reference rather than depending on network access at build/check time. Only the design-language *concept* (named regions; tokens-vs-semantic-styles; Ant's stable interaction patterns) is adopted; the React `classNames`/DOM *mechanism* from these sources is explicitly out of scope (FR-010).
- **Control families**: The ten families are taken from the Controls Gallery (feature 123) catalog taxonomy, which is the repo's authoritative grouping of the 52 controls.
- **Docs location**: Pattern docs and recipes live under `docs/` (most likely a new `docs/product/ant-design/` area beside the existing `decisions/`, `layering.md`, and `module-map.md`); exact paths are a planning detail.
- **Skill location and format**: The `fs-gg-ant-design` skill is a repo-local advisory skill under `.claude/skills/` (per the constitution's Local Skills section), using the same front-matter format as the existing `.claude/skills/*/SKILL.md` files. Whether it is user-invocable is a planning detail; advisory status is fixed by FR-005.
- **Tier 2 / no contract chain**: Because the feature adds no public `.fs`/`.fsi` surface and no behavioral change, the constitution's Spec → FSI → semantic-tests → implementation order does not apply in full; verification is the automated docs honesty/coverage check plus review, and `.fsi`/baselines are untouched.
- **Decision record**: A decision record (next number after `0004`) is optional and only authored if F6 makes a recorded scope decision (e.g., "Ant patterns are docs-only; no token or behavior change in F6"); this is left to planning.
- **Out of scope**: No new controls, themes, or kits (D2/D3); no concrete Ant `Theme` instance; no enterprise-template *implementation*; no React/DOM/CSS dependency; **no adoption of Ant's `classNames`-prop / DOM semantic-styling mechanism** (only the named-region *concept* is adopted, mapped to local primitives); no token-value or public-API change; no migration of the existing skill-sync tooling.
