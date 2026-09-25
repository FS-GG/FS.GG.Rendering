#load "../../scripts/SkillStagingPhysical.fsx"

open System
open System.IO
open System.Runtime.InteropServices
open System.Text
open SkillStagingPolicy
open SkillStagingPhysical

let body = Encoding.UTF8.GetBytes "body\n"
let sidecar = Encoding.UTF8.GetBytes "note\n"
let foreign = Encoding.UTF8.GetBytes "evil\n"
let declared =
    [ { Path = "SKILL.md"; Sha256 = canonicalDigest body }
      { Path = "notes/detail.md"; Sha256 = canonicalDigest sidecar } ]
let expected = Ok [ "SKILL.md"; "notes/detail.md" ]
let mutable checks = 0
let check label wanted actual =
    if actual <> wanted then failwithf "%s: expected %A; got %A" label wanted actual
    checks <- checks + 1
    printfn "ok %s" label

let fixture action =
    let scratch = Path.Combine(Path.GetTempPath(), "fsc08-pinned-" + Guid.NewGuid().ToString("N"))
    let root = Path.Combine(scratch, "repo")
    let source = Path.Combine(root, "skill")
    let outside = Path.Combine(scratch, "outside")
    Directory.CreateDirectory(Path.Combine(source, "notes")) |> ignore
    Directory.CreateDirectory(outside) |> ignore
    File.WriteAllBytes(Path.Combine(source, "SKILL.md"), body)
    File.WriteAllBytes(Path.Combine(source, "notes/detail.md"), sidecar)
    File.WriteAllBytes(Path.Combine(outside, "SKILL.md"), foreign)
    File.WriteAllBytes(Path.Combine(outside, "detail.md"), foreign)
    try action root source outside
    finally Directory.Delete(scratch, true)

let inspect root = inspectSnapshot root "skill/" (canonicalDigest body) declared
let hooked beforeOpen afterOpen root =
    inspectSnapshotWithHooks beforeOpen afterOpen root "skill/" (canonicalDigest body) declared

[<DllImport("libc", EntryPoint = "mkfifo", SetLastError = true)>]
extern int mkfifo(string path, uint32 mode)

fixture (fun root _ _ ->
    check "ordinary pinned tree" expected (inspect root))

// This is the deterministic race in #1334's old path-check/read shape.
fixture (fun _ source outside ->
    let path = Path.Combine(source, "SKILL.md")
    let probe = FileInfo path
    if not (isNull probe.LinkTarget) || probe.Attributes.HasFlag FileAttributes.ReparsePoint then
        failwith "ordinary body was not a regular file at the probe"
    File.Move(path, path + ".held")
    File.CreateSymbolicLink(path, Path.Combine(outside, "SKILL.md")) |> ignore
    check "path-check/read swap observes foreign bytes" foreign (File.ReadAllBytes path))

fixture (fun root source outside ->
    let path = Path.Combine(source, "SKILL.md")
    let mutable swapped = false
    let afterOpen relative =
        if relative = "SKILL.md" then
            File.Move(path, path + ".held")
            File.CreateSymbolicLink(path, Path.Combine(outside, "SKILL.md")) |> ignore
            swapped <- true
    check "held file fd resists path swap" expected (hooked ignore afterOpen root)
    check "held file hook fired" true swapped)

fixture (fun root source outside ->
    let path = Path.Combine(source, "SKILL.md")
    let beforeOpen relative =
        if relative = "SKILL.md" then
            File.Move(path, path + ".held")
            File.CreateSymbolicLink(path, Path.Combine(outside, "SKILL.md")) |> ignore
    check "link introduced before open refuses" (Error "source-symlink")
        (hooked beforeOpen ignore root))

fixture (fun root source outside ->
    let notes = Path.Combine(source, "notes")
    let mutable swapped = false
    let afterOpen relative =
        if relative = "notes" then
            Directory.Move(notes, notes + ".held")
            Directory.CreateSymbolicLink(notes, outside) |> ignore
            swapped <- true
    check "held parent fd resists directory swap" expected (hooked ignore afterOpen root)
    check "held parent hook fired" true swapped)

fixture (fun root source _ ->
    let pipe = Path.Combine(source, "notes/pipe")
    if mkfifo(pipe, 0o600u) <> 0 then failwith "mkfifo fixture failed"
    check "FIFO refuses without blocking" (Error "source-entry-unsupported") (inspect root))

fixture (fun root source _ ->
    File.WriteAllBytes(Path.Combine(source, "skill.md"), body)
    check "case alias regular file refuses" (Error "source-path-alias") (inspect root))

fixture (fun root source _ ->
    Directory.CreateDirectory(Path.Combine(source, "NOTES")) |> ignore
    check "case alias empty directory refuses" (Error "source-path-alias") (inspect root))

printfn "skill staging pinned Linux source: %d/%d controls passed" checks checks
