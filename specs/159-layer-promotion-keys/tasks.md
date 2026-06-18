# Tasks: Layer Promotion and Content/Transform Key Split

**Input**: Design documents from `/specs/159-layer-promotion-keys/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/`, `quickstart.md`

**Tests**: Required. The feature specification includes independent testing for every user story, and this Tier 1 behavioral change must have failing-first semantic tests before implementation.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated as an independent increment.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel because it touches different files and has no dependency on incomplete tasks.
- **[Story]**: User-story label, used only inside user-story phases.
- Every task includes exact repository-relative file paths.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare Feature 159 readiness locations and review placeholders without changing runtime behavior.

- [X] T001 Create Feature 159 readiness directory placeholders in specs/159-layer-promotion-keys/readiness/promotion/attempts/.gitkeep, specs/159-layer-promotion-keys/readiness/promotion/reuse/.gitkeep, specs/159-layer-promotion-keys/readiness/promotion/demotions/.gitkeep, specs/159-layer-promotion-keys/readiness/promotion/fallbacks/.gitkeep, specs/159-layer-promotion-keys/readiness/promotion/parity/.gitkeep, specs/159-layer-promotion-keys/readiness/promotion/unsupported/.gitkeep, specs/159-layer-promotion-keys/readiness/counters/.gitkeep, and specs/159-layer-promotion-keys/readiness/fsi/.gitkeep
- [X] T002 [P] Add promotion evidence index with required scenario ids, policy id, accepted-profile scope, and artifact inventory in specs/159-layer-promotion-keys/readiness/promotion/README.md
- [X] T003 [P] Add promotion summary placeholder covering attempts, reuse, demotions, fallbacks, parity, unsupported host, counters, and final Feature 159 status in specs/159-layer-promotion-keys/readiness/promotion/summary.md
- [X] T004 [P] Add counter evidence placeholder for avoided content work, placement-only reuse, content re-recording, demotions, fallbacks, overhead, and net saved work in specs/159-layer-promotion-keys/readiness/counters/README.md
- [X] T005 [P] Add compatibility ledger placeholder for no public-surface change or intentional `.fsi` deltas in specs/159-layer-promotion-keys/readiness/compatibility-ledger.md
- [X] T006 [P] Add package and regression validation placeholders for focused Feature 159 checks and Feature 155/157/158 preservation in specs/159-layer-promotion-keys/readiness/package-validation.md and specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T007 [P] Add validation summary placeholder naming `performance-not-accepted`, remaining timing and host-lane gates, and under-5-minute reviewer entry links in specs/159-layer-promotion-keys/readiness/validation-summary.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Declare observable Feature 159 contracts, command names, helper surfaces, and validation hooks before story implementation.

**Critical**: No user-story implementation should begin until these contracts and expected failing tests can be added against stable names.

- [X] T008 Add Feature 159 constants, readiness paths, policy id `layer-promotion-v1`, accepted profile `probe-08a47c01`, required scenario ids, status tokens, attempt/reuse/counter/parity records, and MVU `Model`/`Msg`/`Effect` declarations in tests/Rendering.Harness/Compositor.fsi
- [X] T009 Add Feature 159 public or observable declarations for content identity, placement identity, promotion candidate, retained layer state, reuse decision, demotion reason tokens, reuse counters, parity result, and split-key helper signatures in src/Controls/RetainedRender.fsi; keep private hashing/storage helpers out of the signature
- [X] T010 Add Feature 159 replay-cache contract updates for content-keyed picture reuse, placement-only replay diagnostics, safe re-recording, and counter stats in src/SkiaViewer/PictureReplayCache.fsi
- [X] T011 Add package-visible Feature 159 readiness helper declarations and status-token validation contracts in src/Testing/Testing.fsi
- [X] T012 Add Feature 159 command detection stubs for `--feature 159`, `feature159`, `159-layer-promotion-keys`, `compositor-promotion`, and `compositor-readiness --feature 159` in tests/Rendering.Harness/Cli.fs
- [X] T013 [P] Add pre-implementation Feature 159 FSI authoring scripts for content/placement identity, promotion command, and readiness helper APIs in specs/159-layer-promotion-keys/readiness/fsi/content-placement-identity-authoring.fsx, specs/159-layer-promotion-keys/readiness/fsi/compositor-promotion-authoring.fsx, and specs/159-layer-promotion-keys/readiness/fsi/compositor-readiness-authoring.fsx, then add transcript reader helpers and expected transcript coverage hooks in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T014 [P] Add failing Feature 159 compatibility package test skeleton and register it in tests/Package.Tests/Feature159CompatibilityTests.fs and tests/Package.Tests/Package.Tests.fsproj
- [X] T015 [P] Add Feature 159 validation plan covering real evidence, unsupported-host output, synthetic disclosure, FSI transcripts, package validation, and regression preservation in specs/159-layer-promotion-keys/readiness/validation-plan.md
- [X] T016 Document the Feature 159 public-surface decision, including whether Scene, SkiaViewer, Controls, and Testing `.fsi` files changed, in specs/159-layer-promotion-keys/readiness/compatibility-ledger.md

