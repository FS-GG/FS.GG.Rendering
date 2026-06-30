namespace FS.GG.UI.SkiaViewer

open System
open System.IO
open System.Security.Cryptography
open SkiaSharp
open FS.GG.UI.Scene

type ReferenceRenderVerdict =
    | ReferencePassed
    | ReferenceFailed
    | ReferenceEnvironmentLimited

type ReferenceFailureClassification =
    | ReferenceProductDefect
    | ReferenceUnsupportedEnvironment
    | ReferencePackageResourceIncompatibility
    | ReferenceVerificationDepth

type ReferenceRenderingRequest =
    { PackageBytes: byte[]
      OutputDirectory: string
      OutputSize: Size
      Resources: ResourceAvailability list }

type ReferenceRenderingEvidence =
    { PackageIdentity: string
      ProtocolVersion: ProtocolVersion option
      CapabilityProfile: string
      ResourceStatus: string
      OutputSize: Size
      ImagePath: string option
      ImageIdentity: string option
      RendererIdentity: string
      Verdict: ReferenceRenderVerdict
      Classification: ReferenceFailureClassification option
      Diagnostics: string list }

type ReferenceRenderingModel =
    { Request: ReferenceRenderingRequest
      Inspection: PackageInspectionReport option
      Evidence: ReferenceRenderingEvidence option
      Diagnostics: string list }

type ReferenceRenderingMsg =
    | Start
    | PackageInspected of PackageInspectionReport
    | RenderCompleted of ReferenceRenderingEvidence
    | RenderFailed of ReferenceFailureClassification * string

type ReferenceRenderingEffect =
    | InspectPackage of byte[]
    | RenderPackage of byte[] * Size * string * ResourceAvailability list
    | WriteReferenceEvidence of ReferenceRenderingEvidence * string

