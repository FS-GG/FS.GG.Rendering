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

module ControlInspection =
    let private rectContains (outer: Rect) (inner: Rect) =
        inner.X >= outer.X
        && inner.Y >= outer.Y
        && inner.X + inner.Width <= outer.X + outer.Width
        && inner.Y + inner.Height <= outer.Y + outer.Height

    let private controlId path (control: Control<'msg>) =
        control.Key |> Option.defaultValue path

    let private kindOf path (control: Control<'msg>) =
        if path = "0" then
            VisualInspectionNodeKind.Root
        else
            match control.Kind with
            | "text-block"
            | "label"
            | "button"
            | "validation-message"
            | "tooltip"
            | "toast" -> VisualInspectionNodeKind.Text
            | "image" -> VisualInspectionNodeKind.Image
            | "overlay" -> VisualInspectionNodeKind.Overlay
            | "popup" -> VisualInspectionNodeKind.Popup
            | "stack"
            | "panel"
            | "scroll-viewer"
            | "data-grid" -> VisualInspectionNodeKind.Container
            | value -> VisualInspectionNodeKind.Custom value

    let private surfaceRoleOf path (control: Control<'msg>) =
        if path = "0" then
            VisualInspectionSurfaceRole.Root
        else
            match control.Kind with
            | "overlay" -> VisualInspectionSurfaceRole.Overlay
            | "popup"
            | "tooltip" -> VisualInspectionSurfaceRole.Popup
            | "toast"
            | "validation-message" -> VisualInspectionSurfaceRole.Feedback
            | "menu"
            | "tabs" -> VisualInspectionSurfaceRole.Navigation
            | _ -> VisualInspectionSurfaceRole.Content

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
