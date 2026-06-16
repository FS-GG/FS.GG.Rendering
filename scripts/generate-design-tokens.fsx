// Generates the internal Ant-derived token taxonomy module from the DTCG source of truth.
//
//   Input:  src/Themes.Default/design-tokens.tokens.json  (feature 069 + 126 groups)
//   Output: src/DesignSystem/DesignTokensExt.fs            (module internal DesignTokensExt)
//
// Feature 126 (Workstream F, F1): the new seed/map/alias/component + spacing/density/type/elevation
// groups are GENERATED here so they stay in lock-step with the DTCG source (no hand-coded values).
// This script is build-time tooling run via `dotnet fsi`; it is NOT compiled into any product or test
// assembly, so the framework's no-JSON-parser-dependency rule is preserved (System.Text.Json is used
// here at script time only). The existing flat `light`/`dark` primitive blocks are NOT emitted here —
// they remain the hand-curated public `DesignTokens` module; this generator only emits the new groups.
//
// Usage:
//   dotnet fsi scripts/generate-design-tokens.fsx            # regenerate the file in place
//   dotnet fsi scripts/generate-design-tokens.fsx --check    # exit 1 if committed file != freshly generated

open System
open System.IO
open System.Text
open System.Text.Json

let scriptDir = __SOURCE_DIRECTORY__
let repoRoot = Path.GetFullPath(Path.Combine(scriptDir, ".."))
let sourcePath = Path.Combine(repoRoot, "src", "Themes.Default", "design-tokens.tokens.json")
let outputPath = Path.Combine(repoRoot, "src", "DesignSystem", "DesignTokensExt.fs")
let sourceRel = "src/Themes.Default/design-tokens.tokens.json"

// Top-level keys that are NOT part of the new taxonomy (existing public primitives + metadata).
let skipTopLevel = set [ "$description"; "light"; "dark" ]

let fail (msg: string) : 'a =
    eprintfn "generate-design-tokens: %s" msg
    exit 2

// ---- name mapping -------------------------------------------------------------------------------
let private capitalize (s: string) =
    if s.Length = 0 then s else string (Char.ToUpperInvariant s.[0]) + s.Substring 1

// A dotted DTCG leaf key -> a camelCase F# identifier: "text.default" -> "textDefault".
let private letName (key: string) =
    let parts = key.Split('.')
    parts.[0] + (parts.[1..] |> Array.map capitalize |> String.concat "")

// A group/sub-group key -> a PascalCase F# module name: "light" -> "Light", "button" -> "Button".
let private moduleName (key: string) = capitalize (letName key)

// ---- leaf value emission ------------------------------------------------------------------------
let private parseColor (hex: string) =
    let h = hex.TrimStart('#')
    let h = if h.Length = 6 then h + "ff" else h
    if h.Length <> 8 then fail (sprintf "bad color value '%s'" hex)
    let b i = Convert.ToByte(h.Substring(i, 2), 16)
    b 0, b 2, b 4, b 6

// The F# type annotation a DTCG leaf compiles to — SHARED by the `.fs` (`let`) and `.fsi` (`val`)
// walks so the paired files can never disagree on a leaf's type.
let private leafType (leaf: JsonElement) : string =
    match leaf.GetProperty("$type").GetString() with
    | "color" -> "Color"
    | "dimension" | "number" -> "float"
    | "shadow" -> "string"
    | "fontFamily" -> "string option"
    | other -> fail (sprintf "unknown $type '%s'" other)

// Emit a `let <name> : <ty> = <expr>` line for a DTCG leaf (an object carrying "$type"/"$value").
let private emitLeaf (indent: string) (name: string) (leaf: JsonElement) (sb: StringBuilder) =
    let ty = leaf.GetProperty("$type").GetString()
    let v = leaf.GetProperty("$value")
    let line (tyAnn: string) (expr: string) =
        sb.AppendLine(sprintf "%slet %s : %s = %s" indent name tyAnn expr) |> ignore
    // Emit a float literal that always carries a decimal point, so an integral token (e.g. 14)
    // is `14.0` (a valid `float`) rather than `14` (an `int` that fails the `: float` annotation).
    let fmtFloat (d: float) =
        let s = d.ToString("R", Globalization.CultureInfo.InvariantCulture)
        if s.IndexOfAny [| '.'; 'e'; 'E' |] >= 0 then s else s + ".0"
    match ty with
    | "color" ->
        let r, g, b, a = parseColor (v.GetString())
        line "Color" (sprintf "Colors.rgba %duy %duy %duy %duy" r g b a)
    | "dimension" | "number" ->
        line "float" (fmtFloat (v.GetDouble()))
    | "shadow" ->
        line "string" (sprintf "\"%s\"" (v.GetString()))
    | "fontFamily" ->
        let expr = if v.ValueKind = JsonValueKind.Null then "None" else sprintf "Some \"%s\"" (v.GetString())
        line "string option" expr
    | other -> fail (sprintf "unknown $type '%s' for token '%s'" other name)

