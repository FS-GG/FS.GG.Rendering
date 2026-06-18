# Tasks: Compositor Live Integration

**Input**: Design documents from `/specs/148-compositor-live-integration/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: Required. The specification declares mandatory user scenarios, live host proof, oracle
parity, reuse and snapshot lifecycle validation, real timing probes, package surface checks, and
readiness evidence.

**Organization**: Tasks are grouped by user story so each story can be implemented and verified as
an independently testable increment. Public or observable surfaces follow the repository rule:
`.fsi` first, semantic/FSI tests next, implementation after that.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create readiness locations and shared evidence skeletons before implementation begins.

- [X] T001 Create readiness placeholder files in specs/148-compositor-live-integration/readiness/live-proof/.gitkeep, specs/148-compositor-live-integration/readiness/parity/.gitkeep, specs/148-compositor-live-integration/readiness/reuse/.gitkeep, specs/148-compositor-live-integration/readiness/snapshots/.gitkeep, and specs/148-compositor-live-integration/readiness/timing/.gitkeep
- [X] T002 Create the validation summary skeleton in specs/148-compositor-live-integration/readiness/validation-summary.md
- [X] T003 Create the compatibility ledger skeleton in specs/148-compositor-live-integration/readiness/compatibility-ledger.md
- [X] T004 [P] Create the exact Feature148 corpus, target host profiles, threshold, and resource budget inventory in specs/148-compositor-live-integration/readiness/corpus.md
- [X] T005 [P] Create the live proof artifact schema and environment-limited disclosure note in specs/148-compositor-live-integration/readiness/live-proof/README.md

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Establish shared Feature148 host profiles, corpus names, artifact paths, threshold
values, and assertion helpers used by every user story.

**Critical**: No user story implementation should start until this phase is complete.

- [X] T006 Define exact Feature148 readiness directories, live proof profiles, scenario IDs, tier names including replay, timing thresholds, snapshot budgets, and artifact path contracts in tests/Rendering.Harness/Compositor.fsi
- [X] T007 Implement exact Feature148 readiness directories, live proof profiles, scenario IDs, tier names including replay, timing thresholds, snapshot budgets, and artifact path helpers in tests/Rendering.Harness/Compositor.fs
- [X] T008 [P] Add reusable Feature148 assertion helpers for proof verdicts, damage parity, reuse decisions, snapshot lifecycle, timing thresholds, and readiness paths in tests/Rendering.Harness/TestAssertions.fs
- [X] T009 [P] Add Feature148 compatibility ledger test scaffolding in tests/Package.Tests/Feature148CompatibilityLedgerTests.fs and add it to tests/Package.Tests/Package.Tests.fsproj before Tests.fs
- [X] T010 [P] Add concrete Feature148 FSI transcript coverage scaffolding for SkiaViewer, Controls, Controls.Elmish, Rendering.Harness, and Testing surfaces in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T011 Add Feature148 harness command routing stubs for compositor-live-proof, compositor-parity, compositor-reuse, compositor-snapshots, compositor-timing, and compositor-readiness in tests/Rendering.Harness/Cli.fs

**Checkpoint**: Shared host profiles, corpus IDs, thresholds, budgets, paths, assertion helpers, and
command names exist.

---

## Phase 3: User Story 1 - Prove Live Partial Redraw Safety (Priority: P1) MVP

**Goal**: Maintainers can run a live preservation proof on the active SkiaViewer/OpenGL host and
receive passed, failed, or environment-limited before partial redraw can be accepted.

**Independent Test**: Run the live proof on capable, non-preserving, stale, host-mismatched, and
unsupported profiles; only a fresh passed proof for the active profile unlocks partial redraw
readiness.

### Public Surface for User Story 1

- [ ] T012 [US1] Extend HostProfile, PresentProof, proof artifact, sample-region, freshness, package version, failure cause, MVU Model/Msg/Effect, and live interpreter contracts in src/SkiaViewer/CompositorProof.fsi

### Tests for User Story 1

- [X] T013 [P] [US1] Add CompositorProof FSI transcript coverage for live profile facts, sample regions, artifacts, freshness, readiness rejection, and MVU transitions in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T014 [P] [US1] Add live proof MVU, capable-host, stale-proof, host-mismatch, missing-artifact, algorithm-mismatch, and deterministic ID tests in tests/SkiaViewer.Tests/Feature148LiveProofTests.fs
- [X] T015 [P] [US1] Add Synthetic-named simulated non-preserving, unsupported readback, missing display, timeout, permission, and host error tests with `// SYNTHETIC:` disclosure comments in tests/SkiaViewer.Tests/Feature148LiveProofSimulationTests.fs
- [X] T016 [P] [US1] Add compositor-live-proof harness artifact, verdict formatting, environment-limited disclosure, and synthetic-evidence disclosure tests in tests/Rendering.Harness.Tests/Feature148LiveProofEvidenceTests.fs
- [X] T017 [US1] Add tests/SkiaViewer.Tests/Feature148LiveProofTests.fs, tests/SkiaViewer.Tests/Feature148LiveProofSimulationTests.fs, and tests/Rendering.Harness.Tests/Feature148LiveProofEvidenceTests.fs to tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 1

