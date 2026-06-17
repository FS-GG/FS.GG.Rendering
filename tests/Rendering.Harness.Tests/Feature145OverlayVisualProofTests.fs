module Feature145OverlayVisualProofTests

open System
open System.IO
open Expecto
open Rendering.Harness
open SkiaSharp

let writePng (path: string) (nonBlank: bool) =
    match Path.GetDirectoryName path with
    | null
    | "" -> ()
    | directory -> Directory.CreateDirectory directory |> ignore
    use bitmap = new SKBitmap(12, 12)
    use canvas = new SKCanvas(bitmap)
    canvas.Clear(SKColors.White)
    if nonBlank then
        use paint = new SKPaint(Color = SKColors.Black, Style = SKPaintStyle.Fill)
        canvas.DrawRect(2.0f, 2.0f, 6.0f, 6.0f, paint)
    use image = SKImage.FromBitmap(bitmap)
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    use stream = File.Open(path, FileMode.Create, FileAccess.Write)
    data.SaveTo(stream)

let facts backend renderer =
    { EffectiveBackend = backend
      Display = if backend = NoDisplay then None else Some ":99"
      GlRenderer = renderer
      GlVersion = renderer |> Option.map (fun _ -> "4.6")
      GlDirect = Option.isSome renderer
      RefreshHz = Some 60.0
      Extensions = []
      SwapControl = None
      VblankSource = None
      UinputAvailable = false }

let host (status: Evidence.HostCapabilityStatus) : Evidence.HostCapabilityResult =
    { EffectiveBackend = "x11"
      Display = Some ":99"
      GlRenderer = Some "llvmpipe"
      CaptureAvailability = Evidence.CaptureAvailable
      Status = status
      Owner = "Rendering.Harness"
      Cause = "capable"
      NextProofPath = "overlay-visual-proof"
      HostFacts = [ "effective-backend=x11"; "gl-renderer=llvmpipe" ] }

let artifact (readiness: string) (runId: string) (state: Evidence.VisualArtifactState) : Evidence.VisualArtifact =
    let path = Evidence.visualArtifactRelativePath runId state
    writePng (Path.Combine(readiness, path)) true
    { ArtifactId = sprintf "%s-%s" Evidence.feature144DatePickerProofScenario.ScenarioId (Evidence.artifactStateToken state)
      Path = path
      State = state
      Width = 12
      Height = 12
      PixelContentValidation = Evidence.VisualPixelNonBlank
      CaptureSource = Evidence.VisualLiveViewerWindow
      RunId = runId
      ScenarioId = Evidence.feature144DatePickerProofScenario.ScenarioId
      CreatedAt = DateTimeOffset.UtcNow
      OverlayAboveContent = if state = Evidence.OpenOverlay then Some true else None
      TopmostHitTarget = if state = Evidence.OpenOverlay then Some Evidence.feature144DatePickerProofScenario.ExpectedTopmostHitTarget else None
      NoStaleOverlayPixel = if state = Evidence.ClosedOverlay then Some true else None }

let openCorrelation (artifact: Evidence.VisualArtifact) : Evidence.OverlayVisualCorrelation =
    let scenario = Evidence.feature144DatePickerProofScenario
    { ScenarioId = scenario.ScenarioId
      InputStep = scenario.OpenStateStep
      ExpectedOverlayState = Evidence.ExpectedOpen
      TopmostHitTarget = Some scenario.ExpectedTopmostHitTarget
      FocusState = "date-picker-calendar"
      ProductDispatchSummary = "DatePickerOpenChanged:true"
      ReplayLogReference = "Feature144 replay"
      BehavioralEvidenceReference = "AntShowcase.Core.Evidence.datePickerReferenceOverlayEvidence"
      ArtifactPath = artifact.Path
      OverlayAboveContent = Some true
      NoStaleOverlayPixel = None }

