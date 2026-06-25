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

// T018 [US2] Degenerate-token-with-label (FR-007/SC-005): a `R <= 0` token carrying a `Some label` still
// degrades to the existing visible placeholder and never throws — the placeholder rule WINS over the label
// (no label is drawn on a placeholder). Asserted in every grammar.
let private degenLabelled render =
    render { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = 0.0; Label = Some (LabelText.Plain "OVERLONG-CALLSIGN") }

[<Tests>]
let degenerateWithLabel =
    testList
        "US2 degenerate token with label"
        [ for gname, render in [ "token", Symbology.token; "badge", Symbology.badge; "ring", Symbology.ring ] do
              test (sprintf "[%s] R <= 0 with a label => placeholder, no throw, no label glyph" gname) {
                  let scene = degenLabelled render
                  let kinds = scene |> Scene.describe |> List.distinct
                  Expect.contains kinds PathElement "the visible placeholder is drawn"
                  Expect.isFalse (List.contains GlyphRunElement kinds) "the placeholder rule wins: no label on a degenerate token"
              }

              test (sprintf "[%s] degenerate-with-label equals degenerate-without-label (placeholder wins)" gname) {
                  let withLabel = render { Symbology.defaultToken with R = 0.0; Label = Some (LabelText.Plain "X") }
                  let without = render { Symbology.defaultToken with R = 0.0; Label = None }
                  Expect.equal withLabel without "the label never alters a placeholder (FR-007)"
              }

              // T012 [US2] explicit MULTI-LINE variant: a `\n`-bearing / over-budget label on a degenerate
              // token still yields the placeholder, draws no label glyph, and equals the no-label placeholder.
              test (sprintf "[%s] R <= 0 with a MULTI-LINE label => placeholder, no throw, no label glyph" gname) {
                  let scene = render { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = 0.0; Label = Some (LabelText.Plain "ALPHA\nBRAVO\nCHARLIE\nDELTA") }
                  let kinds = scene |> Scene.describe |> List.distinct
                  Expect.contains kinds PathElement "the visible placeholder is drawn (FR-007)"
                  Expect.isFalse (List.contains GlyphRunElement kinds) "the placeholder rule wins over a multi-line label"

                  let without = render { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = 0.0; Label = None }
                  Expect.equal scene without "a multi-line label never alters a placeholder (FR-007)"
              }

              // Feature 198 (B9/FR-008): a degenerate token carrying a STYLED `Rich` label still yields the
              // placeholder, draws no label glyph, and equals the no-label placeholder — placeholder wins.
              test (sprintf "[%s] R <= 0 with a STYLED rich label => placeholder, no throw, no label glyph" gname) {
                  let styled =
                      render
                          { Symbology.defaultToken with
                              Cx = 10.0
                              Cy = 10.0
                              R = 0.0
                              Label = Some(LabelText.Rich [ { Symbology.run "BRAVO" with Weight = Some 700; Color = Some(Colors.rgb 24uy 144uy 255uy) } ]) }

                  let kinds = styled |> Scene.describe |> List.distinct
                  Expect.contains kinds PathElement "the visible placeholder is drawn (FR-008)"
                  Expect.isFalse (List.contains GlyphRunElement kinds) "the placeholder rule wins over a styled label (B9)"

                  let without = render { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = 0.0; Label = None }
                  Expect.equal styled without "a styled label never alters a placeholder (FR-008)"
              }

              // Feature 199 (T028/B14/FR-010): a degenerate token carrying a LAID-OUT / decorated label still
              // yields the placeholder, draws no label glyph, and equals the no-label placeholder — the
              // placeholder rule WINS over an aligned/decorated label.
              test (sprintf "[%s] R <= 0 with a LAID-OUT / decorated label => placeholder, no throw, no glyph" gname) {
                  let laidOut =
                      render
                          { Symbology.defaultToken with
                              Cx = 10.0
                              Cy = 10.0
                              R = 0.0
                              Label =
                                  Some(
                                      Symbology.laidLabel
                                          [ Symbology.align Justify [ { Symbology.run "ALPHA BRAVO" with Italic = Some true; Color = Some(Colors.rgb 24uy 144uy 255uy) } ]
                                            Symbology.align Trailing [ { Symbology.run "OLD" with Strike = Some true } ] ]
                                  ) }

                  let kinds = laidOut |> Scene.describe |> List.distinct
                  Expect.contains kinds PathElement "the visible placeholder is drawn (FR-010)"
                  Expect.isFalse (List.contains GlyphRunElement kinds) "the placeholder rule wins over a laid-out label (B14)"

                  let without = render { Symbology.defaultToken with Cx = 10.0; Cy = 10.0; R = 0.0; Label = None }
                  Expect.equal laidOut without "a laid-out / decorated label never alters a placeholder (FR-010)"
              } ]
