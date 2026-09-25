#load "../../scripts/SkillStagingPhysical.fsx"

open System
open System.IO
open System.Text
open System.Text.Json
open SkillStagingPolicy
open SkillStagingPhysical

let body = Encoding.UTF8.GetBytes "body\n"
let sidecar = Encoding.UTF8.GetBytes "note\n"
let declared =
    [ { Path = "SKILL.md"; Sha256 = canonicalDigest body }
      { Path = "notes/detail.md"; Sha256 = canonicalDigest sidecar } ]
let mutable checks = 0
let expect label wanted actual =
    checks <- checks + 1
    if wanted <> actual then failwithf "%s: expected %A; got %A" label wanted actual

let fixture action =
    let scratch = Path.Combine(Path.GetTempPath(), "fsc08-physical-" + Guid.NewGuid().ToString("N"))
    let repository = Path.Combine(scratch, "repo")
    let source = Path.Combine(repository, "skill")
    let outside = Path.Combine(scratch, "outside")
    Directory.CreateDirectory(Path.Combine(source, "notes")) |> ignore
    Directory.CreateDirectory(outside) |> ignore
    File.WriteAllBytes(Path.Combine(source, "SKILL.md"), body)
    File.WriteAllBytes(Path.Combine(source, "notes/detail.md"), sidecar)
    File.WriteAllBytes(Path.Combine(outside, "SKILL.md"), body)
    File.WriteAllBytes(Path.Combine(outside, "detail.md"), sidecar)
    try action repository source outside
    finally Directory.Delete(scratch, true)

let inspect repository suppliedBy =
    inspectSnapshot repository suppliedBy (canonicalDigest body) declared

fixture (fun repository _ _ ->
    expect "ordinary closed tree" (Ok [ "SKILL.md"; "notes/detail.md" ])
        (inspect repository "skill/"))

fixture (fun repository _ outside ->
    Directory.CreateSymbolicLink(Path.Combine(repository, "link"), outside) |> ignore
    match sourceDirectory repository "link/" with
    | Error reason -> failwithf "red-before lexical seam unexpectedly refused link: %s" reason
    | Ok _ -> ()
    expect "pure seam accepts bytes for linked supplied-by"
        (Ok [ "SKILL.md"; "notes/detail.md" ])
        (validateSnapshot repository "link/" (canonicalDigest body) declared
            [ { Path = "SKILL.md"; Bytes = body }; { Path = "notes/detail.md"; Bytes = sidecar } ])
    expect "outside-pointing supplied-by link" (Error "source-symlink")
        (inspect repository "link/"))

fixture (fun repository _ _ ->
    let linkedRoot = repository + "-link"
    Directory.CreateSymbolicLink(linkedRoot, repository) |> ignore
    expect "linked repository root" (Error "source-symlink")
        (inspect linkedRoot "skill/"))

fixture (fun repository source outside ->
    Directory.Delete(Path.Combine(source, "notes"), true)
    Directory.CreateSymbolicLink(Path.Combine(source, "notes"), outside) |> ignore
    expect "nested directory link" (Error "source-symlink") (inspect repository "skill/"))

fixture (fun repository source outside ->
    File.Delete(Path.Combine(source, "SKILL.md"))
    File.CreateSymbolicLink(Path.Combine(source, "SKILL.md"), Path.Combine(outside, "SKILL.md")) |> ignore
    expect "body file link" (Error "source-symlink") (inspect repository "skill/"))

fixture (fun repository source _ ->
    File.Delete(Path.Combine(source, "notes/detail.md"))
    File.CreateSymbolicLink(Path.Combine(source, "notes/detail.md"), Path.Combine(source, "SKILL.md")) |> ignore
    expect "in-tree sidecar link" (Error "source-symlink") (inspect repository "skill/"))

fixture (fun repository source _ ->
    File.CreateSymbolicLink(Path.Combine(source, "notes/dangling.md"), "missing.md") |> ignore
    expect "dangling unlisted link" (Error "source-symlink") (inspect repository "skill/"))

fixture (fun repository source _ ->
    File.WriteAllText(Path.Combine(source, "notes/extra.md"), "extra")
    expect "regular extra file closes set" (Error "file-set-mismatch") (inspect repository "skill/"))

fixture (fun repository source _ ->
    File.WriteAllText(Path.Combine(source, "notes/detail.md"), "changed")
    expect "changed source bytes" (Error "source-digest-mismatch") (inspect repository "skill/"))

// Read the checked-in product sources through the same physical adapter.
let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "template/skill-manifest/skill-manifest.json")))
let productRows =
    manifest.RootElement.GetProperty("skills").EnumerateArray()
    |> Seq.filter (fun row -> row.GetProperty("scope").GetString() = "product")
    |> Seq.toList
if productRows.IsEmpty then failwith "committed product catalog is empty"
for row in productRows do
    let files =
        row.GetProperty("files").EnumerateArray()
        |> Seq.map (fun file ->
            { Path = file.GetProperty("path").GetString()
              Sha256 = file.GetProperty("sha256").GetString() })
        |> Seq.toList
    let skillId = row.GetProperty("id").GetString()
    match inspectSnapshot repositoryRoot (row.GetProperty("supplied-by").GetString())
        (row.GetProperty("sha256").GetString()) files with
    | Ok _ -> ()
    | Error reason -> failwithf "%s: physical observation refused: %s" skillId reason
manifest.Dispose()

printfn "skill staging physical observation: %d controls and %d product rows passed" checks productRows.Length
