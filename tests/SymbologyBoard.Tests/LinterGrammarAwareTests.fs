module SymbologyBoard.Tests.LinterGrammarAwareTests

// [US3] Grammar-AWARE legibility scoring (#286). `Legibility.score` stays grammar-blind by contract
// (see LinterGrammarIndependenceTests, which passes unmodified). `scoreIn`/`scoreAnimatedIn` add the two
// findings that genuinely depend on WHICH grammar draws the roster:
//
//   1. Badge/Ring build their animation frames from the grammar-agnostic overlays alone, so `Motion.Spin`
//      and `Motion.Moving` contribute no node — the unit renders identically to an `Idle` one.
//   2. The identity label's line budget is per-grammar (Token 3, Badge 2, Ring 2); the surplus is dropped.
//
// Both are strictly additive: `scoreIn` never removes a finding `score` would emit.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

/// A clean roster: inside every capacity, no label, so it is `Clean` under `score` and under every grammar.
let private roster: Token list =
    [ { Symbology.defaultToken with R = 24.0; Faction = Ally; Klass = Mobile; Sigil = Bolt; Health = 0.7; Speed = 2 }
      { Symbology.defaultToken with R = 24.0; Faction = Enemy; Klass = Heavy; Sigil = Fang; Threat = 0.9; State = Suspected }
      { Symbology.defaultToken with R = 24.0; Faction = Neutral; Klass = Scout; Sigil = Ring; Charge = 0.8; Shield = true } ]

let private grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

let private labelled (lines: string list) (t: Token) =
    { t with Label = Some(LabelText.Plain(String.concat "\n" lines)) }

let private findingsOn channel (r: Legibility.Report) =
    r.Findings |> List.filter (fun f -> f.Channel = channel)

