namespace FS.GG.UI.Scene

open System

type SvgAffine =
    { A: float
      B: float
      C: float
      D: float
      E: float
      F: float }

[<RequireQualifiedAccess>]
type SvgAffineError =
    | NonFinite
    | Singular

[<RequireQualifiedAccess>]
module SvgAffine =
    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)
    let identity = { A = 1.0; B = 0.0; C = 0.0; D = 1.0; E = 0.0; F = 0.0 }
    let translate x y = { identity with E = x; F = y }
    let scale x y = { A = x; B = 0.0; C = 0.0; D = y; E = 0.0; F = 0.0 }

    let rotateDegrees angle =
        let radians = angle * Math.PI / 180.0
        let cosine = Math.Cos radians
        let sine = Math.Sin radians
        { A = cosine; B = sine; C = -sine; D = cosine; E = 0.0; F = 0.0 }

    let skewXDegrees angle =
        { identity with C = Math.Tan(angle * Math.PI / 180.0) }

    let skewYDegrees angle =
        { identity with B = Math.Tan(angle * Math.PI / 180.0) }

    let compose parent local =
        { A = parent.A * local.A + parent.C * local.B
          B = parent.B * local.A + parent.D * local.B
          C = parent.A * local.C + parent.C * local.D
          D = parent.B * local.C + parent.D * local.D
          E = parent.A * local.E + parent.C * local.F + parent.E
          F = parent.B * local.E + parent.D * local.F + parent.F }

    let transformPoint (transform: SvgAffine) (point: Point) =
        { X = transform.A * point.X + transform.C * point.Y + transform.E
          Y = transform.B * point.X + transform.D * point.Y + transform.F }

    let isFinite transform =
        finite transform.A && finite transform.B && finite transform.C && finite transform.D
        && finite transform.E && finite transform.F

    let tryInverse transform =
        if not (isFinite transform) then
            Error SvgAffineError.NonFinite
        else
            let determinant = transform.A * transform.D - transform.B * transform.C
            if not (finite determinant) || abs determinant <= 1e-12 then
                Error SvgAffineError.Singular
            else
                Ok
                    { A = transform.D / determinant
                      B = -transform.B / determinant
                      C = -transform.C / determinant
                      D = transform.A / determinant
                      E = (transform.C * transform.F - transform.D * transform.E) / determinant
                      F = (transform.B * transform.E - transform.A * transform.F) / determinant }

[<RequireQualifiedAccess>]
type SvgCoordinateUnits =
    | UserSpaceOnUse
    | ObjectBoundingBox

[<RequireQualifiedAccess>]
type SvgSpreadMethod =
    | Pad
    | Repeat
    | Reflect

type SvgGradientStop =
    { Offset: float
      Color: Color
      StopOpacity: float }

[<RequireQualifiedAccess>]
type SvgGradientGeometry =
    | Linear of Start: Point * Finish: Point
    | Radial of Center: Point * Radius: float * Focal: Point option

type SvgGradientDefinition =
    { Geometry: SvgGradientGeometry
      Units: SvgCoordinateUnits
      Transform: SvgAffine
      Spread: SvgSpreadMethod
      Stops: SvgGradientStop list
      InheritFrom: string option }

[<RequireQualifiedAccess>]
type SvgPaintSource =
    | Solid of Color
    | Definition of string

type SvgStrokePresentation =
    { Source: SvgPaintSource
      Width: float
      Cap: StrokeCap
      Join: StrokeJoin
      Miter: float
      Dash: float list
      DashOffset: float }

type SvgPresentation =
    { FillSource: SvgPaintSource option
      StrokeStyle: SvgStrokePresentation option
      OverallOpacity: float
      FillRule: PathFillType }

[<RequireQualifiedAccess>]
type SvgMaskKind =
    | Alpha
    | Luminance

[<RequireQualifiedAccess>]
type SvgClipShape =
    | Rectangle of Rect
    | Path of PathSpec
    | Intersection of definitionIds: string list

type SvgFontReference =
    { Family: string
      Source: string
      Sha256: string
      License: string }

type SvgElement =
    { Id: string
      SemanticId: string option
      Visible: bool
      Transform: SvgAffine
      ClipId: string option
      MaskId: string option
      Presentation: SvgPresentation option
      Content: SvgElementContent }

and [<RequireQualifiedAccess>] SvgElementContent =
    | SceneLeaf of Scene
    | Group of SvgElement list
    | SymbolInstance of definitionId: string * viewport: Rect option

