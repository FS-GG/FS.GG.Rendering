namespace FS.GG.UI.SkiaViewer

#nowarn "3261" // GetManifestResourceStream nullability — the null case is handled explicitly (fail loudly)

open System
open System.IO
open System.Collections.Generic
open System.Reflection
open SkiaSharp
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

    let defaultSansFamily = "Noto Sans"
    let defaultMonoFamily = "Noto Sans Mono"

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

    let installMeasurementSeam () = Scene.setRealTextMeasurer (Some realMeasure)
