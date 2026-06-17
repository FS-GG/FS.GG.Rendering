---
description: "Task list — Games + Productivity Sample Apps (curated G2 slice)"
---

# Tasks: Games + Productivity Sample Apps — curated G2 slice

**Input**: Design documents from `specs/134-sample-apps-g2/`
**Prerequisites**: plan.md, spec.md, research.md (R1–R9), data-model.md, contracts/ (cli, sample-registry,
evidence-record, coverage-backlog), quickstart.md (V1–V8)

**Tests**: INCLUDED. The Expecto suites are the feature's CI-facing deliverable (FR-009/FR-014); test tasks
are first-class here, not optional.

**Organization**: Tasks are grouped by user story. The shared harness (Phases 1–2) is the only cross-story
prerequisite; each sample is an independent MVU app, so once the harness exists the stories can proceed in
parallel. All paths are under `samples/SampleApps/` unless noted. The tree is a **package-only consumer**
(local NuGet feed) **outside `FS.GG.Rendering.slnx`** — building it is the SC-006 proof. **No public product
surface / `.fsi` / token-baseline changes (Tier 2).**

## Format: `[ID] [P?] [Story?] Description with file path`

- **[P]**: parallelizable (different file, no dependency on an incomplete task)
- **[Story]**: US1–US5 for user-story phases; Setup/Foundational/Polish carry none
- F# note: a project's `<Compile Include>` order is significant — author it in dependency order
  (`Prng → SampleTheme → Evidence → Harness → Games/* → Productivity/* → Registry → Coverage`); builds
  happen at checkpoints, not after every file.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Stand up the standalone consumer tree and the two pure leaf modules every sample uses.

- [X] T001 Create the `samples/SampleApps/` tree and copy `samples/ControlsGallery/nuget.config` → `samples/SampleApps/nuget.config` (local feed → `~/.local/share/nuget-local/`, research R1)
- [X] T002 [P] Create `samples/SampleApps/SampleApps.Core/SampleApps.Core.fsproj` — `OutputType=Library`, package refs `FS.GG.UI.{Controls,Color,Controls.Elmish,SkiaViewer,DesignSystem,Themes.Default,KeyboardInput}` @ `0.1.0-preview.1`, **no `ProjectReference` into `src/`** (FR-010)
- [X] T003 [P] Create `samples/SampleApps/SampleApps.App/SampleApps.App.fsproj` — `OutputType=Exe`, `AssemblyName=SampleApps`, `ProjectReference` Core + package refs `Controls.Elmish`/`SkiaViewer`/`Testing`
- [X] T004 [P] Create `samples/SampleApps/SampleApps.Tests/SampleApps.Tests.fsproj` — Expecto + `Microsoft.NET.Test.Sdk` + `YoloDev.Expecto.TestSdk`, `IsPackable=false`, `GenerateProgramFile=false`, `ProjectReference` Core + package refs `Controls`/`Controls.Elmish`/`SkiaViewer`
- [X] T005 [P] Add `artifacts/sample-apps/` to `.gitignore` (evidence output is transient, regenerated per run — mirror the `artifacts/controls-gallery/` entry)
- [X] T006 [P] Implement the pure seeded PRNG in `SampleApps.Core/Prng.fs` — `type Prng`, `seed : int -> Prng`, `next`, `nextBelow`, `shuffle`; no `System.Random`/wall-clock (research R4)
- [X] T007 [P] Implement `SampleApps.Core/SampleTheme.fs` — `resolve : ThemeMode -> Color -> Theme` over `Themes.Default` Light/Dark + consumer-owned accent literals (research R9, mirrors G1 `GalleryTheme`)

**Checkpoint**: tree exists; `Prng`/`SampleTheme` compile in `Core`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: The shared evidence schema, the host bridge + closure-erased `SampleEntry` seam, and the full
App edge + Tests entry — all compiling against an initially-empty registry so every later story just adds
its own sample module + registers it. **No user-story work begins until this phase is done.**

