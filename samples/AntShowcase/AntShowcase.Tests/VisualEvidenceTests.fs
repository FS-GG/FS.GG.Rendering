module AntShowcase.Tests.VisualEvidenceTests

open Expecto
open AntShowcase.Core
open AntShowcase.Core.VisualReadinessWorkflow

[<Tests>]
let visualEvidenceTests =
    testList "VisualEvidence" [
        test "workflow expands page and theme matrix" {
            let model, effects = init 1 VisualConfig.preferredSize VisualConfig.supportedThemeIds (PageRegistry.all |> List.map _.Id) "out"
            Expect.equal model.Targets.Length 38 "19 pages x 2 themes"
            Expect.equal effects.Length 38 "one capture effect per target"
        }

        test "degraded capture keeps readiness environment-limited" {
            let model, _ = init 1 VisualConfig.preferredSize [ "antLight" ] [ "display-typography" ] "out"
            let model', _ = update (ScreenshotCaptureDegraded("display-typography", "antLight", "no GL")) model
            Expect.equal model'.Status EnvironmentLimited "degraded capture cannot accept readiness"
        }

        test "complete screenshots without reviewer classification are blocked" {
            let model, _ = init 1 VisualConfig.preferredSize [ "antLight" ] [ "display-typography" ] "out"
            let captured, _ = update (ScreenshotCaptureSucceeded("display-typography", "antLight")) model
            Expect.equal captured.Status Blocked "reviewer classification is required"
        }

        test "visual summary serializers carry canonical status fields" {
            let screenshot: Evidence.VisualScreenshotRecord =
                { PageId = "display-typography"
                  ThemeId = "antLight"
                  Width = 1600
                  Height = 1000
                  RelativePath = "light/display-typography.png"
                  CaptureSource = "real-screenshot"
                  Completeness = "complete"
                  DegradedReason = None }
            let summary: Evidence.VisualReadinessSummary =
                { Seed = 1
                  Size = "1600x1000"
                  AcceptedSizeRole = "preferred"
                  PageIds = [ "display-typography" ]
                  ThemeIds = [ "antLight" ]
                  RequiredScreenshotCount = 1
                  PresentScreenshotCount = 1
                  CompletenessStatus = "complete"
                  CaptureAvailability = "available"
                  ReviewerDefectStatus = "clear"
                  VisualReadinessStatus = "accepted"
                  Screenshots = [ screenshot ]
                  ContactSheets = [ "contact-sheet-light.png" ]
                  Limitations = [] }
            let json = Evidence.visualSummaryToJson summary
            Expect.stringContains json "\"themeIds\": [\"antLight\"]" "canonical theme ids in json"
            Expect.stringContains (Evidence.visualSummaryToMarkdown summary) "visual readiness" "markdown summary produced"
        }

        test "reviewer rubric covers every page/theme pair" {
            let rubric = Evidence.reviewerDefectTemplate [ "p1"; "p2" ] [ "antLight"; "antDark" ]
            Expect.stringContains rubric "| p1 | antLight |" "p1 light row"
            Expect.stringContains rubric "| p2 | antDark |" "p2 dark row"
            Expect.stringContains rubric "shell overlap" "defect classes documented"
        }
    ]
