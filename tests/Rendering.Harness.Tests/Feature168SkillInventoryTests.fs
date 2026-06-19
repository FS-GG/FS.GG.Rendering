module Feature168SkillInventoryTests

open System
open System.IO
open Expecto
open Rendering.Harness

let private repoRoot () =
    AppContext.BaseDirectory
    |> fun dir -> DirectoryInfo(dir)
    |> fun dir ->
        let rec walk (current: DirectoryInfo) =
            if File.Exists(Path.Combine(current.FullName, "FS.GG.Rendering.slnx")) then
                current.FullName
            else
                match current.Parent with
                | null -> Directory.GetCurrentDirectory()
                | parent -> walk parent

        walk dir

let private surfaceLines (path: string) =
    File.ReadAllLines path
    |> Array.choose (fun line ->
        let trimmed = line.Trim()

        if trimmed = "module SkillParity =" then
            Some trimmed
        elif trimmed.StartsWith("type ", StringComparison.Ordinal) then
            trimmed.Split([| ' '; '=' |], StringSplitOptions.RemoveEmptyEntries)
            |> fun parts -> if parts.Length >= 2 then Some($"type {parts[1]}") else None
        elif trimmed.StartsWith("val ", StringComparison.Ordinal) then
            trimmed.Split([| ' '; ':' |], StringSplitOptions.RemoveEmptyEntries)
            |> fun parts -> if parts.Length >= 2 then Some($"val {parts[1]}") else None
        else
            None)
    |> Array.toList

[<Tests>]
let tests =
    testList "Feature168 SkillInventory" [
        test "SkillParity FSI surface matches the readiness baseline" {
            let root = repoRoot ()
            let fsi = Path.Combine(root, "tests", "Rendering.Harness", "SkillParity.fsi")
            let baseline = Path.Combine(root, "specs", "168-skill-parity-evidence", "readiness", "surface-baselines", "Rendering.Harness.SkillParity.txt")

            Expect.equal (surfaceLines fsi) (File.ReadAllLines baseline |> Array.toList) "surface drift"
        }

        test "fixture passing case resolves wrapper targets without broken-target findings" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-inventory"

            try
                let report = SkillParity.runCheck (Feature168SkillParityFixtures.request root "passing")
                Expect.isGreaterThan report.WrapperCount 0 "wrappers discovered"
                Expect.isFalse (report.Findings |> List.exists (fun finding -> finding.Category = SkillParity.BrokenTarget)) "target resolves"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "repository inventory includes canonical, wrapper, command, and Ant surfaces" {
            let root = repoRoot ()
            let surfaces = SkillParity.discoverDefaultSurfaces root
            let ids = surfaces |> List.map (fun surface -> surface.SurfaceId) |> Set.ofList

            Expect.contains ids "package-canonical" "package surface"
            Expect.contains ids "template-canonical" "template surface"
            Expect.contains ids "codex-local" "codex surface"
            Expect.contains ids "claude" "claude surface"
            Expect.contains ids "ant-canonical" "ant canonical"
            Expect.contains ids "spec-kit-command" "command surface"
        }

        test "repository parity has no unresolved findings" {
            let root = repoRoot ()
            let report = SkillParity.runCheck (SkillParity.defaultRequest root)

            Expect.equal report.OverallStatus SkillParity.Passed "repository parity status"
            Expect.isEmpty report.Findings "unresolved findings"
        }
    ]