type SvgDefinition =
    { Id: string
      Content: SvgDefinitionContent }

and [<RequireQualifiedAccess>] SvgDefinitionContent =
    | Symbol of viewBox: Rect option * children: SvgElement list
    | Clip of units: SvgCoordinateUnits * shapes: SvgClipShape list
    | Mask of units: SvgCoordinateUnits * region: Rect * kind: SvgMaskKind * children: SvgElement list
    | Gradient of SvgGradientDefinition
    | Font of SvgFontReference

type SvgDocument =
    { Schema: string
      Id: string
      ViewBox: Rect
      Definitions: SvgDefinition list
      Children: SvgElement list }

type SvgDocumentLimits =
    { MaxSerializedBytes: int
      MaxNodes: int
      MaxPathSegments: int
      MaxDefinitions: int
      MaxReferenceDepth: int
      MaxExpandedSymbolNodes: int }

type SvgDocumentIssue =
    { Code: string
      Location: string
      Message: string }

[<RequireQualifiedAccess>]
type SvgRuntimeSupport =
    | Supported
    | ContractOnly of reason: string
    | Unsupported of reason: string

type SvgAssetDescriptor =
    { AssetId: string
      Version: string
      Sha256: string
      License: string
      Document: SvgDocument }

type SvgBuildExtensionDescriptor =
    { ExtensionId: string
      Version: string
      EntryPoint: string
      Capabilities: string list
      Support: SvgRuntimeSupport }

