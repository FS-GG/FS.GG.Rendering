---
description: "Task list for Symbology Live Board Sample (M6)"
---

# Tasks: Symbology Live Board Sample (M6)

**Input**: Design documents from `/specs/193-symbology-live-board/`

**Prerequisites**: plan.md (required), spec.md (user stories), research.md, data-model.md, contracts/ (cli-contract.md, board-core.md), quickstart.md

**Tests**: INCLUDED — the spec makes US2 evidence-grade (P1) and plan.md / research.md (D6) / contracts/board-core.md all mandate `tests/SymbologyBoard.Tests` with fail-before/pass-after assertions (Constitution I/V).

**Organization**: Tasks are grouped by user story so each can be implemented and tested independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on incomplete tasks)
- **[Story]**: Which user story the task serves (US1, US2, US3); Setup/Foundational/Polish carry no story label
- Every task names an exact file path

## Path Conventions

This is a multi-project F# solution (`FS.GG.Rendering.slnx`). New code lives under:

- `samples/SymbologyBoard/` — the M6 sample (`OutputType=Exe`, `IsPackable=false`): `Roster.fs`, `Board.fs`, `Program.fs`, `SymbologyBoard.fsproj`
- `tests/SymbologyBoard.Tests/` — semantic tests over the sample's deterministic core
- `specs/193-symbology-live-board/readiness/` — captured evidence + baseline artifacts

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Register the new projects so the solution builds and the baseline is known before any feature work.

> **⚠️ Comprehensive baseline (STANDING, do not narrow).** Record the full red/green set across **every**
> test project up front so pre-existing failures are not mistaken for regressions at merge. Use the
> discovery-based runner (it globs `*.Tests.fsproj`, so `Package.Tests` — the public-surface gate — and the
> `samples/**/*.Tests` package-feed consumers cannot silently drop out), not a hand-picked subset.

- [X] T001 Create `samples/SymbologyBoard/SymbologyBoard.fsproj` (`OutputType=Exe`, `IsPackable=false`, `net10.0`) mirroring `samples/CanvasDemo/CanvasDemo.fsproj`, with in-tree `ProjectReference`s to `FS.GG.UI.Scene`, `FS.GG.UI.Symbology`, `FS.GG.UI.Canvas`, `FS.GG.UI.Controls`, `FS.GG.UI.Controls.Elmish`, `FS.GG.UI.SkiaViewer`, `FS.GG.UI.Themes.Default` (compile order `Roster.fs` → `Board.fs` → `Program.fs`); register it in `FS.GG.Rendering.slnx` under the `/samples/` solution folder (FR-008/FR-009, D1)
- [X] T002 Establish the no-regression baseline: `dotnet fsi scripts/baseline-tests.fsx --out specs/193-symbology-live-board/readiness/baseline.md` (runs EVERY test project — solution + `Package.Tests` + samples — and records the full red/green set; pre-existing reds are flagged here, not discovered at merge)
- [X] T003 [P] Confirm formatting/`.editorconfig` conventions match `samples/CanvasDemo` so the new `samples/SymbologyBoard/*.fs` files pass the repo's existing format/lint gate with no new violations

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Stand up the deterministic core (`Roster`/`Board`) that BOTH P1 stories depend on, then prove it really renders and reproduces in *this* checkout.

**⚠️ CRITICAL**: No user-story work begins until this phase completes.

> **⚠️ Early evidence smoke run (STANDING, do not omit).** This feature is a greenfield additive sample, not a
> defect fix, so there is no root-cause map; per plan.md (lines 22–28) the analogue of the live-smoke mandate is
> an **early evidence smoke run**. Before US1/US3 presentation work, build the new sample and run its `evidence`
> subcommand in this checkout — twice from one seed (stable fingerprint) and once from a different seed
> (divergent fingerprint) — confirming a non-empty board reproduces. Treat the plan's narrative as **unverified
> until that real run is observed**: deterministic unit tests can pass while the actual run path is broken.