let private isLeaf (e: JsonElement) =
    e.ValueKind = JsonValueKind.Object
    && (let mutable p = Unchecked.defaultof<JsonElement> in e.TryGetProperty("$value", &p))

// Recursively emit a node: a leaf becomes a `let`, an object-of-nodes becomes a nested `module`.
let rec private emitNode (indent: string) (key: string) (node: JsonElement) (sb: StringBuilder) =
    if isLeaf node then
        emitLeaf indent (letName key) node sb
    elif node.ValueKind = JsonValueKind.Object then
        sb.AppendLine(sprintf "%smodule %s =" indent (moduleName key)) |> ignore
        let mutable any = false
        for child in node.EnumerateObject() do
            any <- true
            emitNode (indent + "    ") child.Name child.Value sb
        if not any then sb.AppendLine(sprintf "%s    ()" indent) |> ignore
        sb.AppendLine() |> ignore
    else
        fail (sprintf "token '%s' is neither a DTCG leaf nor an object" key)

// Mode-parity guard: any `light` sub-group must have a matching `dark` sub-group with identical keys.
let rec private checkModeParity (key: string) (node: JsonElement) =
    if node.ValueKind = JsonValueKind.Object && not (isLeaf node) then
        let mutable light = Unchecked.defaultof<JsonElement>
        let mutable dark = Unchecked.defaultof<JsonElement>
        if node.TryGetProperty("light", &light) || node.TryGetProperty("dark", &dark) then
            if not (node.TryGetProperty("light", &light) && node.TryGetProperty("dark", &dark)) then
                fail (sprintf "group '%s' has only one of light/dark (mode parity)" key)
            let keysOf (e: JsonElement) = e.EnumerateObject() |> Seq.map (fun p -> p.Name) |> Set.ofSeq
            if keysOf light <> keysOf dark then
                fail (sprintf "group '%s' light/dark key sets differ (mode parity)" key)
        for child in node.EnumerateObject() do
            checkModeParity child.Name child.Value

// ---- .fsi signature emission (F5/130) -----------------------------------------------------------
// A module key -> a one-line `///` doc describing the layer. Module-granularity docs (FR-010): each
// nested layer/sub-module is documented; per-leaf doc comments are NOT emitted. Keyed by the lowercased
// DTCG group key with a generic fallback so a future group is still documented (never bare).
let private moduleDocText (key: string) : string =
    match key.ToLowerInvariant() with
    | "seed" -> "Seed layer: the primitive brand/semantic colors and base scalar units the map layer derives from."
    | "map" -> "Map layer: semantic color roles per light/dark mode, derived from the seed."
    | "alias" -> "Alias layer: intent-named semantic aliases (text/surface/border/feedback) per light/dark mode."
    | "component" -> "Component layer: per-component color tokens (Button, Input, Table, Tabs, Menu, …)."
    | "space" -> "Spacing scale (xs…xl), in layout units."
    | "density" -> "Density multipliers (comfortable/middle/compact)."
    | "type" -> "Type scale: font-size/line-height per typographic role."
    | "elevation" -> "Elevation shadow tokens (none/low/medium/high)."
    | "light" -> "Light-mode values."
    | "dark" -> "Dark-mode values."
    | _ -> sprintf "The %s token sub-group." (moduleName key)

// Recursively emit the SIGNATURE for a node: a leaf becomes `val name : ty`; an object-of-nodes becomes a
// doc-commented nested `module`. Mirrors emitNode's structure so the `.fsi` stays byte-locked to the `.fs`.
let rec private emitSig (indent: string) (key: string) (node: JsonElement) (sb: StringBuilder) =
    if isLeaf node then
        sb.AppendLine(sprintf "%sval %s : %s" indent (letName key) (leafType node)) |> ignore
    elif node.ValueKind = JsonValueKind.Object then
        sb.AppendLine(sprintf "%s/// %s" indent (moduleDocText key)) |> ignore
        sb.AppendLine(sprintf "%smodule %s =" indent (moduleName key)) |> ignore
        for child in node.EnumerateObject() do
            emitSig (indent + "    ") child.Name child.Value sb
        sb.AppendLine() |> ignore
    else
        fail (sprintf "token '%s' is neither a DTCG leaf nor an object" key)

