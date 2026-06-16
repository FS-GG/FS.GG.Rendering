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
    sb.AppendLine("module internal DesignTokensExt =") |> ignore
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

let generated = generate ()

let check = Array.contains "--check" (Environment.GetCommandLineArgs())
if check then
    let current = if File.Exists outputPath then File.ReadAllText(outputPath).Replace("\r\n", "\n") else ""
    if current = generated then
        printfn "design-tokens: up to date"
        exit 0
    else
        eprintfn "design-tokens: DRIFT — %s is stale; run: dotnet fsi scripts/generate-design-tokens.fsx" (Path.GetRelativePath(repoRoot, outputPath))
        exit 1
else
    File.WriteAllText(outputPath, generated)
    printfn "wrote %s" (Path.GetRelativePath(repoRoot, outputPath))
