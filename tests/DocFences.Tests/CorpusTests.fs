module FS.GG.DocFences.CorpusTests

open Expecto
open FS.GG.DocFences
open FS.GG.DocFences.Corpus

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

          test "the map covers both fence-bearing corpora and excludes the generated mirror" {
              let kinds = Corpus.all () |> List.map fst
              Expect.contains kinds ProductSkill "product skills must be a corpus"
              Expect.contains kinds ScaffoldSource "scaffold sources must be a corpus"
              Expect.equal (List.length kinds) 2 "exactly the two fence-bearing corpora — the generated mirror is not one"
          } ]
