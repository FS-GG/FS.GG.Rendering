namespace FS.GG.UI.Scene

open System

type SceneEvidenceFormat =
    | Hash
    | Png
    | Metadata

type SceneEvidenceFailureClassification =
    | UnsupportedEnvironment
    | ProductDefect

type SceneEvidenceFailure =
    { BlockedStage: string
      Classification: SceneEvidenceFailureClassification
      DiagnosticCategory: string
      Message: string }

type SceneEvidenceRequest =
    { Scene: Scene
      OutputSize: Size
      Format: SceneEvidenceFormat
      RendererMode: string
      EvidencePath: string option }

type SceneEvidence =
    { Format: SceneEvidenceFormat
      OutputSize: Size
      RendererMode: string
      EvidencePath: string option
      Value: string }

// Feature 105 (US3, FR-009): the closed set of scene-evidence failure stages, typed so the
// internal classification is a compile-checked DU instead of a bare string. The public
// `SceneEvidenceFailure.BlockedStage`/`DiagnosticCategory` fields stay `string`, written via the
// single `EvidenceStage.name` projection at construction, so the evidence text is byte-identical
// "scene"/"renderer". Hidden from consumers by absence from Scene.fsi.
[<RequireQualifiedAccess>]
type EvidenceStage =
    | Scene
    | Renderer

module SceneEvidence =
    let stageName (stage: EvidenceStage) : string =
        match stage with
        | EvidenceStage.Scene -> "scene"
        | EvidenceStage.Renderer -> "renderer"

    // Feature 221 (US1, FR-001/FR-004): the real headless PNG rasterizer seam, mirroring
    // `Scene.setRealTextMeasurer`. `src/Scene` stays SkiaSharp-free; `SkiaViewer` injects a CPU
    // rasterizer (`ReferenceRendering.renderScenePngResult`) here so `renderPng` returns real pixels.
    // `None` (the default) ⇒ `renderPng` returns the typed `UnsupportedEnvironment` failure (FR-005),
    // never the prior hash/stub bytes. Disclosed interpreter-edge mutation (constitution III/IV); the
    // injected closure must be re-entrant so concurrent `renderPng` calls stay isolated (Edge Case).
    let mutable private realPngRasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) option =
        None

    let setRealPngRasterizer (rasterizer: (Size -> Scene -> Result<byte[], SceneEvidenceFailure>) option) : unit =
        realPngRasterizer <- rasterizer

    let supportedRendererMode mode =
        String.IsNullOrWhiteSpace mode
        || String.Equals(mode, "deterministic-scene", StringComparison.Ordinal)

    let writeEvidence (path: string) (value: string) =
        let directory = IO.Path.GetDirectoryName(path)

        if not (String.IsNullOrWhiteSpace directory) then
            IO.Directory.CreateDirectory(directory |> string) |> ignore

        IO.File.WriteAllText(path, value)

    let render (request: SceneEvidenceRequest) =
        if request.OutputSize.Width <= 0 || request.OutputSize.Height <= 0 then
            Result.Error
                { BlockedStage = stageName EvidenceStage.Scene
                  Classification = ProductDefect
                  DiagnosticCategory = stageName EvidenceStage.Scene
                  Message = "Scene evidence output size must be positive." }
        elif not (supportedRendererMode request.RendererMode) then
            Result.Error
                { BlockedStage = stageName EvidenceStage.Renderer
                  Classification = UnsupportedEnvironment
                  DiagnosticCategory = stageName EvidenceStage.Renderer
                  Message = $"Scene evidence renderer mode '{request.RendererMode}' is not available for non-window deterministic evidence." }
        else
            let readback = Scene.renderReadbackEvidence request.OutputSize request.Scene

            let value =
                match request.Format with
                | Hash -> readback.DeterministicHash
                | Metadata -> $"size={request.OutputSize.Width}x{request.OutputSize.Height};capabilities={readback.CapabilityCount};hash={readback.DeterministicHash}"
                | Png -> readback.DeterministicHash

            request.EvidencePath |> Option.iter (fun path -> writeEvidence path value)

            Result.Ok
                { Format = request.Format
                  OutputSize = request.OutputSize
                  RendererMode = "deterministic-scene"
                  EvidencePath = request.EvidencePath
                  Value = value }

    /// A capability-set digest of `scene`: it hashes the sorted, DISTINCT set of element-type markers
    /// produced by `describe` (plus the output size), deliberately discarding every node PAYLOAD —
    /// geometry, colour, and OPACITY/ALPHA. Consequently an opacity-only (or any value-only) change does
    /// NOT change `renderHash` (Workstream E3 limitation, by design — this is a coarse "what kinds of things
    /// are drawn" hash, not a render fingerprint). For a collision-resistant, value-sensitive (alpha-sensitive)
    /// structural fingerprint, use feature 120's `RetainedRender.hashScene` instead.
    let renderHash size scene =
        render
            { Scene = scene
              OutputSize = size
              Format = Hash
              RendererMode = "deterministic-scene"
              EvidencePath = None }

    let renderPng size scene =
        // Feature 221 (US1/US3): validate first through the existing `render` path so the size/mode
        // rules and their classifications are preserved EXACTLY (zero/negative size → `ProductDefect`,
        // unsupported mode → `UnsupportedEnvironment`; FR-007, contract C1.4). On success we DISCARD the
        // hash-bearing record and source real pixels from the injected CPU rasterizer instead — the
        // prior `Encoding.UTF8.GetBytes hash` stub is gone (FR-002/SC-005, contract C1.5).
        match
            render
                { Scene = scene
                  OutputSize = size
                  Format = Png
                  RendererMode = "deterministic-scene"
                  EvidencePath = None }
        with
        | Result.Error failure -> Result.Error failure
        | Result.Ok _ ->
            match realPngRasterizer with
            | Some rasterize -> rasterize size scene
            | None ->
                // FR-001/FR-005 (contract C1.3): no CPU rasterizer injected ⇒ real pixels genuinely
                // cannot be produced here. Fail honestly, naming the blocked Renderer stage; write
                // nothing, never a success-shaped non-image.
                Result.Error
                    { BlockedStage = stageName EvidenceStage.Renderer
                      Classification = UnsupportedEnvironment
                      DiagnosticCategory = stageName EvidenceStage.Renderer
                      Message =
                        "Headless PNG evidence requires the SkiaViewer CPU rasterizer to be injected via SceneEvidence.setRealPngRasterizer; no rasterizer is installed in this environment." }

