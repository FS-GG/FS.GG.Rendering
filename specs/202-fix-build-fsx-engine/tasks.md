---
description: "Task list for feature 202 — Fix the generated build.fsx governance-engine resolution"
---

# Tasks: Fix the generated build.fsx governance-engine resolution

**Input**: Design documents from `/specs/202-fix-build-fsx-engine/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: INCLUDED — the constitution (Principle I: Spec → FSI → Semantic Tests → Implementation; Principle II: visibility in `.fsi` + surface baseline) makes tests mandatory for the new packable `FS.GG.UI.Build` engine. Engine semantic tests and the surface-area baseline are first-class tasks, not optional.

**Organization**: Tasks are grouped by user story (US1 P1 → US2 P2 → US3 P3) so each can be implemented and verified independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Every task names exact file path(s)

## Path Conventions

- New engine producer: `src/Build/` (auto-discovered by the pack harness: `FS.GG.UI.*` prefix + `IsPackable`)
- Engine semantic tests: `tests/Build.Tests/` (repo convention: `tests/<Name>.Tests`)
- Surface baseline: `readiness/surface-baselines/FS.GG.UI.Build.txt`
- Template under fix: `template/base/build.fsx`, `template/base/tests/Product.Tests/GovernanceTests.fs`
- Solution: `FS.GG.Rendering.slnx`
- Per-feature evidence: `specs/202-fix-build-fsx-engine/readiness/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project scaffolding and the no-regression baseline.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline task MUST run **every** test
> project via the discovery-based runner so pre-existing reds are known up front and not mistaken for
> regressions at merge. `dotnet test FS.GG.Rendering.slnx` deliberately omits `tests/Package.Tests`
> (release-only, owns the public-surface gate) and `samples/**/*.Tests` — exactly where stale surface
> baselines / pins hide. Use `scripts/baseline-tests.fsx`, which globs `*.Tests.fsproj`.

