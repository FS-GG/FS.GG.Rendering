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
