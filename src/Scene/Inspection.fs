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