**Checkpoint**: Feature 159 names, signatures, command routes, status tokens, and validation expectations are stable before implementation.

---

## Phase 3: User Story 1 - Reuse Recorded Content for Placement-Only Movement (Priority: P1) - MVP

**Goal**: Reuse recorded content when content identity is unchanged and only placement, scroll, or transform evidence changes.

**Independent Test**: Run representative placement-only movement and scrolling scenarios, then verify unchanged content is reused while placement evidence and old/new damage coverage are recorded separately.

### Tests for User Story 1

Write these tests first and verify they fail before implementation.

- [X] T017 [P] [US1] Add failing content/placement identity split tests for stable content, changed placement, content change, and content-plus-placement change in tests/Controls.Tests/Feature159IdentitySplitTests.fs and register it in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T018 [P] [US1] Add failing backend replay tests for placement-only replay hits, content-change re-recording, changed placement stats, and disabled-cache parity in tests/SkiaViewer.Tests/Feature159ReplayReuseTests.fs and register it in tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj
- [X] T019 [P] [US1] Add failing harness command tests for `compositor-promotion --feature 159 --policy layer-promotion-v1` placement-only reuse output and summary fields in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs and register it in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T020 [US1] Run the failing US1 focused tests and record expected failures in specs/159-layer-promotion-keys/readiness/promotion/reuse/validation.md

### Implementation for User Story 1

- [X] T021 [US1] Implement content identity and placement identity construction over retained boundaries, local-content fingerprints, boxes, transforms, scroll offsets, scale, and coverage regions in src/Controls/RetainedRender.fs
- [X] T022 [US1] Extend retained work-reduction records with placement-only reuse, content record, content re-record, avoided content work, and split identity counters in src/Controls/RetainedRender.fsi and src/Controls/RetainedRender.fs
- [X] T023 [US1] Update retained replay-boundary emission so unchanged content can be reused at a new placement only when old and new coverage are included in damage evidence in src/Controls/RetainedRender.fs
- [X] T024 [US1] Implement content-keyed recorded-picture reuse and placement-only replay stats while preserving content-change re-recording in src/SkiaViewer/PictureReplayCache.fs
- [X] T025 [US1] Implement Feature 159 placement-only attempt records, reuse decision rendering, content identity rendering, placement identity rendering, and counter formatting in tests/Rendering.Harness/Compositor.fs
- [X] T026 [US1] Wire `compositor-promotion --feature 159 --scenario promotion/placement-only-move`, `promotion/scroll-shifted`, and `promotion/content-change` output under specs/159-layer-promotion-keys/readiness/promotion in tests/Rendering.Harness/Cli.fs
- [X] T027 [US1] Run the pre-implementation Feature 159 identity-split authoring script and refresh the PASS log in specs/159-layer-promotion-keys/readiness/fsi/content-placement-identity-authoring.log after the `.fsi`, semantic tests, and `.fs` implementation agree
- [X] T028 [US1] Generate or refresh placement-only reuse evidence in specs/159-layer-promotion-keys/readiness/promotion/reuse/README.md, specs/159-layer-promotion-keys/readiness/promotion/attempts/placement-only-move.md, and specs/159-layer-promotion-keys/readiness/promotion/attempts/scroll-shifted.md
- [X] T029 [US1] Run US1 focused tests and the placement-only quickstart command, then record accepted, rejected, or environment-limited reuse outcome in specs/159-layer-promotion-keys/readiness/promotion/reuse/validation.md
- [X] T030 [US1] Update the Feature 159 validation summary with US1 scenario links, split identity evidence, reuse counters, and `performance-not-accepted` claim status in specs/159-layer-promotion-keys/readiness/validation-summary.md