- [ ] T018 [US1] Implement active host profile detection, package version capture, proof identity, proof freshness, and proof readiness rejection in src/SkiaViewer/CompositorProof.fs
- [ ] T019 [US1] Implement full sentinel draw, scissored damage draw, no-clear state, readback sampling, and artifact capture effects in src/SkiaViewer/Host/OpenGl.fs and src/SkiaViewer/CompositorProof.fs
- [X] T020 [US1] Implement passed, failed, and environment-limited classification for untouched samples, damaged samples, unsupported readback, missing display, timeout, permission, host error, stale proof, and host mismatch in src/SkiaViewer/CompositorProof.fs
- [ ] T021 [US1] Implement live proof Markdown and machine-readable artifact rendering with host facts, sample regions, artifact references, verdicts, and diagnostics in src/SkiaViewer/CompositorProof.fs
- [X] T022 [US1] Implement compositor-live-proof command execution, output argument parsing, live proof artifact writing, and non-overclaim exit codes in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [X] T023 [US1] Record live proof run results, artifact schema, target host facts, failure categories, and environment-limited limitations in specs/148-compositor-live-integration/readiness/live-proof/README.md

**Checkpoint**: User Story 1 can be validated with `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148LiveProof` and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-live-proof --out specs/148-compositor-live-integration/readiness/live-proof`.

---

## Phase 4: User Story 2 - Redraw Only Damaged Areas With Safe Fallback (Priority: P1)

**Goal**: Proof-gated damage-scoped redraw produces the same final frame as full-frame redraw while
unsupported or unsafe frames fall back with explicit diagnostics.

**Independent Test**: Compare proof-gated damage-scoped frames against full-frame oracle frames for
localized, overlapping, edge, resize, theme, stale-proof, disabled, and parity-failure scenarios.

### Public Surface for User Story 2

- [ ] T024 [US2] Extend damage plan, damage cause, source boundary, full-frame invalidation, fallback reason, scissor metric, and frame diagnostic contracts in src/Controls/RetainedRender.fsi, src/Controls/Diagnostics.fsi, src/Controls.Elmish/ControlsElmish.fsi, and src/SkiaViewer/SkiaViewer.fsi

### Tests for User Story 2

- [X] T025 [P] [US2] Add clipped damage, overlap union-area, edge clipping, full-frame invalidation, source-boundary, movement old/new region, and idle empty-damage tests in tests/Controls.Tests/Feature148DamagePlanTests.fs
- [X] T026 [P] [US2] Add proof-gated scissor, no-clear draw, scissor reset, full-redraw fallback, disabled mode, stale proof, host mismatch, and readback reset tests in tests/SkiaViewer.Tests/Feature148DamageScopedRedrawTests.fs
- [X] T027 [P] [US2] Add compositor-parity harness tests for localized, overlapping, edge, movement, resize, theme/global, stale-proof, disabled, unsupported, and parity-failure cases in tests/Rendering.Harness.Tests/Feature148DamageParityTests.fs
- [X] T028 [US2] Add tests/Controls.Tests/Feature148DamagePlanTests.fs, tests/SkiaViewer.Tests/Feature148DamageScopedRedrawTests.fs, and tests/Rendering.Harness.Tests/Feature148DamageParityTests.fs to tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 2

- [X] T029 [US2] Implement damage plan clipping, deduplication, true union area, source boundary attribution, movement old/new regions, empty-idle handling, and full-frame invalidation causes in src/Controls/RetainedRender.fs
- [ ] T030 [US2] Implement proof, fallback, damage, full-frame invalidation, scissor, no-clear, reset, and parity diagnostics in src/Controls/Diagnostics.fs and src/Controls.Elmish/ControlsElmish.fs
- [ ] T031 [US2] Implement accepted-proof damage-scoped redraw, damage-union scissor coverage, no-clear presentation, and host state reset in src/SkiaViewer/SceneRenderer.fs and src/SkiaViewer/Host/OpenGl.fs
- [ ] T032 [US2] Implement full-frame fallback for missing proof, stale proof, failed proof, host mismatch, disabled compositor mode, frame-wide invalidation, unsupported host, unsafe damage, and parity failure in src/SkiaViewer/SkiaViewer.fs and src/SkiaViewer/SkiaViewer.fsi
- [X] T033 [US2] Implement compositor-parity command corpus execution, full-frame oracle comparison, damage-scoped output identity records, fallback summaries, and parity artifact writing in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [X] T034 [US2] Record damage parity schema, corpus coverage, fallback categories, scissor state reset evidence, and parity limitations in specs/148-compositor-live-integration/readiness/parity/README.md

**Checkpoint**: User Story 2 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148Damage`, `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148Damage`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-parity --feature 148 --out specs/148-compositor-live-integration/readiness/parity`.

---

## Phase 5: User Story 3 - Reuse Stable Moving Content Safely (Priority: P2)

**Goal**: Stable content that only moves can be reused at the new placement while old and new
regions are damaged, and stale or churning content is refreshed or demoted.

**Independent Test**: Run stable, moving-only, scrolling, content-changing, and churning scenarios;
verify frame parity, old/new movement damage, deterministic decisions, and at least 30% repeated
work reduction on the moving/scrolling corpus.

### Public Surface for User Story 3

- [ ] T035 [US3] Define reusable boundary, content identity, placement identity, previous placement, reuse decision, demotion reason, expected saved work, measured overhead, and diagnostic contracts in src/Controls/RetainedRender.fsi and src/Controls/Diagnostics.fsi

### Tests for User Story 3

- [X] T036 [P] [US3] Add stable promotion, placement-only movement, old/new region damage, content-change rejection, theme/resource invalidation, churn demotion, no-benefit demotion, and deterministic decision tests in tests/Controls.Tests/Feature148PlacementReuseTests.fs
- [X] T037 [P] [US3] Add ControlsElmish metric tests for content identity, placement identity, reuse hits, refreshes, demotions, fallback reasons, repeated-work reduction, and overhead reporting in tests/Elmish.Tests/Feature148CompositorMetricsTests.fs
- [X] T038 [P] [US3] Add compositor-reuse harness tests for stable, moving-only, scrolling, content-changing, churning, failed-parity, and same-seed evidence records in tests/Rendering.Harness.Tests/Feature148ReuseEvidenceTests.fs
- [X] T039 [US3] Add tests/Controls.Tests/Feature148PlacementReuseTests.fs, tests/Elmish.Tests/Feature148CompositorMetricsTests.fs, and tests/Rendering.Harness.Tests/Feature148ReuseEvidenceTests.fs to tests/Controls.Tests/Controls.Tests.fsproj, tests/Elmish.Tests/Elmish.Tests.fsproj, and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 3

- [X] T040 [US3] Implement separate content identity and placement identity tracking for reusable retained boundaries in src/Controls/RetainedRender.fs
- [X] T041 [US3] Implement placement-only reuse with previous placement capture, new placement capture, old/new region damage, and unsafe movement fallback in src/Controls/RetainedRender.fs
- [X] T042 [US3] Implement content-change, theme-change, provider-change, resource-change, host-profile-change, failed-parity, churn, and no-benefit invalidation or demotion decisions in src/Controls/RetainedRender.fs and src/Controls/Diagnostics.fs
- [X] T043 [US3] Surface deterministic reuse, refresh, fallback, demotion, expected saved work, measured overhead, and repeated-work reduction metrics in src/Controls.Elmish/ControlsElmish.fsi and src/Controls.Elmish/ControlsElmish.fs
- [X] T044 [US3] Implement compositor-reuse command corpus execution, reuse decision artifact writing, repeated-work reduction summaries, and demotion records in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [X] T045 [US3] Record reuse artifact schema, moving/scrolling corpus coverage, parity requirements, 30% reduction evidence, and demotion limitations in specs/148-compositor-live-integration/readiness/reuse/README.md

**Checkpoint**: User Story 3 can be validated with `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148Reuse`, `dotnet test tests/Elmish.Tests/Elmish.Tests.fsproj --filter Feature148CompositorMetrics`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-reuse --out specs/148-compositor-live-integration/readiness/reuse`.

