# Tasks: Modifier Layer IR Foundation (Feature 140)

**Input**: Design documents from `/specs/140-modifier-layer-ir/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/modifier-layer-foundation.md`, `quickstart.md`

**Tests**: Required by the feature specification and constitution. Signature tasks precede semantic tests where `.fsi` surface changes are involved; tests still precede `.fs` implementation.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel after its stated prerequisites because it touches different files
- **[Story]**: User story label for traceability, e.g. `[US1]`
- **File paths**: Every task names the concrete file path or paths it changes or records evidence into

## Phase 1: Setup (Shared Evidence and Planning Artifacts)

**Purpose**: Create the feature-owned evidence targets required by Tier 1 architecture work.

- [X] T001 Create the public-surface and legacy-form compatibility plan skeleton in specs/140-modifier-layer-ir/compatibility-plan.md
- [X] T002 [P] Create the pixel and rendering disclosure ledger skeleton in specs/140-modifier-layer-ir/contracts/rebaseline-ledger.md
- [X] T003 [P] Create the validation readiness log skeleton in specs/140-modifier-layer-ir/readiness.md
- [X] T004 [P] Create the verification limitations log skeleton in specs/140-modifier-layer-ir/verification-limitations.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add compile-time slots for the internal composition model and focused Feature 140 test suites.

**Critical**: No user story implementation should start until this phase compiles.

- [X] T005 Draft the internal composition signature skeleton in src/Controls/Composition.fsi for modifier effects, classifications, chains, composition nodes, portals, layer hosts, legacy lowering, and compatibility evidence helpers
- [X] T006 Implement a compile-only composition module skeleton matching the signature in src/Controls/Composition.fs
- [X] T007 Add src/Controls/Composition.fsi and src/Controls/Composition.fs to the F# compile order in src/Controls/Controls.fsproj after src/Controls/Attributes.fs and before src/Controls/Control.fsi
- [X] T008 Create and register focused Controls feature test modules in tests/Controls.Tests/Feature140ModifierLayerTests.fs, tests/Controls.Tests/Feature140ModifierNormalizationTests.fs, tests/Controls.Tests/Feature140ZOrderTests.fs, tests/Controls.Tests/Feature140PortalLayerTests.fs, tests/Controls.Tests/Feature140LegacyCompatibilityTests.fs, tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs, and tests/Controls.Tests/Controls.Tests.fsproj
- [X] T009 [P] Create and register the Scene glyph-run test module in tests/Scene.Tests/Feature140GlyphRunTests.fs and tests/Scene.Tests/Scene.Tests.fsproj
- [X] T010 [P] Create and register the SkiaViewer glyph-run rendering test module in tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs and tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj

**Checkpoint**: The solution compiles with empty Feature 140 modules and user-story tests can now be added.

---

## Phase 3: User Story 1 - Compose Visual Semantics Consistently (Priority: P1) MVP

**Goal**: Ordered visual effects are represented as values, classified consistently, normalized safely, and folded through the existing Controls assembly seam.