**Checkpoint**: User Story 1 is independently testable and delivers the MVP placement-only reuse path without accepting stale content.

---

## Phase 4: User Story 2 - Promote Only Stable Expensive Subtrees (Priority: P1)

**Goal**: Promote only stable, expensive, parity-passing retained boundaries whose saved work exceeds promotion overhead.

**Independent Test**: Feed stable expensive, stable cheap, and unstable expensive scenarios through the promotion decision path and verify decisions, reason tokens, and counters.

### Tests for User Story 2

Write these tests first and verify they fail before implementation.

- [X] T031 [P] [US2] Add failing promotion policy tests for three-frame stability, 30% repeated-work reduction, net saved work, cheap stable rejection, parity failure, and observing state in tests/Controls.Tests/Feature159PromotionDecisionTests.fs and register it in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T032 [P] [US2] Add failing pure workflow transition and effect tests for candidate observation, promotion evaluation, parity recording, counter aggregation, and summary publishing in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs
- [X] T033 [P] [US2] Add failing required scenario inventory tests for `promotion/static-retained`, `promotion/nested-retained`, and stable-expensive promotion output in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs
- [X] T034 [US2] Run the failing US2 focused tests and record expected failures in specs/159-layer-promotion-keys/readiness/promotion/attempts/validation.md

### Implementation for User Story 2

- [X] T035 [US2] Implement Feature 159 promotion decision tokens `promoted`, `observing`, `kept`, `demoted`, `rejected`, `bypassed`, `non-beneficial`, `fallback-only`, and `environment-limited` in src/Controls/RetainedRender.fs
- [X] T036 [US2] Implement promotion candidate observation, three-frame stability tracking, expected-saved-work calculation, measured-overhead calculation, and repeated-work reduction checks in src/Controls/RetainedRender.fs
- [X] T037 [US2] Implement Feature 159 pure workflow `init` and `update` for profile binding, scenario preparation, candidate observation, promotion evaluation, reuse evaluation, parity checking, counter aggregation, and summary publication in tests/Rendering.Harness/Compositor.fs
- [X] T038 [US2] Wire `promotion/static-retained` and `promotion/nested-retained` scenario preparation and artifact writing for `compositor-promotion --feature 159` in tests/Rendering.Harness/Cli.fs
- [X] T039 [US2] Implement promotion decision, candidate, stability-window, overhead, and net-positive counter report renderers in tests/Rendering.Harness/Compositor.fs
- [X] T040 [US2] Generate or refresh promotion decision evidence in specs/159-layer-promotion-keys/readiness/promotion/attempts/static-retained.md, specs/159-layer-promotion-keys/readiness/promotion/attempts/nested-retained.md, and specs/159-layer-promotion-keys/readiness/counters/promotion.md
- [X] T041 [US2] Run US2 focused tests and promotion quickstart scenarios, then record stable-expensive, stable-cheap, and unstable-expensive outcomes in specs/159-layer-promotion-keys/readiness/promotion/summary.md

**Checkpoint**: User Story 2 is independently testable and promotion is gated by stability, parity, and net-positive counters.

---

## Phase 5: User Story 3 - Fail Closed for Churn, Stale Identity, and Unsafe Reuse (Priority: P1)

**Goal**: Reject, demote, bypass, or fall back for churning, stale, missing, cross-profile, resource-limited, unsupported-host, or parity-failing reuse evidence.

**Independent Test**: Exercise churn, missing retained content, stale identity, cross-profile evidence, unsupported host, resource limits, and parity mismatch, then verify zero accepted reuse artifacts and stable primary reasons.

### Tests for User Story 3

Write these tests first and verify they fail before implementation.

