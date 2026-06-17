# Tasks: Render-Anywhere Scene Protocol

**Input**: Design documents from `/specs/146-render-anywhere-protocol/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Required. The specification defines mandatory user scenarios, measurable outcomes, and quickstart validation commands for the portable codec, reference rendering, browser feasibility, package surface, and compatibility ledger.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as an independently testable increment.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create shared readiness locations and project evidence scaffolding before public surface work begins.

- [X] T001 Create readiness artifact directories and placeholder `.gitkeep` files in specs/146-render-anywhere-protocol/readiness/roundtrip/.gitkeep, specs/146-render-anywhere-protocol/readiness/reference/.gitkeep, and specs/146-render-anywhere-protocol/readiness/browser/.gitkeep
- [X] T002 Create the initial compatibility ledger skeleton in specs/146-render-anywhere-protocol/readiness/compatibility-ledger.md
- [X] T003 Create the implementation validation summary placeholder in specs/146-render-anywhere-protocol/readiness/validation-summary.md
- [X] T004 Create the round-trip corpus evidence placeholder in specs/146-render-anywhere-protocol/readiness/roundtrip/corpus.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Add shared harness and evidence foundations that later stories depend on without implementing story-specific behavior.

**Critical**: No user story implementation should start until this phase is complete.

- [X] T005 Draft shared render-anywhere corpus and artifact path contracts in tests/Rendering.Harness/RenderAnywhere.fsi
- [X] T006 Implement shared corpus identifiers, scenario names, and readiness path helpers in tests/Rendering.Harness/RenderAnywhere.fs
- [X] T007 Add tests/Rendering.Harness/RenderAnywhere.fsi and tests/Rendering.Harness/RenderAnywhere.fs to tests/Rendering.Harness/Rendering.Harness.fsproj before Evidence.fsi
- [X] T008 [P] Add reusable Feature146 assertion helpers for package identities, diagnostics, and artifact metadata in tests/Rendering.Harness/TestAssertions.fs
- [X] T009 Document the agreed representative corpus coverage and exact scene IDs in specs/146-render-anywhere-protocol/readiness/roundtrip/corpus.md before story tests begin
- [X] T010 [P] Create baseline public-surface expectations and add the compile entry in tests/Package.Tests/Feature146CompatibilityLedgerTests.fs and tests/Package.Tests/Package.Tests.fsproj

**Checkpoint**: Shared corpus names, exact representative scene IDs, readiness paths, and compatibility evidence scaffolding exist.

---

## Phase 3: User Story 1 - Exchange a Portable Scene (Priority: P1) MVP

**Goal**: Maintainers can export an existing scene into a versioned deterministic portable package, import it back, and inspect compatibility diagnostics without Skia or browser dependencies.

**Independent Test**: Export and import the representative corpus, compare restored scenes with semantic diagnostics, verify 50 repeated exports are byte-identical, and verify unsupported versions/tags/resources/capabilities fail safely before rendering.

### Public Surface for User Story 1

- [X] T011 [US1] Define the public portable package signature, protocol model, diagnostics, export/import/inspect functions, semantic comparison, and identity functions in src/Scene/SceneCodec.fsi

### Tests for User Story 1

- [X] T012 [P] [US1] Add SceneCodec FSI transcript coverage for export, import, inspect, and package identity in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T013 [P] [US1] Add portable scene round-trip, determinism, shaped text, and semantic comparison tests in tests/Scene.Tests/Feature146PortableSceneRoundTripTests.fs
- [X] T014 [P] [US1] Add malformed package, newer major version, unknown required tag, and unknown optional tag tests in tests/Scene.Tests/Feature146PortableSceneCompatibilityTests.fs
- [X] T015 [P] [US1] Add required resource missing, resource hash mismatch, and unsupported required capability rejection tests in tests/Scene.Tests/Feature146PortableSceneResourceTests.fs
- [X] T016 [US1] Add tests/Scene.Tests/Feature146PortableSceneRoundTripTests.fs, tests/Scene.Tests/Feature146PortableSceneCompatibilityTests.fs, and tests/Scene.Tests/Feature146PortableSceneResourceTests.fs to tests/Scene.Tests/Scene.Tests.fsproj before Program.fs

### Implementation for User Story 1

- [X] T017 [US1] Add src/Scene/SceneCodec.fsi and src/Scene/SceneCodec.fs to src/Scene/Scene.fsproj after src/Scene/Scene.fs and before src/Scene/Animation.fsi
- [X] T018 [US1] Implement protocol constants, magic header `FSGGSCENE`, version range, tag table comments, and canonical primitive encoders in src/Scene/SceneCodec.fs
- [X] T019 [US1] Implement deterministic TLV writer support for protocol header, capability profile, resource manifest, and scene payload records in src/Scene/SceneCodec.fs
- [X] T020 [US1] Implement deterministic TLV reader support with length-skipping optional tags and rejecting unknown required tags in src/Scene/SceneCodec.fs
- [X] T021 [US1] Implement resource manifest encoding with canonical ResourceId ordering and no local-path identity use in src/Scene/SceneCodec.fs
- [X] T022 [US1] Implement Scene node payload encoding that preserves authored child order, path commands, paint data, layers, portals, and image resource references in src/Scene/SceneCodec.fs
- [X] T023 [US1] Implement shaped text and GlyphRunData export/import preservation, including provider evidence and fallback diagnostics, in src/Scene/SceneCodec.fs
- [X] T024 [US1] Implement package inspection status, version diagnostics, capability diagnostics, resource diagnostics, and semantic comparison in src/Scene/SceneCodec.fs
- [X] T025 [US1] Implement package identity hashing over canonical bytes in src/Scene/SceneCodec.fs
- [X] T026 [US1] Record round-trip evidence and public contract notes for the portable scene package in specs/146-render-anywhere-protocol/readiness/roundtrip/corpus.md and specs/146-render-anywhere-protocol/readiness/compatibility-ledger.md

**Checkpoint**: User Story 1 can be validated with `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature146` and package transcript checks.

---

## Phase 4: User Story 2 - Produce a Reference Rendering Oracle (Priority: P2)

**Goal**: Release reviewers can render portable packages through the trusted Skia path and receive real PNG artifacts plus audit metadata or an honest failed/environment-limited result.

**Independent Test**: Render fixed portable packages into reference images and validate that accepted results include dimensions, image identity, protocol version, capability profile, resource status, and verdict while missing-resource and unsupported-host cases do not accept misleading artifacts.

### Public Surface for User Story 2

- [X] T027 [US2] Define the public ReferenceRendering MVU/effect/evidence signature in src/SkiaViewer/ReferenceRendering.fsi

### Tests for User Story 2

- [X] T028 [P] [US2] Add ReferenceRendering FSI transcript coverage for init, update, effects, and evidence records in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T029 [P] [US2] Add valid package PNG evidence, non-blank image, metadata, missing resource failure, unsupported environment, and repeat-render metadata tests in tests/SkiaViewer.Tests/Feature146ReferenceRenderingTests.fs
- [X] T030 [P] [US2] Add harness reference command and evidence formatting tests in tests/Rendering.Harness.Tests/Feature146RenderAnywhereEvidenceTests.fs
- [X] T031 [US2] Add tests/SkiaViewer.Tests/Feature146ReferenceRenderingTests.fs to tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj before Program.fs
- [X] T032 [US2] Add tests/Rendering.Harness.Tests/Feature146RenderAnywhereEvidenceTests.fs to tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 2

- [X] T033 [US2] Add src/SkiaViewer/ReferenceRendering.fsi and src/SkiaViewer/ReferenceRendering.fs to src/SkiaViewer/SkiaViewer.fsproj after src/SkiaViewer/SceneRenderer.fs and before src/SkiaViewer/Host/OpenGl.fsi
- [X] T034 [US2] Implement ReferenceRendering Model, Msg, Effect, init, and pure update transitions in src/SkiaViewer/ReferenceRendering.fs
- [X] T035 [US2] Implement package inspection integration, resource resolver effects, and failure classification before rendering in src/SkiaViewer/ReferenceRendering.fs
- [X] T036 [US2] Implement Skia-backed package rendering through the existing SceneRenderer path and PNG artifact writing in src/SkiaViewer/ReferenceRendering.fs
- [X] T037 [US2] Implement PNG decodability, non-blank validation, output checksum, renderer identity, and environment-limited evidence classification in src/SkiaViewer/ReferenceRendering.fs
- [X] T038 [US2] Add the `render-anywhere-reference` command, output argument parsing, and readiness/reference artifact writing in tests/Rendering.Harness/Cli.fs and tests/Rendering.Harness/RenderAnywhere.fs
- [X] T039 [US2] Record reference oracle evidence requirements and environment-limited handling in specs/146-render-anywhere-protocol/readiness/reference/README.md

**Checkpoint**: User Story 2 can be validated with `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature146` and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-reference --out specs/146-render-anywhere-protocol/readiness/reference`.

