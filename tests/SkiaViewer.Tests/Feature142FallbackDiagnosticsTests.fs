module Feature142FallbackDiagnosticsTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private font: FontSpec = { Family = None; Size = 18.0; Weight = None }

[<Tests>]
let tests =
    testList "Feature142 fallback diagnostics" [
        test "clearing the provider is explicit" {
            let status = Text.clearShapingProvider ()
            let shaped = Text.shapeText "fallback" font

            Expect.equal status.Evidence.Availability ProviderCleared "provider clear is visible"
            Expect.equal shaped.FallbackMode ProviderUnavailableFallback "shape requests fall back explicitly"
        }

        test "negative missing-glyph fixtures disclose affected text" {
            Text.installShapingProvider () |> ignore
            let shaped = Text.shapeText "\uFFFF" font

            Expect.isTrue (shaped.Diagnostics |> List.exists (fun d -> d.Contains("tofu") || d.Contains("missing"))) "missing glyph diagnostic is surfaced"
        }
    ]
