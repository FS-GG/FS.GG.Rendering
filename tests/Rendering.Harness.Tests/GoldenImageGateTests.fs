module GoldenImageGateTests

// The §7 golden-IMAGE equivalence gate for the god-module decomposition
// (`docs/reports/2026-06-21-23-57-god-module-decomposition-analysis-and-plan.md`, §7.1). Four
// responsibilities, mirroring how the Feature 190 golden-HASH gate proves itself non-vacuous:
//   (a) REAL gate — every corpus scene, freshly rendered through the in-process CPU raster, is
//       perceptually equivalent to its committed reference PNG (and byte-identical under `exact` in
//       this environment). This is the regression assertion the SkiaViewer/Control/RetainedRender
//       decomposition must keep green.
//   (b) IDENTITY — a PNG compared against itself is `Equivalent` with zero drift, even under `exact`.
//   (c) INJECTED-REGRESSION proof — a deliberately perturbed reference exceeds the perceptual budget
//       and comes back `Drifted`, so the gate is discriminating, not vacuous. It also confirms the
//       tolerance does NOT swallow a structural change.
//   (d) FAIL-CLOSED — a dimension mismatch and an undecodable PNG are typed non-matches, never a
//       false `Equivalent`.

open System.IO
open Expecto
open SkiaSharp
open FS.GG.TestSupport
open Rendering.Harness
open Rendering.Harness.GoldenImage

let private root = RepositoryRoot.value
let private repo (path: string) = Path.Combine(root, path.Replace('/', Path.DirectorySeparatorChar))
let private referenceDirectory = repo RenderAnywhere.referenceDirectory

/// The single committed reference PNG for a scenario (fails the test loudly if it cannot be found).
let private loadReference scenarioId =
    match GoldenImage.referencePng referenceDirectory scenarioId with
    | Result.Ok bytes -> bytes
    | Result.Error message -> failtestf "could not load reference PNG for %s: %s" scenarioId message

/// Decode `png`, mutate a contrasting block of pixels to force a structural-scale change, re-encode.
/// The perturbation is far larger than any benign antialiasing drift the perceptual budget forgives.
let private perturb (png: byte[]) : byte[] =
    use bitmap = SKBitmap.Decode(png)
    for y in 0 .. min 31 (bitmap.Height - 1) do
        for x in 0 .. min 31 (bitmap.Width - 1) do
            let original = bitmap.GetPixel(x, y)
            // Invert every channel: a maximal per-channel delta over a 32x32 block (>=1024 pixels),
            // dwarfing `perceptual.MaxDiffPixels`.
            bitmap.SetPixel(x, y, SKColor(255uy - original.Red, 255uy - original.Green, 255uy - original.Blue, 255uy))
    use image = SKImage.FromBitmap(bitmap)
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    data.ToArray()

/// A 1x1 transparent PNG, decodable but the wrong size for any corpus reference.
let private onePixelPng () : byte[] =
    let info = SKImageInfo(1, 1, SKColorType.Rgba8888, SKAlphaType.Premul)
    use surface = SKSurface.Create(info)
    surface.Canvas.Clear(SKColors.Transparent)
    use image = surface.Snapshot()
    use data = image.Encode(SKEncodedImageFormat.Png, 100)
    data.ToArray()

