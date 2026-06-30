module SceneCapabilityTests

open System.IO
open Expecto
open FS.GG.UI.Scene

[<Tests>]
let tests =
    testList "Scene public contract" [
        test "rectangle descriptions are stable" {
            let node =
                Scene.rectangle
                    (0.0, 0.0, 10.0, 20.0)
                    (Colors.rgb 1uy 2uy 3uy)

            Expect.contains (Scene.describe node) RectangleElement "scene description includes rectangle kind"
        }

        test "filled circle and ellipse constructors are public and describe deterministic shape kinds" {
            let circle =
                Scene.circle
                    { X = 32.0; Y = 24.0 }
                    12.0
                    (Colors.rgb 240uy 64uy 32uy)

            let ellipse =
                Scene.filledEllipse
                    { X = 8.0; Y = 12.0; Width = 48.0; Height = 20.0 }
                    (Colors.rgb 32uy 160uy 220uy)

            Expect.contains (Scene.describe circle) CircleElement "circle is a first-class scene element"
            Expect.contains (Scene.describe ellipse) EllipseElement "filled ellipse is a first-class scene element"
        }

        test "shape evidence records fill bounds and partial out-of-bounds placement" {
            let fill = Colors.rgb 255uy 128uy 0uy

            let circle =
                Scene.circleEvidence
                    { Width = 64; Height = 48 }
                    { X = 4.0; Y = 8.0 }
                    12.0
                    fill

            let ellipse =
                Scene.ellipseEvidence
                    { Width = 64; Height = 48 }
                    { X = 16.0; Y = 20.0; Width = 24.0; Height = 12.0 }
                    fill

            Expect.equal circle.Fill fill "circle evidence records fill"
            Expect.equal circle.Bounds { X = -8.0; Y = -4.0; Width = 24.0; Height = 24.0 } "circle evidence derives bounds from center/radius"
            Expect.equal circle.Placement PartiallyOutOfBounds "circle evidence classifies partial clipping"
            Expect.equal ellipse.Fill fill "ellipse evidence records fill"
            Expect.equal ellipse.Placement FullyInside "ellipse evidence records placement"
        }

        test "scene evidence helper returns deterministic hash without a viewer window" {
            let scene = Scene.rectangle (0.0, 0.0, 10.0, 20.0) (Colors.rgb 1uy 2uy 3uy)

            let evidence =
                SceneEvidence.renderHash { Width = 64; Height = 64 } scene

            match evidence with
            | Result.Ok value ->
                Expect.equal value.Format Hash "hash evidence is returned"
                Expect.equal value.OutputSize { Width = 64; Height = 64 } "output size is recorded"
                Expect.equal value.RendererMode "deterministic-scene" "renderer mode is scene-level"
                Expect.isNonEmpty value.Value "hash value is populated"
            | Result.Error failure -> failtestf "scene evidence should not fail: %s" failure.Message
        }

        test "scene evidence writes metadata output with stable size renderer and value" {
            let scene =
                Scene.group [
                    Scene.rectangle (0.0, 0.0, 10.0, 20.0) (Colors.rgb 1uy 2uy 3uy)
                    Scene.text (4.0, 18.0) "Generated" Colors.white
                ]

            let path = Path.Combine(Path.GetTempPath(), "fs-gg-scene-evidence-metadata.txt")
            if File.Exists path then
                File.Delete path

            let result =
                SceneEvidence.render
                    { Scene = scene
                      OutputSize = { Width = 128; Height = 96 }
                      Format = Metadata
                      RendererMode = "deterministic-scene"
                      EvidencePath = Some path }

            match result with
            | Result.Ok evidence ->
                Expect.equal evidence.Format Metadata "metadata evidence is returned"
                Expect.equal evidence.OutputSize { Width = 128; Height = 96 } "metadata records output size"
                Expect.equal evidence.RendererMode "deterministic-scene" "metadata uses deterministic scene renderer"
                Expect.stringContains evidence.Value "size=128x96" "metadata names output size"
                Expect.stringContains (File.ReadAllText path) evidence.Value "metadata evidence is written to disk"
            | Result.Error failure -> failtestf "scene evidence should write metadata: %A" failure
        }

        test "renderPng fails honestly (no stub) in the dependency-light Scene assembly with no rasterizer injected" {
            // Feature 221 (US1/US3, FR-002/FR-005/SC-005): pre-221 `renderPng` returned the UTF-8 bytes
            // of a capability HASH as a fake "PNG". That success-shaped non-image is eliminated. In the
            // SkiaSharp-free `Scene` assembly nothing injects a CPU rasterizer, so `renderPng` now returns
            // a typed `UnsupportedEnvironment` failure naming the blocked renderer stage — never a stub.
            // The viewer-free DETERMINISTIC capability-hash guarantee lives on `renderHash` (asserted below).
            let scene = Scene.text (12.0, 24.0) "Generated app scene" Colors.white

            match SceneEvidence.renderPng { Width = 80; Height = 40 } scene with
            | Result.Error failure ->
                Expect.equal failure.Classification UnsupportedEnvironment "no rasterizer ⇒ unsupported-environment"
                Expect.equal failure.BlockedStage "renderer" "the blocked stage names the renderer"
                Expect.isFalse (System.String.IsNullOrWhiteSpace failure.Message) "the failure carries a message"
            | Result.Ok bytes -> failtestf "expected a typed failure, not a %d-byte stub (SC-005)" bytes.Length
        }

        test "renderHash bytes are deterministic and do not require viewer startup" {
            let scene = Scene.text (12.0, 24.0) "Generated app scene" Colors.white

            match SceneEvidence.renderHash { Width = 80; Height = 40 } scene, SceneEvidence.renderHash { Width = 80; Height = 40 } scene with
            | Result.Ok first, Result.Ok second ->
                Expect.equal first.Value second.Value "capability-hash evidence is stable for the same scene (no viewer startup)"
                Expect.isGreaterThan first.Value.Length 0 "capability-hash evidence produces a value"
            | other -> failtestf "expected deterministic capability-hash evidence, got %A" other
        }

        test "scene evidence reports unsupported renderer capabilities explicitly" {
            let result =
                SceneEvidence.render
                    { Scene = Scene.empty
                      OutputSize = { Width = 64; Height = 64 }
                      Format = Hash
                      RendererMode = "live-window"
                      EvidencePath = None }

            match result with
            | Result.Error failure ->
                Expect.equal failure.Classification UnsupportedEnvironment "unsupported renderer mode is environment/capability failure"
                Expect.equal failure.BlockedStage "renderer" "blocked renderer capability is named"
                Expect.stringContains failure.Message "live-window" "message names unsupported renderer mode"
            | Result.Ok evidence -> failtestf "expected unsupported renderer failure, got %A" evidence
        }

        test "layout evidence classifies complete non-overlapping HUD and gameplay facts as readable" {
            let report =
                { Scene = Scene.text (16.0, 28.0) "Score 100" Colors.white
                  OutputSize = { Width = 1280; Height = 720 }
                  ProofLevel = DeterministicRenderOnly
                  HudRegion = Some { Name = "hud"; Bounds = { X = 0.0; Y = 0.0; Width = 1280.0; Height = 96.0 } }
                  GameplayRegion = Some { Name = "gameplay"; Bounds = { X = 0.0; Y = 96.0; Width = 1280.0; Height = 624.0 } }
                  TextBounds = [ { Name = "score"; Text = "Score 100"; Bounds = { X = 16.0; Y = 12.0; Width = 92.0; Height = 24.0 }; MeasurementMode = ExactTextBounds } ]
                  GameplayBounds = [ { Name = "ship"; Bounds = { X = 620.0; Y = 420.0; Width = 28.0; Height = 28.0 } } ]
                  OverlapStatus = NoLayoutOverlap
                  MeasurementMode = ExactTextBounds
                  UnsupportedReasons = []
                  Diagnostics = []
                  RenderEvidence = None }

            Expect.equal (LayoutEvidence.classify report).ProofLevel ReadableLayout "complete layout facts prove readability"
        }

        test "layout evidence keeps deterministic render metadata separate from readability proof" {
            let scene = Scene.rectangle (0.0, 0.0, 10.0, 10.0) Colors.white
            let renderEvidence = Scene.renderReadbackEvidence { Width = 640; Height = 480 } scene

            let report =
                LayoutEvidence.fromRenderEvidence scene renderEvidence

            Expect.equal report.ProofLevel DeterministicRenderOnly "render hash evidence is not relabeled as readable layout"
            Expect.isNone report.HudRegion "deterministic-only report does not invent HUD facts"
            Expect.isEmpty report.TextBounds "deterministic-only report does not invent text bounds"
        }

        test "layout evidence reports unsupported inspection without claiming readability" {
            let report =
                LayoutEvidence.unsupported
                    Scene.empty
                    { Width = 640; Height = 480 }
                    { Fact = "font-metrics"; Reason = "host metrics unavailable"; Diagnostic = "unsupported layout inspection" }

            Expect.equal report.ProofLevel UnsupportedLayoutInspection "unsupported facts are explicit"
            Expect.isNonEmpty report.UnsupportedReasons "unsupported reason is preserved"
            Expect.exists report.Diagnostics (fun item -> item.Contains "font-metrics") "diagnostic names unsupported fact"
        }

        test "layout evidence detects missing and overlapping bounds" {
            let report =
                { Scene = Scene.empty
                  OutputSize = { Width = 640; Height = 480 }
                  ProofLevel = ReadableLayout
                  HudRegion = Some { Name = "hud"; Bounds = { X = 0.0; Y = 0.0; Width = 640.0; Height = 80.0 } }
                  GameplayRegion = Some { Name = "gameplay"; Bounds = { X = 0.0; Y = 80.0; Width = 640.0; Height = 400.0 } }
                  TextBounds =
                    [ { Name = "score"; Text = "Score"; Bounds = { X = 12.0; Y = 12.0; Width = 80.0; Height = 24.0 }; MeasurementMode = ApproximateTextBounds }
                      { Name = "lives"; Text = "Lives"; Bounds = { X = 40.0; Y = 18.0; Width = 80.0; Height = 24.0 }; MeasurementMode = ApproximateTextBounds } ]
                  GameplayBounds = []
                  OverlapStatus = NoLayoutOverlap
                  MeasurementMode = ApproximateTextBounds
                  UnsupportedReasons = []
                  Diagnostics = []
                  RenderEvidence = None }

            let classified = LayoutEvidence.classify report

            Expect.equal classified.ProofLevel DeterministicRenderOnly "missing gameplay bounds prevent readability"
            match classified.OverlapStatus with
            | LayoutOverlaps overlaps -> Expect.exists overlaps (fun item -> item.Kind = HudTextOverlap) "HUD/HUD overlap is reported"
            | NoLayoutOverlap -> failtest "expected HUD overlap diagnostics"
        }
    ]
