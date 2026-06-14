namespace FS.Skia.UI.Layout

type GraphValidationIssue =
    | DuplicateNodeId of string
    | MissingSource of edgeIndex: int * nodeId: string
    | MissingTarget of edgeIndex: int * nodeId: string
    | SelfLoop of edgeIndex: int * nodeId: string
    | CycleDetected of nodeIds: string list

module GraphValidation =
    let duplicates values =
        values
        |> List.countBy id
        |> List.choose (fun (value, count) -> if count > 1 then Some value else None)

    let hasCycle (graph: GraphDefinition) =
        if graph.Config.Kind = Undirected then
            false
        else
            let nodes = graph.Nodes |> List.map _.Id |> Set.ofList
            let outgoing =
                graph.Edges
                |> List.filter (fun edge -> nodes.Contains edge.Source && nodes.Contains edge.Target)
                |> List.groupBy _.Source
                |> Map.ofList

            let rec visit visiting visited node =
                if Set.contains node visiting then
                    true
                elif Set.contains node visited then
                    false
                else
                    let next = outgoing |> Map.tryFind node |> Option.defaultValue [] |> List.map _.Target
                    next |> List.exists (visit (Set.add node visiting) (Set.add node visited))

            graph.Nodes |> List.exists (fun node -> visit Set.empty Set.empty node.Id)

    let validate (graph: GraphDefinition) =
        let nodeIds = graph.Nodes |> List.map _.Id
        let nodeSet = Set.ofList nodeIds

        let duplicateIssues = nodeIds |> duplicates |> List.map DuplicateNodeId

        let edgeIssues =
            graph.Edges
            |> List.mapi (fun index edge ->
                [ if not (nodeSet.Contains edge.Source) then MissingSource(index, edge.Source)
                  if not (nodeSet.Contains edge.Target) then MissingTarget(index, edge.Target)
                  if edge.Source = edge.Target then SelfLoop(index, edge.Source) ])
            |> List.concat

        let cycleIssues =
            if hasCycle graph then [ CycleDetected nodeIds ] else []

        duplicateIssues @ edgeIssues @ cycleIssues

    let disconnectedComponents (graph: GraphDefinition) =
        let adjacency =
            graph.Edges
            |> List.collect (fun edge -> [ edge.Source, edge.Target; edge.Target, edge.Source ])
            |> List.groupBy fst
            |> List.map (fun (key, pairs) -> key, pairs |> List.map snd)
            |> Map.ofList

        let rec collect seen frontier =
            match frontier with
            | [] -> seen
            | node :: rest when Set.contains node seen -> collect seen rest
            | node :: rest ->
                let next = adjacency |> Map.tryFind node |> Option.defaultValue []
                collect (Set.add node seen) (next @ rest)

        let rec loop remaining components =
            match remaining with
            | [] -> List.rev components
            | node :: rest ->
                let group = collect Set.empty [ node.Id ]
                let rest = rest |> List.filter (fun item -> not (group.Contains item.Id))
                loop rest ((Set.toList group) :: components)

        loop graph.Nodes []
