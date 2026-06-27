---
description: "Task list for Feature 206 — Publish FS.GG.UI.Template & tag the coherent set"
---

# Tasks: Publish FS.GG.UI.Template Carrying the Lifecycle Parameter & Tag the Coherent Set

**Input**: Design documents from `/specs/206-publish-template-coherent-set/`

**Prerequisites**: plan.md ✓, spec.md ✓, research.md ✓, data-model.md ✓, contracts/ ✓ (coherent-set, publish-verification, cross-repo-resolution), quickstart.md ✓

**Tests**: This feature adds no new F# surface. The "tests" are the **existing** template suites
(`Feature204LifecycleTemplateTests`, `Feature205TemplateSideEffectTests`) and the live validators
(`scripts/validate-lifecycle-template.fsx`, `scripts/baseline-tests.fsx`) run **against the installed
published package**, not the working tree. No new TDD tests are authored.

**Organization**: Tasks are grouped by user story. Note this is a **release runbook with hard
ordering** (PUBLISHED → TAGGED → COHERENT, per data-model.md): US2 must not start until every PV gate
(US1) is green, and US3 must not start until the tag is pushed (US2). The stories are independently
*testable* but not independently *runnable in parallel* — the partial-failure rule (FR-010) requires
the sequence.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files/artifacts, no dependencies on incomplete tasks)
- **[Story]**: US1 / US2 / US3 (Setup & Foundational & Polish carry no story label)
- Exact paths/commands included per task

## Resolved identifiers (research.md R1–R2)

- Published template version: **`0.1.50-preview.1`** (strictly > `0.1.17-preview.1`)
- Coherent-set tag: **`fs-gg-ui-template/v0.1.50-preview.1`** (annotated, template-scoped)
- Dependent cross-repo request: **`FS-GG/FS.GG.SDD#1`** (open)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Evidence scaffolding and a comprehensive non-regression baseline before any artifact changes.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project and record the
> full red/green set so pre-existing reds are known up front and not mistaken for regressions at the
> end. Use the discovery-based runner (it globs `*.Tests.fsproj`, including `tests/Package.Tests` and
> `samples/**/*.Tests` that the `.slnx` omits) — exactly where Feature 175's surprises hid.

