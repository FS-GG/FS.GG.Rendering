#load "../../scripts/SkillStagingCapturedPlan.fsx"

open System
open System.IO
open System.Text
open SkillStagingPolicy
open SkillStagingCapturedPlan

let body = Encoding.UTF8.GetBytes "body\n"
let detail = Encoding.UTF8.GetBytes "note\n"
let manifest =
    $"{{\"schemaVersion\":2,\"skills\":[{{\"id\":\"audio\",\"scope\":\"product\",\"supplied-by\":\"skill/\",\"sha256\":\"{canonicalDigest body}\",\"files\":[{{\"path\":\"SKILL.md\",\"sha256\":\"{canonicalDigest body}\"}},{{\"path\":\"notes/detail.md\",\"sha256\":\"{canonicalDigest detail}\"}}]}}]}}"
    |> Encoding.UTF8.GetBytes
let mutable checks = 0
let check label expected actual =
    if actual <> expected then failwithf "%s: expected %A; got %A" label expected actual
    checks <- checks + 1
    printfn "ok %s" label
let refuses label reason result =
    match result with
    | Error issue -> check label reason issue
    | Ok _ -> failwithf "%s: unexpectedly accepted" label

let fixture action =
    let root = Path.Combine(Path.GetTempPath(), "fsc08-copy-plan-" + Guid.NewGuid().ToString("N"))
    let source = Path.Combine(root, "skill")
    Directory.CreateDirectory(Path.Combine(source, "notes")) |> ignore
    let skill = Path.Combine(source, "SKILL.md")
    let sidecar = Path.Combine(source, "notes/detail.md")
    File.WriteAllBytes(skill, body)
    File.WriteAllBytes(sidecar, detail)
    File.SetUnixFileMode(skill, UnixFileMode.UserRead ||| UnixFileMode.UserWrite |||
        UnixFileMode.GroupRead ||| UnixFileMode.OtherRead)
    File.SetUnixFileMode(sidecar, UnixFileMode.UserRead ||| UnixFileMode.UserWrite |||
        UnixFileMode.GroupRead)
    try action root skill sidecar
    finally Directory.Delete(root, true)

let prepare root =
    match prepareCurrent root manifest with
    | Ok plan -> plan
    | Error issue -> failwithf "valid fixture refused: %s" issue
let candidates (plan: StagePlan) =
    plan.Files |> List.map (fun file ->
        { Destination = file.Destination; SourceMode = file.SourceMode; Bytes = file.Bytes })

fixture (fun root _ _ ->
    let manifestInput = Array.copy manifest
    let plan =
        match prepareCurrent root manifestInput with
        | Ok value -> value
        | Error issue -> failwithf "valid fixture refused: %s" issue
    manifestInput.[0] <- byte 'X'
    check "caller manifest mutation cannot alter plan" manifest plan.ManifestBytes
    check "one product and two files" (1, 2) (plan.ProductCount, plan.Files.Length)
    check "captured exact destinations"
        [ "skills/audio/SKILL.md"; "skills/audio/notes/detail.md" ]
        (plan.Files |> List.map _.Destination)
    check "captured source file modes" [ 0o644u; 0o640u ]
        (plan.Files |> List.map _.SourceMode)
    check "captured raw bytes" [ body; detail ] (plan.Files |> List.map _.Bytes)
    check "valid source-only copy projection" (Ok ()) (verifyProjection plan (candidates plan))
    check "fresh source matches plan" (Ok ()) (verifyCurrent plan root manifest)
    let projection = candidates plan
    let head = projection.Head
    refuses "wrong destination refuses" "copy-path-mismatch"
        (verifyProjection plan ({ head with Destination = "skills/other/SKILL.md" } :: projection.Tail))
    refuses "wrong copy mode refuses" "copy-mode-mismatch"
        (verifyProjection plan ({ head with SourceMode = 0o600u } :: projection.Tail))
    refuses "changed copy bytes refuse" "copy-bytes-mismatch"
        (verifyProjection plan ({ head with Bytes = Encoding.UTF8.GetBytes "evil\n" } :: projection.Tail))
    let exposed = plan.Files.Head.Bytes
    exposed.[0] <- byte 'X'
    check "plan getter protects captured bytes" body plan.Files.Head.Bytes
    let manifestGetter = plan.ManifestBytes
    manifestGetter.[0] <- byte 'X'
    check "plan getter protects manifest bytes" manifest plan.ManifestBytes)

fixture (fun root skill _ ->
    let plan = prepare root
    File.WriteAllBytes(skill, Encoding.UTF8.GetBytes "evil\n")
    refuses "changed source refuses fresh check" "source-digest-mismatch"
        (verifyCurrent plan root manifest)
    check "changed source cannot alter planned bytes" body plan.Files.Head.Bytes)

fixture (fun root skill _ ->
    let plan = prepare root
    File.Move(skill, skill + ".moved")
    refuses "changed source path refuses fresh check" "file-set-mismatch"
        (verifyCurrent plan root manifest))

fixture (fun root skill _ ->
    let plan = prepare root
    File.SetUnixFileMode(skill, UnixFileMode.UserRead ||| UnixFileMode.UserWrite)
    refuses "changed source mode refuses fresh check" "source-changed"
        (verifyCurrent plan root manifest))

fixture (fun root _ _ ->
    let plan = prepare root
    let newManifest = Array.append manifest [| byte ' ' |]
    refuses "stale manifest bytes refuse" "stale-plan"
        (verifyCurrent plan root newManifest))

fixture (fun root _ _ ->
    let duplicate =
        (Encoding.UTF8.GetString manifest).Replace("\"schemaVersion\":2,", "\"schemaVersion\":2,\"schemaVersion\":2,")
        |> Encoding.UTF8.GetBytes
    refuses "duplicate JSON property refuses" "manifest-invalid" (prepareCurrent root duplicate))

fixture (fun root _ _ ->
    let noncanonical =
        (Encoding.UTF8.GetString manifest).Replace("notes/detail.md", @"notes\\detail.md")
        |> Encoding.UTF8.GetBytes
    refuses "declared backslash path refuses before copy plan" "declared-path-noncanonical"
        (prepareCurrent root noncanonical))

let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let realManifest = File.ReadAllBytes(Path.Combine(repositoryRoot, "template/skill-manifest/skill-manifest.json"))
let realPlan =
    match prepareCurrent repositoryRoot realManifest with
    | Ok plan -> plan
    | Error issue -> failwithf "real catalog plan refused: %s" issue
check "real catalog plans all 20 products" 20 realPlan.ProductCount
check "real catalog has at least one file per product" true (realPlan.Files.Length >= 20)
check "real catalog source still matches plan" (Ok ()) (verifyCurrent realPlan repositoryRoot realManifest)

printfn "captured skill copy plan: %d/%d controls passed" checks checks
