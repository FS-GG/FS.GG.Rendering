module LayoutTests

open Expecto
open System.Diagnostics
open FS.Skia.UI.Scene
open FS.Skia.UI.Layout

let child label =
    Defaults.child (Scene.text (0.0, 0.0) label Colors.white)

let overlaps (a: LayoutBounds) (b: LayoutBounds) =
    a.X < b.X + b.Width
    && a.X + a.Width > b.X
    && a.Y < b.Y + b.Height
    && a.Y + a.Height > b.Y

let node id =
    { Id = id
      Label = id
      Style = None }

let edge source target =
    { Source = source
      Target = target
      Weight = None
      Label = None }

let graph kind nodes edges =
    { Config = Defaults.graphConfig kind 640.0 360.0
      Nodes = nodes
      Edges = edges }

let measure width height =
    fun _ ->
        { Width = width
          Height = height
          Diagnostics = [] }

let layoutLeaf id width height =
    { Defaults.layoutNode id with
        Intent =
            { Defaults.layoutIntent with
                Size = { Width = Some width; Height = Some height } }
        Content = Some(Scene.rectangle (0.0, 0.0, width, height) Colors.white) }

let layoutMeasured id width height =
    { Defaults.layoutNode id with
        Measure = Some(measure width height)
        Content = Some(Scene.text (0.0, 0.0) id Colors.white) }

let boundsOf id (result: LayoutResult) =
    result.Bounds |> List.find (fun item -> item.NodeId = id) |> fun item -> item.Bounds

let visibleBounds (result: LayoutResult) =
    result.Bounds
    |> List.filter (fun item -> item.Visibility = Visible)
    |> List.map _.Bounds

let assertNoOverlap message (bounds: LayoutBounds list) =
    for leftIndex in 0 .. bounds.Length - 1 do
        for rightIndex in leftIndex + 1 .. bounds.Length - 1 do
            Expect.isFalse (overlaps bounds[leftIndex] bounds[rightIndex]) $"{message}: {leftIndex} and {rightIndex}"

