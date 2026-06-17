module SampleApps.Tests.DeterminismTests

open Expecto
open SampleApps.Core
open SampleApps.Core.Evidence
open SampleApps.Core.Games
open SampleApps.Core.Productivity

/// FR-006 / SC-002 / SC-007: same-seed runs are byte-identical (`run.json` + `state.txt`) and
/// every game reaches its terminal state within the scripted steps (bounded, no hang).
let private tetrisHost () =
    Harness.host (fun () -> Tetris.init (Prng.seed 7)) Tetris.update Tetris.view Tetris.mapKey (fun _ -> None) Tetris.tick SampleTheme.defaultTheme

/// All six pure records at a given seed (run.json determinism spans the whole slice).
let private recordsAt (seed: int) =
    [ Tetris.recordAt seed
      Snake.recordAt seed
      Pong.recordAt seed
      Todo.recordAt seed
      Kanban.recordAt seed
      Calendar.recordAt seed ]

[<Tests>]
let determinismTests =
    testList "Determinism" [
        test "all six samples' run.json is byte-identical across two same-seed runs (SC-002)" {
            for r1, r2 in List.zip (recordsAt 7) (recordsAt 7) do
                Expect.equal (Evidence.toRunJson r1) (Evidence.toRunJson r2) (sprintf "%s run.json deterministic" r1.SampleId)
        }

        test "tetris golden state.txt is byte-identical across two same-seed runs" {
            let host = tetrisHost ()
            let g1 = Harness.goldenStateFor host Tetris.script
            let g2 = Harness.goldenStateFor host Tetris.script
            Expect.equal g1 g2 "state.txt golden deterministic"
        }

        test "the grid/continuous games reach their terminal state within the scripted steps (SC-007)" {
            let tetris = Harness.replay (tetrisHost ()) Tetris.script
            Expect.isTrue tetris.Over "tetris tops out"
            let snakeHost = Harness.host (fun () -> Snake.init (Prng.seed 7)) Snake.update Snake.view Snake.mapKey (fun _ -> None) Snake.tick SampleTheme.defaultTheme
            Expect.isTrue (Harness.replay snakeHost Snake.script).Over "snake reaches a collision"
            let pongHost = Harness.host (fun () -> Pong.init (Prng.seed 7)) Pong.update Pong.view Pong.mapKey (fun _ -> None) Pong.tick SampleTheme.defaultTheme
            Expect.isTrue (Harness.replay pongHost Pong.script).Over "pong reaches match-over"
        }
    ]