[<Tests>]
let tests =
    testList "GoldenImageGate" [
        test "corpus renders perceptually equivalent to committed references" {
            let results = GoldenImage.gateCorpus GoldenImage.perceptual referenceDirectory

            // Non-vacuous: the gate must actually cover every corpus scene.
            Expect.equal results.Length (RenderAnywhere.corpus () |> List.length) "one result per corpus scene"
            Expect.isNonEmpty results "corpus is non-empty"

            // Environment-limited hosts (no Skia native) produce no candidate; the gate proves nothing
            // there, so skip rather than false-green. Every host that DID raster must be equivalent, and
            // a render failure is a hard defect.
            let rendered =
                results
                |> List.choose (fun r ->
                    match r.Status with
                    | Rendered outcome -> Some(r.ScenarioId, outcome)
                    | RenderFailed reason -> failtestf "%s render failed: %s" r.ScenarioId reason
                    | EnvironmentLimited _ -> None)

            if rendered.IsEmpty then
                skiptest "Skia raster unavailable in this host — no candidate produced (environment-limited)"

            for scenarioId, outcome in rendered do
                match outcome with
                | Equivalent _ -> ()
                | other -> failtestf "%s is not equivalent to its reference: %A" scenarioId other
        }

        test "corpus is byte-identical to committed references under the exact budget in this environment" {
            let results = GoldenImage.gateCorpus GoldenImage.exact referenceDirectory

            let rendered =
                results
                |> List.choose (fun r ->
                    match r.Status with
                    | Rendered outcome -> Some(r.ScenarioId, outcome)
                    | _ -> None)

            if rendered.IsEmpty then
                skiptest "Skia raster unavailable in this host (environment-limited)"

            for scenarioId, outcome in rendered do
                match outcome with
                | Equivalent comparison ->
                    Expect.equal comparison.DiffPixelCount 0 (sprintf "%s: zero differing pixels under exact" scenarioId)
                    Expect.equal comparison.MaxChannelDelta 0 (sprintf "%s: zero channel delta under exact" scenarioId)
                | other -> failtestf "%s drifted under the exact budget: %A" scenarioId other
        }

        test "a reference compared against itself is exactly equivalent" {
            let png = loadReference "basic-primitives"

            match GoldenImage.compareImages GoldenImage.exact png png with
            | Equivalent comparison ->
                Expect.equal comparison.DiffPixelCount 0 "self-compare has no differing pixels"
                Expect.equal comparison.MaxChannelDelta 0 "self-compare has no channel delta"
                Expect.isGreaterThan comparison.TotalPixels 0 "self-compare covered real pixels"
            | other -> failtestf "self-compare was not equivalent: %A" other
        }

        test "a perturbed reference drifts beyond the perceptual budget (gate is discriminating)" {
            let reference = loadReference "basic-primitives"
            let perturbed = perturb reference

            // The perturbation is a real change, not a re-encode no-op.
            Expect.notEqual perturbed reference "perturbed bytes differ from the reference"

            match GoldenImage.compareImages GoldenImage.perceptual reference perturbed with
            | Drifted comparison ->
                Expect.isGreaterThan comparison.DiffPixelCount GoldenImage.perceptual.MaxDiffPixels "drift exceeds the pixel budget"
                Expect.isGreaterThan comparison.MaxChannelDelta GoldenImage.perceptual.ChannelTolerance "drift exceeds the channel budget"
            | other -> failtestf "the perceptual gate failed to flag a 32x32 inversion: %A" other
        }

        test "a dimension mismatch is a typed non-match, not a false pass" {
            let reference = loadReference "basic-primitives"
            let wrongSize = onePixelPng ()

            match GoldenImage.compareImages GoldenImage.perceptual reference wrongSize with
            | DimensionMismatch(rw, rh, cw, ch) ->
                Expect.isGreaterThan (rw * rh) (cw * ch) "reference is larger than the 1x1 candidate"
                Expect.equal (cw, ch) (1, 1) "candidate is the 1x1 PNG"
            | other -> failtestf "a size mismatch was not reported as DimensionMismatch: %A" other
        }

        test "an undecodable candidate fails closed" {
            let reference = loadReference "basic-primitives"
            let garbage = [| 0uy; 1uy; 2uy; 3uy; 4uy |]

            match GoldenImage.compareImages GoldenImage.perceptual reference garbage with
            | Undecodable _ -> ()
            | other -> failtestf "undecodable candidate bytes did not fail closed: %A" other
        }
    ]
