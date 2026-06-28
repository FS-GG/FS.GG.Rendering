---
description: "Task list for Feature 213 — Adopt org-shared .NET build config (unified restore-lock gate)"
---

# Tasks: Adopt org-shared .NET build config (unified restore-lock gate)

**Input**: Design documents from `/specs/213-adopt-shared-build-config/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/shared-build-config-adoption.md, quickstart.md

**Tests**: INCLUDED. The feature explicitly requires updating two breaking policy tests (research R8,
Constitution Principle V) so they assert the new contract (fail-before / pass-after). No new test
projects are created; existing Expecto assertions are amended.

**Organization**: Tasks are grouped by user story. Because US1/US2/US3 all edit the same two
repo-owned `*.local.props` files (and the canonical files they import), the stories are **independently
testable** but **not file-parallel** across stories — they run in priority order P1 → P1 → P2.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies on incomplete tasks)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Exact file paths are included in each task

## Path Conventions

Repo-root configuration change. Managed (synced) files and repo-owned override files live at the
repository root; the two policy tests live under `tests/`. No new source directories are created.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the canonical source is reachable and capture the pre-adoption baseline so any
post-adoption red is known to be pre-existing, not a regression.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** The baseline MUST run **every** test
> project — the solution PLUS `tests/Package.Tests` (release-only public-surface gate) and the
> `samples/**/*.Tests` package-feed consumers — via the discovery-based runner, so nothing silently
> drops out (the Feature 175 trap).

- [X] T001 Verify prerequisites: the sibling source of truth `../.github/dist/dotnet/` and `../.github/scripts/sync-build-config.sh` are present; the .NET `net10.0` SDK is installed; the working tree is on branch `213-adopt-shared-build-config`. Record the result in `specs/213-adopt-shared-build-config/readiness/preflight.md`.
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/213-adopt-shared-build-config/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + Package.Tests + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge).
- [X] T003 [P] Snapshot the pre-adoption inputs for later scope/diff checks: **record the discovered count** of committed `**/packages.lock.json` (plan expects ~39 = the 38 slnx members + the sample lanes outside the slnx; capture the actual number rather than asserting it) into `specs/213-adopt-shared-build-config/readiness/lockfile-inventory.md`, and record the current `git rev-parse HEAD` of `template/base/` as the unchanged-scope reference for INV-7 / C-8.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the property/pin partition map every story depends on, and run the real toolchain
end-to-end **once** so the plan's provisional root-cause hypotheses are confirmed (or replaced) before
any targeted prune or test edit is committed as done.

**⚠️ CRITICAL**: No user-story work can begin until this phase is complete.

> **⚠️ Early live smoke run — build-level live proof (STANDING, do not omit).** This feature has no
> application behavior, so the "drive the running app" smoke is replaced by its build-level equivalent
> (plan.md §Standing assumption). The plan's "local clobbers the gate / clobbers NU1603" hypotheses are
> **unverified assumptions until the real toolchain has been run**. T005 drives `--adopt` + a first
> `--force-evaluate` restore to observe the actual raw failure modes, exactly as the running-app smoke
> would. Do not defer this to the per-story checkpoints.

- [X] T004 Build the property / pin partition map: read the current root `Directory.Build.props` and `Directory.Packages.props`, and reconcile every setting against the partition table in `data-model.md` (canonical-owned vs local-kept vs dropped). Record the reconciled map in `specs/213-adopt-shared-build-config/readiness/partition-map.md` — this is the root-cause map all three stories implement against.
- [X] T005 **Early live smoke run (build-level live proof)**: run `../.github/scripts/sync-build-config.sh --adopt .` (renames the hand-authored root files to `Directory.Build.local.props` / `Directory.Packages.local.props`, writes the three managed files + `.config/dotnet-tools.json`), then run a first `dotnet restore FS.GG.Rendering.slnx --force-evaluate` **before pruning** to observe the actual raw failures (e.g. duplicate `FSharp.Core` `NU1504`/`NU1011`; gate/NU clobber from the unpruned local). Record the observed failures in `specs/213-adopt-shared-build-config/readiness/smoke-adopt.md` and confirm or replace the plan's hypotheses there.
- [X] T006 [P] Confirm the test + evidence scaffolding: ensure `specs/213-adopt-shared-build-config/readiness/` exists and identify the exact assertions that break — the `ContinuousIntegrationBuild` string assertion in `tests/Build.Tests/RestoreLockTests.fs` and the `SkiaSharp.HarfBuzz` read of the root `Directory.Packages.props` in `tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs` — so US1/US2 know precisely which lines they amend.

**Checkpoint**: Partition map recorded, raw adoption observed against a live restore, breaking
assertions pinpointed — user-story pruning can now begin.

---

## Phase 3: User Story 1 - Adopt the shared baseline without losing repo-specific settings (Priority: P1) 🎯 MVP

**Goal**: The three managed files are taken verbatim from the canonical source (drift-clean) and every
repo-specific setting survives in the repo-owned `*.local.props`, so the build behaves as before apart
from the deliberately unified gate.

**Independent Test**: `sync-build-config.sh --check .` reports zero drift AND a full
restore→build→test cycle is green — proving the relocated settings still take effect.

### Tests for User Story 1 ⚠️

> **Write the test edit FIRST, confirm it FAILS against the moved pin, then make it pass.**

- [X] T007 [US1] Update `tests/SkiaViewer.Tests/Feature142SurfaceAndDependencyTests.fs` to read the `SkiaSharp.HarfBuzz` pin from `Directory.Packages.local.props` (the file the repo now owns its package versions in) instead of the root `Directory.Packages.props`; confirm it fails before the prune and passes after.

### Implementation for User Story 1

- [X] T008 [US1] Prune `Directory.Build.local.props`: **REMOVE the entire "Restore (211)" group** — `RestorePackagesWithLockFile`, the old `RestoreLockedMode` **CIB** gate, and the `NU1603;NU1608` promotion — because the canonical `Directory.Build.props` now owns them; a surviving local CIB gate would silently win (imported last) and re-defeat the migration (research R2/R5). KEEP the repo-specific settings — `TargetFramework=net10.0`, `LangVersion`, `Nullable`, `AllowUnsafeBlocks`, `TreatWarningsAsErrors`, the `Package` metadata group (Version/Authors/repository URLs/license/`PackageReadmeFile`), the `FsDocs*` group, the `FSharp.Core` `PackageReference` item (no version), and the README `None Include … Pack=true` group. Make the first F# warning-promotion line **append-form** — `<WarningsAsErrors>$(WarningsAsErrors);FS0025;FS0026;FS0052;FS0064</WarningsAsErrors>` then `;FS0078` — so the canonical `NU1603;NU1608` are preserved (research R3). Do NOT edit the canonical `Directory.Build.props` (it imports the local last).
- [X] T009 [US1] Prune `Directory.Packages.local.props`: KEEP every non-baseline `PackageVersion` (Fable.Elmish, SkiaSharp*, HarfBuzz*, Silk.NET*, Yoga.Net, YamlDotNet, the Expecto/Test.Sdk/YoloDev test pins, the Fake.*/FSharp.SystemTextJson/XParsec build-tooling pins) at their existing versions; REMOVE the redundant `ManagePackageVersionsCentrally` property group (now canonical, with transitive pinning); and **REMOVE the `FSharp.Core` `PackageVersion 10.1.301`** — it is now provided by the org baseline in the canonical `Directory.Packages.props`, and leaving the duplicate would fail the very next restore (T011) with CPM `NU1504`/`NU1011` (FR-004 / FR-005). The version-less `FSharp.Core` `PackageReference` stays in `Directory.Build.local.props` (T008) to resolve via CPM. (US3 verifies the resulting single-sourcing.)
- [X] T010 [US1] Verify managed files are pristine: run `../.github/scripts/sync-build-config.sh --check .` and confirm `ok:` for `Directory.Build.props`, `Directory.Packages.props`, `.config/dotnet-tools.json` with exit 0 (INV-1 / C-1). Any `DRIFT` line means content must be moved into the matching `*.local.props`.
- [X] T011 [US1] Regenerate and commit lockfiles under transitive pinning: `dotnet restore FS.GG.Rendering.slnx --force-evaluate`, then regenerate any committed lockfile outside the slnx (the `samples/CanvasDemo`, `samples/SymbologyBoard` lanes and any other committed `packages.lock.json`); `git add '**/packages.lock.json'`.
- [X] T012 [US1] Prove reproducible restore: re-run `dotnet restore FS.GG.Rendering.slnx --locked-mode` and confirm `git diff --quiet -- '**/packages.lock.json'` (REPRODUCIBLE — no churn) (INV-6 / SC-004 / C-6).
- [X] T013 [US1] Build + test green: `dotnet build FS.GG.Rendering.slnx -c Debug --no-restore`, then `dotnet test tests/SkiaViewer.Tests/SkiaViewer.Tests.fsproj` — the relocated warnings/metadata/fsdocs settings still apply and the updated T007 test passes (SC-003 / C-7).

**Checkpoint**: Managed files drift-clean, repo settings preserved via locals, restore→build→test green — US1 is the deliverable MVP.

---

## Phase 4: User Story 2 - Unify the restore-lock gate on the real CI signal (Priority: P1)

**Goal**: The `RestoreLockedMode` gate is spelled `GITHUB_ACTIONS And Exists(lockfile)` (canonical) and
no stale `ContinuousIntegrationBuild` gate survives in the local file to re-defeat the migration.

**Independent Test**: The gate evaluates to locked when `GITHUB_ACTIONS=true` and a lockfile exists,
and to unlocked with no CI variable set — verifiable by inspecting restore behavior in each condition.

### Tests for User Story 2 ⚠️

> **Write the test edit FIRST, confirm it FAILS against the canonical file's new spelling, then pass.**

- [X] T014 [US2] Update `tests/Build.Tests/RestoreLockTests.fs`: change the gate assertion (and its failure message) from `ContinuousIntegrationBuild` to `GITHUB_ACTIONS`; leave the `RestorePackagesWithLockFile`, `<RestoreLockedMode`, and `NU1603`-in-`WarningsAsErrors` assertions intact (they still hold against the canonical root file). Confirm fail-before / pass-after.

### Implementation for User Story 2

- [X] T015 [US2] Verify no `ContinuousIntegrationBuild` `RestoreLockedMode` gate survives in `Directory.Build.local.props` (the "Restore (211)" group removed by T008): grep the local file for `ContinuousIntegrationBuild` and `RestoreLockedMode` and confirm zero hits, so the effective gate is the canonical `GITHUB_ACTIONS` spelling and not a stale local CIB gate that would silently win on last-import (research R2/R5).
- [X] T016 [US2] Run `dotnet test tests/Build.Tests/Build.Tests.fsproj` — the updated `RestoreLockTests` gate assertion passes against the canonical `GITHUB_ACTIONS` spelling (C-2).
- [X] T017 [US2] Gate-condition probes (SC-002): `GITHUB_ACTIONS=true dotnet restore FS.GG.Rendering.slnx --locked-mode` engages locked restore and succeeds with the committed lockfile present; `env -u GITHUB_ACTIONS dotnet restore FS.GG.Rendering.slnx` is NOT blocked. Record both outcomes in `specs/213-adopt-shared-build-config/readiness/gate-probes.md`. **SC-005 cross-repo match (by construction):** the `GITHUB_ACTIONS` gate string lives in the canonical `Directory.Build.props` taken verbatim from the shared source, so the drift-clean `--check` (T010) is itself the proof that Rendering's gate spelling equals the other three FS-GG repos' — note this in the same evidence file rather than re-fetching sibling repos.

**Checkpoint**: Gate spelled `GITHUB_ACTIONS`, no stale local CIB gate, both probes behave as labeled — Rendering is no longer the lone CIB outlier (SC-005).

---

## Phase 5: User Story 3 - Central-package and tool coherence (Priority: P2)

**Goal**: `FSharp.Core` resolves from the single org baseline (`10.1.301`) with no duplicate-pin error,
and the shared tool manifest's `fake-cli` version agrees with the repo's `Fake.Core.*` library pin.

**Independent Test**: Restore succeeds with no `NU1504`/`NU1011` duplicate-package error and the
lockfile shows `FSharp.Core 10.1.301`; `.config/dotnet-tools.json` `fake-cli` equals the `Fake.Core.*`
pin (`6.1.4`).

### Implementation for User Story 3

- [X] T018 [US3] Verify `FSharp.Core` is single-sourced from the org baseline: confirm `Directory.Packages.local.props` carries **no** `FSharp.Core` `PackageVersion` (removed in T009), that the canonical `Directory.Packages.props` baseline declares it once, and that the version-less `FSharp.Core` `PackageReference` remains in `Directory.Build.local.props` to resolve via CPM (FR-004 / FR-005 / C-3).
- [X] T019 [US3] Verify baseline non-duplication end-to-end: a clean `dotnet restore FS.GG.Rendering.slnx` succeeds with no `NU1504`/`NU1011` duplicate-version error and the lockfile records `FSharp.Core` at `10.1.301` (INV-3 / SC-006). (The removal and lockfile regeneration already happened in US1 T009/T011; this is the coherence confirmation.)
- [X] T020 [US3] Verify tool/library parity: confirm `.config/dotnet-tools.json` `fake-cli` is `6.1.4` and the `Fake.Core.*` pin in `Directory.Packages.local.props` is `6.1.4` (INV-5 / C-5); confirm the compiled-FAKE build path still runs (`dotnet run --project build` resolves), so the manifest does not disturb the existing front-end.

**Checkpoint**: `FSharp.Core` single-sourced from baseline, no duplicate error, `fake-cli`==`Fake.Core.*`==`6.1.4` — central-package and tool coherence proven.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Whole-feature enforcement, scope, and acceptance evidence spanning all three stories.

- [X] T021 Deliberate-substitution probe (INV-4 / C-4): temporarily change one `PackageVersion` in `Directory.Packages.local.props` (e.g. bump `YamlDotNet`) WITHOUT regenerating the lockfile, run `GITHUB_ACTIONS=true dotnet restore FS.GG.Rendering.slnx --locked-mode` and confirm it FAILS (graph ≠ lockfile / NU1603) — proving the `WarningsAsErrors` append rule did not silently disable enforcement — then **revert** the change. Record in `specs/213-adopt-shared-build-config/readiness/substitution-probe.md`.
- [X] T022 [P] Scope-boundary check (INV-7 / C-8 / FR-010): `git diff --quiet -- template/base && echo "template/base UNCHANGED"` — confirm this feature changed no `template/base/**` emitted build file.
- [X] T023 [P] Opt-out preservation (FR-011): confirm the package-validation test project (and any project that sets `RestoreLockedMode=false` / carries no lockfile) still opts out unchanged after adoption.
- [X] T024 Re-run the comprehensive baseline (`dotnet fsi scripts/baseline-tests.fsx --out specs/213-adopt-shared-build-config/readiness/baseline-after.md`) and diff against T002 — confirm no new reds beyond the known pre-existing set.
- [X] T025 Run the full `quickstart.md` validation end-to-end (§1–§7) and confirm the "Success = all of" line: zero drift · REPRODUCIBLE · build+tests green · both gate probes · substitution fails-then-reverted · `6.1.4`==`6.1.4` · `template/base UNCHANGED`. Maps to SC-001…SC-006.
- [X] T026 Move the Coordination board item FS.GG.Rendering#11 Ready → In review/Done per `contracts/shared-build-config-adoption.md` (cross-repo coordination); no new cross-repo request is created (this consumes an already-merged contract).

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup. The `--adopt` smoke (T005) BLOCKS all stories (there are no `*.local.props` to prune until it runs).
- **User Stories (Phase 3→4→5)**: All depend on Foundational. They edit the same two `*.local.props` files, so they run **sequentially in priority order**, not in parallel.
- **Polish (Phase 6)**: Depends on all three stories being complete.

### User Story Dependencies

- **US1 (P1)**: Starts after Foundational. Performs the full prune of both local files — including the `FSharp.Core` `PackageVersion` removal (T009), so the very next restore is duplicate-free — plus drift-clean + lockfile regen + green cycle. Independently testable via `--check` + restore→build→test.
- **US2 (P1)**: Logically depends on the US1 prune of `Directory.Build.local.props` (the CIB restore block removed by T008); its test edit and gate probes are independent. Independently testable via the locked/unlocked probes.
- **US3 (P2)**: Depends on US1's `Directory.Packages.local.props` prune; **verifies** the `FSharp.Core` baseline single-sourcing (the removal itself is done in US1 T009) and the `fake-cli`/`Fake.Core.*` parity. Independently testable via clean restore + `fake-cli` parity.

### Within Each User Story

- Test edit written and failing BEFORE the implementing prune (US1: T007; US2: T014).
- Prune local files → drift `--check` → regenerate lockfiles → reproducible restore → build/test green.

### Parallel Opportunities

- Setup: T003 [P] runs alongside T001/T002.
- Foundational: T006 [P] runs alongside T004/T005.
- Polish: T022 [P] and T023 [P] run alongside T021.
- Across stories: NONE — US1/US2/US3 share the `*.local.props` files and must serialize.

---

## Parallel Example: Phase 1 Setup

```bash
# T001/T002 establish prerequisites + baseline; T003 runs concurrently:
Task: "T003 Snapshot lockfile inventory (39) + template/base ref in specs/213-adopt-shared-build-config/readiness/"
```

---

## Implementation Strategy

### MVP First (User Story 1 only)

1. Phase 1: Setup (prerequisites + baseline).
2. Phase 2: Foundational — **including the early live `--adopt` smoke (T005)** that validates the
   root-cause hypotheses against the real toolchain before any targeted prune.
3. Phase 3: US1 — adopt + relocate repo settings, drift-clean, green cycle.
4. **STOP and VALIDATE**: `--check` zero drift + restore→build→test green.
5. This is a shippable increment (the substance of board item #11).

### Incremental Delivery

1. Setup + Foundational → adoption mechanically applied and observed.
2. US1 → drift-clean baseline with repo settings preserved → validate (MVP).
3. US2 → gate unified on `GITHUB_ACTIONS`, probes pass → validate.
4. US3 → `FSharp.Core` single-sourced + tool parity → validate.
5. Polish → substitution probe, scope boundary, quickstart, board move.

---

## Notes

- [P] = different files, no dependency on an incomplete task.
- The two policy-test edits (T007, T014) are the Principle V fail-before/pass-after evidence — write
  them first.
- The canonical files (`Directory.Build.props`, `Directory.Packages.props`,
  `.config/dotnet-tools.json`) are DO-NOT-EDIT; all repo intent lives in the `*.local.props`. Any edit
  to a managed file is drift (`--check` fails).
- Deterministic string tests can pass while the effective property differs (Feature 175 lesson) — the
  live restore proof (T005, T012, T017) and the substitution probe (T021) are the honest checks.
- Commit the large mechanical lockfile diff (T011) and the test edits separately for a readable history.
