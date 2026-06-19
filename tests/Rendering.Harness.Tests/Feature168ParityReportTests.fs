module Feature168ParityReportTests

open System.IO
open System.Text.Json
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature168 ParityReport" [
        test "passing fixture report Markdown and JSON agree on status and counts" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-report"

            try
                let request = Feature168SkillParityFixtures.request root "passing"
                let report = SkillParity.runCheck request
                let markdown = SkillParity.renderMarkdown report
                let json = SkillParity.renderSummaryJson report

                Expect.stringContains markdown "Overall status: `passed`" "markdown status"
                Expect.stringContains markdown "Guidance Coverage" "coverage section"

                use doc = JsonDocument.Parse json
                let status = doc.RootElement.GetProperty("overallStatus").GetString()
                let high = doc.RootElement.GetProperty("findingCountsBySeverity").GetProperty("high").GetInt32()

                Expect.equal status "passed" "json status"
                Expect.equal high report.FindingCountsBySeverity.High "json high count"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "generated report section preserves manual caveats outside markers" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-report-preserve"

            try
                let request = Feature168SkillParityFixtures.request root "passing"
                match Path.GetDirectoryName request.ReportPath with
                | null
                | "" -> ()
                | directory -> Directory.CreateDirectory directory |> ignore
                File.WriteAllText(request.ReportPath, "Manual reviewer caveat.\n\n<!-- SKILL-PARITY:START -->\nold\n<!-- SKILL-PARITY:END -->\n")

                let report = SkillParity.runCheck request
                SkillParity.writeReport request report |> ignore

                let content = File.ReadAllText request.ReportPath
                Expect.stringContains content "Manual reviewer caveat." "manual text preserved"
                Expect.stringContains content "Overall status: `passed`" "generated text replaced"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "CLI-style JSON report contains first high finding visibility for fixture mode" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-report-high"

            try
                let report = SkillParity.runCheck (Feature168SkillParityFixtures.request root "broken-target")
                let markdown = SkillParity.renderMarkdown report

                Expect.equal report.OverallStatus SkillParity.Failed "broken target fails"
                Expect.stringContains markdown "broken-target" "finding category visible"
                Expect.stringContains markdown "Wrapper target does not resolve" "remediation context visible"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }
    ]
