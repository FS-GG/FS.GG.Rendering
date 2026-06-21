---
description: "Task list for Backward-Compatibility Shim Removal"
---

# Tasks: Backward-Compatibility Shim Removal

**Input**: Design documents from `/specs/184-backcompat-cleanup/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓, quickstart.md ✓

**Tests**: No TDD/contract tests are requested by the spec. The only new tests are the two **focused
byte-stability snapshots** the contracts require for the production-path removals (US2 overlay chain +
fingerprint, US4 typed `chartValues`); they appear as implementation tasks inside those stories. All
other test work is **retarget/delete** of existing tests (FR-008).

**Organization**: Tasks grouped by user story (P1→P4). Each story is independently shippable and shares
**one** baseline captured up front (mirrors features 179–183).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 / US4
- Exact file paths are included in each task

## Path Conventions

Single-solution F# multi-project layout (`FS.GG.Rendering.slnx`). All production edits live in
`src/Controls/` (+ `src/Controls.Elmish/` for US3 writers). Tests in `tests/Controls.Tests/` and
`tests/Elmish.Tests/`. Local readiness artifacts in `specs/184-backcompat-cleanup/readiness/`
(`.gitignore`'d per repo convention). All commands run from repo root; GL tests need `DISPLAY=:1`.

> **⚠ Cross-story serialization (do not parallelize stories).** US1, US2, US3, and US4 **all edit
> `src/Controls/Control.fs`** (US2 @2398–2402, US4 @482–483, US1 @3083/3326, US3 @3408/3412/3415/3503).
> Land them **sequentially** (US1 → US2 → US3 → US4) so each `Control.fs`/`.fsi` diff is one reviewable
> change. `[P]` markers below apply **within** a story (distinct test files), never across stories.

---

## Phase 1: Setup (Shared Infrastructure & Baseline)

**Purpose**: Create the readiness scaffold and capture the single pre-edit baseline every story is gated on.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project. The solution (`dotnet test FS.GG.Rendering.slnx`) deliberately omits `tests/Package.Tests`
> (release-only — owns the public-surface gate) and the `samples/**/*.Tests` projects (package-feed
> consumers) — exactly where prior surprises hid. Use the discovery-based runner
> (`scripts/baseline-tests.fsx`), which globs `*.Tests.fsproj` so nothing silently drops out.

- [X] T001 Create the local readiness scaffold: `mkdir -p specs/184-backcompat-cleanup/readiness/{baseline,post-change}` (per plan.md Project Structure; `.gitignore`'d).
- [X] T002 Snapshot the 12 surface baselines before any edit: `dotnet build FS.GG.Rendering.slnx -c Debug` then `dotnet fsi scripts/refresh-surface-baselines.fsx`, and copy `readiness/surface-baselines/` aside (or `git stash`) so the empty-diff check in Polish has a clean reference (quickstart §1a).
- [X] T003 Establish the no-regression baseline across EVERY test project: `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/184-backcompat-cleanup/readiness/baseline/test-baseline.md` (expect `Package.Tests` 8-fail, `ControlsGallery` 2-fail, 14 others green — research cross-cutting).
- [X] T004 Record the allowed pre-existing reds in `specs/184-backcompat-cleanup/readiness/baseline/known-reds.md` (copy/adapt from `specs/183-…/readiness/baseline/known-reds.md`): `Package.Tests` 8-fail + `ControlsGallery` 2-fail are stale-feed, **baseline-not-regression** (FR-011 / SC-004).

---

## Phase 2: Foundational (Blocking Prerequisites — GATE)

**Purpose**: Capture the production-path behavior baselines and confirm the descope/tier facts every
story depends on. **No code edits in this phase.**

**⚠️ CRITICAL**: No user-story work begins until this phase is complete.

> **Early-live-smoke clause resolved as N/A (per plan.md).** This feature carries **no defect/root-cause
> hypothesis** — it is a deletion/migration of dead compatibility surface that must not change observed
> behavior on retained paths. The plan template's standing "early live smoke run" is therefore replaced,
> as the plan directs, by the **behavior-baseline capture** tasks below (overlay modifier chain +
> fingerprint, typed-chart `chartValues`) recorded *before any edit*.

- [X] T005 Record the US2 overlay-path behavior baseline: build an overlay control through the production path (`compositionEntriesForControl`, `Control.fs:2398-2402`) and snapshot its normalized `ModifierEntry list` + `Composition.fingerprint` into `specs/184-backcompat-cleanup/readiness/baseline/overlay-chain.txt` (the byte-stability reference for US2 / FR-005 / contract I1).
- [X] T006 Record the US4 typed-chart behavior baseline: snapshot `chartValues` output (the `ChartPoint list`) for every typed-front-door chart in the test corpus into `specs/184-backcompat-cleanup/readiness/baseline/chartvalues.txt` (the byte-stability reference for US4 / FR-005 / contract I1).
- [X] T007 Re-verify the US4 descope gate (FR-004): scan `src/`, all 4 `samples/`, and the template for any author of flat `float list`/`float array` chart data. Expect **zero** (research D4). If any author is found → **drop US4**, record the finding in `readiness/baseline/us4-descope.md` (Acceptance 4.3), break nothing.
- [X] T008 Confirm the per-item tier and lock the removal-invariance oracle: record in `readiness/baseline/tiers.md` that US1+US3 are Tier 1 (public `.fsi` field removal → bump + ledger) and US2+US4 are Tier 2 (internal → no bump/ledger), per research D1 and `contracts/removal-invariance.md` (I1–I4).
- [X] T008a Re-confirm the FR-009 "no consumer" premise **at HEAD, per item** for US1/US2/US3 (T007 already covers US4) — scan `src/`, all 4 `samples/`, and the template: US1 → exactly 3 test-only readers, no src/sample/template reader; US2 → the single overlay caller `Control.fs:2398-2402`, no other production caller; US3 → the ~7 src readers + dual-set writers enumerated in plan.md (none in samples/template beyond re-pin). Record the per-item consumer scan in `readiness/baseline/consumers.md`. If any unexpected consumer is found → migrate-first or descope, never break (FR-009).

**Checkpoint**: Behavior baselines captured, US4 descope confirmed, tiers locked, FR-009 consumer premise re-confirmed per item (T007/T008a) — story implementation can begin (sequentially, US1 → US4).

---

## Phase 3: User Story 1 - Remove the `MaxOffset` scroll alias (Priority: P1) 🎯 MVP

**Goal**: Expose the vertical scroll maximum through exactly one field (`MaxVerticalOffset`); remove the
read-only `MaxOffset` duplicate from the public `ScrollViewport`.

**Independent Test**: Remove `MaxOffset`; retarget the 3 consuming tests to `MaxVerticalOffset`; build +
full sweep stays at baseline parity; `git diff src/Controls/Control.fsi` shows only the `MaxOffset` line gone.

- [X] T009 [US1] Delete `MaxOffset: float` from the `ScrollViewport` record in `src/Controls/Control.fsi:283` and update the doc-comment at `Control.fsi:272-273` to drop the `MaxOffset` mention (keep `Offset`).
- [X] T010 [US1] Delete the `MaxOffset` field in `src/Controls/Control.fs:3083` and the `MaxOffset = extent.MaxVerticalOffset` assignment at `src/Controls/Control.fs:3326`.
- [X] T011 [P] [US1] Retarget the reader in `tests/Controls.Tests/Feature150ScrollViewerExtentTests.fs:16` from `MaxOffset` to `MaxVerticalOffset`.
- [X] T012 [P] [US1] Retarget the reader in `tests/Controls.Tests/Feature151ScrollViewerCorpusTests.fs:36` from `MaxOffset` to `MaxVerticalOffset`.
- [X] T013 [P] [US1] Retarget the reader in `tests/Controls.Tests/Feature137ClippingTests.fs:162` from `MaxOffset` to `MaxVerticalOffset`.
- [X] T014 [US1] Build + verify: `dotnet build FS.GG.Rendering.slnx -c Release`, run the 3 affected `tests/Controls.Tests` projects under `DISPLAY=:1`, and confirm `git diff src/Controls/Control.fsi` shows **only** the `MaxOffset: float` line removed (contract I2; defer the bump/ledger to Polish).

**Checkpoint**: `ScrollViewport` exposes one vertical-max field; 3 tests pass via `MaxVerticalOffset`; `.fsi` diff is one clean line. (SC-001/002/004)

---

## Phase 4: User Story 2 - Retire the `Composition` legacy node-form layer (Priority: P2)

**Goal**: Remove the internal `LegacyForm` compatibility layer so each modifier has one expression,
after migrating the single overlay caller onto the modern modifier IR **byte-stably**.

**Independent Test**: Migrate the one production caller off `legacyLower`; delete the legacy layer + the
Feature-140 legacy-compat tests; confirm the overlay modifier chain + `Composition.fingerprint` are
byte-identical to the T005 baseline and the full sweep stays at parity.

- [X] T015 [US2] **Migrate the caller first** — in `src/Controls/Control.fs:2398-2402` (`compositionEntriesForControl`), replace `Composition.legacyLower Composition.LegacyOverlay` with the literal it produces: `[ { Composition.Source = Composition.LegacyOverlaySource; Composition.Effect = Composition.LayerHint "overlay" } ]` (contract US2 edit 1; byte-stable per research D3).
- [X] T016 [US2] Add/confirm a focused byte-stability test in `tests/Controls.Tests/` that builds an overlay control and asserts its normalized `ModifierEntry list` + `Composition.fingerprint` equal the T005 baseline (`readiness/baseline/overlay-chain.txt`) — the FR-005 / I1 oracle for US2.
- [X] T017 [US2] Delete `LegacyForm`, `LegacyCompatibilityStatus`, `legacyLower`, and `compatibilityEvidence` from `src/Controls/Composition.fsi:125-139` and `src/Controls/Composition.fs:367-399`. **Retain** `ModifierSource.LegacyOverlaySource` and the other `ModifierSource.Legacy*Source` cases (FR-010; required for overlay byte-stability — research D3).
- [X] T018 [US2] Delete `tests/Controls.Tests/Feature140LegacyCompatibilityTests.fs` (asserts the removed legacy-form lowering — FR-008, delete not weaken).
- [X] T019 [US2] Verify the other `Feature140*` tests (ZOrder, ModifierLayer, ModifierNormalization, PortalLayer, LegacyCacheTextOverlay) do **not** reference `LegacyForm`/`legacyLower`/`compatibilityEvidence`; if any do, migrate or delete the specific assertion (no weakening — contract US2 edit 4).
- [X] T020 [US2] Build + verify: `dotnet build FS.GG.Rendering.slnx -c Release`, run the `Controls.Tests` project under `DISPLAY=:1`, and confirm the overlay chain + fingerprint **byte-diff = identical** vs T005 baseline, and **no** public `.fsi` change (internal module — no bump/ledger).

**Checkpoint**: Overlay path produces a byte-identical chain with no `legacy*` helper; legacy layer + its tests gone; no public surface moved. (SC-001/003/004/007)

---

## Phase 5: User Story 3 - Retire the `ControlEvent.Payload` string-compat field (Priority: P3)

**Goal**: Remove the stringly-typed `Payload : string option` so control events have one typed
representation (`Nav : NavPayload option`), after migrating every reader and stopping the dual-set writers.

**Independent Test**: Migrate every `Payload` reader to the typed `Nav` (or a typed accessor); stop
dual-setting in the writers; remove the field; confirm event/widget/navigation behavior is unchanged at
baseline parity; `git diff src/Controls/Types.fsi` shows only the `Payload` line (+ any accessor).

- [X] T021 [US3] **Decide the typed accessor (FSI-first, Principle I).** If multiple readers need the same projection, add one typed accessor to `src/Controls/Types.fsi` (e.g. `val navValue: ControlEvent -> float option`, `val navItem: ControlEvent -> string option`) backed by `Nav`, and implement it in `src/Controls/Types.fs`; otherwise readers decode `ev.Nav` directly. Keep it typed — never reconstruct a string (contract US3 edit 1).
- [X] T022 [P] [US3] Migrate the `onPayload` readers to typed `Nav`: `src/Controls/Interactive2.fs:6`, `src/Controls/Navigation2.fs:6`, `src/Controls/DataEntry2.fs:6` (decode the relevant `NavPayload` case per data-model reader map).
- [X] T023 [P] [US3] Migrate the widget-lowering readers to typed `Nav`: `src/Controls/Widgets/WidgetLowering.fs:21` (`onString` → `MovedSelection` item) and `:26` (`onStringList` → typed list source).
- [X] T024 [P] [US3] Migrate the data-grid + container readers to typed `Nav`: `src/Controls/Widgets/DataGridWidget.fs:40` (`MovedCell(row,col)`) and `src/Controls/Widgets/Containers.fs:59` (`SteppedValue value`).
- [X] T025 [US3] Migrate the `Control.fs` readers to typed `Nav`: `src/Controls/Control.fs:3408/3412/3415` (`onChangedBool/Float/String` → `SteppedValue`) and `:3503` (menu `onSelected` → `MovedSelection(index, Some item)`).
- [X] T026 [US3] Stop dual-setting `Payload` in the Elmish writers — `src/Controls.Elmish/ControlsElmish.fs` `dispatchBindings`@412/426-427, `dispatchNav`@941, and `:558/863/954`: construct `ControlEvent` with `Nav` only.
- [X] T027 [US3] Stop dual-setting `Payload` in `src/Controls/OverlayState.fs:537`: construct `ControlEvent` with `Nav` only.
- [X] T028 [US3] **Remove the field** — delete `Payload: string option` from `src/Controls/Types.fsi:312-322` and `src/Controls/Types.fs:252-257` (do this only after T021–T027 land).
- [X] T029 [P] [US3] Migrate the test readers in `tests/Controls.Tests/TypedMigrationTests.fs:337/357` to read `Nav` instead of `Payload`.
- [X] T030 [P] [US3] Migrate the test readers in `tests/Elmish.Tests/Feature100NavigationTests.fs:113/177/197` to assert via `Nav`.
- [X] T031 [P] [US3] Migrate the test reader in `tests/Elmish.Tests/Feature144ProductOwnedVisibilityTests.fs:23` to assert via `Nav`.
- [X] T032 [US3] Build + verify: `dotnet build FS.GG.Rendering.slnx -c Release`, run `Controls.Tests` + `Elmish.Tests` under `DISPLAY=:1`, and confirm `git diff src/Controls/Types.fsi` shows **only** the `Payload` line removed (+ any deliberate accessor `val`) and `Controls.Elmish` public `.fsi` is unchanged (writers internal — recompile/re-pin, no Elmish bump). (contract I2)

**Checkpoint**: Every handler reads the moved item/value from the typed `Nav`; no code reads a string `Payload`; `Types.fsi` diff is one clean line (+ accessor). (SC-001/002/004/007)

---

## Phase 6: User Story 4 - Remove the untyped flat-chart authoring fallback (Priority: P4)

**Goal**: Remove the `float list`/`float array` chart fallback so chart values have a single typed
source — **only** because T007 confirmed zero in-tree flat-list authors.

**Independent Test**: Delete the fallback arms + the one fallback test; confirm `chartValues` output for
the typed path is byte-identical to the T006 baseline and the full sweep stays at parity.

> **Descope gate:** proceed only if T007 found zero flat-list authors. If T007 found any, **skip this
> phase**, the story is descoped, and the finding stands recorded (FR-004 / Acceptance 4.3).

- [X] T033 [US4] Delete the two fallback arms in `src/Controls/Control.fs:482-483` (`| UntypedValue(:? (float list) …)` and `| UntypedValue(:? (float array) …)`), keep the typed arms `479-481`, and update the doc-comment `469-471` to drop the flat-list mention (contract US4 edit 1).
- [X] T034 [US4] Delete the one fallback test `tests/Controls.Tests/Feature080ExtractionTests.fs:62-71` ("flat float-list fallback still extracts (legacy authoring)") — FR-008, delete not weaken.
- [X] T035 [US4] Build + verify: `dotnet build FS.GG.Rendering.slnx -c Release`, run `Controls.Tests` under `DISPLAY=:1`, and confirm the typed `chartValues` output **byte-diff = identical** vs the T006 baseline (`readiness/baseline/chartvalues.txt`); no public surface change (internal — no bump/ledger). (contract I1/I2)

**Checkpoint**: Typed-front-door charts read byte-identically; flat fallback + its test gone. (SC-001/003/004)

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Land the single public bump (covers US1+US3), the ledger, feed/sample alignment, the
surface confirmation, the post-change parity capture, and the FR-010 retention record.

- [X] T036 Full clean build + sweep: `dotnet build FS.GG.Rendering.slnx -c Debug` then `dotnet fsi scripts/refresh-surface-baselines.fsx` (regenerates the per-module baselines **and** the public-surface union — FR-006); confirm `git diff readiness/surface-baselines/` is **empty** (type-granular oracle — research D2/contract I2). A non-empty baseline diff means an unintended type-level change → blocks.
- [X] T037 Bump `FS.GG.UI.Controls` `0.1.45-preview.1 → 0.1.46-preview.1`: edit `<Version>` in `src/Controls/Controls.fsproj` (one bump covers US1 + US3).
- [X] T038 Re-pin the consumer: update `src/Controls.Elmish/Controls.Elmish.fsproj` to reference `FS.GG.UI.Controls 0.1.46-preview.1` (recompile/re-pin only — no Elmish bump; writers are internal).
- [X] T039 Write `specs/184-backcompat-cleanup/readiness/compatibility-ledger.md` (format per `specs/147-…/readiness/compatibility-ledger.md`): **US1** entry (removed `ScrollViewport.MaxOffset`; migrate to `MaxVerticalOffset`; surface delta = `Control.fsi` diff) and **US3** entry (removed `ControlEvent.Payload : string option`; read typed `Nav : NavPayload option`; surface delta = `Types.fsi` diff). US2/US4 are Tier 2 → no entry.
- [X] T040 Align the feed + actively-maintained sample: `dotnet fsi scripts/dev-repack.fsx --sample samples/SecondAntShowcase` (pack → local feed, re-pin, restore), then `DISPLAY=:1 dotnet test samples/SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj -c Release`; confirm zero sample breakage and update any template package pins that reference `FS.GG.UI.Controls` (SC-006).
- [X] T041 Record the FR-010 retentions in `specs/184-backcompat-cleanup/readiness/post-change/retentions.md`: `ModifierSource.LegacyOverlaySource` (+ other `Legacy*Source`), the widget `*.create` builders, the SkiaViewer `LegacyHostMsg` pump, and the `-v1`/`-v2` identity tags — each with its live-despite-name rationale.
- [X] T042 Capture post-change parity: `DISPLAY=:1 dotnet fsi scripts/baseline-tests.fsx --config Release --out specs/184-backcompat-cleanup/readiness/post-change/test-baseline.md`, then `diff specs/184-backcompat-cleanup/readiness/{baseline,post-change}/test-baseline.md`; confirm the **same** red/green set (`Package.Tests` 8-fail, `ControlsGallery` 2-fail, 14 green — no new red, no flipped green). (FR-011 / SC-004)
- [X] T043 Final acceptance pass: verify SC-001…SC-007 against the captured evidence (four identities removed/descoped with reason; surface strictly smaller; production paths byte-identical; baseline parity; bump+ledger for US1/US3; samples/template pass; no weakened tests) and confirm the dependency graph is unchanged and no new project/dependency/reference was added (FR-011).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup (needs the built solution + baseline) — **BLOCKS all stories**.
- **User Stories (Phases 3–6)**: All depend on Foundational. **Serialized** (US1 → US2 → US3 → US4) because all four edit `src/Controls/Control.fs` — they are *not* run in parallel.
- **Polish (Phase 7)**: Depends on all in-scope stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on other stories. MVP.
- **US2 (P2)**: After Foundational. Independent of US1, but serialized after it (shared `Control.fs`).
- **US3 (P3)**: After Foundational. Independent, but serialized after US2 (shared `Control.fs`; largest blast radius — land last among public edits).
- **US4 (P4)**: After Foundational **and** gated on T007's zero-author confirmation. Serialized after US3.

### Within Each User Story

- US1: surface/impl edits (T009–T010) → test retargets (T011–T013, parallel) → verify (T014).
- US2: migrate caller (T015) → byte-stability test (T016) → delete layer + tests (T017–T019) → verify (T020). Migrate-before-delete is mandatory.
- US3: accessor decision (T021) → reader migrations (T022–T025) + writer migrations (T026–T027) → **then** remove field (T028) → test migrations (T029–T031, parallel) → verify (T032).
- US4: delete arms + test (T033–T034) → verify (T035).

### Parallel Opportunities

- **Within US1**: T011, T012, T013 (three distinct test files).
- **Within US3**: T022, T023, T024 (distinct reader files) can run together; T025 is separate (`Control.fs`). After T028, test migrations T029, T030, T031 (distinct files) run together.
- **No cross-story parallelism** — all four stories share `src/Controls/Control.fs`.

---

## Parallel Example: User Story 1 test retargets

```bash
# After the US1 surface/impl edits (T009–T010), retarget the three readers together:
Task: "Retarget MaxOffset → MaxVerticalOffset in tests/Controls.Tests/Feature150ScrollViewerExtentTests.fs:16"
Task: "Retarget MaxOffset → MaxVerticalOffset in tests/Controls.Tests/Feature151ScrollViewerCorpusTests.fs:36"
Task: "Retarget MaxOffset → MaxVerticalOffset in tests/Controls.Tests/Feature137ClippingTests.fs:162"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Complete Phase 1: Setup (readiness scaffold + the single pre-edit baseline).
2. Complete Phase 2: Foundational (behavior baselines + US4 descope confirm + tier lock) — replaces the
   template's early-live-smoke with baseline capture, per plan.md.
