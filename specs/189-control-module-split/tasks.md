---
description: "Task list for Control.fs / ControlInternals decomposition (Patterns A+E)"
---

# Tasks: Control.fs / ControlInternals Decomposition (Patterns A+E, kind registry)

**Input**: Design documents from `/specs/189-control-module-split/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/module-topology.md ✅, quickstart.md ✅

**Tests**: This is a refactor validated by the **existing** test suites + `hashScene`/fingerprint
byte-equality + semantic artifact diff + the §7 golden-hash review gate (FR-015). No new golden-image
harness is built. The **one** new test asset is the extension of the catalog↔registry completeness
oracle (`Feature183KindRegistryTests.fs`) to cover the painter (FR-007 / SC-007) — written as part of
US3.

**Organization**: Tasks grouped by user story (US1→US4) in strict risk order. Stories are sequenced,
not parallel: US3 routes *into* the US1 geometry, and US2's hash extraction must be stable before US3's
behavior-sensitive registry routing. The geometry/hash work within a story is parallelizable per `[P]`.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 / US4 (Setup, Foundational, Polish carry no story label)
- Exact file paths are given in each task.

## Path Conventions

Single F# library project: `src/Controls/`, tests under `tests/`, baselines under `readiness/` and
`specs/189-control-module-split/readiness/`. All work stays within `src/Controls` plus baseline/consumer
edits a non-empty surface diff would require (FR-013).

---

## Phase 1: Setup — Pre-refactor baseline capture (FR-014, BEFORE any production edit)

**Purpose**: Capture the immutable pre-refactor reference every story diffs against. NO production edit
may precede these tasks (FR-014). Per the standing comprehensive-baseline requirement, the test baseline
runs **every** `*.Tests.fsproj` (solution + `Package.Tests` + samples) so pre-existing reds are known up
front, not discovered at merge.

- [X] T001 Capture the no-regression test baseline (every test project): `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/189-control-module-split/readiness/baseline/test-baseline.md` — record the full red/green set (expect a small known pre-existing red set per 188; flag it here)
- [X] T002 [P] Capture the pre-refactor public-surface snapshot: run `dotnet fsi scripts/refresh-surface-baselines.fsx`, then copy `readiness/surface-baselines/FS.GG.UI.Controls.txt` → `specs/189-control-module-split/readiness/baseline/FS.GG.UI.Controls.pre.txt`
- [X] T003 [P] Capture the reference scene-hash / fingerprint / faithful-content / inspection corpus: `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Fingerprint|RetainedRender|Inspection|KindRegistry|Layout|Rendering" > specs/189-control-module-split/readiness/baseline/controls-corpus.log`
- [X] T004 [P] Re-confirm the affected test-project list against the current tree (research "Resolved unknowns": `Controls.Tests`, `Package.Tests` `SurfaceAreaTests`, `Elmish.Tests`, `ControlsGallery.Tests`, any `tools/Rendering.Harness`/`Rendering.Harness.Tests` reading controls scene-hash/inspection artifacts) and record it in `specs/189-control-module-split/readiness/baseline/affected-suites.md`

**Checkpoint**: Pre-refactor baseline (red/green set + surface snapshot + corpus log + suite list)
captured and committed. No production code touched yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Convert the unverified compile-order hypothesis (plan standing-assumption) into a checked
fact, observe the real rendered output once, author the internal `.fsi` seams, and extract the shared
prelude every story consumes — all BEFORE any geometry/hash/painter body moves.

**⚠️ CRITICAL**: No user-story work (US1–US4) may begin until this phase is complete.

> **⚠️ Compile probe IS the early de-risking gate (research D5).** The "drive the real thing before
> trusting the plan" requirement is met here by (a) the full-solution compile probe with stub modules,
> which proves the load-bearing module ordering, and (b) the live smoke run of the real rendered output.
> Treat the C1 ordering and the painter-field-vs-separate-map shape as **unverified until T006 builds
> clean** — do not move bodies onto an unproven topology.

