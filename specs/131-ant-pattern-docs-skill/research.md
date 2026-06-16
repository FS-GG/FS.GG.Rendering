# Research: Ant interaction-pattern docs + `fs-gg-ant-design` agent skill (F6)

Phase 0 decisions. Each resolves an open choice the spec deferred to planning. No `NEEDS CLARIFICATION` markers remained in the spec; these record the rationale behind the planning defaults.

## R1 — Coverage anchor: `Catalog.categories`, not the gallery's 10 pages

**Decision**: Anchor "one pattern doc per control family" to the **code-derived `Catalog.categories`** value (the distinct `Category` strings across `Catalog.supportedControls`), producing **one pattern doc per category**. The current categories are the **lowercase** distinct `Category` values (verified against `src/Controls/Catalog.fs`): `display, input, selection, layout, navigation, overlay, feedback, data, chart, graph, custom` (11, summing to all 52 controls). `family` front-matter MUST match these case-sensitively (per the grammar contract).

**Rationale**: The spec's FR-001 enumerated the Controls Gallery's **10 presentation families** (display/typography, buttons, text/numeric input, selection/toggles, data/collections, layout/containers, navigation/menus, overlays/feedback, charts, pointer-playground/custom). But that 10-page grouping lives in the `samples/` gallery project (feature 123), which `Controls.Tests` does not reference, and it collapses `Chart`+`Graph` into one "charts" page and renames others. Anchoring coverage to `Catalog.categories` instead makes the completeness check (FR-008) enumerate a **real source of truth that `Controls.Tests` already sees**, and stays automatically correct if a category is added or removed. The 11 category docs are a strict **superset** of the spec's 10 families (the only delta is `Chart` and `Graph` get separate docs rather than a merged "charts" page), so FR-001's intent — exhaustive, one-doc-per-family coverage — is satisfied more rigorously, not weakened.

**Reconciliation with the spec**: This is a planning refinement, not a contradiction. The pattern index (`README.md`) cross-references each catalog category to its gallery family label so a reader coming from the gallery taxonomy still finds the right page. Recorded so the spec's "10 families" and the plan's "11 category docs" are not read as a discrepancy.

**Alternatives considered**:
- *Hard-code the 10 gallery family labels in the test* — rejected: drifts from code, duplicates the gallery's grouping, and couples F6 to a samples project the test cannot reference.
- *Reference the gallery's `GalleryPage list`* — rejected: cross-tier dependency from a test project into `samples/`; brittle and out of scope.

## R2 — Machine-checked reference grammar: front-matter + a fenced `refs` block

