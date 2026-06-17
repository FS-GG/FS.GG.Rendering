# Tasks: Shared Assembly Extraction (Feature 139)

**Input**: Design documents from `/specs/139-shared-assembly-extraction/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/assembly-compatibility.md, quickstart.md

**Tests**: Required by the feature specification. Write the listed tests before implementation and confirm
they fail for the missing shared ownership, missing compatibility behavior, or missing work-reduction
non-regression proof. Every validation command must record pass/fail/skip status and distinguish
environment/pre-existing failures from feature-caused failures.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an
independent increment after the shared foundation is complete.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User story label from spec.md: US1, US2, US3.
- Every task includes exact repository file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Register the Feature 139 test module so story tests can be added before implementation.

- [X] T001 Register `Feature139AssemblyExtractionTests.fs` in `tests/Controls.Tests/Controls.Tests.fsproj` and create the empty module file at `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: Establish a compiling internal assembly seam before story tests and call-site routing begin.

**CRITICAL**: No user story work should begin until this phase is complete.

- [X] T002 Add a compile-only internal assembly result type and placeholder `assembleCurrentNode` seam in `src/Controls/Control.fsi` and `src/Controls/Control.fs`

**Checkpoint**: The internal seam is reachable from `Control.renderTree`, `RetainedRender`, and `Controls.Tests` through existing internal visibility, but behavior has not been routed through it yet.

---

## Phase 3: User Story 1 - Change Composition Rules in One Place (Priority: P1) MVP

**Goal**: Maintainers can find and test one authoritative current-semantics assembly rule for own paint,
child in-flow paint, child overlay paint, container clipping, and overlay promotion.

**Independent Test**: `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139` proves
the internal seam implements the contract and that immediate, retained initial, and retained warm paths agree
for focused composition fixtures.

### Tests for User Story 1

- [X] T003 [US1] Add direct failing assembly-contract tests for leaf, box-less, clipped-child, and overlay-node behavior in `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`
- [X] T004 [US1] Add failing immediate-versus-retained parity and work-reduction non-regression tests for nested clipping, offsets, cache boundaries, overlays, no extra full-tree pass, and warm retained reuse in `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`
- [X] T005 [US1] Add a failing source-ownership guard that rejects direct `composeContainerScene` plus overlay split reimplementation outside the shared seam in `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`

### Implementation for User Story 1

- [X] T006 [US1] Implement the internal `assembleCurrentNode` current-semantics rule in `src/Controls/Control.fs`
- [X] T007 [US1] Route `Control.renderTree` paint recursion through `assembleCurrentNode` in `src/Controls/Control.fs`
- [X] T008 [US1] Route retained first-frame build, fresh rebuild, carry rebuild, and changed-node rebuild through `assembleCurrentNode` in `src/Controls/RetainedRender.fs`
- [X] T009 [US1] Route the retained cache/replay emit walk through `assembleCurrentNode` in `src/Controls/RetainedRender.fs`
- [X] T010 [US1] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139` and record the result in `specs/139-shared-assembly-extraction/quickstart.md`

**Checkpoint**: US1 is functional and independently testable; all current assembly call sites use the shared seam.

---

## Phase 4: User Story 2 - Preserve Existing Rendering Behavior (Priority: P1)

**Goal**: Existing screens, diagnostics, event/bounds metadata, and cache parity remain unchanged after the
internal refactor.

**Independent Test**: Existing parity, cache, layout, and surface checks pass with no intentional public
surface drift and no intentional rendering baseline changes.

### Tests for User Story 2

- [X] T011 [US2] Add compatibility tests for empty controls, overlay-free trees, overlay trees, clipped trees, bounds, diagnostics, event bindings, and bound ids in `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`
- [X] T012 [P] [US2] Add a cache-disabled parity fixture for a cached subtree inside clipped and offset content in `tests/Controls.Tests/Audit_PictureCache.fs`

### Implementation for User Story 2

- [X] T013 [P] [US2] Update internal XML comments to name the shared assembly owner and remove stale duplicated-site wording in `src/Controls/Control.fsi` and `src/Controls/RetainedRender.fsi`
- [X] T014 [US2] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature137` and record the result in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T015 [US2] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature091` and `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature092` and record the results in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T016 [US2] Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_PictureCache`, `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_MemoCache`, and `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Audit_TextCache` and record the results in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T017 [US2] Run `dotnet test tests/Layout.Tests/Layout.Tests.fsproj --filter Audit` and record the result in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T018 [US2] Run `./fake.sh build -t PackageSurfaceCheck` and record the zero-public-drift result in `specs/139-shared-assembly-extraction/quickstart.md`

**Checkpoint**: US2 is functional and independently testable; existing behavior and public surface remain unchanged.

---

## Phase 5: User Story 3 - Prepare Later Radical Rendering Work (Priority: P2)

**Goal**: Later modifier, portal, and retained-renderer unification planning can target the shared seam without
guessing which behavior is already centralized.