- [X] T042 [P] [US3] Add failing demotion and reuse-counter tests for churn, stale content identity, stale placement identity, ambiguous identity, missing retained content, resource limits, and non-beneficial counters in tests/Controls.Tests/Feature159ReuseCounterTests.fs and register it in tests/Controls.Tests/Controls.Tests.fsproj
- [X] T043 [P] [US3] Add failing backend replay fallback tests for missing resident picture, changed run/profile metadata, disabled replay, resource-limited retention, and parity mismatch diagnostics in tests/SkiaViewer.Tests/Feature159ReplayReuseTests.fs
- [X] T044 [P] [US3] Add failing harness tests for unsupported-host output, cross-profile rejection, missing policy rejection, parity rejection, and zero accepted artifacts in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs
- [X] T045 [US3] Run the failing US3 focused tests and record expected failures in specs/159-layer-promotion-keys/readiness/promotion/fallbacks/validation.md

### Implementation for User Story 3

- [X] T046 [US3] Implement stable primary reason tokens for `instability`, `low-cost`, `overhead-exceeds-saved-work`, `stale-content-identity`, `stale-placement-identity`, `ambiguous-identity`, `cross-profile-evidence`, `missing-retained-content`, `resource-limited`, `unsupported-host`, `parity-mismatch`, `non-beneficial-counters`, `run-identity-mismatch`, `scenario-definition-mismatch`, and `missing-policy` in src/Controls/RetainedRender.fsi and src/Controls/RetainedRender.fs
- [X] T047 [US3] Implement churn demotion, bypass, zero accepted counters, and fail-closed reuse classification in src/Controls/RetainedRender.fs
- [X] T048 [US3] Implement missing retained content, stale content key, resource-limited retained picture, and disabled replay fallback handling with diagnostic stats in src/SkiaViewer/PictureReplayCache.fs
- [X] T049 [US3] Enforce policy id, host profile, run identity, package version, scenario definition, parity verdict, and current retained-layer state validation for accepted Feature 159 evidence in tests/Rendering.Harness/Compositor.fs
- [X] T050 [US3] Wire unsupported-host and unavailable-presentation output for `compositor-promotion --feature 159` to publish `environment-limited` and zero accepted reuse or promotion artifacts in tests/Rendering.Harness/Cli.fs
- [X] T051 [US3] Implement demotion, fallback, parity, unsupported-host, and rejection report renderers in tests/Rendering.Harness/Compositor.fs
- [X] T052 [US3] Generate or refresh fail-closed evidence in specs/159-layer-promotion-keys/readiness/promotion/demotions/churn-demotion.md, specs/159-layer-promotion-keys/readiness/promotion/fallbacks/fallback-safe.md, specs/159-layer-promotion-keys/readiness/promotion/fallbacks/ambiguous-identity.md, specs/159-layer-promotion-keys/readiness/promotion/parity/parity-mismatch.md, and specs/159-layer-promotion-keys/readiness/promotion/unsupported/README.md
- [X] T053 [US3] Run the unsupported-host quickstart command and record elapsed time, `environment-limited` status, accepted reuse artifacts `0`, and accepted promotion artifacts `0` in specs/159-layer-promotion-keys/readiness/promotion/unsupported/validation.md
- [X] T054 [US3] Run US3 focused tests and record fail-closed validation outcomes in specs/159-layer-promotion-keys/readiness/promotion/fallbacks/validation.md and specs/159-layer-promotion-keys/readiness/promotion/demotions/validation.md

**Checkpoint**: User Story 3 is independently testable and every unsafe reuse path fails closed with a reviewer-visible reason.

---

## Phase 6: User Story 4 - Publish Net-Positive Reuse and Promotion Evidence (Priority: P2)

**Goal**: Publish one readiness package that explains promotion decisions, reuse counters, demotions, parity, host scope, compatibility, package validation, regression preservation, and final claim status.

**Independent Test**: Open `specs/159-layer-promotion-keys/readiness/validation-summary.md` and verify a reviewer can identify promoted scenarios, reused scenarios, demoted scenarios, counter totals, parity status, host scope, compatibility impact, artifact paths, and final status in under 5 minutes.

### Tests for User Story 4

Write these tests first and verify they fail before implementation.

