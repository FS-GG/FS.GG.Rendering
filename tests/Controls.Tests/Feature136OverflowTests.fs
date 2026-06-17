module Feature136OverflowTests

// Feature 136 (US1/T010A, FR-002): the overflow affordance. After measure/draw reconciliation a label
// can still be authored wider than its fixed box; it must then show an explicit ellipsis (`…`) rather
// than have the clip rect silently drop characters. Exercises `ControlInternals.ellipsize` (reached via
// InternalsVisibleTo). Pre-fix there was no affordance — over-long labels were hard-clipped by the box.

open Expecto
open FS.GG.UI.Controls

[<Tests>]
let tests =
    testList
        "Feature136 text overflow affordance (US1/T010A)"
        [ test "a label wider than its box is ellipsized with an explicit … (no silent drop)" {
              let label = "A very long action label that cannot fit"
              // A box far too narrow for the label at size 14.
              let shown = ControlInternals.ellipsize None 14.0 60.0 label
              Expect.isTrue (shown.EndsWith "…") "overflowing label ends with an explicit ellipsis"
              Expect.notEqual shown label "the label was actually shortened, not left to clip"
              Expect.isTrue (shown.Length > 1) "some authored characters remain visible before the …"
              Expect.isTrue (label.StartsWith(shown.TrimEnd('…'))) "the visible prefix is the start of the label"
          }

          test "a label that already fits is returned unchanged" {
              let label = "OK"
              Expect.equal (ControlInternals.ellipsize None 14.0 400.0 label) label "fitting label is untouched"
          }

          test "a single-character label is never truncated away" {
              Expect.equal (ControlInternals.ellipsize None 14.0 1.0 "X") "X" "single char never dropped"
          } ]
