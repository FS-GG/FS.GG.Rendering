namespace FS.GG.UI.Layout

open System
open Facebook.Yoga
open FS.GG.UI.Scene

module Layout =
    // R2: Yoga's internal pixel-grid rounding snaps each node using its UNROUNDED absolute position
    // as context. A partial subtree relayout only knows the boundary's ROUNDED cached origin, so a
    // flex-distributed fractional size rounds a pixel differently than a full evaluate — breaking the
    // incremental==full equivalence invariant (INV-1). Disabling Yoga's internal rounding makes layout
    // exact-float and position-independent, so a re-rooted subtree is byte-identical to the full tree.
    // Explicit pixel snapping remains available, unchanged, via the separate `snapBounds`/PixelSnapPolicy.
    // Maintainer-approved (rationale recorded by feature 102, R8): the blast-radius is nil — the Controls
    // layer uses integer geometry, so disabling Yoga's sub-pixel rounding leaves Controls bounds unaffected;
    // the change buys the INV-1 equivalence above at no observable cost.
    let private yogaConfig =
        let c = YGConfigAPI.YGConfigNew()
        YGConfigAPI.YGConfigSetPointScaleFactor(c, 0.0f)
        c

    let finite value = Double.IsFinite value

    let nonNegative value = finite value && value >= 0.0

    let clampNonNegative value =
        if nonNegative value then value else 0.0

    let diagnostic nodeId code severity message constraintName fallbackApplied =
        { NodeId = nodeId
          Code = code
          Severity = severity
          Message = message
          Constraint = constraintName
          FallbackApplied = fallbackApplied }

    let normalizeDimension nodeId name value =
        match value with
        | Some value when nonNegative value -> value, []
        | Some value ->
            0.0,
            [ diagnostic
                  nodeId
                  InvalidLayoutValue
                  FS.GG.UI.Layout.DiagnosticSeverity.Warning
                  $"Invalid {name} value '{value}' was normalized to 0."
                  (Some name)
                  true ]
        | None -> 0.0, []

    let normalizeOptionalDimension nodeId name value =
        match value with
        | Some value when nonNegative value -> Some value, []
        | Some value ->
            Some 0.0,
            [ diagnostic
                  nodeId
                  InvalidLayoutValue
                  FS.GG.UI.Layout.DiagnosticSeverity.Warning
                  $"Invalid {name} value '{value}' was normalized to 0."
                  (Some name)
                  true ]
        | None -> None, []

    let normalizePadding nodeId (padding: LayoutPadding) =
        let left, leftDiagnostics = normalizeDimension nodeId "padding-left" (Some padding.Left)
        let top, topDiagnostics = normalizeDimension nodeId "padding-top" (Some padding.Top)
        let right, rightDiagnostics = normalizeDimension nodeId "padding-right" (Some padding.Right)
        let bottom, bottomDiagnostics = normalizeDimension nodeId "padding-bottom" (Some padding.Bottom)

        { Left = left
          Top = top
          Right = right
          Bottom = bottom },
        leftDiagnostics @ topDiagnostics @ rightDiagnostics @ bottomDiagnostics

    let normalizeGap nodeId (gap: LayoutGap) =
        let row, rowDiagnostics = normalizeDimension nodeId "row-gap" (Some gap.Row)
        let column, columnDiagnostics = normalizeDimension nodeId "column-gap" (Some gap.Column)
        { Row = row; Column = column }, rowDiagnostics @ columnDiagnostics

    let normalizeAvailable (available: AvailableSpace) =
        let width =
            if nonNegative available.Width then
                available.Width
            else
                0.0

        let height =
            if nonNegative available.Height then
                available.Height
            else
                0.0

        let diagnostics =
            [ if not (nonNegative available.Width) then
                  diagnostic None InvalidAvailableSpace FS.GG.UI.Layout.DiagnosticSeverity.Error "Invalid available width was normalized to 0." (Some "available-width") true
              if not (nonNegative available.Height) then
                  diagnostic None InvalidAvailableSpace FS.GG.UI.Layout.DiagnosticSeverity.Error "Invalid available height was normalized to 0." (Some "available-height") true ]

        { available with Width = width; Height = height }, diagnostics

    let validateTree (root: LayoutNode) =
        let rec collect path (node: LayoutNode) =
            let own =
                if String.IsNullOrWhiteSpace node.Id then
                    [ diagnostic None InvalidLayoutValue FS.GG.UI.Layout.DiagnosticSeverity.Error $"Layout node at {path} has an empty id." (Some "node-id") true ]
                else
                    []

            own @ (node.Children |> List.mapi (fun index child -> collect $"{path}/{index}" child) |> List.concat)

        let ids =
            let rec loop (node: LayoutNode) =
                node.Id :: (node.Children |> List.collect loop)

            loop root

        let duplicateDiagnostics =
            ids
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> List.countBy id
            |> List.choose (fun (nodeId, count) ->
                if count > 1 then
                    Some(diagnostic (Some nodeId) DuplicateLayoutNodeId FS.GG.UI.Layout.DiagnosticSeverity.Error $"Duplicate layout node id '{nodeId}' appears {count} times." (Some "node-id") true)
                else
                    None)

        collect "root" root @ duplicateDiagnostics

    let constrain nodeId requested minSize maxSize axis =
        let minValue, minDiagnostics = normalizeOptionalDimension nodeId $"min-{axis}" minSize
        let maxValue, maxDiagnostics = normalizeOptionalDimension nodeId $"max-{axis}" maxSize

        let conflictDiagnostics =
            match minValue, maxValue with
            | Some minValue, Some maxValue when minValue > maxValue ->
                [ diagnostic nodeId UnsatisfiedConstraint FS.GG.UI.Layout.DiagnosticSeverity.Warning $"Minimum {axis} exceeds maximum {axis}; maximum was used." (Some axis) true ]
            | _ -> []

        let bounded =
            let afterMin =
                match minValue with
                | Some value -> max value requested
                | None -> requested

            match maxValue with
            | Some value -> min value afterMin
            | None -> afterMin

        max 0.0 bounded, minDiagnostics @ maxDiagnostics @ conflictDiagnostics

    let measureLeaf nodeId availableWidth availableHeight (measure: ContentMeasure option) =
        match measure with
        | None -> 0.0, 0.0, []
        | Some measure ->
            let response =
                measure
                    { AvailableWidth = max 0.0 availableWidth
                      WidthMode = FS.GG.UI.Layout.MeasureMode.AtMost
                      AvailableHeight = max 0.0 availableHeight
                      HeightMode = FS.GG.UI.Layout.MeasureMode.AtMost }

            let diagnostics = response.Diagnostics

            if nonNegative response.Width && nonNegative response.Height then
                response.Width, response.Height, diagnostics
            else
                0.0,
                0.0,
                diagnostics
                @ [ diagnostic nodeId UnmeasurableContent FS.GG.UI.Layout.DiagnosticSeverity.Warning "Invalid measurement output was normalized to 0x0." (Some "measure") true ]

    let preferredMainSize isRow availableMain (node: LayoutNode) =
        let explicit =
            if isRow then
                node.Intent.Size.Width
            else
                node.Intent.Size.Height

        match node.Visibility, explicit, node.Intent.FlexBasis with
        | Collapsed, _, _ -> 0.0
        | _, Some value, _ when nonNegative value -> value
        | _, _, Some value when nonNegative value -> value
        | _ ->
            let measuredWidth, measuredHeight, _ = measureLeaf (Some node.Id) availableMain availableMain node.Measure
            if isRow then measuredWidth else measuredHeight

    let alignOffset align available childSize =
        match align with
        | LayoutAlign.Center -> max 0.0 ((available - childSize) / 2.0)
        | LayoutAlign.End -> max 0.0 (available - childSize)
        | _ -> 0.0

    let rec layoutNode (bounds: LayoutBounds) (node: LayoutNode) =
        let padding, paddingDiagnostics = normalizePadding (Some node.Id) node.Intent.Padding
        let gap, gapDiagnostics = normalizeGap (Some node.Id) node.Intent.Gap
        let margin, marginDiagnostics = normalizePadding (Some node.Id) node.Intent.Margin

        let widthFromIntent, widthDiagnostics =
            match node.Intent.Size.Width with
            | Some value -> normalizeDimension (Some node.Id) "width" (Some value)
            | None -> bounds.Width, []

        let heightFromIntent, heightDiagnostics =
            match node.Intent.Size.Height with
            | Some value -> normalizeDimension (Some node.Id) "height" (Some value)
            | None -> bounds.Height, []

        let width, minMaxWidthDiagnostics = constrain (Some node.Id) widthFromIntent node.Intent.MinSize.Width node.Intent.MaxSize.Width "width"
        let height, minMaxHeightDiagnostics = constrain (Some node.Id) heightFromIntent node.Intent.MinSize.Height node.Intent.MaxSize.Height "height"

        let ownBounds: LayoutBounds =
            match node.Visibility with
            | Collapsed ->
                { X = bounds.X + margin.Left
                  Y = bounds.Y + margin.Top
                  Width = 0.0
                  Height = 0.0 }
            | _ ->
                { X = bounds.X + margin.Left
                  Y = bounds.Y + margin.Top
                  Width = max 0.0 (width - margin.Left - margin.Right)
                  Height = max 0.0 (height - margin.Top - margin.Bottom) }

        let own: ComputedBounds = { NodeId = node.Id; Bounds = ownBounds; Visibility = node.Visibility }
        let diagnostics = paddingDiagnostics @ gapDiagnostics @ marginDiagnostics @ widthDiagnostics @ heightDiagnostics @ minMaxWidthDiagnostics @ minMaxHeightDiagnostics

        if node.Visibility = Collapsed || List.isEmpty node.Children then
            [ own ], diagnostics
        else
            let inner: LayoutBounds =
                { X = ownBounds.X + padding.Left
                  Y = ownBounds.Y + padding.Top
                  Width = max 0.0 (ownBounds.Width - padding.Left - padding.Right)
                  Height = max 0.0 (ownBounds.Height - padding.Top - padding.Bottom) }

            let children = node.Children
            let isRow = node.Intent.Direction = LayoutDirection.Row
            let mainAvailable = if isRow then inner.Width else inner.Height
            let crossAvailable = if isRow then inner.Height else inner.Width
            let mainGap = if isRow then gap.Column else gap.Row
            let crossGap = if isRow then gap.Row else gap.Column

            let childDescriptors =
                children
                |> List.map (fun child ->
                    let basis = preferredMainSize isRow mainAvailable child
                    let grow = if nonNegative child.Intent.FlexGrow then child.Intent.FlexGrow else 0.0
                    let shrink = if nonNegative child.Intent.FlexShrink then child.Intent.FlexShrink else 1.0
                    child, basis, grow, shrink)

            let totalBasis = childDescriptors |> List.sumBy (fun (_, basis, _, _) -> basis)
            let totalGap = mainGap * float (max 0 (children.Length - 1))
            let remaining = mainAvailable - totalBasis - totalGap
            let totalGrow = childDescriptors |> List.sumBy (fun (_, _, grow, _) -> grow)
            let totalShrink = childDescriptors |> List.sumBy (fun (_, _, _, shrink) -> shrink)

            let mainSizes =
                childDescriptors
                |> List.map (fun (child, basis, grow, shrink) ->
                    let adjusted =
                        if node.Intent.Wrap = LayoutWrap.Wrap then
                            basis
                        elif remaining > 0.0 && totalGrow > 0.0 then
                            basis + remaining * grow / totalGrow
                        elif remaining < 0.0 && totalShrink > 0.0 then
                            basis + remaining * shrink / totalShrink
                        elif remaining > 0.0 && basis = 0.0 && totalGrow = 0.0 then
                            basis
                        elif basis = 0.0 && children.Length > 0 then
                            max 0.0 ((mainAvailable - totalGap) / float children.Length)
                        else
                            basis

                    let axis = if isRow then "width" else "height"
                    let minValue = if isRow then child.Intent.MinSize.Width else child.Intent.MinSize.Height
                    let maxValue = if isRow then child.Intent.MaxSize.Width else child.Intent.MaxSize.Height
                    let constrained, constrainedDiagnostics = constrain (Some child.Id) adjusted minValue maxValue axis
                    child, constrained, constrainedDiagnostics)

            let wrapLines =
                if node.Intent.Wrap = LayoutWrap.Wrap then
                    (([], [], 0.0), mainSizes)
                    ||> List.fold (fun (lines, current, used) (child, size, childDiagnostics) ->
                        let nextUsed = if List.isEmpty current then size else used + mainGap + size
                        if not (List.isEmpty current) && nextUsed > mainAvailable then
                            ((List.rev current) :: lines, [ child, size, childDiagnostics ], size)
                        else
                            (lines, (child, size, childDiagnostics) :: current, nextUsed))
                    |> fun (lines, current, _) -> List.rev ((List.rev current) :: lines |> List.filter (List.isEmpty >> not))
                else
                    [ mainSizes ]

            let childResults, childDiagnostics, _ =
                (([], diagnostics, 0.0), wrapLines)
                ||> List.fold (fun (allBounds, allDiagnostics, crossOffset) line ->
                    let lineMain =
                        line |> List.sumBy (fun (_, size, _) -> size)

                    let lineCross =
                        if List.isEmpty line then 0.0 else max 0.0 ((crossAvailable - crossGap * float (wrapLines.Length - 1)) / float wrapLines.Length)

                    let startMain =
                        match node.Intent.JustifyContent with
                        | LayoutAlign.Center -> max 0.0 ((mainAvailable - lineMain - mainGap * float (max 0 (line.Length - 1))) / 2.0)
                        | LayoutAlign.End -> max 0.0 (mainAvailable - lineMain - mainGap * float (max 0 (line.Length - 1)))
                        | _ -> 0.0

                    let _, lineBounds, lineDiagnostics =
                        ((startMain, [], allDiagnostics), line)
                        ||> List.fold (fun (mainOffset, boundsAcc, diagnosticsAcc) (child, mainSize, childSizeDiagnostics) ->
                            let measuredWidth, measuredHeight, measureDiagnostics = measureLeaf (Some child.Id) mainSize lineCross child.Measure
                            let explicitCross = if isRow then child.Intent.Size.Height else child.Intent.Size.Width
                            let measuredCross = if isRow then measuredHeight else measuredWidth
                            let crossSize =
                                match child.Visibility, (child.Intent.AlignSelf |> Option.defaultValue node.Intent.AlignItems), explicitCross with
                                | Collapsed, _, _ -> 0.0
                                | _, LayoutAlign.Stretch, None -> lineCross
                                | _, _, Some value when nonNegative value -> min lineCross value
                                | _ -> if measuredCross > 0.0 then min lineCross measuredCross else lineCross

                            let crossAlign = child.Intent.AlignSelf |> Option.defaultValue node.Intent.AlignItems
                            let crossPosition = crossOffset + alignOffset crossAlign lineCross crossSize
                            let childBounds: LayoutBounds =
                                if isRow then
                                    { X = inner.X + mainOffset
                                      Y = inner.Y + crossPosition
                                      Width = mainSize
                                      Height = crossSize }
                                else
                                    { X = inner.X + crossPosition
                                      Y = inner.Y + mainOffset
                                      Width = crossSize
                                      Height = mainSize }

                            let childComputed, childLayoutDiagnostics = layoutNode childBounds child
                            mainOffset + mainSize + mainGap, boundsAcc @ childComputed, diagnosticsAcc @ childSizeDiagnostics @ measureDiagnostics @ childLayoutDiagnostics)

                    allBounds @ lineBounds, lineDiagnostics, crossOffset + lineCross + crossGap)

            own :: childResults, childDiagnostics

    let yogaAlign align =
        match align with
        | LayoutAlign.Auto -> YGAlign.Auto
        | LayoutAlign.Start -> YGAlign.FlexStart
        | LayoutAlign.Center -> YGAlign.Center
        | LayoutAlign.End -> YGAlign.FlexEnd
        | LayoutAlign.Stretch -> YGAlign.Stretch
        | LayoutAlign.SpaceBetween -> YGAlign.SpaceBetween
        | LayoutAlign.SpaceAround -> YGAlign.SpaceAround
        | LayoutAlign.SpaceEvenly -> YGAlign.SpaceEvenly

    let yogaJustify align =
        match align with
        | LayoutAlign.Center -> YGJustify.Center
        | LayoutAlign.End -> YGJustify.FlexEnd
        | LayoutAlign.SpaceBetween -> YGJustify.SpaceBetween
        | LayoutAlign.SpaceAround -> YGJustify.SpaceAround
        | LayoutAlign.SpaceEvenly -> YGJustify.SpaceEvenly
        | _ -> YGJustify.FlexStart

    let yogaMeasureMode mode =
        match mode with
        | Facebook.Yoga.MeasureMode.Exactly -> FS.GG.UI.Layout.MeasureMode.Exactly
        | Facebook.Yoga.MeasureMode.AtMost -> FS.GG.UI.Layout.MeasureMode.AtMost
        | _ -> FS.GG.UI.Layout.MeasureMode.Undefined

    let setOptional value apply =
        match value with
        | Some value when nonNegative value -> apply (single value)
        | _ -> ()

    let applyYogaStyle (yogaNode: Node) (node: LayoutNode) =
        YGNodeStyleAPI.YGNodeStyleSetFlexDirection(
            yogaNode,
            if node.Intent.Direction = LayoutDirection.Row then
                YGFlexDirection.Row
            else
                YGFlexDirection.Column
        )

        YGNodeStyleAPI.YGNodeStyleSetFlexWrap(
            yogaNode,
            if node.Intent.Wrap = LayoutWrap.Wrap then
                YGWrap.Wrap
            else
                YGWrap.NoWrap
        )

        YGNodeStyleAPI.YGNodeStyleSetAlignItems(yogaNode, yogaAlign node.Intent.AlignItems)
        node.Intent.AlignSelf |> Option.iter (fun align -> YGNodeStyleAPI.YGNodeStyleSetAlignSelf(yogaNode, yogaAlign align))
        YGNodeStyleAPI.YGNodeStyleSetJustifyContent(yogaNode, yogaJustify node.Intent.JustifyContent)
        YGNodeStyleAPI.YGNodeStyleSetDisplay(yogaNode, if node.Visibility = Collapsed then YGDisplay.None else YGDisplay.Flex)
        YGNodeStyleAPI.YGNodeStyleSetPadding(yogaNode, YGEdge.Left, single (clampNonNegative node.Intent.Padding.Left))
        YGNodeStyleAPI.YGNodeStyleSetPadding(yogaNode, YGEdge.Top, single (clampNonNegative node.Intent.Padding.Top))
        YGNodeStyleAPI.YGNodeStyleSetPadding(yogaNode, YGEdge.Right, single (clampNonNegative node.Intent.Padding.Right))
        YGNodeStyleAPI.YGNodeStyleSetPadding(yogaNode, YGEdge.Bottom, single (clampNonNegative node.Intent.Padding.Bottom))
        YGNodeStyleAPI.YGNodeStyleSetMargin(yogaNode, YGEdge.Left, single (clampNonNegative node.Intent.Margin.Left))
        YGNodeStyleAPI.YGNodeStyleSetMargin(yogaNode, YGEdge.Top, single (clampNonNegative node.Intent.Margin.Top))
        YGNodeStyleAPI.YGNodeStyleSetMargin(yogaNode, YGEdge.Right, single (clampNonNegative node.Intent.Margin.Right))
        YGNodeStyleAPI.YGNodeStyleSetMargin(yogaNode, YGEdge.Bottom, single (clampNonNegative node.Intent.Margin.Bottom))
        YGNodeStyleAPI.YGNodeStyleSetGap(yogaNode, YGGutter.Row, single (clampNonNegative node.Intent.Gap.Row))
        YGNodeStyleAPI.YGNodeStyleSetGap(yogaNode, YGGutter.Column, single (clampNonNegative node.Intent.Gap.Column))
        setOptional node.Intent.Size.Width (fun value -> YGNodeStyleAPI.YGNodeStyleSetWidth(yogaNode, value))
        setOptional node.Intent.Size.Height (fun value -> YGNodeStyleAPI.YGNodeStyleSetHeight(yogaNode, value))
        setOptional node.Intent.MinSize.Width (fun value -> YGNodeStyleAPI.YGNodeStyleSetMinWidth(yogaNode, value))
        setOptional node.Intent.MinSize.Height (fun value -> YGNodeStyleAPI.YGNodeStyleSetMinHeight(yogaNode, value))
        setOptional node.Intent.MaxSize.Width (fun value -> YGNodeStyleAPI.YGNodeStyleSetMaxWidth(yogaNode, value))
        setOptional node.Intent.MaxSize.Height (fun value -> YGNodeStyleAPI.YGNodeStyleSetMaxHeight(yogaNode, value))
        YGNodeStyleAPI.YGNodeStyleSetFlexGrow(yogaNode, single (clampNonNegative node.Intent.FlexGrow))
        YGNodeStyleAPI.YGNodeStyleSetFlexShrink(yogaNode, single (clampNonNegative node.Intent.FlexShrink))
        node.Intent.FlexBasis |> Option.iter (fun basis -> if nonNegative basis then YGNodeStyleAPI.YGNodeStyleSetFlexBasis(yogaNode, single basis))

    let yogaFailureInjectionEnabled () =
        let mutable enabled = false
        AppContext.TryGetSwitch("FS.GG.UI.Layout.ForceYogaFailure", &enabled) && enabled

    let tryYogaLayout (available: AvailableSpace) (pin: LayoutBounds option) (root: LayoutNode) =
        let measurementDiagnostics = ResizeArray<LayoutDiagnostic>()
        let nodePairs = ResizeArray<LayoutNode * Node>()

        let rec createNode (node: LayoutNode) =
            let yogaNode = YGNodeAPI.YGNodeNewWithConfig(yogaConfig)
            nodePairs.Add(node, yogaNode)
            applyYogaStyle yogaNode node

            match node.Measure, node.Children with
            | Some measure, [] ->
                let callback =
                    YGMeasureFunc(fun _ width widthMode height heightMode ->
                        let response =
                            measure
                                { AvailableWidth = float width
                                  WidthMode = yogaMeasureMode widthMode
                                  AvailableHeight = float height
                                  HeightMode = yogaMeasureMode heightMode }

                        measurementDiagnostics.AddRange(response.Diagnostics)

                        if nonNegative response.Width && nonNegative response.Height then
                            YGSize(Width = single response.Width, Height = single response.Height)
                        else
                            measurementDiagnostics.Add(
                                diagnostic
                                    (Some node.Id)
                                    UnmeasurableContent
                                    FS.GG.UI.Layout.DiagnosticSeverity.Warning
                                    "Invalid measurement output was normalized to 0x0."
                                    (Some "measure")
                                    true
                            )

                            YGSize(Width = 0.0f, Height = 0.0f))

                YGNodeAPI.YGNodeSetMeasureFunc(yogaNode, callback)
            | _ -> ()

            node.Children
            |> List.iteri (fun index child ->
                let childNode = createNode child
                YGNodeAPI.YGNodeInsertChild(yogaNode, childNode, unativeint index))

            yogaNode

        let mutable rootYoga = Unchecked.defaultof<Node>
        let mutable rootCreated = false

        try
            // Test-only diagnostic switch used to exercise the recoverable Yoga fallback path without changing the public API.
            if yogaFailureInjectionEnabled () then
                invalidOp "Forced Yoga execution failure."

            rootYoga <- createNode root
            rootCreated <- true

            match pin with
            | None ->
                YGNodeStyleAPI.YGNodeStyleSetWidth(rootYoga, single available.Width)
                YGNodeStyleAPI.YGNodeStyleSetHeight(rootYoga, single available.Height)
                YGNodeAPI.YGNodeCalculateLayout(rootYoga, single available.Width, single available.Height, YGDirection.LTR)
            | Some(cached: LayoutBounds) ->
                // Incremental subtree relayout (R2): pin the boundary to its cached (content-independent)
                // size and lay its descendants out within it. With Yoga's internal pixel rounding
                // disabled (see `yogaConfig`), flex distribution is exact-float and position-independent,
                // so a re-rooted subtree's relative geometry is byte-identical to the full tree's — the
                // cached size round-trips through float32 exactly, giving identical child layout (INV-1).
                YGNodeStyleAPI.YGNodeStyleSetWidth(rootYoga, single cached.Width)
                YGNodeStyleAPI.YGNodeStyleSetHeight(rootYoga, single cached.Height)
                YGNodeAPI.YGNodeCalculateLayout(rootYoga, single cached.Width, single cached.Height, YGDirection.LTR)

            let rec read absoluteX absoluteY (node: LayoutNode) (yogaNode: Node) =
                let x = absoluteX + float (YGNodeLayoutAPI.YGNodeLayoutGetLeft yogaNode)
                let y = absoluteY + float (YGNodeLayoutAPI.YGNodeLayoutGetTop yogaNode)
                let own =
                    { NodeId = node.Id
                      Bounds =
                        { X = x
                          Y = y
                          Width = max 0.0 (float (YGNodeLayoutAPI.YGNodeLayoutGetWidth yogaNode))
                          Height = max 0.0 (float (YGNodeLayoutAPI.YGNodeLayoutGetHeight yogaNode)) }
                      Visibility = node.Visibility }

                let children =
                    node.Children
                    |> List.mapi (fun index child ->
                        match YGNodeAPI.YGNodeGetChild(yogaNode, unativeint index) with
                        | null -> invalidOp $"Yoga did not return layout child {index} for node '{node.Id}'."
                        | childYoga -> read x y child childYoga)
                    |> List.concat

                own :: children

            let bounds =
                match pin with
                | None -> read 0.0 0.0 root rootYoga
                | Some(cached: LayoutBounds) ->
                    // Incremental subtree relayout (R2): the boundary's own box is PINNED to its
                    // cached value (its size is content-independent, so an interior change cannot move
                    // it) and its descendants are read at the boundary's cached border-box origin —
                    // the SAME left-associated position accumulation `evaluate` performs from the true
                    // root — so every computed bound is byte-identical to a full `evaluate` (INV-1).
                    let own =
                        { NodeId = root.Id
                          Bounds = cached
                          Visibility = root.Visibility }
                    let children =
                        root.Children
                        |> List.mapi (fun index child ->
                            match YGNodeAPI.YGNodeGetChild(rootYoga, unativeint index) with
                            | null -> invalidOp $"Yoga did not return layout child {index} for node '{root.Id}'."
                            | childYoga -> read cached.X cached.Y child childYoga)
                        |> List.concat
                    own :: children
            YGNodeAPI.YGNodeFreeRecursive(rootYoga)
            rootCreated <- false
            Ok(bounds, List.ofSeq measurementDiagnostics)
        with ex ->
            if rootCreated then
                try
                    YGNodeAPI.YGNodeFreeRecursive(rootYoga)
                with _ ->
                    ()

            Result.Error ex

    let evaluate available root =
        let available, availableDiagnostics = normalizeAvailable available
        let rootBounds: LayoutBounds =
            { X = 0.0
              Y = 0.0
              Width = available.Width
              Height = available.Height }

        let _, pureValidationDiagnostics = layoutNode rootBounds root

        let bounds, diagnostics =
            match tryYogaLayout available None root with
            | Ok(bounds, yogaDiagnostics) -> bounds, yogaDiagnostics @ pureValidationDiagnostics
            | Result.Error ex ->
                let bounds, pureDiagnostics = layoutNode rootBounds root
                let fallbackDiagnostic =
                    diagnostic
                        (Some root.Id)
                        FallbackBoundsApplied
                        FS.GG.UI.Layout.DiagnosticSeverity.Warning
                        $"Yoga execution failed recoverably; pure fallback layout was applied. {ex.GetType().Name}: {ex.Message}"
                        (Some "yoga")
                        true

                bounds, fallbackDiagnostic :: pureDiagnostics
        let allDiagnostics = availableDiagnostics @ validateTree root @ diagnostics

        let fallbackDiagnostics =
            if allDiagnostics |> List.exists (fun item -> item.FallbackApplied) then
                [ diagnostic None FallbackBoundsApplied FS.GG.UI.Layout.DiagnosticSeverity.Info "One or more layout inputs required bounded fallback geometry." None true ]
            else
                []

        { Bounds = bounds
          Diagnostics = allDiagnostics @ fallbackDiagnostics
          Invalidated = [ root.Id ]
          Revision = 1L }

    let evaluateIncremental (previous: LayoutResult) (changedNodeIds: LayoutNodeId list) available (root: LayoutNode) =
        // R2 — genuine incremental evaluator (FR-001). Re-measures ONLY the dirty nodes and their
        // conservatively-propagated flex containers; reuses `previous.Bounds` (the per-frame measure
        // cache, FR-002) for everything else. The returned `Bounds` are byte-identical to a full
        // `evaluate available root` (INV-1). `changedNodeIds` is a performance hint, never a
        // correctness input (contract C1): every uncertainty falls back to a full `evaluate`, which
        // is byte-identical by definition, so a wrong dirty set can only cost extra re-measure work.
        let preorder = ResizeArray<LayoutNodeId * LayoutNode * LayoutNodeId option>()
        let rec walk parent (n: LayoutNode) =
            preorder.Add(n.Id, n, parent)
            for c in n.Children do
                walk (Some n.Id) c
        walk None root

        let nodeById = System.Collections.Generic.Dictionary<LayoutNodeId, LayoutNode>()
        let parentById = System.Collections.Generic.Dictionary<LayoutNodeId, LayoutNodeId option>()
        for (nid, n, p) in preorder do
            nodeById.[nid] <- n
            parentById.[nid] <- p

        let prevBounds =
            previous.Bounds
            |> List.map (fun (b: ComputedBounds) -> b.NodeId, b)
            |> Map.ofList

        // A node whose Size is concrete on BOTH axes has a content-independent border box: an interior
        // content change cannot resize it, so it is a safe re-measure boundary (FR-004).
        let isFixed (n: LayoutNode) = n.Intent.Size.Width.IsSome && n.Intent.Size.Height.IsSome

        let fullEvaluate () : LayoutResult =
            // Whole-tree re-measure: the correct, honest result when the change reaches the root or any
            // precondition for partial reuse is unmet. `Invalidated` honestly reports every node.
            let full: LayoutResult = evaluate available root
            { previous with
                Bounds = full.Bounds
                Diagnostics = full.Diagnostics
                Invalidated = full.Bounds |> List.map (fun b -> b.NodeId)
                Revision = previous.Revision + 1L }

        let changed =
            changedNodeIds |> List.filter nodeById.ContainsKey |> List.distinct

        if nodeById.Count <> preorder.Count then
            // Duplicate LayoutNodeIds (e.g. Key collisions) make the positional parent map and the
            // bounds cache ambiguous — and could cycle the ancestor walk. The reuse scheme is unsound,
            // so fall back to a full evaluate: total and conservative (contract C1, totality).
            fullEvaluate ()
        elif List.isEmpty changed then
            // Identity at rest (spec edge): re-measure nothing, reuse every cached bound. Falls back to
            // full evaluate if the cache lacks a current id (a structural change the empty dirty set
            // failed to flag — correctness dominates the metric).
            let reused =
                preorder |> Seq.map (fun (nid, _, _) -> Map.tryFind nid prevBounds) |> List.ofSeq
            if reused |> List.exists Option.isNone then
                fullEvaluate ()
            else
                { previous with
                    Bounds = reused |> List.choose id
                    Invalidated = []
                    Revision = previous.Revision + 1L }
        else
            // The re-measure boundary of a changed node is its first fixed-size ancestor (strictly
            // above it); a fully content-sized chain reaches the root (FR-004).
            let rec boundaryOf (nid: LayoutNodeId) : LayoutNodeId =
                match parentById.TryGetValue nid with
                | true, Some pid ->
                    match nodeById.TryGetValue pid with
                    | true, pn -> if isFixed pn then pid else boundaryOf pid
                    | _ -> root.Id
                | _ -> root.Id

            let boundaries = changed |> List.map boundaryOf |> List.distinct

            if boundaries |> List.contains root.Id then
                fullEvaluate ()
            else
                let rec ancestorsOf (nid: LayoutNodeId) =
                    seq {
                        match parentById.TryGetValue nid with
                        | true, Some p ->
                            yield p
                            yield! ancestorsOf p
                        | _ -> ()
                    }

                // Drop any boundary nested inside another boundary's subtree (the outer one re-measures
                // it, including a fixed-size node that is itself dirty).
                let topmost =
                    boundaries
                    |> List.filter (fun b ->
                        not (boundaries |> List.exists (fun o -> o <> b && (ancestorsOf b |> Seq.contains o))))

                let relayout (b: LayoutNodeId) : Map<LayoutNodeId, ComputedBounds> option =
                    match Map.tryFind b prevBounds with
                    | Some cb ->
                        let cached = cb.Bounds
                        let bnode = nodeById.[b]
                        let av =
                            { Width = max 0.0 cached.Width
                              WidthMode = Exactly
                              Height = max 0.0 cached.Height
                              HeightMode = Exactly }
                        match tryYogaLayout av (Some cached) bnode with
                        | Ok(bounds, _) -> bounds |> List.map (fun x -> x.NodeId, x) |> Map.ofList |> Some
                        | Result.Error _ -> None
                    | None -> None

                let relayouts = topmost |> List.map relayout

                if relayouts |> List.exists Option.isNone then
                    fullEvaluate ()
                else
                    let remeasured =
                        relayouts
                        |> List.choose id
                        |> List.fold (fun acc m -> Map.fold (fun a k v -> Map.add k v a) acc m) Map.empty

                    let resolved =
                        preorder
                        |> Seq.map (fun (nid, _, _) ->
                            match Map.tryFind nid remeasured with
                            | Some cb -> Some cb
                            | None -> Map.tryFind nid prevBounds)
                        |> List.ofSeq

                    if resolved |> List.exists Option.isNone then
                        fullEvaluate ()
                    else
                        { previous with
                            Bounds = resolved |> List.choose id
                            // FR-001a: the actual re-measured set (post flex-line / fixed-size-ancestor
                            // propagation), not the verbatim requested input.
                            Invalidated = remeasured |> Map.toList |> List.map fst
                            Revision = previous.Revision + 1L }

    let private boundIdentity bound =
        match bound with
        | Bounded value -> $"bounded:{value:R}"
        | Unbounded -> "unbounded"

    let private finiteNonNegativeOrZero value =
        if nonNegative value then value else 0.0

    let private normalizeBound value =
        match value with
        | Some value when nonNegative value -> Bounded value
        | Some _ -> Bounded 0.0
        | None -> Unbounded

    let private maxBoundValue fallback bound =
        match bound with
        | Bounded value -> max 0.0 value
        | Unbounded -> fallback

    let constraints source minWidth maxWidth minHeight maxHeight =
        let minWidth = finiteNonNegativeOrZero minWidth
        let minHeight = finiteNonNegativeOrZero minHeight
        let maxWidth = normalizeBound maxWidth
        let maxHeight = normalizeBound maxHeight

        let maxWidth =
            match maxWidth with
            | Bounded value when value < minWidth -> Bounded minWidth
            | other -> other

        let maxHeight =
            match maxHeight with
            | Bounded value when value < minHeight -> Bounded minHeight
            | other -> other

        let widthMode =
            match maxWidth with
            | Bounded value when value = minWidth -> Exactly
            | Bounded _ -> AtMost
            | Unbounded -> Undefined

        let heightMode =
            match maxHeight with
            | Bounded value when value = minHeight -> Exactly
            | Bounded _ -> AtMost
            | Unbounded -> Undefined

        let identity =
            [ string source
              minWidth.ToString("R", Globalization.CultureInfo.InvariantCulture)
              boundIdentity maxWidth
              minHeight.ToString("R", Globalization.CultureInfo.InvariantCulture)
              boundIdentity maxHeight
              string widthMode
              string heightMode ]
            |> String.concat "|"

        { MinWidth = minWidth
          MaxWidth = maxWidth
          MinHeight = minHeight
          MaxHeight = maxHeight
          WidthMode = widthMode
          HeightMode = heightMode
          Source = source
          NormalizedIdentity = identity }

    let constraintsFromAvailable source available =
        let available, _ = normalizeAvailable available
        let maxWidth =
            match available.WidthMode with
            | Undefined -> None
            | _ -> Some available.Width

        let maxHeight =
            match available.HeightMode with
            | Undefined -> None
            | _ -> Some available.Height

        constraints source 0.0 maxWidth 0.0 maxHeight

    let rec layoutInputKey (node: LayoutNode) =
        let sizeKey (size: LayoutSize) =
            let item (value: float option) =
                value
                |> Option.map (fun value -> value.ToString("R", Globalization.CultureInfo.InvariantCulture))
                |> Option.defaultValue "auto"

            $"w={item size.Width};h={item size.Height}"

        let intent = node.Intent

        [ node.Id
          string node.Visibility
          string intent.Direction
          string intent.Wrap
          string intent.AlignItems
          string intent.AlignSelf
          string intent.JustifyContent
          $"pad={intent.Padding.Left:R},{intent.Padding.Top:R},{intent.Padding.Right:R},{intent.Padding.Bottom:R}"
          $"margin={intent.Margin.Left:R},{intent.Margin.Top:R},{intent.Margin.Right:R},{intent.Margin.Bottom:R}"
          $"gap={intent.Gap.Row:R},{intent.Gap.Column:R}"
          sizeKey intent.Size
          sizeKey intent.MinSize
          sizeKey intent.MaxSize
          let basis = intent.FlexBasis |> Option.map (fun v -> v.ToString("R", Globalization.CultureInfo.InvariantCulture)) |> Option.defaultValue "auto"
          $"grow={intent.FlexGrow:R};shrink={intent.FlexShrink:R};basis={basis}"
          if node.Measure.IsSome then "measure=some" else "measure=none"
          if node.Content.IsSome then "content=some" else "content=none"
          node.Children |> List.map layoutInputKey |> String.concat "[" ]
        |> String.concat "|"

    // Single source of truth for the layout-cache version: feeds both the `rev=…` identity token and
    // the `Revision` field on every cache record, so a future bump is one edit. Private by omission
    // from Layout.fsi. Renders to the exact bytes `rev=150` (invariant integer formatting).
    [<Literal>]
    let layoutCacheRevision = 150

    let intrinsicQuery participantId axis crossAxisConstraint layoutInputKey source : IntrinsicQuery =
        let cross =
            crossAxisConstraint
            |> Option.filter nonNegative
            |> Option.map (fun value -> value.ToString("R", Globalization.CultureInfo.InvariantCulture))
            |> Option.defaultValue "unbounded"

        let identity = $"{participantId}|{axis}|cross={cross}|input={layoutInputKey}|source={source}|rev={layoutCacheRevision}"

        { ParticipantId = participantId
          Axis = axis
          CrossAxisConstraint = crossAxisConstraint |> Option.filter nonNegative
          LayoutInputKey = layoutInputKey
          QuerySource = source
          QueryIdentity = identity
          Revision = layoutCacheRevision }

    let private resultIdentity size (diagnostics: LayoutDiagnostic list) =
        let diagnosticKey =
            diagnostics
            |> List.map (fun d -> $"{d.NodeId}|{d.Code}|{d.Severity}|{d.Constraint}|{d.FallbackApplied}")
            |> String.concat ","

        $"{size:R}|{diagnosticKey}"

    let private boundsExtent (bounds: ComputedBounds list) =
        match bounds with
        | [] -> 0.0, 0.0
        | root :: rest ->
            let measured = if List.isEmpty rest then [ root ] else rest

            let maxRight =
                measured
                |> List.map (fun item -> item.Bounds.X + item.Bounds.Width)
                |> List.max

            let maxBottom =
                measured
                |> List.map (fun item -> item.Bounds.Y + item.Bounds.Height)
                |> List.max

            max 0.0 (maxRight - root.Bounds.X), max 0.0 (maxBottom - root.Bounds.Y)

    let evaluateIntrinsic (query: IntrinsicQuery) (node: LayoutNode) : IntrinsicSizeResult =
        if query.ParticipantId <> node.Id then
            { QueryIdentity = query.QueryIdentity
              Size = 0.0
              Dependencies = []
              Accepted = false
              Diagnostics =
                [ diagnostic
                      (Some query.ParticipantId)
                      UnsupportedIntrinsicQuery
                      FS.GG.UI.Layout.DiagnosticSeverity.Warning
                      $"Intrinsic query target '{query.ParticipantId}' does not match node '{node.Id}'."
                      (Some "participant")
                      false ] }
        else
            let cross = query.CrossAxisConstraint |> Option.defaultValue 10000.0 |> max 0.0
            let large = 1000000.0
            let leafIntrinsic () =
                let measuredWidth, measuredHeight, measureDiagnostics = measureLeaf (Some node.Id) cross cross node.Measure

                match query.Axis with
                | IntrinsicMinWidth
                | IntrinsicMaxWidth -> node.Intent.Size.Width |> Option.defaultValue measuredWidth, measureDiagnostics
                | IntrinsicMinHeight
                | IntrinsicMaxHeight -> node.Intent.Size.Height |> Option.defaultValue measuredHeight, measureDiagnostics

            let size, resultDiagnostics =
                if List.isEmpty node.Children then
                    leafIntrinsic ()
                else
                    let available : AvailableSpace =
                        match query.Axis with
                        | IntrinsicMinWidth
                        | IntrinsicMaxWidth ->
                            { Width = large
                              WidthMode = AtMost
                              Height = cross
                              HeightMode = if query.CrossAxisConstraint.IsSome then Exactly else Undefined }
                        | IntrinsicMinHeight
                        | IntrinsicMaxHeight ->
                            { Width = cross
                              WidthMode = if query.CrossAxisConstraint.IsSome then Exactly else Undefined
                              Height = large
                              HeightMode = AtMost }

                    let result = evaluate available node
                    let extentWidth, extentHeight = boundsExtent result.Bounds

                    let size =
                        match query.Axis with
                        | IntrinsicMinWidth
                        | IntrinsicMaxWidth -> extentWidth
                        | IntrinsicMinHeight
                        | IntrinsicMaxHeight -> extentHeight

                    size, result.Diagnostics

            let accepted = nonNegative size
            let diagnostics =
                if accepted then
                    resultDiagnostics
                else
                    diagnostic
                        (Some node.Id)
                        RejectedIntrinsicResult
                        FS.GG.UI.Layout.DiagnosticSeverity.Error
                        "Intrinsic result was not finite and non-negative."
                        (Some "intrinsic")
                        false
                    :: resultDiagnostics

            { QueryIdentity = query.QueryIdentity
              Size = if accepted then size else 0.0
              Dependencies =
                node.Children
                |> List.map (fun (child: LayoutNode) ->
                    { QueryIdentity = $"{query.QueryIdentity}|child={child.Id}"
                      ResultIdentity = layoutInputKey child })
              Accepted = accepted
              Diagnostics = diagnostics }

    let cacheEntry kind participantId constraintIdentity layoutInputKey childDependencyKeys resultIdentity : LayoutCacheEntry =
        let id =
            [ string kind
              participantId
              constraintIdentity
              layoutInputKey
              childDependencyKeys |> String.concat ","
              resultIdentity
              $"rev={layoutCacheRevision}" ]
            |> String.concat "|"

        { EntryId = id
          EntryKind = kind
          ParticipantId = participantId
          ConstraintIdentity = constraintIdentity
          LayoutInputKey = layoutInputKey
          ChildDependencyKeys = childDependencyKeys
          ResultIdentity = resultIdentity
          Revision = layoutCacheRevision }

    let measureProtocol (constraints: LayoutConstraints) (node: LayoutNode) : MeasuredLayoutResult =
        let width = max constraints.MinWidth (maxBoundValue constraints.MinWidth constraints.MaxWidth)
        let height = max constraints.MinHeight (maxBoundValue constraints.MinHeight constraints.MaxHeight)

        let available : AvailableSpace =
            { Width = width
              WidthMode = constraints.WidthMode
              Height = height
              HeightMode = constraints.HeightMode }

        let result = evaluate available node
        let byId : Map<LayoutNodeId, ComputedBounds> = result.Bounds |> List.map (fun b -> b.NodeId, b) |> Map.ofList

        let measuredSize =
            match Map.tryFind node.Id byId with
            | Some own -> ({ MeasuredWidth = own.Bounds.Width; MeasuredHeight = own.Bounds.Height }: LayoutMeasuredSize)
            | None -> ({ MeasuredWidth = 0.0; MeasuredHeight = 0.0 }: LayoutMeasuredSize)

        let placement (child: LayoutNode) =
            match Map.tryFind child.Id byId with
            | Some bounds ->
                let b = bounds.Bounds
                Some
                    ({ ChildId = child.Id
                       Bounds = b
                       Visibility = bounds.Visibility
                       PlacementIdentity = $"{child.Id}|{b.X:R},{b.Y:R},{b.Width:R},{b.Height:R}|{bounds.Visibility}" }
                    : LayoutChildPlacement)
            | None -> None

        let duplicateMeasurementDiagnostics =
            node.Children
            |> List.countBy (fun child -> child.Id)
            |> List.choose (fun (id, count) ->
                if count > 1 then
                    Some(
                        diagnostic
                            (Some id)
                            DuplicateMeasurement
                            FS.GG.UI.Layout.DiagnosticSeverity.Warning
                            $"Participant '{id}' appears {count} times under one measurement parent; cache reuse requires unique participant ids."
                            (Some "participant")
                            false
                    )
                else
                    None)

        let childDependencyKeys = node.Children |> List.map layoutInputKey
        let measuredIdentity = $"{measuredSize.MeasuredWidth:R}x{measuredSize.MeasuredHeight:R}"

        let entry =
            cacheEntry MeasuredLayoutEntry node.Id constraints.NormalizedIdentity (layoutInputKey node) childDependencyKeys measuredIdentity

        { ParticipantId = node.Id
          Constraints = constraints
          MeasuredSize = measuredSize
          ChildPlacements = node.Children |> List.choose placement
          IntrinsicDependencies = []
          CacheEntryId = entry.EntryId
          Diagnostics = result.Diagnostics @ duplicateMeasurementDiagnostics }

    let contentExtent viewportWidth viewportHeight (content: LayoutNode option) : LayoutContentExtent =
        let viewportWidth = finiteNonNegativeOrZero viewportWidth
        let viewportHeight = finiteNonNegativeOrZero viewportHeight

        match content with
        | None ->
            { ContentWidth = viewportWidth
              ContentHeight = viewportHeight
              MaxHorizontalOffset = 0.0
              MaxVerticalOffset = 0.0
              ExtentSource = EmptyContent
              DependencyKeys = []
              Diagnostics = [] }
        | Some node ->
            let inputKey = layoutInputKey node
            let widthQuery = intrinsicQuery node.Id IntrinsicMaxWidth (Some viewportHeight) inputKey IntrinsicQuerySource.ScrollViewer
            let heightQuery = intrinsicQuery node.Id IntrinsicMaxHeight (Some viewportWidth) inputKey IntrinsicQuerySource.ScrollViewer
            let widthResult = evaluateIntrinsic widthQuery node
            let heightResult = evaluateIntrinsic heightQuery node

            let accepted = widthResult.Accepted && heightResult.Accepted
            let contentWidth = max viewportWidth (if accepted then widthResult.Size else viewportWidth)
            let contentHeight = max viewportHeight (if accepted then heightResult.Size else viewportHeight)

            let fallbackDiagnostics =
                if accepted then
                    []
                else
                    [ diagnostic
                          (Some node.Id)
                          InsufficientDependencyEvidence
                          FS.GG.UI.Layout.DiagnosticSeverity.Warning
                          "Scroll content extent fell back to the viewport because intrinsic dependency evidence was incomplete."
                          (Some "scroll-extent")
                          true ]

            { ContentWidth = contentWidth
              ContentHeight = contentHeight
              MaxHorizontalOffset = max 0.0 (contentWidth - viewportWidth)
              MaxVerticalOffset = max 0.0 (contentHeight - viewportHeight)
              ExtentSource = if accepted then IntrinsicResult else MeasuredFallback
              DependencyKeys = [ widthQuery.QueryIdentity; heightQuery.QueryIdentity ]
              Diagnostics = widthResult.Diagnostics @ heightResult.Diagnostics @ fallbackDiagnostics }

    let rec contentById (node: LayoutNode) =
        seq {
            yield node.Id, node.Content
            for child in node.Children do
                yield! contentById child
        }

    let renderComputed (result: LayoutResult) (root: LayoutNode) =
        let content = contentById root |> Map.ofSeq

        result.Bounds
        |> List.choose (fun item ->
            match item.Visibility, Map.tryFind item.NodeId content with
            | Visible, Some(Some scene) -> Some scene
            | _ -> None)
        |> Scene.group

    let snapValue mode (scale: float) (value: float) =
        let scaled = value * scale

        let snapped =
            match mode with
            | SnapMode.Floor -> Math.Floor scaled
            | SnapMode.Round -> Math.Round(scaled, MidpointRounding.AwayFromZero)
            | SnapMode.Expand -> Math.Floor scaled

        snapped / scale

    let snapEnd mode (scale: float) (value: float) =
        let scaled = value * scale

        let snapped =
            match mode with
            | SnapMode.Expand -> Math.Ceiling scaled
            | SnapMode.Floor -> Math.Floor scaled
            | SnapMode.Round -> Math.Round(scaled, MidpointRounding.AwayFromZero)

        snapped / scale

    let snapBounds (policy: PixelSnapPolicy) (bounds: LayoutBounds) =
        let scale =
            if finite policy.ScaleFactor && policy.ScaleFactor > 0.0 then
                policy.ScaleFactor
            else
                1.0

        let x = snapValue policy.Mode scale bounds.X
        let y = snapValue policy.Mode scale bounds.Y
        let right = snapEnd policy.Mode scale (bounds.X + bounds.Width)
        let bottom = snapEnd policy.Mode scale (bounds.Y + bounds.Height)

        ({ X = x
           Y = y
           Width = max 0.0 (right - x)
           Height = max 0.0 (bottom - y) }
        : LayoutBounds)

    let hitTestComputed (policy: PixelSnapPolicy) (result: LayoutResult) (x: float) (y: float) =
        result.Bounds
        |> List.rev
        |> List.tryPick (fun item ->
            if item.Visibility = Visible then
                let bounds = snapBounds policy item.Bounds

                if x >= bounds.X && x <= bounds.X + bounds.Width && y >= bounds.Y && y <= bounds.Y + bounds.Height then
                    Some item.NodeId
                else
                    None
            else
                None)

    let initWorkflow available root =
        { Root = root
          Available = available
          Result = None
          LastChangedNodeIds = [ root.Id ]
          PixelSnapPolicy = Defaults.pixelSnapPolicy 1.0 },
        [ EvaluateLayout ]

    let rec updateNode nodeId apply (node: LayoutNode) =
        let updated =
            if node.Id = nodeId then
                apply node
            else
                node

        { updated with Children = updated.Children |> List.map (updateNode nodeId apply) }

    let updateWorkflow msg model =
        match msg with
        | LayoutHostResized available ->
            { model with
                Available = available
                LastChangedNodeIds = [ model.Root.Id ] },
            [ EvaluateIncrementalLayout [ model.Root.Id ] ]
        | LayoutVisibilityChanged(nodeId, visibility) ->
            { model with
                Root = updateNode nodeId (fun node -> { node with Visibility = visibility }) model.Root
                LastChangedNodeIds = [ nodeId ] },
            [ EvaluateIncrementalLayout [ nodeId ] ]
        | LayoutIntentChanged(nodeId, intent) ->
            { model with
                Root = updateNode nodeId (fun node -> { node with Intent = intent }) model.Root
                LastChangedNodeIds = [ nodeId ] },
            [ EvaluateIncrementalLayout [ nodeId ] ]
        | LayoutMeasurementChanged nodeId ->
            { model with LastChangedNodeIds = [ nodeId ] },
            [ EvaluateIncrementalLayout [ nodeId ] ]
        | LayoutEvaluationCompleted result ->
            { model with
                Result = Some result
                LastChangedNodeIds = result.Invalidated },
            []

    let interpretWorkflowEffect effect model =
        let result =
            match effect, model.Result with
            | EvaluateLayout, _
            | EvaluateIncrementalLayout _, None -> evaluate model.Available model.Root
            | EvaluateIncrementalLayout changedNodeIds, Some previous -> evaluateIncremental previous changedNodeIds model.Available model.Root

        LayoutEvaluationCompleted result

    let content (children: LayoutChild list) =
        children |> List.map _.Content |> Scene.group

    let innerBounds (bounds: LayoutBounds) (padding: LayoutPadding) =
        { X = bounds.X + padding.Left
          Y = bounds.Y + padding.Top
          Width = max 0.0 (bounds.Width - padding.Left - padding.Right)
          Height = max 0.0 (bounds.Height - padding.Top - padding.Bottom) }

    let measureHorizontal (config: StackConfig) (children: LayoutChild list) =
        let inner = innerBounds config.Bounds config.Padding
        let count = max 1 children.Length
        let totalSpacing = config.Spacing * float (max 0 (children.Length - 1))
        let width = max 0.0 ((inner.Width - totalSpacing) / float count)

        children
        |> List.mapi (fun index _ ->
            { LayoutBounds.X = inner.X + float index * (width + config.Spacing)
              Y = inner.Y
              Width = width
              Height = inner.Height })

    let measureVertical (config: StackConfig) (children: LayoutChild list) =
        let inner = innerBounds config.Bounds config.Padding
        let count = max 1 children.Length
        let totalSpacing = config.Spacing * float (max 0 (children.Length - 1))
        let height = max 0.0 ((inner.Height - totalSpacing) / float count)

        children
        |> List.mapi (fun index _ ->
            { LayoutBounds.X = inner.X
              Y = inner.Y + float index * (height + config.Spacing)
              Width = inner.Width
              Height = height })

    let horizontalStack (_: StackConfig) (children: LayoutChild list) = content children
    let verticalStack (_: StackConfig) (children: LayoutChild list) = content children
    let dock (_: DockConfig) (children: LayoutChild list) = content children
