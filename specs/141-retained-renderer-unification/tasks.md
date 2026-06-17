# Tasks: Retained Renderer Unification

**Input**: Design documents from `/specs/141-retained-renderer-unification/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/retained-renderer-unification.md, quickstart.md, `.specify/memory/constitution.md`

**Tests**: Required. The feature specification requires semantic, parity, randomized, public-surface, and readiness evidence.

**Organization**: Tasks are grouped by user story so each story has focused tests, implementation work, and independent validation evidence.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the feature test and evidence locations without changing renderer behavior.

- [X] T001 Create `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` with a module skeleton and shared direct/cold-retained/warm-retained helper signatures
- [X] T002 Add `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` before `Program.fs` in `tests/Controls.Tests/Controls.Tests.fsproj`
- [X] T003 [P] Create `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md` with a command-result table copied from `specs/141-retained-renderer-unification/quickstart.md`
- [X] T004 [P] Create `specs/141-retained-renderer-unification/readiness/retained-renderer-inventory.md` listing current assembly build, warm step, emit/replay, Composition evidence, and Controls.Elmish retained host sites from `src/Controls/Control.fs`, `src/Controls/RetainedRender.fs`, `src/Controls/Composition.fsi`, and `src/Controls.Elmish/ControlsElmish.fs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish the FSI-owned internal contract before test and implementation work touches `.fs` bodies.

**Critical**: No `.fs` implementation task should begin until this phase is complete.

- [X] T005 Evolve `ControlInternals.CurrentNodeAssemblyResult` in `src/Controls/Control.fsi` into the FSI-owned internal assembly result contract for in-flow scene, overlay scene, fingerprints, diagnostics, and child contribution metadata
- [X] T006 Update `src/Controls/RetainedRender.fsi` so `RenderFragment` and retained node state store owner-produced assembly results and invalidation evidence instead of independently constructible retained composition fields
- [X] T007 Update `src/Controls/Composition.fsi` to expose the retained reuse evidence contract for normalized modifier, layer, portal, and legacy evidence shared with Feature 140 classification
- [X] T008 Run signature-facing `dotnet fsi` validation for the changed `.fsi` contracts and record the transcript in `specs/141-retained-renderer-unification/readiness/fsi-contract-validation.md`

**Checkpoint**: Internal signatures describe one assembly owner, retained state as a consumer/cache of owner-produced output, and FSI-facing validation evidence exists before `.fs` implementation.

---

## Phase 3: User Story 1 - Render Through One Authoritative Assembly Producer (Priority: P1) - MVP

**Goal**: Direct rendering, first-frame retained rendering, warm retained rendering, and retained emit/replay all consume the same assembly result owned by `ControlInternals.assembleCurrentNode`.