module LayoutEvidence =
    let private intersects (first: Rect) (second: Rect) =
        first.X < second.X + second.Width
        && first.X + first.Width > second.X
        && first.Y < second.Y + second.Height
        && first.Y + first.Height > second.Y

    let private overlapDiagnostics (report: LayoutEvidenceReport) =
        let hudTextOverlaps =
            report.TextBounds
            |> List.mapi (fun index (first: LayoutTextBounds) ->
                report.TextBounds
                |> List.skip (index + 1)
                |> List.choose (fun (second: LayoutTextBounds) ->
                    if intersects first.Bounds second.Bounds then
                        Some
                            { Kind = HudTextOverlap
                              FirstName = first.Name
                              SecondName = Some second.Name
                              Bounds = first.Bounds
                              Message = $"HUD text '{first.Name}' overlaps '{second.Name}'" }
                    else
                        None))
            |> List.concat

        let hudGameplayOverlaps =
            report.TextBounds
            |> List.collect (fun (text: LayoutTextBounds) ->
                report.GameplayBounds
                |> List.choose (fun (gameplay: LayoutGameplayBounds) ->
                    if intersects text.Bounds gameplay.Bounds then
                        Some
                            { Kind = HudGameplayOverlap
                              FirstName = text.Name
                              SecondName = Some gameplay.Name
                              Bounds = text.Bounds
                              Message = $"HUD text '{text.Name}' overlaps gameplay '{gameplay.Name}'" }
                    else
                        None))

        hudTextOverlaps @ hudGameplayOverlaps

    let classify (report: LayoutEvidenceReport) =
        let overlaps = overlapDiagnostics report

        let missingFacts =
            report.HudRegion.IsNone
            || report.GameplayRegion.IsNone
            || report.TextBounds.IsEmpty
            || report.GameplayBounds.IsEmpty

        if not report.UnsupportedReasons.IsEmpty || report.MeasurementMode = UnsupportedTextBounds then
            { report with
                ProofLevel = UnsupportedLayoutInspection
                OverlapStatus = if overlaps.IsEmpty then report.OverlapStatus else LayoutOverlaps overlaps
                Diagnostics =
                    report.Diagnostics
                    @ (report.UnsupportedReasons |> List.map (fun reason -> $"{reason.Fact}: {reason.Reason}"))
                    @ (overlaps |> List.map _.Message) }
        elif missingFacts || not overlaps.IsEmpty then
            { report with
                ProofLevel = DeterministicRenderOnly
                OverlapStatus = if overlaps.IsEmpty then report.OverlapStatus else LayoutOverlaps overlaps
                Diagnostics =
                    report.Diagnostics
                    @ [ if report.HudRegion.IsNone then "missing HUD region"
                        if report.GameplayRegion.IsNone then "missing gameplay region"
                        if report.TextBounds.IsEmpty then "missing HUD text bounds"
                        if report.GameplayBounds.IsEmpty then "missing gameplay bounds"
                        yield! overlaps |> List.map _.Message ] }
        else
            { report with
                ProofLevel = ReadableLayout
                OverlapStatus = NoLayoutOverlap }

    let fromRenderEvidence scene (evidence: RenderReadbackEvidence) : LayoutEvidenceReport =
        { Scene = scene
          OutputSize = evidence.Size
          ProofLevel = DeterministicRenderOnly
          HudRegion = None
          GameplayRegion = None
          TextBounds = []
          GameplayBounds = []
          OverlapStatus = NoLayoutOverlap
          MeasurementMode = ApproximateTextBounds
          UnsupportedReasons = []
          Diagnostics = [ "deterministic render metadata only" ]
          RenderEvidence = Some evidence }

    let unsupported scene outputSize (reason: LayoutUnsupportedReason) : LayoutEvidenceReport =
        { Scene = scene
          OutputSize = outputSize
          ProofLevel = UnsupportedLayoutInspection
          HudRegion = None
          GameplayRegion = None
          TextBounds = []
          GameplayBounds = []
          OverlapStatus = NoLayoutOverlap
          MeasurementMode = UnsupportedTextBounds
          UnsupportedReasons = [ reason ]
          Diagnostics = [ $"unsupported layout fact: {reason.Fact}; {reason.Reason}" ]
          RenderEvidence = None }