- [X] T008 [P] Implement `SampleApps.Core/Evidence.fs` — port G1's package-only schema (`ScreenshotSummary`, byte-stable `run.json`/`state.txt`/`summary.md` writers, count/bool `FrameMetrics` golden, `degraded`/`ofScreenshotResult`) and **add** the `ExpectedOutcome` + `SampleEvidenceRecord.Outcome` block per `contracts/evidence-record.md` (research R5)
- [X] T009 Implement `SampleApps.Core/Harness.fs` — `ExpectedOutcome` type, the non-generic `SampleEntry` type (`contracts/sample-registry.md`), `host` bridge (generalized G1 `Host.create`, **`Tick` non-None capable**), and `evidenceFor` (replays via `ControlsElmish.Perf.runScript`, derives + compares outcome, captures optional screenshot, builds the record). Depends on T006/T008
- [X] T010 Create `SampleApps.Core/Registry.fs` — `let all : SampleEntry list = []` (grown one entry per story)
- [X] T011 Create `SampleApps.Core/Coverage.fs` skeleton — `coverageRows = []`, `backlog = []`, `check : unit -> CoverageBacklogResult` (all-empty ⇒ pass), `render : unit -> string` (filled in US5). Lets the App `coverage` branch compile
- [X] T012 Implement `SampleApps.App/Evidence.fs` — per-`SampleEntry` headless writer: `Perf.runScript` for `state.txt`, `Viewer.captureScreenshotEvidence` (offscreen) with try/degrade, write `run.json`/`summary.md`/`frame.png` under `artifacts/sample-apps/<seed>/<id>/` (mirror G1 `App/Evidence.fs`, FR-008)
- [X] T013 Implement `SampleApps.App/Interactive.fs` — look up a `SampleEntry` by id and call its `Interactive` closure (`runInteractiveApp host`), GL-gated
- [X] T014 Implement `SampleApps.App/Program.fs` — CLI dispatch `list | interactive <id> | evidence --seed N [--sample id] [--out dir] | coverage [--out file]` with the exit codes in `contracts/cli.md`
- [X] T015 [P] Implement `SampleApps.Tests/Main.fs` — Expecto `[<EntryPoint>]` runner
- [X] T016 Build the tree: `cd samples/SampleApps && dotnet build -c Release` green against the local feed with the empty registry (proves the consumer path resolves, SC-006)

**Checkpoint**: Foundation ready — `list` runs (shows zero samples), build green. User stories can start.

---

## Phase 3: User Story 1 — A game sample runs as a live, deterministic interactive loop (Priority: P1) 🎯 MVP

**Goal**: One complete game (Tetris) with a `Tick`-driven gravity loop, seeded keyboard play, a bounded
terminal state, and a deterministic seeded-evidence run that meets its authored outcome.

**Independent Test**: `evidence --seed 7 --sample tetris` twice → byte-identical; `run.json.outcome` equals
the authored Tetris outcome (terminal `game-over`, pinned cleared-rows/score); `NotAuthoritativeFor`
non-empty (quickstart V2/V3).

- [X] T017 [P] [US1] Implement `SampleApps.Core/Games/Tetris.fs` — `Model` (board/active/7-bag/`Prng`/score/cleared/over), `Msg` (`Left|Right|RotateCW|SoftDrop|HardDrop|Gravity`), `init : Prng -> Model`, pure `update` (folds `Gravity` + keys), `view`, `mapKey`, `tick` (delta → `Gravity`), seeded `script : FrameInput<Msg> list`, authored `expected : ExpectedOutcome`, and `entry : SampleEntry` via `Harness` (data-model.md; research R3/R4/R6)
- [X] T018 [US1] Register `Tetris.entry` in `SampleApps.Core/Registry.fs` (`all = [ Tetris.entry ]`)
- [X] T019 [P] [US1] Create `SampleApps.Tests/BuildOutcomeTests.fs` — assert Tetris builds, produces a non-empty record, and `record.Outcome = Tetris.expected` (FR-009/SC-001)
- [X] T020 [P] [US1] Create `SampleApps.Tests/DeterminismTests.fs` — same-seed run of Tetris is byte-identical (`run.json`+`state.txt`) and reaches its terminal state within the scripted steps (FR-006/SC-002/SC-007)

**Checkpoint**: Tetris MVP — V2/V3 pass headlessly; `interactive tetris` runs where GL is present.

---

## Phase 4: User Story 2 — A productivity sample exercises forms, lists, and inline-edited data (Priority: P1)

**Goal**: One productivity app (Todo) with form validation that rejects invalid input, list + inline edit
that commits to the data model, a defined empty-state, and a deterministic data-state outcome.

**Independent Test**: a seeded script that adds (valid), adds (invalid → rejected), toggles, and inline-edits
yields an `outcome` with the expected committed/rejected/completed counts; byte-identical across two seeds
(quickstart V4).

