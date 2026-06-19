module AntShowcase.App.VisualReadiness

open System
open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.SkiaViewer
open FS.GG.UI.Testing
open SkiaSharp
open AntShowcase.Core
open AntShowcase.Core.Model

module CoreEvidence = AntShowcase.Core.Evidence

let flag name args =
    let rec loop =
        function
        | k :: v :: _ when k = name -> Some v
        | _ :: rest -> loop rest
        | [] -> None
    loop args

let hasFlag name args = args |> List.exists ((=) name)

let splitCsv (text: string): string list =
    text.Split(',', StringSplitOptions.RemoveEmptyEntries ||| StringSplitOptions.TrimEntries) |> Array.toList

let themeFolder themeId =
    match themeId with
    | "antLight" -> "light"
    | "antDark" -> "dark"
    | other -> other

let onePixelPng: byte[] =
    Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/p9sAAAAASUVORK5CYII=")

let writeContactSheet path imagePaths =
    try
        let bitmaps =
            imagePaths
            |> List.choose (fun imagePath ->
                if File.Exists imagePath then
                    let bitmap = SKBitmap.Decode imagePath
                    if isNull bitmap then None else Some bitmap
                else None)
        if List.isEmpty bitmaps then
            File.WriteAllBytes(path, onePixelPng)
        else
            let tileWidth = 320
            let tileHeight = 200
            let columns = 3
            let rows = int (ceil (float bitmaps.Length / float columns))
            use sheet = new SKBitmap(columns * tileWidth, rows * tileHeight)
            use canvas = new SKCanvas(sheet)
            canvas.Clear(SKColors.White)
            bitmaps
            |> List.iteri (fun index bitmap ->
                let col = index % columns
                let row = index / columns
                let dest = SKRect.Create(float32 (col * tileWidth), float32 (row * tileHeight), float32 tileWidth, float32 tileHeight)
                canvas.DrawBitmap(bitmap, dest))
            canvas.Flush()
            use image = SKImage.FromBitmap(sheet)
            use data = image.Encode(SKEncodedImageFormat.Png, 90)
            use stream = File.Open(path, FileMode.Create, FileAccess.Write)
            data.SaveTo(stream)
        bitmaps |> List.iter (fun bitmap -> bitmap.Dispose())
    with _ ->
        File.WriteAllBytes(path, onePixelPng)

let pageSelection pageText =
    let known = PageRegistry.all |> List.map (fun p -> p.Id, p) |> Map.ofList
    match pageText with
    | None -> Result.Ok PageRegistry.all
    | Some text ->
        let requested = splitCsv text
        let unknown = requested |> List.filter (fun id -> not (known.ContainsKey id))
        if not (List.isEmpty unknown) then
            Result.Error(sprintf "unknown page id(s): %s" (String.concat "," unknown))
        else
            Result.Ok(requested |> List.map (fun id -> known[id]))

let reviewerStatus outDir (targets: VisualCaptureTarget list) =
    let path = Path.Combine(outDir, "reviewer-defects.md")
    if not (File.Exists path) then
        File.WriteAllText(path, VisualReviewerClassifications.writeTemplate targets)
        "missing", false, []
    else
        let text = File.ReadAllText(path)
        let parsed = VisualReviewerClassifications.parse text targets
        let hasPending = not parsed.MissingTargetIds.IsEmpty || not parsed.PendingTargetIds.IsEmpty
        let hasBlocking = parsed.Classifications |> List.exists (fun row -> row.Severity = VisualReviewerBlocking)
        let hasMalformed = not parsed.DuplicateTargetIds.IsEmpty || not parsed.UnknownTargetIds.IsEmpty || not parsed.MalformedRows.IsEmpty

        if hasPending then "missing", hasBlocking, parsed.Classifications
        elif hasBlocking || hasMalformed then "critical", true, parsed.Classifications
        else "clear", false, parsed.Classifications

