---
description: "Task list for Headless Image Evidence Path"
---

# Tasks: Headless Image Evidence Path

**Input**: Design documents from `/specs/221-headless-image-evidence/`

**Prerequisites**: spec.md (required — user stories), plan.md (authored — Summary, Technical Context, Constitution Check, Project Structure); tasks are grounded in spec.md + plan.md + the verified codebase architecture map below

**Tests**: Included where the spec explicitly demands them — FR-003 determinism, SC-001 non-blank, SC-005 no-stub regression, the concurrency edge case, and each story's "Independent Test". Other tasks omit tests.

**Change Classification**: Tier 1 (per spec.md) — `.fsi` updates + surface-area baseline updates are mandatory (T007, T024).

## Architecture grounding (verified against the codebase)

- **Integration target (the stub):** `SceneEvidence.renderPng` in `src/Scene/Evidence.fs:108-118` (sig `src/Scene/Evidence.fsi:44`) returns `Encoding.UTF8.GetBytes` of the deterministic **hash string**, not a PNG. `render` (`Evidence.fs:79-83`) routes `Format.Png` to `readback.DeterministicHash`. This is where real pixels must come from.
- **Working no-GL CPU rasterizer already exists:** `ReferenceRendering.renderScenePng` (`src/SkiaViewer/ReferenceRendering.fs:119-137`) builds `SKSurface.Create(info)` (CPU raster, **no `GRContext`**), runs the shared painter, and `image.Encode(Png)`. Sibling already wired through `captureScreenshotEvidence`: `writeSceneImageEvidence` (`src/SkiaViewer/SkiaViewer.fs:1824-1842`).
- **Shared painter:** `SceneRenderer.paintNode` (`src/SkiaViewer/SceneRenderer.fs:246-418`) — exhaustive, no-wildcard match over every `SceneNode`. GL and CPU paths share only this painter, not the surface.
- **Dependency constraint:** `src/Scene/Scene.fsproj` has **zero** package/project refs and MUST stay SkiaSharp-free (`src/Scene/skill/SKILL.md:53`). So the rasterizer lives in **SkiaViewer** and is injected into Scene via a seam mirroring the existing `Scene.setRealTextMeasurer` (`src/Scene/Scene.fsi:131`, injected from `src/SkiaViewer/Fonts.fs:520`).
- **Deterministic fonts:** 9 bundled `.ttf` embedded resources + `src/SkiaViewer/Fonts.fs` registry → host-independent text, with per-character glyph-coverage fallback / tofu for uncovered glyphs (`Fonts.fs:203-247`, `SceneRenderer.fs:165-173`).
- **Typed failure model already exists:** `SceneEvidenceFailureClassification = UnsupportedEnvironment | ProductDefect` and `SceneEvidenceFailure = { BlockedStage; Classification; DiagnosticCategory; Message }` at `src/Scene/Evidence.fs:11-19`, with `EvidenceStage = Scene | Renderer` (`Evidence.fs:40-49`). US3 **uses** this model — it does not invent a new one.
- **A CPU determinism harness already exists at T0:** `Tiers.runOffscreen` → `Viewer.captureScreenshotEvidence` (`PresentMode = OffscreenReadback`, CPU `writeSceneImageEvidence`) re-renders `frame.png`/`frame2.png` and asserts byte-identical determinism (`tools/Rendering.Harness/Tiers.fs:49-54`). The gap is the **dependency-light `Scene.Evidence.renderPng` surface** consumers/CI call directly, which still returns the hash stub.
- **US2 live path is GL-required:** `OffscreenReadback` → `renderSceneToPixels` (`src/SkiaViewer/Host/OpenGl.fs:788-826`) creates a GL surface over a `GRContext` and reads back. This is distinct from the no-GPU P1 path and needs a GL/virtual-display host (consistent with the spec Assumption that a virtual display may exist for US2).

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and baseline

- [X] T001 Create the feature evidence/readiness scaffolding: `specs/221-headless-image-evidence/readiness/` and `specs/221-headless-image-evidence/evidence/` directories with a `README.md` describing what each artifact proves
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/221-headless-image-evidence/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T003 [P] Sanity-confirm the existing CPU raster path builds and runs headless: `dotnet build src/SkiaViewer/SkiaViewer.fsproj`, then exercise `ReferenceRendering.renderScenePng` once with no GPU/display and record that a non-blank PNG is produced in `specs/221-headless-image-evidence/readiness/cpu-raster-sanity.md`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core decisions and seams that ALL user stories depend on

**⚠️ CRITICAL**: No user story work begins until this phase is complete