// ---- generate -----------------------------------------------------------------------------------
let generate () =
    let json = File.ReadAllText sourcePath
    use doc = JsonDocument.Parse json
    let root = doc.RootElement
    let sb = StringBuilder()
    sb.AppendLine(sprintf "// GENERATED — do not edit. Source: %s" sourceRel) |> ignore
    sb.AppendLine("// Regenerate via: dotnet fsi scripts/generate-design-tokens.fsx") |> ignore
    sb.AppendLine("//") |> ignore
    sb.AppendLine("// Feature 126 (Workstream F, F1): the Ant-derived token taxonomy (seed/map/alias/component +") |> ignore
    sb.AppendLine("// spacing/density/type/elevation). INTERNAL and additive — no .fsi, no public-surface delta;") |> ignore
    sb.AppendLine("// nothing reads these yet (the F4 resolver / D2 themes consume them later).") |> ignore
    sb.AppendLine("namespace FS.GG.UI.DesignSystem") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("open FS.GG.UI.Scene") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("module DesignTokensExt =") |> ignore
    sb.AppendLine() |> ignore
    let mutable emitted = false
    for prop in root.EnumerateObject() do
        if not (skipTopLevel.Contains prop.Name) then
            checkModeParity prop.Name prop.Value
            emitNode "    " prop.Name prop.Value sb
            emitted <- true
    if not emitted then fail "no taxonomy groups found in source (expected seed/map/alias/…)"
    // Normalize to LF and a single trailing newline for stable, OS-independent output.
    sb.ToString().Replace("\r\n", "\n").TrimEnd('\n') + "\n"

// Emit the paired .fsi (F5/130): the curated PUBLIC signature, generated in lock-step with the .fs.
let generateFsi () =
    let json = File.ReadAllText sourcePath
    use doc = JsonDocument.Parse json
    let root = doc.RootElement
    let sb = StringBuilder()
    sb.AppendLine(sprintf "// GENERATED — do not edit. Source: %s" sourceRel) |> ignore
    sb.AppendLine("// Regenerate via: dotnet fsi scripts/generate-design-tokens.fsx") |> ignore
    sb.AppendLine("//") |> ignore
    sb.AppendLine("// Feature 130 (Workstream F, F5): the PUBLIC signature for the Ant-derived token taxonomy —") |> ignore
    sb.AppendLine("// the deliberate, baseline-gated promotion F1 deferred. Generated in lock-step with the paired") |> ignore
    sb.AppendLine("// DesignTokensExt.fs; currency is enforced by this generator's --check over BOTH files.") |> ignore
    sb.AppendLine("namespace FS.GG.UI.DesignSystem") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("open FS.GG.UI.Scene") |> ignore
    sb.AppendLine() |> ignore
    sb.AppendLine("/// The Ant-derived design-token taxonomy: seed -> map -> alias -> component layers plus spacing,") |> ignore
    sb.AppendLine("/// density, type scale, and elevation. Generated from the DTCG source; values are byte-identical") |> ignore
    sb.AppendLine("/// to the flat primitives.") |> ignore
    sb.AppendLine("module DesignTokensExt =") |> ignore
    sb.AppendLine() |> ignore
    let mutable emitted = false
    for prop in root.EnumerateObject() do
        if not (skipTopLevel.Contains prop.Name) then
            emitSig "    " prop.Name prop.Value sb
            emitted <- true
    if not emitted then fail "no taxonomy groups found in source (expected seed/map/alias/…)"
    // Normalize to LF and a single trailing newline for stable, OS-independent output.
    sb.ToString().Replace("\r\n", "\n").TrimEnd('\n') + "\n"

let generated = generate ()
let generatedFsi = generateFsi ()

let fsiOutputPath = Path.Combine(repoRoot, "src", "DesignSystem", "DesignTokensExt.fsi")

let rel (p: string) = Path.GetRelativePath(repoRoot, p)
let currentOf (path: string) =
    if File.Exists path then File.ReadAllText(path).Replace("\r\n", "\n") else ""

let check = Array.contains "--check" (Environment.GetCommandLineArgs())
if check then
    // Drift mode covers BOTH committed files: the generated .fs AND the paired .fsi (R3, INV-2).
    let drift =
        [ outputPath, generated; fsiOutputPath, generatedFsi ]
        |> List.filter (fun (path, gen) -> currentOf path <> gen)
    if List.isEmpty drift then
        printfn "design-tokens: up to date (DesignTokensExt.fs + DesignTokensExt.fsi)"
        exit 0
    else
        for (path, _) in drift do
            eprintfn "design-tokens: DRIFT — %s is stale; run: dotnet fsi scripts/generate-design-tokens.fsx" (rel path)
        exit 1
else
    File.WriteAllText(outputPath, generated)
    printfn "wrote %s" (rel outputPath)
    File.WriteAllText(fsiOutputPath, generatedFsi)
    printfn "wrote %s" (rel fsiOutputPath)