**Independent Test**: Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140` and confirm modifier ordering, invalidation classification, normalization, diagnostics, and cache fingerprints behave as specified.

### Surface and Tests for User Story 1

- [X] T011 [US1] Finalize modifier effect, effect classification, modifier chain, normalization, diagnostics, and fingerprint APIs in src/Controls/Composition.fsi
- [X] T012 [P] [US1] Add failing supported-effect ordering and invalidation classification tests for clip, opacity, offset, transform, background, overlay, cache boundary, local z-order, and layer hint in tests/Controls.Tests/Feature140ModifierLayerTests.fs
- [X] T013 [P] [US1] Add failing normalization, idempotence, diagnostic equivalence, and byte-stable fingerprint tests for at least 12 representative chains in tests/Controls.Tests/Feature140ModifierNormalizationTests.fs
- [X] T014 [US1] Document the canonical modifier effect order and invalidation classification table in src/Controls/Composition.fsi and specs/140-modifier-layer-ir/compatibility-plan.md
- [X] T015 [P] [US1] Add failing cache-enabled versus cache-disabled parity tests for modifier-chain scenarios in tests/Controls.Tests/Feature140ModifierNormalizationTests.fs

### Implementation for User Story 1

- [X] T016 [US1] Implement modifier effect values, the classification table, normalization rules, diagnostics, and deterministic fingerprint inputs in src/Controls/Composition.fs
- [X] T017 [US1] Fold modifier chains through the shared current-node assembly seam in src/Controls/Control.fs
- [X] T018 [US1] Use the shared effect classification table for retained invalidation and work-reduction evidence in src/Controls/RetainedRender.fsi and src/Controls/RetainedRender.fs
- [X] T019 [US1] Preserve cache fingerprints for original and normalized equivalent chains in src/Controls/Composition.fs and src/Controls/RetainedRender.fs
- [X] T020 [US1] Record the focused User Story 1 validation command and result in specs/140-modifier-layer-ir/readiness.md

**Checkpoint**: User Story 1 is independently testable as the MVP.

---

## Phase 4: User Story 2 - Replace Ad Hoc Overlays with Portals and Layers (Priority: P1)

**Goal**: Local z-order stays scoped to siblings, overlay-like content lowers to portals, and paint order plus hit-test order come from one ordered contribution stream.

**Independent Test**: Render and hit-test scenes with in-flow content, clipped ancestors, equal and unequal local z-order values, multiple portal layers, missing portal evidence, and legacy overlay behavior.

### Surface and Tests for User Story 2

- [X] T021 [US2] Extend portal, layer host, ordered contribution, paint-order, and hit-order APIs in src/Controls/Composition.fsi
- [X] T022 [P] [US2] Add failing local z-order scope, stable sort, and equal-z declaration-order fallback tests in tests/Controls.Tests/Feature140ZOrderTests.fs
- [X] T023 [P] [US2] Add failing portal escape, missing target, missing anchor, empty layer, multiple layer paint-order, and hit-order tests in tests/Controls.Tests/Feature140PortalLayerTests.fs
- [X] T024 [P] [US2] Add failing transformed-ancestor portal anchor evidence and portal/layer cache parity tests in tests/Controls.Tests/Feature140PortalLayerTests.fs

### Implementation for User Story 2

- [X] T025 [US2] Implement local z-order stable sorting and declaration-index tie breakers in src/Controls/Composition.fs
- [X] T026 [US2] Implement portal collection, layer-host ordering, escaping-layer behavior, transformed-ancestor anchor evidence, and actionable portal diagnostics in src/Controls/Composition.fs
- [X] T027 [US2] Lower existing overlay-like control output into portal and layer contributions in src/Controls/Control.fs and src/Controls/Widgets/Overlay.fs
- [X] T028 [US2] Derive Controls hit-test priority from the same ordered contribution stream used for paint order in src/Controls/Control.fs and src/Controls/RetainedRender.fs
- [X] T029 [US2] Record portal and layer validation results plus any pixel deltas in specs/140-modifier-layer-ir/readiness.md and specs/140-modifier-layer-ir/contracts/rebaseline-ledger.md

**Checkpoint**: User Story 2 can prove layer and portal ordering without relying on the old overlay split.

---

## Phase 5: User Story 3 - Preserve Legacy Scene Compatibility (Priority: P1)

**Goal**: Existing clipping, translation, perspective, cached subtree, text, and overlay forms lower through the new foundation while preserving compatibility by default.

**Independent Test**: Run legacy compatibility scenes and existing Feature137, Feature139, Feature091, Feature092, cache audit, text cache, picture cache, and layout audit commands from quickstart.md.

### Surface and Tests for User Story 3

- [X] T030 [US3] Extend legacy lowering status, migration note, and compatibility evidence APIs in src/Controls/Composition.fsi
- [X] T031 [P] [US3] Add failing legacy clipping, translation, and perspective compatibility tests in tests/Controls.Tests/Feature140LegacyCompatibilityTests.fs
- [X] T032 [P] [US3] Add failing cached subtree, text, text-run, and overlay compatibility tests in tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs

### Implementation for User Story 3

- [X] T033 [US3] Implement legacy lowering for clipping, translation, perspective, cached subtree, text, text-run, and overlay forms in src/Controls/Composition.fs
- [X] T034 [US3] Route legacy clip, translate, perspective, cache, text, and overlay forms through the composition lowering path in src/Controls/Control.fs
- [X] T035 [US3] Preserve cache-enabled versus cache-disabled and full versus retained parity for legacy-lowered scenes in src/Controls/RetainedRender.fs
- [X] T036 [US3] Document supported, deprecated, and intentionally changed legacy forms in specs/140-modifier-layer-ir/compatibility-plan.md
- [X] T037 [US3] Record legacy compatibility oracle command results in specs/140-modifier-layer-ir/readiness.md

**Checkpoint**: User Story 3 demonstrates compatibility or documented intentional deltas for existing scene forms.

---

## Phase 6: User Story 4 - Establish a Glyph-Run Data Shape for Future Text Work (Priority: P2)

**Goal**: Add the smallest public Scene glyph-run proof surface needed for deterministic measurement, drawing, diagnostics, and fingerprints without implementing full shaping.

**Independent Test**: Run `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature140` and `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature140 -c Release`.

### Surface and Tests for User Story 4

- [X] T038 [US4] Design the public glyph-run proof data, diagnostic, fingerprint, and drawable constructor surface in src/Scene/Scene.fsi without adding public modifier, layer, portal, or layout-container Scene nodes
- [X] T039 [P] [US4] Add failing Scene glyph-run data, measurement, fallback diagnostic, and stable fingerprint tests for at least five deterministic sample strings in tests/Scene.Tests/Feature140GlyphRunTests.fs
- [X] T040 [P] [US4] Add failing SkiaViewer glyph-run draw proof and non-opt-in text fallback compatibility tests in tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs
- [X] T041 [US4] Add an F# Interactive or prelude API exercise for the public glyph-run proof surface in tests/Scene.Tests/Feature140GlyphRunTests.fs and record the expected command in specs/140-modifier-layer-ir/readiness.md
- [X] T042 [P] [US4] Add failing glyph-run proof cache parity tests in tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs and tests/Scene.Tests/Feature140GlyphRunTests.fs

### Implementation for User Story 4

- [X] T043 [US4] Implement glyph-run proof records, constructors, diagnostics, and deterministic fingerprinting in src/Scene/Scene.fs
- [X] T044 [US4] Add bundled-font glyph-run measurement helpers for the proof surface in src/SkiaViewer/Fonts.fsi and src/SkiaViewer/Fonts.fs
- [X] T045 [US4] Add the exhaustive glyph-run proof drawing case to src/SkiaViewer/SceneRenderer.fs
- [X] T046 [US4] Preserve existing non-opt-in text fallback behavior in src/Scene/Scene.fs and src/SkiaViewer/SceneRenderer.fs
- [X] T047 [US4] Update public-surface baselines and migration notes for glyph-run changes in tests/surface-baselines/FS.GG.UI.Scene.txt, tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, tests/surface-baselines/FS.GG.UI.Controls.txt, and specs/140-modifier-layer-ir/compatibility-plan.md

**Checkpoint**: User Story 4 proves glyph-run representation without expanding into full text shaping.

---

## Phase 7: User Story 5 - Produce Compatibility and Evidence for P3 Planning (Priority: P2)

**Goal**: Deliver an evidence package that tells P3 maintainers what changed, what stayed compatible, what remains deferred, and which verification limitations are known.

**Independent Test**: Review the evidence package and rerun the required validation suites listed in quickstart.md.

### Evidence Tasks for User Story 5

- [X] T048 [P] [US5] Map every contract clause to the command, test file, or evidence artifact that proves it in specs/140-modifier-layer-ir/evidence-map.md
- [X] T049 [P] [US5] Record deferred non-goals and verification limitations for retained unification, full shaping, overlay interaction state, portable serialization, compositor work, intrinsic layout, GL availability, and pre-existing failures in specs/140-modifier-layer-ir/verification-limitations.md
- [X] T050 [US5] Run focused Controls Feature140 validation and record the command, status, and failure attribution in specs/140-modifier-layer-ir/readiness.md
- [X] T051 [US5] Run legacy compatibility oracle commands from specs/140-modifier-layer-ir/quickstart.md and record the command, status, and failure attribution in specs/140-modifier-layer-ir/readiness.md
- [X] T052 [US5] Run glyph-run Scene and SkiaViewer proof commands from specs/140-modifier-layer-ir/quickstart.md and record the command, status, and failure attribution in specs/140-modifier-layer-ir/readiness.md
- [X] T053 [US5] Run ./fake.sh build -t PackageSurfaceCheck, ./fake.sh build -t ControlsRenderingCheck, and ./fake.sh build -t VerifyPreflight, then record public-surface and rendering status in specs/140-modifier-layer-ir/readiness.md and specs/140-modifier-layer-ir/compatibility-plan.md
- [X] T054 [US5] Run the offscreen rendering harness, store evidence under artifacts/feature140-harness/run.json, and record pass, intentional delta, or environment limitation in specs/140-modifier-layer-ir/readiness.md and specs/140-modifier-layer-ir/contracts/rebaseline-ledger.md

**Checkpoint**: User Story 5 provides the compatibility and evidence package P3 planning needs.

---

## Phase 8: Polish and Cross-Cutting Concerns

**Purpose**: Finalize documentation, verification, and consistency after the selected stories are complete.

- [X] T055 [P] Update the P2 status and follow-up boundary in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T056 [P] Update final validated commands and expected outcomes in specs/140-modifier-layer-ir/quickstart.md
- [X] T057 Review src/Controls/Composition.fs, src/Controls/Control.fs, src/Controls/RetainedRender.fs, src/Scene/Scene.fs, src/SkiaViewer/Fonts.fs, and src/SkiaViewer/SceneRenderer.fs against the `.fsi` visibility rule and record the result in specs/140-modifier-layer-ir/readiness.md
- [X] T058 Run the broad test suites from specs/140-modifier-layer-ir/quickstart.md and record final pass, pre-existing failure, or environment-limitation status in specs/140-modifier-layer-ir/readiness.md
- [X] T059 Run dotnet fsi scripts/refresh-surface-baselines.fsx and update tests/surface-baselines/FS.GG.UI.Scene.txt, tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, and tests/surface-baselines/FS.GG.UI.Controls.txt if public surface changed
- [X] T060 Perform final consistency review across specs/140-modifier-layer-ir/compatibility-plan.md, specs/140-modifier-layer-ir/contracts/rebaseline-ledger.md, specs/140-modifier-layer-ir/evidence-map.md, specs/140-modifier-layer-ir/verification-limitations.md, and specs/140-modifier-layer-ir/readiness.md

---

## Dependencies and Execution Order

### Phase Dependencies

- **Phase 1 Setup**: No dependencies.
- **Phase 2 Foundational**: Depends on Phase 1 and blocks all user stories.
- **User Stories**: Depend on Phase 2. User Stories 1, 2, 3, and 4 can be staffed in parallel after the foundational compile skeleton exists, but the recommended risk order is US1, US2, US3, US4.
- **User Story 5**: Depends on the selected implementation stories whose evidence it records.
- **Polish**: Depends on all selected user stories and evidence tasks.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2. No dependency on other stories. This is the MVP.
- **US2 (P1)**: Starts after Phase 2. Portal tests involving clipped ancestors are strongest once US1 modifier folding exists.
- **US3 (P1)**: Starts after Phase 2. Full compatibility validation should run after US1 and US2 because legacy lowering uses their modifier and layer paths.
- **US4 (P2)**: Starts after Phase 2. Independent of Controls portal semantics except final package surface and broad verification.
- **US5 (P2)**: Starts once there is evidence to record. Final completion depends on all desired implementation stories.

### Within Each User Story

- `.fsi` signature tasks happen before semantic tests when public or cross-file F# surface changes.
- Tests must be written and observed failing before `.fs` implementation tasks.
- Implementation follows the tested signature and records readiness evidence before the story checkpoint.

---

## Parallel Execution Examples

### User Story 1

```bash
Task: "T012 Add supported-effect ordering and invalidation tests in tests/Controls.Tests/Feature140ModifierLayerTests.fs"
Task: "T013 Add normalization and fingerprint tests in tests/Controls.Tests/Feature140ModifierNormalizationTests.fs"
Task: "T015 Add modifier-chain cache parity tests in tests/Controls.Tests/Feature140ModifierNormalizationTests.fs"
```

### User Story 2

```bash
Task: "T022 Add local z-order tests in tests/Controls.Tests/Feature140ZOrderTests.fs"
Task: "T023 Add portal and layer tests in tests/Controls.Tests/Feature140PortalLayerTests.fs"
Task: "T024 Add transformed-anchor and portal cache parity tests in tests/Controls.Tests/Feature140PortalLayerTests.fs"
```

### User Story 3

```bash
Task: "T031 Add clipping, translation, and perspective compatibility tests in tests/Controls.Tests/Feature140LegacyCompatibilityTests.fs"
Task: "T032 Add cache, text, and overlay compatibility tests in tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs"
```

### User Story 4

```bash
Task: "T039 Add Scene glyph-run proof tests in tests/Scene.Tests/Feature140GlyphRunTests.fs"
Task: "T040 Add SkiaViewer glyph-run rendering tests in tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs"
Task: "T042 Add glyph-run cache parity tests in tests/Controls.Tests/Feature140LegacyCacheTextOverlayTests.fs and tests/Scene.Tests/Feature140GlyphRunTests.fs"
```

### User Story 5

```bash
Task: "T048 Map contract clauses in specs/140-modifier-layer-ir/evidence-map.md"
Task: "T049 Record deferred non-goals and verification limitations in specs/140-modifier-layer-ir/verification-limitations.md"
```

Validation commands in US5 may run in separate shells, but updates to specs/140-modifier-layer-ir/readiness.md should be serialized.

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup artifacts.
2. Complete Phase 2 foundational compile skeleton and test registrations.
3. Complete Phase 3 User Story 1.
4. Stop and validate with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature140`.
5. Record MVP evidence in specs/140-modifier-layer-ir/readiness.md.

