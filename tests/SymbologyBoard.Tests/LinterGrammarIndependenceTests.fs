module SymbologyBoard.Tests.LinterGrammarIndependenceTests

// T020 [US3] Linter grammar-independence (SC-005/FR-009). The legibility linter scores the Token CHANNEL
// VALUES of a roster; it never sees which grammar draws them. This test renders one fixed roster in all
// three grammars — asserting the SCENES differ, so the test is meaningful — yet asserts `Legibility.score`
// returns the IDENTICAL Report (usage summary, findings, verdict) regardless of grammar. No linter change.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private roster: Token list =
    [ { Symbology.defaultToken with R = 24.0; Faction = Ally; Klass = Mobile; Sigil = Bolt; Health = 0.7; Speed = 2 }
      { Symbology.defaultToken with R = 24.0; Faction = Enemy; Klass = Heavy; Sigil = Fang; Threat = 0.9; State = Suspected }
      { Symbology.defaultToken with R = 24.0; Faction = Neutral; Klass = Scout; Sigil = Ring; Charge = 0.8; Shield = true } ]

let private bytesIn (g: Grammar) =
    roster
    |> List.map (Symbology.render g)
    |> Scene.group
    |> SceneCodec.export
    |> fun p -> p.CanonicalBytes

[<Tests>]
let tests =
    testList
        "SymbologyBoard.LinterGrammarIndependence"
        [ test "the three grammars draw the roster as observably different scenes (test is meaningful)" {
              let tok = bytesIn Grammar.Token
              let bdg = bytesIn Grammar.Badge
              let rng = bytesIn Grammar.Ring
              Expect.notEqual tok bdg "Token and Badge boards differ"
              Expect.notEqual tok rng "Token and Ring boards differ"
              Expect.notEqual bdg rng "Badge and Ring boards differ"
          }

          test "Legibility.score returns the IDENTICAL Report across all three grammars (FR-009/SC-005)" {
              // The grammar is not an input to scoring — the report is a function of the Token roster alone.
              let report = Legibility.score roster
              Expect.equal (Legibility.score roster) report "scoring is deterministic for the roster"

              // Drawing the roster in any grammar leaves the scored channel values untouched, so the report
              // a reviewer reads is identical no matter which form factor is on the board.
              for g in [ Grammar.Token; Grammar.Badge; Grammar.Ring ] do
                  ignore (bytesIn g) // the grammar renders a (different) scene...
                  Expect.equal (Legibility.score roster) report "...yet the legibility Report is grammar-independent"
          } ]

// T026 [US3] (OPTIONAL) The grammar-compare sample board is byte-reproducible (quickstart §7).
[<Tests>]
let grammarCompareTests =
    let bytesOf (scene: Scene) = (SceneCodec.export scene).CanonicalBytes

    testList
        "SymbologyBoard.GrammarCompare"
        [ test "compareBoard is byte-reproducible across builds" {
              Expect.equal
                  (bytesOf (SymbologyBoard.GrammarCompare.compareBoard 90.0))
                  (bytesOf (SymbologyBoard.GrammarCompare.compareBoard 90.0))
                  "the three-grammar compare board is a pure, reproducible scene"
          }

          test "compareBoard draws a non-blank board" {
              Expect.isNonEmpty (Scene.describe (SymbologyBoard.GrammarCompare.compareBoard 90.0)) "the compare board renders symbols"
          } ]
