#load "../../scripts/SkillStagingManifestCapture.fsx"

open System
open System.IO
open System.Runtime.InteropServices
open System.Text
open SkillStagingPolicy
open SkillStagingManifestCapture
open SkillStagingCapturedPlan

let body = Encoding.UTF8.GetBytes "body\n"
let manifest =
    $"{{\"schemaVersion\":2,\"skills\":[{{\"id\":\"audio\",\"scope\":\"product\",\"supplied-by\":\"skill/\",\"sha256\":\"{canonicalDigest body}\",\"files\":[{{\"path\":\"SKILL.md\",\"sha256\":\"{canonicalDigest body}\"}}]}}]}}"
    |> Encoding.UTF8.GetBytes
let foreign = Array.append manifest [| byte ' ' |]
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
    let scratch = Path.Combine(Path.GetTempPath(), "fsc08-manifest-" + Guid.NewGuid().ToString("N"))
    let root = Path.Combine(scratch, "repo")
    let manifestDirectory = Path.Combine(root, "template/skill-manifest")
    let source = Path.Combine(root, "skill")
    Directory.CreateDirectory(manifestDirectory) |> ignore
    Directory.CreateDirectory(source) |> ignore
    File.WriteAllBytes(Path.Combine(source, "SKILL.md"), body)
    let path = Path.Combine(manifestDirectory, "skill-manifest.json")
    File.WriteAllBytes(path, manifest)
    let foreignDirectory = Path.Combine(scratch, "foreign")
    Directory.CreateDirectory(foreignDirectory) |> ignore
    let foreignPath = Path.Combine(foreignDirectory, "skill-manifest.json")
    File.WriteAllBytes(foreignPath, foreign)
    try action root manifestDirectory path foreignDirectory foreignPath
    finally Directory.Delete(scratch, true)

let planBytes (result: Result<StagePlan, string>) =
    match result with
    | Ok plan -> plan.ManifestBytes
    | Error issue -> failwithf "pinned plan refused: %s" issue
let capturedBytes (result: Result<byte[], string>) =
    match result with
    | Ok raw -> raw
    | Error issue -> failwithf "pinned manifest capture refused: %s" issue

[<DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)>]
extern int mkfifo(string path, uint32 mode)

fixture (fun root _ _ _ _ ->
    let captured =
        match captureManifest root with
        | Ok raw -> raw
        | Error issue -> failwithf "ordinary manifest capture refused: %s" issue
    check "ordinary pinned manifest bytes" manifest captured
    check "ordinary pinned plan owns manifest bytes" manifest (preparePinned root |> planBytes)
    let plan =
        match preparePinned root with
        | Ok value -> value
        | Error issue -> failwithf "ordinary pinned plan refused: %s" issue
    check "ordinary pinned plan has one product" 1 plan.ProductCount
    check "fresh pinned manifest and source match" (Ok ()) (verifyPinnedCurrent plan root))

// The old path-check/read shape can follow a link introduced between calls.
fixture (fun _ _ path _ foreignPath ->
    let probe = FileInfo path
    if not (isNull probe.LinkTarget) || probe.Attributes.HasFlag FileAttributes.ReparsePoint then
        failwith "ordinary manifest was already a link"
    File.Move(path, path + ".held")
    File.CreateSymbolicLink(path, foreignPath) |> ignore
    check "path probe/read swap observes foreign manifest" foreign (File.ReadAllBytes path))

fixture (fun root _ path _ foreignPath ->
    let beforeOpen relative =
        if relative = "template/skill-manifest/skill-manifest.json" then
            File.Move(path, path + ".held")
            File.CreateSymbolicLink(path, foreignPath) |> ignore
    refuses "manifest link introduced before open refuses" "manifest-symlink"
        (preparePinnedWithHooks beforeOpen ignore ignore root))

fixture (fun root _ path _ foreignPath ->
    let mutable swapped = false
    let afterOpen relative =
        if relative = "template/skill-manifest/skill-manifest.json" then
            File.Move(path, path + ".held")
            File.CreateSymbolicLink(path, foreignPath) |> ignore
            swapped <- true
    check "held manifest fd resists path swap" manifest
        (captureManifestWithHooks ignore afterOpen ignore root |> capturedBytes)
    check "held manifest hook fired" true swapped)

fixture (fun root manifestDirectory _ foreignDirectory _ ->
    let mutable swapped = false
    let afterOpen relative =
        if relative = "template/skill-manifest" then
            Directory.Move(manifestDirectory, manifestDirectory + ".held")
            Directory.CreateSymbolicLink(manifestDirectory, foreignDirectory) |> ignore
            swapped <- true
    check "held manifest parent resists directory swap" manifest
        (captureManifestWithHooks ignore afterOpen ignore root |> capturedBytes)
    check "held manifest parent hook fired" true swapped)

fixture (fun root _ path _ _ ->
    let mutable changed = false
    let afterFirstChunk () =
        File.WriteAllBytes(path, foreign)
        changed <- true
    refuses "in-place manifest mutation during held read refuses" "manifest-unstable"
        (preparePinnedWithHooks ignore ignore afterFirstChunk root)
    check "manifest mutation hook fired" true changed)

fixture (fun root _ path _ _ ->
    let plan =
        match preparePinned root with
        | Ok value -> value
        | Error issue -> failwithf "ordinary pinned plan refused: %s" issue
    File.WriteAllBytes(path, foreign)
    refuses "changed manifest makes pinned plan stale" "stale-plan"
        (verifyPinnedCurrent plan root))

fixture (fun root _ path _ foreignPath ->
    File.Delete path
    File.CreateSymbolicLink(path, foreignPath) |> ignore
    refuses "manifest symlink refuses" "manifest-symlink" (preparePinned root))

fixture (fun root manifestDirectory _ foreignDirectory _ ->
    Directory.Move(manifestDirectory, manifestDirectory + ".held")
    Directory.CreateSymbolicLink(manifestDirectory, foreignDirectory) |> ignore
    refuses "manifest directory symlink refuses" "manifest-symlink" (preparePinned root))

fixture (fun root _ path _ _ ->
    File.Delete path
    if mkfifo(path, 0o600u) <> 0 then failwith "mkfifo fixture failed"
    refuses "manifest FIFO refuses without blocking" "manifest-nonregular" (preparePinned root))

let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
let realPlan =
    match preparePinned repositoryRoot with
    | Ok value -> value
    | Error issue -> failwithf "real pinned manifest plan refused: %s" issue
check "real pinned manifest plans all 20 products" 20 realPlan.ProductCount
check "real pinned manifest and source still match" (Ok ())
    (verifyPinnedCurrent realPlan repositoryRoot)

printfn "pinned skill manifest capture: %d/%d controls passed" checks checks