**Independent Test**: Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature141` and confirm focused direct/cold/warm parity plus source guards pass for current composition semantics.

### Tests for User Story 1

- [X] T009 [US1] Add failing direct/cold-retained/warm-retained parity tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` for nested controls, modifiers, layers, portals, legacy lowering, cache boundaries, diagnostics, fingerprints, and glyph-run proof
- [X] T010 [US1] Add failing source guard tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` asserting `src/Controls/RetainedRender.fs` contains no retained-local composition helper names and all fresh/replay assembly sites call `ControlInternals.assembleCurrentNode`

### Implementation for User Story 1

- [X] T011 [US1] Implement the assembly result shape and fingerprint/diagnostic plumbing in `src/Controls/Control.fs` while keeping `ControlInternals.assembleCurrentNode` the only owner of current-node scene semantics
- [X] T012 [US1] Refactor `Control.renderTree` in `src/Controls/Control.fs` to consume the evolved assembly result and preserve `ControlRenderResult.Scene`, `Bounds`, `Diagnostics`, `EventBindings`, `BoundIds`, and `NodeCount`
- [X] T013 [US1] Refactor `RetainedRender.assembleRetainedNode` and the first-frame `init` build path in `src/Controls/RetainedRender.fs` to store owner-produced assembly results instead of retained-owned in-flow and overlay scene assembly
- [X] T014 [US1] Refactor warm retained `buildFresh`, `carry`, `Update`, child move, and child reorder paths in `src/Controls/RetainedRender.fs` to rebuild affected scopes by calling the assembly owner
- [X] T015 [US1] Refactor retained emit/replay in `src/Controls/RetainedRender.fs` so active animation and `CachedSubtree` replay wrap owner-produced assemblies and rejoin overlays through the assembly owner
- [X] T016 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature141` and record direct/cold/warm parity results in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`

**Checkpoint**: User Story 1 proves the MVP one-owner rendering path independently.

---

## Phase 4: User Story 2 - Preserve Existing Consumer Behavior (Priority: P1)

**Goal**: Consumers observe compatible layout, visuals, overlay layering, text proof behavior, diagnostics, cache parity, and public authoring contracts.

**Independent Test**: Run focused compatibility tests plus existing Feature 139, Feature 140, retained/cache, and public surface checks; every observed change is either zero or documented with migration and versioning rationale.

### Tests for User Story 2

- [X] T017 [US2] Add failing compatibility tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` for no-reuse screens, empty content, bounds, diagnostics, event bindings, bound ids, hit-test ordering, overlay layering, local z-order, portals, legacy lowering, cached subtrees, and glyph-run proof
- [X] T018 [P] [US2] Extend `tests/Controls.Tests/PublicSurfaceTests.fs` with a Feature141 assertion that public `ControlRenderResult`, public Scene constructors, and public Controls authoring surface remain unchanged or require explicit migration evidence

### Implementation for User Story 2

- [X] T019 [US2] Preserve retained `ControlRenderResult` construction compatibility in `src/Controls/RetainedRender.fs` for `Scene`, `Layout`, `Bounds`, `Diagnostics`, `EventBindings`, `BoundIds`, and `NodeCount`
- [X] T020 [US2] Preserve direct rendering compatibility in `src/Controls/Control.fs` for layout, hit-test, diagnostics, overlay layering, and cache-transparent scene output
- [X] T021 [US2] Review `src/Controls/Types.fsi` and `src/Controls/Types.fs` for `ControlRenderResult` surface compatibility and record the zero-change or migration/versioning decision in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T022 [US2] Inspect retained host usage in `src/Controls.Elmish/ControlsElmish.fsi` and `src/Controls.Elmish/ControlsElmish.fs`, then update host wiring or record a no-change decision in `specs/141-retained-renderer-unification/readiness/retained-renderer-inventory.md`
- [X] T023 [US2] Run Feature139, Feature140, PublicSurface, and Feature141 compatibility filters and record results in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`

**Checkpoint**: User Story 2 proves the unification is consumer-compatible or has explicit compatibility evidence.

---

## Phase 5: User Story 3 - Reuse Retained State Without Owning Scene Semantics (Priority: P1)

**Goal**: Retained state stores prior assembly results, identities, cache state, and reuse evidence while invalidating any scope whose render-affecting inputs changed.

**Independent Test**: Exercise stable inputs, visual changes, layout changes, modifier/layer changes, text proof changes, identity changes, child reorder/remove/insert, duplicate keys, and unsafe fallback; retained output must match direct output.

### Tests for User Story 3

- [X] T024 [US3] Add failing invalidation tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` for visual input, layout input, modifier/layer input, text proof input, explicit identity, child ordering, removal, insertion, duplicate keys, and unsafe fresh fallback
- [X] T025 [P] [US3] Extend `tests/Controls.Tests/Audit_Reconcile.fs` with a child reorder/remove stale-output guard for owner-produced retained assembly reuse
- [X] T026 [P] [US3] Extend `tests/Controls.Tests/Audit_Fingerprint.fs` with deterministic assembly-result fingerprint checks for repeated equivalent retained frames

### Implementation for User Story 3

