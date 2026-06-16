# Contract: Docs honesty/coverage check

The single executable artifact of F6 (FR-008). Lives at `tests/Controls.Tests/Feature131AntPatternDocsTests.fs`, runs headless via `dotnet test --filter "131"`, and is the test evidence (Constitution V) that the docs do not drift from the code.

## Inputs (real, no synthetic fixtures)

- The doc tree under `docs/product/ant-design/` (`patterns/*.md`, `templates/*.md`, `README.md`).
- `.claude/skills/fs-gg-ant-design/SKILL.md`.
- `Catalog.supportedControls` / `Catalog.categories` (from referenced `FS.GG.UI.Controls`).
- Public `FS.GG.UI.DesignSystem` types: `StyleResolver`, `DesignTokensExt.*`, `DesignTokens` (reflection).
- `ColorPolicy.byName` (from `FS.GG.UI.Color`, existing `InternalsVisibleTo Controls.Tests`).
- The central Ant source-of-truth hub `docs/product/ant-design/reference/ant-llms-sources.md` (cataloging the three Ant LLM files + the curated semantic-parts snapshot) is a **referenced source document** for the `part:` vocabulary; the check verifies it exists, lists all three files, and is cited by the skill + index (`Upstream_source_hub_is_central`), but does **not** reflect or resolve `part:`/token mappings against it.

Repo root is located by walking up from the test assembly directory until `docs/product/ant-design/` is found (the established pattern for file-reading tests in this repo).

## Assertions (each a named test case)

| Case | Asserts | Maps to |
|---|---|---|
| `Family_coverage_is_bijective` | the set of `family` front-matter values across `patterns/*.md` equals `Catalog.categories` exactly — no missing, no duplicate, no extra | FR-001, SC-001 |
| `Each_template_recipe_present_once_and_groundwork` | the six template names each appear exactly once across `templates/*.md`, each with `status: groundwork` | FR-006, SC-003 |
| `All_control_refs_resolve` | every `control:` ref in every doc/skill ∈ catalog ids | FR-008, SC-002 |
| `All_resolver_refs_resolve` | every `resolver:` ref is a public `StyleResolver` member | FR-003, FR-008, SC-002 |
| `All_token_refs_resolve` | every `token:` ref resolves on a public DesignSystem token type | FR-003, FR-008, SC-002 |
| `All_policy_refs_resolve` | every `policy:` ref is accepted by `ColorPolicy.byName` | FR-003, FR-008, SC-002 |
| `All_doc_links_resolve` | every `doc:` ref points to an existing file | FR-008, SC-002 |
| `Pattern_docs_have_required_refs` | each pattern doc has ≥1 control/token/resolver + applicable policy ref | FR-002, FR-003 |
| `Pattern_docs_declare_semantic_parts` | each pattern doc has ≥1 well-shaped `part:<Component>/<part>` ref (non-empty component, non-empty part, exactly one `/`) and the companion `control:`/`token:`/`resolver:` refs; shape only, no code resolution of `part:` | FR-011, SC-008 |
| `Pattern_docs_state_design_language_only` | each pattern doc contains the no-React/DOM assertion line | FR-007, SC-004 |
| `Skill_is_advisory_and_reminds_layering` | `SKILL.md` front-matter parses with `name`/`description`; body contains the no-React/DOM and no-per-theme-fork statements; no gate keyword introduced | FR-004, FR-005, FR-007 |
| `Upstream_source_hub_is_central` | the hub `docs/product/ant-design/reference/ant-llms-sources.md` exists; its text names all three Ant LLM files (`llms.txt`, `llms-full.txt`, `llms-semantic.md`); the skill and `README.md` each carry a `doc:` ref that resolves to the hub | FR-012, SC-009 |
| `No_unknown_ref_prefixes` | every line in every `refs` block uses an allowed prefix (`control`/`token`/`resolver`/`policy`/`doc`/`part`) and `prefix:value` shape | grammar contract |

Failure messages MUST name the offending file and the specific missing family/template or dangling reference (Constitution VI — loud, actionable failure).

## Out of scope for this check

- **Surface/token neutrality (SC-005)** is proven separately by the unchanged `tests/surface-baselines/*.txt` and the design-token-drift gate — this test asserts nothing about baselines.
- **Prose quality / accuracy of the Ant mapping** is a review concern (SC-004, SC-006), not automatable here. The check guarantees *structure and reference integrity*, not editorial correctness.

## Determinism & environment

Pure file-read + reflection. No GL, display, network, or randomness. Byte-identical result on every run and every host. No new dependency; no `.fsi`; no baseline regeneration.
