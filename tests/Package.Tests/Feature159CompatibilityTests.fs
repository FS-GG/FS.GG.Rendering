module Feature159CompatibilityTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature159 compatibility package" [
        test "compatibility ledger documents package surface decisions and claim boundary" {
            let path = repo "specs/159-layer-promotion-keys/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) $"ledger exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "FS.GG.UI.Controls" "Controls surface"
            Expect.stringContains text "FS.GG.UI.SkiaViewer" "SkiaViewer surface"
            Expect.stringContains text "Feature159Readiness" "Testing helper"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }

        test "package validation records command surface and FSI evidence" {
            let path = repo "specs/159-layer-promotion-keys/readiness/package-validation.md"
            Expect.isTrue (File.Exists path) $"package validation exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "compositor-readiness --feature 159" "readiness command"
            Expect.stringContains text "Feature159Readiness" "Testing helper"
        }

        test "validation summary links promotion counters and unsupported-host evidence" {
            let path = repo "specs/159-layer-promotion-keys/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) $"validation summary exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "promotion/summary.md" "promotion summary"
            Expect.stringContains text "counters/promotion.md" "counter evidence"
            Expect.stringContains text "promotion/unsupported/validation.md" "unsupported"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }
    ]
