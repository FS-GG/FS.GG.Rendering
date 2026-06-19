module TestingCapability.Feature164VisualReadinessTests

open System
open System.IO
open Expecto
open SkiaSharp
open FS.GG.UI.Testing

let private pages: VisualPage list =
    [ for order, id in [ 0, "overview"; 1, "details"; 2, "dense" ] ->
          { PageId = id
            Title = id
            Order = order
            Required = true } ]

let private themes: VisualTheme list =
    [ { ThemeId = "light"; Title = "Light"; Order = 0 }
      { ThemeId = "dark"; Title = "Dark"; Order = 1 } ]

let private sizes: VisualSize list =
    [ { Role = "preferred"; Width = 1600; Height = 1000; Order = 0 }
      { Role = "minimum"; Width = 1280; Height = 800; Order = 1 } ]

let private pathFor (page: VisualPage) (theme: VisualTheme) (size: VisualSize) =
    sprintf "%s/%s/%s.png" size.Role theme.ThemeId page.PageId

let private matrix () =
    match VisualCaptureMatrix.expand pages themes sizes pathFor with
    | Ok targets -> targets
    | Result.Error diagnostics -> failtest (String.concat "; " diagnostics)

let private tempDir () =
    let path = Path.Combine(Path.GetTempPath(), "fs-gg-feature164-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(path) |> ignore
    path

let private ensureParentDirectory (path: string) =
    match Path.GetDirectoryName(path) with
    | null
    | "" -> ()
    | parent -> Directory.CreateDirectory(parent) |> ignore

let private writePng (path: string) (width: int) (height: int) =
    // SYNTHETIC: deterministic tiny PNG fixtures avoid requiring a GL host in unit tests.
    ensureParentDirectory path
    use bitmap = new SKBitmap(width, height)
    bitmap.Erase(SKColors.CornflowerBlue)
    use image = SKImage.FromBitmap(bitmap)
    use data = image.Encode(SKEncodedImageFormat.Png, 90)
    use stream = File.Open(path, FileMode.Create, FileAccess.Write)
    data.SaveTo(stream)

let private writeText (path: string) (text: string) =
    ensureParentDirectory path
    File.WriteAllText(path, text)

[<Tests>]
let feature164VisualReadinessTests =
    testList
        "Feature164"
        [ test "matrix expands 3 pages x 2 themes x 2 sizes deterministically" {
              let targets = matrix ()
              Expect.equal targets.Length 12 "expected target count"
              Expect.equal (targets |> List.head |> _.RelativePath) "preferred/light/overview.png" "size/theme/page ordering"
              Expect.equal (targets |> List.last |> _.RelativePath) "minimum/dark/dense.png" "last target ordering"
              Expect.isTrue (targets |> List.forall _.Required) "all required pages produce required targets"
          }

          test "matrix rejects duplicate ids duplicate paths and escaping paths" {
              let duplicatePages = pages @ [ { (List.head pages) with Title = "Duplicate" } ]
              match VisualCaptureMatrix.expand duplicatePages themes sizes pathFor with
              | Result.Error diagnostics -> Expect.exists diagnostics (fun d -> d.Contains("duplicate page id")) "duplicate page rejected"
              | Ok _ -> failtest "duplicate page ids should fail"

              match VisualCaptureMatrix.expand pages themes sizes (fun _ _ _ -> "same.png") with
              | Result.Error diagnostics -> Expect.exists diagnostics (fun d -> d.Contains("duplicate relative path")) "duplicate path rejected"
              | Ok _ -> failtest "duplicate relative paths should fail"

              match VisualCaptureMatrix.expand pages themes sizes (fun _ _ _ -> "../escape.png") with
              | Result.Error diagnostics -> Expect.exists diagnostics (fun d -> d.Contains("escapes evidence root")) "escape path rejected"
              | Ok _ -> failtest "escaping relative paths should fail"
          }

          test "Synthetic PNG completeness classifies complete missing wrong-size corrupt and zero-byte artifacts" {
              let root = tempDir ()
              try
                  let targets = matrix () |> List.take 5
                  writePng (Path.Combine(root, targets[0].RelativePath)) targets[0].Size.Width targets[0].Size.Height
                  writePng (Path.Combine(root, targets[2].RelativePath)) 20 20
                  writeText (Path.Combine(root, targets[3].RelativePath)) "not a png"
                  writeText (Path.Combine(root, targets[4].RelativePath)) ""

                  let records, diagnostics = VisualCompleteness.validate root targets
                  Expect.isEmpty diagnostics "no stale files yet"
                  Expect.equal records[0].Status VisualCaptureComplete "complete PNG"
                  Expect.isSome records[0].Artifact.Value.ContentHash "complete PNG has content identity"
                  Expect.equal records[1].Status VisualCaptureMissing "missing PNG"
                  Expect.equal records[2].Status VisualCaptureWrongSize "wrong-size PNG"
                  Expect.equal records[3].Status VisualCaptureUndecodable "corrupt PNG"
                  Expect.equal records[4].Status VisualCaptureUndecodable "zero-byte PNG"
              finally
                  if Directory.Exists root then Directory.Delete(root, true)
          }

          test "Synthetic degraded records require reasons and stale artifacts are diagnosed" {
              let root = tempDir ()
              try
                  let target = matrix () |> List.head
                  writePng (Path.Combine(root, "stale/outside.png")) 10 10
                  let degraded = VisualCompleteness.degraded target "headless host"
                  let blocked = VisualCompleteness.degraded target " "
                  let _, diagnostics = VisualCompleteness.validate root [ target ]
                  Expect.equal degraded.Status VisualCaptureDegraded "degraded with reason is explicit"
                  Expect.equal blocked.Status VisualCaptureBlocked "degraded without reason is blocked"
                  Expect.exists diagnostics (fun d -> d.Contains("stale artifact")) "stale artifact diagnostic"
              finally
                  if Directory.Exists root then Directory.Delete(root, true)
          }

          test "reviewer template and parser cover missing duplicate malformed unknown pending minor major and blocking rows" {
              let targets = matrix () |> List.take 3
              let template = VisualReviewerClassifications.writeTemplate targets
              for target in targets do
                  Expect.stringContains template target.TargetId "template row"

              let row (target: VisualCaptureTarget) severity impact =
                  sprintf "| %s | %s | %s | %s | %s | none | %s | reviewer | 2026-06-19 | note |" target.TargetId target.Page.PageId target.Theme.ThemeId target.Size.Role severity impact

              let markdown =
                  String.concat
                      Environment.NewLine
                      [ "| targetId | pageId | themeId | size | severity | defectClass | readinessImpact | reviewer | timestamp | notes |"
                        "|---|---|---|---|---|---|---|---|---|---|"
                        row targets[0] "minor" "no-blocker"
                        row targets[0] "major" "needs-review"
                        row targets[1] "blocking" "blocking"
                        "| unknown | p | t | s | none | none | no-blocker | reviewer | now | note |"
                        "| malformed | short | row |" ]

              let parsed = VisualReviewerClassifications.parse markdown targets
              Expect.contains parsed.DuplicateTargetIds targets[0].TargetId "duplicate target"
              Expect.contains parsed.UnknownTargetIds "unknown" "unknown target"
              Expect.isNonEmpty parsed.MalformedRows "malformed row"
              Expect.contains parsed.MissingTargetIds targets[2].TargetId "missing row"
              Expect.exists parsed.Classifications (fun c -> c.Severity = VisualReviewerBlocking) "blocking parsed"
              Expect.exists parsed.Diagnostics (fun d -> d.Contains("duplicate reviewer row")) "diagnostics include duplicate"
          }

          test "readiness gates pending review blocking defect all-clear and default no exceptions" {
              let targets = matrix () |> List.take 2
              let complete target =
                  { Target = target
                    Status = VisualCaptureComplete
                    Artifact = None
                    ExpectedWidth = target.Size.Width
                    ExpectedHeight = target.Size.Height
                    ObservedWidth = Some target.Size.Width
                    ObservedHeight = Some target.Size.Height
                    Reason = None
                    Diagnostics = [] }

              let captures = targets |> List.map complete
              let pending = VisualReadiness.evaluate "run" "root" targets captures [] [] [] []
              Expect.equal pending.ReadinessStatus VisualReadinessPendingReview "review is required"

              let reviews severity =
                  targets
                  |> List.map (fun target ->
                      { TargetId = target.TargetId
                        Severity = severity
                        DefectClass = "none"
                        ReadinessImpact = "no-blocker"
                        Reviewer = "reviewer"
                        ReviewedAt = "2026-06-19"
                        Notes = "ok" })

              let blocked = VisualReadiness.evaluate "run" "root" targets captures (reviews VisualReviewerBlocking) [] [] []
              Expect.equal blocked.ReadinessStatus VisualReadinessBlocked "blocking reviewer defect"

              let accepted = VisualReadiness.evaluate "run" "root" targets captures (reviews VisualReviewerNone) [] [] []
              Expect.equal accepted.ReadinessStatus VisualReadinessAccepted "all-clear accepted"

              let wrongSize = { complete targets[0] with Status = VisualCaptureWrongSize; Diagnostics = [ "wrong-size" ] }
              let noException = VisualReadiness.evaluate "run" "root" targets (wrongSize :: [ complete targets[1] ]) (reviews VisualReviewerNone) [] [] []
              Expect.equal noException.ReadinessStatus VisualReadinessBlocked "exceptions default to none"
          }

          test "contact sheet metadata and Markdown JSON summaries expose counts diagnostics and readiness" {
              let targets = matrix () |> List.take 2
              let captures =
                  targets
                  |> List.map (fun target ->
                      { Target = target
                        Status = VisualCaptureComplete
                        Artifact = None
                        ExpectedWidth = target.Size.Width
                        ExpectedHeight = target.Size.Height
                        ObservedWidth = Some target.Size.Width
                        ObservedHeight = Some target.Size.Height
                        Reason = None
                        Diagnostics = [] })
              let reviews =
                  targets
                  |> List.map (fun target ->
                      { TargetId = target.TargetId
                        Severity = VisualReviewerNone
                        DefectClass = "none"
                        ReadinessImpact = "no-blocker"
                        Reviewer = "reviewer"
                        ReviewedAt = "2026-06-19"
                        Notes = "clear" })
              let sheet =
                  { SheetId = "preferred-light"
                    RelativePath = "contact-sheet-light.png"
                    SizeRole = Some "preferred"
                    ThemeId = Some "light"
                    TargetIds = targets |> List.map _.TargetId
                    MissingTargetIds = []
                    Diagnostics = [ "composed by sample" ] }
              let report = VisualReadiness.evaluate "run" "root" targets captures reviews [ sheet ] [ "manual caveat" ] []
              let markdown = VisualReadinessMarkdown.renderSummary report
              let json = VisualReadinessMarkdown.renderJson report
              Expect.equal report.ReadinessStatus VisualReadinessAccepted "accepted report"
              Expect.stringContains markdown "contact-sheet-light.png" "contact sheet path in markdown"
              Expect.stringContains markdown "manual caveat" "caveat in markdown"
              Expect.stringContains json "\"targetCount\": 2" "target count in json"
              Expect.stringContains json "\"readinessStatus\": \"accepted\"" "readiness status in json"
          }

          test "managed summary regeneration preserves manual content and fails safely for malformed markers" {
              let generatedA = "## Generated\n\n- status: accepted\n"
              let generatedB = "## Generated\n\n- status: blocked\n"
              let manual = "# Summary\n\nmanual before\n\nmanual after\n"
              let inserted = VisualReadinessMarkdown.updateManagedSection manual generatedA
              Expect.isTrue inserted.SafeToWrite "missing markers insert safely"
              Expect.isTrue inserted.InsertedMarkers "inserted markers"
              Expect.stringContains inserted.UpdatedText "manual before" "manual before preserved"
              Expect.stringContains inserted.UpdatedText "manual after" "manual after preserved"
              let updated1 = VisualReadinessMarkdown.updateManagedSection inserted.UpdatedText generatedB
              let updated2 = VisualReadinessMarkdown.updateManagedSection updated1.UpdatedText generatedA
              Expect.isTrue updated2.SafeToWrite "repeat regeneration safe"
              Expect.stringContains updated2.UpdatedText "manual before" "manual before still preserved"
              Expect.stringContains updated2.UpdatedText "manual after" "manual after still preserved"

              let malformed = VisualReadinessMarkdown.startMarker + "\nbody\n" + VisualReadinessMarkdown.startMarker + "\n"
              let result = VisualReadinessMarkdown.updateManagedSection malformed generatedA
              Expect.isFalse result.SafeToWrite "multiple markers fail safely"
              Expect.equal result.UpdatedText malformed "malformed text is unchanged"
          } ]
