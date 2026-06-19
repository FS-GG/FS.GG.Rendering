module SecondAntShowcase.Tests.Feature173LiveResponsivenessArtifactTests

open System.IO
open System.Text.Json
open Expecto

[<Tests>]
let tests =
    testList "Feature173 live responsiveness artifacts" [
        test "summary and records use contracted paths and status fields" {
            let code, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            use doc = Feature173LiveResponsivenessFixtures.summaryJson outDir
            use recordDoc = Feature173LiveResponsivenessFixtures.firstRecord outDir
            let root = doc.RootElement
            let record = recordDoc.RootElement

            Expect.equal code 4 "headless run fails closed"
            Expect.equal (root.GetProperty("recordsPath").GetString()) "records.jsonl" "recordsPath is relative"
            Expect.equal (root.GetProperty("artifactWriteStatus").GetString()) "complete" "write status is explicit"
            Expect.equal (record.GetProperty("visibleResponse").GetString()) "environment-limited" "record visible response"
            Expect.equal (record.GetProperty("environmentStatus").GetString()) "headless-substitute" "record environment"
            Expect.equal (record.GetProperty("acceptanceStatus").GetString()) "environment-limited" "record is not accepted"
        }

        test "run directory contains required artifact files" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            let summary = Feature173LiveResponsivenessFixtures.summaryFile outDir
            let runRoot =
                match Directory.GetParent(summary) with
                | null -> failtest "summary has a run directory"
                | parent -> parent.FullName

            Expect.isTrue (File.Exists(Path.Combine(runRoot, "records.jsonl"))) "records written"
            Expect.isTrue (File.Exists(Path.Combine(runRoot, "summary.md"))) "summary markdown written"
            Expect.isTrue (File.Exists(Path.Combine(runRoot, "environment.md"))) "environment markdown written"
            Expect.stringStarts runRoot (Path.GetFullPath(outDir)) "run root stays under out dir"
        }

        test "summary markdown names caveats and linked artifacts" {
            let _, outDir = Feature173LiveResponsivenessFixtures.runHeadlessRequireLive ()
            let summaryPath = Feature173LiveResponsivenessFixtures.summaryFile outDir
            let markdown =
                match Directory.GetParent(summaryPath) with
                | null -> failtest "summary has a run directory"
                | parent -> File.ReadAllText(Path.Combine(parent.FullName, "summary.md"))

            Expect.stringContains markdown "SYNTHETIC deterministic headless substitute" "substitute caveat visible"
            Expect.stringContains markdown "records.jsonl" "records link visible"
            Expect.stringContains markdown "environment.md" "environment link visible"
        }
    ]
