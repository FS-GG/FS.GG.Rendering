module Symbology.Tests.PlaceholderTests

// T013 [US1] Zero-area placeholder (FR-020): a Token with R <= 0 (or otherwise no drawable area)
// renders a visible placeholder, not a blank/crash.

open Expecto
open FS.GG.UI.Scene
open FS.GG.UI.Symbology

let private placeholderKinds r =
    Symbology.token { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = r }
    |> Scene.describe
    |> List.distinct

[<Tests>]
let tests =
    testList
        "US1 zero-area placeholder"
        [ test "R = 0 renders a visible (non-blank) placeholder" {
              let kinds = placeholderKinds 0.0
              Expect.isNonEmpty kinds "placeholder draws something"
              Expect.contains kinds PathElement "placeholder has a visible marker outline"
          }

          test "negative R renders a visible placeholder, not a crash" {
              let kinds = placeholderKinds -8.0
              Expect.isNonEmpty kinds "negative radius still draws a placeholder"
          }

          test "placeholder differs from a real symbol" {
              let blankish = Symbology.token { Symbology.defaultToken with R = 0.0 }
              let real = Symbology.token { Symbology.defaultToken with R = 20.0 }
              Expect.notEqual blankish real "placeholder is a distinct degraded rendering"
          } ]