[<Tests>]
let scoreInTests =
    testList
        "SymbologyBoard.LinterGrammarAware.scoreIn"
        [ test "scoreIn Grammar.Token reproduces score exactly when no grammar-conditional channel is in use" {
              Expect.equal (Legibility.scoreIn Grammar.Token roster) (Legibility.score roster) "Token is the reference grammar"
          }

          test "scoreIn is deterministic" {
              for g in grammars do
                  Expect.equal (Legibility.scoreIn g roster) (Legibility.scoreIn g roster) "equal input, equal report"
          }

          test "scoreIn never removes a finding score would emit" {
              // Overload Faction past its capacity of 7 so `score` has something to say, and give unit 0 a
              // label that busts every grammar's budget so `scoreIn` adds to it.
              let overloaded =
                  [ for i in 0..8 ->
                        { Symbology.defaultToken with
                            R = 24.0
                            Faction = Custom(Colors.rgb (byte (i * 20)) 10uy 10uy) } ]
                  |> List.mapi (fun i t -> if i = 0 then labelled [ "A"; "B"; "C"; "D" ] t else t)

              let baseFindings = (Legibility.score overloaded).Findings

              for g in grammars do
                  let grammarFindings = (Legibility.scoreIn g overloaded).Findings

                  for f in baseFindings do
                      Expect.contains grammarFindings f $"scoreIn %A{g} keeps every score finding"
          }

          test "usage stays the 12 per-unit table channels — Label is not a table row" {
              for g in grammars do
                  let usage = (Legibility.scoreIn g roster).Usage
                  Expect.equal usage.Length 12 "one entry per table channel"
                  Expect.isFalse (usage |> List.exists (fun u -> u.Channel = Legibility.Label)) "Label has no usage row"
                  Expect.isFalse (usage |> List.exists (fun u -> u.Channel = Legibility.Motion)) "Motion has no usage row"
          }

          test "the warned-against budget is the budget the renderer actually draws" {
              // Not a tautology over a shared constant: this drives the REAL emitter, so if
              // `labelLineBudget` ever drifts from `wrapLabel`'s cap, the bytes stop matching and this reds.
              let wide = { roster.Head with R = 64.0 }

              for g in grammars do
                  let budget = Symbology.labelLineBudget g
                  let ls n = List.init n (fun i -> $"L%d{i}")
                  let drawn lines = (SceneCodec.export (Symbology.render g (labelled lines wide))).CanonicalBytes

                  // `wrapLabel` keeps the first `budget - 1` lines and appends an ellipsis to the last kept
                  // one. So a roster one line past the budget must draw EXACTLY like that truncation.
                  let truncated = ls (budget - 1) @ [ $"L%d{budget - 1}…" ]

                  Expect.equal (drawn (ls (budget + 1))) (drawn truncated) $"%A{g} truncates one line past its budget of {budget}"
                  Expect.notEqual (drawn (ls budget)) (drawn truncated) $"%A{g} draws exactly {budget} lines intact"

                  // ...and the linter's verdict lines up with what the renderer just did.
                  Expect.isEmpty (findingsOn Legibility.Label (Legibility.scoreIn g [ labelled (ls budget) roster.Head ])) "at budget: silent"
                  Expect.hasLength (findingsOn Legibility.Label (Legibility.scoreIn g [ labelled (ls (budget + 1)) roster.Head ])) 1 "over budget: warned"
          }

          test "a 3-line label fits Token but busts Badge and Ring" {
              let board = [ labelled [ "HMR-7"; "ARMOR 80"; "SPD 2" ] roster.Head ]

              Expect.isEmpty (findingsOn Legibility.Label (Legibility.scoreIn Grammar.Token board)) "3 lines is exactly Token's budget"

              for g in [ Grammar.Badge; Grammar.Ring ] do
                  let findings = findingsOn Legibility.Label (Legibility.scoreIn g board)
                  Expect.hasLength findings 1 $"%A{g} budgets 2 lines"
                  Expect.equal findings.Head.Severity Legibility.Warning "the surplus is dropped, not un-encodable"
                  Expect.equal findings.Head.Units [ 0 ] "the finding names the offending unit"
          }

          test "a 4-line label busts every grammar, Token included" {
              for g in grammars do
                  let board = [ labelled [ "A"; "B"; "C"; "D" ] roster.Head ]
                  Expect.hasLength (findingsOn Legibility.Label (Legibility.scoreIn g board)) 1 $"4 lines exceeds %A{g}"
          }

          test "blank and whitespace-only lines are dropped before counting, as wrapLabel drops them" {
              // Five raw segments, three drawable — inside Token's budget of 3, over Badge's 2.
              let board = [ labelled [ "HMR-7"; ""; "ARMOR 80"; "   "; "SPD 2" ] roster.Head ]
              Expect.isEmpty (findingsOn Legibility.Label (Legibility.scoreIn Grammar.Token board)) "blank segments do not consume budget"
              Expect.hasLength (findingsOn Legibility.Label (Legibility.scoreIn Grammar.Badge board)) 1 "three drawable lines still bust Badge"
          }

          test "an AutoLabel whose separator carries hard breaks is scored, not ignored" {
              // The separator is caller-supplied; this is why the linter reads the RESOLVED label rather
              // than reconstructing it from `Label` alone.
              let t =
                  { roster.Head with
                      Label = None
                      AutoLabel = Some(Symbology.autoLabelSep "\n" [ FactionCode; KlassCode; StateCode ]) }

              Expect.hasLength (findingsOn Legibility.Label (Legibility.scoreIn Grammar.Badge [ t ])) 1 "3 projected lines bust Badge's 2"
              Expect.isEmpty (findingsOn Legibility.Label (Legibility.scoreIn Grammar.Token [ t ])) "...and fit Token's 3"
          }

          test "a Laid label counts one line per paragraph even with no hard break" {
              let para text = Symbology.paragraph [ Symbology.run text ]

              let t = { roster.Head with Label = Some(Symbology.laidLabel [ para "ONE"; para "TWO"; para "THREE" ]) }

              Expect.hasLength (findingsOn Legibility.Label (Legibility.scoreIn Grammar.Ring [ t ])) 1 "three paragraphs bust Ring's 2"
          } ]

