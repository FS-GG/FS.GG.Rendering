namespace Rendering.Harness

open System.IO
open SkiaSharp
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

module GoldenImage =

    type ImageComparison =
        { Width: int
          Height: int
          TotalPixels: int
          DiffPixelCount: int
          MaxChannelDelta: int }

    type GoldenTolerance =
        { ChannelTolerance: int
          MaxDiffPixels: int }

    type GoldenOutcome =
        | Equivalent of ImageComparison
        | Drifted of ImageComparison
        | DimensionMismatch of referenceWidth: int * referenceHeight: int * candidateWidth: int * candidateHeight: int
        | Undecodable of reason: string

    type CandidateStatus =
        | Rendered of GoldenOutcome
        | EnvironmentLimited of reason: string
        | RenderFailed of reason: string

    type SceneGolden =
        { ScenarioId: string
          Status: CandidateStatus
          Diagnostics: string list }

    let exact = { ChannelTolerance = 0; MaxDiffPixels = 0 }

    // A deliberately small budget: a couple of levels of per-channel drift on a handful of antialiased
    // edge pixels, which is the shape a benign Skia/font-version change takes. It is nowhere near large
    // enough to hide a structural render regression (a moved/dropped shape moves thousands of pixels by
    // hundreds of levels), which the injected-regression test pins.
    let perceptual = { ChannelTolerance = 2; MaxDiffPixels = 16 }

    // The size `RenderAnywhere.runReferenceCommand` renders the corpus at, and therefore the size of
    // every committed reference PNG. Kept in lockstep with that command so the candidate and the
    // reference are the same dimensions by construction.
    let private referenceOutputSize: Size = { Width = 192; Height = 128 }

    let private decode (png: byte[]) : SKBitmap option =
        try
            let bitmap = SKBitmap.Decode(png)
            if isNull bitmap then None else Some bitmap
        with _ ->
            None

    let compareImages (tolerance: GoldenTolerance) (referencePng: byte[]) (candidatePng: byte[]) : GoldenOutcome =
        match decode referencePng, decode candidatePng with
        | None, _ -> Undecodable "reference PNG could not be decoded"
        | _, None -> Undecodable "candidate PNG could not be decoded"
        | Some referenceBitmap, Some candidateBitmap ->
            use reference = referenceBitmap
            use candidate = candidateBitmap

            if reference.Width <> candidate.Width || reference.Height <> candidate.Height then
                DimensionMismatch(reference.Width, reference.Height, candidate.Width, candidate.Height)
            else
                let referencePixels = reference.Pixels
                let candidatePixels = candidate.Pixels
                let mutable diffPixelCount = 0
                let mutable maxChannelDelta = 0

                for index in 0 .. referencePixels.Length - 1 do
                    let a = referencePixels.[index]
                    let b = candidatePixels.[index]
                    let dr = abs (int a.Red - int b.Red)
                    let dg = abs (int a.Green - int b.Green)
                    let db = abs (int a.Blue - int b.Blue)
                    let da = abs (int a.Alpha - int b.Alpha)
                    let worst = max (max dr dg) (max db da)

                    if worst > maxChannelDelta then
                        maxChannelDelta <- worst

                    if worst > tolerance.ChannelTolerance then
                        diffPixelCount <- diffPixelCount + 1

                let comparison =
                    { Width = reference.Width
                      Height = reference.Height
                      TotalPixels = referencePixels.Length
                      DiffPixelCount = diffPixelCount
                      MaxChannelDelta = maxChannelDelta }

                if diffPixelCount <= tolerance.MaxDiffPixels then
                    Equivalent comparison
                else
                    Drifted comparison

    let referencePng (referenceDirectory: string) (scenarioId: string) : Result<byte[], string> =
        let directory = Path.Combine(referenceDirectory, scenarioId)

        if not (Directory.Exists directory) then
            Result.Error $"reference directory missing: {directory}"
        else
            match Directory.GetFiles(directory, "*.png") |> Array.sort with
            | [||] -> Result.Error $"no reference PNG under {directory}"
            | files -> Result.Ok(File.ReadAllBytes(Array.head files))

    let gateScene (tolerance: GoldenTolerance) (referenceDirectory: string) (item: RenderAnywhere.CorpusItem) : SceneGolden =
        let scenarioId = item.ScenarioId

        match SceneCodec.importPackage item.Package.CanonicalBytes with
        | Result.Error diagnostics ->
            { ScenarioId = scenarioId
              Status = RenderFailed "package import failed"
              Diagnostics = SceneCodec.formatDiagnostics diagnostics }
        | Result.Ok package ->
            match ReferenceRendering.renderScenePngResult referenceOutputSize package.Scene with
            | Result.Error failure when failure.Classification = SceneEvidenceFailureClassification.UnsupportedEnvironment ->
                { ScenarioId = scenarioId
                  Status = EnvironmentLimited failure.Message
                  Diagnostics = [ failure.Message ] }
            | Result.Error failure ->
                { ScenarioId = scenarioId
                  Status = RenderFailed failure.Message
                  Diagnostics = [ failure.Message ] }
            | Result.Ok candidatePng ->
                match referencePng referenceDirectory scenarioId with
                | Result.Error message ->
                    { ScenarioId = scenarioId
                      Status = RenderFailed message
                      Diagnostics = [ message ] }
                | Result.Ok referenceBytes ->
                    { ScenarioId = scenarioId
                      Status = Rendered(compareImages tolerance referenceBytes candidatePng)
                      Diagnostics = [] }

    let gateCorpus (tolerance: GoldenTolerance) (referenceDirectory: string) : SceneGolden list =
        RenderAnywhere.corpus () |> List.map (gateScene tolerance referenceDirectory)

    let summarize (results: SceneGolden list) : string list =
        [ "# Golden-image gate"
          ""
          for result in results do
              let status =
                  match result.Status with
                  | Rendered(Equivalent c) -> $"equivalent (max-channel-delta={c.MaxChannelDelta}, diff-pixels={c.DiffPixelCount}/{c.TotalPixels})"
                  | Rendered(Drifted c) -> $"DRIFTED (max-channel-delta={c.MaxChannelDelta}, diff-pixels={c.DiffPixelCount}/{c.TotalPixels})"
                  | Rendered(DimensionMismatch(rw, rh, cw, ch)) -> $"DIMENSION-MISMATCH (reference={rw}x{rh}, candidate={cw}x{ch})"
                  | Rendered(Undecodable reason) -> $"UNDECODABLE ({reason})"
                  | EnvironmentLimited reason -> $"environment-limited ({reason})"
                  | RenderFailed reason -> $"RENDER-FAILED ({reason})"

              $"- {result.ScenarioId}: {status}"
              yield! result.Diagnostics |> List.map (fun d -> $"    - {d}") ]
