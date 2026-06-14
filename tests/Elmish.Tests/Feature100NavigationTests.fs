module Feature100NavigationTests

// Feature 100 (R5) — the HOST per-intent resolver, driven through the REAL `routeFocusedKey` seam
// (the same `RetainedRender` path `runInteractiveApp` wires), reached via InternalsVisibleTo with NO
// hand-seeded identity map (mirrors the landed Feature094 routing tests). A focused selection role
// moves selection on arrows and dispatches its `"selected"`/`"changed"` binding with the moved item;
// a focused slider steps by its DECLARED step (a default-step slider byte-identical to the pre-R5
// path); a focused grid moves selection in 2-D; every boundary/empty/unset case is a verified no-op
// with no spurious dispatch (FR-009). Render-only / deterministic — no live Vulkan window.

open System
open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Elmish
open FS.Skia.UI.KeyboardInput

type private Msg =
    | RadioChanged of string
    | SliderChanged of float
    | Captured of ControlEvent

let private theme = Theme.light
let private size: Size = { Width = 640; Height = 480 }

let private rinit (c: Control<'msg>) : RetainedRender<'msg> = (RetainedRender.init theme size c).Retained

let rec private findByKey (key: ControlId) (n: RetainedNode<'msg>) : RetainedNode<'msg> option =
    if n.Control.Key = Some key then Some n else n.Children |> List.tryPick (findByKey key)

let private idOfKey (key: ControlId) (r: RetainedRender<'msg>) : RetainedId option =
    findByKey key r.Root |> Option.map (fun n -> n.Identity)

let private order (r: RetainedRender<'msg>) : TabOrder = Focus.order r.Root.Control

let private route (r: RetainedRender<'msg>) (focused: RetainedId option) key =
    let _, _, msgs = ControlsElmish.routeFocusedKey r focused (order r) key false
    msgs

// --- views -----------------------------------------------------------------------------------

let private radioView (selected: string) (items: string list) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ RadioGroup.create [ RadioGroup.items items; RadioGroup.selected selected; RadioGroup.onChanged RadioChanged ]
                |> Control.withKey "rg" ] ]

// A selection control that captures the WHOLE ControlEvent (a `"selected"` binding), so the dual-set
// Payload + closed Nav can be asserted directly (research R-2).
let private radioCaptureView (selected: string) (items: string list) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ RadioGroup.create [ RadioGroup.items items; RadioGroup.selected selected; Attr.onWith "onSelected" Captured ]
                |> Control.withKey "rg" ] ]

let private sliderMeta (range: NavRange option) =
    Accessibility.metadata
        AccessibilityRole.Slider
        "slider"
        [ "normal" ]
        None
        (Accessibility.keyboard true [] [ "ArrowLeft"; "ArrowRight" ])
        None
        range

let private sliderView (value: float) (range: NavRange option) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Slider.create [ Slider.value value; Slider.onChanged SliderChanged; Attr.accessibility (sliderMeta range) ]
                |> Control.withKey "sld" ] ]

// A default-step slider with NO explicit metadata: it inherits `Accessibility.defaultFor "slider"`,
// whose NavRange is { Step = 0.1; Min = 0.0; Max = 1.0 } — the byte-identity reference (FR-007).
let private defaultSliderView (value: float) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ Slider.create [ Slider.value value; Attr.onWith "onChanged" Captured ] |> Control.withKey "sld" ] ]

let private gridCols =
    [ { Key = "c0"; Header = "C0"; Width = 40.0; ColumnType = TextColumn }
      { Key = "c1"; Header = "C1"; Width = 40.0; ColumnType = TextColumn } ]

let private gridRows = [ { Key = "r0"; Cells = [] }; { Key = "r1"; Cells = [] }; { Key = "r2"; Cells = [] } ]

let private gridView (focused: DataGridFocusedCell option) : Control<Msg> =
    Stack.create
        [ Stack.children
              [ DataGrid.create gridCols [ DataGrid.rows gridRows; DataGrid.focusedCell focused; Attr.onWith "onSelected" Captured ]
                |> Control.withKey "grid" ] ]

