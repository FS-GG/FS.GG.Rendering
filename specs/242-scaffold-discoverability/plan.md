# Implementation Plan: Scaffold discoverability sharpening

**Branch**: `242-scaffold-discoverability` | **Date**: 2026-07-04 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/242-scaffold-discoverability/spec.md`. Roadmap item FS-GG/FS.GG.Rendering#75 (epic FS-GG/.github#165), Space Invaders consumer feedback §2.2/§2.3.

## Summary

Two additive discoverability edges on the `fs-gg-ui` generated-product template, with **no** change to the scaffold seam model, the durable governance spine, or versioned cross-repo contracts:

1. **SWAP-CHECKLIST.md** — ship a root-level, profile-correct model-swap to-do list that names the exact symbols in the durable must-re-point files (`LayoutEvidence.fs`, `EvidenceCommands.fs`) plus the rewrite-wholesale files, so a swap is not reverse-engineered from compiler errors. Emitted via the proven exclude-and-re-emit `sources[]` pattern (three model families; unproven markdown `#if` avoided).
2. **Build-target `--help` banner** — surface the load-bearing `Dev`/`Test`/`Verify` semantics at the build entry (`build.fsx` + `build.sh`), side-effect-free, kept in sync with `docs/product.md` by a governance assertion.

> **Standing assumption — verify by running the real instantiation.** The design rests on template-engine behavior (per-profile source emission, `copyOnly`, `build.fsx` arg routing). `/speckit-tasks` MUST schedule an **early live check** in the Foundational phase — instantiate the template for at least the `game` and `app` profiles and observe the emitted `SWAP-CHECKLIST.md` + `dotnet fsi build.fsx --help` output — before building the per-family content and tests on top. Deterministic text-scan tests can pass while the real `dotnet new` emission differs; confirm the mechanism first.

## Technical Context

**Language/Version**: F# on .NET (per `global.json`); F# script (`build.fsx`), bash (`build.sh`), Markdown docs, JSON template config.

**Primary Dependencies**: dotnet `new` template engine (`.template.config/template.json`); Expecto (test framework); FAKE-style `build.fsx` entry. No new packages.

**Storage**: N/A (files only).

**Testing**: Expecto — `template/base/tests/Product.Tests/GovernanceTests.fs` (durable, in-product), and a new deterministic gate under `tests/Package.Tests/` (template-authoring correctness). Live per-profile instantiation via `scripts/validate-*-template.fsx`-style `dotnet new` runs.

**Target Platform**: developer workstation / CI (Linux + Windows parity for `build.sh`/`build.cmd`).

**Project Type**: generated-product template (framework repo) — single project tree; changes are template content + config + tests.

**Performance Goals**: N/A (docs + a help branch).

**Constraints**: Additive only (FR-010). No versioned-contract change; no six-scanned-file reorder; must-survive evidence tokens untouched; markdown `#if` preprocessing avoided (unproven). Internal `FeatureNNN` test names must avoid the existing `Feature242` collision.

**Scale/Scope**: 5 profiles → 3 model families for the checklist; 1 shared banner; ~3 fragment files, ~3 `template.json` sources, 2 file edits (`build.fsx`, `build.sh`), 2 test surfaces.

## Constitution Check

*GATE: must pass before Phase 0 research. Re-checked after Phase 1 design.*

- **I. Spec → FSI → Semantic Tests → Implementation** — PASS. Spec + this plan precede code; tests specified before implementation (`data-model.md` validation rules, `quickstart.md`). No new library API, so no `.fsi` surface is introduced.
- **II. Visibility lives in `.fsi`** — N/A. No packaged public surface changes; `build.fsx` is a script and `SWAP-CHECKLIST.md` is content.
- **III. Idiomatic simplicity is the default** — PASS. Reuses the existing exclude-and-re-emit `sources[]` idiom rather than inventing markdown conditional processing; the banner is a plain early-return in the existing arg router.
- **IV. Elmish/MVU boundary** — N/A. No stateful/IO workflow added.
- **V. Test evidence is mandatory** — PASS. Two test surfaces (generated-product presence + template-authoring correctness) plus the live instantiation check. Honesty caveat recorded: "zero omissions" is proven only as *no-phantom + known-reader coverage*, not a full F#-parse proof — stated in the test header (Principle V, honest disclosure).
- **VI. Observability and safe failure** — PASS. The help path fails safe (exits 0, no side effects); the sync gate fails closed on drift.

**No violations → Complexity Tracking omitted.**

## Project Structure

### Documentation (this feature)

```text
specs/242-scaffold-discoverability/
├── plan.md              # this file
├── spec.md
├── research.md          # Phase 0 — mechanics + decisions
├── data-model.md        # Phase 1 — per-profile symbol inventory + banner content
├── quickstart.md        # Phase 1 — runnable validation
├── contracts/
│   └── discoverability-contracts.md
└── checklists/
    └── requirements.md
```

### Source Code (repository root)

```text
.template.config/
└── template.json                         # + 3 profile-gated sources re-emitting SWAP-CHECKLIST.md (skillist-reference precedent)

template/
├── fragments/
│   └── swap-checklist/                    # NEW authored per-family content (copyOnly)
│       ├── game/SWAP-CHECKLIST.md
│       ├── app/SWAP-CHECKLIST.md          # app + sample-pack
│       └── governed/SWAP-CHECKLIST.md     # governed + headless-scene
└── base/
    ├── build.fsx                          # + --help/-h/help early-return banner (before target dispatch)
    ├── build.sh                           # + --help/-h verb; print_usage → semantics banner
    ├── docs/product.md                    # (unchanged prose home; asserted in sync with the banner)
    └── tests/Product.Tests/
        └── GovernanceTests.fs             # + SWAP-CHECKLIST presence test (both profile branches) + banner↔product.md sync assertion

tests/
└── Package.Tests/
    └── SwapChecklistTemplateTests.fs      # NEW deterministic gate: no-phantom + reader coverage; banner phrase presence
```

**Structure Decision**: Framework-repo template edit. Per-family checklist content lives under `template/fragments/swap-checklist/` (mirroring `template/fragments/samples/`), emitted by mutually-exclusive profile-gated sources — never inline markdown conditionals. The banner is unconditional base content. Tests split into durable in-product presence (`GovernanceTests.fs`) and template-authoring correctness (`tests/Package.Tests/`).

## Complexity Tracking

No constitution violations; section intentionally empty.