[<Tests>]
let scoreAnimatedInTests =
    let movingBoard = [ Moving, roster.Head ]

    testList
        "SymbologyBoard.LinterGrammarAware.scoreAnimatedIn"
        [ test "the bug: scoreAnimated calls a moving Badge unit Clean, and the scene proves it is Idle" {
              // Grammar-blind scoring sees `Motion.Moving` and says nothing is wrong...
              Expect.equal (Legibility.scoreAnimated movingBoard).Verdict Legibility.Clean "the grammar-blind linter is silent"

              // ...yet Badge draws the moving unit byte-identically to an idle one. The channel is gone.
              let bytesOf m =
                  (SceneCodec.export (Symbology.animateIn Grammar.Badge m roster.Head 0.25)).CanonicalBytes

              Expect.equal (bytesOf Moving) (bytesOf Idle) "Badge renders Moving exactly as Idle"
          }

          test "scoreAnimatedIn Badge/Ring reports the dropped rhythm as an Error naming the unit" {
              for g in [ Grammar.Badge; Grammar.Ring ] do
                  for motion in [ Spin; Moving ] do
                      let report = Legibility.scoreAnimatedIn g [ motion, roster.Head ]
                      let findings = findingsOn Legibility.Motion report

                      Expect.hasLength findings 1 $"%A{g} drops Motion.%A{motion}"
                      Expect.equal findings.Head.Severity Legibility.Error "a dropped channel is an Error, not a Warning"
                      Expect.equal findings.Head.Units [ 0 ] "the finding names the offending unit"
                      Expect.notEqual report.Verdict Legibility.Clean "the board no longer scores Clean"
          }

          test "Token draws Spin and Moving, so it reports nothing" {
              for motion in [ Spin; Moving ] do
                  Expect.isEmpty
                      (findingsOn Legibility.Motion (Legibility.scoreAnimatedIn Grammar.Token [ motion, roster.Head ]))
                      "whole-body rotation and travel are drawn under Token"
          }

          test "the rhythms Badge/Ring DO draw are not flagged" {
              // Pulse/Blink/Damage are the grammar-agnostic overlays `animateIn` composes for Badge/Ring.
              for g in [ Grammar.Badge; Grammar.Ring ] do
                  for motion in [ Idle; Pulse; Blink; Damage ] do
                      Expect.isEmpty
                          (findingsOn Legibility.Motion (Legibility.scoreAnimatedIn g [ motion, roster.Head ]))
                          $"%A{g} draws Motion.%A{motion}"
          }

          test "scoreAnimatedIn Grammar.Token reproduces scoreAnimated exactly on an unlabelled board" {
              let board = roster |> List.map (fun t -> Idle, t)
              Expect.equal (Legibility.scoreAnimatedIn Grammar.Token board) (Legibility.scoreAnimated board) "Token is the reference grammar"
          }

          test "the whole-board motion Warning still fires, and sorts ahead of the per-unit Errors" {
              // Two distinct active rhythms overloads the whole-board budget of 1; under Badge each is also
              // a dropped-rhythm Error. Deterministic order is table order, then unit index — and the
              // whole-board finding (Units = []) precedes both per-unit ones.
              let board = [ Spin, roster.[0]; Moving, roster.[1] ]
              let motionFindings = findingsOn Legibility.Motion (Legibility.scoreAnimatedIn Grammar.Badge board)

              Expect.hasLength motionFindings 3 "one board Warning + two per-unit Errors"
              Expect.equal motionFindings.Head.Severity Legibility.Warning "the board-level overload sorts first"
              Expect.equal motionFindings.Head.Units [] "...and names no unit"
              Expect.equal (motionFindings |> List.map (fun f -> f.Units)) [ []; [ 0 ]; [ 1 ] ] "then ascending unit index"
          }

          test "scoreAnimatedIn also carries the scoreIn label budget" {
              let board = [ Idle, labelled [ "A"; "B"; "C" ] roster.Head ]
              Expect.hasLength (findingsOn Legibility.Label (Legibility.scoreAnimatedIn Grammar.Ring board)) 1 "3 lines bust Ring's 2"
          } ]
