---
description: "Task list for Type-Safety Hardening (Code-Health Refactoring Phase 6)"
---

# Tasks: Type-Safety Hardening (Code-Health Refactoring Phase 6)

**Input**: Design documents from `/specs/183-type-safety-hardening/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓ (behavior-invariance, kind-registry, scenenode-codec, flag-records)

**Tests**: This is a byte-stable representation/typing refactor. Test coverage is the **existing** Release `*.Tests.fsproj` sweep run at the **same** red/green as baseline, plus exactly **two** new compiler-symmetry guard tests the spec mandates (catalog↔registry completeness — SC-001; every-case codec round-trip — SC-002). No other new tests.

**Organization**: Tasks grouped by user story (US1/US2/US3) so each is independently implementable and shippable; all three share **one** baseline captured up front.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1, US2, US3 (Setup/Foundational/Polish carry no story label)
- Exact file paths included in every task

## Path Conventions

Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). Units of change: `src/Controls/` (US1 + Controls side of US2/US3), `src/Scene/` (US2 + US3), `src/SkiaViewer/` (US3). Baseline/evidence artifacts under `specs/183-type-safety-hardening/readiness/`.

---

## Phase 1: Setup (Baseline Capture — Shared Infrastructure)

**Purpose**: Capture the one shared pre-edit baseline that gates all three stories. **No code edits in this phase.**

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test project via the discovery runner (`scripts/baseline-tests.fsx` globs every `*.Tests.fsproj` incl. release-only `Package.Tests` and the sample lanes) so pre-existing reds are known up front, not discovered at merge. Do NOT hand-pick a subset.

- [X] T001 Create the readiness tree: `mkdir -p specs/183-type-safety-hardening/readiness/{baseline,post-change}`
- [X] T002 Build clean + snapshot the 12 surface baselines: `dotnet build FS.GG.Rendering.slnx -c Debug` then `dotnet fsi scripts/refresh-surface-baselines.fsx`; confirm `git diff --quiet -- readiness/surface-baselines` is clean (records the pre-edit public-surface oracle for FR-006/§B)
- [X] T003 Establish the no-regression test baseline: `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/183-type-safety-hardening/readiness/baseline/test-baseline.md` (full Release sweep; expect the known reds — `Package.Tests` 8-fail + `ControlsGallery` 2-fail + 14 greens — recorded as the FR-005/§A.5 red/green set)
- [X] T004 [P] Capture the **behavior corpus** into `specs/183-type-safety-hardening/readiness/baseline/`: `SceneNode` codec wire bytes for one value of **all 25 cases** (US2 §A.1 hard gate), scene hashes/fingerprints for the control/scene corpus (US1/US2 §A.2), and `damageRegion`/`validateDamage`/`classifyWindowObservation`/`promotionDecision`/`damageRegionSet` outputs for fixed inputs (US3 §A.4)

---

## Phase 2: Foundational (Blocking Prerequisites — GATE)

**Purpose**: Lock the acceptance contract every story is gated on. **No code edits in this phase.**

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **Early-live-smoke resolution = N/A (per plan.md).** This feature carries **no defect/root-cause hypothesis** — it is a representation/typing refactor that must not change any observed behavior. The plan template's early-live-smoke clause is therefore resolved as **N/A**; the standing live-run requirement is satisfied by **baseline capture** (Phase 1) + the per-story byte-diff gates. T005 records this resolution explicitly so the omission is intentional, not dropped.

- [X] T005 Record the gate decisions in `specs/183-type-safety-hardening/readiness/baseline/known-reds.md`: (a) the allowed pre-existing reds (`Package.Tests` 8-fail, `ControlsGallery` 2-fail — stale-feed, cross-ref `specs/182-god-module-splits/readiness/baseline/known-reds.md`) flagged baseline-not-regression; (b) the early-live-smoke clause resolved **N/A** with rationale (byte-stable refactor, no root-cause hypothesis)
- [X] T006 Confirm the two simultaneously-binding invariants from `contracts/behavior-invariance.md` are understood and the capture/diff commands (§D) are runnable: **(A)** behavior byte-stable (codec bytes, scene hashes, evidence, damage/diagnostics, same test red/green) and **(B)** surface change intentional/minimal/exact (only `FS.GG.UI.Scene` + `FS.GG.UI.SkiaViewer` may move; the other 10 baselines unchanged)

**Checkpoint**: Baseline captured, known-reds + N/A resolution recorded, behavior+surface contract locked — US1/US2/US3 may now proceed (in priority order; US2 before US3 since both touch `src/Scene/`).

---

## Phase 3: User Story 1 - Single-source control `Kind` registry (Priority: P1) 🎯 MVP

**Goal**: Collapse the ~13 parallel `Kind`-keyed dispatch sites into one **internal** registry table keyed by the existing `Control.Kind: string`, restoring exhaustiveness. Tier 2 — **no public surface change, no bump**.

**Independent Test**: Build `FS.GG.UI.Controls`, run `Controls.Tests`; every control kind's painter output, required-attribute validation, pretty name, a11y role, layout traits, inspection node-kind/surface-role/clip, virtualization counts are byte-identical to baseline; `Control.fsi` + `FS.GG.UI.Controls.txt` unchanged.

### Tests for User Story 1 (mandated guard — SC-001)

- [X] T007 [US1] Add the catalog↔registry completeness test in `tests/Controls.Tests/` asserting `registry` keys == the `Catalog.fs` kind set **both directions** (a kind in one but not the other fails) — the restored exhaustiveness guard (SC-001). Write it to FAIL until T008/T009 exist.

### Implementation for User Story 1

- [X] T008 [US1] Create `src/Controls/ControlKindRegistry.fs` with `type internal ControlKindEntry = { Painter; RequiredAttributes; ChartSeriesKey; IsRich; IsChart; LayoutRowKind; HasScrollAffordance; Virtualization; InspectionNodeKind; SurfaceRole; ClipsContent; A11yRole }` (+ `internal VirtualizationRole = Grid | GridRow`), the eager module-level `internal registry : Map<string, ControlKindEntry>` (~98 entries derived from the Catalog SSOT), and `internal tryEntry` — per `data-model.md §1`. **NOT** in any `.fsi` (internal dispatch only)
- [X] T009 [US1] Insert `ControlKindRegistry.fs` into `src/Controls/Controls.fsproj` `<Compile Include>` order **before** `Control.fs`/`Inspection.fs`/`Accessibility.fs`/`Catalog.fs`/`ControlRuntime.fs`/`RetainedRender.fs` (no back-edge, FR-011)
- [X] T010 [US1] Migrate the `Control.fs` dispatch sites (@502 chart series routing, @606/613, @1930 `faithfulContent`→`emptyState` default, @2050, @2157 `directionOf` Row-vs-Column, @2351/2356 chart-clip, @2413, plus `richFamilies`/`chartFamilies`/`prettyKind`) to `registry`/`tryEntry` lookups in `src/Controls/Control.fs`, preserving each site's **exact** current default on `None` (painter→`emptyState`, direction→`Column`, etc.)
- [X] T011 [P] [US1] Migrate the inspection dispatch in `src/Controls/Inspection.fs` (@48 `kindOf`→`Custom` default, @68 `surfaceRoleOf`→`Content`, @89 `clipStatusOf`) to registry lookups; leave the `@161 Kind.Contains("transform")` substring test **inline** (not a kind-key lookup, per `contracts/kind-registry.md`)
- [X] T012 [P] [US1] Migrate `src/Controls/Accessibility.fs` (@28 `roleFor`→`Custom` default) to a registry `A11yRole` lookup
- [X] T013 [P] [US1] Migrate the required-attribute validation in `src/Controls/Catalog.fs` (@501) to the registry `RequiredAttributes` lookup, keeping `Catalog.fs` as the kind SSOT the registry derives from
- [X] T014 [P] [US1] Migrate `src/Controls/ControlRuntime.fs` (@373) to its registry lookup, preserving current default behavior
- [X] T015 [P] [US1] Migrate `src/Controls/RetainedRender.fs` (@1732 `countVirtual` virtualization) to the registry `Virtualization` lookup; confirm the registry is read (not rebuilt) on the `countVirtual`/`paintLeaf` hot paths — no per-frame allocation (contract §4)
- [X] T016 [US1] Build + verify behavioral equivalence: `dotnet build FS.GG.Rendering.slnx -c Debug`; `DISPLAY=:1 dotnet test tests/Controls.Tests/Controls.Tests.fsproj -c Release` green incl. T007; diff control scene-hash/fingerprint/inspection/a11y/virtualization outputs vs `baseline/` (byte-identical, §A); `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff --quiet -- readiness/surface-baselines 'src/Controls/**/*.fsi'` MUST be clean (no `.fsi`/baseline change — §B, no bump; the `.fs` edits + new `ControlKindRegistry.fs` are expected and not part of this gate)

**Checkpoint**: US1 fully functional, independently shippable, public baseline unchanged — MVP (SC-001/004/005). Stop and validate.

---

## Phase 4: User Story 2 - Compiler-enforced `SceneNode` codec symmetry (Priority: P2)

**Goal**: Drive `writeSceneNode`/`readSceneNode` from one per-case table (a missing case = build/test failure) and normalize the 19 bare-tuple `SceneNode` cases to **named fields preserving exact arity/types** over the **frozen** wire format. Tier 1 — bump `FS.GG.UI.Scene`.

**Independent Test**: Build `FS.GG.UI.Scene`, run the Scene + codec round-trip suites; every `SceneNode` case serializes to **byte-identical** bytes and round-trips identically to baseline; `Scene.fsi` diff is only the planned field names; the whole solution (consumers, samples, template, generated products) still compiles (source-compatible).

### Tests for User Story 2 (mandated guard — SC-002)

- [X] T017 [US2] Add the **every-case codec round-trip** test in `tests/Scene.Tests/` constructing one value of **all 25 cases**, asserting `deserialize (serialize x) = x` **and** bytes == the captured `baseline/` codec bytes; also assert the table has exactly 25 rows with contiguous tags `0..24` (read-side symmetry guard, FR-002/SC-002). Write to FAIL until T019/T020 land.

### Implementation for User Story 2

- [X] T018 [US2] FSI-first (Constitution I): edit `src/Scene/Scene.fsi` to add named fields to the 19 bare-tuple `SceneNode` cases per `data-model.md §3` (e.g. `Rectangle of bounds:(float*float*float*float) * fill:Color`), arity/types preserved, 6 already-named cases untouched
- [X] T019 [US2] Apply the matching named-field normalization to the `SceneNode` DU in `src/Scene/Scene.fs` (@391); **do not** flatten inner tuples or retype `(float*float*float*float)`→`Rect` (FR-010 / Out of Scope) — positional construction/matching must stay valid
- [X] T020 [US2] Convert `writeSceneNode`/`readSceneNode` in `src/Scene/SceneCodec.fs` to a per-case table: `type private SceneNodeCodecRow = { Tag; Write; Read }`, 25 rows in frozen tag order (0–24), Write stays an exhaustive `match node` (tag + payload; `FS0025`-as-error = compile gate), Read dispatches via `readerByTag`; retain the `| tag -> failwithf` wildcard **only** for genuinely-corrupt/unknown tags. Internal — not in `SceneCodec.fsi`. Per `data-model.md §2`
- [ ] T021 [P] [US2] (Optional — SKIPPED; the 3 `*Option` helpers already delegate to generic `writeOption`/`readOption`, no dedup gain, byte risk) Fold the 3 `writeXOption`/`readXOption` near-clones into generic `writeOption`/`readOption` in `src/Scene/SceneCodec.fs`; **revert immediately if any byte perturbs** (byte-stability wins, contract §6)
- [X] T022 [US2] Bump `<Version>` in `src/Scene/Scene.fsproj` (from `0.1.36-preview.1`) per FR-007
- [X] T023 [US2] Verify codec byte-stability + symmetry + whole-tree recompile: `dotnet build FS.GG.Rendering.slnx -c Debug` (full solution compiles — source-compatible DU); `DISPLAY=:1 dotnet test tests/Scene.Tests/Scene.Tests.fsproj -c Release` green incl. T017 (codec bytes byte-identical to `baseline/`); `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff -- readiness/surface-baselines src/Scene/Scene.fsi` shows **only** the planned DU field names (§B)

**Checkpoint**: US1 + US2 both work independently; `SceneNode` codec is compiler-symmetric; Scene bumped; codec bytes byte-stable.

---

## Phase 5: User Story 3 - Named flag records replace boolean traps (Priority: P3)

**Goal**: Replace the positional `bool`/positional tails of the six trap functions with small **named** records (each flag named at the call site; a transposition becomes a compile error) — values passed and results unchanged. Tier 1 — bump `FS.GG.UI.SkiaViewer` (Scene already bumped by US2).

**Independent Test**: Build `FS.GG.UI.SkiaViewer`/`FS.GG.UI.Scene`/`FS.GG.UI.Controls`, run their suites + damage/diagnostics lanes; every damage-validation verdict, window observation, damage region, promotion decision, popover geometry is byte-identical to baseline; each surface diff contains only the planned signature + new record type.

### Implementation for User Story 3

- [X] T024 [US3] FSI-first (Constitution I): draft the **public** signatures + record types in `.fsi` — `DamageValidationFlags` for `validateDamage` in `src/SkiaViewer/Host/OpenGl.fsi` (@299), `WindowObservationInputs` for `classifyWindowObservation` in `src/SkiaViewer/SkiaViewer.fsi` (@118), `DamageNodeCounts` (3 int counters) for `damageRegion` in `src/Scene/Scene.fsi` (@1276) — per `data-model.md §4`
- [X] T025 [P] [US3] Implement `DamageValidationFlags` (5 bools) + convert `validateDamage` in `src/SkiaViewer/Host/OpenGl.fs` (@522) and update the internal call site (@562); identical verdict for fixed inputs
- [X] T026 [P] [US3] Implement `WindowObservationInputs` (2 bool + 2 bool option) + convert `classifyWindowObservation` in `src/SkiaViewer/SkiaViewer.fs` (@935); update the test call sites (`tests/SkiaViewer.Tests/Tests.fs:399,444`)
- [X] T027 [US3] Implement `DamageNodeCounts` + convert `damageRegion` in `src/Scene/Scene.fs` (@2000) grouping **only** the 3 transposable int counters (leave ids/cause/threshold as named params — minimal change; if grouping ripples into emitted JSON/markdown shape, retain & record per FR-010)
- [X] T028 [US3] Update the **cross-package** `damageRegion` call at `src/Controls/Inspection.fs` (@460) + the ~6 test files to the new `DamageNodeCounts` signature; `FS.GG.UI.Controls` public surface stays unchanged → Controls **not** bumped (contract §2)
- [X] T029 [P] [US3] Convert the `internal` `promotionDecision` (`src/Controls/RetainedRender.fs:768`) to `PromotionInputs` and `damageRegionSet` (`@731`) to `DamageSetInputs`; update the internal call site (`@756`) + test sites (`Feature147/148/149*`); `internal` → **no public baseline change**
- [X] T030 [P] [US3] Convert the `private` `popoverGeom` (`src/Controls/Control.fs:1755`) — `withActions:bool` → a small private record or 2-case `PopoverKind` DU (pick clearer at the 3 sites @2009/2010/2011); private → no surface change
- [X] T031 [US3] Bump `<Version>` in `src/SkiaViewer/SkiaViewer.fsproj` (from `0.1.46-preview.1`) per FR-007 (Scene already bumped in T022)
- [X] T032 [US3] Verify behavior + exact surface: `dotnet build FS.GG.Rendering.slnx -c Debug`; `DISPLAY=:1 dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` + `tests/Controls.Tests/Controls.Tests.fsproj -c Release` green; diff damage/diagnostic/promotion/popover outputs vs `baseline/` (byte-identical, §A.4); `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff -- readiness/surface-baselines 'src/**/*.fsi'` shows **only** Scene + SkiaViewer planned record types — `Controls.txt` unchanged (§B)

**Checkpoint**: All three stories independently functional; only Scene/SkiaViewer surfaces moved (as planned); Scene + SkiaViewer bumped.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Full-sweep verification, feed/sample/template alignment, success-criteria sign-off.

- [X] T033 Full-solution sweep: `dotnet build FS.GG.Rendering.slnx -c Release` then `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/183-type-safety-hardening/readiness/post-change/test-baseline.md`; confirm the **same** red/green as `baseline/test-baseline.md` (Package.Tests + ControlsGallery only — SC-006)
- [X] T034 Align feed + actively-maintained sample for the bumped packages: `dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase` then `DISPLAY=:1 dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release` (FR-007 / SC-007; template inherits versions at generation)
- [X] T035 [P] Capture `post-change/` behavior corpus (codec bytes, scene hashes/fingerprints, damage/diagnostics) and diff against `baseline/`: byte-identical across all three stories (SC-004 / §A)
- [X] T036 [P] Confirm the surface envelope: only `FS.GG.UI.Scene.txt` + `FS.GG.UI.SkiaViewer.txt` changed (planned record/field names), the other 10 baselines unchanged; dependency graph acyclic & unchanged — no new project/dependency/inter-project reference (SC-005 / SC-008 / FR-011)
- [X] T037 [P] Record every FR-010 retention (any site/case/grouping left explicit) with rationale in `specs/183-type-safety-hardening/readiness/post-change/retentions.md`
- [X] T038 Run the `quickstart.md` validation end-to-end and confirm SC-001…SC-008 all hold; mark the feature done

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately. T004 [P] may run alongside T002/T003 once T001 exists.
- **Foundational (Phase 2)**: Depends on Setup (needs the captured baseline). **BLOCKS all stories.** No code edits.
- **User Stories (Phase 3–5)**: All depend on Foundational. Independently shippable. Sequenced US1 → US2 → US3 (highest payoff/lowest surface risk first); **US2 before US3** because both edit `src/Scene/` — serialize them for one clean `FS.GG.UI.Scene.txt` diff each (US2 = field names, US3 = `damageRegion`).
- **Polish (Phase 6)**: Depends on all desired stories complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on US2/US3. Internal-only, no bump → the MVP.
- **US2 (P2)**: After Foundational. Independent of US1. Bumps Scene.
- **US3 (P3)**: After Foundational. Independent of US1/US2, but **share the `src/Scene/` edit window with US2** — land US2 first so each Scene surface diff is clean. Bumps SkiaViewer (+ Scene, already bumped by US2).

### Within Each Story

- US1: completeness test (T007, FAIL-first) → registry file (T008) → fsproj order (T009) → migrate sites (T010–T015) → verify (T016).
- US2: round-trip test (T017, FAIL-first) → `.fsi` (T018) → DU `.fs` (T019) → codec table (T020) → verify (T023).
- US3: `.fsi` seams (T024) → per-function conversions (T025–T030) → bump (T031) → verify (T032).
- FSI-first: public-surface seam (`.fsi`) drafted before the `.fs` implementation (Constitution I/II).

### Parallel Opportunities

- **Setup**: T004 [P] runs alongside T002/T003.
- **US1 migrations**: T011/T012/T013/T014/T015 [P] are different files — parallel after T008/T009/T010; T010 (`Control.fs`) is the largest and sequenced first.
- **US2**: T021 [P] (option-codec fold) is independent of the table once T020 lands.
- **US3**: T025 (SkiaViewer OpenGl), T026 (SkiaViewer), T029 (Controls internal), T030 (Controls private) [P] are different files; T027/T028 (`damageRegion` + its cross-package caller) are sequenced together.
- **Polish**: T035/T036/T037 [P] are independent verification artifacts.

---

## Parallel Example: User Story 1 migrations

```bash
# After ControlKindRegistry.fs (T008), fsproj order (T009), and Control.fs (T010):
Task: "Migrate Inspection.fs dispatch to registry in src/Controls/Inspection.fs"        # T011
Task: "Migrate Accessibility.fs roleFor to registry in src/Controls/Accessibility.fs"   # T012
Task: "Migrate Catalog.fs required-attrs to registry in src/Controls/Catalog.fs"        # T013
Task: "Migrate ControlRuntime.fs dispatch to registry in src/Controls/ControlRuntime.fs"# T014
Task: "Migrate RetainedRender.fs countVirtual to registry in src/Controls/RetainedRender.fs" # T015
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup (capture the shared baseline).
2. Phase 2 Foundational (lock known-reds + behavior/surface contract; early-live-smoke = N/A).
3. Phase 3 US1 (internal Kind registry).
4. **STOP and VALIDATE**: Controls byte-identical, `FS.GG.UI.Controls.txt` unchanged, no bump — independently shippable.

### Incremental Delivery

1. Setup + Foundational → baseline ready.
2. US1 → validate → ship (MVP, no bump).
3. US2 → validate (codec bytes byte-stable, Scene bumped) → ship.
4. US3 → validate (damage/diagnostics byte-stable, SkiaViewer bumped) → ship.
5. Polish → feed/sample/template aligned, SC-001…SC-008 signed off.

### Notes

- [P] = different files, no dependency on an incomplete task.
- Behavior byte-stability (§A) is the non-negotiable gate; when it conflicts with type-safety, **byte-stable behavior wins** (FR-005) — retain that part per FR-010 and record why.
- Surface may move **only** for `FS.GG.UI.Scene` + `FS.GG.UI.SkiaViewer`, **only** as planned (§B).
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
- No new project/dependency/inter-project reference (FR-011); the one new file (`ControlKindRegistry.fs`) lives inside `src/Controls/`.