**Decision**: Each pattern/recipe doc carries (a) a minimal YAML-style front-matter with one typed field — `family: <category>` for pattern docs, `template: <name>` for recipes, plus `status: groundwork` for recipes — and (b) a `## Machine-checked references` section containing a single fenced ` ```refs ` block, one typed reference per line:

```
control:button-primary
token:Seed.colorPrimary
resolver:resolveDefault
policy:ant
doc:../decisions/0005-ant-design-pattern-docs.md
```

**Rationale**: A dedicated, explicit reference block is unambiguous to line-parse (no Markdown parser dependency — honoring the repo's no-extra-parser-dep posture), keeps prose free to read naturally, and lets the test resolve each reference against the correct source of truth by prefix. Front-matter is line-parsed too (the test only needs the one `family`/`template`/`status` key). This mirrors how the gallery derives its coverage mapping from declared structure rather than from prose.

**Alternatives considered**:
- *Scan all inline-code spans in prose* — rejected: high false-positive rate (every backticked word becomes a "reference"), brittle.
- *Add a Markdown/YAML parser* — rejected: violates the minimize-dependencies constraint for what is a few lines of `String.Split`.

## R3 — Reference resolution: reflection + catalog lookup, no new dependency

**Decision**: The test resolves each reference type as follows:
- `control:<id>` ∈ `Catalog.supportedControls |> List.map (fun c -> c.Id)`.
- `resolver:<member>` ∈ the public members of `FS.GG.UI.DesignSystem.StyleResolver` (`baseStyleFor`, `neutralPolicy`, `resolve`, `resolveDefault`, `IntentPolicy`), resolved by reflection over the module type.
- `token:<Module>.<member>` resolves by reflection over the public token types in `FS.GG.UI.DesignSystem` — the nested `DesignTokensExt` modules (`Seed`, `Map`, `Alias`, `Component`, `Space`, `Type`, `Elevation`, `Density`, …) and the flat `DesignTokens` module.
- `policy:<name>` resolves via `ColorPolicy.byName` (reached through the existing `Color` `InternalsVisibleTo Controls.Tests`) — currently `wcag` and `ant`.
- `doc:<relative-path>` resolves by `File.Exists` relative to the citing doc.

**Rationale**: Every needed symbol is reachable from `Controls.Tests` with the project references it **already has** (confirmed: Controls, DesignSystem, Themes.Default, Color). Reflection over the public surface means the check fails the moment a referenced symbol is renamed/removed — exactly the drift protection FR-008/SC-002 require. No new project reference, no new dependency.

**Alternatives considered**:
- *Assert against a hand-maintained allowlist of valid symbols* — rejected: a second source of truth that itself drifts.
- *Reference Color's `ColorPolicy` by a public API* — not possible (127 is internal-only, no surface baseline for `Color`); the IVT path is the established F2 pattern and keeps `Color` un-promoted.

## R4 — Docs & skill placement

**Decision**: Pattern/recipe/index docs under `docs/product/ant-design/` (`patterns/`, `templates/`, `README.md`). The skill at `.claude/skills/fs-gg-ant-design/SKILL.md`. Optional decision record `docs/product/decisions/0005-ant-design-pattern-docs.md`.

**Rationale**: `docs/product/` already hosts the design-language artifacts (`layering.md`, `module-map.md`, `decisions/`); F6 is product/design-language documentation, so it belongs there rather than in package-owned `src/*/skill/` docs. `.claude/skills/` is the constitution's named home for repo-local advisory skills, matching the existing `.claude/skills/*/SKILL.md` layout and front-matter format (`name`, `description`, `metadata`, `user-invocable`, `disable-model-invocation`).

## R5 — Skill front-matter & invocability

**Decision**: `fs-gg-ant-design/SKILL.md` uses the repo's existing skill front-matter (`name: "fs-gg-ant-design"`, a one-line `description`, `metadata.author`/`source`, `user-invocable: true`, `disable-model-invocation: false`), and is **advisory** — its body steers contributors to existing seams and the pattern docs, and explicitly warns against per-theme control forks and React/DOM dependencies. It introduces no gate, no task metadata, and no readiness blocking (FR-005, constitution Local Skills).

**Rationale**: Matching the established skill format makes it discoverable by the same machinery as the other repo skills. `user-invocable: true` lets a contributor pull it up on demand without making it a mandatory step. The constitution is explicit that skills are advisory aids, never gates.

**Alternatives considered**: *A package-owned `src/*/skill/SKILL.md`* — rejected: F6's skill spans controls + tokens + policy + resolver across several packages, so it is a product-level skill, not owned by one package.

## R6 — Constitution narrowing for a docs-only Tier-2 feature

**Decision**: Record that Principle I's "sketch in FSI" step does not apply because F6 adds **no public `.fs`/`.fsi` surface**. The honored order is: spec → author the coverage/honesty test against the doc contract → write docs/skill to satisfy it. `.fsi` files and surface/token baselines are untouched (Tier 2).

**Rationale**: The constitution's Tier 2 definition (internal change, no behavioral/public-API change) explicitly requires only spec + tests, not `.fsi`/baseline updates. A documentation feature is the canonical Tier-2 case. Disclosing the narrowing keeps the deviation explicit rather than silent.

## R7 — Enforcing "design language only, no React/DOM" (SC-004)

**Decision**: Automated enforcement is **positive, not negative**: the test asserts that every pattern doc contains the required design-language assertion line (a fixed marker such as "Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency"), rather than trying to prove the absence of forbidden terms. Absence-of-React is additionally a review item (SC-004 names review as the confirmer). Where an Ant idea has no faithful local realization (FR-010), the doc records it under a "Not adopted" subsection with the reason.

**Rationale**: A denylist scan for "css"/"dom"/"html" produces false positives (prose legitimately explains *what Ant does in the browser* before mapping it to local primitives). A required positive assertion is deterministic, false-positive-free, and still guarantees every page reinforces the rule. Review covers the residual judgment call.

**Alternatives considered**: *Denylist token scan with an inline opt-out marker* — rejected as the primary mechanism (noisy, gameable); folded into review instead.

## R8 — Ant LLM documentation set as the central source of truth + semantic-parts model

**Decision**: Adopt Ant Design's **three machine-readable LLM documentation files** as the canonical, repo-wide upstream source of truth for Ant facts (FR-012), and adopt Ant's **semantic-parts model** as a first-class structuring concept for the pattern docs (FR-011/SC-008). The three files and their roles (verified 2026-06-16):

| File | Role | This repo uses it for |
|---|---|---|
| `https://ant.design/llms.txt` | **Index / navigation** (llms.txt standard) — TOC linking design guidance, 70+ component docs, and semantic docs (EN/CN) | discovery: which Ant doc to read for a given need |
| `https://ant.design/llms-full.txt` | **Full aggregated API/usage** — 74 components: when-to-use, examples, prop tables, **component design tokens**, FAQs | grounding the control + token-taxonomy mappings (the "materials") |
| `https://ant.design/llms-semantic.md` | **Semantic-parts model** — named regions per component + tokens-as-materials / semantic-styles-as-application split | the `part:` semantic-part mappings (FR-011) |

From `llms-semantic.md`, what is adopted is the **concept**: (a) the named-region vocabulary per component (e.g. `root`, `header`, `body`, `icon`, `content`), and (b) Ant's core split **tokens = atomic design materials / semantic styles = how those materials are applied to named regions**. What is **not** adopted is the **mechanism**: React `classNames` props over an HTML/CSS DOM (recorded as the canonical "Not adopted" item per FR-010). The same concept-not-mechanism posture applies to the whole set.

**Mapping onto repo machinery**: each Ant semantic part maps to a repo **control region** (the control's structural element, named in prose / aligned with the control's `VisualStates` where stateful), whose *material* comes from a **token-taxonomy entry** (`DesignTokensExt`/`DesignTokens`), and whose *stateful styling* (hover/active/disabled/selected/focus) comes from the central **`StyleResolver`** visual-state. So a part like Button's `icon` → repo `icon-button`/`button` icon region, material from `Seed.*`/`Component.Button.*`, states via `resolveDefault`/`resolve`. This is exactly the tokens-as-materials / semantic-styles-as-application split expressed in local primitives.

**Central hub + curated in-repo snapshot (no network at check time)**: rather than depend on `ant.design` being reachable when the coverage check or a contributor reads the docs, the feature saves a single **central source-of-truth hub** at `docs/product/ant-design/reference/ant-llms-sources.md`. The hub catalogs the three files (URL, role, retrieval date), states the concept-not-mechanism posture, and embeds a **curated snapshot** of only the Ant component slots (named semantic parts) for the families the feature covers. Docs and the skill cite this in-repo hub. The hub is a **referenced source document**, not a machine-resolved target — the coverage check verifies it exists, lists all three files, and is cited by the skill + index, but never resolves `part:` refs or token mappings against it.

**Grammar/check extension (structure-only)**: the semantic-part mapping is encoded in the existing ` ```refs ` block via a new typed line `part:<AntComponent>/<partName>` (e.g. `part:Button/icon`). The coverage check validates only its **shape** — non-empty `<AntComponent>` and non-empty `<partName>`, with exactly one `/` separator — and requires each pattern doc to declare **≥1 `part:`** plus the already-required `control:`/`token:`/`resolver:`/`policy:` refs (which resolve against code). A `part:` ref does **not** resolve against any code symbol — it is a declared upstream-vocabulary reference, deliberately not coupled to a code target (Ant component names are not repo symbols). The new named case is `Pattern_docs_declare_semantic_parts`; `No_unknown_ref_prefixes` is widened to allow the `part` prefix. The check explicitly does **not** assert that a given Ant part semantically equals a given repo region — that editorial judgment stays a review concern (FR-011).

**Rationale**: encoding parts as `part:` lines keeps the no-extra-parser-dependency posture (still line-parsed) and the same refs-block ergonomics; validating shape-not-semantics keeps the check deterministic and false-positive-free while still guaranteeing every pattern doc *declares* its Ant components and their named parts and carries resolving control/token/resolver refs to anchor them. The curated snapshot removes network dependence and pins the vocabulary to a dated source.

**Alternatives considered**:
- *Resolve `part:` against a code enum of region names* — rejected: there is no such public enum, and inventing one would add public surface (breaks Tier 2) and a second source of truth.
- *Embed the part mapping as a Markdown table parsed by the check* — rejected: needs a table parser and is brittle; the `part:` line reuses the existing line-parser.
- *Depend on `ant.design/llms-semantic.md` live at check time* — rejected: introduces a network dependency into a headless/deterministic test; the curated in-repo snapshot is the deterministic substitute.

## R9 — Repo-level placement & wiring of the central source-of-truth hub (FR-012)

**Decision**: Make the hub `docs/product/ant-design/reference/ant-llms-sources.md` the **single canonical Ant reference for all of FS.GG**, not just feature 131. It is cited centrally by: (1) every pattern doc (a `doc:` ref to the hub), (2) the `fs-gg-ant-design` `SKILL.md` (a `doc:` ref), (3) the pattern index `README.md` (a `doc:` ref), (4) the product docs area (a pointer from `docs/product/`), (5) the coding-agent context file `CLAUDE.md`, and (6) the most relevant existing product skills (`src/Controls/skill/SKILL.md` and the design-system skill if present) via a short "Ant upstream source of truth" pointer. No raw `ant.design` URL is cited ad hoc outside the hub.

The coverage check enforces the *machine-checkable* slice of this (FR-012/SC-009) with a new case `Upstream_source_hub_is_central`: the hub file exists, its text contains all three file names (`llms.txt`, `llms-full.txt`, `llms-semantic.md`), and both the skill and `README.md` carry a `doc:` ref that resolves to the hub. The broader repo-level pointers (product docs index, `CLAUDE.md`, other skills) are a review concern (editing those files is docs/agent-context only — Tier 2, no public surface).

**Rationale**: a single hub avoids drift between many ad-hoc upstream citations, gives one place to bump the retrieval date when Ant's docs move, and lets any FS.GG contributor or agent find the canonical Ant source (and the right file for the need) in one hop. Wiring it into `CLAUDE.md` and the key skills makes it the default the model reaches for.

**Alternatives considered**:
- *Cite the three URLs directly in each doc/skill* — rejected: N copies drift; no single retrieval-date owner.
- *Put the hub at repo root or in `.specify/`* — rejected: it is product/design-language documentation, so it belongs under `docs/product/ant-design/` beside the other Ant artifacts, surfaced repo-wide by pointers.

## Summary of decisions

| # | Decision |
|---|---|
| R1 | Coverage anchored to `Catalog.categories` (11 docs); superset of the spec's 10 gallery families; index cross-maps them. |
| R2 | Front-matter (`family`/`template`/`status`) + a fenced ` ```refs ` block of typed references; line-parsed, no parser dependency. |
| R3 | References resolved by reflection over public `StyleResolver`/`DesignTokensExt`/`DesignTokens`, `Catalog`, and `ColorPolicy.byName` (existing IVT). |
| R4 | Docs under `docs/product/ant-design/`; skill under `.claude/skills/fs-gg-ant-design/`; optional decision record `0005`. |
| R5 | Skill uses repo front-matter, `user-invocable: true`, advisory only — no gate. |
| R6 | Docs-only Tier 2: Principle I's FSI step N/A; `.fsi`/baselines untouched; deviation disclosed. |
| R7 | "No React/DOM" enforced positively (required assertion line per page) + review; "Not adopted" subsection for unfaithful ideas. |
| R8 | Adopt the three Ant LLM files (`llms.txt` index / `llms-full.txt` API+tokens / `llms-semantic.md` semantic parts) as the canonical upstream source of truth; central hub + curated snapshot at `docs/product/ant-design/reference/ant-llms-sources.md`; encode parts via `part:<Component>/<part>` refs validated shape-only; adopt the design-language *concept*, NOT the React `classNames`/DOM mechanism. |
| R9 | The hub is repo-level: cited by every pattern doc, the skill, the index, the product docs area, `CLAUDE.md`, and key existing skills; coverage case `Upstream_source_hub_is_central` enforces existence + all-three-files-listed + skill/README `doc:`-link. |
