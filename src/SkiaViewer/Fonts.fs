namespace FS.GG.UI.SkiaViewer

#nowarn "3261" // GetManifestResourceStream nullability — the null case is handled explicitly (fail loudly)

open System
open System.IO
open System.Collections.Generic
open System.Reflection
open SkiaSharp
open SkiaSharp.HarfBuzz
open FS.GG.UI.Scene

module Fonts =

    [<RequireQualifiedAccess>]
    type FallbackResolution =
        | Authored of family: string
        | Substituted of original: char * substitute: char * family: string
        | Tofu of original: char

    type ResolvedChar =
        { Original: char
          Rendered: char
          Font: SKFont
          Resolution: FallbackResolution }

    type FallbackReport =
        { SubstitutedCount: int
          TofuCount: int
          AffectedCodePoints: int list }

    type TextShapingProviderStatus =
        { Evidence: ShapingProviderEvidence
          Diagnostics: string list }

    let defaultSansFamily = "Noto Sans"
    let defaultMonoFamily = "Noto Sans Mono"
    let private harfbuzzProviderId = "harfbuzz-skiasharp"
    let private harfbuzzVersionBucket = $"SkiaSharp.HarfBuzz/{typeof<SKShaper>.Assembly.GetName().Version}"
    let private fallbackVersionBucket = "skia-bundled-font-fallback/v1"

    let private installedEvidence () =
        { Availability = ProviderInstalled
          ProviderId = harfbuzzProviderId
          VersionBucket = harfbuzzVersionBucket
          Failure = None }

    let private clearedEvidence () =
        { Availability = ProviderCleared
          ProviderId = harfbuzzProviderId
          VersionBucket = fallbackVersionBucket
          Failure = None }

    let private failedEvidence message =
        { Availability = ProviderFailed
          ProviderId = harfbuzzProviderId
          VersionBucket = harfbuzzVersionBucket
          Failure = Some message }

    let private providerGate = obj()
    let mutable private providerEvidence = clearedEvidence ()

    let private providerDiagnostics evidence =
        [ match evidence.Availability with
          | ProviderInstalled -> $"text-shaping-provider: installed {evidence.ProviderId} ({evidence.VersionBucket})"
          | ProviderCleared -> $"text-shaping-provider: cleared; using bundled-font fallback ({evidence.VersionBucket})"
          | ProviderUnavailable -> $"text-shaping-provider: unavailable {evidence.ProviderId} ({evidence.VersionBucket})"
          | ProviderFailed -> $"text-shaping-provider: failed {evidence.ProviderId} ({evidence.VersionBucket})"
          match evidence.Failure with
          | Some failure -> $"text-shaping-provider-failure: {failure}"
          | None -> () ]

    let shapingProviderStatus () : TextShapingProviderStatus =
        lock providerGate (fun () ->
            { Evidence = providerEvidence
              Diagnostics = providerDiagnostics providerEvidence })

    // Manifest-resource logical-name prefix (matches the explicit <LogicalName> in SkiaViewer.fsproj).
    let private resourcePrefix = "FS.GG.UI.SkiaViewer.Fonts."

    // (canonical family, isBold, resource leaf) for every bundled face. Bold rows present only where a
    // real bold face is bundled; a bold request for a family without one falls to its regular face.
    let private faceAssets: (string * bool * string) list =
        [ "Noto Sans", false, "NotoSans-Regular.ttf"
          "Noto Sans", true, "NotoSans-Bold.ttf"
          "Noto Sans Mono", false, "NotoSansMono-Regular.ttf"
          "Inter", false, "Inter-Regular.ttf"
          "Inter", true, "Inter-Bold.ttf"
          "JetBrains Mono", false, "JetBrainsMono-Regular.ttf"
          "DejaVu Sans", false, "DejaVuSans.ttf"
          "DejaVu Sans", true, "DejaVuSans-Bold.ttf"
          "DejaVu Sans Mono", false, "DejaVuSansMono.ttf" ]

    // Fixed fallback orders (data-model §2): a glyph missing in one family is sought in the next
    // before any substitute/tofu. The requested family is tried first, then the rest of its chain.
    let private sansChain = [ "Noto Sans"; "Inter"; "DejaVu Sans" ]
    let private monoChain = [ "Noto Sans Mono"; "JetBrains Mono"; "DejaVu Sans Mono" ]
    let private monoFamilies = set [ "Noto Sans Mono"; "JetBrains Mono"; "DejaVu Sans Mono" ]

    let private gate = obj ()
    let private typefaceCache = Dictionary<struct (string * bool), SKTypeface>()
    let private fontCache = Dictionary<struct (string * bool * float), SKFont>()

    let private loadTypeface (family: string) (bold: bool) : SKTypeface =
        // Prefer the exact (family, bold) asset; if bold was requested but only a regular face is
        // bundled for the family, fall back to that regular face.
        let leaf =
            faceAssets
            |> List.tryPick (fun (f, b, leaf) -> if f = family && b = bold then Some leaf else None)
            |> Option.orElseWith (fun () ->
                faceAssets |> List.tryPick (fun (f, b, leaf) -> if f = family && not b then Some leaf else None))

        match leaf with
        | None -> failwithf "Fonts: no bundled face for family '%s'" family
        | Some leaf ->
            let asm = Assembly.GetExecutingAssembly()
            let resName = resourcePrefix + leaf
            use stream = asm.GetManifestResourceStream(resName)

            if isNull stream then
                failwithf
                    "Fonts: missing embedded font asset '%s' (available: %s)"
                    resName
                    (String.Join(", ", asm.GetManifestResourceNames()))

            // Read fully into managed bytes → SKData so the typeface owns a persistent copy
            // (the manifest stream is disposed by `use`).
            use ms = new MemoryStream()
            stream.CopyTo ms
            use data = SKData.CreateCopy(ms.ToArray())
            let tf = SKTypeface.FromData(data)

            if isNull tf then
                failwithf "Fonts: SKTypeface.FromData returned null for bundled asset '%s'" resName

            tf

    let private cachedTypeface (family: string) (bold: bool) : SKTypeface =
        lock gate (fun () ->
            let key = struct (family, bold)

            match typefaceCache.TryGetValue key with
            | true, tf -> tf
            | _ ->
                let tf = loadTypeface family bold
                typefaceCache.[key] <- tf
                tf)

    let private cachedFont (family: string) (bold: bool) (size: float) : SKFont =
        lock gate (fun () ->
            let size = max 1.0 size
            let key = struct (family, bold, size)

            match fontCache.TryGetValue key with
            | true, f -> f
            | _ ->
                let f = new SKFont(cachedTypeface family bold, float32 size)
                fontCache.[key] <- f
                f)

    // Map a logical family-name request (possibly a CSS-ish list like "Inter, sans-serif") to one of
    // the bundled canonical families, longest specific names first so "Noto Sans Mono" wins over
    // "Noto Sans".
    let private canonicalFamily (name: string) : string option =
        let n = name.Trim().ToLowerInvariant()

        [ "noto sans mono", "Noto Sans Mono"
          "dejavu sans mono", "DejaVu Sans Mono"
          "jetbrains mono", "JetBrains Mono"
          "noto sans", "Noto Sans"
          "dejavu sans", "DejaVu Sans"
          "inter", "Inter" ]
        |> List.tryPick (fun (k, v) -> if n.Contains k then Some v else None)

    let private looksMonospace (name: string) =
        let n = name.ToLowerInvariant()
        [ "mono"; "code"; "consol"; "menlo"; "courier" ] |> List.exists n.Contains

    // (bold, primary family, ordered fallback chain, size) for a FontSpec.
    let private setup (font: FontSpec) =
        let bold =
            match font.Weight with
            | Some w -> w >= 600
            | None -> false

        let size = max 1.0 font.Size

        let primary, mono =
            match font.Family with
            | Some name ->
                match canonicalFamily name with
                | Some fam -> fam, monoFamilies.Contains fam
                | None -> if looksMonospace name then defaultMonoFamily, true else defaultSansFamily, false
            | None -> defaultSansFamily, false

        let chain =
            (primary :: (if mono then monoChain else sansChain)) |> List.distinct

        bold, primary, chain, size

    // Deliberate, legible ASCII/Unicode substitutes for decoratives that may lack coverage (R3). Only
    // applied when no bundled face in the chain covers the original character.
    let private substitute (c: char) : char option =
        match c with
        | '—' -> Some '–' // — em dash → – en dash
        | '▸' -> Some '>' // ▸ → >
        | '·' -> Some '•' // · → • bullet
        | _ -> None

    let private resolveChar (chain: string list) (primary: string) (bold: bool) (size: float) (c: char) : ResolvedChar =
        let covers (fam: string) (ch: char) =
            let f = cachedFont fam bold size
            if f.ContainsGlyph(int ch) then Some f else None

        match chain |> List.tryPick (fun fam -> covers fam c |> Option.map (fun f -> f, fam)) with
        | Some(f, fam) ->
            { Original = c
              Rendered = c
              Font = f
              Resolution = FallbackResolution.Authored fam }
        | None ->
            match substitute c with
            | Some sub ->
                match chain |> List.tryPick (fun fam -> covers fam sub |> Option.map (fun f -> f, fam)) with
                | Some(f, fam) ->
                    { Original = c
                      Rendered = sub
                      Font = f
                      Resolution = FallbackResolution.Substituted(c, sub, fam) }
                | None ->
                    { Original = c
                      Rendered = c
                      Font = cachedFont primary bold size
                      Resolution = FallbackResolution.Tofu c }
            | None ->
                { Original = c
                  Rendered = c
                  Font = cachedFont primary bold size
                  Resolution = FallbackResolution.Tofu c }

    let resolveFont (font: FontSpec) : SKFont =
        let bold, primary, _, size = setup font
        cachedFont primary bold size

    let resolveText (font: FontSpec) (text: string) : ResolvedChar list =
        let bold, primary, chain, size = setup font
        [ for c in text -> resolveChar chain primary bold size c ]

    /// The draw/measure advance of one resolved character: a tofu box advances by ~0.6·size; every
    /// other character advances by its drawing font's real glyph advance (so measure == draw).
    let internal charAdvance (size: float) (rc: ResolvedChar) : float =
        match rc.Resolution with
        | FallbackResolution.Tofu _ -> size * 0.6
        | _ -> float (rc.Font.MeasureText(string rc.Rendered))

    let measureWidth (font: FontSpec) (text: string) : float =
        let _, _, _, size = setup font
        resolveText font text |> List.sumBy (charAdvance size)

    let realMeasure (text: string) (font: FontSpec) : TextMetrics =
        let size = max 1.0 font.Size
        // Width from real advances; Height/Baseline kept identical to the pure heuristic so only the
        // truncation-causing width is corrected (no line-height ripple).
        { Width = measureWidth font text
          Height = size
          Baseline = size * 0.8 }

    let report (resolved: ResolvedChar list) : FallbackReport =
        let substituted =
            resolved
            |> List.filter (fun rc ->
                match rc.Resolution with
                | FallbackResolution.Substituted _ -> true
                | _ -> false)

        let tofu =
            resolved
            |> List.filter (fun rc ->
                match rc.Resolution with
                | FallbackResolution.Tofu _ -> true
                | _ -> false)

        { SubstitutedCount = List.length substituted
          TofuCount = List.length tofu
          AffectedCodePoints =
            (substituted @ tofu) |> List.map (fun rc -> int rc.Original) |> List.distinct |> List.sort }

    let diagnostics (resolved: ResolvedChar list) : string list =
        resolved
        |> List.choose (fun rc ->
            match rc.Resolution with
            | FallbackResolution.Authored _ -> None
            | FallbackResolution.Substituted(o, s, fam) ->
                Some(sprintf "text-fallback: substituted U+%04X '%c' -> '%c' (family %s)" (int o) o s fam)
            | FallbackResolution.Tofu o -> Some(sprintf "text-fallback: tofu U+%04X '%c' (no bundled coverage)" (int o) o))

    let private resolvedFamily rc =
        match rc.Resolution with
        | FallbackResolution.Authored family -> Some family
        | FallbackResolution.Substituted(_, _, family) -> Some family
        | FallbackResolution.Tofu _ -> None

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

    let private fallbackDecision (font: FontSpec) (resolved: ResolvedChar list) =
        let hasTofu =
            resolved
            |> List.exists (fun rc ->
                match rc.Resolution with
                | FallbackResolution.Tofu _ -> true
                | _ -> false)

        let substituted =
            resolved
            |> List.choose (fun rc ->
                match rc.Resolution with
                | FallbackResolution.Substituted(_, _, family) -> Some family
                | _ -> None)

        if hasTofu then
            MissingGlyphs(resolved |> List.map (fun rc -> string rc.Original) |> String.concat "")
        elif not substituted.IsEmpty then
            SubstitutedFace(font.Family |> Option.defaultValue defaultSansFamily, substituted |> List.distinct |> String.concat ",")
        else
            let family =
                resolved
                |> List.tryPick resolvedFamily
                |> Option.orElse font.Family
                |> Option.defaultValue defaultSansFamily

            AuthoredFace family

    let private unsupportedDiagnostics (text: string) =
        [ if text.Contains("\n") || text.Contains("\r") then
              "text-shaping: newline control handled as deterministic single-line control character; paragraph layout is out of scope."
          for ch in text do
              let code = int ch
              if (code >= 0x202A && code <= 0x202E) || (code >= 0x2066 && code <= 0x2069) then
                  $"text-shaping: unsupported bidi control U+{code:X4} disclosed; paragraph bidi layout is out of scope." ]

    let private fallbackResultWith evidence mode extraDiagnostics text font =
        let fallback = Scene.buildFallbackShapedText text font
        let result =
            { fallback with
                Provider = evidence
                Diagnostics = fallback.Diagnostics @ extraDiagnostics @ providerDiagnostics evidence
                FallbackMode = mode }

        { result with Fingerprint = Scene.shapedTextFingerprint result }

    let shapeText (text: string) (font: FontSpec) : ShapedTextResult =
        let evidence = (shapingProviderStatus ()).Evidence

        match evidence.Availability with
        | ProviderInstalled ->
            try
                let skFont = resolveFont font
                use shaper = new SKShaper(skFont.Typeface)
                let shaped = shaper.Shape(text, skFont)
                let resolved = resolveText font text
                let fallbackDiagnostics = diagnostics resolved
                let extraDiagnostics = unsupportedDiagnostics text
                let codepoints = shaped.Codepoints |> Array.toList
                let clusters = shaped.Clusters |> Array.toList
                let points = shaped.Points |> Array.toList
                let width = float shaped.Width
                let size = max 1.0 font.Size
                let pointAt index =
                    points
                    |> List.tryItem index
                    |> Option.defaultValue (SKPoint(0.0f, 0.0f))

                let sourceAt cluster =
                    if String.IsNullOrEmpty text then
                        ""
                    else
                        let index = max 0 (min (text.Length - 1) cluster)
                        text.Substring(index, 1)

                let isMissing cluster glyphId =
                    glyphId = 0
                    || (resolved
                        |> List.tryItem cluster
                        |> Option.exists (fun rc ->
                            match rc.Resolution with
                            | FallbackResolution.Tofu _ -> true
                            | _ -> false))

                let glyphs =
                    codepoints
                    |> List.mapi (fun index glyphId ->
                        let cluster =
                            clusters
                            |> List.tryItem index
                            |> Option.map int
                            |> Option.defaultValue index

                        let point = pointAt index
                        let nextX =
                            if index + 1 < points.Length then
                                float (pointAt (index + 1)).X
                            else
                                width

                        let x = float point.X
                        let resolvedFace =
                            resolved
                            |> List.tryItem (max 0 (min (max 0 (resolved.Length - 1)) cluster))
                            |> Option.bind resolvedFamily
                            |> Option.orElse font.Family

                        { GlyphId = int glyphId
                          SourceCluster = cluster
                          SourceText = sourceAt cluster
                          ResolvedFace = resolvedFace
                          Advance = max 0.0 (nextX - x)
                          Offset = { X = 0.0; Y = float point.Y }
                          Position = { X = x; Y = 0.0 }
                          Missing = isMissing cluster (int glyphId) })

                let metrics =
                    { Advance = width
                      Width = width
                      Height = size
                      Baseline = size * 0.8
                      Bounds =
                        Some
                            { X = 0.0
                              Y = -(size * 0.8)
                              Width = width
                              Height = size } }

                let run =
                    { TextRange = (0, text.Length)
                      SourceText = text
                      ResolvedFont = resolved |> List.tryPick resolvedFamily |> Option.orElse font.Family
                      Direction = directionOf text
                      Script = scriptOf text
                      FallbackDecision = fallbackDecision font resolved
                      Glyphs = glyphs
                      Advance = width
                      Diagnostics = fallbackDiagnostics @ extraDiagnostics }

                let result =
                    { Text = text
                      Font = font
                      Provider = evidence
                      Runs = [ run ]
                      Glyphs = glyphs
                      Metrics = metrics
                      Diagnostics = fallbackDiagnostics @ extraDiagnostics @ providerDiagnostics evidence
                      Fingerprint = ""
                      FallbackMode = Shaped }

                { result with Fingerprint = Scene.shapedTextFingerprint result }
            with ex ->
                let failed = failedEvidence ex.Message
                lock providerGate (fun () -> providerEvidence <- failed)
                Scene.setTextMeasurementVersionBucket failed.VersionBucket
                fallbackResultWith failed ShapingFailedFallback [ $"text-shaping: HarfBuzz shaping failed: {ex.Message}" ] text font
        | ProviderCleared -> fallbackResultWith evidence ProviderUnavailableFallback [] text font
        | ProviderUnavailable -> fallbackResultWith evidence ProviderUnavailableFallback [] text font
        | ProviderFailed -> fallbackResultWith evidence ShapingFailedFallback [] text font

    let private shapedMeasure text font =
        let shaped = shapeText text font
        Scene.measureShapedText shaped

    let installShapingProvider () : TextShapingProviderStatus =
        let evidence =
            try
                let font = resolveFont { Family = None; Size = 16.0; Weight = None }
                use _shaper = new SKShaper(font.Typeface)
                installedEvidence ()
            with ex ->
                failedEvidence ex.Message

        lock providerGate (fun () -> providerEvidence <- evidence)

        match evidence.Availability with
        | ProviderInstalled ->
            Scene.setRealTextMeasurer (Some shapedMeasure)
            Scene.setTextMeasurementVersionBucket evidence.VersionBucket
        | _ ->
            Scene.setRealTextMeasurer (Some realMeasure)
            Scene.setTextMeasurementVersionBucket evidence.VersionBucket

        { Evidence = evidence
          Diagnostics = providerDiagnostics evidence }

    let clearShapingProvider () : TextShapingProviderStatus =
        let evidence = clearedEvidence ()
        lock providerGate (fun () -> providerEvidence <- evidence)
        Scene.setRealTextMeasurer (Some realMeasure)
        Scene.setTextMeasurementVersionBucket evidence.VersionBucket

        { Evidence = evidence
          Diagnostics = providerDiagnostics evidence }

    let buildShapedGlyphRunData text font =
        shapeText text font |> Scene.glyphRunDataFromShapedText

    let buildGlyphRunData (text: string) (font: FontSpec) : GlyphRunData =
        let size = max 1.0 font.Size
        let resolved = resolveText font text
        let mutable x = 0.0

        let glyphs =
            resolved
            |> List.mapi (fun index rc ->
                let advance = charAdvance size rc
                let current = x
                x <- x + advance

                { GlyphId = int rc.Rendered
                  SourceText = string rc.Original
                  Advance = advance
                  Offset = { X = 0.0; Y = 0.0 }
                  Cluster = index
                  Position = { X = current; Y = 0.0 }
                  ResolvedFace = resolvedFamily rc |> Option.orElse font.Family
                  Missing =
                    match rc.Resolution with
                    | FallbackResolution.Tofu _ -> true
                    | _ -> false })

        let measured = realMeasure text font
        let glyphMetrics =
            { Advance = measured.Width
              Height = measured.Height
              Baseline = measured.Baseline }

        let shapedGlyphs =
            glyphs
            |> List.map (fun g ->
                ({ GlyphId = g.GlyphId
                   SourceCluster = g.Cluster
                   SourceText = g.SourceText
                   ResolvedFace = g.ResolvedFace
                   Advance = g.Advance
                   Offset = g.Offset
                   Position = g.Position
                   Missing = g.Missing }
                 : ShapedGlyph))

        let runDiagnostics = diagnostics resolved

        let run =
            { TextRange = (0, text.Length)
              SourceText = text
              ResolvedFont = resolved |> List.tryPick resolvedFamily |> Option.orElse font.Family
              Direction = directionOf text
              Script = scriptOf text
              FallbackDecision = fallbackDecision font resolved
              Glyphs = shapedGlyphs
              Advance = measured.Width
              Diagnostics = runDiagnostics }

        let evidence = clearedEvidence ()

        let data =
            { Text = text
              Font = font
              Provider = evidence
              Runs = [ run ]
              Glyphs = glyphs
              Metrics = glyphMetrics
              Fingerprint = ""
              FallbackMode = ProviderUnavailableFallback
              FallbackDiagnostics = runDiagnostics }

        { data with Fingerprint = Scene.glyphRunFingerprint data }

    let installMeasurementSeam () =
        Scene.setRealTextMeasurer (Some realMeasure)
        Scene.setTextMeasurementVersionBucket fallbackVersionBucket
