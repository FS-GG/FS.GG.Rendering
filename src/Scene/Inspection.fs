namespace FS.GG.UI.Scene

open System

module VisualInspection =
    let private cleanToken (value: string) =
        if String.IsNullOrWhiteSpace value then
            "unknown"
        else
            value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-")

    let statusText status =
        match status with
        | VisualInspectionStatus.Accepted -> "accepted"
        | VisualInspectionStatus.Blocked -> "blocked"
        | VisualInspectionStatus.Incomplete -> "incomplete"
        | VisualInspectionStatus.Unsupported -> "unsupported"
        | VisualInspectionStatus.EnvironmentLimited -> "environment-limited"
        | VisualInspectionStatus.NotInspected -> "not-inspected"
        | VisualInspectionStatus.NotRun -> "not-run"

    let severityText severity =
        match severity with
        | VisualInspectionSeverity.Pass -> "pass"
        | VisualInspectionSeverity.Info -> "info"
        | VisualInspectionSeverity.Warning -> "warning"
        | VisualInspectionSeverity.Blocking -> "blocking"
        | VisualInspectionSeverity.Unsupported -> "unsupported"
        | VisualInspectionSeverity.EnvironmentLimited -> "environment-limited"

    let measurementModeText mode =
        match mode with
        | VisualInspectionMeasurementMode.Exact -> "exact"
        | VisualInspectionMeasurementMode.Approximate -> "approximate"
        | VisualInspectionMeasurementMode.Unsupported -> "unsupported"
        | VisualInspectionMeasurementMode.Unavailable -> "unavailable"

    let fitStatusText status =
        match status with
        | VisualInspectionFitStatus.Inside -> "inside"
        | VisualInspectionFitStatus.Overflow -> "overflow"
        | VisualInspectionFitStatus.Clipped -> "clipped"
        | VisualInspectionFitStatus.Wrapped -> "wrapped"
        | VisualInspectionFitStatus.Truncated -> "truncated"
        | VisualInspectionFitStatus.Unsupported -> "unsupported"
        | VisualInspectionFitStatus.Unavailable -> "unavailable"

    let nodeKindText kind =
        match kind with
        | VisualInspectionNodeKind.Root -> "root"
        | VisualInspectionNodeKind.Container -> "container"
        | VisualInspectionNodeKind.Text -> "text"
        | VisualInspectionNodeKind.Shape -> "shape"
        | VisualInspectionNodeKind.Image -> "image"
        | VisualInspectionNodeKind.Overlay -> "overlay"
        | VisualInspectionNodeKind.Popup -> "popup"
        | VisualInspectionNodeKind.Custom value -> cleanToken value
        | VisualInspectionNodeKind.Unknown -> "unknown"

    let paintRoleText role =
        match role with
        | VisualInspectionPaintRole.Background -> "background"
        | VisualInspectionPaintRole.Surface -> "surface"
        | VisualInspectionPaintRole.Border -> "border"
        | VisualInspectionPaintRole.Foreground -> "foreground"
        | VisualInspectionPaintRole.Content -> "content"
        | VisualInspectionPaintRole.Overlay -> "overlay"
        | VisualInspectionPaintRole.None -> "none"
        | VisualInspectionPaintRole.Unknown -> "unknown"

    let surfaceRoleText role =
        match role with
        | VisualInspectionSurfaceRole.Root -> "root"
        | VisualInspectionSurfaceRole.Shell -> "shell"
        | VisualInspectionSurfaceRole.Content -> "content"
        | VisualInspectionSurfaceRole.Navigation -> "navigation"
        | VisualInspectionSurfaceRole.Feedback -> "feedback"
        | VisualInspectionSurfaceRole.Overlay -> "overlay"
        | VisualInspectionSurfaceRole.Popup -> "popup"
        | VisualInspectionSurfaceRole.Floating -> "floating"
        | VisualInspectionSurfaceRole.Custom value -> cleanToken value
        | VisualInspectionSurfaceRole.Unknown -> "unknown"

    let clipStatusText status =
        match status with
        | VisualInspectionClipStatus.None -> "none"
        | VisualInspectionClipStatus.Intentional -> "intentional"
        | VisualInspectionClipStatus.Accidental -> "accidental"
        | VisualInspectionClipStatus.Unsupported -> "unsupported"
        | VisualInspectionClipStatus.Unavailable -> "unavailable"

    let coverageStatusText status =
        match status with
        | VisualInspectionCoverageStatus.Complete -> "complete"
        | VisualInspectionCoverageStatus.Partial -> "partial"
        | VisualInspectionCoverageStatus.Missing -> "missing"
        | VisualInspectionCoverageStatus.Unsupported -> "unsupported"
        | VisualInspectionCoverageStatus.Unavailable -> "unavailable"

    let unsupportedFact fact ownerId required reason diagnostic environmentLimited =
        { Fact = fact
          OwnerId = ownerId
          Required = required
          Reason = reason
          Diagnostic = diagnostic
          EnvironmentLimited = environmentLimited }

    let stableFindingId (ruleId: string) (affectedIds: string list) =
        let ids =
            affectedIds
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> List.map cleanToken
            |> List.sort

        match ids with
        | [] -> cleanToken ruleId
        | _ -> cleanToken ruleId + ":" + String.concat "+" ids

    let finding ruleId severity affectedNodeIds affectedRegionIds message expected actual =
        { FindingId = stableFindingId ruleId (affectedNodeIds @ affectedRegionIds)
          RuleId = ruleId
          Severity = severity
          AffectedNodeIds = affectedNodeIds |> List.sort
          AffectedRegionIds = affectedRegionIds |> List.sort
          Message = message
          Expected = expected
          Actual = actual
          ExceptionId = None
          Diagnostics = [] }

    let private duplicateIds ids =
        ids
        |> List.countBy id
        |> List.choose (fun (id, count) -> if count > 1 then Some id else None)

    let artifactDiagnostics (artifact: VisualInspectionArtifact) =
        let nodeIds = artifact.Nodes |> List.map _.NodeId
        let regionIds = artifact.Regions |> List.map _.RegionId
        let findingIds = artifact.Findings |> List.map _.FindingId
        let parentIds = nodeIds |> Set.ofList

        [ for id in duplicateIds nodeIds do
              $"duplicate visual inspection node id: {id}"
          for id in duplicateIds regionIds do
              $"duplicate visual inspection region id: {id}"
          for id in duplicateIds findingIds do
              $"duplicate visual inspection finding id: {id}"
          for node in artifact.Nodes do
              match node.ParentId with
              | Some parent when not (Set.contains parent parentIds) -> $"node {node.NodeId} references missing parent {parent}"
              | _ -> ()
              match node.Bounds with
              | Some bounds when bounds.Width < 0.0 || bounds.Height < 0.0 || Double.IsNaN bounds.Width || Double.IsNaN bounds.Height ->
                  $"node {node.NodeId} has invalid bounds"
              | _ -> ()
          for region in artifact.Regions do
              match region.Bounds with
              | Some bounds when bounds.Width < 0.0 || bounds.Height < 0.0 || Double.IsNaN bounds.Width || Double.IsNaN bounds.Height ->
                  $"region {region.RegionId} has invalid bounds"
              | _ -> ()
          for fact in artifact.UnsupportedFacts do
              if String.IsNullOrWhiteSpace fact.Fact || String.IsNullOrWhiteSpace fact.Reason then
                  "unsupported visual inspection fact is missing fact name or reason" ]

    let normalizeArtifact (artifact: VisualInspectionArtifact) =
        { artifact with
            Nodes = artifact.Nodes |> List.sortBy (fun node -> node.ZOrder, node.NodeId)
            Regions = artifact.Regions |> List.sortBy _.RegionId
            TextRuns = artifact.TextRuns |> List.sortBy _.TextId
            PaintCoverage = artifact.PaintCoverage |> List.sortBy _.CoverageId
            ClipFacts = artifact.ClipFacts |> List.sortBy _.ClipId
            Findings = artifact.Findings |> List.sortBy _.FindingId |> List.distinctBy _.FindingId
            UnsupportedFacts = artifact.UnsupportedFacts |> List.sortBy (fun fact -> fact.Fact, defaultArg fact.OwnerId "") }