- [X] T027 [US3] Implement retained reuse input comparison in `src/Controls/RetainedRender.fs` requiring equivalent identity, control inputs, layout box, theme, child assemblies, modifier/layer/portal evidence, text proof, and cache boundary fingerprint before reuse
- [X] T028 [US3] Implement invalidation evidence collection in `src/Controls/RetainedRender.fs` for reuse, rebuild, discard, and fresh fallback decisions without changing rendered output
- [X] T029 [US3] Update retained reuse classification wiring in `src/Controls/Composition.fs` so retained invalidation reads the same normalized modifier, layer, portal, and legacy evidence as Feature 140 tests
- [X] T030 [US3] Ensure retained `step` in `src/Controls/RetainedRender.fs` computes next root, caches, diagnostics, metrics, and render result as work-in-progress values before returning committed retained state
- [X] T031 [US3] Run retained, reconcile, cache, and fingerprint filters from `specs/141-retained-renderer-unification/quickstart.md` and record invalidation category coverage in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`

**Checkpoint**: User Story 3 proves retained reuse is deterministic, invalidates correctly, and never becomes a second scene-semantics owner.

---

## Phase 6: User Story 4 - Prove the Drift Bug Class Is Closed (Priority: P2)

**Goal**: Provide durable tests and evidence showing there is one assembly owner, zero retained-only composition rule sets, and clear scope boundaries for later rendering phases.

**Independent Test**: Run randomized equivalence and source-review tests; the evidence package must identify the owner, retained reuse boundary, removed responsibilities, randomized count/seed, and out-of-scope work.

### Tests for User Story 4

- [X] T032 [US4] Add failing randomized equivalence tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` generating at least 200 control trees or composition chains and comparing direct, cold retained, warm retained, cache-on/off, diagnostics, fingerprints, and reuse evidence
- [X] T033 [US4] Add source-review tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` asserting exactly one assembly owner in `src/Controls/Control.fs` and zero retained-only composition rule sets in `src/Controls/RetainedRender.fs`
- [X] T034 [US4] Add out-of-scope guard tests in `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` scanning `src/Controls` for no new public retained renderer APIs, portable scene serialization, compositor promotion, damage-scissored presentation, intrinsic layout protocol, overlay interaction state, or full text shaping in this feature

### Implementation for User Story 4

- [X] T035 [US4] Write unification evidence in `specs/141-retained-renderer-unification/readiness/one-owner-evidence.md` naming the assembly owner, retained reuse boundary, removed responsibilities, compatibility results, and remaining out-of-scope work
- [X] T036 [US4] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter RetainedRandomizedEquivalence` and record randomized seed, generated count, and failure count in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T037 [US4] Run architecture guard tests from `tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs` and record one-owner and zero-retained-rule evidence in `specs/141-retained-renderer-unification/readiness/one-owner-evidence.md`

**Checkpoint**: User Story 4 proves the direct-vs-retained drift class is closed for this feature scope.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete broad validation, readiness evidence, and final compatibility review.

