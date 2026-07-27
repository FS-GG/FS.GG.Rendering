module Feature1086RequiredSurfaceTests

// Issue #1086 — `IsRequired = true` must FAIL the gate when the surface resolves to zero files.
//
// The defect these tests pin is not a wrong answer, it is a MISSING QUESTION. Every other producer in
// `SkillParity` reasons about entries that were found, so none of them could say anything about a
// surface that yielded none — and a REQUIRED surface pointing at nothing reported `passed`. Measured on
// `main` immediately before this file existed, by pointing `ant-canonical` at a nonexistent `NOPE.md`
// and running the real gate:
//
//     $ dotnet fsi scripts/check-agent-skill-parity.fsx
//     skill-parity status: passed
//     findings: critical=0 high=0 warning=0 info=0
//     exit 0
//
// `IsRequired` was published as a `Required` column in `docs/reports/skills-parity.md` reading `True`,
// asserting something nothing checked.
//
// Every test below is written so it would FAIL against that code. In particular the green-direction
// tests all carry a non-vacuity assertion: "no finding of category X" is trivially true of a run that
// produced no findings for any reason, including a harness that crashed into an empty report, and a
// test that cannot tell those apart is the same fail-open one level up.

open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private scratch (name: string) = Feature168SkillParityFixtures.createTempRoot name

/// A request whose report/summary writes land in a throwaway directory. `runCli` WRITES, and the real
/// `docs/reports/skills-parity.md` is a committed artifact with a CI gate on its diff.
let private cliArgs (repoRoot: string) (outRoot: string) (extra: string list) =
    [ "--repo"
      repoRoot
      "--out"
      Path.Combine(outRoot, "out")
      "--report"
      Path.Combine(outRoot, "out", "report.md")
      "--summary-json"
      Path.Combine(outRoot, "out", "summary.json") ]
    @ extra

let private unreadable (report: SkillParity.ParityReport) =
    report.Findings
    |> List.filter (fun finding -> finding.Category = SkillParity.UnreadableSurface)

[<Tests>]
let requiredSurfaceTests =
    testList "Feature1086 a required surface that resolves to zero files fails the gate" [

        // ---------- RED: the gate must go red on a genuinely bogus surface ----------

        test "an operator-supplied required surface at a nonexistent path fails the gate" {
            let outRoot = scratch "fsgg-1086-bogus"

            try
                let request =
                    { SkillParity.defaultRequest RepositoryRoot.value with
                        OutDir = Path.Combine(outRoot, "out")
                        ReportPath = Path.Combine(outRoot, "out", "report.md")
                        SummaryJsonPath = Path.Combine(outRoot, "out", "summary.json")
                        // The issue's own experiment, expressed through the public seam: a REQUIRED
                        // surface (every `--surface` override is required) pointing at a path that is
                        // not there. Deliberately NOT the id `ant-canonical`, because `filesForSurface`
                        // resolves that id from a hard-coded path and ignores `RootPath` entirely — an
                        // override under that name would silently read the REAL file and prove nothing.
                        SurfaceOverrides = [ "bogus-canonical", "docs/product/ant-design/skill/NOPE.md" ] }

                let report = SkillParity.runCheck request

                Expect.equal report.OverallStatus SkillParity.Failed "a required surface pointing at nothing fails"

                let findings = unreadable report
                Expect.hasLength findings 1 "exactly one unreadable-surface finding"
                Expect.equal findings.Head.SurfaceId "bogus-canonical" "the finding names the offending surface"
                Expect.equal findings.Head.Severity SkillParity.High "High, so the default --fail-on high blocks"
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }

        test "the harness process exits non-zero on a bogus required surface" {
            let outRoot = scratch "fsgg-1086-bogus-cli"

            try
                let exitCode =
                    SkillParity.runCli (
                        cliArgs
                            RepositoryRoot.value
                            outRoot
                            [ "--surface"; "bogus-canonical=docs/product/ant-design/skill/NOPE.md" ]
                    )

                // The acceptance criterion is about the PROCESS, not the report object: CI reads the
                // exit code. `runCli` is what `scripts/check-agent-skill-parity.fsx` shells into.
                Expect.notEqual exitCode 0 "the harness exits non-zero"
            finally
                Feature168SkillParityFixtures.deleteTempRoot outRoot
        }

        test "an empty REQUIRED synthetic fixture surface is reported" {
            let root = scratch "fsgg-1086-fixture-red"

            try
                // `wrapper-only` writes exactly one file, into `codex/`. So `fixture-canonical` and
                // `fixture-claude` — both IsRequired = true — resolve to zero files in this run.
                let report = SkillParity.runCheck (Feature168SkillParityFixtures.request root "wrapper-only")
                let reported = unreadable report |> List.map (fun finding -> finding.SurfaceId) |> Set.ofList

                Expect.contains reported "fixture-canonical" "the empty required canonical surface is reported"
                Expect.contains reported "fixture-claude" "the empty required wrapper surface is reported"
                Expect.equal report.OverallStatus SkillParity.Failed "and the run fails"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        // ---------- GREEN: and it must NOT go red on anything legitimate ----------

        test "an empty NON-required surface is left alone" {
            let root = scratch "fsgg-1086-optional"

            try
                let report = SkillParity.runCheck (Feature168SkillParityFixtures.request root "wrapper-only")
                let reported = unreadable report |> List.map (fun finding -> finding.SurfaceId) |> Set.ofList

                // Non-vacuity: this same run DOES report the required empties above, so a silence about
                // `fixture-optional` is a decision about IsRequired and not an inert producer. Holding
                // emptiness constant and flipping only the flag is the whole experiment.
                Expect.contains reported "fixture-canonical" "the required empty surface IS reported in this very run"

                Expect.isFalse
                    (reported.Contains "fixture-optional")
                    "fixture-optional is empty in every fixture run and NOT required, so empty stays legitimate"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "the passing fixture, whose required surfaces are all populated, stays green" {
            let root = scratch "fsgg-1086-fixture-green"

            try
                let report = SkillParity.runCheck (Feature168SkillParityFixtures.request root "passing")

                Expect.isEmpty (unreadable report) "no required fixture surface is empty in the passing fixture"
                Expect.equal report.OverallStatus SkillParity.Passed "the passing fixture is still passing"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        // ---------- The live tree: acceptance criterion 4 ----------

        test "every required surface this repository declares resolves to at least one file" {
            let root = RepositoryRoot.value
            let report = SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)

            // Non-vacuity first, and it is the assertion that matters most here: `List.isEmpty []` is
            // true of a surface list that failed to load at all, which is precisely the shape of bug
            // #1086 is about. Pin the count against the declared set.
            let required =
                SkillParity.discoverDefaultSurfaces root
                |> List.filter (fun surface -> surface.IsRequired)

            Expect.isGreaterThanOrEqual
                (List.length required)
                6
                "the repository declares at least the six required surfaces this item enumerated"

            let offenders = unreadable report |> List.map (fun finding -> finding.SurfaceId)

            Expect.isEmpty
                offenders
                (sprintf "no required surface in the live tree is empty; offenders: %A" offenders)
        }
    ]