---

## Phase 5: User Story 3 - Decide Browser Rendering Feasibility (Priority: P3)

**Goal**: Maintainers can evaluate a CanvasKit-compatible browser candidate or documented fallback against the reference oracle for at least three representative scenes.

**Independent Test**: Run the feasibility corpus through the candidate path, compare outputs with explicit tolerance against passed reference evidence, and record unsupported capabilities, resource gaps, or final fallback rationale.

### Public Surface for User Story 3

- [X] T040 [US3] Extend tests/Rendering.Harness/RenderAnywhere.fsi with browser feasibility Model, Msg, Effect, init, update, interpreter contract, report records, comparison tolerance, candidate verdicts, and final decision types

### Tests for User Story 3

- [X] T041 [US3] Add browser feasibility MVU transition and effect emission tests for init, candidate execution, reference lookup, comparison, unsupported capability, and fallback decisions in tests/Rendering.Harness.Tests/Feature146BrowserFeasibilityTests.fs
- [X] T042 [US3] Add browser feasibility report, tolerance, per-scene verdict, unsupported capability, and final decision tests in tests/Rendering.Harness.Tests/Feature146BrowserFeasibilityTests.fs
- [X] T043 [P] [US3] Add evidence formatter tests for accepted candidate, unsupported capability, missing resource, and environment-limited browser outcomes in tests/Rendering.Harness.Tests/Feature146BrowserEvidenceFormatterTests.fs
- [X] T044 [US3] Add tests/Rendering.Harness.Tests/Feature146BrowserFeasibilityTests.fs and tests/Rendering.Harness.Tests/Feature146BrowserEvidenceFormatterTests.fs to tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 3

