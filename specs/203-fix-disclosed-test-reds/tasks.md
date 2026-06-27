---
description: "Task list for feature 203 — clear the disclosed pre-existing test reds and baseline flakiness"
---

# Tasks: Clear the disclosed pre-existing test reds and baseline flakiness

**Input**: Design documents from `/specs/203-fix-disclosed-test-reds/`

**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/baseline-green.contract.md ✅, quickstart.md ✅

**Tests**: This feature does **not** add new test suites — the relevant tests already exist and already
fail (the disclosed reds). The work is **corrective**: bring sample pins current, self-provision a
gate's report, correct drifted assertions to their true values (never weaken), and make GL tests
deterministic. Per-story validation runs the existing suites; no new test-authoring tasks are generated.

**Organization**: Tasks are grouped by user story (P1 → P3) so each can be implemented and verified
independently against the existing baseline runner.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story the task belongs to (US1–US4)
- Exact file paths are included in every task

## Path Conventions

Single repository, multi-package F# product. Sources under `src/`, samples under `samples/`, tests
under `tests/` and `samples/**/*.Tests`, orchestration scripts under `scripts/` and `tools/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the confirmed before-state and the verification scaffolding the whole feature
measures against.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project via the discovery-based runner `scripts/baseline-tests.fsx` (it globs `*.Tests.fsproj`, so
> the solution + `tests/Package.Tests` + every `samples/**/*.Tests` + `tests/SkiaViewer.Tests` are all
> swept). Do NOT hand-pick a subset — `dotnet test FS.GG.Rendering.slnx` deliberately omits
> `Package.Tests` and the sample suites, which is exactly where the disclosed reds hide.

- [X] T001 Confirm prerequisites and create the readiness output dir: verify .NET SDK `net10.0` and the local feed `~/.local/share/nuget-local/` exist, then `mkdir -p specs/203-fix-disclosed-test-reds/readiness` (gitignored, transient)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx | tee specs/203-fix-disclosed-test-reds/readiness/baseline-before.md` — runs EVERY test project and records the full red/green set so pre-existing reds are flagged here, not discovered at merge

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm or replace the four root-cause hypotheses against the **live** repository before
any fix. The plan's hypotheses (catalog count, ControlsGallery 52-vs-97 intent, 97th-control identity,
exact flaky GL test set) are **unverified assumptions until the suites have actually been run**.

**⚠️ CRITICAL**: No user-story fix may begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** This feature's "running app" is the test
> baseline itself. Drive the real suites and capture the *actual* numbers and failure identities before
> building on the plan's hypotheses — feature 175 showed deterministic cores passing while reality
> diverged. Pull this forward; do not defer it to per-story checkpoints.

- [X] T003 Build the root-cause map from `specs/203-fix-disclosed-test-reds/readiness/baseline-before.md`: tabulate every red project, the exact failing test names, and which user story (US1–US4) owns each, into `specs/203-fix-disclosed-test-reds/readiness/root-cause-map.md`
- [X] T004 **Early live smoke run — confirm the live numbers** that the plan left open (research.md "open items"): run the three sample suites and Package.Tests directly and record into `specs/203-fix-disclosed-test-reds/readiness/root-cause-map.md`: (a) the **true current `Catalog.supportedControls` count** (hypothesis 97); (b) the **identity of the new/97th control** (the one in `Unreferenced` / `MissingContractOrReason`); (c) the **ControlsGallery 52-vs-full intent** (does `ControlsGallery.Core` `CoverageMap.catalogIds()` still resolve to a curated subset after a trial pin bump, or track the full set?); (d) the **exact flaky `SkiaViewer.Tests` names** and confirm each flake is environment-bound, NOT a hidden defect (Constitution VI)
- [X] T005 [P] Confirm the canonical mechanisms are runnable before relying on them: dry-run `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --help` (or inspect its argument surface) and confirm `tests/SkiaViewer.Tests/Audit_ReplayCache.fs` `rasterAvailable`/`tierSkip` idiom is present to mirror; note any deviation in the root-cause map

**Checkpoint**: Root-cause map written, the four live numbers/identities confirmed, mechanisms verified
runnable — user-story fixes may now begin.

---

## Phase 3: User Story 1 — Sample package pins & the package-feed gate are coherent (Priority: P1) 🎯 MVP

**Goal**: Every `FS.GG.UI.*` pin in every sample equals the source-controlled version, resolves from
the local feed, and the *Feature163* gate (extended to all four samples) plus per-sample pin checks pass.

