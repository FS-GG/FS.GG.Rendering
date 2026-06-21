namespace FS.GG.UI.Controls

open System
open FS.GG.UI.DesignSystem
open FS.GG.UI.Scene

type ControlInspectionRequest<'msg> =
    { Scope: VisualInspectionScope
      Theme: Theme
      OutputSize: Size
      Control: Control<'msg>
      Presentation: string
      RunId: string option
      RelatedVisualEvidence: string list }

type RetainedControlTransition<'msg> =
    { TransitionId: string
      PriorControl: Control<'msg> option
      CurrentControl: Control<'msg>
      InteractionId: string option
      ExpectedAffectedRegionIds: string list
      MaximumDirtyPercentage: float option
      IntentionalExceptions: IntentionalDamageException list }

type RetainedControlInspectionRequest<'msg> =
    { Scope: VisualInspectionScope
      Theme: Theme
      OutputSize: Size
      Presentation: string
      RunId: string option
      Transition: RetainedControlTransition<'msg>
      RelatedVisualEvidence: string list }

module ControlInspection =
    let private rectContains (outer: Rect) (inner: Rect) =
        inner.X >= outer.X
        && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width
        && inner.Y + inner.Height <= outer.Y + outer.Height

    let private controlId path (control: Control<'msg>) =
        control.Key |> Option.defaultValue path

    // Feature 183 (US1): the non-root per-Kind inspection node-kind / surface-role dispatch now lives
    // in the single ControlKindRegistry SSOT (byte-identical, incl. the `Custom value` / `Content`
    // defaults). The root case (`path = "0"`) stays here — it is path-keyed, not kind-keyed.
    let private kindOf path (control: Control<'msg>) =
        if path = "0" then
            VisualInspectionNodeKind.Root
        else
            ControlKindRegistry.inspectionNodeKind control.Kind

    let private surfaceRoleOf path (control: Control<'msg>) =
        if path = "0" then
            VisualInspectionSurfaceRole.Root
        else
            ControlKindRegistry.surfaceRole control.Kind

    let private paintRoleOf path (control: Control<'msg>) =
        if path = "0" then
            VisualInspectionPaintRole.Background
        elif Control.isOverlaySurface control then
            VisualInspectionPaintRole.Overlay
        elif List.isEmpty control.Children then
            VisualInspectionPaintRole.Content
        else
            VisualInspectionPaintRole.Border

    let private clipStatusOf (control: Control<'msg>) =
        match control.Kind with
        | "scroll-viewer" -> VisualInspectionClipStatus.Intentional
        | _ when List.isEmpty control.Children -> VisualInspectionClipStatus.None
        | _ -> VisualInspectionClipStatus.Intentional

    let private textBounds (theme: Theme) (owner: Rect) (text: string) =
        let font: FontSpec = { Family = theme.FontFamily; Size = theme.FontSize; Weight = None }
        let metrics = Scene.measureTextResolved text font

        ({ X = owner.X + 8.0
           Y = owner.Y + max 0.0 ((owner.Height - metrics.Height) * 0.5)
           Width = metrics.Width
           Height = metrics.Height }: Rect),
        metrics

    let private textRunFor (theme: Theme) (id: string) (bounds: Rect option) (control: Control<'msg>) =
        match control.Content, bounds with
        | Some text, Some owner when not (String.IsNullOrWhiteSpace text) ->
            let textRect, metrics = textBounds theme owner text
            let fitStatus =
                if rectContains owner textRect then
                    VisualInspectionFitStatus.Inside
                else
                    VisualInspectionFitStatus.Overflow

            Some
                ({ TextId = id + ":text"
                   OwnerNodeId = id
                   Text = text
                   TextBounds = Some textRect
                   OwnerBounds = Some owner
                   Baseline = Some metrics.Baseline
                   MeasurementMode = VisualInspectionMeasurementMode.Approximate
                   FitStatus = fitStatus
                   Required = true
                   Diagnostics =
                     [ if fitStatus = VisualInspectionFitStatus.Overflow then
                           "text bounds exceed owner bounds" ] }: VisualTextInspection)
        | Some text, None when not (String.IsNullOrWhiteSpace text) ->
            Some
                ({ TextId = id + ":text"
                   OwnerNodeId = id
                   Text = text
                   TextBounds = None
                   OwnerBounds = None
                   Baseline = None
                   MeasurementMode = VisualInspectionMeasurementMode.Unavailable
                   FitStatus = VisualInspectionFitStatus.Unavailable
                   Required = true
                   Diagnostics = [ "owner bounds unavailable" ] }: VisualTextInspection)
        | _ -> None

    let private inspectCore
        (scope: VisualInspectionScope)
        (presentation: string)
        (outputSize: Size)
        (theme: Theme option)
        (control: Control<'msg>)
        (render: ControlRenderResult<'msg>)
        =
        let boundsById = render.Bounds |> Map.ofList
        let unsupportedFacts = ResizeArray<VisualInspectionUnsupportedFact>()

        let rec walk (path: string) (parentId: string option) (z: int) (current: Control<'msg>) =
            let id = controlId path current
            let bounds = boundsById |> Map.tryFind id
            let textRun = theme |> Option.bind (fun t -> textRunFor t id bounds current)
            let childIds =
                current.Children
                |> List.mapi (fun index child -> controlId (path + "." + string index) child)

            let ownUnsupported =
                [ if current.Kind.Contains("transform", StringComparison.OrdinalIgnoreCase) then
                      VisualInspection.unsupportedFact
                          "transform-bounds"
                          (Some id)
                          true
                          "transformed bounds are not represented by the first inspection adapter"
                          "ControlInspection records this node as unsupported instead of inferring rectangular bounds"
                          false ]

            ownUnsupported |> List.iter unsupportedFacts.Add

            let node: VisualInspectionNode =
                { NodeId = id
                  ParentId = parentId
                  Kind = kindOf path current
                  OwnerId = current.Key
                  Bounds = bounds
                  Clip = clipStatusOf current
                  ZOrder = z
                  PaintRole = paintRoleOf path current
                  SurfaceRole = surfaceRoleOf path current
                  TextRunIds = textRun |> Option.map (fun run -> [ run.TextId ]) |> Option.defaultValue []
                  Children = childIds
                  Dynamic = false
                  UnsupportedFacts = ownUnsupported }

            let region: VisualRegionBoundary =
                { RegionId = id
                  Name = current.Key |> Option.defaultValue current.Kind
                  Role = node.SurfaceRole
                  Bounds = bounds
                  Required = path = "0"
                  OwnerNodeIds = [ id ]
                  AllowedOverlapRoles =
                    [ VisualInspectionSurfaceRole.Overlay
                      VisualInspectionSurfaceRole.Popup
                      VisualInspectionSurfaceRole.Floating ] }

            let coverage: VisualPaintCoverage =
                { CoverageId = id + ":paint"
                  TargetId = id
                  PaintRole = node.PaintRole
                  CoverageBounds = bounds
                  CoverageStatus =
                    if bounds.IsSome then
                        VisualInspectionCoverageStatus.Complete
                    else
                        VisualInspectionCoverageStatus.Unavailable
                  Reason =
                    if bounds.IsSome then
                        None
                    else
                        Some "bounds unavailable" }

            let clip: VisualClipFact =
                { ClipId = id + ":clip"
                  NodeId = id
                  ClipBounds = bounds
                  ClipStatus = node.Clip
                  Reason =
                    match node.Clip with
                    | VisualInspectionClipStatus.Intentional -> Some "container or scroll clipping is part of Control.renderTree composition"
                    | _ -> None
                  AffectedTextRunIds = node.TextRunIds }

            let childResults =
                current.Children
                |> List.mapi (fun index child -> walk (path + "." + string index) (Some id) (z + index + 1) child)

            let nodes = node :: (childResults |> List.collect (fun (nodes, _, _, _, _) -> nodes))
            let regions = region :: (childResults |> List.collect (fun (_, regions, _, _, _) -> regions))
            let textRuns =
                [ match textRun with
                  | Some run -> yield run
                  | None -> ()
                  yield! childResults |> List.collect (fun (_, _, textRuns, _, _) -> textRuns) ]
            let paintCoverage = coverage :: (childResults |> List.collect (fun (_, _, _, paintCoverage, _) -> paintCoverage))
            let clipFacts = clip :: (childResults |> List.collect (fun (_, _, _, _, clipFacts) -> clipFacts))

            nodes, regions, textRuns, paintCoverage, clipFacts

        let nodes, regions, textRuns, paintCoverage, clipFacts = walk "0" None 0 control

        let artifact: VisualInspectionArtifact =
            { ArtifactId = $"{scope.ScopeId}:{outputSize.Width}x{outputSize.Height}:{presentation}"
              Scope = scope
              OutputSize = outputSize
              Presentation = presentation
              ReadinessStatus = VisualInspectionStatus.Accepted
              Nodes = nodes
              Regions = regions
              TextRuns = textRuns
              PaintCoverage = paintCoverage
              ClipFacts = clipFacts
              Findings = []
              UnsupportedFacts = List.ofSeq unsupportedFacts
              Diagnostics = render.Diagnostics |> List.map _.Message
              GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("O") }

        VisualInspection.normalizeArtifact
            { artifact with
                Diagnostics = artifact.Diagnostics @ VisualInspection.artifactDiagnostics artifact }

    let inspectRendered scope presentation outputSize control render =
        inspectCore scope presentation outputSize None control render

    let inspect (request: ControlInspectionRequest<'msg>) =
        let render = Control.renderTree request.Theme request.OutputSize request.Control
        inspectCore request.Scope request.Presentation request.OutputSize (Some request.Theme) request.Control render

    type private FlatControl<'msg> =
        { NodeId: string
          ParentId: string option
          Control: Control<'msg>
          Bounds: Rect option }

    let private frameBounds (size: Size) : Rect =
        { X = 0.0
          Y = 0.0
          Width = float size.Width
          Height = float size.Height }

    let private rectEqual (a: Rect option) (b: Rect option) =
        match a, b with
        | None, None -> true
        | Some left, Some right -> left = right
        | _ -> false

    let private controlSignature (control: Control<'msg>) =
        control.Kind, control.Content, control.Children.Length

    let private flattenControl (render: ControlRenderResult<'msg>) (control: Control<'msg>) =
        let boundsById = render.Bounds |> Map.ofList

        let rec walk path parent current =
            let id = controlId path current
            let row =
                { NodeId = id
                  ParentId = parent
                  Control = current
                  Bounds = boundsById |> Map.tryFind id }

            row
            :: (current.Children
                |> List.mapi (fun index child -> walk (path + "." + string index) (Some id) child)
                |> List.concat)

        walk "0" None control
        |> List.map (fun row -> row.NodeId, row)
        |> Map.ofList

    let private retainedIdentityMap (retained: RetainedRender<'msg>) =
        let rec walk path (node: RetainedNode<'msg>) =
            let id = controlId path node.Control
            let (RetainedId raw) = node.Identity
            (id, $"retained:{raw}")
            :: (node.Children
                |> List.mapi (fun index child -> walk (path + "." + string index) child)
                |> List.concat)

        walk "0" retained.Root |> Map.ofList

    let private nodeUnsupported nodeId status prior current =
        match status, prior, current with
        | RetainedNodeStatus.Shifted, None, _
        | RetainedNodeStatus.Shifted, _, None
        | RetainedNodeStatus.ShiftedAndRepainted, None, _
        | RetainedNodeStatus.ShiftedAndRepainted, _, None ->
            [ RetainedInspection.unsupportedFact
                  "shifted-node-bounds"
                  (Some nodeId)
                  true
                  "shifted nodes require prior and current bounds"
                  "ControlInspection could not correlate both bounds for this shifted node"
                  false ]
        | _ -> []

    let private retainedNodeFacts prior current identities expectedAffectedRegionIds =
        let priorIds = prior |> Map.toList |> List.map fst
        let currentIds = current |> Map.toList |> List.map fst
        let allIds = (priorIds @ currentIds) |> List.distinct |> List.sort

        allIds
        |> List.map (fun id ->
            let before = prior |> Map.tryFind id
            let after = current |> Map.tryFind id
            let priorBounds = before |> Option.bind _.Bounds
            let currentBounds = after |> Option.bind _.Bounds
            let shifted = not (rectEqual priorBounds currentBounds)
            let repainted =
                match before, after with
                | Some left, Some right -> controlSignature left.Control <> controlSignature right.Control
                | None, Some _
                | Some _, None -> true
                | None, None -> false

            let status =
                match before, after with
                | None, Some _ -> RetainedNodeStatus.Added
                | Some _, None -> RetainedNodeStatus.Removed
                | Some _, Some _ when shifted && repainted -> RetainedNodeStatus.ShiftedAndRepainted
                | Some _, Some _ when shifted -> RetainedNodeStatus.Shifted
                | Some _, Some _ when repainted -> RetainedNodeStatus.Repainted
                | Some _, Some _ -> RetainedNodeStatus.Reused
                | None, None -> RetainedNodeStatus.Unsupported

            let owner = after |> Option.orElse before
            let unsupported = nodeUnsupported id status priorBounds currentBounds

            { NodeId = id
              ParentId = owner |> Option.bind _.ParentId
              RetainedIdentity = identities |> Map.tryFind id
              Kind = owner |> Option.map (fun row -> row.Control.Kind) |> Option.defaultValue "unknown"
              OwnerId = owner |> Option.bind (fun row -> row.Control.Key)
              Status = status
              PriorBounds = priorBounds
              CurrentBounds = currentBounds
              AffectedRegionIds =
                if status = RetainedNodeStatus.Reused then
                    []
                else
                    expectedAffectedRegionIds |> List.distinct |> List.sort
              Repainted = repainted
              Shifted = shifted
              UnsupportedFacts = unsupported
              Diagnostics = [] })

    let private dirtyRects (nodes: RetainedNodeInspection list) =
        nodes
        |> List.collect (fun node ->
            match node.Status with
            | RetainedNodeStatus.Reused
            | RetainedNodeStatus.Retained
            | RetainedNodeStatus.Unaffected
            | RetainedNodeStatus.Unsupported -> []
            | RetainedNodeStatus.Added -> node.CurrentBounds |> Option.map List.singleton |> Option.defaultValue []
            | RetainedNodeStatus.Removed -> node.PriorBounds |> Option.map List.singleton |> Option.defaultValue []
            | RetainedNodeStatus.Repainted -> node.CurrentBounds |> Option.map List.singleton |> Option.defaultValue []
            | RetainedNodeStatus.Shifted
            | RetainedNodeStatus.ShiftedAndRepainted -> [ node.PriorBounds; node.CurrentBounds ] |> List.choose id)

    let private artifactFromRetained
        (request: RetainedControlInspectionRequest<'msg>)
        (transition: RetainedControlTransition<'msg>)
        (finalRender: ControlRenderResult<'msg>)
        (finalControl: Control<'msg>)
        (identities: Map<string, string>)
        (workReduction: WorkReductionRecord option)
        (diagnostics: string list)
        : RetainedInspectionArtifact =
        let priorRender, priorControl =
            match transition.PriorControl with
            | Some prior ->
                let init = RetainedRender.init request.Theme request.OutputSize prior
                Some init.Render, Some prior
            | None -> None, None

        let priorMap =
            match priorRender, priorControl with
            | Some render, Some control -> flattenControl render control
            | _ -> Map.empty

        let currentMap = flattenControl finalRender finalControl
        let nodes = retainedNodeFacts priorMap currentMap identities transition.ExpectedAffectedRegionIds
        let affectedNodeIds =
            nodes
            |> List.filter (fun node -> node.Status <> RetainedNodeStatus.Reused)
            |> List.map _.NodeId

        let repaintedCount =
            workReduction |> Option.map _.RepaintedNodeCount |> Option.defaultValue (nodes |> List.filter _.Repainted |> List.length)

        let shiftedCount =
            workReduction |> Option.map _.ShiftedNodeCount |> Option.defaultValue (nodes |> List.filter _.Shifted |> List.length)

        let unaffectedCount =
            nodes |> List.filter (fun node -> node.Status = RetainedNodeStatus.Reused || node.Status = RetainedNodeStatus.Unaffected) |> List.length

        let damage =
            match transition.PriorControl with
            | None ->
                Some
                    { TransitionId = transition.TransitionId
                      DamageStatus = DamageInspectionStatus.NotInspected
                      FrameBounds = frameBounds request.OutputSize
                      DirtyRectangles = []
                      UnionBounds = None
                      UnionArea = 0
                      VisibleDirtyArea = 0
                      DirtyPercentage = 0.0
                      AffectedRegionIds = []
                      AffectedNodeIds = []
                      RepaintedNodeCount = 0
                      ShiftedNodeCount = 0
                      UnaffectedNodeCount = unaffectedCount
                      Cause = Some "first-frame-no-prior"
                      Diagnostics = [ "first frame has no prior retained frame for damage comparison" ] }
            | Some _ ->
                Some(
                    RetainedInspection.damageRegion
                        transition.TransitionId
                        (frameBounds request.OutputSize)
                        (dirtyRects nodes)
                        transition.ExpectedAffectedRegionIds
                        affectedNodeIds
                        { Repainted = repaintedCount
                          Shifted = shiftedCount
                          Unaffected = unaffectedCount }
                        transition.InteractionId
                        transition.MaximumDirtyPercentage)

        let frameTransition: RetainedFrameTransition =
            { TransitionId = transition.TransitionId
              PriorFrameId = transition.PriorControl |> Option.map (fun _ -> transition.TransitionId + ":prior")
              CurrentFrameId = transition.TransitionId + ":current"
              InteractionId = transition.InteractionId
              ExpectedAffectedRegionIds = transition.ExpectedAffectedRegionIds
              MaximumDirtyPercentage = transition.MaximumDirtyPercentage
              IntentionalExceptions = transition.IntentionalExceptions }

        let finalVisual =
            inspectCore request.Scope request.Presentation request.OutputSize (Some request.Theme) finalControl finalRender

        let unsupported = nodes |> List.collect _.UnsupportedFacts

        let artifact: RetainedInspectionArtifact =
            { ArtifactId = $"{request.Scope.ScopeId}:{transition.TransitionId}:{request.OutputSize.Width}x{request.OutputSize.Height}:{request.Presentation}"
              RunId = request.RunId |> Option.defaultValue "retained-inspection"
              Scope = request.Scope
              OutputSize = request.OutputSize
              Presentation = request.Presentation
              Transition = Some frameTransition
              FinalVisualArtifact = Some finalVisual
              RetainedNodes = nodes
              Damage = damage
              Findings = []
              UnsupportedFacts = unsupported
              RelatedVisualEvidence = request.RelatedVisualEvidence
              ReadinessStatus =
                if unsupported |> List.exists _.Required then
                    RetainedInspectionStatus.Unsupported
                else
                    RetainedInspectionStatus.Accepted
              Diagnostics = diagnostics
              GeneratedAtUtc = DateTimeOffset.UtcNow.ToString("O") }

        RetainedInspection.normalizeArtifact
            { artifact with
                Diagnostics = artifact.Diagnostics @ RetainedInspection.artifactDiagnostics artifact }

    let inspectRetained (request: RetainedControlInspectionRequest<'msg>) =
        match request.Transition.PriorControl with
        | None ->
            let init = RetainedRender.init request.Theme request.OutputSize request.Transition.CurrentControl
            artifactFromRetained
                request
                request.Transition
                init.Render
                request.Transition.CurrentControl
                (retainedIdentityMap init.Retained)
                None
                (init.Diagnostics |> List.map _.Message)
        | Some prior ->
            let init = RetainedRender.init request.Theme request.OutputSize prior
            let step = RetainedRender.step request.Theme request.OutputSize init.Retained request.Transition.CurrentControl
            artifactFromRetained
                request
                request.Transition
                step.Render
                request.Transition.CurrentControl
                (retainedIdentityMap step.Retained)
                (Some step.WorkReduction)
                ((init.Diagnostics @ step.Diagnostics) |> List.map _.Message)
