module Feature142FallbackDiagnosticsTests

open System.Collections.Concurrent
open System.Threading.Tasks
open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.SkiaViewer

let private font: FontSpec = { Family = None; Size = 18.0; Weight = None }

[<Tests>]
let tests =
    testSequenced
    <| testList "Feature142 fallback diagnostics" [
        test "clearing the provider is explicit" {
            Fonts.withClearedShapingProvider (fun () ->
                let status = Text.shapingProviderStatus ()
                let shaped = Text.shapeText "fallback" font

                Expect.equal status.Evidence.Availability ProviderCleared "provider clear is visible"
                Expect.equal shaped.FallbackMode ProviderUnavailableFallback "shape requests fall back explicitly")
        }

        test "negative missing-glyph fixtures disclose affected text" {
            Fonts.withInstalledShapingProvider (fun () ->
                let shaped = Text.shapeText "\uFFFF" font

                Expect.isTrue
                    (shaped.Diagnostics |> List.exists (fun d -> d.Contains("tofu") || d.Contains("missing")))
                    "missing glyph diagnostic is surfaced")
        }

        test "scoped provider selection stays isolated and restores state under parallel stress" {
            let previous = Text.shapingProviderStatus ()
            let failures = ConcurrentQueue<string>()

            Parallel.For(
                0,
                200,
                fun iteration ->
                    try
                        if iteration % 2 = 0 then
                            Fonts.withClearedShapingProvider (fun () ->
                                let shaped = Text.shapeText "fallback" font

                                if shaped.Provider.Availability <> ProviderCleared
                                   || shaped.FallbackMode <> ProviderUnavailableFallback then
                                    failures.Enqueue $"iteration {iteration}: cleared scope observed {shaped.Provider.Availability}/{shaped.FallbackMode}")
                        else
                            Fonts.withInstalledShapingProvider (fun () ->
                                let shaped = Text.shapeText "office" font

                                if shaped.Provider.Availability <> ProviderInstalled
                                   || shaped.FallbackMode <> Shaped then
                                    failures.Enqueue $"iteration {iteration}: installed scope observed {shaped.Provider.Availability}/{shaped.FallbackMode}")
                    with ex ->
                        failures.Enqueue $"iteration {iteration}: {ex.Message}")
            |> ignore

            Expect.isEmpty failures "parallel scopes never observe another scope's provider"
            Expect.equal (Text.shapingProviderStatus ()) previous "parallel scopes restore the exact prior provider status"
        }
    ]