---

## Phase 6: User Story 4 - Manage Snapshot Reuse With Bounded Lifecycle (Priority: P2)

**Goal**: Expensive stable content can use a bounded SkiaViewer-owned snapshot tier with visible
creation, reuse, refresh, eviction, disposal, fallback, and benefit evidence.

**Independent Test**: Run expensive-stable, simple, churning, over-budget, invalid-resource, and
unsupported-host scenarios; verify parity, bounded resource use, safe fallback, and at least 20%
frame-cost improvement before a snapshot-ready verdict.

### Public Surface for User Story 4

- [ ] T046 [US4] Define snapshot resource identity, content freshness, host profile validity, byte budget, lifecycle state, support status, fallback, and diagnostic contracts in src/SkiaViewer/PictureReplayCache.fsi, src/SkiaViewer/SkiaViewer.fsi, and src/Controls/RetainedRender.fsi

### Tests for User Story 4

- [X] T047 [P] [US4] Add snapshot eligibility, expensive-stable promotion, simple-scene rejection, churning demotion, no-benefit demotion, over-budget rejection, and parity-clean prerequisite tests in tests/Controls.Tests/Feature148SnapshotEligibilityTests.fs
- [X] T048 [P] [US4] Add snapshot resource allocation, composition, byte budget, content mismatch, host mismatch, refresh, eviction, disposal, invalid-resource, unsupported-host, and fallback tests in tests/SkiaViewer.Tests/Feature148SnapshotLifecycleTests.fs
- [X] T049 [P] [US4] Add compositor-snapshots harness tests for lifecycle artifact records, resource budget summaries, unsupported-host limitations, parity failure rejection, and 20% benefit requirement in tests/Rendering.Harness.Tests/Feature148SnapshotEvidenceTests.fs
- [X] T050 [US4] Add tests/Controls.Tests/Feature148SnapshotEligibilityTests.fs, tests/SkiaViewer.Tests/Feature148SnapshotLifecycleTests.fs, and tests/Rendering.Harness.Tests/Feature148SnapshotEvidenceTests.fs to tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 4

