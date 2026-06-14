module ControlsTypedGalleryPanel

open FS.Skia.UI.Controls
open FS.Skia.UI.Controls.Typed

// Typed-authoring gallery panel for the US2 render/accessibility coverage suites
// (feature 071). Authored ONLY through `FS.Skia.UI.Controls.Typed.*` `view` functions —
// no `Attr`, no `*.create` — mirroring `samples/ControlsGallery/Program.fs`
// `typedAuthoringPanel`. Covers >=1 control per mechanic group (display, input, stateful
// input, layout container, navigation/composite, overlay, selection collection,
// charts/graph). Stateful controls reuse the shipped 070 MVU models via their `init`.
//
// Stateful controls (TextArea, ListBox) reuse their shipped 070 MVU models via `init`
// — no new Model/Msg/Effect (contract G4). The same composition is mirrored into the
// sample's `typedAuthoringPanel` (T014).
let private noteProps : TextAreaProps<int> = { TextArea.defaults "typed-note" with Value = "draft" }
let private noteModel = fst (TextArea.init noteProps)
let private listProps : ListBoxProps<int> = { ListBox.defaults "typed-list" with Items = [ "Alpha"; "Beta"; "Gamma" ] }
let private listModel = fst (ListBox.init listProps)

let panel : Control<int> =
    Stack.view
        { Stack.defaults with
            Orientation = Vertical
            Spacing = 4.0
            Children =
                [ TextBlock.view { TextBlock.defaults with Text = "Typed gallery (071)" } // display
                  Button.view { Button.defaults with Text = "Typed Save"; Intent = Primary } // input (action)
                  TextArea.view noteProps noteModel // stateful input
                  CheckBox.view { CheckBox.defaults with Text = "Typed toggle"; Checked = true } // selection
                  ListBox.view listProps listModel // selection collection
                  Tabs.view { Tabs.defaults with Items = [ "Form"; "Data" ]; SelectedKey = Some "Form" } // navigation/composite
                  Tooltip.view { Tooltip.defaults with Text = "Typed tooltip" } // overlay
                  LineChart.view
                      { LineChart.defaults with
                          Series = [ { Name = "count"; Points = [ { X = 0.0; Y = 1.0; Label = None } ] } ] } // chart
                  GraphView.view { GraphView.defaults with Nodes = [ "form"; "data" ] } ] } // graph
    |> Widget.toControl

/// Every control node in the panel tree, descending through child attributes too
/// (Stack children lower into a `ChildrenValue` attribute, not `Control.Children`).
let rec nodes (c: Control<'msg>) : Control<'msg> list =
    let attrChildren =
        c.Attributes
        |> List.collect (fun a ->
            match a.Value with
            | ChildValue ch -> nodes ch
            | ChildrenValue chs -> chs |> List.collect nodes
            | _ -> [])
    c :: (c.Children |> List.collect nodes) @ attrChildren

/// The set of control kinds present in the panel.
let kindsPresent (root: Control<'msg>) : Set<string> =
    nodes root |> List.map (fun c -> c.Kind) |> Set.ofList

/// The set of catalog categories the panel covers (kind -> catalog `Category`).
let coveredCategories (root: Control<'msg>) : Set<string> =
    let categoryById =
        Catalog.supportedControls |> List.map (fun r -> r.Id, r.Category) |> Map.ofList
    nodes root
    |> List.choose (fun c -> Map.tryFind c.Kind categoryById)
    |> Set.ofList
