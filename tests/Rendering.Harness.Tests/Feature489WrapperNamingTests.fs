module Feature489WrapperNamingTests

// #489 — "the two feedback wrappers are named by canonical id, not the fs-gg-product-* alias every other
// product skill uses; decide which rule is real."
//
// THE DECISION, and it is not the one the issue framed.
//
// #489 read the tree as INCONSISTENT with Feature 223's alias rule, having found two wrappers that break it.
// The tree is not inconsistent. It obeys a rule exactly — 21 of 21 — that nobody had written down:
//
//     supplied-by `template/product-skills/<id>/`  ->  wrapper is the `fs-gg-product-*` ALIAS   (17 of 17)
//     supplied-by anywhere else                    ->  wrapper is the CANONICAL id              ( 4 of 4)
//
// The issue found two of those four. `fs-gg-project` (supplied from template/base/.agents/skills/) and
// `fs-gg-samples` (from template/fragments/samples/skill/) are canonical-id product wrappers too. So the
// choice was never "rename two oddities" vs "accept an exception": the convention was already universal, and
// what was missing was the RULE — which is worse than an inconsistency, because nothing can disagree with a
// rule nobody wrote.
//
// Renaming (the issue's option 1) is rejected: it changes the name a user invokes, and `fs-gg-project` and
// the feedback pair are invoked in THIS repo too, where a `product-` prefix would be a plain lie about scope.
//
// Why the split is principled rather than historical: `template/product-skills/` is the CAPABILITY family,
// and its ids collide with the framework skills in `src/<X>/skill/` that teach the library side of the same
// capability. `.claude/skills/fs-gg-scene` is the FRAMEWORK wrapper (it routes to `src/Scene/skill/`);
// `.claude/skills/fs-gg-product-scene` is the product one. The alias is what keeps them apart. The four
// off-convention skills have no framework counterpart and cannot acquire one, so there is nothing to
// disambiguate.

open System.IO
open Expecto
open Rendering.Harness
open FS.GG.TestSupport

let private writeManifest (root: string) (rows: (string * string) list) =
    let dir = Path.Combine(root, "template", "skill-manifest")
    Directory.CreateDirectory dir |> ignore

    let entries =
        rows
        |> List.map (fun (id, suppliedBy) ->
            $"""{{"id": "{id}", "scope": "product", "supplied-by": "{suppliedBy}", "materializes-when": "true"}}""")
        |> String.concat ",\n    "

    File.WriteAllText(Path.Combine(dir, "skill-manifest.json"), $"{{\n  \"skills\": [\n    {entries}\n  ]\n}}\n")

/// An activation wrapper on BOTH orchestrator roots — a skill wrapped for one agent and not the other is
/// half-shipped, which is what the coverage check exists to catch.
let private writeWrapper (root: string) (name: string) =
    for surface in [ ".claude"; ".agents" ] do
        let dir = Path.Combine(root, surface, "skills", name)
        Directory.CreateDirectory dir |> ignore

        File.WriteAllText(
            Path.Combine(dir, "SKILL.md"),
            $"---\nname: \"{name}\"\ndescription: \"Wrapper for {name}.\"\n---\n\n# {name}\n\nBefore acting, read `../../../template/product-skills/x/SKILL.md`.\n"
        )

let private missingWrapperFindings (root: string) =
    SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)
    |> fun report -> report.Findings
    |> List.filter (fun finding -> finding.Category = SkillParity.MissingWrapper)

