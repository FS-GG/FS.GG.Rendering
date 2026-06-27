# Implementation Plan: Lifecycle Choice Symbol for the fs-gg-ui Template

**Branch**: `204-template-lifecycle-symbol` | **Date**: 2026-06-27 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/204-template-lifecycle-symbol/spec.md`

## Summary

Add a single `lifecycle` choice parameter (`spec-kit` | `sdd` | `none`, default `spec-kit`)
to the `fs-gg-ui` dotnet template (`.template.config/template.json`). The default value
reproduces today's output byte-for-byte; `sdd` and `none` suppress the **gated lifecycle
scaffolding** — the `.specify/` workspace, the project constitution, the agent skill/context
files (`.agents/`, `.claude/`), and the generated agent-context tree
(`.template.config/generated/` → `./`, which carries the constitution, `AGENTS.md`, and
`CLAUDE.md`) — while leaving the generated product (source, project files, product tests,
profile-specific content, and the `designSystem` overlay) untouched.

Technical approach: the template already has a proven precedent for a no-diff-default choice
that gates `source` entries by a condition — the `designSystem` choice (Feature 128) and the
`feedback` bool. This feature reuses that precedent exactly: introduce the choice symbol, add
`lifecycle == "spec-kit"` to the `condition` of every `source` entry that targets a gated
location, and compose it with the existing `profile`/`feedback` conditions. Validation reuses
the Feature 128 report-gate + env-gated live-scaffold pattern (an always-on deterministic
gate backed by an env-gated regenerator that runs real `dotnet new`).

> **Standing assumption — root-cause hypotheses are unverified until the app is run.**
> This is a template-engineering feature, not a runtime defect fix, so there is no "broken app"
> to smoke. The equivalent live proof is **real `dotnet new` instantiation** of each
> `lifecycle` × `profile` combination. `/speckit-tasks` MUST schedule that live scaffold run
> early (Foundational phase) before building US2/US3 work on the assumption that the conditions
> gate the right file set. Deterministic JSON-shape assertions can pass while a real scaffold
> still emits or omits the wrong files.

## Technical Context

**Language/Version**: dotnet template engine (`.template.config/template.json`, JSON schema
`http://json.schemastore.org/template`); validation/tests in F# on .NET `net10.0` (Expecto),
regenerator as `dotnet fsi` script.

**Primary Dependencies**: `dotnet new` template engine; existing repo test support
(`FS.GG.TestSupport`, `RepositoryRoot`); `git` (only for the unrelated post-action, out of scope).

**Storage**: Files only — the template source tree and the generated output tree.

**Testing**: Expecto suite in `tests/Package.Tests/` (always-on deterministic gate over a
self-provisioned, gitignored validation report), plus an env-gated `dotnet fsi` regenerator under `scripts/`
that performs real `dotnet new` instantiation. Mirrors `Feature128DesignSystemTemplateTests.fs`
+ `scripts/validate-design-system-template.fsx`.

**Target Platform**: dotnet SDK host (Linux/macOS/Windows). Live scaffold validation is
GL-free and network-free.

**Project Type**: dotnet project-template engineering (not a runtime library change).

**Performance Goals**: N/A (generation-time only). The default path MUST add zero output diff.

**Constraints**:
- **Byte-identical default** (SC-001): default `lifecycle=spec-kit` produces zero diff vs the
  pre-feature template, for every profile. The condition on each gated `source` must evaluate
  identically true when the default is selected.
- **Zero test churn** (SC-002/FR-009): existing profile/template suites pass with no edits.
- **Fail-fast on unknown values** (FR-006/SC-004): a `choice` datatype with a fixed `choices`
  list rejects out-of-set values natively, exactly as `designSystem` does today.

