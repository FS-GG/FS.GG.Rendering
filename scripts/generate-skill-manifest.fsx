// Feature 231 — (re)generate the fs-gg-ui product skill-manifest (ADR-0014 §Decision 1).
//
// Writes template/skill-manifest/skill-manifest.json: the full product-scope catalog, one
// entry per provider skill the template can emit, each carrying the SHA256 of its canonical
// SKILL.md body. Digest semantics match Fsgg.SkillMirror.sha256 (lowercase hex over the
// UTF-8 bytes of the body TEXT, CRLF normalized to LF first — i.e.
// hash(Encoding.UTF8.GetBytes(File.ReadAllText(path).Replace("\r\n","\n"))), so neither a BOM
// nor a checkout's line endings enter the digest on the producing or the verifying side).
//
// The manifest is the contract the standalone materialize step and the release gates read;
// Feature231SkillManifestTests recomputes these digests independently and fails on drift.
//
// Usage:
//   dotnet fsi scripts/generate-skill-manifest.fsx            # regenerate
//   dotnet fsi scripts/generate-skill-manifest.fsx --check    # exit 1 if on-disk manifest differs

open System
open System.IO
open System.Security.Cryptography
open System.Text

let repoRoot =
    let rec find dir =
        if File.Exists(Path.Combine(dir, "FS.GG.Rendering.slnx")) then dir
        else
            match Directory.GetParent dir |> Option.ofObj with
            | Some p -> find p.FullName
            | None -> failwith "Could not locate repository root (FS.GG.Rendering.slnx)."
    find __SOURCE_DIRECTORY__

let repoPath (rel: string) =
    Path.Combine(repoRoot, rel.Replace('/', Path.DirectorySeparatorChar))