- [X] T051 [US4] Implement snapshot eligibility inputs, expensive-stable classification, simple/churning rejection, measured benefit gating, and demotion decisions in src/Controls/RetainedRender.fs
- [ ] T052 [US4] Implement bounded snapshot resource allocation, content identity validation, host profile validation, byte estimates, budget checks, refresh, eviction, disposal, bypass, and lifecycle diagnostics in src/SkiaViewer/PictureReplayCache.fs
- [ ] T053 [US4] Integrate snapshot composition, lower-tier fallback, full-redraw fallback, resource failure handling, and unsupported-host disclosure in src/SkiaViewer/SceneRenderer.fs and src/SkiaViewer/Host/OpenGl.fs
- [X] T054 [US4] Implement compositor-snapshots command execution, snapshot lifecycle artifact writing, resource budget summaries, parity summaries, and unsupported-host limitations in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [X] T055 [US4] Record snapshot artifact schema, budget policy, lifecycle states, unsupported-host limitations, parity requirements, and 20% benefit evidence in specs/148-compositor-live-integration/readiness/snapshots/README.md

**Checkpoint**: User Story 4 can be validated with `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj --filter Feature148Snapshot`, `dotnet test tests/Controls.Tests/Controls.Tests.fsproj --filter Feature148Snapshot`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-snapshots --out specs/148-compositor-live-integration/readiness/snapshots`.

---

## Phase 7: User Story 5 - Claim Real Timing Wins With Reviewable Evidence (Priority: P2)

**Goal**: Reviewers can inspect one readiness package that ties proof, parity, fallback, reuse,
snapshot lifecycle, real timing probes, and compatibility impact to tier verdicts before P7 is
reported as delivering performance value.

**Independent Test**: Assemble readiness from capable, unsupported, beneficial, non-beneficial,
failed-parity, missing-timing, stale-proof, and environment-limited runs; reviewers can determine
ready, limited, rejected, or skipped tier status within 10 minutes.

### Public Surface for User Story 5

- [ ] T056 [US5] Define timing probe records, tier baselines for damage, placement, replay, and snapshot, thresholds, readiness package records, compatibility impact records, MVU Model/Msg/Effect, update, and formatter contracts in tests/Rendering.Harness/Compositor.fsi, tests/Rendering.Harness/Evidence.fsi, and tests/Rendering.Harness/Perf.fsi

### Tests for User Story 5

- [X] T057 [P] [US5] Add timing probe tests for damage, placement, replay, snapshot, beneficial corpus, non-beneficial corpus, warmup exclusion, lower-tier baselines, failed thresholds, demotion, and environment-limited timing in tests/Rendering.Harness.Tests/Feature148TimingEvidenceTests.fs
- [X] T058 [P] [US5] Add readiness assembly tests for ready, limited, rejected, skipped, failed parity, missing proof, stale proof, host mismatch, resource failure, missing timing, Synthetic-named synthetic-only, environment-limited, and compatibility-blocked evidence in tests/Rendering.Harness.Tests/Feature148ReadinessPackageTests.fs
- [X] T059 [P] [US5] Add compatibility ledger tests for public metrics, diagnostics, surface baseline references, release notes, migration guidance, limitations, and changed fallback behavior in tests/Package.Tests/Feature148CompatibilityLedgerTests.fs
- [X] T060 [P] [US5] Add FSI transcript coverage for new proof, diagnostics, metrics, harness, testing, and readiness surfaces in tests/Package.Tests/FsiTranscriptCoverageTests.fs
- [X] T061 [US5] Add tests/Rendering.Harness.Tests/Feature148TimingEvidenceTests.fs and tests/Rendering.Harness.Tests/Feature148ReadinessPackageTests.fs to tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj before Program.fs

### Implementation for User Story 5

- [ ] T062 [US5] Implement real timing probe baselines, warmup/measured frame separation, damage/placement/replay/snapshot tier comparisons, beneficial and non-beneficial corpus thresholds, and environment-limited timing verdicts in tests/Rendering.Harness/Perf.fs
- [X] T063 [US5] Implement readiness Model, Msg, Effect, pure update transitions, tier verdict evaluation, missing-evidence rejection, artifact path validation, and deterministic same-seed verdict records in tests/Rendering.Harness/Compositor.fs
- [ ] T064 [US5] Implement validation-summary, compatibility-ledger, timing report, fallback summary, proof summary, reuse summary, snapshot summary, and limitation formatting in tests/Rendering.Harness/Evidence.fs
- [X] T065 [US5] Implement compositor-timing including `--tier replay` and compositor-readiness command execution, output argument parsing, artifact writing, and non-overclaim exit codes in tests/Rendering.Harness/Compositor.fs and tests/Rendering.Harness/Cli.fs
- [X] T066 [US5] Populate tier verdicts, proof links, parity links, fallback links, reuse links, snapshot links, timing links, compatibility impact, and limitations in specs/148-compositor-live-integration/readiness/validation-summary.md and specs/148-compositor-live-integration/readiness/compatibility-ledger.md

**Checkpoint**: User Story 5 can be validated with `dotnet test tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj --filter Feature148`, `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-timing --tier damage --out specs/148-compositor-live-integration/readiness/timing`, and `dotnet run --project tests/Rendering.Harness/Rendering.Harness.fsproj -- compositor-readiness --feature 148 --out specs/148-compositor-live-integration/readiness`.

---

## Phase 8: Polish and Cross-Cutting Concerns

**Purpose**: Complete Tier 1 documentation, surface baselines, quickstart validation, package proof,
and final readiness evidence.

- [X] T067 [P] Update compositor behavior, proof limitations, fallback diagnostics, reuse diagnostics, snapshot lifecycle, timing claims, and migration notes in src/Controls/README.md and src/SkiaViewer/README.md
- [X] T068 [P] Update public metrics, diagnostics, baseline delta, release note, migration guidance, and limitation sections in specs/148-compositor-live-integration/readiness/compatibility-ledger.md
- [X] T069 Refresh public surface baselines with scripts/refresh-surface-baselines.fsx and verify intentional Feature148 deltas in tests/surface-baselines/FS.GG.UI.Controls.txt, tests/surface-baselines/FS.GG.UI.Controls.Elmish.txt, tests/surface-baselines/FS.GG.UI.SkiaViewer.txt, tests/surface-baselines/FS.GG.UI.Testing.txt, and tests/surface-baselines/FS.GG.UI.Scene.txt
- [X] T070 Run live preservation proof validation and record host profile verdicts, artifacts, and environment limitations in specs/148-compositor-live-integration/readiness/validation-summary.md using tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T071 Run damage-scoped redraw parity validation and record corpus coverage, parity verdicts, fallback reasons, and scissor reset evidence in specs/148-compositor-live-integration/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T072 Run content/placement reuse validation and record repeated-work reduction, old/new movement damage, demotion, and parity results in specs/148-compositor-live-integration/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj, tests/Elmish.Tests/Elmish.Tests.fsproj, and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T073 Run snapshot lifecycle validation and record resource budget, refresh, eviction, disposal, unsupported-host, parity, and benefit results in specs/148-compositor-live-integration/readiness/validation-summary.md using tests/Controls.Tests/Controls.Tests.fsproj, tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj, and tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T074 Run real timing probes and record damage, placement, replay, snapshot, beneficial-corpus, non-beneficial-corpus, lower-tier baseline, threshold, and environment-limited results in specs/148-compositor-live-integration/readiness/validation-summary.md using tests/Rendering.Harness/Rendering.Harness.fsproj
- [X] T075 Run readiness package and compatibility ledger validation and record ready, limited, rejected, and skipped tier statuses in specs/148-compositor-live-integration/readiness/validation-summary.md using tests/Rendering.Harness.Tests/Rendering.Harness.Tests.fsproj and tests/Package.Tests/Package.Tests.fsproj
- [X] T076 Run full solution, package surface, and pack validation and record final results in specs/148-compositor-live-integration/readiness/validation-summary.md using FS.GG.Rendering.slnx, tests/Package.Tests/Package.Tests.fsproj, and ~/.local/share/nuget-local/

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks all user stories.
- **US1 Live Proof (Phase 3)**: Depends on Foundational and is the MVP.
- **US2 Damage Redraw (Phase 4)**: Depends on US1 proof readiness and fallback contracts.
- **US3 Content/Placement Reuse (Phase 5)**: Depends on US2 damage planning and oracle parity.
- **US4 Snapshot Lifecycle (Phase 6)**: Depends on US3 boundary identities and demotion policy.
- **US5 Timing and Readiness (Phase 7)**: Depends on US1 through US4 evidence formats; it can classify incomplete later tiers as skipped or limited during incremental delivery.
- **Polish (Phase 8)**: Depends on all desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational and has no dependency on other user stories.
- **US2 (P1)**: Requires US1 live proof acceptance and rejection behavior.
- **US3 (P2)**: Requires US2 damage records, fallback reasons, and full-frame oracle path.
- **US4 (P2)**: Requires US3 reusable boundary identities and demotion policy.
- **US5 (P2)**: Requires US1 and US2 for the first reviewable package; full ready claims require US3 and US4 evidence too.

### Within Each User Story

- Write tests first and confirm they fail for missing behavior.
- Draft `.fsi` public signatures before semantic/FSI transcript tests and before `.fs` implementation bodies.
- Update `.fsproj` compile ordering whenever new F# files are added.
- Preserve full-frame oracle parity before accepting timing or work-reduction evidence.
- Record readiness evidence before claiming a tier as ready.
- Keep `tests/Rendering.Harness` dependencies flowing through contracts and formatters, with `Cli.fs` orchestrating edge execution.

---

## Parallel Opportunities

- T004 and T005 can run in parallel with T002 and T003 because they write different readiness files.
- T008, T009, and T010 can run in parallel after T006 defines shared names.
- US1 tests T013, T014, T015, and T016 can run in parallel after T012; T017 is the compile-order wiring task.
- US2 tests T025, T026, and T027 can run in parallel after T024; implementation T029 and T030 can run in parallel before host integration T031.
- US3 tests T036, T037, and T038 can run in parallel after T035; implementation T040 and T043 touch different packages and can run in parallel if their metric names are agreed.
- US4 tests T047, T048, and T049 can run in parallel after T046; implementation T051 and T052 can run in parallel before snapshot composition T053.
- US5 tests T057, T058, T059, and T060 can run in parallel after T056; implementation T062, T063, and T064 touch different harness modules and can proceed in parallel before CLI wiring T065.
- Polish tasks T067 and T068 can run in parallel after public behavior and compatibility impact are stable.

---

## Parallel Example: User Story 1

```bash
Task: "T013 Add CompositorProof FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
Task: "T014 Add live proof tests in tests/SkiaViewer.Tests/Feature148LiveProofTests.fs"
Task: "T015 Add live proof simulation tests in tests/SkiaViewer.Tests/Feature148LiveProofSimulationTests.fs"
Task: "T016 Add live proof evidence tests in tests/Rendering.Harness.Tests/Feature148LiveProofEvidenceTests.fs"
```

## Parallel Example: User Story 2

```bash
Task: "T025 Add damage plan tests in tests/Controls.Tests/Feature148DamagePlanTests.fs"
Task: "T026 Add damage-scoped redraw tests in tests/SkiaViewer.Tests/Feature148DamageScopedRedrawTests.fs"
Task: "T027 Add damage parity harness tests in tests/Rendering.Harness.Tests/Feature148DamageParityTests.fs"
```

## Parallel Example: User Story 3

```bash
Task: "T036 Add placement reuse tests in tests/Controls.Tests/Feature148PlacementReuseTests.fs"
Task: "T037 Add compositor metric tests in tests/Elmish.Tests/Feature148CompositorMetricsTests.fs"
Task: "T038 Add reuse evidence tests in tests/Rendering.Harness.Tests/Feature148ReuseEvidenceTests.fs"
```

## Parallel Example: User Story 4

```bash
Task: "T047 Add snapshot eligibility tests in tests/Controls.Tests/Feature148SnapshotEligibilityTests.fs"
Task: "T048 Add snapshot lifecycle tests in tests/SkiaViewer.Tests/Feature148SnapshotLifecycleTests.fs"
Task: "T049 Add snapshot evidence tests in tests/Rendering.Harness.Tests/Feature148SnapshotEvidenceTests.fs"
```

## Parallel Example: User Story 5

```bash
Task: "T057 Add timing evidence tests in tests/Rendering.Harness.Tests/Feature148TimingEvidenceTests.fs"
Task: "T058 Add readiness package tests in tests/Rendering.Harness.Tests/Feature148ReadinessPackageTests.fs"
Task: "T059 Add compatibility ledger tests in tests/Package.Tests/Feature148CompatibilityLedgerTests.fs"
Task: "T060 Add FSI transcript coverage in tests/Package.Tests/FsiTranscriptCoverageTests.fs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational.
3. Complete Phase 3: User Story 1.
4. Stop and validate live proof independently with SkiaViewer tests and `compositor-live-proof`.
5. Record whether the current environment is capable, failed, or environment-limited.

