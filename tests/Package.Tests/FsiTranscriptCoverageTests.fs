module FsiTranscriptCoverageTests

open System
open System.IO
open Expecto
open FS.GG.TestSupport

let repositoryRoot = RepositoryRoot.value

let repositoryPath (relativePath: string) =
    Path.Combine(repositoryRoot, relativePath.Replace('/', Path.DirectorySeparatorChar))

let transcriptPath name =
    repositoryPath $"specs/035-api-discovery-names/readiness/fsi/{name}"

let feature146TranscriptPath name =
    repositoryPath $"specs/146-render-anywhere-protocol/readiness/fsi/{name}"

let feature147TranscriptPath name =
    repositoryPath $"specs/147-compositor-damage-redraw/readiness/fsi/{name}"

let feature148TranscriptPath name =
    repositoryPath $"specs/148-compositor-live-integration/readiness/fsi/{name}"

let feature149TranscriptPath name =
    repositoryPath $"specs/149-complete-compositor-p7/readiness/fsi/{name}"

let feature152TranscriptPath name =
    repositoryPath $"specs/152-compositor-live-proof/readiness/fsi/{name}"

let feature153TranscriptPath name =
    repositoryPath $"specs/153-compositor-proof-interpreter/readiness/fsi/{name}"

let feature154TranscriptPath name =
    repositoryPath $"specs/154-compositor-proof-acceptance/readiness/fsi/{name}"

let feature156TranscriptPath name =
    repositoryPath $"specs/156-same-profile-timing/readiness/fsi/{name}"

let feature157TranscriptPath name =
    repositoryPath $"specs/157-no-clear-damage-scissor/readiness/fsi/{name}"

let feature159TranscriptPath name =
    repositoryPath $"specs/159-layer-promotion-keys/readiness/fsi/{name}"

let feature150TranscriptPath name =
    repositoryPath $"specs/150-intrinsic-layout-protocol/readiness/fsi/{name}"

let readTranscript name =
    let path = transcriptPath name
    Expect.isTrue (File.Exists path) $"FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature146Transcript name =
    let path = feature146TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature146 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature147Transcript name =
    let path = feature147TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature147 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature148Transcript name =
    let path = feature148TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature148 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature149Transcript name =
    let path = feature149TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature149 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature152Transcript name =
    let path = feature152TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature152 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature153Transcript name =
    let path = feature153TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature153 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature154Transcript name =
    let path = feature154TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature154 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature156Transcript name =
    let path = feature156TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature156 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature157Transcript name =
    let path = feature157TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature157 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature159Transcript name =
    let path = feature159TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature159 FSI transcript evidence exists at {path}"
    File.ReadAllText path

let readFeature150Transcript name =
    let path = feature150TranscriptPath name
    Expect.isTrue (File.Exists path) $"Feature150 FSI transcript evidence exists at {path}"
    File.ReadAllText path