- [X] T005 Re-confirm current line ranges / module-topology map against `src/Controls/Control.fs` (line refs in research.md §top may have drifted) and record any deltas in `specs/189-control-module-split/readiness/baseline/topology-confirm.md` (target ranges: `ControlInternals` L124–~3066; prelude L124–625; chart/widget geom L626–~1860; `faithfulContent` L1868; `renderScene` L2036; `toLayout` L2163; `evaluateLayout` L2268; `paintLeaf` L2298; `hashScene` L2405–2853; `required` L353; `module Control` L3085; 30 tail modules L3358–3511)
- [X] T006 **Compile probe** (research D5): add empty/stub `module internal` files for `ControlPrimitives`, `ChartGeometry`, `WidgetGeometry`, `SceneHash`, `ContentRender`, `LayoutEval`, `NodeAssembly` (each with a paired `.fsi`) to `src/Controls/Controls.fsproj` in the C1 order; run `dotnet build FS.GG.Rendering.slnx -c Release` — MUST succeed with stubs in place. This empirically fixes the `ContentRender`↔`NodeAssembly` relative order and the painter-field-vs-separate-map shape (D2/C1). Record the confirmed order in `topology-confirm.md`
- [X] T007 **Early live smoke run**: drive the real rendered output for the controls under change (launch the ControlsGallery / viewer host per the `fs-gg-skiaviewer` / `run` skill, exercise the chart/widget/text controls) and capture live visual/readback evidence (or `environment-limited` with a disclosed substitute per the Feature-168 evidence rules) into `specs/189-control-module-split/readiness/baseline/live-smoke.md` — establishes the rendered-output reference before any body moves
- [X] T008 Author the paired `.fsi` seams for each new internal module (`ControlPrimitives.fsi`, `ChartGeometry.fsi`, `WidgetGeometry.fsi`, `SceneHash.fsi`, `ContentRender.fsi`, `LayoutEval.fsi`, `NodeAssembly.fsi`) in `src/Controls/` — all `module internal`, reached by tests via the existing `InternalsVisibleTo Controls.Tests`; nothing on the public surface (Constitution II; C1)
- [X] T009 Extract the shared helper prelude (`Control.fs` L124–625: `measureText`/`setMeasureTextHook` seam, `chartValues`, `styleClassesOf`, `visualStateOf`, `fittedFontSize`, `ellipsize`, accessibility, `required` helpers, `lowerSlots`, `nodeWidth`, …) into `src/Controls/ControlPrimitives.fs` (members `module internal`, NOT `private`); wire compile order so it precedes geometry; `dotnet build FS.GG.Rendering.slnx -c Release` MUST stay clean. NOTE: the `required`-attribute validation at `Control.fs:353` stays at its site (FR-012) — only its helpers move. (This realizes the spec's "US1 foundational" `ControlPrimitives` prelude — placed in Foundational because US1–US4 all depend on it)

**Checkpoint**: Topology proven to compile, rendered-output reference captured, `.fsi` seams drafted,
shared prelude extracted. User-story extractions can now proceed in strict priority order.

---

## Phase 3: User Story 1 — Extract chart + widget geometry; factor the shared preamble (Priority: P1) 🎯 MVP

**Goal**: Relocate the ~40 `private *Geom` functions into `ChartGeometry` + `WidgetGeometry` groupings
and factor the ~17 repeated `match pts with [] -> emptyState …` guards into one `withPoints` combinator.
Pure relocation + shared-skeleton collapse — every `Scene list` byte-identical.

**Independent Test**: Build the full solution; run the controls/faithful-content/chart suites; confirm
every chart and widget renders a `Scene list` byte-identical to the T003 corpus log, `Control.fs`
shrinks by the relocated geometry, and the `FS.GG.UI.Controls` surface baseline is unchanged.

- [X] T010 [P] [US1] Move the chart `*Geom` producers (`lineGeom`, `barGeom`, `pieGeom`, `scatterGeom`, `graphGeom`, `areaGeom`, `columnGeom`, `histogramGeom`, `boxPlotGeom`, `heatmapGeom`, `radarGeom`, … + `emptyState` and `pillGeom`, `Control.fs` L626–~1030) into `src/Controls/ChartGeometry.fs`, preserving function names and call shapes; declare them in `ChartGeometry.fsi`
- [X] T011 [US1] Add the `withPoints theme box caption pts body` combinator to `src/Controls/ChartGeometry.fs` (≡ `match pts with [] -> emptyState theme box caption | nonEmpty -> body nonEmpty`, data-model C3) and route the ~17 chart `*Geom` empty-points guards through it — collapse ONLY the shared guard skeleton; divergent bodies stay in the `body` lambda (FR-002, feature-180/181 lesson). Depends on T010
- [X] T012 [P] [US1] Move the widget/layout/container `*Geom` producers (`buttonGeom`/`switchGeom`/`checkboxGeom`/`toggleGeom`/`sliderGeom`/`tabsGeom`/… `Control.fs` ~L1032–1860) into `src/Controls/WidgetGeometry.fs`; declare them in `WidgetGeometry.fsi`. Widget geoms that don't share the empty-points guard are NOT forced through `withPoints` (D4). `WidgetGeometry` may reference `ChartGeometry`'s shared `pillGeom`/`emptyState`
- [X] T013 [US1] Update `src/Controls/Controls.fsproj` compile order so `ControlPrimitives` → `ChartGeometry` → `WidgetGeometry` precede their consumers (C1 order); remove the relocated bodies from `Control.fs`. Depends on T010–T012
- [X] T014 [US1] Validate US1: `dotnet build FS.GG.Rendering.slnx -c Release` clean (no back-edge), then `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Rendering|TextShaping|Chart|Fingerprint"` — every chart/widget `Scene list` byte-identical to the T003 corpus; empty-point charts produce the identical `emptyState` scene (acceptance #2/#3, SC-002)
- [X] T015 [US1] Confirm `FS.GG.UI.Controls` surface unchanged (`dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff readiness/surface-baselines/FS.GG.UI.Controls.txt` → empty) and that `Control.fs` shrank by the relocated geometry (acceptance #1; SC-001)

**Checkpoint**: US1 complete and independently testable — geometry relocated, `withPoints` in place,
byte-identical output, surface unchanged. This is the MVP increment and the dispatch floor US3 builds on.

---

## Phase 4: User Story 2 — Extract `SceneHash`, `LayoutEval`, `NodeAssembly` (Priority: P2)

**Goal**: Recast `hashScene` as a `SceneHasher` visitor in `SceneHash`; move the layout evaluators into
`LayoutEval` and the assembly functions into `NodeAssembly`, preserving internal names/call shapes.
Also relocate `faithfulContent` **as-is (byte-identical, painter table deferred to US3)** into
`ContentRender` — required because `NodeAssembly` calls `faithfulContent`, which must compile *before*
it (C1); deferring the relocation to US3 would create a forward-reference back-edge.

**Independent Test**: Run the controls/retained-render/hash-fingerprint/layout suites; `hashScene` output
byte-identical for the scene corpus (incl. `hashScene []` canary), `evaluateLayout` bounds byte-identical
(INV-1), `paintNode`/`renderScene` scenes equivalent — or any legitimate hash reorder captured under an
explicit reviewed golden-hash record.

- [X] T016 [US2] Recast `hashScene` (`Control.fs` L2405–2853, the 25-case inline `goNode`/`goScene` walk) as a structured `SceneHasher` visitor over `SceneNode` in `src/Controls/SceneHash.fs`, preserving the exact mix order (tag → fields → children) and the FNV-1a `mutable h` accumulator (keep the `// mutable: hot path` disclosure); keep the public-internal name `hashScene: Scene list -> uint64` and the `emptySceneListFingerprint = hashScene []` canary. Declare the surface in `SceneHash.fsi` (FR-003, D3)
- [X] T017 [P] [US2] Move `toLayout` (L2163), `evaluateLayout` (L2268), `evaluateLayoutIncremental` into `src/Controls/LayoutEval.fs`, preserving names/call shapes; declare in `LayoutEval.fsi` (FR-004)
- [X] T018 [US2] **Resolve the `NodeAssembly`→`faithfulContent` compile-order edge first**: relocate `faithfulContent` (`Control.fs` L1868–1990) **as-is — inline kind `match`, byte-identical, no painter table yet** — into a new `src/Controls/ContentRender.fs` (+ `ContentRender.fsi`) compiled *before* `NodeAssembly` (C1), so the reference resolves to an earlier-compiled module (no back-edge). Then move `paintLeaf` (L2298), `paintNode`, `renderScene` (L2036) into `src/Controls/NodeAssembly.fs` (consumes `ContentRender`, `LayoutEval`, `SceneHash`, `ControlPrimitives`), preserving names/call shapes; declare in `NodeAssembly.fsi`. US2 keeps `ContentRender` byte-identical; US3 (T023) transforms its dispatch into the painter table. Depends on T016, T017
- [X] T019 [US2] Update `src/Controls/Controls.fsproj` compile order (`SceneHash` → `ContentRender` → `LayoutEval` → `NodeAssembly` per C1); remove relocated bodies from `Control.fs`; `dotnet build FS.GG.Rendering.slnx -c Release` clean. Depends on T016–T018
- [X] T020 [US2] Validate hash/assembly: `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "Fingerprint|RetainedRender|Layout|Rendering"` — `hashScene` byte-identical for the T003 corpus (incl. `hashScene []` canary); `paintNode`/`renderScene` scenes equivalent. Any legitimate hash reorder → record in `specs/189-control-module-split/readiness/golden-hash-review.md` with proof it breaks no `RetainedRender` picture-cache invariant (FR-009/SC-008); otherwise NO expected-output edits
- [X] T021 [US2] Confirm INV-1: `evaluateLayout` `root`/`boundsById` byte-identical to a pre-refactor full evaluate over the layout corpus (SC-004), and all internal callers (`Control`, `ControlRuntime`, `RetainedRender`) resolve the moved functions unchanged

**Checkpoint**: US1 + US2 complete — hash/layout/assembly extracted and `faithfulContent` relocated
byte-identically into `ContentRender`, hot-path equivalence proven or reviewed. `ContentRender`'s inline
dispatch is the stable Pattern-A target US3 transforms.

---

## Phase 5: User Story 3 — Route `faithfulContent` + the 6 `match …Kind` sites through the registry (Priority: P3)

**Goal**: Add a `Painter` field to `ControlKindEntry`; express `faithfulContent`'s 60+ kind branches as
a per-kind painter table in `ContentRender`; route the ~6 disjoint `match …Kind` sites through the single
registry; extend the completeness oracle to cover the painter.

**Independent Test**: Run the controls/faithful-content/inspection/catalog-completeness suites; every
catalog kind renders the same faithful geometry through the painter as through the old `match`; the 6
former sites resolve via one table read each; the oracle fails loudly if any catalog kind lacks a painter.

- [~] T022 [US3] (DEFERRED — see readiness/us3-us4-decision.md: painter-table genericity) Express the per-kind painter in `src/Controls/ControlKindRegistry.fsi`/`.fs`. **Prefer a sibling `painters: Map<string, Painter>` over a `Painter` field on `ControlKindEntry`**: the current `ControlKindEntry` is **non-generic** (metadata only), so a `Painter: Theme -> Rect -> Control<'msg> -> Scene list` field would force the record (and the registry `Map` + every existing metadata reader) to become `ControlKindEntry<'msg>` — an unwanted genericity ripple. Record this as the FR-005 deviation rationale in `topology-confirm.md`. The field-on-entry form MAY be used only if a non-generic painter shape (e.g. boxing/existential wrapper) makes it clean. Keep the existing metadata predicates (`isRich`/`isChart`/`chartSource`/`layoutRow`/`hasScrollAffordance`/`virtualizationOf`/`inspectionNodeKind`/`surfaceRole`/`a11yRole`) at their early compile site (D2)
- [~] T023 [US3] (DEFERRED — see readiness/us3-us4-decision.md: painter-table genericity) In `src/Controls/ContentRender.fs` (created byte-identical in T018), **convert** `faithfulContent`'s inline kind `match` into the per-kind **painter table** bound here (after geometry compiles, D2); each painter closes over the inline per-kind arg extraction (`chartValues control`, `styleClassesOf`, `visualStateOf`, label/intent) so per-kind float/dispatch order is byte-preserved (C2). `faithfulContent` keeps its name/shape and resolves via one painter lookup; a non-catalog runtime kind returns the SAME default the pre-refactor `match | _ ->` produced (FR-005/FR-007). Update `ContentRender.fsi` only if its internal surface changes. Depends on T018, T022
- [X] T024 [US3] Route the remaining disjoint `match …Kind` sites through the single registry table (FR-006, data-model §4): `Control.fs` faithful dispatch (now via painter), the kind-string matches at `Control.fs` ~L112/259/354/537 (read registry metadata where it is the *same* fact — but RETAIN `required` validation at L353 and genuine slot-region structure matches at their sites per FR-012), `ControlRuntime.fs:375` (confirm single `hasScrollAffordance` read), `Catalog.fs:501`, `Inspection.fs:70` (use registry `inspectionNodeKind`/`surfaceRole`), `RetainedRender.fs:1820` (confirm single `virtualizationOf` read) — eliminate the disjoint faithful-geometry/kind-metadata switches
- [X] T025 [US3] Extend the catalog↔registry completeness oracle in `tests/Controls.Tests/Feature183KindRegistryTests.fs` so that, in addition to both-directions key-equality, it asserts **every catalog kind resolves a `Painter`** — fails loudly if any kind lacks a painter entry (FR-007/SC-007/C4). Verify the test goes RED before the painter table is complete and GREEN after (and stays red when an entry is temporarily deleted)
- [X] T026 [US3] `dotnet build FS.GG.Rendering.slnx -c Release` clean (compile order already set in T019; `ContentRender` sits after geometry per C1), then validate: `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "KindRegistry|Catalog|Inspection|Rendering|Fingerprint"` — every catalog kind renders equivalent faithful geometry through the painter (byte-identical where dispatch order is preserved; else within the reviewed golden-hash delta recorded in `golden-hash-review.md`); the 6 former sites resolve via one table read each (SC-003); the oracle fails loudly on a missing painter (SC-007)

**Checkpoint**: US1–US3 complete — geometry, hash, and kind-dispatch all decomposed; cross-file
exhaustiveness drift eliminated; completeness machine-enforced. These three stories stand on their own.

---

## Phase 6: User Story 4 — Collapse the 30 tail-constructor bodies behind `Control.Helpers` (Priority: P4, CONDITIONAL)

**Goal**: Collapse the duplicated bodies of the ~30 public tail modules (`TextBlock`…`Overlay`,
`Control.fs` L3358–3511) behind a shared data-driven `Control.Helpers` routine while preserving every
public `create`/`text` surface as thin delegations. **Ships ONLY if it nets a real line reduction**
(FR-008 / D6); otherwise dropped without blocking US1–US3.

**Independent Test**: Build the full solution; run the controls construction / public-surface suites;
every public tail-module entry point produces an identical `Control` value, the surface baseline is
unchanged for the tail modules, and `Control.fs` is net-smaller (else the slice is reverted).

- [~] T027 [US4] (DEFERRED — conditional; SC-001 already met, see readiness/us3-us4-decision.md) Implement a data-driven `Control.Helpers` routine in `src/Controls/Control.fs` and delegate the ~30 tail modules' (`TextBlock`/`Label`/`Image`/`Icon`/`Separator`/`Badge`/`Button`/…/`Overlay`) `create`/`text` bodies to it as thin delegations — preserving every public module/function name and signature (surface-neutral by construction; acceptance #1)
- [~] T028 [US4] (DEFERRED — conditional; SC-001 already met, see readiness/us3-us4-decision.md) Measure the line delta (`git diff --stat src/Controls/Control.fs`): if it nets a real reduction, keep it and validate `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release --filter "PublicSurface|TypedControlContract"` (each `create`/`text` yields an identical `Control` value; tail-module surface diff empty); if indirection ≥ duplication removed, **revert US4** — US1–US3 stand alone (FR-008/SC-001 footnote; acceptance #2). Record the ship/drop decision in `specs/189-control-module-split/readiness/us4-decision.md`

**Checkpoint**: US4 shipped (net reduction) or cleanly dropped, with the decision recorded. No impact on
US1–US3.

---

## Phase 7: Polish & Cross-Cutting Concerns (Final Gates)

**Purpose**: Whole-solution regression, surface-drift / version-bump gate, golden-hash review sign-off,
line-count check, and phase-feedback capture.

- [X] T029 Run the full suite green: `DISPLAY=:1 dotnet test FS.GG.Rendering.slnx -c Release` — red/green set matches the T001 baseline except explicitly reviewed golden-hash expected-output updates; no assertion weakened, no test skipped/deleted (FR-011/SC-005)
- [X] T030 Re-run the comprehensive baseline runner (`DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/189-control-module-split/readiness/final-tests.md`) to cover `Package.Tests` + samples that the solution test omits; diff against `T001` baseline
- [X] T031 Surface-drift / version-bump gate: `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff readiness/surface-baselines/FS.GG.UI.Controls.txt` — target EMPTY ⇒ no bump; if non-empty, review the diff and bump `FS.GG.UI.Controls` (and update `readiness/surface-baselines/FS.GG.UI.Controls.txt`) per FR-010/SC-006. Confirm `tests/Package.Tests/SurfaceAreaTests.fs` green. **Confirm FR-013 (no new dependency)**: `git diff src/Controls/Controls.fsproj` shows ONLY added `<Compile>` entries — no new `<PackageReference>`/`<ProjectReference>` and no new project added to `FS.GG.Rendering.slnx` (contract C7)
- [X] T032 [P] Finalize `specs/189-control-module-split/readiness/golden-hash-review.md`: every hash/fingerprint delta (if any) carries an explicit reviewed "intentional reorder, breaks no picture-cache invariant" sign-off (SC-008); if zero deltas, record "byte-identical, no deltas"
- [X] T033 [P] Confirm SC-001: `Control.fs` and each extracted file (`ControlPrimitives`, `ChartGeometry`, `WidgetGeometry`, `SceneHash`, `ContentRender`, `LayoutEval`, `NodeAssembly`) are at/below the ~1,500-line guideline (down from ~3,513); record the line-count table in `specs/189-control-module-split/readiness/line-counts.md`
- [X] T034 [P] Capture per-phase feedback via the `fs-gg-feedback-capture` skill into `specs/189-control-module-split/feedback/` (process friction, generalizable-code candidates, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — must run FIRST (FR-014: baseline before any edit).
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. The compile probe (T006) and
  prelude extraction (T009) gate every later extraction.
- **User Stories (Phases 3–6)**: Depend on Foundational. **Sequenced, not parallel** — US3 routes into
  US1's geometry; US2's hash must be stable before US3's behavior-sensitive routing; US4 is last/conditional.
- **Polish (Phase 7)**: Depends on US1–US3 complete (US4 optional).

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational. Independent. The MVP and the dispatch floor for US3.
- **US2 (P2)**: Starts after US1. Extracts `SceneHash`/`LayoutEval`/`NodeAssembly` **and relocates
  `faithfulContent` byte-identically into `ContentRender`** (T018) — required because `NodeAssembly`
  calls `faithfulContent`, which must compile *before* it (C1); deferring the relocation to US3 would
  create a forward-reference back-edge the stub compile probe (T006) cannot catch.
- **US3 (P3)**: Starts after US1+US2 — its painter table dispatches to US1 geometry and its routing is
  the most behavior-sensitive, so it lands on already-stable groupings. It **transforms** the
  `ContentRender` relocated in US2 (T018→T023) into the painter table rather than creating the file.
- **US4 (P4)**: Last; conditional on a measured net reduction; never blocks US1–US3.

### Within Each User Story

- `.fsi` seam (drafted in Foundational T008) → body move → `Controls.fsproj` order update → build →
  test/byte-equality validation.
- Producers before consumers (INV-ORDER): `ControlPrimitives` → geometry → `ContentRender` → `NodeAssembly`.

### Parallel Opportunities

- Setup: T002, T003, T004 run in parallel after T001.
- US1: T010 (chart geom) and T012 (widget geom) are different files → parallel; T011 (`withPoints`)
  depends on T010.
- US2: T016 (`SceneHash`) and T017 (`LayoutEval`) are different files → parallel; T018 (`NodeAssembly`)
  depends on both.
- Polish: T032, T033, T034 run in parallel.
- Stories themselves are NOT parallel (strict risk sequencing US1→US2→US3→US4).

---

## Parallel Example: User Story 1

```bash
# After Foundational (ControlPrimitives extracted), move the two geometry groupings in parallel:
Task: "Move chart *Geom producers + emptyState/pillGeom into src/Controls/ChartGeometry.fs"   # T010
Task: "Move widget/layout/container *Geom producers into src/Controls/WidgetGeometry.fs"       # T012
# Then (depends on T010): add withPoints and route the ~17 chart guards through it             # T011
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup — capture the pre-refactor baseline (FR-014).
2. Phase 2 Foundational — compile probe + live smoke run + `.fsi` seams + `ControlPrimitives` (CRITICAL,
   blocks all stories; converts the ordering hypothesis to a checked fact).
3. Phase 3 US1 — extract `ChartGeometry`/`WidgetGeometry` + `withPoints`.
4. **STOP and VALIDATE**: byte-identical scenes, empty surface diff, `Control.fs` shrank.
5. Ship the MVP increment.

### Incremental Delivery

1. Setup + Foundational → topology proven, prelude extracted.
2. US1 → byte-identical geometry move → validate → ship (MVP).
3. US2 → hash/layout/assembly extraction → golden-hash gate → ship.
4. US3 → registry painter + 6 sites + oracle → validate → ship (Pattern-A core).
5. US4 → conditional tail collapse → ship iff net reduction, else drop.
6. Polish → full suite, surface/version gate, golden-hash sign-off, line-count, feedback.

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- This is a structural refactor: the regression gate is the **existing** suites + `hashScene`/fingerprint
  byte-equality + semantic artifact diff + the §7 golden-hash review (FR-015). No new golden-image harness.
- The ONLY new test asset is the painter-coverage extension of `Feature183KindRegistryTests.fs` (T025).
- Every new module is `module internal` + a paired `.fsi`; nothing reaches the public surface. Target is
  an EMPTY `FS.GG.UI.Controls` surface diff; version bump iff non-empty and reviewed.
- Preserve internal names on move (`renderScene`, `paintNode`, `evaluateLayout`, `hashScene`,
  `faithfulContent`, `required`, `chartValues`, `measureText`) so callers resolve unchanged.
- Commit after each task or logical group; stop at any checkpoint to validate the story independently.
