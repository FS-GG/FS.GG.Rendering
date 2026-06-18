module Feature151CorpusFixtures

open System
open FS.GG.UI.Layout

type LayoutCase =
    { CaseId: string
      Category: string
      Root: LayoutNode
      Available: AvailableSpace
      RequiredNodeIds: LayoutNodeId list
      ExpectedDiagnosticCodes: LayoutDiagnosticCode list
      ChangedRoot: LayoutNode option
      ChangedNodeIds: LayoutNodeId list }

let measuredLeaf id width height =
    let measure _ =
        { Width = width
          Height = height
          Diagnostics = [] }

    { Defaults.layoutNode id with
        Intent =
            { Defaults.layoutIntent with
                Size = { Width = Some width; Height = Some height } }
        Measure = Some measure }

let container id width height children =
    { Defaults.layoutNode id with
        Intent =
            { Defaults.layoutIntent with
                Direction = Column
                Size = { Width = Some width; Height = Some height }
                AlignItems = Stretch
                Gap = { Row = 4.0; Column = 0.0 } }
        Children = children }

let rowContainer id width height children =
    { Defaults.layoutNode id with
        Intent =
            { Defaults.layoutIntent with
                Direction = Row
                Size = { Width = Some width; Height = Some height }
                Gap = { Row = 0.0; Column = 4.0 } }
        Children = children }

let available width height = Defaults.availableSpace width height

let finiteRoot =
    container "finite-root" 160.0 120.0 [ measuredLeaf "header" 120.0 24.0; measuredLeaf "body" 140.0 72.0 ]

let zeroRoot =
    container "zero-root" 0.0 0.0 [ measuredLeaf "zero-child" 12.0 12.0 ]

let verySmallRoot =
    container "very-small-root" 12.0 8.0 [ measuredLeaf "tiny-a" 8.0 4.0; measuredLeaf "tiny-b" 8.0 4.0 ]

let veryLargeRoot =
    rowContainer "very-large-root" 4096.0 2048.0 [ measuredLeaf "large-a" 512.0 256.0; measuredLeaf "large-b" 768.0 256.0 ]

let measuredLeaves =
    container "measured-leaves" 180.0 160.0 [ measuredLeaf "measured-a" 72.0 18.0; measuredLeaf "measured-b" 96.0 24.0 ]

let emptyContainer =
    container "empty-container" 90.0 70.0 []

let singleChild =
    container "single-child" 90.0 70.0 [ measuredLeaf "only-child" 40.0 24.0 ]

let deepNesting =
    container
        "deep-root"
        180.0
        160.0
        [ container "level-1" 150.0 120.0 [ container "level-2" 120.0 80.0 [ measuredLeaf "level-3" 64.0 32.0 ] ] ]

let dynamicContent height =
    container "dynamic-root" 180.0 160.0 [ measuredLeaf "dynamic-a" 72.0 height; measuredLeaf "dynamic-b" 48.0 24.0 ]

let childSet includeExtra =
    let children =
        [ measuredLeaf "child-a" 40.0 20.0
          if includeExtra then
              measuredLeaf "child-extra" 32.0 20.0
          measuredLeaf "child-b" 40.0 20.0 ]

    rowContainer "child-set-root" 180.0 60.0 children

let childOrder reversed =
    let children = [ measuredLeaf "order-a" 40.0 20.0; measuredLeaf "order-b" 50.0 20.0 ]
    rowContainer "child-order-root" 180.0 60.0 (if reversed then List.rev children else children)

let visibilityRoot =
    container
        "visibility-root"
        160.0
        120.0
        [ measuredLeaf "visible-child" 50.0 20.0
          { measuredLeaf "hidden-child" 50.0 20.0 with Visibility = Hidden }
          { measuredLeaf "collapsed-child" 50.0 20.0 with Visibility = Collapsed } ]

let invalidAvailableRoot =
    container "invalid-available-root" 90.0 70.0 [ measuredLeaf "invalid-child" 40.0 20.0 ]

let contradictoryRoot =
    { measuredLeaf "contradictory-size" 80.0 40.0 with
        Intent =
            { Defaults.layoutIntent with
                Size = { Width = Some 80.0; Height = Some 40.0 }
                MinSize = { Width = Some 120.0; Height = None }
                MaxSize = { Width = Some 40.0; Height = None } } }

let duplicateRoot =
    container "duplicate-root" 120.0 90.0 [ measuredLeaf "dup" 40.0 20.0; measuredLeaf "dup" 50.0 20.0 ]

