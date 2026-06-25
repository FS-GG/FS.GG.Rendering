module SymbologyBoard.Tests.BoardTests

// Feature 193 (M6) semantic tests over the sample's deterministic core (board-core.md), exercised through
// its public modules. Fail-before/pass-after the implementation (Constitution I/V):
//   - reproducible      same seed + script ⇒ byte-identical fingerprint     (SC-001/FR-005)
//   - seed-sensitive    different seed       ⇒ different fingerprint          (SC-002/FR-006)
//   - on-board          after N steps every unit centre stays on-board       (FR-011/SC-003)
//   - non-empty board   a single / zero-area unit still renders non-blank    (edge case, spec)

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Canvas
open FS.GG.UI.Symbology
open SymbologyBoard.Board

/// The canonical 120-Tick evidence script (mirrors Program.fs).
let private script: Msg list = [ for _ in 1..120 -> Tick dt ]

/// Canonical bytes of a model's rendered scene — the determinism identity the fingerprint hashes.
let private bytesOf (model: Model) : byte[] =
    (SceneCodec.export (renderScene model)).CanonicalBytes

/// Build a one-unit world directly (the Board's own World/BoardUnit are public values) so the edge cases
/// can exercise a degenerate roster the fixed `init` never produces.
let private singleUnitModel (token: Token) : Model =
    let unit =
        { Token = token
          Motion = Idle
          X = BoardWidth / 2.0
          Y = BoardHeight / 2.0
          Vx = 0.0
          Vy = 0.0 }

    { Step = Loop.init { Units = [ unit ]; T = 0.0 }; Seed = 0 }

[<Tests>]
let tests =
    testList
        "SymbologyBoard.Board"
        [ test "reproducible — same seed + script ⇒ byte-identical fingerprint (SC-001/FR-005)" {
              let a = evidence 1 script
              let b = evidence 1 script
              Expect.equal a b "two same-seed evidence runs must be byte-identical"
          }

          test "seed-sensitive — different seed ⇒ different fingerprint (SC-002/FR-006)" {
              let s1 = evidence 1 script
              let s2 = evidence 2 script
              Expect.notEqual s1 s2 "different seeds must materially change the board fingerprint"
          }

          test "on-board invariant — every unit centre stays within [radius, extent-radius] (FR-011/SC-003)" {
              // Advance the fixed roster through many whole steps; the bounce + clamp must keep every symbol
              // fully on-board on both axes for the entire run.
              let stepped =
                  List.fold (fun m _ -> update (Tick dt) m) (init 7) [ 1..600 ]

              let eps = 1e-9

              for u in stepped.Step.Current.Units do
                  let r = u.Token.R
                  Expect.isGreaterThanOrEqual u.X (r - eps) (sprintf "unit X %f below left edge (r=%f)" u.X r)
                  Expect.isLessThanOrEqual u.X (BoardWidth - r + eps) (sprintf "unit X %f past right edge" u.X)
                  Expect.isGreaterThanOrEqual u.Y (r - eps) (sprintf "unit Y %f below top edge (r=%f)" u.Y r)
                  Expect.isLessThanOrEqual u.Y (BoardHeight - r + eps) (sprintf "unit Y %f past bottom edge" u.Y)
          }

          test "non-empty board — a single-unit roster yields a non-blank scene (edge case)" {
              let model = singleUnitModel Symbology.defaultToken
              let bytes = bytesOf model
              Expect.isGreaterThan bytes.Length 0 "a single-unit board must produce non-empty canonical bytes"
              Expect.isNonEmpty (Scene.describe (renderScene model)) "a single-unit board must describe ≥1 element"
          }

          test "non-empty board — a zero-area symbol still renders via the grammar placeholder (edge case)" {
              // A unit whose channels collapse to zero radius must still produce a visible placeholder, never
              // a blank board passed off as success (spec Edge Cases, plan.md:46).
              let model = singleUnitModel { Symbology.defaultToken with R = 0.0 }
              let bytes = bytesOf model
              Expect.isGreaterThan bytes.Length 0 "a zero-area symbol must still produce non-empty canonical bytes"
              Expect.isNonEmpty (Scene.describe (renderScene model)) "the zero-area placeholder must describe ≥1 element"
          } ]
