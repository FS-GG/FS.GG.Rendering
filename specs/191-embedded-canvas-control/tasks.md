---
description: "Task list for Embedded Canvas Control"
---

# Tasks: Embedded Canvas Control

**Input**: Design documents from `/specs/191-embedded-canvas-control/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/canvas-control.md ✅, quickstart.md ✅

**Tests**: INCLUDED — this is a Tier-1 contracted change. Constitution V ("Test Evidence Is Mandatory") and the spec's per-story Independent Tests require golden-scene byte-identity, fingerprint-sensitivity, cache-isolation `WorkReduction`, input-forwarding, and `Loop.advance` determinism/clamp tests, all authored fail-before / pass-after.

**Organization**: Tasks are grouped by user story (US1 → US2 → US3) for independent implementation and testing. Per Constitution I & II, every contracted surface is authored in `.fsi` and exercised in FSI **before** its `.fs` body.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (no label for Setup, Foundational, Polish)
- Every task names exact file paths.

## Path Conventions

Single-solution, multi-project repo (`FS.GG.Rendering.slnx`). The canvas control kind lives in the existing `src/Controls`, `src/SkiaViewer`, `src/Controls.Elmish` projects; the pure element/loop library is a new `src/Canvas` project (`FS.GG.UI.Canvas`); the sample is `samples/CanvasDemo`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: New project scaffolding and the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** T002 MUST run **every** test project via the discovery-based runner so pre-existing reds are known up front and not mistaken for regressions at merge. Do NOT hand-pick a subset: the solution deliberately omits `tests/Package.Tests` (release-only surface gate) and `samples/**/*.Tests` (feed consumers) — exactly where stale surface baselines / sample pins hide.

- [X] T001 Create the new library project `src/Canvas/Canvas.Lib.fsproj` (`FS.GG.UI.Canvas`, `net10.0`, references **only** `src/Scene`), add it to `FS.GG.Rendering.slnx`, and add empty placeholder files `src/Canvas/Elements.fsi`, `src/Canvas/Elements.fs`, `src/Canvas/Loop.fsi`, `src/Canvas/Loop.fs` wired into the `.fsproj` compile order (Scene-only dependency per plan Structure Decision)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/191-embedded-canvas-control/readiness/baseline.md` (globs every `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T003 [P] Create the new test project `tests/Canvas.Tests/Canvas.Tests.fsproj` (Expecto + FsCheck, references `src/Canvas/Canvas.Lib.fsproj`) and register it in `FS.GG.Rendering.slnx`; add a placeholder `tests/Canvas.Tests/LoopTests.fs` with a trivial passing test to confirm the project runs headless (no GL)
- [X] T004 [P] Add the new test entry point `tests/Controls.Tests/Feature191CanvasTests.fs` (empty Expecto test list registered in the suite) so US1/US2 GL-gated tests have a home that compiles green before any assertion is added

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The `.fsi` seams and the early live spike that every user story depends on. **No user-story work begins until this phase is complete.**

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's two provisional hypotheses — (1) determinism/byte-identity round-trip and (2) cache isolation — are **unverified assumptions until the app has been run**. T009 is the Scenario-0 decision spike: it drives a real host (`DISPLAY=:1`) with a static authored scene through a hard-coded `paintLeaf "canvas"` branch and observes fingerprint sensitivity **before** US2 builds the cache-isolation machinery on that assumption. Do not defer this to a per-story checkpoint.

- [X] T005 [P] Author the `SceneValue` contract seam (C1): add `| SceneValue of FS.GG.UI.Scene.Scene` to the attribute value DU in `src/Controls/Attributes.fsi` (and `Types.fsi` if the DU is declared there) and declare `val internal sceneAttr : Control<'msg> -> FS.GG.UI.Scene.Scene option` on `ControlInternals` — signatures only, exercised in FSI per Constitution I
- [X] T006 [P] Author the `Canvas` constructor `.fsi` seam (C2) in `src/Controls/Canvas.fsi`: `scene`, `viewport`, `volatile'`, `onPointer`, `onKey`, `create` — signatures only, no bodies
- [X] T007 [P] Author the `FS.GG.UI.Canvas` library `.fsi` seams: `Element<'props>` + `Elements` (`rect`/`sprite`/`circle`/`polyline`/`at`/`layer`/`cached`, C3) in `src/Canvas/Elements.fsi`, and `StepState<'world>` + `Loop` (`init`/`advance`/`alpha`, C4) in `src/Canvas/Loop.fsi` — signatures only
- [X] T008 Exercise all Phase-2 `.fsi` seams in FSI (load the signatures, confirm they type-check and compose) and record the FSI transcript under `specs/191-embedded-canvas-control/readiness/`, per Constitution I (FSI-first before any `.fs` body)
- [X] T009 **Early live smoke run (Scenario 0)**: add a minimal hard-coded `paintLeaf "canvas"` branch in `src/Controls/Control.fs` plus the `SceneValue` body in `src/Controls/Attributes.fs`, author a static red-rect+circle scene inside a `stack` with sibling chrome, render through a real host under `DISPLAY=:1`, and record live evidence (or `environment-limited` with disclosed substitute) that the scene paints translated-to-box-origin + clipped, that changing the scene changes `hashScene` (cache miss) and an unchanged scene keeps it (cache hit) — into `specs/191-embedded-canvas-control/readiness/`. **This spike branch is throwaway** (replaced by the real branch in T016) and is intentionally a probe, not the feature: it MUST be removed or guarded so it does not green the US1 tests (T010–T013) before their implementation — those tests target behavior the spike does not satisfy (box clipping, box-origin translation, the no-scene placeholder, zero-area safety), preserving genuine fail-before per Constitution I/V

**Checkpoint**: `.fsi` seams compile and are FSI-exercised; the determinism + fingerprint-sensitivity hypotheses are confirmed against a live run. User-story implementation can now begin.

---

## Phase 3: User Story 1 - Embed an application-drawn canvas in the UI (Priority: P1) 🎯 MVP

**Goal**: A `canvas` control kind paints an application-supplied immutable `Scene` into its laid-out box (box-origin local coords, clipped), integrated with layout; absent scene → placeholder; identical model → byte-identical scene + fingerprint.

**Independent Test**: Author a fixed scene (red rectangle + circle), place a `canvas` carrying it inside a `stack` with sibling themed controls, render headlessly, and assert the content appears, is translated to the box origin, clipped to the box, sized by explicit width/height, with chrome laying out normally — and that two renders are byte-identical.

### Tests for User Story 1 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T010 [P] [US1] Golden paint + clip + box-origin translation test for the `canvas` kind in `tests/Controls.Tests/Feature191CanvasTests.fs` (mirror `RenderingTests.fs`): assert author content paints at box origin and is clipped to the box; assert explicit `width`/`height` size the control and siblings lay out around it. Primary assertions MUST be **behavioral** (painted/emitted output observed at the public render path), not return values of the `internal sceneAttr` accessor (Constitution I.3); verify this test FAILS against the T009 spike before T016 lands (the spike does not clip or translate-to-box-origin)
- [X] T011 [P] [US1] Fingerprint-sensitivity + byte-identity determinism test in `tests/Controls.Tests/Feature191CanvasTests.fs` (mirror `Feature120FingerprintTests.fs`): same model → identical emitted `Scene` + identical `hashScene`; a render-affecting scene change → changed fingerprint
- [X] T012 [P] [US1] Placeholder / safe-failure test in `tests/Controls.Tests/Feature191CanvasTests.fs`: a `canvas` with no `SceneValue` paints a clear placeholder (FR-013); a zero-area / unmeasured box paints nothing and does not error
- [X] T013 [P] [US1] CustomControl no-regression test in `tests/Controls.Tests/Feature191CanvasTests.fs`: confirm the existing `CustomControl` placeholder behavior is unchanged by the new kind (FR-001)
- [X] T013a [P] [US1] Viewport-transform test in `tests/Controls.Tests/Feature191CanvasTests.fs` (FR-016): a `canvas` with a `viewport` (pan/zoom) attr applies the transform to its **content only** — the laid-out box size and the hit-test box are unchanged, and content remains clipped to the box; absent `viewport`, content paints at the box origin with no extra transform. (This grounds the public `Canvas.viewport` surface from contracts C2 / T017 in a fail-before/pass-after test.)

### Implementation for User Story 1

- [X] T014 [US1] Implement the `SceneValue` attribute body in `src/Controls/Attributes.fs` and the internal `sceneAttr` accessor on `ControlInternals` (per the T005 `.fsi`), reading the scene off a `"canvas"` control's attributes
- [X] T015 [US1] Register the GENERATED `canvas` kind in `src/Controls/Catalog.fs` (category `display`, events `onPointer`/`onKey`) and `src/Controls/Catalog.fsi`; ensure the kind flows through `ControlKindRegistry`
- [X] T016 [US1] Replace the T009 spike branch with the real `paintLeaf "canvas"` branch in `src/Controls/Control.fs` (before the `richFamilies` check): translate author-local content to the box origin, clip to the box, honor explicit `width`/`height` (default box otherwise), and paint the placeholder when no `SceneValue` is present (FR-001/FR-002/FR-013)
- [X] T017 [US1] Implement the `Canvas` constructor module body in `src/Controls/Canvas.fs` (`scene`, `viewport` per FR-016, `create`; `volatile'`/`onPointer`/`onKey` may be stubbed-but-typed until US2) satisfying `src/Controls/Canvas.fsi`
- [X] T018 [US1] Update `readiness/surface-baselines/FS.GG.UI.Controls.txt` deliberately for the Tier-1 additions visible after US1 (+ `Canvas` module, + `canvas` kind, + `SceneValue`). Because `Canvas.fsi` references `FS.GG.UI.Controls.PointerSample`, `FS.GG.UI.KeyboardInput.ViewerKey`, and `KeyModifiers` (onPointer/onKey signatures), **verify whether any of these raw types newly enters the public surface**; if so, enumerate them explicitly in the affected baseline rather than letting them slip in implicitly. Confirm `tests/Package.Tests/SurfaceAreaTests.fs` passes against the new baseline (never silenced)
- [X] T019 [US1] Run the US1 suite green under `DISPLAY=:1` (`dotnet test tests/Controls.Tests --filter Feature191`) and record the pass evidence in `specs/191-embedded-canvas-control/readiness/`

**Checkpoint**: A static embedded canvas paints, clips, sizes, placeholders, and is deterministic — MVP is independently testable and demoable.

---

## Phase 4: User Story 2 - Animate and interact without disturbing the surrounding UI (Priority: P2)

**Goal**: A canvas can be marked `volatile'` (no-cache, walled behind a repaint boundary) so per-frame redraw leaves surrounding chrome cache-stable (0 chrome repaints); raw pointer + keyboard input reaches the bound model when in-box / focused.

**Independent Test**: Run a canvas whose scene changes every frame next to static chrome; assert (a) raw pointer move/press/release/wheel + key events reach the bound model only when targeted/focused, and (b) surrounding chrome registers as render-work-skipped (cache-stable) across frames while the canvas repaints.

### Tests for User Story 2 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T020 [P] [US2] Cache-isolation `WorkReduction` test in `tests/Controls.Tests/Feature191CanvasTests.fs` (mirror `Feature116PictureCacheTests.fs`): a `volatile'` canvas repainting every frame while chrome is unchanged → chrome stays `PictureCacheHits` and `WorkReduction.RepaintedNodeCount` excludes the chrome (target 0 chrome repaints, SC-003/FR-005)
- [X] T021 [P] [US2] Unchanged-scene cache-hit test in `tests/Controls.Tests/Feature191CanvasTests.fs`: a non-volatile canvas whose scene is identical between two frames is recognized as a cache hit and not repainted (FR-003 acceptance #4)
- [X] T022 [P] [US2] Raw pointer-forwarding test in `tests/Controls.Tests/Feature191CanvasTests.fs` (mirror `PointerInteractionTests.fs`): an in-box pointer move/press/release/wheel dispatches the raw `PointerSample` (coords resolvable to canvas-local space) to `onPointer`; an out-of-box pointer does **not** (FR-006, SC-005)
- [X] T023 [P] [US2] Raw key-forwarding + focus test in `tests/Controls.Tests/Feature191CanvasTests.fs`: a focused canvas delivers `ViewerKey` + `KeyModifiers` to `onKey`; an unfocused canvas does not; the canvas participates in `Focus.order` (FR-007, SC-005)

### Implementation for User Story 2

- [X] T024 [US2] Add the per-kind `volatileFamilies` set + `volatile'` attribute signal in `src/Controls/Control.fs` and thread the no-cache / always-dirty signal into fragment assembly in `src/Controls/RetainedRender.fs` (and `RetainedRender.fsi` if surface changes) so the canvas subtree bypasses record/replay without a `CachedSubtree` boundary (D4/FR-004)
- [X] T025 [US2] Implement the per-node volatile bypass at the paint boundary in `src/SkiaViewer/PictureReplayCache.fs` so record/replay is skipped for the volatile canvas node while sibling/parent picture-cache entries survive (FR-005)
- [X] T026 [US2] Complete the `Canvas.volatile'` constructor body in `src/Controls/Canvas.fs` (wire the attribute that marks `volatileFamilies` membership) per `src/Controls/Canvas.fsi`
- [X] T027 [US2] Implement raw pointer forwarding in `src/Controls.Elmish/ControlsElmish.fs` (and `.fsi`): a `canvas` node bound to `onPointer` dispatches the raw `PointerSample` via a raw-sample channel for canvas kinds in `routeInteractivePointer`, leaving existing `PointerInteraction` routing intact (C6/FR-006)
- [X] T028 [US2] Implement raw key forwarding + focusability in `src/Controls.Elmish/ControlsElmish.fs` (and `.fsi`): a focused `canvas` bound to `onKey` receives `ViewerKey` + `KeyModifiers` in `routeFocusedKey` before default navigation; mark the canvas focusable (C6/FR-007)
- [X] T029 [US2] Complete the `Canvas.onPointer` / `Canvas.onKey` constructor bodies in `src/Controls/Canvas.fs` per `src/Controls/Canvas.fsi`
- [X] T030 [US2] Update `readiness/surface-baselines/FS.GG.UI.Controls.Elmish.txt` (and `FS.GG.UI.Controls.txt` if the volatile/input surface changed) for any new public input-forwarding surface. Explicitly determine whether `PointerSample` / `ViewerKey` / `KeyModifiers` are exposed publicly through the C6 routing or the `Canvas.onPointer`/`onKey` constructors and, if so, enumerate them in the appropriate baseline (do not rely on the conditional "if exposed"); confirm `tests/Package.Tests/SurfaceAreaTests.fs` passes (never silenced)
- [X] T031 [US2] Run the US2 suite green under `DISPLAY=:1` (`dotnet test tests/Controls.Tests --filter Feature191`) and record cache-isolation + input-forwarding pass evidence in `specs/191-embedded-canvas-control/readiness/`

**Checkpoint**: The canvas is interactive and cache-isolated — US1 + US2 both work independently; 0 chrome repaints demonstrated.

---

## Phase 5: User Story 3 - Build games with a reusable element kit and a deterministic game loop (Priority: P3)

**Goal**: A pure `Elements` library (`'props -> Scene`) and a fixed-timestep `Loop.advance` (clamped, deterministic) plus a documented held-input pattern, proven by a runnable seeded embedded sample.

**Independent Test**: Use `Elements` + `Loop.advance` to drive a small sample (bouncing sprites / Pong) from a seed + scripted input sequence; assert same seed + inputs → identical world state, scene, and fingerprint each run, and that the loop clamps a runaway frame time (no spiral of death).

### Tests for User Story 3 (write FIRST, ensure they FAIL before implementation) ⚠️

- [X] T032 [P] [US3] `Elements` purity / golden test in `tests/Canvas.Tests/ElementsTests.fs`: each combinator (`rect`/`sprite`/`circle`/`polyline`/`at`/`layer`/`cached`) returns an identical `Scene` for identical props and composes (FR-008, SC-007) — headless, no GL
- [X] T033 [P] [US3] `Loop.advance` determinism + step-count + clamp test in `tests/Canvas.Tests/LoopTests.fs`: step count = `floor((Accumulator + clamp(frameTime)) / dt)`; an injected oversized `frameTime` (e.g. `5.0`) is clamped to `0.25`; `init`/`alpha` behave per contract; same inputs → identical `StepState` (FR-009/FR-011, SC-006)
- [X] T034 [P] [US3] Seeded-sample reproducibility test in `tests/Canvas.Tests/CanvasDemoTests.fs`: the sample run from a fixed seed + scripted input sequence yields identical world state, emitted `Scene`, and fingerprint across two headless runs (FR-014, SC-006)

### Implementation for User Story 3

- [X] T035 [P] [US3] Implement the pure `Elements` bodies in `src/Canvas/Elements.fs` (`rect`/`sprite`/`circle`/`polyline`/`at`/`layer`/`cached`) per `src/Canvas/Elements.fsi` — all return immutable `Scene`, none mutate (FR-008)
- [X] T036 [P] [US3] Implement the `Loop` bodies in `src/Canvas/Loop.fs` (`init`, `advance` with `frameTime ≤ 0.25` clamp + fixed-step accumulator, `alpha`) per `src/Canvas/Loop.fsi` — output depends only on arguments, no wall-clock read (FR-009/FR-011)
- [X] T037 [US3] Build the runnable embedded sample `samples/CanvasDemo/` (`.fsproj` + program) — bouncing sprites / Pong composed from `Elements`, advanced by `Loop.advance` on the `Animation.tickSubscription` (`isAnimating`-gated) tick carrying the nominal fixed interval, using `Canvas.volatile'` + `onPointer`/`onKey`, rendering `lerp Previous Current (Loop.alpha dt state)` into `Canvas.scene` — where `lerp` is the **sample's own** world-interpolation helper (no framework `lerp`/interpolation API is provided; `Loop.alpha` supplies only the factor); register it in `FS.GG.Rendering.slnx`; builds against the public package surface only (FR-012/FR-014/FR-015)
- [X] T038 [US3] Document the held-input reconstruction pattern (`Set<ViewerKey>` level state + per-tick edge sets cleared per fixed step; pointer state; wheel deltas distributed across substeps) in `samples/CanvasDemo/` and `specs/191-embedded-canvas-control/quickstart.md` (FR-010/D7)
- [X] T039 [US3] Create the NEW surface baseline `readiness/surface-baselines/FS.GG.UI.Canvas.txt` for `Elements` + `Loop`/`StepState` (and optional `DrawScope` if shipped), and confirm `tests/Package.Tests/SurfaceAreaTests.fs` covers/passes the new package surface (never silenced)
- [X] T040 [US3] Run the US3 suites green: `dotnet test tests/Canvas.Tests` (headless) + the seeded sample evidence, and record reproducibility + clamp evidence in `specs/191-embedded-canvas-control/readiness/`

**Checkpoint**: All three stories independently functional; the sample proves the full stack with repeatable seeded evidence.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Cross-story validation, docs, and the final no-regression gate.

- [X] T041 [P] (OPTIONAL — **DEFERRED**) `DrawScope` ergonomic builder (C5). **Deferred**: the pure `Elements` combinators + `at`/`layer` composition proved sufficient and idiomatic for both the US3 tests and the runnable `samples/CanvasDemo` (a bouncing-ball mini-game); a stateful appender added no expressive power over the immutable combinators and would enlarge the public surface. Revisit only if a real consumer hits ergonomic friction.
- [X] T042 Run the full `quickstart.md` validation (Scenarios 0–3) end-to-end under `DISPLAY=:1` and check off the quickstart Done-when boxes with recorded evidence
- [X] T043 Re-run the comprehensive baseline (`dotnet fsi scripts/baseline-tests.fsx --out specs/191-embedded-canvas-control/readiness/final.md`) and diff against T002 to prove no existing test was deleted, skipped, weakened, or newly red (Constitution V). **Additionally assert SC-004**: run the existing perf/responsiveness lanes (features 160/161/167/173) with an animating canvas in the tree and record evidence (or `environment-limited` with disclosed substitute) that the ~60 fps cadence holds and no per-frame frame-time/allocation budget regresses
- [X] T044 [P] Capture per-phase fs-gg / Spec Kit feedback into `specs/191-embedded-canvas-control/feedback/` (process friction, generalizable-code candidates, severity) via the `fs-gg-feedback-capture` flow

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. **BLOCKS all user stories.** T009 (live spike) gates US2's cache assumption.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 is the MVP. US2 and US3 build on US1's `Canvas` constructor + `canvas` kind but are independently testable.
- **Polish (Phase 6)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. No dependency on US2/US3.
- **US2 (P2)**: After Foundational. Reuses the US1 `canvas` kind + `Canvas` module (volatile/input bodies were typed-but-stubbed in US1); independently testable.
- **US3 (P3)**: After Foundational. The pure `FS.GG.UI.Canvas` library (T035/T036) is fully independent of US1/US2 and can be built in parallel; the sample (T037) consumes US1 (`Canvas.scene`/`create`) and US2 (`volatile'`/`onPointer`/`onKey`).

### Within Each User Story

- `.fsi` seams (Phase 2) before `.fs` bodies (Constitution I/II).
- Tests written and FAILING before implementation.
- Attribute/kind/paint before constructor; constructor before routing; routing before sample.
- Surface-baseline update + drift test before the story's green-run task.

### Parallel Opportunities

- Setup: T003, T004 in parallel (different new files).
- Foundational: T005, T006, T007 (`.fsi` seams in different files) in parallel; T008/T009 follow.
- US1 tests T010–T013a in parallel (same file — coordinate, or split into stubs first); US2 tests T020–T023 in parallel; US3 tests T032–T034 in parallel.
- US3 library bodies T035, T036 in parallel (different files), and the whole pure library can proceed alongside US1/US2.
- Polish: T041, T044 in parallel.

---

## Parallel Example: User Story 1

```bash
# Author all four US1 tests first (fail-before), then implement:
Task T010: Golden paint + clip + box-origin test in tests/Controls.Tests/Feature191CanvasTests.fs
Task T011: Fingerprint-sensitivity + byte-identity test in tests/Controls.Tests/Feature191CanvasTests.fs
Task T012: Placeholder / zero-size safe-failure test in tests/Controls.Tests/Feature191CanvasTests.fs
Task T013: CustomControl no-regression test in tests/Controls.Tests/Feature191CanvasTests.fs
Task T013a: Viewport-transform (content-only, FR-016) test in tests/Controls.Tests/Feature191CanvasTests.fs
```

## Parallel Example: Foundational `.fsi` seams

```bash
Task T005: SceneValue + sceneAttr in src/Controls/Attributes.fsi
Task T006: Canvas constructor module in src/Controls/Canvas.fsi
Task T007: Elements + Loop signatures in src/Canvas/Elements.fsi and src/Canvas/Loop.fsi
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Phase 1: Setup (new project + baseline).
2. Phase 2: Foundational — `.fsi` seams + FSI exercise + **early live spike (T009)** confirming the determinism/fingerprint hypotheses against the real app before US2 depends on them.
3. Phase 3: User Story 1.
4. **STOP and VALIDATE**: a static embedded canvas paints, clips, sizes, placeholders, and is byte-identical — demoable MVP.

### Incremental Delivery

1. Setup + Foundational → foundation ready.
2. US1 → static embedded canvas (MVP, charts/diagrams unblocked).
3. US2 → volatile + input + cache isolation (interactive surface).
4. US3 → element kit + loop + runnable seeded sample (game/sim ergonomics + end-to-end proof).
5. Each story adds value without breaking the previous.

### Parallel Team Strategy

Once Foundational completes: Developer A on US1, Developer B can start the fully-independent pure `FS.GG.UI.Canvas` library (US3 T035/T036), Developer C on US2 after US1's `canvas` kind lands. The US3 sample integrates last.

---

## Notes

- [P] = different files, no dependencies on incomplete tasks.
- Tier-1 discipline: every public symbol declared in `.fsi`, FSI-exercised, baseline-updated, drift-test-green — never silence the surface gate.
- Verify each test FAILS before implementing; commit after each task or logical group.
- GL-gated suites run under `DISPLAY=:1`; pure `tests/Canvas.Tests` run headless.
- Determinism is load-bearing: no wall-clock read in `Loop` or paint; the tick carries the nominal fixed interval.
