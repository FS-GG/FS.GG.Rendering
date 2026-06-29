# Implementation Plan: fs-gg-ui `productName` Scaffold Symbol

**Branch**: `217-template-productname-symbol` | **Date**: 2026-06-29 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/217-template-productname-symbol/spec.md`

## Summary

The `fs-gg-ui` (`FS.GG.UI.Template`) project template names the generated product via the built-in `-n`/`--name` flag, driven by `"sourceName": "Product"`. The SDD scaffold-provider convention instead passes `--productName`, so `dotnet new fs-gg-ui … --productName Acme` is rejected as an invalid option (exit 127) and the rendering provider cannot be composed from SDD.

Technical approach: make `productName` an **additive** template parameter that drives the same name substitution `sourceName` performs today, with a single unambiguous rename driver and `productName`-over-`name` precedence — without perturbing the `-n`/default/existing-flag paths (which must stay byte-identical). The change is validated by a real `dotnet new` + byte-diff + `dotnet build` validator behind an env flag, plus an always-on, deterministic verdict-core gate that re-derives the contract fact from `template.json`. The `scaffold-provider` contract change is recorded in the org-level cross-repo registry.

> **Standing assumption — the wiring mechanism is a hypothesis until instantiated.**
> The mechanism chosen in `research.md` (remove `sourceName`; drive renames from a `productName ?? name`
> coalesce symbol) is the documented dotnet-templating pattern, but "byte-identical to today on the
> `-n`/default paths" is an empirical claim. `/speckit-tasks` MUST schedule an **early live instantiation +
> byte-diff** (real `dotnet new` against a pre-change baseline) in the Foundational phase, before the
> contract/test work is built on top, to confirm or replace this hypothesis.

## Technical Context

**Language/Version**: F# on .NET `net10.0`. The changed artifact is `.template.config/template.json` (Microsoft dotnet templating engine config, JSON). Validator + gate are F# (`dotnet fsi` script + Expecto test).

**Primary Dependencies**: dotnet templating engine (`dotnet new`); Expecto + `FS.GG.TestSupport` (`tests/Package.Tests`). External composition toolchain: `fsgg-sdd` ≥ 0.2.0 and `FS.GG.UI.Template` from the org NuGet feed.

**Storage**: N/A.

**Testing**: `tests/Package.Tests` Expecto suite — a new always-on env-free verdict-core gate (`Feature217ProductNameTemplateTests.fs`) re-derives the contract from `template.json`; the heavy live work (real `dotnet new` × name-path matrix + byte-diff + `dotnet build`) runs behind an env flag in a new `scripts/validate-productname-template.fsx`, mirroring `validate-lifecycle-template.fsx` / `validate-design-system-template.fsx`.

**Target Platform**: Cross-platform dotnet CLI (Linux/Windows/macOS); template instantiation time only.

**Project Type**: dotnet project-template package (`FS.GG.UI.Template`) owned by the rendering repo.

**Performance Goals**: N/A — template-instantiation-time concern, not a render hot path.

**Constraints**: Additive and backward-compatible only. Output for every existing path (`-n`, default no-name, all existing flag combinations) MUST be byte-identical to today when `productName` is not supplied (FR-004 / SC-003). Empty/whitespace `productName` ⇒ treated as not supplied (FR-006). `productName` over `-n` precedence (FR-005).

**Scale/Scope**: One `template.json` symbol set + one slug-generator source repoint; one validator script; one Package.Tests module; one gitignored readiness report; one org-level cross-repo contract record.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

- **Change Classification — Tier 1 (contracted change).** Modifies the public template parameter contract (`scaffold-provider`, rendering side). Full artifact chain required: spec ✓, plan ✓, contract surface (`contracts/`) ✓, test evidence (live validator + always-on gate) ✓, compatibility documentation (additive; cross-repo record, FR-008) ✓.
- **Principle I (Spec → FSI → Semantic Tests → Implementation).** No F# public surface is added, so there is no `.fsi` to sketch; the analogue here is *template-contract → instantiation tests → wiring*. Authored failing-first: the verdict-core gate + live validator are written to fail before the `template.json` wiring lands (Principle V).
- **Principle II (visibility in `.fsi`) / surface-area baselines.** Not engaged — this feature adds no public F# module surface. The validator/test live in existing test/script assemblies and expose no new public `.fs` API, so no `.fsi` or surface-area baseline updates are required. (If a shared helper grows a public surface during implementation, it gets a curated `.fsi` then.)
- **Principle III (idiomatic simplicity).** Prefer the least-invasive `template.json` change that yields a *single* rename driver; no custom operators / SRTP / reflection. JSON wiring stays plain.
- **Principle V (test evidence is mandatory, prefer real).** The env-gated validator uses **real** `dotnet new` + **real** `dotnet build` (no synthetic substitutes) and byte-diffs against a real pre-change baseline. The always-on gate is deterministic (re-derives from `template.json`, no network/GL). No synthetic evidence anticipated.
- **Principle VI (observability / safe failure).** Validator emits an explicit pass/fail report and fails loudly on any byte-diff regression or build error; no swallowed diffs.
- **Development Workflow kept-check — "template pack/install/instantiate checks".** This feature *is* that check, extended for the new option.

**Gate verdict: PASS — no violations.** Complexity Tracking left empty.

## Project Structure

### Documentation (this feature)

```text
specs/217-template-productname-symbol/
├── plan.md              # This file (/speckit-plan)
├── research.md          # Phase 0 — mechanism decision + risks
├── data-model.md        # Phase 1 — template symbols & rename tokens
├── quickstart.md        # Phase 1 — runnable validation guide
├── contracts/
│   └── productname-scaffold-provider.md   # Phase 1 — template parameter contract
├── checklists/
│   └── requirements.md  # spec quality checklist (already present)
└── tasks.md             # Phase 2 — /speckit-tasks (NOT created here)
```

### Source Code (repository root)

```text
.template.config/
└── template.json                        # MODIFY: add `productName` parameter + coalesce-driven rename;
                                          #         repoint projectSlug generator source to the effective name

scripts/
└── validate-productname-template.fsx    # NEW: env-gated live validator (dotnet new × name-path matrix,
                                          #      byte-diff vs baseline, dotnet build) + report writer;
                                          #      models scripts/validate-lifecycle-template.fsx

tests/Package.Tests/
└── Feature217ProductNameTemplateTests.fs # NEW: always-on env-free verdict-core gate + report self-provision/assert
                                          #      (registered in Package.Tests.fsproj)

specs/217-template-productname-symbol/readiness/
└── productname-template-validation.md   # gitignored, self-provisioned report asserted by the gate
```

**Structure Decision**: Single template-package change in the existing rendering repo. The validation surface reuses the repo's proven *report-gate + env-gated-live-run* pattern (`tests/Package.Tests` + `scripts/validate-*.fsx` + gitignored `specs/<feature>/readiness/`), so no new project or structure is introduced. The cross-repo contract record (FR-008) lives **out of this repo** in the org-level coordination registry (`FS-GG/.github`), reached via the cross-repo-coordination protocol, with cross-references on `FS-GG/FS.GG.Rendering#27` and `FS-GG/FS.GG.SDD#35`.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
