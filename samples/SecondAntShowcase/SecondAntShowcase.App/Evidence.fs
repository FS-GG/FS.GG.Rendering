/// Headless deterministic evidence mode (US4). For each page it: (1) replays the seeded
/// script via `Perf.runScript` for the golden state outcome, (2) captures an offscreen
/// screenshot via `captureScreenshotEvidence`, then (3) writes the per-page record. The
/// state half needs no GL, so the run degrades-and-discloses cleanly on a no-GL host
/// (FR-013/SC-005) and always exits 0 on success.
module SecondAntShowcase.App.Evidence

open System
open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.Controls
open FS.GG.UI.Controls.Elmish
open FS.GG.UI.SkiaViewer
open SecondAntShowcase.Core
open SecondAntShowcase.Core.Model

let private size: Size = { Width = 1024; Height = 768 }

let private modelFor (pageId: string) =
    { Host.initModel with CurrentPage = pageId }

/// Render the page's scene and attempt an offscreen screenshot.
let private capture (pageId: string) (outPath: string): ScreenshotEvidenceResult =
    let model = modelFor pageId
    let theme = AntTheme.resolve model.Mode
    let rendered = Control.renderTree theme size (Shell.view size model)
    let scene = SceneNode.Group [ rendered.Scene ]
    let request: ScreenshotEvidenceRequest =
        { Command = "evidence"
          AppOrSample = "second-ant-showcase"
          OutputPath = outPath
          Width = size.Width
          Height = size.Height
          RendererMode = "viewer-render-target"
          CaptureMode = ViewerRenderTargetPng
          HostFacts = []
          Timeout = TimeSpan.FromSeconds 10.0 }
    let options: ViewerOptions =
        { Title = "second-ant-showcase-evidence"
          InitialSize = size
          PresentMode = ViewerPresentMode.OffscreenReadback
          FrameRateCap = None }
    Viewer.captureScreenshotEvidence request options scene

/// Produce and persist one page's evidence record.
let runPage (seed: int) (outDir: string) (page: Page): Evidence.PageEvidenceRecord =
    let dir = Path.Combine(outDir, string seed, page.Id)
    Directory.CreateDirectory(dir) |> ignore

    // 1. deterministic state outcome (no GL needed).
    let script = Scripts.forPage page.Id
    let metrics = ControlsElmish.Perf.runScript Host.defaultHost size script
    File.WriteAllText(Path.Combine(dir, "state.txt"), Evidence.goldenState metrics)

    // 2. screenshot — degrade-and-disclose on any GL/capture failure.
    let framePath = Path.Combine(dir, "frame.png")
    let summary =
        try
            let shot = capture page.Id framePath
            let path = if shot.ProvesScreenshot && File.Exists framePath then Some "frame.png" else None
            Evidence.ofScreenshotResult shot path
        with ex ->
            Evidence.degraded (sprintf "screenshot capture raised: %s" ex.Message)

    // never leave an unproven/stale frame.png on disk.
    if summary.Path.IsNone && File.Exists framePath then
        File.Delete framePath

    // 3. write the record.
    let mode = AntTheme.modeName Host.initModel.Mode
    let record = Evidence.build page.Id seed mode page.ControlIds metrics summary
    File.WriteAllText(Path.Combine(dir, "run.json"), Evidence.toRunJson record)
    File.WriteAllText(Path.Combine(dir, "summary.md"), Evidence.toSummaryMd record)

    if page.Id = "text-numeric-input" then
        let overlay = Evidence.datePickerReferenceOverlayEvidence ()
        let overlayLines =
            [ "# Feature 144 date-picker overlay evidence"
              ""
              sprintf "- replay: %s" (String.concat " -> " overlay.ReplayLog)
              sprintf "- product messages: %s" (String.concat ", " overlay.ProductMessages)
              sprintf "- diagnostics: %s" (if List.isEmpty overlay.Diagnostics then "none" else String.concat ", " overlay.Diagnostics)
              sprintf "- no stale overlay: %b" overlay.NoStaleOverlay ]
        File.WriteAllText(Path.Combine(dir, "feature144-date-picker-overlay.md"), String.concat Environment.NewLine overlayLines + Environment.NewLine)
    record

/// Run evidence over all pages (or one via `pageFilter`). Always exits 0 on success,
/// including disclosed degraded runs (FR-013).
let run (seed: int) (outDir: string) (pageFilter: string option): int =
    let pages =
        match pageFilter with
        | Some id -> PageRegistry.all |> List.filter (fun p -> p.Id = id)
        | None -> PageRegistry.all
    if List.isEmpty pages then
        eprintfn "second-ant-showcase: no page matched '%s'." (Option.defaultValue "" pageFilter)
        2
    else
        let records = pages |> List.map (runPage seed outDir)
        for r in records do
            printfn "  %-22s provesScreenshot=%-5b notAuthoritativeFor=[%s]" r.PageId r.Screenshot.ProvesScreenshot (String.concat "; " r.NotAuthoritativeFor)
        printfn "second-ant-showcase: wrote %d page evidence record(s) under %s/%d" (List.length records) outDir seed
        0
