# Tasks: Complete P7 Compositor

**Input**: Design documents from `/specs/149-complete-compositor-p7/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required. The specification declares mandatory user scenarios, live host proof,
damage/full-redraw parity, reuse and snapshot lifecycle validation, real timing probes, public
diagnostic contract checks, package validation, and readiness evidence.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as
an independently testable increment. Public or observable surfaces follow the repository rule:
`.fsi` first, semantic/FSI tests next, implementation after that.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the Feature149 readiness locations and reviewable evidence skeletons before
implementation starts.

- [X] T001 Create readiness placeholder files in specs/149-complete-compositor-p7/readiness/live-proof/.gitkeep, specs/149-complete-compositor-p7/readiness/parity/.gitkeep, specs/149-complete-compositor-p7/readiness/reuse/.gitkeep, specs/149-complete-compositor-p7/readiness/snapshots/.gitkeep, and specs/149-complete-compositor-p7/readiness/timing/.gitkeep
- [X] T002 Create the Feature149 validation summary skeleton in specs/149-complete-compositor-p7/readiness/validation-summary.md
- [X] T003 Create the Feature149 compatibility ledger skeleton in specs/149-complete-compositor-p7/readiness/compatibility-ledger.md
- [X] T004 [P] Create the Feature149 corpus, host-profile, threshold, and resource-budget inventory in specs/149-complete-compositor-p7/readiness/corpus.md
- [X] T005 [P] Create the live proof artifact schema and environment-limited disclosure note in specs/149-complete-compositor-p7/readiness/live-proof/README.md
- [X] T006 [P] Create parity, reuse, snapshot, and timing evidence schema notes in specs/149-complete-compositor-p7/readiness/parity/README.md, specs/149-complete-compositor-p7/readiness/reuse/README.md, specs/149-complete-compositor-p7/readiness/snapshots/README.md, and specs/149-complete-compositor-p7/readiness/timing/README.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared Feature149 identifiers, paths, command routing, formatter contracts,
and assertion helpers used by every user story.

**Critical**: No user story implementation should start until this phase is complete.

- [X] T007 Define Feature149 readiness directories, command feature id, corpus names, host profiles, tier names, thresholds, and artifact path contracts in tests/Rendering.Harness/Compositor.fsi
- [X] T008 Implement Feature149 readiness directories, command feature id, corpus names, host profiles, tier names, thresholds, and artifact path helpers in tests/Rendering.Harness/Compositor.fs
- [X] T009 [P] Define Feature149 evidence formatter contracts for host profiles, frame artifacts, fallback decisions, reuse decisions, snapshot resources, timing probes, and readiness summaries in tests/Rendering.Harness/Evidence.fsi
- [X] T010 [P] Add Feature149 assertion helpers for proof verdicts, artifact freshness, damage parity, fallback reasons, reuse decisions, snapshot lifecycle, timing verdicts, and readiness paths in tests/Rendering.Harness/TestAssertions.fs
- [X] T011 Add Feature149 harness command routing stubs for compositor-live-proof, compositor-parity, compositor-reuse, compositor-snapshots, compositor-timing, and compositor-readiness in tests/Rendering.Harness/Cli.fs
- [X] T012 [P] Add Feature149 FSI transcript scaffolding for SkiaViewer, Controls, Controls.Elmish, Rendering.Harness, and Testing surfaces in tests/Package.Tests/FsiTranscriptCoverageTests.fs

**Checkpoint**: Shared names, paths, tiers, command stubs, formatter contracts, and assertion
helpers exist.

---

## Phase 3: User Story 1 - Prove Partial Redraw Is Safe (Priority: P1) MVP

**Goal**: Maintainers can run a live SkiaViewer/OpenGL preservation proof that returns accepted,
failed, or environment-limited, and partial redraw is enabled only from fresh matching accepted
evidence.

**Independent Test**: Run the live compositor proof on capable, non-preserving, stale,
host-mismatched, synthetic-only, and unsupported profiles; only three fresh accepted capable-host
runs unlock damage-scoped readiness.

### Public Surface for User Story 1

- [X] T013 [US1] Extend host profile, package version, proof artifact, sample observation, blank/stale artifact, consecutive proof acceptance, failure cause, MVU Model/Msg/Effect, and live interpreter contracts in src/SkiaViewer/CompositorProof.fsi

### Tests for User Story 1

- [X] T014 [P] [US1] Add CompositorProof FSI transcript coverage for profile facts, proof artifacts, sample observations, freshness rejection, host matching, readiness tokens, and MVU transitions in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T015 [P] [US1] Add live proof tests for capable-host acceptance, three consecutive proof runs, stale proof, host mismatch, algorithm mismatch, missing artifact, blank artifact, and deterministic proof ids in tests/SkiaViewer.Tests/Feature149LiveProofTests.fs
- [X] T016 [P] [US1] Add Synthetic-named simulated tests for non-preserving hosts, stale damaged pixels, cleared untouched pixels, unsupported readback, missing display, timeout, permission failure, and synthetic-only rejection with `// SYNTHETIC:` comments in tests/SkiaViewer.Tests/Feature149LiveProofSimulationTests.fs
- [X] T017 [P] [US1] Add compositor-live-proof harness tests for artifact paths, damaged/untouched sample formatting, environment-limited disclosure, failed proof disclosure, and non-overclaim exit behavior in tests/Rendering.Harness.Tests/Feature149LiveProofEvidenceTests.fs
- [X] T018 [US1] Add tests/SkiaViewer.Tests/Feature149LiveProofTests.fs, tests/SkiaViewer.Tests/Feature149LiveProofSimulationTests.fs, and tests/Rendering.Harness.Tests/Feature149LiveProofEvidenceTests.fs to tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 1

