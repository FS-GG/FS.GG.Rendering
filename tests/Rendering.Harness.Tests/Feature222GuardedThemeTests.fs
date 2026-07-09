module Feature222GuardedThemeTests

open System.IO
open Expecto
open Rendering.Harness

/// A synthetic closed world: `package-feed` is dispatched, `feed-proof` is not.
let private harnessCommands = Set [ "package-feed"; "skill-parity" ]

let private theme =
    { SkillParity.ThemeId = "package-pin-drift"
      SkillParity.Intent = "samples prove their pins against the local feed"
      SkillParity.Artifacts =
        [ SkillParity.HarnessCommand "package-feed"
          SkillParity.RepoPath "scripts/refresh.fsx" ]
      SkillParity.ApplicablePatterns = [ "template/fragments/samples" ] }

let private inScope body =
    Feature168SkillParityFixtures.entry "template/fragments/samples/skill/SKILL.md" "fs-gg-samples" "sample guidance" body

/// The repository root is only consulted for `RepoPath`; a root with no `scripts/refresh.fsx` makes
/// that artifact unresolvable, which is what the dangling cases need.
let private statusIn root body =
    SkillParity.evaluateArtifactReferences root harnessCommands [ theme ] [ inScope body ]
    |> List.map (fun item -> item.Status)

let private statusOf body = statusIn "/nonexistent-root" body