[<Tests>]
let fsiTranscriptCoverageTests =
    testList "Package-shaped FSI transcript coverage" [
        test "Scene transcript authors primitives, Paint helpers, and geometry records" {
            let transcript = readTranscript "scene-authoring.fsx"

            [ "#r \"nuget: FS.GG.UI.Scene"
              "open FS.GG.UI.Scene"
              "Rect"
              "Paint"
              "Stroke"
              "TextRun"
              "SceneElementKind.RectangleElement" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Scene transcript includes {required}")
        }

        test "Viewer and keyboard transcript authors public records and cases" {
            let transcript = readTranscript "viewer-keyboard-authoring.fsx"

            [ "#r \"nuget: FS.GG.UI.SkiaViewer"
              "#r \"nuget: FS.GG.UI.KeyboardInput"
              "ViewerOptions"
              "InitialSize"
              "ViewerWindowPosition.Coordinates"
              "KeyboardModel"
              "KeyDown"
              "KeyUp" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Viewer/keyboard transcript includes {required}")
        }

        test "Controls-adjacent transcript authors controls without relying on repository source" {
            let transcript = readTranscript "controls-adjacent-authoring.fsx"

            [ "#r \"nuget: FS.GG.UI.Controls"
              "#r \"nuget: FS.GG.UI.Controls.Elmish"
              "FS.GG.UI.Controls.TextBlock.create"
              "FS.GG.UI.Controls.TextBox.onChanged"
              "FS.GG.UI.Controls.DataGrid.create"
              "ControlsElmish" ]
            |> List.iter (fun required ->
                Expect.stringContains transcript required $"Controls transcript includes {required}")

            [ "#load"
              "../src/"
              "Assembly.Load"
              "GetExportedTypes" ]
            |> List.iter (fun forbidden ->
                Expect.isFalse (transcript.Contains(forbidden, StringComparison.OrdinalIgnoreCase)) $"Controls transcript does not use {forbidden}")
        }

        test "FSI transcript run logs are captured as readiness artifacts" {
            [ "scene-authoring.log"
              "viewer-keyboard-authoring.log"
              "controls-adjacent-authoring.log" ]
            |> List.iter (fun logName ->
                let logPath = transcriptPath logName
                Expect.isTrue (File.Exists logPath) $"{logName} exists"
                Expect.stringContains (File.ReadAllText logPath) "FSI transcript PASS" $"{logName} records a passing FSI run")
        }

        test "Feature146 transcripts cover SceneCodec and ReferenceRendering authoring" {
            let sceneCodec = readFeature146Transcript "scene-codec-authoring.fsx"
            let referenceRendering = readFeature146Transcript "reference-rendering-authoring.fsx"

            [ "SceneCodec.export"
              "SceneCodec.inspect"
              "SceneCodec.packageIdentity"
              "SceneCodec.compareScenes" ]
            |> List.iter (fun required ->
                Expect.stringContains sceneCodec required $"SceneCodec transcript includes {required}")

            [ "ReferenceRendering.init"
              "PackageBytes"
              "OutputDirectory"
              "OutputSize" ]
            |> List.iter (fun required ->
                Expect.stringContains referenceRendering required $"ReferenceRendering transcript includes {required}")

            [ "scene-codec-authoring.log"
              "reference-rendering-authoring.log" ]
            |> List.iter (fun logName ->
                let log = readFeature146Transcript logName
                Expect.stringContains log "FSI transcript PASS" $"{logName} records passing FSI coverage")
        }

        test "Feature147 transcripts cover compositor proof and metrics authoring" {
            let proof = readFeature147Transcript "compositor-proof-authoring.fsx"
            let metrics = readFeature147Transcript "compositor-metrics-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "CompositorProof.HostProfile"
              "CompositorProof.readiness" ]
            |> List.iter (fun required -> Expect.stringContains proof required $"proof transcript includes {required}")

            [ "FS.GG.UI.Controls.Elmish"
              "CompositorFrameDiagnostics"
              "FrameMetrics" ]
            |> List.iter (fun required -> Expect.stringContains metrics required $"metrics transcript includes {required}")

            [ "compositor-proof-authoring.log"
              "compositor-metrics-authoring.log" ]
            |> List.iter (fun logName ->
                let log = readFeature147Transcript logName
                Expect.stringContains log "FSI transcript PASS" $"{logName} records passing FSI coverage")
        }

        test "Feature148 transcripts cover live proof and compositor diagnostics authoring" {
            let transcript = readFeature148Transcript "compositor-live-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "CompositorProof.ProofReadiness"
              "CompositorProof.readinessToken"
              "FS.GG.UI.Controls.Elmish"
              "CompositorFrameDiagnostics" ]
            |> List.iter (fun required -> Expect.stringContains transcript required $"Feature148 transcript includes {required}")

            let log = readFeature148Transcript "compositor-live-authoring.log"
            Expect.stringContains log "FSI transcript PASS" "Feature148 transcript log records passing coverage"
        }

        test "Feature149 transcripts cover final compositor readiness authoring" {
            let transcript = readFeature149Transcript "compositor-readiness-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "CompositorProof.ProofReadiness"
              "CompositorProof.readinessToken"
              "FS.GG.UI.Controls.Elmish"
              "CompositorFrameDiagnostics"
              "FS.GG.UI.Testing"
              "ReadinessFileDiscovery" ]
            |> List.iter (fun required -> Expect.stringContains transcript required $"Feature149 transcript includes {required}")

            let log = readFeature149Transcript "compositor-readiness-authoring.log"
            Expect.stringContains log "FSI transcript PASS" "Feature149 transcript log records passing coverage"
        }

        test "Feature152 transcripts cover proof-set and readiness helper authoring" {
            let transcript = readFeature152Transcript "compositor-live-proof-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "CompositorProof.LiveProofAttempt"
              "CompositorProof.evaluateProofSet"
              "CompositorProof.ProofSetReadiness"
              "FS.GG.UI.Testing"
              "CompositorReadiness.statusText"
              "CompositorReadiness.validate" ]
            |> List.iter (fun required -> Expect.stringContains transcript required $"Feature152 transcript includes {required}")

            let log = readFeature152Transcript "compositor-live-proof-authoring.log"
            Expect.stringContains log "FSI transcript PASS" "Feature152 transcript log records passing coverage"
        }

        test "Feature153 transcripts cover proof interpreter readiness authoring" {
            let transcript = readFeature153Transcript "compositor-proof-interpreter-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "CompositorProof.AcceptedProofSet"
              "CompositorProof.evaluateProofSet"
              "CompositorProof.ProofSetReadiness"
              "FS.GG.UI.SkiaViewer.Host"
              "GlHost.LiveProofHostFacts"
              "Viewer.liveProofInterpreterSupported"
              "FS.GG.UI.Testing"
              "CompositorReadiness.validate" ]
            |> List.iter (fun required -> Expect.stringContains transcript required $"Feature153 transcript includes {required}")

            let log = readFeature153Transcript "compositor-proof-interpreter-authoring.log"
            Expect.stringContains log "FSI transcript PASS" "Feature153 transcript log records passing coverage"
        }

        test "Feature154 transcripts cover proof acceptance and final readiness authoring" {
            let proof = readFeature154Transcript "compositor-proof-acceptance-authoring.fsx"
            let readiness = readFeature154Transcript "compositor-readiness-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "CompositorProof.LiveProofAttempt"
              "CompositorProof.evaluateProofSet"
              "CompositorProof.ProofSetReadiness"
              "FS.GG.UI.Testing"
              "CompositorReadiness.validate" ]
            |> List.iter (fun required -> Expect.stringContains proof required $"Feature154 proof transcript includes {required}")

            [ "FS.GG.UI.Testing"
              "CompositorReadiness.statusText"
              "CompositorReadiness.validate"
              "154-compositor-proof-acceptance" ]
            |> List.iter (fun required -> Expect.stringContains readiness required $"Feature154 readiness transcript includes {required}")

            [ "compositor-proof-acceptance-authoring.log"
              "compositor-readiness-authoring.log" ]
            |> List.iter (fun logName ->
                let log = readFeature154Transcript logName
                Expect.stringContains log "FSI transcript PASS" $"{logName} records passing FSI coverage")
        }

        test "Feature156 transcripts cover timing policy readiness and Testing helpers" {
            let timing = readFeature156Transcript "compositor-performance-authoring.fsx"
            let readiness = readFeature156Transcript "compositor-readiness-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "ViewerTimingPath.DamageScoped"
              "CompositorProof.TimingOverheadDisclosure"
              "FS.GG.UI.Testing"
              "CompositorTimingAssertions.validateSummary"
              "same-profile-live-threshold-v2" ]
            |> List.iter (fun required -> Expect.stringContains timing required $"Feature156 timing transcript includes {required}")

            [ "FS.GG.UI.Testing"
              "CompositorTimingAssertions.verdictText"
              "CompositorReadiness.validate"
              "156-same-profile-timing" ]
            |> List.iter (fun required -> Expect.stringContains readiness required $"Feature156 readiness transcript includes {required}")

            [ "compositor-performance-authoring.log"
              "compositor-readiness-authoring.log" ]
            |> List.iter (fun logName ->
                let log = readFeature156Transcript logName
                Expect.stringContains log "FSI transcript PASS" $"{logName} records passing FSI coverage")
        }

        test "Feature157 transcripts cover damage decisions and Testing helpers" {
            let damage = readFeature157Transcript "compositor-damage-authoring.fsx"
            let readiness = readFeature157Transcript "compositor-readiness-authoring.fsx"

            [ "FS.GG.UI.SkiaViewer"
              "ViewerDamageDecision.DamageScopedAccepted"
              "FS.GG.UI.Testing"
              "CompositorDamageReadiness.statusText"
              "CompositorDamageReadiness.validate" ]
            |> List.iter (fun required -> Expect.stringContains damage required $"Feature157 damage transcript includes {required}")

            [ "FS.GG.UI.Testing"
              "CompositorDamageAccepted"
              "CompositorDamageEnvironmentLimited"
              "performance-not-accepted"
              "157-no-clear-damage-scissor" ]
            |> List.iter (fun required -> Expect.stringContains readiness required $"Feature157 readiness transcript includes {required}")

            [ "compositor-damage-authoring.log"
              "compositor-readiness-authoring.log" ]
            |> List.iter (fun logName ->
                let log = readFeature157Transcript logName
                Expect.stringContains log "Feature157" $"{logName} records Feature157 FSI coverage")
        }

        test "Feature159 transcripts cover split identity promotion and readiness helpers" {
            let identity = readFeature159Transcript "content-placement-identity-authoring.fsx"
            let promotion = readFeature159Transcript "compositor-promotion-authoring.fsx"
            let readiness = readFeature159Transcript "compositor-readiness-authoring.fsx"

            [ "content-identity-v1"
              "placement-identity-v1"
              "content-reused-placement-updated" ]
            |> List.iter (fun required -> Expect.stringContains identity required $"Feature159 identity transcript includes {required}")

            [ "compositor-promotion --feature 159"
              "layer-promotion-v1"
              "promotion/static-retained"
              "promotion/placement-only-move" ]
            |> List.iter (fun required -> Expect.stringContains promotion required $"Feature159 promotion transcript includes {required}")

            [ "FS.GG.UI.Testing"
              "Feature159Readiness.statusText"
              "Feature159Accepted"
              "performance-not-accepted" ]
            |> List.iter (fun required -> Expect.stringContains readiness required $"Feature159 readiness transcript includes {required}")

            [ "content-placement-identity-authoring.log"
              "compositor-promotion-authoring.log"
              "compositor-readiness-authoring.log" ]
            |> List.iter (fun logName ->
                let log = readFeature159Transcript logName
                Expect.stringContains log "Feature159" $"{logName} records Feature159 FSI coverage")
        }

        test "Feature150 transcript covers intrinsic layout and readiness authoring" {
            let transcript = readFeature150Transcript "layout-intrinsic-authoring.fsx"

            [ "FS.GG.UI.Layout"
              "Layout.constraints"
              "Layout.intrinsicQuery"
              "IntrinsicMaxHeight"
              "FS.GG.UI.Controls"
              "FS.GG.UI.Controls.Elmish"
              "FS.GG.UI.Testing"
              "LayoutReadiness.statusText" ]
            |> List.iter (fun required -> Expect.stringContains transcript required $"Feature150 transcript includes {required}")

            let log = readFeature150Transcript "layout-intrinsic-authoring.log"
            Expect.stringContains log "FSI transcript PASS" "Feature150 transcript log records passing coverage"
        }

        test "Feature151 records no new public FSI delta on top of Feature150" {
            let ledger = File.ReadAllText(repositoryPath "specs/151-complete-p8-layout/readiness/compatibility-ledger.md")

            Expect.stringContains ledger "No new public `.fsi` surface" "no public delta"
            Expect.stringContains ledger "Feature150 layout" "Feature150 remains the public substrate"
            Expect.stringContains ledger "No consumer migration is required" "migration guidance"
        }
    ]