- [X] T019 [US1] Implement active host profile detection, package version capture, renderer/display facts, profile matching, and proof identity generation in src/SkiaViewer/CompositorProof.fs and src/SkiaViewer/Host/OpenGl.fs
- [X] T020 [US1] Implement full sentinel presentation, damage-only no-clear scissored presentation, readback sampling, and artifact capture effects in src/SkiaViewer/CompositorProof.fs and src/SkiaViewer/Host/OpenGl.fs
- [X] T021 [US1] Implement proof rejection for missing artifacts, stale artifacts, blank artifacts, synthetic-only evidence, failed proof, environment-limited proof, host mismatch, and algorithm mismatch in src/SkiaViewer/CompositorProof.fs
- [X] T022 [US1] Implement three-run capable-host acceptance, proof freshness, proof-readiness tokens, and fallback-gating diagnostics in src/SkiaViewer/CompositorProof.fs
- [X] T023 [US1] Implement Feature149 live proof Markdown and machine-readable artifact rendering with host facts, sample identities, artifact references, verdicts, and diagnostics in tests/Rendering.Harness/Compositor.fs, tests/Rendering.Harness/Evidence.fs, and tests/Rendering.Harness/Cli.fs
- [X] T024 [US1] Record live proof run outcomes, accepted/failed/environment-limited criteria, artifact schema, and current host limitations in specs/149-complete-compositor-p7/readiness/live-proof/README.md