let captureScreenshot seed outDir (size: Size) (mode: ThemeMode) themeId (page: Page): CoreEvidence.VisualScreenshotRecord =
    let folder = themeFolder themeId
    let themeDir = Path.Combine(outDir, folder)
    Directory.CreateDirectory(themeDir) |> ignore
    let outPath = Path.Combine(themeDir, page.Id + ".png")
    let relativePath = folder + "/" + page.Id + ".png"
    let model = { Host.initModel with CurrentPage = page.Id; Mode = mode }
    let theme = AntTheme.resolve mode
    let rendered = Control.renderTree theme size (Shell.view size model)
    let scene = SceneNode.Group [ rendered.Scene ]
    let request: ScreenshotEvidenceRequest =
        { Command = "visual-readiness"
          AppOrSample = "ant-showcase"
          OutputPath = outPath
          Width = size.Width
          Height = size.Height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = [ sprintf "seed=%d" seed; sprintf "theme=%s" themeId; sprintf "page=%s" page.Id ]
          Timeout = TimeSpan.FromSeconds 10.0 }
    let options: ViewerOptions =
        { Title = "ant-showcase-visual-readiness"
          InitialSize = size
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }
    try
        let result = Viewer.captureScreenshotEvidence request options scene
        if result.ProvesScreenshot && File.Exists outPath then
            { PageId = page.Id
              ThemeId = themeId
              Width = size.Width
              Height = size.Height
              RelativePath = relativePath
              CaptureSource = "real-screenshot"
              Completeness = "complete"
              DegradedReason = None }
        else
            if File.Exists outPath then File.Delete outPath
            let reason =
                match result.UnsupportedHostReason with
                | Some text -> text
                | None ->
                    result.BlockedStage
                    |> Option.map (sprintf "%A")
                    |> Option.defaultValue "screenshot capture unavailable"
            { PageId = page.Id
              ThemeId = themeId
              Width = size.Width
              Height = size.Height
              RelativePath = relativePath
              CaptureSource = "degraded"
              Completeness = "degraded"
              DegradedReason = Some reason }
    with ex ->
        if File.Exists outPath then File.Delete outPath
        { PageId = page.Id
          ThemeId = themeId
          Width = size.Width
          Height = size.Height
          RelativePath = relativePath
          CaptureSource = "degraded"
          Completeness = "degraded"
          DegradedReason = Some(sprintf "screenshot capture raised: %s" ex.Message) }

let writeCompleteness outDir (summary: CoreEvidence.VisualReadinessSummary) (records: CoreEvidence.VisualScreenshotRecord list) =
    let dir = Path.Combine(outDir, "completeness")
    Directory.CreateDirectory(dir) |> ignore
    let degraded =
        records
        |> List.filter (fun r -> r.Completeness <> "complete")
        |> List.map (fun r -> sprintf "- `%s` `%s`: %s" r.PageId r.ThemeId (r.DegradedReason |> Option.defaultValue r.Completeness))
    File.WriteAllText(Path.Combine(dir, "summary.md"), sprintf "# Completeness\n\n- status: **%s**\n- present: `%d/%d`\n" summary.CompletenessStatus summary.PresentScreenshotCount summary.RequiredScreenshotCount)
    File.WriteAllText(Path.Combine(dir, "missing.md"), "# Missing\n\nMissing screenshots are represented as degraded capture records in this run.\n")
    File.WriteAllText(Path.Combine(dir, "degraded.md"), "# Degraded\n\n" + (if List.isEmpty degraded then "None\n" else String.concat Environment.NewLine degraded + Environment.NewLine))
    File.WriteAllText(Path.Combine(dir, "dimensions.md"), sprintf "# Dimensions\n\nExpected size: `%s`\n" summary.Size)

let captureRecordFor (records: CoreEvidence.VisualScreenshotRecord list) (target: VisualCaptureTarget): VisualCaptureRecord =
    let screenshot =
        records
        |> List.tryFind (fun record -> record.PageId = target.Page.PageId && record.ThemeId = target.Theme.ThemeId && record.RelativePath = target.RelativePath)

    match screenshot with
    | Some record when record.Completeness = "complete" && record.CaptureSource = "real-screenshot" ->
        { Target = target
          Status = VisualCaptureComplete
          Artifact = None
          ExpectedWidth = target.Size.Width
          ExpectedHeight = target.Size.Height
          ObservedWidth = Some record.Width
          ObservedHeight = Some record.Height
          Reason = None
          Diagnostics = [] }
    | Some record when record.Completeness = "degraded" ->
        VisualCompleteness.degraded target (record.DegradedReason |> Option.defaultValue "degraded capture")
    | Some record ->
        { Target = target
          Status = VisualCaptureBlocked
          Artifact = None
          ExpectedWidth = target.Size.Width
          ExpectedHeight = target.Size.Height
          ObservedWidth = Some record.Width
          ObservedHeight = Some record.Height
          Reason = Some record.Completeness
          Diagnostics = [ record.Completeness ] }
    | None ->
        { Target = target
          Status = VisualCaptureMissing
          Artifact = None
          ExpectedWidth = target.Size.Width
          ExpectedHeight = target.Size.Height
          ObservedWidth = None
          ObservedHeight = None
          Reason = Some "missing screenshot record"
          Diagnostics = [ "missing screenshot record" ] }