[<Tests>]
let tests =
    testList "Feature222 GuardedThemes" [
        test "a skill naming a dispatched harness verb satisfies its theme" {
            Expect.equal
                (statusOf "Use the `package-feed` proof workflow.")
                [ SkillParity.ArtifactResolved ]
                "the verb exists, so the guidance points at something real"
        }

        test "a skill in scope that names none of the theme's artifacts is unnamed" {
            // The spec-235 FR-006 regression: delete the local-feed guidance and the theme goes red.
            Expect.equal
                (statusOf "Use the proof workflow to check pins.")
                [ SkillParity.ArtifactUnnamed ]
                "guidance deleted, so nothing points at the local feed"
        }

        test "a skill naming an artifact that no longer exists is dangling" {
            // Intact prose over a renamed verb. The words are all still there; the artifact is not.
            let references =
                SkillParity.evaluateArtifactReferences
                    "/nonexistent-root"
                    (Set [ "feed-proof" ])
                    [ theme ]
                    [ inScope "Use the `package-feed` proof workflow." ]

            Expect.equal (references |> List.map (fun item -> item.Status)) [ SkillParity.ArtifactDangling ] "the verb was renamed"

            Expect.equal
                (references |> List.map (fun item -> item.Reference))
                [ Some(SkillParity.HarnessCommand "package-feed") ]
                "the finding names the artifact that went away"
        }

        test "keeping the theme's words without naming an artifact does not satisfy it" {
            // The whole point of #222: `content.Contains "local feed"` was satisfiable by prose.
            Expect.equal
                (statusOf "Prove stale package pins are absent and that the local feed is the restore source.")
                [ SkillParity.ArtifactUnnamed ]
                "vocabulary is not a reference"
        }

        test "an artifact named only in prose is not named, because prose cannot point at code" {
            Expect.equal (statusOf "Use the package-feed proof workflow.") [ SkillParity.ArtifactUnnamed ] "no code span"
        }

        test "a code span whose word merely contains the verb does not name it" {
            Expect.equal (statusOf "See `package-feedback` for details.") [ SkillParity.ArtifactUnnamed ] "not the verb"
        }

        test "a verb inside a longer command line still names it" {
            Expect.equal (statusOf "Run `harness package-feed --check` first.") [ SkillParity.ArtifactResolved ] "one word is the verb"
        }

        test "a theme is satisfied by whichever of its artifacts resolves" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature222-alternation"

            try
                let scripts = Path.Combine(root, "scripts")
                Directory.CreateDirectory scripts |> ignore
                File.WriteAllText(Path.Combine(scripts, "refresh.fsx"), "// refresh")

                // The verb is gone from this closed world, but the script the guidance also names is real.
                let references =
                    SkillParity.evaluateArtifactReferences
                        root
                        Set.empty
                        [ theme ]
                        [ inScope "Run `dotnet fsi scripts/refresh.fsx`, then the `package-feed` workflow." ]

                Expect.equal
                    (references |> List.map (fun item -> item.Status))
                    [ SkillParity.ArtifactResolved ]
                    "an alternative that resolves satisfies the theme"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "a theme is dangling only when every artifact it names has gone away" {
            let references =
                SkillParity.evaluateArtifactReferences
                    "/nonexistent-root"
                    Set.empty
                    [ theme ]
                    [ inScope "Run `dotnet fsi scripts/refresh.fsx`, then the `package-feed` workflow." ]

            Expect.equal
                (references |> List.map (fun item -> item.Status))
                [ SkillParity.ArtifactDangling ]
                "neither the verb nor the script resolves"
        }

        test "a skill outside the theme's scope is not judged by it" {
            let outOfScope =
                Feature168SkillParityFixtures.entry "src/Scene/skill/SKILL.md" "fs-gg-scene" "scene guidance" "No pins here."

            Expect.isEmpty
                (SkillParity.evaluateArtifactReferences "/nonexistent-root" harnessCommands [ theme ] [ outOfScope ])
                "the theme claims nothing about an unrelated skill"
        }

        test "wrapper entries are not a process-guidance surface" {
            let wrapper =
                { inScope "Use the proof workflow." with EntryKind = SkillParity.WrapperEntry }

            Expect.isEmpty
                (SkillParity.evaluateArtifactReferences "/nonexistent-root" harnessCommands [ theme ] [ wrapper ])
                "only canonical and command skills carry process guidance"
        }

        test "the harness dispatch table is the closed world for a command artifact" {
            let root = FS.GG.TestSupport.RepositoryRoot.value
            let commands = SkillParity.loadHarnessCommands root |> Option.get

            Expect.contains commands "package-feed" "a dispatched verb"
            Expect.contains commands "skill-parity" "a dispatched verb"
            Expect.isFalse (commands |> Set.contains "__viewer") "internal arms are not verbs a skill may point at"
            Expect.isFalse (commands |> Set.contains "--help") "options are not verbs"
        }

        test "a missing dispatch table is reported, never silently passed" {
            let root = Feature168SkillParityFixtures.createTempRoot "feature222-missing-cli"

            try
                Expect.isNone (SkillParity.loadHarnessCommands root) "no harness dispatch table"
            finally
                Feature168SkillParityFixtures.deleteTempRoot root
        }

        test "every guarded theme covers at least one repository skill" {
            // A theme whose scope silently stopped matching anything would report a green it never earned.
            let root = FS.GG.TestSupport.RepositoryRoot.value
            let report = SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)

            Expect.isNonEmpty report.GuardedThemeCoverage "themes are resolved against the repository"

            for summary in report.GuardedThemeCoverage do
                Expect.isGreaterThan summary.Scoped 0 $"{summary.ThemeId} applies to no skill"
        }

        test "spec 235 FR-006: the repository's guarded themes all resolve, with zero findings" {
            let root = FS.GG.TestSupport.RepositoryRoot.value
            let report = SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)

            for summary in report.GuardedThemeCoverage do
                Expect.equal summary.Dangling 0 $"{summary.ThemeId} points at a missing artifact"
                Expect.equal summary.Unnamed 0 $"{summary.ThemeId} guidance is missing from a skill in its scope"
                Expect.equal summary.Resolved summary.Scoped $"{summary.ThemeId} resolves for every skill in scope"

            let artifactFindings =
                report.Findings
                |> List.filter (fun finding ->
                    finding.Category = SkillParity.UnresolvedArtifactReference
                    || finding.Category = SkillParity.MissingRequiredArtifact)

            Expect.isEmpty artifactFindings "no guarded-theme findings"
        }

        test "the fs-gg-samples skill still points at the local-feed proof workflow" {
            // The concrete artifact FR-006 named. Deleting the guidance makes this test, and the
            // checker, red — which is the assurance the substring rule only appeared to give.
            let root = FS.GG.TestSupport.RepositoryRoot.value
            let commands = SkillParity.loadHarnessCommands root |> Option.get
            let request = SkillParity.defaultRequest root
            let entries = SkillParity.inventorySkills request (SkillParity.discoverDefaultSurfaces root)

            let samples =
                SkillParity.evaluateArtifactReferences root commands (SkillParity.defaultGuardedThemes ()) entries
                |> List.filter (fun item ->
                    item.ThemeId = "package-pin-drift"
                    && item.Path.Contains "template/fragments/samples")

            Expect.isNonEmpty samples "the samples skill is in package-pin-drift's scope"

            for item in samples do
                Expect.equal item.Status SkillParity.ArtifactResolved "names a local-feed artifact that resolves"
        }
    ]
