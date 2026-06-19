module Feature168ParityFindingTests

open System.IO
open Expecto
open Rendering.Harness

[<Tests>]
let tests =
    testList "Feature168 ParityFindings" [
        test "all synthetic fixture cases produce the expected finding categories" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature168-findings"

            try
                let report = SkillParity.runCheck (Feature168SkillParityFixtures.request root "all")
                let categories = report.Findings |> List.map (fun finding -> finding.Category) |> Set.ofList

                Expect.contains categories SkillParity.MissingWrapper "missing wrapper"
                Expect.contains categories SkillParity.WrapperOnly "wrapper only"
                Expect.contains categories SkillParity.StaleDescription "stale description"
                Expect.contains categories SkillParity.BrokenTarget "broken target"
                Expect.contains categories SkillParity.CanonicalDrift "canonical drift"
                Expect.contains categories SkillParity.GuidanceRuleGap "guidance gap"
                Expect.isTrue (report.FindingCountsBySeverity.High > 0) "high severity fixture findings"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "repository parity check does not rewrite existing skill files" {
            let root =
                let rec walk (dir: DirectoryInfo) =
                    if File.Exists(Path.Combine(dir.FullName, "FS.GG.Rendering.slnx")) then
                        dir.FullName
                    else
                        match dir.Parent with
                        | null -> Directory.GetCurrentDirectory()
                        | parent -> walk parent

                walk (DirectoryInfo(System.AppContext.BaseDirectory))

            let skillPath = Path.Combine(root, ".agents", "skills", "fs-gg-testing", "SKILL.md")
            let before = Feature168SkillParityFixtures.fileHash skillPath
            SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root) |> ignore
            let after = Feature168SkillParityFixtures.fileHash skillPath

            Expect.equal after before "non-destructive checker"
        }
    ]
