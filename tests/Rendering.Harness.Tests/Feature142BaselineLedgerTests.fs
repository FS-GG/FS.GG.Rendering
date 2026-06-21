module Feature142BaselineLedgerTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value

[<Tests>]
let tests =
    testList "Feature142 baseline ledger" [
        test "readiness ledger documents intentional shaping deltas and pure fallback status" {
            let path = Path.Combine(root, "specs", "142-harfbuzz-text-shaping", "readiness", "baseline-disclosure-ledger.md")
            Expect.isTrue (File.Exists path) "baseline ledger exists"

            let text = File.ReadAllText path
            Expect.stringContains text "Pure fallback baseline changes: zero" "pure fallback zero-delta is disclosed"
            Expect.stringContains text "SkiaSharp.HarfBuzz" "dependency delta is disclosed"
        }
    ]
