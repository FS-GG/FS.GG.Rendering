module TestingCapabilityTests

open System
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Testing
open SkiaSharp

let writePng (path: string) (width: int) (height: int) draw =
    match IO.Path.GetDirectoryName path with
    | null
    | "" -> ()
    | directory -> IO.Directory.CreateDirectory directory |> ignore
    use bitmap = new SKBitmap(width, height)
    use canvas = new SKCanvas(bitmap)
    canvas.Clear(SKColors.White)
    draw canvas
    use image = SKImage.FromBitmap(bitmap)
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    use stream = IO.File.Open(path, IO.FileMode.Create, IO.FileAccess.Write)
    data.SaveTo(stream)

let drawGlyphText (canvas: SKCanvas) (x0: float32) (y0: float32) (scale: float32) (text: string) =
    let patterns =
        Map.ofList
            [ 'H', [ "10001"; "10001"; "10001"; "11111"; "10001"; "10001"; "10001" ]
              'U', [ "10001"; "10001"; "10001"; "10001"; "10001"; "10001"; "01110" ]
              'D', [ "11110"; "10001"; "10001"; "10001"; "10001"; "10001"; "11110" ] ]

    use paint = new SKPaint(Color = SKColors.Black, IsAntialias = false, Style = SKPaintStyle.Fill)

    text
    |> Seq.iteri (fun index character ->
        let pattern = patterns[character]
        let x = x0 + float32 index * scale * 6.0f

        pattern
        |> List.iteri (fun row line ->
            line
            |> Seq.iteri (fun column value ->
                if value = '1' then
                    canvas.DrawRect(SKRect.Create(x + float32 column * scale, y0 + float32 row * scale, scale * 0.86f, scale * 0.86f), paint))))