- [X] T045 [US3] Implement browser feasibility Model, Msg, Effect, init, pure update transitions, and edge interpreter contracts in tests/Rendering.Harness/RenderAnywhere.fs
- [X] T046 [US3] Implement CanvasKit-compatible candidate command stream or fallback classification in tests/Rendering.Harness/RenderAnywhere.fs
- [X] T047 [US3] Implement reference evidence lookup, candidate artifact identity, diff metric calculation, unsupported capability summaries, and final decision generation in tests/Rendering.Harness/RenderAnywhere.fs
- [X] T048 [US3] Add the `render-anywhere-browser-feasibility` command, output argument parsing, and readiness/browser report writing in tests/Rendering.Harness/Cli.fs
- [X] T049 [US3] Record browser feasibility run requirements, accepted-candidate criteria, MVU/effect workflow, and fallback categories in specs/146-render-anywhere-protocol/readiness/browser/README.md

**Checkpoint**: User Story 3 can be validated with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature146` and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- render-anywhere-browser-feasibility --out specs/146-render-anywhere-protocol/readiness/browser`.

---

## Phase 6: User Story 4 - Inspect Capabilities and Resources (Priority: P3)

**Goal**: Package consumers can inspect required and optional capabilities/resources before rendering and receive accepted, accepted-with-degradation, or rejected outcomes with actionable diagnostics.

**Independent Test**: Create supported, unsupported, missing, optional, corrupted, duplicated, and mismatched resource/capability packages and verify the pre-render inspection report for each case.

### Public Surface for User Story 4

- [X] T050 [US4] Extend SceneCodec inspection signatures with target capability profiles, resource availability inputs, degradation policy, and detailed verdict records in src/Scene/SceneCodec.fsi
- [X] T051 [US4] Define public package inspection assertion helper signatures in src/Testing/Testing.fsi

