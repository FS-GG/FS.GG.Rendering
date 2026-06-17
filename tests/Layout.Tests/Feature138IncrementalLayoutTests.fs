module Feature138IncrementalLayoutTests

open Expecto
open FS.GG.UI.Layout

let private available =
    { Width = 240.0
      WidthMode = Exactly
      Height = 160.0
      HeightMode = Exactly }

let private zeroPadding = { Left = 0.0; Top = 0.0; Right = 0.0; Bottom = 0.0 }

let private leaf id w h : LayoutNode =
    { Defaults.layoutNode id with
        Intent = { Defaults.layoutIntent with Size = { Width = Some w; Height = Some h } } }

let private child id intent =
    { Defaults.layoutNode id with
        Intent = intent
        Children = [ leaf $"{id}.text" 4.0 4.0 ] }

let private rootWith childIntent rootIntent =
    { Defaults.layoutNode "root" with
        Intent =
            { rootIntent with
                Direction = Row
                Size = { Width = Some 200.0; Height = Some 100.0 }
                Padding = zeroPadding
                Gap = { Row = 0.0; Column = 0.0 } }
        Children =
            [ child "target" childIntent
              child "sibling" { Defaults.layoutIntent with Size = { Width = Some 20.0; Height = Some 20.0 } } ] }

let private baseTree () =
    rootWith
        { Defaults.layoutIntent with
            Size = { Width = None; Height = Some 20.0 }
            FlexBasis = Some 20.0 }
        Defaults.layoutIntent

let private boundsMap (r: LayoutResult) =
    r.Bounds |> List.map (fun b -> b.NodeId, b) |> Map.ofList

let private updateTarget f node =
    let children =
        node.Children
        |> List.map (fun child -> if child.Id = "target" then { child with Intent = f child.Intent } else child)

    { node with Children = children }

let private cases: (string * LayoutNodeId * (LayoutNode -> LayoutNode)) list =
    [ "padding", "target", updateTarget (fun i -> { i with Padding = { Left = 4.0; Top = 4.0; Right = 4.0; Bottom = 4.0 } })
      "margin", "target", updateTarget (fun i -> { i with Margin = { Left = 3.0; Top = 3.0; Right = 3.0; Bottom = 3.0 } })
      "gap", "root", fun root -> { root with Intent = { root.Intent with Gap = { Row = 5.0; Column = 5.0 } } }
      "alignItems", "root", fun root -> { root with Intent = { root.Intent with AlignItems = LayoutAlign.Center } }
      "alignSelf", "target", updateTarget (fun i -> { i with AlignSelf = Some LayoutAlign.End })
      "justifyContent", "root", fun root -> { root with Intent = { root.Intent with JustifyContent = LayoutAlign.End } }
      "flexGrow", "target", updateTarget (fun i -> { i with FlexGrow = 1.0 })
      "flexShrink", "target", updateTarget (fun i -> { i with FlexShrink = 0.0 })
      "flexBasis", "target", updateTarget (fun i -> { i with FlexBasis = Some 50.0 })
      "minWidth", "target", updateTarget (fun i -> { i with MinSize = { i.MinSize with Width = Some 35.0 } })
      "minHeight", "target", updateTarget (fun i -> { i with MinSize = { i.MinSize with Height = Some 28.0 } })
      "maxWidth", "target", updateTarget (fun i -> { i with MaxSize = { i.MaxSize with Width = Some 18.0 } })
      "maxHeight", "target", updateTarget (fun i -> { i with MaxSize = { i.MaxSize with Height = Some 16.0 } }) ]

[<Tests>]
let tests =
    testList "Feature138IncrementalLayout" [
        for name, dirtyId, edit in cases do
            test $"{name} changed value: incremental layout equals full layout" {
                let before = baseTree ()
                let after = edit before
                let previous = Layout.evaluate available before
                let incremental = Layout.evaluateIncremental previous [ dirtyId ] available after
                let full = Layout.evaluate available after

                Expect.equal (boundsMap incremental) (boundsMap full) $"{name} bounds match full layout"
                Expect.contains incremental.Invalidated dirtyId $"{name} dirty id is re-measured"
            }
    ]
