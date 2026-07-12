module Feature529MetadataSourceTests

// #529 (from #466) — `metadata.source` is a citation, and until now nothing followed it.
//
// #466 was a SKILL.md whose `metadata.source` named a spec path that had never existed in this repo. Its
// FR-014/FR-015/FR-016 citations then landed on UNRELATED features holding those same numbers — worse than a
// dead link, because it resolves to something plausible and wrong. The failure is silent for exactly as long
// as nobody tries to follow the link, which is how it survived a repo migration.
//
// EVERY TEST BELOW IS ABOUT THE EXEMPTION, because the exemption is the only place this rule can fail silently.
// The first draft of this guard keyed on `author: FS.GG` and exempted everything else — and #466's own file is
// authored `fs-gg-ui`, so the rule exempted the very skill it existed to catch and reported green. The rule is
// now inverted: a closed allow-list of VENDORED authors, so an unknown author is enforced, not excused. These
// tests pin that inversion in both directions, since getting it wrong either way is the whole risk.

open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private skillDir (root: string) (surface: string) (name: string) =
    let dir = Path.Combine(root, surface, "skills", name)
    Directory.CreateDirectory dir |> ignore
    dir

/// A canonical skill on a surface `defaultRequest` really scans, carrying the frontmatter under test.
let private writeSkillOn (root: string) (surface: string) (name: string) (frontMatter: string) =
    File.WriteAllText(
        Path.Combine(skillDir root surface name, "SKILL.md"),
        $"---\nname: \"{name}\"\ndescription: \"A skill for {name}.\"\n{frontMatter}---\n\n# {name}\n\nBody.\n"
    )

let private writeSkill root name frontMatter =
    writeSkillOn root ".claude" name frontMatter

let private sourceFindings (root: string) =
    SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)
    |> fun report -> report.Findings
    |> List.filter (fun finding -> finding.Category = SkillParity.UnresolvedMetadataSource)

/// A citation naming a spec path that does not exist — #466's shape, parameterised by author.
let private danglingSource (author: string) =
    $"metadata:\n  author: \"{author}\"\n  source: \"specs/058-a-feature-that-never-existed/contracts/x.md\"\n"