// =============================================================================================
// T009 / SC-001/FR-003/FR-009 — a focused selection role moves selection on arrows.
// =============================================================================================

[<Tests>]
let selectionTests =
    testList "100 US1 selection move at the host seam (SC-001)" [
        test "ArrowDown moves a focused radio-group's selection to the next item and dispatches its changed binding" {
            let r = radioView "B" [ "A"; "B"; "C" ] |> rinit
            let focused = idOfKey "rg" r
            Expect.equal (route r focused ViewerKey.ArrowDown) [ RadioChanged "C" ] "Down: B -> C via the changed fallback (radio binds onChanged)"
            Expect.equal (route r focused ViewerKey.ArrowUp) [ RadioChanged "A" ] "Up: B -> A"
        }

        test "the selection dispatch DUAL-SETS Payload (moved item) and the closed Nav (MovedSelection)" {
            let r = radioCaptureView "B" [ "A"; "B"; "C" ] |> rinit
            let focused = idOfKey "rg" r

            match route r focused ViewerKey.ArrowDown with
            | [ Captured ev ] ->
                Expect.equal ev.Payload (Some "C") "Payload carries the moved item id"
                Expect.equal ev.Nav (Some(MovedSelection(2, Some "C"))) "Nav carries the closed MovedSelection (newIndex, item)"
                Expect.equal ev.Origin ControlEventOrigin.Keyboard "navigation dispatches carry Origin = Keyboard"
            | other -> failtestf "expected one Captured selection event, got %A" other
        }

        test "boundary clamp is a verified no-op: last + Next and first + Previous dispatch NOTHING" {
            let rLast = radioView "C" [ "A"; "B"; "C" ] |> rinit
            Expect.isEmpty (route rLast (idOfKey "rg" rLast) ViewerKey.ArrowDown) "last item + Next -> no dispatch"

            let rFirst = radioView "A" [ "A"; "B"; "C" ] |> rinit
            Expect.isEmpty (route rFirst (idOfKey "rg" rFirst) ViewerKey.ArrowUp) "first item + Previous -> no dispatch"
        }

        test "an empty group and an unresolvable current index dispatch NOTHING (research R-7)" {
            let rEmpty = radioView "" [] |> rinit
            Expect.isEmpty (route rEmpty (idOfKey "rg" rEmpty) ViewerKey.ArrowDown) "empty items -> no dispatch"

            let rUnset = radioView "Z" [ "A"; "B" ] |> rinit
            Expect.isEmpty (route rUnset (idOfKey "rg" rUnset) ViewerKey.ArrowDown) "selected value not in items -> no dispatch"
        }
    ]

// =============================================================================================
// T012 / SC-002/FR-002/FR-007/FR-009 — a focused slider steps by its DECLARED step.
// =============================================================================================