- [X] T001 Create the producer project scaffold `src/Build/FS.GG.UI.Build.fsproj` (`PackageId=FS.GG.UI.Build`, `AssemblyName=FS.GG.UI.Build`, `IsPackable=true`, `OutputType=Library`, `TargetFramework=net10.0`, `Version` overridable by the coherent `-p:Version` pack; dependency-minimal per plan) and register it in `FS.GG.Rendering.slnx`
- [X] T002 [P] Create the engine semantic-test project scaffold `tests/Build.Tests/Build.Tests.fsproj` (net10.0, references `src/Build/FS.GG.UI.Build.fsproj`, repo test-runner convention) and register it in `FS.GG.Rendering.slnx`
- [X] T003 [P] Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/202-fix-build-fsx-engine/readiness/baseline.md` (runs EVERY test project — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm the root cause against the live gate, stand up a minimal-but-resolvable engine, and lock the seams before the real audit logic is written.

**⚠️ CRITICAL**: No user-story work may begin until this phase is complete.

> **⚠️ Early live smoke run (STANDING, do not omit).** The plan's root-cause hypotheses (no producer +
> stale cache path) and — critically — *which `readiness/**` artifacts actually exist at gate time* are
> **unverified assumptions until the gate is run**. After the root-cause map (T004) and a minimal
> compiling engine (T005) plus the one-line cache-path fix (T006), drive the real generated product
> through `dotnet fsi build.fsx target EvidenceGraph` (T007) and OBSERVE the available evidence surface
> BEFORE building any audit rules on it. Do not build the EvidenceGraph/EvidenceAudit logic on the
> assumption that the artifacts are present (research.md "Open runtime question").

- [X] T004 Confirm the root-cause map (research.md R0–R4) against repo HEAD: verify the stale probe literal at `template/base/build.fsx:~126` (`fs.skia.ui.build`), confirm no `FS.GG.UI.Build` producer/`GeneratedRunner` exists anywhere, and confirm the package is absent from `~/.local/share/nuget-local/` and `~/.nuget/packages`; record findings in `specs/202-fix-build-fsx-engine/readiness/root-cause-confirmation.md`
- [X] T005 Implement a MINIMAL compiling engine in `src/Build/Evidence.fs` exposing `FS.GG.UI.Build.Evidence.GeneratedRunner.run : target:string -> dir:string -> int` (a stub that writes a placeholder `readiness/evidence-graph.md`/`evidence-audit.md` and returns 0) — just enough to prove the reflected resolve/load/invoke path; honors the engine-invocation-contract symbol shape (`GeneratedRunner` + static `run`)
- [X] T006 Apply the cache-path correction in `template/base/build.fsx` (line ~126): `fs.skia.ui.build` → `fs.gg.ui.build` (blocks BOTH US1 resolution and US2 no-pre-rebrand; the strengthened scan + full FR-002 verification live in US2)
- [X] T007 **Early live smoke run**: pack a COHERENT feed at a fresh `V` (`dotnet pack FS.GG.Rendering.slnx -c Release -p:Version=$V -o ~/.local/share/nuget-local`; confirm `FS.GG.UI.Build.$V.nupkg` now exists), set `template/base/Directory.Packages.props` `FsSkiaUiVersion=$V`, generate the `governed` profile, restore, run `dotnet fsi build.fsx target EvidenceGraph`, and RECORD which `readiness/**` artifacts exist at gate time into `specs/202-fix-build-fsx-engine/readiness/smoke-evidence.md` (live, or `environment-limited` with disclosed substitute per Feature-168 evidence rules)
- [X] T008 Draft the curated public surface `src/Build/Evidence.fsi` (namespace `FS.GG.UI.Build.Evidence`; `GeneratedRunner.run` plus the evidence-node / audit-verdict types per data-model.md §3–4) against the contracts and the T007-observed surface; no `private/internal/public` modifiers in `.fs`

**Checkpoint**: Resolve/load/invoke path proven on a live `EvidenceGraph` run, real evidence surface observed, `.fsi` seam locked — user-story implementation can begin.

---

## Phase 3: User Story 1 — Verify gate runs the governance evidence gates green (Priority: P1) 🎯 MVP

**Goal**: A freshly scaffolded product (gate-including profile) restores and runs the full `Verify` gate; EvidenceGraph and EvidenceAudit execute against the resolved engine, produce real `readiness/evidence-graph.md` + `readiness/evidence-audit.md` (not log-only stubs), and the gate exits 0. Gate-less profiles still pass without the engine.

**Independent Test**: In a freshly generated `governed`/`headless-scene` product, after restore run `dotnet fsi build.fsx target Verify`; confirm the evidence + audit steps run (produce evidence output) and the gate exits 0; confirm `app`/`sample-pack` pass `Verify` without the engine.

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL before implementation)

- [X] T009 [P] [US1] Surface-area baseline for the engine: create `readiness/surface-baselines/FS.GG.UI.Build.txt` from the curated `Evidence.fsi` and wire it into the repo surface-drift check (Principle II)
- [X] T010 [P] [US1] Engine semantic tests in `tests/Build.Tests/` exercising `GeneratedRunner.run "EvidenceGraph" dir` and `"EvidenceAudit" dir` against a fixture `readiness/` tree (pass case): assert `readiness/evidence-graph.md` is real synthesized content and `readiness/evidence-audit.md` contains a `verdict=PASS` token and `run` returns 0

### Implementation for User Story 1

- [X] T011 [US1] Implement the EvidenceGraph target in `src/Build/Evidence.fs`: sense the generated product's `readiness/**` surface (headless: `layout-evidence.txt`, `headless-scene-evidence.txt`; interactive adds launch/image/screenshot/pixel-readback/window-diagnostics/window-options/bounded-smoke per evidence-output-contract.md), build the validation graph, write a real `readiness/evidence-graph.md` (parent-dir creation), return 0 on a well-formed available surface
- [X] T012 [US1] Implement the EvidenceAudit target in `src/Build/Evidence.fs`: audit the graph against the `template/base/docs/evidence-formats.md` token contract, write `readiness/evidence-audit.md` with the required `verdict` token (`verdict=PASS` ⇒ return 0), honoring the evidence-output-contract.md shapes
- [X] T013 [US1] Finalize `src/Build/Evidence.fs` against `src/Build/Evidence.fsi` so the engine compiles cleanly and T009/T010 pass; remove the T005 stub behavior
- [X] T014 [US1] Re-pack the coherent feed at `$V` and run the full per-profile end-to-end validation (quickstart Step 4): for `/tmp/p-governed`, `/tmp/p-headless`, `/tmp/p-app`, `/tmp/p-sample` → generate, restore, `dotnet fsi build.fsx target Verify`; capture results to `specs/202-fix-build-fsx-engine/readiness/verify-evidence.md` (governed/headless: gates execute + `evidence-*.md` exist + exit 0; app/sample-pack: exit 0 without the engine — SC-001, FR-008). **Public-feed (nuget.org / published-consumer) path is environment-limited:** in-repo validation exercises only the local `~/.local/share/nuget-local` feed; the spec edge case "local development feed vs public feed — both must resolve" (engine-invocation-contract.md "default NuGet config ⇒ … nuget.org for a published consumer") cannot be exercised here without publishing. Record this as a disclosed `environment-limited` substitution per Feature-168 evidence rules and reason about the public path from the shared `AssemblyResolve`/restore mechanism rather than asserting it live

**Checkpoint**: `Verify` is green end-to-end for gate-including profiles with real evidence output; MVP deliverable.

---

## Phase 4: User Story 2 — Engine resolves in lock-step with the single version pin (Priority: P2)

**Goal**: The generated build resolves the engine from the single `FsSkiaUiVersion` source of truth at the current (post-rebrand) identity; no second version literal and no `fs.skia.ui.build`/`FS.Skia.UI` identifier or cache path remains; one version edit + restore moves libraries and engine together.

**Independent Test**: Inspect a generated product — confirm the engine resolves from the single pin, no pre-rebrand identifier/cache path remains in `build.fsx`, and changing `FsSkiaUiVersion` + restore moves both libraries and the gate engine.

### Tests for User Story 2 ⚠️

- [X] T015 [P] [US2] Strengthen `template/base/tests/Product.Tests/GovernanceTests.fs`: add a scan asserting the generated `build.fsx` contains NO pre-rebrand identifier (`fs.skia.ui.build` cache path or `FS.Skia.UI` package name), added without breaking existing assertions (contracts §"Optional strengthening", FR-002)

### Implementation for User Story 2

- [X] T016 [US2] Verify the single-pin contract holds: confirm `template/base/Directory.Packages.props` carries exactly one `FsSkiaUiVersion` literal and the `FS.GG.UI.Build` `PackageVersion` reads `Version="$(FsSkiaUiVersion)"` (no second version value introduced — FR-004); document in `verify-evidence.md`
- [X] T017 [US2] Execute the no-pre-rebrand + lock-step checks (quickstart Step 5) against a generated product: `! grep -Eri "fs\.skia\.ui\.build|FS\.Skia\.UI" build.fsx`, then change `FsSkiaUiVersion` to a new packed `V2` + restore and confirm the resolved engine version == `V2` (SC-003/SC-004); record results
- [X] T018 [US2] Confirm existing `GovernanceTests.fs` assertions stay green (in-process engine invocation, `GeneratedRunner`, `Assembly.LoadFrom`, `FsSkiaUiVersion`, no `#r "nuget: FS.Skia.UI.Build,"`, clean text logs, no decommissioned scripts) plus the new T015 scan: `dotnet test tests/Product.Tests/Product.Tests.fsproj` in a generated product (FR-007). **This task also discharges FR-006** — the in-process-invocation (`Assembly.LoadFrom`/`GeneratedRunner`) and no-decommissioned-scripts (`run-audit.sh`/`python3`/`ProcessStartInfo("bash"`) assertions are the gate that proves the gates stay in-process with `dotnet test` as the only retained external process

**Checkpoint**: Single-pin lock-step proven; pre-rebrand identifiers eliminated and guarded by a governance scan.

---

## Phase 5: User Story 3 — Honest, diagnosable failure when the engine is unavailable (Priority: P3)

**Goal**: When the engine cannot be resolved (pinned version not on any feed, offline), the gate fails loudly with a message naming the engine (`FS.GG.UI.Build <version>`) and the feed/path searched — never a silent pass, never mislabelled as a defect in the developer's product.

**Independent Test**: Point a generated product at a version/feed where the engine is absent; run `Verify`; confirm it fails with a message naming the engine identity and the feed/location, and does not report success.

### Tests for User Story 3 ⚠️

- [X] T019 [P] [US3] Engine semantic tests in `tests/Build.Tests/` for the honest-fail path: `GeneratedRunner.run` against a fixture `readiness/` tree with a missing/invalid required-for-profile artifact returns non-0 and writes `readiness/evidence-audit.md` with `verdict=FAIL` plus a reason that distinguishes framework/feed condition from product defect (evidence-output-contract.md "Failure / honesty contract")

### Implementation for User Story 3

- [X] T020 [US3] Confirm `template/base/build.fsx`'s engine-resolution failure path emits a loud diagnostic naming `FS.GG.UI.Build <version>` and the cache path/feed searched before `run` is ever called, and never reports success (engine-invocation-contract.md "Resolution contract"); adjust the message if it does not already name both
- [X] T021 [US3] Live honest-failure validation (quickstart Step 3 tail): point a generated product at an unpacked version/empty feed, run `dotnet fsi build.fsx target EvidenceGraph`, confirm the failure NAMES the engine + feed/path and exit ≠ 0 with no fabricated success; record to `specs/202-fix-build-fsx-engine/readiness/honest-failure-evidence.md` (SC-005)

**Checkpoint**: All three stories independently functional — gate runs green, resolves in lock-step, and fails honestly.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, surface confirmation, and full-suite validation.

- [X] T022 [P] Update `UPGRADING`/`PROVENANCE` docs to record that the `FS.GG.UI.Build` engine is now produced in-repo at `src/Build/` (Tier-1 obligation per plan Constitution Check)
- [X] T023 [P] Confirm the engine surface baseline + drift check (quickstart Step 7): `dotnet build src/Build/FS.GG.UI.Build.fsproj` compiles and `readiness/surface-baselines/FS.GG.UI.Build.txt` matches the curated `.fsi`
- [X] T024 Re-run the comprehensive baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against the Phase-1 baseline to confirm zero net new reds across all test projects (including Package.Tests + samples)
- [X] T025 Run the full `quickstart.md` Steps 1–7 as the acceptance pass and record the SC-001…SC-005 mapping in `specs/202-fix-build-fsx-engine/readiness/quickstart-evidence.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. The early live smoke run (T007) is the gate for all downstream work.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 is the MVP; US2 and US3 build on the same engine + `build.fsx` and are independently testable.
- **Polish (Phase 6)**: Depends on all targeted user stories.

### User Story Dependencies

- **US1 (P1)**: After Foundational. Delivers the working engine + green `Verify` (the feature's whole point).
- **US2 (P2)**: After Foundational. Shares `build.fsx`/`Directory.Packages.props`; the one-line cache fix landed in T006, US2 strengthens the guard and proves lock-step. Independently testable by inspection + re-pin.
- **US3 (P3)**: After Foundational. Exercises the resolution-failure path; independently testable against an empty feed.

### Within Each User Story

- Tests (T009/T010, T015, T019) are written FIRST and FAIL before the implementation tasks they cover.
- Engine `.fsi` (T008) before `.fs` bodies (T011–T013).
- EvidenceGraph (T011) before EvidenceAudit (T012, which audits the graph).
- End-to-end validation (T014, T017, T021) after the code it exercises compiles.

### Parallel Opportunities

- Setup: T002 and T003 run in parallel (after T001 creates the producer project).
- US1 tests T009 and T010 run in parallel (different files).
- Cross-story: once Foundational is done, US2 (T015 scan authoring) and US3 (T019 fixture tests) can be drafted in parallel with US1, since they touch different files — but final validation of each depends on the engine from US1.
- Polish T022 and T023 run in parallel.

---

## Parallel Example: User Story 1

```bash
# After the .fsi seam (T008) is locked, launch the two US1 test tasks together:
Task: "Surface-area baseline readiness/surface-baselines/FS.GG.UI.Build.txt (T009)"
Task: "Engine semantic pass-case tests in tests/Build.Tests/ (T010)"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (producer + test scaffold + baseline).
2. Phase 2: Foundational — **including the early live smoke run (T007)** that proves resolve/load/invoke and reveals the real `readiness/**` surface before any audit rule is written.
3. Phase 3: US1 — implement the engine, re-pack the coherent feed, prove `Verify` green per profile.
4. **STOP and VALIDATE**: `Verify` exits 0 for governed/headless with real `evidence-*.md`; app/sample-pack pass without the engine.

### Incremental Delivery

1. Setup + Foundational → engine resolves and loads (stub), surface observed.
2. US1 → `Verify` green end-to-end (MVP).
3. US2 → single-pin lock-step proven, pre-rebrand identifiers eliminated + guarded.
4. US3 → honest, named failure when the engine is absent.
5. Polish → docs, surface baseline, full-suite no-regression.

---

## Notes

- [P] = different files, no incomplete dependencies.
- The cache-path fix (T006) is intentionally in Foundational because it blocks both US1 resolution and US2's no-pre-rebrand requirement; US2 strengthens it with a governance scan and lock-step proof.
- All validation is against a **generated** product (per profile), never `template/base` in place (memory `template-feed-version-model`); pack the COHERENT feed with `-p:Version=$V` at a fresh `V` and set `FsSkiaUiVersion=$V`.
- Commit after each task or logical group; stop at any checkpoint to validate a story independently.