let closedCorrelation (artifact: Evidence.VisualArtifact) : Evidence.OverlayVisualCorrelation =
    let scenario = Evidence.feature144DatePickerProofScenario
    { ScenarioId = scenario.ScenarioId
      InputStep = scenario.ClosedStateStep
      ExpectedOverlayState = Evidence.ExpectedClosed
      TopmostHitTarget = None
      FocusState = scenario.ExpectedFocusState
      ProductDispatchSummary = scenario.ExpectedDispatchSummary
      ReplayLogReference = "Feature144 replay"
      BehavioralEvidenceReference = "AntShowcase.Core.Evidence.datePickerReferenceOverlayEvidence"
      ArtifactPath = artifact.Path
      OverlayAboveContent = None
      NoStaleOverlayPixel = Some true }

let tempReadiness () =
    let dir = Path.Combine(Path.GetTempPath(), "feature145-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory dir |> ignore
    dir

[<Tests>]
let tests =
    testList "Feature145 overlay visual proof" [
        test "evidence model exposes stable scenario and artifact tokens" {
            let scenario = Evidence.feature144DatePickerProofScenario

            Expect.equal scenario.ScenarioId "feature144-antshowcase-date-picker-reference" "scenario id is stable"
            Expect.equal scenario.OpenStateStep "open:date-picker-calendar" "open step is stable"
            Expect.equal (Evidence.visualArtifactRelativePath "run-1" Evidence.OpenOverlay) "artifacts/run-1/open.png" "open path is stable"
            Expect.equal (Evidence.visualProofStatusToken Evidence.VisualProofEnvironmentLimited) "environment-limited" "status token is stable"
        }

        test "capable-host proof accepts current-run open and closed artifacts" {
            let readiness = tempReadiness ()
            let runId = "run-capable"
            let scenario = Evidence.feature144DatePickerProofScenario
            let openArtifact = artifact readiness runId Evidence.OpenOverlay
            let closedArtifact = artifact readiness runId Evidence.ClosedOverlay

            let openResult = Evidence.validateVisualArtifact readiness runId scenario openArtifact
            let closedResult = Evidence.validateVisualArtifact readiness runId scenario closedArtifact

            Expect.isTrue openResult.Accepted "open artifact is accepted"
            Expect.isTrue closedResult.Accepted "closed artifact is accepted"
        }

        test "artifact validation rejects missing blank zero-sized stale unreadable and disconnected artifacts" {
            let readiness = tempReadiness ()
            let runId = "run-reject"
            let scenario = Evidence.feature144DatePickerProofScenario
            let valid = artifact readiness runId Evidence.OpenOverlay

            let missing = { valid with Path = Evidence.visualArtifactRelativePath runId Evidence.ClosedOverlay; State = Evidence.ClosedOverlay; TopmostHitTarget = None; NoStaleOverlayPixel = Some true }
            let blank =
                let path = Evidence.visualArtifactRelativePath runId Evidence.ClosedOverlay
                writePng (Path.Combine(readiness, path)) false
                { missing with Path = path; Width = 12; Height = 12 }
            let zeroSized = { valid with Width = 0 }
            let stale = { valid with RunId = "old-run"; Path = Evidence.visualArtifactRelativePath "old-run" Evidence.OpenOverlay }
            let unreadable =
                let path = Evidence.visualArtifactRelativePath runId Evidence.ClosedOverlay
                File.WriteAllText(Path.Combine(readiness, path), "not a png")
                { missing with Path = path; Width = 12; Height = 12 }
            let disconnected = { valid with ScenarioId = "other-scenario" }

            let rejected =
                [ missing; blank; zeroSized; stale; unreadable; disconnected ]
                |> List.map (Evidence.validateVisualArtifact readiness runId scenario)

            Expect.all rejected (fun result -> not result.Accepted) "all invalid artifacts are rejected"
        }

        test "unsupported hosts classify no-display and missing-GL limitations" {
            let noDisplay = Live.classifyOverlayVisualProofHost (facts NoDisplay None)
            let missingGl = Live.classifyOverlayVisualProofHost (facts X11 None)

            Expect.equal noDisplay.Status Evidence.HostUnsupported "no display is unsupported"
            Expect.equal noDisplay.Cause "missing-display" "no display cause is explicit"
            Expect.equal missingGl.Status Evidence.HostUnsupported "missing GL is unsupported"
            Expect.equal missingGl.Cause "missing-gl-renderer" "missing GL cause is explicit"
        }

        test "Synthetic artifacts deterministic logs and unsupported records cannot satisfy real visual proof" {
            let readiness = tempReadiness ()
            let runId = "run-synthetic"
            let scenario = Evidence.feature144DatePickerProofScenario
            // Synthetic: this deliberately uses a valid PNG with a synthetic source to prove source disclosure is rejected.
            let synthetic = { artifact readiness runId Evidence.OpenOverlay with CaptureSource = Evidence.VisualSynthetic }

            let result = Evidence.validateVisualArtifact readiness runId scenario synthetic

            Expect.isFalse result.Accepted "synthetic artifact is rejected"
            Expect.contains result.Diagnostics "synthetic artifacts cannot satisfy real visual proof" "diagnostic names synthetic rejection"
        }

        test "correlation validation rejects artifact state hit target focus and dispatch disagreement" {
            let readiness = tempReadiness ()
            let runId = "run-correlation"
            let scenario = Evidence.feature144DatePickerProofScenario
            let openArtifact = artifact readiness runId Evidence.OpenOverlay
            let mismatch =
                { openCorrelation openArtifact with
                    ExpectedOverlayState = Evidence.ExpectedClosed
                    TopmostHitTarget = Some "covered-content"
                    FocusState = ""
                    ProductDispatchSummary = "" }

            let result = Evidence.validateOverlayVisualCorrelation scenario openArtifact mismatch

            Expect.isFalse result.Accepted "correlation mismatch is rejected"
            Expect.equal result.FailureCategory Evidence.OverlayBehavior "mismatch is classified as overlay behavior"
        }

        test "readiness decisions close gate gate unsupported host or fail classified runs" {
            let readiness = tempReadiness ()
            let runId = "run-decision"
            let openArtifact = artifact readiness runId Evidence.OpenOverlay
            let closedArtifact = artifact readiness runId Evidence.ClosedOverlay
            let passed: Evidence.VisualProofRun =
                { RunId = runId
                  ScenarioId = Evidence.feature144DatePickerProofScenario.ScenarioId
                  HostCapability = host Evidence.HostCapable
                  Status = Evidence.VisualProofPassed
                  OpenArtifact = Some openArtifact
                  ClosedArtifact = Some closedArtifact
                  Correlations = [ openCorrelation openArtifact; closedCorrelation closedArtifact ]
                  FailureCategory = Evidence.NoFailure
                  Limitation = None
                  ReadinessDecision = None }
            let limitation = Evidence.unsupportedHostLimitation { (host Evidence.HostUnsupported) with Cause = "missing-display" }
            let gated = { passed with Status = Evidence.VisualProofEnvironmentLimited; OpenArtifact = None; ClosedArtifact = None; FailureCategory = Evidence.Environment; Limitation = Some limitation }
            let failed = { passed with Status = Evidence.VisualProofFailed; FailureCategory = Evidence.Capture }

            Expect.equal (Evidence.evaluateReadinessCaveat passed).Decision Evidence.CaveatClosed "passed proof closes the caveat"
            Expect.equal (Evidence.evaluateReadinessCaveat gated).Decision Evidence.CaveatEnvironmentGated "unsupported proof keeps gate environment-gated"
            Expect.equal (Evidence.evaluateReadinessCaveat failed).Decision Evidence.CaveatFailed "failed proof does not close the caveat"
        }
    ]
