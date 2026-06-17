# Phase 1 Data Model — Games + Productivity Sample Apps (G2)

Entities are **application-internal** to the sample tree (no public package surface, FR-010). Shapes are
F#-leaning sketches; field names are indicative, not a frozen contract. All state lives in MVU models;
`update` is pure; the PRNG is part of the model so randomness is referentially transparent.

## Shared harness entities

### `SampleEntry` (the closure-erased registry element — research R2)

| Field | Type | Notes |
|---|---|---|
| `Id` | `string` | stable sample id, e.g. `"tetris"`, `"todo"` (CLI selector, evidence dir name) |
| `Family` | `string` | `"game"` \| `"productivity"` |
| `Title` | `string` | human label |
| `Controls` | `string list` | catalog control ids the sample renders (validated vs `Catalog.supportedControls`) |
| `Inputs` | `string list` | subset of `"keyboard"`, `"pointer"`, `"timing-step"` |
| `RunEvidence` | `int -> string -> SampleEvidenceRecord` | `seed -> outDir -> record`; state half needs no GL |
| `Interactive` | `ThemeMode -> int` | GL-gated live run; returns process exit code |
| `Outcome` | `ExpectedOutcome` | authored acceptance outcome (R6) |

`Registry.all : SampleEntry list` — all six. Non-generic (type params hidden in the closures).

### `Prng` (pure seeded RNG — R4)

`{ State: uint64 }` · `seed : int -> Prng` · `next : Prng -> uint32 * Prng` · helpers `nextInt`,
`nextBelow n`, `shuffle`. No `System.Random`/wall-clock. Threaded through each game model.

### `ExpectedOutcome` (authored acceptance criterion — R6)

A small discriminated record the build-outcome suite checks against the run, e.g.:
`{ Kind: string; Values: (string * string) list }` where `Kind` is `"game"`/`"productivity"` and `Values`
holds the pinned facts — e.g. `[ "terminal", "game-over"; "clearedRows", "4"; "score", "1200" ]` for
Tetris, or `[ "committed", "3"; "rejected", "1"; "completed", "2" ]` for Todo. Stored as the sample's
`Outcome`; serialized into the evidence record; asserted in `BuildOutcomeTests`.

### `SampleEvidenceRecord` (per-sample proof — extends G1's schema, R5)

| Field | Type | Notes |
|---|---|---|
| `SampleId` | `string` | |
| `Seed` | `int` | |
| `ProofLevel` | `string` | `"deterministic"` |
| `AuthoritativeFor` | `string list` | e.g. `determinism`, `tree-equality`, `outcome`, `non-blank-offscreen-png` |
| `NotAuthoritativeFor` | `string list` | **non-empty** (FR-007), e.g. `renderer-vs-desktop-pixels`, `live-host`, `timing` |
| `Outcome` | `ExpectedOutcome` | the achieved-and-expected facts (equal ⇒ pass) |
| `Screenshot` | `ScreenshotSummary` | G1 shape: `ProvesScreenshot`, `BlockedStage`, `UnsupportedHostReason`, `Fallback`, `Path` |

Serialized to byte-stable `run.json` + `summary.md`; the `FrameMetrics` count/bool golden to `state.txt`
(timing excluded), as G1.

## Game entities (per sample; illustrative)

### Tetris

`Model = { Board: CellColor[][]; Active: Piece; Bag: PieceKind list; Rng: Prng; Score: int; ClearedRows:
int; Over: bool }` · `Msg = Left | Right | RotateCW | SoftDrop | HardDrop | Gravity` · `update` folds
`Gravity` (from a `Tick`) and key messages; `host.Tick` maps a fixed delta → `Gravity`. Terminal: `Over =
true` when a new piece can't spawn. Randomness: 7-bag refilled via `Prng.shuffle`.

### Snake

`Model = { Snake: Cell list; Dir: Dir; Food: Cell; Rng: Prng; Score: int; Over: bool }` · `Msg = Turn of
Dir | Advance` · `Advance` (from a `Tick`) moves the head; eating grows + reseeds food via `Prng`; self/wall
collision ⇒ `Over`.

### Pong

`Model = { BallX/Y; BallVX/VY; LeftY; RightY; Rng; ScoreL; ScoreR; Over }` · `Msg = MoveLeft of Dir |
MoveRight of Dir | Step` · `Step` (from a `Tick`) integrates ball motion + paddle AI; serve direction from
`Prng`; first to N points ⇒ `Over`. (Continuous motion, not grid — distinct render/input coverage.)

## Productivity entities (per sample; illustrative)

### Kanban

`Model = { Columns: (string * Card list) list; Selected: CardId option; Draft: CardDraft }` · `Msg = AddCard
| MoveCard of CardId*Column | BeginEdit of CardId | CommitEdit of CardId*string | Validate`. Pointer-driven
move (drag); empty board ⇒ empty-state.

### Todo

`Model = { Items: TodoItem list; Draft: TodoDraft; Errors: string list }` · `Msg = AddItem | Toggle of Id |
BeginEdit of Id | CommitEdit of Id*string | DraftChanged of string`. `validate : TodoDraft -> Result<
TodoItem, string list>`; `AddItem` commits only on `Ok` (FR-004/SC-007).

### Calendar

`Model = { Month; Entries: (Date * Entry list) list; Selected: Date; Draft: EntryDraft }` · `Msg = SelectDate
of Date | AddEntry | BeginEdit | CommitEdit | PrevMonth | NextMonth`. Date-grid navigation; validation on
entry add.

## Coverage / backlog entities (R7)

### `CoverageRow`

`{ SampleId: string; Family: string; Controls: string list; Inputs: string list }` — one per curated
sample. Honesty check: every `Controls` id ∈ `Catalog.supportedControls`; `⋃ Inputs ⊇ {keyboard, pointer,
timing-step}`.

### `BacklogEntry`

`{ Spec: string; Family: string; Disposition: string; Reason: string }` — **22** entries (12 games + 10
productivity). `Disposition ∈ {Adopted, Deferred}`; `Adopted` ⇒ a matching `SampleEntry.Id` exists.
Honesty check: exactly 22 entries, no duplicate `Spec`, every entry has a disposition + non-empty reason,
the six `Adopted` match the registry.

## Seeded input script

`FrameInput<'Msg> list` per sample (as G1's `Scripts.fs`): interleaved `Tick(TimeSpan)` (game step / settle)
and `Key(ViewerKey, mods)` (moves / form entry) ending in `Idle`. Fixed deltas ⇒ deterministic frame
sequence ⇒ byte-identical evidence (SC-002). Replayed via `ControlsElmish.Perf.runScript host size script`.

## Relationships

```
Registry.all : SampleEntry list
   ├── each SampleEntry.RunEvidence  ──▶ SampleEvidenceRecord  ──▶ run.json / state.txt / summary.md / frame.png
   ├── each SampleEntry.Outcome       ◀─ authored ExpectedOutcome (R6, PROVENANCE-disclosed)
   ├── each game model carries        ──▶ Prng (seeded by --seed)
   └── Coverage.coverageRows / backlog ─▶ coverage-backlog.md  (honesty-checked vs Catalog + Registry)
```