- [X] T004 Record the classification / root-cause map in `specs/221-headless-image-evidence/readiness/root-cause-map.md`: confirm (a) `Evidence.fs:renderPng` returns hash bytes not a PNG, (b) the live viewer presents to the swapchain with no GPU→CPU readback, (c) `ReferenceRendering.renderScenePng` is a working no-GRContext CPU raster — and name which of these each FR closes
- [X] T005 **Early live smoke run**: drive the real viewer with `PresentMode = ViewerPresentMode.OffscreenReadback` (`docs/usage.md:196`) for the representative game scene (the T006 fixture), to validate the hypotheses in T004 against a live GL readback BEFORE building any fix. **Environment note:** `OffscreenReadback` is the **GL-required** route (GRContext + virtual display, per the architecture grounding above) — it is *not* the no-GL P1 path; it is used here only to confirm the live viewer's present/readback behavior. On a bare no-GL runner this step is expected to be `environment-limited`; record either the live non-blank capture **or** the `environment-limited` result with the disclosed substitute in `specs/221-headless-image-evidence/readiness/smoke-run.md`
- [X] T006 [P] Set up the test + evidence scaffolding all stories share: define the canonical **"representative game scene" fixture** in `tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs` and register it in the test project. The fixture MUST be concretely pinned and reproducible so the determinism guarantees (FR-003, SC-001, SC-002) rest on a fixed input: a single named `Scene` constructor returning a deterministic scene (geometry + color + text, exercising the bundled-font path) at a **fixed output size** (e.g. `Size = 800×600`). Every determinism/non-blank/timing task (T005, T008, T009, T014) references this one fixture by name — no ad-hoc scenes. Record the fixture's identity (constructor name + size) in `specs/221-headless-image-evidence/readiness/` so SC-001/SC-002/SC-004 are pinned to a known input
- [X] T007 Draft the public-surface seam FIRST (before implementation, per constitution Principle I / Tier 1): add an injectable rasterizer seam to `src/Scene/Scene.fsi` (e.g. `setRealPngRasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) -> unit`) mirroring `setRealTextMeasurer` at `Scene.fsi:131`, keeping `src/Scene` SkiaSharp-free; stub the default to the current typed `UnsupportedEnvironment` failure. Exercise the shape in FSI before writing `.fs`

**Checkpoint**: Root-cause map confirmed against a live headless run; the Scene rasterizer seam (`.fsi`) is drafted and FSI-validated — user-story implementation can begin

---

## Phase 3: User Story 1 - Deterministic image evidence in a headless environment (Priority: P1) 🎯 MVP

**Goal**: In a bare container (no GPU/X/display), render a scene description to a real, decodable PNG of the requested size whose pixels show the scene; repeating the render yields identical bytes.

**Independent Test**: With no GPU/display, request a PNG for a representative game scene at a fixed size; assert it decodes as a PNG of the requested dimensions, has non-blank pixel content, and is byte-for-byte identical to a second run.

### Tests for User Story 1 ⚠️ (write first, ensure they FAIL)

- [X] T008 [P] [US1] Determinism + dimensions + non-blank test in `tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs`: render the representative scene to PNG twice, assert (a) decodes as PNG of requested W×H, (b) non-blank pixels (reuse `ReferenceRendering.imageNonBlank` semantics), (c) byte-for-byte identical across the two runs (FR-003, SC-001, SC-002)
- [X] T009 [P] [US1] Cross-instance determinism check in the same test file: render in two independent fixtures/processes and assert identical bytes (SC-002 "across machines" proxy). **Intentionally distinct from T008** — T008 proves same-process repeatability; T009 proves cross-process/instance determinism (the "across machines" guarantee). Both are required; neither subsumes the other
- [X] T010 [P] [US1] **Concurrency edge-case test** (spec Edge Cases): run several independent headless `renderPng` requests concurrently and assert each result is unaffected by the others and each remains byte-for-byte deterministic per scene (no shared-state interference in the injected rasterizer)

### Implementation for User Story 1

- [X] T011 [US1] Implement the headless CPU rasterizer entry point in SkiaViewer: expose `renderScenePngResult: Size -> Scene -> Result<byte[], SceneEvidenceFailure>` reusing an existing CPU donor — `ReferenceRendering.renderScenePng` (`ReferenceRendering.fs:119-137`) or the already-wired `writeSceneImageEvidence` (`SkiaViewer.fs:1824-1842`), both via `SceneRenderer.paintNode` on an `SKBitmap`/`SKSurface.Create(info)` with no `GRContext`; force deterministic premul Rgba8888; guarantee bundled-font text via `Fonts.fs`; and ensure a **missing font face is disclosed** (via the typed failure or recorded metadata) rather than silently substituted in a way that breaks determinism (spec Edge Cases — fonts/text)
- [X] T012 [US1] Inject the rasterizer into the Scene seam from SkiaViewer: call `Scene.setRealPngRasterizer (...)` where `Scene.setRealTextMeasurer` is wired (`src/SkiaViewer/Fonts.fs:520`), so Scene gains real pixels without a SkiaSharp reference; confirm the injected closure is re-entrant / thread-safe for T010
- [X] T013 [US1] Rewire `SceneEvidence.renderPng` / `render` `Format.Png` branch in `src/Scene/Evidence.fs:79-118` to call the injected rasterizer and return its real PNG bytes (replacing the `Encoding.UTF8.GetBytes hash` stub); preserve `Hash` and metadata branches unchanged (FR-001, FR-002, FR-007)
- [X] T014 [US1] Enforce the CI time bound (FR-008/SC-004): add an assertion/measurement that a single representative-scene render completes under the 5s target on a standard runner, recorded in `specs/221-headless-image-evidence/evidence/`

