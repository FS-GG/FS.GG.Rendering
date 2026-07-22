module Symbology.Tests.CoverageTests

// Issue #989 — the visual-representation COVERAGE check: every gameplay element must map to a visible
// symbology `Token` OR carry an explicit hidden-by-mechanic opt-out. These tests exercise the public
// `Coverage` surface over a SAMPLE product element set, and double as the documented PRODUCT TEST
// PATTERN a game copies to gate its own roster (see `productCoverageGate` below).

open Expecto
open FS.GG.UI.Symbology

// ---------------------------------------------------------------------------------------------------
// A sample game's PRODUCT-DEFINED renderable-element set. The framework never owns this DU — a game
// declares its own doors/bombs/explosions/projectiles/enemies. `declaredElements` is the exhaustive
// list the coverage check is run against (the "must be representable" roster).
// ---------------------------------------------------------------------------------------------------
type GameElement =
    | Door
    | Bomb
    | Explosion
    | Projectile
    | Enemy
    | StealthAmbusher // deliberately invisible until it strikes — a legal hidden-by-mechanic element

/// The declared renderable-element set — the visual analog of the match arms that must be covered.
let private declaredElements =
    [ Door; Bomb; Explosion; Projectile; Enemy; StealthAmbusher ]

/// A distinct visible token per element (only the fields that must differ need overriding for a real
/// game; here any `Token` witnesses "has a visible representation").
let private tokenFor (element: GameElement) : Coverage.Representation =
    let sigil =
        match element with
        | Door -> Sigil.Ring
        | Bomb -> Sigil.Bolt
        | Explosion -> Sigil.Fang
        | Projectile -> Sigil.Bolt
        | Enemy -> Sigil.Fang
        | StealthAmbusher -> Sigil.Fang

    Coverage.Shown { Symbology.defaultToken with Sigil = sigil }

/// The COMPLETE, correct mapping: every visible element -> a Token, the one stealth element -> an
/// explicit reasoned opt-out. This is what a shipped game keeps green.
let private completeMapping (element: GameElement) : Coverage.Representation option =
    match element with
    | StealthAmbusher -> Some(Coverage.Hidden "stealth: invisible to the player until it attacks (fog-of-war mechanic)")
    | e -> Some(tokenFor e)

[<Tests>]
let coverageTests =
    testList
        "Issue989 Coverage — visual exhaustiveness"
        [

          // ---- THE PRODUCT TEST PATTERN (copy this into a game's own test suite) --------------------
          test "productCoverageGate — every declared element has a visual or a reasoned opt-out" {
              let report = Coverage.check declaredElements completeMapping

              // The whole gate is one assertion: a clean coverage verdict. A forgotten element (below)
              // reds this before ship, exactly as a missing match arm reds the compiler.
              Expect.equal report.Verdict Coverage.Covered "the roster must be fully covered"
              Expect.isEmpty report.Findings "no element may be silently unrepresented"
          }

          test "a forgotten element (no token, no opt-out) is a Missing finding — the silent omission" {
              // The game adds `Enemy` to the roster but forgets to map it: `resolve` returns `None`.
              let forgetful element =
                  match element with
                  | Enemy -> None
                  | e -> completeMapping e

              let report = Coverage.check declaredElements forgetful

              Expect.equal report.Verdict Coverage.HasGaps "a forgotten element must fail coverage"
              Expect.equal report.Findings.Length 1 "exactly the forgotten element is reported"
              let f = report.Findings.Head
              Expect.equal f.Element Enemy "the finding names the forgotten element"
              Expect.equal f.Gap Coverage.Missing "the gap is Missing (no token, no opt-out)"
              Expect.stringContains f.Message "no visible representation" "the message explains the defect"
          }

          test "an explicit Hidden opt-out with a reason passes and lands in the OptedOut ledger" {
              let report = Coverage.check declaredElements completeMapping

              Expect.equal report.Verdict Coverage.Covered "a reasoned opt-out is covered"

              Expect.equal
                  (report.OptedOut |> List.map fst)
                  [ StealthAmbusher ]
                  "only the deliberately-hidden element is on the ledger"

              let _, reason = report.OptedOut.Head
              Expect.stringContains reason "stealth" "the ledger carries the stated mechanic"
          }

          test "a blank-reason opt-out is Unreasoned — indistinguishable from forgetting, so rejected" {
              let blankOptOut element =
                  match element with
                  | StealthAmbusher -> Some(Coverage.Hidden "   ")
                  | e -> completeMapping e

              let report = Coverage.check declaredElements blankOptOut

              Expect.equal report.Verdict Coverage.HasGaps "a blank opt-out must not pass"
              Expect.equal report.Findings.Head.Gap Coverage.Unreasoned "the gap is Unreasoned"
              Expect.isEmpty report.OptedOut "a blank-reason opt-out is not a ledger row"
          }

          test "multiple gaps are reported in DECLARED-element order (deterministic)" {
              // Forget Door and Explosion; leave the rest correct.
              let mapping element =
                  match element with
                  | Door -> None
                  | Explosion -> None
                  | e -> completeMapping e

              let report = Coverage.check declaredElements mapping

              Expect.equal
                  (report.Findings |> List.map (fun f -> f.Element))
                  [ Door; Explosion ]
                  "findings follow the declared order, not resolution or hash order"

              // Determinism: re-checking an equal input yields an equal report.
              let again = Coverage.check declaredElements mapping
              Expect.equal report again "equal input => equal report"
          }

          test "checkMap ≡ check over Map.tryFind — a forgotten element is simply an absent key" {
              // The canonical pattern: the mapping is a lookup table; a new element with no row returns
              // `None` from `tryFind` and reds coverage.
              let table =
                  declaredElements
                  |> List.choose (fun e -> completeMapping e |> Option.map (fun r -> e, r))
                  |> Map.ofList
                  // drop Bomb's row to simulate a forgotten element
                  |> Map.remove Bomb

              let viaMap = Coverage.checkMap declaredElements table
              let viaCheck = Coverage.check declaredElements (fun e -> Map.tryFind e table)

              Expect.equal viaMap viaCheck "checkMap is check over Map.tryFind"
              Expect.equal viaMap.Verdict Coverage.HasGaps "the absent Bomb key reds coverage"
              Expect.equal viaMap.Findings.Head.Element Bomb "the absent key is the reported gap"
          }

          test "the empty roster is trivially Covered" {
              let report = Coverage.check ([]: GameElement list) completeMapping
              Expect.equal report.Verdict Coverage.Covered "no elements => nothing to omit"
              Expect.isEmpty report.Findings "no findings on an empty roster"
              Expect.isEmpty report.OptedOut "no ledger rows on an empty roster"
          } ]