- [X] T055 [P] [US4] Add failing readiness package tests for required files, required summary fields, scenario coverage, accepted attempt count, counter totals, parity status, unsupported-host result, compatibility links, and `performance-not-accepted` in tests/Rendering.Harness.Tests/Feature159ReadinessPackageTests.fs and register it in tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj
- [X] T056 [P] [US4] Add failing package-visible readiness helper tests for accepted, non-beneficial, fallback-only, rejected, and environment-limited Feature 159 summaries in tests/Testing.Tests/Feature159ReadinessHelperTests.fs and register it in tests/Testing.Tests/Testing.Tests.fsproj
- [X] T057 [P] [US4] Complete failing compatibility package tests for public-surface decision, FSI transcript evidence, package validation, regression validation, and final claim boundary in tests/Package.Tests/Feature159CompatibilityTests.fs
- [X] T058 [P] [US4] Add Feature 159 transcript assertions for identity authoring, promotion command authoring, readiness helper authoring, and PASS logs in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T059 [US4] Run the failing US4 focused tests and record expected failures in specs/159-layer-promotion-keys/readiness/validation-summary.md

### Implementation for User Story 4

- [X] T060 [US4] Implement Feature 159 validation summary, readiness package, compatibility ledger, package validation, regression validation, counter summary, and reviewer checklist renderers in tests/Rendering.Harness/Compositor.fs
- [X] T061 [US4] Wire `compositor-readiness --feature 159 --out specs/159-layer-promotion-keys/readiness` to assemble promotion, counters, parity, unsupported-host, compatibility, package, regression, FSI, and validation-summary files in tests/Rendering.Harness/Cli.fs
- [X] T062 [US4] Implement package-visible Feature 159 readiness helper records, status tokens, validation rules, diagnostics, and status text in src/Testing/Testing.fs
- [X] T063 [US4] Run the pre-implementation Feature 159 promotion command and readiness helper FSI scripts during readiness assembly and refresh PASS logs in specs/159-layer-promotion-keys/readiness/fsi/compositor-promotion-authoring.log and specs/159-layer-promotion-keys/readiness/fsi/compositor-readiness-authoring.log after the `.fsi`, semantic tests, and `.fs` implementation agree
- [X] T064 [US4] Refresh public surface baselines when `.fsi` changes occur by updating tests/surface-baselines/FS.GG.UI.Controls.txt, tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, and tests/surface-baselines/FS.GG.UI.Testing.txt as applicable, then copy accepted Feature 159 surface evidence into specs/159-layer-promotion-keys/readiness/fsi/FS.GG.UI.Controls.txt, specs/159-layer-promotion-keys/readiness/fsi/FS.GG.UI.SkiaViewer.txt, and specs/159-layer-promotion-keys/readiness/fsi/FS.GG.UI.Testing.txt
- [X] T065 [US4] Write final compatibility, package, and regression evidence in specs/159-layer-promotion-keys/readiness/compatibility-ledger.md, specs/159-layer-promotion-keys/readiness/package-validation.md, and specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T066 [US4] Run the readiness quickstart command and record final Feature 159 status, required scenario coverage, accepted attempt count, counter result, parity result, unsupported-host result, and remaining performance gates in specs/159-layer-promotion-keys/readiness/validation-summary.md
- [X] T067 [US4] Run US4 focused tests and record package-visible readiness helper, compatibility ledger, FSI transcript, and readiness package outcomes in specs/159-layer-promotion-keys/readiness/package-validation.md

**Checkpoint**: User Story 4 publishes one reviewable readiness verdict without broadening the shipped compositor performance claim.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final validation, report alignment, regression evidence, package checks, and task closeout.

