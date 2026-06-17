# Contract — Coverage + Backlog Report

The honesty artifact (FR-011 / FR-012). A committed `samples/SampleApps/coverage-backlog.md` rendered from
`Coverage.coverageRows` + `Coverage.backlog`, validated by `Coverage.check` and the `CoverageBacklogTests`
suite. Analogue of G1's catalog→page coverage check and the feature 131/132/133 matrix-honesty checks.

## Part A — Per-sample coverage (`coverageRows`)

One row per curated sample: the catalog control ids it renders and the input modalities it exercises.

| Sample | Family | Inputs | Representative controls |
|---|---|---|---|
| tetris | game | keyboard, timing-step | (grid render; status/score labels) |
| snake | game | keyboard, timing-step | (grid render; score label) |
| pong | game | keyboard, timing-step | (continuous render; score labels) |
| kanban | productivity | pointer, keyboard | data/collection, card, button, text input |
| todo | productivity | keyboard, pointer | list, checkbox/toggle, text input, button |
| calendar | productivity | keyboard, pointer | date grid, list, text input, button |

(Exact `Controls` ids are fixed in `Coverage.fs` against the live `Catalog.supportedControls`; the table
above is indicative.)

### Coverage honesty rules

- **R-C1**: every curated `SampleEntry.Id` appears in exactly one `CoverageRow`.
- **R-C2**: every `CoverageRow.Controls` id ∈ `Catalog.supportedControls` (no dangling control reference).
- **R-C3**: `⋃ Inputs` over all rows ⊇ `{ keyboard; pointer; timing-step }` (the slice maximizes distinct
  input coverage, FR-011/SC-004).

## Part B — Backlog (`backlog`) — all 22 archived specs

Every archived game/productivity spec is dispositioned. Source: implementation plan §10 (lines 367–368).

| Spec | Family | Disposition | Reason (abbrev) |
|---|---|---|---|
| Tetris | game | **Adopted** | curated slice — grid + gravity loop + keyboard |
| Snake | game | **Adopted** | curated slice — grid + directional + step loop |
| Pong | game | **Adopted** | curated slice — continuous motion + paddle |
| Kanban board | productivity | **Adopted** | curated slice — data grid + pointer move + inline edit |
| Todo/task manager | productivity | **Adopted** | curated slice — forms + validation + list + inline edit |
| Calendar scheduler | productivity | **Adopted** | curated slice — date grid + forms |
| Asteroids | game | Deferred | backlog — coverage already met by Tetris/Snake/Pong |
| Breakout | game | Deferred | backlog |
| Lunar Lander | game | Deferred | backlog |
| Sokoban | game | Deferred | backlog |
| Space Invaders | game | Deferred | backlog |
| Tower Defense | game | Deferred | backlog |
| Top-down Racer | game | Deferred | backlog |
| Bomberman-lite | game | Deferred | backlog |
| Platformer | game | Deferred | backlog |
| Contact manager | productivity | Deferred | backlog — patterns already met by Kanban/Todo/Calendar |
| Expense tracker | productivity | Deferred | backlog |
| File manager | productivity | Deferred | backlog |
| Invoice builder | productivity | Deferred | backlog |
| Markdown notes | productivity | Deferred | backlog |
| Pomodoro timer | productivity | Deferred | backlog |
| Spreadsheet editor | productivity | Deferred | backlog |

### Backlog honesty rules

- **R-B1**: exactly **22** entries; no duplicate `Spec`.
- **R-B2**: every entry has `Disposition ∈ {Adopted, Deferred}` and a non-empty `Reason`.
- **R-B3**: exactly **6** `Adopted`, and each maps 1:1 to a `Registry.all` entry id (none unaccounted).
- **R-B4**: the 12 game + 10 productivity counts match the plan enumeration.

## `Coverage.check` result

`{ DanglingControls: string list; MissingInputs: string list; UnaccountedSpecs: string list;
   AdoptedMismatch: string list }` — all-empty ⇒ pass (exit `0`); any non-empty ⇒ fail (`coverage` exits
`1`, `CoverageBacklogTests` fails). The committed `coverage-backlog.md` MUST match `Coverage.render ()`
(drift-checked, like the repo's other rendered-report gates).