// The full product-scope catalog: id -> (canonical SKILL.md source, template.json body-source condition).
// Feature 238 (issue #71, ADR-0017): the third element mirrors the VERBATIM .template.config/template.json
// sources[].condition that gates this skill's body — the single source of truth. supplied-by is
// derived from the source path (dirname + "/"). The concrete scaffold's union is the manifest,
// filtered by these conditions, ∩ the emitted `.agents/skills/` set. Feature231/238 tests re-read
// template.json and fail on any drift between these strings and the live conditions.
//
// NOTE (issue #77): template.json conditions use the C-style grammar the `dotnet new` engine requires
// (`==`/`&&`/`||`/parens/quoted literals). The published manifest instead carries materializes-when in
// the ADR-0017 CANONICAL grammar the skill-union gate (.github scripts/skill-union-assert.sh) + the
// typed Fsgg.Registry validator evaluate: bare tokens, `in [..]` for same-param sets, `and`/`or`, no
// parens/quotes. normalizeCondition (below) is the deterministic bridge; Feature238 proves manifest ≡
// template.json semantically so the two grammars never drift.
let catalog =
    // #436: audio reaches every profile that opens a viewer window, not the simulation profiles only.
    // Must stay identical to the fs-gg-audio row in .template.config/template.json and to the audio
    // package gate in template/base/{Directory.Packages.props,src/Product/Product.fsproj} — R-REACH
    // (SkillPackageReachTests) fails if the skill materializes where its four packages do not.
    [ "fs-gg-audio", "template/product-skills/fs-gg-audio/SKILL.md", "(profile == \"app\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-collision", "template/product-skills/fs-gg-collision/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      "fs-gg-elmish", "template/product-skills/fs-gg-elmish/SKILL.md", "(profile == \"app\" || profile == \"sample-pack\" || profile == \"game\")"
      // #434: the report is UNCONDITIONAL — its template.json row carries no `condition` at all, which
      // is the engine's native "always". The empty string mirrors that absent condition verbatim, and
      // normalizeCondition maps it to the canonical `always`. Kept identical to the row by G-EQUIV.
      "fs-gg-feedback-report", "template/feedback-report/skill/SKILL.md", ""
      "fs-gg-game-core", "template/product-skills/fs-gg-game-core/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      "fs-gg-grids", "template/product-skills/fs-gg-grids/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      "fs-gg-keyboard-input", "template/product-skills/fs-gg-keyboard-input/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      "fs-gg-layout", "template/product-skills/fs-gg-layout/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      "fs-gg-line-drawing", "template/product-skills/fs-gg-line-drawing/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      "fs-gg-model-swap", "template/product-skills/fs-gg-model-swap/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      "fs-gg-persistence", "template/product-skills/fs-gg-persistence/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      "fs-gg-project", "template/base/.agents/skills/fs-gg-project/SKILL.md", "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-samples", "template/fragments/samples/skill/SKILL.md", "(profile == \"sample-pack\") && lifecycle == \"spec-kit\""
      "fs-gg-scene", "template/product-skills/fs-gg-scene/SKILL.md", "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-skiaviewer", "template/product-skills/fs-gg-skiaviewer/SKILL.md", "(profile == \"app\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-styling", "template/product-skills/fs-gg-styling/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      // The divergent, whole-frame symbol-DESIGN loop on top of fs-gg-symbology. game/sample-pack only:
      // it is meaningless without a captured gamestate frame. Kept to pure Scene + the two Symbology
      // packages (no render adapter), so its reach is a SUBSET of fs-gg-symbology's — R-REACH holds.
      "fs-gg-symbol-design", "template/product-skills/fs-gg-symbol-design/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")"
      // #430: the three SkiaViewer profiles, not all five — Render.toPng reaches SkiaViewer, which
      // `headless-scene`/`governed` do not pin. Kept identical to the template.json row by G-EQUIV.
      "fs-gg-symbology", "template/product-skills/fs-gg-symbology/SKILL.md", "(profile == \"app\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-testing", "template/product-skills/fs-gg-testing/SKILL.md", "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-ui-widgets", "template/product-skills/fs-gg-ui-widgets/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      "fs-gg-visibility", "template/product-skills/fs-gg-visibility/SKILL.md", "(profile == \"game\" || profile == \"sample-pack\")" ]

/// Provider source directory (trailing slash) that holds the canonical SKILL.md — supplied-by.
let suppliedByOf (source: string) : string =
    source.Substring(0, source.LastIndexOf '/') + "/"

// ---- C-style → ADR-0017 canonical materializes-when normalizer (issue #77) ---------------------
// template.json speaks the engine's C-style grammar; the gate + typed validator speak a deliberately
// tiny one:  always | <clause> [ (and|or) <clause> ]...  clause = <param> == <v> | <param> != <v> |
// <param> in [<v>, ...].  No parens, bare tokens, no quotes.  The real template.json conditions are
// an AND of conjuncts, each conjunct a (parenthesized) OR-chain of same-param equalities — so a
// same-param `==` OR-chain collapses to `in [..]`, `&&`→`and`, quotes are stripped. Deterministic and
// total over the shape template.json actually emits; Feature238 asserts the result is gate-evaluable
// AND semantically equal to the source condition, catching any shape this doesn't anticipate.

/// Split a C-style boolean on top-level (paren-depth-0) occurrences of `op` ("&&" / "||").
let private splitTopLevel (op: string) (s: string) : string list =
    let parts = ResizeArray<string>()
    let sb = System.Text.StringBuilder()
    let mutable depth = 0
    let mutable i = 0
    while i < s.Length do
        let c = s.[i]
        if c = '(' then depth <- depth + 1; sb.Append c |> ignore; i <- i + 1
        elif c = ')' then depth <- depth - 1; sb.Append c |> ignore; i <- i + 1
        elif depth = 0 && i + op.Length <= s.Length && s.Substring(i, op.Length) = op then
            parts.Add(sb.ToString()); sb.Clear() |> ignore; i <- i + op.Length
        else sb.Append c |> ignore; i <- i + 1
    parts.Add(sb.ToString())
    [ for p in parts -> p.Trim() ]

/// Strip one layer of parens iff they wrap the whole expression.
let private stripOuterParens (s: string) : string =
    let t = s.Trim()
    if t.StartsWith "(" && t.EndsWith ")" then
        let mutable depth = 0
        let mutable wrapsWhole = true
        for i in 0 .. t.Length - 1 do
            if t.[i] = '(' then depth <- depth + 1
            elif t.[i] = ')' then
                depth <- depth - 1
                if depth = 0 && i <> t.Length - 1 then wrapsWhole <- false
        if wrapsWhole then t.Substring(1, t.Length - 2).Trim() else t
    else t

/// Parse a single comparison `param (==|!=) value` → (param, op, value-without-quotes).
let private parseClause (c: string) : string * string * string =
    let op = if c.Contains "!=" then "!=" else "=="
    let idx = c.IndexOf op
    let param = c.Substring(0, idx).Trim()
    let value = c.Substring(idx + op.Length).Trim().Trim('"')
    param, op, value

/// One AND-conjunct: an OR-chain of same-param `==` collapses to `in [..]`; length-1 stays `p == v`.
let private normalizeConjunct (conj: string) : string =
    let disjuncts = splitTopLevel "||" (stripOuterParens conj) |> List.map parseClause
    match disjuncts with
    | [ (p, op, v) ] -> sprintf "%s %s %s" p op v
    | many ->
        let paramsUsed = many |> List.map (fun (p, _, _) -> p) |> List.distinct
        let allEq = many |> List.forall (fun (_, op, _) -> op = "==")
        match paramsUsed, allEq with
        | [ p ], true -> sprintf "%s in [%s]" p (many |> List.map (fun (_, _, v) -> v) |> String.concat ", ")
        | _ -> many |> List.map (fun (p, op, v) -> sprintf "%s %s %s" p op v) |> String.concat " or "

/// #434: a `sources` row with no `condition` fires on every scaffold — the engine's native
/// "unconditional". The canonical grammar spells that `always`; without this case the empty string
/// would reach parseClause, which indexes on a `==` that is not there.
let normalizeCondition (condition: string) : string =
    if System.String.IsNullOrWhiteSpace condition then
        "always"
    else
        splitTopLevel "&&" condition
        |> List.map normalizeConjunct
        |> String.concat " and "

/// Minimal JSON string escape (conditions carry embedded double quotes around literals).
let jsonEscape (s: string) : string =
    s.Replace("\\", "\\\\").Replace("\"", "\\\"")

// Normalize CRLF -> LF before hashing, matching Fsgg.SkillMirror.sha256 (FS.GG.Contracts
// 4.0.0), so an LF-authored body digests identically on a CRLF checkout.
let sha256Text (body: string) : string =
    (if String.IsNullOrEmpty body then "" else body.Replace("\r\n", "\n"))
    |> Encoding.UTF8.GetBytes
    |> SHA256.HashData
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

let manifestJson =
    let entries =
        catalog
        |> List.sortBy (fun (id, _, _) -> id)
        |> List.map (fun (id, source, condition) ->
            let body = File.ReadAllText(repoPath source)
            sprintf
                "    {\n      \"id\": \"%s\",\n      \"scope\": \"product\",\n      \"sha256\": \"%s\",\n      \"resolvablePath\": \".agents/skills/%s/SKILL.md\",\n      \"materializes-when\": \"%s\",\n      \"supplied-by\": \"%s\"\n    }"
                id (sha256Text body) id (jsonEscape (normalizeCondition condition)) (jsonEscape (suppliedByOf source)))
        |> String.concat ",\n"

    sprintf "{\n  \"schemaVersion\": 1,\n  \"skills\": [\n%s\n  ]\n}\n" entries

let manifestPath = repoPath "template/skill-manifest/skill-manifest.json"
let check = Environment.GetCommandLineArgs() |> Array.contains "--check"

if check then
    let current = if File.Exists manifestPath then File.ReadAllText manifestPath else ""

    if current = manifestJson then
        printfn "skill-manifest: up to date (%d skills)" catalog.Length
        exit 0
    else
        eprintfn "skill-manifest: STALE — run `dotnet fsi scripts/generate-skill-manifest.fsx`"
        exit 1
else
    Directory.CreateDirectory(Path.GetDirectoryName manifestPath) |> ignore
    File.WriteAllText(manifestPath, manifestJson)
    printfn "skill-manifest: wrote %s (%d skills)" manifestPath catalog.Length
