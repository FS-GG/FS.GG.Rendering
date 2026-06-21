---

description: "Task list for Shared Test/Util Helpers (Code-Health Refactoring Phase 1)"
---

# Tasks: Shared Test/Util Helpers (Code-Health Refactoring Phase 1)

**Input**: Design documents from `specs/178-shared-test-util-helpers/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: No new test tasks. This is a Tier-2, behavior-preserving refactor — the spec asks for
**no new behavior** and pins correctness with the **existing** suites (Feature 159 identity/reuse/
promotion, composition/control fingerprint, path-dependent tests, surface-area/API-reference
baselines). "Fail-before/pass-after" does not apply to a no-op refactor; the gate is **no new red**
plus the `grep`-based duplicate-elimination checks. Per-story verification tasks run those existing
suites and greps.

**Organization**: Tasks grouped by user story (P1 → P2 → P3), each independently shippable and
revertible (FR-009, SC-006).

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: US1 (repo-root), US2 (FNV), US3 (clamp). Setup/Foundational/Polish carry no label.

## Path Conventions
Single multi-project solution `FS.GG.Rendering.slnx`; `src/*` packages and `tests/*` test projects at
repo root.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Capture the pre-refactor baseline so pre-existing reds are never mistaken for regressions.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Run **every** test project, including the
> ones the solution omits (`tests/Package.Tests` and the `samples/**/*.Tests` package-feed consumers)
> — that is exactly where Feature 175's surprises hid. Use the discovery-based runner so nothing
> silently drops out.

- [X] T001 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/178-shared-test-util-helpers/readiness/baseline.md` (runs EVERY `*.Tests.fsproj` — solution + `tests/Package.Tests` + `samples/**/*.Tests`); confirm green **except** the two documented package-feed reds (`tests/Package.Tests`, `samples/ControlsGallery/ControlsGallery.Tests`) and flag any other red here (SC-001).
- [X] T002 [P] Record the pre-refactor public-surface snapshot for later diffing: capture `git diff --stat -- '*.fsi'` baseline (expected empty) and note the surface-area/API-reference baseline files under `tests/` that must stay green (FR-007, SC-005).
- [X] T003 [P] Snapshot the pre-refactor duplicate counts for the three target families (used to prove elimination later): repo-root finders `grep -rn --include='*.fs' -e 'findRepositoryRoot' -e 'FS.GG.Rendering.slnx' tests/ | wc -l` (~59 files), FNV offset basis `grep -rn --include='*.fs' '0xcbf29ce484222325UL' src/ | wc -l` (4 sites), and `grep -rn --include='*.fs' 'let clamp\|let inline clamp' src/` (3 defs). Save to `specs/178-shared-test-util-helpers/readiness/baseline.md`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Create the three shared helper homes and the project/build wiring every story depends on,
and confirm the no-runtime-change posture against a real build/run.

**⚠️ CRITICAL**: No story migration can begin until the helper homes build clean and are reachable.

> **⚠️ "Live smoke run", adapted (STANDING requirement honored).** This refactor changes **no runtime
> behavior** — there is no defect/root-cause hypothesis to confirm against a running viewer. The
> honest live evidence here is a **clean solution build + the full regression suite green** (Feature
> 159 reuse/promotion outcomes are hash-driven, so a green run *is* the real-app evidence that
> layer-reuse/promotion identity is unchanged). T007 below is that early "smoke run": it MUST pass on
> the **untouched** code (re-confirming T001) before any duplicate is deleted, so a later red is
> unambiguously attributable to a migration step, not a pre-existing condition.

