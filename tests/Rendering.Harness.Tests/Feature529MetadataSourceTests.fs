module Feature529MetadataSourceTests

// #529 (from #466) — `metadata.source` is a citation, and until now nothing followed it.
//
// #466 was a template/** SKILL.md whose `metadata.source` named a spec path that had never existed in this
// repo. Its FR-014/FR-015/FR-016 citations then landed on UNRELATED features holding those same numbers —
// worse than a dead link, because it resolves to something plausible and wrong. The failure is silent for
// exactly as long as nobody tries to follow the link, which is why it survived a repo migration.
//
// The rule is scoped by `metadata.author`, and these tests pin BOTH halves of that scoping — the half that
// must fire and the half that must not. Getting the exemption wrong in either direction is the whole risk:
// too narrow and the gate reddens on 30 vendored spec-kit skills nobody here can fix (an unfixable red is a
// disabled gate); too wide and #466 walks straight back in.

open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

/// A canonical skill on a surface `defaultRequest` actually scans, with the frontmatter under test.
let private writeSkill (root: string) (name: string) (frontMatter: string) =
    let dir = Path.Combine(root, ".claude", "skills", name)
    Directory.CreateDirectory dir |> ignore

    File.WriteAllText(
        Path.Combine(dir, "SKILL.md"),
        $"---\nname: \"{name}\"\ndescription: \"A skill for {name}.\"\n{frontMatter}---\n\n# {name}\n\nBody.\n"
    )

let private sourceFindingsFor (root: string) =
    SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)
    |> fun report -> report.Findings
    |> List.filter (fun finding -> finding.Category = SkillParity.UnresolvedMetadataSource)

[<Tests>]
let tests =
    testList
        "Feature529 metadata.source resolves"
        [ test "an FS.GG-authored skill whose metadata.source does not resolve is a High finding (#466)" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-dangling"

              try
                  writeSkill root "fs-gg-dangling" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/058-a-feature-that-never-existed/contracts/x.md\"\n"

                  let findings = sourceFindingsFor root

                  Expect.hasLength findings 1 "the dangling citation is reported"
                  Expect.equal findings.Head.Severity SkillParity.High "High — FailOnSeverity is High, so a Warning here would not fail the gate and the citation would stay unchecked"
                  Expect.stringContains findings.Head.SkillName "fs-gg-dangling" "the finding names the skill"
                  Expect.stringContains findings.Head.Message "058-a-feature-that-never-existed" "the finding names the path that did not resolve, so the author does not have to go looking for it"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "an FS.GG-authored skill whose metadata.source resolves is clean" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-resolves"

              try
                  Directory.CreateDirectory(Path.Combine(root, "specs", "131-real")) |> ignore
                  writeSkill root "fs-gg-real" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/131-real\"\n"

                  Expect.isEmpty (sourceFindingsFor root) "a citation that resolves is not a finding — a DIRECTORY counts, as fs-gg-ant-design's does"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // The half that must NOT fire. 30 vendored spec-kit skills declare `metadata.source` as UPSTREAM
          // provenance — a path inside github/spec-kit, which cannot resolve here by construction. They are
          // synced, so an edit here is reverted by the next sync: reddening on them would be an unfixable red,
          // and an unfixable red is a gate somebody switches off.
          test "a vendored skill's upstream metadata.source is provenance, not a citation of this repo" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-vendored"

              try
                  writeSkill root "speckit-analyze" "metadata:\n  author: \"github-spec-kit\"\n  source: \"templates/commands/analyze.md\"\n"
                  writeSkill root "speckit-git-commit" "metadata:\n  author: \"github-spec-kit\"\n  source: \"git:commands/speckit.git.commit.md\"\n"

                  Expect.isEmpty (sourceFindingsFor root) "a vendored skill's `source` names a path in the UPSTREAM repo; requiring it to resolve here would red the gate on content this repo cannot fix"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // The exemption must be entered by DECLARING an author, never by omitting one — otherwise the cheapest
          // way to silence this rule is to delete a line, which is the silent-drift shape it exists to stop.
          test "a metadata.source with no metadata.author is a High finding, not a free pass" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-unattributed"

              try
                  writeSkill root "fs-gg-unattributed" "metadata:\n  source: \"specs/058-a-feature-that-never-existed/contracts/x.md\"\n"

                  let findings = sourceFindingsFor root

                  Expect.hasLength findings 1 "an unattributed source cannot fall silently into the vendored exemption"
                  Expect.equal findings.Head.Severity SkillParity.High "High, or dropping `author:` would be a way to evade the rule while the gate stayed green"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // Guards the live tree, and guards itself against being vacuous: if no FS.GG-authored skill declares a
          // `source:` at all, the clean result below proves nothing.
          test "this repository's own skills cite nothing that does not resolve" {
              let root = RepositoryRoot.value
              let report = SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)

              let authoredHereWithSource =
                  Directory.EnumerateFiles(root, "SKILL.md", SearchOption.AllDirectories)
                  |> Seq.filter (fun path -> not (path.Contains(Path.DirectorySeparatorChar.ToString() + ".git" + Path.DirectorySeparatorChar.ToString())))
                  |> Seq.map (File.ReadAllText >> SkillParity.parseFrontMatter >> fst)
                  |> Seq.filter (fun metadata ->
                      (metadata |> Map.tryFind "author" |> Option.map (fun a -> a.Trim()) = Some "FS.GG")
                      && (metadata |> Map.containsKey "source"))
                  |> Seq.length

              Expect.isGreaterThan
                  authoredHereWithSource
                  0
                  "at least one FS.GG-authored skill declares `metadata.source` (if this fails, the assertion below is vacuous and the rule guards nothing)"

              let dangling =
                  report.Findings
                  |> List.filter (fun finding -> finding.Category = SkillParity.UnresolvedMetadataSource)

              Expect.isEmpty dangling $"every metadata.source this repo authors resolves; unresolved: {dangling |> List.map (fun f -> f.SkillName)}"
          } ]
