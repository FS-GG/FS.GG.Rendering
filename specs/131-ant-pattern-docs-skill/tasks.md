---
description: "Task list for feature 131 — Ant interaction-pattern docs + fs-gg-ant-design agent skill"
---

# Tasks: Ant interaction-pattern docs + `fs-gg-ant-design` agent skill

**Input**: Design documents from `/specs/131-ant-pattern-docs-skill/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/ (all present)

**Tests**: INCLUDED. The feature's single executable artifact is the docs honesty/coverage check (FR-008), and the constitution requires "fails before, passes after" evidence (V). Per quickstart V1, the test is authored first (red) and is the blocking prerequisite that defines the doc contract — it lives in Phase 2 (Foundational).

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) so each can be implemented and verified independently against the same coverage check.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1/US2/US3)
- Exact file paths are included in each task

## Path Conventions

Documentation feature — no `src/` product code. Deliverables live at the repository root under `docs/product/ant-design/`, `.claude/skills/fs-gg-ant-design/`, `docs/product/decisions/`, and the single test under `tests/Controls.Tests/`.

The 11 catalog families (research R1, from `Catalog.categories`, lowercase, case-sensitive): `display, input, selection, layout, navigation, overlay, feedback, data, chart, graph, custom`. The six enterprise templates: `workbench, list, detail, form, result, exception`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the directory skeleton for all deliverables.

- [X] T001 Create the deliverable directory structure: `docs/product/ant-design/patterns/`, `docs/product/ant-design/templates/`, `docs/product/ant-design/reference/`, `.claude/skills/fs-gg-ant-design/`, and confirm `docs/product/decisions/` exists (it hosts `0004-*`).

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Author the docs honesty/coverage check that defines the doc contract for every story. This is the test evidence (Constitution V) and the gate every later phase is verified against.

**⚠️ CRITICAL**: No user-story doc work should be verified until the test exists and fails (red). Per quickstart V1 the test is written before the docs.

- [X] T002 Author the coverage check `tests/Controls.Tests/Feature131AntPatternDocsTests.fs` implementing all 13 named cases from `contracts/docs-coverage-check.md`: `Family_coverage_is_bijective`, `Each_template_recipe_present_once_and_groundwork`, `All_control_refs_resolve`, `All_resolver_refs_resolve`, `All_token_refs_resolve`, `All_policy_refs_resolve`, `All_doc_links_resolve`, `Pattern_docs_have_required_refs`, `Pattern_docs_declare_semantic_parts`, `Pattern_docs_state_design_language_only`, `Skill_is_advisory_and_reminds_layering`, `Upstream_source_hub_is_central`, `No_unknown_ref_prefixes`. `Upstream_source_hub_is_central` MUST verify the hub `docs/product/ant-design/reference/ant-llms-sources.md` exists, its text names all three files (`llms.txt`, `llms-full.txt`, `llms-semantic.md`), and the skill + `README.md` each carry a `doc:` ref resolving to it (FR-012, SC-009). **Framework: Expecto** (`[<Tests>] testList`, NOT xUnit); **repo root via the `FS.GG.Rendering.slnx` marker** (walk up from `__SOURCE_DIRECTORY__`, the established `Feature126`/`Feature127` pattern). Add `<Compile Include="Feature131AntPatternDocsTests.fs" />` to `tests/Controls.Tests/Controls.Tests.fsproj` (after `Feature130PublicSurfaceTests.fs`, before `Program.fs`). Implement per research R2/R3/R8: line-parse front-matter (`family`/`template`/`status`) and the fenced ` ```refs ` blocks, and resolve *code* refs by reflection over public `FS.GG.UI.DesignSystem.StyleResolver`, `DesignTokensExt.*`/`DesignTokens`, `Catalog.supportedControls`/`Catalog.categories`, and `ColorPolicy.byName` (via the existing `Color` IVT). `No_unknown_ref_prefixes` MUST allow the `part` prefix (allowed set: `control`/`token`/`resolver`/`policy`/`doc`/`part`). `Pattern_docs_declare_semantic_parts` MUST verify each pattern doc has ≥1 well-shaped `part:<Component>/<partName>` ref (non-empty component, non-empty part, exactly one `/`) plus the companion `control:`/`token:`/`resolver:` refs — **`part:` is validated by SHAPE ONLY and is NEVER resolved against code** (it is upstream Ant vocabulary). No new project reference, no Markdown/YAML/JSON parser dependency. Failure messages MUST name the offending file and the specific missing family/template/part or dangling ref (Constitution VI).
- [X] T003 Run `dotnet test tests/Controls.Tests --filter "131"` and confirm it COMPILES and FAILS (red): `Family_coverage_is_bijective` reports categories with no pattern doc; `Pattern_docs_declare_semantic_parts` and the template/skill cases fail too. This is the V1 "fails before" evidence.
- [X] T003a Author the central Ant source-of-truth hub `docs/product/ant-design/reference/ant-llms-sources.md` (research R8/R9, FR-012). MUST: (a) catalog all three Ant LLM files with role + URL + retrieval date — `https://ant.design/llms.txt` (index/navigation), `https://ant.design/llms-full.txt` (full API/usage + component design tokens), `https://ant.design/llms-semantic.md` (semantic parts); (b) state that only the design-language CONCEPT (named regions; tokens-as-materials / semantic-styles-as-application; Ant's stable patterns) is adopted — NOT the React `classNames`-prop / DOM mechanism (FR-010); (c) embed a curated snapshot listing each covered Ant component with its named semantic parts (Button, Input, Checkbox, Card, Tabs, Modal, Alert, Table, Badge, ColorPicker, … as relevant). This is a **referenced source document** (the coverage check verifies it exists + lists all three files + is cited by skill/README, but never reads it for `part:`/token resolution); it MUST exist before pattern docs so their `part:` refs and `doc:` citation have a documented source. Blocks T004–T014.

**Checkpoint**: Coverage check exists and is red, and the Ant semantic-parts snapshot source is in place. User-story docs can now be authored and verified against the check.

---

## Phase 3: User Story 1 - Per-control-family Ant interaction-pattern docs (Priority: P1) 🎯 MVP

**Goal**: One Ant interaction-pattern reference page per catalog control family, each mapping the Ant pattern — including the Ant component's named semantic parts (FR-011) — onto repo controls + token taxonomy + resolver visual-states + color policy, in local primitives only.

**Independent Test**: `dotnet test tests/Controls.Tests --filter "131"` → `Family_coverage_is_bijective`, `Pattern_docs_have_required_refs`, `Pattern_docs_declare_semantic_parts`, `Pattern_docs_state_design_language_only`, and all `*_refs_resolve` / `No_unknown_ref_prefixes` cases pass for the pattern docs. Manually confirm exactly one page per family, each naming a concrete control, ≥1 token entry, a resolver state, a policy, and ≥1 Ant semantic part mapped to a repo region (FR-011/SC-008), with no React/DOM construct.

Each pattern doc MUST carry `family:` front-matter (case-sensitive `Catalog.categories` value), a `## Machine-checked references` section with a ` ```refs ` block holding ≥1 `control:`, ≥1 `token:`, ≥1 `resolver:`, the applicable `policy:` line, and ≥1 `part:<AntComponent>/<partName>` (FR-011), the verbatim assertion `Ant is adopted as a design language only — no React/DOM/HTML/CSS dependency.`, and a "Not adopted" subsection (FR-010) that names the React `classNames`-prop / DOM realization as the canonical not-adopted mechanism. The doc body MUST, for the Ant component(s) it covers, enumerate the Ant semantic parts and map each (in prose) to a repo control region + token + resolver state, citing the central hub via a `doc:../reference/ant-llms-sources.md` ref. All *code* refs (`control`/`token`/`resolver`/`policy`/`doc`) MUST resolve; `part:` refs are shape-checked only.

- [X] T004 [P] [US1] Author `docs/product/ant-design/patterns/display.md` (display family) per the grammar contract.
- [X] T005 [P] [US1] Author `docs/product/ant-design/patterns/input.md` (input family), covering intents (primary/default/dashed/text/link, danger), sizing on the 8-unit grid / `controlHeight 32`.
- [X] T006 [P] [US1] Author `docs/product/ant-design/patterns/selection.md` (selection/toggles family).
- [X] T007 [P] [US1] Author `docs/product/ant-design/patterns/layout.md` (layout/containers family).
- [X] T008 [P] [US1] Author `docs/product/ant-design/patterns/navigation.md` (navigation/menus family).
- [X] T009 [P] [US1] Author `docs/product/ant-design/patterns/overlay.md` (overlay family).
- [X] T010 [P] [US1] Author `docs/product/ant-design/patterns/feedback.md` (feedback family).
- [X] T011 [P] [US1] Author `docs/product/ant-design/patterns/data.md` (data/collections family).
- [X] T012 [P] [US1] Author `docs/product/ant-design/patterns/chart.md` (chart family).
- [X] T013 [P] [US1] Author `docs/product/ant-design/patterns/graph.md` (graph family).
- [X] T014 [P] [US1] Author `docs/product/ant-design/patterns/custom.md` (pointer-playground/custom family).
- [X] T015 [US1] Author `docs/product/ant-design/README.md` index: the three pillars, the one-control-set/no-per-theme-fork layering rule, links to each pattern page, a `doc:reference/ant-llms-sources.md` ref to the central Ant source-of-truth hub and a note on the three Ant LLM files + the adopted-concept / not-adopted-mechanism split (R8/R9, FR-012), and a cross-map from the gallery's 10 family labels to the 11 catalog categories (research R1 reconciliation).
- [X] T016 [US1] Run `dotnet test tests/Controls.Tests --filter "131"` and confirm `Family_coverage_is_bijective`, `Pattern_docs_have_required_refs`, `Pattern_docs_declare_semantic_parts`, `Pattern_docs_state_design_language_only`, and the per-pattern reference-resolution cases pass (SC-001, SC-002, SC-004, SC-008; FR-011). To prove the guards bite (V3/V8), temporarily break one `token:` ref (confirm failure naming it, then revert) AND malform one `part:` ref by dropping the `/` (confirm `Pattern_docs_declare_semantic_parts` fails naming the doc, then revert).

**Checkpoint**: US1 is the shippable MVP — all 11 families documented and honesty-checked, independent of US2/US3.

---

## Phase 4: User Story 2 - `fs-gg-ant-design` agent skill (Priority: P2)

**Goal**: A single advisory `SKILL.md` that maps Ant's stable ideas to the repo's token/control/resolver/policy machinery, links the US1 pattern docs, and steers contributors to the correct local seams.

**Independent Test**: `dotnet test tests/Controls.Tests --filter "131"` → `Skill_is_advisory_and_reminds_layering`, `Upstream_source_hub_is_central`, and `All_doc_links_resolve` pass. Confirm front-matter parses with required fields, the skill is advisory (no gate), and it links the pattern docs and the central Ant source-of-truth hub.

- [X] T017 [US2] Author `.claude/skills/fs-gg-ant-design/SKILL.md` with repo-standard front-matter (`name: "fs-gg-ant-design"`, non-empty `description`, `metadata.author`/`source`, `user-invocable: true`, `disable-model-invocation: false`; research R5). Body MUST: link the US1 pattern docs (≥1 `doc:` ref in the `## Machine-checked references` block), a `doc:` ref to the central Ant source-of-truth hub (`reference/ant-llms-sources.md`, FR-012), and the public F1–F5 seams (≥1 of `token:`/`resolver:`/`policy:`); contain the no-React/DOM statement AND the one-semantic-control-set / no-per-theme-fork statement (FR-007); name the three Ant LLM files as the canonical source via the hub; introduce NO gate, blocking step, or readiness condition (FR-005). All refs MUST resolve.
- [X] T018 [US2] Run `dotnet test tests/Controls.Tests --filter "131"` and confirm `Skill_is_advisory_and_reminds_layering`, `Upstream_source_hub_is_central`, `All_doc_links_resolve`, and the skill's reference-resolution cases pass (FR-004, FR-005, FR-012, SC-002, SC-006, SC-009).

**Checkpoint**: US1 + US2 both pass independently; the docs are reachable at the moment of work.

---

## Phase 5: User Story 3 - Enterprise page-template recipes (Priority: P3)

**Goal**: One forward-looking recipe per Ant enterprise template (workbench, list, detail, form, result, exception), composed only of existing control families and tokens, marked groundwork for D3/G3.

**Independent Test**: `dotnet test tests/Controls.Tests --filter "131"` → `Each_template_recipe_present_once_and_groundwork` passes and all recipe refs resolve. Confirm each recipe is clearly marked groundwork, not shipped behavior.

Each recipe MUST carry `template:` + `status: groundwork` front-matter, a `## Machine-checked references` block with ≥1 `control:` and ≥1 `token:` (resolving), prose describing the page structure and the control families it draws on, and explicit framing as target shape for Workstream D3 kits / G3 showcase (FR-006).

- [X] T019 [P] [US3] Author `docs/product/ant-design/templates/workbench.md` (`status: groundwork`).
- [X] T020 [P] [US3] Author `docs/product/ant-design/templates/list.md` (`status: groundwork`).
- [X] T021 [P] [US3] Author `docs/product/ant-design/templates/detail.md` (`status: groundwork`).
- [X] T022 [P] [US3] Author `docs/product/ant-design/templates/form.md` (`status: groundwork`).
- [X] T023 [P] [US3] Author `docs/product/ant-design/templates/result.md` (`status: groundwork`).
- [X] T024 [P] [US3] Author `docs/product/ant-design/templates/exception.md` (`status: groundwork`).
- [X] T025 [US3] Run `dotnet test tests/Controls.Tests --filter "131"` and confirm `Each_template_recipe_present_once_and_groundwork` and the recipe reference-resolution cases pass (SC-003, SC-002).

**Checkpoint**: All three stories independently functional and honesty-checked.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Optional decision record and the Tier-2 / full-suite proofs.

- [X] T026 [P] (Optional, FR-010 scope decision) Author `docs/product/decisions/0005-ant-design-pattern-docs.md` recording the docs-only Tier-2 scope, the `Catalog.categories` coverage-anchor choice (research R1/R6), and the three-Ant-LLM-files central source-of-truth decision (R8/R9). Reference it from `README.md` via a `doc:` ref if cited.
- [X] T030 [P] Repo-level wiring of the central hub (FR-012, R9): add a short "Ant upstream source of truth" pointer to `docs/product/ant-design/reference/ant-llms-sources.md` from (a) the product docs area (`docs/product/` index/README if present, else a brief note), (b) the coding-agent context file `CLAUDE.md`, and (c) the most relevant existing product skills (`src/Controls/skill/SKILL.md` and the design-system skill if present). Docs/agent-context only — no public surface, no code change (Tier 2). These pointers are a review concern; the machine check covers only the hub + skill/README citation (`Upstream_source_hub_is_central`).
- [X] T027 Run the full `131` filter once more: `dotnet test tests/Controls.Tests --filter "131"` — confirm ALL 13 cases green (SC-001/002/003/004/008/009).
- [X] T028 Prove zero public-surface / token-value delta (V6, SC-005): `dotnet fsi scripts/refresh-surface-baselines.fsx`; `git diff --stat tests/surface-baselines/` (MUST be empty); `dotnet fsi scripts/generate-design-tokens.fsx --check` (MUST report no drift). Verify via `git diff`, not exit code. Precondition: run inside the git work-tree (confirmed present at the repo root). If git is ever unavailable, snapshot `tests/surface-baselines/*.txt` before regenerating and compare by checksum (`sha256sum`) instead of `git diff`.
- [X] T029 Prove the full suite stays green (V7, SC-007): `dotnet test -c Release` → 0 failures across all projects; skip count unchanged from the pre-feature baseline.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS verification of all user stories (the test defines the contract and must be red first).
- **User Stories (Phase 3–5)**: All depend on Phase 2. Once the test exists they can be authored in parallel; each is verified independently against the same check.
- **Polish (Phase 6)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Depends only on Foundational. The MVP — shippable alone.
- **US2 (P2)**: Depends on Foundational; its `doc:` links resolve against US1 pattern docs, so author US1 first (or ensure linked files exist). Independently testable.
- **US3 (P3)**: Depends on Foundational only. Independently testable; no dependency on US1/US2 content.

### Within Each User Story

- The coverage check (Phase 2) is written and FAILS before the docs (Constitution V).
- All pattern docs (T004–T014) are independent files → parallelizable; README (T015) after them so links resolve.
- All recipe docs (T019–T024) are independent files → parallelizable.

### Parallel Opportunities

- T004–T014 (11 pattern docs) can all be authored in parallel.
- T019–T024 (6 recipe docs) can all be authored in parallel.
- After Phase 2, US1 / US2 / US3 can be staffed in parallel (US2 needs US1 doc paths to exist for `doc:` link resolution).
- T026 (decision record) is independent and parallelizable.

---

## Parallel Example: User Story 1

```bash
# Author all 11 pattern docs together (different files, no interdependencies):
Task: "Author docs/product/ant-design/patterns/display.md"
Task: "Author docs/product/ant-design/patterns/input.md"
Task: "Author docs/product/ant-design/patterns/selection.md"
Task: "Author docs/product/ant-design/patterns/layout.md"
Task: "Author docs/product/ant-design/patterns/navigation.md"
Task: "Author docs/product/ant-design/patterns/overlay.md"
Task: "Author docs/product/ant-design/patterns/feedback.md"
Task: "Author docs/product/ant-design/patterns/data.md"
Task: "Author docs/product/ant-design/patterns/chart.md"
Task: "Author docs/product/ant-design/patterns/graph.md"
Task: "Author docs/product/ant-design/patterns/custom.md"
# Then author the README index once the pattern files exist.
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (T001).
2. Phase 2: Foundational — author the coverage check, confirm red (T002–T003).
3. Phase 3: User Story 1 — 11 pattern docs + README, confirm green (T004–T016).
4. **STOP and VALIDATE**: US1 alone satisfies SC-001/002/004 and is shippable as the F6 substance.

### Incremental Delivery

1. Setup + Foundational → red coverage check in place.
2. US1 → pattern docs green → MVP.
3. US2 → advisory skill green.
4. US3 → enterprise recipes green.
5. Polish → decision record + Tier-2 (V6) + full-suite (V7) proofs.

---

## Notes

- [P] = different files, no dependencies.
- Tier 2 / docs-only: no public `.fs`/`.fsi`, no new package/dependency, no token-value or behavior change → surface-drift and design-token-drift gates stay green with no baseline regeneration (FR-009, verified in T028).
- Surface/token neutrality (SC-005) is proven by unchanged baselines (T028), NOT by the `131` test.
- Prose accuracy of the Ant mapping (SC-004/SC-006) is a review concern; the check guarantees structure + reference integrity only.
- Commit after each logical group; stop at any checkpoint to validate a story independently.