- [X] T004 Draft the `.fsi`-style module seams from `contracts/board-core.md` as the shapes/signatures to implement first (Constitution I): `Roster` (`UnitStats`, `mapUnit`, `motionOf`, `roster`) and `Board` (`dt`, `BoardWidth/Height`, `BoardUnit`, `World`, `Model`, `Msg`, `init`, `update`, `renderScene`, `evidence`) — pin these in `samples/SymbologyBoard/Roster.fs` and `samples/SymbologyBoard/Board.fs` as stubs before fleshing them out
- [X] T005 Port the approved M5 mapping **verbatim** from `specs/192-agent-unit-symbology/readiness/dry-run/FinalSymbolSet.fsx` into `samples/SymbologyBoard/Roster.fs`: the `UnitStats` record, `factionOf`/`klassOf`/`sigilOf`/`mapUnit` (R=30.0, faction/klass/sigil/threat/charge/speed/health/state/shield/heading via the existing `FS.GG.UI.Symbology` public surface — grammar NOT re-opened), the fixed `roster : UnitStats list` literal (6–10 units), and the pure `motionOf : UnitStats -> Token -> Motion` (Suspected→Blink; high Threat→Pulse; else Moving/Spin) (FR-001, D2/D3)
- [X] T006 Implement the deterministic core in `samples/SymbologyBoard/Board.fs`: `dt = 1.0/60.0`, fixed `BoardWidth`/`BoardHeight`; `BoardUnit`/`World`/`Model`/`Msg`; `seedWorld : int -> World` (positions/velocities derived from `seed`+index, `T=0`); `init` via `Loop.init`; pure `integrate` (advance `X/Y` by `Vx/Vy*dt`, **bounce** so the symbol radius stays on-board, `T += dt`); `update` via `Loop.advance dt integrate`; `renderScene` (interpolate `Previous`→`Current` with `Loop.alpha`, compose `Symbology.animate motion token world.T` placed at each interpolated position); `evidence : int -> Msg list -> string` = `SceneCodec.packageIdentity (SceneCodec.export (renderScene final)).CanonicalBytes` — no wall clock, no IO, no render-time randomness (FR-002/FR-003/FR-011, D3/D4/D5)
- [X] T007 Add the minimal `[<EntryPoint>]` evidence dispatch in `samples/SymbologyBoard/Program.fs` (default → `evidence`: build the fixed seed + scripted `Tick` sequence, fold it, print `symbology-board: seeded fingerprint = <id>`) — just enough to run the smoke (full reporting/exit-codes hardened in US2, full dispatch in US3)
- [X] T008 **Early evidence smoke run**: `dotnet build FS.GG.Rendering.slnx`, then `dotnet run --project samples/SymbologyBoard -- evidence` twice from the same seed (confirm identical fingerprint) and once from a different seed (confirm a different fingerprint); paste the raw stdout into `specs/193-symbology-live-board/readiness/smoke.md` as proof the board renders non-empty and reproduces in this checkout BEFORE building presentation/test layers on the plan's hypotheses
- [X] T009 [P] Create `tests/SymbologyBoard.Tests/SymbologyBoard.Tests.fsproj` (xUnit-style, mirroring `tests/Canvas.Tests/`) with a `ProjectReference` to `samples/SymbologyBoard/SymbologyBoard.fsproj`; register it in `FS.GG.Rendering.slnx`; add one trivial assertion that calls into `Board`/`Roster` to confirm the Exe's modules are reachable (D6) — if the toolchain rejects the Exe reference, fall back to the disclosed FSI-`#r` substitute under `readiness/` and note it here

**Checkpoint**: Real `evidence` run observed (stable + divergent fingerprints), core reachable from tests — user stories can now proceed.

---

## Phase 3: User Story 1 - Watch the approved roster move on a live board (Priority: P1) 🎯 MVP

**Goal**: Open a window showing every approved-roster unit as its fixed-grammar symbol, each animating continuously and smoothly between fixed steps, none drifting off-board; degrade gracefully to a skip notice on a headless host.

**Independent Test**: Launch `dotnet run --project samples/SymbologyBoard -- interactive` on a live-window host → all roster units render and animate, none leaves the board; on a headless host → clear "skipped — no live window" notice + exit `0`. The on-board invariant test stays green.

### Tests for User Story 1 ⚠️ (write FIRST, ensure they FAIL before implementation)

