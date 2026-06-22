namespace FS.GG.UI.Scene.Text

open System
open System.Security.Cryptography
open System.Text
open FS.GG.UI.Scene

/// Feature 188 (US2): the unified shaped-text core relocated out of `module Scene`. `module Scene`
/// keeps thin public delegations into here, so glyph runs / shaped text / fingerprints / measurement
/// stay byte-identical (field/accumulation order preserved verbatim).
module internal Shaping =
    let private measureTextHeuristic (text: string) (font: FontSpec) =
        // Feature 136 (R2/T016): the pure, host-independent heuristic kept for pure callers and pure
        // goldens. The per-glyph advance ratio `0.58·size` is calibrated against the bundled default
        // family (Noto Sans averages ~0.49·size; the probe in research.md R1 measured "Stable" at
        // 0.49·size·n): it stays deliberately *conservative* (>= the real average advance) so a box
        // sized by this heuristic is never narrower than the bundled-font renderer draws.
        let size = max 1.0 font.Size
        let glyphAdvance = max 1.0 (size * 0.58)

        { Width = glyphAdvance * float text.Length
          Height = size
          Baseline = size * 0.8 }

    let private pureFallbackProvider =
        { Availability = ProviderUnavailable
          ProviderId = "scene-pure-fallback"
          VersionBucket = "scene-pure-fallback/v1"
          Failure = None }

    let private directionOf (text: string) =
        let mutable hasRtl = false
        let mutable hasLtr = false

        for ch in text do
            let code = int ch

            if (code >= 0x0590 && code <= 0x08FF) || (code >= 0xFB1D && code <= 0xFEFC) then
                hasRtl <- true
            elif Char.IsLetter ch then
                hasLtr <- true

        match hasLtr, hasRtl with
        | true, true -> MixedDirection
        | false, true -> RightToLeft
        | true, false -> LeftToRight
        | false, false -> AutoDirection

    let private scriptOf (text: string) =
        let buckets =
            text
            |> Seq.choose (fun ch ->
                let code = int ch

                if (code >= 0x0041 && code <= 0x024F) then Some LatinScript
                elif code >= 0x0600 && code <= 0x06FF then Some ArabicScript
                elif code >= 0x0900 && code <= 0x097F then Some DevanagariScript
                elif code >= 0x0E00 && code <= 0x0E7F then Some ThaiScript
                elif code >= 0x2600 && code <= 0x27BF then Some SymbolScript
                elif Char.IsSurrogate ch then Some EmojiScript
                elif Char.IsLetterOrDigit ch then Some UnknownScript
                else None)
            |> Seq.distinct
            |> Seq.toList

        match buckets with
        | [] -> AutoScript
        | [ single ] -> single
        | _ -> MixedScript

    let private glyphRunFingerprintOf provider runs fallbackMode text font glyphs metrics diagnostics =
        let glyphPayload =
            glyphs
            |> List.map (fun g ->
                sprintf
                    "%d:%s:%.12g:%.12g:%.12g:%d:%.12g:%.12g:%s:%b"
                    g.GlyphId
                    g.SourceText
                    g.Advance
                    g.Offset.X
                    g.Offset.Y
                    g.Cluster
                    g.Position.X
                    g.Position.Y
                    (g.ResolvedFace |> Option.defaultValue "")
                    g.Missing)
            |> String.concat "|"

        let runPayload =
            runs
            |> List.map (fun r ->
                let start, length = r.TextRange
                sprintf "%d:%d:%s:%A:%A:%A:%.12g:%s" start length r.SourceText r.Direction r.Script r.FallbackDecision r.Advance (String.concat ";" r.Diagnostics))
            |> String.concat "|"

        let payload =
            String.concat
                "\u001f"
                [ sprintf "%A:%s:%s:%A" provider.Availability provider.ProviderId provider.VersionBucket provider.Failure
                  sprintf "%A" fallbackMode
                  runPayload
                  text
                  sprintf "%A" font
                  glyphPayload
                  sprintf "%.12g:%.12g:%.12g" metrics.Advance metrics.Height metrics.Baseline
                  String.concat "|" diagnostics ]

        SHA256.HashData(Encoding.UTF8.GetBytes payload)
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    let private shapedTextFingerprintOf (result: ShapedTextResult) =
        let glyphPayload =
            result.Glyphs
            |> List.map (fun g ->
                sprintf
                    "%d:%d:%s:%s:%.12g:%.12g:%.12g:%.12g:%.12g:%b"
                    g.GlyphId
                    g.SourceCluster
                    g.SourceText
                    (g.ResolvedFace |> Option.defaultValue "")
                    g.Advance
                    g.Offset.X
                    g.Offset.Y
                    g.Position.X
                    g.Position.Y
                    g.Missing)
            |> String.concat "|"

        let runPayload =
            result.Runs
            |> List.map (fun r ->
                let start, length = r.TextRange
                sprintf "%d:%d:%s:%s:%A:%A:%A:%.12g:%s" start length r.SourceText (r.ResolvedFont |> Option.defaultValue "") r.Direction r.Script r.FallbackDecision r.Advance (String.concat ";" r.Diagnostics))
            |> String.concat "|"

        let boundsPayload =
            result.Metrics.Bounds
            |> Option.map (fun b -> sprintf "%.12g:%.12g:%.12g:%.12g" b.X b.Y b.Width b.Height)
            |> Option.defaultValue ""

        let payload =
            String.concat
                "\u001f"
                [ result.Text
                  sprintf "%A" result.Font
                  sprintf "%A:%s:%s:%A" result.Provider.Availability result.Provider.ProviderId result.Provider.VersionBucket result.Provider.Failure
                  runPayload
                  glyphPayload
                  sprintf "%.12g:%.12g:%.12g:%.12g:%s" result.Metrics.Advance result.Metrics.Width result.Metrics.Height result.Metrics.Baseline boundsPayload
                  String.concat "|" result.Diagnostics
                  sprintf "%A" result.FallbackMode ]

        SHA256.HashData(Encoding.UTF8.GetBytes payload)
        |> Convert.ToHexString
        |> fun value -> value.ToLowerInvariant()

    // Feature 188 (US2): the single glyph-record conversion shared by `buildGlyphRun` and
    // `buildFallbackShapedText` (was duplicated verbatim at both sites) — `GlyphRunGlyph -> ShapedGlyph`,
    // field-for-field identical, so the collapse is byte-neutral.
    let private shapedGlyphOfGlyphRun (g: GlyphRunGlyph) : ShapedGlyph =
        { GlyphId = g.GlyphId
          SourceCluster = g.Cluster
          SourceText = g.SourceText
          ResolvedFace = g.ResolvedFace
          Advance = g.Advance
          Offset = g.Offset
          Position = g.Position
          Missing = g.Missing }

    let buildGlyphRun (text: string) (font: FontSpec) : GlyphRunData =
        let metrics = measureTextHeuristic text font
        let perGlyph =
            if String.IsNullOrEmpty text then
                0.0
            else
                metrics.Width / float text.Length

        let mutable x = 0.0

        let glyphs =
            text
            |> Seq.mapi (fun index ch ->
                let current = x
                x <- x + perGlyph

                { GlyphId = int ch
                  SourceText = string ch
                  Advance = perGlyph
                  Offset = { X = 0.0; Y = 0.0 }
                  Cluster = index
                  Position = { X = current; Y = 0.0 }
                  ResolvedFace = font.Family
                  Missing = false })
            |> Seq.toList

        let glyphMetrics =
            { Advance = metrics.Width
              Height = metrics.Height
              Baseline = metrics.Baseline }

        let diagnostics =
            text
            |> Seq.choose (fun ch ->
                if Char.IsSurrogate ch then
                    Some(sprintf "glyph-run-proof: unsupported surrogate code unit U+%04X deferred to full shaping" (int ch))
                else
                    None)
            |> Seq.toList

        let shapedGlyphs =
            glyphs
            |> List.map shapedGlyphOfGlyphRun

        let run =
            { TextRange = (0, text.Length)
              SourceText = text
              ResolvedFont = font.Family
              Direction = directionOf text
              Script = scriptOf text
              FallbackDecision = PureFallback
              Glyphs = shapedGlyphs
              Advance = metrics.Width
              Diagnostics = diagnostics }

        { Text = text
          Font = font
          Provider = pureFallbackProvider
          Runs = [ run ]
          Glyphs = glyphs
          Metrics = glyphMetrics
          Fingerprint = glyphRunFingerprintOf pureFallbackProvider [ run ] PureFallbackMode text font glyphs glyphMetrics diagnostics
          FallbackMode = PureFallbackMode
          FallbackDiagnostics = diagnostics }

    let buildFallbackShapedText (text: string) (font: FontSpec) : ShapedTextResult =
        let data = buildGlyphRun text font

        let glyphs =
            data.Glyphs
            |> List.map shapedGlyphOfGlyphRun

        let metrics =
            { Advance = data.Metrics.Advance
              Width = data.Metrics.Advance
              Height = data.Metrics.Height
              Baseline = data.Metrics.Baseline
              Bounds =
                Some
                    { X = 0.0
                      Y = -data.Metrics.Baseline
                      Width = data.Metrics.Advance
                      Height = data.Metrics.Height } }

        let result =
            { Text = text
              Font = font
              Provider = pureFallbackProvider
              Runs = data.Runs
              Glyphs = glyphs
              Metrics = metrics
              Diagnostics = data.FallbackDiagnostics
              Fingerprint = ""
              FallbackMode = PureFallbackMode }

        { result with Fingerprint = shapedTextFingerprintOf result }

    let shapedTextFingerprint result = shapedTextFingerprintOf result

    let measureShapedText (result: ShapedTextResult) : TextMetrics =
        { Width = result.Metrics.Width
          Height = result.Metrics.Height
          Baseline = result.Metrics.Baseline }

    let glyphRunDataFromShapedText (result: ShapedTextResult) : GlyphRunData =
        let glyphs =
            result.Glyphs
            |> List.map (fun g ->
                ({ GlyphId = g.GlyphId
                   SourceText = g.SourceText
                   Advance = g.Advance
                   Offset = g.Offset
                   Cluster = g.SourceCluster
                   Position = g.Position
                   ResolvedFace = g.ResolvedFace
                   Missing = g.Missing }
                 : GlyphRunGlyph))

        let metrics =
            { Advance = result.Metrics.Advance
              Height = result.Metrics.Height
              Baseline = result.Metrics.Baseline }

        let diagnostics =
            result.Diagnostics

        let data =
            { Text = result.Text
              Font = result.Font
              Provider = result.Provider
              Runs = result.Runs
              Glyphs = glyphs
              Metrics = metrics
              Fingerprint = ""
              FallbackMode = result.FallbackMode
              FallbackDiagnostics = diagnostics }

        { data with Fingerprint = glyphRunFingerprintOf data.Provider data.Runs data.FallbackMode data.Text data.Font data.Glyphs data.Metrics data.FallbackDiagnostics }

    let glyphRunFingerprint (data: GlyphRunData) =
        glyphRunFingerprintOf data.Provider data.Runs data.FallbackMode data.Text data.Font data.Glyphs data.Metrics data.FallbackDiagnostics

    let measureGlyphRun (data: GlyphRunData) =
        { Width = data.Metrics.Advance
          Height = data.Metrics.Height
          Baseline = data.Metrics.Baseline }

    let measureText (text: string) (font: FontSpec) =
        measureTextHeuristic text font

    // Feature 136 (R2/FR-002): the real-metrics measurer seam. `measureText` above stays pure; the
    // rendering edge (`SkiaViewer.Fonts`) installs a measurer here that returns the bundled-font
    // renderer's true advances so the advance used to SIZE a text box equals the advance used to DRAW
    // it. Process-wide, disclosed interpreter-edge mutation (constitution IV). `None` (the default) ⇒
    // `measureTextResolved` is byte-identical to the pure `measureText` path. Feature 188 (US2): this
    // module is the single owner of the seam (relocated out of `module Scene`).
    let mutable private realTextMeasurer: (string -> FontSpec -> TextMetrics) option = None
    let mutable private measurementVersionBucket = pureFallbackProvider.VersionBucket

    let setRealTextMeasurer (measurer: (string -> FontSpec -> TextMetrics) option) = realTextMeasurer <- measurer

    let textMeasurementVersionBucket () = measurementVersionBucket

    let setTextMeasurementVersionBucket (bucket: string) =
        measurementVersionBucket <-
            if String.IsNullOrWhiteSpace bucket then
                pureFallbackProvider.VersionBucket
            else
                bucket

    let measureTextResolved (text: string) (font: FontSpec) : TextMetrics =
        match realTextMeasurer with
        | Some m -> m text font
        | None -> measureText text font
