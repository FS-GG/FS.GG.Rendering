# Implementation Plan: Ant interaction-pattern docs + `fs-gg-ant-design` agent skill

**Branch**: `131-ant-pattern-docs-skill` | **Date**: 2026-06-16 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/131-ant-pattern-docs-skill/spec.md`

## Summary

F6 is the **knowledge layer** over the already-shipped Ant adoption machinery (F1–F5). It adds, **with no compiled product code**: (1) one Ant interaction-pattern reference doc per catalog control family, mapping each Ant pattern onto existing controls + the public token taxonomy (F1/126) + the central style resolver (F4/129) + the `wcag`/`ant` color policy (F2/127); (2) a single advisory `fs-gg-ant-design` `SKILL.md`; and (3) six enterprise page-template recipe docs as forward-looking groundwork for D3/G3.

The only executable artifact is a **docs honesty/coverage check** (one F# test in `Controls.Tests`) that keeps the docs from drifting away from the code: it enforces per-family and per-template completeness and verifies that every machine-checked reference (control id / token / resolver member / policy / internal doc link) resolves against the real source of truth. This is the same discipline the Controls Gallery (123) uses for its catalog-coverage check.

F6 also adopts the **three Ant LLM files as the central, repo-wide source of truth** (FR-012, R8/R9) — `llms.txt` (index), `llms-full.txt` (full API/usage + component tokens), `llms-semantic.md` (semantic parts) — catalogued in one in-repo hub `docs/product/ant-design/reference/ant-llms-sources.md` that every pattern doc, the skill, the index, the product docs area, `CLAUDE.md`, and key existing skills cite. From that source set F6 adopts Ant's **semantic-parts model** (FR-011): each pattern doc enumerates the Ant component's named regions via `part:<Component>/<partName>` refs and maps each to a repo control region + token + resolver state (tokens-as-materials / semantic-styles-as-application), while the React `classNames`/DOM realization is recorded as explicitly not-adopted (FR-010). The hub embeds a curated snapshot of the relevant component slots so neither the docs nor the check depend on the network.

**Technical approach**: docs are plain Markdown under `docs/product/ant-design/`; each pattern/recipe doc carries a small typed front-matter (`family`/`template`) and a `## Machine-checked references` section with a fenced ` ```refs ` block listing typed references. The test parses those blocks and resolves the *code* refs (`control`/`token`/`resolver`/`policy`/`doc`) by reflection over the already-referenced public assemblies (`Catalog.supportedControls`/`Catalog.categories`, `StyleResolver`, `DesignTokensExt`/`DesignTokens`) plus `ColorPolicy.byName` (reached via the existing `Color` IVT); the new `part:` refs are validated by **shape only** (non-empty `<Component>` / `<partName>`, one `/`) — they declare upstream Ant vocabulary, not repo symbols. **Tier 2 / docs-only**: no public `.fs`/`.fsi`, no new package, no new dependency, no token-value or behavior change ⇒ surface-drift and design-token-drift gates stay green with no baseline regeneration.

## Technical Context

**Language/Version**: F# on .NET `net10.0` (only for the single coverage-check test). Documentation is GitHub-flavored Markdown.

**Primary Dependencies**: None new. The test uses only what `Controls.Tests` already references — `FS.GG.UI.Controls` (`Catalog`), `FS.GG.UI.DesignSystem` (public `StyleResolver`, `DesignTokensExt`, `DesignTokens`), and `FS.GG.UI.Color` (`ColorPolicy.byName`, via existing `InternalsVisibleTo`). System.Reflection (BCL) resolves token/resolver members. No JSON/Markdown parser dependency is added (the `refs` block is line-parsed).

**Storage**: Files only — Markdown docs under `docs/product/ant-design/`, the skill under `.claude/skills/fs-gg-ant-design/`, an optional decision record under `docs/product/decisions/`.