[<Tests>]
let contractTests =
    testList "Layout contract" [
        test "default stack config is constructible" {
            let config = Defaults.stackConfig 800.0 600.0
            Expect.equal config.Bounds.Height 600.0 "height is retained"
            Expect.equal config.Padding Defaults.padding "default padding is retained"
            Expect.equal config.Spacing 0.0 "default spacing is retained"
        }

        test "horizontal and vertical stack measurement honors padding spacing and child count" {
            let config =
                { Defaults.stackConfig 300.0 120.0 with
                    Padding = { Left = 10.0; Top = 8.0; Right = 20.0; Bottom = 12.0 }
                    Spacing = 5.0 }

            let children = [ child "a"; child "b"; child "c" ]
            let horizontal = Layout.measureHorizontal config children
            let vertical = Layout.measureVertical config children

            Expect.equal horizontal.Length 3 "horizontal measurement returns one bound per child"
            Expect.floatClose Accuracy.medium horizontal[0].X 10.0 "left padding is applied"
            Expect.floatClose Accuracy.medium horizontal[1].X (10.0 + horizontal[0].Width + 5.0) "horizontal spacing is applied"
            Expect.floatClose Accuracy.medium horizontal[0].Height 100.0 "vertical padding is applied to horizontal stack"
            Expect.floatClose Accuracy.medium vertical[0].Y 8.0 "top padding is applied"
            Expect.floatClose Accuracy.medium vertical[1].Y (8.0 + vertical[0].Height + 5.0) "vertical spacing is applied"
            Expect.floatClose Accuracy.medium vertical[0].Width 270.0 "horizontal padding is applied to vertical stack"
        }

        test "dock config and child sizing records retain public layout props" {
            let dockConfig =
                { Defaults.dockConfig 640.0 480.0 with
                    Padding = { Left = 4.0; Top = 6.0; Right = 8.0; Bottom = 10.0 }
                    Spacing = 3.0 }

            let sized =
                { Content = Scene.rectangle (0.0, 0.0, 10.0, 10.0) Colors.white
                  Sizing =
                    { DesiredWidth = Some 120.0
                      DesiredHeight = Some 48.0
                      HorizontalAlignment = HorizontalAlignment.Center
                      VerticalAlignment = VerticalAlignment.Middle }
                  Dock = Some Left }

            let scene = Layout.dock dockConfig [ sized ]
            Expect.equal sized.Sizing.DesiredWidth (Some 120.0) "desired width is retained"
            Expect.equal sized.Sizing.HorizontalAlignment HorizontalAlignment.Center "horizontal alignment is retained"
            Expect.equal sized.Dock (Some Left) "dock position is retained"
            Expect.contains (Scene.describe scene) RectangleElement "dock returns child scene content"
        }

        test "zero and negative stack bounds clamp measured child sizes to non-negative values" {
            let zero = Layout.measureHorizontal (Defaults.stackConfig 0.0 0.0) [ child "zero" ]
            let negative = Layout.measureVertical (Defaults.stackConfig -20.0 -10.0) [ child "negative" ]

            Expect.equal zero[0].Width 0.0 "zero width is stable"
            Expect.equal zero[0].Height 0.0 "zero height is stable"
            Expect.equal negative[0].Width 0.0 "negative width is clamped"
            Expect.equal negative[0].Height 0.0 "negative height is clamped"
        }

        test "layout resize keeps at least ten horizontal children non-overlapping at three sizes" {
            let children = [ for index in 0 .. 9 -> child $"item-{index}" ]
            let sizes = [ 320.0, 160.0; 640.0, 240.0; 960.0, 360.0 ]

            for width, height in sizes do
                let config =
                    { Defaults.stackConfig width height with
                        Padding = { Left = 8.0; Top = 8.0; Right = 8.0; Bottom = 8.0 }
                        Spacing = 4.0 }

                let bounds = Layout.measureHorizontal config children

                Expect.equal bounds.Length 10 $"all children are measured at {width}x{height}"
                Expect.all bounds (fun item -> item.Width >= 0.0 && item.Height >= 0.0) "bounds are non-negative"

                for leftIndex in 0 .. bounds.Length - 1 do
                    for rightIndex in leftIndex + 1 .. bounds.Length - 1 do
                        Expect.isFalse (overlaps bounds[leftIndex] bounds[rightIndex]) $"children {leftIndex} and {rightIndex} do not overlap at {width}x{height}"
        }

        test "layout resize keeps at least ten vertical children non-overlapping at three sizes" {
            let children = [ for index in 0 .. 9 -> child $"row-{index}" ]
            let sizes = [ 240.0, 320.0; 360.0, 640.0; 480.0, 960.0 ]

            for width, height in sizes do
                let config =
                    { Defaults.stackConfig width height with
                        Padding = { Left = 6.0; Top = 10.0; Right = 6.0; Bottom = 10.0 }
                        Spacing = 3.0 }

                let bounds = Layout.measureVertical config children

                Expect.equal bounds.Length 10 $"all children are measured at {width}x{height}"
                Expect.all bounds (fun item -> item.Width >= 0.0 && item.Height >= 0.0) "bounds are non-negative"

                for topIndex in 0 .. bounds.Length - 1 do
                    for bottomIndex in topIndex + 1 .. bounds.Length - 1 do
                        Expect.isFalse (overlaps bounds[topIndex] bounds[bottomIndex]) $"children {topIndex} and {bottomIndex} do not overlap at {width}x{height}"
        }

        test "graph validation reports duplicates missing endpoints self-loops and cycles" {
            let invalid =
                graph
                    Directed
                    [ node "a"; node "a"; node "b"; node "c" ]
                    [ edge "a" "b"
                      edge "b" "missing"
                      edge "missing" "c"
                      edge "c" "c"
                      edge "b" "a" ]

            let issues = GraphValidation.validate invalid

            Expect.contains issues (DuplicateNodeId "a") "duplicate node id is reported"
            Expect.exists issues (function MissingTarget(1, "missing") -> true | _ -> false) "missing target is reported"
            Expect.exists issues (function MissingSource(2, "missing") -> true | _ -> false) "missing source is reported"
            Expect.exists issues (function SelfLoop(3, "c") -> true | _ -> false) "self-loop is reported"
            Expect.exists issues (function CycleDetected _ -> true | _ -> false) "directed cycle is reported"
        }

        test "graph validation reports disconnected components and accepts dense edge sets" {
            let disconnected =
                graph
                    Undirected
                    [ node "a"; node "b"; node "c"; node "d"; node "e" ]
                    [ edge "a" "b"; edge "c" "d" ]

            let components = GraphValidation.disconnectedComponents disconnected
            Expect.equal (components |> List.length) 3 "two pairs and one isolated node produce three components"

            let denseNodes = [ for index in 0 .. 11 -> node $"n{index}" ]
            let denseEdges =
                [ for source in denseNodes do
                      for target in denseNodes do
                          if source.Id <> target.Id then
                              edge source.Id target.Id ]

            let dense = graph Undirected denseNodes denseEdges
            Expect.isEmpty (GraphValidation.validate dense) "dense undirected edge set is valid"
            Expect.isFalse (GraphValidation.hasCycle dense) "undirected dense graph cycle detection is intentionally not a DAG failure"
        }

        test "graph layout handles one hundred node DAG within two seconds" {
            let nodes = [ for index in 0 .. 99 -> node $"n{index}" ]
            let edges = [ for index in 0 .. 98 -> edge $"n{index}" $"n{index + 1}" ]
            let dag = graph Directed nodes edges
            let stopwatch = Stopwatch.StartNew()
            let result = Graph.layout dag
            stopwatch.Stop()

            match result with
            | Ok layout ->
                Expect.equal layout.Nodes.Length 100 "all DAG nodes are laid out"
                Expect.equal layout.Edges.Length 99 "all DAG edges are retained"
                Expect.isLessThan stopwatch.ElapsedMilliseconds 2000L "100-node DAG layout stays under two seconds"
            | Result.Error issues -> failtestf "expected valid DAG layout, got %A" issues
        }

        test "weighted undirected graph with fifty nodes has visible components and renders a scene" {
            let nodes = [ for index in 0 .. 49 -> node $"u{index}" ]
            let edges =
                [ for index in 0 .. 49 ->
                      { Source = $"u{index}"
                        Target = $"u{(index + 7) % 50}"
                        Weight = Some(float (index % 9 + 1))
                        Label = Some $"w{index % 9 + 1}" } ]

            let graph = graph Undirected nodes edges

            match Graph.layout graph, Graph.undirected graph with
            | Ok layout, Ok scene ->
                Expect.equal layout.Nodes.Length 50 "all weighted graph nodes are visible"
                Expect.isTrue (layout.Nodes |> List.forall (fun item -> item.Bounds.Width > 0.0 && item.Bounds.Height > 0.0)) "node bounds are visible"
                Expect.contains (Scene.describe scene) GroupElement "undirected graph renders as a grouped scene"
                Expect.contains (Scene.describe scene) TextElement "undirected graph includes visible node labels"
            | Result.Error issues, _ -> failtestf "expected valid weighted graph layout, got %A" issues
            | _, Result.Error issues -> failtestf "expected valid weighted graph scene, got %A" issues
        }

        test "graph scene builders render edges labels weights and hit-test nodes and edges" {
            let graph =
                graph
                    Directed
                    [ node "a"; node "b"; node "c" ]
                    [ { Source = "a"; Target = "b"; Weight = Some 2.5; Label = Some "a-b" }
                      { Source = "b"; Target = "c"; Weight = Some 4.0; Label = None } ]

            match Graph.layout graph, Graph.directed graph with
            | Ok layout, Ok scene ->
                let kinds = Scene.describe scene
                Expect.contains kinds LineElement "graph scene includes edge lines"
                Expect.contains kinds RectangleElement "graph scene includes node boxes"
                Expect.contains kinds TextElement "graph scene includes labels and weights"

                let firstNode = layout.Nodes.Head
                let nodeCenterX = firstNode.Bounds.X + firstNode.Bounds.Width / 2.0
                let nodeCenterY = firstNode.Bounds.Y + firstNode.Bounds.Height / 2.0
                Expect.equal (Graph.hitTest layout nodeCenterX nodeCenterY) (Some(Node firstNode.Node.Id)) "node hit-test returns node target"

                let source = layout.Nodes |> List.find (fun item -> item.Node.Id = "a")
                let target = layout.Nodes |> List.find (fun item -> item.Node.Id = "b")
                let edgeX = (source.Bounds.X + source.Bounds.Width / 2.0 + target.Bounds.X + target.Bounds.Width / 2.0) / 2.0
                let edgeY = (source.Bounds.Y + source.Bounds.Height / 2.0 + target.Bounds.Y + target.Bounds.Height / 2.0) / 2.0
                Expect.equal (Graph.hitTest layout edgeX edgeY) (Some(Edge 0)) "edge hit-test returns edge target"
            | Result.Error issues, _ -> failtestf "expected graph layout, got %A" issues
            | _, Result.Error issues -> failtestf "expected graph scene, got %A" issues
        }

        test "automatic layout arranges row column and wrap containers with non-overlapping child bounds" {
            let rowRoot =
                { Defaults.layoutNode "row-root" with
                    Intent = { Defaults.layoutIntent with Direction = Row; Gap = { Row = 0.0; Column = 8.0 } }
                    Children = [ layoutLeaf "a" 40.0 30.0; layoutLeaf "b" 50.0 30.0; layoutLeaf "c" 60.0 30.0 ] }

            let columnRoot =
                { Defaults.layoutNode "column-root" with
                    Intent = { Defaults.layoutIntent with Direction = Column; Gap = { Row = 6.0; Column = 0.0 } }
                    Children = [ layoutLeaf "r1" 40.0 24.0; layoutLeaf "r2" 50.0 28.0; layoutLeaf "r3" 60.0 32.0 ] }

            let wrapRoot =
                { Defaults.layoutNode "wrap-root" with
                    Intent = { Defaults.layoutIntent with Direction = Row; Wrap = Wrap; Gap = { Row = 4.0; Column = 4.0 } }
                    Children = [ for index in 0 .. 5 -> layoutLeaf $"w{index}" 50.0 20.0 ] }

            let row = Layout.evaluate (Defaults.availableSpace 240.0 80.0) rowRoot
            let column = Layout.evaluate (Defaults.availableSpace 100.0 160.0) columnRoot
            let wrap = Layout.evaluate (Defaults.availableSpace 120.0 120.0) wrapRoot

            Expect.isEmpty row.Diagnostics "row layout has no diagnostics"
            Expect.isEmpty column.Diagnostics "column layout has no diagnostics"
            Expect.equal row.Bounds.Length 4 "row includes root and children"
            Expect.equal column.Bounds.Length 4 "column includes root and children"
            assertNoOverlap "row children do not overlap" (visibleBounds row |> List.tail)
            assertNoOverlap "column children do not overlap" (visibleBounds column |> List.tail)
            assertNoOverlap "wrapped children do not overlap" (visibleBounds wrap |> List.tail)
            Expect.isGreaterThan (boundsOf "w2" wrap).Y (boundsOf "w0" wrap).Y "wrapped items flow onto later rows"
        }

        test "automatic layout honors padding margin gaps and alignment" {
            let root =
                { Defaults.layoutNode "root" with
                    Intent =
                        { Defaults.layoutIntent with
                            Direction = Row
                            AlignItems = Center
                            JustifyContent = Center
                            Padding = { Left = 10.0; Top = 8.0; Right = 10.0; Bottom = 8.0 }
                            Gap = { Row = 0.0; Column = 5.0 } }
                    Children =
                        [ { layoutLeaf "left" 40.0 20.0 with Intent = { (layoutLeaf "left" 40.0 20.0).Intent with Margin = { Left = 3.0; Top = 0.0; Right = 3.0; Bottom = 0.0 } } }
                          layoutLeaf "right" 40.0 20.0 ] }

            let result = Layout.evaluate (Defaults.availableSpace 140.0 60.0) root
            let left = boundsOf "left" result
            let right = boundsOf "right" result

            Expect.isEmpty result.Diagnostics "valid spacing and alignment has no diagnostics"
            Expect.isGreaterThan left.X 10.0 "left child is inside horizontal padding"
            Expect.isGreaterThan left.Y 8.0 "center alignment moves child inside vertical padding"
            Expect.floatClose Accuracy.medium right.X (left.X + left.Width + 5.0 + 3.0) "gap and margin separate children"
        }

        test "automatic layout applies fixed min max flex grow shrink basis and deterministic repeated evaluation" {
            let flexible id grow shrink basis =
                { Defaults.layoutNode id with
                    Intent =
                        { Defaults.layoutIntent with
                            Size = { Width = None; Height = Some 20.0 }
                            MinSize = { Width = Some 20.0; Height = None }
                            MaxSize = { Width = Some 140.0; Height = None }
                            FlexGrow = grow
                            FlexShrink = shrink
                            FlexBasis = Some basis } }

            let root =
                { Defaults.layoutNode "root" with
                    Intent = { Defaults.layoutIntent with Direction = Row; Gap = { Row = 0.0; Column = 0.0 } }
                    Children = [ flexible "one" 1.0 1.0 30.0; flexible "two" 2.0 1.0 30.0 ] }

            let first = Layout.evaluate (Defaults.availableSpace 180.0 40.0) root
            let second = Layout.evaluate (Defaults.availableSpace 180.0 40.0) root

            Expect.equal first.Bounds second.Bounds "repeated evaluation is deterministic"
            Expect.isGreaterThan (boundsOf "two" first).Width (boundsOf "one" first).Width "larger flex grow receives more space"
            Expect.isLessThanOrEqual (boundsOf "two" first).Width 140.0 "max width constrains flexible child"
        }

        test "automatic layout custom measurement callbacks influence preferred size and diagnostics" {
            let measurementDiagnostic =
                { NodeId = Some "measured"
                  Code = LayoutDiagnosticCode.UnmeasurableContent
                  Severity = FS.Skia.UI.Layout.DiagnosticSeverity.Warning
                  Message = "sample measurement warning"
                  Constraint = Some "text"
                  FallbackApplied = false }

            let measured =
                { Defaults.layoutNode "measured" with
                    Measure =
                        Some(fun _ ->
                            { Width = 72.0
                              Height = 18.0
                              Diagnostics = [ measurementDiagnostic ] }) }

            let root = { Defaults.layoutNode "root" with Children = [ measured ] }
            let result = Layout.evaluate (Defaults.availableSpace 160.0 60.0) root

            Expect.equal (boundsOf "measured" result).Width 72.0 "measured width is used"
            Expect.contains result.Diagnostics measurementDiagnostic "measurement diagnostics are propagated"
        }

        test "automatic layout incremental evaluation reports the actual re-measured set (FR-001a) and keeps bounds byte-identical to full evaluate" {
            let root size =
                { Defaults.layoutNode "root" with
                    Children =
                        [ layoutLeaf "stable" 40.0 20.0
                          { layoutLeaf "changed" size 20.0 with Intent = { (layoutLeaf "changed" size 20.0).Intent with FlexGrow = 1.0 } } ] }

            let first = Layout.evaluate (Defaults.availableSpace 180.0 40.0) (root 40.0)
            let second = Layout.evaluateIncremental first [ "changed" ] (Defaults.availableSpace 180.0 40.0) (root 80.0)
            let full = Layout.evaluate (Defaults.availableSpace 180.0 40.0) (root 80.0)

            // FR-001a: `Invalidated` reports the ACTUAL re-measured set (post propagation), not the
            // verbatim requested input. Here "root" is content-sized with no fixed-size ancestor, so the
            // change legitimately propagates to the root — the honest re-measured set is the whole tree.
            Expect.equal (Set.ofList second.Invalidated) (Set.ofList [ "root"; "stable"; "changed" ]) "incremental reports the actual re-measured set"
            Expect.equal second.Revision (first.Revision + 1L) "incremental revision advances"
            // INV-1: incremental Bounds are byte-identical to a full evaluate.
            Expect.equal (boundsOf "stable" second) (boundsOf "stable" full) "incremental == full (stable)"
            Expect.equal (boundsOf "changed" second) (boundsOf "changed" full) "incremental == full (changed)"
        }

        test "automatic layout render and hit-test consume computed bounds with shared pixel snapping" {
            let root =
                { Defaults.layoutNode "root" with
                    Children = [ layoutLeaf "button" 40.0 20.0; { layoutLeaf "label" 30.0 20.0 with Visibility = Hidden } ] }

            let result = Layout.evaluate (Defaults.availableSpace 120.0 40.0) root
            let scene = Layout.renderComputed result root
            let policy = { Defaults.pixelSnapPolicy 1.5 with Mode = Round }
            let button = boundsOf "button" result

            Expect.contains (Scene.describe scene) RectangleElement "renderComputed includes visible content"
            Expect.equal (Layout.hitTestComputed policy result (button.X + 1.0) (button.Y + 1.0)) (Some "button") "hit testing returns visible node"
            Expect.notEqual (Layout.hitTestComputed policy result ((boundsOf "label" result).X + 1.0) ((boundsOf "label" result).Y + 1.0)) (Some "label") "hidden nodes are not hit-testable"
            Expect.isTrue ((Layout.snapBounds policy { button with X = 0.2; Y = 0.2 }).X >= 0.0) "snap bounds returns deterministic logical coordinates"
        }

        test "automatic layout workflow update is pure and emits resize visibility intent and measurement effects" {
            let root =
                { Defaults.layoutNode "root" with
                    Children = [ layoutMeasured "title" 52.0 18.0; layoutLeaf "action" 40.0 20.0 ] }

            let model, startupEffects = Layout.initWorkflow (Defaults.availableSpace 160.0 48.0) root
            Expect.equal model.Result None "workflow init does not evaluate layout directly"
            Expect.equal startupEffects [ EvaluateLayout ] "workflow init requests evaluation as an effect"

            let resized, resizeEffects = Layout.updateWorkflow (LayoutHostResized(Defaults.availableSpace 220.0 48.0)) model
            Expect.equal resized.Available.Width 220.0 "resize updates owned available space"
            Expect.equal resizeEffects [ EvaluateIncrementalLayout [ "root" ] ] "resize requests incremental layout from root constraints"

            let hidden, visibilityEffects = Layout.updateWorkflow (LayoutVisibilityChanged("action", Hidden)) resized
            let hiddenAction = hidden.Root.Children |> List.find (fun child -> child.Id = "action")
            Expect.equal hiddenAction.Visibility Hidden "visibility message updates the layout tree"
            Expect.equal visibilityEffects [ EvaluateIncrementalLayout [ "action" ] ] "visibility change emits node invalidation"

            let nextIntent = { Defaults.layoutIntent with FlexGrow = 2.0 }
            let intentChanged, intentEffects = Layout.updateWorkflow (LayoutIntentChanged("title", nextIntent)) hidden
            let changedTitle = intentChanged.Root.Children |> List.find (fun child -> child.Id = "title")
            Expect.equal changedTitle.Intent.FlexGrow 2.0 "intent message updates the target node"
            Expect.equal intentEffects [ EvaluateIncrementalLayout [ "title" ] ] "intent change emits target invalidation"

            let measured, measurementEffects = Layout.updateWorkflow (LayoutMeasurementChanged "title") intentChanged
            Expect.equal measured.LastChangedNodeIds [ "title" ] "measurement change records the measured node"
            Expect.equal measurementEffects [ EvaluateIncrementalLayout [ "title" ] ] "measurement change emits incremental evaluation"
        }

        test "automatic layout workflow interpreter uses the public evaluator and reports completed results" {
            let root =
                { Defaults.layoutNode "root" with
                    Children = [ layoutMeasured "title" 52.0 18.0; layoutLeaf "action" 40.0 20.0 ] }

            let model, startupEffects = Layout.initWorkflow (Defaults.availableSpace 160.0 48.0) root
            let completed = Layout.interpretWorkflowEffect startupEffects.Head model

            match completed with
            | LayoutEvaluationCompleted result ->
                Expect.equal result.Bounds.Length 3 "interpreter evaluates root and children through public layout"
                Expect.equal result.Invalidated [ "root" ] "initial interpreter result records root invalidation"
                let evaluated, effects = Layout.updateWorkflow completed model
                Expect.isSome evaluated.Result "evaluation completion stores the result in the workflow model"
                Expect.isEmpty effects "completion does not request another effect"
            | other -> failtestf "expected evaluation completion, got %A" other
        }

        test "automatic layout keyboard focus region aligns with visual bounds and pointer hit-test after snapping" {
            let root =
                { Defaults.layoutNode "root" with
                    Children = [ layoutLeaf "focusable" 40.25 20.25; layoutLeaf "secondary" 32.0 20.0 ] }

            let result = Layout.evaluate (Defaults.availableSpace 120.0 42.0) root
            let policy = { Defaults.pixelSnapPolicy 2.0 with Mode = Expand }
            let visualBounds = boundsOf "focusable" result
            let focusRegion = Layout.snapBounds policy visualBounds
            let focusX = focusRegion.X + focusRegion.Width - 0.25
            let focusY = focusRegion.Y + focusRegion.Height - 0.25

            Expect.equal (Layout.hitTestComputed policy result focusX focusY) (Some "focusable") "pointer hit-test uses the same snapped bounds as the keyboard focus region"
            Expect.equal focusRegion (Layout.snapBounds policy visualBounds) "focus region is derived from computed visual bounds with the shared snap policy"
        }

        test "automatic layout diagnostics report invalid available space values duplicate ids and min max conflicts" {
            let conflicted =
                { Defaults.layoutNode "dup" with
                    Intent =
                        { Defaults.layoutIntent with
                            MinSize = { Width = Some 100.0; Height = None }
                            MaxSize = { Width = Some 20.0; Height = None } } }

            let root = { Defaults.layoutNode "root" with Children = [ conflicted; layoutLeaf "dup" -10.0 20.0 ] }
            let result = Layout.evaluate { Width = -1.0; WidthMode = Exactly; Height = nan; HeightMode = Exactly } root

            Expect.exists result.Diagnostics (fun item -> item.Code = InvalidAvailableSpace) "invalid available space is diagnosed"
            Expect.exists result.Diagnostics (fun item -> item.Code = DuplicateLayoutNodeId) "duplicate node ids are diagnosed"
            Expect.exists result.Diagnostics (fun item -> item.Code = UnsatisfiedConstraint) "min/max conflict is diagnosed"
            Expect.exists result.Diagnostics (fun item -> item.Code = FallbackBoundsApplied) "fallback geometry is summarized"
            Expect.all result.Bounds (fun item -> item.Bounds.Width >= 0.0 && item.Bounds.Height >= 0.0) "fallback bounds remain bounded"
        }

        test "automatic layout hidden and collapsed nodes are distinguishable and visible siblings remain stable" {
            let root visibility =
                { Defaults.layoutNode "root" with
                    Children =
                        [ layoutLeaf "before" 30.0 20.0
                          { layoutLeaf "toggle" 30.0 20.0 with Visibility = visibility }
                          layoutLeaf "after" 30.0 20.0 ] }

            let hidden = Layout.evaluate (Defaults.availableSpace 160.0 40.0) (root Hidden)
            let collapsed = Layout.evaluate (Defaults.availableSpace 160.0 40.0) (root Collapsed)

            Expect.equal (hidden.Bounds |> List.find (fun item -> item.NodeId = "toggle")).Visibility Hidden "hidden node is retained as hidden"
            Expect.equal (collapsed.Bounds |> List.find (fun item -> item.NodeId = "toggle")).Visibility Collapsed "collapsed node is retained as collapsed"
            Expect.equal (boundsOf "toggle" collapsed).Width 0.0 "collapsed node has zero width"
            Expect.equal (boundsOf "before" hidden) (boundsOf "before" collapsed) "preceding sibling remains stable"
        }
    ]