- [X] T004 Create `tests/TestSupport/RepositoryRoot.fs` with the shared finder per `contracts/repo-root-finder.md`: `module RepositoryRoot` exposing `find : string -> string` (canonical marker set `*.sln` ∪ `*.slnx` ∪ `build.fsx`, parent-walk, fail-loud with actionable message at the filesystem root) and `value = find AppContext.BaseDirectory`. No `private`/`internal`/`public` keyword on top-level bindings.
- [X] T005 Create `tests/TestSupport/TestSupport.fsproj` targeting `net10.0` with `<IsPackable>false</IsPackable>` and `<IsTestProject>false</IsTestProject>` (non-packed support assembly, no package surface — FR-007, research R4); add it to `FS.GG.Rendering.slnx`.
- [X] T006 [P] Create `src/Controls/Internal/Hashing.fs` per `contracts/fnv-hash-primitive.md`: `module internal Hashing` (no `.fsi`, mirroring `Internal/AttrKeys.fs`) exposing `offsetBasis = 0xcbf29ce484222325UL`, `prime = 0x100000001b3UL`, `inline step h x = (h ^^^ x) * prime`, and `foldBytes`/char-mix helpers covering the three existing conventions. Register it in `Controls.fsproj` **before `Composition.fs`** (next to `Internal/AttrKeys.fs`). It is a primitive — do NOT collapse the four folds into one function.
- [X] T007 [P] Create `src/Shared/Numeric.fs` per `contracts/clamp.md`: `module internal Numeric` with `let inline clamp lo hi value = min hi (max lo value)` (no `.fsi`). Do not wire consumers yet (that happens in US3).
- [X] T008 Early "smoke" gate (no-runtime-change posture): with the new helper files added but **no call site migrated yet**, run `dotnet build FS.GG.Rendering.slnx` and `dotnet test FS.GG.Rendering.slnx`; confirm the result matches the T001 baseline exactly (green except the two known package-feed reds). This is the live evidence that the seams compile and changed nothing before any deletion begins.

**Checkpoint**: Three helper homes exist and build; baseline re-confirmed on untouched call sites — story migrations can begin (P1 → P2 → P3, each independently shippable).

---

## Phase 3: User Story 1 - One repository-root finder for all tests (Priority: P1) 🎯 MVP

**Goal**: Exactly one repository-root finder; every test/harness call site uses it; all copies gone.

**Independent Test**: Full `dotnet test` green (path-dependent tests resolve the same root) **and** a
repo-wide grep finds zero local finders outside `tests/TestSupport` (SC-002, Acceptance 1–3).

### Implementation for User Story 1

- [X] T009 [P] [US1] Add a `ProjectReference` to `..\TestSupport\TestSupport.fsproj` in every consuming test/harness `.fsproj` that currently defines a local finder: `tests/{Lib,Smoke,Controls,Elmish,Package,SkiaViewer,Testing}.Tests`, `tests/Rendering.Harness`, `tests/Rendering.Harness.Tests` (see research R1 file list). Confirm no reference cycle is introduced.
- [X] T010 [US1] Migrate **Family A** (named `findRepositoryRoot`) sites: delete each local `let rec findRepositoryRoot` + `let repositoryRoot` and route callers to `RepositoryRoot.find` / `RepositoryRoot.value` (e.g. `tests/Lib.Tests/Tests.fs:173`, `tests/Package.Tests/*`, `tests/Rendering.Harness/Cli.fs`, `tests/Rendering.Harness/SkillParity.fs`, the Feature151 regression files). Preserve each caller's downstream path logic (e.g. `readinessPath`).
- [X] T011 [US1] Migrate **Family B** (inline anonymous `FS.GG.Rendering.slnx` walks) sites: delete each inline `let rec loop`/`repoRoot` block in `tests/Controls.Tests/*` and `tests/Elmish.Tests/*` (and any others from the R1 list) and route to `RepositoryRoot.find`/`value`. Reconciles onto the canonical marker set (research R1) — resolved root is unchanged for the current tree.
- [X] T012 [US1] Verify US1: run `dotnet test FS.GG.Rendering.slnx` (+ `tests/Package.Tests` and `tests/Rendering.Harness.Tests`) — all path-dependent tests green vs T001 baseline; then a **repo-wide** grep `grep -rn --include='*.fs' --include='*.fsx' -e 'let findRepositoryRoot' -e 'let rec findRepositoryRoot' -e 'FS.GG.Rendering.slnx' . | grep -v 'tests/TestSupport/'` returns **nothing** (SC-002 says "repo-wide" — also catches any finder in `samples/` or `src/`). Confirm fail-loud message via the no-marker case (Acceptance 3).

