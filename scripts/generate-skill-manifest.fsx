// Feature 231 — (re)generate the fs-gg-ui product skill-manifest (ADR-0014 §Decision 1).
//
// Writes template/skill-manifest/skill-manifest.json: the full product-scope catalog, one
// entry per provider skill the template can emit, each carrying the SHA256 of its canonical
// SKILL.md body. Digest semantics match Fsgg.SkillMirror.sha256 (lowercase hex over the
// UTF-8 bytes of the body TEXT — i.e. hash(Encoding.UTF8.GetBytes(File.ReadAllText path)),
// so a BOM never enters the digest on either the producing or the verifying side).
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

// The full product-scope catalog: id -> (canonical SKILL.md source, materializes-when condition).
// Feature 238 (issue #71, ADR-0017): materializes-when is the VERBATIM .template.config/template.json
// sources[].condition that gates this skill's body — the single source of truth. supplied-by is
// derived from the source path (dirname + "/"). The concrete scaffold's union is the manifest,
// filtered by these conditions, ∩ the emitted `.agents/skills/` set. Feature231/238 tests re-read
// template.json and fail on any drift between these strings and the live conditions.
let catalog =
    [ "fs-gg-elmish", "template/product-skills/fs-gg-elmish/SKILL.md", "(profile == \"app\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-feedback-capture", "template/feedback/skill/SKILL.md", "(feedback == true) && lifecycle == \"spec-kit\""
      "fs-gg-keyboard-input", "template/product-skills/fs-gg-keyboard-input/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      "fs-gg-layout", "template/product-skills/fs-gg-layout/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      "fs-gg-project", "template/base/.agents/skills/fs-gg-project/SKILL.md", "(lifecycle == \"spec-kit\")"
      "fs-gg-samples", "template/fragments/samples/skill/SKILL.md", "(profile == \"sample-pack\") && lifecycle == \"spec-kit\""
      "fs-gg-scene", "template/product-skills/fs-gg-scene/SKILL.md", "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-skiaviewer", "template/product-skills/fs-gg-skiaviewer/SKILL.md", "(profile == \"app\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-styling", "template/product-skills/fs-gg-styling/SKILL.md", "(profile == \"app\" || profile == \"game\")"
      "fs-gg-symbology", "template/product-skills/fs-gg-symbology/SKILL.md", "(profile == \"app\" || profile == \"headless-scene\" || profile == \"governed\" || profile == \"sample-pack\" || profile == \"game\")"
      "fs-gg-testing", "template/product-skills/fs-gg-testing/SKILL.md", "(profile == \"governed\")"
      "fs-gg-ui-widgets", "template/product-skills/fs-gg-ui-widgets/SKILL.md", "(profile == \"app\" || profile == \"game\")" ]

/// Provider source directory (trailing slash) that holds the canonical SKILL.md — supplied-by.
let suppliedByOf (source: string) : string =
    source.Substring(0, source.LastIndexOf '/') + "/"

/// Minimal JSON string escape (conditions carry embedded double quotes around literals).
let jsonEscape (s: string) : string =
    s.Replace("\\", "\\\\").Replace("\"", "\\\"")

let sha256Text (body: string) : string =
    Encoding.UTF8.GetBytes body
    |> SHA256.HashData
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

let manifestJson =
    let entries =
        catalog
        |> List.sortBy (fun (id, _, _) -> id)
        |> List.map (fun (id, source, materializesWhen) ->
            let body = File.ReadAllText(repoPath source)
            sprintf
                "    {\n      \"id\": \"%s\",\n      \"scope\": \"product\",\n      \"sha256\": \"%s\",\n      \"resolvablePath\": \".agents/skills/%s/SKILL.md\",\n      \"materializes-when\": \"%s\",\n      \"supplied-by\": \"%s\"\n    }"
                id (sha256Text body) id (jsonEscape materializesWhen) (jsonEscape (suppliedByOf source)))
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
