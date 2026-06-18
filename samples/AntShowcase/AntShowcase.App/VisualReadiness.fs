module AntShowcase.App.VisualReadiness

open System
open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.SkiaViewer
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

let reviewerStatus outDir pageIds themeIds =
    let path = Path.Combine(outDir, "reviewer-defects.md")
    if not (File.Exists path) then
        File.WriteAllText(path, CoreEvidence.reviewerDefectTemplate pageIds themeIds)
        "missing", false
    else
        let text = File.ReadAllText(path)
        let hasClassification = not (text.Contains("pending review"))
        let hasCritical = text.Contains("| critical |", StringComparison.OrdinalIgnoreCase) || text.Contains(" critical ", StringComparison.OrdinalIgnoreCase)
        if not hasClassification then "missing", hasCritical
        elif hasCritical then "critical", true
        else "clear", false

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

let buildSummary seed size role pages themeIds (records: CoreEvidence.VisualScreenshotRecord list) contactSheets reviewerStatus critical : CoreEvidence.VisualReadinessSummary =
    let required = (List.length pages) * (List.length themeIds)
    let present = records |> List.filter (fun r -> r.Completeness = "complete" && r.CaptureSource = "real-screenshot") |> List.length
    let completeness = if present = required then "complete" else "incomplete"
    let captureAvailability = if present = required then "available" else "environment-limited"
    let limitations =
        records
        |> List.choose (fun r -> r.DegradedReason |> Option.map (fun reason -> sprintf "%s/%s: %s" r.ThemeId r.PageId reason))
    let readiness =
        if captureAvailability = "environment-limited" then VisualConfig.visualReadinessStatusEnvironmentLimited
        elif completeness <> "complete" then VisualConfig.visualReadinessStatusBlocked
        elif reviewerStatus <> "clear" || critical then VisualConfig.visualReadinessStatusBlocked
        else VisualConfig.visualReadinessStatusAccepted
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
      ContactSheets = contactSheets
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
            let records =
                [ for mode, themeId in themes do
                      for page in pages do
                          captureScreenshot seed outDir size mode themeId page ]
            let completeTheme themeId =
                records
                |> List.filter (fun r -> r.ThemeId = themeId)
                |> List.forall (fun r -> r.Completeness = "complete" && r.CaptureSource = "real-screenshot")
            let contactSheets =
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
                        Some name
                    else None)
            let reviewer, critical = reviewerStatus outDir pageIds themeIds
            let role = VisualConfig.classifySize size
            let summary = buildSummary workflow.Seed workflow.Size role pages themeIds records contactSheets reviewer critical
            writeCompleteness outDir summary records
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
        let outDir = flag "--out" args |> Option.defaultValue "specs/162-enhance-showcase-visuals/readiness"
        Directory.CreateDirectory(outDir) |> ignore
        let minimum =
            flag "--minimum-size" args
            |> Option.map (fun path -> sprintf "- minimum-size evidence: `%s`" path)
            |> Option.defaultValue "- minimum-size evidence: not provided"
        let lines =
            [ "# Feature 162 Visual Readiness Summary"
              ""
              sprintf "- preferred evidence: `%s`" dir
              minimum
              "- package-feed validation: `package-feed.md`"
              "- compatibility ledger: `compatibility-ledger.md`"
              "- regression validation: `regression-validation.md`"
              "- full validation: `full-validation/validation.md`" ]
        File.WriteAllText(Path.Combine(outDir, "validation-summary.md"), String.concat Environment.NewLine lines + Environment.NewLine)
        printfn "ant-showcase: wrote readiness summary under %s" outDir
        0
    | Some dir ->
        eprintfn "ant-showcase: --summarize directory does not exist: %s" dir
        2
    | None -> runCapture args

let run args =
    if hasFlag "--summarize" args then runSummary args else runCapture args
