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

          // A source that RESOLVES but cannot be read is the fail-open this rule shipped with in review: a
          // directory source satisfies #529 (`File.Exists || Directory.Exists`), and the first draft of THIS
          // rule then found no spec.md and skipped too. Both went green, so `source: src/Symbology` — one
          // line — was a complete bypass of a High gate. Neither rule may defer to the other here.
          test "a source that resolves but has no spec.md is a finding, not a free pass" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-unreadable"

              try
                  Directory.CreateDirectory(Path.Combine(root, "src", "Symbology")) |> ignore
                  writeSkill root "fs-gg-bypass" "metadata:\n  author: \"FS.GG\"\n  source: \"src/Symbology\"\n" "The loop is fixed (FR-014)."

                  Expect.hasLength (citationFindings root) 1 "an existing directory with no spec.md checks NOTHING — if this passes, any skill can silence this gate by pointing `source:` at any folder in the repo"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // A requirement merely MENTIONED in the source's prose must not bless a citation — only one the
          // spec DEFINES (`- **FR-014**: …`). Specs cross-reference each other's FRs constantly.
          test "a requirement the source only mentions in passing does not count as stated" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-mentioned"

              try
                  writeSpec root "192-real" "- **FR-014**: an orchestrating skill.\n\nUnlike FR-021 in spec 194, this stays scoped."
                  writeSkill root "fs-gg-mention" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/192-real\"\n" "See FR-021."

                  Expect.hasLength (citationFindings root) 1 "FR-021 is name-dropped in 192's prose, not DEFINED by it — a passing mention is not a requirement this spec states"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // `FR-14` and `FR-014` are the same requirement. Reporting the unpadded form as phantom would be a
          // false RED — and a gate that reds on a correct citation is one somebody switches off.
          test "an unpadded citation is the same requirement, not a phantom" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-padding"

              try
                  writeSpec root "192-real" "- **FR-014**: an orchestrating skill."
                  writeSkill root "fs-gg-padding" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/192-real\"\n" "See FR-14."

                  Expect.isEmpty (citationFindings root) "FR-14 means the spec's FR-014 — comparing as strings would red a correct citation"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // The coordination kit is synced verbatim from FS-GG/.github and cites FR-007, so it must never red
          // this gate. It cannot today — `filesForSurface` drops it at INVENTORY, so it never reaches the
          // rule. Assert that real mechanism: a test that writes a kit skill and finds no finding would pass
          // even with the exemption deleted, and would be proving nothing.
          test "the externally-owned coordination kit never reaches this rule (it is not inventoried)" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature573-kit"

              try
                  writeSkill root "cross-repo-coordination" "" "Follow the protocol (FR-007)."
                  writeSkill root "fs-gg-ordinary" "metadata:\n  author: \"FS.GG\"\n  source: \"specs/192-real\"\n" "No citations here."
                  writeSpec root "192-real" "- **FR-014**: an orchestrating skill."

                  let request = Feature168SkillParityFixtures.repositoryRequest root

                  let inventoried =
                      SkillParity.inventorySkills request (SkillParity.discoverDefaultSurfaces root)
                      |> List.map (fun entry -> entry.SkillName)

                  Expect.contains inventoried "fs-gg-ordinary" "the fixture root really is being inventoried (else this test proves nothing)"

                  Expect.isFalse
                      (List.contains "cross-repo-coordination" inventoried)
                      "the kit is externally owned (FS-GG/.github, synced verbatim) and is excluded at inventory — an edit here is reverted by the next sync, so reddening on it would be a gate nobody can clear"

                  Expect.isEmpty (citationFindings root) "and so its FR-007 produces no finding"
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

              // Count only the skills the rule ACTUALLY governs. Counting every citing skill would include the
              // vendored speckit ones, which are exempt — so the guard would stay satisfied even if every
              // enforced citation vanished, and `isEmpty` below would go vacuous without anybody noticing.
              let citing =
                  SkillParity.inventorySkills request (SkillParity.discoverDefaultSurfaces root)
                  |> List.filter (fun entry ->
                      let metadata, body = SkillParity.parseFrontMatter entry.Content

                      let vendored =
                          metadata
                          |> Map.tryFind "author"
                          |> Option.map (fun a -> a.Trim().ToLowerInvariant())
                          |> Option.exists (fun a -> a = "github-spec-kit")

                      not vendored
                      && System.Text.RegularExpressions.Regex.IsMatch(body, @"\bFR-\d+\b"))

              Expect.isGreaterThan
                  (List.length citing)
                  0
                  "at least one inventoried, NON-EXEMPT skill cites an FR (if this fails, the assertion below passes vacuously and the rule guards nothing)"

              let unsourced =
                  report.Findings
                  |> List.filter (fun finding -> finding.Category = SkillParity.UnsourcedRequirementCitation)

              Expect.isEmpty
                  unsourced
                  $"every FR this repo's skills cite is placed in a spec that states it; unplaced: {unsourced |> List.map (fun f -> f.CanonicalPath)}"
          } ]
