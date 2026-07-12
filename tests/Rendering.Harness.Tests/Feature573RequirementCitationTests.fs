module Feature573RequirementCitationTests

// #573 — the other half of #466.
//
// #529 made a DECLARED citation checkable. This makes an UNDECLARED one impossible to hide behind, and
// closes the gap between them: a citation that names a real spec which does not state the requirement.
//
// The bug was live in this repo. `src/Symbology/skill/SKILL.md` cited FR-014, FR-016, FR-017, FR-018 and
// FR-019 with no `metadata.source` at all — while SEVEN symbology specs each define their own FR-014 and
// FR-016 meaning entirely different things (192's FR-014 is "provide an orchestrating skill"; 194's is
// "the approved symbol set must lint clean"; 196's is "stay in the pure scene-only layer"). An FR number
// is unique only WITHIN a feature, so the bare citation named nothing and a reader landed on whichever of
// the seven they found first.

open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private writeSkill (root: string) (name: string) (frontMatter: string) (body: string) =
    let dir = Path.Combine(root, ".claude", "skills", name)
    Directory.CreateDirectory dir |> ignore

    File.WriteAllText(
        Path.Combine(dir, "SKILL.md"),
        $"---\nname: \"{name}\"\ndescription: \"A skill for {name}.\"\n{frontMatter}---\n\n# {name}\n\n{body}\n"
    )

let private writeSpec (root: string) (feature: string) (requirements: string) =
    let dir = Path.Combine(root, "specs", feature)
    Directory.CreateDirectory dir |> ignore
    File.WriteAllText(Path.Combine(dir, "spec.md"), $"# {feature}\n\n{requirements}\n")

let private citationFindings (root: string) =
    SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)
    |> fun report -> report.Findings
    |> List.filter (fun finding -> finding.Category = SkillParity.UnsourcedRequirementCitation)

[<Tests>]
let tests =
    testList
        "Feature573 requirement citations resolve"
        [ test "a skill citing FR-nnn with no metadata.source is a High finding (#466/#573)" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-bare"

              try
                  writeSkill root "fs-gg-bare" "" "The loop is fixed (FR-014 / FR-016)."

                  let findings = citationFindings root

                  Expect.hasLength findings 1 "a bare FR citation names nothing — FR numbers are unique only within a feature"
                  Expect.equal findings.Head.Severity SkillParity.High "High, or it does not fail the gate"
                  Expect.stringContains findings.Head.Message "FR-014" "the finding names the citations it could not place"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // THE ONE THAT MATTERS. A source line makes a citation look checked. This is the case where it
          // looks checked and is still wrong — #466's failure with a source bolted on.
          test "a citation naming a REAL spec that does not state the requirement is still a finding" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-phantom"

              try
                  writeSpec root "194-the-linter" "- **FR-014**: something else entirely."
                  writeSkill root "fs-gg-phantom" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/194-the-linter\"\n" "Provenance the loop MUST write (FR-017 / FR-018)."

                  let findings = citationFindings root

                  Expect.hasLength findings 1 "the spec resolves, so #529's rule is satisfied — but it does not STATE FR-017/FR-018, and a citation that resolves to the wrong feature is the whole of #466"
                  Expect.stringContains findings.Head.Message "FR-017" "the finding names the phantom requirement"
                  Expect.isFalse (findings.Head.Message.Contains "FR-014") "FR-014 IS stated by that spec, so it is not phantom — the rule must not cry wolf on the citations that do resolve"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "a citation whose source states every requirement it cites is clean" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-clean"

              try
                  writeSpec root "192-the-real-one" "- **FR-014**: an orchestrating skill.\n- **FR-016**: a fixed protocol."
                  writeSkill root "fs-gg-clean" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/192-the-real-one\"\n" "The loop is fixed (FR-014 / FR-016)."

                  Expect.isEmpty (citationFindings root) "every cited requirement is stated by the declared source"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // Must NOT fire. The coordination kit is synced verbatim from FS-GG/.github and cites FR-007; an
          // edit here is reverted by the next sync, so a red would be one nobody can clear.
          test "the externally-owned coordination kit is exempt" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-kit"

              try
                  writeSkill root "cross-repo-coordination" "" "Follow the protocol (FR-007)."

                  Expect.isEmpty (citationFindings root) "the coordination kit is owned by FS-GG/.github and synced verbatim — reddening on it would be an unfixable red, and an unfixable red is a gate somebody switches off"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "a vendored spec-kit skill is exempt" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-vendored"

              try
                  writeSkill root "speckit-analyze" "metadata:\n  author: \"github-spec-kit\"\n  source: \"templates/commands/analyze.md\"\n" "Upstream text mentioning FR-001."

                  Expect.isEmpty (citationFindings root) "vendored upstream content is not this repo's to cite-check"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // Guards the live tree, and guards itself against being vacuous.
          test "this repository's own skills cite no requirement they cannot place" {
              let root = RepositoryRoot.value
              let request = Feature168SkillParityFixtures.repositoryRequest root
              let report = SkillParity.runCheck request

              let citing =
                  SkillParity.inventorySkills request (SkillParity.discoverDefaultSurfaces root)
                  |> List.filter (fun entry ->
                      let _, body = SkillParity.parseFrontMatter entry.Content
                      System.Text.RegularExpressions.Regex.IsMatch(body, @"\bFR-\d+\b"))

              Expect.isGreaterThan
                  (List.length citing)
                  0
                  "at least one inventoried skill cites an FR (if this fails, the assertion below passes vacuously and the rule guards nothing)"

              let unsourced =
                  report.Findings
                  |> List.filter (fun finding -> finding.Category = SkillParity.UnsourcedRequirementCitation)

              Expect.isEmpty
                  unsourced
                  $"every FR this repo's skills cite is placed in a spec that states it; unplaced: {unsourced |> List.map (fun f -> f.CanonicalPath)}"
          } ]
