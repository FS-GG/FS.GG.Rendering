module Feature146ReferenceRenderingTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private tempOut name =
    let path = Path.Combine(Path.GetTempPath(), "fs-gg-feature146", name + "-" + Guid.NewGuid().ToString("N"))
    Directory.CreateDirectory(path) |> ignore
    path

let private packageBytes () =
    let scene =
        Scene.group
            [ Scene.filledRectangle { X = 0.0; Y = 0.0; Width = 96.0; Height = 64.0 } (Colors.rgb 12uy 24uy 36uy)
              Scene.circle { X = 48.0; Y = 32.0 } 16.0 (Colors.rgb 240uy 120uy 40uy) ]

    (SceneCodec.export scene).CanonicalBytes

let private request () =
    { PackageBytes = packageBytes ()
      OutputDirectory = tempOut "reference"
      OutputSize = { Width = 96; Height = 64 }
      Resources = [] }

[<Tests>]
let feature146ReferenceRenderingTests =
    testList "Feature146 reference rendering" [
        test "init emits package inspection effect" {
            let model, effects = ReferenceRendering.init (request ())
            Expect.isNone model.Inspection "inspection starts empty"
            Expect.equal effects [ InspectPackage model.Request.PackageBytes ] "init requests inspection"
        }

        test "update emits render effect for accepted inspection" {
            let req = request ()
            let report = SceneCodec.inspect req.PackageBytes
            let model, _ = ReferenceRendering.init req
            let updated, effects = ReferenceRendering.update (PackageInspected report) model

            Expect.isSome updated.Inspection "inspection stored"
            match effects with
            | [ RenderPackage(bytes, size, outDir, resources) ] ->
                Expect.sequenceEqual bytes req.PackageBytes "render effect carries package bytes"
                Expect.equal size req.OutputSize "render effect carries output size"
                Expect.equal outDir req.OutputDirectory "render effect carries output directory"
                Expect.equal resources [] "render effect carries resources"
            | other -> failtestf "unexpected effects: %A" other
        }

        test "rejected package does not emit render effect" {
            let req =
                { request () with
                    PackageBytes = [| 1uy; 2uy; 3uy |] }

            let report = SceneCodec.inspect req.PackageBytes
            let model, _ = ReferenceRendering.init req
            let updated, effects = ReferenceRendering.update (PackageInspected report) model

            Expect.isSome updated.Evidence "rejected inspection produces evidence"
            Expect.equal updated.Evidence.Value.Verdict ReferenceFailed "rejected inspection fails safely"
            Expect.exists effects (function WriteReferenceEvidence _ -> true | _ -> false) "failed evidence is written"
        }

        test "valid package produces passed or environment-limited evidence" {
            let req = request ()
            let evidence = ReferenceRendering.run req

            match evidence.Verdict with
            | ReferencePassed ->
                Expect.isSome evidence.ImagePath "passed evidence has image path"
                Expect.isTrue (File.Exists evidence.ImagePath.Value) "reference PNG exists"
                Expect.isSome evidence.ImageIdentity "passed evidence has image identity"
                Expect.equal evidence.OutputSize req.OutputSize "output size is recorded"
                Expect.isTrue (File.Exists(Path.Combine(req.OutputDirectory, "reference-evidence.md"))) "summary exists"
            | ReferenceEnvironmentLimited ->
                Expect.isNone evidence.ImagePath "environment-limited evidence has no accepted image"
                Expect.exists evidence.Diagnostics (fun d -> d.Length > 0) "environment-limited evidence has diagnostics"
            | ReferenceFailed ->
                failtestf "valid package failed reference rendering: %A" evidence.Diagnostics
        }
    ]