### Tests for User Story 4

- [X] T052 [P] [US4] Add target profile, required capability rejection, optional capability degradation, and affected scene path tests in tests/Scene.Tests/Feature146PackageCapabilityInspectionTests.fs
- [X] T053 [P] [US4] Add required/optional image and font resource availability, corruption, duplication, hash mismatch, and degradation tests in tests/Scene.Tests/Feature146PackageResourceInspectionTests.fs
- [X] T054 [P] [US4] Add Testing package assertion coverage for package inspection reports and diagnostics in tests/Package.Tests/Feature146CompatibilityLedgerTests.fs
- [X] T055 [US4] Add tests/Scene.Tests/Feature146PackageCapabilityInspectionTests.fs and tests/Scene.Tests/Feature146PackageResourceInspectionTests.fs to tests/Scene.Tests/Scene.Tests.fsproj before Program.fs

### Implementation for User Story 4

- [X] T056 [US4] Implement target profile matching, required/optional capability decisions, degradation policies, and deterministic affected scene paths in src/Scene/SceneCodec.fs
- [X] T057 [US4] Implement resource availability evaluation for required, optional, missing, corrupted, duplicated, mismatched, image, and font resources in src/Scene/SceneCodec.fs
- [X] T058 [US4] Implement package inspection assertion helpers in src/Testing/Testing.fs and update src/Testing/Testing.fsproj and tests/Package.Tests/Package.Tests.fsproj so inspection helpers compile and tests/Package.Tests/Package.Tests.fsproj references src/Testing/Testing.fsproj

**Checkpoint**: User Story 4 can be validated with `dotnet test tests/Scene.Tests/Scene.Tests.fsproj --filter Feature146PackageInspection` and package inspection helper tests.

---

## Phase 7: Polish and Cross-Cutting Concerns

**Purpose**: Complete Tier 1 readiness, surface baselines, documentation, and full validation.

- [X] T059 [P] Update public API and migration guidance for SceneCodec, ReferenceRendering, package inspection, and evidence surfaces in src/Scene/README.md and src/SkiaViewer/README.md
- [X] T060 [P] Update compatibility notes, intentional limitations, browser fallback decision rules, and evidence links in specs/146-render-anywhere-protocol/readiness/compatibility-ledger.md
- [X] T061 Refresh surface baselines with scripts/refresh-surface-baselines.fsx and verify intentional Feature146 changes in tests/Package.Tests/SurfaceAreaTests.fs
- [X] T062 Run quickstart round-trip validation and record results in specs/146-render-anywhere-protocol/readiness/validation-summary.md using tests/Scene.Tests/Scene.Tests.fsproj
- [X] T063 Run quickstart reference rendering validation and record results in specs/146-render-anywhere-protocol/readiness/validation-summary.md using tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T064 Run quickstart browser feasibility validation and record results in specs/146-render-anywhere-protocol/readiness/validation-summary.md using tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T065 Run package, surface, full solution, and pack validation and record results in specs/146-render-anywhere-protocol/readiness/validation-summary.md using tests/Package.Tests/Package.Tests.fsproj and FS.GG.Rendering.slnx

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **User Story 1 (Phase 3)**: Depends on Foundational. This is the MVP.
- **User Story 2 (Phase 4)**: Depends on User Story 1 package export/import and inspection.
- **User Story 3 (Phase 5)**: Depends on User Story 1 packages and User Story 2 reference oracle evidence.
- **User Story 4 (Phase 6)**: Depends on User Story 1 package model and may run alongside User Story 2 after US1.
- **Polish (Phase 7)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 Portable Scene Exchange (P1)**: Starts after Foundational and has no dependency on other user stories.
- **US2 Reference Rendering Oracle (P2)**: Requires US1 portable package bytes and inspection results.
- **US3 Browser Feasibility (P3)**: Requires US1 portable packages and US2 passed or environment-limited reference evidence records.
- **US4 Capability and Resource Inspection (P3)**: Requires US1 package structures, then can be validated independently with targeted inspection packages.

### Within Each User Story

