module AntShowcase.Tests.Feature169DiagnosticsCliTests

open System
open System.IO
open Expecto
open AntShowcase.App
open FS.GG.UI.Diagnostics

[<Tests>]
let tests =
    testList "Feature169 AntShowcase diagnostics CLI" [
        test "Synthetic diagnostics command writes JSON Markdown and compact default output" {
            let outDir = Path.Combine(Path.GetTempPath(), "antshowcase-diagnostics-" + Guid.NewGuid().ToString("N"))

            try
                let lines = Diagnostics.render false outDir

                Expect.isLessThanOrEqual lines.Length 12 "default output line budget"
                Expect.isTrue (File.Exists(Path.Combine(outDir, "diagnostics-summary.json"))) "json artifact"
                Expect.isTrue (File.Exists(Path.Combine(outDir, "diagnostics-summary.md"))) "markdown artifact"
                Expect.isTrue (lines |> List.exists (fun line -> line.Contains("Diagnostics: accepted"))) "accepted status"
            finally
                if Directory.Exists outDir then
                    Directory.Delete(outDir, true)
        }

        test "Synthetic diagnostics command verbose output includes detailed records" {
            let outDir = Path.Combine(Path.GetTempPath(), "antshowcase-diagnostics-" + Guid.NewGuid().ToString("N"))

            try
                let compact = Diagnostics.render false outDir
                let verbose = Diagnostics.render true outDir
                let summary = Diagnostics.buildSummary outDir

                Expect.equal summary.Status ReadinessDiagnosticStatus.Accepted "summary accepted"
                Expect.isGreaterThan verbose.Length compact.Length "verbose output has more detail"
                Expect.isTrue (verbose |> List.exists (fun line -> line.Contains("DamageScopedDecision"))) "verbose code"
            finally
                if Directory.Exists outDir then
                    Directory.Delete(outDir, true)
        }
    ]
