#load "../../scripts/skill-staging-policy.fsx"

open System
open System.IO
open System.Text
open System.Text.Json
open SkillStagingPolicy

let bytes (value: string) = Encoding.UTF8.GetBytes value
let body = bytes "body\n"
let sidecar = bytes "note\n"
let hash = canonicalDigest

let declared = [ { Path = "SKILL.md"; Sha256 = hash body }; { Path = "notes/detail.md"; Sha256 = hash sidecar } ]
let actual = [ { Path = "SKILL.md"; Bytes = body }; { Path = "notes/detail.md"; Bytes = sidecar } ]
let validate source rows files = validateSnapshot "/checkout/rendering" source (hash body) rows files

let expect expected observed =
    if expected <> observed then failwithf "expected %A; got %A" expected observed

// Independent byte controls: a BOM is removed only at the front, and CRLF is
// folded for hashing without rewriting the bytes a later stager would copy.
let mixed = Array.append [| 0xefuy; 0xbbuy; 0xbfuy |] (bytes "one\r\ntwo\n")
let normalized = bytes "one\ntwo\n"
expect (hash normalized) (hash mixed)
if hash (bytes "x\ry") = hash (bytes "x\ny") then failwith "bare CR was folded"

expect (Ok [ "SKILL.md"; "notes/detail.md" ])
    (validate "template/product-skills/example/" declared actual)
expect (Error "supplied-by-unsafe") (validate "../outside/" declared actual)
expect (Error "supplied-by-unsafe") (validate "/outside/" declared actual)
expect (Error "duplicate-declared-file")
    (validate "template/product-skills/example/" (declared @ [ { Path = "notes\\detail.md"; Sha256 = hash sidecar } ]) actual)
expect (Error "duplicate-source-file")
    (validate "template/product-skills/example/" declared (actual @ [ { Path = "notes\\detail.md"; Bytes = sidecar } ]))
expect (Error "file-set-mismatch")
    (validate "template/product-skills/example/" declared (actual @ [ { Path = "extra.md"; Bytes = sidecar } ]))
expect (Error "source-digest-mismatch")
    (validate "template/product-skills/example/" declared [ { Path = "SKILL.md"; Bytes = bytes "changed" }; actual.[1] ])
expect (Error "body-digest-binding")
    (validate "template/product-skills/example/" [ { declared.[0] with Sha256 = hash sidecar }; declared.[1] ] actual)

// Read-only catalog check: all current product rows and all their sidecars
// independently enter the same closed-set reducer. It does not stage bytes.
let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let manifest = JsonDocument.Parse(File.ReadAllBytes(Path.Combine(repositoryRoot, "template/skill-manifest/skill-manifest.json")))

let productRows =
    manifest.RootElement.GetProperty("skills").EnumerateArray()
    |> Seq.filter (fun row -> row.GetProperty("scope").GetString() = "product")
    |> Seq.toList

if productRows.Length = 0 then failwith "committed product catalog is empty"

for row in productRows do
    let source = row.GetProperty("supplied-by").GetString()
    let directory =
        match sourceDirectory repositoryRoot source with
        | Ok value -> value
        | Error reason -> failwithf "%s: %s" source reason

    let declarations =
        row.GetProperty("files").EnumerateArray()
        |> Seq.map (fun file ->
            { Path = file.GetProperty("path").GetString()
              Sha256 = file.GetProperty("sha256").GetString() })
        |> Seq.toList

    let files =
        Directory.GetFiles(directory, "*", SearchOption.AllDirectories)
        |> Array.map (fun path ->
            { Path = Path.GetRelativePath(directory, path).Replace('\\', '/')
              Bytes = File.ReadAllBytes path })
        |> Array.toList

    match validateSnapshot repositoryRoot source (row.GetProperty("sha256").GetString()) declarations files with
    | Ok _ -> ()
    | Error reason -> failwithf "%s: %s" (row.GetProperty("id").GetString()) reason

printfn "skill staging pure policy: 10 controls and %d committed product rows passed" productRows.Length