**Checkpoint**: A real, deterministic, non-blank PNG is produced headlessly via `renderPng`, safe under concurrency; US1 is independently testable and is the MVP.

---

## Phase 4: User Story 2 - Pixel proof of the live game window (Priority: P2)

**Goal**: One documented, supported path to obtain an image of the live frame (or an offscreen render equivalent) in an environment where the GL window can't be captured externally — no guesswork, no decompiling.

**Independent Test**: Follow the documented capture path against a running viewer in a virtual-display environment and confirm a non-black image of the current frame, with zero undocumented steps.

> **GL prerequisite (N1):** Unlike the no-GPU P1 path, US2's route (`OffscreenReadback` → `renderSceneToPixels`) **requires a GL context / virtual display**. This matches the spec Assumption that a virtual display may exist for the live-window path; tasks below must state this prerequisite so US2 is not mistaken for a headless-no-GL path.

### Implementation for User Story 2

- [X] T015 [US2] Confirm/expose the supported live-frame capture route end to end: verify `ViewerPresentMode.OffscreenReadback` + `renderSceneToPixels` (`src/SkiaViewer/Host/OpenGl.fs:788-826`) yields a non-black current-frame image on a GL/virtual-display host, and that any missing seam to request it on demand is added (FR-006)
- [X] T016 [US2] Write the supported capture-path documentation in `docs/usage.md` (headless/offscreen section near line 196): describe the live-window capture end to end — including the GL/virtual-display prerequisite — with every step explicit, no binary inspection or trial-and-error (FR-006, SC-003)
- [X] T017 [US2] Capture and record the live-frame proof for a representative scene under a virtual display in `specs/221-headless-image-evidence/evidence/` (or `environment-limited` with disclosed substitute), proving the documented path produces a non-black image

**Checkpoint**: US1 + US2 both work independently; the live-window route is documented (with its GL prerequisite) and proven.

---

## Phase 5: User Story 3 - Honest failure instead of silent stubs (Priority: P3)

**Goal**: When image evidence genuinely can't be produced, return a typed failure naming the blocked stage and classifying it (unsupported-environment vs product-defect) — and never emit a success-shaped non-image.

**Independent Test**: Force an unproducible request (e.g., unsupported renderer mode) and assert a typed failure with classification + message is returned and no image-shaped artifact is written.

### Tests for User Story 3 ⚠️

- [X] T018 [P] [US3] Regression test in `tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs`: force an unproducible request and assert a typed `SceneEvidenceFailure` with stage + classification + message is returned and NO byte payload smaller than a valid image is emitted as success (FR-005, SC-005)
- [X] T019 [P] [US3] Edge-case tests: zero/negative size → `ProductDefect` failure; very large size → success within bounds OR clear resource diagnostic, never a stub (spec Edge Cases)

### Implementation for User Story 3

- [X] T020 [US3] Use the existing failure model (`SceneEvidenceFailure` / `SceneEvidenceFailureClassification` / `EvidenceStage` at `src/Scene/Evidence.fs:11-49`) on the PNG path: when no rasterizer is injected → `UnsupportedEnvironment` naming the blocked `Renderer` stage; preserve the existing `ProductDefect` rule for zero/negative size (FR-005). Add new variants only if a real gap is found
- [X] T021 [US3] Make `renderPng` fail honestly: when rendering can't complete, return the typed failure and write nothing; remove every path that returns the hash/undersized payload as a "success" (FR-002, FR-005, SC-005)
- [X] T022 [US3] **GPU-only-effect degradation disclosure** (spec Edge Cases): enumerate any `SceneNode`/`Paint` features the CPU rasterizer cannot reproduce faithfully (vs the GL path); for each, ensure the headless render still produces an image with a **documented, deterministic** degradation that is **disclosed** (via metadata or a typed advisory) rather than silently dropped — document the list in `specs/221-headless-image-evidence/readiness/` and assert disclosure in a test

