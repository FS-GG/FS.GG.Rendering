module Feature140ZOrderTests

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Controls

let private c id decl z =
    Composition.contribution id decl z "content" [ Scene.textAt { X = float decl; Y = 0.0 } id Colors.white ] None

[<Tests>]
let tests =
    testList
        "Feature140 local z-order"
        [ test "local z-order sorts only sibling contributions and preserves equal-z declaration order" {
              let ordered =
                  [ c "second" 1 0
                    c "top" 2 10
                    c "first" 0 0
                    c "bottom" 3 -2 ]
                  |> Composition.orderSiblings
                  |> List.map _.Id

              Expect.equal ordered [ "bottom"; "first"; "second"; "top" ] "z-order sorts by local z, then declaration index"
          }

          test "hit order is derived as reverse paint order from the same contribution stream" {
              let contributions = [ c "a" 0 0; c "b" 1 4; c "c" 2 4 ]
              let paint = contributions |> Composition.paintOrder |> List.map _.Id
              let hit = contributions |> Composition.hitOrder |> List.map _.Id

              Expect.equal paint [ "a"; "b"; "c" ] "paint uses z/declaration order"
              Expect.equal hit [ "c"; "b"; "a" ] "hit is reverse paint order"
          } ]