module RetainedInspection =
    let private cleanToken (value: string) =
        if String.IsNullOrWhiteSpace value then
            "unknown"
        else
            value.Trim().ToLowerInvariant().Replace(" ", "-").Replace("_", "-")

    let statusText status =
        match status with
        | RetainedInspectionStatus.Accepted -> "accepted"
        | RetainedInspectionStatus.Blocked -> "blocked"
        | RetainedInspectionStatus.ReviewRequired -> "review-required"
        | RetainedInspectionStatus.Unsupported -> "unsupported"
        | RetainedInspectionStatus.EnvironmentLimited -> "environment-limited"
        | RetainedInspectionStatus.NotInspected -> "not-inspected"
        | RetainedInspectionStatus.NotRun -> "not-run"

    let nodeStatusText status =
        match status with
        | RetainedNodeStatus.Retained -> "retained"
        | RetainedNodeStatus.Reused -> "reused"
        | RetainedNodeStatus.Repainted -> "repainted"
        | RetainedNodeStatus.Shifted -> "shifted"
        | RetainedNodeStatus.ShiftedAndRepainted -> "shifted-and-repainted"
        | RetainedNodeStatus.Added -> "added"
        | RetainedNodeStatus.Removed -> "removed"
        | RetainedNodeStatus.Unaffected -> "unaffected"
        | RetainedNodeStatus.Unsupported -> "unsupported"

    let damageStatusText status =
        match status with
        | DamageInspectionStatus.Empty -> "empty"
        | DamageInspectionStatus.Localized -> "localized"
        | DamageInspectionStatus.Broad -> "broad"
        | DamageInspectionStatus.FullSurface -> "full-surface"
        | DamageInspectionStatus.Unsupported -> "unsupported"
        | DamageInspectionStatus.NotInspected -> "not-inspected"

    let unsupportedFact fact ownerId required reason diagnostic environmentLimited =
        VisualInspection.unsupportedFact fact ownerId required reason diagnostic environmentLimited

    let stableFindingId ruleId transitionId affectedIds =
        let ids =
            affectedIds
            |> List.filter (String.IsNullOrWhiteSpace >> not)
            |> List.map cleanToken
            |> List.sort

        let prefix = cleanToken ruleId + ":" + cleanToken transitionId
        match ids with
        | [] -> prefix
        | _ -> prefix + ":" + String.concat "+" ids

    let finding ruleId severity transitionId affectedNodeIds affectedRegionIds message expected actual =
        { FindingId = stableFindingId ruleId transitionId (affectedNodeIds @ affectedRegionIds)
          RuleId = ruleId
          Severity = severity
          TransitionId = transitionId
          AffectedNodeIds = affectedNodeIds |> List.sort
          AffectedRegionIds = affectedRegionIds |> List.sort
          Message = message
          Expected = expected
          Actual = actual
          ExceptionId = None
          Diagnostics = [] }

    let private clipRect (frame: Rect) (rect: Rect) =
        let x1 = max frame.X rect.X
        let y1 = max frame.Y rect.Y
        let x2 = min (frame.X + frame.Width) (rect.X + rect.Width)
        let y2 = min (frame.Y + frame.Height) (rect.Y + rect.Height)

        if x2 <= x1 || y2 <= y1 then
            None
        else
            Some({ X = x1; Y = y1; Width = x2 - x1; Height = y2 - y1 }: Rect)

    let private clipped frame rects =
        rects
        |> List.choose (clipRect frame)
        |> List.distinct

    let dirtyUnionBounds frameBounds dirtyRectangles =
        match clipped frameBounds dirtyRectangles with
        | [] -> None
        | rects ->
            let minX = rects |> List.map _.X |> List.min
            let minY = rects |> List.map _.Y |> List.min
            let maxX = rects |> List.map (fun r -> r.X + r.Width) |> List.max
            let maxY = rects |> List.map (fun r -> r.Y + r.Height) |> List.max
            Some({ X = minX; Y = minY; Width = maxX - minX; Height = maxY - minY }: Rect)

    let dirtyUnionArea frameBounds dirtyRectangles =
        let rects = clipped frameBounds dirtyRectangles

        match rects with
        | [] -> 0
        | _ ->
            let xs =
                rects
                |> List.collect (fun r -> [ r.X; r.X + r.Width ])
                |> List.distinct
                |> List.sort

            let ys =
                rects
                |> List.collect (fun r -> [ r.Y; r.Y + r.Height ])
                |> List.distinct
                |> List.sort

            let covered x1 x2 y1 y2 =
                rects
                |> List.exists (fun r ->
                    x1 >= r.X
                    && x2 <= r.X + r.Width
                    && y1 >= r.Y
                    && y2 <= r.Y + r.Height)

            let mutable area = 0.0

            for x1, x2 in xs |> List.pairwise do
                for y1, y2 in ys |> List.pairwise do
                    if x2 > x1 && y2 > y1 && covered x1 x2 y1 y2 then
                        area <- area + ((x2 - x1) * (y2 - y1))

            area |> int

    let damageRegion transitionId frameBounds dirtyRectangles expectedAffectedRegionIds affectedNodeIds (nodeCounts: DamageNodeCounts) cause maximumDirtyPercentage =
        let clippedRectangles = clipped frameBounds dirtyRectangles
        let area = dirtyUnionArea frameBounds clippedRectangles
        let frameArea = max 0.0 (frameBounds.Width * frameBounds.Height)
        let dirtyPercentage =
            if frameArea <= 0.0 then
                0.0
            else
                float area / frameArea * 100.0

        let status =
            if clippedRectangles.IsEmpty || area = 0 then
                DamageInspectionStatus.Empty
            elif area >= int frameArea && frameArea > 0.0 then
                DamageInspectionStatus.FullSurface
            else
                match maximumDirtyPercentage with
                | Some limit when dirtyPercentage > limit -> DamageInspectionStatus.Broad
                | _ -> DamageInspectionStatus.Localized

        { TransitionId = transitionId
          DamageStatus = status
          FrameBounds = frameBounds
          DirtyRectangles = clippedRectangles |> List.sortBy (fun r -> r.Y, r.X, r.Width, r.Height)
          UnionBounds = dirtyUnionBounds frameBounds clippedRectangles
          UnionArea = area
          VisibleDirtyArea = area
          DirtyPercentage = dirtyPercentage
          AffectedRegionIds = expectedAffectedRegionIds |> List.distinct |> List.sort
          AffectedNodeIds = affectedNodeIds |> List.distinct |> List.sort
          RepaintedNodeCount = nodeCounts.Repainted
          ShiftedNodeCount = nodeCounts.Shifted
          UnaffectedNodeCount = nodeCounts.Unaffected
          Cause = cause
          Diagnostics = [] }

    let private duplicateIds ids =
        ids
        |> List.countBy id
        |> List.choose (fun (id, count) -> if count > 1 then Some id else None)

    let artifactDiagnostics (artifact: RetainedInspectionArtifact) =
        let nodeIds = artifact.RetainedNodes |> List.map _.NodeId
        let findingIds = artifact.Findings |> List.map _.FindingId

        [ for id in duplicateIds nodeIds do
              $"duplicate retained inspection node id: {id}"
          for id in duplicateIds findingIds do
              $"duplicate retained inspection finding id: {id}"
          for node in artifact.RetainedNodes do
              match node.Status, node.PriorBounds, node.CurrentBounds with
              | RetainedNodeStatus.Shifted, None, _
              | RetainedNodeStatus.Shifted, _, None
              | RetainedNodeStatus.ShiftedAndRepainted, None, _
              | RetainedNodeStatus.ShiftedAndRepainted, _, None ->
                  $"shifted retained node {node.NodeId} is missing prior or current bounds"
              | _ -> ()
              for fact in node.UnsupportedFacts do
                  if String.IsNullOrWhiteSpace fact.Fact || String.IsNullOrWhiteSpace fact.Reason then
                      $"retained node {node.NodeId} has unsupported fact missing fact name or reason"
          for fact in artifact.UnsupportedFacts do
              if String.IsNullOrWhiteSpace fact.Fact || String.IsNullOrWhiteSpace fact.Reason then
                  "retained inspection unsupported fact is missing fact name or reason" ]

    let normalizeArtifact (artifact: RetainedInspectionArtifact) =
        { artifact with
            RetainedNodes = artifact.RetainedNodes |> List.sortBy _.NodeId
            Damage =
                artifact.Damage
                |> Option.map (fun damage ->
                    { damage with
                        DirtyRectangles = damage.DirtyRectangles |> List.sortBy (fun r -> r.Y, r.X, r.Width, r.Height)
                        AffectedNodeIds = damage.AffectedNodeIds |> List.distinct |> List.sort
                        AffectedRegionIds = damage.AffectedRegionIds |> List.distinct |> List.sort })
            Findings = artifact.Findings |> List.sortBy _.FindingId |> List.distinctBy _.FindingId
            UnsupportedFacts = artifact.UnsupportedFacts |> List.sortBy (fun fact -> fact.Fact, defaultArg fact.OwnerId "")
            RelatedVisualEvidence = artifact.RelatedVisualEvidence |> List.distinct |> List.sort
            Diagnostics = artifact.Diagnostics |> List.distinct |> List.sort }

