module Symbology.Tests.DeterminismTests

// T010 [US1] Determinism / stable-identity (SC-001): identical Token => equal Scene and byte-equal
// canonical bytes across runs; a gallery package identity is stable across runs.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private sample =
    { Symbology.defaultToken with
        R = 28.0
        Faction = Ally
        Klass = Heavy
        Sigil = Bolt
        State = Suspected
        Threat = 0.7
        Charge = 0.6
        Health = 0.8
        Speed = 3
        Heading = 0.5
        Shield = true }

[<Tests>]
let tests =
    testList
        "US1 determinism"
        [ test "same Token => equal Scene value" {
              Expect.equal (Symbology.token sample) (Symbology.token sample) "token is a pure function of Token"
          }

          test "same Token => byte-equal canonical bytes across runs" {
              let a = (SceneCodec.export (Symbology.token sample)).CanonicalBytes
              let b = (SceneCodec.export (Symbology.token sample)).CanonicalBytes
              Expect.equal a b "canonical bytes are the determinism identity (SC-001)"
          }

          test "gallery package identity is stable across runs" {
              let board () =
                  Symbology.gallery 3 80.0 [ sample; { sample with Faction = Enemy }; { sample with Klass = Scout } ]

              let id1 = (SceneCodec.export (board ())).PackageIdentity
              let id2 = (SceneCodec.export (board ())).PackageIdentity
              Expect.equal id1 id2 "stable gallery identity"
          } ]
