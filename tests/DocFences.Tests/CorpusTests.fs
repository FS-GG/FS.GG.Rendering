module FS.GG.DocFences.CorpusTests

open Expecto
open FS.GG.DocFences
open FS.GG.DocFences.Corpus

/// Executable companion to fs-gg-testing's numbered-scenario recipe. The index is
/// deliberately data, rather than inferred from test names, so omissions and duplicates
/// both make the guard red.
let private scenarioIndex total scenarios =
    let indexed = scenarios |> List.sort
    if indexed = [ 1 .. total ] then None else Some indexed

type private PauseModel =
    { Paused: bool
      EnemyTurns: int
      NewlyAddedFuse: int }

let private encodePauseModel model =
    $"{model.Paused}|{model.EnemyTurns}|{model.NewlyAddedFuse}"

/// Deliberately defective pause transition: it freezes the old field but advances a
/// field added later. The whole-state encoding must expose that regression.
let private brokenFixedTick model =
    if model.Paused then { model with NewlyAddedFuse = model.NewlyAddedFuse + 1 }
    else { model with EnemyTurns = model.EnemyTurns + 1; NewlyAddedFuse = model.NewlyAddedFuse + 1 }

/// Foundational tests for the corpus/fence map (spec 255, T004). These are deterministic — they read the
/// in-tree corpora and need no package restore. The build-against-the-pin harness (US1) is separate.
[<Tests>]
let tests =
    testList
        "DocFences.Corpus"
        [ test "the product-skill corpus yields F# fences (no silent zero)" {
              // FR-001, no-silent-drop: the skill corpus is KNOWN to carry F# fences; if the map ever reads
              // zero, the extractor has silently broken, which reads as "everything documented / nothing to
              // check" — the exact fail-open shape this feature exists to refuse.
              let fences = Corpus.productSkillFences ()

              Expect.isGreaterThan
                  (List.length fences)
                  0
                  "template/product-skills/**/*.md must yield at least one F# fence"
          }

          test "every extracted skill fence has a body and a locating line" {
              for f in Corpus.productSkillFences () do
                  Expect.isGreaterThan
                      (List.length f.Body)
                      0
                      (sprintf "fence at %s:%d has no body" f.Doc f.StartLine)

                  Expect.isGreaterThan f.StartLine 0 (sprintf "fence in %s has no line number" f.Doc)
          }

          test "scenario-index guard reddens for N-minus-one and duplicate coverage" {
              Expect.isNone (scenarioIndex 3 [ 1; 2; 3 ]) "complete 1..N coverage is accepted"
              Expect.isSome (scenarioIndex 3 [ 1; 2 ]) "N-minus-one coverage must fail even when its remaining tests pass"
              Expect.isSome (scenarioIndex 3 [ 1; 2; 2 ]) "a duplicate cannot stand in for the missing scenario"
          }

          test "whole-state pause encoding reddens when a newly added field advances" {
              let before = { Paused = true; EnemyTurns = 4; NewlyAddedFuse = 9 }
              let after = brokenFixedTick before
              Expect.notEqual (encodePauseModel after) (encodePauseModel before)
                  "the encoded pause invariant must catch an advancing field added after the test was written"
          }

          test "no shipped skill document ends inside an unclosed fence" {
              // An unclosed fence makes prose silently become code (or code become nothing) — a defect the
              // scan reports and a caller must surface, per MarkdownFences' own contract.
              let broken = Corpus.unclosedFenceDocs ()
              Expect.isEmpty broken (sprintf "documents end inside a fence: %A" broken)
          }

          test "the scaffold-source fence path runs and is well-formed (fences pending T014b)" {
              // FR-013 authoring has not landed, so this corpus is expected empty TODAY. The assertion is
              // that the path RUNS without error and returns well-formed blocks — so that authoring fences
              // into scaffold `///` comments lights it up with no further wiring. When T014b lands, tighten
              // this to `isGreaterThan 0` like the skill corpus above.
              let fences = Corpus.scaffoldSourceFences ()

              for f in fences do
                  Expect.equal f.Kind ScaffoldSource "scaffold fences must be tagged ScaffoldSource"
                  Expect.isGreaterThan (List.length f.Body) 0 (sprintf "empty scaffold fence at %s:%d" f.Doc f.StartLine)
          }

          test "docfences:skip and docfences:open directives are parsed off the fences they precede (T007)" {
              // Exercised against a FIXTURE, not a shipped skill: the product skills are frozen and
              // manifest-tracked, so authoring real skip directives into them is a deliberate act tied to
              // the (still open) full-corpus design decision — not something a unit test should require.
              let fixture =
                  String.concat
                      "\n"
                      [ "# Fixture"
                        ""
                        "A plain fence:"
                        ""
                        "```fsharp"
                        "let a = 1"
                        "```"
                        ""
                        "<!-- docfences:skip illustrative — undefined locals -->"
                        "```fsharp"
                        "doThing undefinedLocal"
                        "```"
                        ""
                        "<!-- docfences:open FS.GG.UI.Controls -->"
                        "```fsharp"
                        "Button.create ()"
                        "```" ]

              let fences = Corpus.parseMarkdown Corpus.ProductSkill "fixture/SKILL.md" fixture

              Expect.equal (List.length fences) 3 "three fences in the fixture"

              let plain = fences.[0]
              Expect.isNone plain.Skip "the first fence has no directive"
              Expect.isEmpty plain.ExtraOpens "the first fence adds no opens"

              let skipped = fences.[1]
              Expect.isSome skipped.Skip "the second fence carries a skip directive"
              Expect.stringContains
                  (skipped.Skip |> Option.defaultValue "")
                  "illustrative"
                  "the skip reason is captured"

              let opened = fences.[2]
              Expect.isNone opened.Skip "the third fence is not skipped"
              Expect.contains opened.ExtraOpens "FS.GG.UI.Controls" "the open directive adds the namespace"
          }

          test "the map covers both fence-bearing corpora and excludes the generated mirror" {
              let kinds = Corpus.all () |> List.map fst
              Expect.contains kinds ProductSkill "product skills must be a corpus"
              Expect.contains kinds ScaffoldSource "scaffold sources must be a corpus"
              Expect.equal (List.length kinds) 2 "exactly the two fence-bearing corpora — the generated mirror is not one"
          }

          test "every product-skill fence has a current explicit compilation disposition" {
              let plan = Harness.productSkillCompilationPlan ()

              let selfContained =
                  plan
                  |> List.choose (fun (fence, disposition) ->
                      match disposition with
                      | Harness.SelfContained -> Some fence
                      | Harness.Contextual _ -> None)

              let contextualReasons =
                  plan
                  |> List.choose (fun (_, disposition) ->
                      match disposition with
                      | Harness.SelfContained -> None
                      | Harness.Contextual reason -> Some reason)

              Expect.equal plan.Length 92 "the reviewed whole-corpus inventory remains exact"
              Expect.equal selfContained.Length 15 "the positive compiler corpus remains explicit"
              Expect.all selfContained (fun fence -> fence.Skip.IsNone) "positive compiler members cannot be skipped"
              Expect.equal contextualReasons.Length 77 "every remaining teaching fragment is accounted for"
              Expect.all contextualReasons (System.String.IsNullOrWhiteSpace >> not) "contextual reasons are never blank"
          } ]
