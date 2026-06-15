module Feature108CompositionTests

// Feature 108 (US5) — `Control.map`/`Widget.map` change only the message type (structure / key /
// focus identity preserved, lowering structurally equal to authoring directly in 'b, SC-007), and
// the DataGrid sort cycles asc → desc → none on the third toggle (SC-008). Pure transitions; the
// in-assembly test is the user-reachable surface ([[fs-gg-ui-widgets]]). Red on the pre-108 build
// (no `Control.map`; bi-state sort).

open Expecto
open FS.GG.UI.Controls

type private Msg =
    | Inc
    | Dec

type private Outer = Wrap of Msg

[<Tests>]
let tests =
    testList "Feature 108 composition (US5, FR-014/015, SC-007/008)" [
        test "Control.map lowers structurally equal to authoring directly in 'b (SC-007)" {
            let inA: Control<Msg> =
                Button.create [ Button.text "x"; Button.onClick Inc ] |> Control.withKey "b1"

            let mapped: Control<Outer> = Control.map Wrap inA

            let directB: Control<Outer> =
                Button.create [ Button.text "x"; Button.onClick (Wrap Inc) ] |> Control.withKey "b1"

            Expect.equal (sprintf "%A" mapped) (sprintf "%A" directB) "map only changes the message type"
        }

        test "Control.map preserves key / structure through nested children (FR-014)" {
            let child =
                Button.create [ Button.text "c"; Button.onClick Dec ] |> Control.withKey "child"

            let inA: Control<Msg> = Control.create "stack" [ Attr.children [ child ] ] |> Control.withKey "root"
            let mapped: Control<Outer> = Control.map Wrap inA

            Expect.equal mapped.Key (Some "root") "root key preserved"
            Expect.equal mapped.Kind "stack" "root kind preserved"
            Expect.equal mapped.Children.Length 1 "child count preserved"
            Expect.equal mapped.Children.[0].Key (Some "child") "child key/focus identity preserved"
        }

        test "Widget.map = ofControl ∘ Control.map ∘ toControl (FR-014)" {
            let inA: Control<Msg> =
                Button.create [ Button.text "x"; Button.onClick Inc ] |> Control.withKey "b1"

            let viaWidget = Widget.map Wrap (Widget.ofControl inA) |> Widget.toControl
            let viaControl = Control.map Wrap inA
            Expect.equal (sprintf "%A" viaWidget) (sprintf "%A" viaControl) "Widget.map composes Control.map"
        }

        test "DataGrid sort cycles None -> Asc -> Desc -> None on repeated SortBy (SC-008)" {
            let col =
                { Key = "name"
                  Header = "Name"
                  Width = 120.0
                  ColumnType = TextColumn }

            let model0, _ = DataGrid.init "grid" [ col ] 10 24.0 240.0
            Expect.equal model0.Sort None "initial sort is unsorted"

            let m1, e1 = DataGrid.update (SortBy "name") model0
            Expect.equal m1.Sort (Some { ColumnKey = "name"; Direction = Ascending }) "first toggle -> Ascending"
            Expect.contains e1 (DataGridSortChanged(Some { ColumnKey = "name"; Direction = Ascending })) "asc effect fired"

            let m2, _ = DataGrid.update (SortBy "name") m1
            Expect.equal m2.Sort (Some { ColumnKey = "name"; Direction = Descending }) "second toggle -> Descending"

            let m3, e3 = DataGrid.update (SortBy "name") m2
            Expect.equal m3.Sort None "third toggle clears the sort"
            Expect.contains e3 (DataGridSortChanged None) "clearing effect fired (DataGridSortChanged None)"
        }

        test "DataGrid sort restarts at Ascending when a different column is sorted" {
            let cols =
                [ { Key = "a"; Header = "A"; Width = 80.0; ColumnType = TextColumn }
                  { Key = "b"; Header = "B"; Width = 80.0; ColumnType = TextColumn } ]

            let model0, _ = DataGrid.init "grid" cols 5 24.0 120.0
            let m1, _ = DataGrid.update (SortBy "a") model0
            let m2, _ = DataGrid.update (SortBy "b") m1
            Expect.equal m2.Sort (Some { ColumnKey = "b"; Direction = Ascending }) "a different column restarts at Ascending"
        }
    ]
