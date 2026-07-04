---

description: "Task list for feature 242 — scaffold discoverability sharpening"
---

# Tasks: Scaffold discoverability sharpening

**Input**: Design documents from `specs/242-scaffold-discoverability/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: INCLUDED — the spec defines explicit test surfaces and constitution Principle V mandates test evidence. Test tasks are written to FAIL first.

**Organization**: By user story. US1 = generated `SWAP-CHECKLIST.md` (P1). US2 = build-target `--help` banner (P2). The two are independent and independently shippable.

## Path Conventions

Framework repo (single tree). Template content under `template/base/` + `template/fragments/`; template config `.template.config/template.json`; template gates under `tests/Package.Tests/`; durable in-product gate `template/base/tests/Product.Tests/GovernanceTests.fs`.

---

## Phase 1: Setup (Shared Infrastructure)

> **⚠️ Comprehensive baseline (STANDING).** Run EVERY test project via the discovery-based runner so pre-existing reds are known up front and not mistaken for regressions.

- [X] T001 Create the authored content directory `template/fragments/swap-checklist/{game,app,governed}/` (empty placeholders) per plan.md Project Structure
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/242-scaffold-discoverability/readiness/baseline.md` (runs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set)

---

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **⚠️ Early live instantiation check (STANDING — the "run the real thing" gate).** The design rests on template-engine behavior (per-profile `sources[]` emission, `copyOnly`, `build.fsx` arg routing). Confirm the mechanism against a real `dotnet new` BEFORE building content/tests on it. Treat the plan's emission assumptions as unverified until observed.