**Checkpoint**: All three stories independently functional; success-shaped failures are eliminated and regression-covered; CPU/GL fidelity gaps are disclosed, not silent.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T023 [P] Update the runtime-limitations claims that previously said headless image evidence was unobtainable (FR-009): flip the `no software-renderer fallback` token in the `runtime-limitations.md` contract at `template/base/docs/evidence-formats.md:25-28` (and its schema source), reconcile `real-image-evidence.md` (`evidence-formats.md:68-72`), and revise `docs/usage.md:182-226` plus the T1 row in `docs/harness/capability-baseline.md:27` to describe the new supported headless PNG path
- [X] T024 Update the public-surface (`.fsi`) baseline / surface gate so the new Scene seam and any failure-model additions pass `tests/Package.Tests` (release-only surface gate) — mandatory for this Tier 1 change
- [X] T025 Verify FR-007 non-regression: run the structural-hash, metadata, and evidence-file consumers and confirm unchanged behavior, recorded in `specs/221-headless-image-evidence/readiness/fr007-diff.md`
- [X] T026 Re-run the full baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against T002 to prove no regressions across solution + Package.Tests + samples
- [X] T027 Validate all Success Criteria (SC-001..SC-005) and the spec Edge Cases against the recorded evidence and write the closeout summary in `specs/221-headless-image-evidence/readiness/`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational (esp. the T007 seam)
  - US1 (P1) is the MVP and the integration backbone for `renderPng`
  - US2 (P2) is documentation + live-route proof (GL-required) — independent of US1's CPU path but shares the evidence scaffolding
  - US3 (P3) extends the failure model — meaningful only once US1's real path exists
- **Polish (Phase 6)**: Depends on the user stories being complete

### Critical path within US1

- T007 (seam `.fsi`) → T011 (rasterizer) → T012 (inject) → T013 (rewire `renderPng`); T008/T009/T010 written first and made to pass by T013; T014 after T013.

### Parallel Opportunities

- T003 (Setup) runs parallel to T001/T002 prep
- T006 scaffolding runs parallel within Phase 2
- US1 tests T008/T009/T010 are [P]; US3 tests T018/T019 are [P]
- US2 (docs-centric, GL-required) can proceed in parallel with US3 once Foundational is done
- Polish T023 is [P]

---

## Parallel Example: User Story 1

```bash
# Write US1 tests together (they must FAIL first):
Task: "Determinism + dimensions + non-blank test in tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs"
Task: "Cross-instance determinism check in tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs"
Task: "Concurrency edge-case test in tests/SkiaViewer.Tests/HeadlessImageEvidenceTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup + baseline
2. Phase 2: Foundational — including the **early live smoke run** (T005) that validates the hypotheses against the real headless viewer before any fix, and the FSI-validated seam (T007)
3. Phase 3: US1 — real deterministic headless PNG through `renderPng`
4. **STOP and VALIDATE**: run T008/T009/T010 headless, confirm decodable + non-blank + identical bytes + concurrency-safe
5. Ship the MVP — every downstream consumer can now capture headless pixel proof

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. US1 → test independently → MVP
3. US2 → documented + proven live-window route (GL-required)
4. US3 → honest typed failures, no stubs, disclosed degradation
5. Polish → FR-009 docs, Tier 1 surface gate, full baseline diff

---

## Notes

- `plan.md` is authored for this feature; these tasks are grounded in `spec.md` + `plan.md` + the verified codebase architecture map at the top. The T007 seam signature (`setRealPngRasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) -> unit`) matches `plan.md` (Summary §13, Project Structure §82) — ratify it via the FSI sketch in T007 before `/speckit-implement`.
- This is a **Tier 1** change: `.fsi` updates (T007) and surface-baseline updates (T024) are mandatory, not optional.
- [P] = different files, no dependencies. [Story] label maps task → user story.
- Verify US1/US3 tests fail before implementing; commit after each task or logical group.
- The hard architectural rule: `src/Scene` stays SkiaSharp-free — the rasterizer lives in SkiaViewer and is injected via the T007 seam.

## Completion note (2026-06-30)

All 27 tasks `[X]`. Two carry a **disclosed environment limitation** (recorded with a substitute, not
claimed as live-green): **T005** (early live `OffscreenReadback` smoke run) and **T017** (US2 live-frame
capture) both require a GL/virtual-display host this bare no-GL runner lacks — see
`readiness/smoke-run.md` and `evidence/us2-live-frame.md`. The P1 no-GL deliverable is fully proven live
(real 800×600 PNG, `evidence/representative-game-scene.png`; median 11.9 ms). Two seam-location deviations
from the plan's literal wording (forced by F# compile ordering) are recorded in
`readiness/surface-baseline.md`. Closeout: `readiness/closeout.md`.
