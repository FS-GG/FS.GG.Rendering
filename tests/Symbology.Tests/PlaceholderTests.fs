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

// T009 [US1] Badge degenerate-input placeholder (SC-004/FR-005): R <= 0 through `badge` yields a visible
// (non-empty) placeholder scene and never throws; a real Badge differs from the degraded placeholder.
let private badgeKinds r =
    Symbology.badge { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = r }
    |> Scene.describe
    |> List.distinct

[<Tests>]
let badgePlaceholder =
    testList
        "US1 badge zero-area placeholder"
        [ test "R = 0 through badge renders a visible (non-blank) placeholder" {
              let kinds = badgeKinds 0.0
              Expect.isNonEmpty kinds "badge placeholder draws something"
              Expect.contains kinds PathElement "badge placeholder has a visible marker outline"
          }

          test "negative R through badge renders a placeholder, not a crash" {
              Expect.isNonEmpty (badgeKinds -8.0) "negative radius still draws a placeholder"
          }

          test "a real badge differs from the degraded placeholder" {
              let blankish = Symbology.badge { Symbology.defaultToken with R = 0.0 }
              let real = Symbology.badge { Symbology.defaultToken with R = 20.0 }
              Expect.notEqual blankish real "a real badge is a distinct rendering from its placeholder"
          } ]

// T014 [US2] Ring degenerate-input placeholder (SC-004/FR-005): R <= 0 through `ring` yields a visible
// (non-empty) placeholder scene and never throws; a real Ring differs from the degraded placeholder.
let private ringKinds r =
    Symbology.ring { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = r }
    |> Scene.describe
    |> List.distinct

[<Tests>]
let ringPlaceholder =
    testList
        "US2 ring zero-area placeholder"
        [ test "R = 0 through ring renders a visible (non-blank) placeholder" {
              let kinds = ringKinds 0.0
              Expect.isNonEmpty kinds "ring placeholder draws something"
              Expect.contains kinds PathElement "ring placeholder has a visible marker outline"
          }

          test "negative R through ring renders a placeholder, not a crash" {
              Expect.isNonEmpty (ringKinds -8.0) "negative radius still draws a placeholder"
          }

          test "a real ring differs from the degraded placeholder" {
              let blankish = Symbology.ring { Symbology.defaultToken with R = 0.0 }
              let real = Symbology.ring { Symbology.defaultToken with R = 20.0 }
              Expect.notEqual blankish real "a real ring is a distinct rendering from its placeholder"
          } ]