3. Complete Phase 3: User Story 1 (remove `MaxOffset`).
4. **STOP and VALIDATE**: `.fsi` diff is one clean line; 3 tests pass via `MaxVerticalOffset`; sweep at parity.
5. This validates the whole pipeline (surface diff + byte-stability discipline) end-to-end.

### Incremental Delivery

1. Setup + Foundational → baseline locked.
2. US1 → validate → (MVP).
3. US2 → overlay byte-diff identical → validate.
4. US3 → typed migration, `Types.fsi` one clean line → validate.
5. US4 → typed `chartValues` byte-diff identical → validate.
6. Polish → one `Controls` bump (US1+US3), ledger, feed/sample align, post-change parity.

Each story is a separate, independently reviewable commit in priority order (P1 → P4).

---

## Notes

- `[P]` = different files, no dependencies — safe to run together.
- `[Story]` label maps each task to its user story for traceability.
- Migrate-then-delete is mandatory for US2 (T015 before T017) and US3 (T021–T027 before T028).
- Tests for removed behavior are **deleted, not weakened** (FR-008): `Feature140LegacyCompatibilityTests.fs` (US2), `Feature080ExtractionTests.fs:62-71` (US4).
- Byte-stability is the safety bar for production-path removals (US2 overlay, US4 chart); any drift blocks the removal — narrow or descope, never ship drift (contract I1).
- US2/US4 are Tier 2 (internal) → **no bump, no ledger**; only US1/US3 bump `FS.GG.UI.Controls` (research D1).
- Commit after each story or logical group; stop at any checkpoint to validate independently.