### Incremental Delivery

1. Deliver US1 live proof and rejection behavior.
2. Deliver US2 proof-gated damage redraw and oracle parity.
3. Deliver US3 content/placement reuse and repeated-work reduction.
4. Deliver US4 bounded snapshot lifecycle and benefit gating.
5. Deliver US5 timing/readiness package and compatibility evidence.
6. Finish Phase 8 validation and package proof.

### Parallel Team Strategy

After Phase 2, split by package boundary where possible: one contributor on SkiaViewer live proof
and host state, one on Controls retained damage/reuse policy, one on Rendering.Harness evidence and
readiness, and one on package/compatibility tests. Merge only after each story checkpoint passes
its independent validation.

---

## Notes

- `[P]` tasks write different files or can proceed without depending on incomplete task results.
- `[US1]` through `[US5]` labels map directly to the user stories in spec.md.
- Tests are required by this feature specification and should fail before the implementation task that satisfies them.
- Performance claims remain blocked until live proof, parity, fallback, resource, timing, and compatibility evidence all pass.
- Environment-limited or synthetic-only evidence must be disclosed and cannot mark a tier ready.
- Synthetic tests or fixtures must use `Synthetic` in the test name, include a `// SYNTHETIC:` use-site disclosure with reason, and be listed in the PR description per the constitution.