- [X] T021 [P] [US2] Implement `SampleApps.Core/Productivity/Todo.fs` — `Model` (items/draft/errors), `Msg` (`AddItem|Toggle|BeginEdit|CommitEdit|DraftChanged`), pure `validate : TodoDraft -> Result<TodoItem,string list>` (commit only on `Ok`), `update`, `view` (incl. empty-state), `mapKey`, seeded `script`, `expected`, `entry` (research R8; FR-004/SC-007)
- [X] T022 [US2] Register `Todo.entry` in `SampleApps.Core/Registry.fs`
- [X] T023 [P] [US2] Create `SampleApps.Tests/ValidationTests.fs` — pure-`update` assertions: invalid draft is **not** committed (error surfaced), an inline edit commits to both displayed value and data, empty model renders the empty-state (FR-004/SC-007)
- [X] T024 [US2] Extend `BuildOutcomeTests.fs` to cover Todo (build + `record.Outcome = Todo.expected`)

**Checkpoint**: Todo — V4 passes; the enterprise-pattern half is proven.

---

## Phase 5: User Story 3 — The full curated slice builds and passes its own acceptance criteria (Priority: P1)

**Goal**: All six samples present in the registry, each building against the public surface and meeting its
authored outcome.

**Independent Test**: `list` shows six; the build-outcome suite passes for all six; each compiles
package-only (quickstart V1/V7).

- [X] T025 [P] [US3] Implement `SampleApps.Core/Games/Snake.fs` — grid + directional `Turn` + `Advance` (tick) loop, food via `Prng`, self/wall collision ⇒ terminal; `script`/`expected`/`entry` (data-model.md)
- [X] T026 [P] [US3] Implement `SampleApps.Core/Games/Pong.fs` — continuous ball/paddle `Step` (tick) loop, serve dir via `Prng`, first-to-N ⇒ terminal; `script`/`expected`/`entry`
- [X] T027 [P] [US3] Implement `SampleApps.Core/Productivity/Kanban.fs` — columns + `MoveCard` (pointer) + `BeginEdit`/`CommitEdit`, empty-state; `script`/`expected`/`entry`
- [X] T028 [P] [US3] Implement `SampleApps.Core/Productivity/Calendar.fs` — month/date-grid nav + `AddEntry` with validation; `script`/`expected`/`entry`
- [X] T029 [US3] Register Snake/Pong/Kanban/Calendar entries in `SampleApps.Core/Registry.fs` (`all` now has all six, stable order)
- [X] T030 [US3] Extend `BuildOutcomeTests.fs` to span all six (build + outcome) and add a public-surface-only assertion — the suite/app reference only `FS.GG.UI.*` packages, no `src/` project ref (FR-010/SC-006)

**Checkpoint**: V1 shows six; V7 build-outcome green for the whole slice.

---

## Phase 6: User Story 4 — Determinism and disclosed evidence make the samples CI-trustworthy (Priority: P2)

**Goal**: Across all six samples, same-seed evidence is byte-identical, every record discloses a non-empty
`NotAuthoritativeFor`, and a no-GL host degrades-and-discloses with a clean non-hanging exit.

**Independent Test**: two same-seed runs over the slice diff clean; each `run.json` has a non-empty
`notAuthoritativeFor`; on a no-GL host every record has `provesScreenshot=false` + reason and exit `0`
(quickstart V3/V5).

- [X] T031 [P] [US4] Create `SampleApps.Tests/DegradeTests.fs` — drive each sample's evidence with capture forced unavailable; assert `provesScreenshot=false`, `unsupportedHostReason` set, `fallback="deterministic-state-only"`, no `frame.png`, non-empty `notAuthoritativeFor`, and a success (non-hang) outcome (FR-007/FR-008/SC-003)
- [X] T032 [US4] Extend `DeterminismTests.fs` to the full slice — byte-identity for all six + bounded-terminal assertions for Snake/Pong (FR-006/SC-002/SC-007)

**Checkpoint**: V3/V5 pass; the slice is CI-trustworthy headlessly.

---

## Phase 7: User Story 5 — Coverage is maximized, measured, and the backlog is disclosed (Priority: P2)

**Goal**: A coverage report listing per-sample control/input coverage and dispositioning all 22 archived
specs adopted/deferred, machine-checked for honesty.

**Independent Test**: `coverage` prints the per-sample table (input union spans keyboard+pointer+timing-step)
and a 22-row adopted/deferred table (6 adopted = registry); exit `0`; tamper → exit `1` (quickstart V6).