- [X] T068 [P] Update the originating P7 report status for Feature 159 layer promotion and content/transform key split in docs/reports/2026-06-17-13-54-radical-rendering-architecture-analysis-and-plan.md
- [X] T069 [P] Review every synthetic Feature 159 test or artifact, ensure each synthetic use has a `// SYNTHETIC:` comment with rationale, each synthetic test name contains `Synthetic`, and each synthetic use is listed in the PR-description checklist section of specs/159-layer-promotion-keys/readiness/validation-summary.md; audit tests/Controls.Tests/Feature159IdentitySplitTests.fs, tests/Controls.Tests/Feature159PromotionDecisionTests.fs, tests/Controls.Tests/Feature159ReuseCounterTests.fs, tests/SkiaViewer.Tests/Feature159ReplayReuseTests.fs, and specs/159-layer-promotion-keys/readiness/validation-summary.md
- [X] T070 Run `dotnet build FS.GG.Rendering.slnx --no-restore` and record the result in specs/159-layer-promotion-keys/readiness/package-validation.md
- [X] T071 Run `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --no-restore --filter "Feature159"` and record the result in specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T072 Run `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --no-restore --filter "Feature159"` and record the result in specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T073 Run `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature159"` and record the result in specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T074 Run `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --no-restore --filter "Feature159"` and `dotnet test tests/Package.Tests/Package.Tests.fsproj --no-restore --filter "Feature159"` and record results in specs/159-layer-promotion-keys/readiness/package-validation.md
- [X] T075 Run `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-promotion --feature 159 --out specs/159-layer-promotion-keys/readiness/promotion --policy layer-promotion-v1 --attempts 3` and record promotion evidence in specs/159-layer-promotion-keys/readiness/promotion/summary.md
- [X] T076 Run `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-readiness --feature 159 --out specs/159-layer-promotion-keys/readiness` and record final readiness links in specs/159-layer-promotion-keys/readiness/validation-summary.md
- [X] T077 Run `env -u DISPLAY -u WAYLAND_DISPLAY dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj --no-build -- compositor-promotion --feature 159 --out specs/159-layer-promotion-keys/readiness/promotion/unsupported --policy layer-promotion-v1 --attempts 1` and record under-2-minute unsupported-host evidence in specs/159-layer-promotion-keys/readiness/promotion/unsupported/validation.md
- [X] T078 Run focused Feature 155, Feature 157, and Feature 158 regression checks with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --no-restore --filter "Feature155|Feature157|Feature158|Feature159"` and record preservation evidence in specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T079 Run `dotnet test FS.GG.Rendering.slnx --no-restore` and `git diff --check`, then record final validation in specs/159-layer-promotion-keys/readiness/regression-validation.md
- [X] T080 Mark completed Feature 159 task rows after evidence is recorded in specs/159-layer-promotion-keys/tasks.md

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup and blocks all user-story implementation.
- **User Story 1 (Phase 3)**: Depends on Foundational and is the MVP placement-only reuse path.
- **User Story 2 (Phase 4)**: Depends on Foundational; can develop in parallel with US1 after shared split-key contracts exist, but final net-positive promotion evidence benefits from US1 counters.
- **User Story 3 (Phase 5)**: Depends on Foundational; final validation depends on US1 split identity and US2 promotion counters so every unsafe path can reject accepted evidence.
- **User Story 4 (Phase 6)**: Depends on US1, US2, and US3 evidence shapes to publish the full readiness package.
- **Polish (Phase 7)**: Depends on all selected user stories.

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational; delivers the MVP placement-only content reuse path.
- **US2 (P1)**: Can start after Foundational with promotion-policy fixtures; final promotion acceptance depends on content/reuse counters from US1.
- **US3 (P1)**: Can start after Foundational with fallback fixtures; final fail-closed evidence depends on US1 identity validation and US2 demotion counters.
- **US4 (P2)**: Requires US1, US2, and US3 outputs to assemble one complete reviewer package.

### Dependency Graph

```text
Phase 1 Setup
  -> Phase 2 Foundational
      -> US1 Placement-Only Reuse (MVP)
      -> US2 Stable Expensive Promotion
      -> US3 Fail-Closed Safety