module SceneInspection =
    type private Matrix =
        { A: float; B: float; C: float
          D: float; E: float; F: float
          G: float; H: float; I: float }

    let private identity =
        { A = 1.0; B = 0.0; C = 0.0
          D = 0.0; E = 1.0; F = 0.0
          G = 0.0; H = 0.0; I = 1.0 }

    let private multiply left right =
        { A = left.A * right.A + left.B * right.D + left.C * right.G
          B = left.A * right.B + left.B * right.E + left.C * right.H
          C = left.A * right.C + left.B * right.F + left.C * right.I
          D = left.D * right.A + left.E * right.D + left.F * right.G
          E = left.D * right.B + left.E * right.E + left.F * right.H
          F = left.D * right.C + left.E * right.F + left.F * right.I
          G = left.G * right.A + left.H * right.D + left.I * right.G
          H = left.G * right.B + left.H * right.E + left.I * right.H
          I = left.G * right.C + left.H * right.F + left.I * right.I }

    let private perspective (value: PerspectiveTransform) =
        { A = value.M11; B = value.M12; C = value.M13
          D = value.M21; E = value.M22; F = value.M23
          G = value.M31; H = value.M32; I = value.M33 }

    let private translation dx dy = { identity with C = dx; F = dy }
    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)

    let private validRect (rect: Rect) =
        finite rect.X && finite rect.Y && finite rect.Width && finite rect.Height
        && rect.Width >= 0.0 && rect.Height >= 0.0

    let private boundsOfPoints (points: Point list) =
        if points |> List.forall (fun p -> finite p.X && finite p.Y) |> not then
            SceneDrawableBounds.Unknown SceneBoundsUnknownReason.NonFiniteGeometry
        else
            match points with
            | [] -> SceneDrawableBounds.NoDrawableContent
            | _ ->
                let minX = points |> List.minBy _.X |> _.X
                let minY = points |> List.minBy _.Y |> _.Y
                let maxX = points |> List.maxBy _.X |> _.X
                let maxY = points |> List.maxBy _.Y |> _.Y
                SceneDrawableBounds.Known
                    { X = minX; Y = minY; Width = maxX - minX; Height = maxY - minY }

    let private transformPoint matrix (point: Point) =
        let w = matrix.G * point.X + matrix.H * point.Y + matrix.I
        if not (finite w) || abs w < 1e-12 then None
        else
            let x = (matrix.A * point.X + matrix.B * point.Y + matrix.C) / w
            let y = (matrix.D * point.X + matrix.E * point.Y + matrix.F) / w
            if finite x && finite y then Some ({ X = x; Y = y }: Point) else None

    let private transformRect matrix (rect: Rect) =
        if not (validRect rect) then
            SceneDrawableBounds.Unknown SceneBoundsUnknownReason.NonFiniteGeometry
        else
            let corners =
                [ ({ X = rect.X; Y = rect.Y }: Point)
                  { X = rect.X + rect.Width; Y = rect.Y }
                  { X = rect.X + rect.Width; Y = rect.Y + rect.Height }
                  { X = rect.X; Y = rect.Y + rect.Height } ]
            let ws =
                corners
                |> List.map (fun point -> matrix.G * point.X + matrix.H * point.Y + matrix.I)
            if
                ws |> List.exists (fun w -> not (finite w) || abs w < 1e-12)
                || (List.min ws < 0.0 && List.max ws > 0.0)
            then
                SceneDrawableBounds.Unknown SceneBoundsUnknownReason.PerspectiveHorizon
            else
                corners
                |> List.map (transformPoint matrix)
                |> function
                | points when List.exists Option.isNone points ->
                    SceneDrawableBounds.Unknown SceneBoundsUnknownReason.PerspectiveHorizon
                | points -> points |> List.choose id |> boundsOfPoints

    let private intersect left right =
        let x1 = max left.X right.X
        let y1 = max left.Y right.Y
        let x2 = min (left.X + left.Width) (right.X + right.Width)
        let y2 = min (left.Y + left.Height) (right.Y + right.Height)
        if x2 <= x1 || y2 <= y1 then None
        else Some { X = x1; Y = y1; Width = x2 - x1; Height = y2 - y1 }

    let private union results =
        match results |> List.tryPick (function SceneDrawableBounds.Unknown reason -> Some reason | _ -> None) with
        | Some reason -> SceneDrawableBounds.Unknown reason
        | None ->
            match results |> List.choose (function SceneDrawableBounds.Known value -> Some value | _ -> None) with
            | [] -> SceneDrawableBounds.NoDrawableContent
            | values ->
                let minX = values |> List.minBy _.X |> _.X
                let minY = values |> List.minBy _.Y |> _.Y
                let maxX = values |> List.maxBy (fun r -> r.X + r.Width) |> fun r -> r.X + r.Width
                let maxY = values |> List.maxBy (fun r -> r.Y + r.Height) |> fun r -> r.Y + r.Height
                SceneDrawableBounds.Known
                    { X = minX; Y = minY; Width = maxX - minX; Height = maxY - minY }

    let private expand amount rect =
        { X = rect.X - amount
          Y = rect.Y - amount
          Width = rect.Width + 2.0 * amount
          Height = rect.Height + 2.0 * amount }

    let private unionRects left right =
        let minX = min left.X right.X
        let minY = min left.Y right.Y
        let maxX = max (left.X + left.Width) (right.X + right.Width)
        let maxY = max (left.Y + left.Height) (right.Y + right.Height)
        { X = minX; Y = minY; Width = maxX - minX; Height = maxY - minY }

    let private expandByPaint paint rect =
        let strokeExtent =
            paint.Stroke
            |> Option.map (fun stroke ->
                let radius = max 0.0 stroke.Width / 2.0
                match stroke.Join with
                | StrokeJoin.Miter -> radius * max 1.0 stroke.Miter
                | RoundJoin
                | Bevel -> radius)
            |> Option.defaultValue 0.0
        let pathEffectExtent =
            match paint.PathEffect with
            | Discrete(segmentLength, deviation) when segmentLength > 0.0 -> abs deviation
            | _ -> 0.0
        let maskExtent =
            match paint.MaskFilter with
            // Skia's Gaussian mask kernel has finite raster support at three sigma.
            | Blur sigma when sigma > 0.0 -> 3.0 * sigma
            | _ -> 0.0
        let finitePaint =
            [ strokeExtent; pathEffectExtent; maskExtent ]
            |> List.forall finite

        if not finitePaint then
            SceneDrawableBounds.Unknown SceneBoundsUnknownReason.NonFiniteGeometry
        else
            let source = expand (strokeExtent + pathEffectExtent + maskExtent) rect
            match paint.ImageFilter with
            | DropShadow(dx, dy, blur, _) when blur >= 0.0 ->
                if [ dx; dy; blur ] |> List.forall finite then
                    let shadowExtent = 3.0 * blur
                    let shadow = expand shadowExtent source
                    let translatedShadow =
                        { shadow with X = shadow.X + dx; Y = shadow.Y + dy }
                    SceneDrawableBounds.Known (unionRects source translatedShadow)
                else
                    SceneDrawableBounds.Unknown SceneBoundsUnknownReason.NonFiniteGeometry
            | _ -> SceneDrawableBounds.Known source

    let private pointGeometry minimumRadius (points: Point list) paint =
        match points with
        | [] -> SceneDrawableBounds.Unknown SceneBoundsUnknownReason.EmptyGeometry
        | _ ->
            match boundsOfPoints points with
            | SceneDrawableBounds.Known rect ->
                let hairlineRadius = if paint.Stroke.IsSome then 0.0 else minimumRadius
                expand hairlineRadius rect |> expandByPaint paint
            | value -> value

    let private textBounds (position: Point) text font =
        let metrics = Scene.measureText text font
        { X = position.X
          Y = position.Y - metrics.Baseline
          Width = metrics.Width
          Height = metrics.Height }

    let private applyClip clip bounds =
        match clip, bounds with
        | _, SceneDrawableBounds.Unknown reason -> SceneDrawableBounds.Unknown reason
        | Error reason, _ -> SceneDrawableBounds.Unknown reason
        | Ok None, value -> value
        | Ok (Some clipBounds), SceneDrawableBounds.Known value ->
            intersect clipBounds value
            |> Option.map SceneDrawableBounds.Known
            |> Option.defaultValue SceneDrawableBounds.NoDrawableContent
        | _, SceneDrawableBounds.NoDrawableContent -> SceneDrawableBounds.NoDrawableContent

    let private viewportRelation viewport bounds =
        match bounds with
        | SceneDrawableBounds.NoDrawableContent -> SceneViewportRelation.NotDrawable
        | SceneDrawableBounds.Unknown _ -> SceneViewportRelation.Unknown
        | SceneDrawableBounds.Known value ->
            match intersect viewport value with
            | None -> SceneViewportRelation.Outside
            | Some overlap when overlap = value -> SceneViewportRelation.Inside
            | Some _ -> SceneViewportRelation.PartiallyOutside

    let private nodeKind = function
        | Empty -> EmptyElement
        | Group _ -> GroupElement
        | Rectangle _ | PaintedRectangle _ -> RectangleElement
        | Circle _ -> CircleElement
        | FilledEllipse _ | Ellipse _ -> EllipseElement
        | Line _ -> LineElement
        | SceneNode.Path _ -> PathElement
        | Points _ -> PointsElement
        | Vertices _ -> VerticesElement
        | Arc _ -> ArcElement
        | Text _ -> TextElement
        | TextRun _ -> TextRunElement
        | Image _ -> ImageElement
        | ClipNode _ -> ClipElement
        | RegionNode _ -> RegionElement
        | ColorSpaceNode _ -> ColorSpaceElement
        | PerspectiveNode _ -> PerspectiveElement
        | PictureNode _ -> PictureElement
        | Chart _ -> ChartElement
        | Translate _ -> TranslateElement
        | SizedText _ -> SizedTextElement
        | GlyphRun _ -> GlyphRunElement
        | CachedSubtree _ -> GroupElement

    let inspect viewport scene =
        let emptyClip = { X = 0.0; Y = 0.0; Width = 0.0; Height = 0.0 }

        let rec walkScene parentPath path matrix clip (value: Scene) =
            value.Nodes
            |> List.mapi (fun index node -> walkNode parentPath ($"{path}/nodes/{index}") matrix clip node)
            |> List.collect fst

        and walkNode parentPath path matrix clip node =
            let walkChildScene segment childMatrix childClip (childScene: Scene) =
                childScene.Nodes
                |> List.mapi (fun index child ->
                    walkNode (Some path) ($"{path}/{segment}/nodes/{index}") childMatrix childClip child)

            let children, localBounds =
                match node with
                | Empty -> [], SceneDrawableBounds.NoDrawableContent
                | Group scenes ->
                    let nested =
                        scenes
                        |> List.mapi (fun sceneIndex child ->
                            child.Nodes
                            |> List.mapi (fun nodeIndex childNode ->
                                walkNode (Some path) ($"{path}/group/{sceneIndex}/nodes/{nodeIndex}") matrix clip childNode))
                        |> List.collect id
                    nested, nested |> List.map snd |> union
                | Rectangle((x, y, width, height), _) ->
                    [], SceneDrawableBounds.Known { X = x; Y = y; Width = width; Height = height }
                | PaintedRectangle(bounds, paint)
                | Ellipse(bounds, paint)
                | Arc(bounds, _, _, paint) ->
                    [], expandByPaint paint bounds
                | Circle(center, radius, _) ->
                    [], SceneDrawableBounds.Known
                        { X = center.X - radius; Y = center.Y - radius
                          Width = radius * 2.0; Height = radius * 2.0 }
                | FilledEllipse(bounds, _) -> [], SceneDrawableBounds.Known bounds
                | Line(startPoint, endPoint, paint) -> [], pointGeometry 0.5 [ startPoint; endPoint ] paint
                | SceneNode.Path(pathSpec, paint) ->
                    [], Path.bounds pathSpec
                        |> Option.map (expandByPaint paint)
                        |> Option.defaultValue (SceneDrawableBounds.Unknown SceneBoundsUnknownReason.EmptyGeometry)
                | Points(points, paint) -> [], pointGeometry 0.5 points paint
                | Vertices(_, vertices, paint) ->
                    [], pointGeometry (if vertices.Length < 3 then 2.0 else 0.0) (vertices |> List.map _.Position) paint
                | Text((x, y), text, _) ->
                    [], SceneDrawableBounds.Known
                        (textBounds ({ X = x; Y = y }: Point) text
                            { Family = None; Size = 24.0; Weight = None })
                | TextRun run -> [], SceneDrawableBounds.Known (textBounds run.Position run.Text run.Font)
                | Image((x, y, width, height), _) ->
                    [], SceneDrawableBounds.Known { X = x; Y = y; Width = width; Height = height }
                | ClipNode(clipShape, childScene) ->
                    let clipBounds =
                        match clipShape with
                        | RectClip bounds -> SceneDrawableBounds.Known bounds
                        | PathClip pathSpec ->
                            Path.bounds pathSpec
                            |> Option.map SceneDrawableBounds.Known
                            |> Option.defaultValue
                                (SceneDrawableBounds.Unknown SceneBoundsUnknownReason.UnsupportedClipGeometry)
                    let transformedClip =
                        match clipBounds with
                        | SceneDrawableBounds.Known bounds -> transformRect matrix bounds
                        | value -> value
                    let nextClip =
                        match transformedClip, clip with
                        | SceneDrawableBounds.Unknown reason, _ -> Error reason
                        | _, Error reason -> Error reason
                        | SceneDrawableBounds.NoDrawableContent, _ -> Ok (Some emptyClip)
                        | SceneDrawableBounds.Known value, Ok None -> Ok (Some value)
                        | SceneDrawableBounds.Known value, Ok (Some current) ->
                            Ok (Some (intersect current value |> Option.defaultValue emptyClip))
                    let nested = walkChildScene "clip" matrix nextClip childScene
                    nested, nested |> List.map snd |> union
                | RegionNode(region, paint) ->
                    [], region.Bounds
                        |> List.map (expandByPaint paint)
                        |> union
                | ColorSpaceNode(_, childScene) ->
                    let nested = walkChildScene "color-space" matrix clip childScene
                    nested, nested |> List.map snd |> union
                | PerspectiveNode(transform, childScene) ->
                    let nested =
                        walkChildScene "perspective" (multiply matrix (perspective transform)) clip childScene
                    nested, nested |> List.map snd |> union
                | PictureNode picture ->
                    let nested = walkChildScene "picture" matrix clip picture.Scene
                    nested, nested |> List.map snd |> union
                | Chart values ->
                    if values |> List.exists (finite >> not) then
                        [], SceneDrawableBounds.Unknown SceneBoundsUnknownReason.NonFiniteGeometry
                    else
                        let maxValue =
                            match values with
                            | [] -> 0.0
                            | _ -> List.max values
                        values
                        |> List.mapi (fun index value ->
                            if maxValue <= 0.0 || value <= 0.0 then
                                SceneDrawableBounds.NoDrawableContent
                            else
                                let height = value / maxValue * 220.0
                                SceneDrawableBounds.Known
                                    { X = 32.0 + float index * 44.0
                                      Y = 400.0 - height
                                      Width = 32.0
                                      Height = height })
                        |> union
                        |> fun bounds -> [], bounds
                | Translate((dx, dy), childScene) ->
                    let nested =
                        walkChildScene "translate" (multiply matrix (translation dx dy)) clip childScene
                    nested, nested |> List.map snd |> union
                | SizedText((x, y), text, size, _) ->
                    [], SceneDrawableBounds.Known
                        (textBounds ({ X = x; Y = y }: Point) text
                            { Family = None; Size = size; Weight = None })
                | GlyphRun run ->
                    [], SceneDrawableBounds.Known
                        { X = run.Position.X
                          Y = run.Position.Y - run.Data.Metrics.Baseline
                          Width = run.Data.Metrics.Advance
                          Height = run.Data.Metrics.Height }
                | CachedSubtree boundary ->
                    let nested = walkChildScene "cached" matrix clip boundary.Scene
                    nested, nested |> List.map snd |> union

            let effective =
                match children, localBounds with
                | [], SceneDrawableBounds.Known bounds -> transformRect matrix bounds |> applyClip clip
                | [], value -> applyClip clip value
                | _ -> localBounds

            let directChildren =
                children
                |> List.choose (fun (rows, _) -> rows |> List.tryHead |> Option.map _.Path)
            let contributes =
                match effective with
                | SceneDrawableBounds.Known bounds -> bounds.Width > 0.0 && bounds.Height > 0.0
                | SceneDrawableBounds.NoDrawableContent -> false
                | SceneDrawableBounds.Unknown _ -> true
            let row =
                { Path = path
                  ParentPath = parentPath
                  Kind = nodeKind node
                  Bounds = effective
                  ViewportRelation = viewportRelation viewport effective
                  Contributes = contributes
                  Children = directChildren }
            (row :: (children |> List.collect fst)), effective

        walkScene None "" identity (Ok None) scene

    let contributingDescendants (subtreePath: string) (nodes: SceneInspectionNode list) =
        let prefix = subtreePath.TrimEnd('/') + "/"
        nodes
        |> List.filter (fun node ->
            node.Contributes
            && (node.Path = subtreePath
                || node.Path.StartsWith(prefix, StringComparison.Ordinal)))

    let outsideViewport (nodes: SceneInspectionNode list) =
        nodes
        |> List.filter (fun node ->
            node.Contributes
            && (node.ViewportRelation = SceneViewportRelation.PartiallyOutside
                || node.ViewportRelation = SceneViewportRelation.Outside))