- [X] T033 [US5] Fill `SampleApps.Core/Coverage.fs` — `coverageRows` (one per sample, controls validated vs `Catalog.supportedControls`), `backlog` (all 22 from `contracts/coverage-backlog.md`), `check` (R-C1–3 + R-B1–4), `render` (the report text)
- [X] T034 [P] [US5] Create `SampleApps.Tests/CoverageBacklogTests.fs` — assert `Coverage.check ()` is all-empty, the 6↔registry adopted mapping, no dangling control id, input union complete, exactly 22 specs / no dup / all dispositioned (FR-011/FR-012/SC-004/SC-005)
- [X] T035 [US5] Generate the committed `samples/SampleApps/coverage-backlog.md` from `Coverage.render ()` and add a drift assertion (committed == rendered) to `CoverageBacklogTests.fs`
- [X] T036 [P] [US5] Write `samples/SampleApps/PROVENANCE.md` — `FS.Skia.UI.* → FS.GG.UI.*` rebrand + the authored-acceptance-outcome disclosure and authoritative fallbacks (research R6, mirrors G1 PROVENANCE)
- [X] T037 [P] [US5] Write `samples/SampleApps/README.md` — curated-slice rationale, the six samples, and the run commands (V1–V8)

**Checkpoint**: V6 passes; coverage + 22-spec backlog honest and machine-checked.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, drift confirmation, and optional provenance record.

- [X] T038 Run the full quickstart V1–V8 from `samples/SampleApps/`; confirm byte-identity (V3), degrade-and-disclose (V5), coverage honesty (V6), and a green Expecto suite (V7)
- [X] T039 Confirm **zero drift** (G2 is a consumer): `git diff tests/surface-baselines/` is empty and `dotnet fsi scripts/generate-design-tokens.fsx --check` reports no drift — both gates stay green with no baseline regen
- [X] T040 [P] *(optional)* Add decision record `docs/product/decisions/0008-g2-sample-apps.md` recording the curated-slice choice + the three G1-divergences (Tick loop, seeded PRNG, closure-erased registry). Tier-2 consumer ⇒ optional, as G1 shipped without one

---

## Dependencies & Execution Order

- **Setup (P1)** → **Foundational (P2)** are strict prerequisites for everything. T009 (Harness) depends on
  T006 (Prng) + T008 (Evidence). The App edge (T012–T014) and Tests entry (T015) depend on the Core seam
  (T009–T011) and compile against the empty registry; T016 gates the foundation.
- **US1 (P3)** is the MVP and depends only on the foundation.
- **US2 (P4)** depends only on the foundation; independent of US1 (different files; both append to
  `Registry.fs` — serialize those two appends, T018 before/after T022).
- **US3 (P5)** adds the remaining four samples; its registry edit (T029) and the build-outcome extension
  (T030) come after US1+US2 entries exist.
- **US4 (P6)** hardens across the whole slice → after US3.
- **US5 (P7)** needs all six entries to compute coverage → after US3.
- **Polish (P8)** last.
- **Registry.fs serialization**: T018, T022, T029 all edit `Registry.fs` — do them in order, not in
  parallel. Everything else marked `[P]` touches a distinct file.

## Parallel opportunities

- **Setup**: T002–T007 are all `[P]` (distinct files) after T001.
- **Foundational**: T008 and T015 are `[P]`; T009–T014 are mostly sequential on the Core seam then the App
  edge.
- **Sample cores**: T017, T021, T025–T028 are all `[P]` (one file each) — the six samples can be authored
  concurrently once the harness exists; only their `Registry.fs` registrations serialize.
- **Tests**: T019/T020 (US1), T023 (US2), T031 (US4), T034 (US5) are `[P]` (distinct test files).

## Implementation strategy

- **MVP = US1 (Tetris)**: foundation + Phase 3. Delivers the hardest new capability (deterministic
  `Tick`-driven game loop + seeded evidence) as a standalone, shippable proof.
- **Increment 2 = US2 (Todo)**: the enterprise-pattern half (forms/validation/inline-edit).
- **Increment 3 = US3**: complete the curated six.
- **Hardening = US4 + US5**: determinism/disclosure across the slice, then coverage + 22-spec backlog
  honesty.
- Build at each checkpoint against the local feed; if a consumed public surface changed, re-pack + clear the
  NuGet cache (the G1/D1 caveat).