let writeContactSheetMetadata outDir (sheets: VisualContactSheet list) =
    let sheetJson (sheet: VisualContactSheet) =
        let q (text: string) = "\"" + text.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\""
        let list values = "[" + (values |> List.map q |> String.concat ", ") + "]"

        String.concat
            "\n"
            [ "  {"
              sprintf "    \"sheetId\": %s," (q sheet.SheetId)
              sprintf "    \"relativePath\": %s," (q sheet.RelativePath)
              sprintf "    \"sizeRole\": %s," (sheet.SizeRole |> Option.map q |> Option.defaultValue "null")
              sprintf "    \"themeId\": %s," (sheet.ThemeId |> Option.map q |> Option.defaultValue "null")
              sprintf "    \"targetIds\": %s," (list sheet.TargetIds)
              sprintf "    \"missingTargetIds\": %s," (list sheet.MissingTargetIds)
              sprintf "    \"diagnostics\": %s" (list sheet.Diagnostics)
              "  }" ]

    File.WriteAllText(Path.Combine(outDir, "contact-sheets.json"), "[\n" + (sheets |> List.map sheetJson |> String.concat ",\n") + "\n]\n")

let buildSummary seed size role pages themeIds (targets: VisualCaptureTarget list) (records: CoreEvidence.VisualScreenshotRecord list) (contactSheetMetadata: VisualContactSheet list) reviewerStatus _critical reviewerClassifications : CoreEvidence.VisualReadinessSummary =
    let captureRecords = targets |> List.map (captureRecordFor records)
    let limitations =
        records
        |> List.choose (fun r -> r.DegradedReason |> Option.map (fun reason -> sprintf "%s/%s: %s" r.ThemeId r.PageId reason))

    let report =
        VisualReadiness.evaluate
            "ant-showcase"
            "."
            targets
            captureRecords
            reviewerClassifications
            contactSheetMetadata
            limitations
            []

    let required = targets |> List.filter _.Required |> List.length
    let present = captureRecords |> List.filter (fun r -> r.Status = VisualCaptureComplete) |> List.length
    let completeness = if present = required then "complete" else "incomplete"
    let captureAvailability =
        if report.ReadinessStatus = VisualReadinessEnvironmentLimited then "environment-limited"
        elif present = required then "available"
        else "environment-limited"

    let readiness =
        match report.ReadinessStatus with
        | VisualReadinessAccepted -> VisualConfig.visualReadinessStatusAccepted
        | VisualReadinessEnvironmentLimited -> VisualConfig.visualReadinessStatusEnvironmentLimited
        | VisualReadinessPendingReview
        | VisualReadinessIncomplete
        | VisualReadinessBlocked -> VisualConfig.visualReadinessStatusBlocked
    { Seed = seed
      Size = VisualConfig.sizeText size
      AcceptedSizeRole = VisualConfig.roleName role
      PageIds = pages |> List.map _.Id
      ThemeIds = themeIds
      RequiredScreenshotCount = required
      PresentScreenshotCount = present
      CompletenessStatus = completeness
      CaptureAvailability = captureAvailability
      ReviewerDefectStatus = reviewerStatus
      VisualReadinessStatus = readiness
      Screenshots = records
      ContactSheets = contactSheetMetadata |> List.map _.RelativePath
      Limitations = limitations }

let writeSummary outDir (summary: CoreEvidence.VisualReadinessSummary) =
    File.WriteAllText(Path.Combine(outDir, "summary.md"), CoreEvidence.visualSummaryToMarkdown summary)
    File.WriteAllText(Path.Combine(outDir, "summary.json"), CoreEvidence.visualSummaryToJson summary)

