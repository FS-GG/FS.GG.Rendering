module Feature142ShapedTextTests

open System
open System.IO
open Expecto
open FS.GG.UI.Scene
open FS.GG.TestSupport

let private font: FontSpec = { Family = Some "Inter"; Size = 18.0; Weight = None }

let private root = RepositoryRoot.value

[<Tests>]
let tests =
    testList "Feature142 Scene shaped text data" [
        test "fallback shaped text exposes metrics, glyphs, runs, provider evidence, and fingerprint" {
            let shaped = Scene.buildFallbackShapedText "office" font

            Expect.equal shaped.Provider.Availability ProviderUnavailable "Scene fallback does not claim a provider"
            Expect.equal shaped.FallbackMode PureFallbackMode "Scene fallback mode is explicit"
            Expect.isNonEmpty shaped.Runs "run evidence is present"
            Expect.isNonEmpty shaped.Glyphs "glyph evidence is present"
            Expect.floatClose Accuracy.medium shaped.Metrics.Advance (shaped.Glyphs |> List.sumBy _.Advance) "advance comes from the glyph evidence"
            Expect.equal (Scene.shapedTextFingerprint shaped) shaped.Fingerprint "fingerprint is recomputable"
        }

        test "glyph-run data can be projected from shaped text" {
            let shaped = Scene.buildFallbackShapedText "Stable" font
            let data = Scene.glyphRunDataFromShapedText shaped

            Expect.equal data.Provider shaped.Provider "provider evidence is carried"
            Expect.equal data.FallbackMode shaped.FallbackMode "fallback mode is carried"
            Expect.equal (Scene.measureGlyphRun data).Width shaped.Metrics.Width "metrics project to the legacy shape"
        }

        test "Scene project remains dependency-light" {
            let fsproj = File.ReadAllText(Path.Combine(root, "src", "Scene", "Scene.fsproj"))
            for forbidden in [ "SkiaSharp"; "HarfBuzzSharp"; "SkiaViewer"; "Controls"; "Elmish"; "Yoga"; "Silk.NET" ] do
                Expect.isFalse (fsproj.Contains forbidden) $"Scene.fsproj must not reference {forbidden}"
        }
    ]
