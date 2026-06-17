# Sample Apps — coverage + backlog

Generated from `Coverage.render ()` (research R7). Do not hand-edit — regenerate via the
`coverage` CLI. Validated by `Coverage.check` / `CoverageBacklogTests`.

## Part A — per-sample coverage

| Sample | Family | Inputs | Controls |
|---|---|---|---|
| tetris | game | keyboard, timing-step | stack, label, custom-control |
| snake | game | keyboard, timing-step | stack, label, custom-control |
| pong | game | keyboard, timing-step | stack, label, custom-control |
| todo | productivity | keyboard, pointer | stack, text-box, check-box, label, button, text-block |
| kanban | productivity | pointer, keyboard | stack, border, text-box, button, label, text-block |
| calendar | productivity | keyboard, pointer | stack, grid, text-box, button, label, text-block |

Input union spans: keyboard, pointer, timing-step.

## Part B — backlog (all 22 archived specs)

| Spec | Family | Disposition | Reason |
|---|---|---|---|
| Tetris | game | Adopted | curated slice — grid + gravity loop + keyboard |
| Snake | game | Adopted | curated slice — grid + directional + step loop |
| Pong | game | Adopted | curated slice — continuous motion + paddle |
| Asteroids | game | Deferred | backlog — coverage already met by Tetris/Snake/Pong |
| Breakout | game | Deferred | backlog |
| Lunar Lander | game | Deferred | backlog |
| Sokoban | game | Deferred | backlog |
| Space Invaders | game | Deferred | backlog |
| Tower Defense | game | Deferred | backlog |
| Top-down Racer | game | Deferred | backlog |
| Bomberman-lite | game | Deferred | backlog |
| Platformer | game | Deferred | backlog |
| Kanban board | productivity | Adopted | curated slice — data grid + pointer move + inline edit |
| Todo/task manager | productivity | Adopted | curated slice — forms + validation + list + inline edit |
| Calendar scheduler | productivity | Adopted | curated slice — date grid + forms |
| Contact manager | productivity | Deferred | backlog — patterns already met by Kanban/Todo/Calendar |
| Expense tracker | productivity | Deferred | backlog |
| File manager | productivity | Deferred | backlog |
| Invoice builder | productivity | Deferred | backlog |
| Markdown notes | productivity | Deferred | backlog |
| Pomodoro timer | productivity | Deferred | backlog |
| Spreadsheet editor | productivity | Deferred | backlog |

6 adopted (the curated slice) · 16 deferred · 22 total.
