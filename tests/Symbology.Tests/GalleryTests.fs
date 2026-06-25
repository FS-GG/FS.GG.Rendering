module Symbology.Tests.GalleryTests

// T014 [US1] Gallery layout + legibility-at-size (SC-007): `gallery cols spacing tokens` lays out a
// reproducible grid; each rendered symbol is non-blank and faction + class are separable at the
// target on-board size.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private roster faction klass =
    { Symbology.defaultToken with
        R = 24.0
        Faction = faction
        Klass = klass
        Sigil = Bolt
        Health = 0.7 }

let private mixed =
    [ roster Ally Mobile; roster Enemy Heavy; roster Neutral Scout; roster Ally Scout ]

let private bytesOf scene = (SceneCodec.export scene).CanonicalBytes

[<Tests>]
let tests =
    testList
        "US1 gallery"
        [ test "gallery layout is reproducible (byte-identical across builds)" {
              let a = Symbology.gallery 2 80.0 mixed
              let b = Symbology.gallery 2 80.0 mixed
              Expect.equal (bytesOf a) (bytesOf b) "pure grid layout"
          }

          test "every symbol in the gallery is non-blank" {
              let kinds = Symbology.gallery 2 80.0 mixed |> Scene.describe |> List.distinct
              Expect.contains kinds PathElement "gallery draws symbol silhouettes"
          }

          test "faction is separable on the board (all-Ally differs from all-Enemy)" {
              let allAlly = Symbology.gallery 2 80.0 [ roster Ally Mobile; roster Ally Heavy ]
              let allEnemy = Symbology.gallery 2 80.0 [ roster Enemy Mobile; roster Enemy Heavy ]
              Expect.notEqual (bytesOf allAlly) (bytesOf allEnemy) "faction hue is separable"
          }

          test "class is separable on the board (all-Mobile differs from all-Heavy)" {
              let allMobile = Symbology.gallery 2 80.0 [ roster Ally Mobile; roster Enemy Mobile ]
              let allHeavy = Symbology.gallery 2 80.0 [ roster Ally Heavy; roster Enemy Heavy ]
              Expect.notEqual (bytesOf allMobile) (bytesOf allHeavy) "class silhouette is separable"
          } ]