US1 + US2 + US3 -> US4 Readiness Package
US1/US2/US3/US4 -> Phase 7 Polish
```

### Within Each User Story

- Tests must be written and observed failing before implementation.
- New Feature 159 test files must be registered in their owning `.fsproj` files before focused test execution.
- `.fsi`, public contracts, FSI authoring transcripts, and FSI transcript expectations come before `.fs` bodies for public or observable surfaces.
- Stateful or I/O-bearing behavior must add pure `Model`/`Msg`/`update` transition tests and edge interpreter/effect tests before implementation.
- Evidence files are updated only after the related command or focused test has been run or an explicit environment limitation is recorded.

---

## Parallel Opportunities

- Setup documentation tasks T002-T007 can run in parallel after T001 creates the readiness directories.
- Foundational tasks T013-T015 can run in parallel with signature work T008-T011 and CLI stub work T012.
- US1 test tasks T017-T019 can run in parallel because they touch Controls, SkiaViewer, and Harness tests separately.
- US2 test tasks T031-T033 can run in parallel after foundational contracts exist.
- US3 test tasks T042-T044 can run in parallel after foundational contracts exist.
- US4 test tasks T055-T058 can run in parallel once US4 summary field names are declared.
- Final focused validation tasks T071-T074 can run in parallel after the build in T070 succeeds.

## Parallel Example: User Story 1

```bash
Task: "T017 [P] [US1] Add failing content/placement identity split tests in tests/Controls.Tests/Feature159IdentitySplitTests.fs"
Task: "T018 [P] [US1] Add failing backend replay tests in tests/SkiaViewer.Tests/Feature159ReplayReuseTests.fs"
Task: "T019 [P] [US1] Add failing harness command tests in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T031 [P] [US2] Add failing promotion policy tests in tests/Controls.Tests/Feature159PromotionDecisionTests.fs"
Task: "T032 [P] [US2] Add failing pure workflow transition and effect tests in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs"
Task: "T033 [P] [US2] Add failing required scenario inventory tests in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T042 [P] [US3] Add failing demotion and reuse-counter tests in tests/Controls.Tests/Feature159ReuseCounterTests.fs"
Task: "T043 [P] [US3] Add failing backend replay fallback tests in tests/SkiaViewer.Tests/Feature159ReplayReuseTests.fs"
Task: "T044 [P] [US3] Add failing harness safety tests in tests/Rendering.Harness.Tests/Feature159PromotionEvidenceTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T055 [P] [US4] Add failing readiness package tests in tests/Rendering.Harness.Tests/Feature159ReadinessPackageTests.fs"
Task: "T056 [P] [US4] Add failing package-visible readiness helper tests in tests/Testing.Tests/Feature159ReadinessHelperTests.fs"
Task: "T057 [P] [US4] Complete failing compatibility package tests in tests/Package.Tests/Feature159CompatibilityTests.fs"
Task: "T058 [P] [US4] Add Feature 159 transcript assertions in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational contracts and failing validation hooks.
3. Complete Phase 3: User Story 1 placement-only reuse.
4. Stop and validate US1 independently with focused Controls, SkiaViewer, Harness tests and placement-only quickstart evidence.
5. Keep shipped compositor performance claim at `performance-not-accepted`.

### Incremental Delivery

1. Setup + Foundational: establish names, `.fsi` contracts, command routes, status tokens, and validation placeholders.
2. US1: deliver split content/placement identity and placement-only reuse evidence.
3. US2: add promotion eligibility with stability, parity, and net-positive counter gates.
4. US3: harden fail-closed behavior for churn, stale identity, unsafe reuse, unsupported hosts, and parity mismatch.
5. US4: publish one readiness package with compatibility, package, regression, and final claim status.

### Team Parallel Strategy

1. One contributor owns `src/Controls/RetainedRender.fsi` and `src/Controls/RetainedRender.fs`.
2. One contributor owns `src/SkiaViewer/PictureReplayCache.fsi`, `src/SkiaViewer/PictureReplayCache.fs`, and SkiaViewer replay tests.
3. One contributor owns `tests/Rendering.Harness/Compositor.fsi`, `tests/Rendering.Harness/Compositor.fs`, `tests/Rendering.Harness/Cli.fs`, and harness readiness tests.
4. One contributor owns `src/Testing/Testing.fsi`, `src/Testing/Testing.fs`, Package/Testing tests, FSI transcripts, and readiness documentation.

## Notes

- `[P]` tasks touch different files and can run in parallel after their dependencies are complete.
- User-story labels map directly to user stories in `spec.md`.
- Public or package-visible changes must follow FSI before implementation and update surface evidence.
- Unsupported-host and environment-limited results are valid only when they record zero accepted Feature 159 reuse and promotion artifacts.
- The final shipped compositor performance claim remains `performance-not-accepted` unless later timing and host-lane gates are also satisfied.