**Checkpoint**: User Story 1 can be validated with `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149LiveProof` and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --feature 149 --out specs/149-complete-compositor-p7/readiness/live-proof`.

---

## Phase 4: User Story 2 - Render Damage-Scoped Frames With Full-Redraw Fallback (Priority: P1)

**Goal**: Proof-gated damage-scoped rendering matches the full-redraw oracle, while unsafe or
unsupported frames automatically use full redraw with visible fallback reasons.

**Independent Test**: Compare damage-scoped and full-redraw output over localized, overlapping,
edge, movement, resize, theme, zero-damage, stale-proof, unsupported-host, disabled-mode, and
parity-failure scenarios.

### Public Surface for User Story 2

- [X] T025 [US2] Extend damage region, damage cause, source boundary, full-frame invalidation, fallback reason, scissor metric, no-clear mode, frame parity, and diagnostic contracts in src/Controls/RetainedRender.fsi, src/Controls/Diagnostics.fsi, src/Controls.Elmish/ControlsElmish.fsi, and src/SkiaViewer/SkiaViewer.fsi

### Tests for User Story 2

- [X] T026 [P] [US2] Add damage plan tests for clipping, overlap union area, edge damage, movement old/new regions, full-frame invalidation, invalid damage, source-boundary attribution, and idle zero-damage handling in tests/Controls.Tests/Feature149DamagePlanTests.fs
- [X] T027 [P] [US2] Add damage-scoped redraw tests for proof gating, scissor coverage, no-clear frames, state reset before full redraw, state reset before readback, disabled mode, stale proof, host mismatch, and full-redraw fallback in tests/SkiaViewer.Tests/Feature149DamageScopedRedrawTests.fs
- [X] T028 [P] [US2] Add compositor-parity harness tests for localized, overlapping, edge, movement, resize, theme/global, zero-damage, unsupported-host, resource-failure, internal-error, and parity-failure cases in tests/Rendering.Harness.Tests/Feature149DamageParityTests.fs
- [X] T029 [US2] Add tests/Controls.Tests/Feature149DamagePlanTests.fs, tests/SkiaViewer.Tests/Feature149DamageScopedRedrawTests.fs, and tests/Rendering.Harness.Tests/Feature149DamageParityTests.fs to tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 2

- [X] T030 [US2] Complete damage region clipping, deduplication, true union-area calculation, source-boundary attribution, movement old/new regions, zero-damage preserve decisions, and full-frame invalidation causes in src/Controls/RetainedRender.fs
- [X] T031 [US2] Implement proof, fallback, damage, full-frame invalidation, scissor, no-clear, reset, and parity diagnostic constructors and metrics in src/Controls/Diagnostics.fs and src/Controls.Elmish/ControlsElmish.fs
- [X] T032 [US2] Implement accepted-proof damage-scoped redraw, damage-union scissor coverage, no-clear presentation, and untouched-pixel preservation in src/SkiaViewer/SceneRenderer.fs and src/SkiaViewer/Host/OpenGl.fs
- [X] T033 [US2] Implement full-redraw fallback for missing proof, stale proof, failed proof, environment-limited proof, host mismatch, disabled compositor mode, frame-wide invalidation, unsupported host, unsafe damage, resource failure, internal error, and parity failure in src/SkiaViewer/SkiaViewer.fs and src/SkiaViewer/SceneRenderer.fs
- [X] T034 [US2] Implement compositor-parity --feature 149 corpus execution, full-redraw oracle capture, damage-scoped output identity records, fallback summaries, scissor reset evidence, and parity artifact writing in tests/Rendering.Harness/Compositor.fs, tests/Rendering.Harness/Evidence.fs, and tests/Rendering.Harness/Cli.fs
- [X] T035 [US2] Record damage parity schema, corpus coverage, fallback categories, scissor/no-clear reset evidence, and current limitations in specs/149-complete-compositor-p7/readiness/parity/README.md

**Checkpoint**: User Story 2 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature149Damage`, `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149Damage`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --feature 149 --out specs/149-complete-compositor-p7/readiness/parity`.

---

## Phase 5: User Story 3 - Validate Reuse, Snapshot, and Timing Readiness (Priority: P2)

**Goal**: Maintainers can verify content/placement reuse, bounded snapshot resources, snapshot
composition, and real timing measurements without accepting stale output or overclaiming a
performance benefit.

**Independent Test**: Run reuse, snapshot, and timing commands over stable, placement-only,
content-changing, mixed-change, no-change, invalid-resource, beneficial, non-beneficial, noisy, and
environment-limited scenarios; every report ties decisions to visible output and artifact paths.

### Public Surface for User Story 3

- [X] T036 [US3] Extend reusable boundary, content identity, placement identity, previous placement, reuse decision, demotion reason, snapshot resource, snapshot lifecycle, timing probe, baseline tier, thresholds, and timing verdict contracts in src/Controls/RetainedRender.fsi, src/Controls.Elmish/ControlsElmish.fsi, src/SkiaViewer/PictureReplayCache.fsi, tests/Rendering.Harness/Perf.fsi, tests/Rendering.Harness/Compositor.fsi, and tests/Rendering.Harness/Evidence.fsi

### Tests for User Story 3

- [X] T037 [P] [US3] Add reuse decision tests for stable promotion, placement-only movement, old/new region damage, content-change refresh, mixed change, no change, churn demotion, no-benefit demotion, failed-parity demotion, and deterministic decisions in tests/Controls.Tests/Feature149ReuseDecisionTests.fs
- [X] T038 [P] [US3] Add compositor metric tests for content identities, placement identities, reuse hits, refreshes, demotions, fallback reasons, saved work, measured overhead, snapshot bytes, and timing status in tests/Elmish.Tests/Feature149CompositorMetricsTests.fs
- [X] T039 [P] [US3] Add snapshot lifecycle tests for resource creation, composition, reuse, refresh, replacement, eviction, disposal, host mismatch, content mismatch, invalid resource, unsupported host, over-budget state, and full-redraw fallback in tests/SkiaViewer.Tests/Feature149SnapshotLifecycleTests.fs
- [X] T040 [P] [US3] Add compositor-reuse harness tests for stable, placement-only, content-changing, mixed-change, no-change, churning, failed-parity, and same-seed evidence records in tests/Rendering.Harness.Tests/Feature149ReuseEvidenceTests.fs
- [X] T041 [P] [US3] Add compositor-snapshots harness tests for lifecycle artifacts, resource budget summaries, unsupported-host limitations, parity failure rejection, stale-resource rejection, and snapshot-assisted parity in tests/Rendering.Harness.Tests/Feature149SnapshotEvidenceTests.fs
- [X] T042 [P] [US3] Add compositor-timing harness tests for full-redraw, damage-scoped, placement/replay, snapshot-assisted, warmup exclusion, measured frame counts, beneficial corpus, non-beneficial corpus, noisy data, environment-limited timing, and inconclusive verdicts in tests/Rendering.Harness.Tests/Feature149TimingEvidenceTests.fs
- [X] T043 [US3] Add tests/Controls.Tests/Feature149ReuseDecisionTests.fs, tests/Elmish.Tests/Feature149CompositorMetricsTests.fs, tests/SkiaViewer.Tests/Feature149SnapshotLifecycleTests.fs, tests/Rendering.Harness.Tests/Feature149ReuseEvidenceTests.fs, tests/Rendering.Harness.Tests/Feature149SnapshotEvidenceTests.fs, and tests/Rendering.Harness.Tests/Feature149TimingEvidenceTests.fs to tests/Controls.Tests/Controls.Tests.fsproj, tests/Elmish.Tests/Elmish.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 3

- [X] T044 [US3] Complete separate content identity and placement identity tracking, previous placement capture, content-change refresh, placement-only reuse, churn demotion, no-benefit demotion, and failed-parity demotion in src/Controls/RetainedRender.fs and src/Controls/Diagnostics.fs
- [X] T045 [US3] Implement deterministic reuse, refresh, fallback, demotion, expected saved work, measured overhead, snapshot byte, and timing summary metrics in src/Controls.Elmish/ControlsElmish.fs against the T036 public contracts
- [X] T046 [US3] Implement bounded snapshot resource allocation, content identity validation, host profile validation, byte estimates, budget checks, refresh, replacement, eviction, disposal, bypass, unsupported state, and lifecycle diagnostics in src/SkiaViewer/PictureReplayCache.fs
- [X] T047 [US3] Integrate snapshot composition, snapshot-assisted oracle parity, resource failure handling, unsupported-host disclosure, lower-tier fallback, and full-redraw fallback in src/SkiaViewer/SceneRenderer.fs and src/SkiaViewer/Host/OpenGl.fs
- [X] T048 [US3] Implement real timing probes with warmup frames, measured frames, full-redraw baseline, damage-scoped comparison, placement/replay comparison, snapshot-assisted comparison, threshold evaluation, noisy/incomplete handling, and environment-limited verdicts in tests/Rendering.Harness/Perf.fs
- [X] T049 [US3] Implement compositor-reuse, compositor-snapshots, and compositor-timing --feature 149 command execution, artifact writing, tier verdict formatting, fallback formatting, and non-overclaim exit behavior in tests/Rendering.Harness/Compositor.fs, tests/Rendering.Harness/Evidence.fs, and tests/Rendering.Harness/Cli.fs
- [X] T050 [US3] Record reuse, snapshot, and timing artifact schemas, corpus coverage, lifecycle states, threshold policy, measured/inconclusive outcomes, unsupported-host limitations, and current evidence links in specs/149-complete-compositor-p7/readiness/reuse/README.md, specs/149-complete-compositor-p7/readiness/snapshots/README.md, and specs/149-complete-compositor-p7/readiness/timing/README.md

**Checkpoint**: User Story 3 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature149Reuse`, `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature149CompositorMetrics`, `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature149Snapshot`, `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-reuse --feature 149 --out specs/149-complete-compositor-p7/readiness/reuse`, `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-snapshots --feature 149 --out specs/149-complete-compositor-p7/readiness/snapshots`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --feature 149 --tier snapshot --out specs/149-complete-compositor-p7/readiness/timing`.

---

## Phase 6: User Story 4 - Publish Consumer-Visible Compositor Readiness (Priority: P3)

**Goal**: Package consumers and generated products can query stable compositor readiness diagnostics
and maintainers can review one summary that distinguishes accepted, environment-limited, failed,
fallback, incomplete, and blocked states.

**Independent Test**: Use only public package contracts to query proof status, damage parity,
reuse, snapshot, timing, fallback, readiness verdict, limitations, and artifact paths; package
validation reports only documented compositor deltas.

### Public Surface for User Story 4

- [X] T051 [US4] Define final consumer-visible compositor proof, damage parity, reuse, snapshot, timing, fallback, readiness verdict, limitation, and artifact-path diagnostic contracts in src/Controls/Diagnostics.fsi, src/Controls.Elmish/ControlsElmish.fsi, src/SkiaViewer/CompositorProof.fsi, src/Testing/Testing.fsi, and tests/Rendering.Harness/Compositor.fsi

### Tests for User Story 4

- [X] T052 [P] [US4] Add public FSI transcript coverage for proof status, damage parity status, reuse status, snapshot status, timing status, fallback status, readiness verdict, limitations, and artifact paths in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T053 [P] [US4] Add package compatibility ledger tests for public API deltas, diagnostic deltas, surface baseline references, release notes, migration guidance, fallback behavior changes, limitations, and prior P5/P6/P7 evidence-surface regressions in tests/Package.Tests/Feature149CompatibilityLedgerTests.fs
- [X] T054 [P] [US4] Add consumer validation helper tests for querying compositor readiness from generated-product-facing APIs without broad implementation dependencies in tests/Testing.Tests/Feature149CompositorReadinessTests.fs
- [X] T055 [P] [US4] Add readiness assembly tests for accepted, environment-limited, failed, incomplete, rejected, skipped, stale-proof, host-mismatch, missing timing, failed parity, resource failure, compatibility-blocked, unsupported-host under-2-minute completion with zero accepted partial-redraw artifacts, and formatter output cases in tests/Rendering.Harness.Tests/Feature149ReadinessPackageTests.fs
- [X] T056 [US4] Add tests/Package.Tests/Feature149CompatibilityLedgerTests.fs, tests/Testing.Tests/Feature149CompositorReadinessTests.fs, and tests/Rendering.Harness.Tests/Feature149ReadinessPackageTests.fs to tests/Package.Tests/Package.Tests.fsproj, tests/Testing.Tests/Testing.Tests.fsproj, and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 4

- [X] T057 [US4] Implement public diagnostic status values, fallback messages, readiness summary helpers, limitations, and artifact-path formatters in src/Controls/Diagnostics.fs, src/Controls.Elmish/ControlsElmish.fs, src/SkiaViewer/CompositorProof.fs, and src/Testing/Testing.fs
- [X] T058 [US4] Implement readiness Model, Msg, Effect, pure update transitions, artifact path validation, tier verdict evaluation, compatibility-blocking logic, and deterministic summary rendering in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Evidence.fs
- [X] T059 [US4] Implement compositor-readiness --feature 149 CLI execution, evidence loading, validation-summary writing, compatibility-ledger writing, limitation classification, and non-overclaim exit behavior in tests/Rendering.Harness/Cli.fs and tests/Rendering.Harness/Compositor.fs
- [X] T060 [US4] Populate public API changes, diagnostics changes, fallback behavior changes, surface baseline references, release notes, migration guidance, and limitations in specs/149-complete-compositor-p7/readiness/compatibility-ledger.md
- [X] T061 [US4] Update consumer-facing compositor readiness, fallback interpretation, proof limitations, damage parity, reuse, snapshot, timing, and generated-product helper documentation in src/Controls/README.md, src/SkiaViewer/README.md, and src/Testing/README.md
- [X] T062 [US4] Generate the final Feature149 readiness verdict, tier verdicts, artifact links, fallbacks, compatibility impact, and limitations in specs/149-complete-compositor-p7/readiness/validation-summary.md

**Checkpoint**: User Story 4 can be validated with `dotnet fsi scripts/refresh-surface-baselines.fsx`, `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature149`, `dotnet test tests/Testing.Tests/Testing.Tests.fsproj --filter Feature149`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 149 --out specs/149-complete-compositor-p7/readiness`.