**Testing**: Expecto (the repo's existing test framework — `[<Tests>] testList`, run via `dotnet test tests/Controls.Tests --filter "131"`) in `tests/Controls.Tests/Feature131AntPatternDocsTests.fs`. Repo root is located by walking up to the `FS.GG.Rendering.slnx` marker (the established pattern, e.g. `Feature126`/`Feature127` tests). Tests read real files from the repo tree and reflect over the real public assemblies — no synthetic fixtures.

**Target Platform**: Headless / deterministic. No GL, window-system, display, or network dependency. Runs anywhere `dotnet test` runs.

**Project Type**: Documentation + one advisory agent skill, guarded by a single conformance test. No application, service, or library surface.

**Performance Goals**: N/A (a file-scan + reflection check completing well under a second; not a hot path).

**Constraints**: Zero public-API surface delta (FR-009/SC-005); both drift gates remain green with no baseline regeneration; the skill stays advisory (FR-005); Ant adopted as design language only — no React/DOM/HTML/CSS dependency or implementation requirement (FR-002/FR-007/SC-004).

**Scale/Scope**: 11 pattern docs (one per `Catalog.categories` value — the lowercase set `display, input, selection, layout, navigation, overlay, feedback, data, chart, graph, custom`; see research R1), each declaring its Ant semantic parts via `part:` refs (FR-011/R8), 6 enterprise-template recipes, 1 index doc, 1 central Ant source-of-truth hub (`reference/ant-llms-sources.md`, cataloging the 3 LLM files + curated snapshot), 1 `SKILL.md`, 1 optional decision record, repo-level hub pointers (`docs/product/` index, `CLAUDE.md`, key existing skills), 1 test file. No change to the 52-control catalog, the `Theme`, or any token value.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Verdict | Notes |
|---|---|---|
| **I. Spec → FSI → Semantic Tests → Implementation** | **PASS (with documented narrowing)** | No public `.fs`/`.fsi` is added, so the FSI-sketch step is not applicable. The order is preserved in spirit: spec (done) → the coverage/honesty **test** is authored against the doc contract → docs + skill are written to make it pass. Deviation recorded in research R6: docs-only Tier 2 features have no FSI surface to sketch. |
| **II. Visibility lives in `.fsi`** | **PASS** | No new public F# module ⇒ no `.fsi` required. The test file is non-public test code. No `.fs` access modifiers introduced. |
| **III. Idiomatic simplicity** | **PASS** | The check is plain F#: read files, line-parse the `refs` block, reflect over public types, assert. No SRTP/reflection-tricks beyond standard `Type.GetMember`, no custom operators, no computation expressions. |
| **IV. Elmish/MVU boundary** | **PASS (N/A)** | No stateful workflow or I/O orchestration; the check is a pure read-and-assert. No `Model`/`Msg`/`Cmd` needed. |
| **V. Test evidence is mandatory** | **PASS** | The honesty/coverage check fails before the docs/skill exist and passes after — real files, real assemblies, no synthetic evidence. Forward-looking recipe docs are asserted to be *marked* forward-looking, not asserted as shipped behavior. |
| **VI. Observability & safe failure** | **PASS** | The check fails loudly, naming the missing family/template or the specific dangling reference, so docs cannot silently drift from code. |
| **Change Classification** | **Tier 2** | No public API, no behavior change. `.fsi` and surface/token baselines untouched (FR-009). A Tier-1 chain is not required. |
| **Engineering constraints** | **PASS** | No new dependency or package; layering rule reinforced in docs (FR-007); skill advisory per Local Skills (FR-005); no React/DOM. Ant adopted as *design language / concept only* via a central in-repo source-of-truth hub (`reference/ant-llms-sources.md` cataloging the 3 LLM files, R8/R9) — no network dependency, no public surface, `part:` refs validated shape-only; repo-level hub pointers (`CLAUDE.md`, key skills) are docs/agent-context only. Package identity unchanged. |

**Result**: No violations. Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/131-ant-pattern-docs-skill/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output (R1–R7)
├── data-model.md        # Phase 1 output (entities + reference grammar)
├── quickstart.md        # Phase 1 output (V1–V7 validation guide)
├── contracts/
│   ├── doc-reference-grammar.md     # front-matter + `refs` block contract
│   └── docs-coverage-check.md       # the honesty/coverage check contract
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source / deliverable layout (repository root)

```text
docs/product/ant-design/
├── README.md                        # index: the three pillars, layering rule, links
├── reference/
│   └── ant-llms-sources.md          # CENTRAL Ant source-of-truth hub: catalogs llms.txt/llms-full.txt/llms-semantic.md (R8/R9) + curated semantic-parts snapshot; referenced source, not machine-resolved
├── patterns/
│   ├── display.md                   # one per Catalog.categories value (research R1)
│   ├── input.md
│   ├── selection.md
│   ├── layout.md
│   ├── navigation.md
│   ├── overlay.md
│   ├── feedback.md
│   ├── data.md
│   ├── chart.md
│   ├── graph.md
│   └── custom.md
└── templates/
    ├── workbench.md                 # the six enterprise page-template recipes
    ├── list.md
    ├── detail.md
    ├── form.md
    ├── result.md
    └── exception.md

.claude/skills/fs-gg-ant-design/
└── SKILL.md                         # single advisory skill (Local Skills model)

docs/product/decisions/
└── 0005-ant-design-pattern-docs.md  # records docs-only scope + coverage-anchor choice

tests/Controls.Tests/
└── Feature131AntPatternDocsTests.fs # the docs honesty/coverage check (FR-008)
```

**Structure Decision**: Docs live under the existing `docs/product/` tree (beside `decisions/`, `layering.md`, `module-map.md`) in a new `ant-design/` area, because F6 is product/design-language documentation, not package-owned API docs. The skill lives under `.claude/skills/` per the constitution's Local Skills section (advisory, repo-local). The coverage check lives in `tests/Controls.Tests` because that is the single existing test project that already references `Controls` (→ `Catalog`), `DesignSystem` (→ public resolver/tokens), and `Color` (→ `ColorPolicy` via IVT) — so it can resolve every reference type with **no new project reference**.

## Complexity Tracking

> No constitution violations. Section intentionally empty.