- Write tests before implementation and verify they fail for missing behavior.
- Draft `.fsi` public signatures before semantic/FSI tests and before `.fs` implementation bodies.
- Update `.fsproj` compile ordering whenever new F# files are added.
- Complete core behavior before readiness artifact generation.
- Validate the story checkpoint before moving to the next priority.

---

## Parallel Opportunities

- T008 and T010 can run in parallel after T005 to T007 define shared names; T009 follows T004 because both update specs/146-render-anywhere-protocol/readiness/roundtrip/corpus.md.
- US1 test tasks T012 to T015 can be authored in parallel after T011 and before T016.
- US2 test tasks T028 to T030 can be authored in parallel after T027 and before T031 and T032.
- US3 evidence formatter task T043 can run in parallel with the sequential Feature146BrowserFeasibilityTests.fs edits in T041 and T042 after T040 and before T044.
- US4 test tasks T052 to T054 can be authored in parallel after T050 and T051 and before T055.
- Polish documentation tasks T059 and T060 can run in parallel after the public surfaces stabilize.

---

## Parallel Example: User Story 1

```bash
Task: "T012 Add SceneCodec FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T013 Add portable scene round-trip tests in tests/Scene.Tests/Feature146PortableSceneRoundTripTests.fs"
Task: "T014 Add malformed package/version/tag tests in tests/Scene.Tests/Feature146PortableSceneCompatibilityTests.fs"
Task: "T015 Add resource and capability rejection tests in tests/Scene.Tests/Feature146PortableSceneResourceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T028 Add ReferenceRendering FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T029 Add SkiaViewer reference rendering tests in tests/SkiaViewer.Tests/Feature146ReferenceRenderingTests.fs"
Task: "T030 Add harness reference command tests in tests/Rendering.Harness.Tests/Feature146RenderAnywhereEvidenceTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T041 Add browser feasibility MVU transition tests in tests/Rendering.Harness.Tests/Feature146BrowserFeasibilityTests.fs"
Task: "T042 Add browser feasibility report tests in tests/Rendering.Harness.Tests/Feature146BrowserFeasibilityTests.fs"
Task: "T043 Add browser evidence formatter tests in tests/Rendering.Harness.Tests/Feature146BrowserEvidenceFormatterTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T052 Add target profile and capability inspection tests in tests/Scene.Tests/Feature146PackageCapabilityInspectionTests.fs"
Task: "T053 Add resource availability inspection tests in tests/Scene.Tests/Feature146PackageResourceInspectionTests.fs"
Task: "T054 Add package inspection assertion coverage in tests/Package.Tests/Feature146CompatibilityLedgerTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1 setup.
2. Complete Phase 2 foundational shared names and readiness paths.
3. Complete Phase 3 User Story 1 `.fsi`, tests, implementation, project wiring, and round-trip evidence.
4. Stop and validate with the US1 checkpoint before beginning rendering or browser feasibility.

### Incremental Delivery

1. US1 delivers the durable package exchange contract.
2. US2 adds the trusted Skia reference oracle on top of accepted portable packages.
3. US4 can deepen pre-render inspection after US1 while US2 progresses.
4. US3 compares a browser-capable candidate or fallback decision against the reference oracle.
5. Polish completes Tier 1 readiness, surface baselines, compatibility notes, and package validation.

### Parallel Team Strategy

1. One contributor completes setup and foundational harness naming.
2. After US1 stabilizes, one contributor can work on US2 reference rendering while another works on US4 detailed inspection.
3. US3 starts after reference evidence records are available.
4. Documentation, compatibility ledger, and validation summary updates run after public surfaces stabilize.

---

## Notes

- All public F# modules added by this feature require `.fsi` signatures.
- Top-level visibility remains controlled by `.fsi`; do not add top-level `private`, `internal`, or `public` in paired `.fs` files.
- Stateful or I/O-bearing workflows use MVU-style Model, Msg, Effect, init, update, and interpreter boundaries.
- SceneCodec must remain dependency-light and must not reference SkiaSharp, browser runtimes, native host packages, or filesystem-specific local paths.
- Accepted reference evidence requires real, decodable, non-blank PNG artifacts; environment-limited records cannot count as passed reference evidence.