module ReferenceRendering =
    let private sha256Hex (bytes: byte[]) =
        SHA256.HashData bytes |> Convert.ToHexString |> fun value -> value.ToLowerInvariant()

    let private rendererIdentity =
        "FS.GG.UI.SkiaViewer.SceneRenderer/skia-reference"

    let private statusText status =
        match status with
        | PackageAccepted -> "accepted"
        | PackageAcceptedWithDegradation -> "accepted-with-degradation"
        | PackageRejected -> "rejected"

    let private verdictText verdict =
        match verdict with
        | ReferencePassed -> "passed"
        | ReferenceFailed -> "failed"
        | ReferenceEnvironmentLimited -> "environment-limited"

    let private classificationText classification =
        match classification with
        | None -> "none"
        | Some ReferenceProductDefect -> "product-defect"
        | Some ReferenceUnsupportedEnvironment -> "unsupported-environment"
        | Some ReferencePackageResourceIncompatibility -> "package-resource-incompatibility"
        | Some ReferenceVerificationDepth -> "verification-depth"

    let private resourceSummary (report: PackageInspectionReport) =
        if report.ResourceVerdicts.IsEmpty then
            "none"
        else
            report.ResourceVerdicts
            |> List.map (fun verdict ->
                let status =
                    if verdict.Accepted && verdict.Degraded then "degraded"
                    elif verdict.Accepted then "accepted"
                    else "rejected"
                $"{verdict.Entry.ResourceId}:{status}")
            |> String.concat ","

    let private failure request classification diagnostics =
        { PackageIdentity = SceneCodec.packageIdentity request.PackageBytes
          ProtocolVersion = None
          CapabilityProfile = "unavailable"
          ResourceStatus = "unavailable"
          OutputSize = request.OutputSize
          ImagePath = None
          ImageIdentity = None
          RendererIdentity = rendererIdentity
          Verdict = if classification = ReferenceUnsupportedEnvironment then ReferenceEnvironmentLimited else ReferenceFailed
          Classification = Some classification
          Diagnostics = diagnostics }

    let private imageNonBlank (pngBytes: byte[]) =
        try
            use bitmap = SKBitmap.Decode(pngBytes)
            if isNull bitmap then
                false
            else
                bitmap.Pixels |> Array.exists (fun pixel -> pixel.Alpha > 0uy)
        with _ ->
            false

    let private renderScenePng (outputSize: Size) (scene: Scene) =
        let width = max 1 outputSize.Width
        let height = max 1 outputSize.Height
        let info = SKImageInfo(width, height, SKColorType.Rgba8888, SKAlphaType.Premul)
        use surface = SKSurface.Create(info)

        if isNull surface then
            Result.Error "Skia could not create an offscreen raster surface."
        else
            surface.Canvas.Clear(SKColors.Transparent)
            scene.Nodes |> List.iter (SceneRenderer.paintNode surface.Canvas)
            surface.Canvas.Flush()
            use image = surface.Snapshot()
            use data = image.Encode(SKEncodedImageFormat.Png, 100)

            if isNull data then
                Result.Error "Skia could not encode the reference PNG."
            else
                Result.Ok(data.ToArray())

    // Feature 221 (US1, FR-001/FR-002/FR-004): the public CPU-raster entry injected into
    // `SceneEvidence.setRealPngRasterizer`. Reuses the existing no-`GRContext` `renderScenePng` donor
    // (`SKSurface.Create` over an `SKImageInfo` — no GPU/GL/X/display) and the shared exhaustive
    // `SceneRenderer.paintNode`, so `renderPng` returns real pixels in a bare container. Donor string
    // and native-load failures are mapped onto the typed `SceneEvidenceFailure` so the PNG surface
    // never returns a success-shaped non-image (FR-005/SC-005). Serialized via `rasterGate`:
    // `SceneRenderer.paintNode` mutates the shared `fallbackEvents` disclosure accumulator, so
    // concurrent calls are serialized to keep each render isolated and deterministic (contract
    // C2.3/C1.7). The render surface is per-call/local, so output bytes depend only on (size, scene).
    let private rasterGate = obj ()

    let renderScenePngResult (outputSize: Size) (scene: Scene) : Result<byte[], SceneEvidenceFailure> =
        let rendererFailure classification message : SceneEvidenceFailure =
            { BlockedStage = "renderer"
              Classification = classification
              DiagnosticCategory = "renderer"
              Message = message }

        lock rasterGate (fun () ->
            try
                match renderScenePng outputSize scene with
                | Result.Ok pngBytes -> Result.Ok pngBytes
                | Result.Error message -> Result.Error(rendererFailure UnsupportedEnvironment message)
            with
            | :? DllNotFoundException as ex -> Result.Error(rendererFailure UnsupportedEnvironment $"Skia native library unavailable: {ex.Message}")
            | :? EntryPointNotFoundException as ex -> Result.Error(rendererFailure UnsupportedEnvironment $"Skia native entry point unavailable: {ex.Message}")
            | :? TypeInitializationException as ex -> Result.Error(rendererFailure UnsupportedEnvironment $"Skia initialization failed: {ex.Message}")
            | ex -> Result.Error(rendererFailure ProductDefect ex.Message))

    let init request =
        { Request = request
          Inspection = None
          Evidence = None
          Diagnostics = [] },
        [ InspectPackage request.PackageBytes ]

    let update msg model =
        match msg with
        | Start -> model, [ InspectPackage model.Request.PackageBytes ]
        | PackageInspected report ->
            let diagnostics = SceneCodec.formatDiagnostics report.Diagnostics
            let model = { model with Inspection = Some report; Diagnostics = diagnostics }

            match report.Status with
            | PackageRejected ->
                let evidence =
                    failure model.Request ReferencePackageResourceIncompatibility ("package inspection rejected reference rendering" :: diagnostics)
                { model with Evidence = Some evidence }, [ WriteReferenceEvidence(evidence, model.Request.OutputDirectory) ]
            | PackageAccepted
            | PackageAcceptedWithDegradation ->
                model, [ RenderPackage(model.Request.PackageBytes, model.Request.OutputSize, model.Request.OutputDirectory, model.Request.Resources) ]
        | RenderCompleted evidence ->
            { model with Evidence = Some evidence; Diagnostics = evidence.Diagnostics },
            [ WriteReferenceEvidence(evidence, model.Request.OutputDirectory) ]
        | RenderFailed(classification, message) ->
            let evidence = failure model.Request classification [ message ]
            { model with Evidence = Some evidence; Diagnostics = evidence.Diagnostics },
            [ WriteReferenceEvidence(evidence, model.Request.OutputDirectory) ]

    let writeEvidenceSummary outputDirectory evidence =
        Directory.CreateDirectory(outputDirectory) |> ignore
        let path = Path.Combine(outputDirectory, "reference-evidence.md")
        let protocolVersion =
            evidence.ProtocolVersion
            |> Option.map (fun v -> $"{v.Major}.{v.Minor}")
            |> Option.defaultValue "none"
        let imagePath = evidence.ImagePath |> Option.defaultValue "none"
        let imageIdentity = evidence.ImageIdentity |> Option.defaultValue "none"

        let lines =
            [ "# Feature 146 Reference Rendering Evidence"
              ""
              $"- verdict: {verdictText evidence.Verdict}"
              $"- classification: {classificationText evidence.Classification}"
              $"- package-identity: {evidence.PackageIdentity}"
              $"- protocol-version: {protocolVersion}"
              $"- capability-profile: {evidence.CapabilityProfile}"
              $"- resource-status: {evidence.ResourceStatus}"
              $"- output-size: {evidence.OutputSize.Width}x{evidence.OutputSize.Height}"
              $"- renderer-identity: {evidence.RendererIdentity}"
              $"- image-path: {imagePath}"
              $"- image-identity: {imageIdentity}"
              ""
              "## Diagnostics"
              yield!
                  if evidence.Diagnostics.IsEmpty then
                      [ "- none" ]
                  else
                      evidence.Diagnostics |> List.map (fun item -> "- " + item) ]

        File.WriteAllLines(path, lines)
        path

    let renderPackage request =
        let inspectionOptions =
            { SceneCodec.defaultInspectionOptions with
                Resources = request.Resources }

        let report = SceneCodec.inspectWith inspectionOptions request.PackageBytes
        let diagnostics = SceneCodec.formatDiagnostics report.Diagnostics

        match report.Status, SceneCodec.importPackage request.PackageBytes with
        | PackageRejected, _ ->
            failure request ReferencePackageResourceIncompatibility ("package inspection rejected reference rendering" :: diagnostics)
        | _, Result.Error importDiagnostics ->
            failure request ReferencePackageResourceIncompatibility (SceneCodec.formatDiagnostics importDiagnostics)
        | _, Result.Ok package ->
            try
                match renderScenePng request.OutputSize package.Scene with
                | Result.Error message -> failure request ReferenceUnsupportedEnvironment (message :: diagnostics)
                | Result.Ok pngBytes ->
                    Directory.CreateDirectory(request.OutputDirectory) |> ignore
                    let imagePath = Path.Combine(request.OutputDirectory, package.PackageIdentity.Replace(":", "-") + ".png")
                    File.WriteAllBytes(imagePath, pngBytes)

                    if imageNonBlank pngBytes then
                        { PackageIdentity = package.PackageIdentity
                          ProtocolVersion = Some package.Version
                          CapabilityProfile = report.ProfileId |> Option.defaultValue package.ProfileId
                          ResourceStatus = resourceSummary report
                          OutputSize = request.OutputSize
                          ImagePath = Some imagePath
                          ImageIdentity = Some("sha256:" + sha256Hex pngBytes)
                          RendererIdentity = rendererIdentity
                          Verdict = ReferencePassed
                          Classification = None
                          Diagnostics = diagnostics }
                    else
                        failure request ReferenceVerificationDepth ("reference PNG decoded but did not contain non-transparent pixels" :: diagnostics)
            with
            | :? DllNotFoundException as ex -> failure request ReferenceUnsupportedEnvironment [ ex.Message ]
            | :? EntryPointNotFoundException as ex -> failure request ReferenceUnsupportedEnvironment [ ex.Message ]
            | :? TypeInitializationException as ex -> failure request ReferenceUnsupportedEnvironment [ ex.Message ]
            | ex -> failure request ReferenceProductDefect (ex.Message :: diagnostics)

    let run request =
        let evidence = renderPackage request
        writeEvidenceSummary request.OutputDirectory evidence |> ignore
        evidence