let allCases =
    [ { CaseId = "finite-root"
        Category = "constrained root"
        Root = finiteRoot
        Available = available 160.0 120.0
        RequiredNodeIds = [ "finite-root"; "header"; "body" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "zero-root"
        Category = "zero constraint"
        Root = zeroRoot
        Available = available 0.0 0.0
        RequiredNodeIds = [ "zero-root"; "zero-child" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "very-small-root"
        Category = "small constraint"
        Root = verySmallRoot
        Available = available 12.0 8.0
        RequiredNodeIds = [ "very-small-root"; "tiny-a"; "tiny-b" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "very-large-root"
        Category = "large constraint"
        Root = veryLargeRoot
        Available = available 4096.0 2048.0
        RequiredNodeIds = [ "very-large-root"; "large-a"; "large-b" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "measured-leaves"
        Category = "measured leaves"
        Root = measuredLeaves
        Available = available 180.0 160.0
        RequiredNodeIds = [ "measured-leaves"; "measured-a"; "measured-b" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = Some(container "measured-leaves" 180.0 160.0 [ measuredLeaf "measured-a" 88.0 18.0; measuredLeaf "measured-b" 96.0 24.0 ])
        ChangedNodeIds = [ "measured-a" ] }
      { CaseId = "empty-container"
        Category = "empty container"
        Root = emptyContainer
        Available = available 90.0 70.0
        RequiredNodeIds = [ "empty-container" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "single-child"
        Category = "single child"
        Root = singleChild
        Available = available 90.0 70.0
        RequiredNodeIds = [ "single-child"; "only-child" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "deep-nesting"
        Category = "deep nesting"
        Root = deepNesting
        Available = available 180.0 160.0
        RequiredNodeIds = [ "deep-root"; "level-1"; "level-2"; "level-3" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "dynamic-content"
        Category = "dynamic content"
        Root = dynamicContent 24.0
        Available = available 180.0 160.0
        RequiredNodeIds = [ "dynamic-root"; "dynamic-a"; "dynamic-b" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = Some(dynamicContent 48.0)
        ChangedNodeIds = [ "dynamic-a" ] }
      { CaseId = "child-insert-remove"
        Category = "child insertion removal"
        Root = childSet false
        Available = available 180.0 60.0
        RequiredNodeIds = [ "child-set-root"; "child-a"; "child-b" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = Some(childSet true)
        ChangedNodeIds = [ "child-set-root" ] }
      { CaseId = "child-reorder"
        Category = "child reorder"
        Root = childOrder false
        Available = available 180.0 60.0
        RequiredNodeIds = [ "child-order-root"; "order-a"; "order-b" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = Some(childOrder true)
        ChangedNodeIds = [ "child-order-root" ] }
      { CaseId = "visibility-change"
        Category = "visibility"
        Root = visibilityRoot
        Available = available 160.0 120.0
        RequiredNodeIds = [ "visibility-root"; "visible-child"; "hidden-child"; "collapsed-child" ]
        ExpectedDiagnosticCodes = []
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "invalid-available"
        Category = "invalid constraints"
        Root = invalidAvailableRoot
        Available = { Width = Double.NaN; WidthMode = Exactly; Height = -1.0; HeightMode = Exactly }
        RequiredNodeIds = [ "invalid-available-root"; "invalid-child" ]
        ExpectedDiagnosticCodes = [ InvalidAvailableSpace ]
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "contradictory-size"
        Category = "contradictory constraints"
        Root = contradictoryRoot
        Available = available 180.0 100.0
        RequiredNodeIds = [ "contradictory-size" ]
        ExpectedDiagnosticCodes = [ UnsatisfiedConstraint ]
        ChangedRoot = None
        ChangedNodeIds = [] }
      { CaseId = "duplicate-node"
        Category = "diagnostic"
        Root = duplicateRoot
        Available = available 120.0 90.0
        RequiredNodeIds = [ "duplicate-root"; "dup" ]
        ExpectedDiagnosticCodes = [ DuplicateLayoutNodeId; DuplicateMeasurement ]
        ChangedRoot = None
        ChangedNodeIds = [] } ]

let acceptedCases =
    allCases |> List.filter (fun item -> List.isEmpty item.ExpectedDiagnosticCodes)

let resultOf (item: LayoutCase) = Layout.evaluate item.Available item.Root

let boundsOf (nodeId: LayoutNodeId) (result: LayoutResult) =
    result.Bounds |> List.find (fun item -> item.NodeId = nodeId) |> fun item -> item.Bounds

let diagnosticCodes (result: LayoutResult) =
    result.Diagnostics |> List.map (fun item -> item.Code) |> Set.ofList

let finiteBounds (bounds: LayoutBounds) =
    [ bounds.X; bounds.Y; bounds.Width; bounds.Height ]
    |> List.forall (fun value -> Double.IsFinite value && value >= 0.0)

let constraintsFor (item: LayoutCase) =
    Layout.constraintsFromAvailable Viewport item.Available
