#load "../../scripts/SkillStagingOutputMode.fsx"

open System
open System.Diagnostics
open System.IO
open System.Text
open System.Text.Json
open SkillStagingPolicy
open SkillStagingCapturedPlan
open SkillStagingOutputMode

let body = Encoding.UTF8.GetBytes "body\n"
let manifest =
    $"{{\"schemaVersion\":2,\"skills\":[{{\"id\":\"audio\",\"scope\":\"product\",\"supplied-by\":\"skill/\",\"sha256\":\"{canonicalDigest body}\",\"files\":[{{\"path\":\"SKILL.md\",\"sha256\":\"{canonicalDigest body}\"}}]}}]}}"
    |> Encoding.UTF8.GetBytes
let repositoryRoot = Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "../.."))
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
    let root = Path.Combine(Path.GetTempPath(), "fsc08-output-mode-" + Guid.NewGuid().ToString("N"))
    let source = Path.Combine(root, "skill")
    let docs = Path.Combine(source, "docs")
    let empty = Path.Combine(docs, "empty")
    Directory.CreateDirectory(empty) |> ignore
    let skill = Path.Combine(source, "SKILL.md")
    File.WriteAllBytes(skill, body)
    File.SetUnixFileMode(source, enum<UnixFileMode> 0o750)
    File.SetUnixFileMode(docs, enum<UnixFileMode> 0o710)
    File.SetUnixFileMode(empty, enum<UnixFileMode> 0o700)
    File.SetUnixFileMode(skill, enum<UnixFileMode> 0o640)
    try action root source
    finally Directory.Delete(root, true)

let pythonReference source umask =
    let info = ProcessStartInfo("python3")
    info.ArgumentList.Add(Path.Combine(repositoryRoot, "tests/skill-staging-physical/OutputModeReference.py"))
    info.ArgumentList.Add source
    info.ArgumentList.Add "audio"
    info.ArgumentList.Add(string umask)
    info.RedirectStandardOutput <- true
    info.RedirectStandardError <- true
    use childProcess = new Process()
    childProcess.StartInfo <- info
    childProcess.Start() |> ignore
    let output = childProcess.StandardOutput.ReadToEnd()
    let errors = childProcess.StandardError.ReadToEnd()
    childProcess.WaitForExit()
    if childProcess.ExitCode <> 0 then failwithf "Python source reference failed: %s" errors
    use document = JsonDocument.Parse output
    document.RootElement.EnumerateArray()
    |> Seq.map (fun entry ->
        { Path = entry.GetProperty("path").GetString()
          Kind = if entry.GetProperty("kind").GetString() = "file" then File else Directory
          Mode = entry.GetProperty("mode").GetUInt32() })
    |> Seq.toList

let changed path transform rows =
    rows |> List.map (fun entry -> if entry.Path = path then transform entry else entry)

fixture (fun root source ->
    let plan =
        match prepareCurrent root manifest with
        | Ok value -> value
        | Error issue -> failwithf "valid source refused: %s" issue
    let contract =
        match expected plan 0o022u with
        | Ok value -> value
        | Error issue -> failwithf "valid umask refused: %A" issue
    let independentlyAuthored =
        [ { Path = "."; Kind = Directory; Mode = 0o755u }
          { Path = "skill-manifest.json"; Kind = File; Mode = 0o644u }
          { Path = "skills"; Kind = Directory; Mode = 0o755u }
          { Path = "skills/audio"; Kind = Directory; Mode = 0o750u }
          { Path = "skills/audio/SKILL.md"; Kind = File; Mode = 0o640u }
          { Path = "skills/audio/docs"; Kind = Directory; Mode = 0o710u }
          { Path = "skills/audio/docs/empty"; Kind = Directory; Mode = 0o700u } ]
    check "independently authored fresh-output roster and modes" independentlyAuthored contract.Entries
    check "Python source-only reference agrees" (pythonReference source 0o022u) contract.Entries
    check "valid proposed output mode facts" (Ok ()) (verify contract independentlyAuthored)
    refusal "wrong manifest mode refuses" (WrongMode "skill-manifest.json")
        (verify contract (changed "skill-manifest.json" (fun row -> { row with Mode = 0o600u }) independentlyAuthored))
    refusal "wrong output root mode refuses" (WrongMode ".")
        (verify contract (changed "." (fun row -> { row with Mode = 0o700u }) independentlyAuthored))
    refusal "wrong skills container mode refuses" (WrongMode "skills")
        (verify contract (changed "skills" (fun row -> { row with Mode = 0o700u }) independentlyAuthored))
    refusal "wrong copied source file mode refuses" (WrongMode "skills/audio/SKILL.md")
        (verify contract (changed "skills/audio/SKILL.md" (fun row -> { row with Mode = 0o600u }) independentlyAuthored))
    refusal "wrong copied empty directory mode refuses" (WrongMode "skills/audio/docs/empty")
        (verify contract (changed "skills/audio/docs/empty" (fun row -> { row with Mode = 0o755u }) independentlyAuthored))
    refusal "wrong manifest kind refuses" (WrongKind "skill-manifest.json")
        (verify contract (changed "skill-manifest.json" (fun row -> { row with Kind = Directory }) independentlyAuthored))
    refusal "missing path refuses" (MissingPath "skills/audio/docs/empty")
        (verify contract (independentlyAuthored |> List.filter (fun row -> row.Path <> "skills/audio/docs/empty")))
    refusal "extra path refuses" (UnexpectedPath "extra.txt")
        (verify contract ({ Path = "extra.txt"; Kind = File; Mode = 0o644u } :: independentlyAuthored))
    refusal "duplicate path refuses" (DuplicatePath "skill-manifest.json")
        (verify contract (independentlyAuthored.[1] :: independentlyAuthored))
    refusal "invalid umask refuses" (InvalidUmask 0o1000u)
        (expected plan 0o1000u |> Result.map (fun _ -> ()))
    let restricted =
        match expected plan 0o077u with
        | Ok value -> value
        | Error issue -> failwithf "restricted umask refused: %A" issue
    check "restricted umask manifest mode" 0o600u
        (restricted.Entries |> List.find (fun row -> row.Path = "skill-manifest.json") |> _.Mode)
    check "restricted umask receiver root mode" 0o700u
        (restricted.Entries |> List.find (fun row -> row.Path = ".") |> _.Mode)
    check "restricted umask keeps copied source mode" 0o640u
        (restricted.Entries |> List.find (fun row -> row.Path = "skills/audio/SKILL.md") |> _.Mode)
    check "Python restricted-umask reference agrees" (pythonReference source 0o077u) restricted.Entries)

let realManifest = File.ReadAllBytes(Path.Combine(repositoryRoot, "template/skill-manifest/skill-manifest.json"))
let realPlan =
    match prepareCurrent repositoryRoot realManifest with
    | Ok value -> value
    | Error issue -> failwithf "real catalog refused: %s" issue
let realContract =
    match expected realPlan 0o022u with
    | Ok value -> value
    | Error issue -> failwithf "real catalog mode plan refused: %A" issue
check "real catalog output mode facts cover all planned paths"
    (3 + realPlan.Files.Length + realPlan.Directories.Length) realContract.Entries.Length

printfn "skill output mode policy: %d/%d controls passed" checks checks