---

## Phase 7: Polish and Cross-Cutting Concerns

**Purpose**: Complete Tier 1 baselines, regression validation, quickstart evidence, pack proof, and
final readiness records.

- [X] T063 [P] Refresh public surface baselines with scripts/refresh-surface-baselines.fsx and verify intentional Feature149 deltas in tests/surface-baselines/FS.GG.UI.Controls.txt, tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt, tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, tests/surface-baselines/FS.GG.UI.Testing.txt, and tests/surface-baselines/FS.GG.UI.Scene.txt
- [X] T064 Run live proof validation and record capable-host, failed, environment-limited, and unsupported-host under-2-minute results in specs/149-complete-compositor-p7/readiness/validation-summary.md using tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T065 Run damage-scoped parity validation and record corpus coverage, full-redraw oracle parity, fallback reasons, zero-damage behavior, and scissor reset evidence in specs/149-complete-compositor-p7/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T066 Run reuse, snapshot, and timing validation and record reuse decisions, snapshot lifecycle, resource budgets, timing verdicts, inconclusive timing, and unsupported-host limitations in specs/149-complete-compositor-p7/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj, tests/Elmish.Tests/Elmish.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T067 Run focused P5, P6, and P7 evidence-surface regression validation for render-anywhere, overlay, text-shaping, package-readiness, readiness, compatibility, package, and generated-product behavior; record documented public-surface deltas in specs/149-complete-compositor-p7/readiness/validation-summary.md using tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj, tests/Package.Tests/Package.Tests.fsproj, tests/Testing.Tests/Testing.Tests.fsproj, and tests/surface-baselines/FS.GG.UI.SkiaViewer.txt
- [X] T068 Run full solution, package surface, and local pack validation and record final results in specs/149-complete-compositor-p7/readiness/validation-summary.md using FS.GG.Rendering.slnx, tests/Package.Tests/Package.Tests.fsproj, scripts/refresh-surface-baselines.fsx, and ~/.local/share/nuget-local/

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **US1 Live Proof (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 Damage Redraw (Phase 4)**: Depends on US1 proof readiness and fallback diagnostics.
- **US3 Reuse, Snapshot, and Timing (Phase 5)**: Depends on US1 proof behavior and US2 oracle parity/fallback records.
- **US4 Public Readiness (Phase 6)**: Depends on US1 and US2 for minimum readiness, and depends on US3 for full P7 accepted performance claims.
- **Polish (Phase 7)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and has no dependency on other user stories.
- **US2 (P1)**: Requires US1 proof acceptance and rejection behavior.
- **US3 (P2)**: Requires US2 damage records, fallback reasons, and full-redraw oracle path.
- **US4 (P3)**: Requires evidence from US1 and US2 for the first reviewable package; full accepted P7 readiness requires US3 reuse, snapshot, and timing evidence.

### Within Each User Story

- Write tests first and confirm they fail for missing behavior.
- Draft `.fsi` public signatures before semantic/FSI transcript tests and before `.fs` implementation bodies.
- Update `.fsproj` compile ordering whenever new F# files are added.
- Preserve full-redraw oracle parity before accepting damage-scoped, reuse, snapshot, or timing evidence.
- Record readiness evidence before claiming a tier as ready.
- Keep `tests/Rendering.Harness` as the evidence and CLI edge, with package code exposing only consumer-appropriate diagnostics.

---

## Parallel Opportunities

- T004, T005, and T006 can run in parallel because they write different readiness files.
- T009, T010, and T012 can run in parallel after T007 defines shared Feature149 names.
- US1 tests T014, T015, T016, and T017 can run in parallel after T013; T018 is the compile-order wiring task.
- US2 tests T026, T027, and T028 can run in parallel after T025; implementation T030 and T031 can run in parallel before host integration T032.
- US3 tests T037, T038, T039, T040, T041, and T042 can run in parallel after T036; implementation T044, T045, T046, and T048 touch separate packages and can proceed in parallel once contract names are stable.
- US4 tests T052, T053, T054, and T055 can run in parallel after T051; implementation T057 and T058 touch different package/harness boundaries and can proceed in parallel before CLI wiring T059.
- T063 can run in parallel with documentation review once public contracts are stable.

---

## Parallel Example: User Story 1

```bash
Task: "T014 Add CompositorProof FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T015 Add live proof tests in tests/SkiaViewer.Tests/Feature149LiveProofTests.fs"
Task: "T016 Add live proof simulation tests in tests/SkiaViewer.Tests/Feature149LiveProofSimulationTests.fs"
Task: "T017 Add live proof evidence tests in tests/Rendering.Harness.Tests/Feature149LiveProofEvidenceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T026 Add damage plan tests in tests/Controls.Tests/Feature149DamagePlanTests.fs"
Task: "T027 Add damage-scoped redraw tests in tests/SkiaViewer.Tests/Feature149DamageScopedRedrawTests.fs"
Task: "T028 Add damage parity harness tests in tests/Rendering.Harness.Tests/Feature149DamageParityTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T037 Add reuse decision tests in tests/Controls.Tests/Feature149ReuseDecisionTests.fs"
Task: "T038 Add compositor metric tests in tests/Elmish.Tests/Feature149CompositorMetricsTests.fs"
Task: "T039 Add snapshot lifecycle tests in tests/SkiaViewer.Tests/Feature149SnapshotLifecycleTests.fs"
Task: "T040 Add reuse evidence tests in tests/Rendering.Harness.Tests/Feature149ReuseEvidenceTests.fs"
Task: "T041 Add snapshot evidence tests in tests/Rendering.Harness.Tests/Feature149SnapshotEvidenceTests.fs"
Task: "T042 Add timing evidence tests in tests/Rendering.Harness.Tests/Feature149TimingEvidenceTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T052 Add public FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T053 Add compatibility ledger tests in tests/Package.Tests/Feature149CompatibilityLedgerTests.fs"
Task: "T054 Add consumer validation helper tests in tests/Testing.Tests/Feature149CompositorReadinessTests.fs"
Task: "T055 Add readiness package tests in tests/Rendering.Harness.Tests/Feature149ReadinessPackageTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate live proof independently with SkiaViewer tests and `compositor-live-proof`.
5. Record whether the current environment is accepted, failed, or environment-limited.

### Incremental Delivery

1. Deliver US1 live proof and rejection behavior.
2. Deliver US2 proof-gated damage redraw and oracle parity.
3. Deliver US3 reuse, snapshot, and timing evidence.
4. Deliver US4 public diagnostics and readiness packaging.
5. Finish Phase 7 validation and package proof.

### Parallel Team Strategy

After Phase 2, split by package boundary where possible: one contributor on SkiaViewer live proof
and host state, one on Controls retained damage/reuse policy, one on Rendering.Harness evidence and
readiness, and one on public package/compatibility tests. Merge only after each story checkpoint
passes its independent validation.

---

## Notes

- `[P]` tasks write different files or can proceed without depending on incomplete task results.
- `[US1]` through `[US4]` labels map directly to the user stories in spec.md.
- Tests are required by this feature specification and should fail before the implementation task that satisfies them.
- Performance claims remain blocked until live proof, parity, fallback, resource, timing, and compatibility evidence all pass.
- Environment-limited or synthetic-only evidence must be disclosed and cannot mark a tier ready.
- Synthetic tests or fixtures must use `Synthetic` in the test name, include a `// SYNTHETIC:` use-site disclosure with reason, and be listed in the PR description per the constitution.
