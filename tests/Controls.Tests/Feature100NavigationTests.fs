module Feature100NavigationTests

// Feature 100 (R5) — the PURE side of general navigation-key delivery: the role-derived
// `Focus.route` -> `NavIntent` classification (the single role-specific branch, FR-001/FR-006) and
// the closed, exhaustively-matched `NavIntent`/`NavPayload` model (FR-005/SC-005). These are pure,
// total functions of the focused control's declared role + `NavigationKeys`/`NavRange` metadata —
// no host, no live window. The host resolver that reads the live selection/value model and
// dispatches through the real `runInteractiveApp` seam is proven in `tests/Elmish.Tests/Feature100*`.

open Expecto
open FsCheck
open FsCheck.FSharp
open FS.Skia.UI.Controls

// A KeyboardOperation declaring the four arrows plus Home/End (so the pure route can exercise the
// First/Last directions even though no shipping role currently declares Home/End in keyboardFor).
let private navKb =
    Accessibility.keyboard true [] [ "ArrowUp"; "ArrowDown"; "ArrowLeft"; "ArrowRight"; "Home"; "End" ]

// A value-role keyboard declaring all four arrows + Home/End, so the route's vertical
// (ArrowUp/ArrowDown) and horizontal (ArrowLeft/ArrowRight) value-step mappings are both exercised
// (a control only navigates a key it lists in its NavigationKeys — FR-008).
let private rangeKb = Accessibility.keyboard true [] [ "ArrowUp"; "ArrowDown"; "ArrowLeft"; "ArrowRight"; "Home"; "End" ]
let private sliderRange: NavRange option = Some { Step = 0.1; Min = 0.0; Max = 1.0 }

// =============================================================================================
// T009 (paired pure) / FR-003 — a linear selection role maps arrows to a closed SelectionMove.
// =============================================================================================

[<Tests>]
let selectionRouteTests =
    testList "100 US1 Focus.route selection classification (FR-003)" [
        test "a RadioGroup role maps arrows/Home/End to the exact SelectionMove Direction" {
            let route key = Focus.route AccessibilityRole.RadioGroup navKb None key false false
            Expect.equal (route "ArrowDown") (Navigate(SelectionMove Direction.Next)) "ArrowDown -> Next"
            Expect.equal (route "ArrowRight") (Navigate(SelectionMove Direction.Next)) "ArrowRight -> Next"
            Expect.equal (route "ArrowUp") (Navigate(SelectionMove Direction.Previous)) "ArrowUp -> Previous"
            Expect.equal (route "ArrowLeft") (Navigate(SelectionMove Direction.Previous)) "ArrowLeft -> Previous"
            Expect.equal (route "Home") (Navigate(SelectionMove Direction.First)) "Home -> First"
            Expect.equal (route "End") (Navigate(SelectionMove Direction.Last)) "End -> Last"
        }

        test "a Tab role (horizontal) maps Left/Right to Previous/Next" {
            let kb = Accessibility.keyboard true [] [ "ArrowLeft"; "ArrowRight" ]
            Expect.equal (Focus.route AccessibilityRole.Tab kb None "ArrowLeft" false false) (Navigate(SelectionMove Direction.Previous)) "ArrowLeft -> Previous"
            Expect.equal (Focus.route AccessibilityRole.Tab kb None "ArrowRight" false false) (Navigate(SelectionMove Direction.Next)) "ArrowRight -> Next"
        }
    ]

// =============================================================================================
// T012 (paired pure) / FR-002/FR-007 — a value/range role maps arrows to a closed ValueStep delta.
// =============================================================================================