**Checkpoint**: US1 fully functional and independently revertible. **MVP banked** (highest-volume, lowest-risk duplication removed).

---

## Phase 4: User Story 2 - One FNV-1a hash helper for production folds (Priority: P2)

**Goal**: The four `src/Controls` folds draw the offset basis, prime, and core step from one shared
`Internal/Hashing` primitive, producing bitwise-identical hashes (including the Phase-0-corrected
`feature159Hash`).

**Independent Test**: Feature 159 identity/reuse/promotion + composition/control fingerprint suites
green, and zero `0xcbf29ce484222325UL` outside `Internal/Hashing.fs` (SC-003, Acceptance 1–3).

### Implementation for User Story 2

- [X] T013 [P] [US2] Migrate `src/Controls/Composition.fs:156` `fnv1a` to use `Hashing.offsetBasis`/`prime`/`foldBytes` (UTF-8 byte convention), preserving exact output including empty-string.
- [X] T014 [P] [US2] Migrate `src/Controls/RetainedRender.fs:850` `feature159Hash` to use `Hashing` constants/step while keeping its per-char `int ch` widening, **separate** xor/multiply statements, and `'|'` separator — byte-identical to the Phase-0-corrected baseline.
- [X] T015 [US2] Migrate the two `src/Controls/Control.fs` sites — `hashScene` (~2453) and `fingerprintParts`/`fingerprintString` (~2830, currently using local `fnvOffset`/`fnvPrime`) — to `Hashing` constants/step, keeping the `uint64` mix and `uint16 c` char widening and all length/domain prefixes. (Same file → sequential after T013/T014 are independent.)
- [X] T016 [US2] Verify US2: `dotnet test FS.GG.Rendering.slnx --filter 'FullyQualifiedName~Feature159'` and `--filter 'FullyQualifiedName~Fingerprint'` green (relational hash identity unchanged — no absolute-constant assertions, per Assumptions); then a **repo-wide** grep `grep -rn --include='*.fs' --include='*.fsx' '0xcbf29ce484222325UL' . | grep -v 'Internal/Hashing.fs'` returns **nothing** (SC-003 says "repo-wide" — also catches the literal in any `tests/` assertion). Confirm no `.fsi` diff (T002).

**Checkpoint**: US1 + US2 both independently functional; production hashing single-sourced with no identity drift.

---

## Phase 5: User Story 3 - One `clamp` helper (Priority: P3)

**Goal**: One shared `clamp`; the three `src` local definitions removed and routed through it.

**Independent Test**: Layout-sizing / text-caret / viewer-scaling tests green, and zero local
`let clamp` outside `src/Shared/Numeric.fs` (SC-004, Acceptance 1–2).

### Implementation for User Story 3

- [X] T017 [US3] Link `src/Shared/Numeric.fs` into `src/Controls/Controls.fsproj` and `src/SkiaViewer/SkiaViewer.fsproj` via `<Compile Include="..\Shared\Numeric.fs"><Link>Shared/Numeric.fs</Link></Compile>`. Pin the compile position so it precedes every consumer: in `Controls.fsproj` insert it **before `RetainedRender.fs`** (the `let clamp` consumer at ~line 714; placing it near `Internal/AttrKeys.fs` at the top is safe); in `SkiaViewer.fsproj` insert it **before `Host/OpenGl.fs`**. No new `ProjectReference`; no package surface (research R3).
- [X] T018 [P] [US3] Remove `let clamp` from `src/SkiaViewer/Host/OpenGl.fs:461` and route call sites to `Numeric.clamp` (same `(lo, hi, value)` order).
- [X] T019 [P] [US3] Remove `let clamp` from `src/Controls/RetainedRender.fs:714` and route to `Numeric.clamp`.
- [X] T020 [P] [US3] Remove `let clamp` from `src/Controls/TextInput.fs:45` (was `value |> max low |> min high` — identical semantics) and route to `Numeric.clamp`. Leave `Layout.clampNonNegative` untouched (different function — research R3).
- [X] T021 [US3] Verify US3: `dotnet test FS.GG.Rendering.slnx --filter 'FullyQualifiedName~Layout|Caret|Viewer'` green; then a **repo-wide** grep `grep -rn --include='*.fs' --include='*.fsx' -e 'let clamp' -e 'let inline clamp' . | grep -v 'Shared/Numeric.fs'` returns **nothing** (SC-004 says "repo-wide", and FR-006 names "any duplicated in tests" — the grep over `tests/` confirms no test-side `let clamp` exists; `clampNonNegative` does not match this pattern).

