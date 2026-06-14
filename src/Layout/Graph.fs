namespace FS.Skia.UI.Layout

open System
open FS.Skia.UI.Scene

type GraphTarget =
    | Node of nodeId: string
    | Edge of edgeIndex: int

module Graph =
    let nodeBounds width height x y : LayoutBounds =
        { LayoutBounds.X = x - width / 2.0
          Y = y - height / 2.0
          Width = width
          Height = height }

    let directedLayout (graph: GraphDefinition) =
        let incoming =
            graph.Edges
            |> List.groupBy _.Target
            |> List.map (fun (target, edges) -> target, edges |> List.map _.Source)
            |> Map.ofList

        let rec layerOf memo nodeId =
            match memo |> Map.tryFind nodeId with
            | Some layer -> memo, layer
            | None ->
                let parents = incoming |> Map.tryFind nodeId |> Option.defaultValue []

                let memo, layer =
                    parents
                    |> List.fold
                        (fun (state, maxLayer) parent ->
                            let nextState, parentLayer = layerOf state parent
                            nextState, max maxLayer (parentLayer + 1))
                        (memo, 0)

                Map.add nodeId layer memo, layer

        let layered =
            ((Map.empty, []), graph.Nodes)
            ||> List.fold (fun (memo, items) node ->
                let nextMemo, layer = layerOf memo node.Id
                nextMemo, (layer, node) :: items)
            |> snd
            |> List.rev
            |> List.groupBy fst
            |> List.sortBy fst

        let layerCount = max 1 layered.Length
        let nodeWidth = min 120.0 (max 42.0 (graph.Config.Bounds.Width / float (layerCount * 2)))
        let nodeHeight = 28.0

        layered
        |> List.collect (fun (layer, nodes) ->
            let count = max 1 nodes.Length
            nodes
            |> List.map snd
            |> List.mapi (fun index node ->
                let x = graph.Config.Bounds.X + ((float layer + 0.5) / float layerCount) * graph.Config.Bounds.Width
                let y = graph.Config.Bounds.Y + ((float index + 0.5) / float count) * graph.Config.Bounds.Height

                { Node = node
                  Bounds = nodeBounds nodeWidth nodeHeight x y }))

    let undirectedLayout (graph: GraphDefinition) =
        let count = max 1 graph.Nodes.Length
        let radius = max 24.0 (min graph.Config.Bounds.Width graph.Config.Bounds.Height / 2.0 - 36.0)
        let centerX = graph.Config.Bounds.X + graph.Config.Bounds.Width / 2.0
        let centerY = graph.Config.Bounds.Y + graph.Config.Bounds.Height / 2.0

        graph.Nodes
        |> List.mapi (fun index node ->
            let angle = (Math.PI * 2.0 * float index) / float count
            let x = centerX + cos angle * radius
            let y = centerY + sin angle * radius

            { Node = node
              Bounds = nodeBounds 72.0 28.0 x y })

    let center (bounds: LayoutBounds) =
        { X = bounds.X + bounds.Width / 2.0
          Y = bounds.Y + bounds.Height / 2.0 }

    let rect (bounds: LayoutBounds) =
        { Rect.X = bounds.X
          Y = bounds.Y
          Width = bounds.Width
          Height = bounds.Height }

    let nodeLookup (layout: GraphLayoutResult) =
        layout.Nodes
        |> List.map (fun item -> item.Node.Id, item)
        |> Map.ofList

    let layout (graph: GraphDefinition) =
        match GraphValidation.validate graph with
        | [] ->
            let nodes =
                match graph.Config.Kind with
                | Directed -> directedLayout graph
                | Undirected -> undirectedLayout graph

            Ok { Nodes = nodes; Edges = graph.Edges }
        | issues -> Result.Error issues

    let render (graph: GraphDefinition) =
        layout graph
        |> Result.map (fun result ->
            let nodes = nodeLookup result

            let edgeScenes =
                result.Edges
                |> List.mapi (fun index edge ->
                    match nodes |> Map.tryFind edge.Source, nodes |> Map.tryFind edge.Target with
                    | Some source, Some target ->
                        let sourceCenter = center source.Bounds
                        let targetCenter = center target.Bounds
                        let label =
                            edge.Label
                            |> Option.orElse (edge.Weight |> Option.map (fun value -> value.ToString("G4", Globalization.CultureInfo.InvariantCulture)))

                        Scene.group [
                            Scene.line sourceCenter targetCenter (Paint.stroke (Colors.rgba 150uy 170uy 195uy 220uy) 1.5)
                            match label with
                            | Some text ->
                                let midX = (sourceCenter.X + targetCenter.X) / 2.0
                                let midY = (sourceCenter.Y + targetCenter.Y) / 2.0
                                Scene.text (midX, midY) text Colors.white
                            | None -> Scene.empty
                        ]
                    | _ ->
                        Scene.text (graph.Config.Bounds.X, graph.Config.Bounds.Y + float index * 16.0) "invalid edge" (Colors.rgba 220uy 64uy 52uy 255uy))

            let nodeScenes =
                result.Nodes
                |> List.map (fun item ->
                    Scene.group [
                        Scene.rectangleWithPaint (rect item.Bounds) (Paint.fill (item.Node.Style |> Option.defaultValue (Colors.rgba 64uy 128uy 220uy 230uy)))
                        Scene.text (item.Bounds.X + 6.0, item.Bounds.Y + 18.0) item.Node.Label Colors.white
                    ])

            Scene.group (edgeScenes @ nodeScenes))

    let directed (graph: GraphDefinition) =
        render { graph with Config = { graph.Config with Kind = Directed } }

    let undirected (graph: GraphDefinition) =
        render { graph with Config = { graph.Config with Kind = Undirected } }

    let hitTest (layout: GraphLayoutResult) x y =
        let nodeHit =
            layout.Nodes
            |> List.tryPick (fun item ->
            if x >= item.Bounds.X
               && x <= item.Bounds.X + item.Bounds.Width
               && y >= item.Bounds.Y
               && y <= item.Bounds.Y + item.Bounds.Height then
                Some(Node item.Node.Id)
            else
                None)

        match nodeHit with
        | Some target -> Some target
        | None ->
            let nodes = nodeLookup layout

            layout.Edges
            |> List.mapi (fun index edge ->
                match nodes |> Map.tryFind edge.Source, nodes |> Map.tryFind edge.Target with
                | Some source, Some target ->
                    let sourceCenter = center source.Bounds
                    let targetCenter = center target.Bounds
                    let midX = (sourceCenter.X + targetCenter.X) / 2.0
                    let midY = (sourceCenter.Y + targetCenter.Y) / 2.0
                    let distance = sqrt ((x - midX) ** 2.0 + (y - midY) ** 2.0)
                    Some(distance, Edge index)
                | _ -> None)
            |> List.choose id
            |> List.sortBy fst
            |> List.tryHead
            |> Option.bind (fun (distance, target) -> if distance <= 16.0 then Some target else None)
