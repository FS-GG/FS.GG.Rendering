// Feature 231 — the ONE standalone-lane skill-root materialize step (ADR-0014 §Decision 2).
//
// Fans the union under `.agents/skills/` (the provider source root) into the remaining
// declared agent-skill roots (`.claude/`, `.codex/`) as byte-identical copies, then verifies
// the three-root invariant against the shipped skill-manifest: present in each root ∧
// byte-identical across roots ∧ SKILL.md matches the manifest digest. Pure BCL — no restore,
// no network. Invoked by the `FsGgMaterializeSkillRoots` MSBuild target on build (advisory)
// and by release/composition gates with `--enforce` (non-zero exit on drift).
//
// This file ships only in the standalone spec-kit lane; under an orchestrated (sdd) scaffold
// the fsgg-sdd CLI is the mirror authority and this script is never emitted (ADR-0011 §2).
//
// Usage:
//   dotnet fsi materialize-skill-roots.fsx [--enforce] [--product-root <dir>]

#load "skill-mirror-vendored.fs"

open System
open System.IO
open System.Text.Json
open FsGg.Vendored
open FsGg.Vendored.SkillMirror

let args = Environment.GetCommandLineArgs() |> Array.toList
let enforce = args |> List.contains "--enforce"

let productRoot =
    let rec afterFlag =
        function
        | "--product-root" :: value :: _ -> Some value
        | _ :: rest -> afterFlag rest
        | [] -> None
    // The emitted script lives at <productRoot>/.specify/scripts/fs-gg/.
    match afterFlag args with
    | Some dir -> Path.GetFullPath dir
    | None -> Path.GetFullPath(Path.Combine(__SOURCE_DIRECTORY__, "..", "..", ".."))

let toNative (rel: string) = Path.Combine(productRoot, rel.Replace('/', Path.DirectorySeparatorChar))
let agentsSkillsDir = toNative (SkillMirror.providerSourceRoot + "/skills")

if not (Directory.Exists agentsSkillsDir) then
    printfn "fs-gg-skill-roots: no %s/skills directory — nothing to materialize" SkillMirror.providerSourceRoot
    exit 0

// ---- mirror: every file under .agents/skills/ -> each non-source root ----------------------

let relSourceFiles =
    Directory.EnumerateFiles(agentsSkillsDir, "*", SearchOption.AllDirectories)
    |> Seq.map (fun f ->
        let rel = Path.GetRelativePath(productRoot, f).Replace('\\', '/')
        rel)
    |> Seq.sort
    |> Seq.toList

let targetRoots = SkillMirror.mirrorTargetRoots SkillMirror.agentSkillRoots

let mutable mirroredCount = 0

for rel in relSourceFiles do
    let sourceBytes = File.ReadAllBytes(toNative rel)

    for targetRoot in targetRoots do
        let targetRel = SkillMirror.retargetSkillPath targetRoot rel
        let targetPath = toNative targetRel

        let upToDate =
            File.Exists targetPath && File.ReadAllBytes targetPath = sourceBytes

        if not upToDate then
            Directory.CreateDirectory(Path.GetDirectoryName targetPath) |> ignore
            File.WriteAllBytes(targetPath, sourceBytes)
            mirroredCount <- mirroredCount + 1

// ---- verify: manifest-driven three-root invariant -------------------------------------------

let manifestDigests =
    let manifestPath = toNative (SkillMirror.providerSourceRoot + "/skills/skill-manifest.json")

    if not (File.Exists manifestPath) then
        Map.empty
    else
        use doc = JsonDocument.Parse(File.ReadAllText manifestPath)

        doc.RootElement.GetProperty("skills").EnumerateArray()
        |> Seq.map (fun entry -> entry.GetProperty("id").GetString(), entry.GetProperty("sha256").GetString())
        |> Map.ofSeq

let presentIds =
    Directory.EnumerateDirectories agentsSkillsDir
    |> Seq.filter (fun d -> File.Exists(Path.Combine(d, "SKILL.md")))
    |> Seq.map Path.GetFileName
    |> Seq.sort
    |> Seq.toList

let expected =
    presentIds
    |> List.map (fun id ->
        match Map.tryFind id manifestDigests with
        | Some digest -> { Id = id; Scope = Product; Sha256 = digest }: ExpectedSkill
        // Process (or co-tenant) skill: no reference digest — presence + identity only.
        | None -> { Id = id; Scope = Process; Sha256 = "" })

let actual =
    [ for root in agentSkillRoots do
          for id in presentIds do
              let path = toNative (skillPath root id)

              { Root = root
                Id = id
                Body = if File.Exists path then Some(File.ReadAllText path) else None }: ActualCopy ]

let drift = verify agentSkillRoots expected actual

// Non-SKILL.md files (e.g. reference.fsx, skill-manifest.json) are outside the manifest's
// granularity; assert their cross-root byte-identity directly so the whole union is covered.
let fileDrift =
    [ for rel in relSourceFiles do
          let sourceBytes = File.ReadAllBytes(toNative rel)

          for targetRoot in targetRoots do
              let targetRel = SkillMirror.retargetSkillPath targetRoot rel
              let targetPath = toNative targetRel

              if not (File.Exists targetPath) then
                  yield sprintf "%s missing" targetRel
              elif File.ReadAllBytes targetPath <> sourceBytes then
                  yield sprintf "%s divergent" targetRel ]

if List.isEmpty drift && List.isEmpty fileDrift then
    printfn "fs-gg-skill-roots: ok (%d skills, %d files mirrored)" presentIds.Length mirroredCount
    exit 0
else
    for d in drift do
        printfn
            "fs-gg-skill-roots: DRIFT %s missing=[%s] divergent=%b hash-mismatch=[%s]"
            d.Id
            (String.concat "; " d.MissingRoots)
            d.Divergent
            (String.concat "; " d.HashMismatchRoots)

    for f in fileDrift do
        printfn "fs-gg-skill-roots: DRIFT-FILE %s" f

    if enforce then exit 1 else exit 0
