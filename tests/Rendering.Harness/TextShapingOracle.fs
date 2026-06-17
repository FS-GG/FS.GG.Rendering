namespace Rendering.Harness

open FS.GG.UI.Scene

module TextShapingOracle =
    let assertFingerprintStable (result: ShapedTextResult) =
        Scene.shapedTextFingerprint result = result.Fingerprint

    let measureDrawAdvanceDelta (result: ShapedTextResult) =
        let drawAdvance = result.Glyphs |> List.sumBy _.Advance
        abs (result.Metrics.Advance - drawAdvance)

    let diagnosticsCoverFixture (fixture: TextShapingFixture) (result: ShapedTextResult) =
        if fixture.ExpectsMissingGlyph then
            result.Diagnostics |> List.exists (fun d -> d.Contains("tofu") || d.Contains("missing"))
        elif fixture.ExpectsFallback then
            result.Diagnostics |> List.exists (fun d -> d.Contains("fallback") || d.Contains("substituted"))
        else
            true