- [X] T001 Create the evidence directory `specs/206-publish-template-coherent-set/readiness/` (will hold `baseline.md`, `publish-evidence.md`, `profile-*.md`, `reproducibility.md`, `cross-repo-resolution.md` per plan.md Project Structure)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/206-publish-template-coherent-set/readiness/baseline.md` (runs EVERY test project — solution + `tests/Package.Tests` + `samples/**`; record the full red/green set; disclose any pre-existing reds here, not at merge)
- [X] T003 [P] Confirm release preconditions and record them in `readiness/baseline.md`: `gh auth status` (authenticated as `EHotwagner`), framework set present (`ls ~/.local/share/nuget-local/ | grep 0.1.50-preview.1` shows the `FS.GG.UI.*` packages from feature 204), and the current published template is `0.1.17-preview.1` only

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Confirm collision-free identifiers and prove the publish/install/scaffold path itself
works **before** any gate is claimed. **No user-story work begins until this phase is complete.**

> **⚠️ Early live smoke run (STANDING, do not omit).** This feature changes no F# runtime, but
> **the publish→install→scaffold path is the "app" here** (plan.md Standing Assumption). After the
> precondition/collision map is built and BEFORE the US1 verification gates are claimed, include one
> task that **packs to the feed and instantiates one profile from the _installed_ package** (not the
> working tree). Working-tree green does NOT prove the published package is correct. Treat the plan's
> "the packed artifact carries 204/205 surfaces" as an **unverified hypothesis until the package has
> been installed and run**.

- [X] T004 Build the collision / precondition map in `readiness/publish-evidence.md`: assert the target version is unused (`test ! -f ~/.local/share/nuget-local/FS.GG.UI.Template.0.1.50-preview.1.nupkg`) and the target tag is unused (`git tag --list | grep -x fs-gg-ui-template/v0.1.50-preview.1` is empty); on collision, STOP and select the next unused identifier per `contracts/coherent-set.md` collision rules (FR-002)
- [X] T005 Bump the template package version (the **only** in-repo file edit): `.template.package/FS.GG.UI.Template.fsproj` `<Version>0.1.17-preview.1</Version>` → `<Version>0.1.50-preview.1</Version>`; verify with `grep '<Version>' .template.package/FS.GG.UI.Template.fsproj` (R1, FR-001)
- [X] T006 **Early live smoke run (publish/install/scaffold path is the app)**: pack with `dotnet pack .template.package/FS.GG.UI.Template.fsproj -c Release -o ~/.local/share/nuget-local`, then `dotnet new install FS.GG.UI.Template::0.1.50-preview.1` and instantiate one profile from the **installed** package (`dotnet new fs-gg-ui --profile headless-scene -o /tmp/fsgg-smoke`); confirm the template instantiates from the package and capture live evidence in `readiness/publish-evidence.md` BEFORE claiming any PV gate. On failure: STOP, do not tag, do not reconcile, leave the record in-progress (FR-010)

**Checkpoint**: Identifiers confirmed collision-free, version bumped, package proven installable and
instantiable from the feed — the US1 verification gates can now be claimed against the installed package.

---

## Phase 3: User Story 1 - Consumer installs the published template & scaffolds with the new lifecycle behavior (Priority: P1) 🎯 MVP

**Goal**: A `FS.GG.UI.Template 0.1.50-preview.1` package is on the feed; installing it by id resolves
the new version, exposes the `lifecycle` + `initGit` surface, reproduces the `spec-kit` baseline
byte-for-byte, and scaffolds side-effect-free by default. This is the board-item deliverable (state
**PUBLISHED**).

**Independent Test**: From an empty cache, `dotnet new install FS.GG.UI.Template::0.1.50-preview.1`,
then `dotnet new fs-gg-ui` per lifecycle value with no git flag: resolved version > `0.1.17-preview.1`,
manifest carries `lifecycle`/`initGit` (no `skipGitInit`), `spec-kit` default is byte-identical, and
no `.git`/no spawned process appears.

> All gates run against the **installed** package (PV-1..PV-5, `contracts/publish-verification.md`).
> A red gate blocks the release and leaves the record in-progress (FR-010).

- [X] T007 [US1] **PV-1 (resolvable new version)**: confirm `dotnet new fs-gg-ui --help` lists the template from the installed `0.1.50-preview.1` and the resolved version is strictly newer than `0.1.17-preview.1`; record in `readiness/publish-evidence.md` (this file is the rollup; the `[P]` gates below write sibling files to avoid write contention) (FR-002, FR-004, SC-001)
- [X] T008 [P] [US1] **PV-2 (manifest surfaces present)**: run `dotnet test tests/Package.Tests --filter Feature204LifecycleTemplateTests` and `dotnet test tests/Package.Tests --filter Feature205TemplateSideEffectTests` (GV-1, GV-2) to assert the packaged `template.json` exposes the `lifecycle` choice symbol and `initGit` opt-in and that `skipGitInit` is absent; record pass in `readiness/pv2-manifest.md` (FR-001, SC-001)
- [X] T009 [US1] **PV-3 (byte-identical default)**: run `FS_GG_RUN_LIFECYCLE_VALIDATION=1 dotnet fsi scripts/validate-lifecycle-template.fsx --emit-report`; confirm `lifecycle=spec-kit` output is byte-identical to the prior published baseline for **every** profile (`app`, `headless-scene`, `governed`, `sample-pack`) with zero diffs; on any diff STOP (block publish, do not tag) and record the regression in `readiness/publish-evidence.md` (this is the blocking gate, written to the rollup) (FR-005, SC-002)
- [X] T010 [P] [US1] **PV-4 (side-effect-free default)**: instantiate each profile from the installed package with **no** git flag in a headless context (`dotnet new fs-gg-ui --profile <p> -o /tmp/fsgg-sef-<p>`); assert `test ! -d /tmp/fsgg-sef-<p>/.git`, zero spawned processes, prompt return; cross-check `Feature205TemplateSideEffectTests` GV-3..GV-6; record in `readiness/pv4-side-effect.md` (FR-006, SC-003)
- [X] T011 [P] [US1] **PV-5 (lifecycle variants emit correctly)**: instantiate per lifecycle value — `spec-kit` emits the Spec-Kit-present set (`.specify/`, constitution, agent-context tree present); `sdd` and `none` each emit the Spec-Kit-absent set; confirm presence/absence and reject unknown values; record in `readiness/pv5-lifecycle.md` (FR-004, US1 AS-4)

**Checkpoint (state PUBLISHED)**: PV-1..PV-5 all green against the installed package — the template is
installable with the new behavior. US1 is independently demonstrable here. Do NOT proceed to US2 until
all five gates are green (FR-010).

---

## Phase 4: User Story 2 - A reproducible, named coherent snapshot exists (Priority: P2)

**Goal**: An annotated tag binds the published template version to the framework set it scaffolds
against, and a from-tag rebuild reproduces the package set; all four profiles restore+build from the
tag against one framework version, reproducibly (state **TAGGED**).

**Independent Test**: From the tagged snapshot, install the recorded version and scaffold each profile;
restore/build succeeds against the recorded `FS.GG.UI.* 0.1.50-preview.1` set with zero NU1101 / zero
version-conflict; two installs at different times resolve identically.

> Gated on US1: do **not** tag until PV-1..PV-5 are green (data-model.md hard ordering).

- [X] T012 [US2] **PV-6a (profiles restore & build from the package, pre-tag)**: for each of `app`, `headless-scene`, `governed`, `sample-pack` — `dotnet new fs-gg-ui --profile <p> -o /tmp/fsgg-<p>` → `dotnet restore` → `dotnet build`; assert zero missing-package (NU1101) and zero version-conflict errors against one consistent `FS.GG.UI.* 0.1.50-preview.1`; record per-profile evidence in `readiness/profile-<p>.md` (FR-009, SC-004)
- [X] T013 [P] [US2] **Reproducibility (double-restore)**: restore one scaffolded profile into two clean caches (`dotnet restore /tmp/fsgg-app --packages /tmp/cacheA`; `… --packages /tmp/cacheB`) and diff the resulting `packages.lock.json`; confirm identical resolution at two times; record in `readiness/reproducibility.md` (SC-005)
- [X] T014 [US2] **Tag the coherent set** (only after T012–T013 green): `git tag -a fs-gg-ui-template/v0.1.50-preview.1 -m "coherent fs-gg-ui template snapshot: FS.GG.UI.Template 0.1.50-preview.1 over FS.GG.UI.* 0.1.50-preview.1"` then `git push origin fs-gg-ui-template/v0.1.50-preview.1`; never move an existing tag (FR-002, FR-003)
- [X] T015 [US2] **PV-6b (from-tag repack reproduces the package, post-tag)** (FR-009 / invariant I1): from a clean checkout of the tag confirm `grep '<Version>' .template.package/FS.GG.UI.Template.fsproj` shows `0.1.50-preview.1` and a re-`dotnet pack` reproduces `FS.GG.UI.Template.0.1.50-preview.1.nupkg`; confirm version-agreement across package + tag + (pending) registry; record in `readiness/reproducibility.md`

**Checkpoint (state TAGGED)**: The coherent set is named, pushed, and reproducible from the tag. US1+US2
both independently verifiable. Do NOT reconcile the cross-repo record until the tag is pushed (FR-010).

---

## Phase 5: User Story 3 - The cross-repo record agrees with the published reality (Priority: P3)

**Goal**: Registry row + compatibility projection record the coherent release at `0.1.50-preview.1` /
tag `fs-gg-ui-template/v0.1.50-preview.1` and link tracking; the dependent request carries a response
citing the version + tag; the board item moves to Done (state **COHERENT**).

**Independent Test**: Read the `fs-gg-ui-template` registry row and projection — both reference the
published version + tag and are marked coherent; `FS-GG/FS.GG.SDD#1` shows a `## Response` citing the
version + tag — all verifiable without reading code.

> Executed **last**, only after PV-1..PV-6 are green and the tag is pushed (US1+US2 complete). All
> shared cross-repo state changes go through the coordination protocol — never by editing another
> repo's files directly. On any partial failure, the record stays **in-progress / not-yet-coherent**
> (FR-010), recorded in `readiness/cross-repo-resolution.md`.

- [X] T016 [US3] **XR-A (registry row)**: via the coordination protocol, update the `fs-gg-ui-template` row in `FS-GG/.github` `registry/dependencies.yml` — recorded version `0.1.50-preview.1`, coherent state recorded, tag reference `fs-gg-ui-template/v0.1.50-preview.1`, `resolved_by` (publishing commit + tag), `tracking` link to this feature; commit prefix `registry:` (FR-007)
- [X] T017 [US3] **XR-B (compatibility projection)**: update the `fs-gg-ui-template` row in `FS-GG/.github` `docs/registry/compatibility.md` to state the **same** version/tag/coherent state as XR-A (authoritative row and projection must agree — FR-007)
- [X] T018 [US3] **XR-C (dependent request response)**: post a `## Response` on `FS-GG/FS.GG.SDD#1` via `gh issue comment 1 --repo FS-GG/FS.GG.SDD` citing the published `FS.GG.UI.Template 0.1.50-preview.1`, the tag `fs-gg-ui-template/v0.1.50-preview.1`, and the published §5 generation contract the scaffold path fulfils; close if SDD confirms, else leave open with the response (FR-008); record in `readiness/cross-repo-resolution.md`
- [X] T019 [US3] **XR-D (board transition)**: move the P1 Rendering Coordination item "Publish FS.GG.UI.Template carrying the new parameter; tag the coherent set" to **Done** and clear the "blocked by lifecycle symbol" relationship — only once XR-A..XR-C and PV-1..PV-6 hold (FR-011)

**Checkpoint (state COHERENT)**: Package + tag + registry/projection + request response all agree. The
release is coherent and the board item is Done.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final coherence proof and non-regression backstop.

- [X] T020 **Non-regression backstop**: re-run `dotnet fsi scripts/baseline-tests.fsx --out specs/206-publish-template-coherent-set/readiness/baseline-after.md`; confirm no project flipped green→red vs T002 (mirrors 204 T001/T025); disclose any change
- [X] T021 [P] Run the full `quickstart.md` end-to-end (Steps 0–8) and confirm every "Done when" bullet (SC-001..SC-006) is satisfiable from the recorded evidence
- [X] T022 Final FR-010 audit: confirm the cross-repo record reads **coherent** only because all of publish/tag/registry/projection/response agree, and that no intermediate failure left it falsely coherent; summarize the release in `readiness/cross-repo-resolution.md`

---

## Dependencies & Execution Order

### Phase Dependencies (hard release ordering)

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. BLOCKS all user stories. Includes the early live
  smoke run that proves the package is installable before any gate is claimed.
- **User Story 1 (Phase 3, PUBLISHED)**: Depends on Foundational. PV-1..PV-5 against the installed package.
- **User Story 2 (Phase 4, TAGGED)**: Depends on US1 — **must not tag until PV-1..PV-5 are green.**
- **User Story 3 (Phase 5, COHERENT)**: Depends on US2 — **must not reconcile until the tag is pushed.**
- **Polish (Phase 6)**: Depends on US3.

> Unlike a typical code feature, the stories here are a **gated sequence** (FR-010): each is
> independently *testable*, but US2/US3 cannot start before the prior state is reached.

### Within Each User Story

- US1: PV-3 (T009) is the blocking gate — a byte diff stops the release before US2. T008/T010/T011 [P]
  run against the same installed package without ordering between them, each writing its own evidence
  file (`pv2-manifest.md` / `pv4-side-effect.md` / `pv5-lifecycle.md`) so the parallel runs don't
  contend on `publish-evidence.md` (the rollup, owned by T007/T009).
- US2: T012 (PV-6a restore/build) and T013 (reproducibility) before T014 (tag) before T015 (PV-6b from-tag repack).
- US3: XR-A→XR-B→XR-C→XR-D in order; XR-D only after all prior XR and all PV gates hold.

### Parallel Opportunities

- Setup: T003 [P] alongside T002.
- US1: T008, T010, T011 [P] — independent assertions against the one installed package, each writing a distinct evidence file (T009, the blocking byte-diff gate, must pass to proceed to US2).
- US2: T013 [P] reproducibility check alongside the per-profile T012 evidence capture.
- Polish: T021 [P] quickstart replay alongside the T020 backstop.

---

## Parallel Example: User Story 1 (gates against the installed package)

```bash
# After T007 (PV-1) confirms the version resolves, run the independent gate assertions together:
Task: "PV-2 manifest surfaces — Feature204/205 template tests against the packed manifest"
Task: "PV-4 side-effect-free default — instantiate each profile with no git flag, assert no .git"
Task: "PV-5 lifecycle variants — spec-kit present / sdd|none absent file sets"
# PV-3 (byte-identical default, T009) is the blocking gate and must pass before US2.
```

---

## Implementation Strategy

### MVP First (User Story 1 → PUBLISHED)

1. Phase 1 Setup (baseline + preconditions).
2. Phase 2 Foundational — including the **early live smoke run** (pack→install→instantiate from the
   package) that proves the publish path before any gate is claimed.
3. Phase 3 US1 — PV-1..PV-5 green against the installed package.
4. **STOP and VALIDATE**: the published template is installable with lifecycle + side-effect-free
   default. This alone delivers the board item's core promise.

### Incremental Delivery

1. Setup + Foundational → package on the feed, proven installable.
2. US1 (PUBLISHED) → consumers can install the new behavior (MVP).
3. US2 (TAGGED) → reproducible named snapshot, pin-able.
4. US3 (COHERENT) → cross-repo record agrees; board item Done.
5. Each state is a checkpoint; a failure at any state leaves the record honestly in-progress (FR-010).

---

## Notes

- [P] = different artifacts, no dependency on an incomplete task. Parallel US1 gates write per-gate
  evidence files (`pv2-manifest.md`, `pv4-side-effect.md`, `pv5-lifecycle.md`) to keep that invariant
  honest; `publish-evidence.md` is the rollup written only by the sequential PV-1/PV-3 tasks.
- [Story] label maps each task to US1/US2/US3 for traceability; Setup/Foundational/Polish carry none.
- The only in-repo source edit is the `<Version>` bump (T005); everything else packs existing content,
  tags, and reconciles the cross-repo record.
- All verification runs against the **installed** package, never the working tree (plan.md Standing
  Assumption; research.md R4–R5).
- Commit after each logical group; cross-repo commits to `FS-GG/.github` use the `registry:` prefix,
  in-repo commits use the `206:` prefix.
- On any version/tag collision, fail loudly and pick the next unused identifier — never overwrite or
  move (FR-002, `contracts/coherent-set.md`).