[<RequireQualifiedAccess>]
module SvgDocument =
    let schema = "fsgg.svg-document/1"

    let defaultLimits =
        { MaxSerializedBytes = 4 * 1024 * 1024
          MaxNodes = 10000
          MaxPathSegments = 100000
          MaxDefinitions = 512
          MaxReferenceDepth = 32
          MaxExpandedSymbolNodes = 50000 }

    let defaultPresentation =
        { FillSource = None
          StrokeStyle = None
          OverallOpacity = 1.0
          FillRule = PathFillType.Winding }

    let private finite value = not (Double.IsNaN value || Double.IsInfinity value)
    let private hex value =
        (value >= '0' && value <= '9') || (value >= 'a' && value <= 'f') || (value >= 'A' && value <= 'F')
    let private issue code path message = { Code = code; Location = path; Message = message }
    let private validRect rect =
        finite rect.X && finite rect.Y && finite rect.Width && finite rect.Height
        && rect.Width >= 0.0 && rect.Height >= 0.0

    let private childPath parent index = $"{parent}/children/{index}"

    let rec private sceneCounts (scene: Scene) =
        let rec nodeCount node =
            match node with
            | SceneNode.Group scenes -> 1 + (scenes |> List.sumBy (sceneCounts >> fst))
            | SceneNode.Translate(_, child)
            | SceneNode.ClipNode(_, child)
            | SceneNode.ColorSpaceNode(_, child) -> 1 + fst (sceneCounts child)
            | SceneNode.PictureNode picture -> 1 + fst (sceneCounts picture.Scene)
            | SceneNode.CachedSubtree cached -> 1 + fst (sceneCounts cached.Scene)
            | _ -> 1
        let rec segmentCount node =
            match node with
            | SceneNode.Path(path, _) -> path.Commands.Length
            | SceneNode.Group scenes -> scenes |> List.sumBy (sceneCounts >> snd)
            | SceneNode.Translate(_, child)
            | SceneNode.ClipNode(_, child)
            | SceneNode.ColorSpaceNode(_, child) -> snd (sceneCounts child)
            | SceneNode.PictureNode picture -> snd (sceneCounts picture.Scene)
            | SceneNode.CachedSubtree cached -> snd (sceneCounts cached.Scene)
            | _ -> 0
        scene.Nodes |> List.sumBy nodeCount, scene.Nodes |> List.sumBy segmentCount

    let private refsOfElement (element: SvgElement) =
        [ yield! element.ClipId |> Option.toList
          yield! element.MaskId |> Option.toList
          match element.Presentation with
          | Some presentation ->
              let sourceRef = function SvgPaintSource.Definition id -> [ id ] | _ -> []
              yield! presentation.FillSource |> Option.toList |> List.collect sourceRef
              yield! presentation.StrokeStyle |> Option.toList |> List.collect (fun stroke -> sourceRef stroke.Source)
          | None -> ()
          match element.Content with
          | SvgElementContent.SymbolInstance(id, _) -> yield id
          | _ -> () ]

    let rec private elementCounts (element: SvgElement) =
        let ownNodes, ownSegments =
            match element.Content with
            | SvgElementContent.SceneLeaf scene -> sceneCounts scene
            | _ -> 0, 0
        match element.Content with
        | SvgElementContent.Group children ->
            let nestedNodes, nestedSegments =
                children |> List.map elementCounts |> List.fold (fun (n, s) (cn, cs) -> n + cn, s + cs) (0, 0)
            1 + ownNodes + nestedNodes, ownSegments + nestedSegments
        | _ -> 1 + ownNodes, ownSegments

    let validate serializedByteCount (limits: SvgDocumentLimits) (document: SvgDocument) =
        let mutable issues: SvgDocumentIssue list = []
        let add code path message = issues <- issue code path message :: issues
        let definitions: Map<string, SvgDefinition> = document.Definitions |> List.map (fun value -> value.Id, value) |> Map.ofList

        let definitionKind (content: SvgDefinitionContent) =
            match content with
            | SvgDefinitionContent.Symbol _ -> "symbol"
            | SvgDefinitionContent.Clip _ -> "clip"
            | SvgDefinitionContent.Mask _ -> "mask"
            | SvgDefinitionContent.Gradient _ -> "gradient"
            | SvgDefinitionContent.Font _ -> "font"

        let requireDefinition expected path reference =
            match definitions |> Map.tryFind reference with
            | None -> add "missing-reference" path $"definition does not exist: {reference}"
            | Some definition when definitionKind definition.Content <> expected ->
                add "wrong-reference-kind" path $"expected {expected} definition but {reference} is {definitionKind definition.Content}"
            | Some _ -> ()

        if document.Schema <> schema then add "unknown-schema" "/schema" $"expected {schema}"
        if String.IsNullOrWhiteSpace document.Id then add "blank-document-id" "/id" "document id must not be blank"
        if not (validRect document.ViewBox) || document.ViewBox.Width <= 0.0 || document.ViewBox.Height <= 0.0 then
            add "invalid-view-box" "/viewBox" "viewBox must be finite with positive width and height"
        if serializedByteCount < 0 || serializedByteCount > limits.MaxSerializedBytes then
            add "document-byte-limit" "/" $"serialized document exceeds {limits.MaxSerializedBytes} bytes"
        if document.Definitions.Length > limits.MaxDefinitions then
            add "definition-limit" "/definitions" $"document exceeds {limits.MaxDefinitions} definitions"

        let duplicateIds kind path ids =
            ids |> List.countBy id |> List.iter (fun (id, count) -> if count > 1 then add "duplicate-id" path $"{kind} id is duplicated: {id}")
        duplicateIds "definition" "/definitions" (document.Definitions |> List.map _.Id)

        let mutable allElementIds = []
        let mutable allSemanticIds = []
        let rec validateElement path (element: SvgElement) =
            allElementIds <- element.Id :: allElementIds
            element.SemanticId |> Option.iter (fun id -> allSemanticIds <- id :: allSemanticIds)
            if String.IsNullOrWhiteSpace element.Id then add "blank-element-id" (path + "/id") "element id must not be blank"
            if element.SemanticId |> Option.exists String.IsNullOrWhiteSpace then
                add "blank-semantic-id" (path + "/semanticId") "semantic id must not be blank"
            if not (SvgAffine.isFinite element.Transform) then add "non-finite-transform" (path + "/transform") "transform must be finite"
            element.ClipId |> Option.iter (requireDefinition "clip" path)
            element.MaskId |> Option.iter (requireDefinition "mask" path)
            match element.Presentation with
            | Some presentation ->
                let requirePaint = function SvgPaintSource.Definition reference -> requireDefinition "gradient" path reference | _ -> ()
                presentation.FillSource |> Option.iter requirePaint
                presentation.StrokeStyle |> Option.iter (fun stroke -> requirePaint stroke.Source)
            | None -> ()
            match element.Content with
            | SvgElementContent.Group children -> children |> List.iteri (fun index child -> validateElement (childPath path index) child)
            | SvgElementContent.SceneLeaf scene ->
                let validationScene =
                    { RootId = "document-validation"
                      Revision = 0
                      Camera = { PanX = 0.0; PanY = 0.0; Zoom = 1.0 }
                      Layers =
                        [ { Id = "document"
                            Visible = true
                            Objects =
                              [ { Id = element.Id
                                  Selectable = element.SemanticId.IsSome
                                  AccessibleLabel = element.SemanticId |> Option.defaultValue element.Id
                                  Content = scene } ] } ] }
                match SvgRetained.project validationScene with
                | SvgAdapterResult.Rendered _ -> ()
                | SvgAdapterResult.Invalid adapterIssues
                | SvgAdapterResult.Unsupported adapterIssues ->
                    adapterIssues |> List.iter (fun adapterIssue -> add "unsupported-scene-leaf" path adapterIssue.Reason)
            | SvgElementContent.SymbolInstance(_, Some viewport) when not (validRect viewport) ->
                add "invalid-symbol-viewport" (path + "/viewport") "symbol viewport must be finite and non-negative"
            | SvgElementContent.SymbolInstance(reference, _) -> requireDefinition "symbol" path reference

        let rec validateDefinition path (definition: SvgDefinition) =
            if String.IsNullOrWhiteSpace definition.Id then add "blank-definition-id" (path + "/id") "definition id must not be blank"
            match definition.Content with
            | SvgDefinitionContent.Symbol(Some viewBox, _) when not (validRect viewBox) ->
                add "invalid-symbol-view-box" (path + "/viewBox") "symbol viewBox must be finite and non-negative"
            | SvgDefinitionContent.Symbol(_, children) ->
                children |> List.iteri (fun index child -> validateElement (childPath path index) child)
            | SvgDefinitionContent.Clip(_, shapes) ->
                shapes |> List.iteri (fun index shape ->
                    match shape with
                    | SvgClipShape.Rectangle rect when not (validRect rect) -> add "invalid-clip-rectangle" $"{path}/shapes/{index}" "clip rectangle must be finite"
                    | SvgClipShape.Intersection references ->
                        references |> List.iter (requireDefinition "clip" path)
                    | _ -> ())
            | SvgDefinitionContent.Mask(_, region, _, children) ->
                if not (validRect region) then add "invalid-mask-region" (path + "/region") "mask region must be finite"
                children |> List.iteri (fun index child -> validateElement (childPath path index) child)
            | SvgDefinitionContent.Gradient gradient ->
                if not (SvgAffine.isFinite gradient.Transform) then add "non-finite-transform" (path + "/transform") "gradient transform must be finite"
                gradient.InheritFrom |> Option.iter (requireDefinition "gradient" path)
                gradient.Stops |> List.iteri (fun index stop ->
                    if not (finite stop.Offset) || stop.Offset < 0.0 || stop.Offset > 1.0 then add "invalid-gradient-stop" $"{path}/stops/{index}" "offset must be between zero and one"
                    if not (finite stop.StopOpacity) || stop.StopOpacity < 0.0 || stop.StopOpacity > 1.0 then add "invalid-gradient-stop" $"{path}/stops/{index}" "opacity must be between zero and one")
                if gradient.Stops <> (gradient.Stops |> List.sortBy _.Offset) then add "unordered-gradient-stops" (path + "/stops") "gradient stops must be ordered"
            | SvgDefinitionContent.Font font ->
                if String.IsNullOrWhiteSpace font.Family || String.IsNullOrWhiteSpace font.Source || String.IsNullOrWhiteSpace font.Sha256 || String.IsNullOrWhiteSpace font.License then
                    add "invalid-font-reference" path "font family, source, sha256 and license are required"
                let source = font.Source.Replace('\\', '/')
                if source.StartsWith("/") || source.Contains(":") || (source.Split('/') |> Array.exists ((=) "..")) || not (source.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase)) then
                    add "external-font-source" (path + "/source") "font source must be a local relative WOFF2 path"
                if font.Sha256.Length <> 64 || font.Sha256 |> Seq.exists (hex >> not) then
                    add "invalid-font-sha256" (path + "/sha256") "font sha256 must be 64 hexadecimal characters"

        document.Children |> List.iteri (fun index child -> validateElement (childPath "" index) child)
        document.Definitions |> List.iteri (fun index definition -> validateDefinition $"/definitions/{index}" definition)
        duplicateIds "element" "/children" allElementIds
        duplicateIds "semantic" "/children" allSemanticIds
        duplicateIds "document" "/" (document.Id :: ((document.Definitions |> List.map _.Id) @ allElementIds))

        let definitionRefs (definition: SvgDefinition) =
            let rec fromElements (elements: SvgElement list) =
                elements |> List.collect (fun (element: SvgElement) ->
                    refsOfElement element @
                    (match element.Content with SvgElementContent.Group children -> fromElements children | _ -> []))
            match definition.Content with
            | SvgDefinitionContent.Symbol(_, children)
            | SvgDefinitionContent.Mask(_, _, _, children) -> fromElements children
            | SvgDefinitionContent.Clip(_, shapes) -> shapes |> List.collect (function SvgClipShape.Intersection ids -> ids | _ -> [])
            | SvgDefinitionContent.Gradient gradient -> gradient.InheritFrom |> Option.toList
            | SvgDefinitionContent.Font _ -> []

        let rec visit stack depth id =
            if depth > limits.MaxReferenceDepth then add "reference-depth" "/definitions" $"reference depth exceeds {limits.MaxReferenceDepth}: {id}"
            elif List.contains id stack then
                let cycle = String.concat " -> " (List.rev (id :: stack))
                add "cyclic-reference" "/definitions" $"definition cycle: {cycle}"
            else definitions |> Map.tryFind id |> Option.iter (fun definition -> definitionRefs definition |> List.iter (visit (id :: stack) (depth + 1)))
        document.Definitions |> List.iter (fun definition -> visit [] 1 definition.Id)

        let directNodes, pathSegments =
            document.Children @
            (document.Definitions |> List.collect (fun definition ->
                match definition.Content with
                | SvgDefinitionContent.Symbol(_, children)
                | SvgDefinitionContent.Mask(_, _, _, children) -> children
                | _ -> []))
            |> List.map elementCounts
            |> List.fold (fun (n, s) (cn, cs) -> n + cn, s + cs) (0, 0)
        if directNodes > limits.MaxNodes then add "node-limit" "/children" $"document exceeds {limits.MaxNodes} nodes"
        if pathSegments > limits.MaxPathSegments then add "path-segment-limit" "/children" $"document exceeds {limits.MaxPathSegments} path segments"

        let rec expanded stack depth (element: SvgElement) =
            if depth > limits.MaxReferenceDepth then 0
            else
                match element.Content with
                | SvgElementContent.Group children -> 1 + (children |> List.sumBy (expanded stack (depth + 1)))
                | SvgElementContent.SymbolInstance(id, _) when not (List.contains id stack) ->
                    match definitions |> Map.tryFind id with
                    | Some { Content = SvgDefinitionContent.Symbol(_, children) } -> 1 + (children |> List.sumBy (expanded (id :: stack) (depth + 1)))
                    | _ -> 1
                | _ -> 1
        let expandedNodes = document.Children |> List.sumBy (expanded [] 1)
        if expandedNodes > limits.MaxExpandedSymbolNodes then add "expanded-symbol-limit" "/children" $"expanded symbols exceed {limits.MaxExpandedSymbolNodes} nodes"
        List.rev issues

    let ofRetainedScene (viewBox: Rect) (scene: RetainedScene) =
        match SvgRetained.project scene with
        | SvgAdapterResult.Invalid adapterIssues
        | SvgAdapterResult.Unsupported adapterIssues ->
            adapterIssues
            |> List.map (fun value ->
                { Code = "retained-scene"
                  Location = value.NodePath |> List.map string |> String.concat "/" |> fun path -> "/scene/" + path
                  Message = value.Reason })
            |> Error
        | SvgAdapterResult.Rendered accepted ->
            let presentation = defaultPresentation
            let children =
                accepted.Layers
                |> List.map (fun layer ->
                    { Id = "layer:" + layer.Id
                      SemanticId = None
                      Visible = layer.Visible
                      Transform = SvgAffine.identity
                      ClipId = None
                      MaskId = None
                      Presentation = None
                      Content =
                        SvgElementContent.Group(
                            layer.Objects
                            |> List.map (fun value ->
                                { Id = "object:" + value.Id
                                  SemanticId = Some value.Id
                                  Visible = true
                                  Transform = SvgAffine.identity
                                  ClipId = None
                                  MaskId = None
                                  Presentation = Some presentation
                                  Content = SvgElementContent.SceneLeaf value.Content })) })
            let document =
                { Schema = schema
                  Id = accepted.RootId
                  ViewBox = viewBox
                  Definitions = []
                  Children = children }
            let issues = validate 0 defaultLimits document
            if issues.IsEmpty then Ok document else Error issues