[<Tests>]
let tests =
    testList "Testing helper contract" [
        test "summaries include profile and packages" {
            let summary =
                GeneratedProductAssertions.summarize
                    { Profile = "app"
                      RequiredFiles = [ "src/Product/Product.fsproj" ]
                      ForbiddenPrefixes = [ "samples/" ]
                      PackageReferences = [ { PackageId = "FS.GG.UI.Scene"; Required = true } ] }

            Expect.stringContains summary "app" "profile is included"
            Expect.stringContains summary "FS.GG.UI.Scene" "package is included"
        }

        test "local consumer package reports include feed snippets restore command and drift" {
            let expected =
                [ { PackageId = "FS.GG.UI.Scene"; Version = "1.2.3"; FeedPath = "/tmp/feed" }
                  { PackageId = "FS.GG.UI.SkiaViewer"; Version = "1.2.3"; FeedPath = "/tmp/feed" } ]

            let actual =
                [ { PackageId = "FS.GG.UI.Scene"; Version = "1.2.2"; FeedPath = "/tmp/feed" } ]

            let drift = LocalConsumerPackages.classifyDrift expected actual
            Expect.hasLength drift 2 "stale and missing packages are both reported"

            let report = LocalConsumerPackages.report "/tmp/feed" expected
            Expect.equal report.FeedPath "/tmp/feed" "feed path is recorded"
            Expect.equal (report.Packages |> List.map _.PackageId) [ "FS.GG.UI.Scene"; "FS.GG.UI.SkiaViewer" ] "generated consumer package set is recorded"
            Expect.stringContains report.ConsumerConfigSnippet "FS.GG.UI.Scene" "package snippet names identities"
            Expect.stringContains report.ConsumerConfigSnippet "1.2.3" "package snippet names versions"
            Expect.isSome report.NuGetConfigSnippet "optional NuGet.config snippet is provided"
            Expect.stringContains (report.NuGetConfigSnippet |> Option.defaultValue "") "/tmp/feed" "NuGet.config snippet names feed path"
            Expect.stringContains report.RestoreCommand "dotnet restore" "restore command is included"
            Expect.exists drift (fun item -> item.PackageId = "FS.GG.UI.Scene" && item.ActualVersion = Some "1.2.2") "stale package drift is reported before generated build failures"
            Expect.exists drift (fun item -> item.PackageId = "FS.GG.UI.SkiaViewer" && item.ActualVersion = None) "missing package drift is reported before generated build failures"
            drift
            |> List.iter (fun item ->
                Expect.stringContains item.RemediationCommand "PackLocal" "drift diagnostics name PackLocal remediation")
        }

        test "evidence report helper writes stable ordered fields and validates unsupported fallback" {
            let root = IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-evidence-report-{Guid.NewGuid():N}")
            let path = IO.Path.Combine(root, "nested", "fs-gg-evidence-report.txt")
            let originalOut = Console.Out
            use capturedOut = new IO.StringWriter()

            let report =
                try
                    Console.SetOut capturedOut
                    EvidenceReports.write
                        { Status = EvidenceUnsupported
                          Command = "dotnet run -- --screenshot-evidence readiness/screenshot-evidence.md"
                          OutputPath = Some path
                          Fields =
                            [ EvidenceReports.field "evidence-kind" "screenshot"
                              EvidenceReports.field "unsupported-host-reason" "viewer host does not expose screenshot capture"
                              EvidenceReports.field "fallback" "deterministic-scene-evidence" ] }
                finally
                    Console.SetOut originalOut

            Expect.equal report.ExitCode 0 "unsupported host facts are classified without product failure"
            Expect.equal (report.Lines |> List.take 3) [ "status=unsupported"; $"command={report.Command}"; $"output={path}" ] "standard fields lead the report"
            Expect.isTrue (IO.Directory.Exists(IO.Path.GetDirectoryName path)) "parent directories are created"
            Expect.sequenceEqual (IO.File.ReadAllLines path) report.Lines "file output matches echoed report lines"
            Expect.sequenceEqual (capturedOut.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries)) report.Lines "stdout output matches report lines"
            Expect.equal (EvidenceReports.statusText EvidenceOk) "ok" "ok status vocabulary is normalized"
            Expect.equal (EvidenceReports.statusText EvidenceUnsupported) "unsupported" "unsupported status vocabulary is normalized"
            Expect.equal (EvidenceReports.statusText EvidenceFailed) "failed" "failed status vocabulary is normalized"

            let validation = EvidenceReports.validate report
            Expect.isTrue validation.Accepted "unsupported evidence report includes required fields"
            Expect.isEmpty validation.MissingFields "all unsupported fields are present"

            let invalidUnsupported =
                EvidenceReports.build
                    { Status = EvidenceUnsupported
                      Command = "--screenshot-evidence"
                      OutputPath = None
                      Fields = [ EvidenceReports.field "evidence-kind" "screenshot" ] }

            let invalidValidation = EvidenceReports.validate invalidUnsupported
            Expect.isFalse invalidValidation.Accepted "unsupported reports must name reason and fallback"
            Expect.containsAll invalidValidation.MissingFields [ "unsupported-host-reason"; "fallback" ] "unsupported required fields are reported"

            let failed =
                EvidenceReports.build
                    { Status = EvidenceFailed
                      Command = "--scene-evidence"
                      OutputPath = None
                      Fields = [] }

            Expect.equal failed.ExitCode 1 "failed reports use non-zero exit"
        }

        test "screenshot evidence report validator accepts live-window success fields" {
            let result =
                EvidenceReports.validateScreenshotEvidence
                    { Status = "ok"
                      Command = Some "--screenshot-evidence"
                      AppOrSample = Some "testing-sample"
                      HostFacts = [ "host=test" ]
                      CaptureMode = Some "viewer-render-target-png"
                      EvidenceKind = Some "screenshot"
                      ArtifactPath = Some "readiness/artifacts/screenshot.png"
                      ScreenshotPath = Some "readiness/artifacts/screenshot.png"
                      Width = Some 320
                      Height = Some 200
                      PixelContentValidation = Some "non-blank"
                      CaptureSource = Some "live-viewer-window"
                      ProvesScreenshot = Some true
                      BlockedStage = Some "none"
                      Classification = Some "none"
                      Category = Some "none"
                      Message = Some "ok"
                      Timestamp = Some DateTimeOffset.UnixEpoch
                      ViewerOpenStatus = Some "confirmed"
                      FirstFrameStatus = Some "presented"
                      CaptureAvailability = Some "available"
                      UnsupportedHostReason = None
                      Fallback = None
                      Diagnostics = [] }

            Expect.isTrue result.Accepted "complete live-window screenshot proof is accepted"
            Expect.isNone result.FailureClass "accepted report has no failure class"
        }

        test "ScreenshotEvidenceReport_Synthetic rejects unsupported records that hide capability detail" {
            // SYNTHETIC: malformed unsupported screenshot report fixture approved by T024 SEH; real path is generated screenshot evidence command validation.
            let result =
                EvidenceReports.validateScreenshotEvidence
                    { Status = "unsupported"
                      Command = Some "--screenshot-evidence"
                      AppOrSample = Some "testing-sample"
                      HostFacts = [ "host=test" ]
                      CaptureMode = Some "viewer-render-target-png"
                      EvidenceKind = Some "screenshot"
                      ArtifactPath = Some "none"
                      ScreenshotPath = Some "readiness/artifacts/screenshot.png"
                      Width = None
                      Height = None
                      PixelContentValidation = Some "not-validated"
                      CaptureSource = Some "live-viewer-window"
                      ProvesScreenshot = Some false
                      BlockedStage = Some "capture"
                      Classification = Some "unsupported"
                      Category = Some "screenshot"
                      Message = Some "unsupported"
                      Timestamp = Some DateTimeOffset.UnixEpoch
                      ViewerOpenStatus = Some "confirmed"
                      FirstFrameStatus = Some "presented"
                      CaptureAvailability = None
                      UnsupportedHostReason = None
                      Fallback = None
                      Diagnostics = [] }

            Expect.isFalse result.Accepted "unsupported screenshot records must keep capability details visible"
            Expect.containsAll result.MissingFields [ "capture-availability"; "unsupported-host-reason"; "fallback" ] "missing capability fields are named"
            Expect.equal result.FailureClass (Some "missing-screenshot-evidence-fields") "missing fields get stable failure class"
        }

        test "ScreenshotEvidenceReport_Synthetic rejects invalid success proof fields" {
            // SYNTHETIC: malformed screenshot report fixture approved by T024 SEH; real path is live screenshot evidence validation.
            let result =
                EvidenceReports.validateScreenshotEvidence
                    { Status = "ok"
                      Command = Some "--screenshot-evidence"
                      AppOrSample = Some "testing-sample"
                      HostFacts = [ "host=test" ]
                      CaptureMode = Some "viewer-render-target-png"
                      EvidenceKind = Some "screenshot"
                      ArtifactPath = None
                      ScreenshotPath = None
                      Width = Some 0
                      Height = Some 200
                      PixelContentValidation = Some "blank"
                      CaptureSource = Some "deterministic-scene-render"
                      ProvesScreenshot = Some false
                      BlockedStage = Some "none"
                      Classification = Some "none"
                      Category = Some "none"
                      Message = Some "invalid"
                      Timestamp = Some DateTimeOffset.UnixEpoch
                      ViewerOpenStatus = Some "confirmed"
                      FirstFrameStatus = Some "presented"
                      CaptureAvailability = Some "available"
                      UnsupportedHostReason = None
                      Fallback = Some "deterministic-scene-evidence"
                      Diagnostics = [] }

            Expect.isFalse result.Accepted "invalid success proof fields are rejected"
            Expect.contains result.MissingFields "artifact-path" "success proof must name artifact path"
            Expect.equal result.FailureClass (Some "missing-screenshot-evidence-fields") "missing success field has stable failure class"
        }

        test "ScreenshotEvidenceReport_Synthetic rejects hidden warnings in successful proof" {
            // SYNTHETIC: hidden-warning fixture approved by T024 SEH; real path is captured launch output classification.
            let result =
                EvidenceReports.validateScreenshotEvidence
                    { Status = "ok"
                      Command = Some "--screenshot-evidence"
                      AppOrSample = Some "testing-sample"
                      HostFacts = [ "host=test" ]
                      CaptureMode = Some "viewer-render-target-png"
                      EvidenceKind = Some "screenshot"
                      ArtifactPath = Some "readiness/artifacts/screenshot.png"
                      ScreenshotPath = Some "readiness/artifacts/screenshot.png"
                      Width = Some 320
                      Height = Some 200
                      PixelContentValidation = Some "non-blank"
                      CaptureSource = Some "live-viewer-window"
                      ProvesScreenshot = Some true
                      BlockedStage = Some "none"
                      Classification = Some "none"
                      Category = Some "none"
                      Message = Some "ok"
                      Timestamp = Some DateTimeOffset.UnixEpoch
                      ViewerOpenStatus = Some "confirmed"
                      FirstFrameStatus = Some "presented"
                      CaptureAvailability = Some "available"
                      UnsupportedHostReason = None
                      Fallback = None
                      Diagnostics = [ "Gtk-Message: Failed to load module \"colorreload-gtk-module\"" ] }

            Expect.isFalse result.Accepted "successful screenshot proof must not hide warning diagnostics"
            Expect.equal result.FailureClass (Some "invalid-screenshot-evidence-fields") "hidden warnings have stable failure class"
        }

        test "ScreenshotEvidenceReport_Synthetic rejects hostile artifact paths" {
            // SYNTHETIC: hostile artifact path fixture approved by T024 SEH; real path is generated report path validation.
            let result =
                EvidenceReports.validateScreenshotEvidence
                    { Status = "ok"
                      Command = Some "--screenshot-evidence"
                      AppOrSample = Some "testing-sample"
                      HostFacts = [ "host=test" ]
                      CaptureMode = Some "viewer-render-target-png"
                      EvidenceKind = Some "screenshot"
                      ArtifactPath = Some "../outside-readiness.png"
                      ScreenshotPath = Some "../outside-readiness.png"
                      Width = Some 320
                      Height = Some 200
                      PixelContentValidation = Some "non-blank"
                      CaptureSource = Some "live-viewer-window"
                      ProvesScreenshot = Some true
                      BlockedStage = Some "none"
                      Classification = Some "none"
                      Category = Some "none"
                      Message = Some "ok"
                      Timestamp = Some DateTimeOffset.UnixEpoch
                      ViewerOpenStatus = Some "confirmed"
                      FirstFrameStatus = Some "presented"
                      CaptureAvailability = Some "available"
                      UnsupportedHostReason = None
                      Fallback = None
                      Diagnostics = [] }

            Expect.isFalse result.Accepted "hostile artifact paths are rejected"
            Expect.equal result.FailureClass (Some "invalid-screenshot-evidence-fields") "hostile paths have stable failure class"
        }

        test "default text glyph evidence accepts glyph-shaped PNG coverage" {
            let root = IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-default-text-{Guid.NewGuid():N}")
            let screenshot = IO.Path.Combine(root, "artifacts", "default-text.png")

            writePng screenshot 160 80 (fun canvas -> drawGlyphText canvas 12.0f 12.0f 7.0f "HUD")

            let result =
                DefaultTextGlyphEvidence.validate
                    { ReadinessDirectory = root
                      ScreenshotPath = screenshot
                      TextRegion = Some { X = 0.0; Y = 0.0; Width = 150.0; Height = 72.0 }
                      ExpectedWidth = Some 160
                      ExpectedHeight = Some 80
                      Status = "ok"
                      FontResolution = Some "SKTypeface.Default"
                      FallbackUsed = Some false
                      UnsupportedHostReason = None
                      Diagnostics = [] }

            let diagnostics = String.concat "; " result.Diagnostics
            Expect.isTrue result.Accepted $"glyph-shaped text should pass: {diagnostics}"
            Expect.isGreaterThan result.GlyphCoverageMetric 0.015 "glyph coverage metric records shape transitions"
            Expect.isLessThan result.SolidBlockMetric 0.82 "glyph text is not classified as a solid block"
            Expect.isLessThan result.PlaceholderMetric 0.55 "glyph text is not classified as tofu-only"
        }

        test "default text glyph evidence rejects solid block and tofu-like screenshots" {
            let root = IO.Path.Combine(IO.Path.GetTempPath(), $"fs-gg-default-text-negative-{Guid.NewGuid():N}")
            let solid = IO.Path.Combine(root, "artifacts", "solid.png")
            let tofu = IO.Path.Combine(root, "artifacts", "tofu.png")

            writePng solid 120 80 (fun canvas ->
                use paint = new SKPaint(Color = SKColors.Black, IsAntialias = false)
                canvas.DrawRect(SKRect.Create(8.0f, 8.0f, 96.0f, 48.0f), paint))

            writePng tofu 120 80 (fun canvas ->
                use paint = new SKPaint(Color = SKColors.Black, IsAntialias = false, Style = SKPaintStyle.Stroke, StrokeWidth = 4.0f)
                canvas.DrawRect(SKRect.Create(20.0f, 12.0f, 80.0f, 48.0f), paint))

            let validate path =
                DefaultTextGlyphEvidence.validate
                    { ReadinessDirectory = root
                      ScreenshotPath = path
                      TextRegion = Some { X = 0.0; Y = 0.0; Width = 120.0; Height = 80.0 }
                      ExpectedWidth = Some 120
                      ExpectedHeight = Some 80
                      Status = "ok"
                      FontResolution = Some "synthetic-negative-fixture"
                      FallbackUsed = Some false
                      UnsupportedHostReason = None
                      Diagnostics = [] }

            let solidResult = validate solid
            Expect.isFalse solidResult.Accepted "solid blocks do not satisfy default text glyph evidence"
            Expect.equal solidResult.FailureClass (Some "solid-block-default-text") "solid block gets stable failure class"

            let tofuResult = validate tofu
            let tofuDiagnostics = String.concat "; " tofuResult.Diagnostics
            Expect.isFalse tofuResult.Accepted $"tofu-like boxes do not satisfy default text glyph evidence: glyph={tofuResult.GlyphCoverageMetric}; solid={tofuResult.SolidBlockMetric}; placeholder={tofuResult.PlaceholderMetric}; diagnostics={tofuDiagnostics}"
            Expect.equal tofuResult.FailureClass (Some "placeholder-default-text") "tofu box gets stable failure class"
        }

        test "known GTK module warnings are benign only with first-frame launch evidence and preserved raw text" {
            let raw = "Gtk-Message: Failed to load module \"colorreload-gtk-module\""
            let check =
                { RawMessage = raw
                  KnownBenignMarkers = [ "colorreload-gtk-module"; "window-decorations-gtk-module" ]
                  LaunchSucceeded = true
                  RenderingSucceeded = true
                  LayoutReadable = Some true
                  ExplicitlyUnsupportedWithoutReadabilityClaim = false
                  PackageSucceeded = true
                  EvidencePath = Some "readiness/host-warning-classification.md" }

            let result = HostWarningClassification.classify check
            Expect.equal result.WarningClass BenignEnvironmentWarning "known GTK warning is benign when launch/render/layout/package facts passed"
            Expect.isFalse result.Fatal "benign warning is not fatal"
            Expect.equal result.RawMessage raw "raw warning text is preserved"

            let missingFirstFrame = HostWarningClassification.classify { check with RenderingSucceeded = false }
            Expect.equal missingFirstFrame.WarningClass RenderingFailure "missing first-frame/render evidence is not hidden by benign marker"
            Expect.isTrue missingFirstFrame.Fatal "rendering failure remains fatal"
        }

        test "known GTK module warning variants are benign with first-frame success" {
            [ "Gtk-Message: Failed to load module \"colorreload-gtk-module\""
              "Gtk-Message: Failed to load module \"window-decorations-gtk-module\"" ]
            |> List.iter (fun raw ->
                let result =
                    HostWarningClassification.classify
                        { RawMessage = raw
                          KnownBenignMarkers = [ "colorreload-gtk-module"; "window-decorations-gtk-module" ]
                          LaunchSucceeded = true
                          RenderingSucceeded = true
                          LayoutReadable = Some true
                          ExplicitlyUnsupportedWithoutReadabilityClaim = false
                          PackageSucceeded = true
                          EvidencePath = Some "readiness/host-warning-classification.md" }

                Expect.equal result.WarningClass BenignEnvironmentWarning $"{raw} is benign after first-frame success"
                Expect.equal result.RawMessage raw "raw GTK warning text is preserved")
        }

        test "known GTK module warnings do not hide mixed unrelated failures" {
            let result =
                HostWarningClassification.classify
                    { RawMessage = "Gtk-Message: Failed to load module \"colorreload-gtk-module\"\nUnhandled renderer exception"
                      KnownBenignMarkers = [ "colorreload-gtk-module"; "window-decorations-gtk-module" ]
                      LaunchSucceeded = true
                      RenderingSucceeded = true
                      LayoutReadable = Some true
                      ExplicitlyUnsupportedWithoutReadabilityClaim = false
                      PackageSucceeded = true
                      EvidencePath = Some "readiness/host-warning-classification.md" }

            Expect.equal result.WarningClass UnknownWarning "mixed unrelated warning/error text remains visible"
            Expect.isTrue result.Fatal "mixed warning result is fatal"
        }

        test "generated consumer validation summaries expose category elapsed command and evidence" {
            let result =
                { Category = Completed
                  Elapsed = TimeSpan.FromSeconds 3.0
                  CommandContext = "./fake.sh build -t GeneratedProductCheck"
                  EvidencePath = Some "readiness/generated-consumer-validation.md"
                  Diagnostics = [ "scene evidence captured" ] }

            let summary = GeneratedConsumerValidation.summarize result
            Expect.stringContains summary "Completed" "category is present"
            Expect.stringContains summary "GeneratedProductCheck" "command context is present"
            Expect.stringContains summary "readiness/generated-consumer-validation.md" "evidence path is present"
            Expect.stringContains summary "scene evidence captured" "scene evidence diagnostics are present"

            let unsupported =
                GeneratedConsumerValidation.summarize
                    { result with
                        Category = GeneratedValidationCategory.UnsupportedHost
                        Diagnostics = [ "bounded viewer smoke unsupported"; "headless scene evidence captured" ] }

            Expect.stringContains unsupported "UnsupportedHost" "unsupported host category is preserved"
            Expect.stringContains unsupported "bounded viewer smoke unsupported" "bounded smoke unsupported diagnostic is summarized"
        }

        test "generated package verification fails NU1603 exact-version drift and missing sources" {
            let requested =
                [ { PackageId = "FS.GG.UI.SkiaViewer"; Version = "0.1.16-persistent.1"; FeedPath = "/tmp/feed" } ]

            let resolved =
                [ { PackageId = "FS.GG.UI.SkiaViewer"; Version = "0.1.16-preview.1"; FeedPath = "/tmp/feed" } ]

            let result =
                GeneratedConsumerValidation.verifyPackageResolution
                    { RequestedPackages = requested
                      ResolvedPackages = resolved
                      PackageSources = []
                      RestoreWarnings = [ "NU1603: FS.GG.UI.SkiaViewer 0.1.16-persistent.1 was not found" ] }

            Expect.isFalse result.ExactMatch "NU1603 prevents exact package verification"
            Expect.equal result.FailureReason (Some "NU1603") "NU1603 is the primary failure class"
            Expect.exists result.Diagnostics (fun item -> item.Contains "missing package sources") "missing sources are reported"
            Expect.exists result.Diagnostics (fun item -> item.Contains "package mismatch") "requested/resolved drift is reported"
        }

        test "generated package verification accepts exact requested resolved versions and configured sources" {
            let requested =
                [ { PackageId = "FS.GG.UI.Scene"; Version = "0.1.16-persistent.1"; FeedPath = "/tmp/feed" }
                  { PackageId = "FS.GG.UI.SkiaViewer"; Version = "0.1.16-persistent.1"; FeedPath = "/tmp/feed" }
                  { PackageId = "FS.GG.UI.Testing"; Version = "0.1.16-persistent.1"; FeedPath = "/tmp/feed" } ]

            let result =
                GeneratedConsumerValidation.verifyPackageResolution
                    { RequestedPackages = requested
                      ResolvedPackages = requested
                      PackageSources = [ "/tmp/feed"; "https://api.nuget.org/v3/index.json" ]
                      RestoreWarnings = [] }

            Expect.isTrue result.ExactMatch "exact requested/resolved package versions are authoritative"
            Expect.isNone result.FailureReason "no package failure class is reported for exact resolution"
            Expect.isEmpty result.Diagnostics "exact package resolution with configured sources has no diagnostics"
        }

        test "generated package verification reports version mismatch failure when NU1603 is absent" {
            let requested =
                [ { PackageId = "FS.GG.UI.Testing"; Version = "0.1.16-persistent.1"; FeedPath = "/tmp/feed" } ]

            let resolved =
                [ { PackageId = "FS.GG.UI.Testing"; Version = "0.1.16-preview.1"; FeedPath = "/tmp/feed" } ]

            let result =
                GeneratedConsumerValidation.verifyPackageResolution
                    { RequestedPackages = requested
                      ResolvedPackages = resolved
                      PackageSources = [ "/tmp/feed" ]
                      RestoreWarnings = [] }

            Expect.isFalse result.ExactMatch "requested/resolved drift blocks exact package verification"
            Expect.equal result.FailureReason (Some "version-mismatch") "version mismatch has its own failure class"
            Expect.exists result.Diagnostics (fun item -> item.Contains "requested=0.1.16-persistent.1") "requested version is reported"
            Expect.exists result.Diagnostics (fun item -> item.Contains "resolved=0.1.16-preview.1") "resolved version is reported"
        }

        test "generated verification is non-authoritative when tests exist but do not run" {
            let result =
                GeneratedConsumerValidation.verifyGeneratedTests
                    { TestsExist = true
                      TestsRan = false
                      VerifyRan = true }

            Expect.isFalse result.Authoritative "verify is not authoritative when generated tests are skipped"
            Expect.equal result.NonAuthoritativeReason (Some "missing-generated-test-execution") "missing generated tests are classified"
            Expect.exists result.Diagnostics (fun item -> item.Contains "did not run") "diagnostic explains skipped tests"
        }

        test "generated verification is authoritative only when generated tests run through Verify" {
            let result =
                GeneratedConsumerValidation.verifyGeneratedTests
                    { TestsExist = true
                      TestsRan = true
                      VerifyRan = true }

            Expect.isTrue result.Authoritative "generated tests run through Verify are authoritative"
            Expect.isNone result.NonAuthoritativeReason "authoritative generated verification has no failure class"
            Expect.isEmpty result.Diagnostics "authoritative generated verification has no diagnostics"
        }

        test "generated verification is non-authoritative when tests bypass Verify" {
            let result =
                GeneratedConsumerValidation.verifyGeneratedTests
                    { TestsExist = true
                      TestsRan = true
                      VerifyRan = false }

            Expect.isFalse result.Authoritative "generated tests outside Verify do not prove generated Verify coverage"
            Expect.equal result.NonAuthoritativeReason (Some "verify-target-not-authoritative") "Verify bypass has its own failure class"
            Expect.exists result.Diagnostics (fun item -> item.Contains "outside generated Verify") "diagnostic names Verify bypass"
        }

        test "generated product validation requires interactive default launch and rejects bounded-only substitutes" {
            let validSource =
                """
[<EntryPoint>]
let main args =
    match List.ofArray args with
    | "--launch-evidence" :: path :: _ -> launchEvidence path
    | _ ->
        match Viewer.runApp viewerOptions generatedHost with
        | Result.Ok outcome ->
            printfn "status=%s mode=interactive-window accessible-window=true window-visible=observed:true" outcome.Status
            0
        | Result.Error _ -> 1
"""

            let invalidSource =
                """
[<EntryPoint>]
let main args =
    match List.ofArray args with
    | _ ->
        let evidence = Viewer.runBounded request viewerOptions scene
        printfn "mode=persistent-evidence self-closed-for-evidence=true print metadata"
        0
"""

            let valid = GeneratedProductAssertions.validateDefaultInteractiveLaunch validSource
            Expect.isTrue valid.InteractiveLaunchRequired "interactive runApp default path is accepted"
            Expect.isEmpty valid.Diagnostics "valid default launch has no diagnostics"

            let invalid = GeneratedProductAssertions.validateDefaultInteractiveLaunch invalidSource
            Expect.isFalse invalid.InteractiveLaunchRequired "bounded-only default path is rejected"
            Expect.exists invalid.Diagnostics (fun item -> item.Contains "Viewer.runApp") "missing runApp is diagnostic"
            Expect.exists invalid.Diagnostics (fun item -> item.Contains "Viewer.runBounded") "bounded substitute is diagnostic"
            Expect.exists invalid.Diagnostics (fun item -> item.Contains "self-close") "evidence self-close is diagnostic"
            Expect.exists invalid.Diagnostics (fun item -> item.Contains "metadata-only") "metadata-only default is diagnostic"
        }

        test "generated product validation rejects first-frame metadata and inaccessible default commands" {
            let invalidDefaults =
                [ ("first-frame-only",
                   "match List.ofArray args with\n| _ ->\n    printfn \"mode=interactive-window first-frame-only=true exit after first frame\"\n    0")
                  ("metadata-only",
                   "match List.ofArray args with\n| _ ->\n    printfn \"mode=interactive-window accessible-window=true print metadata\"\n    0")
                  ("missing-accessible-window",
                   "match List.ofArray args with\n| _ ->\n    match Viewer.runApp viewerOptions generatedHost with\n    | Result.Ok outcome ->\n        printfn \"status=%s mode=interactive-window\" outcome.Status\n        0\n    | Result.Error _ -> 1") ]

            invalidDefaults
            |> List.iter (fun (caseName, source) ->
                let result = GeneratedProductAssertions.validateDefaultInteractiveLaunch source
                Expect.isFalse result.InteractiveLaunchRequired $"{caseName} default command is rejected")

            let firstFrame =
                GeneratedProductAssertions.validateDefaultInteractiveLaunch (invalidDefaults |> List.item 0 |> snd)

            Expect.exists firstFrame.Diagnostics (fun item -> item.Contains "first frame") "first-frame-only default is diagnostic"

            let metadataOnly =
                GeneratedProductAssertions.validateDefaultInteractiveLaunch (invalidDefaults |> List.item 1 |> snd)

            Expect.exists metadataOnly.Diagnostics (fun item -> item.Contains "metadata-only") "metadata-only default is diagnostic"

            let missingAccessible =
                GeneratedProductAssertions.validateDefaultInteractiveLaunch (invalidDefaults |> List.item 2 |> snd)

            Expect.exists missingAccessible.Diagnostics (fun item -> item.Contains "accessible desktop window") "missing accessible-window claim is diagnostic"
        }

        test "generated layout validation accepts complete readable layout reports" {
            let report =
                { Scene = Scene.empty
                  OutputSize = { Width = 640; Height = 480 }
                  ProofLevel = ReadableLayout
                  HudRegion = Some { Name = "hud"; Bounds = { X = 0.0; Y = 0.0; Width = 640.0; Height = 96.0 } }
                  GameplayRegion = Some { Name = "gameplay"; Bounds = { X = 0.0; Y = 96.0; Width = 640.0; Height = 384.0 } }
                  TextBounds = [ { Name = "score"; Text = "score"; Bounds = { X = 16.0; Y = 16.0; Width = 80.0; Height = 24.0 }; MeasurementMode = ExactTextBounds } ]
                  GameplayBounds = [ { Name = "ship"; Bounds = { X = 120.0; Y = 160.0; Width = 24.0; Height = 24.0 } } ]
                  OverlapStatus = NoLayoutOverlap
                  MeasurementMode = ExactTextBounds
                  UnsupportedReasons = []
                  Diagnostics = []
                  RenderEvidence = None }

            let result = GeneratedLayoutValidation.validate { Report = report; RequireReadableLayout = true }
            Expect.isTrue result.Accepted "complete layout report is accepted"
            Expect.isNone result.FailureClass "accepted report has no failure class"
        }

        test "generated layout validation rejects missing unsupported overlapping and deterministic-only reports" {
            let deterministic =
                LayoutEvidence.fromRenderEvidence Scene.empty (Scene.renderReadbackEvidence { Width = 640; Height = 480 } Scene.empty)

            let unsupported =
                LayoutEvidence.unsupported
                    Scene.empty
                    { Width = 640; Height = 480 }
                    { Fact = "font-metrics"; Reason = "host metrics unavailable"; Diagnostic = "unsupported layout inspection" }

            let overlapping =
                LayoutEvidence.classify
                    { deterministic with
                        HudRegion = Some { Name = "hud"; Bounds = { X = 0.0; Y = 0.0; Width = 640.0; Height = 96.0 } }
                        GameplayRegion = Some { Name = "gameplay"; Bounds = { X = 0.0; Y = 96.0; Width = 640.0; Height = 384.0 } }
                        TextBounds = [ { Name = "score"; Text = "score"; Bounds = { X = 16.0; Y = 16.0; Width = 80.0; Height = 24.0 }; MeasurementMode = ApproximateTextBounds } ]
                        GameplayBounds = [ { Name = "ship"; Bounds = { X = 16.0; Y = 16.0; Width = 24.0; Height = 24.0 } } ]
                        RenderEvidence = None }

            let deterministicResult = GeneratedLayoutValidation.validate { Report = deterministic; RequireReadableLayout = true }
            let unsupportedResult = GeneratedLayoutValidation.validate { Report = unsupported; RequireReadableLayout = true }
            let overlappingResult = GeneratedLayoutValidation.validate { Report = overlapping; RequireReadableLayout = true }

            Expect.equal deterministicResult.FailureClass (Some DeterministicRenderOnlyClaim) "deterministic render only cannot satisfy readability"
            Expect.equal unsupportedResult.FailureClass (Some UnsupportedLayoutFacts) "unsupported facts are classified"
            Expect.equal overlappingResult.FailureClass (Some OverlappingLayoutBounds) "overlap is classified"
        }

        test "host warning classification keeps benign environment warnings non-fatal only after supporting checks pass" {
            let baseline =
                { RawMessage = "libEGL warning: failed to open swrast"
                  KnownBenignMarkers = [ "libEGL warning" ]
                  LaunchSucceeded = true
                  RenderingSucceeded = true
                  LayoutReadable = Some true
                  ExplicitlyUnsupportedWithoutReadabilityClaim = false
                  PackageSucceeded = true
                  EvidencePath = Some "readiness/host-warning-classification.md" }

            let benign = HostWarningClassification.classify baseline
            Expect.equal benign.WarningClass BenignEnvironmentWarning "known warning is benign when evidence passed"
            Expect.isFalse benign.Fatal "benign environment warning is non-fatal"

            let launchFailure = HostWarningClassification.classify { baseline with LaunchSucceeded = false }
            let renderingFailure = HostWarningClassification.classify { baseline with RenderingSucceeded = false }
            let layoutFailure = HostWarningClassification.classify { baseline with LayoutReadable = Some false }
            let packageFailure = HostWarningClassification.classify { baseline with PackageSucceeded = false }
            let unknown = HostWarningClassification.classify { baseline with RawMessage = "unexpected warning"; KnownBenignMarkers = [] }

            Expect.equal launchFailure.WarningClass LaunchFailure "launch failure remains fatal"
            Expect.equal renderingFailure.WarningClass RenderingFailure "rendering failure remains fatal"
            Expect.equal layoutFailure.WarningClass LayoutFailure "layout failure remains fatal"
            Expect.equal packageFailure.WarningClass PackageFailure "package failure remains fatal"
            Expect.equal unknown.WarningClass UnknownWarning "unknown warning remains visible"
            [ launchFailure; renderingFailure; layoutFailure; packageFailure; unknown ]
            |> List.iter (fun result -> Expect.isTrue result.Fatal $"{result.WarningClass} is fatal")
        }

        test "generated diagnostic validation requires failure classes and observable native facts" {
            let output =
                """
status=degraded mode=interactive-window diagnostic-class=environment-session native-handle=unsupported visible=unsupported focusable=unsupported focused=unsupported minimized=unsupported maximized=unsupported client-size=unavailable renderable-surface=unsupported input-devices=unsupported
status=failed mode=interactive-window diagnostic-class=window-visibility native-handle=observed:true visible=observed:false focusable=observed:false focused=unsupported minimized=observed:false maximized=observed:false client-size=640x480 renderable-surface=observed:true input-devices=observed:false
status=failed mode=interactive-window diagnostic-class=app-lifecycle native-handle=observed:true visible=observed:true focusable=observed:true focused=observed:true minimized=observed:false maximized=observed:false client-size=640x480 renderable-surface=observed:true input-devices=observed:true
status=failed mode=interactive-window diagnostic-class=product-defect native-handle=observed:true visible=observed:true focusable=observed:true focused=unsupported minimized=observed:false maximized=observed:false client-size=0x0 renderable-surface=observed:false input-devices=unavailable
"""

            let result =
                GeneratedProductAssertions.validateWindowDiagnostics
                    { Output = output
                      RequiredFailureClasses = [ "environment-session"; "window-visibility"; "app-lifecycle"; "product-defect" ]
                      RequiredNativeFacts =
                        [ "native-handle"
                          "visible"
                          "focusable"
                          "focused"
                          "minimized"
                          "maximized"
                          "client-size"
                          "renderable-surface"
                          "input-devices" ] }

            Expect.isTrue result.DiagnosticsComplete "generated diagnostics include all required failure classes and native facts"
            Expect.isEmpty result.Diagnostics "complete diagnostics have no validation errors"
        }

        test "generated diagnostic validation rejects taskbar-only success and missing native facts" {
            let output =
                "status=ok mode=interactive-window diagnostic-class=window-visibility taskbar-only=true native-handle=observed:true visible=observed:false"

            let result =
                GeneratedProductAssertions.validateWindowDiagnostics
                    { Output = output
                      RequiredFailureClasses = [ "environment-session"; "window-visibility"; "app-lifecycle"; "product-defect" ]
                      RequiredNativeFacts = [ "native-handle"; "visible"; "renderable-surface"; "input-devices" ] }

            Expect.isFalse result.DiagnosticsComplete "incomplete taskbar-only success is rejected"
            Expect.exists result.Diagnostics (fun item -> item.Contains "environment-session") "missing environment/session class is diagnostic"
            Expect.exists result.Diagnostics (fun item -> item.Contains "renderable-surface") "missing renderable-surface fact is diagnostic"
            Expect.exists result.Diagnostics (fun item -> item.Contains "taskbar-only") "taskbar-only status=ok is diagnostic"
        }

        test "visual evidence prefers screenshots and preserves board input progress fields" {
            let result =
                GeneratedConsumerValidation.selectVisualEvidence
                    { ScreenshotAvailable = true
                      PixelReadbackAvailable = true
                      BoardReadable = Some true
                      InputOrProgressObserved = Some true
                      UnsupportedReason = None }

            Expect.equal result.EvidenceKind Screenshot "screenshot is preferred when available"
            Expect.equal result.BoardReadable (Some true) "board readability is preserved"
            Expect.equal result.InputOrProgressObserved (Some true) "input/progress observation is preserved"
            Expect.isNone result.FallbackReason "screenshot path does not need fallback"
        }

        test "visual evidence uses pixel-readback fallback only when screenshots are unavailable" {
            let result =
                GeneratedConsumerValidation.selectVisualEvidence
                    { ScreenshotAvailable = false
                      PixelReadbackAvailable = true
                      BoardReadable = Some true
                      InputOrProgressObserved = Some true
                      UnsupportedReason = None }

            Expect.equal result.EvidenceKind PixelReadback "pixel-readback is selected as fallback"
            Expect.equal result.FallbackReason (Some "screenshot unavailable; pixel-readback selected") "fallback reason is explicit"
            Expect.exists result.Diagnostics (fun item -> item.Contains "screenshot unavailable") "diagnostics name screenshot fallback"
        }

        test "visual evidence reports unsupported host when screenshot and readback are unavailable" {
            let result =
                GeneratedConsumerValidation.selectVisualEvidence
                    { ScreenshotAvailable = false
                      PixelReadbackAvailable = false
                      BoardReadable = None
                      InputOrProgressObserved = None
                      UnsupportedReason = Some "headless session has no display socket" }

            Expect.equal result.EvidenceKind VisualEvidenceKind.UnsupportedHost "unsupported host is explicit"
            Expect.equal result.UnsupportedReason (Some "headless session has no display socket") "unsupported reason is retained"
            Expect.isNone result.BoardReadable "unsupported host cannot claim readable board"
            Expect.isNone result.InputOrProgressObserved "unsupported host cannot claim input/progress"
        }

        test "generated image evidence command output accepts decodable image proof fields" {
            let result =
                GeneratedConsumerValidation.validateVisualEvidenceCommandOutput
                    { Output =
                        "status=ok\nevidence-kind=image\npath=readiness/artifacts/window.png\nimage-decodable=true\nproves-scene-rendering=true\nproves-desktop-visibility=false\n"
                      RequestedImageEvidence = true }

            Expect.isTrue result.Accepted "decodable requested image evidence is accepted"
            Expect.equal result.EvidenceKind (Some "image") "image evidence kind is preserved"
            Expect.isNone result.FailureReason "valid image evidence has no failure class"
            Expect.isEmpty result.Diagnostics "valid image evidence has no diagnostics"
        }

        test "generated visual evidence command output distinguishes pixel metadata and unsupported host cases" {
            let cases =
                [ "pixel-readback",
                  "status=ok\nevidence-kind=pixel-readback\npath=readiness/artifacts/readback.txt\nfallback-reason=screenshot-unavailable\nproves-scene-rendering=true\nproves-desktop-visibility=false\n"
                  "metadata-hash",
                  "status=ok\nevidence-kind=metadata-hash\npath=readiness/artifacts/scene.hash\nproves-scene-rendering=false\nproves-desktop-visibility=false\n"
                  "unsupported-host",
                  "status=unsupported\nevidence-kind=unsupported-host\nunsupported-reason=headless session has no display socket\n" ]

            for expectedKind, output in cases do
                let result =
                    GeneratedConsumerValidation.validateVisualEvidenceCommandOutput
                        { Output = output
                          RequestedImageEvidence = expectedKind = "pixel-readback" }

                Expect.isTrue result.Accepted $"{expectedKind} output is accepted when labeled accurately"
                Expect.equal result.EvidenceKind (Some expectedKind) $"{expectedKind} kind is preserved"
        }

        test "generated visual evidence command output rejects text hashes mislabeled as screenshots" {
            let result =
                GeneratedConsumerValidation.validateVisualEvidenceCommandOutput
                    { Output =
                        "status=ok\nevidence-kind=image\npath=readiness/artifacts/window.png\nimage-decodable=false\nhash=abc123\nproves-scene-rendering=false\nproves-desktop-visibility=false\n"
                      RequestedImageEvidence = true }

            Expect.isFalse result.Accepted "text hash cannot satisfy requested image evidence"
            Expect.equal result.FailureReason (Some "metadata-only-image-evidence") "metadata-only screenshot claims have a specific failure class"
            Expect.exists result.Diagnostics (fun item -> item.Contains "decodable image") "diagnostic names decodable image requirement"
            Expect.exists result.Diagnostics (fun item -> item.Contains "metadata") "diagnostic names metadata/hash mislabeling"
        }

        test "PersistentLaunchArtifactValidation_Synthetic rejects missing required fields" {
            // SYNTHETIC: malformed readiness artifact validates parser diagnostics; real launch artifact path is readiness/window-observation-diagnostics.md.
            let result =
                PersistentLaunchArtifactValidation.validate
                    { ArtifactPath = "readiness/synthetic-missing-fields.txt"
                      Lines =
                        [ "status=failed"
                          "mode=interactive-window"
                          "command=synthetic-fixture" ]
                      SyntheticFixture = true
                      SupportedHostPassClaimed = false }

            Expect.isFalse result.Accepted "synthetic malformed artifact with missing fields is rejected"
            Expect.contains result.MissingFields "window-opened" "missing window-opened is reported"
            Expect.contains result.MissingFields "input-dispatch" "missing input-dispatch is reported"
            Expect.exists result.Diagnostics (fun item -> item = "synthetic-fixture=true") "synthetic fixture is disclosed in diagnostics"
        }

        test "PersistentLaunchArtifactValidation_Synthetic rejects invalid field values" {
            // SYNTHETIC: invalid enum values validate rejection behavior; real launch artifact path is readiness/window-observation-diagnostics.md.
            let result =
                PersistentLaunchArtifactValidation.validate
                    { ArtifactPath = "readiness/synthetic-invalid-values.txt"
                      Lines =
                        [ "status=maybe"
                          "mode=headless-only"
                          "command=synthetic-fixture"
                          "window-opened=yes"
                          "input-dispatch=magic"
                          "exit-path=no"
                          "blocked-stage=somewhere"
                          "classification=headless-only"
                          "category=unknown-category"
                          "message=invalid enum fixture" ]
                      SyntheticFixture = true
                      SupportedHostPassClaimed = false }

            Expect.isFalse result.Accepted "invalid artifact enum/string values are rejected"
            Expect.exists result.Contradictions (fun item -> item = "status=maybe") "invalid status is reported"
            Expect.exists result.Contradictions (fun item -> item = "classification=headless-only") "headless-only classification is rejected"
            Expect.exists result.Diagnostics (fun item -> item = "invalid-field=mode=headless-only") "invalid mode diagnostic is actionable"
        }

        test "PersistentLaunchArtifactValidation_Synthetic rejects contradictory supported-host pass claims" {
            // SYNTHETIC: contradictory pass claims validate rejection behavior; real launch artifact path is readiness/window-observation-diagnostics.md.
            let result =
                PersistentLaunchArtifactValidation.validate
                    { ArtifactPath = "readiness/synthetic-contradictory-pass.txt"
                      Lines =
                        [ "status=ok"
                          "mode=interactive-window"
                          "command=synthetic-fixture"
                          "window-opened=false"
                          "first-frame-presented=false"
                          "input-dispatch=not-required"
                          "exit-path=false"
                          "blocked-stage=observation"
                          "classification=ok"
                          "category=startup"
                          "message=contradictory pass fixture" ]
                      SyntheticFixture = true
                      SupportedHostPassClaimed = true }

            Expect.isFalse result.Accepted "synthetic contradictory pass claim is rejected"
            Expect.exists result.Contradictions (fun item -> item.Contains "synthetic fixture cannot satisfy") "synthetic pass claim is rejected"
            Expect.exists result.Contradictions (fun item -> item.Contains "window-opened=true") "window contradiction is reported"
            Expect.exists result.Contradictions (fun item -> item.Contains "blocked-stage=none") "blocked-stage contradiction is reported"
        }

        test "generated validation contract output includes all required validation fields" {
            let packageResolution =
                GeneratedConsumerValidation.verifyPackageResolution
                    { RequestedPackages = [ { PackageId = "FS.GG.UI.SkiaViewer"; Version = "0.1.17-preview.1"; FeedPath = "/tmp/feed" } ]
                      ResolvedPackages = [ { PackageId = "FS.GG.UI.SkiaViewer"; Version = "0.1.17-preview.1"; FeedPath = "/tmp/feed" } ]
                      PackageSources = [ "/tmp/feed" ]
                      RestoreWarnings = [] }

            let generatedTests =
                GeneratedConsumerValidation.verifyGeneratedTests
                    { TestsExist = true
                      TestsRan = true
                      VerifyRan = true }

            let interactive =
                GeneratedProductAssertions.validateDefaultInteractiveLaunch
                    "match args with | _ -> match Viewer.runApp viewerOptions generatedHost with | Result.Ok outcome -> printfn \"mode=interactive-window accessible-window=true window-visible=observed:true\" | Result.Error _ -> ()"

            let windowDiagnostics =
                GeneratedProductAssertions.validateWindowDiagnostics
                    { Output = "status=failed diagnostic-class=window-visibility native-handle=observed:true visible=observed:false"
                      RequiredFailureClasses = [ "window-visibility" ]
                      RequiredNativeFacts = [ "native-handle"; "visible" ] }

            let imageEvidence =
                GeneratedConsumerValidation.validateVisualEvidenceCommandOutput
                    { Output = "evidence-kind=image\nimage-decodable=true\nproves-scene-rendering=true\nproves-desktop-visibility=false\n"
                      RequestedImageEvidence = true }

            let result =
                GeneratedConsumerValidation.buildValidationContractOutput
                    { PackageResolution = packageResolution
                      GeneratedTests = generatedTests
                      DefaultInteractiveLaunch = interactive
                      BoundedEvidenceValidated = true
                      CloseReasonValidated = true
                      WindowDiagnostics = windowDiagnostics
                      WindowOptionsValidated = true
                      ImageEvidence = imageEvidence }

            Expect.isTrue result.Authoritative "complete generated validation is authoritative"
            Expect.equal result.FailureClass "none" "successful validation reports no failure class"
            Expect.stringContains result.Output "exact-package-match=true" "package resolution field is emitted"
            Expect.stringContains result.Output "generated-tests-ran=true" "generated test execution field is emitted"
            Expect.stringContains result.Output "default-interactive-launch=true" "interactive launch field is emitted"
            Expect.stringContains result.Output "bounded-evidence-validation=true" "bounded evidence field is emitted"
            Expect.stringContains result.Output "close-reason-validation=true" "close reason field is emitted"
            Expect.stringContains result.Output "window-diagnostics-validation=true" "window diagnostics field is emitted"
            Expect.stringContains result.Output "window-options-validation=true" "window options field is emitted"
            Expect.stringContains result.Output "image-evidence-validation=true" "image evidence field is emitted"
            Expect.stringContains result.Output "authoritative=true" "authoritative flag is emitted"
            Expect.stringContains result.Output "failure-class=none" "failure class field is emitted"
        }

        test "generated validation contract output reports first failing class" {
            let result =
                GeneratedConsumerValidation.buildValidationContractOutput
                    { PackageResolution =
                        { ExactMatch = false
                          FailureReason = Some "NU1603"
                          Diagnostics = [ "restore warning: NU1603" ] }
                      GeneratedTests =
                        { Authoritative = true
                          NonAuthoritativeReason = None
                          Diagnostics = [] }
                      DefaultInteractiveLaunch =
                        { InteractiveLaunchRequired = true
                          Diagnostics = [] }
                      BoundedEvidenceValidated = true
                      CloseReasonValidated = true
                      WindowDiagnostics =
                        { DiagnosticsComplete = true
                          Diagnostics = [] }
                      WindowOptionsValidated = true
                      ImageEvidence =
                        { Accepted = true
                          EvidenceKind = Some "image"
                          FailureReason = None
                          Diagnostics = [] } }

            Expect.isFalse result.Authoritative "package drift makes validation non-authoritative"
            Expect.equal result.FailureClass "NU1603" "first failing class is surfaced"
            Expect.stringContains result.Output "authoritative=false" "non-authoritative output is explicit"
            Expect.stringContains result.Output "failure-class=NU1603" "failure class is emitted"
            Expect.exists result.Diagnostics (fun item -> item.Contains "NU1603") "diagnostics are retained"
        }
    ]
