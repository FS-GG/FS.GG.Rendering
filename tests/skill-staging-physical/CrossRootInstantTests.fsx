#load "../../scripts/SkillStagingManifestCapture.fsx"

open System
open System.IO
open System.Text
open SkillStagingPolicy
open SkillStagingCapturedPlan
open SkillStagingManifestCapture

let bodyA = Encoding.UTF8.GetBytes "skill A\n"
let bodyB = Encoding.UTF8.GetBytes "skill B\n"
let manifestFor body =
    $"{{\"schemaVersion\":2,\"skills\":[{{\"id\":\"audio\",\"scope\":\"product\",\"supplied-by\":\"skill/\",\"sha256\":\"{canonicalDigest body}\",\"files\":[{{\"path\":\"SKILL.md\",\"sha256\":\"{canonicalDigest body}\"}}]}}]}}"
    |> Encoding.UTF8.GetBytes
let manifestA = manifestFor bodyA
let manifestB = manifestFor bodyB

let fixture (initialBody: byte[]) action =
    let scratch = Path.Combine(Path.GetTempPath(), "fsc08-cross-root-" + Guid.NewGuid().ToString("N"))
    let root = Path.Combine(scratch, "repo")
    let manifestDirectory = Path.Combine(root, "template/skill-manifest")
    let sourceDirectory = Path.Combine(root, "skill")
    Directory.CreateDirectory(manifestDirectory) |> ignore
    Directory.CreateDirectory(sourceDirectory) |> ignore
    let manifestPath = Path.Combine(manifestDirectory, "skill-manifest.json")
    let skillPath = Path.Combine(sourceDirectory, "SKILL.md")
    File.WriteAllBytes(manifestPath, manifestA)
    File.WriteAllBytes(skillPath, initialBody)
    try action root manifestPath skillPath
    finally Directory.Delete(scratch, true)

let mutable checks = 0
let check label expected actual =
    if expected <> actual then failwithf "%s: expected %A; got %A" label expected actual
    checks <- checks + 1
    printfn "ok %s" label
let refuses label expected result =
    match result with
    | Error issue -> check label expected issue
    | Ok _ -> failwithf "%s: incoherent plan accepted" label

fixture bodyA (fun root _ _ ->
    let plan =
        match preparePinned root with
        | Ok value -> value
        | Error issue -> failwithf "stable source refused: %s" issue
    check "stable manifest is owned" manifestA plan.ManifestBytes
    check "stable product file is owned" bodyA plan.Files.Head.Bytes
    check "stable source recheck" (Ok ()) (verifyPinnedCurrent plan root))

fixture bodyB (fun root manifestPath skillPath ->
    let states = ResizeArray<byte[] * byte[]>()
    let record () = states.Add(File.ReadAllBytes manifestPath, File.ReadAllBytes skillPath)
    record ()
    let mutable changed = false
    let afterOpen relative =
        if relative = "template/skill-manifest" && not changed then
            changed <- true
            let manifestDirectory = Path.GetDirectoryName manifestPath
            Directory.Move(manifestDirectory, manifestDirectory + ".held")
            Directory.CreateDirectory(manifestDirectory) |> ignore
            File.WriteAllBytes(manifestPath, manifestB)
            record ()
            File.WriteAllBytes(skillPath, bodyA)
            record ()
    refuses "different-instant manifest and product refuse" "manifest-changed-during-capture"
        (preparePinnedWithHooks ignore afterOpen ignore root)
    check "manifest read hook fired" true changed
    check "recorded phases never paired manifest A with product A"
        [ (manifestA, bodyB); (manifestB, bodyB); (manifestB, bodyA) ]
        (states |> Seq.toList))

printfn "cross-root instant: %d/%d controls passed" checks checks
