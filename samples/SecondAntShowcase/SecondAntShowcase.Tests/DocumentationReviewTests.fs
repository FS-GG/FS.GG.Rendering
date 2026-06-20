module SecondAntShowcase.Tests.DocumentationReviewTests

open System.IO
open Expecto

let private repoRoot =
    Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let private samplePath file =
    Path.Combine(repoRoot, "samples", "SecondAntShowcase", file)

[<Tests>]
let documentationReviewTests =
    testList "DocumentationReview" [
        test "README explains purpose, AntShowcase relationship, commands, and visual review status" {
            let text = File.ReadAllText(samplePath "README.md")
            Expect.stringContains text "second Ant showcase" "purpose is identifiable"
            Expect.stringContains text "not a replacement" "relationship to existing AntShowcase is explicit"
            Expect.stringContains text "samples/AntShowcase" "existing sample is named"
            Expect.stringContains text "docs/product/ant-design/reference/ant-llms-sources.md" "local Ant source hub is cited"
            Expect.stringContains text "visual-readiness" "visual review command is documented"
            Expect.stringContains text "environment-limited" "limitations are disclosed"
            Expect.stringContains text "10-minute maintainer checklist" "documentation review checklist is present"
        }

        test "provenance cites local Ant guidance and package-consumer proof" {
            let text = File.ReadAllText(samplePath "PROVENANCE.md")
            Expect.stringContains text "docs/product/ant-design/reference/ant-llms-sources.md" "Ant hub cited"
            Expect.stringContains text "docs/product/ant-design/README.md" "Ant pattern index cited"
            Expect.stringContains text "~/.local/share/nuget-local" "local feed proof cited"
            Expect.stringContains text "FS.GG.UI.*" "package consumer surface cited"
        }

        // T033 (US4) — the consolidated framework/library report exists, sits under docs/reports/,
        // carries the required Part headers, separates framework from sample-local, and every
        // prioritisation-table row has a non-empty severity and recommendation (framework-report.md
        // Test obligation — a structural check, not a prose review).
        test "feature 176 control-pass report exists with the required structure and a complete prioritisation table" {
            let reportPath =
                Path.Combine(repoRoot, "docs", "reports", "2026-06-20-feature-176-second-antshowcase-control-pass-report.md")

            Expect.isTrue (File.Exists reportPath) "report exists under docs/reports/"
            let text = File.ReadAllText reportPath

            Expect.stringContains text "## Executive summary" "executive summary present"
            Expect.stringContains text "## Background" "background present"
            Expect.stringContains text "## Part 1 — Framework / library" "framework part present"
            Expect.stringContains text "Sample-local fixes" "sample-local fixes separated"
            Expect.stringContains text "## Prioritisation" "prioritisation table present"
            Expect.stringContains text "Suggested phased roadmap" "phased roadmap present"
            Expect.stringContains text "Appendix" "evidence appendix present"
            Expect.stringContains text "specs/176-test-antshowcase-controls" "links back to the feature"

            // Every data row of the prioritisation table carries a non-empty severity (col 4) and
            // leverage/recommendation (col 6), so no framework item ships without triage data.
            let lines = text.Replace("\r\n", "\n").Split('\n')

            let prioritisationRows =
                let startIdx = lines |> Array.findIndex (fun l -> l.Trim() = "## Prioritisation")

                lines
                |> Array.skip (startIdx + 1)
                |> Array.takeWhile (fun l -> not (l.StartsWith "## "))
                |> Array.filter (fun l -> l.TrimStart().StartsWith "|")
                |> Array.filter (fun l -> not (l.Contains "---"))
                |> Array.filter (fun l -> not (l.Contains "| ID |"))

            Expect.isNonEmpty prioritisationRows "the prioritisation table has at least one row"

            for row in prioritisationRows do
                let cells = row.Trim().Trim('|').Split('|') |> Array.map (fun c -> c.Trim())
                Expect.isGreaterThanOrEqual cells.Length 5 (sprintf "row has all columns: %s" row)
                Expect.isFalse (System.String.IsNullOrWhiteSpace cells.[2]) (sprintf "severity is non-empty in: %s" row)
                Expect.isFalse (System.String.IsNullOrWhiteSpace cells.[cells.Length - 1]) (sprintf "leverage/recommendation is non-empty in: %s" row)
        }
    ]
