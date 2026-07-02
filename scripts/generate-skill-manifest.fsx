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

// The full product-scope catalog: id -> canonical SKILL.md source (data-model.md, Feature 231).
// Profile/feedback conditionality lives in .template.config/template.json; the concrete
// scaffold's union is the manifest ∩ the emitted `.agents/skills/` set.
let catalog =
    [ "fs-gg-elmish", "template/product-skills/fs-gg-elmish/SKILL.md"
      "fs-gg-feedback-capture", "template/feedback/skill/SKILL.md"
      "fs-gg-keyboard-input", "template/product-skills/fs-gg-keyboard-input/SKILL.md"
      "fs-gg-layout", "template/product-skills/fs-gg-layout/SKILL.md"
      "fs-gg-project", "template/base/.agents/skills/fs-gg-project/SKILL.md"
      "fs-gg-samples", "template/fragments/samples/skill/SKILL.md"
      "fs-gg-scene", "template/product-skills/fs-gg-scene/SKILL.md"
      "fs-gg-skiaviewer", "template/product-skills/fs-gg-skiaviewer/SKILL.md"
      "fs-gg-styling", "template/product-skills/fs-gg-styling/SKILL.md"
      "fs-gg-symbology", "template/product-skills/fs-gg-symbology/SKILL.md"
      "fs-gg-testing", "template/product-skills/fs-gg-testing/SKILL.md"
      "fs-gg-ui-widgets", "template/product-skills/fs-gg-ui-widgets/SKILL.md" ]

let sha256Text (body: string) : string =
    Encoding.UTF8.GetBytes body
    |> SHA256.HashData
    |> Array.map (fun b -> b.ToString "x2")
    |> String.concat ""

let manifestJson =
    let entries =
        catalog
        |> List.sortBy fst
        |> List.map (fun (id, source) ->
            let body = File.ReadAllText(repoPath source)
            sprintf
                "    {\n      \"id\": \"%s\",\n      \"scope\": \"product\",\n      \"sha256\": \"%s\",\n      \"resolvablePath\": \".agents/skills/%s/SKILL.md\"\n    }"
                id (sha256Text body) id)
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
