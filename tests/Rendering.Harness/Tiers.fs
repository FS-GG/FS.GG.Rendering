namespace Rendering.Harness

open System
open System.IO
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module Tiers =

    // A deterministic, non-blank demo scene: a filled rectangle on the offscreen target.
    let demoScene: SceneNode =
        Rectangle((20.0, 20.0, 160.0, 120.0), Colors.white)

    let capture (path: string) (w: int) (h: int) : ScreenshotEvidenceResult =
        let request: ScreenshotEvidenceRequest =
            { Command = "screenshot"
              AppOrSample = "harness"
              OutputPath = path
              Width = w
              Height = h
              RendererMode = "viewer-render-target"
              CaptureMode = ViewerRenderTargetPng
              HostFacts = []
              Timeout = TimeSpan.FromSeconds 10.0 }
        let options: ViewerOptions =
            { Title = "harness"
              InitialSize = { Width = w; Height = h }
              PresentMode = ViewerPresentMode.OffscreenReadback
              FrameRateCap = None }
        Viewer.captureScreenshotEvidence request options demoScene

    let nonBlank (r: ScreenshotEvidenceResult) =
        match r.PixelContentValidation with
        | PixelContentNonBlank -> true
        | PixelContentBlank -> false
        | PixelContentUnreadable _ -> false
        | PixelContentNotValidated _ -> false

    let runOffscreen (tier: Tier) (facts: ProbeFacts) (outDir: string) : Evidence.Evidence * float list =
        let p = RunPlan.plan tier facts
        let w, h = 200, 160
        Directory.CreateDirectory outDir |> ignore
        let path1 = Path.Combine(outDir, "frame.png")
        let sw = Diagnostics.Stopwatch.StartNew()
        let r1 = capture path1 w h
        sw.Stop()
        let nb = nonBlank r1
        // T0 additionally proves determinism: a second render is byte-identical.
        let deterministic =
            if tier = T0 then
                let path2 = Path.Combine(outDir, "frame2.png")
                capture path2 w h |> ignore
                try File.ReadAllBytes path1 = File.ReadAllBytes path2 with _ -> false
            else true
        let status = if nb && deterministic then RunStatus.Passed else RunStatus.Failed
        let evidence: Evidence.Evidence =
            { RunId = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff")
              Tier = tier
              Subcommand = "offscreen"
              Status = status
              SkipReason = None
              ProofLevel = p.ClaimableProof
              AuthoritativeFor = p.AuthoritativeFor
              NotAuthoritativeFor = p.NotAuthoritativeFor
              Facts = facts
              Frames = 1
              P50Ms = None
              P95Ms = None
              P99Ms = None
              Artifacts = [ "frame.png"; "summary.md" ] }
        evidence, [ sw.Elapsed.TotalMilliseconds ]
