#load "../../scripts/SkillStagingCapturedPlan.fsx"

open System
open System.IO
open System.Text
open SkillStagingPolicy
open SkillStagingCapturedPlan

let body = Encoding.UTF8.GetBytes "body\n"
let manifest =
    $"{{\"schemaVersion\":2,\"skills\":[{{\"id\":\"audio\",\"scope\":\"product\",\"supplied-by\":\"skill/\",\"sha256\":\"{canonicalDigest body}\",\"files\":[{{\"path\":\"SKILL.md\",\"sha256\":\"{canonicalDigest body}\"}}]}}]}}"
    |> Encoding.UTF8.GetBytes
let mutable checks = 0
let check label expected actual =
    if expected <> actual then failwithf "%s: expected %A; got %A" label expected actual
    checks <- checks + 1
    printfn "ok %s" label
let refusal label expected result =
    match result with
    | Error actual -> check label expected actual
    | Ok _ -> failwithf "%s: unexpectedly accepted" label

let fixture action =
    let root = Path.Combine(Path.GetTempPath(), "fsc08-dir-mode-" + Guid.NewGuid().ToString("N"))
    let source = Path.Combine(root, "skill")
    Directory.CreateDirectory(source) |> ignore
    File.WriteAllBytes(Path.Combine(source, "SKILL.md"), body)
    try action root source
    finally Directory.Delete(root, true)

let prepare root =
    match prepareCurrent root manifest with
    | Ok value -> value
    | Error issue -> failwithf "ordinary plan refused: %s" issue
let directoryCandidates (plan: StagePlan) =
    plan.Directories |> List.map (fun directory ->
        { Destination = directory.Destination; SourceMode = directory.SourceMode }: DirectoryCandidate)

fixture (fun root source ->
    let plan = prepare root
    Directory.CreateDirectory(Path.Combine(source, "empty")) |> ignore
    refusal "added empty directory changes source plan" "source-changed"
        (verifyCurrent plan root manifest))

fixture (fun root source ->
    let empty = Path.Combine(source, "empty")
    let nested = Path.Combine(empty, "nested")
    Directory.CreateDirectory(nested) |> ignore
    File.SetUnixFileMode(source, enum<UnixFileMode> 0o750)
    File.SetUnixFileMode(empty, enum<UnixFileMode> 0o700)
    File.SetUnixFileMode(nested, enum<UnixFileMode> 0o755)
    let plan = prepare root
    check "independently authored empty directory roster and modes"
        [ "skills/audio", 0o750u
          "skills/audio/empty", 0o700u
          "skills/audio/empty/nested", 0o755u ]
        (plan.Directories |> List.map (fun directory -> directory.Destination, directory.SourceMode))
    check "empty directories leave one captured file" 1 plan.Files.Length
    check "valid directory projection" (Ok ())
        (verifyDirectoryProjection plan (directoryCandidates plan))
    let candidates = directoryCandidates plan
    let rootCandidate = candidates.Head
    refusal "wrong root directory mode refuses" "copy-directory-mode-mismatch"
        (verifyDirectoryProjection plan
            ({ rootCandidate with SourceMode = 0o755u } :: candidates.Tail))
    refusal "wrong directory destination refuses" "copy-directory-mismatch"
        (verifyDirectoryProjection plan
            ({ rootCandidate with Destination = "skills/other" } :: candidates.Tail))
    refusal "missing empty directory refuses" "copy-directory-mismatch"
        (verifyDirectoryProjection plan (candidates |> List.filter (fun item ->
            item.Destination <> "skills/audio/empty/nested")))
    refusal "extra directory refuses" "copy-directory-mismatch"
        (verifyDirectoryProjection plan
            ({ Destination = "skills/audio/foreign"; SourceMode = 0o755u } :: candidates))
    check "fresh empty directory tree matches" (Ok ()) (verifyCurrent plan root manifest)
    File.SetUnixFileMode(empty, enum<UnixFileMode> 0o755)
    refusal "changed empty directory mode refuses fresh check" "source-changed"
        (verifyCurrent plan root manifest))

fixture (fun root source ->
    let empty = Path.Combine(source, "empty")
    Directory.CreateDirectory(empty) |> ignore
    let plan = prepare root
    Directory.Delete empty
    refusal "removed empty directory refuses fresh check" "source-changed"
        (verifyCurrent plan root manifest))

let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let realManifest = File.ReadAllBytes(Path.Combine(repositoryRoot, "template/skill-manifest/skill-manifest.json"))
let realPlan =
    match prepareCurrent repositoryRoot realManifest with
    | Ok value -> value
    | Error issue -> failwithf "real catalog refused: %s" issue
check "real catalog includes product-root directory facts" true
    (realPlan.Directories.Length >= realPlan.ProductCount)

printfn "directory mode plan: %d/%d controls passed" checks checks
