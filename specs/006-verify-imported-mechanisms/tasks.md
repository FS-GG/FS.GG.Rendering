---
description: "Task list for Verify Imported Rendering & Controls Mechanisms"
---

# Tasks: Verify Imported Rendering & Controls Mechanisms

**Input**: Design documents from `/specs/006-verify-imported-mechanisms/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests note**: This feature *is* a verification/audit effort — the audit tests (Expecto + FsCheck, `Audit:`-prefixed, in `Audit_*.fs`) and the two Markdown deliverables under `docs/audit/` are the product. There is therefore no separate "write tests first" sub-phase; the test code below is the implementation work itself. No new product F# module, no new project, no new NuGet, no new `.fsi` (plan §Technical Context).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 (inventory), US2 (correctness), US3 (effectiveness), US4 (report)
- Exact file paths are included in every task

## Path conventions

- Audit tests: `tests/{Controls,Layout,Scene,Elmish,SkiaViewer}.Tests/Audit_*.fs` (existing projects; they already carry `InternalsVisibleTo`)
- Durable deliverables: `docs/audit/mechanism-inventory.md`, `docs/audit/mechanism-audit.md`
- Capability-tier evidence: existing harness `tests/Rendering.Harness` (`offscreen`/`perf`/`live-x11`)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up the audit artifacts and confirm the toolchain + filter convention work.

- [X] T001 Create `docs/audit/` and seed `docs/audit/mechanism-inventory.md` (Claim-row table headers per `specs/006-verify-imported-mechanisms/contracts/claim-record.md` + Verification columns per `contracts/verification-record.md`) and `docs/audit/mechanism-audit.md` (Verdict-row headers + coverage-summary footer stub per `contracts/verdict-record.md`)
- [X] T002 Verify the solution builds clean: `dotnet build FS.GG.Rendering.slnx -c Release`
- [X] T003 [P] Confirm the audit filter convention runs end-to-end by invoking `dotnet test FS.GG.Rendering.slnx -c Release -- --filter "Audit"` (expected: zero matches now, exit 0 — proves the `Audit:`-prefix filter selects the audit subset once tests exist; quickstart §2)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Wire the `Audit_*.fs` files into each test project and prove the verification seams (oracle flags, work-reduction counters, harness subcommands) are actually reachable from the test/harness paths. Every per-mechanism test in US2/US3 lives in these files, so this phase blocks US2 and US3. (US1 is doc-only and may proceed in parallel with this phase.)

**⚠️ CRITICAL**: No US2/US3 test task can begin until the relevant scaffold task here is complete.

- [X] T004 [P] Scaffold + wire Controls.Tests audit files into `tests/Controls.Tests/Controls.Tests.fsproj` — create empty `Audit_Reconcile.fs`, `Audit_MemoCache.fs`, `Audit_PictureCache.fs`, `Audit_TextCache.fs`, `Audit_Fingerprint.fs`, `Audit_AnimationClock.fs`, each with one `Audit:` sanity test asserting the oracle seams (`MemoEnabled`/`PictureCacheEnabled`/`TextCacheEnabled` in `Controls/RetainedRender.fsi`) and `WorkReductionRecord`/`FrameMetrics` counters are accessible (internals visible)
- [X] T005 [P] Scaffold + wire `tests/Layout.Tests/Audit_IncrementalLayout.fs` into `tests/Layout.Tests/Layout.Tests.fsproj` — sanity test asserting `Layout.evaluate`/`evaluateIncremental` (`Layout/Layout.fsi`) and `RemeasuredNodeCount`/`Invalidated` counters are reachable
- [X] T006 [P] Scaffold + wire `tests/Scene.Tests/Audit_AnimationSampling.fs` into `tests/Scene.Tests/Scene.Tests.fsproj` — sanity test asserting `Animation.applyAt`/`sampleFrames` (`Scene/Animation.fsi`) are reachable
- [X] T007 [P] Scaffold + wire Elmish.Tests audit files into `tests/Elmish.Tests/Elmish.Tests.fsproj` — create `Audit_AnimationTickGating.fs`, `Audit_DamageTracking.fs`, `Audit_Virtualization.fs`, each with an `Audit:` sanity test asserting `AnimationTick.tickSubscription` (`Elmish/AnimationTick.fsi`) and the `DirtyRectCount`/`DirtyArea`/`VirtualMaterialized`/`VirtualTotal` counters are reachable
- [X] T008 [P] Scaffold + wire `tests/SkiaViewer.Tests/Audit_ReplayCache.fs` into `tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` — sanity test asserting `PictureReplayCache.create enabled:false` and `stats` (`SkiaViewer/PictureReplayCache.fsi`) are reachable; mark GL-dependent assertions to degrade-and-disclose when no `DISPLAY`
- [X] T009 Confirm capability-tier evidence engine is callable for the timing/live claims: `dotnet run --project tests/Rendering.Harness -- offscreen`, `-- perf --mode paced-60 --frames 10`, `-- live-x11` each return a parseable result (run.json on a capable runner, or exit 0 with `status:"skipped"` + `SkipReason` headless) — records which tiers are available in this environment for US3/T036

**Checkpoint**: Audit files compile, seams proven reachable, harness tiers probed — US2 and US3 can proceed.

---

## Phase 3: User Story 1 - Auditable claims inventory (Priority: P1) 🎯 MVP

**Goal**: A complete inventory listing every advertised mechanism as a falsifiable claim with its source reference and an initial `unverified` status (`docs/audit/mechanism-inventory.md`).

**Independent Test**: Cross-check the inventory against the plan's mechanism table and the imported source; confirm each of the 14 mechanisms appears with name, restated falsifiable claim, `path.fsi:line` source, and status `unverified`; ambiguous claims flagged "needs sharpening". Delivers a standalone audit map before any verification test exists.

> All US1 tasks edit the single file `docs/audit/mechanism-inventory.md`, so they are **sequential** (no [P]).

- [X] T010 [US1] Add Claim rows for keyed-reconciliation (`Controls/Reconcile.fsi` `diff`/`apply`) and scene-fingerprint (`RetainedRender.fsi` `hashScene`) to `docs/audit/mechanism-inventory.md`, each with restated claim + `path.fsi:line` source + `unverified` status
- [X] T011 [US1] Add Claim rows for the three caches — memo (`RetainedRender.fsi MemoEnabled`), picture (`PictureCacheEnabled`), text (`TextCacheEnabled`) — including both a `correctness` (parity) and an `effectiveness` claim per cache, to `docs/audit/mechanism-inventory.md`
- [X] T012 [US1] Add Claim rows for incremental-layout (`Layout/Layout.fsi` `evaluate`/`evaluateIncremental`; correctness=equivalence, effectiveness=`RemeasuredNodeCount`≪baseline) to `docs/audit/mechanism-inventory.md`
- [X] T013 [US1] Add Claim rows for the animation mechanisms — animation-clock (`RetainedRender.fsi advance`/`clockActive`/`sampleOnPaint`), declarative animation-sampling (`Scene/Animation.fsi applyAt`/`sampleFrames`), animation-tick-gating (`Elmish/AnimationTick.fsi tickSubscription`) — to `docs/audit/mechanism-inventory.md`
- [X] T014 [US1] Add Claim rows for damage-rect tracking (`RetainedRender.fsi DirtyRectCount`/`DirtyArea`) and virtualization (`VirtualMaterialized`/`VirtualTotal`) to `docs/audit/mechanism-inventory.md`
- [X] T015 [US1] Add Claim rows for backend replay-cache (`SkiaViewer/PictureReplayCache.fsi create enabled`/`stats`) to `docs/audit/mechanism-inventory.md`
- [X] T016 [US1] Add Claim rows for the capability-tier mechanisms — present-mode selection (`SkiaViewer/PresentMode.fsi ViewerPresentMode`) and frame-rate cap (`SkiaViewer/SkiaViewer.fsi FrameRateCap`) — with Kind `timing`/`liveness`, Verification Method `harness-timing`, in `docs/audit/mechanism-inventory.md`
- [X] T017 [US1] Completeness + ambiguity pass on `docs/audit/mechanism-inventory.md`: verify all 14 plan-table mechanisms have ≥1 Claim row (SC-001), mark `Advertised=inferred` where no explicit claim exists, and annotate any non-falsifiable statement "needs sharpening" (US1 AS2) rather than recording a vague pass

**Checkpoint**: Inventory is a complete, reviewable map; every mechanism present with a falsifiable claim and source; nothing `verified` yet.

---

## Phase 4: User Story 2 - Behavioral correctness verified against real code (Priority: P1)

**Goal**: Each correctness claim is backed by a test that exercises the real imported code, is shown to be transparent (on==off), and has **proven discriminating power** (goes red when the mechanism is bypassed) — SC-003.

**Independent Test**: For each correctness-bearing mechanism, run optimization-on vs bypassed on identical inputs and assert identical output; then flip the oracle/bypass and confirm the assertion fails. Deterministic mechanisms run headless.

> Each task below edits a different `Audit_*.fs` file, so they are mutually [P]. Each depends on its Phase-2 scaffold (T004–T008).

- [X] T018 [P] [US2] In `tests/Controls.Tests/Audit_Reconcile.fs`: discriminating-power + adversarial keyed/positional/kind-mismatch tests — `diff`-then-`apply` reproduces target tree across generated tree pairs (FsCheck), and a deliberately-broken apply turns the assertion red (FR-005, SC-003)
- [X] T019 [P] [US2] In `tests/Controls.Tests/Audit_MemoCache.fs`: parity test (`MemoEnabled` true vs false → identical scene) + proof it goes red when memo returns a stale entry, plus cache-key-completeness adversarial inputs differing only in a render-affecting field (FR-004, FR-009)
- [X] T020 [P] [US2] In `tests/Controls.Tests/Audit_PictureCache.fs`: parity test (`PictureCacheEnabled` on vs off) with discriminating proof + present-but-dead check (counters provably move on a representative scene; report dead if they never move) (FR-004, FR-010, D5)
- [X] T021 [P] [US2] In `tests/Controls.Tests/Audit_TextCache.fs`: parity test (`TextCacheEnabled` on vs off) + key-completeness adversarial across text+family+size+weight (a single-field difference must miss and return correct fresh measurement) (FR-004, FR-009)
- [X] T022 [P] [US2] In `tests/Controls.Tests/Audit_Fingerprint.fs`: determinism across repeated runs + collision probe over single-field render-affecting diffs (`hashScene` must differ) (FR-007, edge case "determinism violations")
- [X] T023 [P] [US2] In `tests/Controls.Tests/Audit_AnimationClock.fs`: determinism + clamp (no overshoot past endpoint) for `advance`/`sampleOnPaint`, and `clockActive` correctly gates redraw (FR-007)
- [X] T024 [P] [US2] In `tests/Layout.Tests/Audit_IncrementalLayout.fs`: equivalence test — `evaluateIncremental` geometry equals full `evaluate` of the same final tree across generated change sets, with discriminating proof (FR-006, SC-003)
- [X] T025 [P] [US2] In `tests/Scene.Tests/Audit_AnimationSampling.fs`: determinism of `applyAt` at fixed time points + settled-animation byte-identity to the equivalent static scene (identity-at-rest) (FR-007, spec US2 AS4)
- [X] T026 [P] [US2] In `tests/Elmish.Tests/Audit_DamageTracking.fs`: union-area correctness — overlapping dirty rects counted once; geometry of union matches expected (correctness portion; effectiveness added in US3)
- [X] T027 [P] [US2] In `tests/SkiaViewer.Tests/Audit_ReplayCache.fs`: parity test — `PictureReplayCache.create enabled:true` vs `false` produce identical output; degrade-and-disclose (recorded `skipped` + required tier) when no GL/`DISPLAY` (FR-004, FR-011)
- [X] T028 [US2] Record each US2 Verification into `docs/audit/mechanism-inventory.md`: set Result, set `Discriminating Proof=true` only where red-when-bypassed was demonstrated, transition Status `unverified`→`verified`/`refuted` (per `contracts/verification-record.md` rule 1, SC-003)

**Checkpoint**: Every correctness claim has a discriminating test (or is recorded as a finding); inventory correctness rows resolved.

---

## Phase 5: User Story 3 - Performance claims measured, not assumed (Priority: P2)

**Goal**: Each work-reduction claim measured enabled-vs-disabled via the existing counters by a meaningful margin; correct-but-ineffective mechanisms exposed as no-ops; timing/live claims deferred-and-disclosed to their tier (FR-008, SC-004, SC-005).

**Independent Test**: Drive the three canonical scenarios (localized change in a large tree; repeated unchanged render; collection larger than viewport) and assert the relevant counter beats the disabled baseline by the claimed threshold; equal-to-baseline ⇒ recorded no-op.

> Each task edits a different `Audit_*.fs` file (mutually [P]); each appends to the file its US2 counterpart created, so it depends on that US2 task (e.g. T029 after T019).

- [X] T029 [P] [US3] In `tests/Layout.Tests/Audit_IncrementalLayout.fs`: effectiveness — single localized change in a large tree ⇒ `RemeasuredNodeCount`/`Invalidated` ≪ full-`evaluate` baseline; record margin (FR-008, US3 AS1)
- [X] T030 [P] [US3] In `tests/Controls.Tests/Audit_MemoCache.fs`: effectiveness — repeated unchanged render ⇒ `MemoHits` near-100% steady-state, recomputation→0; record hit-rate margin vs disabled (FR-008, US3 AS2)
- [X] T031 [P] [US3] In `tests/Controls.Tests/Audit_PictureCache.fs`: effectiveness — `PictureCacheHits` reaches steady-state ≫0 while `PictureCacheMisses`→0 across repeated frames; record margin (FR-008)
- [X] T032 [P] [US3] In `tests/Controls.Tests/Audit_TextCache.fs`: effectiveness — repeated text-measure ⇒ high text-cache hit-rate; record margin vs disabled (FR-008)
- [X] T033 [P] [US3] In `tests/Elmish.Tests/Audit_DamageTracking.fs`: effectiveness — localized change ⇒ `DirtyArea`/`DirtyRectCount` a small fraction of full-repaint baseline; record margin (FR-008, US3 AS1)
- [X] T034 [P] [US3] In `tests/Elmish.Tests/Audit_Virtualization.fs`: effectiveness — collection larger than viewport ⇒ `VirtualMaterialized` bounded by viewport need, not `VirtualTotal`; record margin (FR-008, US3 AS3)
- [X] T035 [P] [US3] In `tests/Elmish.Tests/Audit_AnimationTickGating.fs`: effectiveness — no tick requested by `tickSubscription` when no clock is active; ticks requested when one is (FR-008, edge case "correctly a no-op" vs broken)
- [X] T036 [P] [US3] In `tests/SkiaViewer.Tests/Audit_ReplayCache.fs`: effectiveness via `stats` Hits/Records steady-state; degrade-and-disclose (`skipped` + required tier) when no GL (FR-008, FR-011)
- [X] T037 [US3] Capability-tier runs for present-mode + frame-rate-cap via `tests/Rendering.Harness` — `offscreen` (T1 pixel), `perf --mode paced-60` (T3 pacing), `live-x11` (T2): on a capable runner capture `artifacts/harness/run-*/run.json`; headless record `deferred` + `SkipReason` naming the tier — never `pass` (FR-011, D6, US3 AS5)
- [X] T038 [US3] Record each US3 Verification into `docs/audit/mechanism-inventory.md`: effectiveness rows get a `Margin` and classification realized / no-op / deferred (rule 2 + 3 of `contracts/verification-record.md`); skipped rows get `Skip Rationale` + tier (SC-004, SC-005)

**Checkpoint**: Every work-reduction claim measured or deferred-with-rationale; no-ops surfaced; inventory effectiveness rows resolved; no Claim remains `unverified` (SC-002).

---

## Phase 6: User Story 4 - Findings report with severity and recommendations (Priority: P2)

**Goal**: `docs/audit/mechanism-audit.md` — one verdict per mechanism with evidence, severity for divergences, recommendation, and a reproducible reference (FR-013, FR-014).

**Independent Test**: Read the report; confirm every inventoried mechanism has exactly one verdict, an evidence pointer, a severity where divergent, a concrete recommendation, and a `Reproduce` command.

> US4 tasks edit the single file `docs/audit/mechanism-audit.md` and depend on US1–US3 evidence, so they are sequential.

- [X] T039 [US4] Derive one Verdict row per mechanism into `docs/audit/mechanism-audit.md` (`works-as-advertised` / `benefit-overstated` / `not-working-or-no-op` / `unverifiable-here`) applying the verdict-derivation rules in `contracts/verdict-record.md`, citing the backing Verifications as Evidence
- [X] T040 [US4] Add Severity (`correctness-defect` > `silent-no-op` > `overstated-benefit` > `cosmetic`) and Recommendation (`fix`/`simplify`/`remove`/`re-scope-claim`/`defer-to-tier` + detail) for every divergent mechanism in `docs/audit/mechanism-audit.md` (FR-013, SC-006)
- [X] T041 [US4] Add a `Reproduce` field to every Verdict row (the `--filter "Audit: …"` test selector or the harness command) so any verdict is reproducible without further guidance (FR-014, SC-007)
- [X] T042 [US4] Append the coverage-summary footer to `docs/audit/mechanism-audit.md` per `contracts/verdict-record.md` — N audited; counts of works-as-advertised / overstated / no-op (with #correctness-defects + #silent-no-ops) / unverifiable; and "Discriminating-power confirmed for all correctness passes: yes/no" (SC-008)
- [X] T043 [US4] Verify closure: every mechanism has exactly one verdict and no Claim in `docs/audit/mechanism-inventory.md` remains `unverified` (SC-002); reconcile any mismatch between inventory and report

**Checkpoint**: The audit is a decision surface — every mechanism judged, evidenced, and reproducible.

---

## Phase 7: Polish & Cross-Cutting Concerns

- [X] T044 [P] Run the full deterministic audit subset headless and confirm it fits the inner-loop budget: `dotnet test FS.GG.Rendering.slnx -c Release -- --filter "Audit"` (a red `Audit:` test is a *finding*, not a build to green by weakening assertions — Principle V)
- [X] T045 [P] Walk the quickstart §5 validation checklist against the produced artifacts (discriminating proof on all correctness passes; margins recorded; capability-absent checks deferred-with-tier; severities + recommendations present; coverage footer accurate)
- [X] T046 Final consistency pass: confirm `docs/audit/mechanism-inventory.md` ↔ `docs/audit/mechanism-audit.md` cross-references resolve, all source `path.fsi:line` refs are accurate, and no synthetic substitute is used without in-line + report disclosure (FR-012, FR-015)

---

## Dependencies & Execution Order

### Phase dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup; blocks US2 and US3 (the `Audit_*.fs` files must exist + seams proven reachable). US1 (doc-only) does **not** depend on Phase 2 and may run alongside it.
- **US1 (Phase 3, P1)**: independent doc work; needs only the inventory skeleton (T001).
- **US2 (Phase 4, P1)**: each task needs its Phase-2 scaffold; benefits from US1 claim IDs but does not require them to start coding.
- **US3 (Phase 5, P2)**: each task appends to the file created by its US2 counterpart → depends on that US2 task (T029←T019-style); harness task T037 depends on T009.
- **US4 (Phase 6, P2)**: depends on US1–US3 evidence (the recording tasks T028/T038 in particular).
- **Polish (Phase 7)**: depends on all desired stories complete.

### Story independence

- **US1** delivers standalone value (the audit map) with zero test code.
- **US2** is independently testable: correctness + discriminating power for the deterministic mechanisms, no effectiveness or report needed.
- **US3** is independently testable: counter margins + capability-tier deferral, no report needed.
- **US4** turns accumulated evidence into verdicts; independently checkable for completeness.

### Parallel opportunities

- Setup: T003 [P].
- Foundational: T004–T008 all [P] (different projects/files).
- US1: sequential (single file).
- US2: T018–T027 all [P] (different files), once their scaffolds exist; T028 after them.
- US3: T029–T037 all [P] (different files), each after its US2 counterpart; T038 after them.
- US4: sequential (single file).
- Polish: T044, T045 [P].

---

## Parallel Example: User Story 2

```bash
# After Phase 2 scaffolds (T004–T008), launch the correctness/adversarial tests together:
Task: "Audit_Reconcile.fs discriminating-power + adversarial in tests/Controls.Tests/"
Task: "Audit_MemoCache.fs parity + key-completeness in tests/Controls.Tests/"
Task: "Audit_PictureCache.fs parity + present-but-dead in tests/Controls.Tests/"
Task: "Audit_TextCache.fs parity + key-completeness in tests/Controls.Tests/"
Task: "Audit_Fingerprint.fs determinism + collision in tests/Controls.Tests/"
Task: "Audit_IncrementalLayout.fs equivalence in tests/Layout.Tests/"
Task: "Audit_AnimationSampling.fs settled-identity in tests/Scene.Tests/"
Task: "Audit_ReplayCache.fs parity (degrade-disclose) in tests/SkiaViewer.Tests/"
```

---

## Implementation Strategy

### MVP first (User Story 1 only)

1. Phase 1: Setup (T001–T003).
2. Phase 3: US1 inventory (T010–T017) — can run alongside Phase 2.
3. **STOP and VALIDATE**: the inventory alone converts "complicated, untested code" into a reviewable list of falsifiable claims with sources. Shippable as the audit map.

### Incremental delivery

1. Setup + Foundational → seams proven reachable.
2. US1 → inventory (MVP).
3. US2 → correctness + discriminating power (the floor: caches transparent, reconcile/layout faithful).
4. US3 → effectiveness margins + capability-tier deferral (exposes no-ops / overstated benefit).
5. US4 → verdicts + severities + recommendations + coverage summary (the decision surface).

### Notes

- [P] = different files, no incomplete-task dependency.
- A red `Audit:` test is a successful audit outcome (a Finding), never something to green by weakening an assertion (Constitution Principle V).
- Capability-absent ⇒ `skipped`/`deferred` with rationale + tier, never `pass` (Principle VI).
- No product runtime behavior changes — oracle flags + instrumentation stay on test/harness paths only.