**Independent Test**: Review evidence shows one assembly owner, the required call-site list, and explicit
exclusion of modifier algebra, portals, public IR changes, intrinsic layout, text shaping, compositor, and
portable protocol changes.

### Tests for User Story 3

- [X] T019 [US3] Add an architecture evidence test for the required caller list and forbidden later-phase terms in `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`

### Implementation for User Story 3

- [X] T020 [P] [US3] Document the final assembly owner, routed call sites, and later-phase exclusions in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T021 [P] [US3] Add source comments that identify R1a scope and defer R2/R1b semantics near the shared seam in `src/Controls/Control.fsi`
- [X] T022 [US3] Verify `src/Scene/Scene.fsi`, `src/SkiaViewer/SceneRenderer.fs`, and `tests/surface-baselines/FS.GG.UI.Scene.txt` have no feature-139 public IR or rendering-baseline changes, then record the result in `specs/139-shared-assembly-extraction/quickstart.md`

**Checkpoint**: US3 is functional and independently testable; the next radical rendering phases have a clear, documented target seam.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Validate the complete feature, record evidence, and keep the scope clean.

- [X] T023 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj` and record the result in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T024 Run `./fake.sh build -t ControlsRenderingCheck` and record the result in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T025 Run `./fake.sh build -t VerifyPreflight` and record the result or environment limitation in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T026 Review the final diff against `specs/139-shared-assembly-extraction/contracts/assembly-compatibility.md` and record the scope confirmation in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T027 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature096` and `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature097` to verify existing work-reduction metrics and no extra full-tree rendering pass, then record the result in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T028 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj -c Release` and `dotnet run --project tests/Rendering.Harness -- offscreen --json --out artifacts/feature139-harness` to cover the planned SkiaViewer and Rendering.Harness gates, then record pass or GL/headless skip evidence in `specs/139-shared-assembly-extraction/quickstart.md`
- [X] T029 Review every validation result recorded in `specs/139-shared-assembly-extraction/quickstart.md` and ensure any verification limitation or pre-existing external failure names the command, observed status, environment facts, and why it is not attributable to feature 139

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Phase 1 and blocks all user stories.
- **US1 (Phase 3)**: Depends on Phase 2. This is the MVP and should complete before US2 and US3.
- **US2 (Phase 4)**: Depends on US1 because it validates the routed shared seam against existing behavior.
- **US3 (Phase 5)**: Depends on US1 and can proceed alongside US2 after the shared seam exists, but final scope evidence should incorporate US2's compatibility results.
- **Polish (Phase 6)**: Depends on all desired user stories.

### User Story Dependencies

- **US1 (P1)**: Start after Phase 2; no dependency on other stories.
- **US2 (P1)**: Start after US1; relies on the shared seam being routed.
- **US3 (P2)**: Start after US1; final evidence should be completed after US2 validation.

### Within Each User Story

- Add tests and confirm they fail for the missing behavior before implementation.
- Update `.fsi` signatures or internal contracts before `.fs` bodies when a contract changes.
- Route immediate rendering before retained routing only if that makes failures easier to isolate.
- Run the independent test command before moving to the next checkpoint.

---

## Parallel Opportunities

- T012 can run alongside T011 because it touches `tests/Controls.Tests/Audit_PictureCache.fs` while T011 touches `tests/Controls.Tests/Feature139AssemblyExtractionTests.fs`.
- T013 can run alongside T011 and T012 after US1 because it touches internal `.fsi` docs rather than test bodies.
- T020 and T021 can run in parallel after US1 because they touch `quickstart.md` and `Control.fsi` respectively.
- US3 documentation work can proceed alongside US2 compatibility-command runs once US1 has routed the shared seam.

## Parallel Example: User Story 2

```bash
Task: "T011 Add compatibility tests in tests/Controls.Tests/Feature139AssemblyExtractionTests.fs"
Task: "T012 Add cache-disabled parity fixture in tests/Controls.Tests/Audit_PictureCache.fs"
Task: "T013 Update internal XML comments in src/Controls/Control.fsi and src/Controls/RetainedRender.fsi"
```

## Parallel Example: User Story 3

```bash
Task: "T020 Document final assembly owner in specs/139-shared-assembly-extraction/quickstart.md"
Task: "T021 Add R1a scope comments in src/Controls/Control.fsi"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 and Phase 2.
2. Add the US1 failing tests.
3. Implement and route the shared current-node assembly seam.
4. Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature139`.
5. Stop and inspect the diff to confirm the change is still behavior-preserving.

### Incremental Delivery

1. US1 centralizes assembly ownership.
2. US2 proves existing behavior, cache parity, layout parity, and public surface remain unchanged.
3. US3 records the target seam and scope fence for later R2/R1b planning.
4. Polish runs full focused and broad validation.

### Scope Guard

Do not add modifier algebra, portals, public scene IR, intrinsic layout, text shaping, compositor, portable
protocol, new dependencies, or public control builders in this feature. If any of those appear necessary,
stop and revise the feature spec before implementation.