- [X] T003 Verify the per-profile re-point inventory in `data-model.md` against the actual source branches of `template/base/src/Product/{Model,View,LayoutEvidence,EvidenceCommands}.fs` (confirm every listed symbol/field read exists in that profile's `//#if` branch; correct data-model.md if any drift). Explicitly confirm `sample-pack` resolves to the app-model `//#else` branch (no separate model branch), so the app-family checklist correctly serves it.
- [X] T004 **Early live instantiation check**: pack + `dotnet new install` the template, then `dotnet new fs-gg-ui --profile game -o <tmp>` and `--profile app -o <tmp>`; observe that (a) a single `SWAP-CHECKLIST.md` would land at product root from a conditional source without target-path collision, and (b) `dotnet fsi build.fsx --help` currently has NO help path (baseline behavior). Record findings in `specs/242-scaffold-discoverability/readiness/instantiation-check.md`. If the separate-source emission mechanism does not behave as designed, revise research.md Decision 1 before proceeding.
- [X] T005 [P] Confirm the test seams: locate the two `[<Tests>]` branches in `template/base/tests/Product.Tests/GovernanceTests.fs` (governed at L41, app/game `//#else` at L59) and the `Package.Tests.fsproj` compile list (insert point before `Tests.fs` at L50)
- [X] T006 Draft the two content seams as skeletons (no per-profile symbols yet): the shared `SWAP-CHECKLIST.md` section structure (from data-model.md) and the `build.fsx` banner string constant shape (the `Dev`/`Test`/`Verify` rows)

**Checkpoint**: Emission mechanism confirmed live; inventory verified; seams drafted — user stories can begin.

---

## Phase 3: User Story 1 - Precise swap checklist (Priority: P1) 🎯 MVP

**Goal**: Every scaffolded product ships a profile-correct `SWAP-CHECKLIST.md` naming the exact model-field-reading symbols to re-point, so a swap needs no compiler-error archaeology.

**Independent Test**: Instantiate each profile; confirm `SWAP-CHECKLIST.md` is present with family-correct content and every named symbol exists in the generated tree (per quickstart.md §2).

### Tests for User Story 1 (write FIRST, ensure they FAIL)

- [X] T007 [P] [US1] Create `tests/Package.Tests/SwapChecklistTemplateTests.fs` (module `SwapChecklistTemplateTests`; header: "spec 242-scaffold-discoverability"; NOT `Feature242*` — that name is taken by `Feature242DocsCurrencyTests.fs`). Assert per family: every symbol named in `template/fragments/swap-checklist/<family>/SWAP-CHECKLIST.md` exists in the corresponding `template/base/src/Product/*.fs` profile branch (no phantoms), and every durable re-point function from data-model.md is listed (coverage). Include the honesty caveat in the header (Principle V: proven = no-phantom + known-reader coverage, not full F#-parse). Wire it into `Package.Tests.fsproj` before `Tests.fs`.
- [X] T008 [P] [US1] In `template/base/tests/Product.Tests/GovernanceTests.fs`, add a presence/discoverability test to BOTH `[<Tests>]` branches: `SWAP-CHECKLIST.md` exists at product root and contains `LayoutEvidence.fs`, `EvidenceCommands.fs`, `Model.fs`, `View.fs`, and a `scaffold-map.md` reference. STRUCTURAL ONLY — no exact per-symbol prose (so a swap can rewrite it freely).

### Implementation for User Story 1

- [X] T009 [P] [US1] Author `template/fragments/swap-checklist/game/SWAP-CHECKLIST.md` per data-model.md "Family: game" (rewrite-wholesale files; `LayoutEvidence` readers `activeGameplayBoundsForSize`/`spawnUsesGameplayRegion`/`scoreTextBounds`/`layoutEvidenceForSize`/`movement|collisionUsesGameplayRegion`/`validateGeneratedLayout`; `EvidenceCommands` `mapKey`/`layoutEvidenceCommand`/`sceneEvidence`; leave-untouched spine; additive-swap note; scaffold-map pointer)
- [X] T010 [P] [US1] Author `template/fragments/swap-checklist/app/SWAP-CHECKLIST.md` per data-model.md "Family: app" (`contentLayout`/`activeGameplayBoundsForSize`/`spawnUsesGameplayRegion`/`hudTextBounds`/… reading `ContentColumn`/`ContentRow`/`ItemCount`/`Step`/`NextLabel`/`Page`; `mapKey`; same skeleton)
- [X] T011 [P] [US1] Author `template/fragments/swap-checklist/governed/SWAP-CHECKLIST.md` per data-model.md "Family: governed" (`LayoutEvidence.layoutEvidenceForSize` reading `model.Name`; `EvidenceCommands.layoutEvidenceCommand`/`sceneEvidence`; same skeleton)
- [X] T012 [US1] Add three mutually-exclusive, profile-gated `sources[]` entries to `.template.config/template.json` re-emitting the family file to `./SWAP-CHECKLIST.md` (`copyOnly`; conditions `game` / `(app||sample-pack)` / `(governed||headless-scene)`), following the `docs/skillist-reference.md` exclude-and-re-emit precedent. Confirm the base source does not otherwise ship the file.
- [X] T013 [US1] Run the US1 gates green: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter SwapChecklist` and the `product-governance` list; then re-instantiate `game`/`app`/`governed`/`headless-scene`/`sample-pack` and confirm exactly one family-correct `SWAP-CHECKLIST.md` lands each (quickstart.md §2)

**Checkpoint**: US1 fully functional — every profile ships a correct, phantom-free checklist. MVP shippable here.

---

## Phase 4: User Story 2 - Build-target help banner (Priority: P2)

**Goal**: A developer running the build sees the load-bearing `Dev`/`Test`/`Verify` semantics at the entry point, side-effect-free, kept in sync with `docs/product.md`.

**Independent Test**: `dotnet fsi build.fsx --help` (and `./build.sh --help`) print the banner; no `readiness/logs/*` written; exit 0 (quickstart.md §1).

### Tests for User Story 2 (write FIRST, ensure they FAIL)

- [X] T014 [P] [US2] In `template/base/tests/Product.Tests/GovernanceTests.fs` (the existing build.fsx test list, both branches as applicable), assert: `build.fsx` contains a help branch recognizing the bare `help` token (fsi reserves `--help`/`-h` — see T004 evidence); the banner string carries the `Dev`(completion-marker / does-not-compile), `Test`(first real `dotnet test`), `Verify`(merge-gate audit / hard-blocks until every task `[X]`) phrases; and the help branch returns BEFORE `writeLog`/target dispatch (no side effect). SYNC assertion: the same load-bearing phrases (`completion-marker`, `merge-gate audit`, `hard-blocks`, `first real`) appear in `docs/product.md` (anchors at product.md L181/L189/L191/L194).
- [X] T015 [P] [US2] In `tests/Package.Tests/SwapChecklistTemplateTests.fs` (or a sibling `BuildHelpBannerTemplateTests.fs`), assert `build.sh` exposes a `--help`/`-h` verb whose banner mirrors the `build.fsx` semantics (shell/`.fsx` parity)

### Implementation for User Story 2

- [X] T016 [US2] Add the help path to `template/base/build.fsx`: detect the bare `help` token (and defensively `Help`/`--help`/`-h`) in the skipped `Environment.GetCommandLineArgs()` args BEFORE `targetFromArgs`/`run`; print a banner (targets + `Dev`/`Test`/`Verify`/pass-through semantics per data-model.md); return without writing any `readiness/logs/*.txt`; exit 0. Do NOT touch the frozen `Test`/`Verify` bodies or the engine-resolution path.
- [X] T017 [P] [US2] Extend `template/base/build.sh`: add `--help`/`-h` to the `case` block and expand `print_usage` into the semantics banner (parity with `build.fsx`); keep `set -euo pipefail` and the verb→target mapping intact
- [X] T018 [US2] Reconcile `docs/product.md` wording so the sync assertion (T014) passes with the banner — adjust phrasing on either side to a single agreed set of load-bearing phrases (no semantic change to product.md's narrative)
- [X] T019 [US2] Run the US2 gates green and execute quickstart.md §1: `dotnet fsi build.fsx --help`, `-h`, `help`, and `./build.sh --help`; confirm banner content, exit 0, and NO `readiness/logs/Dev.txt` side effect

**Checkpoint**: US1 and US2 both work independently.

---

## Phase 5: Polish & Cross-Cutting Concerns

- [X] T020 [P] Cross-reference `SWAP-CHECKLIST.md` from `template/base/docs/scaffold-map.md` (one pointer line: "for the precise per-symbol re-point to-do list, see the generated SWAP-CHECKLIST.md") — verbatim `copyOnly` doc, so keep tokens intact
- [X] T021 Run full quickstart.md validation (§1–§4), including the SC-005 additive-swap regression (a trivial added model field keeps `product-governance` + `-t Test` green)
- [X] T022 Re-run the no-regression baseline (`scripts/baseline-tests.fsx`) and diff against T002 — confirm zero new reds; capture in `specs/242-scaffold-discoverability/readiness/`
- [X] T023 Update roadmap issue FS-GG/FS.GG.Rendering#75 with the shipped evidence (checklist per profile + banner), and confirm no `contract-change`/registry work is required (FR-010)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (P1)**: none — start immediately.
- **Foundational (P2)**: after Setup — BLOCKS both stories. T004 (live check) gates all content/test work.
- **US1 (P3)**: after Foundational — independent of US2.
- **US2 (P4)**: after Foundational — independent of US1 (touches `build.fsx`/`build.sh`/`product.md`, disjoint from US1's checklist files, except both add tests to `GovernanceTests.fs` — sequence those two edits).
- **Polish (P5)**: after the desired stories.

### Within US1

- T007/T008 (tests, FAIL first) before T009–T012 (content/config). T009–T011 are parallel (distinct files). T012 depends on T009–T011 existing. T013 verifies.

### Within US2

- T014/T015 (tests, FAIL first) before T016–T018. T016 (`build.fsx`) and T017 (`build.sh`) are parallel-ish (distinct files); T018 (`product.md`) reconciles with T016's banner. T019 verifies.

### Parallel Opportunities

- T009 ∥ T010 ∥ T011 (three authored files).
- T007 ∥ T008 (distinct test files/branches) — but both US1 and US2 add to `GovernanceTests.fs`; serialize the actual GovernanceTests edits (T008 then T014) to avoid a same-file conflict.
- Given one developer: do US1 fully (MVP), then US2.

---

## Implementation Strategy

### MVP First (US1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (incl. **early live instantiation check** T004) → 3. Phase 3 US1 → **STOP & VALIDATE** (every profile ships a correct checklist) → shippable MVP.

### Incremental Delivery

US1 (checklist) is the high-leverage half per the consumer report; ship it first. US2 (banner) layers on independently. Either can merge alone.

---

## Notes

- `[P]` = different files, no dependency. Both stories touch `GovernanceTests.fs` — serialize those specific edits.
- No versioned-contract/registry change (FR-010) — T023 confirms.
- Honesty caveat (Principle V) lives in the `SwapChecklistTemplateTests.fs` header: coverage proof is no-phantom + known-reader, not a full F#-parse.
- Internal test names are descriptive, NOT `Feature242*` (collision with existing docs-currency test).