**Independent Test**: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature163` and
each sample test project — zero "pin does not match source-controlled version" failures. (Contract C2.)

### Implementation for User Story 1

- [X] T006 [US1] Refresh the local feed and rewrite every sample pin coherently: `dotnet fsi scripts/refresh-local-feed-and-samples.fsx --mode refresh --pack --sample samples/AntShowcase --sample samples/SampleApps --sample samples/SecondAntShowcase --sample samples/ControlsGallery` (packs current `src/**` sources into `~/.local/share/nuget-local/` and rewrites ~64 stale `FS.GG.UI.*` pins forward; never roll a source `<Version>` back)
- [X] T007 [US1] Verify the rewrite is coherent: confirm every `<PackageReference Include="FS.GG.UI.*">` `Version` in `samples/AntShowcase/**`, `samples/SampleApps/**`, `samples/SecondAntShowcase/**`, `samples/ControlsGallery/**` `.fsproj` equals the matching `src/**/*.fsproj` `<Version>` oracle, and that the local feed contains each pinned `.nupkg`
- [X] T008 [US1] Extend the *Feature163* gate to enforce all four samples in `tests/Package.Tests/Feature163PackageFeedValidationTests.fs` (today it covers only AntShowcase) so pin coherence for SampleApps, SecondAntShowcase, and ControlsGallery is guarded going forward — additive coverage, no assertion weakened
- [X] T009 [US1] Run the gate and confirm green: `dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature163` → `package-feed status: passed`, zero pin-mismatch failures; also run `dotnet test samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` (its suite is otherwise only swept at T025) to confirm the SampleApps pin bump introduced no regression

**Checkpoint**: Feature163 + per-sample pin checks green; the Package.Tests pin-debt portion of SC-002
cleared. MVP is independently demonstrable.

---

## Phase 4: User Story 2 — The design-system validation gate self-provisions a present, current report (Priority: P1)

**Goal**: The *Feature128* gate (GV-1…GV-7) produces its report from the env-free **verdict-core** path
in its own setup, so a fresh checkout (gitignored `readiness/` absent) is **not red by default**; the
ANT record reports `overall=PASS`. The heavy live scaffold+build stays opt-in.

**Independent Test**: `rm -rf specs/128-design-system-template-param/readiness` then
`dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature128` → GV-1…GV-7 pass, no "report
missing". (Contract C3.)

### Implementation for User Story 2

- [X] T010 [US2] Identify/expose the env-free verdict-core entry point in `scripts/validate-design-system-template.fsx` (the phase that enumerates `designSystem` choices, evaluates the pairing catalog vs the committed oracles `docs/reports/color-policy-{wcag,ant}.md`, and emits the exact GV-1…GV-7 tokens — no `dotnet new`, build, GL, or network) so it can be invoked from a test fixture
- [X] T011 [US2] Make the gate self-provisioning in `tests/Package.Tests/Feature128DesignSystemTemplateTests.fs`: in one-time fixture setup, if `specs/128-design-system-template-param/readiness/design-system-template-validation.md` is absent, generate it via the verdict-core path from T010 before GV-1…GV-7 evaluate (do NOT commit the report — `readiness/` is gitignored by policy; do NOT weaken any GV assertion)
- [X] T012 [US2] Verify clean-state self-provisioning: `rm -rf specs/128-design-system-template-param/readiness && dotnet test tests/Package.Tests/Package.Tests.fsproj --filter Feature128` → all seven GV gates pass, ANT `overall=PASS`, not red-by-default; confirm the opt-in live path still works with `FS_GG_RUN_DESIGN_SYSTEM_VALIDATION=1`

**Checkpoint**: Both P1 stories done → `tests/Package.Tests` is 100% green (SC-002), the release-surface
gate restored, no assertion weakened.

---

## Phase 5: User Story 3 — Sample internal assertions match current reality (Priority: P2)

**Goal**: Drifted count/bijection/contract assertions in the sample suites are corrected to their
**true current values** (confirmed in T004) and still verify a real property — never loosened (`=`
stays `=`, not `>`/`>=`), broadened, or deleted (FR-003, Constitution V). The 97th control is genuinely
placed and classified, not counted away.

**Independent Test**: `dotnet test` for `AntShowcase.Tests`, `ControlsGallery.Tests`,
`SecondAntShowcase.Tests` → 100% pass; the diff still uses `Expect.equal` / `Set.equal`; `Unreferenced`
and `MissingContractOrReason` are genuinely empty. (Contract C4.)

> **Depends on US1** (the pin bump is what surfaces the true catalog) and on the T004 confirmed values.

### Implementation for User Story 3

- [X] T013 [P] [US3] Correct the catalog count assertion in `samples/AntShowcase/AntShowcase.Tests/CoverageTests.fs` (the `Expect.equal (List.length catalog) 96` assertion — locate by content; data-model cites ~L22, but confirm post-pin-bump) from `96` to the true value confirmed in T004 (keep `Expect.equal`)
- [X] T014 [US3] Assign the 97th control to a page in the AntShowcase `*.Core` page-assignment source so `result.Unreferenced` is genuinely empty (a real bijection, not a loosened check)
- [X] T015 [P] [US3] Correct the catalog count assertion in `samples/SecondAntShowcase/SecondAntShowcase.Tests/CoverageTests.fs` (the `96` count assertion — locate by content; data-model cites ~L22) to the true value (keep `Expect.equal`)
- [X] T016 [US3] In SecondAntShowcase, place the 97th control on a page AND give it an interaction contract or an explicit display-only reason in the `*.Core` classification source so `Unreferenced` and `MissingContractOrReason` are genuinely empty — satisfying `Feature172CoverageRegressionTests.fs`, `Feature173LiveResponsivenessRegressionTests.fs`, and `InteractionTests.fs` in `samples/SecondAntShowcase/SecondAntShowcase.Tests/`
- [X] T017 [US3] Resolve ControlsGallery per the T004-confirmed intent in `samples/ControlsGallery/ControlsGallery.Tests/CoverageTests.fs` (the `52` subset assertions — locate by content; data-model cites ~L20,22,23) and `ControlsGallery.Core`'s `CoverageMap.catalogIds()`: if the curated 52-control/10-page subset is the intent, ensure `catalogIds()` returns exactly that subset and the bijection holds at 52; if it legitimately tracks the full set, set counts to the true value and assign every control — either way set the assertion to the **real** value with `Set.equal`/`Expect.equal` intact (never `>`/`>=`, never deleted)
- [X] T018 [US3] Run the three sample suites and confirm 100% pass: `dotnet test samples/AntShowcase/AntShowcase.Tests/AntShowcase.Tests.fsproj`, `.../ControlsGallery/ControlsGallery.Tests/ControlsGallery.Tests.fsproj`, `.../SecondAntShowcase/SecondAntShowcase.Tests/SecondAntShowcase.Tests.fsproj`; verify via diff that no assertion was weakened or removed (SC-003)

**Checkpoint**: All three previously-red sample suites green with assertions intact.

---

## Phase 6: User Story 4 — The comprehensive baseline is deterministic (no flaky GL failures) (Priority: P3)

**Goal**: `tests/SkiaViewer.Tests` yields an identical pass set across repeated runs. Each
GL/window-system-sensitive test either passes (capability present) or is **deterministically
skipped-with-rationale** (capability absent, Constitution VI) — never an intermittent red, and a
genuine defect inside an available context still fails loudly.

**Independent Test**: run `tests/SkiaViewer.Tests` 5 consecutive times → identical pass set, 0 flips;
skip count stated. (Contract C5.)

> **Idiom to mirror**: the canonical `rasterAvailable` probe + `tierSkip`/`skiptest` in
> `tests/SkiaViewer.Tests/Audit_ReplayCache.fs` (reference only — do not modify it).

### Implementation for User Story 4

- [X] T019 [P] [US4] Guard the live `runApp`/persistent-run cases in `tests/SkiaViewer.Tests/Tests.fs`: probe the native-window/GL-context capability before calling `Viewer.runApp`/`Viewer.run`, and on absence emit a deterministic `skiptest` citing Constitution VI (do not swallow failures; do not always-skip)
- [X] T020 [P] [US4] Add the `rasterAvailable` probe before `Viewer.captureScreenshotEvidence` → `SKSurface.Create` in `tests/SkiaViewer.Tests/Feature063RendererTests.fs`, skipping with a Constitution-VI rationale when absent; run full assertions unchanged when present
- [X] T021 [P] [US4] Same `rasterAvailable` guard in `tests/SkiaViewer.Tests/Feature086SceneTranslateTests.fs`
- [X] T022 [P] [US4] Same `rasterAvailable` guard in `tests/SkiaViewer.Tests/Feature136TextRenderingTests.fs`
- [X] T023 [P] [US4] Same `rasterAvailable` guard in `tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs`
- [X] T024 [US4] Prove determinism: run `tests/SkiaViewer.Tests` 5 consecutive times capturing each to `specs/203-fix-disclosed-test-reds/readiness/skiaviewer-run-{1..5}.md`, confirm the pass set is identical across all five (0 pass↔fail flips) and record the explicit skip count (SC-004, SC-005)

**Checkpoint**: SkiaViewer suite deterministic; any residue is an explicit bounded skip.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Prove the whole-baseline contract and retire the feature-202 disclosures.

- [X] T025 Whole-baseline green + determinism: run `scripts/baseline-tests.fsx` twice into `specs/203-fix-disclosed-test-reds/readiness/baseline-after.md` and `baseline-after-2.md` and `diff` the PASS/FAIL/SKIP lines — 0 red projects (SC-001), identical across runs, any residue an explicit skip with stated count (SC-005)
- [X] T026 [P] Confirm no regression: verify the public-surface / governance / evidence gates remain green and no previously-passing test flipped red; confirm no `.fsi` or surface-baseline changes were made (Tier 2, FR-007, contract C6)
- [X] T027 Retire the disclosures: re-check the feature-202 readiness ledger (`specs/202-fix-build-fsx-engine/readiness/quickstart-evidence.md`) pre-existing-red + flaky items and record in `specs/203-fix-disclosed-test-reds/readiness/disclosures-obsolete.md` that each is now resolved or reduced to a bounded explicit skip (FR-008, SC-006)
- [X] T028 Run the quickstart end-to-end (`specs/203-fix-disclosed-test-reds/quickstart.md` Steps 0–5) as the final acceptance pass and confirm SC-001…SC-006

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup; **BLOCKS all user stories** (the live numbers from T004
  feed US3 directly).
- **US1 (Phase 3, P1)**: after Foundational. No dependency on other stories.
- **US2 (Phase 4, P1)**: after Foundational. Independent of US1 (different files) — can run in parallel
  with US1.
- **US3 (Phase 5, P2)**: after Foundational **and US1** (the pin bump surfaces the true catalog) and the
  T004 confirmed values.
- **US4 (Phase 6, P3)**: after Foundational. Independent of US1–US3 (SkiaViewer-only files).
- **Polish (Phase 7)**: after all desired user stories complete.

### Within Each User Story

- US1: refresh (T006) → verify (T007) → extend gate (T008) → confirm (T009), sequential.
- US2: expose verdict-core (T010) → wire fixture (T011) → confirm (T012), sequential.
- US3: count edits (T013, T015) parallel; page/classification placement (T014, T016, T017) follow; then
  confirm (T018).
- US4: each per-file guard (T019–T023) parallel; then 5× determinism run (T024).

### Parallel Opportunities

- T005 in Foundational is [P].
- **US1 and US2 (both P1) can run fully in parallel** — disjoint files (`samples/**` + Feature163 vs
  Feature128 + the validate script).
- **US4 can run in parallel with US1/US2/US3** — SkiaViewer files are disjoint from all others.
- Within US3, T013 and T015 are [P]; within US4, T019–T023 are all [P].

---

## Parallel Example: the two P1 stories + US4 together

```bash
# After Foundational completes, three independent tracks:
# Track A (US1): refresh pins + extend Feature163 gate
dotnet fsi scripts/refresh-local-feed-and-samples.fsx --mode refresh --pack \
  --sample samples/AntShowcase --sample samples/SampleApps \
  --sample samples/SecondAntShowcase --sample samples/ControlsGallery
# Track B (US2): expose verdict-core + self-provision Feature128 fixture
# Track C (US4): add rasterAvailable guards across the five SkiaViewer files
```

```bash
# Within US4, the per-file raster guards are independent:
Task: "rasterAvailable guard in tests/SkiaViewer.Tests/Feature063RendererTests.fs"
Task: "rasterAvailable guard in tests/SkiaViewer.Tests/Feature086SceneTranslateTests.fs"
Task: "rasterAvailable guard in tests/SkiaViewer.Tests/Feature136TextRenderingTests.fs"
Task: "rasterAvailable guard in tests/SkiaViewer.Tests/Feature140GlyphRunRenderingTests.fs"
```

---

## Implementation Strategy

### MVP First (the two P1 stories)

1. Phase 1 Setup → Phase 2 Foundational (CRITICAL — the **early live smoke run** T004 confirms the
   numbers before any fix).
2. Phase 3 (US1) and Phase 4 (US2) — both P1, parallel. **STOP and VALIDATE**: `tests/Package.Tests` is
   now 100% green (SC-002). This is the highest signal-to-effort slice and a self-contained MVP.

### Incremental Delivery

1. Setup + Foundational → confirmed before-state.
2. US1 + US2 → Package.Tests green (MVP).
3. US3 → three sample suites green with assertions intact.
4. US4 → SkiaViewer deterministic.
5. Polish → whole-baseline 0-red + determinism proven, feature-202 disclosures retired.

---

## Notes

- This is a **Tier 2 (internal/corrective)** change — no public `.fs`/`.fsi` surface, no new deps, no
  inter-package contract changes. Only sample `.fsproj` pins, sample `*.Core`/`*.Tests` fixtures, two
  `Package.Tests` gates, and `SkiaViewer.Tests` GL guards are touched.
- **Never weaken an assertion to green a build** (Constitution V): correct to the true value, keep
  `=`/`Set.equal`. **Never mask a real GL defect as "unsupported"** (Constitution VI): the probe gates
  only the environment-sensitive path.
- `specs/203-fix-disclosed-test-reds/readiness/` is gitignored/transient — safe for all evidence
  artifacts.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
