module SampleApps.Tests.BuildOutcomeTests

open Expecto
open SampleApps.Core
open SampleApps.Core.Games
open SampleApps.Core.Productivity

/// FR-009 / SC-001: each sample builds, produces a non-empty disclosed record, and its
/// achieved `Outcome` equals its authored `ExpectedOutcome`. (Extended to Todo in US2 and
/// the full slice + public-surface assertion in US3.)
[<Tests>]
let buildOutcomeTests =
    testList "BuildOutcome" [
        test "tetris produces a non-empty disclosed record" {
            let record = Tetris.recordAt 7
            Expect.equal record.SampleId "tetris" "sampleId set"
            Expect.isNonEmpty record.NotAuthoritativeFor "discloses what it is NOT authoritative for (FR-007)"
            Expect.isNonEmpty record.Outcome.Values "outcome carries pinned facts"
            Expect.contains record.AuthoritativeFor "outcome" "outcome is authoritative (checked headlessly)"
        }

        test "tetris achieved outcome equals the authored expected (seed 7)" {
            let record = Tetris.recordAt 7
            Expect.equal record.Outcome Tetris.expected "record.Outcome = Tetris.expected"
        }

        test "tetris reaches its terminal game-over state" {
            let record = Tetris.recordAt 7
            Expect.contains record.Outcome.Values ("terminal", "game-over") "terminal state pinned in the outcome"
        }

        // The seeded script tops the stack out without completing a row (real-piece holes),
        // so line-clear scoring is proven directly on the pure reducer instead.
        test "tetris clears a full row and scores it (line-clear logic is real)" {
            let board = Array.zeroCreate (10 * 20)
            for c in 0 .. 7 do
                board.[19 * 10 + c] <- 1 // bottom row pre-filled cols 0..7
            let model: Tetris.Model =
                { Board = board
                  Active = { Kind = 1; Rot = 0; Row = 0; Col = 8 } // an O piece over the col 8/9 gap
                  Bag = [ 2 ]
                  Rng = Prng.seed 0
                  Score = 0
                  Cleared = 0
                  Over = false
                  Started = true
                  DropTimer = 0.0 }
            let after = Tetris.update Tetris.HardDrop model
            Expect.equal after.Cleared 1 "one full row cleared"
            Expect.equal after.Score 100 "single-line score awarded"
        }

        test "todo produces a non-empty disclosed record" {
            let record = Todo.recordAt 7
            Expect.equal record.SampleId "todo" "sampleId set"
            Expect.isNonEmpty record.NotAuthoritativeFor "discloses what it is NOT authoritative for (FR-007)"
            Expect.isNonEmpty record.Outcome.Values "outcome carries pinned facts"
        }

        test "todo achieved outcome equals the authored expected (committed/rejected/completed)" {
            let record = Todo.recordAt 7
            Expect.equal record.Outcome Todo.expected "record.Outcome = Todo.expected"
            Expect.contains record.Outcome.Values ("committed", "2") "two valid items committed"
            Expect.contains record.Outcome.Values ("rejected", "1") "one invalid input rejected"
            Expect.contains record.Outcome.Values ("completed", "1") "one item toggled complete"
        }

        // US3: the build-outcome gate now spans the whole curated slice.
        test "all six samples build and meet their authored outcome (FR-009/SC-001)" {
            let cases =
                [ Tetris.recordAt 7, Tetris.expected
                  Snake.recordAt 7, Snake.expected
                  Pong.recordAt 7, Pong.expected
                  Todo.recordAt 7, Todo.expected
                  Kanban.recordAt 7, Kanban.expected
                  Calendar.recordAt 7, Calendar.expected ]
            for record, expected in cases do
                Expect.equal record.Outcome expected (sprintf "%s meets its authored outcome" record.SampleId)
                Expect.isNonEmpty record.NotAuthoritativeFor (sprintf "%s discloses (FR-007)" record.SampleId)
                Expect.isNonEmpty record.Outcome.Values (sprintf "%s outcome non-empty" record.SampleId)
        }

        // FR-010 / SC-006: the consumer tree references ONLY the public FS.GG.UI.* packages —
        // no project reference into src/ would otherwise pull in an internally-named assembly.
        test "Core references only public FS.GG.UI.* package assemblies (no src leak)" {
            let asm = typeof<SampleApps.Core.Harness.SampleEntry>.Assembly
            let fsRefs =
                asm.GetReferencedAssemblies()
                |> Array.choose (fun a -> Option.ofObj a.Name)
                |> Array.filter (fun n -> n.StartsWith "FS.")
            Expect.isNonEmpty fsRefs "references the FS.GG.UI packages"
            for n in fsRefs do
                Expect.isTrue (n.StartsWith "FS.GG.UI.") (sprintf "%s is a public FS.GG.UI.* package (no internal src assembly)" n)
        }
    ]
