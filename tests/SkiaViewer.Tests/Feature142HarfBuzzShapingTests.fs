module Feature142HarfBuzzShapingTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private font: FontSpec = { Family = Some "Noto Sans"; Size = 24.0; Weight = None }

[<Tests>]
let tests =
    testSequenced
    <| testList "Feature142 HarfBuzz shaping provider" [
        test "provider install exposes HarfBuzz evidence and shaped results" {
            Fonts.withInstalledShapingProvider (fun () ->
                let status = Text.shapingProviderStatus ()
                let shaped = Text.shapeText "office" font

                Expect.equal status.Evidence.Availability ProviderInstalled "provider is installed"
                Expect.equal shaped.Provider.Availability ProviderInstalled "shape result carries provider evidence"
                Expect.isNonEmpty shaped.Glyphs "shaped glyph ids are recorded"
                Expect.isNonEmpty shaped.Fingerprint "fingerprint is populated")
        }

        test "measure projection uses the shaped result aggregate" {
            Fonts.withInstalledShapingProvider (fun () ->
                let shaped = Text.shapeText "AV office" font
                let measured = Scene.measureShapedText shaped

                Expect.floatClose Accuracy.medium measured.Width shaped.Metrics.Advance "measure uses shaped advance"
                Expect.isTrue (shaped.Metrics.Advance >= 0.0) "advance is non-negative")
        }
    ]