[<Tests>]
let tests =
    testList
        "Feature529 metadata.source resolves"
        [
          // THE REGRESSION TEST FOR THE GUARD ITSELF. #466's file (template/feedback/skill/SKILL.md) is authored
          // `fs-gg-ui`, not `FS.GG`. An FS.GG-only rule exempts it — green, with #466 fully reintroduced.
          test "an fs-gg-ui-authored skill is held to its citation — #466's own author must not be exempt" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-fsggui"

              try
                  writeSkill root "fs-gg-feedback-capture" (danglingSource "fs-gg-ui")

                  let findings = sourceFindings root

                  Expect.hasLength findings 1 "an `fs-gg-ui` skill is authored HERE — exempting it would exempt the exact file #466 was about"
                  Expect.equal findings.Head.Severity SkillParity.High "High, or it does not fail the gate"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // Fail-closed: an author nobody has heard of is CHECKED, not excused.
          test "an unknown author is enforced, not silently exempted" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-unknown"

              try
                  writeSkill root "fs-gg-newcomer" (danglingSource "some-new-team")

                  Expect.hasLength
                      (sourceFindings root)
                      1
                      "the exemption is a closed allow-list of vendored authors; a new name must default to ENFORCED, or the rule fails open the first time somebody coins one"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "an FS.GG-authored skill whose metadata.source does not resolve is a High finding (#466)" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-dangling"

              try
                  writeSkill root "fs-gg-dangling" (danglingSource "FS.GG")

                  let findings = sourceFindings root

                  Expect.hasLength findings 1 "the dangling citation is reported"
                  Expect.stringContains findings.Head.Message "058-a-feature-that-never-existed" "the finding names the path that did not resolve, so the author need not go hunting for it"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "a metadata.source resolving OUTSIDE the repository is a finding, not a pass" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-escape"

              try
                  // Both EXIST. Neither is in this repository — and "a pointer carried in from another repo that
                  // happens to resolve on the author's disk" is #466's shape precisely.
                  writeSkill root "fs-gg-absolute" "metadata:\n  author: \"FS.GG\"\n  source: \"/etc\"\n"
                  writeSkill root "fs-gg-relative" "metadata:\n  author: \"FS.GG\"\n  source: \"../..\"\n"

                  let findings = sourceFindings root

                  Expect.hasLength findings 2 "existence is not enough — the citation must be inside THIS repository, or no other checkout can follow it"
                  Expect.all findings (fun f -> f.Severity = SkillParity.High) "High"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "an FS.GG-authored skill whose metadata.source resolves is clean" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-resolves"

              try
                  Directory.CreateDirectory(Path.Combine(root, "specs", "131-real")) |> ignore
                  writeSkill root "fs-gg-real" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/131-real\"\n"

                  Expect.isEmpty (sourceFindings root) "a citation that resolves is not a finding — a DIRECTORY counts, as fs-gg-ant-design's does"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // The half that must NOT fire. These are synced from upstream, so a red here is a red nobody can clear —
          // and an unfixable red is a gate somebody switches off.
          test "a vendored skill's upstream metadata.source is provenance, not a citation of this repo" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-vendored"

              try
                  writeSkill root "speckit-analyze" "metadata:\n  author: \"github-spec-kit\"\n  source: \"templates/commands/analyze.md\"\n"
                  writeSkill root "speckit-git-commit" "metadata:\n  author: \"github-spec-kit\"\n  source: \"git:commands/speckit.git.commit.md\"\n"

                  Expect.isEmpty (sourceFindings root) "a vendored skill's `source` names a path in the UPSTREAM repo; requiring it to resolve here would red the gate on content this repo cannot fix"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "a metadata.source with no metadata.author is a High finding, not a free pass" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-unattributed"

              try
                  writeSkill root "fs-gg-unattributed" "metadata:\n  source: \"specs/058-a-feature-that-never-existed/contracts/x.md\"\n"

                  Expect.hasLength (sourceFindings root) 1 "an unattributed source cannot fall silently into the vendored exemption — dropping a line must not be a way to evade the rule"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // Two SKILL.md can share a SurfaceId AND a SkillName (the spec-kit surface concatenates .agents and
          // .claude), and findings are deduped by FindingId — so a finding id without the path reports ONE of two
          // broken files and silently drops the other.
          test "two broken copies of one skill name are two findings, not one" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature529-dedupe"

              try
                  writeSkillOn root ".claude" "fs-gg-twin" (danglingSource "FS.GG")
                  writeSkillOn root ".agents" "fs-gg-twin" (danglingSource "FS.GG")

                  let findings = sourceFindings root

                  Expect.hasLength findings 2 "both copies are broken and both must be reported; collapsing them hides one file from whoever fixes the other"

                  Expect.equal
                      (findings |> List.map (fun f -> f.FindingId) |> List.distinct |> List.length)
                      2
                      "the finding ids must differ, or List.distinctBy in classifyFindings drops one"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // Guards the live tree — and guards itself against being vacuous, using the RULE's own predicate rather
          // than a stricter one (an exact-case "FS.GG" test would count 1 file and miss the fs-gg-ui skills the
          // rule now covers, so the guard would not actually be guarding the rule's scope).
          test "this repository's own skills cite nothing that does not resolve" {
              let root = RepositoryRoot.value
              let report = SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)

              // The entries `runCheck` really inventories — NOT every SKILL.md on disk. Some trees
              // (template/base/.claude/skills/**) sit under no surface, so counting files would let this guard be
              // satisfied by a skill the rule never sees, leaving the assertion below vacuous while looking green.
              let request = Feature168SkillParityFixtures.repositoryRequest root

              let enforced =
                  SkillParity.inventorySkills request (SkillParity.discoverDefaultSurfaces root)
                  |> List.filter (fun entry ->
                      let has key =
                          entry.Metadata
                          |> Map.tryFind key
                          |> Option.map (fun v -> v.Trim())
                          |> Option.filter (fun v -> v <> "")

                      match has "source", has "author" with
                      | Some _, Some author -> author.Trim().ToLowerInvariant() <> "github-spec-kit"
                      | Some _, None -> true
                      | None, _ -> false)

              Expect.isGreaterThan
                  (List.length enforced)
                  0
                  "at least one INVENTORIED skill declares a `metadata.source` this rule enforces (if this fails, the assertion below passes vacuously and the rule guards nothing)"

              let dangling =
                  report.Findings
                  |> List.filter (fun finding -> finding.Category = SkillParity.UnresolvedMetadataSource)

              Expect.isEmpty
                  dangling
                  $"every enforced metadata.source in this repo resolves; unresolved: {dangling |> List.map (fun f -> f.CanonicalPath)}"
          } ]
