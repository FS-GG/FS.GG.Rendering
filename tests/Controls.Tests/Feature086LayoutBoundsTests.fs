module ControlsFeature086LayoutBoundsTests

// Feature 086 — US3 multi-axis layout (FR-007/008/009/010) and US4 per-ControlId
// bounds + hit-test (FR-011/012). Exercises the PUBLIC `Control.renderTree` result
// surface (`Bounds`) and `Control.hitTest`, not internal helpers (vertical-slice rule).

open Expecto
open FS.Skia.UI.Scene
open FS.Skia.UI.Controls

type private Msg = Clicked

let private theme = Theme.light
let private size = { Width = 640; Height = 480 }

let private overlaps (a: Rect) (b: Rect) =
    a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y

let private boundsOf kind (rendered: ControlRenderResult<Msg>) =
    rendered.Bounds |> List.filter (fun (id, _) -> id = kind) |> List.map snd

let private center (r: Rect) = r.X + r.Width / 2.0, r.Y + r.Height / 2.0

[<Tests>]
let feature086LayoutBoundsTests =
    testList "Feature 086 renderTree bounds + hit-test (US3/US4)" [

        // FR-007 — a horizontal-orientation Stack lays children along the row axis.
        test "horizontal Stack lays two children side-by-side (distinct x, shared y) (FR-007)" {
            let horizontal: Control<Msg> =
                Stack.create
                    [ Stack.orientation "horizontal"
                      Stack.children
                          [ TextBlock.create [ TextBlock.text "left" ]
                            TextBlock.create [ TextBlock.text "right" ] ] ]

            let rendered = Control.renderTree theme size horizontal
            // Feature 098 (FR-007): unkeyed siblings are keyed in `Bounds` by their structural path
            // ("0.0", "0.1"), not the shared `Kind` "text-block".
            let kids = [ "0.0"; "0.1" ] |> List.collect (fun id -> boundsOf id rendered)
            Expect.equal kids.Length 2 "both unkeyed text-block children are laid out (keyed by structural path)"

            match kids with
            | [ a; b ] ->
                Expect.notEqual a.X b.X "children occupy distinct x positions on the row axis"
                Expect.isLessThan (abs (a.Y - b.Y)) 2.0 "row-laid children share a y baseline"
                Expect.isFalse (overlaps a b) "side-by-side children do not overlap"
            | _ -> failtest "expected exactly two child bounds"
        }

        // FR-008 — two structurally similar UNKEYED same-kind siblings get distinct,
        // non-overlapping bounds (the collision case the structural id fixes).
        test "unkeyed same-kind siblings get distinct non-overlapping bounds (FR-008)" {
            let vertical: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "one" ]
                            TextBlock.create [ TextBlock.text "two" ] ] ]

            // Feature 098 (FR-007/SC-004): the two same-kind siblings now carry DISTINCT structural
            // path ids ("0.0", "0.1") in `Bounds`, replacing the old shared `Kind` collision.
            let kids = [ "0.0"; "0.1" ] |> List.collect (fun id -> boundsOf id (Control.renderTree theme size vertical))
            Expect.equal kids.Length 2 "both unkeyed siblings appear in Bounds with distinct path ids"

            match kids with
            | [ a; b ] ->
                Expect.isFalse (overlaps a b) "unkeyed same-kind siblings must not overlap"
                Expect.notEqual a.Y b.Y "column-laid children occupy distinct y positions"
            | _ -> failtest "expected exactly two child bounds"
        }

        // FR-009 — an explicit container width/height is reflected in its Bounds entry.
        test "explicit container width/height is reflected in computed bounds (FR-009)" {
            let sized: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Stack.create
                                [ Attr.width 200.0
                                  Attr.height 120.0
                                  Stack.children [ TextBlock.create [ TextBlock.text "boxed" ] ] ] ] ]

            let rendered = Control.renderTree theme size sized
            // Feature 098 (FR-007): the explicitly-sized inner stack is keyed by its structural path
            // "0.0" (the root stack is "0"), not the shared `Kind` "stack".
            let inner = boundsOf "0.0" rendered |> List.filter (fun r -> r.Width < 400.0)
            Expect.isNonEmpty inner "the explicitly-sized inner stack appears in Bounds"
            let box = List.head inner
            Expect.floatClose Accuracy.medium box.Width 200.0 "explicit width is honored"
            Expect.floatClose Accuracy.medium box.Height 120.0 "explicit height is honored"
        }

        // FR-011 — every laid-out control with a ControlId appears in Bounds with its box.
        test "Bounds exposes every laid-out control keyed by ControlId (FR-011)" {
            let tree: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ TextBlock.create [ TextBlock.text "label" ]
                            Button.create [ Button.text "Go"; Button.onClick Clicked ] ] ]

            let rendered = Control.renderTree theme size tree
            Expect.isNonEmpty rendered.Bounds "Bounds is populated"
            let ids = rendered.Bounds |> List.map fst
            // Feature 098 (FR-007): unkeyed controls carry their structural path id in `Bounds`.
            Expect.contains ids "0.0" "the text-block control has an evaluated box (structural path id)"
            Expect.contains ids "0.1" "the button control has an evaluated box (structural path id)"
            // The bound button must join with its EventBinding by ControlId (now the unified path id).
            let buttonId = rendered.EventBindings |> List.map (fun b -> b.ControlId) |> List.head
            Expect.contains ids buttonId "EventBindings join Bounds by the unified ControlId"
        }

        // FR-012 — a point inside a control resolves to it; a gap point resolves to None.
        test "hitTest resolves an inside point to its control and a gap to None (FR-012)" {
            let tree: Control<Msg> =
                Stack.create
                    [ Stack.children
                          [ Button.create [ Button.text "Go"; Button.onClick Clicked ] ] ]

            let rendered = Control.renderTree theme size tree
            // Feature 098 (FR-007): the sole unkeyed button is keyed by its structural path "0.0".
            let buttonBox = boundsOf "0.0" rendered |> List.head
            let cx, cy = center buttonBox
            Expect.equal (Control.hitTest rendered cx cy) (Some "0.0") "inside point hits the button (structural path id)"
            Expect.equal (Control.hitTest rendered -50.0 -50.0) None "a point outside every control is a gap"
        }
    ]
