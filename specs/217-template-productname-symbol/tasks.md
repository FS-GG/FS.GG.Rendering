---
description: "Task list — fs-gg-ui productName Scaffold Symbol"
---

# Tasks: fs-gg-ui `productName` Scaffold Symbol

**Input**: Design documents from `/specs/217-template-productname-symbol/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/productname-scaffold-provider.md ✓, quickstart.md ✓

**Tests**: REQUIRED for this feature. The spec/plan make test evidence mandatory (Constitution Principle V; Tier 1 contracted change). The always-on verdict-core gate and the env-gated live validator **are** the test artifacts and are authored **failing-first** (they assert the `productName` wiring before it lands).

**Organization**: Tasks are grouped by user story (US1 P1, US2 P2, US3 P3) so each is independently implementable and testable.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- Exact file paths are included in every task

## Path Conventions

This is a single dotnet template-package change in the rendering repo root. Key paths:

- Template config: `.template.config/template.json`
- Live validator (NEW): `scripts/validate-productname-template.fsx`
- Verdict-core gate (NEW): `tests/Package.Tests/Feature217ProductNameTemplateTests.fs` (registered in `tests/Package.Tests/Package.Tests.fsproj`)
- Readiness evidence (gitignored via `specs/*/readiness/`): `specs/217-template-productname-symbol/readiness/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm structure and capture the no-regression baseline before any change.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project (solution + `tests/Package.Tests` + `samples/**/*.Tests`) so pre-existing reds are known up
> front and not mistaken for regressions at merge. Use the discovery-based runner; do not hand-pick a
> subset.

- [X] T001 Confirm the feature readiness directory `specs/217-template-productname-symbol/readiness/` is writable and covered by `.gitignore` (`specs/*/readiness/` rule already present — verify no whitelist exception is needed for this feature)
- [X] T002 Establish the no-regression test baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/217-template-productname-symbol/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-change baseline trees and stand up the (failing-first) test seams that every user story is validated against.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

> **⚠️ Early live run (STANDING, do not omit — this feature's analogue of the "early live smoke run").**
> The plan carries a **standing assumption**: "byte-identical to today on the `-n`/default paths" is an
> empirical claim, and the wiring mechanism (remove `sourceName`; drive renames from a `productName ?? name`
> coalesce) is a **hypothesis until instantiated**. Therefore T004 captures real `dotnet new` output trees
> from the **pre-change** `template.json` (clean `HEAD`/worktree) across the name-path matrix **BEFORE T008
> edits the template**. This is the mandated early live evidence: the byte-diff in US2 is meaningless without
> a real pre-change baseline captured first. Do not defer it; do not synthesize it.

- [X] T003 Build the rename-token inventory map in `specs/217-template-productname-symbol/readiness/rename-tokens.md` by re-deriving from the real `.template.config/template.json` + on-disk template payload: confirm every `Product` path-segment (`src/Product/`, `src/Product/Product.fsproj`, `Product.slnx`, `tests/Product.Tests/`, `tests/Product.Tests/Product.Tests.fsproj`) and the ~22 `Product` content hits currently driven by `sourceName`, and verify the lowercase `product` cases (`load-product.fsx`, `docs/product.md`) are driven by `projectSlug`/separate replaces and must stay unaffected (data-model.md "Rename token inventory"; research R3)
- [X] T004 **Early live pre-change baseline (real `dotnet new`)**: from a clean pre-edit `template.json`, instantiate the **frozen matrix M** below and snapshot each tree under `specs/217-template-productname-symbol/readiness/baseline-trees/<id>/`. Matrix M (the single pinned combo set — T016 MUST diff against exactly these, no re-choosing):
  - **M1** `-n Foo` (default `--profile`/`--designSystem`/`--lifecycle`)
  - **M2** no name flag, default everything (the no-name default path)
  - **M3** `-n Foo --profile app --designSystem wcag --lifecycle spec-kit` (explicit defaults)
  - **M4** `-n Foo --profile app --designSystem wcag --lifecycle sdd` (the SDD provider's flag combo, no `--productName`)
  - **M5** `-n Acme --profile app --designSystem wcag --lifecycle sdd` (the SC-004 convergence reference)
  These frozen trees are the byte-diff oracle for US2 — capture them BEFORE T008 (plan standing assumption; research R2). If any flag value above is not a valid choice on the current template, substitute the nearest valid value and record the substitution in `readiness/rename-tokens.md` so M stays reproducible.
- [X] T005 [P] Scaffold the env-gated live validator `scripts/validate-productname-template.fsx` modeled on `scripts/validate-lifecycle-template.fsx`: env flag `FS_GG_RUN_PRODUCTNAME_VALIDATION=1`, `--emit-report` verdict core, report writer targeting `specs/217-template-productname-symbol/readiness/productname-template-validation.md`. Author the check stubs to **fail** (assert `productName` wiring that does not exist yet) — failing-first per Principle V
- [X] T006 [P] Scaffold the always-on verdict-core gate `tests/Package.Tests/Feature217ProductNameTemplateTests.fs` (env-free, deterministic; re-derives the contract straight from `template.json`) with `Feature217`-filterable test names; write assertions to **fail** before the wiring lands. Include the report self-provision/assert path (gate provisions its report from the validator's `--emit-report` verdict core, then asserts it)
- [X] T007 Register the new gate module in `tests/Package.Tests/Package.Tests.fsproj` (add `<Compile Include="Feature217ProductNameTemplateTests.fs" />` before `Tests.fs`/`Program.fs`, mirroring the `Feature209VersionCoherenceTests.fs` entry) and confirm `dotnet test tests/Package.Tests -c Release --filter Feature217` compiles and runs **red**

**Checkpoint**: Pre-change baseline trees frozen; validator + gate compile and fail for the right reason — user-story implementation can begin.

---

## Phase 3: User Story 1 - SDD scaffold-provider composition succeeds (Priority: P1) 🎯 MVP

**Goal**: `dotnet new fs-gg-ui … --productName Acme` (no `-n`) instantiates successfully (no exit 127) and emits a correctly-named, buildable `Acme` product — unblocking the SDD rendering provider and FS.GG.Templates#30.

**Independent Test**: Run `dotnet new fs-gg-ui -o Acme --productName Acme --profile app --lifecycle sdd --designSystem wcag` and confirm (a) success instead of exit 127, (b) project/namespace/slug all reflect `Acme`, (c) `dotnet build Acme/Acme.slnx -c Release` is clean (0 warn / 0 err).

### Implementation for User Story 1

- [X] T008 [US1] In `.template.config/template.json`, add the `productName` parameter symbol: `type: parameter`, `datatype: text`, `defaultValue: ""`, with the SDD-convention description (data-model.md "`productName` (NEW)"). Additive — its presence is what removes the exit-127 invalid-option error (FR-001)
- [X] T009 [US1] In `.template.config/template.json`, add the `effectiveName` generated **coalesce** symbol (`generator: coalesce`, `sourceVariableName: productName`, `fallbackVariableName: name`) and give it the rename duties `"replaces": "Product"` + `"fileRename": "Product"` (data-model.md "`effectiveName` (NEW)"; research R1 steps 2–3) (depends on T008)
- [X] T010 [US1] In `.template.config/template.json`, **remove** the top-level `"sourceName": "Product"` so `effectiveName` is the single driver of the `Product` literal — no double-substitution (research R1 step 4; data-model "`sourceName` (REMOVED)") (depends on T009)
- [X] T011 [US1] In `.template.config/template.json`, repoint the existing `projectSlug` casing symbol's `parameters.source` from `name` → `effectiveName` (keep `toLower: true`, `replaces: "fs-gg-ui"`) so the lowercased slug tracks `productName` on the `--productName`-only path (data-model "`projectSlug` (MODIFIED)"; research R1 step 5) (depends on T009)
- [X] T012 [US1] Handle whitespace `productName` (FR-006): confirm the engine's `coalesce` treats whitespace-only as empty; if not, normalize via trim (e.g. a `replace`/`join` generated symbol feeding the coalesce) so `--productName "  "` falls back to `name`. Leave `rootNamespace` (no-op compat text, default `"Product"`) untouched — do NOT give it rename duties (research R3) (depends on T009)
- [X] T013 [US1] Implement the live validator's acceptance checks in `scripts/validate-productname-template.fsx`: G1 the **low-level** `dotnet new fs-gg-ui -o Acme --productName Acme --profile app --lifecycle sdd --designSystem wcag` (no `-n`) instantiates with no exit 127 — **this invocation is the always-on, env-free proof of FR-007/SC-001** (it does not depend on the org feed or `fsgg-sdd`; the full `fsgg-sdd` composition in T022 is an additional, skippable tier on top); G2 the generated tree is named `Acme` across project/file names, namespaces, and slug; SC-002 `dotnet build -c Release` of the `Acme` product is 0 warn / 0 err (quickstart.md §2 rows G1/G2/G6) (depends on T008–T012)
- [X] T014 [US1] Implement the precedence + fallback validator checks: G3 both `--productName Acme` and `-n Foo` ⇒ product named `Acme` (productName wins, no half-rename); G4 fallback — assert **two distinct cases**: (a) empty `--productName ""` ⇒ falls back to default, **and** (b) whitespace-only `--productName "  "` ⇒ falls back to default (this case forces the trim branch in T012; do NOT rely on the empty-string case alone, per research R3) (quickstart.md §2 rows; FR-005/FR-006) (depends on T013)
- [X] T015 [US1] Run `FS_GG_RUN_PRODUCTNAME_VALIDATION=1 dotnet fsi scripts/validate-productname-template.fsx` and confirm G1/G2/G3/G4/SC-002 pass; the report at `specs/217-template-productname-symbol/readiness/productname-template-validation.md` is emitted (or env-skipped tiers explicitly disclosed if the live tier cannot run). **The G1 low-level `dotnet new --productName` result here is the authoritative SC-001 / FR-007 evidence**; T022's `fsgg-sdd` composition is supplementary and may be `environment-limited` without weakening this proof (depends on T013, T014)

**Checkpoint**: `--productName` path instantiates and builds clean; US1 independent test passes. MVP delivered.

---

## Phase 4: User Story 2 - Existing name-based consumers are unaffected (Priority: P2)

**Goal**: Removing `sourceName` perturbs nothing — every existing path (`-n`, default, full flag matrix) produces byte-identical output when `productName` is absent, and the two name paths converge.

**Independent Test**: Diff post-change output against the T004 pre-change baseline trees: `-n Foo`, no-name default, and the flag matrix must all be zero-diff; and `--productName Acme` ≡ `-n Acme` must be zero-diff.

### Implementation for User Story 2

- [X] T016 [US2] Implement the byte-diff backward-compat checks in `scripts/validate-productname-template.fsx`: SC-003 — instantiate **exactly the frozen matrix M (M1–M4) defined in T004** (no `--productName`) on the changed template and assert zero-diff vs the corresponding `readiness/baseline-trees/<id>/` snapshots. Use the same pinned combos T004 captured — do not re-pick "representative" combos here, or the diff oracle is invalidated (quickstart §2 row G5; research R2) (depends on T004, T015)
- [X] T017 [US2] Implement the path-convergence check: SC-004 — instantiate `--productName Acme` and `-n Acme` with the **M5 flag set** (`--profile app --designSystem wcag --lifecycle sdd`) on the changed template and assert byte-identical trees (quickstart §2 row G2/G3; research R2) (depends on T016)
- [X] T018 [US2] Implement the verdict-core gate assertions in `tests/Package.Tests/Feature217ProductNameTemplateTests.fs` that re-derive the additive contract straight from `template.json`: `productName` parameter present (datatype text, default `""`); top-level `sourceName` removed; `effectiveName` coalesce present with `replaces`+`fileRename` on `Product`; `projectSlug.source == effectiveName`. Wire the report self-provision/assert so the gate is green on a fresh checkout (depends on T010, T011)
- [X] T019 [US2] Run `FS_GG_RUN_PRODUCTNAME_VALIDATION=1 dotnet fsi scripts/validate-productname-template.fsx` and confirm SC-003 (0 diffs) and SC-004 (0 diffs); then run `dotnet test tests/Package.Tests -c Release --filter Feature217` and confirm the verdict-core gate is **green** (depends on T016, T017, T018)

**Checkpoint**: Byte-diff proves full backward compatibility and path convergence; always-on gate green. US1 + US2 both verified.

---

## Phase 5: User Story 3 - Cross-repo contract stays coherent (Priority: P3)

**Goal**: The `scaffold-provider` (rendering) contract change is recorded as additive in the org-level registry and is coherent with the SDD side.

**Independent Test**: Inspect the org-level cross-repo contract/compatibility registry and confirm it records the additive `scaffold-provider` change (rendering now honors `productName`) cross-referencing #27 ⇄ SDD#35.

### Implementation for User Story 3

- [X] T020 [US3] Using the `cross-repo-coordination` skill/protocol, record the `scaffold-provider` (rendering) contract change in the org-level registry under `FS-GG/.github` (no in-repo `registry/dependencies.yml`): mark it **additive / backward-compatible**, citing the `contracts/productname-scaffold-provider.md` surface (FR-008; research R4; SC-005). **If org-level write access is unavailable in the run environment, disclose `environment-limited` and record the intended registry entry + cross-references in `readiness/cross-repo-record.md` as the disclosed substitute** (do not silently skip US3)
- [X] T021 [US3] Cross-reference `FS-GG/FS.GG.Rendering#27` ⇄ `FS-GG/FS.GG.SDD#35` and confirm both agree the Rendering side owns the `productName ↔ name` mapping; record the compatibility-projection update so no contract incoherence remains (SC-005 acceptance scenarios 1–2) (depends on T020)

**Checkpoint**: Registry/compatibility projection truthful; all three stories complete.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: End-to-end proof and no-regression confirmation.

- [X] T022 Run the full quickstart.md end-to-end: step 1 gate green, step 2 validator all-pass (or env-skips disclosed), step 3 the **full `fsgg-sdd scaffold --provider rendering --param productName=Acme` composition** (the additional cross-repo tier — SC-001/FR-007 are already proven always-on by T015's low-level `dotnet new --productName`) succeeds where it previously failed with exit 127. Disclose `environment-limited` with the documented substitute if the org feed / `fsgg-sdd` ≥ 0.2.0 is unavailable; this skip does NOT invalidate SC-001/FR-007 (proven in T015) (research R3 "Org feed availability")
- [X] T023 Re-run the no-regression baseline `dotnet fsi scripts/baseline-tests.fsx --out specs/217-template-productname-symbol/readiness/baseline-after.md` and diff against T002 to confirm zero new reds across solution + Package.Tests + samples
- [X] T024 [P] Final docs/contract consistency pass: confirm `contracts/productname-scaffold-provider.md`, `data-model.md`, and the emitted readiness report agree on the shipped symbol set; note any deviation discovered during implementation back into the spec docs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **Blocks all user stories.** T004 (pre-change baseline) MUST complete before any `template.json` edit (T008).
- **US1 (Phase 3)**: Depends on Foundational. Delivers the MVP unblock.
- **US2 (Phase 4)**: Depends on Foundational + the `template.json` edits (T008–T012) being in place to diff against; T016 also depends on the T004 baseline trees.
- **US3 (Phase 5)**: Depends only on the contract surface (Phase 1 docs) being final; functionally independent of US1/US2 landing — paperwork that trails the unblock.
- **Polish (Phase 6)**: Depends on US1 + US2 (and US3 for the registry check in T022 step 4).

### Critical ordering inside the `template.json` change

T008 (add `productName`) → T009 (add `effectiveName` coalesce) → {T010 remove `sourceName`, T011 repoint `projectSlug`, T012 whitespace/`rootNamespace`}. These all touch the **same file** (`.template.config/template.json`) so they are **sequential, not parallel**.

### Parallel Opportunities

- **Foundational**: T005 (validator scaffold) and T006 (gate scaffold) are different files → `[P]`. Both can proceed while T003/T004 evidence is being captured, but the validator's byte-diff oracle (T004) must exist before US2's T016 runs.
- **Cross-story**: US3 (T020–T021, org-registry work, no repo-file overlap) can run in parallel with US1/US2 once the contract surface is final.
- **Polish**: T024 (docs pass) is `[P]` against T022/T023 (run-based) — different artifacts.
- The validator script (`scripts/validate-productname-template.fsx`) is a single file: T005, T013, T014, T016, T017 touch it and are therefore **sequential** with respect to each other.

---

## Parallel Example: Foundational seams

```bash
# After T004's pre-change baseline is captured, stand up the two test seams together:
Task: "T005 Scaffold scripts/validate-productname-template.fsx (failing-first)"
Task: "T006 Scaffold tests/Package.Tests/Feature217ProductNameTemplateTests.fs (failing-first)"
```

## Parallel Example: US3 alongside US1/US2

```bash
# Once contracts/ is final, the cross-repo paperwork runs independently of the template edit:
Task: "T020 Record additive scaffold-provider change in FS-GG/.github registry"
Task: "T021 Cross-reference #27 ⇄ SDD#35; update compatibility projection"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → baseline captured.
2. Phase 2 Foundational → **pre-change trees frozen (T004)**, validator + gate failing-first.
3. Phase 3 US1 → `template.json` wired; `--productName` instantiates and builds clean.
4. **STOP and VALIDATE**: run the US1 independent test (`dotnet new … --productName Acme` + Release build).
5. This already unblocks the SDD rendering provider / FS.GG.Templates#30.

### Incremental Delivery

1. Setup + Foundational → oracle + seams ready.
2. US1 → the `--productName` path works → MVP.
3. US2 → byte-diff proves nothing else moved → backward compat locked.
4. US3 → org registry recorded → cross-repo coherence.
5. Polish → end-to-end + no-regression confirmation.

### Notes

- `[P]` = different files, no incomplete-task dependency.
- The `template.json` edits and the validator script are each single files → tasks on them are sequential.
- Verify the validator + gate FAIL (failing-first) before T008 lands the wiring.
- Capture the T004 pre-change baseline BEFORE editing `template.json` — the standing assumption in plan.md.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
