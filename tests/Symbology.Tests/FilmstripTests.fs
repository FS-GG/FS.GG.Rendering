module Symbology.Tests.FilmstripTests

// T023 [US2] Filmstrip reproducibility (SC-006): `filmstrip samples entries` rendered twice =>
// byte-identical frames; phase comes from the schedule alone, no wall-clock read.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private entries =
    [ Pulse, { Symbology.defaultToken with R = 22.0; Faction = Ally; Klass = Mobile; Sigil = Bolt }
      Moving, { Symbology.defaultToken with R = 22.0; Faction = Enemy; Klass = Scout; Sigil = Fang; Speed = 3 } ]

let private bytesOf scene = (SceneCodec.export scene).CanonicalBytes

[<Tests>]
let tests =
    testList
        "US2 filmstrip reproducibility"
        [ test "filmstrip is byte-reproducible across builds" {
              let a = Symbology.filmstrip 5 entries
              let b = Symbology.filmstrip 5 entries
              Expect.equal (bytesOf a) (bytesOf b) "frames reproducible from the schedule alone (SC-006)"
          }

          test "sample count drives the number of frames (more samples => richer board)" {
              Expect.notEqual (bytesOf (Symbology.filmstrip 3 entries)) (bytesOf (Symbology.filmstrip 7 entries)) "schedule length matters"
          } ]

// T019 [US3] Grammar-parameterized filmstrip `filmstripIn` (FR-014/FR-010): byte-reproducible per grammar;
// Grammar.Token reproduces `filmstrip` byte-for-byte; a different grammar yields a different filmstrip.
[<Tests>]
let filmstripInTests =
    let grammars = [ Grammar.Token; Grammar.Badge; Grammar.Ring ]

    testList
        "US3 filmstripIn"
        [ yield!
              grammars
              |> List.map (fun g ->
                  test (sprintf "filmstripIn %A is byte-reproducible" g) {
                      Expect.equal (bytesOf (Symbology.filmstripIn g 5 entries)) (bytesOf (Symbology.filmstripIn g 5 entries)) "frames reproducible per grammar"
                  })

          test "filmstripIn Grammar.Token reproduces `filmstrip` byte-for-byte (FR-010)" {
              Expect.equal (bytesOf (Symbology.filmstripIn Grammar.Token 5 entries)) (bytesOf (Symbology.filmstrip 5 entries)) "Token path is the existing filmstrip"
          }

          test "a different grammar yields a different filmstrip" {
              Expect.notEqual (bytesOf (Symbology.filmstripIn Grammar.Token 5 entries)) (bytesOf (Symbology.filmstripIn Grammar.Ring 5 entries)) "grammar selection changes the strip"
          } ]