### Incremental Delivery

1. Deliver US1 to prove modifier ordering, invalidation classification, normalization, and fingerprints.
2. Deliver US2 to replace overlay special cases with portal and layer ordering.
3. Deliver US3 to prove legacy compatibility through lowering.
4. Deliver US4 to add the minimal glyph-run proof surface and drawing path.
5. Deliver US5 evidence and polish tasks to make P3 planning safe.

### Parallel Team Strategy

1. One contributor completes the foundational compile skeleton while others prepare evidence templates.
2. After Phase 2, split by file ownership: Controls modifier tests, Controls portal tests, Controls legacy tests, Scene glyph-run tests, and SkiaViewer glyph-run tests.
3. Serialize edits to shared files such as src/Controls/Composition.fs, src/Controls/Control.fs, src/Controls/RetainedRender.fs, and specs/140-modifier-layer-ir/readiness.md.

---

## Notes

- Keep modifier, layer, and portal nodes internal to Controls for this feature.
- Keep full shaping, bidi, expanded fallback, line breaking, retained-renderer unification, overlay interaction state, portable serialization, compositor promotion, and intrinsic layout out of scope.
- Public surface changes require `.fsi` edits, semantic tests, surface baseline updates, migration notes, and versioning recommendations.
- Pixel or rendering baseline changes require entries in specs/140-modifier-layer-ir/contracts/rebaseline-ledger.md before readiness.