[<Tests>]
let valueStepTests =
    testList "100 US2 declared-step value move at the host seam (SC-002)" [
        test "a non-default-step slider steps by EXACTLY its declared step within bounds" {
            let range = Some { Step = 5.0; Min = 0.0; Max = 100.0 }
            let r = sliderView 50.0 range |> rinit
            let focused = idOfKey "sld" r

            match route r focused ViewerKey.ArrowRight with
            | [ SliderChanged v ] -> Expect.floatClose Accuracy.high v 55.0 "ArrowRight steps by the declared step (50 -> 55), NOT the hardcoded 0.1"
            | other -> failtestf "expected one SliderChanged, got %A" other

            match route r focused ViewerKey.ArrowLeft with
            | [ SliderChanged v ] -> Expect.floatClose Accuracy.high v 45.0 "ArrowLeft steps down by the declared step (50 -> 45)"
            | other -> failtestf "expected one SliderChanged, got %A" other
        }

        test "min/max clamp is a verified no-op: at the bound + a step toward it dispatches NOTHING" {
            let range = Some { Step = 5.0; Min = 0.0; Max = 100.0 }
            let rMax = sliderView 100.0 range |> rinit
            Expect.isEmpty (route rMax (idOfKey "sld" rMax) ViewerKey.ArrowRight) "at Max + step up -> no dispatch"

            let rMin = sliderView 0.0 range |> rinit
            Expect.isEmpty (route rMin (idOfKey "sld" rMin) ViewerKey.ArrowLeft) "at Min + step down -> no dispatch"
        }

        test "a DEFAULT-step slider's dispatched value is byte-identical to the pre-R5 numeric path (FR-007 golden)" {
            let r = defaultSliderView 0.5 |> rinit
            let focused = idOfKey "sld" r

            // The pre-R5 path computed exactly Math.Clamp(current + 0.1, 0.0, 1.0) and dispatched its
            // InvariantCulture string. Recompute it with the SAME operations as the golden reference.
            let preR5Value = Math.Clamp(0.5 + 0.1, 0.0, 1.0)
            let preR5Payload = preR5Value.ToString(Globalization.CultureInfo.InvariantCulture)

            match route r focused ViewerKey.ArrowRight with
            | [ Captured ev ] ->
                Expect.equal ev.Payload (Some preR5Payload) "the dispatched Payload string equals the pre-R5 steppedValue path byte-for-byte"
                Expect.equal ev.Nav (Some(SteppedValue preR5Value)) "Nav carries the closed SteppedValue equal to the pre-R5 value"
            | other -> failtestf "expected one Captured changed event, got %A" other
        }
    ]

// =============================================================================================
// T015 / SC-003/FR-004/FR-009 — a focused grid moves selection in two dimensions.
// =============================================================================================

[<Tests>]
let gridTests =
    testList "100 US3 grid 2-D move at the host seam (SC-003)" [
        test "ArrowDown/ArrowRight move the focused cell by a 2-D delta and dispatch the resulting cell" {
            let r = gridView (Some { RowKey = "r1"; ColumnKey = "c0" }) |> rinit
            let focused = idOfKey "grid" r

            match route r focused ViewerKey.ArrowDown with
            | [ Captured ev ] ->
                Expect.equal ev.Nav (Some(MovedCell(2, 0))) "ArrowDown: (1,0) -> (2,0)"
                Expect.equal ev.Payload (Some "r2:c0") "Payload carries the resulting cell id"
            | other -> failtestf "expected one Captured cell event, got %A" other

            match route r focused ViewerKey.ArrowRight with
            | [ Captured ev ] -> Expect.equal ev.Nav (Some(MovedCell(1, 1))) "ArrowRight: (1,0) -> (1,1)"
            | other -> failtestf "expected one Captured cell event, got %A" other
        }

        test "an edge cell + an outward arrow is a verified no-op (edge clamp)" {
            let rBottom = gridView (Some { RowKey = "r2"; ColumnKey = "c0" }) |> rinit
            Expect.isEmpty (route rBottom (idOfKey "grid" rBottom) ViewerKey.ArrowDown) "bottom row + Down -> no dispatch"

            let rLeft = gridView (Some { RowKey = "r1"; ColumnKey = "c0" }) |> rinit
            Expect.isEmpty (route rLeft (idOfKey "grid" rLeft) ViewerKey.ArrowLeft) "left column + Left -> no dispatch"
        }
    ]

// =============================================================================================
// T018 / FR-008 — a non-navigable focused control is an arrow no-op (activation unaffected).
// =============================================================================================

[<Tests>]
let nonNavigableTests =
    testList "100 US4 non-navigable control no-op (FR-008)" [
        test "a focused button is an arrow no-op but still activates on Space/Enter" {
            let view = Stack.create [ Stack.children [ Button.create [ Button.text "Go"; Button.onClick (RadioChanged "clicked") ] |> Control.withKey "btn" ] ]
            let r = view |> rinit
            let focused = idOfKey "btn" r
            Expect.isEmpty (route r focused ViewerKey.ArrowRight) "arrow on a button -> no navigation dispatch"
            Expect.equal (route r focused ViewerKey.Enter) [ RadioChanged "clicked" ] "Enter still activates the button (E4 unaffected)"
        }
    ]
