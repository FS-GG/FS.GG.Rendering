---
description: "Task list for Lifecycle Choice Symbol for the fs-gg-ui Template"
---

# Tasks: Lifecycle Choice Symbol for the fs-gg-ui Template

**Input**: Design documents from `/specs/204-template-lifecycle-symbol/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/lifecycle-symbol.contract.md, quickstart.md

**Tests**: This feature's test evidence IS its deliverable. Per plan.md Decision 4 (Constitution
Principle V), the always-on deterministic report gate (`Feature204LifecycleTemplateTests.fs`) and
the env-gated live regenerator (`scripts/validate-lifecycle-template.fsx`) are first-class
implementation tasks, not optional extras. There is no separate "TDD vs. not" choice here — the
report gate is authored RED-first and the live regenerator provides the real `dotnet new` evidence.

**Organization**: Tasks are grouped by user story (P1 → P2 → P3) to enable independent
implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task

## Path Conventions

- Template engine source: `.template.config/template.json` (the single product-code edit)
- Gated agent-context tree: `.template.config/generated/`
- Ungated product docs: `template/base/CLAUDE.md`, `template/base/README.md`
- Live regenerator: `scripts/validate-lifecycle-template.fsx`
- Always-on gate: `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`
- Validation report: `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` — **gitignored** (`specs/*/readiness/`), regenerated and **never committed**; the always-on gate self-provisions it via an env-free `--emit-report` path before asserting (Feature 128 precedent)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the no-regression baseline and capture the pre-feature reference output the
byte-identical proof depends on.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project and record the full red/green set, so pre-existing failures are known up front and not
> mistaken for regressions at merge. Do NOT hand-pick a subset: the solution
> (`dotnet test FS.GG.Rendering.slnx`) deliberately omits `tests/Package.Tests` (release-only — owns
> the template/public-surface gate, exactly where this feature's new test lands) and the
> `samples/**/*.Tests` projects. Use the discovery-based runner so nothing silently drops out.

- [X] T001 Establish the no-regression baseline: run `dotnet fsi scripts/baseline-tests.fsx --out specs/204-template-lifecycle-symbol/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**` — and records the full red/green set so pre-existing reds are flagged here, not discovered at merge)
- [X] T002 Capture the pre-feature reference tree for the SC-001 byte-identical proof: from a clean tree (pre-edit `git` HEAD of `.template.config/template.json`), install the template and `dotnet new fs-gg-ui` each profile (`app`, `headless-scene`, `governed`, `sample-pack`) into a stash/reference dir, recording the command + output locations in `specs/204-template-lifecycle-symbol/readiness/reference-trees.md` for the regenerator to diff against (research Decision 5)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Land the single declarative `template.json` edit and prove it with a real `dotnet new`
run BEFORE building US2/US3 suppression assumptions on top of it. Per plan.md, the conditions gate
the right file set is an **unverified hypothesis until a real scaffold is run** — deterministic
JSON-shape assertions can pass while a real scaffold emits or omits the wrong files.

**⚠️ CRITICAL**: No user-story validation work can begin until the symbol + conditions exist and have
been proven to generate by a real `dotnet new` for at least one `lifecycle` × `profile` combination.

> **⚠️ Early live scaffold run (STANDING, do not omit).** The template-engineering analogue of the
> "drive the real app" smoke run is **real `dotnet new` instantiation**. T005 below pulls this
> forward into the Foundational phase exactly as plan.md mandates: prove the symbol parses, the
> default still generates, and a non-default value actually suppresses files on disk — BEFORE
> US2/US3 build on the assumption that the conditions are correct.

- [X] T003 Add the `lifecycle` choice symbol to `.template.config/template.json`: `{"type":"parameter","datatype":"choice","defaultValue":"spec-kit"}` with exactly three `choices` (`spec-kit`, `sdd`, `none`), a self-describing symbol `description`, and a per-choice `description` for each value stating what it emits/suppresses (FR-001/FR-010/SC-005), mirroring the existing `designSystem` symbol shape (research Decision 1)
- [X] T004 Gate every lifecycle `source` entry in `.template.config/template.json` per the plan's gated-source map: add `lifecycle == "spec-kit"` to the 4 unconditional gated sources (`.specify/`→`.specify/`, `.agents/skills/`→`.agents/skills/`, `.agents/skills/`→`.claude/skills/`, `.template.config/generated/`→`./`) and compose `&& lifecycle == "spec-kit"` onto the 8 product-skill entries, the 2 sample-pack skill entries (`profile == "sample-pack"`), and the 2 feedback-skill + 1 feedback-extensions entries (`feedback == true`); leave `template/base/`, `template/fragments/samples/`→`samples/`, and the `designSystem=ant` overlay UNGATED (FR-003/FR-004/research Decision 2 & 3)
- [X] T005 **Early live scaffold run**: `dotnet new install` the working-tree template, then `dotnet new fs-gg-ui --profile app` (default) and `dotnet new fs-gg-ui --profile app --lifecycle sdd`, confirming on disk that the default still generates and `sdd` suppresses `.specify/`, `.claude/`, `.agents/`, `AGENTS.md`, generated `CLAUDE.md` while keeping the product — record this live evidence in `specs/204-template-lifecycle-symbol/readiness/early-scaffold.md` BEFORE building US2/US3 on the gating hypothesis (plan standing assumption; quickstart Scenarios 1–2)

**Checkpoint**: The symbol parses, the default path still generates, and a real non-default scaffold
demonstrably suppresses gated files. The gating hypothesis is now verified — user-story validation
can begin.

---

## Phase 3: User Story 1 - Default callers see no change (Priority: P1) 🎯 MVP

**Goal**: Prove that scaffolding any profile with the default `lifecycle` (or explicit `spec-kit`) is
byte-identical to the pre-feature output, and that all existing suites pass unmodified — the
non-regression gate the whole feature depends on.

**Independent Test**: Scaffold each profile without a lifecycle value and confirm the tree is
byte-identical to the Phase-1 reference (SC-001), and that the existing profile/template suites pass
with zero edits (SC-002).

- [X] T006 [US1] Author the always-on deterministic report gate `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` RED-first: at module init it **self-provisions** the gitignored report via an env-free `--emit-report` verdict-core path (no `dotnet new`/build/GL/network), then asserts it — `covered-values` equals the `lifecycle` choices enumerated from `.template.config/template.json` (TP-7 coverage), the gated `source` entries in `template.json` carry the `lifecycle == "spec-kit"` condition (the env-free verdict-core fact), `spec-kit: diff-vs-today=none` per profile, a `provenance: verdict-core (env-free; full live proof gated behind FS_GG_RUN_LIFECYCLE_VALIDATION=1)` disclosure (Constitution V — the diff/suppression lines are synthesized, not observed, until the live run), and `result: pass`; mirror `Feature128DesignSystemTemplateTests.fs`'s self-provisioning pattern (research Decision 4; data-model "Validation report")
- [X] T007 [US1] Register the new module in `tests/Package.Tests/Package.Tests.fsproj` with a `<Compile Include="Feature204LifecycleTemplateTests.fs" />` entry placed before `Tests.fs`/`Program.fs` (alongside `Feature128DesignSystemTemplateTests.fs`)
- [X] T008 [US1] Create the env-gated live regenerator `scripts/validate-lifecycle-template.fsx` modeled on `scripts/validate-design-system-template.fsx`, with **two paths**: (a) an env-free `--emit-report` verdict-core path the gate uses to self-provision the gitignored report — it computes the real env-free facts (covered-values from `template.json`; the gated `source` entries carry `lifecycle == "spec-kit"`), synthesizes the live-only verdict lines (`diff-vs-today=none`, `gated-absent`, `diff-vs-default=gated-only`) as expected values, and writes a `provenance: verdict-core (env-free; full live proof gated behind FS_GG_RUN_LIFECYCLE_VALIDATION=1)` disclosure (Constitution V) — no `dotnet new`/build/GL/network; and (b) the live path behind `FS_GG_RUN_LIFECYCLE_VALIDATION=1` that scaffolds each profile with no `--lifecycle` and with `--lifecycle spec-kit`, diffs both against the Phase-1 pre-feature reference, and overwrites the report at `specs/204-template-lifecycle-symbol/readiness/lifecycle-template-validation.md` with real `spec-kit: diff-vs-today=none` per profile and `provenance: live` (FR-002/SC-001; research Decision 5)
- [X] T009 [US1] Run the live regenerator (`FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx`) to write the (gitignored) validation report from real `dotnet new` evidence, then confirm `dotnet test tests/Package.Tests --filter Feature204` turns the T006 gate GREEN on the US1 assertions
- [X] T010 [US1] Confirm SC-002 zero-churn: re-run the existing profile/template suites (the Phase-1 baseline set) and verify they pass with **no** test edits; record the green result in `specs/204-template-lifecycle-symbol/readiness/no-churn.md`

**Checkpoint**: Default output is provably byte-identical and existing suites are green untouched —
MVP non-regression gate is airtight. US2/US3 can now be safely validated.

---

## Phase 4: User Story 2 - Compose a product under an external lifecycle owner (Priority: P2)

**Goal**: Prove `--lifecycle sdd` emits the full generated product (present and buildable) while
suppressing the entire gated set, with no dangling references to suppressed paths.

**Independent Test**: Scaffold a profile with `sdd` and confirm the product is present/buildable while
`.specify/`, the constitution, `.agents/`/`.claude/`, and the generated agent-context tree are absent
(quickstart Scenario 2).

- [X] T011 [US2] Resolve the CC-1 dangling-reference edge case (research CC-1): make `template/base/CLAUDE.md` and `template/base/README.md` lifecycle-aware so that under `sdd`/`none` no emitted file references a suppressed `.specify/`/skills path — either move the lifecycle-referencing prose into the gated generated-tree copy and keep a neutral ungated base copy, or gate the base copies with a neutral fallback (spec "Suppressed-but-referenced" edge case)
- [X] T012 [US2] Extend `scripts/validate-lifecycle-template.fsx`: for each profile under `--lifecycle sdd`, assert the gated set is absent and the product (source, project files, product tests) is present and that `dotnet build` succeeds; **additionally assert that `diff -r` of the default tree vs. the `sdd` tree differs in *only* gated paths** (the set of removed entries equals exactly the gated set — proves FR-009's "suppresses exactly the gated set and nothing else"), emitting `sdd: gated-absent=ok product-present=ok diff-vs-default=gated-only` to the report (FR-004/FR-005/FR-009/SC-003; quickstart Scenario 2)
- [X] T013 [US2] Add the dangling-reference assertion to `scripts/validate-lifecycle-template.fsx`: grep the emitted `sdd` tree for references to suppressed paths (e.g. `.specify/`) and emit `dangling-refs: none` to the report, proving T011's fix (research CC-1)
- [X] T014 [US2] Add the matching assertions to `tests/Package.Tests/Feature204LifecycleTemplateTests.fs` (`sdd: gated-absent=ok product-present=ok diff-vs-default=gated-only`, `dangling-refs: none`), regenerate the report, and confirm `--filter Feature204` is GREEN for the US2 lines

**Checkpoint**: `sdd` suppresses exactly the gated set, the product still builds, and no dangling
references remain — the downstream P2 SDD composition epic is unblocked.

---

## Phase 5: User Story 3 - Scaffold a bare product with no lifecycle (Priority: P3)

**Goal**: Prove `--lifecycle none` emits the generated product alone with the gated set suppressed,
producing the identical template-level file set as `sdd`.

**Independent Test**: Scaffold a profile with `none`; confirm only the product is produced and that
`diff -r` of the `none` tree against the `sdd` tree is empty (quickstart Scenario 3).

- [X] T015 [US3] Extend `scripts/validate-lifecycle-template.fsx`: for each profile under `--lifecycle none`, assert the gated set is absent and the product is present, and that `diff -r` of `none` vs `sdd` is empty, emitting `none: gated-absent=ok product-present=ok` to the report (FR-004/SC-003; research CC-3; quickstart Scenario 3)
- [X] T016 [US3] Add the matching `none` assertions to `tests/Package.Tests/Feature204LifecycleTemplateTests.fs`, regenerate the report, and confirm `--filter Feature204` is GREEN for the US3 lines

**Checkpoint**: All three lifecycle values behave per contract; `none` and `sdd` produce identical
template-level suppression.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Close out the composition, fail-fast, and discoverability success criteria that span all
three stories, and run the full quickstart proof.

- [X] T017 [P] Add the composition-matrix proof to `scripts/validate-lifecycle-template.fsx`: generate all 12 `lifecycle` (3) × `profile` (4) combinations crossed with `designSystem`/`feedback`, asserting each generates and the ant overlay (ungated) is present in every case and `feedback=true` emits no gated feedback skill under `sdd`/`none` (FR-007/FR-008/SC-004; quickstart Scenario 5)
- [X] T018 [P] Add the fail-fast proof to `scripts/validate-lifecycle-template.fsx` and the gate: assert `dotnet new fs-gg-ui --lifecycle bogus` exits non-zero with no output tree, emitting `unknown-value: rejected` to the report (FR-006/SC-004; quickstart Scenario 4)
- [X] T019 Finalize the validation report (`result: pass` with all lines present) via one full `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx` run, and confirm the env-free `--emit-report` self-provisioning path produces a report whose deterministic lines match (so a fresh checkout is GREEN); then confirm the always-on `dotnet test tests/Package.Tests --filter Feature204` is fully GREEN against it
- [X] T020 Run the full quickstart.md Scenarios 1–5 against the working-tree template and record the outcomes in `specs/204-template-lifecycle-symbol/readiness/quickstart-evidence.md` ("Done when" satisfied)
- [X] T021 Update the `fs-gg-ui-template` contract/compatibility registry entry (Tier 1 additive change) noting the new `lifecycle` option per the cross-repo coordination protocol, and confirm `dotnet new fs-gg-ui --help` lists the option with its three self-describing value descriptions (SC-005)
- [X] T022 Re-run the comprehensive baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against the T001 baseline to confirm zero new reds across every test project before merge

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately. T002 (reference tree) blocks the byte-identical proof.
- **Foundational (Phase 2)**: Depends on Setup. The `template.json` edit (T003→T004) and the early live scaffold (T005) BLOCK all user-story validation.
- **User Stories (Phase 3+)**: All depend on Foundational completion.
  - US1 (P1) is the non-regression gate and SHOULD complete before US2/US3 are trusted (its byte-identical proof underwrites them).
  - US2 and US3 can then proceed in parallel (different report sections / assertion blocks).
- **Polish (Phase 6)**: Depends on US1–US3 assertions existing in the regenerator + gate.

### Within / Across Stories

- T003 (add symbol) precedes T004 (gate sources) — same file, sequential.
- T006 (RED gate) precedes T008 (regenerator) precedes T009 (turn green) — author the failing gate first (Principle V).
- T011 (CC-1 fix) precedes T013 (dangling-ref assertion) — the assertion proves the fix.
- US2 (T012–T014) and US3 (T015–T016) both extend the same two files (`validate-lifecycle-template.fsx`, `Feature204LifecycleTemplateTests.fs`); if worked in parallel, coordinate edits to those two files (not [P] across each other).

### Parallel Opportunities

- Phase 1: T001 and T002 touch different artifacts and can overlap.
- Phase 6: T017 and T018 are independent assertion blocks ([P]).
- US2 and US3 validation can be developed in parallel by different people provided the two shared files are merged carefully.

---

## Parallel Example: Phase 6 cross-cutting

```bash
# Independent assertion blocks added to the regenerator can be developed together:
Task: "T017 composition-matrix proof (12 combos × designSystem/feedback) in scripts/validate-lifecycle-template.fsx"
Task: "T018 fail-fast proof (unknown value rejected) in scripts/validate-lifecycle-template.fsx and the gate"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (baseline + pre-feature reference tree).
2. Complete Phase 2: Foundational — the `template.json` symbol + conditions, **proven by the early
   live `dotnet new` scaffold (T005)** before anything builds on the gating hypothesis.
3. Complete Phase 3: User Story 1 — RED gate → regenerator → byte-identical proof → zero-churn.
4. **STOP and VALIDATE**: default is byte-identical and existing suites are green untouched.
5. This MVP alone satisfies the P1 board item and unblocks "Publish FS.GG.UI.Template".

### Incremental Delivery

1. Setup + Foundational → symbol exists and is proven to gate.
2. US1 → byte-identical non-regression gate (MVP — ship).
3. US2 → `sdd` suppression + product-builds + dangling-ref fix (unblocks the SDD composition epic).
4. US3 → `none` suppression (completes the option set).
5. Polish → composition matrix, fail-fast, discoverability, full quickstart + final baseline.

---

## Notes

- [P] tasks = different files, no dependencies.
- The single product-code edit is declarative (`.template.config/template.json`); everything else is
  validation/evidence or the CC-1 docs fix.
- Author the report gate RED before the regenerator produces the report (Constitution Principle V).
- The early live scaffold (T005) is non-negotiable: deterministic JSON-shape assertions can pass while
  a real scaffold emits/omits the wrong files.
- Commit after each task or logical group; keep existing tests unmodified (SC-002).
