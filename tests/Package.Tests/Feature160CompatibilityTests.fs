module Feature160CompatibilityTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))

[<Tests>]
let tests =
    testList "Feature160 compatibility package" [
        test "compatibility ledger documents helper surface and claim boundary" {
            let path = repo "specs/160-performance-validation-throughput/readiness/compatibility-ledger.md"
            Expect.isTrue (File.Exists path) $"ledger exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "Feature160ThroughputReadiness" "Testing helper"
            Expect.stringContains text "compositor-performance --feature 160 --lane focused" "focused command"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }

        test "package validation records command surface and FSI evidence" {
            let path = repo "specs/160-performance-validation-throughput/readiness/package-validation.md"
            Expect.isTrue (File.Exists path) $"package validation exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "compositor-readiness --feature 160" "readiness command"
            Expect.stringContains text "Feature160ThroughputReadiness" "Testing helper"
        }

        test "validation summary links throughput full validation and unsupported-host evidence" {
            let path = repo "specs/160-performance-validation-throughput/readiness/validation-summary.md"
            Expect.isTrue (File.Exists path) $"validation summary exists at {path}"
            let text = File.ReadAllText path
            Expect.stringContains text "throughput/summary.md" "throughput summary"
            Expect.stringContains text "full-validation/validation.md" "full validation"
            Expect.stringContains text "throughput/unsupported/README.md" "unsupported"
            Expect.stringContains text "performance-not-accepted" "claim boundary"
        }
    ]