**Scale/Scope**: One new symbol; condition edits to **17** gated `source` entries: the 4
unconditional gated sources + 8 product-skill entries + 2 sample-pack skill entries
(`profile == "sample-pack"`) + 2 feedback-skill entries + 1 feedback-extensions entry, each
composed with its existing condition (see the gated-source map below for the authoritative list);
one new always-on test module; one new env-gated regenerator; one **gitignored, self-provisioned**
validation report (the `readiness/` report is regenerated, never committed — see "Validation
report" in data-model.md). 3 lifecycle values × 4 profiles = 12 generating combinations to prove.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Change Classification (Tier)** — **Tier 1 (contracted change).** It modifies the
  `fs-gg-ui-template` contract by adding a public option. Requires spec, plan, test evidence,
  and documentation (the option's self-describing descriptions, FR-010). ✅ declared.
- **I. Spec → FSI → Semantic Tests → Implementation** — There is no F# public surface in this
  change; the "interface" is the template's option set declared in `template.json`. The
  analogue of FSI-first is **authoring the choice + descriptions and proving them via real
  `dotnet new`**. Semantic tests assert the *behavior* (gated files present/absent, default
  byte-identical), not JSON internals. ✅ satisfied in spirit; no `.fsi` applies.
- **II. Visibility lives in `.fsi`** — N/A; no `.fs` module added/changed. ✅
- **III. Idiomatic Simplicity** — The change is pure declarative JSON conditions reusing the
  existing precedent; no custom operators, reflection, SRTP, or computation expressions.
  The validator script may reuse the Feature 128 string-builder style. ✅
- **IV. Elmish/MVU boundary** — N/A; no stateful/I-O runtime workflow is added. Template
  generation is the engine's concern, not ours. ✅
- **V. Test Evidence Is Mandatory** — Authored failing-first: the always-on report gate is RED
  until the regenerator produces the report (Feature 128 precedent). Live scaffold proof runs
  real `dotnet new` (real evidence). Any synthetic substitution is disclosed with the
  `Synthetic` token. ✅
- **VI. Observability and Safe Failure** — Unknown `lifecycle` value fails fast via the engine
  (no silent fallback, FR-006). The regenerator fails loudly if a combination does not
  generate. ✅
- **Surface-area baselines** — No public `.fs` module changes, so no `.fsi`/baseline updates.
  The template's *own* contract surface is its option set; coverage is enforced by enumerating
  the `lifecycle` choices from `template.json` in the gate (TP-7 pattern from Feature 128). ✅
- **Repo-owned checks** — Adds one narrow template instantiate/verify check that pays for
  itself by protecting the byte-identical-default and suppression contracts. ✅

**Result: PASS.** No violations; Complexity Tracking not required.

## Project Structure

### Documentation (this feature)

```text
specs/204-template-lifecycle-symbol/
├── plan.md              # This file (/speckit-plan output)
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── lifecycle-symbol.contract.md
├── readiness/           # (gitignored) holds the generated validation report
└── tasks.md             # Phase 2 output (/speckit-tasks — NOT created here)
```

### Source Code (repository root)

```text
.template.config/
├── template.json                 # EDIT: add `lifecycle` symbol; add `lifecycle == "spec-kit"`
│                                 #       to the condition of every gated `source` entry
└── generated/                    # the gated "agent-context tree" (constitution, AGENTS.md, CLAUDE.md)
    └── .specify/memory/constitution.md

template/
├── base/                         # UNGATED product content (source, projects, product tests, docs)
│   ├── CLAUDE.md                 # ⚠ references .specify/skills — dangling-ref edge case under sdd/none
│   └── README.md                 # ⚠ references .specify/skills — dangling-ref edge case under sdd/none
├── product-skills/<skill>/       # GATED (compose lifecycle with existing profile condition)
├── fragments/samples/            # samples/ → UNGATED product content; samples/skill/ → GATED
└── feedback/                     # GATED skill + extensions (compose lifecycle with feedback==true)

scripts/
└── validate-lifecycle-template.fsx   # NEW: env-gated regenerator (real `dotnet new` per combo)

tests/Package.Tests/
└── Feature204LifecycleTemplateTests.fs   # NEW: always-on deterministic report gate
```

**Structure Decision**: Single template project. The only product-code edit is declarative
(`.template.config/template.json`). Validation lives beside the Feature 128 precedent
(`tests/Package.Tests/` for the always-on gate; `scripts/` for the env-gated live regenerator)
so the two design-time validation gates share one shape and one mental model.

## Gated-source map (the precise edit list)

Add `&& lifecycle == "spec-kit"` (or as the sole condition where none exists today) to every
`source` whose `target` lands in the gated set. Ungated product sources are left untouched.

| `source` | `target` | Today's condition | New condition |
|----------|----------|-------------------|----------------|
| `.specify/` | `.specify/` | (none) | `lifecycle == "spec-kit"` |
| `.agents/skills/` | `.agents/skills/` | (none) | `lifecycle == "spec-kit"` |
| `.agents/skills/` | `.claude/skills/` | (none) | `lifecycle == "spec-kit"` |
| `.template.config/generated/` | `./` | (none) | `lifecycle == "spec-kit"` |
| `template/product-skills/*` (8) | `.agents/` & `.claude/` | `profile == …` | `(profile == …) && lifecycle == "spec-kit"` |
| `template/fragments/samples/skill/` (2) | `.agents/` & `.claude/` | `profile == "sample-pack"` | `… && lifecycle == "spec-kit"` |
| `template/feedback/skill/` (2) | `.agents/` & `.claude/` | `feedback == true` | `… && lifecycle == "spec-kit"` |
| `template/feedback/extensions/` | `.specify/extensions/feedback/` | `feedback == true` | `… && lifecycle == "spec-kit"` |
| `template/base/` | `./` | (none) | **UNGATED** (product) |
| `template/fragments/samples/` | `samples/` | `profile == "sample-pack"` | **UNGATED** (product) |
| `template/design-system/ant/` | `./` | `designSystem == "ant"` | **UNGATED** (product) |

## Complexity Tracking

> No Constitution Check violations. This section intentionally left empty.