- [X] T038 [P] Run `dotnet build FS.GG.Rendering.slnx -c Debug --no-restore` and record status in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T039 Run the broad deterministic `dotnet test` commands from `specs/141-retained-renderer-unification/quickstart.md` after T038 completes and record pass/fail status in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T040 [P] Run `dotnet fsi scripts/refresh-surface-baselines.fsx` and record baseline diff status in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T041 [P] Run layout, scene, and SkiaViewer compatibility commands from `specs/141-retained-renderer-unification/quickstart.md` and record any GL or window-system limitation in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T042 [P] Attempt offscreen harness validation with `dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature141-harness` and record pass, fail, or unsupported-environment status in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`
- [X] T043 Review `specs/141-retained-renderer-unification/quickstart.md` against final commands and update any changed validation command, expected outcome, or limitation note
- [X] T044 Confirm no unintended public surface or package change by reviewing `git diff -- src/Controls/*.fsi src/Controls/*.fsproj tests/Controls.Tests/PublicSurfaceTests.fs` and record the conclusion in `specs/141-retained-renderer-unification/readiness/feature141-retained-renderer-unification.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Setup (Phase 1): No dependencies.
- Foundational (Phase 2): Depends on Setup and blocks `.fs` implementation.
- User Story 1 (Phase 3): Depends on Foundational and is the MVP.
- User Story 2 (Phase 4): Depends on Foundational; tests can be authored alongside User Story 1, while implementation validates against the final assembly result shape.
- User Story 3 (Phase 5): Depends on Foundational; reuse implementation often follows User Story 1 changes but remains independently testable through invalidation scenarios.
- User Story 4 (Phase 6): Depends on the desired P1 stories because randomized and architecture evidence should prove the final retained renderer state.
- Polish (Phase 7): Depends on all desired user stories.

### User Story Dependencies

- User Story 1 (P1): Start after Phase 2; no dependency on other stories.
- User Story 2 (P1): Start after Phase 2; independent compatibility checks, with final validation after User Story 1 implementation.
- User Story 3 (P1): Start after Phase 2; independent invalidation checks, with final validation after User Story 1 implementation.
- User Story 4 (P2): Start after P1 stories are complete enough to prove one-owner behavior.

### Parallel Opportunities

- T003 and T004 can run in parallel with T001/T002 after repository paths are known.
- T018 can run in parallel with T017 because it edits `tests/Controls.Tests/PublicSurfaceTests.fs`.
- T025 and T026 can run in parallel because they edit separate audit files.
- T038, T040, T041, and T042 can run in parallel after implementation is complete; T039 waits for T038 because broad validation may rely on build output.

---

## Parallel Example: User Story 2

```text
Task: "T017 [US2] Add failing compatibility tests in tests/Controls.Tests/Feature141RetainedRendererUnificationTests.fs"
Task: "T018 [P] [US2] Extend tests/Controls.Tests/PublicSurfaceTests.fs with a Feature141 assertion"
```

## Parallel Example: User Story 3

```text
Task: "T025 [P] [US3] Extend tests/Controls.Tests/Audit_Reconcile.fs with a child reorder/remove stale-output guard"
Task: "T026 [P] [US3] Extend tests/Controls.Tests/Audit_Fingerprint.fs with deterministic assembly-result fingerprint checks"
```

## Parallel Example: Final Validation

```text
Task: "T038 [P] Run dotnet build FS.GG.Rendering.slnx -c Debug --no-restore"
Task: "T040 [P] Run dotnet fsi scripts/refresh-surface-baselines.fsx"
Task: "T041 [P] Run layout, scene, and SkiaViewer compatibility commands"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Complete User Story 1 tests T009-T010 and confirm they fail for the retained-only drift risks they describe.
3. Complete User Story 1 implementation T011-T015.
4. Complete T016 and stop to validate direct/cold/warm parity before adding broader compatibility and randomized coverage.

### Incremental Delivery

1. Deliver User Story 1 to establish the one-owner rendering path.
2. Deliver User Story 2 to prove consumer behavior and public surface compatibility.
3. Deliver User Story 3 to prove retained reuse, invalidation, and atomic commit behavior.
4. Deliver User Story 4 to lock in randomized and architecture evidence.
5. Complete Polish validation and readiness notes.

### Validation Notes

- Tests must be written before `.fs` implementation for each user story.
- `.fsi` changes and T008 FSI validation must precede `.fs` changes for internal contract changes.
- Public authoring and Scene contracts should remain unchanged unless T021 records migration and versioning rationale.
- Verification limitations must be recorded as limitations, not passes.
- Every checklist task above follows `- [ ] T### [P?] [US?] Description with file path`.

## Completion Notes

- Completed on 2026-06-17 with focused Feature 141, Feature 139, Feature 140, public-surface, audit, solution build, non-Controls broad deterministic suites, surface-baseline refresh, and offscreen harness evidence recorded under `readiness/`.
- `Feature091` was attempted with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091 --no-build` and interrupted after more than two minutes without a result in this shell; this is recorded as a validation limitation in `readiness/feature141-retained-renderer-unification.md`.