**Checkpoint**: All three consolidations complete and independently revertible.

---

## Phase 6: Polish & Cross-Cutting Concerns

- [X] T022 Full no-regression re-run: `dotnet fsi scripts/baseline-tests.fsx --out specs/178-shared-test-util-helpers/readiness/post-change.md`; diff against `baseline.md` — only the two known package-feed reds remain, no new failures (SC-001).
- [X] T023 [P] Confirm public surface unchanged: `git diff -- '*.fsi'` shows no published-surface signature change, and surface-area/API-reference baseline tests are green (FR-007, SC-005).
- [X] T024 [P] Record net source-line reduction (expect a four-figure drop) in `specs/178-shared-test-util-helpers/readiness/post-change.md` (SC-005): `git diff --shortstat main`.
- [X] T025 Run the `quickstart.md` validation guide end-to-end (steps 0–5) and confirm each story's grep + suite checks and the independence check (SC-006) pass.

---

## Dependencies & Execution Order

### Phase Dependencies
- **Setup (Phase 1)**: no dependencies — start immediately.
- **Foundational (Phase 2)**: depends on Setup; **blocks all stories**. T008 gates that the seams compile and change nothing.
- **User Stories (Phase 3–5)**: each depends only on Foundational. They are independent and may proceed in parallel or in P1→P2→P3 order. (US1 touches `tests/*`; US2 touches `src/Controls` folds; US3 touches `src/Controls`+`src/SkiaViewer` clamp sites — US2 and US3 both edit `Controls.fsproj`/`RetainedRender.fs`, so coordinate those two files if run concurrently.)
- **Polish (Phase 6)**: after all desired stories complete.

### User Story Dependencies
- **US1 (P1)**: after Foundational. No dependency on US2/US3. → MVP.
- **US2 (P2)**: after Foundational. Independent of US1/US3.
- **US3 (P3)**: after Foundational. Independent of US1/US2 (shares `Controls.fsproj`/`RetainedRender.fs` edits with US2 — sequence those edits).

### Parallel Opportunities
- T002/T003 (Setup) in parallel; T006/T007 (Foundational helper homes) in parallel with T004.
- US1's `.fsproj` reference adds (T009) parallel across projects; US2's T013/T014 parallel (different files); US3's T018/T019/T020 parallel (different files).
- With staffing: US1, US2, US3 can run as three parallel tracks once Phase 2 is done.

---

## Parallel Example: User Story 2

```bash
# Independent fold migrations (different files) can run together:
Task: "Migrate Composition.fs fnv1a to Hashing primitive"      # T013
Task: "Migrate RetainedRender.fs feature159Hash to Hashing"    # T014
# Then T015 (Control.fs, same file twice) runs after, sequentially.
```

---

## Implementation Strategy

### MVP First (User Story 1 only)
1. Phase 1 Setup → 2. Phase 2 Foundational (incl. T008 no-change smoke gate) → 3. Phase 3 US1 →
4. **STOP & VALIDATE**: US1 grep returns zero + suite green → 5. Ship the repo-root consolidation alone.

### Incremental Delivery
Setup + Foundational → US1 (ship) → US2 (ship) → US3 (ship). Each consolidation is an independent,
individually-green change unit and can be reverted without affecting the other two (SC-006).

---

## Notes
- [P] = different files, no incomplete-task dependency.
- No new tests: correctness is pinned by existing regression suites + duplicate-elimination greps (per spec Assumptions).
- The FNV helper is a **primitive**, not a single hash function — each site keeps its byte-identical mixing convention (research R2 / contract).
- Commit after each story (or logical group) to keep the three consolidations independently revertible.
- Keep the two documented package-feed reds in view — they are baseline, not regressions (SC-001).