[<Tests>]
let tests =
    testList
        "Feature489 product-wrapper naming"
        [
          // THE FAIL-OPEN THIS CLOSES. The old check accepted `alias || canonical id` for ANY skill — so for a
          // capability skill the bare canonical id satisfied it, and that name belongs to the FRAMEWORK
          // wrapper. Delete both product wrappers for `fs-gg-scene` and the product skill has NO activation
          // wrapper, while the framework wrapper keeps coverage green. Verified against the live repo before
          // the fix: `skill-parity status: warning`, high=0 — the gate did not fail.
          test "a capability skill's canonical-id wrapper does NOT satisfy it — that name is the framework's" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature489-masking"

              try
                  writeManifest root [ "fs-gg-scene", "template/product-skills/fs-gg-scene/" ]
                  writeWrapper root "fs-gg-scene" // the FRAMEWORK wrapper's name — must not mask the product one

                  let findings = missingWrapperFindings root

                  Expect.isNonEmpty findings "the product wrapper `fs-gg-product-scene` is absent; a bare `fs-gg-scene` wrapper is the FRAMEWORK skill's and must not satisfy the product requirement (Feature 223)"
                  Expect.all findings (fun f -> f.Severity = SkillParity.High) "High — FailOnSeverity is High, so a Warning here is a gate that does not fail"
                  Expect.stringContains findings.Head.Message "fs-gg-product-scene" "the finding names the wrapper that is actually required"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "a capability skill is satisfied by its fs-gg-product-* alias" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature489-alias"

              try
                  writeManifest root [ "fs-gg-scene", "template/product-skills/fs-gg-scene/" ]
                  writeWrapper root "fs-gg-product-scene"

                  Expect.isEmpty (missingWrapperFindings root) "the alias is the required name for a body supplied from template/product-skills/"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // The other direction. The rule is not "alias always" — an off-convention skill takes the canonical
          // id, and the alias must not satisfy it either, or the rule is a suggestion.
          test "an off-convention skill takes the canonical id, and the alias does not satisfy it" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature489-offconvention"

              try
                  writeManifest root [ "fs-gg-feedback-report", "template/feedback-report/skill/" ]
                  writeWrapper root "fs-gg-product-feedback-report" // the WRONG name for this skill

                  let findings = missingWrapperFindings root

                  Expect.isNonEmpty findings "a body supplied off the template/product-skills/ convention is wrapped under its canonical id"
                  Expect.all findings (fun f -> f.Severity = SkillParity.High) "High"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          test "an off-convention skill is satisfied by its canonical id" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature489-canonical"

              try
                  writeManifest root [ "fs-gg-feedback-report", "template/feedback-report/skill/" ]
                  writeWrapper root "fs-gg-feedback-report"

                  Expect.isEmpty (missingWrapperFindings root) "the canonical id is the required name for a body supplied off the convention — this is what the feedback pair, fs-gg-project and fs-gg-samples all do"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // GUARDS THE FIX ITSELF. Two producers deliberately share a FindingId so a missing wrapper is
          // reported once. `List.distinctBy` kept the FIRST — the scan-driven Warning — and silently
          // downgraded the manifest-driven High, which is what made the fail-open above merely a warning.
          // Dedupe now resolves a collision by severity, so the milder finding can never hide the graver one.
          test "when two rules collide on one finding id, the WORSE severity survives" {
              let root = Feature168SkillParityFixtures.createTempRoot "feature489-dedupe"

              try
                  writeManifest root [ "fs-gg-scene", "template/product-skills/fs-gg-scene/" ]
                  writeWrapper root "fs-gg-scene"

                  let report = SkillParity.runCheck (Feature168SkillParityFixtures.repositoryRequest root)

                  Expect.isGreaterThan
                      report.FindingCountsBySeverity.High
                      0
                      "the manifest-driven High must survive the dedupe — if a same-id Warning wins, FailOnSeverity=High lets a product skill ship with no activation wrapper at all"
              finally
                  Feature168SkillParityFixtures.deleteTempRoot root
          }

          // The live tree, and the non-vacuity guard: BOTH branches of the rule must actually be exercised by
          // real skills, or one of them is being asserted against nothing.
          test "this repository's product wrappers all obey the rule, and both branches are populated" {
              let root = RepositoryRoot.value

              let manifest =
                  File.ReadAllText(Path.Combine(root, "template", "skill-manifest", "skill-manifest.json"))

              use doc = System.Text.Json.JsonDocument.Parse manifest

              let text (element: System.Text.Json.JsonElement) (name: string) =
                  match element.TryGetProperty name with
                  | true, value -> value.GetString() |> Option.ofObj |> Option.defaultValue ""
                  | _ -> ""

              let productRows =
                  doc.RootElement.GetProperty("skills").EnumerateArray()
                  |> Seq.filter (fun s -> text s "scope" = "product")
                  |> Seq.map (fun s -> text s "id", text s "supplied-by")
                  |> Seq.toList

              let onConvention, offConvention =
                  productRows
                  |> List.partition (fun (_, suppliedBy) -> suppliedBy.StartsWith "template/product-skills/")

              Expect.isGreaterThan (List.length onConvention) 0 "some product skills are supplied from the convention (else the alias branch guards nothing)"
              Expect.isGreaterThan (List.length offConvention) 0 "some product skills are supplied OFF the convention (else the canonical-id branch guards nothing — and it is the branch #489 was about)"

              for id, suppliedBy in productRows do
                  let required =
                      if suppliedBy.StartsWith "template/product-skills/" then
                          id.Replace("fs-gg-", "fs-gg-product-")
                      else
                          id

                  for surface in [ ".claude"; ".agents" ] do
                      Expect.isTrue
                          (File.Exists(Path.Combine(root, surface, "skills", required, "SKILL.md")))
                          $"`{id}` is supplied from `{suppliedBy}`, so its wrapper is `{surface}/skills/{required}/SKILL.md` (#489)"
          } ]