- [X] T010 [P] [US1] On-board / non-degenerate invariant test in `tests/SymbologyBoard.Tests/` — after N `update` steps every `BoardUnit` centre stays within `[radius, extent-radius]` on both axes (FR-011/SC-003)
- [X] T011 [P] [US1] Non-empty-board edge-case test in `tests/SymbologyBoard.Tests/` — a single-unit / degenerate roster still yields a non-blank `Scene` (non-empty canonical bytes), never a blank board passed off as success; **also** pin the zero-area-symbol case: a unit whose channels collapse to zero area still produces non-empty canonical bytes (the grammar's placeholder fallback is exercised here, not just assumed — spec Edge Cases, plan.md:46)

### Implementation for User Story 1

- [X] T012 [US1] Implement `runInteractive` in `samples/SymbologyBoard/Program.fs`: probe `Viewer.runtimeCapability()`; if not `PersistentWindow` print `symbology-board: interactive mode skipped — no live window/GL host.` and return `0`; else launch `ControlsElmish.runInteractiveApp` with a board-sized `ViewerOptions` (title, `BoardWidth`×`BoardHeight`, `FrameRateCap = Some 60`) and an `InteractiveAppHost` whose `Tick = fun _ -> Some (Tick dt)`, `Init`/`Update`/`View` wired to `Board.init`/`update`/`renderScene` on a `Canvas.volatile'` surface (FR-007/SC-004, D7, Constitution IV)
- [X] T013 [US1] Verify the live board (or `environment-limited` with a disclosed headless substitute per the diagnostics evidence rules): confirm every roster unit appears, motion is smooth via `Loop.alpha` interpolation (no per-frame jump/freeze), and no symbol drifts off-board over a sustained run; record the evidence note under `specs/193-symbology-live-board/readiness/`

**Checkpoint**: Interactive board renders and animates (or skips cleanly headless); on-board + non-empty tests green — US1 independently demonstrable (MVP).

---

## Phase 4: User Story 2 - Reproduce the board deterministically from a seed (Priority: P1)

**Goal**: Evidence-grade reproducibility — same seed + script ⇒ byte-identical fingerprint, reported with a zero exit; different seed ⇒ different fingerprint; any divergence fails loud with a non-zero exit.

**Independent Test**: `dotnet run --project samples/SymbologyBoard -- evidence` twice from the same seed → same fingerprint + "reproducible" + exit `0`; a different seed → different fingerprint; an injected divergence → diff-style stderr + non-zero exit. Reproducibility/divergence tests green.

### Tests for User Story 2 ⚠️ (write FIRST, ensure they FAIL before implementation)

- [X] T014 [P] [US2] Same-seed reproducibility test in `tests/SymbologyBoard.Tests/` — `Board.evidence s script = Board.evidence s script` (byte-identical) (SC-001/FR-005)
- [X] T015 [P] [US2] Different-seed divergence test in `tests/SymbologyBoard.Tests/` — `Board.evidence s1 script <> Board.evidence s2 script` for `s1 <> s2`, confirming the seed materially drives the board (SC-002/FR-006)

### Implementation for User Story 2

- [X] T016 [US2] Harden the `evidence` subcommand in `samples/SymbologyBoard/Program.fs` — the two-run repro-check **wrapper** around the pure `Board.evidence` fingerprint function (not a second fingerprint implementation; see board-core.md naming note): call `Board.evidence` twice on the fixed seed + scripted `Tick` sequence and compare; on byte-identical match print `symbology-board: reproducible (two runs byte-identical).` and exit `0`; on divergence print a diff-style `symbology-board: NON-REPRODUCIBLE — <a> <> <b>` to stderr and exit non-zero (never report divergence as success — FR-005, Constitution VI; canonical strings in cli-contract.md)
- [X] T017 [US2] Capture the milestone evidence artifact `specs/193-symbology-live-board/readiness/board-evidence.md` (FR-013/SC-006): the seed, the canonical fingerprint, the "two same-seed runs matched (byte-identical)" confirmation, the differing fingerprint from a documented second seed, and the exact commands that regenerate it

**Checkpoint**: Evidence path reports reproducibility with correct exit codes; reproducibility + divergence tests green; readiness artifact captured — US2 independently verifiable.

---

## Phase 5: User Story 3 - Build, run, and register the sample like the other samples (Priority: P2)

**Goal**: First-class parity with the existing sample set — builds as a registered solution project, runs from one documented command with discoverable subcommands, and a clear usage hint on an unknown subcommand.

**Independent Test**: From a clean checkout `dotnet build FS.GG.Rendering.slnx` includes and compiles the sample; no-arg run produces the reproducible-board output; `-- frobnicate` prints a usage hint and exits non-zero.

### Implementation for User Story 3

- [X] T018 [US3] Complete subcommand dispatch in `samples/SymbologyBoard/Program.fs`: `none|evidence` → evidence path, `interactive` → `runInteractive`, any other arg → print `symbology-board: unknown subcommand '<x>' (use 'evidence' or 'interactive').` to stderr and exit non-zero (FR-010/US3 scenario 3, cli-contract.md)
- [X] T019 [US3] Verify clean registered-build parity and zero surface drift: `dotnet build FS.GG.Rendering.slnx` shows the sample with zero new errors, and the public-surface gate (`tests/Package.Tests`) shows zero drift on existing baselines — the sample touches no `.fsi`/baseline (FR-008/FR-012/SC-005)
- [X] T020 [US3] Walk the quickstart subcommand checks (`specs/193-symbology-live-board/quickstart.md`): no-arg default → reproducible evidence; explicit `evidence`; `frobnicate` → usage hint + non-zero exit (SC-007)

**Checkpoint**: Sample builds registered, dispatches all subcommands with a clear usage hint, zero surface drift — all three stories independently functional.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Final validation and per-phase feedback capture.

- [X] T021 [P] Run the full `specs/193-symbology-live-board/quickstart.md` end-to-end (build, evidence, interactive/skip, unknown subcommand, tests) and confirm every per-SC validation-map row passes
- [X] T022 [P] Re-run the no-regression baseline (`dotnet fsi scripts/baseline-tests.fsx`) and diff against `specs/193-symbology-live-board/readiness/baseline.md` to confirm zero new reds across all test projects
- [X] T023 Capture per-phase Spec Kit / fs-gg-ui feedback into `specs/193-symbology-live-board/feedback/` via the `fs-gg-feedback-capture` skill (process friction, generalizable-code candidates, severity)

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately.
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories. Includes the early evidence smoke run that must be observed before presentation/test layers are built.
- **User Stories (Phase 3–5)**: All depend on Foundational. US1 (P1) and US2 (P1) are independent of each other and may run in parallel; US3 (P2) depends only on Foundational but is cleanest after US1's `runInteractive` and US2's `evidence` exist (it finalizes dispatch over both).
- **Polish (Phase 6)**: Depends on all desired user stories complete.

### User Story Dependencies

- **US1 (P1)**: After Foundational. Independent of US2/US3.
- **US2 (P1)**: After Foundational. Independent of US1; T016 hardens the minimal evidence dispatch from T007.
- **US3 (P2)**: After Foundational. Finalizes dispatch over the `evidence` (T016) and `interactive` (T012) entry points; verification tasks (T019/T020) assume those exist.

### Within Each User Story

- Tests (T010/T011, T014/T015) are written FIRST and must FAIL before the matching implementation.
- `Roster` before `Board` before `Program` (compile + dependency order).
- Core (Foundational) before presentation (US1) and evidence reporting (US2).
- Story complete and independently testable before moving to the next priority.

### Parallel Opportunities

- T003 (format check) runs parallel to T002 (baseline) in Setup.
- T009 (test-project scaffold) is `[P]` once the Exe builds.
- US1 tests T010/T011 run in parallel; US2 tests T014/T015 run in parallel.
- US1 and US2 (both P1) can be developed in parallel by different developers after Foundational.
- Polish T021/T022 run in parallel.

---

## Parallel Example: User Story 1 + User Story 2 (post-Foundational)

```bash
# Both P1 stories' tests can be written together (different files, independent):
Task: "On-board invariant test in tests/SymbologyBoard.Tests/ (T010)"
Task: "Non-empty-board edge-case test in tests/SymbologyBoard.Tests/ (T011)"
Task: "Same-seed reproducibility test in tests/SymbologyBoard.Tests/ (T014)"
Task: "Different-seed divergence test in tests/SymbologyBoard.Tests/ (T015)"

# Then split the implementation across two developers:
Developer A (US1): runInteractive host + headless fallback in samples/SymbologyBoard/Program.fs (T012)
Developer B (US2): harden evidence subcommand reporting/exit-codes in samples/SymbologyBoard/Program.fs (T016)
```

---

## Implementation Strategy

### MVP First (User Story 1)

1. Complete Phase 1: Setup (register sample + baseline).
2. Complete Phase 2: Foundational — including the **early evidence smoke run** (T008) that proves the board really renders and reproduces in this checkout before any presentation/test layer is built.
3. Complete Phase 3: User Story 1 (live moving board + on-board invariant).
4. **STOP and VALIDATE**: launch interactive (live or environment-limited) and run the US1 tests.
5. Demo the moving board.

### Incremental Delivery

1. Setup + Foundational → deterministic core proven (smoke evidence captured).
2. US1 → moving board, on-board guarantee → demo (MVP).
3. US2 → evidence-grade reproducibility + readiness artifact → milestone evidence.
4. US3 → registered-build parity + discoverable subcommands → polished sample.
5. Polish → quickstart + baseline re-run + feedback capture.

---

## Notes

- [P] = different files, no dependency on incomplete tasks; [Story] maps a task to its user story for traceability.
- This is a Tier 2 additive change: the sample is `IsPackable=false` and consumes existing public API only — no `.fsi`/baseline changes (FR-012/SC-005); T019 verifies zero surface drift.
- Determinism is the hard constraint: nothing in `integrate`/`update`/`renderScene`/`evidence` may read a wall clock, do IO, or use render-time randomness (FR-003).
- Verify each story's tests fail before implementing; commit after each task or logical group; stop at any checkpoint to validate a story independently.