let runCapture args =
    match flag "--seed" args, flag "--size" args, flag "--themes" args with
    | Some seedText, Some sizeText, Some themeText ->
        match Int32.TryParse seedText, VisualConfig.parseSize sizeText, VisualConfig.resolveThemeList themeText, pageSelection (flag "--pages" args) with
        | (true, seed), Result.Ok size, Result.Ok themes, Result.Ok pages ->
            let outDir = flag "--out" args |> Option.defaultValue "artifacts/ant-showcase-visual-readiness"
            Directory.CreateDirectory(outDir) |> ignore
            let themeIds = themes |> List.map snd
            let pageIds = pages |> List.map _.Id
            let workflow, _ = VisualReadinessWorkflow.init seed size themeIds pageIds outDir
            let sharedTargets = workflow.Targets |> List.map _.SharedTarget
            let records =
                [ for mode, themeId in themes do
                      for page in pages do
                          captureScreenshot seed outDir size mode themeId page ]
            let completeTheme themeId =
                records
                |> List.filter (fun r -> r.ThemeId = themeId)
                |> List.forall (fun r -> r.Completeness = "complete" && r.CaptureSource = "real-screenshot")
            let role = VisualConfig.classifySize size
            let contactSheetMetadata =
                themeIds
                |> List.choose (fun themeId ->
                    if completeTheme themeId then
                        let folder = themeFolder themeId
                        let name = sprintf "contact-sheet-%s.png" folder
                        let imagePaths =
                            records
                            |> List.filter (fun r -> r.ThemeId = themeId && r.Completeness = "complete")
                            |> List.map (fun r -> Path.Combine(outDir, r.RelativePath.Replace("/", string Path.DirectorySeparatorChar)))
                        writeContactSheet (Path.Combine(outDir, name)) imagePaths
                        Some
                            { SheetId = sprintf "ant-showcase-%s-%s" (VisualConfig.roleName role) folder
                              RelativePath = name
                              SizeRole = Some(VisualConfig.roleName role)
                              ThemeId = Some themeId
                              TargetIds = sharedTargets |> List.filter (fun target -> target.Theme.ThemeId = themeId) |> List.map _.TargetId
                              MissingTargetIds = []
                              Diagnostics = [ "contact sheet PNG composition is sample-owned" ] }
                    else None)
            let reviewer, critical, reviewerClassifications = reviewerStatus outDir sharedTargets
            let summary = buildSummary workflow.Seed workflow.Size role pages themeIds sharedTargets records contactSheetMetadata reviewer critical reviewerClassifications
            writeCompleteness outDir summary records
            writeContactSheetMetadata outDir contactSheetMetadata
            writeSummary outDir summary
            printfn "ant-showcase: visual-readiness %s, screenshots %d/%d under %s" summary.VisualReadinessStatus summary.PresentScreenshotCount summary.RequiredScreenshotCount outDir
            0
        | (false, _), _, _, _ ->
            eprintfn "ant-showcase: --seed must be an integer (got '%s')." seedText
            2
        | _, Result.Error error, _, _ ->
            eprintfn "ant-showcase: %s" error
            2
        | _, _, Result.Error error, _ ->
            eprintfn "ant-showcase: %s" error
            2
        | _, _, _, Result.Error error ->
            eprintfn "ant-showcase: %s" error
            2
    | _ ->
        eprintfn "ant-showcase: visual-readiness requires --seed <int> --size <width>x<height> --themes <list>."
        2

let runSummary args =
    match flag "--summarize" args with
    | Some dir when Directory.Exists dir ->
        let outDir = flag "--out" args |> Option.defaultValue "specs/164-shared-visual-readiness/readiness"
        Directory.CreateDirectory(outDir) |> ignore
        let minimum =
            flag "--minimum-size" args
            |> Option.map (fun path -> sprintf "- minimum-size evidence: `%s`" path)
            |> Option.defaultValue "- minimum-size evidence: not provided"
        let generated =
            [ "## Generated Visual Readiness Links"
              ""
              sprintf "- preferred evidence: `%s`" dir
              minimum
              "- package-feed validation: `package-feed.md`"
              "- compatibility ledger: `compatibility-ledger.md`"
              "- regression validation: `regression-validation.md`"
              "- full validation: `full-validation/validation.md`" ]
            |> String.concat Environment.NewLine

        let summaryPath = Path.Combine(outDir, "validation-summary.md")
        let existing =
            if File.Exists summaryPath then
                File.ReadAllText(summaryPath)
            else
                "# Feature 164 Visual Readiness Summary" + Environment.NewLine + Environment.NewLine

        let update = VisualReadinessMarkdown.updateManagedSection existing generated

        if update.SafeToWrite then
            File.WriteAllText(summaryPath, update.UpdatedText)
        else
            eprintfn "ant-showcase: refused to update malformed visual-readiness managed section: %s" (String.concat "; " update.Diagnostics)
            failwith "malformed visual-readiness managed section"

        printfn "ant-showcase: wrote readiness summary under %s" outDir
        0
    | Some dir ->
        eprintfn "ant-showcase: --summarize directory does not exist: %s" dir
        2
    | None -> runCapture args

let run args =
    if hasFlag "--summarize" args then runSummary args else runCapture args