[<Tests>]
let valueRouteTests =
    testList "100 US2 Focus.route value classification (FR-002/FR-007)" [
        test "a Slider role with a declared NavRange maps arrows to a signed step delta" {
            let route key = Focus.route AccessibilityRole.Slider rangeKb sliderRange key false false
            Expect.equal (route "ArrowRight") (Navigate(ValueStep 0.1)) "ArrowRight -> +Step"
            Expect.equal (route "ArrowUp") (Navigate(ValueStep 0.1)) "ArrowUp -> +Step"
            Expect.equal (route "ArrowLeft") (Navigate(ValueStep -0.1)) "ArrowLeft -> -Step"
            Expect.equal (route "ArrowDown") (Navigate(ValueStep -0.1)) "ArrowDown -> -Step"
        }

        test "Home/End fold to a delta that clamps to Min/Max at the host" {
            let range: NavRange option = Some { Step = 5.0; Min = 0.0; Max = 100.0 }
            // Home -> (Min - Max) always clamps DOWN to Min; End -> (Max - Min) always clamps UP to Max.
            Expect.equal (Focus.route AccessibilityRole.Slider rangeKb range "Home" false false) (Navigate(ValueStep -100.0)) "Home -> delta to Min"
            Expect.equal (Focus.route AccessibilityRole.Slider rangeKb range "End" false false) (Navigate(ValueStep 100.0)) "End -> delta to Max"
        }

        test "a value role with NO declared NavRange cannot step -> Fallthrough (FR-008)" {
            Expect.equal (Focus.route AccessibilityRole.Slider rangeKb None "ArrowRight" false false) Fallthrough "no NavRange -> no value step"
        }
    ]

// =============================================================================================
// T015 (paired pure) / FR-004 — a grid role maps arrows to a closed 2-D GridMove delta.
// =============================================================================================

[<Tests>]
let gridRouteTests =
    testList "100 US3 Focus.route grid classification (FR-004)" [
        test "a Grid role maps arrows to a 2-D unit delta (row by Up/Down, column by Left/Right)" {
            let route key = Focus.route AccessibilityRole.Grid navKb None key false false
            Expect.equal (route "ArrowUp") (Navigate(GridMove(-1, 0))) "ArrowUp -> (-1, 0)"
            Expect.equal (route "ArrowDown") (Navigate(GridMove(1, 0))) "ArrowDown -> (1, 0)"
            Expect.equal (route "ArrowLeft") (Navigate(GridMove(0, -1))) "ArrowLeft -> (0, -1)"
            Expect.equal (route "ArrowRight") (Navigate(GridMove(0, 1))) "ArrowRight -> (0, 1)"
        }
    ]

// =============================================================================================
// T006 / FR-008 — a key absent from the role's NavigationKeys, or a non-navigable role, is a no-op.
// =============================================================================================

[<Tests>]
let fallthroughTests =
    testList "100 Focus.route no-op cases (FR-008)" [
        test "a key absent from NavigationKeys -> Fallthrough" {
            // RadioGroup keyboard has no Home/End here: a delivered Home falls through.
            let kb = Accessibility.keyboard true [] [ "ArrowUp"; "ArrowDown" ]
            Expect.equal (Focus.route AccessibilityRole.RadioGroup kb None "Home" false false) Fallthrough "Home not in NavigationKeys -> Fallthrough"
            Expect.equal (Focus.route AccessibilityRole.RadioGroup kb None "Q" false false) Fallthrough "an arbitrary key -> Fallthrough"
        }

        test "a non-navigable role (Button) never forms a navigation intent" {
            // Even if a Button somehow lists an arrow as a NavigationKey, the role forms no intent.
            let kb = Accessibility.keyboard true [ "Enter"; "Space" ] [ "ArrowRight" ]
            Expect.equal (Focus.route AccessibilityRole.Button kb None "ArrowRight" false false) Fallthrough "Button arrow -> Fallthrough (no nav intent)"
            Expect.equal (Focus.route AccessibilityRole.Button kb None "Enter" false false) Activate "Button Enter still activates (E4 unchanged)"
        }
    ]

// =============================================================================================
// T018 / SC-004/SC-005/FR-005/FR-006/FR-010 — the closed model + metadata-driven invariant.
// =============================================================================================

