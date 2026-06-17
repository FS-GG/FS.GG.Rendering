/// The closure-erased sample registry (contracts/sample-registry.md). Exactly six entries
/// in a stable order, grown one per user story (US1 Tetris, US2 Todo, US3 the rest). The
/// `App` edge dispatches by `Id`; the Expecto suites bind to this list.
module SampleApps.Core.Registry

open SampleApps.Core.Harness
open SampleApps.Core.Games
open SampleApps.Core.Productivity

/// All registered samples, in stable order: the three games, then the three productivity
/// apps. Non-generic — each entry's `Model`/`Msg` are erased behind its closures.
let all: SampleEntry list =
    [ Tetris.entry
      Snake.entry
      Pong.entry
      Todo.entry
      Kanban.entry
      Calendar.entry ]
