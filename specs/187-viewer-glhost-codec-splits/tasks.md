---
description: "Task list for feature 187 — Viewer + GlHost + SceneCodec module splits"
---

# Tasks: Viewer + GlHost + SceneCodec Module Splits (Pattern E + A)

**Input**: Design documents from `/specs/187-viewer-glhost-codec-splits/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/internal-contracts.md ✅, quickstart.md ✅

**Tests**: This is a **Tier 2 behavior-preserving refactor**. Per spec Assumptions + research R5, **no new
behavioral tests are authored** — the pre-refactor baseline plus the existing suites
(`Feature146`/`Feature183` byte+symmetry oracles, viewer/host/responsiveness/smoke suites, `SurfaceAreaTests`)
are the full regression oracle. Internal-helper unit tests pinning the extracted seams are *optional/additive*,
never required. Each user story therefore ends in a **diff-against-baseline verification** task, not a TDD pair.

**Organization**: Tasks are grouped by user story (US1=P1, US2=P2, US3=P3) so each is implemented, verified, and
shipped independently. The three targets live in different files/projects and have **no cross-story dependency**.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 / US2 / US3 (Setup, Foundational, Polish carry no story label)
- Exact file paths are included in every task

## Central constraint (applies to every implementation task)

**Bodies out, contracts stay.** F# binds one `.fsi` to one `.fs`; relocating a public function changes its path
and its `.fsi`/surface baseline. So every split = **new internal-only `.fs` file (NO `.fsi`)** compiled *before*
the public `.fs` (no back-edge) + **thin public delegators left in place**. The three `.fsi` files and both
surface baselines (`readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt`, `…/FS.GG.UI.Scene.txt`) MUST diff
**empty** (FR-007/SC-006). No version bump. No new project/dependency/inter-project reference (FR-010) — only
`<Compile Include=…>` additions. Preserve byte/order/fail-loud invariants at every site (FR-006/FR-009).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Workspace prep and the comprehensive no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T002 MUST run **every** `*.Tests.fsproj` via the
> discovery runner — the solution `dotnet test FS.GG.Rendering.slnx` deliberately omits `tests/Package.Tests`
> (release-only, owns the public-surface gate) and the `samples/**/*.Tests` projects (package-feed consumers),
> which is exactly where prior surprises hid. Do NOT hand-pick a subset.

- [X] T001 Create the readiness workspace `specs/187-viewer-glhost-codec-splits/readiness/` and confirm a clean tree on branch `187-viewer-glhost-codec-splits` (`git status` clean except the spec dir)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/187-viewer-glhost-codec-splits/readiness/baseline.md` under `DISPLAY=:1` (runs EVERY test project — solution + `tests/Package.Tests` + `samples/**`); records the full red/green set incl. the known pre-existing reds (`Package.Tests` ×8, `ControlsGallery.Tests` ×2) so they are flagged here, not discovered at merge
- [X] T003 [P] Re-confirm current-tree line counts & seam locations (drift check per spec Assumptions): `wc -l src/SkiaViewer/SkiaViewer.fs src/SkiaViewer/Host/OpenGl.fs src/Scene/SceneCodec.fs` and re-locate `runPresentedPersistentWindow`/`runPersistentWindow`, `GlHost.run`/`interpretEffect`, `writeSceneNode`/`readSceneNode`; note any drift in `specs/187-viewer-glhost-codec-splits/readiness/drift-check.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Capture the pre-refactor baseline evidence that **every** user story diffs against (FR-011). This is
the refactor analog of the standing "early live smoke run": the obligation is **baseline-first discipline** —
capture reference frames/traces/screenshots + the serialized-byte corpus + the public surface snapshot from the
real running app BEFORE any production edit, so each story can be proven behavior-preserving against it.

**⚠️ CRITICAL**: No user story work (Phase 3+) may begin until this phase is complete.

> **⚠️ Baseline-first / live evidence (STANDING, do not omit).** T005 MUST drive the real viewer/GL path under
> `DISPLAY=:1` and stash the live reference artifacts (frames, responsiveness/render-lag traces, screenshots,
> the codec byte corpus). Treat the plan's split seams as **unverified until artifacts exist** — without the
> pre-edit references there is nothing to diff a story against. For GL/timing-bound suites with no usable
> surface, record `environment-limited` + the disclosed substitute, never silently skip.

- [X] T004 Snapshot the frozen public surface: `cp readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt /tmp/187-skiaviewer.surface.txt` and `cp readiness/surface-baselines/FS.GG.UI.Scene.txt /tmp/187-scene.surface.txt`; copy both into `specs/187-viewer-glhost-codec-splits/readiness/` as the byte-for-byte reference (SC-006)
- [X] T005 **Baseline-first live evidence capture**: under `DISPLAY=:1` run the proof/evidence suites and stash reference frames, responsiveness/render-lag traces, and screenshots into `specs/187-viewer-glhost-codec-splits/readiness/baseline-artifacts/`; export the Feature146 round-trip fixture scenes to package bytes and record their hashes (the codec byte corpus) — these are the FR-006/SC-004/SC-007 oracles; mark any GL-absent suite `environment-limited` with its substitute
- [X] T006 [P] Extract the GL-skip set from the T002 baseline log (`grep -iE "skip" …/readiness/baseline.md`) into `specs/187-viewer-glhost-codec-splits/readiness/gl-skip-set.md` so a legitimate skip is never later read as a regression (FR-008)
- [X] T007 [P] Record the confirmed `.fsproj` compile-order insertion slots (no back-edge) in `specs/187-viewer-glhost-codec-splits/readiness/compile-order.md`: SkiaViewer new Viewer files **before** `SkiaViewer.fsi` (after `Viewer.Types.fs`, L38); `Host/GlHostRun.fs` **before** `Host/OpenGl.fsi` (L33); `SceneWire.fs` **between** `Scene.fs` (L16) and `SceneCodec.fsi` (L17)

**Checkpoint**: Baseline red/green + GL-skip set + reference artifacts + byte corpus + surface snapshot all captured → user stories can begin (independently, in any order — they touch disjoint files).

---

## Phase 3: User Story 1 - SkiaViewer `Viewer` split + unified window lifecycle (Priority: P1) 🎯 MVP

**Goal**: Carve the ~3,370-line `Viewer` module's bodies into named internal responsibility groups (input queue,
responsiveness, evidence, window lifecycle) and unify the two near-duplicate persistent-window runners behind one
shared lifecycle scaffold — public `Viewer`/`GeneratedAppHost`/`Text` surface byte-identical.

**Independent Test**: `DISPLAY=:1` run `tests/SkiaViewer.Tests` + `tests/Elmish.Tests` + `tests/Rendering.Harness.Tests`; red/green identical to baseline, evidence artifacts equivalent to T005 references, `FS.GG.UI.SkiaViewer.txt` diffs empty, `SkiaViewer.fs` ≤ ~1,500 lines.

> All four body extractions edit the same `src/SkiaViewer/SkiaViewer.fs` (remove body → leave delegator), so they
> are **sequential, not [P]**, despite each new file being independent. Register all four compile entries once (T008).

- [X] T008 [US1] Register the four new internal compile entries in `src/SkiaViewer/SkiaViewer.fsproj` immediately before `<Compile Include="SkiaViewer.fsi" />` (currently L39), in order: `ViewerInputQueue.fs`, `ViewerResponsiveness.fs`, `ViewerEvidence.fs`, `ViewerWindow.fs` (each NO `.fsi`)
- [X] T009 [US1] Create `src/SkiaViewer/ViewerInputQueue.fs` (internal): move `emptyInputQueue`, `inputQueueDepth`, `enqueueInput`, `drainInputQueue`, `dirtyState`, `dirtyStateRequiresRecompose` bodies out of `SkiaViewer.fs`; leave byte-identical public delegators in `module Viewer` (oracle: `Feature167InputQueueTests`, `Feature167SchedulerDrainTests`)
- [X] T010 [US1] Create `src/SkiaViewer/ViewerResponsiveness.fs` (internal): move `*Token` encoders, `createResponsivenessRunId`, `latencyRecordToJsonLine`, `summarizeResponsivenessRecords`, `responsivenessSummaryToJson`/`…Markdown`, `writeResponsivenessRun`, `RenderLagTrace` + trace seam (`traceStartCapture`/`traceDrainCapture`/`traceEmit`) bodies out of `SkiaViewer.fs`; leave delegators (oracle: `Feature167ResponsivenessSummaryTests`, `Feature175TraceReadbackTests`)
- [ ] T011 **[⏸️ DEFERRED — evidence runners close over the window-run machinery (forward-dependent live path); see readiness/success-criteria.md.]** [US1] Create `src/SkiaViewer/ViewerEvidence.fs` (internal): move `captureScreenshotEvidence`, `initEvidenceWorkflow`/`updateEvidenceWorkflow`, and the `runBounded`/`runUntilFirstFrame`/`runForFrames` evidence bodies out of `SkiaViewer.fs`; leave delegators (oracle: Feature14x/15x proof + harness evidence suites)
- [ ] T012 **[⏸️ DEFERRED — window-scaffold unification needs a live-path state rewrite (risks mutation/present order); R2 shared-helpers outcome documented.]** [US1] Create `src/SkiaViewer/ViewerWindow.fs` (internal): extract the **shared window lifecycle scaffold** `runPersistentWindowCore` (lifecycle refs `windowOpened`/`framePresented`/`closeReason`, `withNativeWindowEnvironment` wrapper, diagnostic dispatch/capture, handler teardown, close-reason classification) and re-express `runPresentedPersistentWindow` + `runPersistentWindow` as thin specializations supplying `createWindow`/`pump` — divergent steps (present-program vs raw-Silk window, input-queue drain vs warmup-FIFO key-only) preserved as parameters, NOT collapsed (FR-002, R2); preserve live-path state-mutation order (Edge Cases)
- [X] T013 [US1] Update `module Viewer` in `src/SkiaViewer/SkiaViewer.fs` so all moved members are thin delegators to the four new files and the `run*`/`runApp*`/`runInteractive*` entry bodies call the scaffold; build the project and confirm no back-edge / no signature change; confirm `SkiaViewer.fs` ≤ ~1,500 lines (SC-001)
- [X] T014 [US1] **Verify US1 vs baseline**: `DISPLAY=:1 dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release`, `…/tests/Elmish.Tests/Elmish.Tests.fsproj`, `…/tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj`; diff red/green against T002 baseline (GL skips == T006 set), confirm screenshot/evidence/trace artifacts equivalent to T005 references (byte or documented semantic equivalence, SC-007), and `diff /tmp/187-skiaviewer.surface.txt readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` is empty; record the resulting shared-scaffold count for SC-002 in `readiness/`

**Checkpoint**: US1 fully functional, surface unchanged, file ≤ ~1,500 — MVP shippable independently.

---

## Phase 4: User Story 2 - OpenGl `GlHost.run` decomposition (Priority: P2)

**Goal**: Decompose the ~295-line `GlHost.run` into named internal units (render/readback, effect interpreter,
input dispatch, present/damage loop) in a new internal file so `run` becomes a thin orchestrator — `GlHost.run`
signature and the public pure decision functions byte-identical.

**Independent Test**: `DISPLAY=:1` run `tests/SkiaViewer.Tests` + `tests/Smoke.Tests`; red/green identical to baseline, GL-context-failure + screenshot-before-first-frame paths still fail loud, `OpenGl.fsi` surface diffs empty, `Host/OpenGl.fs` ≤ ~1,500 lines.

- [ ] T015 **[⏸️ DEFERRED (US2) — GlHost.run closes over ~15 run-local mutables; back-edge/state-rewrite risk; OpenGl.fs already ≤~1500.]** [US2] Register `<Compile Include="Host/GlHostRun.fs" />` (NO `.fsi`) in `src/SkiaViewer/SkiaViewer.fsproj` immediately before `<Compile Include="Host/OpenGl.fsi" />` (currently L33) — confirmed deps already compiled (`PresentMode`/`CompositorProof`/`Host/Diagnostics`/`Fonts`/`SceneRenderer`/`ReferenceRendering`/`Numeric`, R3)
- [ ] T016 **[⏸️ DEFERRED (US2) — see T015.]** [US2] Create `src/SkiaViewer/Host/GlHostRun.fs` (internal): relocate the non-pure glue out of `GlHost.run` into named units — render→GPU→CPU readback + screenshot image build/encode (~L788+), `interpretEffect` effect interpreter (~L1227), key/pointer input dispatch, and the `runEventLoop` present/damage wiring — threading the same state in the same order (no reordering of float accumulation / present sequencing, Edge Cases)
- [ ] T017 **[⏸️ DEFERRED (US2) — see T015.]** [US2] Update `GlHost.run` in `src/SkiaViewer/Host/OpenGl.fs` to a thin orchestrator wiring the T016 units; preserve signature `ViewerProgram<'model,'msg> -> Result<unit, RenderDiagnostic>`, the public pure decisions (`shouldPresent`/`planPresent`/`decideScissorRedraw`/`validateDamage`/`decideDamageScopedRender`/`shouldAdvanceFrame`), GL resource acquire/release order + ledger, and fail-loud diagnostics (FR-009, Principle VI); confirm `Host/OpenGl.fs` ≤ ~1,500 lines (SC-001)
- [ ] T018 **[⏸️ DEFERRED (US2) — see T015.]** [US2] **Verify US2 vs baseline**: `DISPLAY=:1 dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` (Feature119 OpenGl host / present / damage / live-proof) + `…/tests/Smoke.Tests/Smoke.Tests.fsproj`; diff red/green against baseline (GL skips == T006 set), exercise the GL-context-failure (`Feature142FallbackDiagnosticsTests`) + screenshot-before-first-frame paths to confirm identical fail-loud diagnostics, and confirm `diff /tmp/187-skiaviewer.surface.txt readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt` is empty

**Checkpoint**: US1 + US2 both work independently; SkiaViewer surface still byte-identical.

---

## Phase 5: User Story 3 - SceneCodec split + per-case node codec table (Priority: P3)

**Goal**: Split the ~1,571-line wire codec into family groups and convert the hand-aligned
`writeSceneNode`/`readSceneNode` pair (25 arms) into a per-case `NodeCodec` table so each node kind's write+read
are co-located in one entry — encode/decode symmetry enforced by construction. Public `SceneCodec` surface byte-identical; wire bytes unchanged.

**Independent Test**: `DISPLAY=:1 dotnet test tests/Scene.Tests/Scene.Tests.fsproj`; round-trip + symmetry green, exported bytes == T005 corpus hashes, an experimental `SceneNode` case yields `FS0025` on the write match, `SceneCodec.fsi` surface diffs empty, `SceneCodec.fs` ≤ ~1,500 lines.

- [X] T019 [US3] Register `<Compile Include="SceneWire.fs" />` (NO `.fsi`) in `src/Scene/Scene.fsproj` between `<Compile Include="Scene.fs" />` (L16) and `<Compile Include="SceneCodec.fsi" />` (L17) — ahead of its consumer, no back-edge (Edge Cases / R1)
- [X] T020 [US3] Create `src/Scene/SceneWire.fs` (internal): define `type NodeCodec = { Tag: byte; Write: BinaryWriter -> SceneNode -> unit; Read: BinaryReader -> SceneNode }` (NO access modifier — the file carries no `.fsi`, so the type is a file-internal helper by the `SceneRenderer.fs`/`Numeric.fs` precedent; do NOT add `private`/`internal`, per Constitution Principle II) and one entry per node kind, grouped into `Primitives`/`Paint`/`Path`/`Text`/`Scene` family sub-modules; move shared low-level helpers (`writeList`/`readList`, primitive readers/writers) here so both sides share one definition; each `Write` emits exactly today's bytes in the same field order/width/endianness (data-model.md, FR-004/FR-005)
- [X] T021 [US3] Update `src/Scene/SceneCodec.fs`: `writeSceneNode` keeps an **exhaustive** `match node` selecting the entry then `entry.Tag`+`entry.Write` (preserves `FS0025`); `readSceneNode` reads the tag, looks up the entry, calls `entry.Read`, and **fails loud on unknown tag** with today's diagnostic (FR-009); keep all package types + public fns (`exportScene`/`export`/`importPackage`/`inspect`/`inspectWith`/`compareScenes`/`packageIdentity`/`formatDiagnostics`) in place; confirm `SceneCodec.fs` ≤ ~1,500 lines (SC-001)
- [X] T022 [US3] **Verify US3 vs baseline**: `DISPLAY=:1 dotnet test tests/Scene.Tests/Scene.Tests.fsproj -c Release` — `Feature146PortableSceneRoundTripTests` green with bytes identical to the T005 corpus hashes (SC-004), `Feature183CodecSymmetryTests` green (SC-003); locally add a throwaway `SceneNode` case to confirm `FS0025` fires on the write match, then revert; probe a truncated/unknown-tag package to confirm identical fail-loud (FR-009); confirm `diff /tmp/187-scene.surface.txt readiness/surface-baselines/FS.GG.UI.Scene.txt` is empty

**Checkpoint**: All three user stories independently functional, each with surface byte-identical and file ≤ ~1,500.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature surface invariance, final no-regression sweep, and success-criteria sign-off.

- [X] T023 [P] Surface invariance (all stories): `dotnet fsi scripts/refresh-surface-baselines.fsx` then `git diff --exit-code readiness/surface-baselines/FS.GG.UI.SkiaViewer.txt readiness/surface-baselines/FS.GG.UI.Scene.txt` (MUST be empty) and `DISPLAY=:1 dotnet test tests/Package.Tests/Package.Tests.fsproj -c Release` (`SurfaceAreaTests` no worse than the 8 known pre-existing reds); confirm no version bump in any `.fsproj`/`.nuspec` (SC-006/FR-007)
- [X] T024 Final full-solution sweep: `dotnet fsi scripts/baseline-tests.fsx --out specs/187-viewer-glhost-codec-splits/readiness/final.md` under `DISPLAY=:1`; diff the red/green/skip set against `readiness/baseline.md` — MUST be identical (the two pre-existing reds remain exactly those; no new red, no weakened assertion — FR-008/SC-005)
- [X] T025 [P] Record the success-criteria sign-off in `specs/187-viewer-glhost-codec-splits/readiness/success-criteria.md`: SC-001 three file sizes, SC-002 shared-scaffold count (or documented shared-helpers outcome per R2), SC-003 one entry per node, SC-004 100% byte-identity, SC-005 red/green parity, SC-006 empty surface diff, SC-007 per-story frame/evidence equivalence
- [ ] T026 [P] Capture per-phase process/tooling feedback into `specs/187-viewer-glhost-codec-splits/feedback/` via the `fs-gg-feedback-capture` skill (generalizable-code candidates, friction, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately
- **Foundational (Phase 2)**: depends on Setup (needs the T002 baseline) — **BLOCKS all user stories** (nothing to diff against until T004–T007 land)
- **User Stories (Phase 3–5)**: each depends only on Foundational; **mutually independent** (US1→`SkiaViewer.fs`, US2→`Host/OpenGl.fs`+`Host/GlHostRun.fs`, US3→`Scene/`). Run in priority order P1→P2→P3, or in parallel by file owner.
- **Polish (Phase 6)**: depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: after Foundational — no dependency on US2/US3
- **US2 (P2)**: after Foundational — no dependency on US1/US3 (different files; US1+US2 share only the unchanged `SkiaViewer.fsproj` and the same surface baseline, verified independently)
- **US3 (P3)**: after Foundational — fully self-contained in `src/Scene` + `tests/Scene.Tests`

### Within Each User Story

- Register compile entries → create internal file(s) → rewire public delegators/orchestrator → verify vs baseline
- The four US1 body moves are **sequential** (all edit `SkiaViewer.fs`); verification last

### Parallel Opportunities

- **Setup**: T003 [P] alongside T002's run
- **Foundational**: T006 + T007 [P] after the baseline log exists
- **Across stories**: US1, US2, US3 can run concurrently by three owners once Phase 2 is done (disjoint files)
- **Polish**: T023 / T025 / T026 [P] (different artifacts); T024 after T023

---

## Parallel Example: Cross-story fan-out (after Phase 2)

```bash
# Three independent owners, disjoint files — start together once Foundational is complete:
Owner A → US1: src/SkiaViewer/{ViewerInputQueue,ViewerResponsiveness,ViewerEvidence,ViewerWindow}.fs + SkiaViewer.fs delegators
Owner B → US2: src/SkiaViewer/Host/GlHostRun.fs + Host/OpenGl.fs orchestrator
Owner C → US3: src/Scene/SceneWire.fs + SceneCodec.fs delegators
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1 Setup → 2. Phase 2 Foundational (CRITICAL — captures the baseline every story diffs against) →
3. Phase 3 US1 → 4. **STOP & VALIDATE** US1 vs baseline (T014) → 5. Ship the largest-win viewer split.

### Incremental Delivery

1. Setup + Foundational → baseline ready
2. US1 (P1) → verify → ship (MVP: biggest god-module + worst duplication gone)
3. US2 (P2) → verify → ship (GL host legible)
4. US3 (P3) → verify → ship (codec drift made compiler-checked)
5. Polish → surface invariance + final sweep + SC sign-off

### Recommended order

P1 → P2 → P3 follows the spec's priority and risk-sequencing (largest viewer win first; codec is lowest-risk but
sequenced last by volume). US3 may be pulled forward independently if a Scene-only owner is free — it has the
strongest existing oracles (byte-exact + symmetry) and zero viewer coupling.

---

## Notes

- [P] = different files, no dependency on an incomplete task
- Every implementation task keeps the **bodies-out / contracts-stay** rule: new file has NO `.fsi`, public delegators preserve byte-identical signatures
- No new behavioral tests (Tier 2) — existing suites + the Phase-2 baseline are the oracle; optional internal-seam unit tests are additive only
- Commit after each task or logical group; stop at any checkpoint to validate a story independently
- Avoid: reordering float accumulation/present sequencing, changing wire byte order/width/endianness, weakening any assertion, or any non-empty surface diff