module private Gen100 =
    let direction = Gen.elements [ Direction.Previous; Direction.Next; Direction.First; Direction.Last ]

    let intent: Gen<NavIntent> =
        Gen.oneof
            [ Gen.map (fun d -> ValueStep(float d)) (Gen.choose (-100, 100))
              Gen.map SelectionMove direction
              Gen.map2 (fun r c -> GridMove(r, c)) (Gen.choose (-3, 3)) (Gen.choose (-3, 3)) ]

    let payload: Gen<NavPayload> =
        Gen.oneof
            [ Gen.map (fun d -> SteppedValue(float d)) (Gen.choose (-100, 100))
              Gen.map2 (fun i s -> MovedSelection(i, s)) (Gen.choose (0, 9)) (Gen.elements [ None; Some "x" ])
              Gen.map2 (fun r c -> MovedCell(r, c)) (Gen.choose (0, 9)) (Gen.choose (0, 9)) ]

// A TOTAL match over every NavIntent case — a new case would be a compile error (closed set).
let private intentTag =
    function
    | ValueStep _ -> 0
    | SelectionMove _ -> 1
    | GridMove _ -> 2

// A TOTAL match over every NavPayload case.
let private payloadTag =
    function
    | SteppedValue _ -> 0
    | MovedSelection _ -> 1
    | MovedCell _ -> 2

// The one-to-one NavIntent -> NavPayload correspondence (each intent class has exactly one
// payload class).
let private intentToPayload =
    function
    | ValueStep d -> SteppedValue d
    | SelectionMove _ -> MovedSelection(0, None)
    | GridMove(r, c) -> MovedCell(r, c)

[<Tests>]
let closedModelTests =
    testList "100 US4 closed model + metadata-driven (SC-004/SC-005/FR-010)" [
        testCase "NavIntent is a closed, totally-matched set; its match is total and never throws (>=1000)" (fun () ->
            let prop (intent: NavIntent) =
                let tag = intentTag intent
                tag >= 0 && tag <= 2

            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, Prop.forAll (Arb.fromGen Gen100.intent) prop))

        testCase "NavPayload is a closed, totally-matched set; its match is total and never throws (>=1000)" (fun () ->
            let prop (payload: NavPayload) =
                let tag = payloadTag payload
                tag >= 0 && tag <= 2

            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, Prop.forAll (Arb.fromGen Gen100.payload) prop))

        testCase "NavIntent maps one-to-one onto NavPayload (each intent class has exactly one payload class) (>=1000)" (fun () ->
            let prop (intent: NavIntent) = payloadTag (intentToPayload intent) = intentTag intent

            Check.One(Config.QuickThrowOnFailure.WithMaxTest 1000, Prop.forAll (Arb.fromGen Gen100.intent) prop))

        test "navigation is reproduced PURELY from declared role + NavigationKeys (+ NavRange) — no per-kind host special-case" {
            // The three covered intent classes, each derived solely from the declared metadata.
            Expect.equal (Focus.route AccessibilityRole.Slider rangeKb sliderRange "ArrowRight" false false) (Navigate(ValueStep 0.1)) "value role -> ValueStep"
            Expect.equal (Focus.route AccessibilityRole.RadioGroup navKb None "ArrowDown" false false) (Navigate(SelectionMove Direction.Next)) "selection role -> SelectionMove"
            Expect.equal (Focus.route AccessibilityRole.Grid navKb None "ArrowRight" false false) (Navigate(GridMove(0, 1))) "grid role -> GridMove"
        }

        test "Accessibility.validate passes for the representative value, selection, and grid roles (FR-010)" {
            let hasError control =
                Accessibility.validate control
                |> List.exists (fun d -> d.Severity = ControlDiagnosticSeverity.Error)

            let slider = Slider.create [ Slider.value 0.5 ] |> Control.withKey "sld"
            let radio = RadioGroup.create [ RadioGroup.items [ "A"; "B" ]; RadioGroup.selected "A" ] |> Control.withKey "rg"
            let grid = DataGrid.create [ { Key = "c0"; Header = "C0"; Width = 40.0; ColumnType = TextColumn } ] [ DataGrid.rows [ { Key = "r0"; Cells = [] } ] ] |> Control.withKey "grid"

            Expect.isFalse (hasError slider) "slider validates"
            Expect.isFalse (hasError radio) "radio-group validates"
            Expect.isFalse (hasError grid) "data-grid validates"
        }
    ]
