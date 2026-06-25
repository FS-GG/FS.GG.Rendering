module Symbology.Tests.GrammarTests

// T015 [US2] Ring health-arc monotonicity (FR-007/SC-002) + render-dispatch identity (data-model §5, G2).
//
// Monotonicity is observed through the public surface: the Ring health gauge is a fixed-start arc sweep
// built from discrete lit segments whose count grows with Health, so the rendered leaf-element count of
// `ring { t with Health = h }` is monotone non-decreasing in h (the sweep extent is monotone in Health).
// Only Health is varied, so the count delta is attributable to the health gauge alone.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private baseT =
    { Symbology.defaultToken with
        Cx = 40.0
        Cy = 40.0
        R = 26.0
        Faction = Ally
        Klass = Mobile
        Sigil = Bolt
        State = Confirmed
        Threat = 0.5
        Charge = 0.5
        Speed = 2
        Heading = 0.4
        Shield = true }

let private bytesOf (scene: Scene) = (SceneCodec.export scene).CanonicalBytes
let private elementCount (scene: Scene) = Scene.describe scene |> List.length

[<Tests>]
let ringHealthMonotonicity =
    testList
        "US2 ring health monotonicity"
        [ test "ring health arc sweep is monotone non-decreasing across Health in [0,1] (FR-007)" {
              let counts =
                  [ 0.0 .. 0.05 .. 1.0 ]
                  |> List.map (fun h -> elementCount (Symbology.ring { baseT with Health = h }))

              let monotone =
                  counts
                  |> List.pairwise
                  |> List.forall (fun (a, b) -> b >= a)

              Expect.isTrue monotone (sprintf "ring health element counts must be non-decreasing in Health; got %A" counts)
          }

          test "ring health sweep strictly grows from empty to full (not a degenerate constant)" {
              let low = elementCount (Symbology.ring { baseT with Health = 0.0 })
              let high = elementCount (Symbology.ring { baseT with Health = 1.0 })
              Expect.isLessThan low high "full health must draw a longer sweep than empty health (the test is meaningful)"
          } ]

// Render-dispatch identity (G2): `render` selects the grammar; each path equals its direct function.
[<Tests>]
let renderDispatch =
    testList
        "render dispatch identity"
        [ test "render Grammar.Token t ≡ token t" {
              Expect.equal
                  (bytesOf (Symbology.render Grammar.Token baseT))
                  (bytesOf (Symbology.token baseT))
                  "Grammar.Token path reproduces `token` byte-for-byte (FR-010)"
          }

          test "render Grammar.Badge t ≡ badge t" {
              Expect.equal
                  (bytesOf (Symbology.render Grammar.Badge baseT))
                  (bytesOf (Symbology.badge baseT))
                  "Grammar.Badge path reproduces `badge` byte-for-byte"
          }

          test "render Grammar.Ring t ≡ ring t" {
              Expect.equal
                  (bytesOf (Symbology.render Grammar.Ring baseT))
                  (bytesOf (Symbology.ring baseT))
                  "Grammar.Ring path reproduces `ring` byte-for-byte"
          }

          test "the three grammars draw observably different scenes (dispatch is meaningful)" {
              let tok = bytesOf (Symbology.render Grammar.Token baseT)
              let bdg = bytesOf (Symbology.render Grammar.Badge baseT)
              let rng = bytesOf (Symbology.render Grammar.Ring baseT)
              Expect.notEqual tok bdg "Token and Badge differ"
              Expect.notEqual tok rng "Token and Ring differ"
              Expect.notEqual bdg rng "Badge and Ring differ"
          } ]
